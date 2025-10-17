using System.Xml.Linq;

namespace VectorRagDemo.BLL
{
    public class SitemapParser
    {
        private readonly HttpClient _httpClient;

        public SitemapParser()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Parses a sitemap.xml file and extracts all URLs
        /// </summary>
        public async Task<List<string>> ParseSitemap(string sitemapUrl, string? filterPattern = null)
        {
            var urls = new List<string>();

            try
            {
                var xmlContent = await _httpClient.GetStringAsync(sitemapUrl);
                var doc = XDocument.Parse(xmlContent);

                // Get the namespace (sitemaps use xmlns)
                XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                // Check if this is a sitemap index (contains other sitemaps)
                var sitemapElements = doc.Descendants(ns + "sitemap").ToList();

                if (sitemapElements.Any())
                {
                    // This is a sitemap index, recursively parse all sitemaps
                    foreach (var sitemapElement in sitemapElements)
                    {
                        var locElement = sitemapElement.Element(ns + "loc");
                        if (locElement != null)
                        {
                            var childSitemapUrl = locElement.Value;

                            // Apply filter to child sitemap URL if needed
                            if (string.IsNullOrEmpty(filterPattern) || childSitemapUrl.Contains(filterPattern, StringComparison.OrdinalIgnoreCase))
                            {
                                var childUrls = await ParseSitemap(childSitemapUrl, filterPattern);
                                urls.AddRange(childUrls);
                            }
                        }
                    }
                }
                else
                {
                    // This is a regular sitemap with URLs
                    var urlElements = doc.Descendants(ns + "url");

                    foreach (var urlElement in urlElements)
                    {
                        var locElement = urlElement.Element(ns + "loc");
                        if (locElement != null)
                        {
                            var url = locElement.Value;

                            // Apply filter if provided
                            if (string.IsNullOrEmpty(filterPattern) || url.Contains(filterPattern, StringComparison.OrdinalIgnoreCase))
                            {
                                urls.Add(url);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing sitemap from {sitemapUrl}: {ex.Message}", ex);
            }

            return urls;
        }

        /// <summary>
        /// Attempts to find the sitemap URL from robots.txt
        /// </summary>
        public async Task<string?> FindSitemapFromRobots(string baseUrl)
        {
            try
            {
                var robotsUrl = new Uri(new Uri(baseUrl), "/robots.txt").ToString();
                var robotsContent = await _httpClient.GetStringAsync(robotsUrl);

                // Look for "Sitemap: " entries in robots.txt
                var lines = robotsContent.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                    {
                        var sitemapUrl = trimmedLine.Substring("Sitemap:".Length).Trim();
                        return sitemapUrl;
                    }
                }
            }
            catch
            {
                // robots.txt not found or not accessible
            }

            return null;
        }

        /// <summary>
        /// Gets common sitemap URLs to try
        /// </summary>
        public List<string> GetCommonSitemapUrls(string baseUrl)
        {
            var uri = new Uri(baseUrl);
            var baseUri = $"{uri.Scheme}://{uri.Host}";

            return new List<string>
            {
                $"{baseUri}/sitemap.xml",
                $"{baseUri}/sitemap_index.xml",
                $"{baseUri}/sitemap-index.xml",
                $"{baseUri}/product-sitemap.xml",
                $"{baseUri}/products-sitemap.xml",
                $"{baseUri}/sitemap/sitemap.xml",
                $"{baseUri}/sitemaps/sitemap.xml"
            };
        }

        /// <summary>
        /// Tries to find and parse sitemap automatically
        /// </summary>
        public async Task<SitemapResult> AutoDiscoverAndParse(string baseUrl, string? filterPattern = null)
        {
            var result = new SitemapResult();

            // First, try to find sitemap from robots.txt
            var sitemapFromRobots = await FindSitemapFromRobots(baseUrl);
            if (sitemapFromRobots != null)
            {
                try
                {
                    result.SitemapUrl = sitemapFromRobots;
                    result.Urls = await ParseSitemap(sitemapFromRobots, filterPattern);
                    result.Success = true;
                    result.Method = "robots.txt";
                    return result;
                }
                catch
                {
                    // Continue to try common URLs
                }
            }

            // Try common sitemap URLs
            var commonUrls = GetCommonSitemapUrls(baseUrl);
            foreach (var url in commonUrls)
            {
                try
                {
                    result.SitemapUrl = url;
                    result.Urls = await ParseSitemap(url, filterPattern);
                    result.Success = true;
                    result.Method = "common URL pattern";
                    return result;
                }
                catch
                {
                    // Continue to next URL
                }
            }

            result.Success = false;
            result.ErrorMessage = "Could not find or parse sitemap. Tried robots.txt and common sitemap URLs.";
            return result;
        }
    }

    public class SitemapResult
    {
        public bool Success { get; set; }
        public string SitemapUrl { get; set; } = string.Empty;
        public List<string> Urls { get; set; } = new List<string>();
        public string Method { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
