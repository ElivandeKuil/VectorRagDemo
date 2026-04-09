using Npgsql;
using VectorRagDemo.BLL.Processors;
using VectorRagDemo.DAL;
using VectorRagDemo.Services;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Re-embeds existing chunks that were embedded with the old Gemini model (768D)
    /// into the new tekstvector_mistral column (1024D) using Mistral.
    /// </summary>
    public class ReembedService
    {
        private readonly EmbeddingProcessor _embeddingProcessor;
        private readonly string _connectionString;

        public record ReembedProgress(int WithEmbedding, int Total);

        public ReembedService(HttpClient httpClient, LogboekDbContext logboekContext, string connectionString)
        {
            _embeddingProcessor = new EmbeddingProcessor(httpClient, logboekContext);
            _connectionString = connectionString;
        }

        /// <summary>
        /// Returns how many active chunks already have a Mistral embedding and the total count.
        /// </summary>
        public async Task<ReembedProgress> GetProgressAsync(int? projectId = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = projectId.HasValue
                ? @"SELECT
                        COUNT(*) FILTER (WHERE c.tekstvector_mistral IS NOT NULL) AS with_emb,
                        COUNT(*) AS total
                    FROM chunk c
                    INNER JOIN bron b ON c.bronid = b.id
                    WHERE c.status = 1 AND b.project = @ProjectId"
                : @"SELECT
                        COUNT(*) FILTER (WHERE tekstvector_mistral IS NOT NULL) AS with_emb,
                        COUNT(*) AS total
                    FROM chunk
                    WHERE status = 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (projectId.HasValue)
                cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new ReembedProgress(
                WithEmbedding: reader.GetInt32(0),
                Total: reader.GetInt32(1)
            );
        }

        /// <summary>
        /// Fetches up to <paramref name="batchSize"/> chunks that still need a Mistral embedding,
        /// generates embeddings, persists them, and returns updated progress.
        /// </summary>
        public async Task<ReembedProgress> ProcessBatchAsync(int batchSize = 50, int? projectId = null)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Fetch a batch of chunk IDs and texts that have no Mistral embedding yet
            var selectSql = projectId.HasValue
                ? @"SELECT c.id, c.tekst FROM chunk c
                    INNER JOIN bron b ON c.bronid = b.id
                    WHERE c.status = 1
                    AND c.tekstvector_mistral IS NULL
                    AND b.project = @ProjectId
                    ORDER BY c.id ASC
                    LIMIT @BatchSize"
                : @"SELECT id, tekst FROM chunk
                    WHERE status = 1
                    AND tekstvector_mistral IS NULL
                    ORDER BY id ASC
                    LIMIT @BatchSize";

            var chunks = new List<(int Id, string Tekst)>();
            await using (var cmd = new NpgsqlCommand(selectSql, conn))
            {
                cmd.Parameters.AddWithValue("@BatchSize", batchSize);
                if (projectId.HasValue)
                    cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    chunks.Add((reader.GetInt32(0), reader.GetString(1)));
            }

            if (chunks.Count == 0)
                return await GetProgressAsync(projectId);

            // Send to Mistral in small sub-batches to stay within API request size limits
            const int EmbedSubBatchSize = 10;
            const string updateSql = "UPDATE chunk SET tekstvector_mistral = @Vector::vector WHERE id = @ID";

            for (int offset = 0; offset < chunks.Count; offset += EmbedSubBatchSize)
            {
                var subBatch = chunks.Skip(offset).Take(EmbedSubBatchSize).ToList();

                var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(
                    subBatch.Select(c => c.Tekst));

                if (embeddings.Count != subBatch.Count)
                    throw new Exception($"Embedding count mismatch: got {embeddings.Count}, expected {subBatch.Count}.");

                for (int i = 0; i < subBatch.Count; i++)
                {
                    var vectorString = "[" + string.Join(",",
                        embeddings[i].Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

                    await using var updateCmd = new NpgsqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@Vector", vectorString);
                    updateCmd.Parameters.AddWithValue("@ID", subBatch[i].Id);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            AppLogger.Log($"Re-embedded {chunks.Count} chunks (sub-batches of {EmbedSubBatchSize})", source: nameof(ReembedService));
            return await GetProgressAsync(projectId);
        }
    }
}
