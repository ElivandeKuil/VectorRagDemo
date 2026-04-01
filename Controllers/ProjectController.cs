using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly OmgevingService _omgevingService;

        public ProjectController(VectorDbContext context, IConfiguration configuration, OmgevingService omgevingService)
        {
            _context = context;
            _configuration = configuration;
            _omgevingService = omgevingService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .OrderBy(p => p.Naam)
                .ToListAsync();

            return View(projects);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Project { BotName = "Assistant" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Project model)
        {
            if (string.IsNullOrWhiteSpace(model.Naam))
                ModelState.AddModelError(nameof(model.Naam), "Naam is verplicht.");

            if (!ModelState.IsValid)
                return View(model);

            var project = new Project
            {
                Naam = model.Naam.Trim(),
                BotName = string.IsNullOrWhiteSpace(model.BotName) ? "Assistant" : model.BotName.Trim(),
                EmbedKey = Guid.NewGuid().ToString("N"),
                GemaaktOp = DateTime.Now,
                Status = 1
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is aangemaakt.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id = 0)
        {
            if (!User.IsInRole("Admin"))
            {
                // Non-admins always see their own active project
                var userId = User.GetUserId();
                id = await _omgevingService.ResolveForUserAsync(userId);
                if (id == 0) return RedirectToAction("Index", "Dashboard");
            }
            else if (id == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ID == id);
            if (project == null) return NotFound();

            if (!User.IsInRole("Admin") && !await CanUserAccessProjectAsync(id))
                return Forbid();

            await LoadOmgevingViewBagAsync(id, project.ProductieProjectId);
            return View(project);
        }

        private async Task LoadOmgevingViewBagAsync(int projectId, int? huidigProductieProjectId)
        {
            ViewBag.BeschikbareProductieProjecten = await _context.Projects
                .Where(p => p.ID != projectId && p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();

            if (huidigProductieProjectId.HasValue)
                ViewBag.ProductieProjectNaam = (await _context.Projects.FindAsync(huidigProductieProjectId))?.Naam;

            var devProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProductieProjectId == projectId);
            ViewBag.DevProjectNaam = devProject?.Naam;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model, int statistiekenTier)
        {
            if (!User.IsInRole("Admin") && !await CanUserAccessProjectAsync(id))
                return Forbid();

            if (string.IsNullOrWhiteSpace(model.Naam))
                ModelState.AddModelError(nameof(model.Naam), "Naam is verplicht.");

            if (!ModelState.IsValid)
            {
                model.ID = id;
                await LoadOmgevingViewBagAsync(id, model.ProductieProjectId);
                return View(model);
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ID == id);
            if (project == null) return NotFound();

            project.Naam = model.Naam.Trim();
            project.BotName = string.IsNullOrWhiteSpace(model.BotName) ? "Assistant" : model.BotName.Trim();

            if (User.IsInRole("Admin"))
            {
                project.GebruikPromptTabel = model.GebruikPromptTabel;
                project.ExtraCommunicationEnabled = model.ExtraCommunicationEnabled;
                project.KoppelingIngeschakeld = model.KoppelingIngeschakeld;
                project.StatistiekenTier = Math.Clamp(statistiekenTier, 0, 2);
            }

            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is bijgewerkt.";
            return User.IsInRole("Admin")
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateEmbedKey(int id)
        {
            if (!User.IsInRole("Admin") && !await CanUserAccessProjectAsync(id))
                return Forbid();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.EmbedKey = Guid.NewGuid().ToString("N");
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Embed-sleutel voor '{project.Naam}' is vervangen.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.Status = project.Status == 1 ? 0 : 1;
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is {(project.Status == 1 ? "geactiveerd" : "gedeactiveerd")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LinkProductieProject(int id, int productieProjectId)
        {
            var devProject = await _context.Projects.FindAsync(id);
            if (devProject == null) return NotFound();

            var prodProject = await _context.Projects.FindAsync(productieProjectId);
            if (prodProject == null) return NotFound();

            var alreadyUsed = await _context.Projects
                .AnyAsync(p => p.ProductieProjectId == productieProjectId && p.ID != id);
            if (alreadyUsed)
            {
                TempData["Error"] = $"'{prodProject.Naam}' is al gekoppeld als productieomgeving van een ander project.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            devProject.ProductieProjectId = productieProjectId;
            devProject.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{prodProject.Naam}' is gekoppeld als productieomgeving.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlinkProductieProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.ProductieProjectId = null;
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Koppeling met productieomgeving verwijderd.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloneNaarProductie(int id)
        {
            if (!User.IsInRole("Admin") && !await CanUserAccessProjectAsync(id))
                return Forbid();

            var devProject = await _context.Projects
                .Include(p => p.WidgetConfig)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (devProject == null) return NotFound();
            if (!devProject.ProductieProjectId.HasValue)
            {
                TempData["Error"] = "Dit project heeft geen gekoppeld productieproject.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var prodProjectId = devProject.ProductieProjectId.Value;
            var devBronnen = await _context.Bronnen
                .Where(b => b.Project == id && b.Status == 1)
                .ToListAsync();

            // --- Clone bronnen + chunks via raw SQL (preserves tekstvector) ---
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using (var cmd = new NpgsqlCommand(
                "UPDATE chunk SET status = 0, gewijzigdop = NOW() WHERE bronid IN (SELECT id FROM bron WHERE project = @ProdId)", conn))
            {
                cmd.Parameters.AddWithValue("ProdId", prodProjectId);
                await cmd.ExecuteNonQueryAsync();
            }
            await using (var cmd = new NpgsqlCommand(
                "UPDATE bron SET status = 0, gewijzigdop = NOW() WHERE project = @ProdId", conn))
            {
                cmd.Parameters.AddWithValue("ProdId", prodProjectId);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var devBron in devBronnen)
            {
                int newBronId;
                await using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO bron (title, project, gemaaktop, status, filename, filepath, folderid, link)
                      VALUES (@Title, @Project, NOW(), 1, @FileName, @FilePath, NULL, @Link)
                      RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("Title", devBron.Title);
                    cmd.Parameters.AddWithValue("Project", prodProjectId);
                    cmd.Parameters.AddWithValue("FileName", (object?)devBron.FileName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("FilePath", (object?)devBron.FilePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("Link", (object?)devBron.Link ?? DBNull.Value);
                    newBronId = (int)(await cmd.ExecuteScalarAsync())!;
                }

                await using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status, correctie)
                      SELECT @NewBronId, tekst, tekstvector, NOW(), 1, correctie
                      FROM chunk WHERE bronid = @DevBronId AND status = 1", conn))
                {
                    cmd.Parameters.AddWithValue("NewBronId", newBronId);
                    cmd.Parameters.AddWithValue("DevBronId", devBron.ID);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await using (var cmd = new NpgsqlCommand("UPDATE project SET gewijzigdop = NOW() WHERE id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("Id", prodProjectId);
                await cmd.ExecuteNonQueryAsync();
            }

            // --- Clone project-level paid feature settings via EF Core ---
            var prodProject = await _context.Projects.FindAsync(prodProjectId);
            if (prodProject != null)
            {
                prodProject.GebruikPromptTabel        = devProject.GebruikPromptTabel;
                prodProject.ExtraCommunicationEnabled = devProject.ExtraCommunicationEnabled;
                prodProject.KoppelingIngeschakeld     = devProject.KoppelingIngeschakeld;
                prodProject.StatistiekenTier          = devProject.StatistiekenTier;
                await _context.SaveChangesAsync();
            }

            // --- Clone widget config via EF Core ---
            if (devProject.WidgetConfig != null)
            {
                var devCfg = devProject.WidgetConfig;
                var prodCfg = await _context.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectID == prodProjectId);

                if (prodCfg == null)
                {
                    prodCfg = new WidgetConfig { ProjectID = prodProjectId };
                    _context.WidgetConfigs.Add(prodCfg);
                }

                prodCfg.WidgetPosition      = devCfg.WidgetPosition;
                prodCfg.OffsetX             = devCfg.OffsetX;
                prodCfg.OffsetY             = devCfg.OffsetY;
                prodCfg.ButtonColor         = devCfg.ButtonColor;
                prodCfg.ButtonSize          = devCfg.ButtonSize;
                prodCfg.ButtonLogoUrl       = devCfg.ButtonLogoUrl;
                prodCfg.PopupWidth          = devCfg.PopupWidth;
                prodCfg.PopupHeight         = devCfg.PopupHeight;
                prodCfg.PopupBorderRadius   = devCfg.PopupBorderRadius;
                prodCfg.HeaderBgColor       = devCfg.HeaderBgColor;
                prodCfg.HeaderTextColor     = devCfg.HeaderTextColor;
                prodCfg.HeaderTitle         = devCfg.HeaderTitle;
                prodCfg.HeaderLogoUrl       = devCfg.HeaderLogoUrl;
                prodCfg.UserBubbleBgColor   = devCfg.UserBubbleBgColor;
                prodCfg.UserBubbleTextColor = devCfg.UserBubbleTextColor;
                prodCfg.BotBubbleBgColor    = devCfg.BotBubbleBgColor;
                prodCfg.BotBubbleTextColor  = devCfg.BotBubbleTextColor;
                prodCfg.ChatBodyBgColor     = devCfg.ChatBodyBgColor;
                prodCfg.InputAreaBgColor    = devCfg.InputAreaBgColor;
                prodCfg.SendButtonColor     = devCfg.SendButtonColor;
                prodCfg.SendButtonIconColor = devCfg.SendButtonIconColor;
                prodCfg.WidgetFontSize      = devCfg.WidgetFontSize;
                prodCfg.GreetingMessage     = devCfg.GreetingMessage;
                prodCfg.ExtraCommunicationEnabled = devCfg.ExtraCommunicationEnabled;
                prodCfg.WhatsAppEnabled     = devCfg.WhatsAppEnabled;
                prodCfg.WhatsAppNumber      = devCfg.WhatsAppNumber;
                prodCfg.WhatsAppButtonText  = devCfg.WhatsAppButtonText;
                prodCfg.WhatsAppCtaText     = devCfg.WhatsAppCtaText;
                prodCfg.EmailEnabled        = devCfg.EmailEnabled;
                prodCfg.EmailAddress        = devCfg.EmailAddress;
                prodCfg.EmailSubject        = devCfg.EmailSubject;
                prodCfg.EmailButtonText     = devCfg.EmailButtonText;
                prodCfg.EmailCtaText        = devCfg.EmailCtaText;

                await _context.SaveChangesAsync();
            }

            // --- Clone PromptInstelling via EF Core ---
            var devPromptInstelling = await _context.PromptInstellingen.FirstOrDefaultAsync(p => p.ProjectID == id);
            if (devPromptInstelling != null)
            {
                var devPi = devPromptInstelling;
                var prodPi = await _context.PromptInstellingen.FirstOrDefaultAsync(p => p.ProjectID == prodProjectId);

                if (prodPi == null)
                {
                    prodPi = new PromptInstelling { ProjectID = prodProjectId };
                    _context.PromptInstellingen.Add(prodPi);
                }

                prodPi.Persona     = devPi.Persona;
                prodPi.Instructies = devPi.Instructies;
                prodPi.Regels      = devPi.Regels;
                prodPi.Voorbeelden = devPi.Voorbeelden;
                prodPi.GewijzigdOp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            // --- Clone Prompts via EF Core ---
            var devPrompts = await _context.Prompts
                .Where(p => p.Project == id && p.Status == 1)
                .ToListAsync();

            var prodPrompts = await _context.Prompts
                .Where(p => p.Project == prodProjectId)
                .ToListAsync();

            // Deactivate existing prod prompts that no longer exist in dev (by PromptType)
            var devPromptTypes = devPrompts.Select(p => p.PromptType).ToHashSet();
            foreach (var prodPrompt in prodPrompts.Where(p => !devPromptTypes.Contains(p.PromptType)))
            {
                prodPrompt.Status     = 0;
                prodPrompt.GewijzigdOp = DateTime.UtcNow;
            }

            foreach (var devPrompt in devPrompts)
            {
                var prodPrompt = prodPrompts.FirstOrDefault(p => p.PromptType == devPrompt.PromptType);
                if (prodPrompt == null)
                {
                    _context.Prompts.Add(new Prompt
                    {
                        Project           = prodProjectId,
                        PromptType        = devPrompt.PromptType,
                        SystemInstruction = devPrompt.SystemInstruction,
                        Content           = devPrompt.Content,
                        ResponseSchema    = devPrompt.ResponseSchema,
                        MaxTokens         = devPrompt.MaxTokens,
                        Temperature       = devPrompt.Temperature,
                        TopP              = devPrompt.TopP,
                        TopK              = devPrompt.TopK,
                        Model             = devPrompt.Model,
                        Volgorde          = devPrompt.Volgorde,
                        Status            = 1,
                        GemaaktOp         = DateTime.UtcNow,
                    });
                }
                else
                {
                    prodPrompt.SystemInstruction = devPrompt.SystemInstruction;
                    prodPrompt.Content           = devPrompt.Content;
                    prodPrompt.ResponseSchema    = devPrompt.ResponseSchema;
                    prodPrompt.MaxTokens         = devPrompt.MaxTokens;
                    prodPrompt.Temperature       = devPrompt.Temperature;
                    prodPrompt.TopP              = devPrompt.TopP;
                    prodPrompt.TopK              = devPrompt.TopK;
                    prodPrompt.Model             = devPrompt.Model;
                    prodPrompt.Volgorde          = devPrompt.Volgorde;
                    prodPrompt.Status            = 1;
                    prodPrompt.GewijzigdOp       = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Kennis, widget-instellingen en prompts succesvol gekloond naar de productieomgeving ({devBronnen.Count} bron{(devBronnen.Count == 1 ? "" : "nen")}).";
            return RedirectToAction(nameof(Edit), new { id });
        }

        private async Task<bool> CanUserAccessProjectAsync(int projectId)
        {
            var userId = User.GetUserId();
            var (devId, prodId) = await _omgevingService.GetUserPairingAsync(userId);
            return projectId == devId || (prodId.HasValue && projectId == prodId.Value);
        }
    }
}
