namespace VectorRagDemo.Models.Entities.Management
{
    public class Conversation
    {
        public int ID { get; set; }

        // null voor widget-gesprekken (anonieme bezoekers)
        // gevuld voor studio-gesprekken (ingelogde gebruikers)
        public int? Gebruiker { get; set; }

        // UUID voor widget-gesprekken — enige koppeling naar de bezoeker
        // null voor studio-gesprekken
        public string? SessionToken { get; set; }

        // 'studio' = ingelogde gebruiker | 'widget' = anonieme websitebezoeker
        public string BronType { get; set; } = "studio";

        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }
    }
}
