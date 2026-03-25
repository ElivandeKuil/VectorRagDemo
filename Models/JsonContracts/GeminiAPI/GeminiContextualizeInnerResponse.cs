using Newtonsoft.Json;

namespace VectorRagDemo.Models.JsonContracts.GeminiAPI
{
    public class GeminiContextualizeInnerResponse
    {
        [JsonProperty("RewrittenQuestion")]
        public string RewrittenQuestion { get; set; } = string.Empty;
    }
}
