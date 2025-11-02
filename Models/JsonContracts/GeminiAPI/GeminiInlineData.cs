using Newtonsoft.Json;

namespace VectorRagDemo.Models.JsonContracts.GeminiAPI
{
    public class GeminiInlineData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
