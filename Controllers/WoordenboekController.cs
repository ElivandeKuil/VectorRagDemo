using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class WoordenboekController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly OmgevingService _omgevingService;

        public WoordenboekController(VectorDbContext context, OmgevingService omgevingService)
        {
            _context = context;
            _omgevingService = omgevingService;
        }

        public async Task<IActionResult> Index(int projectId = 0, int pagina = 1)
        {
            var project = await ResolveProjectAsync(projectId);

            if (project == null)
            {
                if (User.IsInRole("Admin"))
                {
                    var allProjects = await _context.Projects
                        .Where(p => p.Status == 1).OrderBy(p => p.Naam).ToListAsync();
                    ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam");
                    return View("SelectProject");
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1).OrderBy(p => p.Naam).ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            pagina = Math.Max(1, pagina);
            var offset = (pagina - 1) * WoordenboekViewModel.PaginaGrootte;

            var query = _context.Woordenboeken
                .Where(w => w.ProjectID == project.ID && w.Status == 1)
                .OrderBy(w => w.Woord);

            var totaal = await query.CountAsync();

            var rows = await query
                .Skip(offset)
                .Take(WoordenboekViewModel.PaginaGrootte)
                .Select(w => new WoordenboekRijViewModel
                {
                    ID           = w.ID,
                    Woord        = w.Woord,
                    Omschrijving = w.Omschrijving,
                    GemaaktOp    = w.GemaaktOp,
                    GewijzigdOp  = w.GewijzigdOp
                })
                .ToListAsync();

            var vm = new WoordenboekViewModel
            {
                Project      = project,
                Entries      = rows,
                TotaalAantal = totaal,
                Pagina       = pagina
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Aanmaken(int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return RedirectToAction(nameof(Index));

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1).OrderBy(p => p.Naam).ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            return View(new WoordenboekAanmakenViewModel { ProjectID = project.ID, Project = project });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aanmaken(string woord, string omschrijving, int projectId = 0)
        {
            if (string.IsNullOrWhiteSpace(woord) || string.IsNullOrWhiteSpace(omschrijving))
                return BadRequest("Woord en omschrijving zijn verplicht.");

            var project = await ResolveProjectAsync(projectId);
            if (project == null) return Forbid();

            _context.Woordenboeken.Add(new Woordenboek
            {
                ProjectID    = project.ID,
                Woord        = woord.Trim(),
                Omschrijving = omschrijving.Trim(),
                GemaaktOp    = DateTime.Now,
                Status       = 1
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId = User.IsInRole("Admin") ? project.ID : 0 });
        }

        [HttpGet]
        public async Task<IActionResult> Bewerk(int id, int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return RedirectToAction(nameof(Index));

            var entry = await _context.Woordenboeken
                .FirstOrDefaultAsync(w => w.ID == id && w.ProjectID == project.ID && w.Status == 1);

            if (entry == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1).OrderBy(p => p.Naam).ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            return View(new WoordenboekBewerkenViewModel
            {
                ID           = entry.ID,
                ProjectID    = project.ID,
                Woord        = entry.Woord,
                Omschrijving = entry.Omschrijving,
                Project      = project
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bewerk(int id, string woord, string omschrijving, int projectId = 0)
        {
            if (string.IsNullOrWhiteSpace(woord) || string.IsNullOrWhiteSpace(omschrijving))
                return BadRequest("Woord en omschrijving zijn verplicht.");

            var project = await ResolveProjectAsync(projectId);
            if (project == null) return Forbid();

            var entry = await _context.Woordenboeken
                .FirstOrDefaultAsync(w => w.ID == id && w.ProjectID == project.ID && w.Status == 1);

            if (entry == null) return NotFound();

            entry.Woord        = woord.Trim();
            entry.Omschrijving = omschrijving.Trim();
            entry.GewijzigdOp  = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId = User.IsInRole("Admin") ? project.ID : 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verwijder(int id, int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return Forbid();

            var entry = await _context.Woordenboeken
                .FirstOrDefaultAsync(w => w.ID == id && w.ProjectID == project.ID && w.Status == 1);

            if (entry == null) return NotFound();

            entry.Status      = 0;
            entry.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId = User.IsInRole("Admin") ? project.ID : 0 });
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
