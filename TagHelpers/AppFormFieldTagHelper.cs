using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace VectorRagDemo.TagHelpers;

/// <summary>
/// Generates a complete form field group: wrapper div → label → child input → optional help text → validation message.
///
/// The actual input element is provided as child content, which keeps full flexibility
/// (any input type, textarea, select, input-group, etc.).
///
/// Usage:
///   &lt;app-field-group asp-for="Name"&gt;
///       &lt;input asp-for="Name" app-field /&gt;
///   &lt;/app-field-group&gt;
///
///   &lt;app-field-group asp-for="Content" help="Max 4000 tokens."&gt;
///       &lt;textarea asp-for="Content" app-field rows="8"&gt;&lt;/textarea&gt;
///   &lt;/app-field-group&gt;
///
///   &lt;app-field-group asp-for="Type" label="Prompt type"&gt;
///       &lt;select asp-for="Type" asp-items="ViewBag.Types" app-field&gt;&lt;/select&gt;
///   &lt;/app-field-group&gt;
///
/// Attributes:
///   asp-for   – Model expression (required for label + validation auto-generation).
///   label     – Override the label text (defaults to display name from model metadata).
///   help      – Optional help text rendered as &lt;div class="form-text"&gt;.
///   wrapper   – CSS class for the outer div (default: "mb-3").
/// </summary>
[HtmlTargetElement("app-field-group")]
public class AppFormFieldTagHelper : TagHelper
{
    private readonly IHtmlGenerator _generator;

    public AppFormFieldTagHelper(IHtmlGenerator generator) => _generator = generator;

    // Run after built-in helpers so asp-for on child elements is already resolved.
    public override int Order => int.MaxValue;

    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    /// <summary>Override the label text. Defaults to the model property's DisplayName.</summary>
    public string? Label { get; set; }

    /// <summary>Optional help text shown below the input as form-text.</summary>
    public string? Help { get; set; }

    /// <summary>CSS class(es) on the outer wrapper div.</summary>
    public string Wrapper { get; set; } = "mb-3";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", Wrapper);
        output.TagMode = TagMode.StartTagAndEndTag;

        var sb = new StringBuilder();

        // ── Label ────────────────────────────────────────────────────────────
        if (For != null)
        {
            var label = _generator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                Label,
                new { @class = "form-label" });

            sb.Append(Render(label));
        }
        else if (Label != null)
        {
            sb.Append($"<label class=\"form-label\">{HtmlEncoder.Default.Encode(Label)}</label>");
        }

        // ── Child content (the actual input) ─────────────────────────────────
        var children = await output.GetChildContentAsync();
        sb.Append(children.GetContent());

        // ── Optional help text ───────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(Help))
            sb.Append($"<div class=\"form-text\">{HtmlEncoder.Default.Encode(Help)}</div>");

        // ── Validation message ───────────────────────────────────────────────
        if (For != null)
        {
            var validation = _generator.GenerateValidationMessage(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                message: null,
                tag: null,
                htmlAttributes: new { @class = "text-danger small" });

            sb.Append(Render(validation));
        }

        output.Content.SetHtmlContent(sb.ToString());
    }

    private static string Render(IHtmlContent? content)
    {
        if (content == null) return string.Empty;
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
