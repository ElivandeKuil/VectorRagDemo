namespace VectorRagDemo.Models
{
    public class ChatRequest
    {
        public string Query { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
    }
}
