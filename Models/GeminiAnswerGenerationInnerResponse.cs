using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiAnswerGenerationInnerResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }
    }
}
