using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }
    }
}
