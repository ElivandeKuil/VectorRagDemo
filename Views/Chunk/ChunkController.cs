using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.Controllers
{
    public class ChunkController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly LogboekDbContext _logboekContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly EmbeddingProcessor _embeddingProcessor;

        public ChunkController(
            VectorDbContext context,
            LogboekDbContext logboekContext,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _logboekContext = logboekContext;
            _configuration = configuration;
            _env = env;
            _embeddingProcessor = new EmbeddingProcessor(new HttpClient(), logboekContext);
        }

        public async Task<IActionResult> Index()
        {
            var bronnen = await _context.Bronnen
                .Include(b => b.ProjectNavigation)
                .Where(b => b.FileName != null && b.Status == 1)
                .OrderByDescending(b => b.GemaaktOp)
                .ToListAsync();

            var bronIds = bronnen.Select(b => b.ID).ToList();

            var chunkCounts = await _context.Chunks
                .Where(c => bronIds.Contains(c.BronID) && c.Status == 1)
                .GroupBy(c => c.BronID)
                .Select(g => new { BronId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BronId, x => x.Count);

            var viewModels = bronnen.Select(b => new DocumentViewModel
            {
                BronId = b.ID,
                Title = b.Title,
                FileName = b.FileName,
                Extension = b.FileName != null ? Path.GetExtension(b.FileName).ToLowerInvariant() : null,
                ProjectNaam = b.ProjectNavigation?.Naam ?? string.Empty,
                ProjectId = b.Project,
                ChunkCount = chunkCounts.TryGetValue(b.ID, out var count) ? count : 0,
                GemaaktOp = b.GemaaktOp
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var projects = await _context.Projects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();

            ViewBag.Projects = new SelectList(projects, "ID", "Naam");
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile? file, int projectId)
        {
            var projects = await _context.Projects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();
            ViewBag.Projects = new SelectList(projects, "ID", "Naam");

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Selecteer een bestand om te uploaden.");
                return View();
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".txt" && ext != ".docx")
            {
                ModelState.AddModelError(string.Empty, "Alleen .txt en .docx bestanden zijn toegestaan.");
                return View();
            }

            if (projectId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Selecteer een project.");
                return View();
            }

            Bron? bron = null;
            string? diskPath = null;

            try
            {
                // Read file bytes
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                // Parse document to text
                var text = DocumentParser.ParseToText(bytes, ext);

                if (string.IsNullOrWhiteSpace(text))
                {
                    ModelState.AddModelError(string.Empty, "Het bestand bevat geen leesbare tekst.");
                    return View();
                }

                // Split into chunks
                var chunkTexts = TextChunker.ChunkText(text);

                if (chunkTexts.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Het bestand kon niet worden opgesplitst in chunks.");
                    return View();
                }

                // Build relative file path
                var relPath = $"uploads/{Guid.NewGuid()}{ext}";
                diskPath = Path.Combine(_env.WebRootPath, relPath.Replace('/', Path.DirectorySeparatorChar));

                // Create and save Bron
                bron = new Bron
                {
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    Project = projectId,
                    FileName = file.FileName,
                    FilePath = relPath,
                    GemaaktOp = DateTime.Now,
                    Status = 1
                };

                _context.Bronnen.Add(bron);
                await _context.SaveChangesAsync();

                // Write file to disk
                Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
                await System.IO.File.WriteAllBytesAsync(diskPath, bytes);

                // Generate embeddings for all chunks
                var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(chunkTexts);

                if (embeddings.Count != chunkTexts.Count)
                    throw new Exception($"Embedding count mismatch: got {embeddings.Count}, expected {chunkTexts.Count}.");

                // Insert chunks via raw SQL (pgvector pattern)
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status)
                    VALUES (@BronID, @Tekst, @TekstVector::vector, NOW(), @Status)
                    RETURNING id;";

                for (int i = 0; i < chunkTexts.Count; i++)
                {
                    var vectorString = "[" + string.Join(",", embeddings[i]) + "]";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@BronID", bron.ID);
                    cmd.Parameters.AddWithValue("@Tekst", chunkTexts[i]);
                    cmd.Parameters.AddWithValue("@TekstVector", vectorString);
                    cmd.Parameters.AddWithValue("@Status", 1);
                    await cmd.ExecuteScalarAsync();
                }

                TempData["Success"] = $"'{file.FileName}' succesvol geüpload — {chunkTexts.Count} chunks aangemaakt.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Soft-delete bron if it was created
                if (bron?.ID > 0)
                {
                    bron.Status = 2;
                    await _context.SaveChangesAsync();
                }

                // Remove file from disk if it was written
                if (diskPath != null && System.IO.File.Exists(diskPath))
                    System.IO.File.Delete(diskPath);

                ModelState.AddModelError(string.Empty, $"Fout bij verwerking: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null || bron.FilePath == null || bron.FileName == null || bron.Status != 1)
                return NotFound();

            var diskPath = Path.Combine(_env.WebRootPath, bron.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(diskPath))
                return NotFound();

            var ext = Path.GetExtension(bron.FileName).ToLowerInvariant();
            var contentType = ext == ".docx"
                ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                : "text/plain";

            var bytes = await System.IO.File.ReadAllBytesAsync(diskPath);
            return File(bytes, contentType, bron.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null)
                return NotFound();

            // Soft-delete all chunks for this bron
            await _context.Chunks
                .Where(c => c.BronID == id && c.Status == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, 2));

            // Remember file path before soft-deleting bron
            var filePath = bron.FilePath;

            bron.Status = 2;
            await _context.SaveChangesAsync();

            // Delete file from disk
            if (!string.IsNullOrEmpty(filePath))
            {
                var diskPath = Path.Combine(_env.WebRootPath, filePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(diskPath))
                    System.IO.File.Delete(diskPath);
            }

            TempData["Success"] = $"Document '{bron.Title}' en alle bijbehorende chunks zijn verwijderd.";
            return RedirectToAction(nameof(Index));
        }
    }
}
