using Newtonsoft.Json;
using VectorRagDemo.BLL;

namespace VectorRagDemo.Models
{
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }
    }
}
