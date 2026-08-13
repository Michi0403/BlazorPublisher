using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents picture state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class PictureDocument
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this picture instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PictureDocument"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PictureDocument"/>.</value>
    public string Name { get; set; } = "Untitled Picture";
    /// <summary>
    /// Gets or sets the format version value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format version value exposed by <see cref="PictureDocument"/>.</value>
    public string FormatVersion { get; set; } = "1.5";
    /// <summary>
    /// Gets or sets the width px value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width px value exposed by <see cref="PictureDocument"/>.</value>
    public int WidthPx { get; set; } = 1200;
    /// <summary>
    /// Gets or sets the height px value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height px value exposed by <see cref="PictureDocument"/>.</value>
    public int HeightPx { get; set; } = 800;
    /// <summary>
    /// Gets or sets the background value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="PictureDocument"/>.</value>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the zoom value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The zoom value exposed by <see cref="PictureDocument"/>.</value>
    public double Zoom { get; set; } = 0.65;
    /// <summary>
    /// Gets or sets a value indicating whether grid visible applies to the picture state.
    /// </summary>
    /// <value>The grid visible value exposed by <see cref="PictureDocument"/>.</value>
    public bool GridVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether snap to grid applies to the picture state.
    /// </summary>
    /// <value>The snap to grid value exposed by <see cref="PictureDocument"/>.</value>
    public bool SnapToGrid { get; set; } = true;
    /// <summary>
    /// Gets or sets the grid spacing px value that forms part of the picture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grid spacing px value exposed by <see cref="PictureDocument"/>.</value>
    public int GridSpacingPx { get; set; } = 25;
    /// <summary>
    /// Gets or sets the layers collection maintained or exposed by this picture instance for downstream processing.
    /// </summary>
    /// <value>The layers value exposed by <see cref="PictureDocument"/>.</value>
    public List<PictureLayer> Layers { get; set; } = [];




}

/// <summary>
/// Defines the supported picture layer kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureLayerKind { Raster, Text, Shape, Fill, Render, Paint, Vector }
/// <summary>
/// Defines the supported picture blend mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
/// <summary>
/// Defines the supported picture raster fit mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureRasterFitMode { Stretch, Contain, Cover }
/// <summary>Defines how a raster layer changes its source colors without destroying the embedded image.</summary>
public enum PictureRasterColorizeMode { None, ReplaceColor, Luminosity }
/// <summary>
/// Defines the supported picture shape kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureShapeKind { Rectangle, RoundedRectangle, Ellipse, Line, Arrow, Freeform, Path }
/// <summary>
/// Defines the supported picture fill kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureFillKind { Solid, LinearGradient, RadialGradient }
/// <summary>
/// Defines the supported picture render kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureRenderKind { Clouds, Noise, Stripes, Vignette, Bloom, Neon, LensFlare, GrainNoise, MotionBlur, Wind, OceanWaves }
/// <summary>
/// Defines the supported picture text alignment values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureTextAlignment { Left, Center, Right }
/// <summary>
/// Defines the supported picture draw tool values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureDrawTool { Select, Brush, Pencil, Spray, Toothbrush, BrushPath, PencilPath, SprayPath, ToothbrushPath, EraserPath, Square, Rectangle, Ellipse, Arrow, Line, Path, Eraser, Eyedropper, RectangleSelect, EllipseSelect, FreeSelect, MagneticSelect, PolygonSelect, FillSolid, FillGradient }
/// <summary>
/// Defines the supported picture stroke kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PictureStrokeKind { Brush, Pencil, Spray, Toothbrush, Line, Eraser }

