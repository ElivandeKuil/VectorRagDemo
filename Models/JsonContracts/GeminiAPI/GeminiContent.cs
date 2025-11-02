using Newtonsoft.Json;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiContent
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}
