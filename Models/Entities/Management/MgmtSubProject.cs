namespace VectorRagDemo.Models.Entities.Management
{
    public class MgmtSubProject
    {
        public int ID { get; set; }
        public int Project { get; set; }
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }

        public MgmtProject ProjectNavigation { get; set; } = null!;
        public ICollection<GebruikerSubProject> GebruikerSubProjecten { get; set; } = new List<GebruikerSubProject>();
    }
}
