using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Controllers
{
    [Authorize]
    public class PromptInstellingController : Controller
    {
        private readonly VectorDbContext _context;

        public PromptInstellingController(VectorDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int projectId = 0)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return RedirectToAction("Index", "Dashboard");

            if (project.GebruikPromptTabel && !User.IsInRole("Admin"))
                return RedirectToAction("Index", "Dashboard");

            var instelling = _context.PromptInstellingen
                .FirstOrDefault(p => p.ProjectID == project.ID)
                ?? new PromptInstelling { ProjectID = project.ID };

            ViewBag.ProjectID = project.ID;
            ViewBag.ProjectNaam = project.Naam;

            return View(instelling);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int projectId, PromptInstelling model)
        {
            var project = await ResolveProjectAsync(projectId);
            if (project == null) return RedirectToAction("Index", "Dashboard");

            var existing = _context.PromptInstellingen
                .FirstOrDefault(p => p.ProjectID == project.ID);

            if (existing == null)
            {
                _context.PromptInstellingen.Add(new PromptInstelling
                {
                    ProjectID = project.ID,
                    Persona = NullIfEmpty(model.Persona),
                    Instructies = NullIfEmpty(model.Instructies),
                    Regels = NullIfEmpty(model.Regels),
                    Voorbeelden = NullIfEmpty(model.Voorbeelden),
                    GemaaktOp = DateTime.Now
                });
            }
            else
            {
                existing.Persona = NullIfEmpty(model.Persona);
                existing.Instructies = NullIfEmpty(model.Instructies);
                existing.Regels = NullIfEmpty(model.Regels);
                existing.Voorbeelden = NullIfEmpty(model.Voorbeelden);
                existing.GewijzigdOp = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Prompt instellingen opgeslagen.";
            return RedirectToAction(nameof(Edit), new { projectId = project.ID });
        }

        /// <summary>
        /// Admins pass an explicit projectId. Regular users get their assigned project automatically.
        /// </summary>
        private async Task<Project?> ResolveProjectAsync(int projectId)
        {
            if (User.IsInRole("Admin"))
            {
                if (projectId <= 0) return null;
                return await _context.Projects.FindAsync(projectId);
            }

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry == null) return null;
            return await _context.Projects.FindAsync(entry.Project);
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
