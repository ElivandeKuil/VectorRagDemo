using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.DataContracts
{
    public class RetrievedChunk
    {
        public Entities.Chunk Chunk { get; set; }

        public int Freshness { get; set; }

        public double InitialSimilirityScore { get; set; }

        public string? BronTitle { get; set; }

        public string? BronLink { get; set; }
    }
}
