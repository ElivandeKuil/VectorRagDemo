using Newtonsoft.Json.Linq;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.BLL
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

            var summarisedResult = await generativeModelService.ExecutePipelineStep<GeminiSummarisingInnerResponse, (string RelevantOutput, List<int> UsedChunkIds)>(
                PromptTypeEnum.Sumarizing,
                response => (response.RelevantOutput, response.UsedChunkIds ?? new List<int>()),
                formattedNeighbors,
                chatHistoryString,
                currentUserInput
            );

            // Fetch the GenerateResponse prompt and optionally inject escalation behaviour.
            // The base prompt in the DB has no knowledge of extra communication channels —
            // the instruction and schema field are only added at runtime when at least one
            // channel (WhatsApp / email) is active for this project.
            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.GenerateResponse).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.GenerateResponse}");

            var generatePrompt = extraCommunicationEnabled
                ? WithEscalationInjected(basePrompt)
                : basePrompt;

            var result = await generativeModelService.ExecutePipelineStep<GeminiAnswerGenerationInnerResponse, GenerativeModelResponse>(
                generatePrompt,
                response => new GenerativeModelResponse
                {
                    SourceText = summarisedResult.RelevantOutput,
                    ResponseText = response.Response,
                    UsedChunkIds = summarisedResult.UsedChunkIds,
                    TransferToWhatsApp = extraCommunicationEnabled && response.TransferToWhatsApp
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
                properties["transferToWhatsApp"] = JObject.Parse("{\"type\":\"BOOLEAN\"}");
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

        private string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
