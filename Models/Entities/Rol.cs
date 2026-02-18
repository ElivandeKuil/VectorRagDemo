namespace VectorRagDemo.Models.Entities
{
    public class Rol
    {
        public int ID { get; set; }
        public string Omschrijving { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
        public int Status { get; set; }

        // Navigation property
        public ICollection<GebruikerRol> GebruikerRollen { get; set; } = new List<GebruikerRol>();
    }
}
