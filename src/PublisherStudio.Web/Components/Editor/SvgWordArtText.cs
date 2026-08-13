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
    /// Gets or sets the CSS class value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The CSS class value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? CssClass { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? Y { get; set; }
    /// <summary>
    /// Gets or sets the DevExpress value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? Dx { get; set; }
    /// <summary>
    /// Gets or sets the dy value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dy value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? Dy { get; set; }
    /// <summary>
    /// Gets or sets the text anchor value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text anchor value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? TextAnchor { get; set; }
    /// <summary>
    /// Gets or sets the dominant baseline value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dominant baseline value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? DominantBaseline { get; set; }
    /// <summary>
    /// Gets or sets the font family value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font family value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? FontFamily { get; set; }
    /// <summary>
    /// Gets or sets the font size that quantifies the associated SVG word art text data.
    /// </summary>
    /// <value>The font size value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? FontSize { get; set; }
    /// <summary>
    /// Gets or sets the font weight value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font weight value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? FontWeight { get; set; }
    /// <summary>
    /// Gets or sets the font style value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font style value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? FontStyle { get; set; }
    /// <summary>
    /// Gets or sets the letter spacing value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The letter spacing value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? LetterSpacing { get; set; }
    /// <summary>
    /// Gets or sets the fill value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? Fill { get; set; }
    /// <summary>
    /// Gets or sets the fill opacity value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill opacity value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? FillOpacity { get; set; }
    /// <summary>
    /// Gets or sets the stroke value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? Stroke { get; set; }
    /// <summary>
    /// Gets or sets the stroke width value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke width value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? StrokeWidth { get; set; }
    /// <summary>
    /// Gets or sets the paint order value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The paint order value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? PaintOrder { get; set; }
    /// <summary>
    /// Gets or sets the text value that forms part of the SVG word art text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path href used by this SVG word art text instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path href value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? PathHref { get; set; }
    /// <summary>
    /// Gets or sets the path start offset used by this SVG word art text instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path start offset value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? PathStartOffset { get; set; }
    /// <summary>
    /// Gets or sets the path text anchor used by this SVG word art text instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path text anchor value exposed by <see cref="SvgWordArtText"/>.</value>
    [Parameter] public string? PathTextAnchor { get; set; }

    /// <summary>
    /// Builds render tree for <see cref="SvgWordArtText"/>, keeping the operation consistent with the state and invariants of the surrounding SVG word art text workflow.
    /// </summary>
    /// <param name="builder">Builder value supplied to the SVG word art text operation and used when producing its result.</param>
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
