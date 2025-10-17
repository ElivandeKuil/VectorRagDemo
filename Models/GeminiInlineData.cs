using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiInlineData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
