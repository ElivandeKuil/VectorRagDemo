namespace VectorRagDemo.Models.Entities
{
    public class GebruikerRol
    {
        public int ID { get; set; }
        public int Gebruiker { get; set; }
        public int Rol { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation properties
        public Gebruiker GebruikerNavigation { get; set; } = null!;
        public Rol RolNavigation { get; set; } = null!;
    }
}
