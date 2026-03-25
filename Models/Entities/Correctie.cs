namespace VectorRagDemo.Models.Entities
{
    public class Correctie
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public int ConversatieID { get; set; }
        public int MessageID { get; set; }
        public string OrigineleTekst { get; set; } = string.Empty;
        public string GecontextualiseerdeVraag { get; set; } = string.Empty;
        public string Correctietekst { get; set; } = string.Empty;
        public int ChunkID { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }
    }
}
