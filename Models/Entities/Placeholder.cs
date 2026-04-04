namespace VectorRagDemo.Models.Entities
{
    public class Placeholder
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string Naam { get; set; } = string.Empty;
        public string Waarde { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        public Project? ProjectNavigation { get; set; }
    }
}
