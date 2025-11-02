using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }
    }
}
