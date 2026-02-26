namespace VectorRagDemo.Models.Entities
{
    public class Folder
    {
        public int ID { get; set; }
        public string Naam { get; set; } = string.Empty;
        public int Project { get; set; }
        public int? ParentId { get; set; }
        public DateTime GemaaktOp { get; set; }
        public int Status { get; set; }

        public Project? ProjectNavigation { get; set; }
        public Folder? Parent { get; set; }
        public ICollection<Folder> Children { get; set; } = new List<Folder>();
    }
}
