namespace VectorRagDemo.Models.Entities
{
    public class Chunk
    {
        public int ID { get; set; }
        public int BronID { get; set; }
        public string Tekst { get; set; } = string.Empty;
        public float[]? TekstVector { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation property
        public Bron? Bron { get; set; }
    }
}