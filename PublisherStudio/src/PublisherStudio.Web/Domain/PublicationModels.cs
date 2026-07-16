using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public sealed class PublicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Publication";
    public string FormatVersion { get; set; } = "1.5";
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public double Zoom { get; set; } = 0.8;
    public PublicationViewSettings View { get; set; } = new();
    public List<PublicationPage> Pages { get; set; } = [];

    public static PublicationDocument CreateDefault()
    {
        var document = new PublicationDocument();
        var publicationPage = PublicationPage.CreateA4();
        publicationPage.Elements.Add(new TextFrameElement
        {
            Name = "Title",
            X = 20,
            Y = 22,
            Width = 170,
            Height = 34,
            PreviewHtml = "<h1 style=\"margin:0;font:700 28pt Segoe UI;color:#17365d\">Your publication</h1><p style=\"margin:8px 0 0;font:12pt Segoe UI;color:#475569\">Double-click this frame to edit it with DevExpress RichEdit.</p>",
            DocumentContent = RichTextDocumentFactory.CreateOpenXml("Your publication", "Double-click this frame to edit it with DevExpress RichEdit."),
            StoryFormat = StoryStorageFormat.OpenXml,
            ZIndex = 2
        });
        publicationPage.Elements.Add(new ShapeElement
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
        document.Pages.Add(publicationPage);
        return document;
    }
}

public sealed class PublicationViewSettings
{
    public MeasurementUnit RulerUnit { get; set; } = MeasurementUnit.Millimeter;
    public bool RulersVisible { get; set; } = true;
    public bool GridVisible { get; set; } = true;
    public bool GuidesVisible { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;
    public bool SnapToGuides { get; set; } = true;
    public bool SnapToPage { get; set; } = true;
    public double GridSpacingMm { get; set; } = 5;
    public int ExportDpi { get; set; } = 150;
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
public enum MeasurementUnit { Millimeter, Centimeter, Inch, Pixel }
public enum PublicationElementKind { Text, Image, Shape, WordArt, Connector }
public enum PublicationShape { Rectangle, RoundedRectangle, Ellipse, Line }
public enum ConnectorPathKind { Straight, Elbow, Curved }
public enum ConnectorMarker { None, Arrow, Triangle, Diamond }
public enum ConnectorDashStyle { Solid, Dash, Dot }
public enum ConnectorAnchor { TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Center }
public enum ConnectorToolKind { None, Line, Arrow }
public enum ImageMaskShape { Rectangle, RoundedRectangle, Ellipse }
public enum StoryStorageFormat { Html, OpenXml }
public enum ImageTintMode { Overlay, Recolor }
public enum ImageBlendMode { Normal, Multiply, Screen, Darken, Lighten }
public enum WordArtWarp { None, ArchUp, ArchDown, Wave, Custom }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextFrameElement), "text")]
[JsonDerivedType(typeof(ImageFrameElement), "image")]
[JsonDerivedType(typeof(ShapeElement), "shape")]
[JsonDerivedType(typeof(WordArtElement), "wordArt")]
[JsonDerivedType(typeof(ConnectorElement), "connector")]
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
    public byte[] DocumentContent { get; set; } = RichTextDocumentFactory.CreateOpenXml("Text frame");
    public StoryStorageFormat StoryFormat { get; set; } = StoryStorageFormat.OpenXml;
    public double PaddingMm { get; set; } = 2;
    public string Background { get; set; } = "transparent";
    public string BorderColor { get; set; } = "transparent";
    public double BorderWidth { get; set; }
}

public sealed class ImageFrameElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Image;
    public string DataUrl { get; set; } = string.Empty;
    public string OriginalDataUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = "Picture";
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropScale { get; set; } = 1;
    public double ImageRotation { get; set; }
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
    public ImageTintMode TintMode { get; set; }
    public ImageBlendMode BlendMode { get; set; } = ImageBlendMode.Normal;
    public string TintColor { get; set; } = "#2f75b5";
    public double TintOpacity { get; set; }
    public string TransparentColor { get; set; } = "#ffffff";
    public int TransparentColorTolerance { get; set; } = 24;
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


