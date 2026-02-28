namespace VectorRagDemo.Models.Entities
{
    public class AppLog
    {
        public int ID { get; set; }
        public string Level { get; set; } = "Info";
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public DateTime GemaaktOp { get; set; }
    }
}
