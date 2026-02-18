namespace VectorRagDemo.Models.Entities.Management
{
    public class MgmtProject
    {
        public int ID { get; set; }
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }

        public ICollection<MgmtSubProject> SubProjects { get; set; } = new List<MgmtSubProject>();
        public ICollection<GebruikerProject> GebruikerProjecten { get; set; } = new List<GebruikerProject>();
    }
}
