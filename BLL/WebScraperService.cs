using HtmlAgilityPack;
using System.Text;
using VectorRagDemo.Models;

namespace VectorRagDemo.BLL
{
    public class WebScraperService
    {
        private readonly HttpClient _httpClient;

        public WebScraperService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Scrapes all product URLs from a webshop
        /// </summary>
        public async Task<List<string>> GetProductUrls(string baseUrl, string productLinkSelector)
        {
            var productUrls = new List<string>();

            try
            {
                var html = await _httpClient.GetStringAsync(baseUrl);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Find all product links using the provided selector
                var productLinks = htmlDoc.DocumentNode.SelectNodes(productLinkSelector);

                if (productLinks != null)
                {
                    foreach (var link in productLinks)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href))
                        {
                            // Convert relative URLs to absolute
                            if (!href.StartsWith("http"))
                            {
                                var uri = new Uri(new Uri(baseUrl), href);
                                href = uri.ToString();
                            }
                            productUrls.Add(href);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error scraping product URLs: {ex.Message}", ex);
            }

            return productUrls.Distinct().ToList();
        }

        /// <summary>
        /// Scrapes product details from a single product page
        /// </summary>
        public async Task<ProductData> ScrapeProduct(string url, ScrapingSelectors selectors)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var product = new ProductData
                {
                    Url = url,
                    Title = GetTextContent(htmlDoc, selectors.TitleSelector),
                    Description = GetTextContent(htmlDoc, selectors.DescriptionSelector),
                    Price = GetTextContent(htmlDoc, selectors.PriceSelector),
                    SKU = GetTextContent(htmlDoc, selectors.SkuSelector),
                    ImageUrl = GetAttributeValue(htmlDoc, selectors.ImageSelector, "src")
                };

                // Extract additional specs if selector is provided
                if (!string.IsNullOrEmpty(selectors.SpecsSelector))
                {
                    var specsNodes = htmlDoc.DocumentNode.SelectNodes(selectors.SpecsSelector);
                    if (specsNodes != null)
                    {
                        var specs = new StringBuilder();
                        foreach (var node in specsNodes)
                        {
                            specs.AppendLine(node.InnerText.Trim());
                        }
                        product.Specifications = specs.ToString();
                    }
                }

                return product;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error scraping product from {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Scrapes product data dynamically based on ScrapingElement configuration
        /// </summary>
        public async Task<Dictionary<string, string>> ScrapeProductDynamic(string url, List<ScrapingElement> scrapingElements)
        {
            var result = new Dictionary<string, string>();

            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                foreach (var element in scrapingElements.OrderBy(e => e.SortOrder))
                {
                    string value = string.Empty;

                    if (!string.IsNullOrEmpty(element.Selector))
                    {
                        // Check if we need to extract an attribute or text content
                        if (!string.IsNullOrEmpty(element.AttributeName) && element.AttributeName.ToLower() != "text")
                        {
                            value = GetAttributeValue(htmlDoc, element.Selector, element.AttributeName);
                        }
                        else
                        {
                            value = GetTextContent(htmlDoc, element.Selector);
                        }
                    }

                    // Use default value if extraction failed and default is provided
                    if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(element.DefaultValue))
                    {
                        value = element.DefaultValue;
                    }

                    // Store the result with ElementName as key
                    result[element.ElementName] = value;
                }

                // Always add the URL
                result["URL"] = url;

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error scraping product from {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a formatted chunk text from dynamic scraping results
        /// </summary>
        public string GenerateProductChunkFromDynamic(Dictionary<string, string> data)
        {
            var sb = new StringBuilder();

            foreach (var kvp in data.Where(d => d.Key != "URL"))
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
            }

            // Add URL at the end if available
            if (data.ContainsKey("URL"))
            {
                sb.AppendLine();
                sb.AppendLine($"URL: {data["URL"]}");
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Generates a formatted chunk text from product data
        /// </summary>
        public string GenerateProductChunk(ProductData product)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Product: {product.Title}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(product.Description))
            {
                sb.AppendLine($"Description: {product.Description}");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(product.Price))
            {
                sb.AppendLine($"Price: {product.Price}");
            }

            if (!string.IsNullOrEmpty(product.SKU))
            {
                sb.AppendLine($"SKU: {product.SKU}");
            }

            if (!string.IsNullOrEmpty(product.Specifications))
            {
                sb.AppendLine();
                sb.AppendLine("Specifications:");
                sb.AppendLine(product.Specifications);
            }

            if (!string.IsNullOrEmpty(product.Url))
            {
                sb.AppendLine();
                sb.AppendLine($"URL: {product.Url}");
            }

            return sb.ToString().Trim();
        }

        private string GetTextContent(HtmlDocument doc, string? selector)
        {
            if (string.IsNullOrEmpty(selector))
                return string.Empty;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.InnerText.Trim() ?? string.Empty;
        }

        private string GetAttributeValue(HtmlDocument doc, string? selector, string attribute)
        {
            if (string.IsNullOrEmpty(selector))
                return string.Empty;

            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.GetAttributeValue(attribute, string.Empty) ?? string.Empty;
        }
    }

    public class ProductData
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Specifications { get; set; } = string.Empty;
    }

    public class ScrapingSelectors
    {
        public string? TitleSelector { get; set; }
        public string? DescriptionSelector { get; set; }
        public string? PriceSelector { get; set; }
        public string? SkuSelector { get; set; }
        public string? ImageSelector { get; set; }
        public string? SpecsSelector { get; set; }
    }
}