/// <summary>
/// Represents a picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RasterPictureLayer), "raster")]
[JsonDerivedType(typeof(TextPictureLayer), "text")]
[JsonDerivedType(typeof(ShapePictureLayer), "shape")]
[JsonDerivedType(typeof(FillPictureLayer), "fill")]
[JsonDerivedType(typeof(RenderPictureLayer), "render")]
[JsonDerivedType(typeof(PaintPictureLayer), "paint")]
[JsonDerivedType(typeof(SvgPictureLayer), "svg")]
public abstract class PictureLayer
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this picture layer instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PictureLayer"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PictureLayer"/>.</value>
    public string Name { get; set; } = "Layer";
    /// <summary>
    /// Gets the kind value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PictureLayer"/>.</value>
    public abstract PictureLayerKind Kind { get; }
    /// <summary>
    /// Gets or sets the x value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="PictureLayer"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="PictureLayer"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PictureLayer"/>.</value>
    public double Width { get; set; } = 320;
    /// <summary>
    /// Gets or sets the height value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PictureLayer"/>.</value>
    public double Height { get; set; } = 240;
    /// <summary>
    /// Gets or sets the rotation value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rotation value exposed by <see cref="PictureLayer"/>.</value>
    public double Rotation { get; set; }
    /// <summary>
    /// Gets or sets the opacity value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="PictureLayer"/>.</value>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the picture layer state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="PictureLayer"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether locked applies to the picture layer state.
    /// </summary>
    /// <value>The locked value exposed by <see cref="PictureLayer"/>.</value>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets the blend mode value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blend mode value exposed by <see cref="PictureLayer"/>.</value>
    public PictureBlendMode BlendMode { get; set; }
    /// <summary>
    /// Gets or sets the group path used by this picture layer instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The group path value exposed by <see cref="PictureLayer"/>.</value>
    public string GroupPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the clip polygon collection maintained or exposed by this picture layer instance for downstream processing.
    /// </summary>
    /// <value>The clip polygon value exposed by <see cref="PictureLayer"/>.</value>
    public List<PicturePoint> ClipPolygon { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether clip inverted applies to the picture layer state.
    /// </summary>
    /// <value>The clip inverted value exposed by <see cref="PictureLayer"/>.</value>
    public bool ClipInverted { get; set; }

    // Non-destructive layer adjustments shared by raster, text, shape, fill, render and paint layers.
    /// <summary>
    /// Gets or sets the brightness value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brightness value exposed by <see cref="PictureLayer"/>.</value>
    public double Brightness { get; set; } = 1;
    /// <summary>
    /// Gets or sets the contrast value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contrast value exposed by <see cref="PictureLayer"/>.</value>
    public double Contrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets the saturation value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saturation value exposed by <see cref="PictureLayer"/>.</value>
    public double Saturation { get; set; } = 1;
    /// <summary>
    /// Gets or sets the hue rotation value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hue rotation value exposed by <see cref="PictureLayer"/>.</value>
    public double HueRotation { get; set; }
    /// <summary>
    /// Gets or sets the blur value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blur value exposed by <see cref="PictureLayer"/>.</value>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets the grayscale value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grayscale value exposed by <see cref="PictureLayer"/>.</value>
    public double Grayscale { get; set; }
    /// <summary>
    /// Gets or sets the sepia value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sepia value exposed by <see cref="PictureLayer"/>.</value>
    public double Sepia { get; set; }
    /// <summary>
    /// Gets or sets the invert value that forms part of the picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The invert value exposed by <see cref="PictureLayer"/>.</value>
    public double Invert { get; set; }
}

