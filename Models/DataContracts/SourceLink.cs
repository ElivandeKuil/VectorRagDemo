namespace VectorRagDemo.Models.DataContracts
{
    public class SourceLink
    {
        public string Url { get; set; } = string.Empty;

        /// <summary>Fallback label (bron title) — only used when Preview fetch fails.</summary>
        public string FallbackTitle { get; set; } = string.Empty;

        /// <summary>Populated from LinkPreviewService; null when fetch failed or URL is unreachable.</summary>
        public LinkPreviewMetadata? Preview { get; set; }
    }
}
