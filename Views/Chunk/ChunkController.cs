using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VectorRagDemo.BLL;
using VectorRagDemo.BLL.Processors;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    public class ChunkController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly LogboekDbContext _logboekContext;
        private readonly IConfiguration _configuration;
        private readonly EmbeddingProcessor _embeddingProcessor;

        private readonly OmgevingService _omgevingService;

        public ChunkController(
            VectorDbContext context,
            LogboekDbContext logboekContext,
            IConfiguration configuration,
            OmgevingService omgevingService)
        {
            _context = context;
            _logboekContext = logboekContext;
            _configuration = configuration;
            _embeddingProcessor = new EmbeddingProcessor(new HttpClient(), logboekContext);
            _omgevingService = omgevingService;
        }

        public async Task<IActionResult> Index(int? projectId = null, int? folderId = null)
        {
            var accessibleProjects = await GetAccessibleProjectsAsync();

            if (!accessibleProjects.Any())
                return View(new List<DocumentViewModel>());

            int resolvedProjectId;

            if (User.IsInRole("Admin"))
            {
                var cookieId = _omgevingService.ResolveForAdmin(projectId ?? 0);
                resolvedProjectId = accessibleProjects.Any(p => p.ID == cookieId)
                    ? cookieId
                    : accessibleProjects.First().ID;
                ViewBag.Projects = new SelectList(accessibleProjects, "ID", "Naam", resolvedProjectId);
            }
            else
            {
                var userId = User.GetUserId();
                var cookieId = await _omgevingService.ResolveForUserAsync(userId);
                resolvedProjectId = accessibleProjects.Any(p => p.ID == cookieId)
                    ? cookieId
                    : accessibleProjects.First().ID;
            }

            ViewBag.SelectedProjectId = resolvedProjectId;

            // Load all folders for this project (used for tree + breadcrumb + subtree filtering)
            var folders = await _context.Folders
                .Where(f => f.Project == resolvedProjectId && f.Status == 1)
                .OrderBy(f => f.Naam)
                .ToListAsync();

            // Doc counts per folder (direct only)
            var folderDocCounts = await _context.Bronnen
                .Where(b => b.Project == resolvedProjectId && b.Status == 1
                         && b.FileName != null && b.FolderId != null)
                .GroupBy(b => b.FolderId)
                .Select(g => new { FolderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FolderId!.Value, x => x.Count);

            ViewBag.FolderTreeData = folders.Select(f => new
            {
                id = f.ID,
                naam = f.Naam,
                parentId = f.ParentId,
                docCount = folderDocCounts.GetValueOrDefault(f.ID, 0)
            }).ToList();

            ViewBag.CurrentFolderId = folderId;

            ViewBag.TotalDocCount = await _context.Bronnen
                .CountAsync(b => b.FileName != null && b.Status == 1 && b.Project == resolvedProjectId);

            // Build breadcrumb
            if (folderId.HasValue)
            {
                var crumbs = new List<FolderCrumb>();
                var cur = folders.FirstOrDefault(f => f.ID == folderId.Value);
                while (cur != null)
                {
                    crumbs.Insert(0, new FolderCrumb(cur.ID, cur.Naam));
                    cur = cur.ParentId.HasValue
                        ? folders.FirstOrDefault(f => f.ID == cur.ParentId.Value)
                        : null;
                }
                ViewBag.Breadcrumb = crumbs;
            }

            // Filter documents
            IQueryable<Bron> query = _context.Bronnen
                .Include(b => b.ProjectNavigation)
                .Include(b => b.FolderNavigation)
                .Where(b => b.FileName != null && b.Status == 1 && b.Project == resolvedProjectId);

            if (folderId.HasValue)
            {
                var subtreeIds = GetSubtreeFolderIds(folders, folderId.Value);
                query = query.Where(b => b.FolderId.HasValue && subtreeIds.Contains(b.FolderId.Value));
            }

            var bronnen = await query
                .OrderByDescending(b => b.GemaaktOp)
                .ToListAsync();

            var viewModels = bronnen.Select(b => new DocumentViewModel
            {
                BronId = b.ID,
                Title = b.Title,
                FileName = b.FileName,
                Extension = b.FileName != null ? Path.GetExtension(b.FileName).ToLowerInvariant() : null,
                ProjectNaam = b.ProjectNavigation?.Naam ?? string.Empty,
                ProjectId = b.Project,
                GemaaktOp = b.GemaaktOp,
                FolderId = b.FolderId,
                FolderNaam = b.FolderNavigation?.Naam,
                Link = b.Link
            }).ToList();

            return View(viewModels);
        }

        // ── Folder CRUD (AJAX) ────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> FolderCreate(string naam, int projectId, int? parentId)
        {
            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == projectId))
                return Json(new { success = false, message = "Geen toegang tot dit project." });

            if (string.IsNullOrWhiteSpace(naam))
                return Json(new { success = false, message = "Naam is verplicht." });

            if (parentId.HasValue)
            {
                var parent = await _context.Folders.FindAsync(parentId.Value);
                if (parent == null || parent.Status != 1 || parent.Project != projectId)
                    return Json(new { success = false, message = "Ongeldige bovenliggende map." });
            }

            var folder = new Folder
            {
                Naam = naam.Trim(),
                Project = projectId,
                ParentId = parentId,
                GemaaktOp = DateTime.Now,
                Status = 1
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = folder.ID, naam = folder.Naam });
        }

        [HttpPost]
        public async Task<IActionResult> FolderRename(int id, string naam)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null || folder.Status != 1)
                return Json(new { success = false, message = "Map niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == folder.Project))
                return Json(new { success = false, message = "Geen toegang." });

            if (string.IsNullOrWhiteSpace(naam))
                return Json(new { success = false, message = "Naam is verplicht." });

            folder.Naam = naam.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> FolderDelete(int id)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null || folder.Status != 1)
                return Json(new { success = false, message = "Map niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == folder.Project))
                return Json(new { success = false, message = "Geen toegang." });

            var allFolders = await _context.Folders
                .Where(f => f.Project == folder.Project && f.Status == 1)
                .ToListAsync();

            var subtreeIds = GetSubtreeFolderIds(allFolders, id);

            var hasDocuments = await _context.Bronnen
                .AnyAsync(b => b.FolderId.HasValue && subtreeIds.Contains(b.FolderId.Value) && b.Status == 1);

            if (hasDocuments)
                return Json(new { success = false, message = "Verwijder of verplaats eerst alle documenten in deze map." });

            await _context.Folders
                .Where(f => subtreeIds.Contains(f.ID))
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.Status, 2));

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MoveDocument(int bronId, int? folderId)
        {
            var bron = await _context.Bronnen.FindAsync(bronId);
            if (bron == null || bron.Status != 1)
                return Json(new { success = false, message = "Document niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Json(new { success = false, message = "Geen toegang." });

            if (folderId.HasValue)
            {
                var folder = await _context.Folders.FindAsync(folderId.Value);
                if (folder == null || folder.Status != 1 || folder.Project != bron.Project)
                    return Json(new { success = false, message = "Ongeldige map." });
            }

            bron.FolderId = folderId;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ── Document link ──────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> SetDocumentLink(int id, string? link)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null || bron.Status != 1)
                return Json(new { success = false, message = "Document niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Json(new { success = false, message = "Geen toegang." });

            bron.Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ── Document rename ────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> RenameDocument(int id, string title)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null || bron.Status != 1)
                return Json(new { success = false, message = "Document niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Json(new { success = false, message = "Geen toegang." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Titel is verplicht." });

            bron.Title = title.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ── Folder data (AJAX) ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetFolders(int projectId)
        {
            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == projectId))
                return Json(Array.Empty<object>());

            var folders = await _context.Folders
                .Where(f => f.Project == projectId && f.Status == 1)
                .OrderBy(f => f.Naam)
                .ToListAsync();

            return Json(folders.Select(f => new { id = f.ID, naam = f.Naam, parentId = f.ParentId }));
        }

        private async Task<object> GetFolderDataAsync(int projectId)
        {
            if (projectId <= 0) return Array.Empty<object>();

            var folders = await _context.Folders
                .Where(f => f.Project == projectId && f.Status == 1)
                .OrderBy(f => f.Naam)
                .ToListAsync();

            return folders.Select(f => new { id = f.ID, naam = f.Naam, parentId = f.ParentId }).ToList();
        }

        // ── File upload ────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var accessibleProjects = await GetAccessibleProjectsAsync();
            ViewBag.Projects = new SelectList(accessibleProjects, "ID", "Naam");
            var singleProject = !User.IsInRole("Admin") && accessibleProjects.Count == 1
                ? accessibleProjects.First()
                : null;
            ViewBag.SingleProject = singleProject;
            var initialProjectId = singleProject?.ID ?? accessibleProjects.FirstOrDefault()?.ID ?? 0;
            ViewBag.FolderData = await GetFolderDataAsync(initialProjectId);
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile? file, int projectId, string? link = null, int? folderId = null)
        {
            var accessibleProjects = await GetAccessibleProjectsAsync();
            ViewBag.Projects = new SelectList(accessibleProjects, "ID", "Naam");
            var singleProject = !User.IsInRole("Admin") && accessibleProjects.Count == 1
                ? accessibleProjects.First()
                : null;
            ViewBag.SingleProject = singleProject;
            ViewBag.FolderData = await GetFolderDataAsync(projectId > 0 ? projectId : singleProject?.ID ?? accessibleProjects.FirstOrDefault()?.ID ?? 0);

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

            if (!accessibleProjects.Any(p => p.ID == projectId))
            {
                ModelState.AddModelError(string.Empty, "Selecteer een geldig project.");
                return View();
            }

            Bron? bron = null;

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var text = DocumentParser.ParseToText(bytes, ext);

                if (string.IsNullOrWhiteSpace(text))
                {
                    ModelState.AddModelError(string.Empty, "Het bestand bevat geen leesbare tekst.");
                    return View();
                }

                var chunkTexts = TextChunker.ChunkText(text);

                if (chunkTexts.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Het bestand kon niet worden opgesplitst in chunks.");
                    return View();
                }

                bron = new Bron
                {
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    Project = projectId,
                    FileName = file.FileName,
                    FilePath = null,
                    FolderId = folderId,
                    Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
                    GemaaktOp = DateTime.Now,
                    Status = 1
                };

                _context.Bronnen.Add(bron);
                await _context.SaveChangesAsync();

                var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(chunkTexts);

                if (embeddings.Count != chunkTexts.Count)
                    throw new Exception($"Embedding count mismatch: got {embeddings.Count}, expected {chunkTexts.Count}.");

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status)
                    VALUES (@BronID, @Tekst, @TekstVector::vector, NOW(), @Status)
                    RETURNING id;";

                for (int i = 0; i < chunkTexts.Count; i++)
                {
                    var vectorString = "[" + string.Join(",", embeddings[i].Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@BronID", bron.ID);
                    cmd.Parameters.AddWithValue("@Tekst", chunkTexts[i]);
                    cmd.Parameters.AddWithValue("@TekstVector", vectorString);
                    cmd.Parameters.AddWithValue("@Status", 1);
                    await cmd.ExecuteScalarAsync();
                }

                TempData["Success"] = $"'{file.FileName}' succesvol geüpload.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (bron?.ID > 0)
                {
                    bron.Status = 2;
                    await _context.SaveChangesAsync();
                }

                ModelState.AddModelError(string.Empty, $"Fout bij verwerking: {ex.Message}");
                return View();
            }
        }

        // ── Text input ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> TextInput()
        {
            var accessibleProjects = await GetAccessibleProjectsAsync();
            ViewBag.Projects = new SelectList(accessibleProjects, "ID", "Naam");
            var singleProject = !User.IsInRole("Admin") && accessibleProjects.Count == 1
                ? accessibleProjects.First()
                : null;
            ViewBag.SingleProject = singleProject;
            var initialProjectId = singleProject?.ID ?? accessibleProjects.FirstOrDefault()?.ID ?? 0;
            ViewBag.FolderData = await GetFolderDataAsync(initialProjectId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TextInput(string title, string text, int projectId, string? link = null, int? folderId = null)
        {
            var accessibleProjects = await GetAccessibleProjectsAsync();
            ViewBag.Projects = new SelectList(accessibleProjects, "ID", "Naam");
            var singleProject = !User.IsInRole("Admin") && accessibleProjects.Count == 1
                ? accessibleProjects.First()
                : null;
            ViewBag.SingleProject = singleProject;
            ViewBag.FolderData = await GetFolderDataAsync(projectId > 0 ? projectId : singleProject?.ID ?? accessibleProjects.FirstOrDefault()?.ID ?? 0);

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(string.Empty, "Geef een titel op.");

            if (string.IsNullOrWhiteSpace(text))
                ModelState.AddModelError(string.Empty, "Voer tekst in.");

            if (!accessibleProjects.Any(p => p.ID == projectId))
                ModelState.AddModelError(string.Empty, "Selecteer een geldig project.");

            if (!ModelState.IsValid)
            {
                ViewBag.InputTitle = title;
                ViewBag.InputText = text;
                return View();
            }

            Bron? bron = null;

            try
            {
                var chunkTexts = TextChunker.ChunkText(text);

                if (chunkTexts.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "De tekst bevat te weinig inhoud om op te splitsen.");
                    ViewBag.InputTitle = title;
                    ViewBag.InputText = text;
                    return View();
                }

                var safeTitle = string.Concat(title.Trim().Split(Path.GetInvalidFileNameChars()));
                var fileName = (safeTitle.Length > 0 ? safeTitle : "tekst") + ".txt";

                bron = new Bron
                {
                    Title = title.Trim(),
                    Project = projectId,
                    FileName = fileName,
                    FilePath = null,
                    FolderId = folderId,
                    Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
                    GemaaktOp = DateTime.Now,
                    Status = 1
                };

                _context.Bronnen.Add(bron);
                await _context.SaveChangesAsync();

                var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(chunkTexts);

                if (embeddings.Count != chunkTexts.Count)
                    throw new Exception($"Embedding count mismatch: got {embeddings.Count}, expected {chunkTexts.Count}.");

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status)
                    VALUES (@BronID, @Tekst, @TekstVector::vector, NOW(), @Status)
                    RETURNING id;";

                for (int i = 0; i < chunkTexts.Count; i++)
                {
                    var vectorString = "[" + string.Join(",", embeddings[i].Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
                    using var cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@BronID", bron.ID);
                    cmd.Parameters.AddWithValue("@Tekst", chunkTexts[i]);
                    cmd.Parameters.AddWithValue("@TekstVector", vectorString);
                    cmd.Parameters.AddWithValue("@Status", 1);
                    await cmd.ExecuteScalarAsync();
                }

                TempData["Success"] = $"'{title.Trim()}' succesvol verwerkt en opgeslagen.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (bron?.ID > 0)
                {
                    bron.Status = 2;
                    await _context.SaveChangesAsync();
                }

                ModelState.AddModelError(string.Empty, $"Fout bij verwerking: {ex.Message}");
                ViewBag.InputTitle = title;
                ViewBag.InputText = text;
                return View();
            }
        }

        // ── Download / Preview / Delete ─────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null || bron.FileName == null || bron.Status != 1)
                return NotFound();

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Forbid();

            var chunkTexts = await _context.Chunks
                .Where(c => c.BronID == id && c.Status == 1)
                .OrderBy(c => c.ID)
                .Select(c => c.Tekst)
                .ToListAsync();

            if (!chunkTexts.Any())
                return NotFound();

            var text = string.Join("\n\n", chunkTexts);
            var downloadName = Path.GetFileNameWithoutExtension(bron.FileName) + ".txt";
            return File(System.Text.Encoding.UTF8.GetBytes(text), "text/plain", downloadName);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null || bron.FileName == null || bron.Status != 1)
                return NotFound();

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Forbid();

            var text = await GetBronTextAsync(bron);

            const int maxChars = 15000;
            bool truncated = text.Length > maxChars;
            if (truncated)
                text = text[..maxChars];

            return Json(new { title = bron.Title, fileName = bron.FileName, text, truncated });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var bron = await _context.Bronnen.FindAsync(id);
            if (bron == null)
                return NotFound();

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Forbid();

            await _context.Chunks
                .Where(c => c.BronID == id && c.Status == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, 2));

            bron.Status = 2;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Document '{bron.Title}' is verwijderd.";
            return RedirectToAction(nameof(Index));
        }

        // ── Chunk editing ──────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> EditChunks(int bronId)
        {
            var bron = await _context.Bronnen.FindAsync(bronId);
            if (bron == null || bron.Status != 1)
                return NotFound();

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Forbid();

            var chunks = await _context.Chunks
                .Where(c => c.BronID == bronId && c.Status == 1)
                .OrderBy(c => c.ID)
                .ToListAsync();

            ViewBag.Bron = bron;
            return View(chunks);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChunk(int chunkId, string tekst)
        {
            var chunk = await _context.Chunks
                .Include(c => c.Bron)
                .FirstOrDefaultAsync(c => c.ID == chunkId);

            if (chunk == null || chunk.Status != 1)
                return Json(new { success = false, message = "Chunk niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == chunk.Bron!.Project))
                return Json(new { success = false, message = "Geen toegang." });

            if (string.IsNullOrWhiteSpace(tekst))
                return Json(new { success = false, message = "Tekst is verplicht." });

            var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(new List<string> { tekst.Trim() });
            var vectorString = "[" + string.Join(",", embeddings[0].Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "UPDATE chunk SET tekst = @Tekst, tekstvector = @Vector::vector, gewijzigdop = NOW() WHERE id = @ID",
                connection);
            cmd.Parameters.AddWithValue("@Tekst", tekst.Trim());
            cmd.Parameters.AddWithValue("@Vector", vectorString);
            cmd.Parameters.AddWithValue("@ID", chunkId);
            await cmd.ExecuteNonQueryAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChunk(int chunkId)
        {
            var chunk = await _context.Chunks
                .Include(c => c.Bron)
                .FirstOrDefaultAsync(c => c.ID == chunkId);

            if (chunk == null || chunk.Status != 1)
                return Json(new { success = false, message = "Chunk niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == chunk.Bron!.Project))
                return Json(new { success = false, message = "Geen toegang." });

            await _context.Chunks
                .Where(c => c.ID == chunkId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, 2));

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddChunk(int bronId, string tekst)
        {
            var bron = await _context.Bronnen.FindAsync(bronId);
            if (bron == null || bron.Status != 1)
                return Json(new { success = false, message = "Document niet gevonden." });

            var accessible = await GetAccessibleProjectsAsync();
            if (!accessible.Any(p => p.ID == bron.Project))
                return Json(new { success = false, message = "Geen toegang." });

            if (string.IsNullOrWhiteSpace(tekst))
                return Json(new { success = false, message = "Tekst is verplicht." });

            var embeddings = await _embeddingProcessor.GenerateBatchEmbeddingsAsync(new List<string> { tekst.Trim() });
            var vectorString = "[" + string.Join(",", embeddings[0].Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status) VALUES (@BronID, @Tekst, @Vector::vector, NOW(), 1) RETURNING id",
                connection);
            cmd.Parameters.AddWithValue("@BronID", bronId);
            cmd.Parameters.AddWithValue("@Tekst", tekst.Trim());
            cmd.Parameters.AddWithValue("@Vector", vectorString);
            var newId = (int)(await cmd.ExecuteScalarAsync())!;

            return Json(new { success = true, chunkId = newId });
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task<string> GetBronTextAsync(Bron bron)
        {
            var chunkTexts = await _context.Chunks
                .Where(c => c.BronID == bron.ID && c.Status == 1)
                .OrderBy(c => c.ID)
                .Select(c => c.Tekst)
                .ToListAsync();

            return chunkTexts.Any() ? string.Join("\n\n", chunkTexts) : string.Empty;
        }

        private static HashSet<int> GetSubtreeFolderIds(List<Folder> allFolders, int rootId)
        {
            var result = new HashSet<int> { rootId };
            var queue = new Queue<int>(new[] { rootId });
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in allFolders.Where(f => f.ParentId == current))
                {
                    if (result.Add(child.ID))
                        queue.Enqueue(child.ID);
                }
            }
            return result;
        }

        private async Task<List<Project>> GetAccessibleProjectsAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return await _context.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();
            }

            var userId = User.GetUserId();

            var devProjectIds = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .Select(gp => gp.Project)
                .ToListAsync();

            // Also include prod projects that are linked to the user's dev projects
            var prodProjectIds = await _context.Projects
                .Where(p => devProjectIds.Contains(p.ID) && p.ProductieProjectId != null)
                .Select(p => p.ProductieProjectId!.Value)
                .ToListAsync();

            var allIds = devProjectIds.Concat(prodProjectIds).ToList();

            return await _context.Projects
                .Where(p => p.Status == 1 && allIds.Contains(p.ID))
                .OrderBy(p => p.Naam)
                .ToListAsync();
        }
    }
}
