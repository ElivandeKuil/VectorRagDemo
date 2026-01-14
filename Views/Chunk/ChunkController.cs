using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VectorRagDemo.BLL;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Controllers
{
    public class ChunkController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly LogboekDbContext _logboekContext;
        private readonly IConfiguration _configuration;
        private readonly EmbeddingProcessor _embeddingProcessor;

        public ChunkController(VectorDbContext context, LogboekDbContext logboekContext, IConfiguration configuration)
        {
            _context = context;
            _logboekContext = logboekContext;
            _configuration = configuration;
            _embeddingProcessor = new EmbeddingProcessor(new HttpClient(), logboekContext);
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

        // GET: Chunk/Create
        public async Task<IActionResult> Create()
        {
            // Load available Bronnen for dropdown
            var bronnen = await _context.Bronnen
                .Include(b => b.ProjectNavigation)
                .Where(b => b.Status == 1)
                .OrderBy(b => b.Title)
                .ToListAsync();

            ViewBag.Bronnen = new SelectList(
                bronnen.Select(b => new
                {
                    b.ID,
                    DisplayText = $"{b.Title} ({b.ProjectNavigation?.Naam ?? "No Project"})"
                }),
                "ID",
                "DisplayText"
            );

            return View();
        }
    }
}