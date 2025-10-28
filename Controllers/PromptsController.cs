using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.Data;
using VectorRagDemo.Models;

namespace VectorRagDemo.Controllers
{
    public class PromptsController : Controller
    {
        private readonly VectorDbContext _context;

        public PromptsController(VectorDbContext context)
        {
            _context = context;
        }

        // GET: Prompts
        public async Task<IActionResult> Index()
        {
            var prompts = await _context.Prompts
                .Include(p => p.ProjectNavigation)
                .Include(p => p.PromptTypeNavigation)
                .Where(p => p.Status == 1)
                .OrderByDescending(p => p.GemaaktOp)
                .ToListAsync();

            return View(prompts);
        }

        // GET: Prompts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prompt = await _context.Prompts
                .Include(p => p.ProjectNavigation)
                .Include(p => p.PromptTypeNavigation)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (prompt == null)
            {
                return NotFound();
            }

            return View(prompt);
        }

        // GET: Prompts/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        // POST: Prompts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Project,PromptType,SystemInstruction,Content")] Prompt prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt.SystemInstruction) ||
                    string.IsNullOrWhiteSpace(prompt.Content))
                {
                    TempData["Error"] = "System Instruction and Content are required.";
                    await LoadDropdownsAsync();
                    return View(prompt);
                }

                prompt.GemaaktOp = DateTime.Now;
                prompt.Status = 1; // Active

                _context.Add(prompt);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Prompt created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating prompt: {ex.Message}";
                await LoadDropdownsAsync();
                return View(prompt);
            }
        }

        // GET: Prompts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
            {
                return NotFound();
            }

            await LoadDropdownsAsync(prompt.Project, prompt.PromptType);
            return View(prompt);
        }

        // POST: Prompts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Project,PromptType,SystemInstruction,Content,GemaaktOp,Status")] Prompt prompt)
        {
            if (id != prompt.ID)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(prompt.SystemInstruction) ||
                    string.IsNullOrWhiteSpace(prompt.Content))
                {
                    TempData["Error"] = "System Instruction and Content are required.";
                    await LoadDropdownsAsync(prompt.Project, prompt.PromptType);
                    return View(prompt);
                }

                prompt.GewijzigdOp = DateTime.Now;
                _context.Update(prompt);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Prompt updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PromptExists(prompt.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating prompt: {ex.Message}";
                await LoadDropdownsAsync(prompt.Project, prompt.PromptType);
                return View(prompt);
            }
        }

        // GET: Prompts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prompt = await _context.Prompts
                .Include(p => p.ProjectNavigation)
                .Include(p => p.PromptTypeNavigation)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (prompt == null)
            {
                return NotFound();
            }

            return View(prompt);
        }

        // POST: Prompts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var prompt = await _context.Prompts.FindAsync(id);
                if (prompt != null)
                {
                    // Soft delete by setting Status to 0
                    prompt.Status = 0;
                    prompt.GewijzigdOp = DateTime.Now;
                    _context.Update(prompt);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Prompt deleted successfully!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting prompt: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool PromptExists(int id)
        {
            return _context.Prompts.Any(e => e.ID == id);
        }

        private async Task LoadDropdownsAsync(int? selectedProjectId = null, int? selectedPromptTypeId = null)
        {
            var projects = await _context.Projects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();

            var promptTypes = await _context.PromptTypes
                .Where(pt => pt.Status == 1)
                .OrderBy(pt => pt.Omschrijving)
                .ToListAsync();

            ViewBag.Projects = new SelectList(projects, "ID", "Naam", selectedProjectId);
            ViewBag.PromptTypes = new SelectList(promptTypes, "ID", "Omschrijving", selectedPromptTypeId);
        }
    }
}
