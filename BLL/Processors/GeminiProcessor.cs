using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.BLL.Processors
{
    public class GeminiProcessor
    {
        private readonly VectorDbContext _context;
        private readonly HttpClient _client;
        private readonly LogboekDbContext _logboekContext;

        public GeminiProcessor(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext)
        {
            _context = context;
            _client = client;
            _logboekContext = logboekContext;
        }

        public async Task<GenerativeModelResponse> GenerateContent(
            List<ChatMessage> chatHistory,
            string currentUserInput,
            string formattedNeighbors,
            bool extraCommunicationEnabled = false,
            int projectId = 0,
            Guid? correlationId = null)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);
            var chatHistoryString = FormatChatHistory(chatHistory);

            // Summarization detects transferToWhatsApp when escalation is active —
            // it has access to the user query and chunks, so the decision is made once
            // and shared by both the streaming and non-streaming paths.
            var summarisedResult = await SummarizeAsync(
                formattedNeighbors,
                chatHistoryString,
                currentUserInput,
                projectId,
                extraCommunicationEnabled,
                correlationId);

            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.GenerateResponse).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.GenerateResponse}");

            var result = await generativeModelService.ExecutePipelineStep<GeminiAnswerGenerationInnerResponse, GenerativeModelResponse>(
                basePrompt,
                response => new GenerativeModelResponse
                {
                    SourceText = summarisedResult.RelevantOutput,
                    ResponseText = response.Response,
                    UsedChunkIds = summarisedResult.UsedChunkIds,
                    TransferToWhatsApp = summarisedResult.TransferToWhatsApp
                },
                summarisedResult.RelevantOutput,
                currentUserInput,
                chatHistoryString
            );

            return result;
        }

        /// <summary>
        /// Returns a shallow copy of <paramref name="prompt"/> with the escalation instruction
        /// appended and <c>transferToWhatsApp</c> injected into the response schema.
        /// The original DB entity is never modified.
        /// </summary>
        private static Prompt WithEscalationInjected(Prompt prompt) => new()
        {
            ID = prompt.ID,
            Project = prompt.Project,
            PromptType = prompt.PromptType,
            Model = prompt.Model,
            Volgorde = prompt.Volgorde,
            Status = prompt.Status,
            MaxTokens = prompt.MaxTokens,
            Temperature = prompt.Temperature,
            TopP = prompt.TopP,
            TopK = prompt.TopK,
            Content = prompt.Content,
            SystemInstruction = prompt.SystemInstruction + WhatsAppSystemInstruction,
            ResponseSchema = InjectWhatsAppIntoSchema(prompt.ResponseSchema)
        };

        internal const string WhatsAppSystemInstruction =
            "\n\nWanneer je een vraag niet kunt beantwoorden op basis van de beschikbare informatie, " +
            "of wanneer je detecteert dat de gebruiker interesse toont in een aankoop, offerte of persoonlijk contact, " +
            "stel dan 'transferToWhatsApp' in op true. In alle andere gevallen is het false.";

        private static string InjectWhatsAppIntoSchema(string existingSchema)
        {
            try
            {
                var schema = JObject.Parse(existingSchema);

                var properties = schema["properties"] as JObject ?? new JObject();
                properties["transferToWhatsApp"] = JObject.Parse("{\"type\":\"boolean\"}");
                schema["properties"] = properties;

                var required = schema["required"] as JArray ?? new JArray();
                if (!required.Any(t => t.Value<string>() == "transferToWhatsApp"))
                    required.Add("transferToWhatsApp");
                schema["required"] = required;

                return schema.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                return existingSchema;
            }
        }

        internal static string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }

        /// <summary>
        /// Runs only the summarization step of the RAG pipeline.
        /// When <paramref name="extraCommunicationEnabled"/> is true the escalation instruction is
        /// injected into the summarization prompt so that <c>transferToWhatsApp</c> is detected
        /// here — before streaming starts — instead of in the final response step.
        /// </summary>
        public async Task<(string RelevantOutput, List<int> UsedChunkIds, bool TransferToWhatsApp)> SummarizeAsync(
            string formattedNeighbors,
            string chatHistoryString,
            string currentUserInput,
            int projectId,
            bool extraCommunicationEnabled = false,
            Guid? correlationId = null)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);

            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.Sumarizing).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.Sumarizing}");

            var prompt = extraCommunicationEnabled ? WithEscalationInjected(basePrompt) : basePrompt;

            return await generativeModelService.ExecutePipelineStep<GeminiSummarisingInnerResponse, (string, List<int>, bool)>(
                prompt,
                response => (response.RelevantOutput, response.UsedChunkIds ?? new List<int>(), response.TransferToWhatsApp),
                formattedNeighbors,
                chatHistoryString,
                currentUserInput
            );
        }

        /// <summary>
        /// Streams the final LLM response token-by-token (plain text).
        /// Escalation is handled by <see cref="SummarizeAsync"/> when streaming is active.
        /// </summary>
        public async IAsyncEnumerable<string> StreamFinalResponseAsync(
            string summarisedContent,
            string currentUserInput,
            string chatHistoryString,
            int projectId,
            Guid? correlationId = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);

            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.GenerateResponse).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.GenerateResponse}");

            await foreach (var token in generativeModelService.ExecuteStreamingStep(
                basePrompt,
                new[] { summarisedContent, currentUserInput, chatHistoryString },
                ct))
            {
                yield return token;
            }
        }
    }
}
