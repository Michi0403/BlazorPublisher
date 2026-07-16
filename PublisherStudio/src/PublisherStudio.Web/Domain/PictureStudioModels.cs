using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public sealed class PictureDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Picture";
    public string FormatVersion { get; set; } = "1.1";
    public int WidthPx { get; set; } = 1200;
    public int HeightPx { get; set; } = 800;
    public string Background { get; set; } = "transparent";
    public double Zoom { get; set; } = 0.65;
    public bool GridVisible { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;
    public int GridSpacingPx { get; set; } = 25;
    public List<PictureLayer> Layers { get; set; } = [];

    public static PictureDocument CreateDefault(int widthPx = 1200, int heightPx = 800, bool transparent = true)
    {
        return new PictureDocument
        {
            WidthPx = Math.Clamp(widthPx, 16, 8192),
            HeightPx = Math.Clamp(heightPx, 16, 8192),
            Background = transparent ? "transparent" : "#ffffff"
        };
    }

    public static PictureDocument FromRaster(string dataUrl, string name, int widthPx = 1200, int heightPx = 800)
    {
        var document = CreateDefault(widthPx, heightPx, true);
        document.Name = string.IsNullOrWhiteSpace(name) ? "Picture" : Path.GetFileNameWithoutExtension(name);
        document.Layers.Add(new RasterPictureLayer
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Picture" : name,
            DataUrl = dataUrl,
            X = 0,
            Y = 0,
            Width = document.WidthPx,
            Height = document.HeightPx,
            FitMode = PictureRasterFitMode.Contain
        });
        return document;
    }
}

public enum PictureLayerKind { Raster, Text, Shape, Fill, Render, Paint }
public enum PictureBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
public enum PictureRasterFitMode { Stretch, Contain, Cover }
public enum PictureShapeKind { Rectangle, RoundedRectangle, Ellipse, Line, Arrow, Freeform }
public enum PictureFillKind { Solid, LinearGradient, RadialGradient }
public enum PictureRenderKind { Clouds, Noise, Stripes, Vignette, Bloom, Neon, LensFlare, GrainNoise, MotionBlur, Wind, OceanWaves }
public enum PictureTextAlignment { Left, Center, Right }
public enum PictureDrawTool { Select, Brush, Pencil, Spray, Toothbrush, Square, Rectangle, Ellipse, Arrow, Line, Eraser, Eyedropper, RectangleSelect, EllipseSelect, FreeSelect, MagneticSelect, FillSolid, FillGradient }
public enum PictureStrokeKind { Brush, Pencil, Spray, Toothbrush, Line, Eraser }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RasterPictureLayer), "raster")]
[JsonDerivedType(typeof(TextPictureLayer), "text")]
[JsonDerivedType(typeof(ShapePictureLayer), "shape")]
[JsonDerivedType(typeof(FillPictureLayer), "fill")]
[JsonDerivedType(typeof(RenderPictureLayer), "render")]
[JsonDerivedType(typeof(PaintPictureLayer), "paint")]
public abstract class PictureLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Layer";
    public abstract PictureLayerKind Kind { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 240;
    public double Rotation { get; set; }
    public double Opacity { get; set; } = 1;
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public PictureBlendMode BlendMode { get; set; }

    // Non-destructive layer adjustments shared by raster, text, shape, fill, render and paint layers.
    public double Brightness { get; set; } = 1;
    public double Contrast { get; set; } = 1;
    public double Saturation { get; set; } = 1;
    public double HueRotation { get; set; }
    public double Blur { get; set; }
    public double Grayscale { get; set; }
    public double Sepia { get; set; }
    public double Invert { get; set; }
}

public sealed class RasterPictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Raster;
    public string DataUrl { get; set; } = string.Empty;
    public PictureRasterFitMode FitMode { get; set; } = PictureRasterFitMode.Contain;
    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }
    public string TintColor { get; set; } = "#2f75b5";
    public double TintOpacity { get; set; }
}

public sealed class TextPictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Text;
    public string Text { get; set; } = "Picture text";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSizePx { get; set; } = 72;
    public bool Bold { get; set; } = true;
    public bool Italic { get; set; }
    public PictureTextAlignment Alignment { get; set; } = PictureTextAlignment.Center;
    public string FillColor { get; set; } = "#17365d";
    public string OutlineColor { get; set; } = "transparent";
    public double OutlineWidthPx { get; set; }
    public bool ShadowEnabled { get; set; }
    public string ShadowColor { get; set; } = "#00000080";
    public double ShadowBlurPx { get; set; } = 8;
    public double ShadowOffsetXPx { get; set; } = 5;
    public double ShadowOffsetYPx { get; set; } = 6;
}

public sealed class ShapePictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Shape;
    public PictureShapeKind Shape { get; set; } = PictureShapeKind.Rectangle;
    public PictureFillKind FillKind { get; set; } = PictureFillKind.Solid;
    public string FillColor { get; set; } = "#60a5fa";
    public string SecondaryFillColor { get; set; } = "#ffffff";
    public double FillAngleDegrees { get; set; } = 45;
    public string StrokeColor { get; set; } = "#1d4ed8";
    public double StrokeWidthPx { get; set; } = 3;
    public double CornerRadiusPx { get; set; } = 24;
    public List<PicturePoint> PathPoints { get; set; } = [];
}

public sealed class FillPictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Fill;
    public PictureFillKind FillKind { get; set; } = PictureFillKind.LinearGradient;
    public string PrimaryColor { get; set; } = "#dbeafe";
    public string SecondaryColor { get; set; } = "#6366f1";
    public double AngleDegrees { get; set; } = 45;
}

public sealed class RenderPictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Render;
    public PictureRenderKind RenderKind { get; set; } = PictureRenderKind.Clouds;
    public string PrimaryColor { get; set; } = "#ffffff";
    public string SecondaryColor { get; set; } = "#60a5fa";
    public int Seed { get; set; } = 17;
    public double Scale { get; set; } = 90;
    public int Detail { get; set; } = 4;
    public double Softness { get; set; } = 0.6;
    public double RenderContrast { get; set; } = 1;
    public double AngleDegrees { get; set; } = 45;
    public double StripeWidthPx { get; set; } = 32;
}

public sealed class PaintPictureLayer : PictureLayer
{
    public override PictureLayerKind Kind => PictureLayerKind.Paint;
    public List<PictureStroke> Strokes { get; set; } = [];
}

public sealed class PictureStroke
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PictureStrokeKind Kind { get; set; } = PictureStrokeKind.Brush;
    public string Color { get; set; } = "#111827";
    public double WidthPx { get; set; } = 12;
    public double Opacity { get; set; } = 1;
    public double Hardness { get; set; } = 0.8;
    public List<PicturePoint> Points { get; set; } = [];
}

public sealed class PicturePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed record PictureEditorResult(string DataUrl, PictureDocument SourceDocument, string Name);
