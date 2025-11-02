using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiSummarisingInnerResponse
    {
        [JsonProperty("summarisedRelevantOutput")]
        public string SummarisedRelevantOutput { get; set; }
    }
}
