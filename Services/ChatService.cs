using VectorRagDemo.BLL;
using VectorRagDemo.Controllers;
using VectorRagDemo.DAL;
using VectorRagDemo.Models;

namespace VectorRagDemo.Services
{
    public class ChatService : ServiceBase
    {
        public ChatService(VectorDbContext context, IConfiguration configuration)
            : base(context, configuration)
        {
           
        }

        public async Task<object> Ask(ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return new { success = false, error = "Query cannot be empty." };
                }

                var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(request.Query);

                if (queryEmbedding == null || !queryEmbedding.Any())
                {
                    return new { success = false, error = "Failed to generate query embedding." };
                }

                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                var formattedNeighbors = await VectorQueryProcessor.QueryLocalVectorDbAsync(
                    connectionString,
                    queryEmbedding,
                    topK: Config.VectorQueryTopK
                );

                var chatHistory = new List<ChatMessage>();
                if (request.History != null && request.History.Any())
                {
                    chatHistory.AddRange(request.History);
                }

                var response = await GeminiProcessor.GenerateContent(
                    chatHistory,
                    request.Query,
                    formattedNeighbors
                );

                return new
                {
                    success = true,
                    answer = response.ResponseText,
                    sources = response.SourceText
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    error = $"Error processing request: {ex.Message}"
                };
            }
        }
    }
}
