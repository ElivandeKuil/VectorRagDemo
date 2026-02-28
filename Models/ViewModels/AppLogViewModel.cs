using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class AppLogViewModel
    {
        public List<AppLog> Logs { get; set; } = new();

        // Filters
        public string? LevelFilter { get; set; }
        public string? SourceFilter { get; set; }
        public string? SearchText { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Sort
        public string SortBy { get; set; } = "GemaaktOp";
        public string SortOrder { get; set; } = "desc";
    }
}
