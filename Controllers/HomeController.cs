using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace VectorRagDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly VectorDbContext _context;

        public HomeController(VectorDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalChunks = await _context.Chunks.CountAsync();
            ViewBag.TotalBronnen = await _context.Bronnen.CountAsync();
            ViewBag.TotalProjects = await _context.Projects.CountAsync();

            return View();
        }
    }
}