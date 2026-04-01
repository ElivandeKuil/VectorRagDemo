namespace VectorRagDemo.Models.Entities
{
    public class Project
    {
        public int ID { get; set; }
        public string Naam { get; set; } = string.Empty;

        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }
        public string BotName { get; set; } = "Assistant";
        public string EmbedKey { get; set; } = string.Empty;

        /// <summary>
        /// Wanneer true wordt de prompt tabel gebruikt (beheerder-geconfigureerde prompts).
        /// Wanneer false wordt de promptinstelling van de gebruiker gebruikt.
        /// Alleen zichtbaar/wijzigbaar voor admins.
        /// </summary>
        public bool GebruikPromptTabel { get; set; } = true;

        /// <summary>
        /// Admin-only master switch. When false the bot never shows any escalation button,
        /// regardless of the individual channel settings in WidgetConfig.
        /// </summary>
        public bool ExtraCommunicationEnabled { get; set; } = false;

        /// <summary>
        /// Statistieken-tier voor dit project.
        /// 0 = Uitgeschakeld | 1 = Basis | 2 = Uitgebreid
        /// Alleen zichtbaar/wijzigbaar voor admins.
        /// </summary>
        public int StatistiekenTier { get; set; } = 0;

        /// <summary>
        /// Wanneer true mogen gebruikers een URL-koppeling meegeven bij het invoeren of uploaden van tekst.
        /// </summary>
        public bool KoppelingIngeschakeld { get; set; } = false;

        /// <summary>
        /// Wanneer ingesteld is dit de ontwikkelomgeving; de waarde verwijst naar het gekoppelde productieproject.
        /// </summary>
        public int? ProductieProjectId { get; set; }

        // Navigation properties
        public WidgetConfig? WidgetConfig { get; set; }
        public ICollection<Bron> Bronnen { get; set; } = new List<Bron>();
        public ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
        public Project? ProductieProjectNavigation { get; set; }
    }
}