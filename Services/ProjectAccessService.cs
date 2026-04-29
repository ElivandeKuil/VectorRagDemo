using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities.Management;

namespace VectorRagDemo.Services
{
    /// <summary>
    /// Handles project/sub-project access queries and provides the static URL/icon
    /// mapping that links DB records to actual application routes.
    /// </summary>
    public class ProjectAccessService
    {
        private readonly ManagementDbContext _context;

        // Maps MgmtProject.Omschrijving → route info shown on the Dashboard card.
        // Add an entry here whenever a new top-level project is created in the DB.
        public static readonly Dictionary<string, ProjectInfo> ProjectMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["VectorRagDemo"] = new(
                    Url: "/Home/Index",
                    Icon: "bi-cpu",
                    Description: "Semantic search & RAG chat powered by SQL Server 2025 and Google Gemini."
                ),
                ["Gebruikersbeheer"] = new(
                    Url: "/Gebruiker/Index",
                    Icon: "bi-people",
                    Description: "Beheer gebruikers, rollen en toegangsrechten."
                ),
            };

        // Synthetic admin-only project injected into the dashboard; never stored in the DB.
        public static readonly MgmtProject GebruikersbeheerProject = new()
        {
            ID = -1,
            Omschrijving = "Gebruikersbeheer",
            Status = 1,
            GemaaktOp = DateTime.MinValue,
            GewijzigdOp = DateTime.MinValue
        };

        // Maps MgmtSubProject.Omschrijving → the controller/action that renders it.
        // Add an entry here when a new sub-project / personal page is created in the DB.
        public static readonly Dictionary<string, SubProjectInfo> SubProjectMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Chat"]               = new("Chat",               "Chat",        "Index",          "bi-chat-dots"),

                ["Chunks"]             = new("Documenten",         "Chunk",       "Index",          "bi-file-earmark-text"),
                ["Prompts"]            = new("Prompts",            "Prompts",     "Index",          "bi-chat-square-text"),
                ["API Logs"]           = new("API Logs",           "ApiCallLog",  "Index",          "bi-journal-code"),
                ["Widget instellingen"]   = new("Widget instellingen",   "Chat",               "WidgetSettings", "bi-sliders"),
                ["Statistieken"]          = new("Statistieken",          "Statistieken",        "Index",          "bi-bar-chart-line"),
                ["Correcties"]            = new("Correcties",            "Correcties",          "Index",          "bi-pencil-square"),
                ["Gesprekken"]            = new("Gesprekken",            "Conversaties",        "Index",          "bi-chat-left-text"),
                ["Woordenboek"]           = new("Woordenboek",           "Woordenboek",         "Index",          "bi-book"),
            };

        public ProjectAccessService(ManagementDbContext context)
        {
            _context = context;
        }

        /// <summary>Returns all active projects assigned to a user.</summary>
        public async Task<List<MgmtProject>> GetUserProjectsAsync(int userId)
        {
            return await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .Include(gp => gp.ProjectNavigation)
                .Where(gp => gp.ProjectNavigation.Status == 1)
                .Select(gp => gp.ProjectNavigation)
                .ToListAsync();
        }

        /// <summary>Returns all active projects (used for the Admin dashboard view).</summary>
        public async Task<List<MgmtProject>> GetAllActiveProjectsAsync()
        {
            return await _context.MgmtProjects
                .Where(p => p.Status == 1)
                .ToListAsync();
        }

        /// <summary>
        /// Returns the Omschrijving values of all sub-projects a user has access to.
        /// These are stored as claims at login so the layout can filter nav items
        /// without an extra DB round-trip on every request.
        /// </summary>
        public async Task<List<string>> GetUserSubProjectNamesAsync(int userId)
        {
            return await _context.GebruikerSubProjecten
                .Where(gsp => gsp.Gebruiker == userId && gsp.Status == 1)
                .Include(gsp => gsp.SubProjectNavigation)
                .Where(gsp => gsp.SubProjectNavigation.Status == 1)
                .Select(gsp => gsp.SubProjectNavigation.Omschrijving)
                .ToListAsync();
        }
    }

    public record ProjectInfo(string Url, string Icon, string Description);
    public record SubProjectInfo(string DisplayName, string Controller, string Action, string Icon);
}
