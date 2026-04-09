using System.Text;
using Newtonsoft.Json;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.JsonContracts.MistralAPI;
using VectorRagDemo.BLL.Processors;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Centralized service for calling generative models (Mistral) across the application.
    /// This service can be used by any processor that needs to call generative models.
    /// </summary>
    public class GenerativeModelService
    {
        private readonly VectorDbContext _context;
        private readonly ApiClient _apiClient;
        private readonly int _project;
        private readonly Guid? _correlationId;

        public GenerativeModelService(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext, int projectId = 1, Guid? correlationId = null)
        {
            _context = context;
            _apiClient = new ApiClient(client, logboekContext);
            _project = projectId;
            _correlationId = correlationId;
        }

        /// <summary>
        /// Executes a single generative model pipeline step with a provided prompt.
        /// </summary>
        public async Task<TOutput> ExecutePipelineStep<TResponse, TOutput>(
            Prompt prompt,
            Func<TResponse, TOutput> mapper,
            params string[] formatArgs)
        {
            var requestContent = BuildRequestContent(prompt, formatArgs);
            var response = await SendMistralRequestAsync(requestContent, prompt.Model);
            return await ProcessResponse<TResponse, TOutput>(response, mapper);
        }

        /// <summary>
        /// Executes a pipeline step with a provided prompt that returns a string result.
        /// </summary>
        public async Task<string> ExecutePipelineStep<TResponse>(
            Prompt prompt,
            Func<TResponse, string> mapper,
            params string[] formatArgs)
        {
            return await ExecutePipelineStep<TResponse, string>(prompt, mapper, formatArgs);
        }

        /// <summary>
        /// Executes a single generative model pipeline step by retrieving the first prompt of the given type.
        /// </summary>
        public async Task<TOutput> ExecutePipelineStep<TResponse, TOutput>(
            PromptTypeEnum promptType,
            Func<TResponse, TOutput> mapper,
            params string[] formatArgs)
        {
            var prompt = GetPrompt(promptType).FirstOrDefault();
            if (prompt == null)
            {
                throw new Exception($"No active prompt found for type {promptType} in project {_project}");
            }

            return await ExecutePipelineStep<TResponse, TOutput>(prompt, mapper, formatArgs);
        }

        /// <summary>
        /// Executes a pipeline step by retrieving the first prompt of the given type, returns a string result.
        /// </summary>
        public async Task<string> ExecutePipelineStep<TResponse>(
            PromptTypeEnum promptType,
            Func<TResponse, string> mapper,
            params string[] formatArgs)
        {
            return await ExecutePipelineStep<TResponse, string>(promptType, mapper, formatArgs);
        }

        /// <summary>
        /// Retrieves prompts from the database by type.
        /// Falls back to project 1 (generic prompts) when none are found for the current project.
        /// </summary>
        public List<Prompt> GetPrompt(PromptTypeEnum promptType)
        {
            var prompts = _context.Prompts
                .Where(o => o.Project == _project
                    && o.PromptType == (int)promptType
                    && o.Status == 1)
                .OrderBy(o => o.Volgorde)
                .ToList();

            if (prompts.Count == 0 && _project != 1)
            {
                prompts = _context.Prompts
                    .Where(o => o.Project == 1
                        && o.PromptType == (int)promptType
                        && o.Status == 1)
                    .OrderBy(o => o.Volgorde)
                    .ToList();
            }

            return prompts;
        }

        /// <summary>
        /// Sends a request to the Mistral chat completions API.
        /// </summary>
        private async Task<HttpResponseMessage> SendMistralRequestAsync(StringContent content, string model)
        {
            string endpoint = MistralApiEndpointBuilder.BuildChatEndpoint();
            string apiKey = ConnectionProcessor.GetApiKey();

            return await _apiClient.PostAsync(endpoint, content, apiKey, _correlationId);
        }

        /// <summary>
        /// Builds the system instruction for GenerateResponse prompts.
        /// When GebruikPromptTabel is true, the prompt table's SystemInstruction is used as-is.
        /// When false, the instruction is built entirely from the user's PromptInstelling fields.
        /// </summary>
        private string BuildSystemInstruction(Prompt prompt)
        {
            if (prompt.PromptType != (int)PromptTypeEnum.GenerateResponse)
                return prompt.SystemInstruction;

            var project = _context.Projects.Find(_project);

            if (project == null || project.GebruikPromptTabel)
                return prompt.SystemInstruction;

            var instelling = _context.PromptInstellingen
                .FirstOrDefault(p => p.ProjectID == _project);

            if (instelling == null)
                return prompt.SystemInstruction;

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(instelling.Persona))
                parts.Add($"## Persona\n{instelling.Persona.Trim()}");

            if (!string.IsNullOrWhiteSpace(instelling.Instructies))
                parts.Add($"## Instructies\n{instelling.Instructies.Trim()}");

            if (!string.IsNullOrWhiteSpace(instelling.Regels))
                parts.Add($"## Regels\n{instelling.Regels.Trim()}");

            if (!string.IsNullOrWhiteSpace(instelling.Voorbeelden))
                parts.Add($"## Voorbeelden\n{instelling.Voorbeelden.Trim()}");

            var built = parts.Count == 0
                ? prompt.SystemInstruction
                : string.Join("\n\n", parts);

            // If the prompt was escalation-injected (WhatsApp suffix appended at runtime),
            // re-attach that suffix so it isn't lost when building from PromptInstelling.
            if (prompt.SystemInstruction.EndsWith(GeminiProcessor.WhatsAppSystemInstruction,
                    StringComparison.Ordinal))
                built += GeminiProcessor.WhatsAppSystemInstruction;

            return built;
        }

        /// <summary>
        /// Builds the request content for a Mistral chat completions call.
        /// </summary>
        private StringContent BuildRequestContent(Prompt prompt, params string[] formatArgs)
        {
            var formattedContent = string.Format(prompt.Content, formatArgs);

            var payload = new
            {
                model = prompt.Model,
                messages = new[]
                {
                    new { role = "system", content = BuildSystemInstruction(prompt) },
                    new { role = "user",   content = formattedContent }
                },
                temperature = prompt.Temperature,
                top_p = prompt.TopP,
                max_tokens = prompt.MaxTokens,
                response_format = new { type = "json_object" }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            return new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Processes the response from the Mistral API and maps it to the desired output type.
        /// </summary>
        private async Task<TOutput> ProcessResponse<TResponse, TOutput>(
            HttpResponseMessage response,
            Func<TResponse, TOutput> mapper)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Mistral API call failed with status {response.StatusCode}: {errorContent}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var mistralResponse = JsonConvert.DeserializeObject<MistralChatResponse>(jsonResponse);

            var content = mistralResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                var innerData = JsonConvert.DeserializeObject<TResponse>(content);
                if (innerData != null)
                {
                    return mapper(innerData);
                }
            }

            throw new Exception("No valid response received from Mistral API");
        }
    }
}
