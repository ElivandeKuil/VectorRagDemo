using Newtonsoft.Json;
using VectorRagDemo.BLL;

namespace VectorRagDemo.Models
{
    public class GeminiContent
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}
