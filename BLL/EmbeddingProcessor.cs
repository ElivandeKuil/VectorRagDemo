using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace VectorRagDemo.BLL
{
    public static class EmbeddingProcessor
    {
        public static async Task<List<float>> GenerateQueryEmbeddingAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<float>();
            }

            try
            {
                string projectId = ConnectionProcessor.GetProjectId();
                string embeddingApiUrl = $"https://{Config.Location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{Config.Location}/publishers/google/models/{Config.EmbeddingModel}:predict";

                using (HttpClient client = new HttpClient())
                {
                    string accessToken = await ConnectionProcessor.GetAuthenticationToken();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var requestData = new
                    {
                        instances = new[]
                        {
                            new
                            {
                                task_type = "RETRIEVAL_QUERY",
                                content = query
                            }
                        }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(requestData);
                    var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(embeddingApiUrl, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = await response.Content.ReadAsStringAsync();
                        var embeddingResult = JObject.Parse(responseJson);

                        var prediction = embeddingResult["predictions"]?.FirstOrDefault();
                        var embeddingValues = prediction?["embeddings"]?["values"];

                        if (embeddingValues != null)
                        {
                            return embeddingValues.ToObject<List<float>>();
                        }
                        else
                        {
                            throw new Exception("API response did not contain valid embedding values.");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API request failed with status {response.StatusCode}: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}