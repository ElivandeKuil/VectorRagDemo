using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Services;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class ConversatiesController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly ConversatieRepository _repo;
        private readonly CorrectieService _correctieService;
        private readonly OmgevingService _omgevingService;

        public ConversatiesController(VectorDbContext context, ConversatieRepository repo, CorrectieService correctieService, OmgevingService omgevingService)
        {
            _context          = context;
            _repo             = repo;
            _correctieService = correctieService;
            _omgevingService  = omgevingService;
        }

        public async Task<IActionResult> Index(int projectId = 0, ConversatieFilterModel? filter = null)
        {
            filter ??= new ConversatieFilterModel();

            var project = await ResolveProjectAsync(projectId);

            if (project == null)
            {
                if (User.IsInRole("Admin"))
                {
                    var allProjects = await _context.Projects
                        .Where(p => p.Status == 1)
                        .OrderBy(p => p.Naam)
                        .ToListAsync();
                    ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam");
                    return View("SelectProject");
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            var heeftEscalatie = false;
            if (project.ExtraCommunicationEnabled)
            {
                await _context.Entry(project).Reference(p => p.WidgetConfig).LoadAsync();
                var cfg = project.WidgetConfig;
                heeftEscalatie = cfg != null &&
                    ((cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber)) ||
                     (cfg.EmailEnabled   && !string.IsNullOrWhiteSpace(cfg.EmailAddress)));
            }

            var (gesprekken, totaal) = await _repo.GetPaginatedAsync(project.ID, filter);

            var vm = new ConversatiesViewModel
            {
                Project              = project,
                Filter               = filter,
                Gesprekken           = gesprekken,
                TotaalAantal         = totaal,
                HeeftEscalatieFunctie = heeftEscalatie
            };

            return View(vm);
        }

        public async Task<IActionResult> Detail(int id, int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);

            if (project == null)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction(nameof(Index));
                return RedirectToAction("Index", "Dashboard");
            }

            var vm = await _repo.GetDetailAsync(id, project.ID);
            if (vm == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SlaCorrectieOp(
            int conversationId, int messageId, string correctieTekst, int projectId = 0)
        {
            if (string.IsNullOrWhiteSpace(correctieTekst))
                return BadRequest("Correctietekst mag niet leeg zijn.");

            var project = await ResolveProjectAsync(projectId);
            if (project == null)
                return Forbid();

            // Verify the conversation belongs to this project
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ID == conversationId && c.ProjectID == project.ID);
            if (conversation == null)
                return NotFound();

            await _correctieService.SlaCorrectieOpAsync(conversationId, messageId, correctieTekst, project.ID);

            return Ok();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<Project?> ResolveProjectAsync(int projectId)
        {
            if (User.IsInRole("Admin"))
            {
                var id = _omgevingService.ResolveForAdmin(projectId);
                if (id <= 0) return null;
                return await _context.Projects.FindAsync(id);
            }

            var userId = User.GetUserId();
            var activeId = await _omgevingService.ResolveForUserAsync(userId);
            if (activeId <= 0) return null;
            return await _context.Projects.FindAsync(activeId);
        }
    }
}
