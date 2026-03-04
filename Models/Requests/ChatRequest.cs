using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.Models.Requests
{
    public class ChatRequest
    {
        public string Query { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
        public List<RetrievedChunk>? RetrievedChunks { get; set; }
        public int ProjectId { get; set; }
        public int ConversationId { get; set; }
    }
}
