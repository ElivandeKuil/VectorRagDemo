namespace VectorRagDemo.Models.ViewModels
{
    public class DocumentViewModel
    {
        public int BronId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public string ProjectNaam { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int ChunkCount { get; set; }
        public DateTime GemaaktOp { get; set; }
    }
}
