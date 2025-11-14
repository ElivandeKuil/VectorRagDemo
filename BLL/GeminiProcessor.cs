using VectorRagDemo.Models.Enums;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.BLL
{
    public class GeminiProcessor
    {
        private readonly GenerativeModelService _generativeModelService;

        public GeminiProcessor(VectorDbContext context, HttpClient client, LogboekDbContext logboekContext)
        {
            _generativeModelService = new GenerativeModelService(context, client, logboekContext);
        }

        public async Task<GenerativeModelResponse> GenerateContent(
            List<ChatMessage> chatHistory,
            string currentUserInput,
            string formattedNeighbors)
        {
            var chatHistoryString = FormatChatHistory(chatHistory);

            var summarisedContext = await _generativeModelService.ExecutePipelineStep<GeminiSummarisingInnerResponse>(
                PromptTypeEnum.Sumarizing,
                response => response.RelevantOutput,
                formattedNeighbors,
                chatHistoryString,
                currentUserInput
            );

            var result = await _generativeModelService.ExecutePipelineStep<GeminiAnswerGenerationInnerResponse, GenerativeModelResponse>(
                PromptTypeEnum.GenerateResponse,
                response => new GenerativeModelResponse
                {
                    SourceText = summarisedContext,
                    ResponseText = response.Response,
                    RedirectUrl = response.RedirectUrl
                },
                summarisedContext,
                currentUserInput,
                chatHistoryString
            );

            return result;
        }

        private string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
