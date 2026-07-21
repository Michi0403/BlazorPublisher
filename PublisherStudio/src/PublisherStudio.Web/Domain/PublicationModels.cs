using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public sealed class PublicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Publication";
    public string FormatVersion { get; set; } = "1.45";
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public double Zoom { get; set; } = 0.8;
    public PublicationViewSettings View { get; set; } = new();
    public List<PublicationPage> Pages { get; set; } = [];
    public List<PublicationDataObject> DataObjects { get; set; } = [];
    public PublicationPlaybackSettings Playback { get; set; } = new();
    public PublicationStreamingSettings Streaming { get; set; } = new();

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
    public bool SnapToGrid { get; set; } = false;
    public bool SnapToGuides { get; set; } = true;
    public bool SnapToPage { get; set; } = true;
    public bool SnapToObjects { get; set; } = true;
    public bool SnapInObjects { get; set; } = true;
    public double GridSpacingMm { get; set; } = 2.5;
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
    public PublicationPageTransition Transition { get; set; } = new();
    public double TimelineDurationSeconds { get; set; } = 10;

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
public enum PublicationElementKind { Text, Image, Video, Audio, Shape, WordArt, Connector, DataVisual, Barcode, Spreadsheet, DevExtremeComponent, LiveSource }
public enum PublicationShape { Rectangle, RoundedRectangle, Ellipse, Line }
public enum ConnectorPathKind { Straight, Elbow, Curved }
public enum ConnectorMarker { None, Arrow, Triangle, Diamond }
public enum ConnectorDashStyle { Solid, Dash, Dot }
public enum ConnectorAnchor { TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Center }
public enum ConnectorToolKind { None, Line, Arrow, SignalConnector, SignalArrow }
public enum ConnectorEndpointKind { Element, Canvas }
public enum SignalConnectorTrigger { OnPageEnter, OnClick, OnHover, Manual }
public enum SignalConnectorVisual { FlyingArrow, DrawPath, Pulse, None }
public enum SignalGesture { None, Click, Hover }
public enum SignalCompletionAction
{
    None, Click, Hover, Show, Hide, ToggleVisibility, SetOpacity, ReplayAnimation,
    PlayMedia, PauseMedia, ToggleMediaPlayback, Highlight, AddCssClass, RemoveCssClass,
    ToggleCssClass, RunSignal
}
public enum ImageMaskShape { Rectangle, RoundedRectangle, Ellipse }
public enum StoryStorageFormat { Html, OpenXml }
public enum SpreadsheetStorageFormat { Xlsx, Xlsm, Xls, Csv, Text }
public enum PublicationContentFitMode { Clip, Fit, Fill, Stretch }
public enum ImageTintMode { Overlay, Recolor }
public enum ImageBlendMode { Normal, Multiply, Screen, Darken, Lighten }
public enum WordArtWarp { None, ArchUp, ArchDown, Wave, Custom }
public enum PublicationAnimationPhase { Entrance, Emphasis, Exit, Motion }
public enum PublicationAnimationEffect { Fade, Fly, Float, Zoom, Wipe, Bounce, Pulse, Spin, Shake, GrowShrink, Move, PlayMedia, PauseMedia, StopMedia }
public enum PublicationAnimationTrigger { OnPageEnter, WithPrevious, AfterPrevious, OnClick }
public enum PublicationAnimationEasing { Linear, EaseIn, EaseOut, EaseInOut, BackOut, BounceOut }
public enum PublicationAnimationDirection { None, Left, Right, Up, Down }
public enum PublicationPageTransitionKind { None, Fade, Push, Wipe, Zoom, Flip }
public enum PublicationInteractionAction { None, NextPage, PreviousPage, GoToPage, OpenUrl, ToggleVisibility, Show, Hide, ReplayAnimation, PlayMedia, PauseMedia, ToggleMediaPlayback }
public enum PublicationMediaPlaybackTrigger { OnPageEnter, OnClick, WithAnimation }
public enum PublicationAudioDisplayKind { Waveform, Compact, Hidden }
public enum PublicationVideoFitMode { Contain, Cover, Stretch }
public enum PublicationBarcodeFormat { QrCode, Code128, Code39, Ean13, UpcA, Itf14, Codabar }
public enum PublicationBarcodeErrorCorrection { L, M, Q, H }
public enum PublicationBarcodeModuleShape { Square, Rounded, Dots }

public sealed record CanvasInsertRequest(string Kind, double X, double Y);
public sealed record ExternalFileDropRequest(
    Guid AssetId,
    string Kind,
    string Name,
    string MimeType,
    long Size,
    double DurationSeconds,
    int PixelWidth,
    int PixelHeight,
    double X,
    double Y);