public sealed class ConnectorEndpoint
{
    public Guid ElementId { get; set; }
    public ConnectorAnchor Anchor { get; set; } = ConnectorAnchor.Right;
}

public sealed class ConnectorElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Connector;
    public ConnectorEndpoint Source { get; set; } = new();
    public ConnectorEndpoint Target { get; set; } = new();
    public ConnectorPathKind PathKind { get; set; } = ConnectorPathKind.Curved;
    public ConnectorMarker StartMarker { get; set; }
    public ConnectorMarker EndMarker { get; set; } = ConnectorMarker.Arrow;
    public ConnectorDashStyle DashStyle { get; set; }
    public string Stroke { get; set; } = "#245b85";
    public double StrokeWidthMm { get; set; } = 0.7;
}

public sealed class WordArtElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.WordArt;
    public string Text { get; set; } = "WordArt";
    public string FontFamily { get; set; } = "Arial Black";
    public double FontSizePt { get; set; } = 42;
    public bool Bold { get; set; } = true;
    public bool Italic { get; set; }
    public double LetterSpacing { get; set; }
    public string FillColor { get; set; } = "#2f75b5";
    public string SecondaryColor { get; set; } = "#8ec5ff";
    public bool GradientFill { get; set; } = true;
    public string OutlineColor { get; set; } = "#17365d";
    public double OutlineWidth { get; set; } = 2;
    public bool ShadowEnabled { get; set; } = true;
    public string ShadowColor { get; set; } = "#00000080";
    public double ExtrudeDepth { get; set; } = 4;
    public string ExtrudeColor { get; set; } = "#17365d";
    public WordArtWarp Warp { get; set; }
    public List<WordArtPathPoint> CustomPathPoints { get; set; } = WordArtPathGeometry.CreatePreset("GentleWave");
    public double PathStartOffsetPercent { get; set; } = 50;
    public double PathBaselineOffset { get; set; }
}

public static class RichTextDocumentFactory
{
    public static byte[] CreateOpenXml(string title, string? subtitle = null)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
                </Types>
                """);
            Write(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);
            Write(archive, "word/_rels/document.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            Write(archive, "word/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/></w:style>
                  <w:style w:type="character" w:default="1" w:styleId="DefaultParagraphFont"><w:name w:val="Default Paragraph Font"/></w:style>
                </w:styles>
                """);

            var escapedTitle = SecurityElement.Escape(title) ?? string.Empty;
            var escapedSubtitle = SecurityElement.Escape(subtitle ?? string.Empty) ?? string.Empty;
            var subtitleParagraph = string.IsNullOrWhiteSpace(subtitle)
                ? string.Empty
                : $"<w:p><w:pPr><w:spacing w:before=\"120\"/></w:pPr><w:r><w:rPr><w:sz w:val=\"24\"/><w:color w:val=\"475569\"/></w:rPr><w:t>{escapedSubtitle}</w:t></w:r></w:p>";

            Write(archive, "word/document.xml", $$"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    <w:p><w:r><w:rPr><w:b/><w:sz w:val="56"/><w:color w:val="17365D"/></w:rPr><w:t>{{escapedTitle}}</w:t></w:r></w:p>
                    {{subtitleParagraph}}
                    <w:sectPr>
                      <w:pgSz w:w="11906" w:h="16838"/>
                      <w:pgMar w:top="720" w:right="720" w:bottom="720" w:left="720" w:header="360" w:footer="360" w:gutter="0"/>
                    </w:sectPr>
                  </w:body>
                </w:document>
                """);
        }
        return stream.ToArray();
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }
}

public sealed class PublicationFieldRecord
{
    public string PublicationName { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string StoryName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageCount { get; set; }
    public DateTime CurrentDate { get; set; }
    public DateTime CurrentDateTime { get; set; }
}
