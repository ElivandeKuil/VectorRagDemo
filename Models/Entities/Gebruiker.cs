namespace VectorRagDemo.Models.Entities
{
    public class Gebruiker
    {
        public int ID { get; set; }
        public string Naam { get; set; } = string.Empty;
        public string GebruikersNaam { get; set; } = string.Empty;
        public string Wachtwoord { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }
        public bool WachtwoordWijzigen { get; set; }

        // Navigation property
        public ICollection<GebruikerRol> GebruikerRollen { get; set; } = new List<GebruikerRol>();
    }
}
