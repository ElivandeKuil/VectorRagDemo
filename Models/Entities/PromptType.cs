namespace VectorRagDemo.Models.Entities
{
    public class PromptType
    {
        public int ID { get; set; }
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation properties
        public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
    }
}