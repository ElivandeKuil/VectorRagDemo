using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class StatistiekenController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly StatistiekenService _statistiekenService;
        private readonly EscalatieService _escalatieService;
        private readonly OmgevingService _omgevingService;

        public StatistiekenController(
            VectorDbContext context,
            StatistiekenService statistiekenService,
            EscalatieService escalatieService,
            OmgevingService omgevingService)
        {
            _context             = context;
            _statistiekenService = statistiekenService;
            _escalatieService    = escalatieService;
            _omgevingService     = omgevingService;
        }

        public async Task<IActionResult> Index(int projectId = 0)
        {
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

            if (project.StatistiekenTier == 0)
            {
                ViewData["ProjectName"] = project.Naam;
                return View("Unavailable");
            }

            var heeftEscalatie = await _escalatieService.HeeftEscalatieAsync(project);
            var vm = await _statistiekenService.BuildAsync(project, heeftEscalatie);
            return View(vm);
        }

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
