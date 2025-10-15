using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace VectorRagDemo.Controllers
{
    public class ChunkController : Controller
    {
        private readonly VectorDbContext _context;

        public ChunkController(VectorDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var chunks = await _context.Chunks
                .Include(c => c.Bron)
                    .ThenInclude(b => b.ProjectNavigation)
                .Where(c => c.Status == 1)
                .OrderByDescending(c => c.GemaaktOp)
                .ToListAsync();

            return View(chunks);
        }
    }
}