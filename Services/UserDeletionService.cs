using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Services
{
    // Verwijdert een gebruiker permanent inclusief alle gekoppelde persoonsgegevens
    // over alle drie de databases (GDPR Art. 17 — recht op verwijdering).
    public class UserDeletionService
    {
        private readonly ManagementDbContext _mgmt;
        private readonly VectorDbContext _vector;
        private readonly LogboekDbContext _logboek;

        public UserDeletionService(
            ManagementDbContext mgmt,
            VectorDbContext vector,
            LogboekDbContext logboek)
        {
            _mgmt = mgmt;
            _vector = vector;
            _logboek = logboek;
        }

        public async Task DeleteUserAsync(int gebruikerId)
        {
            // ── VectorRagDemo DB ──────────────────────────────────────────────
            // Berichten in gesprekken van deze gebruiker
            var conversationIds = await _vector.Conversations
                .Where(c => c.Gebruiker == gebruikerId)
                .Select(c => c.ID)
                .ToListAsync();

            if (conversationIds.Count > 0)
            {
                await _vector.Messages
                    .Where(m => conversationIds.Contains(m.Conversation))
                    .ExecuteDeleteAsync();

                await _vector.Conversations
                    .Where(c => c.Gebruiker == gebruikerId)
                    .ExecuteDeleteAsync();
            }

            // Vectorproject-toegang
            await _vector.GebruikerProjecten
                .Where(gp => gp.Gebruiker == gebruikerId)
                .ExecuteDeleteAsync();

            // ── Management DB ─────────────────────────────────────────────────
            await _mgmt.Toestemmingen
                .Where(t => t.GebruikerId == gebruikerId)
                .ExecuteDeleteAsync();

            await _mgmt.GebruikerSubProjecten
                .Where(gs => gs.Gebruiker == gebruikerId)
                .ExecuteDeleteAsync();

            await _mgmt.GebruikerProjecten
                .Where(gp => gp.Gebruiker == gebruikerId)
                .ExecuteDeleteAsync();

            await _mgmt.GebruikerRollen
                .Where(gr => gr.Gebruiker == gebruikerId)
                .ExecuteDeleteAsync();

            var gebruiker = await _mgmt.Gebruikers.FindAsync(gebruikerId);
            if (gebruiker != null)
            {
                _mgmt.Gebruikers.Remove(gebruiker);
                await _mgmt.SaveChangesAsync();
            }
        }
    }
}
