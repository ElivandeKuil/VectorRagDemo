namespace VectorRagDemo.DAL
{
    public class GeminiApiClient
    {
        private readonly VertexApiClient _vertexClient;

        public GeminiApiClient(HttpClient client)
        {
            _vertexClient = new VertexApiClient(client);
        }

        public async Task<HttpResponseMessage> SendRequestAsync(StringContent content, string model)
        {
            return await _vertexClient.SendGeminiRequestAsync(content, model);
        }
    }
}