public sealed class PublicationConnectorPort
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Connector point";
    public double XPercent { get; set; } = .5;
    public double YPercent { get; set; } = .5;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextFrameElement), "text")]
[JsonDerivedType(typeof(ImageFrameElement), "image")]
[JsonDerivedType(typeof(VideoElement), "video")]
[JsonDerivedType(typeof(AudioElement), "audio")]
[JsonDerivedType(typeof(ShapeElement), "shape")]
[JsonDerivedType(typeof(WordArtElement), "wordArt")]
[JsonDerivedType(typeof(ConnectorElement), "connector")]
[JsonDerivedType(typeof(DataVisualElement), "dataVisual")]
[JsonDerivedType(typeof(BarcodeElement), "barcode")]
[JsonDerivedType(typeof(SpreadsheetElement), "spreadsheet")]
[JsonDerivedType(typeof(DevExtremeComponentElement), "devExtremeComponent")]
[JsonDerivedType(typeof(LiveSourceElement), "liveSource")]
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
    public bool HiddenAtPresentationStart { get; set; }
    public Guid? GroupId { get; set; }
    public List<PublicationAnimation> Animations { get; set; } = [];
    public PublicationInteraction Interaction { get; set; } = new();
    public List<PublicationConnectorPort> ConnectorPorts { get; set; } = [];
}

public sealed class TextFrameElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Text;
    public string PreviewHtml { get; set; } = "<p>Text frame</p>";
    public byte[] DocumentContent { get; set; } = RichTextDocumentFactory.CreateOpenXml("Text frame");
    public StoryStorageFormat StoryFormat { get; set; } = StoryStorageFormat.OpenXml;
    public string DocumentBackground { get; set; } = "transparent";
    public double PaddingMm { get; set; } = 2;
    public string Background { get; set; } = "transparent";
    public string BorderColor { get; set; } = "transparent";
    public double BorderWidth { get; set; }
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    public double ContentOffsetX { get; set; }
    public double ContentOffsetY { get; set; }
    public double ContentScale { get; set; } = 1;
}

public sealed class SpreadsheetElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Spreadsheet;
    public byte[] WorkbookContent { get; set; } = [];
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    public SpreadsheetStorageFormat StorageFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    public string PreviewHtml { get; set; } = string.Empty;
    public string ActiveSheetName { get; set; } = "Sheet1";
    public bool ShowSheetName { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;
    public string Background { get; set; } = "#ffffff";
    public string BorderColor { get; set; } = "#94a3b8";
    public double BorderWidthMm { get; set; } = 0.25;
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    public double ContentOffsetX { get; set; }
    public double ContentOffsetY { get; set; }
    public double ContentScale { get; set; } = 1;
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
    public PictureDocument? PictureSource { get; set; }
}


public abstract class PublicationMediaElement : PublicationElement
{
    public string DataUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public double TrimStartSeconds { get; set; }
    public double TrimEndSeconds { get; set; }
    public double TimelineStartSeconds { get; set; }
    public double Volume { get; set; } = 1;
    public double PlaybackRate { get; set; } = 1;
    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
    public bool Muted { get; set; }
    public bool Loop { get; set; }
    public bool AutoPlay { get; set; } = true;
    public PublicationMediaPlaybackTrigger PlaybackTrigger { get; set; } = PublicationMediaPlaybackTrigger.OnPageEnter;
    public List<double> WaveformSamples { get; set; } = [];

    [JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    [JsonIgnore]
    public double TimelineLengthSeconds => Math.Max(.05, (EffectiveTrimEndSeconds - TrimStartSeconds) / Math.Max(.1, PlaybackRate));
}

public sealed class VideoElement : PublicationMediaElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Video;
    public string PosterDataUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = "Video";
    public bool ShowControls { get; set; } = true;
    public PublicationVideoFitMode FitMode { get; set; } = PublicationVideoFitMode.Contain;
    public string Background { get; set; } = "#111827";
}

public sealed class AudioElement : PublicationMediaElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Audio;
    public PublicationAudioDisplayKind DisplayKind { get; set; } = PublicationAudioDisplayKind.Waveform;
    public string AccentColor { get; set; } = "#2f75b5";
    public bool ShowControls { get; set; } = true;
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

