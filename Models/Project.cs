namespace VectorRagDemo.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string Naam { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation property
        public ICollection<Bron> Bronnen { get; set; } = new List<Bron>();
    }
}