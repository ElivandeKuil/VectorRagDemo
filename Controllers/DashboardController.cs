using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ProjectAccessService _projectAccessService;
        private readonly VectorDbContext _vectorContext;

        public DashboardController(ProjectAccessService projectAccessService, VectorDbContext vectorContext)
        {
            _projectAccessService = projectAccessService;
            _vectorContext = vectorContext;
        }

        public async Task<IActionResult> Index()
        {
            var fullName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "gebruiker";
            var isAdmin = User.IsInRole("Admin");

            if (isAdmin)
            {
                var projects = await _projectAccessService.GetAllActiveProjectsAsync();

                if (!projects.Any(p => p.Omschrijving.Equals("Gebruikersbeheer", StringComparison.OrdinalIgnoreCase)))
                    projects.Add(ProjectAccessService.GebruikersbeheerProject);

                var cards = projects
                    .Select(p => new ProjectCardViewModel
                    {
                        Project = p,
                        RouteInfo = ProjectAccessService.ProjectMap.GetValueOrDefault(p.Omschrijving)
                    })
                    .ToList();

                return View(new DashboardViewModel
                {
                    IsAdmin = true,
                    FullName = fullName,
                    ProjectCards = cards
                });
            }
            else
            {
                var userId = User.GetUserId();

                var userSubProjects = User.Claims
                    .Where(c => c.Type == "SubProject")
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var userProject = userId > 0
                    ? await _vectorContext.GebruikerProjecten
                        .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                        .Join(_vectorContext.Projects,
                              gp => gp.Project,
                              p => p.ID,
                              (gp, p) => p)
                        .FirstOrDefaultAsync()
                    : null;

                var tiles = ProjectAccessService.SubProjectMap
                    .Where(e => userSubProjects.Contains(e.Key))
                    .Where(e => e.Key != "Statistieken" || (userProject?.StatistiekenTier ?? 0) > 0)
                    .Select(e => e.Value)
                    .ToList();

                var showPromptInstelling = userProject != null && !userProject.GebruikPromptTabel;

                return View(new DashboardViewModel
                {
                    IsAdmin = false,
                    FullName = fullName,
                    Tiles = tiles,
                    ShowPromptInstelling = showPromptInstelling
                });
            }
        }
    }
}
