using Microsoft.AspNetCore.Razor.TagHelpers;

namespace VectorRagDemo.TagHelpers;

/// <summary>
/// Augments &lt;input&gt;, &lt;textarea&gt;, and &lt;select&gt; elements with the correct
/// Bootstrap form class. The attribute value optionally sets a size modifier (sm / lg).
/// Writing the attribute without a value (e.g. <c>app-field</c>) applies the default size.
///
/// Mapping:
///   input (most types)   → form-control  [form-control-sm / form-control-lg]
///   input type=checkbox  → form-check-input
///   input type=radio     → form-check-input
///   input type=range     → form-range
///   input type=color     → form-control form-control-color
///   textarea             → form-control  [form-control-sm / form-control-lg]
///   select               → form-select   [form-select-sm  / form-select-lg ]
///
/// Usage:
///   &lt;input  asp-for="Name"          app-field /&gt;
///   &lt;input  asp-for="Age"           app-field="lg" /&gt;
///   &lt;input  type="checkbox"         app-field asp-for="IsActive" /&gt;
///   &lt;textarea asp-for="Content"     app-field rows="8"&gt;&lt;/textarea&gt;
///   &lt;select asp-for="Type" asp-items="..." app-field&gt;&lt;/select&gt;
///   &lt;select name="size"             app-field="sm" class="w-auto"&gt;&lt;/select&gt;
/// </summary>
[HtmlTargetElement("input",    Attributes = "app-field")]
[HtmlTargetElement("textarea", Attributes = "app-field")]
[HtmlTargetElement("select",   Attributes = "app-field")]
public class AppFieldTagHelper : TagHelper
{
    // Run after the built-in InputTagHelper so the resolved `type` attribute is available.
    public override int Order => int.MaxValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Read directly from context so the attribute can be used with or without a value.
        var size = context.AllAttributes["app-field"]?.Value?.ToString() ?? "";
        output.Attributes.RemoveAll("app-field");

        var tag  = output.TagName.ToLowerInvariant();
        var type = output.Attributes["type"]?.Value?.ToString()?.ToLowerInvariant() ?? "text";

        var baseClasses = (tag, type) switch
        {
            ("select",   _)                => WithSize("form-select",  "form-select",  size),
            ("textarea", _)                => WithSize("form-control", "form-control", size),
            (_, "checkbox" or "radio")     => "form-check-input",
            (_, "range")                   => "form-range",
            (_, "color")                   => "form-control form-control-color",
            _                              => WithSize("form-control", "form-control", size),
        };

        MergeClass(output, baseClasses);
    }

    private static string WithSize(string baseClass, string sizePrefix, string size) =>
        string.IsNullOrWhiteSpace(size) ? baseClass : $"{baseClass} {sizePrefix}-{size}";

    private static void MergeClass(TagHelperOutput output, string newClasses)
    {
        var classes = new List<string>(newClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (output.Attributes.ContainsName("class"))
        {
            var existing = output.Attributes["class"].Value?.ToString() ?? string.Empty;
            foreach (var c in existing.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                classes.Add(c);
            output.Attributes.RemoveAll("class");
        }

        output.Attributes.SetAttribute("class", string.Join(" ", classes));
    }
}

/// <summary>
/// Augments &lt;label&gt; elements with the correct Bootstrap label class.
/// Writing the attribute without a value applies <c>form-label</c>.
///
/// Usage:
///   &lt;label asp-for="Name"  app-label&gt;&lt;/label&gt;        → form-label
///   &lt;label for="chk"       app-label="check"&gt;&lt;/label&gt; → form-check-label
/// </summary>
[HtmlTargetElement("label", Attributes = "app-label")]
public class AppLabelTagHelper : TagHelper
{
    public override int Order => int.MaxValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var variant = context.AllAttributes["app-label"]?.Value?.ToString() ?? "";
        output.Attributes.RemoveAll("app-label");

        var cssClass = variant.Equals("check", StringComparison.OrdinalIgnoreCase)
            ? "form-check-label"
            : "form-label";

        var classes = new List<string> { cssClass };

        if (output.Attributes.ContainsName("class"))
        {
            var existing = output.Attributes["class"].Value?.ToString() ?? string.Empty;
            foreach (var c in existing.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                classes.Add(c);
            output.Attributes.RemoveAll("class");
        }

        output.Attributes.SetAttribute("class", string.Join(" ", classes));
    }
}
