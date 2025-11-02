namespace VectorRagDemo.Models.Entities
{
    public class Prompt
    {
        public int ID { get; set; }
        public int Project { get; set; }
        public int PromptType { get; set; }
        public string SystemInstruction { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }
        public string ResponseSchema { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int TopK { get; set; }

        // Navigation properties
        public virtual Project ProjectNavigation { get; set; }
        public virtual PromptType PromptTypeNavigation { get; set; }
    }
}