using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VectorRagDemo.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [Route("/hoe-het-werkt")]
        public IActionResult HoeHetWerkt() => View();

        [Route("/tarieven")]
        public IActionResult Tarieven() => View();

        [Route("/over-ons")]
        public IActionResult OverOns() => View();

        [Route("/contact")]
        public IActionResult Contact() => View();

        [Route("/privacybeleid")]
        public IActionResult Privacybeleid() => View();

        [Route("/algemene-voorwaarden")]
        public IActionResult AlgemeneVoorwaarden() => View();

        [Route("/Home/Error")]
        public IActionResult Error() => View("Error");
    }
}
