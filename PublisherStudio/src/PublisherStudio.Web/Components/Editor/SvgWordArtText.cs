using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// Renders an SVG &lt;text&gt; element without using Razor's reserved &lt;text&gt;
/// pseudo-element syntax. This keeps WordArt markup compile-safe in .razor files.
/// </summary>
public sealed class SvgWordArtText : ComponentBase
{
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? X { get; set; }
    [Parameter] public string? Y { get; set; }
    [Parameter] public string? Dx { get; set; }
    [Parameter] public string? Dy { get; set; }
    [Parameter] public string? TextAnchor { get; set; }
    [Parameter] public string? DominantBaseline { get; set; }
    [Parameter] public string? FontFamily { get; set; }
    [Parameter] public string? FontSize { get; set; }
    [Parameter] public string? FontWeight { get; set; }
    [Parameter] public string? FontStyle { get; set; }
    [Parameter] public string? LetterSpacing { get; set; }
    [Parameter] public string? Fill { get; set; }
    [Parameter] public string? FillOpacity { get; set; }
    [Parameter] public string? Stroke { get; set; }
    [Parameter] public string? StrokeWidth { get; set; }
    [Parameter] public string? PaintOrder { get; set; }
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    [Parameter] public string? PathHref { get; set; }
    [Parameter] public string? PathStartOffset { get; set; }
    [Parameter] public string? PathTextAnchor { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        AddAttribute(builder, 1, "class", CssClass);
        AddAttribute(builder, 2, "x", X);
        AddAttribute(builder, 3, "y", Y);
        AddAttribute(builder, 4, "dx", Dx);
        AddAttribute(builder, 5, "dy", Dy);
        AddAttribute(builder, 6, "text-anchor", TextAnchor);
        AddAttribute(builder, 7, "dominant-baseline", DominantBaseline);
        AddAttribute(builder, 8, "font-family", FontFamily);
        AddAttribute(builder, 9, "font-size", FontSize);
        AddAttribute(builder, 10, "font-weight", FontWeight);
        AddAttribute(builder, 11, "font-style", FontStyle);
        AddAttribute(builder, 12, "letter-spacing", LetterSpacing);
        AddAttribute(builder, 13, "fill", Fill);
        AddAttribute(builder, 14, "fill-opacity", FillOpacity);
        AddAttribute(builder, 15, "stroke", Stroke);
        AddAttribute(builder, 16, "stroke-width", StrokeWidth);
        AddAttribute(builder, 17, "paint-order", PaintOrder);

        if (!string.IsNullOrWhiteSpace(PathHref))
        {
            builder.OpenElement(18, "textPath");
            AddAttribute(builder, 19, "href", PathHref);
            AddAttribute(builder, 20, "startOffset", PathStartOffset);
            AddAttribute(builder, 21, "text-anchor", PathTextAnchor);
            builder.AddContent(22, Text);
            builder.CloseElement();
        }
        else
        {
            builder.AddContent(23, Text);
        }

        builder.CloseElement();
    }

    private void AddAttribute(RenderTreeBuilder builder, int sequence, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) builder.AddAttribute(sequence, name, value);
    }
}
