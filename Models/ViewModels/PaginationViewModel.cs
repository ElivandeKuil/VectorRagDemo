namespace VectorRagDemo.Models.ViewModels;

public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }

    /// <summary>The action name used for page links (e.g. "Index").</summary>
    public string ActionName { get; set; } = "Index";

    /// <summary>
    /// Route values shared by all page links (filters, sort, page size, etc.).
    /// Do NOT include "CurrentPage" here — it is injected by the partial.
    /// </summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public string PreviousLabel { get; set; } = "Vorige";
    public string NextLabel { get; set; } = "Volgende";

    /// <summary>
    /// Format string for the record count line.
    /// Placeholders: {0} = current page, {1} = total pages, {2} = total records.
    /// </summary>
    public string SummaryFormat { get; set; } = "Pagina {0} van {1} ({2} regels)";
}
