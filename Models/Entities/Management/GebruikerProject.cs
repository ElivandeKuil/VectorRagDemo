using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.Entities.Management
{
    public class GebruikerProject
    {
        public int ID { get; set; }
        public int Gebruiker { get; set; }
        public int Project { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }

        public Gebruiker GebruikerNavigation { get; set; } = null!;
        public MgmtProject ProjectNavigation { get; set; } = null!;
    }
}
