using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// Renders an SVG &lt;text&gt; element without using Razor's reserved &lt;text&gt;
/// pseudo-element syntax. This keeps WordArt markup compile-safe in .razor files.
/// </summary>
public sealed class SvgWordArtText : ComponentBase
{
    /// <summary>
    /// Gets or sets CSS class.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    [Parameter] public string? X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    [Parameter] public string? Y { get; set; }
    /// <summary>
    /// Gets or sets DevExpress.
    /// </summary>
    [Parameter] public string? Dx { get; set; }
    /// <summary>
    /// Gets or sets dy.
    /// </summary>
    [Parameter] public string? Dy { get; set; }
    /// <summary>
    /// Gets or sets text anchor.
    /// </summary>
    [Parameter] public string? TextAnchor { get; set; }
    /// <summary>
    /// Gets or sets dominant baseline.
    /// </summary>
    [Parameter] public string? DominantBaseline { get; set; }
    /// <summary>
    /// Gets or sets font family.
    /// </summary>
    [Parameter] public string? FontFamily { get; set; }
    /// <summary>
    /// Gets or sets font size.
    /// </summary>
    [Parameter] public string? FontSize { get; set; }
    /// <summary>
    /// Gets or sets font weight.
    /// </summary>
    [Parameter] public string? FontWeight { get; set; }
    /// <summary>
    /// Gets or sets font style.
    /// </summary>
    [Parameter] public string? FontStyle { get; set; }
    /// <summary>
    /// Gets or sets letter spacing.
    /// </summary>
    [Parameter] public string? LetterSpacing { get; set; }
    /// <summary>
    /// Gets or sets fill.
    /// </summary>
    [Parameter] public string? Fill { get; set; }
    /// <summary>
    /// Gets or sets fill opacity.
    /// </summary>
    [Parameter] public string? FillOpacity { get; set; }
    /// <summary>
    /// Gets or sets stroke.
    /// </summary>
    [Parameter] public string? Stroke { get; set; }
    /// <summary>
    /// Gets or sets stroke width.
    /// </summary>
    [Parameter] public string? StrokeWidth { get; set; }
    /// <summary>
    /// Gets or sets paint order.
    /// </summary>
    [Parameter] public string? PaintOrder { get; set; }
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Builds render tree.
    /// </summary>
    [Parameter] public string? PathHref { get; set; }
    /// <summary>
    /// Gets or sets path start offset.
    /// </summary>
    [Parameter] public string? PathStartOffset { get; set; }
    /// <summary>
    /// Gets or sets path text anchor.
    /// </summary>
    [Parameter] public string? PathTextAnchor { get; set; }

    /// <summary>
    /// Builds render tree.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        if (!string.IsNullOrWhiteSpace(CssClass)) builder.AddAttribute(1, "class", CssClass);
        if (!string.IsNullOrWhiteSpace(X)) builder.AddAttribute(2, "x", X);
        if (!string.IsNullOrWhiteSpace(Y)) builder.AddAttribute(3, "y", Y);
        if (!string.IsNullOrWhiteSpace(Dx)) builder.AddAttribute(4, "dx", Dx);
        if (!string.IsNullOrWhiteSpace(Dy)) builder.AddAttribute(5, "dy", Dy);
        if (!string.IsNullOrWhiteSpace(TextAnchor)) builder.AddAttribute(6, "text-anchor", TextAnchor);
        if (!string.IsNullOrWhiteSpace(DominantBaseline)) builder.AddAttribute(7, "dominant-baseline", DominantBaseline);
        if (!string.IsNullOrWhiteSpace(FontFamily)) builder.AddAttribute(8, "font-family", FontFamily);
        if (!string.IsNullOrWhiteSpace(FontSize)) builder.AddAttribute(9, "font-size", FontSize);
        if (!string.IsNullOrWhiteSpace(FontWeight)) builder.AddAttribute(10, "font-weight", FontWeight);
        if (!string.IsNullOrWhiteSpace(FontStyle)) builder.AddAttribute(11, "font-style", FontStyle);
        if (!string.IsNullOrWhiteSpace(LetterSpacing)) builder.AddAttribute(12, "letter-spacing", LetterSpacing);
        if (!string.IsNullOrWhiteSpace(Fill)) builder.AddAttribute(13, "fill", Fill);
        if (!string.IsNullOrWhiteSpace(FillOpacity)) builder.AddAttribute(14, "fill-opacity", FillOpacity);
        if (!string.IsNullOrWhiteSpace(Stroke)) builder.AddAttribute(15, "stroke", Stroke);
        if (!string.IsNullOrWhiteSpace(StrokeWidth)) builder.AddAttribute(16, "stroke-width", StrokeWidth);
        if (!string.IsNullOrWhiteSpace(PaintOrder)) builder.AddAttribute(17, "paint-order", PaintOrder);

        if (!string.IsNullOrWhiteSpace(PathHref))
        {
            builder.OpenElement(18, "textPath");
            builder.AddAttribute(19, "href", PathHref);
            if (!string.IsNullOrWhiteSpace(PathStartOffset)) builder.AddAttribute(20, "startOffset", PathStartOffset);
            if (!string.IsNullOrWhiteSpace(PathTextAnchor)) builder.AddAttribute(21, "text-anchor", PathTextAnchor);
            builder.AddContent(22, Text);
            builder.CloseElement();
        }
        else
        {
            builder.AddContent(23, Text);
        }

        builder.CloseElement();
    }

}
