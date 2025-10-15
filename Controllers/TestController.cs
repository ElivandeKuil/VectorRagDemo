using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Data;
using VectorRagDemo.BLL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace VectorRagDemo.Controllers
{
    public class TestController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;

        public TestController(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /Test/Index
        public IActionResult Index()
        {
            return View();
        }

        // Test 1: Generate embeddings for all chunks
        public async Task<IActionResult> GenerateEmbeddings()
        {
            try
            {
                var chunks = await _context.Chunks
                    .Where(c => c.Status == 1)
                    .ToListAsync();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                int updated = 0;

                foreach (var chunk in chunks)
                {
                    // Generate embedding for this chunk's text
                    var embedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(chunk.Tekst);

                    if (embedding != null && embedding.Any())
                    {
                        // Update the vector in SQL directly
                        var vectorString = "[" + string.Join(",", embedding) + "]";

                        using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync();

                        var sql = @"UPDATE Chunk 
                                   SET TekstVector = CAST(@Vector AS VECTOR(768)),
                                       GewijzigdOp = GETDATE()
                                   WHERE ID = @ChunkId";

                        using var command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@Vector", vectorString);
                        command.Parameters.AddWithValue("@ChunkId", chunk.ID);

                        await command.ExecuteNonQueryAsync();
                        updated++;
                    }
                }

                return Json(new { success = true, message = $"Updated {updated} chunks with embeddings" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Test 2: Search for similar chunks
        public async Task<IActionResult> TestVectorSearch(string query = "sample product")
        {
            try
            {
                // Generate embedding for the query
                var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(query);

                if (queryEmbedding == null || !queryEmbedding.Any())
                {
                    return Json(new { success = false, error = "Failed to generate query embedding" });
                }

                // Search for similar chunks
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var results = await VectorQueryProcessor.QueryLocalVectorDbAsync(
                    _context,
                    connectionString,
                    queryEmbedding,
                    topK: 3,
                    filters: null
                );

                return Json(new
                {
                    success = true,
                    query = query,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Test 3: Full RAG test
        public async Task<IActionResult> TestFullRAG(string query = "Tell me about the products")
        {
            try
            {
                // 1. Generate query embedding
                var queryEmbedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(query);

                // 2. Search for relevant chunks
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var context = await VectorQueryProcessor.QueryLocalVectorDbAsync(
                    _context,
                    connectionString,
                    queryEmbedding,
                    topK: 3,
                    filters: null
                );

                // 3. Send to Gemini with context
                var systemPrompt = "You are a helpful assistant. Use the following context to answer the user's question.\n\nContext:\n" + context;

                var chatHistory = new List<ChatMessage>();
                var parameters = new GeminiParameters();

                var response = await GeminiProcessor.GenerateContentAsync(
                    _context,
                    systemPrompt,
                    chatHistory,
                    query,
                    parameters
                );

                return Json(new
                {
                    success = true,
                    query = query,
                    context = context,
                    response = response.ResponseText,
                    sources = response.SourceText
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}