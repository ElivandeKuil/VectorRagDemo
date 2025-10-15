namespace VectorRagDemo.Models
{
    public class Neighbor
    {
        public int ChunkId { get; set; }
        public int BronId { get; set; }
        public string Tekst { get; set; }
        public string BronTitle { get; set; }
        public int ProjectId { get; set; }
        public float Distance { get; set; }
    }
}
