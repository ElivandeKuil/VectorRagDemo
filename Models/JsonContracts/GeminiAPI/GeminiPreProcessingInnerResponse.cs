using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiPreProcessingInnerResponse
    {
        [JsonProperty("ProcessedQuery")]
        public string ProcessedQuery { get; set; } = string.Empty;

    }
}
