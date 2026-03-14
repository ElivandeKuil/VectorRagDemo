namespace VectorRagDemo.Models.Entities
{
    public class PromptInstelling
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string? Persona { get; set; }
        public string? Instructies { get; set; }
        public string? Regels { get; set; }
        public string? Voorbeelden { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }

        // Navigation property
        public virtual Project ProjectNavigation { get; set; } = null!;
    }
}
