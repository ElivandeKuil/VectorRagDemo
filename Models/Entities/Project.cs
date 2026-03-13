namespace VectorRagDemo.Models.Entities
{
    public class Project
    {
        public int ID { get; set; }
        public string Naam { get; set; } = string.Empty;

        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }
        public string BotName { get; set; } = "Assistant";
        public string EmbedKey { get; set; } = string.Empty;

        // Navigation properties
        public WidgetConfig? WidgetConfig { get; set; }
        public ICollection<Bron> Bronnen { get; set; } = new List<Bron>();

        public ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
    }
}