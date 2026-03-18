using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class ConversatiesController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly ConversatieRepository _repo;

        public ConversatiesController(VectorDbContext context, ConversatieRepository repo)
        {
            _context = context;
            _repo    = repo;
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

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<Project?> ResolveProjectAsync(int projectId)
        {
            if (User.IsInRole("Admin"))
            {
                if (projectId <= 0) return null;
                return await _context.Projects.FindAsync(projectId);
            }

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry == null) return null;
            return await _context.Projects.FindAsync(entry.Project);
        }
    }
}
