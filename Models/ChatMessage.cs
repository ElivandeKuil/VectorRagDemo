namespace VectorRagDemo.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsResponse { get; set; }
        public string Context { get; set; }
        public string Role => IsResponse ? "assistant" : "user";
    }
}
