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
        public string Role => IsResponse ? "assistant" : "user";
    }
}
