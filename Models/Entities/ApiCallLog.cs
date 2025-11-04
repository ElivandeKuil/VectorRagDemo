namespace VectorRagDemo.Models.Entities
{
    public class ApiCallLog
    {
        public int ID { get; set; }
        public string RequestMethod { get; set; } = string.Empty;
        public string RequestUri { get; set; } = string.Empty;
        public string? RequestHeaders { get; set; }
        public string? RequestContent { get; set; }
        public int ResponseStatus { get; set; }
        public string? ResponseHeaders { get; set; }
        public string? ResponseContent { get; set; }
        public int? DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? CorrelationId { get; set; }
        public string? ClientIP { get; set; }
        public DateTime GemaaktOp { get; set; }
    }
}
