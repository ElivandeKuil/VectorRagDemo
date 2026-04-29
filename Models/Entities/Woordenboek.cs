namespace VectorRagDemo.Models.Entities
{
    public class Woordenboek
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string Woord { get; set; } = string.Empty;
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        public Project? ProjectNavigation { get; set; }
    }
}
