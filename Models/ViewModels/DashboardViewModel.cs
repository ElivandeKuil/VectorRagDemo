using VectorRagDemo.Services;

namespace VectorRagDemo.Models.ViewModels
{
    public class DashboardViewModel
    {
        public bool IsAdmin { get; set; }
        public string FullName { get; set; } = string.Empty;

        // Admin branch: project cards
        public IReadOnlyList<ProjectCardViewModel> ProjectCards { get; set; } = [];

        // Regular-user branch: pre-filtered app-launcher tiles
        public IReadOnlyList<SubProjectInfo> Tiles { get; set; } = [];
        public bool ShowPromptInstelling { get; set; }
    }
}
