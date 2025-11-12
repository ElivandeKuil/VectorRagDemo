using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;

namespace VectorRagDemo.BLL
{
    public class EmbeddingProcessor
    {
        private readonly ApiClient _apiClient;
        private readonly GenerativeModelService _generativeModelService;

        public EmbeddingProcessor(HttpClient client, VectorDbContext vectorDbContext, LogboekDbContext logboekContext)
        {
            _apiClient = new ApiClient(client, logboekContext);
            _generativeModelService = new GenerativeModelService(vectorDbContext, client, logboekContext);
        }

        public async Task<(string ProcessedQuery, List<float> Embedding)> GenerateQueryEmbeddingAsync(string query, List<ChatMessage> chatHistory)
        {
            var preprocessingPrompt = _generativeModelService.GetPrompt(PromptTypeEnum.PreProcessing);

            string queryToEmbed = query;

            if (preprocessingPrompt != null)
            {
                var chatHistoryString = FormatChatHistory(chatHistory);

                try
                {
                    queryToEmbed = await _generativeModelService.ExecutePipelineStep<GeminiPreProcessingInnerResponse>(
                        PromptTypeEnum.PreProcessing,
                        response => $"{response.ProcessedQuery} \n {response.DimensionAdvise} \n {response.CosmeticAdvise} \n {response.ContrastAdvise}",
                        chatHistoryString,
                        query
                    );
                }
                catch (Exception ex)
                {
                    queryToEmbed = query;
                }
            }

            var requestContent = BuildEmbeddingRequest(queryToEmbed, "RETRIEVAL_QUERY");
            var response = await SendEmbeddingRequestAsync(requestContent);
            return (queryToEmbed, await ProcessEmbeddingResponse(response));
        }

        public async Task<List<float>> GenerateDocumentEmbeddingAsync(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
            {
                return new List<float>();
            }

            var requestContent = BuildEmbeddingRequest(document, "RETRIEVAL_DOCUMENT");
            var response = await SendEmbeddingRequestAsync(requestContent);
            return await ProcessEmbeddingResponse(response);
        }

        public async Task<List<List<float>>> GenerateBatchEmbeddingsAsync(
            IEnumerable<string> texts,
            string taskType = "RETRIEVAL_DOCUMENT")
        {
            var validTexts = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            if (!validTexts.Any())
            {
                return new List<List<float>>();
            }

            var requestContent = BuildBatchEmbeddingRequest(validTexts, taskType);
            var response = await SendEmbeddingRequestAsync(requestContent);
            return await ProcessBatchEmbeddingResponse(response);
        }

        private async Task<HttpResponseMessage> SendEmbeddingRequestAsync(StringContent content)
        {
            string endpoint = VertexApiEndpointBuilder.BuildEmbeddingEndpoint();
            string accessToken = await ConnectionProcessor.GetAuthenticationToken();

            return await _apiClient.PostAsync(endpoint, content, accessToken);
        }

        private StringContent BuildEmbeddingRequest(string text, string taskType)
        {
            var requestData = new
            {
                instances = new[]
                {
                    new
                    {
                        task_type = taskType,
                        content = text
                    }
                }
            };

            return CreateJsonContent(requestData);
        }

        private StringContent BuildBatchEmbeddingRequest(List<string> texts, string taskType)
        {
            var requestData = new
            {
                instances = texts.Select(text => new
                {
                    task_type = taskType,
                    content = text
                }).ToArray()
            };

            return CreateJsonContent(requestData);
        }

        private async Task<List<float>> ProcessEmbeddingResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Embedding API request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResult = JObject.Parse(responseJson);

            var prediction = embeddingResult["predictions"]?.FirstOrDefault();
            var embeddingValues = prediction?["embeddings"]?["values"];

            if (embeddingValues != null)
            {
                return embeddingValues.ToObject<List<float>>();
            }

            throw new Exception("API response did not contain valid embedding values.");
        }

        private async Task<List<List<float>>> ProcessBatchEmbeddingResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Embedding API request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResult = JObject.Parse(responseJson);

            var predictions = embeddingResult["predictions"];
            if (predictions == null)
            {
                throw new Exception("API response did not contain predictions.");
            }

            var embeddings = new List<List<float>>();
            foreach (var prediction in predictions)
            {
                var embeddingValues = prediction["embeddings"]?["values"];
                if (embeddingValues != null)
                {
                    embeddings.Add(embeddingValues.ToObject<List<float>>());
                }
            }

            return embeddings;
        }

        private StringContent CreateJsonContent(object data)
        {
            string jsonRequest = JsonConvert.SerializeObject(data);
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }

        private string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
