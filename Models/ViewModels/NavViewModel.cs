using VectorRagDemo.Services;

namespace VectorRagDemo.Models.ViewModels
{
    public class NavViewModel
    {
        public bool IsAdmin { get; set; }
        public bool IsAuthenticated { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string SwitchReturnUrl { get; set; } = "/";

        // Regular-user nav items (pre-filtered)
        public IReadOnlyList<SubProjectInfo> NavItems { get; set; } = [];
        public bool ShowPromptInstelling { get; set; }

        // Admin environment switcher
        public IReadOnlyList<NavProjectItem> AllProjects { get; set; } = [];
        public int ActiveProjectId { get; set; }
        public string ActiveProjectName { get; set; } = "Geen project";

        // Regular-user dev/prod switcher (null = not shown)
        public DevProdSwitcherInfo? DevProdSwitcher { get; set; }
    }

    public record NavProjectItem(int ID, string Naam, bool IsDev, bool IsProd);
    public record DevProdSwitcherInfo(int DevProjectId, int ProdProjectId, bool IsDevActive);
}
