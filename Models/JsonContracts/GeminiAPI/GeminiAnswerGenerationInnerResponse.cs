using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiAnswerGenerationInnerResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }

        /// <summary>
        /// When true the bot signals it cannot help further OR has detected a lead.
        /// The controller will show a WhatsApp button built from the project's widget config.
        /// </summary>
        [JsonProperty("transferToWhatsApp")]
        public bool TransferToWhatsApp { get; set; }
    }
}
