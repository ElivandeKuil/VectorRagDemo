namespace VectorRagDemo.Models.JsonContracts.GeminiAPI
{
    public class GenerativeModelResponse
    {
        public string ResponseText { get; set; }

        public string SourceText { get; set; }

        public List<int> UsedChunkIds { get; set; } = new();

        /// <summary>Forwarded from the Gemini response when the bot wants to show the WhatsApp button.</summary>
        public bool TransferToWhatsApp { get; set; }
    }
}
