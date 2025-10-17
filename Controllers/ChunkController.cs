using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VectorRagDemo.Data;
using VectorRagDemo.BLL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace VectorRagDemo.Controllers
{
    public class ChunkController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;

        public ChunkController(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var chunks = await _context.Chunks
                .Include(c => c.Bron)
                    .ThenInclude(b => b.ProjectNavigation)
                .Where(c => c.Status == 1)
                .OrderByDescending(c => c.GemaaktOp)
                .ToListAsync();

            return View(chunks);
        }

        // GET: Chunk/Create
        public async Task<IActionResult> Create()
        {
            // Load available Bronnen for dropdown
            var bronnen = await _context.Bronnen
                .Include(b => b.ProjectNavigation)
                .Where(b => b.Status == 1)
                .OrderBy(b => b.Title)
                .ToListAsync();

            ViewBag.Bronnen = new SelectList(
                bronnen.Select(b => new
                {
                    b.ID,
                    DisplayText = $"{b.Title} ({b.ProjectNavigation?.Naam ?? "No Project"})"
                }),
                "ID",
                "DisplayText"
            );

            return View();
        }

        // POST: Chunk/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bronId, string tekst)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tekst))
                {
                    TempData["Error"] = "Chunk text cannot be empty.";
                    return RedirectToAction(nameof(Create));
                }

                if (bronId <= 0)
                {
                    TempData["Error"] = "Please select a valid Bron.";
                    return RedirectToAction(nameof(Create));
                }

                // Generate embedding for the chunk text
                var embedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(tekst);

                if (embedding == null || !embedding.Any())
                {
                    TempData["Error"] = "Failed to generate embedding for the chunk.";
                    return RedirectToAction(nameof(Create));
                }

                // Insert chunk with vector using raw SQL
                var vectorString = "[" + string.Join(",", embedding) + "]";
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO Chunk (BronID, Tekst, TekstVector, GemaaktOp, Status)
                    VALUES (@BronID, @Tekst, CAST(@TekstVector AS VECTOR(768)), GETDATE(), @Status);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BronID", bronId);
                command.Parameters.AddWithValue("@Tekst", tekst);
                command.Parameters.AddWithValue("@TekstVector", vectorString);
                command.Parameters.AddWithValue("@Status", 1); // Active

                var newId = (int)await command.ExecuteScalarAsync();

                TempData["Success"] = $"Chunk created successfully with ID: {newId}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating chunk: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }
    }
}