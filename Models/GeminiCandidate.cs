using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }
    }
}
