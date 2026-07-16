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
        var sequence = 0;

        builder.OpenElement(sequence++, "text");
        AddAttribute(builder, ref sequence, "class", CssClass);
        AddAttribute(builder, ref sequence, "x", X);
        AddAttribute(builder, ref sequence, "y", Y);
        AddAttribute(builder, ref sequence, "dx", Dx);
        AddAttribute(builder, ref sequence, "dy", Dy);
        AddAttribute(builder, ref sequence, "text-anchor", TextAnchor);
        AddAttribute(builder, ref sequence, "dominant-baseline", DominantBaseline);
        AddAttribute(builder, ref sequence, "font-family", FontFamily);
        AddAttribute(builder, ref sequence, "font-size", FontSize);
        AddAttribute(builder, ref sequence, "font-weight", FontWeight);
        AddAttribute(builder, ref sequence, "font-style", FontStyle);
        AddAttribute(builder, ref sequence, "letter-spacing", LetterSpacing);
        AddAttribute(builder, ref sequence, "fill", Fill);
        AddAttribute(builder, ref sequence, "fill-opacity", FillOpacity);
        AddAttribute(builder, ref sequence, "stroke", Stroke);
        AddAttribute(builder, ref sequence, "stroke-width", StrokeWidth);
        AddAttribute(builder, ref sequence, "paint-order", PaintOrder);

        if (!string.IsNullOrWhiteSpace(PathHref))
        {
            builder.OpenElement(sequence++, "textPath");
            AddAttribute(builder, ref sequence, "href", PathHref);
            AddAttribute(builder, ref sequence, "startOffset", PathStartOffset);
            AddAttribute(builder, ref sequence, "text-anchor", PathTextAnchor);
            builder.AddContent(sequence++, Text);
            builder.CloseElement();
        }
        else
        {
            builder.AddContent(sequence++, Text);
        }

        builder.CloseElement();
    }

    private static void AddAttribute(
        RenderTreeBuilder builder,
        ref int sequence,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AddAttribute(sequence++, name, value);
        }
    }
}
