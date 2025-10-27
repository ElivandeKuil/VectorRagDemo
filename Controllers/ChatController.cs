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

                var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(request.Query);

                if (queryEmbedding == null || !queryEmbedding.Any())
                {
                    return Json(new { success = false, error = "Failed to generate query embedding." });
                }

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var formattedNeighbors = await VectorQueryProcessor.QueryLocalVectorDbAsync(
                    _context,
                    connectionString,
                    queryEmbedding,
                    topK: Config.VectorQueryTopK
                );

                var chatHistory = new List<ChatMessage>();
                if (request.History != null && request.History.Any())
                {
                    chatHistory.AddRange(request.History);
                }

                var response = await GeminiProcessor.GenerateContentAsync(
                    _context,
                    chatHistory,
                    request.Query,
                    formattedNeighbors,
                    1
                );

                return Json(new
                {
                    success = true,
                    answer = response.ResponseText,
                    sources = response.SourceText
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