/// <summary>
/// Represents a raster picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RasterPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="RasterPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Raster;
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this raster picture layer state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="RasterPictureLayer"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fit mode value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fit mode value exposed by <see cref="RasterPictureLayer"/>.</value>
    public PictureRasterFitMode FitMode { get; set; } = PictureRasterFitMode.Contain;
    /// <summary>
    /// Gets or sets a value indicating whether flip horizontal applies to the raster picture layer state.
    /// </summary>
    /// <value>The flip horizontal value exposed by <see cref="RasterPictureLayer"/>.</value>
    public bool FlipHorizontal { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether flip vertical applies to the raster picture layer state.
    /// </summary>
    /// <value>The flip vertical value exposed by <see cref="RasterPictureLayer"/>.</value>
    public bool FlipVertical { get; set; }
    /// <summary>
    /// Gets or sets the tint color value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tint color value exposed by <see cref="RasterPictureLayer"/>.</value>
    public string TintColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets the tint opacity value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tint opacity value exposed by <see cref="RasterPictureLayer"/>.</value>
    public double TintOpacity { get; set; }
    /// <summary>
    /// Gets or sets the colorize mode value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The colorize mode value exposed by <see cref="RasterPictureLayer"/>.</value>
    public PictureRasterColorizeMode ColorizeMode { get; set; }
    /// <summary>Gets or sets the source color used by exact/near-color replacement.</summary>
    /// <value>The colorize source color value exposed by <see cref="RasterPictureLayer"/>.</value>
    public string ColorizeSourceColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the colorize target color value that forms part of the raster picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The colorize target color value exposed by <see cref="RasterPictureLayer"/>.</value>
    public string ColorizeTargetColor { get; set; } = "#dc2626";
    /// <summary>Gets or sets the RGB distance tolerance used by near-color replacement.</summary>
    /// <value>The colorize tolerance value exposed by <see cref="RasterPictureLayer"/>.</value>
    public int ColorizeTolerance { get; set; } = 48;
    /// <summary>Gets or sets how strongly the mapped color replaces the source color.</summary>
    /// <value>The colorize strength value exposed by <see cref="RasterPictureLayer"/>.</value>
    public double ColorizeStrength { get; set; } = 1;
}

