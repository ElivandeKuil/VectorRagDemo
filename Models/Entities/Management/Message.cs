namespace VectorRagDemo.Models.Entities.Management
{
    public class Message
    {
        public int ID { get; set; }
        public int Conversation { get; set; }
        public string Tekst { get; set; } = string.Empty;
        public DateTime GemaaktOp { get; set; }
        public int SenderType { get; set; }
    }
}
