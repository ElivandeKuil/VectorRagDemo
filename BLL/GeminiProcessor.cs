using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using VectorRagDemo.Data;
using VectorRagDemo.Models;

namespace VectorRagDemo.BLL
{
    public static class GeminiProcessor
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<ChatbotResponse> GenerateContentAsync(
            VectorDbContext context,
            string systemPrompt,
            List<ChatMessage> chatHistory,
            string currentUserInput,
            GeminiParameters parameters)
        {
            try
            {
                string projectId = ConnectionProcessor.GetProjectId();
                string geminiEndpoint = $"https://{Config.Location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{Config.Location}/publishers/google/models/{Config.GeminiModelID}:generateContent";

                string accessToken = await ConnectionProcessor.GetAuthenticationToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                StringContent content = BuildRequestConent(systemPrompt, chatHistory, currentUserInput, parameters);

                HttpResponseMessage response = await client.PostAsync(geminiEndpoint, content);

                return await ProcessResponse(response, context);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in GeminiConnector: {ex.Message}");
                throw;
            }
        }

        private static StringContent BuildRequestConent(string systemPrompt, List<ChatMessage> chatHistory, string currentUserInput, GeminiParameters parameters)
        {
            var contents = new List<GeminiContent>();
            foreach (var message in chatHistory)
            {
                contents.Add(CreateGeminiContent(message.Role, message.Content));
            }
            contents.Add(CreateGeminiContent("user", currentUserInput));

            var payload = new
            {
                systemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                },
                contents = contents,
                generationConfig = new
                {
                    maxOutputTokens = parameters.MaxOutputTokens,
                    temperature = parameters.Temperature,
                    topP = parameters.TopP,
                    topK = parameters.TopK,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                answer = new { type = "string" },
                                supplementalInfo = new { type = "string" },
                                questionToUser = new { type = "string" },
                                usedChunks = new { type = "string" }
                            },
                            propertyOrdering = new[] { "answer", "supplementalInfo", "questionToUser", "usedChunks" }
                        }
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return content;
        }

        private static async Task<ChatbotResponse> ProcessResponse(
    HttpResponseMessage response,
    VectorDbContext context)  // Add context parameter
        {
            ChatbotResponse output = new ChatbotResponse();

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API call failed with status {response.StatusCode}: {errorContent}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(jsonResponse);

            if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault() is { } part)
            {
                var innerJsonString = part.Text;
                var innerData = JsonConvert.DeserializeObject<List<GeminiInnerResponse>>(innerJsonString);

                if (innerData != null && innerData.SingleOrDefault() != null)
                {
                    var innerResponse = innerData.Single();
                    output.ResponseText = innerResponse.Answer;

                    if (!string.IsNullOrEmpty(innerResponse.SupplementalInfo))
                    {
                        output.ResponseText += $"\n\n{innerResponse.SupplementalInfo}";
                    }

                    if (!string.IsNullOrEmpty(innerResponse.UsedChunks) && innerResponse.UsedChunks != "[]")
                    {
                        int[] chunkIds = innerResponse.UsedChunks.Split(',').Select(int.Parse).ToArray();

                        var chunks = context.Chunks
                            .Where(c => chunkIds.Contains(c.ID))
                            .Select(c => new { c.ID, c.Tekst })
                            .ToList();

                        foreach (var chunk in chunks)
                        {
                            output.SourceText += $"CHUNKID:{chunk.ID} : {chunk.Tekst} \n\n";
                        }
                    }
                    else
                    {
                        output.SourceText = "No source text used.";
                    }
                }
            }
            return output;
        }

        private static GeminiContent CreateGeminiContent(string role, string text)
        {
            var parts = new List<GeminiPart>();

            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(new GeminiPart { Text = text });
            }

            return new GeminiContent { Role = role, Parts = parts };
        }
    }
}
