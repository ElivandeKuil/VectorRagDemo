using System.Drawing;
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
            List<ChatMessage> chatHistory,
            string currentUserInput,
            string formattedNeighbors,
            int project)
        {
            try
            {
                string projectId = ConnectionProcessor.GetProjectId();
                string geminiEndpoint = $"https://{Config.Location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{Config.Location}/publishers/google/models/{Config.GeminiModelID}:generateContent";

                string accessToken = await ConnectionProcessor.GetAuthenticationToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                StringContent content = BuildRequestConent(chatHistory, currentUserInput, formattedNeighbors, context, project);

                HttpResponseMessage response = await client.PostAsync(geminiEndpoint, content);

                return await ProcessResponse(response, context);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in GeminiConnector: {ex.Message}");
                throw;
            }
        }

        private static StringContent BuildRequestConent(List<ChatMessage> chatHistory, string currentUserInput, string formattedNeighbors, VectorDbContext context, int project)
        {
            var contents = new List<GeminiContent>();

            var prompt = context.Prompts.Where(o => o.Project == project && o.PromptType == 1 && o.Status == 1).Single();
            var chatHistoryString = string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
            var formattedPrompt = string.Format(prompt.Content, formattedNeighbors, currentUserInput, chatHistoryString);
            contents.Add(CreateGeminiContent("user", formattedPrompt));

            var payload = new
            {
                systemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = prompt.SystemInstruction } }
                },
                contents = contents,
                generationConfig = new
                {
                    maxOutputTokens = Config.MaxTokens,
                    temperature = Config.Temperature,
                    topP = Config.TopP,
                    topK = Config.TopK,
                    responseMimeType = "application/json",
                    responseSchema = new
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
            };

            string jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
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

            if (geminiResponse?.Candidates?.FirstOrDefault().Content?.Parts?.FirstOrDefault() is { } part)
            {
                var innerJsonString = part.Text;
                var innerData = JsonConvert.DeserializeObject<GeminiInnerResponse>(innerJsonString);

                if (innerData != null)
                {
                    output.ResponseText = innerData.Answer;

                    if (!string.IsNullOrEmpty(innerData.SupplementalInfo))
                    {
                        output.ResponseText += $"\n\n{innerData.SupplementalInfo}";
                    }

                    if (!string.IsNullOrEmpty(innerData.UsedChunks) && innerData.UsedChunks != "[]")
                    {
                        int[] chunkIds = innerData.UsedChunks.Split(',').Select(int.Parse).ToArray();

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
