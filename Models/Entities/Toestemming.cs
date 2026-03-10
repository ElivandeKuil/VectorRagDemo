namespace VectorRagDemo.Models.Entities
{
    public class Toestemming
    {
        public int ID { get; set; }
        public int GebruikerId { get; set; }

        // Versie van de privacyverklaring waarvoor toestemming is gegeven.
        // Bump Config.ConsentVersion als de privacyverklaring wijzigt om opnieuw toestemming te vragen.
        public string Versie { get; set; } = string.Empty;

        public DateTime GegeverOp { get; set; }

        // Geanonimiseerd IP (laatste octet gemaskeerd)
        public string? GeanonimiseerdIp { get; set; }

        public string? UserAgent { get; set; }

        // false = toestemming ingetrokken
        public bool Actief { get; set; } = true;

        public DateTime? IngetrokkenOp { get; set; }
    }
}
