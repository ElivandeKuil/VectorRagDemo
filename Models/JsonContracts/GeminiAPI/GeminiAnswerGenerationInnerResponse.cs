using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiAnswerGenerationInnerResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("redirectUrl")]
        public string RedirectUrl { get; set; }
    }
}
