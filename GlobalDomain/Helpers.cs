using VectorRagDemo.Models.DataContracts;

namespace VectorRagDemo.GlobalDomain
{
    static public class Helpers
    {
        public static string FormatChatHistory(List<ChatMessage> chatHistory)
        {
            return string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
