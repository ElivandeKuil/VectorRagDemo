using System.Text;
using Newtonsoft.Json;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.BLL
{
    public class GeminiProcessor
    {
        private readonly VectorDbContext _context;
        private readonly int _project;
        private readonly ApiClient _apiClient;

        public GeminiProcessor(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext)
        {
            _context = context;
            _project = 1;
            _apiClient = new ApiClient(client, logboekContext);
        }

        public async Task<GenerativeModelResponse> GenerateContent(
            List<ChatMessage> chatHistory,
            string currentUserInput,
            string formattedNeighbors)
        {
            var chatHistoryString = FormatChatHistory(chatHistory);

            var summarisedContext = await ExecutePipelineStep<GeminiSummarisingInnerResponse>(
                PromptTypeEnum.Sumarizing,
                response => response.SummarisedRelevantOutput,
                formattedNeighbors,
                chatHistoryString,
                currentUserInput
            );

            var result = await ExecutePipelineStep<GeminiAnswerGenerationInnerResponse, GenerativeModelResponse>(
                PromptTypeEnum.GenerateResponse,
                response => new GenerativeModelResponse
                {
                    SourceText = summarisedContext,
                    ResponseText = response.Response
                },
                summarisedContext,
                currentUserInput,
                chatHistoryString
            );

            return result;
        }

        private async Task<TOutput> ExecutePipelineStep<TResponse, TOutput>(
            PromptTypeEnum promptType,
            Func<TResponse, TOutput> mapper,
            params string[] formatArgs)
        {
            var prompt = GetPrompt(promptType);
            var requestContent = BuildRequestContent(prompt, formatArgs);
            var response = await SendGeminiRequestAsync(requestContent, prompt.Model);
            return await ProcessResponse(response, mapper);
        }

        private async Task<string> ExecutePipelineStep<TResponse>(
            PromptTypeEnum promptType,
            Func<TResponse, string> mapper,
            params string[] formatArgs)
        {
            return await ExecutePipelineStep<TResponse, string>(promptType, mapper, formatArgs);
        }

        private async Task<HttpResponseMessage> SendGeminiRequestAsync(StringContent content, string model)
        {
            string endpoint = VertexApiEndpointBuilder.BuildGeminiEndpoint(model);
            string accessToken = await ConnectionProcessor.GetAuthenticationToken();

            return await _apiClient.PostAsync(endpoint, content, accessToken);
        }

        private Prompt GetPrompt(PromptTypeEnum promptType)
        {
            return _context.Prompts
                .Where(o => o.Project == _project
                    && o.PromptType == (int)promptType
                    && o.Status == 1)
                .Single();
        }

        private StringContent BuildRequestContent(Prompt prompt, params string[] formatArgs)
        {
            var formattedPrompt = string.Format(prompt.Content, formatArgs);

            var payload = new
            {
                systemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = prompt.SystemInstruction } }
                },
                contents = new List<GeminiContent>
                {
                    CreateGeminiContent("user", formattedPrompt)
                },
                generationConfig = new
                {
                    maxOutputTokens = prompt.MaxTokens,
                    temperature = prompt.Temperature,
                    topP = prompt.TopP,
                    topK = prompt.TopK,
                    responseMimeType = "application/json",
                    responseSchema = JsonConvert.DeserializeObject<object>(prompt.ResponseSchema)
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            return new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        }

        private async Task<TOutput> ProcessResponse<TResponse, TOutput>(
            HttpResponseMessage response,
            Func<TResponse, TOutput> mapper)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API call failed with status {response.StatusCode}: {errorContent}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(jsonResponse);

            if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault() is { } part)
            {
                var innerData = JsonConvert.DeserializeObject<TResponse>(part.Text);
                if (innerData != null)
                {
                    return mapper(innerData);
                }
            }

            throw new Exception("No valid response received from Gemini API");
        }

        private GeminiContent CreateGeminiContent(string role, string text)
        {
            var parts = new List<GeminiPart>();

            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(new GeminiPart { Text = text });
            }

            return new GeminiContent { Role = role, Parts = parts };
        }

        private string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
