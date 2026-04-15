using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class ApiCallLogDetailViewModel
    {
        public ApiCallLog Log { get; set; } = null!;

        public string StatusBadgeClass => Log.ResponseStatus >= 200 && Log.ResponseStatus < 300 ? "bg-success" :
                                          Log.ResponseStatus >= 300 && Log.ResponseStatus < 400 ? "bg-warning" :
                                          Log.ResponseStatus >= 400 ? "bg-danger" : "bg-secondary";

        public string? DurationBadgeClass => Log.DurationMs.HasValue
            ? (Log.DurationMs.Value < 1000 ? "bg-success" :
               Log.DurationMs.Value < 3000 ? "bg-warning" : "bg-danger")
            : null;
    }
}
