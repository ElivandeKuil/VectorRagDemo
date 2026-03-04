namespace VectorRagDemo.Models.Entities.Management
{
    public class Conversation
    {
        public int ID { get; set; }
        public int Gebruiker { get; set; }
        public DateTime GemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int Status { get; set; }
    }
}
