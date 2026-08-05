using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a picture document.
/// </summary>
public sealed class PictureDocument
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Untitled Picture";
    /// <summary>
    /// Gets or sets format version.
    /// </summary>
    public string FormatVersion { get; set; } = "1.4";
    /// <summary>
    /// Gets or sets width px.
    /// </summary>
    public int WidthPx { get; set; } = 1200;
    /// <summary>
    /// Gets or sets height px.
    /// </summary>
    public int HeightPx { get; set; } = 800;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets zoom.
    /// </summary>
    public double Zoom { get; set; } = 0.65;
    /// <summary>
    /// Gets or sets grid visible.
    /// </summary>
    public bool GridVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets snap to grid.
    /// </summary>
    public bool SnapToGrid { get; set; } = true;
    /// <summary>
    /// Gets or sets grid spacing px.
    /// </summary>
    public int GridSpacingPx { get; set; } = 25;
    /// <summary>
    /// Gets or sets layers.
    /// </summary>
    public List<PictureLayer> Layers { get; set; } = [];




}

/// <summary>
/// Lists supported picture layer kind values.
/// </summary>
public enum PictureLayerKind { Raster, Text, Shape, Fill, Render, Paint, Vector }
/// <summary>
/// Lists supported picture blend mode values.
/// </summary>
public enum PictureBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
/// <summary>
/// Lists supported picture raster fit mode values.
/// </summary>
public enum PictureRasterFitMode { Stretch, Contain, Cover }
/// <summary>
/// Lists supported picture shape kind values.
/// </summary>
public enum PictureShapeKind { Rectangle, RoundedRectangle, Ellipse, Line, Arrow, Freeform, Path }
/// <summary>
/// Lists supported picture fill kind values.
/// </summary>
public enum PictureFillKind { Solid, LinearGradient, RadialGradient }
/// <summary>
/// Lists supported picture render kind values.
/// </summary>
public enum PictureRenderKind { Clouds, Noise, Stripes, Vignette, Bloom, Neon, LensFlare, GrainNoise, MotionBlur, Wind, OceanWaves }
/// <summary>
/// Lists supported picture text alignment values.
/// </summary>
public enum PictureTextAlignment { Left, Center, Right }
/// <summary>
/// Lists supported picture draw tool values.
/// </summary>
public enum PictureDrawTool { Select, Brush, Pencil, Spray, Toothbrush, Square, Rectangle, Ellipse, Arrow, Line, Path, Eraser, Eyedropper, RectangleSelect, EllipseSelect, FreeSelect, MagneticSelect, PolygonSelect, FillSolid, FillGradient }
/// <summary>
/// Lists supported picture stroke kind values.
/// </summary>
public enum PictureStrokeKind { Brush, Pencil, Spray, Toothbrush, Line, Eraser }

/// <summary>
/// Represents a picture layer.
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
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Layer";
    /// <summary>
    /// Gets kind.
    /// </summary>
    public abstract PictureLayerKind Kind { get; }
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public double Width { get; set; } = 320;
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public double Height { get; set; } = 240;
    /// <summary>
    /// Gets or sets rotation.
    /// </summary>
    public double Rotation { get; set; }
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets whether the item is locked.
    /// </summary>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets blend mode.
    /// </summary>
    public PictureBlendMode BlendMode { get; set; }
    /// <summary>
    /// Gets or sets group path.
    /// </summary>
    public string GroupPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets clip polygon.
    /// </summary>
    public List<PicturePoint> ClipPolygon { get; set; } = [];
    /// <summary>
    /// Gets or sets clip inverted.
    /// </summary>
    public bool ClipInverted { get; set; }

    // Non-destructive layer adjustments shared by raster, text, shape, fill, render and paint layers.
    /// <summary>
    /// Gets or sets brightness.
    /// </summary>
    public double Brightness { get; set; } = 1;
    /// <summary>
    /// Gets or sets contrast.
    /// </summary>
    public double Contrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets saturation.
    /// </summary>
    public double Saturation { get; set; } = 1;
    /// <summary>
    /// Gets or sets hue rotation.
    /// </summary>
    public double HueRotation { get; set; }
    /// <summary>
    /// Gets or sets blur.
    /// </summary>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets grayscale.
    /// </summary>
    public double Grayscale { get; set; }
    /// <summary>
    /// Gets or sets sepia.
    /// </summary>
    public double Sepia { get; set; }
    /// <summary>
    /// Gets or sets invert.
    /// </summary>
    public double Invert { get; set; }
}

