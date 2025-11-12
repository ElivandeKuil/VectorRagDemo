using System.Text;
using Newtonsoft.Json;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Centralized service for calling generative models (Gemini) across the application.
    /// This service can be used by any processor that needs to call generative models.
    /// </summary>
    public class GenerativeModelService
    {
        private readonly VectorDbContext _context;
        private readonly ApiClient _apiClient;
        private readonly int _project;

        public GenerativeModelService(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext, int projectId = 1)
        {
            _context = context;
            _apiClient = new ApiClient(client, logboekContext);
            _project = projectId;
        }

        /// <summary>
        /// Executes a single generative model pipeline step with custom response mapping.
        /// </summary>
        /// <typeparam name="TResponse">The type of the inner response from the model</typeparam>
        /// <typeparam name="TOutput">The type of the output after mapping</typeparam>
        /// <param name="promptType">The type of prompt to use</param>
        /// <param name="mapper">Function to map the response to the desired output</param>
        /// <param name="formatArgs">Arguments to format the prompt content</param>
        /// <returns>The mapped output</returns>
        public async Task<TOutput> ExecutePipelineStep<TResponse, TOutput>(
            PromptTypeEnum promptType,
            Func<TResponse, TOutput> mapper,
            params string[] formatArgs)
        {
            var prompt = GetPrompt(promptType);
            if (prompt == null)
            {
                throw new Exception($"No active prompt found for type {promptType} in project {_project}");
            }

            var requestContent = BuildRequestContent(prompt, formatArgs);
            var response = await SendGeminiRequestAsync(requestContent, prompt.Model);
            return await ProcessResponse(response, mapper);
        }

        /// <summary>
        /// Executes a pipeline step that returns a string result.
        /// </summary>
        public async Task<string> ExecutePipelineStep<TResponse>(
            PromptTypeEnum promptType,
            Func<TResponse, string> mapper,
            params string[] formatArgs)
        {
            return await ExecutePipelineStep<TResponse, string>(promptType, mapper, formatArgs);
        }

        /// <summary>
        /// Retrieves a prompt from the database by type.
        /// </summary>
        public Prompt? GetPrompt(PromptTypeEnum promptType)
        {
            return _context.Prompts
                .Where(o => o.Project == _project
                    && o.PromptType == (int)promptType
                    && o.Status == 1)
                .SingleOrDefault();
        }

        /// <summary>
        /// Sends a request to the Gemini API.
        /// </summary>
        private async Task<HttpResponseMessage> SendGeminiRequestAsync(StringContent content, string model)
        {
            string endpoint = VertexApiEndpointBuilder.BuildGeminiEndpoint(model);
            string accessToken = await ConnectionProcessor.GetAuthenticationToken();

            return await _apiClient.PostAsync(endpoint, content, accessToken);
        }

        /// <summary>
        /// Builds the request content for a Gemini API call.
        /// </summary>
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

        /// <summary>
        /// Processes the response from the Gemini API and maps it to the desired output type.
        /// </summary>
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

        /// <summary>
        /// Creates a GeminiContent object with the specified role and text.
        /// </summary>
        private GeminiContent CreateGeminiContent(string role, string text)
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
