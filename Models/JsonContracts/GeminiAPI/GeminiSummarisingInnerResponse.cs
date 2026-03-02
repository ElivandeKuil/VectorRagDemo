using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiSummarisingInnerResponse
    {
        [JsonProperty("relevantOutput")]
        public string RelevantOutput { get; set; }

        [JsonProperty("usedChunkIds")]
        public List<int>? UsedChunkIds { get; set; }
    }
}