/// <summary>
/// Represents a text picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class TextPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="TextPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Text;
    /// <summary>
    /// Gets or sets the text value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="TextPictureLayer"/>.</value>
    public string Text { get; set; } = "Picture text";
    /// <summary>
    /// Gets or sets the font family value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font family value exposed by <see cref="TextPictureLayer"/>.</value>
    public string FontFamily { get; set; } = "Segoe UI";
    /// <summary>
    /// Gets or sets the font size px value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font size px value exposed by <see cref="TextPictureLayer"/>.</value>
    public double FontSizePx { get; set; } = 72;
    /// <summary>
    /// Gets or sets a value indicating whether bold applies to the text picture layer state.
    /// </summary>
    /// <value>The bold value exposed by <see cref="TextPictureLayer"/>.</value>
    public bool Bold { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether italic applies to the text picture layer state.
    /// </summary>
    /// <value>The italic value exposed by <see cref="TextPictureLayer"/>.</value>
    public bool Italic { get; set; }
    /// <summary>
    /// Gets or sets the alignment value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The alignment value exposed by <see cref="TextPictureLayer"/>.</value>
    public PictureTextAlignment Alignment { get; set; } = PictureTextAlignment.Center;
    /// <summary>
    /// Gets or sets the fill color value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill color value exposed by <see cref="TextPictureLayer"/>.</value>
    public string FillColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets the outline color value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The outline color value exposed by <see cref="TextPictureLayer"/>.</value>
    public string OutlineColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the outline width px value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The outline width px value exposed by <see cref="TextPictureLayer"/>.</value>
    public double OutlineWidthPx { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether shadow enabled applies to the text picture layer state.
    /// </summary>
    /// <value>The shadow enabled value exposed by <see cref="TextPictureLayer"/>.</value>
    public bool ShadowEnabled { get; set; }
    /// <summary>
    /// Gets or sets the shadow color value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shadow color value exposed by <see cref="TextPictureLayer"/>.</value>
    public string ShadowColor { get; set; } = "#00000080";
    /// <summary>
    /// Gets or sets the shadow blur px value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shadow blur px value exposed by <see cref="TextPictureLayer"/>.</value>
    public double ShadowBlurPx { get; set; } = 8;
    /// <summary>
    /// Gets or sets the shadow offset x px value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shadow offset x px value exposed by <see cref="TextPictureLayer"/>.</value>
    public double ShadowOffsetXPx { get; set; } = 5;
    /// <summary>
    /// Gets or sets the shadow offset y px value that forms part of the text picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shadow offset y px value exposed by <see cref="TextPictureLayer"/>.</value>
    public double ShadowOffsetYPx { get; set; } = 6;
}

/// <summary>
/// Represents a shape picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ShapePictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ShapePictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Shape;
    /// <summary>
    /// Gets or sets the shape value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shape value exposed by <see cref="ShapePictureLayer"/>.</value>
    public PictureShapeKind Shape { get; set; } = PictureShapeKind.Rectangle;
    /// <summary>
    /// Gets or sets the fill kind value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill kind value exposed by <see cref="ShapePictureLayer"/>.</value>
    public PictureFillKind FillKind { get; set; } = PictureFillKind.Solid;
    /// <summary>
    /// Gets or sets the fill color value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill color value exposed by <see cref="ShapePictureLayer"/>.</value>
    public string FillColor { get; set; } = "#60a5fa";
    /// <summary>
    /// Gets or sets the secondary fill color value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secondary fill color value exposed by <see cref="ShapePictureLayer"/>.</value>
    public string SecondaryFillColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the fill angle degrees value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill angle degrees value exposed by <see cref="ShapePictureLayer"/>.</value>
    public double FillAngleDegrees { get; set; } = 45;
    /// <summary>
    /// Gets or sets the stroke color value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke color value exposed by <see cref="ShapePictureLayer"/>.</value>
    public string StrokeColor { get; set; } = "#1d4ed8";
    /// <summary>
    /// Gets or sets the stroke width px value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke width px value exposed by <see cref="ShapePictureLayer"/>.</value>
    public double StrokeWidthPx { get; set; } = 3;
    /// <summary>
    /// Gets or sets the corner radius px value that forms part of the shape picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The corner radius px value exposed by <see cref="ShapePictureLayer"/>.</value>
    public double CornerRadiusPx { get; set; } = 24;
    /// <summary>
    /// Gets or sets the path points used by this shape picture layer instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path points value exposed by <see cref="ShapePictureLayer"/>.</value>
    public List<PicturePoint> PathPoints { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether path closed applies to the shape picture layer state.
    /// </summary>
    /// <value>The path closed value exposed by <see cref="ShapePictureLayer"/>.</value>
    public bool PathClosed { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether path smooth applies to the shape picture layer state.
    /// </summary>
    /// <value>The path smooth value exposed by <see cref="ShapePictureLayer"/>.</value>
    public bool PathSmooth { get; set; }
}

/// <summary>
/// Represents a SVG picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SvgPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the SVG picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="SvgPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Vector;
    /// <summary>
    /// Gets or sets the SVG markup value that forms part of the SVG picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The SVG markup value exposed by <see cref="SvgPictureLayer"/>.</value>
    public string SvgMarkup { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source format value that forms part of the SVG picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format value exposed by <see cref="SvgPictureLayer"/>.</value>
    public string SourceFormat { get; set; } = "SVG";
    /// <summary>
    /// Gets or sets the stable source element identifier used to identify or correlate this SVG picture layer instance with related application state.
    /// </summary>
    /// <value>The source element identifier value exposed by <see cref="SvgPictureLayer"/>.</value>
    public string SourceElementId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether preserve aspect ratio applies to the SVG picture layer state.
    /// </summary>
    /// <value>The preserve aspect ratio value exposed by <see cref="SvgPictureLayer"/>.</value>
    public bool PreserveAspectRatio { get; set; } = true;
}

/// <summary>
/// Represents a fill picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class FillPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the fill picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="FillPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Fill;
    /// <summary>
    /// Gets or sets the fill kind value that forms part of the fill picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill kind value exposed by <see cref="FillPictureLayer"/>.</value>
    public PictureFillKind FillKind { get; set; } = PictureFillKind.LinearGradient;
    /// <summary>
    /// Gets or sets the primary color value that forms part of the fill picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The primary color value exposed by <see cref="FillPictureLayer"/>.</value>
    public string PrimaryColor { get; set; } = "#dbeafe";
    /// <summary>
    /// Gets or sets the secondary color value that forms part of the fill picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secondary color value exposed by <see cref="FillPictureLayer"/>.</value>
    public string SecondaryColor { get; set; } = "#6366f1";
    /// <summary>
    /// Gets or sets the angle degrees value that forms part of the fill picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The angle degrees value exposed by <see cref="FillPictureLayer"/>.</value>
    public double AngleDegrees { get; set; } = 45;
}

/// <summary>
/// Represents a render picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RenderPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="RenderPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Render;
    /// <summary>
    /// Gets or sets the render kind value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The render kind value exposed by <see cref="RenderPictureLayer"/>.</value>
    public PictureRenderKind RenderKind { get; set; } = PictureRenderKind.Clouds;
    /// <summary>
    /// Gets or sets the primary color value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The primary color value exposed by <see cref="RenderPictureLayer"/>.</value>
    public string PrimaryColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the secondary color value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secondary color value exposed by <see cref="RenderPictureLayer"/>.</value>
    public string SecondaryColor { get; set; } = "#60a5fa";
    /// <summary>
    /// Gets or sets the seed value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed value exposed by <see cref="RenderPictureLayer"/>.</value>
    public int Seed { get; set; } = 17;
    /// <summary>
    /// Gets or sets the scale value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scale value exposed by <see cref="RenderPictureLayer"/>.</value>
    public double Scale { get; set; } = 90;
    /// <summary>
    /// Gets or sets the detail value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The detail value exposed by <see cref="RenderPictureLayer"/>.</value>
    public int Detail { get; set; } = 4;
    /// <summary>
    /// Gets or sets the softness value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The softness value exposed by <see cref="RenderPictureLayer"/>.</value>
    public double Softness { get; set; } = 0.6;
    /// <summary>
    /// Gets or sets the render contrast value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The render contrast value exposed by <see cref="RenderPictureLayer"/>.</value>
    public double RenderContrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets the angle degrees value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The angle degrees value exposed by <see cref="RenderPictureLayer"/>.</value>
    public double AngleDegrees { get; set; } = 45;
    /// <summary>
    /// Gets or sets the stripe width px value that forms part of the render picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stripe width px value exposed by <see cref="RenderPictureLayer"/>.</value>
    public double StripeWidthPx { get; set; } = 32;
}

