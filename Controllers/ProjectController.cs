using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectController : Controller
    {
        private readonly VectorDbContext _context;

        public ProjectController(VectorDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .OrderBy(p => p.Naam)
                .ToListAsync();

            return View(projects);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Project { BotName = "Assistant" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            if (string.IsNullOrWhiteSpace(model.Naam))
                ModelState.AddModelError(nameof(model.Naam), "Naam is verplicht.");

            if (!ModelState.IsValid)
                return View(model);

            var project = new Project
            {
                Naam = model.Naam.Trim(),
                BotName = string.IsNullOrWhiteSpace(model.BotName) ? "Assistant" : model.BotName.Trim(),
                GemaaktOp = DateTime.Now,
                Status = 1
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is aangemaakt.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model)
        {
            if (string.IsNullOrWhiteSpace(model.Naam))
                ModelState.AddModelError(nameof(model.Naam), "Naam is verplicht.");

            if (!ModelState.IsValid)
            {
                model.ID = id;
                return View(model);
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.Naam = model.Naam.Trim();
            project.BotName = string.IsNullOrWhiteSpace(model.BotName) ? "Assistant" : model.BotName.Trim();
            project.GewijzigdOp = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is bijgewerkt.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            project.Status = project.Status == 1 ? 0 : 1;
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Project '{project.Naam}' is {(project.Status == 1 ? "geactiveerd" : "gedeactiveerd")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
