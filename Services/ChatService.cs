using System.Text;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.Requests;
using VectorRagDemo.Models.Responses;

namespace VectorRagDemo.Services
{
    public class ChatService : ServiceBase
    {
        private const int MaxRetrievedChunks = 20;
        private const double FreshnessWeight = 0.3;
        private const double SimilarityWeight = 0.7;

        public ChatService(VectorDbContext context, LogboekDbContext logboekContext, IConfiguration configuration)
            : base(context, logboekContext, configuration)
        {

        }

        public async Task<ChatResponse> Ask(ChatRequest request, int projectId = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    throw new ArgumentException("Query cannot be empty.");
                }

                var preProcessedQuery = await PreProcessingProcessor.GetPreProcessedQuery(request.Query, request.History);
                AppLogger.Log($"Pre-processed query: {preProcessedQuery}", source: nameof(ChatService));

                List<float> queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(preProcessedQuery);

                if (queryEmbedding == null || !queryEmbedding.Any())
                {
                    throw new InvalidOperationException("Failed to generate query embedding.");
                }

                AppLogger.Log($"Embedding ready: {queryEmbedding.Count} dimensions — passing to vector search", source: nameof(ChatService));

                var connectionString = Configuration.GetConnectionString("DefaultConnection");

                // Get new chunks from vector search, filtered to user's project when set
                var newNeighbors = await VectorQueryProcessor.GetNearestNeighborsAsync(
                    connectionString,
                    queryEmbedding,
                    topK: Config.VectorQueryTopK,
                    projectId: projectId
                );

                // Process and merge with existing retrieved chunks
                var retrievedChunks = ProcessRetrievedChunks(request.RetrievedChunks, newNeighbors);

                // Build chat history
                var chatHistory = new List<ChatMessage>();
                if (request.History != null && request.History.Any())
                {
                    chatHistory.AddRange(request.History);
                }

                // Generate response using Gemini
                var formattedNeighbors = FormatChunksForGemini(retrievedChunks);
                var geminiResponse = await GeminiProcessor.GenerateContent(
                    chatHistory,
                    preProcessedQuery,
                    formattedNeighbors
                );

                return new ChatResponse
                {
                    GenerativeResponse = geminiResponse,
                    Chunks = retrievedChunks
                };
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex.Message, source: nameof(ChatService), detail: ex.ToString());
                // Return error response
                return new ChatResponse
                {
                    GenerativeResponse = new GenerativeModelResponse
                    {
                        ResponseText = $"Error: {ex.Message}",
                        SourceText = "No source text used."
                    },
                    Chunks = new List<RetrievedChunk>()
                };
            }
        }

        private List<RetrievedChunk> ProcessRetrievedChunks(
            List<RetrievedChunk>? existingChunks,
            List<Neighbor> newNeighbors)
        {
            var result = new List<RetrievedChunk>();

            // Add existing chunks and update freshness
            if (existingChunks != null && existingChunks.Any())
            {
                foreach (var existingChunk in existingChunks)
                {
                    existingChunk.Freshness++; // Increment freshness (higher = older)
                    result.Add(existingChunk);
                }
            }

            // Add new chunks from vector search
            foreach (var neighbor in newNeighbors)
            {
                // Convert distance to similarity score
                var similarityScore = 1.0 - neighbor.Distance;

                // Check if chunk already exists
                var existingChunk = result.FirstOrDefault(c => c.Chunk.ID == neighbor.ChunkId);

                if (existingChunk != null)
                {
                    // Reset freshness if chunk appears again
                    existingChunk.Freshness = 0;
                    // Update similarity score if it's better
                    if (similarityScore > existingChunk.InitialSimilirityScore)
                    {
                        existingChunk.InitialSimilirityScore = similarityScore;
                    }
                }
                else
                {
                    // Add new chunk
                    var chunkEntity = Context.Chunks.FirstOrDefault(c => c.ID == neighbor.ChunkId);
                    if (chunkEntity != null)
                    {
                        result.Add(new RetrievedChunk
                        {
                            Chunk = chunkEntity,
                            Freshness = 0,
                            InitialSimilirityScore = similarityScore
                        });
                    }
                }
            }

            // Trim chunks based on relevance score
            return TrimChunksByRelevance(result);
        }

        private List<RetrievedChunk> TrimChunksByRelevance(List<RetrievedChunk> chunks)
        {
            if (chunks.Count <= MaxRetrievedChunks)
            {
                return chunks;
            }

            // Calculate relevance score for each chunk
            var chunksWithScores = chunks.Select(c => new
            {
                RetrievedChunk = c,
                RelevanceScore = CalculateRelevanceScore(c)
            }).ToList();

            // Sort by relevance (highest first) and take top N
            return chunksWithScores
                .OrderByDescending(x => x.RelevanceScore)
                .Take(MaxRetrievedChunks)
                .Select(x => x.RetrievedChunk)
                .ToList();
        }

        private double CalculateRelevanceScore(RetrievedChunk retrievedChunk)
        {
            // Normalize freshness (invert so 0 is best, scale to 0-1)
            // Assuming max freshness of 10 for normalization
            var normalizedFreshness = 1.0 - Math.Min(retrievedChunk.Freshness / 10.0, 1.0);

            // Similarity score is already 0-1
            var normalizedSimilarity = retrievedChunk.InitialSimilirityScore;

            // Calculate weighted relevance score
            return (normalizedFreshness * FreshnessWeight) + (normalizedSimilarity * SimilarityWeight);
        }

        private string FormatChunksForGemini(List<RetrievedChunk> chunks)
        {
            if (!chunks.Any())
            {
                return "Geen relevante informatie gevonden";
            }

            var formattedChunks = new StringBuilder();

            foreach (var retrievedChunk in chunks)
            {
                var chunkEntity = retrievedChunk.Chunk;
                var bronTitle = chunkEntity.Bron?.Title ?? "Unknown";
                formattedChunks.AppendLine($"=== CHUNK ID: {chunkEntity.ID} (Score: {retrievedChunk.InitialSimilirityScore:F4}) ===");
                formattedChunks.AppendLine($"BRON: {bronTitle}");
                formattedChunks.AppendLine($"CONTENT: {chunkEntity.Tekst}");
                formattedChunks.AppendLine();
            }

            return formattedChunks.ToString();
        }
    }
}