/// <summary>
/// Represents a raster picture layer.
/// </summary>
public sealed class RasterPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Raster;
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fit mode.
    /// </summary>
    public PictureRasterFitMode FitMode { get; set; } = PictureRasterFitMode.Contain;
    /// <summary>
    /// Gets or sets flip horizontal.
    /// </summary>
    public bool FlipHorizontal { get; set; }
    /// <summary>
    /// Gets or sets flip vertical.
    /// </summary>
    public bool FlipVertical { get; set; }
    /// <summary>
    /// Gets or sets tint color.
    /// </summary>
    public string TintColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets tint opacity.
    /// </summary>
    public double TintOpacity { get; set; }
}

/// <summary>
/// Represents a text picture layer.
/// </summary>
public sealed class TextPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Text;
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = "Picture text";
    /// <summary>
    /// Gets or sets font family.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";
    /// <summary>
    /// Gets or sets font size px.
    /// </summary>
    public double FontSizePx { get; set; } = 72;
    /// <summary>
    /// Gets or sets bold.
    /// </summary>
    public bool Bold { get; set; } = true;
    /// <summary>
    /// Gets or sets italic.
    /// </summary>
    public bool Italic { get; set; }
    /// <summary>
    /// Gets or sets alignment.
    /// </summary>
    public PictureTextAlignment Alignment { get; set; } = PictureTextAlignment.Center;
    /// <summary>
    /// Gets or sets fill color.
    /// </summary>
    public string FillColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets outline color.
    /// </summary>
    public string OutlineColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets outline width px.
    /// </summary>
    public double OutlineWidthPx { get; set; }
    /// <summary>
    /// Gets or sets shadow enabled.
    /// </summary>
    public bool ShadowEnabled { get; set; }
    /// <summary>
    /// Gets or sets shadow color.
    /// </summary>
    public string ShadowColor { get; set; } = "#00000080";
    /// <summary>
    /// Gets or sets shadow blur px.
    /// </summary>
    public double ShadowBlurPx { get; set; } = 8;
    /// <summary>
    /// Gets or sets shadow offset xpx.
    /// </summary>
    public double ShadowOffsetXPx { get; set; } = 5;
    /// <summary>
    /// Gets or sets shadow offset ypx.
    /// </summary>
    public double ShadowOffsetYPx { get; set; } = 6;
}

/// <summary>
/// Represents a shape picture layer.
/// </summary>
public sealed class ShapePictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Shape;
    /// <summary>
    /// Gets or sets shape.
    /// </summary>
    public PictureShapeKind Shape { get; set; } = PictureShapeKind.Rectangle;
    /// <summary>
    /// Gets or sets fill kind.
    /// </summary>
    public PictureFillKind FillKind { get; set; } = PictureFillKind.Solid;
    /// <summary>
    /// Gets or sets fill color.
    /// </summary>
    public string FillColor { get; set; } = "#60a5fa";
    /// <summary>
    /// Gets or sets secondary fill color.
    /// </summary>
    public string SecondaryFillColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets fill angle degrees.
    /// </summary>
    public double FillAngleDegrees { get; set; } = 45;
    /// <summary>
    /// Gets or sets stroke color.
    /// </summary>
    public string StrokeColor { get; set; } = "#1d4ed8";
    /// <summary>
    /// Gets or sets stroke width px.
    /// </summary>
    public double StrokeWidthPx { get; set; } = 3;
    /// <summary>
    /// Gets or sets corner radius px.
    /// </summary>
    public double CornerRadiusPx { get; set; } = 24;
    /// <summary>
    /// Gets or sets path points.
    /// </summary>
    public List<PicturePoint> PathPoints { get; set; } = [];
    /// <summary>
    /// Gets or sets path closed.
    /// </summary>
    public bool PathClosed { get; set; } = true;
    /// <summary>
    /// Gets or sets path smooth.
    /// </summary>
    public bool PathSmooth { get; set; }
}