public sealed class BarcodeElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Barcode;
    public PublicationBarcodeFormat Format { get; set; } = PublicationBarcodeFormat.QrCode;
    public string Value { get; set; } = "https://github.com/Michi0403/BlazorPublisher";
    public string ForegroundColor { get; set; } = "#111827";
    public string BackgroundColor { get; set; } = "#ffffff";
    public bool TransparentBackground { get; set; }
    public bool ShowText { get; set; } = true;
    public int Margin { get; set; } = 8;
    public int LineWidth { get; set; } = 2;
    public int BarHeight { get; set; } = 90;
    public int FontSize { get; set; } = 16;
    public PublicationBarcodeErrorCorrection ErrorCorrection { get; set; } = PublicationBarcodeErrorCorrection.M;
    public PublicationBarcodeModuleShape ModuleShape { get; set; } = PublicationBarcodeModuleShape.Square;
    public string SvgMarkup { get; set; } = string.Empty;
}


public sealed class ConnectorEndpoint
{
    public ConnectorEndpointKind Kind { get; set; } = ConnectorEndpointKind.Element;
    public Guid ElementId { get; set; }
    public ConnectorAnchor Anchor { get; set; } = ConnectorAnchor.Right;
    public Guid? PortId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string TargetSelector { get; set; } = string.Empty;
}

public sealed class SignalConnectorSettings
{
    public bool Enabled { get; set; }
    public bool LineVisible { get; set; } = true;
    public SignalConnectorTrigger Trigger { get; set; } = SignalConnectorTrigger.OnPageEnter;
    public SignalConnectorVisual Visual { get; set; } = SignalConnectorVisual.FlyingArrow;
    public double DelaySeconds { get; set; }
    public double DurationSeconds { get; set; } = 1.5;
    public int RepeatCount { get; set; } = 1;
    public bool Loop { get; set; }
    public bool AutoReverse { get; set; }
    public SignalGesture StartGesture { get; set; }
    public SignalGesture EndGesture { get; set; }
    public Guid? MotionTargetElementId { get; set; }
    public string MotionTargetSelector { get; set; } = string.Empty;
    public double TranslateXPercent { get; set; }
    public double TranslateYPercent { get; set; }
    public double Scale { get; set; } = 1;
    public double ResizeWidthPercent { get; set; } = 100;
    public double ResizeHeightPercent { get; set; } = 100;
    public double RotationDegrees { get; set; }
    public double Opacity { get; set; } = 1;
    public bool RestoreMotionAfterRun { get; set; }
    public Guid? CompletionTargetElementId { get; set; }
    public string CompletionTargetSelector { get; set; } = string.Empty;
    public SignalCompletionAction CompletionAction { get; set; }
    public string CompletionValue { get; set; } = string.Empty;
    public double CompletionDurationSeconds { get; set; } = .8;
    public Guid? NextConnectorId { get; set; }
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
    public double? Control1X { get; set; }
    public double? Control1Y { get; set; }
    public double? Control2X { get; set; }
    public double? Control2Y { get; set; }
    public SignalConnectorSettings Signal { get; set; } = new();
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


public sealed class PublicationPlaybackSettings
{
    public bool LoopPresentation { get; set; }
    public bool StartAutomatically { get; set; } = true;
    public bool ShowControls { get; set; } = true;
}

public sealed class PublicationPageTransition
{
    public PublicationPageTransitionKind Kind { get; set; } = PublicationPageTransitionKind.Fade;
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseInOut;
    public double DurationSeconds { get; set; } = .55;
    public bool AdvanceOnClick { get; set; } = true;
    public bool AutoAdvance { get; set; }
    public double AutoAdvanceSeconds { get; set; } = 5;
}

public sealed class PublicationAnimation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Animation";
    public int Order { get; set; }
    public PublicationAnimationPhase Phase { get; set; } = PublicationAnimationPhase.Entrance;
    public PublicationAnimationEffect Effect { get; set; } = PublicationAnimationEffect.Fade;
    public PublicationAnimationTrigger Trigger { get; set; } = PublicationAnimationTrigger.AfterPrevious;
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseOut;
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    public double DurationSeconds { get; set; } = .6;
    public double DelaySeconds { get; set; }
    public double? TimelineStartSeconds { get; set; }
    public double DistancePercent { get; set; } = 18;
    public double ScalePercent { get; set; } = 20;
    public double RotationDegrees { get; set; } = 360;
    public int RepeatCount { get; set; } = 1;
    public bool AutoReverse { get; set; }
}

public sealed class PublicationInteraction
{
    public PublicationInteractionAction Action { get; set; }
    public Guid? TargetPageId { get; set; }
    public Guid? TargetElementId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewWindow { get; set; } = true;
}

public static class RichTextDocumentFactory
{
    public static byte[] CreateOpenXml(string title, string? subtitle = null)
    {
        var paragraphs = new List<string>
        {
            BuildParagraph(
                BuildRun(title, "<w:b/><w:sz w:val=\"56\"/><w:color w:val=\"17365D\"/>"))
        };

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            paragraphs.Add(BuildParagraph(
                BuildRun(subtitle, "<w:sz w:val=\"24\"/><w:color w:val=\"475569\"/>"),
                "<w:spacing w:before=\"120\"/>"));
        }

