using System.Net.Http.Headers;
using VectorRagDemo.BLL;
using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.DAL
{
    public class VertexApiClient
    {
        private readonly HttpClient _client;

        public VertexApiClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<HttpResponseMessage> SendEmbeddingRequestAsync(StringContent content)
        {
            string projectId = ConnectionProcessor.GetProjectId();
            string endpoint = BuildEmbeddingEndpointUrl(projectId);

            await SetAuthorizationHeader();

            return await _client.PostAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> SendGeminiRequestAsync(StringContent content, string model)
        {
            string projectId = ConnectionProcessor.GetProjectId();
            string endpoint = BuildGeminiEndpointUrl(projectId, model);

            await SetAuthorizationHeader();

            return await _client.PostAsync(endpoint, content);
        }

        private async Task SetAuthorizationHeader()
        {
            string accessToken = await ConnectionProcessor.GetAuthenticationToken();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        private string BuildEmbeddingEndpointUrl(string projectId)
        {
            return $"https://{Config.Location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{Config.Location}/publishers/google/models/{Config.EmbeddingModel}:predict";
        }

        private string BuildGeminiEndpointUrl(string projectId, string model)
        {
            return $"https://{GeminiLocationEnum.Netherlands}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{GeminiLocationEnum.Netherlands}/publishers/google/models/{model}:generateContent";
        }
    }
}