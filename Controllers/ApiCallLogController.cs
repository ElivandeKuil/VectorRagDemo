using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.Controllers
{
    public class ApiCallLogController : Controller
    {
        private readonly LogboekDbContext _logboekContext;

        public ApiCallLogController(LogboekDbContext logboekContext)
        {
            _logboekContext = logboekContext;
        }

        public async Task<IActionResult> Index(ApiCallLogViewModel model)
        {
            // Set default date range if not provided (last 7 days)
            if (!model.StartDate.HasValue)
            {
                model.StartDate = DateTime.Now.AddDays(-7).Date;
            }
            if (!model.EndDate.HasValue)
            {
                model.EndDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            }

            // Start with base query
            var query = _logboekContext.ApiCallLogs.AsQueryable();

            // Apply filters
            if (model.StartDate.HasValue)
            {
                query = query.Where(log => log.GemaaktOp >= model.StartDate.Value);
            }

            if (model.EndDate.HasValue)
            {
                query = query.Where(log => log.GemaaktOp <= model.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.RequestUriFilter))
            {
                query = query.Where(log => log.RequestUri.Contains(model.RequestUriFilter));
            }

            if (model.StatusCodeFilter.HasValue)
            {
                query = query.Where(log => log.ResponseStatus == model.StatusCodeFilter.Value);
            }

            if (model.HasError.HasValue && model.HasError.Value)
            {
                query = query.Where(log => log.ErrorMessage != null && log.ErrorMessage != "");
            }

            if (!string.IsNullOrWhiteSpace(model.RequestMethodFilter))
            {
                query = query.Where(log => log.RequestMethod == model.RequestMethodFilter);
            }

            // Get total count for pagination
            model.TotalRecords = await query.CountAsync();

            // Apply sorting
            query = model.SortBy switch
            {
                "ResponseStatus" => model.SortOrder == "asc"
                    ? query.OrderBy(log => log.ResponseStatus)
                    : query.OrderByDescending(log => log.ResponseStatus),
                "DurationMs" => model.SortOrder == "asc"
                    ? query.OrderBy(log => log.DurationMs)
                    : query.OrderByDescending(log => log.DurationMs),
                "RequestMethod" => model.SortOrder == "asc"
                    ? query.OrderBy(log => log.RequestMethod)
                    : query.OrderByDescending(log => log.RequestMethod),
                _ => model.SortOrder == "asc"
                    ? query.OrderBy(log => log.GemaaktOp)
                    : query.OrderByDescending(log => log.GemaaktOp)
            };

            // Apply pagination
            var logs = await query
                .Skip((model.CurrentPage - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            model.Logs = logs;

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _logboekContext.ApiCallLogs
                .FirstOrDefaultAsync(l => l.ID == id);

            if (log == null)
            {
                return NotFound();
            }

            return View(log);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOlderThan(int days)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var logsToDelete = await _logboekContext.ApiCallLogs
                .Where(log => log.GemaaktOp < cutoffDate)
                .ToListAsync();

            _logboekContext.ApiCallLogs.RemoveRange(logsToDelete);
            await _logboekContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Deleted {logsToDelete.Count} logs older than {days} days.";
            return RedirectToAction(nameof(Index));
        }
    }
}
