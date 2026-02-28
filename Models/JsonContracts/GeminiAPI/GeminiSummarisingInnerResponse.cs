using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiSummarisingInnerResponse
    {
        [JsonProperty("relevantOutput")]
        public string RelevantOutput { get; set; }
    }
}
