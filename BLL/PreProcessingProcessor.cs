using VectorRagDemo.DAL;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.BLL
{
    public class PreProcessingProcessor
    {
        private readonly GenerativeModelService _generativeModelService;

        public PreProcessingProcessor(HttpClient client, VectorDbContext vectorDbContext, LogboekDbContext logboekContext)
        {
            _generativeModelService = new GenerativeModelService(vectorDbContext, client, logboekContext);
        }

        public async Task<string> GetPreProcessedQuery(string query, List<ChatMessage> chatHistory)
        {
            var preprocessingPrompts = _generativeModelService.GetPrompt(PromptTypeEnum.PreProcessing);
            var chatHistoryString = GlobalDomain.Helpers.FormatChatHistory(chatHistory);
            string preProcessedQuery = query;

            foreach (var prompt in preprocessingPrompts)
            {
                try
                {
                    preProcessedQuery = await _generativeModelService.ExecutePipelineStep<GeminiPreProcessingInnerResponse>(
                        prompt,
                        response => response.ProcessedQuery,
                        chatHistoryString,
                        preProcessedQuery
                    );
                }
                catch (Exception ex)
                {
                    preProcessedQuery = query;
                }
            }
            return preProcessedQuery;
        }
    }
}
