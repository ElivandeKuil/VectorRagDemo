using Npgsql;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Services;

namespace VectorRagDemo.BLL
{
    public class VectorQueryProcessor
    {
        VectorDbContext _context;

        public VectorQueryProcessor(VectorDbContext context)
        {
            _context = context;
        }
        public async Task<string> QueryLocalVectorDbAsync(
            string connectionString,
            List<float> queryEmbedding,
            int topK)
        {
            try
            {
                var neighbors = await GetNearestNeighbors(connectionString, queryEmbedding, topK);

                if (neighbors == null || !neighbors.Any())
                {
                    AppLogger.LogWarning("Vector search returned no results", source: nameof(VectorQueryProcessor));
                    return "Geen relevante informatie gevonden";
                }

                Dictionary<int, string> chunkDataMap = await ExtractChunkData(neighbors);
                StringBuilder formattedChunks = GetFormattedChunks(neighbors, chunkDataMap);

                return formattedChunks.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<Neighbor>> GetNearestNeighborsAsync(
            string connectionString,
            List<float> queryEmbedding,
            int topK,
            int projectId = 0)
        {
            return await GetNearestNeighbors(connectionString, queryEmbedding, topK, projectId);
        }

        private async Task<List<Neighbor>> GetNearestNeighbors(
            string connectionString,
            List<float> queryEmbedding,
            int topK,
            int projectId = 0)
        {
            var results = new List<Neighbor>();
            AppLogger.Log($"Vector search: {queryEmbedding.Count} dimensions, projectId={projectId}", source: nameof(VectorQueryProcessor));
            var vectorString = "[" + string.Join(",", queryEmbedding.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Log which database the app is actually connected to
            using (var dbCmd = new NpgsqlCommand("SELECT current_database(), current_schema()", connection))
            using (var dbReader = await dbCmd.ExecuteReaderAsync())
            {
                if (await dbReader.ReadAsync())
                    AppLogger.Log($"Connected to database='{dbReader.GetString(0)}' schema='{dbReader.GetString(1)}'", source: nameof(VectorQueryProcessor));
            }

            var sql = @"
                SELECT
                    c.id,
                    c.bronid,
                    c.tekst,
                    b.title as brontitle,
                    b.project as projectid,
                    c.tekstvector <=> @QueryVector::vector as distance
                FROM chunk c
                INNER JOIN bron b ON c.bronid = b.id
                WHERE c.status = 1";

            if (projectId > 0)
                sql += " AND b.project = @ProjectId";

            sql += " ORDER BY distance ASC LIMIT @TopK";

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TopK", topK);
            command.Parameters.AddWithValue("@QueryVector", vectorString);

            if (projectId > 0)
                command.Parameters.AddWithValue("@ProjectId", projectId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new Neighbor
                {
                    ChunkId = reader.GetInt32(0),
                    BronId = reader.GetInt32(1),
                    Tekst = reader.GetString(2),
                    BronTitle = reader.GetString(3),
                    ProjectId = reader.GetInt32(4),
                    Distance = reader.GetDouble(5)
                });
            }

            return results;
        }

        private StringBuilder GetFormattedChunks(
            List<Neighbor> neighbors,
            Dictionary<int, string> chunkDataMap)
        {
            StringBuilder formattedChunks = new StringBuilder();

            if (neighbors == null || !neighbors.Any())
            {
                return formattedChunks.AppendLine("No neighbors found.");
            }

            foreach (var neighbor in neighbors)
            {
                double similarityScore = 1.0 - neighbor.Distance;

                if (chunkDataMap.TryGetValue(neighbor.ChunkId, out var data))
                {
                    formattedChunks.AppendLine($"=== CHUNK ID: {neighbor.ChunkId} (Score: {similarityScore:F4}) ===");
                    formattedChunks.AppendLine($"BRON: {neighbor.BronTitle}");
                    formattedChunks.AppendLine($"CONTENT: {data}");
                    formattedChunks.AppendLine();
                }
            }

            return formattedChunks;
        }

        private async Task<Dictionary<int, string>> ExtractChunkData(List<Neighbor> neighbors)
        {
            var chunkDataMap = new Dictionary<int, string>();
            var chunkIds = neighbors.Select(n => n.ChunkId).ToArray();

            // Retrieve chunk data from local database using EF Core
            var chunks = await _context.Chunks
                .Where(c => chunkIds.Contains(c.ID))
                .Select(c => new { c.ID, c.Tekst })
                .ToListAsync();

            foreach (var chunk in chunks)
            {
                if (!string.IsNullOrEmpty(chunk.Tekst))
                {
                    chunkDataMap[chunk.ID] = chunk.Tekst;
                }
                else
                {
                    chunkDataMap[chunk.ID] = "Text not found for this chunk.";
                }
            }

            return chunkDataMap;
        }
    }

   
}