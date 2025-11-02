using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;

namespace VectorRagDemo.Models.Responses
{
    public class ChatResponse
    {
        public GenerativeModelResponse GenerativeResponse { get; set; }

        public List<RetrievedChunk> Chunks { get; set; }
    }
}
