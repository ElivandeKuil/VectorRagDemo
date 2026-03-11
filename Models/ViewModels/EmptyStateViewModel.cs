namespace VectorRagDemo.Models.ViewModels;

public class EmptyStateViewModel
{
    /// <summary>Bootstrap Icons name without the "bi-" prefix.</summary>
    public string Icon { get; set; } = "inbox";

    public string Title { get; set; } = "Geen items gevonden";

    /// <summary>Optional subtitle shown below the title.</summary>
    public string? Subtitle { get; set; }

    /// <summary>URL for the primary call-to-action button. Leave null to hide the button.</summary>
    public string? ActionUrl { get; set; }

    /// <summary>Label text for the call-to-action button.</summary>
    public string? ActionLabel { get; set; }

    /// <summary>Bootstrap Icons name (without "bi-") for the button icon.</summary>
    public string? ActionIcon { get; set; }
}
