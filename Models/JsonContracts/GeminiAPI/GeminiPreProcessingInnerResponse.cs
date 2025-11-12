using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiPreProcessingInnerResponse
    {
        [JsonProperty("Optimized query")]
        public string ProcessedQuery { get; set; } = string.Empty;

        [JsonProperty("Cosmetical advise")]
        public string CosmeticAdvise { get; set; } = string.Empty;

        [JsonProperty("Frame dimension advise")]
        public string DimensionAdvise { get; set; } = string.Empty;

        [JsonProperty("Passe-partout addvise")]
        public string ContrastAdvise { get; set;} = string.Empty;

    }
}
