using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.Models.ViewModels
{
    public class ChatPanelViewModel
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public List<string> RetrievedChunks { get; set; } = new List<string>();
        public string BotName { get; set; } = "Assistant";
    }
}
