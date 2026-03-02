namespace VectorRagDemo.Models.Entities
{
    public class Bron
    {
        public int ID { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Project { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public int? FolderId { get; set; }
        public string? Link { get; set; }

        // Navigation properties
        public Project? ProjectNavigation { get; set; }
        public Folder? FolderNavigation { get; set; }
        public ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();
    }
}