using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Services
{
    /// <summary>
    /// Tracks which project (dev or prod) the current user is working in.
    /// The selection is stored in a long-lived cookie so it persists across page navigations.
    /// </summary>
    public class OmgevingService
    {
        private const string CookieName = "elai_proj";
        private readonly IHttpContextAccessor _accessor;
        private readonly VectorDbContext _context;

        public OmgevingService(IHttpContextAccessor accessor, VectorDbContext context)
        {
            _accessor = accessor;
            _context  = context;
        }

        // ── Read / write ──────────────────────────────────────────────────────

        public int GetActiveProjectId()
        {
            var val = _accessor.HttpContext?.Request.Cookies[CookieName];
            return int.TryParse(val, out var id) && id > 0 ? id : 0;
        }

        public void SetActiveProjectId(int projectId)
        {
            _accessor.HttpContext?.Response.Cookies.Append(CookieName, projectId.ToString(), new CookieOptions
            {
                HttpOnly  = true,
                SameSite  = SameSiteMode.Lax,
                Expires   = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        // ── Resolution helpers ────────────────────────────────────────────────

        /// <summary>
        /// For admins: returns the stored ID if present, otherwise 0.
        /// Optionally updates the cookie when an explicit override is provided (e.g. from a URL param).
        /// </summary>
        public int ResolveForAdmin(int explicitId = 0)
        {
            if (explicitId > 0)
            {
                SetActiveProjectId(explicitId);
                return explicitId;
            }
            return GetActiveProjectId();
        }

        /// <summary>
        /// For regular users: returns the stored ID if valid for this user,
        /// otherwise falls back to their assigned dev project and stores that.
        /// </summary>
        public async Task<int> ResolveForUserAsync(int userId)
        {
            var storedId = GetActiveProjectId();
            if (storedId > 0)
            {
                // Verify the stored project is still accessible to this user
                if (await IsAccessibleForUserAsync(userId, storedId))
                    return storedId;
            }

            // Fall back to assigned dev project
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry != null)
            {
                SetActiveProjectId(entry.Project);
                return entry.Project;
            }
            return 0;
        }

        /// <summary>
        /// Returns the dev/prod pairing for a user's assigned project.
        /// DevId is their assigned project; ProdId is non-null when a production twin is configured.
        /// </summary>
        public async Task<(int DevId, int? ProdId)> GetUserPairingAsync(int userId)
        {
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry == null) return (0, null);

            var devProject = await _context.Projects.FindAsync(entry.Project);
            return (devProject?.ID ?? 0, devProject?.ProductieProjectId);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<bool> IsAccessibleForUserAsync(int userId, int projectId)
        {
            // Directly assigned
            var directAssignment = await _context.GebruikerProjecten
                .AnyAsync(gp => gp.Gebruiker == userId && gp.Project == projectId && gp.Status == 1);
            if (directAssignment) return true;

            // Or it's the production twin of the user's dev project
            return await _context.Projects
                .AnyAsync(p => p.ProductieProjectId == projectId &&
                               _context.GebruikerProjecten.Any(gp => gp.Gebruiker == userId && gp.Project == p.ID && gp.Status == 1));
        }
    }
}
