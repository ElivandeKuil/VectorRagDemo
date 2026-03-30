using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class OmgevingController : Controller
    {
        private readonly OmgevingService _omgevingService;

        public OmgevingController(OmgevingService omgevingService)
        {
            _omgevingService = omgevingService;
        }

        /// <summary>
        /// Stores the chosen project in a persistent cookie and returns to the calling page.
        /// This is a GET so it works from simple links in the header switcher.
        /// </summary>
        public IActionResult Switch(int projectId, string returnUrl = "/")
        {
            if (projectId > 0)
                _omgevingService.SetActiveProjectId(projectId);

            return LocalRedirect(returnUrl);
        }
    }
}
