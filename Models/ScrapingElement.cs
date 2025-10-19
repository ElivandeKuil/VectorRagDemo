namespace VectorRagDemo.Models
{
    public class ScrapingElement
    {
        public int ID { get; set; }
        public int Project { get; set; }
        public string? ElementName { get; set; }
        public string? Selector { get; set; }
        public string? AttributeName { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public int SortOrder { get; set; }
        public DateTime? GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation properties
        public Project? ProjectNavigation { get; set; }

    }
}
