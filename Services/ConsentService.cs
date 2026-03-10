using Microsoft.EntityFrameworkCore;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Services
{
    public class ConsentService
    {
        private readonly ManagementDbContext _mgmtContext;

        public ConsentService(ManagementDbContext mgmtContext)
        {
            _mgmtContext = mgmtContext;
        }

        /// <summary>
        /// Geeft true terug als de gebruiker actieve toestemming heeft gegeven
        /// voor de huidige versie van de privacyverklaring.
        /// </summary>
        public async Task<bool> HasActiveConsentAsync(int gebruikerId)
        {
            return await _mgmtContext.Toestemmingen
                .AnyAsync(t => t.GebruikerId == gebruikerId
                            && t.Versie == Config.ConsentVersion
                            && t.Actief);
        }

        /// <summary>
        /// Slaat een nieuwe toestemmingsregistratie op.
        /// </summary>
        public async Task RecordConsentAsync(int gebruikerId, string? geanonimiseerdIp, string? userAgent)
        {
            var toestemming = new Toestemming
            {
                GebruikerId = gebruikerId,
                Versie = Config.ConsentVersion,
                GegeverOp = DateTime.UtcNow,
                GeanonimiseerdIp = geanonimiseerdIp,
                UserAgent = userAgent,
                Actief = true
            };

            _mgmtContext.Toestemmingen.Add(toestemming);
            await _mgmtContext.SaveChangesAsync();
        }

        /// <summary>
        /// Trekt alle actieve toestemmingen in voor de opgegeven gebruiker.
        /// </summary>
        public async Task RevokeConsentAsync(int gebruikerId)
        {
            var actieveToestemmingen = await _mgmtContext.Toestemmingen
                .Where(t => t.GebruikerId == gebruikerId && t.Actief)
                .ToListAsync();

            foreach (var t in actieveToestemmingen)
            {
                t.Actief = false;
                t.IngetrokkenOp = DateTime.UtcNow;
            }

            await _mgmtContext.SaveChangesAsync();
        }

        /// <summary>
        /// Geeft de toestemmingsgeschiedenis terug voor een gebruiker.
        /// </summary>
        public async Task<List<Toestemming>> GetConsentHistoryAsync(int gebruikerId)
        {
            return await _mgmtContext.Toestemmingen
                .Where(t => t.GebruikerId == gebruikerId)
                .OrderByDescending(t => t.GegeverOp)
                .ToListAsync();
        }
    }
}