/// <summary>
/// Represents a paint picture layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PaintPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets the kind value that forms part of the paint picture layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PaintPictureLayer"/>.</value>
    public override PictureLayerKind Kind => PictureLayerKind.Paint;
    /// <summary>
    /// Gets or sets the strokes collection maintained or exposed by this paint picture layer instance for downstream processing.
    /// </summary>
    /// <value>The strokes value exposed by <see cref="PaintPictureLayer"/>.</value>
    public List<PictureStroke> Strokes { get; set; } = [];
}

/// <summary>
/// Represents a picture stroke application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PictureStroke
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this picture stroke instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PictureStroke"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the kind value that forms part of the picture stroke state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PictureStroke"/>.</value>
    public PictureStrokeKind Kind { get; set; } = PictureStrokeKind.Brush;
    /// <summary>
    /// Gets or sets the color value that forms part of the picture stroke state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="PictureStroke"/>.</value>
    public string Color { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets the width px value that forms part of the picture stroke state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width px value exposed by <see cref="PictureStroke"/>.</value>
    public double WidthPx { get; set; } = 12;
    /// <summary>
    /// Gets or sets the opacity value that forms part of the picture stroke state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="PictureStroke"/>.</value>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets the hardness value that forms part of the picture stroke state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardness value exposed by <see cref="PictureStroke"/>.</value>
    public double Hardness { get; set; } = 0.8;
    /// <summary>
    /// Gets or sets the points collection maintained or exposed by this picture stroke instance for downstream processing.
    /// </summary>
    /// <value>The points value exposed by <see cref="PictureStroke"/>.</value>
    public List<PicturePoint> Points { get; set; } = [];
}

/// <summary>
/// Represents a picture point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PicturePoint
{
    /// <summary>
    /// Gets or sets the x value that forms part of the picture point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="PicturePoint"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the picture point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="PicturePoint"/>.</value>
    public double Y { get; set; }
}

/// <summary>
/// Represents the outcome of picture editor, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="DataUrl">Data url value supplied to the picture editor operation and used when producing its result.</param>
/// <param name="SourceDocument">Source document value supplied to the picture editor operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the picture editor operation and used when producing its result.</param>
/// <param name="PreserveLayers">Whether the editable Picture Studio document remains attached to the Mainframe image after applying the rendered result.</param>
public sealed record PictureEditorResult(string DataUrl, PictureDocument? SourceDocument, string Name, bool PreserveLayers);
