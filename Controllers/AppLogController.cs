using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AppLogController : Controller
    {
        private readonly LogboekDbContext _context;

        public AppLogController(LogboekDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? levelFilter,
            string? sourceFilter,
            string? searchText,
            DateTime? startDate,
            DateTime? endDate,
            int currentPage = 1,
            int pageSize = 50,
            string sortBy = "GemaaktOp",
            string sortOrder = "desc")
        {
            var query = _context.AppLogs.AsQueryable();

            if (!string.IsNullOrEmpty(levelFilter))
                query = query.Where(l => l.Level == levelFilter);

            if (!string.IsNullOrEmpty(sourceFilter))
                query = query.Where(l => l.Source.Contains(sourceFilter));

            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(l => l.Message.Contains(searchText) || (l.Detail != null && l.Detail.Contains(searchText)));

            if (startDate.HasValue)
                query = query.Where(l => l.GemaaktOp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.GemaaktOp <= endDate.Value);

            var totalRecords = await query.CountAsync();

            query = (sortBy, sortOrder) switch
            {
                ("Level", "asc")     => query.OrderBy(l => l.Level),
                ("Level", _)         => query.OrderByDescending(l => l.Level),
                ("Source", "asc")    => query.OrderBy(l => l.Source),
                ("Source", _)        => query.OrderByDescending(l => l.Source),
                ("GemaaktOp", "asc") => query.OrderBy(l => l.GemaaktOp),
                _                    => query.OrderByDescending(l => l.GemaaktOp),
            };

            var logs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AppLogViewModel
            {
                Logs = logs,
                LevelFilter = levelFilter,
                SourceFilter = sourceFilter,
                SearchText = searchText,
                StartDate = startDate,
                EndDate = endDate,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOlderThan(int days)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            var old = await _context.AppLogs.Where(l => l.GemaaktOp < cutoff).ToListAsync();
            _context.AppLogs.RemoveRange(old);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{old.Count} logregels ouder dan {days} dagen verwijderd.";
            return RedirectToAction(nameof(Index));
        }
    }
}
