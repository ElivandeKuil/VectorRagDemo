using System.ComponentModel.DataAnnotations.Schema;

namespace VectorRagDemo.Models
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

        // Navigation properties
        public virtual Project ProjectNavigation { get; set; }
        public virtual PromptType PromptTypeNavigation { get; set; }
    }
}