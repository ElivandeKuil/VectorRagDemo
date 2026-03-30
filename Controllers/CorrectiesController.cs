using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class CorrectiesController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly CorrectieService _correctieService;
        private readonly OmgevingService _omgevingService;

        public CorrectiesController(VectorDbContext context, CorrectieService correctieService, OmgevingService omgevingService)
        {
            _context          = context;
            _correctieService = correctieService;
            _omgevingService  = omgevingService;
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
            var offset = (pagina - 1) * CorrectiesViewModel.PaginaGrootte;

            var query = _context.Correcties
                .Where(c => c.ProjectID == project.ID && c.Status == 1)
                .OrderByDescending(c => c.GemaaktOp);

            var totaal = await query.CountAsync();

            var rows = await query
                .Skip(offset)
                .Take(CorrectiesViewModel.PaginaGrootte)
                .Select(c => new CorrectieRijViewModel
                {
                    ID                       = c.ID,
                    ConversatieID            = c.ConversatieID,
                    MessageID                = c.MessageID,
                    OrigineleTekst           = c.OrigineleTekst,
                    GecontextualiseerdeVraag = c.GecontextualiseerdeVraag,
                    Correctietekst           = c.Correctietekst,
                    GemaaktOp                = c.GemaaktOp,
                    GewijzigdOp              = c.GewijzigdOp
                })
                .ToListAsync();

            var vm = new CorrectiesViewModel
            {
                Project      = project,
                Correcties   = rows,
                TotaalAantal = totaal,
                Pagina       = pagina
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Bewerk(int id, int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return RedirectToAction(nameof(Index));

            var correctie = await _context.Correcties
                .FirstOrDefaultAsync(c => c.ID == id && c.ProjectID == project.ID && c.Status == 1);

            if (correctie == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Where(p => p.Status == 1).OrderBy(p => p.Naam).ToListAsync();
                ViewData["AdminProjects"] = new SelectList(allProjects, "ID", "Naam", project.ID);
            }

            var vm = new CorrectieBewerkenViewModel
            {
                ID                       = correctie.ID,
                ProjectID                = project.ID,
                ConversatieID            = correctie.ConversatieID,
                OrigineleTekst           = correctie.OrigineleTekst,
                GecontextualiseerdeVraag = correctie.GecontextualiseerdeVraag,
                Correctietekst           = correctie.Correctietekst,
                Project                  = project
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bewerk(int id, string correctietekst, int projectId = 0)
        {
            if (string.IsNullOrWhiteSpace(correctietekst))
                return BadRequest("Correctietekst mag niet leeg zijn.");

            var project = await ResolveProjectAsync(projectId);
            if (project == null) return Forbid();

            await _correctieService.UpdateCorrectieAsync(id, correctietekst, project.ID);

            return RedirectToAction(nameof(Index), new { projectId = User.IsInRole("Admin") ? project.ID : 0 });
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<VectorRagDemo.Models.Entities.Project?> ResolveProjectAsync(int projectId)
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
