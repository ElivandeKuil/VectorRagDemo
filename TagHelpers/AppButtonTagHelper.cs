using Microsoft.AspNetCore.Razor.TagHelpers;

namespace VectorRagDemo.TagHelpers;

/// <summary>
/// Augments any element that carries <c>btn-variant</c> with the correct Bootstrap button
/// classes and an optional Bootstrap Icon prefix.
///
/// Usage:
///   <button type="submit" btn-variant="primary" btn-icon="check-lg">Opslaan</button>
///   <a asp-action="Index" btn-variant="outline-secondary" btn-size="sm" btn-icon="arrow-left">Terug</a>
///
/// btn-variant  : Bootstrap colour token with optional "outline-" prefix.
///                Examples: "primary", "danger", "outline-secondary", "outline-danger"
/// btn-size     : (optional) "sm" or "lg"
/// btn-icon     : (optional) Bootstrap Icons name WITHOUT the "bi-" prefix.
///                Examples: "check-lg", "trash", "arrow-left"
/// </summary>
[HtmlTargetElement(Attributes = "btn-variant")]
public class AppButtonTagHelper : TagHelper
{
    // Run after the built-in AnchorTagHelper so asp-action etc. are resolved first.
    public override int Order => int.MaxValue;

    [HtmlAttributeName("btn-variant")]
    public string Variant { get; set; } = "primary";

    [HtmlAttributeName("btn-size")]
    public string? Size { get; set; }

    [HtmlAttributeName("btn-icon")]
    public string? Icon { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Remove our custom attributes so they never reach the browser.
        output.Attributes.RemoveAll("btn-variant");
        output.Attributes.RemoveAll("btn-size");
        output.Attributes.RemoveAll("btn-icon");

        // Build the class list: btn + variant + optional size + existing classes.
        var classes = new List<string> { "btn", $"btn-{Variant}" };

        if (!string.IsNullOrWhiteSpace(Size))
            classes.Add($"btn-{Size}");

        if (output.Attributes.ContainsName("class"))
        {
            var existing = output.Attributes["class"].Value?.ToString() ?? string.Empty;
            foreach (var cls in existing.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                classes.Add(cls);

            output.Attributes.RemoveAll("class");
        }

        output.Attributes.SetAttribute("class", string.Join(" ", classes));

        // Prepend the Bootstrap icon when requested.
        if (!string.IsNullOrWhiteSpace(Icon))
        {
            var inner = (await output.GetChildContentAsync()).GetContent().Trim();
            var iconTag = $"<i class=\"bi bi-{Icon}\"></i>";

            output.Content.SetHtmlContent(
                string.IsNullOrEmpty(inner) ? iconTag : $"{iconTag} {inner}");
        }
    }
}
