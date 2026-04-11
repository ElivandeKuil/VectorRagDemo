using System.Runtime.CompilerServices;
using System.Text;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.Requests;
using VectorRagDemo.Models.Responses;
using VectorRagDemo.Models.Entities;

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

        public async Task<ChatResponse> Ask(ChatRequest request, int projectId = 0, bool extraCommunicationEnabled = false)
        {
            try
            {
                var prep = await PrepareAsync(request, projectId);
                var geminiResponse = await GeminiProcessor.GenerateContent(
                    prep.ChatHistory,
                    prep.PreProcessedQuery,
                    prep.FormattedNeighbors,
                    extraCommunicationEnabled,
                    projectId,
                    prep.CorrelationId
                );
                return new ChatResponse { GenerativeResponse = geminiResponse, Chunks = prep.RetrievedChunks };
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex.Message, source: nameof(ChatService), detail: ex.ToString());
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

        /// <summary>
        /// Runs the pre-processing, embedding, vector search, and placeholder resolution steps.
        /// Returns all intermediate state needed for the final LLM step (streaming or non-streaming).
        /// Throws on error — callers must handle exceptions.
        /// </summary>
        public async Task<ChatPreparation> PrepareAsync(ChatRequest request, int projectId)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                throw new ArgumentException("Query cannot be empty.");

            var correlationId = Guid.NewGuid();

            var preProcessedQuery = await PreProcessingProcessor.GetPreProcessedQuery(request.Query, request.History, correlationId);
            AppLogger.Log($"Pre-processed query: {preProcessedQuery}", source: nameof(ChatService));

            var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(preProcessedQuery, correlationId);
            if (queryEmbedding == null || !queryEmbedding.Any())
                throw new InvalidOperationException("Failed to generate query embedding.");

            AppLogger.Log($"Embedding ready: {queryEmbedding.Count} dimensions", source: nameof(ChatService));

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var newNeighbors = await VectorQueryProcessor.GetNearestNeighborsAsync(
                connectionString, queryEmbedding, topK: Config.VectorQueryTopK, projectId: projectId);

            var retrievedChunks = ProcessRetrievedChunks(request.RetrievedChunks, newNeighbors);

            var chatHistory = new List<ChatMessage>();
            if (request.History != null && request.History.Any())
                chatHistory.AddRange(request.History);

            Dictionary<string, string> placeholders = new();
            if (projectId > 0)
            {
                var proc = new PlaceholderProcessor(Context);
                placeholders = await proc.GetPlaceholdersAsync(projectId);
            }

            var formattedNeighbors = FormatChunksForGemini(retrievedChunks, placeholders);
            var chatHistoryString = FormatChatHistoryString(chatHistory);

            return new ChatPreparation
            {
                PreProcessedQuery = preProcessedQuery,
                RetrievedChunks = retrievedChunks,
                FormattedNeighbors = formattedNeighbors,
                ChatHistory = chatHistory,
                ChatHistoryString = chatHistoryString,
                CorrelationId = correlationId
            };
        }

        /// <summary>
        /// Runs the summarization step on a prepared pipeline state.
        /// When <paramref name="extraCommunicationEnabled"/> is true the escalation signal
        /// (<c>transferToWhatsApp</c>) is detected here so it is available before streaming starts.
        /// </summary>
        public async Task<(string RelevantOutput, List<int> UsedChunkIds, bool TransferToWhatsApp)> SummarizeAsync(
            ChatPreparation prep, int projectId, bool extraCommunicationEnabled = false)
        {
            return await GeminiProcessor.SummarizeAsync(
                prep.FormattedNeighbors,
                prep.ChatHistoryString,
                prep.PreProcessedQuery,
                projectId,
                extraCommunicationEnabled,
                prep.CorrelationId);
        }

        /// <summary>
        /// Streams the final LLM response token-by-token using the prepared state and summarised context.
        /// </summary>
        public async IAsyncEnumerable<string> StreamFinalResponseAsync(
            ChatPreparation prep,
            string summarisedContent,
            int projectId,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var token in GeminiProcessor.StreamFinalResponseAsync(
                summarisedContent,
                prep.PreProcessedQuery,
                prep.ChatHistoryString,
                projectId,
                prep.CorrelationId,
                ct))
            {
                yield return token;
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
                    // Restore BronLink if it was lost during serialization between turns
                    if (existingChunk.BronLink == null && neighbor.BronLink != null)
                    {
                        existingChunk.BronLink = neighbor.BronLink;
                        existingChunk.BronTitle ??= neighbor.BronTitle;
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
                            InitialSimilirityScore = similarityScore,
                            BronTitle = neighbor.BronTitle,
                            BronLink = neighbor.BronLink
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

        private static string FormatChatHistoryString(List<ChatMessage> chatHistory)
            => string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));

        private string FormatChunksForGemini(List<RetrievedChunk> chunks, Dictionary<string, string>? placeholders = null)
        {
            if (!chunks.Any())
            {
                return "Geen relevante informatie gevonden";
            }

            var formattedChunks = new StringBuilder();

            foreach (var retrievedChunk in chunks)
            {
                var chunkEntity = retrievedChunk.Chunk;
                var bronTitle = retrievedChunk.BronTitle ?? chunkEntity.Bron?.Title ?? "Unknown";
                var tekst = placeholders != null && placeholders.Count > 0
                    ? PlaceholderProcessor.Resolve(chunkEntity.Tekst, placeholders)
                    : chunkEntity.Tekst;

                formattedChunks.AppendLine($"=== CHUNK ID: {chunkEntity.ID} (Score: {retrievedChunk.InitialSimilirityScore:F4}) ===");
                formattedChunks.AppendLine($"BRON: {bronTitle}");
                if (!string.IsNullOrWhiteSpace(retrievedChunk.BronLink))
                    formattedChunks.AppendLine($"LINK: {retrievedChunk.BronLink}");
                formattedChunks.AppendLine($"CONTENT: {tekst}");
                formattedChunks.AppendLine();
            }

            return formattedChunks.ToString();
        }
    }

    /// <summary>
    /// Intermediate result of the RAG preparation steps (pre-processing through placeholder resolution).
    /// Passed to the final LLM step, which may be streaming or non-streaming.
    /// </summary>
    public class ChatPreparation
    {
        public string PreProcessedQuery { get; set; } = string.Empty;
        public List<RetrievedChunk> RetrievedChunks { get; set; } = new();
        public string FormattedNeighbors { get; set; } = string.Empty;
        public List<ChatMessage> ChatHistory { get; set; } = new();
        public string ChatHistoryString { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
    }
}
