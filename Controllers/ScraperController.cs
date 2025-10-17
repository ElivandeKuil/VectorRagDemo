using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Data;
using VectorRagDemo.BLL;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace VectorRagDemo.Controllers
{
    public class ScraperController : Controller
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;

        public ScraperController(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Scraper/Index
        public async Task<IActionResult> Index()
        {
            // Load available Bronnen and Projects for selection
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

        // POST: Scraper/ScrapeProducts
        [HttpPost]
        public async Task<IActionResult> ScrapeProducts([FromBody] ScrapeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BaseUrl))
                {
                    return Json(new { success = false, error = "Base URL is required" });
                }

                // Create or get the Bron
                int bronId;
                if (request.CreateNewBron)
                {
                    if (string.IsNullOrWhiteSpace(request.BronTitle))
                    {
                        return Json(new { success = false, error = "Bron title is required when creating new Bron" });
                    }

                    var newBron = new Models.Bron
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

                // Initialize scraper
                var scraper = new WebScraperService();

                // Step 1: Get all product URLs
                var productUrls = new List<string>();
                string discoveryMethod = "";

                if (request.ScrapeMode == "single")
                {
                    // Single URL mode
                    productUrls.Add(request.BaseUrl);
                    discoveryMethod = "single URL";
                }
                else if (request.ScrapeMode == "sitemap")
                {
                    // Sitemap mode
                    var sitemapParser = new SitemapParser();
                    SitemapResult sitemapResult;

                    if (!string.IsNullOrWhiteSpace(request.SitemapUrl))
                    {
                        // Use provided sitemap URL
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
                        // Auto-discover sitemap
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
                    // Multi-page mode (HTML scraping)
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

                // Step 2: Configure selectors
                var selectors = new ScrapingSelectors
                {
                    TitleSelector = request.TitleSelector,
                    DescriptionSelector = request.DescriptionSelector,
                    PriceSelector = request.PriceSelector,
                    SkuSelector = request.SkuSelector,
                    ImageSelector = request.ImageSelector,
                    SpecsSelector = request.SpecsSelector
                };

                // Step 3: Scrape each product and create chunks
                var results = new List<string>();
                var errors = new List<string>();
                int successCount = 0;
                int errorCount = 0;

                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                foreach (var url in productUrls.Take(request.MaxProducts))
                {
                    try
                    {
                        // Scrape product data
                        var product = await scraper.ScrapeProduct(url, selectors);

                        // Generate chunk text
                        var chunkText = scraper.GenerateProductChunk(product);

                        if (string.IsNullOrWhiteSpace(chunkText))
                        {
                            errors.Add($"Empty content for {url}");
                            errorCount++;
                            continue;
                        }

                        // Generate embedding
                        var embedding = await EmbeddingProcessor.GenerateQueryEmbeddingAsync(chunkText);

                        if (embedding == null || !embedding.Any())
                        {
                            errors.Add($"Failed to generate embedding for {url}");
                            errorCount++;
                            continue;
                        }

                        // Insert chunk with vector
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

                        results.Add($"Created chunk {newId} for {product.Title}");
                        successCount++;

                        // Add small delay to be respectful to the server
                        await Task.Delay(500);
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
}