/// <summary>
/// Represents a SVG picture layer.
/// </summary>
public sealed class SvgPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Vector;
    /// <summary>
    /// Gets or sets SVG markup.
    /// </summary>
    public string SvgMarkup { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source format.
    /// </summary>
    public string SourceFormat { get; set; } = "SVG";
    /// <summary>
    /// Gets or sets source element identifier.
    /// </summary>
    public string SourceElementId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preserve aspect ratio.
    /// </summary>
    public bool PreserveAspectRatio { get; set; } = true;
}

/// <summary>
/// Represents a fill picture layer.
/// </summary>
public sealed class FillPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Fill;
    /// <summary>
    /// Gets or sets fill kind.
    /// </summary>
    public PictureFillKind FillKind { get; set; } = PictureFillKind.LinearGradient;
    /// <summary>
    /// Gets or sets primary color.
    /// </summary>
    public string PrimaryColor { get; set; } = "#dbeafe";
    /// <summary>
    /// Gets or sets secondary color.
    /// </summary>
    public string SecondaryColor { get; set; } = "#6366f1";
    /// <summary>
    /// Gets or sets angle degrees.
    /// </summary>
    public double AngleDegrees { get; set; } = 45;
}

/// <summary>
/// Represents a render picture layer.
/// </summary>
public sealed class RenderPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Render;
    /// <summary>
    /// Gets or sets render kind.
    /// </summary>
    public PictureRenderKind RenderKind { get; set; } = PictureRenderKind.Clouds;
    /// <summary>
    /// Gets or sets primary color.
    /// </summary>
    public string PrimaryColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets secondary color.
    /// </summary>
    public string SecondaryColor { get; set; } = "#60a5fa";
    /// <summary>
    /// Gets or sets seed.
    /// </summary>
    public int Seed { get; set; } = 17;
    /// <summary>
    /// Gets or sets scale.
    /// </summary>
    public double Scale { get; set; } = 90;
    /// <summary>
    /// Gets or sets detail.
    /// </summary>
    public int Detail { get; set; } = 4;
    /// <summary>
    /// Gets or sets softness.
    /// </summary>
    public double Softness { get; set; } = 0.6;
    /// <summary>
    /// Gets or sets render contrast.
    /// </summary>
    public double RenderContrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets angle degrees.
    /// </summary>
    public double AngleDegrees { get; set; } = 45;
    /// <summary>
    /// Gets or sets stripe width px.
    /// </summary>
    public double StripeWidthPx { get; set; } = 32;
}

/// <summary>
/// Represents a paint picture layer.
/// </summary>
public sealed class PaintPictureLayer : PictureLayer
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PictureLayerKind Kind => PictureLayerKind.Paint;
    /// <summary>
    /// Gets or sets strokes.
    /// </summary>
    public List<PictureStroke> Strokes { get; set; } = [];
}

/// <summary>
/// Represents a picture stroke.
/// </summary>
public sealed class PictureStroke
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public PictureStrokeKind Kind { get; set; } = PictureStrokeKind.Brush;
    /// <summary>
    /// Gets or sets color.
    /// </summary>
    public string Color { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets width px.
    /// </summary>
    public double WidthPx { get; set; } = 12;
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets hardness.
    /// </summary>
    public double Hardness { get; set; } = 0.8;
    /// <summary>
    /// Gets or sets points.
    /// </summary>
    public List<PicturePoint> Points { get; set; } = [];
}

/// <summary>
/// Represents a picture point.
/// </summary>
public sealed class PicturePoint
{
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }
}

/// <summary>
/// Represents a picture editor result.
/// </summary>
public sealed record PictureEditorResult(string DataUrl, PictureDocument SourceDocument, string Name);
