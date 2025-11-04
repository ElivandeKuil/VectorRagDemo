using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class ApiCallLogViewModel
    {
        public List<ApiCallLog> Logs { get; set; } = new List<ApiCallLog>();

        // Filter properties
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? RequestUriFilter { get; set; }
        public int? StatusCodeFilter { get; set; }
        public bool? HasError { get; set; }
        public string? RequestMethodFilter { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Sorting
        public string SortBy { get; set; } = "GemaaktOp";
        public string SortOrder { get; set; } = "desc";
    }
}
