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

            var summarisedResult = await _generativeModelService.ExecutePipelineStep<GeminiSummarisingInnerResponse, (string RelevantOutput, string RedirectUrl)>(
                PromptTypeEnum.Sumarizing,
                response => (response.RelevantOutput, response.RedirectUrl),
                formattedNeighbors,
                chatHistoryString,
                currentUserInput
            );

            var result = await _generativeModelService.ExecutePipelineStep<GeminiAnswerGenerationInnerResponse, GenerativeModelResponse>(
                PromptTypeEnum.GenerateResponse,
                response => new GenerativeModelResponse
                {
                    SourceText = summarisedResult.RelevantOutput,
                    ResponseText = response.Response,
                    RedirectUrl = summarisedResult.RedirectUrl
                },
                summarisedResult.RelevantOutput,
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
