using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using VectorRagDemo.DAL;
using VectorRagDemo.Services;

namespace VectorRagDemo.BLL.Processors
{
    public class EmbeddingProcessor
    {
        private readonly ApiClient _apiClient;

        public EmbeddingProcessor(HttpClient client, LogboekDbContext logboekContext)
        {
            _apiClient = new ApiClient(client, logboekContext);
        }

        public async Task<List<float>> GenerateQueryEmbeddingAsync(string query, Guid? correlationId = null)
        {
            var requestContent = BuildEmbeddingRequest(new[] { query });
            var response = await SendEmbeddingRequestAsync(requestContent, correlationId);
            var results = await ProcessEmbeddingResponse(response);
            return results.FirstOrDefault() ?? new List<float>();
        }

        public async Task<List<float>> GenerateDocumentEmbeddingAsync(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                return new List<float>();

            var requestContent = BuildEmbeddingRequest(new[] { document });
            var response = await SendEmbeddingRequestAsync(requestContent);
            var results = await ProcessEmbeddingResponse(response);
            return results.FirstOrDefault() ?? new List<float>();
        }

        public async Task<List<List<float>>> GenerateBatchEmbeddingsAsync(
            IEnumerable<string> texts,
            string taskType = "RETRIEVAL_DOCUMENT")
        {
            var validTexts = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            if (!validTexts.Any())
                return new List<List<float>>();

            var requestContent = BuildEmbeddingRequest(validTexts);
            var response = await SendEmbeddingRequestAsync(requestContent);
            return await ProcessEmbeddingResponse(response);
        }

        private async Task<HttpResponseMessage> SendEmbeddingRequestAsync(StringContent content, Guid? correlationId = null)
        {
            string endpoint = MistralApiEndpointBuilder.BuildEmbeddingEndpoint();
            string apiKey = ConnectionProcessor.GetApiKey();

            const int maxAttempts = 3;
            var bodyJson = await content.ReadAsStringAsync();

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var requestContent = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    var response = await _apiClient.PostAsync(endpoint, requestContent, apiKey, correlationId,
                        cancellationToken: cts.Token);

                    if (response.IsSuccessStatusCode)
                        return response;

                    // Only retry on server errors (5xx); client errors (4xx) are permanent
                    if ((int)response.StatusCode < 500 || attempt == maxAttempts)
                        return response;

                    AppLogger.LogWarning(
                        $"Embedding API returned {(int)response.StatusCode}, retrying (attempt {attempt}/{maxAttempts})",
                        source: nameof(EmbeddingProcessor));
                }
                catch (OperationCanceledException) when (attempt < maxAttempts)
                {
                    AppLogger.LogWarning(
                        $"Embedding API timed out after 1s, retrying (attempt {attempt}/{maxAttempts})",
                        source: nameof(EmbeddingProcessor));
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))); // 1s, 2s
            }

            throw new Exception("Embedding API failed after 3 attempts");
        }

        private StringContent BuildEmbeddingRequest(IEnumerable<string> texts)
        {
            var requestData = new
            {
                model = Config.EmbeddingModel,
                input = texts.ToArray()
            };

            string json = JsonConvert.SerializeObject(requestData);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<List<List<float>>> ProcessEmbeddingResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                AppLogger.LogError($"Embedding API failed: {response.StatusCode}", source: nameof(EmbeddingProcessor), detail: errorContent);
                throw new Exception($"Embedding API request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseJson);

            var data = result["data"] as JArray;
            if (data == null || !data.Any())
            {
                AppLogger.LogError("API response had no embedding data", source: nameof(EmbeddingProcessor), detail: responseJson);
                throw new Exception("API response did not contain valid embedding values.");
            }

            var embeddings = new List<List<float>>();
            foreach (var item in data.OrderBy(t => (int)t["index"]!))
            {
                var values = item["embedding"]?.ToObject<List<float>>();
                if (values != null)
                    embeddings.Add(values);
            }

            AppLogger.Log($"Embeddings generated: {embeddings.Count} × {embeddings.FirstOrDefault()?.Count} dimensions", source: nameof(EmbeddingProcessor));
            return embeddings;
        }
    }
}
