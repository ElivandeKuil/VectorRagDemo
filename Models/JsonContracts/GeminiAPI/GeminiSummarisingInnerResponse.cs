using Newtonsoft.Json;

namespace VectorRagDemo.Models.ApiContracts.GeminiAPI
{
    public class GeminiSummarisingInnerResponse
    {
        [JsonProperty("relevantOutput")]
        public string RelevantOutput { get; set; }

        [JsonProperty("usedChunkIds")]
        public List<int>? UsedChunkIds { get; set; }

        /// <summary>
        /// Only populated when escalation was injected into the summarization prompt.
        /// Indicates the bot should hand off to WhatsApp/email.
        /// </summary>
        [JsonProperty("transferToWhatsApp")]
        public bool TransferToWhatsApp { get; set; }
    }
}
