using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiSummarisingInnerResponse
    {
        [JsonProperty("summarisedRelevantOutput")]
        public string SummarisedRelevantOutput { get; set; }
    }
}
