using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public sealed class PublicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Publication";
    public string FormatVersion { get; set; } = "1.0";
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public double Zoom { get; set; } = 0.8;
    public List<PublicationPage> Pages { get; set; } = [];

    public static PublicationDocument CreateDefault()
    {
        var document = new PublicationDocument();
        var page = PublicationPage.CreateA4();
        page.Elements.Add(new TextFrameElement
        {
            Name = "Title",
            X = 20,
            Y = 22,
            Width = 170,
            Height = 34,
            PreviewHtml = "<h1 style=\"margin:0;font:700 28pt Segoe UI;color:#17365d\">Your publication</h1><p style=\"margin:8px 0 0;font:12pt Segoe UI;color:#475569\">Double-click this frame to edit it with DevExpress RichEdit.</p>",
            DocumentContent = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><h1>Your publication</h1><p>Double-click this frame to edit it with DevExpress RichEdit.</p></body></html>"),
            ZIndex = 2
        });
        page.Elements.Add(new ShapeElement
        {
            Name = "Accent",
            Shape = PublicationShape.Rectangle,
            X = 20,
            Y = 66,
            Width = 170,
            Height = 5,
            Fill = "#2f75b5",
            Stroke = "transparent",
            ZIndex = 1
        });
        document.Pages.Add(page);
        return document;
    }
}

public sealed class PublicationPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Page 1";
    public double WidthMm { get; set; } = 210;
    public double HeightMm { get; set; } = 297;
    public string Background { get; set; } = "#ffffff";
    public List<PublicationElement> Elements { get; set; } = [];
    public List<GuideLine> Guides { get; set; } = [];

    public static PublicationPage CreateA4(string name = "Page 1") => new() { Name = name };
}

public sealed class GuideLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GuideOrientation Orientation { get; set; }
    public double PositionMm { get; set; }
}

public enum GuideOrientation { Horizontal, Vertical }
public enum PublicationElementKind { Text, Image, Shape }
public enum PublicationShape { Rectangle, RoundedRectangle, Ellipse, Line }
public enum ImageMaskShape { Rectangle, RoundedRectangle, Ellipse }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextFrameElement), "text")]
[JsonDerivedType(typeof(ImageFrameElement), "image")]
[JsonDerivedType(typeof(ShapeElement), "shape")]
public abstract class PublicationElement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Element";
    public abstract PublicationElementKind Kind { get; }
    public double X { get; set; } = 20;
    public double Y { get; set; } = 20;
    public double Width { get; set; } = 60;
    public double Height { get; set; } = 40;
    public double Rotation { get; set; }
    public int ZIndex { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
}

public sealed class TextFrameElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Text;
    public string PreviewHtml { get; set; } = "<p>Text frame</p>";
    public byte[] DocumentContent { get; set; } = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><p>Text frame</p></body></html>");
    public double PaddingMm { get; set; } = 2;
    public string Background { get; set; } = "transparent";
    public string BorderColor { get; set; } = "transparent";
    public double BorderWidth { get; set; }
}

public sealed class ImageFrameElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Image;
    public string DataUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = "Picture";
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropScale { get; set; } = 1;
    public double Opacity { get; set; } = 1;
    public double Brightness { get; set; } = 1;
    public double Contrast { get; set; } = 1;
    public double Saturation { get; set; } = 1;
    public double HueRotation { get; set; }
    public double Invert { get; set; }
    public double Grayscale { get; set; }
    public double Sepia { get; set; }
    public double Blur { get; set; }
    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }
    public bool FitInsideFrame { get; set; }
    public ImageMaskShape MaskShape { get; set; }
    public double CornerRadiusMm { get; set; } = 4;
    public string BorderColor { get; set; } = "transparent";
    public double BorderWidthMm { get; set; }
    public bool ShadowEnabled { get; set; }
}

public sealed class ShapeElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Shape;
    public PublicationShape Shape { get; set; } = PublicationShape.Rectangle;
    public string Fill { get; set; } = "#dbeafe";
    public string Stroke { get; set; } = "#1d4ed8";
    public double StrokeWidth { get; set; } = 0.4;
    public double CornerRadiusMm { get; set; } = 3;
}
