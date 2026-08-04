using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

public sealed class PublicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Publication";
    public string FormatVersion { get; set; } = "1.55";
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public double Zoom { get; set; } = 0.8;
    public PublicationViewSettings View { get; set; } = new();
    public List<PublicationPage> Pages { get; set; } = [];
    public List<PublicationDataObject> DataObjects { get; set; } = [];
    public List<PublicationElementTemplate> ComponentTemplates { get; set; } = [];
    public PublicationPlaybackSettings Playback { get; set; } = new();
    public PublicationStreamingSettings Streaming { get; set; } = new();
    public PublicationProjectSettings ProjectSettings { get; set; } = new();

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
    public bool PanelLibraryVisible { get; set; }
    public double GridSpacingMm { get; set; } = 2.5;
    public int ExportDpi { get; set; } = 150;
    public PublicationCanvasZoomMode CanvasZoomMode { get; set; } = PublicationCanvasZoomMode.CssLayout;
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

}

public sealed class GuideLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GuideOrientation Orientation { get; set; }
    public double PositionMm { get; set; }
}

public enum GuideOrientation { Horizontal, Vertical }
public enum MeasurementUnit { Millimeter, Centimeter, Inch, Pixel }
public enum PublicationCanvasZoomMode { CssLayout, Transform }
public enum PublicationHtmlExportSupport { Native, CanvasRuntime, RenderBeforeExport }
public enum PublicationElementKind { Text, Image, Video, Audio, Shape, WordArt, Connector, DataVisual, Barcode, Spreadsheet, DevExtremeComponent, LiveSource, Panel, HtmlEmbed }
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
public enum WordArtFillKind { Solid, Gradient, Picture, Video }
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
    double Y,
    Guid? TargetElementId = null,
    string TargetElementKind = "",
    double TargetX = .5,
    double TargetY = .5);

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
[JsonDerivedType(typeof(PanelElement), "panel")]
[JsonDerivedType(typeof(HtmlEmbedElement), "htmlEmbed")]
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
    public byte[] DocumentContent { get; set; } = [];
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
    public List<PublicationMediaSegment> Segments { get; set; } = [];

    [JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    [JsonIgnore]
    public IReadOnlyList<PublicationMediaSegment> EffectiveSegments => Segments is { Count: > 0 }
        ? Segments
        : [new PublicationMediaSegment
        {
            Id = Id,
            Name = Name,
            DataUrl = DataUrl,
            MimeType = MimeType,
            DurationSeconds = DurationSeconds,
            TrimStartSeconds = TrimStartSeconds,
            TrimEndSeconds = EffectiveTrimEndSeconds,
            WaveformSamples = WaveformSamples
        }];

    [JsonIgnore]
    public double TimelineLengthSeconds => Math.Max(.05, EffectiveSegments.Sum(segment =>
        segment.TimelineDurationSeconds > 0
            ? segment.TimelineDurationSeconds
            : segment.SourceLengthSeconds / Math.Max(.0001, Math.Abs(segment.Speed))) / Math.Max(.1, PlaybackRate));
}

public sealed class VideoElement : PublicationMediaElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Video;
    public string PosterDataUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = "Video";
    public bool ShowControls { get; set; } = true;
    public PublicationVideoFitMode FitMode { get; set; } = PublicationVideoFitMode.Stretch;
    public bool FitModeExplicit { get; set; }
    public string Background { get; set; } = "#111827";
    public VideoProjectDocument? VideoProject { get; set; }
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
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
    public WordArtFillKind FillKind { get; set; } = WordArtFillKind.Gradient;
    public string FillMediaDataUrl { get; set; } = string.Empty;
    public string FillMediaMimeType { get; set; } = string.Empty;
    public string FillMediaPosterDataUrl { get; set; } = string.Empty;
    public PublicationVideoFitMode FillMediaFitMode { get; set; } = PublicationVideoFitMode.Cover;
    public bool FillMediaLoop { get; set; } = true;
    public double FillMediaScale { get; set; } = 1;
    public double FillMediaOffsetXPercent { get; set; }
    public double FillMediaOffsetYPercent { get; set; }
    public string OutlineColor { get; set; } = "#17365d";
    public double OutlineWidth { get; set; } = 2;
    public bool ShadowEnabled { get; set; } = true;
    public string ShadowColor { get; set; } = "#00000080";
    public double ExtrudeDepth { get; set; } = 4;
    public string ExtrudeColor { get; set; } = "#17365d";
    public WordArtWarp Warp { get; set; }
    public List<WordArtPathPoint> CustomPathPoints { get; set; } = [];
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
