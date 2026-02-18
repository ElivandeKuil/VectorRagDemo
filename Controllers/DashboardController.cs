using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ProjectAccessService _projectAccessService;

        public DashboardController(ProjectAccessService projectAccessService)
        {
            _projectAccessService = projectAccessService;
        }

        public async Task<IActionResult> Index()
        {
            List<Models.Entities.Management.MgmtProject> projects;

            if (User.IsInRole("Admin"))
            {
                projects = await _projectAccessService.GetAllActiveProjectsAsync();
            }
            else
            {
                var userId = User.GetUserId();
                projects = await _projectAccessService.GetUserProjectsAsync(userId);
            }

            var vm = projects
                .Select(p => new ProjectCardViewModel
                {
                    Project = p,
                    RouteInfo = ProjectAccessService.ProjectMap.GetValueOrDefault(p.Omschrijving)
                })
                .ToList();

            return View(vm);
        }
    }
}
