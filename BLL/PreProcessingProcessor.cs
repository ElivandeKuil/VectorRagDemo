using VectorRagDemo.DAL;
using VectorRagDemo.Models.ApiContracts.GeminiAPI;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.BLL
{
    public class PreProcessingProcessor
    {
        private readonly GenerativeModelService _generativeModelService;

        private readonly HttpClient _client;
        private readonly VectorDbContext _vectorDbContext;
        private readonly LogboekDbContext _logboekContext;

        public PreProcessingProcessor(HttpClient client, VectorDbContext vectorDbContext, LogboekDbContext logboekContext)
        {
            _client = client;
            _vectorDbContext = vectorDbContext;
            _logboekContext = logboekContext;
            _generativeModelService = new GenerativeModelService(vectorDbContext, client, logboekContext);
        }

        public async Task<string> GetPreProcessedQuery(string query, List<ChatMessage> chatHistory, Guid? correlationId = null)
        {
            var svc = correlationId.HasValue
                ? new GenerativeModelService(_vectorDbContext, _client, _logboekContext, correlationId: correlationId)
                : _generativeModelService;
            var preprocessingPrompts = svc.GetPrompt(PromptTypeEnum.PreProcessing);
            var chatHistoryString = GlobalDomain.Helpers.FormatChatHistory(chatHistory);
            string preProcessedQuery = query;

            foreach (var prompt in preprocessingPrompts)
            {
                try
                {
                    preProcessedQuery = await svc.ExecutePipelineStep<GeminiPreProcessingInnerResponse>(
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
