using Microsoft.Data.SqlClient;
using System.Text;
using VectorRagDemo.Data;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.Models;

namespace VectorRagDemo.BLL
{
    public static class VectorQueryProcessor
    {
        public static async Task<string> QueryLocalVectorDbAsync(
            VectorDbContext context,
            string connectionString,
            List<float> queryEmbedding,
            int topK,
            SearchFilters filters)
        {
            try
            {
                var neighbors = await GetNearestNeighbors(connectionString, queryEmbedding, topK, filters);

                if (neighbors == null || !neighbors.Any())
                {
                    return "Geen relevante informatie gevonden";
                }

                Dictionary<int, string> chunkDataMap = await ExtractChunkData(context, neighbors);
                StringBuilder formattedChunks = GetFormattedChunks(neighbors, chunkDataMap);

                return formattedChunks.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static async Task<List<Models.Neighbor>> GetNearestNeighbors(
            string connectionString,
            List<float> queryEmbedding,
            int topK,
            SearchFilters filters)
        {
            var results = new List<Models.Neighbor>();
            var vectorString = "[" + string.Join(",", queryEmbedding) + "]";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Build SQL query with optional filters
            var sql = @"
                SELECT TOP (@TopK)
                    c.ID,
                    c.BronID,
                    c.Tekst,
                    b.Title as BronTitle,
                    b.Project as ProjectID,
                    VECTOR_DISTANCE('cosine', c.TekstVector, CAST(@QueryVector AS VECTOR(768))) as Distance
                FROM Chunk c
                INNER JOIN Bron b ON c.BronID = b.ID
                WHERE c.Status = 1";

            // Add filters if provided
            if (filters?.HasFilters == true)
            {
                // Add your custom filter logic here if needed
                // For example, filter by project:
                // sql += " AND b.Project IN @ProjectIds";
            }

            sql += " ORDER BY Distance ASC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TopK", topK);
            command.Parameters.AddWithValue("@QueryVector", vectorString);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new Models.Neighbor
                {
                    ChunkId = reader.GetInt32(0),
                    BronId = reader.GetInt32(1),
                    Tekst = reader.GetString(2),
                    BronTitle = reader.GetString(3),
                    ProjectId = reader.GetInt32(4),
                    Distance = reader.GetFloat(5)
                });
            }

            return results;
        }

        private static StringBuilder GetFormattedChunks(
            List<Models.Neighbor> neighbors,
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

        private static async Task<Dictionary<int, string>> ExtractChunkData(
            VectorDbContext context,
            List<Models.Neighbor> neighbors)
        {
            var chunkDataMap = new Dictionary<int, string>();
            var chunkIds = neighbors.Select(n => n.ChunkId).ToArray();

            // Retrieve chunk data from local database using EF Core
            var chunks = await context.Chunks
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