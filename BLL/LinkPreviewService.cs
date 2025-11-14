using System.Text.RegularExpressions;
using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.BLL
{
    public class LinkPreviewService
    {
        private readonly HttpClient _httpClient;

        public LinkPreviewService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<LinkPreviewMetadata?> FetchLinkPreviewAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                // Set user agent to avoid bot blocking
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                var metadata = new LinkPreviewMetadata
                {
                    Url = url,
                    Title = ExtractMetaTag(html, "og:title") ?? ExtractTitleTag(html) ?? ExtractDomain(url),
                    Description = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description"),
                    ImageUrl = ExtractMetaTag(html, "og:image") ?? ExtractMetaTag(html, "twitter:image"),
                    SiteName = ExtractMetaTag(html, "og:site_name") ?? ExtractDomain(url),
                    FaviconUrl = ExtractFaviconUrl(html, uri)
                };

                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching link preview for {url}: {ex.Message}");
                return null;
            }
        }

        private string? ExtractMetaTag(string html, string property)
        {
            // Try Open Graph format: <meta property="og:title" content="...">
            var ogPattern = $@"<meta\s+property=['""](?:og:|twitter:)?{Regex.Escape(property.Replace("og:", "").Replace("twitter:", ""))}['""][^>]*content=['""]([^'""]+)['""]";
            var ogMatch = Regex.Match(html, ogPattern, RegexOptions.IgnoreCase);
            if (ogMatch.Success)
            {
                return DecodeHtml(ogMatch.Groups[1].Value);
            }

            // Try standard format: <meta name="description" content="...">
            var namePattern = $@"<meta\s+name=['""](?:og:|twitter:)?{Regex.Escape(property.Replace("og:", "").Replace("twitter:", ""))}['""][^>]*content=['""]([^'""]+)['""]";
            var nameMatch = Regex.Match(html, namePattern, RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                return DecodeHtml(nameMatch.Groups[1].Value);
            }

            // Try reversed attribute order
            var reversedPattern = $@"<meta\s+content=['""]([^'""]+)['""][^>]*(?:property|name)=['""](?:og:|twitter:)?{Regex.Escape(property.Replace("og:", "").Replace("twitter:", ""))}['""]";
            var reversedMatch = Regex.Match(html, reversedPattern, RegexOptions.IgnoreCase);
            if (reversedMatch.Success)
            {
                return DecodeHtml(reversedMatch.Groups[1].Value);
            }

            return null;
        }

        private string? ExtractTitleTag(string html)
        {
            var match = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
            return match.Success ? DecodeHtml(match.Groups[1].Value.Trim()) : null;
        }

        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return url;
            }
        }

        private string? ExtractFaviconUrl(string html, Uri baseUri)
        {
            // Try to find favicon link
            var patterns = new[]
            {
                @"<link[^>]*rel=['""](?:shortcut )?icon['""][^>]*href=['""]([^'""]+)['""]",
                @"<link[^>]*href=['""]([^'""]+)['""][^>]*rel=['""](?:shortcut )?icon['""]"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var faviconUrl = match.Groups[1].Value;
                    return MakeAbsoluteUrl(faviconUrl, baseUri);
                }
            }

            // Default to /favicon.ico
            return $"{baseUri.Scheme}://{baseUri.Host}/favicon.ico";
        }

        private string MakeAbsoluteUrl(string url, Uri baseUri)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return url;
            }

            if (url.StartsWith("//"))
            {
                return $"{baseUri.Scheme}:{url}";
            }

            if (url.StartsWith("/"))
            {
                return $"{baseUri.Scheme}://{baseUri.Host}{url}";
            }

            return $"{baseUri.Scheme}://{baseUri.Host}/{url}";
        }

        private string DecodeHtml(string text)
        {
            return System.Net.WebUtility.HtmlDecode(text);
        }
    }
}
