using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.ViewComponents
{
    public class NavViewComponent : ViewComponent
    {
        private readonly VectorDbContext _vectorContext;
        private readonly OmgevingService _omgevingService;

        public NavViewComponent(VectorDbContext vectorContext, OmgevingService omgevingService)
        {
            _vectorContext = vectorContext;
            _omgevingService = omgevingService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var isAuthenticated = UserClaimsPrincipal.Identity?.IsAuthenticated == true;
            var isAdmin = UserClaimsPrincipal.IsInRole("Admin");
            var userName = UserClaimsPrincipal.Identity?.Name ?? string.Empty;
            var switchReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;

            if (isAdmin)
            {
                var activeId = _omgevingService.GetActiveProjectId();
                var allProjects = await _vectorContext.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();

                var prodIds = allProjects
                    .Where(p => p.ProductieProjectId.HasValue)
                    .Select(p => p.ProductieProjectId!.Value)
                    .ToHashSet();

                var navProjects = allProjects
                    .Select(p => new NavProjectItem(
                        ID: p.ID,
                        Naam: p.Naam,
                        IsDev: p.ProductieProjectId.HasValue,
                        IsProd: prodIds.Contains(p.ID)
                    ))
                    .ToList();

                return View(new NavViewModel
                {
                    IsAdmin = true,
                    IsAuthenticated = isAuthenticated,
                    UserName = userName,
                    SwitchReturnUrl = switchReturnUrl,
                    AllProjects = navProjects,
                    ActiveProjectId = activeId,
                    ActiveProjectName = allProjects.FirstOrDefault(p => p.ID == activeId)?.Naam ?? "Geen project"
                });
            }
            else
            {
                var userId = int.TryParse(
                    UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var uid) ? uid : 0;

                var userSubProjects = UserClaimsPrincipal.Claims
                    .Where(c => c.Type == "SubProject")
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Models.Entities.Project? userProject = null;
                if (userId > 0)
                {
                    userProject = await _vectorContext.GebruikerProjecten
                        .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                        .Join(_vectorContext.Projects,
                              gp => gp.Project,
                              p => p.ID,
                              (gp, p) => p)
                        .FirstOrDefaultAsync();
                }

                var navItems = ProjectAccessService.SubProjectMap
                    .Where(e => userSubProjects.Contains(e.Key))
                    .Where(e => e.Key != "Statistieken" || (userProject?.StatistiekenTier ?? 0) > 0)
                    .Select(e => e.Value)
                    .ToList();

                DevProdSwitcherInfo? devProdSwitcher = null;
                if (userProject?.ProductieProjectId.HasValue == true)
                {
                    var activeId = _omgevingService.GetActiveProjectId();
                    devProdSwitcher = new DevProdSwitcherInfo(
                        DevProjectId: userProject.ID,
                        ProdProjectId: userProject.ProductieProjectId!.Value,
                        IsDevActive: activeId == 0 || activeId == userProject.ID
                    );
                }

                return View(new NavViewModel
                {
                    IsAdmin = false,
                    IsAuthenticated = isAuthenticated,
                    UserName = userName,
                    SwitchReturnUrl = switchReturnUrl,
                    NavItems = navItems,
                    ShowPromptInstelling = userProject != null && !userProject.GebruikPromptTabel,
                    DevProdSwitcher = devProdSwitcher
                });
            }
        }
    }
}
