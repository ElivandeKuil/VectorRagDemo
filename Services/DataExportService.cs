using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Services
{
    // Verzamelt alle persoonsgegevens van een gebruiker voor een DSAR-export
    // (GDPR Art. 15 — inzagerecht / Art. 20 — dataportabiliteit).
    public class DataExportService
    {
        private readonly ManagementDbContext _mgmt;
        private readonly VectorDbContext _vector;

        public DataExportService(ManagementDbContext mgmt, VectorDbContext vector)
        {
            _mgmt = mgmt;
            _vector = vector;
        }

        public async Task<object> BuildExportAsync(int gebruikerId)
        {
            var gebruiker = await _mgmt.Gebruikers.FindAsync(gebruikerId);
            if (gebruiker == null) throw new InvalidOperationException("Gebruiker niet gevonden.");

            var rollen = await _mgmt.GebruikerRollen
                .Where(gr => gr.Gebruiker == gebruikerId && gr.Status == 1)
                .Include(gr => gr.RolNavigation)
                .Select(gr => gr.RolNavigation.Omschrijving)
                .ToListAsync();

            var toestemmingen = await _mgmt.Toestemmingen
                .Where(t => t.GebruikerId == gebruikerId)
                .OrderByDescending(t => t.GegeverOp)
                .Select(t => new
                {
                    t.Versie,
                    GegeverOp = t.GegeverOp.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.Actief,
                    IngetrokkenOp = t.IngetrokkenOp.HasValue
                        ? t.IngetrokkenOp.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : null
                })
                .ToListAsync();

            var conversaties = await _vector.Conversations
                .Where(c => c.Gebruiker == gebruikerId)
                .OrderByDescending(c => c.GemaaktOp)
                .ToListAsync();

            var gesprekken = new List<object>();
            foreach (var conv in conversaties)
            {
                var berichten = await _vector.Messages
                    .Where(m => m.Conversation == conv.ID)
                    .OrderBy(m => m.GemaaktOp)
                    .Select(m => new
                    {
                        m.SenderType,
                        m.Tekst,
                        GemaaktOp = m.GemaaktOp.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToListAsync();

                gesprekken.Add(new
                {
                    ConversatieId = conv.ID,
                    AangemaaaktOp = conv.GemaaktOp.ToString("yyyy-MM-dd HH:mm:ss"),
                    Berichten = berichten
                });
            }

            return new
            {
                ExportDatum = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Accountgegevens = new
                {
                    gebruiker.ID,
                    gebruiker.Naam,
                    gebruiker.GebruikersNaam,
                    AangemaaaktOp = gebruiker.GemaaktOp.ToString("yyyy-MM-dd HH:mm:ss"),
                    LaatsteWijziging = gebruiker.GewijzigdOp?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = gebruiker.Status == 1 ? "Actief" : "Inactief"
                },
                Rollen = rollen,
                Toestemmingen = toestemmingen,
                Gesprekken = gesprekken
            };
        }
    }
}
