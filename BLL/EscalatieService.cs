using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Business logic for escalation channels (WhatsApp / e-mail handoff).
    /// </summary>
    public class EscalatieService
    {
        private readonly VectorDbContext _context;

        public EscalatieService(VectorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Loads the project's WidgetConfig and returns whether any escalation channel is
        /// active and fully configured. Returns false immediately when ExtraCommunicationEnabled
        /// is off — avoids the extra DB round-trip in that case.
        /// </summary>
        public async Task<bool> HeeftEscalatieAsync(Project project)
        {
            if (!project.ExtraCommunicationEnabled) return false;

            await _context.Entry(project).Reference(p => p.WidgetConfig).LoadAsync();
            var cfg = project.WidgetConfig;
            return cfg != null &&
                   ((cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber)) ||
                    (cfg.EmailEnabled    && !string.IsNullOrWhiteSpace(cfg.EmailAddress)));
        }

        /// <summary>
        /// Builds the escalation URLs to include in a chat response.
        /// Returns (null, null) when the bot did not signal escalation or no channels are
        /// configured.
        /// </summary>
        public static (string? WhatsAppUrl, string? EmailUrl) BouwEscalatieUrls(
            Project? project, WidgetConfig? cfg, bool botSignalled)
        {
            if (!botSignalled || project == null || !project.ExtraCommunicationEnabled || cfg == null)
                return (null, null);

            string? whatsAppUrl = null;
            if (cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber))
            {
                var number = cfg.WhatsAppNumber.TrimStart('+').Replace(" ", "");
                whatsAppUrl = $"https://wa.me/{number}";
            }

            string? emailUrl = null;
            if (cfg.EmailEnabled && !string.IsNullOrWhiteSpace(cfg.EmailAddress))
            {
                var subject = string.IsNullOrWhiteSpace(cfg.EmailSubject)
                    ? string.Empty
                    : $"?subject={Uri.EscapeDataString(cfg.EmailSubject)}";
                emailUrl = $"mailto:{cfg.EmailAddress}{subject}";
            }

            return (whatsAppUrl, emailUrl);
        }

        /// <summary>
        /// Returns true when ExtraCommunicationEnabled is on for the project and at least one
        /// channel (WhatsApp or e-mail) has valid settings in the widget config.
        /// </summary>
        public static bool HeeftActiefKanaal(Project? project, WidgetConfig? cfg)
        {
            if (project == null || !project.ExtraCommunicationEnabled || cfg == null) return false;
            bool hasWhatsapp = cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber);
            bool hasEmail    = cfg.EmailEnabled    && !string.IsNullOrWhiteSpace(cfg.EmailAddress);
            return hasWhatsapp || hasEmail;
        }
    }
}
