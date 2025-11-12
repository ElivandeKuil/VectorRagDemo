using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.BLL;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Controllers
{
    public class ScraperController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly LogboekDbContext _logboekContext;
        private readonly IConfiguration _configuration;
        private readonly EmbeddingProcessor _embeddingProcessor;

        public ScraperController(VectorDbContext context, LogboekDbContext logboekContext, IConfiguration configuration)
        {
            _context = context;
            _logboekContext = logboekContext;
            _configuration = configuration;
            _embeddingProcessor = new EmbeddingProcessor(new HttpClient(), _context, logboekContext);
        }

        public async Task<IActionResult> Index()
        {
            var bronnen = await _context.Bronnen
                .Include(b => b.ProjectNavigation)
                .Where(b => b.Status == 1)
                .OrderBy(b => b.Title)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();

            ViewBag.Bronnen = bronnen;
            ViewBag.Projects = projects;

            return View();
        }

        public async Task<IActionResult> ManageElements()
        {
            var projects = await _context.Projects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Naam)
                .ToListAsync();

            ViewBag.Projects = projects;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetScrapingElements(int projectId)
        {
            var elements = await _context.ScrapingElement
                .Where(e => e.Project == projectId && e.Status == 1)
                .OrderBy(e => e.SortOrder)
                .Select(e => new
                {
                    id = e.ID,
                    project = e.Project,
                    elementName = e.ElementName,
                    selector = e.Selector,
                    attributeName = e.AttributeName,
                    isRequired = e.IsRequired,
                    defaultValue = e.DefaultValue,
                    sortOrder = e.SortOrder
                })
                .ToListAsync();

            return Json(elements);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectDetails(int projectId)
        {
            var project = await _context.Projects
                .Where(p => p.ID == projectId && p.Status == 1)
                .Select(p => new
                {
                    id = p.ID,
                    naam = p.Naam,
                    baseUrl = p.BaseUrl,
                    productLinkSelector = p.ProductLinkSelector
                })
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return Json(new { success = false, error = "Project not found" });
            }

            return Json(new { success = true, project = project });
        }

        [HttpPost]
        public async Task<IActionResult> CreateScrapingElement([FromBody] ScrapingElementRequest request)
        {
            try
            {
                var element = new ScrapingElement
                {
                    Project = request.ProjectId,
                    ElementName = request.ElementName,
                    Selector = request.Selector,
                    AttributeName = request.AttributeName ?? "text",
                    IsRequired = request.IsRequired,
                    DefaultValue = request.DefaultValue ?? string.Empty,
                    SortOrder = request.SortOrder,
                    GemaaktOp = DateTime.Now,
                    Status = 1
                };

                _context.ScrapingElement.Add(element);
                await _context.SaveChangesAsync();

                return Json(new { success = true, id = element.ID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateScrapingElement([FromBody] ScrapingElementRequest request)
        {
            try
            {
                var element = await _context.ScrapingElement.FindAsync(request.Id);
                if (element == null)
                {
                    return Json(new { success = false, error = "Element not found" });
                }

                element.ElementName = request.ElementName;
                element.Selector = request.Selector;
                element.AttributeName = request.AttributeName ?? "text";
                element.IsRequired = request.IsRequired;
                element.DefaultValue = request.DefaultValue ?? string.Empty;
                element.SortOrder = request.SortOrder;
                element.GemaaktOp = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteScrapingElement([FromBody] int id)
        {
            try
            {
                var element = await _context.ScrapingElement.FindAsync(id);
                if (element == null)
                {
                    return Json(new { success = false, error = "Element not found" });
                }

                element.Status = 2;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ScrapeProducts([FromBody] ScrapeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BaseUrl))
                {
                    return Json(new { success = false, error = "Base URL is required" });
                }

                int bronId;
                if (request.CreateNewBron)
                {
                    if (string.IsNullOrWhiteSpace(request.BronTitle))
                    {
                        return Json(new { success = false, error = "Bron title is required when creating new Bron" });
                    }

                    var newBron = new Bron
                    {
                        Title = request.BronTitle,
                        Project = request.ProjectId,
                        GemaaktOp = DateTime.Now,
                        Status = 1
                    };

                    _context.Bronnen.Add(newBron);
                    await _context.SaveChangesAsync();
                    bronId = newBron.ID;
                }
                else
                {
                    bronId = request.BronId;
                }

                var scraper = new WebScraperService();

                var productUrls = new List<string>();
                string discoveryMethod = "";

                if (request.ScrapeMode == "single")
                {
                    productUrls.Add(request.BaseUrl);
                    discoveryMethod = "single URL";
                }
                else if (request.ScrapeMode == "sitemap")
                {
                    var sitemapParser = new SitemapParser();
                    SitemapResult sitemapResult;

                    if (!string.IsNullOrWhiteSpace(request.SitemapUrl))
                    {
                        try
                        {
                            var urls = await sitemapParser.ParseSitemap(request.SitemapUrl, request.UrlFilterPattern);
                            sitemapResult = new SitemapResult
                            {
                                Success = true,
                                SitemapUrl = request.SitemapUrl,
                                Urls = urls,
                                Method = "provided URL"
                            };
                        }
                        catch (Exception ex)
                        {
                            return Json(new { success = false, error = $"Failed to parse sitemap: {ex.Message}" });
                        }
                    }
                    else
                    {
                        sitemapResult = await sitemapParser.AutoDiscoverAndParse(request.BaseUrl, request.UrlFilterPattern);
                    }

                    if (!sitemapResult.Success)
                    {
                        return Json(new { success = false, error = sitemapResult.ErrorMessage });
                    }

                    productUrls = sitemapResult.Urls;
                    discoveryMethod = $"sitemap ({sitemapResult.Method}) - {sitemapResult.SitemapUrl}";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.ProductLinkSelector))
                    {
                        return Json(new { success = false, error = "Product link selector is required for multi-page scraping" });
                    }

                    productUrls = await scraper.GetProductUrls(request.BaseUrl, request.ProductLinkSelector);
                    discoveryMethod = "HTML scraping";
                }

                if (!productUrls.Any())
                {
                    return Json(new { success = false, error = "No product URLs found" });
                }

                var urlBlacklist = _context.ScrapingUrlBlackList
                    .Where(e => e.Project == request.ProjectId && e.Status == 1)
                    .ToList();

                foreach (var element in urlBlacklist)
                {
                    productUrls = productUrls.Where(o => !o.Contains(element.BlackListElement)).ToList();
                }

                var scrapingElements = await _context.ScrapingElement
                    .Where(e => e.Project == request.ProjectId && e.Status == 1)
                    .OrderBy(e => e.SortOrder)
                    .ToListAsync();

                if (!scrapingElements.Any())
                {
                    return Json(new { success = false, error = "No scraping elements configured for this project. Please configure scraping elements in the ScrapingElement table." });
                }

                var results = new List<string>();
                var errors = new List<string>();
                int successCount = 0;
                int errorCount = 0;

                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                for (int i = 0; i < request.MaxProducts; i++)
                {
                    var url = productUrls[i];
                    try
                    {
                        Dictionary<string, string> productData = await scraper.ScrapeProductDynamic(url, scrapingElements);

                        if (productData.Where(o => o.Key != "URL" && !string.IsNullOrEmpty(o.Value)).Any())
                        {
                            var chunkText = scraper.GenerateProductChunkFromDynamic(productData);

                            var productTitle = productData.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? url;

                            if (string.IsNullOrWhiteSpace(chunkText))
                            {
                                errors.Add($"Empty content for {url}");
                                errorCount++;
                                continue;
                            }

                            var embedding = (await _embeddingProcessor.GenerateQueryEmbeddingAsync(chunkText, new List<Models.DataContracts.ChatMessage>())).Embedding;

                            if (embedding == null || !embedding.Any())
                            {
                                errors.Add($"Failed to generate embedding for {url}");
                                errorCount++;
                                continue;
                            }

                            var vectorString = "[" + string.Join(",", embedding) + "]";

                            using var connection = new SqlConnection(connectionString);
                            await connection.OpenAsync();

                            var sql = @"
                            INSERT INTO Chunk (BronID, Tekst, TekstVector, GemaaktOp, Status)
                            VALUES (@BronID, @Tekst, CAST(@TekstVector AS VECTOR(768)), GETDATE(), @Status);
                            SELECT CAST(SCOPE_IDENTITY() as int);";

                            using var command = new SqlCommand(sql, connection);
                            command.Parameters.AddWithValue("@BronID", bronId);
                            command.Parameters.AddWithValue("@Tekst", chunkText);
                            command.Parameters.AddWithValue("@TekstVector", vectorString);
                            command.Parameters.AddWithValue("@Status", 1);

                            var newId = (int)await command.ExecuteScalarAsync();

                            results.Add($"Created chunk {newId} for {productTitle}");
                            successCount++;

                            await Task.Delay(500);
                        }
                        else
                        {
                            request.MaxProducts++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing {url}: {ex.Message}");
                        errorCount++;
                    }
                }

                return Json(new
                {
                    success = true,
                    totalProducts = productUrls.Count,
                    processedCount = successCount + errorCount,
                    successCount = successCount,
                    errorCount = errorCount,
                    results = results,
                    errors = errors,
                    bronId = bronId,
                    discoveryMethod = discoveryMethod
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }

    public class ScrapeRequest
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ScrapeMode { get; set; } = "single"; // "single", "multi", or "sitemap"
        public string ProductLinkSelector { get; set; } = string.Empty;
        public string SitemapUrl { get; set; } = string.Empty;
        public string UrlFilterPattern { get; set; } = string.Empty;
        public string TitleSelector { get; set; } = string.Empty;
        public string DescriptionSelector { get; set; } = string.Empty;
        public string PriceSelector { get; set; } = string.Empty;
        public string SkuSelector { get; set; } = string.Empty;
        public string ImageSelector { get; set; } = string.Empty;
        public string SpecsSelector { get; set; } = string.Empty;
        public int MaxProducts { get; set; } = 50;
        public bool CreateNewBron { get; set; }
        public string BronTitle { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int BronId { get; set; }
    }

    public class ScrapingElementRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ElementName { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string? AttributeName { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public int SortOrder { get; set; }
    }
}
