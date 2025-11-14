namespace VectorRagDemo.Models.DataContracts
{
    public class LinkPreviewMetadata
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? SiteName { get; set; }
        public string? FaviconUrl { get; set; }
    }
}
