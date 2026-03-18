namespace VectorRagDemo.Models.Entities
{
    public class EscalatieEvent
    {
        public int ID { get; set; }

        /// <summary>ID van het gesprek waarin de doorverwijzing plaatsvond.</summary>
        public int? ConversatieID { get; set; }

        /// <summary>Project-ID voor directe statistiekenquery's.</summary>
        public int? ProjectID { get; set; }

        /// <summary>Session-token van widget-gesprekken (null voor studio).</summary>
        public string? SessionToken { get; set; }

        /// <summary>"whatsapp" of "email"</summary>
        public string Kanaal { get; set; } = string.Empty;

        public DateTime GemaaktOp { get; set; }
    }
}
