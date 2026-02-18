using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.Entities.Management
{
    public class GebruikerSubProject
    {
        public int ID { get; set; }
        public int Gebruiker { get; set; }
        public int SubProject { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }

        public Gebruiker GebruikerNavigation { get; set; } = null!;
        public MgmtSubProject SubProjectNavigation { get; set; } = null!;
    }
}
