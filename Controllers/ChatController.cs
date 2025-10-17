using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Data;
using VectorRagDemo.BLL;
using VectorRagDemo.Models;

namespace VectorRagDemo.Controllers
{
    public class ChatController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;

        public ChatController(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Chat/Index
        public IActionResult Index()
        {
            return View();
        }

        // POST: Chat/Ask
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Json(new { success = false, error = "Query cannot be empty." });
                }

                // 1. Generate query embedding
                var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(request.Query);

                if (queryEmbedding == null || !queryEmbedding.Any())
                {
                    return Json(new { success = false, error = "Failed to generate query embedding." });
                }

                // 2. Search for relevant chunks
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var contextText = await VectorQueryProcessor.QueryLocalVectorDbAsync(
                    _context,
                    connectionString,
                    queryEmbedding,
                    topK: Config.VectorQueryTopK
                );

                // 3. Build system prompt with retrieved context
                var systemPrompt = @"You are a helpful AI assistant with access to a knowledge base.
Use the following context from the knowledge base to answer the user's question.
If the context doesn't contain relevant information, say so honestly.
Always cite which chunks you used by including their CHUNK IDs in your response.

Context from knowledge base:
" + contextText;

                // 4. Build chat history from request
                var chatHistory = new List<ChatMessage>();
                if (request.History != null && request.History.Any())
                {
                    chatHistory.AddRange(request.History);
                }

                // 5. Set up Gemini parameters
                var parameters = new GeminiParameters
                {
                    MaxOutputTokens = Config.MaxTokens,
                    Temperature = (float)Config.Temperature,
                    TopP = (float)Config.TopP,
                    TopK = Config.GeminiTopK
                };

                // 6. Get response from Gemini
                var response = await GeminiProcessor.GenerateContentAsync(
                    _context,
                    systemPrompt,
                    chatHistory,
                    request.Query,
                    parameters
                );

                return Json(new
                {
                    success = true,
                    answer = response.ResponseText,
                    sources = response.SourceText,
                    contextUsed = contextText
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = $"Error processing request: {ex.Message}"
                });
            }
        }
    }

    public class ChatRequest
    {
        public string Query { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
    }
}
