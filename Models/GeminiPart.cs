using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiPart
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("inlineData", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiInlineData InlineData { get; set; }
    }
}
