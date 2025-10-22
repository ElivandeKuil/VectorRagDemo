namespace VectorRagDemo.Models
{
    public class ScrapingUrlBlackList
    {
        public int ID { get; set; }
        public int Project { get; set; }
        public string BlackListElement { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation properties
        public Project? ProjectNavigation { get; set; }
    }
}