        return CreateOpenXmlPackage(paragraphs);
    }

    public static byte[] CreateOpenXmlFromPlainText(string text)
    {
        var paragraphs = NormalizeLines(text)
            .Split('\n')
            .Select(line => string.IsNullOrEmpty(line)
                ? "<w:p/>"
                : BuildParagraph(BuildRun(line)))
            .ToList();

        return CreateOpenXmlPackage(paragraphs);
    }

    public static byte[] CreateOpenXmlFromMarkdown(string markdown)
    {
        var paragraphs = new List<string>();
        foreach (var sourceLine in NormalizeLines(markdown).Split('\n'))
        {
            var line = sourceLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                paragraphs.Add("<w:p/>");
                continue;
            }

            var trimmed = line.TrimStart();
            var headingLevel = 0;
            while (headingLevel < trimmed.Length && headingLevel < 6 && trimmed[headingLevel] == '#')
                headingLevel++;

            if (headingLevel > 0
                && headingLevel < trimmed.Length
                && char.IsWhiteSpace(trimmed[headingLevel]))
            {
                var headingText = trimmed[(headingLevel + 1)..].Trim();
                var halfPointSize = Math.Max(24, 48 - (headingLevel - 1) * 4);
                paragraphs.Add(BuildParagraph(
                    BuildMarkdownRuns(headingText, $"<w:b/><w:sz w:val=\"{halfPointSize}\"/>"),
                    "<w:spacing w:before=\"160\" w:after=\"80\"/>"));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal)
                || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                paragraphs.Add(BuildParagraph(
                    BuildRun("• ", "<w:b/>") + BuildMarkdownRuns(trimmed[2..]),
                    "<w:ind w:left=\"360\" w:hanging=\"180\"/><w:spacing w:after=\"40\"/>"));
                continue;
            }

            paragraphs.Add(BuildParagraph(
                BuildMarkdownRuns(trimmed),
                "<w:spacing w:after=\"80\"/>"));
        }

        return CreateOpenXmlPackage(paragraphs);
    }

    private static byte[] CreateOpenXmlPackage(IEnumerable<string> bodyElements)
    {
        var body = string.Join(Environment.NewLine, bodyElements);
        if (string.IsNullOrWhiteSpace(body))
            body = "<w:p/>";

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
            Write(archive, "word/document.xml", $$"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    {{body}}
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

    private static string BuildParagraph(string runs, string? paragraphProperties = null) =>
        string.IsNullOrWhiteSpace(paragraphProperties)
            ? $"<w:p>{runs}</w:p>"
            : $"<w:p><w:pPr>{paragraphProperties}</w:pPr>{runs}</w:p>";

    private static string BuildMarkdownRuns(string value, string? baseProperties = null)
    {
        var runs = new StringBuilder();
        var buffer = new StringBuilder();
        var bold = false;
        var italic = false;
        var code = false;

        void Flush()
        {
            if (buffer.Length == 0) return;
            var properties = new StringBuilder(baseProperties ?? string.Empty);
            if (bold) properties.Append("<w:b/>");
            if (italic) properties.Append("<w:i/>");
            if (code)
                properties.Append("<w:rFonts w:ascii=\"Consolas\" w:hAnsi=\"Consolas\"/><w:shd w:val=\"clear\" w:fill=\"E5E7EB\"/>");
            runs.Append(BuildRun(buffer.ToString(), properties.ToString()));
            buffer.Clear();
        }

        for (var index = 0; index < value.Length;)
        {
            if (!code && index + 1 < value.Length && value[index] == '*' && value[index + 1] == '*')
            {
                Flush();
                bold = !bold;
                index += 2;
                continue;
            }

            if (value[index] == '`')
            {
                Flush();
                code = !code;
                index++;
                continue;
            }

            if (!code && value[index] is '*' or '_')
            {
                Flush();
                italic = !italic;
                index++;
                continue;
            }

            buffer.Append(value[index]);
            index++;
        }

        Flush();
        return runs.Length == 0 ? BuildRun(string.Empty, baseProperties) : runs.ToString();
    }

    private static string BuildRun(string value, string? runProperties = null)
    {
        var escaped = SecurityElement.Escape(value) ?? string.Empty;
        var properties = string.IsNullOrWhiteSpace(runProperties)
            ? string.Empty
            : $"<w:rPr>{runProperties}</w:rPr>";
        return $"<w:r>{properties}<w:t xml:space=\"preserve\">{escaped}</w:t></w:r>";
    }

    private static string NormalizeLines(string? value) =>
        (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

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
