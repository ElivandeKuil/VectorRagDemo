using Microsoft.AspNetCore.Mvc.Rendering;

namespace VectorRagDemo.Models.ViewModels
{
    public class ChatIndexViewModel
    {
        public bool HasProject { get; set; }
        public int SelectedProjectId { get; set; }
        public string? EmbedKey { get; set; }
        public string BotName { get; set; } = "Assistant";
        public SelectList? AdminProjects { get; set; }

        public bool HasWidget => !string.IsNullOrWhiteSpace(EmbedKey);
    }
}
