using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.DAL
{
    public class GeminiApiClient
    {
        private readonly VertexApiClient _vertexClient;

        public GeminiApiClient(HttpClient client)
        {
            _vertexClient = new VertexApiClient(client);
        }

        public async Task<HttpResponseMessage> SendRequestAsync(StringContent content, PromptTypeEnum type)
        {
            return await _vertexClient.SendGeminiRequestAsync(content, type);
        }
    }
}