using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReembedController : Controller
    {
        private readonly LogboekDbContext _logboekContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory? _httpClientFactory;

        public ReembedController(
            LogboekDbContext logboekContext,
            IConfiguration configuration,
            IHttpClientFactory? httpClientFactory = null)
        {
            _logboekContext = logboekContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Shows the re-embedding progress page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? projectId = null)
        {
            var progress = await GetService().GetProgressAsync(projectId);
            ViewBag.ProjectId = projectId;
            ViewBag.WithEmbedding = progress.WithEmbedding;
            ViewBag.Total = progress.Total;
            ViewBag.Done = progress.WithEmbedding >= progress.Total;
            return View();
        }

        /// <summary>
        /// Processes one batch and returns JSON progress — call repeatedly until done is true.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessBatch(int batchSize = 50, int? projectId = null)
        {
            try
            {
                var progress = await GetService().ProcessBatchAsync(batchSize, projectId);
                return Json(new
                {
                    processed = progress.WithEmbedding,
                    total = progress.Total,
                    done = progress.WithEmbedding >= progress.Total
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns current progress as JSON — for polling.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Progress(int? projectId = null)
        {
            var progress = await GetService().GetProgressAsync(projectId);
            return Json(new
            {
                processed = progress.WithEmbedding,
                total = progress.Total,
                done = progress.WithEmbedding >= progress.Total
            });
        }

        private ReembedService GetService()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured.");
            var httpClient = _httpClientFactory?.CreateClient() ?? new HttpClient();
            return new ReembedService(httpClient, _logboekContext, connectionString);
        }
    }
}
