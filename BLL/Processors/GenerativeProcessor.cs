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
    public class GenerativeProcessor
    {
        private readonly VectorDbContext _context;
        private readonly HttpClient _client;
        private readonly LogboekDbContext _logboekContext;

        public GenerativeProcessor(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext)
        {
            _context = context;
            _client = client;
            _logboekContext = logboekContext;
        }

        public async Task<GenerativeModelResponse> GenerateContent(
            List<ChatMessage> chatHistory,
            string currentUserInput,
            string formattedNeighbors,
            int projectId = 0,
            Guid? correlationId = null)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);
            var chatHistoryString = FormatChatHistory(chatHistory);

            // Summarization detects transferToWhatsApp when escalation is active —
            // it has access to the user query and chunks, so the decision is made once
            // and shared by both the streaming and non-streaming paths.
            (string RelevantOutput, List<int> UsedChunkIds, bool TransferToWhatsApp) summarisedResult = await SummarizeAsync(
                formattedNeighbors,
                chatHistoryString,
                currentUserInput,
                projectId,
                correlationId);

            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.GenerateResponse).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.GenerateResponse}");

            var prompt = InjectExtraContext(basePrompt, summarisedResult.TransferToWhatsApp);

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
        private Prompt InjectExtraContext(Prompt prompt, bool hasCommunicationEscalation)
        {
            if (!hasCommunicationEscalation)
                return prompt;

            string extraContext = @"<CONTEXT_INJECTION>A previous step in the pipeline has detected a lead or blockade and has therefore
determined an escalation to human communication channels. This means that there will be one or more buttons underneath your response which the
user can click to go straight to a human communication channel. Adjust your response accordingly to this new context.</CONTEXT_INJECTION>";

            // Return a detached copy so the EF-tracked entity is never mutated
            // and the injected context is never accidentally persisted to the database.
            return new Prompt
            {
                ID = prompt.ID,
                Project = prompt.Project,
                PromptType = prompt.PromptType,
                SystemInstruction = prompt.SystemInstruction,
                Content = prompt.Content + extraContext,
                GemaaktOp = prompt.GemaaktOp,
                GewijzigdOp = prompt.GewijzigdOp,
                Status = prompt.Status,
                ResponseSchema = prompt.ResponseSchema,
                MaxTokens = prompt.MaxTokens,
                Temperature = prompt.Temperature,
                TopP = prompt.TopP,
                TopK = prompt.TopK,
                Model = prompt.Model,
                Volgorde = prompt.Volgorde
            };
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
            Guid? correlationId = null)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);

            var prompt = generativeModelService.GetPrompt(PromptTypeEnum.Sumarizing).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.Sumarizing}");

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
            bool transferToWhatsApp = false,
            Guid? correlationId = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var generativeModelService = new GenerativeModelService(_context, _client, _logboekContext, projectId, correlationId);

            var basePrompt = generativeModelService.GetPrompt(PromptTypeEnum.GenerateResponse).FirstOrDefault()
                ?? throw new Exception($"No active prompt found for type {PromptTypeEnum.GenerateResponse}");

            var prompt = InjectExtraContext(basePrompt, transferToWhatsApp);

            await foreach (var token in generativeModelService.ExecuteStreamingStep(
                prompt,
                new[] { summarisedContent, currentUserInput, chatHistoryString },
                ct))
            {
                yield return token;
            }
        }
    }
}
