namespace VectorRagDemo.Models.DataContracts
{
    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsResponse { get; set; }
        public string Context { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public LinkPreviewMetadata? LinkPreview { get; set; }
        public List<SourceLink> SourceLinks { get; set; } = new();

        /// <summary>wa.me/ deep-link — set when WhatsApp channel is active and bot signals escalation.</summary>
        public string? WhatsAppUrl { get; set; }
        public string? WhatsAppCtaText { get; set; }
        public string? WhatsAppButtonText { get; set; }

        /// <summary>mailto: deep-link — set when email channel is active and bot signals escalation.</summary>
        public string? EmailUrl { get; set; }
        public string? EmailCtaText { get; set; }
        public string? EmailButtonText { get; set; }

        public string Role => IsResponse ? "assistant" : "user";
    }
}
