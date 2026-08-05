using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a publication document.
/// </summary>
public sealed class PublicationDocument
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Untitled Publication";
    /// <summary>
    /// Gets or sets format version.
    /// </summary>
    public string FormatVersion { get; set; } = "1.55";
    /// <summary>
    /// Gets or sets the UTC modification time.
    /// </summary>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets zoom.
    /// </summary>
    public double Zoom { get; set; } = 0.8;
    /// <summary>
    /// Gets or sets view.
    /// </summary>
    public PublicationViewSettings View { get; set; } = new();
    /// <summary>
    /// Gets or sets pages.
    /// </summary>
    public List<PublicationPage> Pages { get; set; } = [];
    /// <summary>
    /// Gets or sets data objects.
    /// </summary>
    public List<PublicationDataObject> DataObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets component templates.
    /// </summary>
    public List<PublicationElementTemplate> ComponentTemplates { get; set; } = [];
    /// <summary>
    /// Gets or sets playback.
    /// </summary>
    public PublicationPlaybackSettings Playback { get; set; } = new();
    /// <summary>
    /// Gets or sets streaming.
    /// </summary>
    public PublicationStreamingSettings Streaming { get; set; } = new();
    /// <summary>
    /// Gets or sets project settings.
    /// </summary>
    public PublicationProjectSettings ProjectSettings { get; set; } = new();

}

/// <summary>
/// Represents a publication view settings.
/// </summary>
public sealed class PublicationViewSettings
{
    /// <summary>
    /// Gets or sets ruler unit.
    /// </summary>
    public MeasurementUnit RulerUnit { get; set; } = MeasurementUnit.Millimeter;
    /// <summary>
    /// Gets or sets rulers visible.
    /// </summary>
    public bool RulersVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets grid visible.
    /// </summary>
    public bool GridVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets guides visible.
    /// </summary>
    public bool GuidesVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets snap to grid.
    /// </summary>
    public bool SnapToGrid { get; set; } = false;
    /// <summary>
    /// Gets or sets snap to guides.
    /// </summary>
    public bool SnapToGuides { get; set; } = true;
    /// <summary>
    /// Gets or sets snap to page.
    /// </summary>
    public bool SnapToPage { get; set; } = true;
    /// <summary>
    /// Gets or sets snap to objects.
    /// </summary>
    public bool SnapToObjects { get; set; } = true;
    /// <summary>
    /// Gets or sets snap in objects.
    /// </summary>
    public bool SnapInObjects { get; set; } = true;
    /// <summary>
    /// Gets or sets panel library visible.
    /// </summary>
    public bool PanelLibraryVisible { get; set; }
    /// <summary>
    /// Gets or sets grid spacing millimetres.
    /// </summary>
    public double GridSpacingMm { get; set; } = 2.5;
    /// <summary>
    /// Gets or sets export DPI.
    /// </summary>
    public int ExportDpi { get; set; } = 150;
    /// <summary>
    /// Gets or sets canvas zoom mode.
    /// </summary>
    public PublicationCanvasZoomMode CanvasZoomMode { get; set; } = PublicationCanvasZoomMode.CssLayout;
}

/// <summary>
/// Represents a publication page.
/// </summary>
public sealed class PublicationPage
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Page 1";
    /// <summary>
    /// Gets or sets width millimetres.
    /// </summary>
    public double WidthMm { get; set; } = 210;
    /// <summary>
    /// Gets or sets height millimetres.
    /// </summary>
    public double HeightMm { get; set; } = 297;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets elements.
    /// </summary>
    public List<PublicationElement> Elements { get; set; } = [];
    /// <summary>
    /// Gets or sets guides.
    /// </summary>
    public List<GuideLine> Guides { get; set; } = [];
    /// <summary>
    /// Gets or sets transition.
    /// </summary>
    public PublicationPageTransition Transition { get; set; } = new();
    /// <summary>
    /// Gets or sets timeline duration seconds.
    /// </summary>
    public double TimelineDurationSeconds { get; set; } = 10;

}

/// <summary>
/// Represents a guide line.
/// </summary>
public sealed class GuideLine
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets orientation.
    /// </summary>
    public GuideOrientation Orientation { get; set; }
    /// <summary>
    /// Gets or sets position millimetres.
    /// </summary>
    public double PositionMm { get; set; }
}

/// <summary>
/// Lists supported guide orientation values.
/// </summary>
public enum GuideOrientation { Horizontal, Vertical }
/// <summary>
/// Lists supported measurement unit values.
/// </summary>
public enum MeasurementUnit { Millimeter, Centimeter, Inch, Pixel }
/// <summary>
/// Lists supported publication canvas zoom mode values.
/// </summary>
public enum PublicationCanvasZoomMode { CssLayout, Transform }
/// <summary>
/// Lists supported publication HTML export support values.
/// </summary>
public enum PublicationHtmlExportSupport { Native, CanvasRuntime, RenderBeforeExport }
/// <summary>
/// Lists supported publication element kind values.
/// </summary>
public enum PublicationElementKind { Text, Image, Video, Audio, Shape, WordArt, Connector, DataVisual, Barcode, Spreadsheet, DevExtremeComponent, LiveSource, Panel, HtmlEmbed }
/// <summary>
/// Lists supported publication shape values.
/// </summary>
public enum PublicationShape { Rectangle, RoundedRectangle, Ellipse, Line }
/// <summary>
/// Lists supported connector path kind values.
/// </summary>
public enum ConnectorPathKind { Straight, Elbow, Curved }
/// <summary>
/// Lists supported connector marker values.
/// </summary>
public enum ConnectorMarker { None, Arrow, Triangle, Diamond }
/// <summary>
/// Lists supported connector dash style values.
/// </summary>
public enum ConnectorDashStyle { Solid, Dash, Dot }
/// <summary>
/// Lists supported connector anchor values.
/// </summary>
public enum ConnectorAnchor { TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Center }
/// <summary>
/// Lists supported connector tool kind values.
/// </summary>
public enum ConnectorToolKind { None, Line, Arrow, SignalConnector, SignalArrow }
/// <summary>
/// Lists supported connector endpoint kind values.
/// </summary>
public enum ConnectorEndpointKind { Element, Canvas }
/// <summary>
/// Lists supported signal connector trigger values.
/// </summary>
public enum SignalConnectorTrigger { OnPageEnter, OnClick, OnHover, Manual }
/// <summary>
/// Lists supported signal connector visual values.
/// </summary>
public enum SignalConnectorVisual { FlyingArrow, DrawPath, Pulse, None }
/// <summary>
/// Lists supported signal gesture values.
/// </summary>
public enum SignalGesture { None, Click, Hover }
/// <summary>
/// Lists supported signal completion action values.
/// </summary>
public enum SignalCompletionAction
{
    None, Click, Hover, Show, Hide, ToggleVisibility, SetOpacity, ReplayAnimation,
    PlayMedia, PauseMedia, ToggleMediaPlayback, Highlight, AddCssClass, RemoveCssClass,
    ToggleCssClass, RunSignal
}
/// <summary>
/// Lists supported image mask shape values.
/// </summary>
public enum ImageMaskShape { Rectangle, RoundedRectangle, Ellipse }
/// <summary>
/// Lists supported story storage format values.
/// </summary>
public enum StoryStorageFormat { Html, OpenXml }
/// <summary>
/// Lists supported spreadsheet storage format values.
/// </summary>
public enum SpreadsheetStorageFormat { Xlsx, Xlsm, Xls, Csv, Text }
/// <summary>
/// Lists supported publication content fit mode values.
/// </summary>
public enum PublicationContentFitMode { Clip, Fit, Fill, Stretch }
/// <summary>
/// Lists supported image tint mode values.
/// </summary>
public enum ImageTintMode { Overlay, Recolor }
/// <summary>
/// Lists supported image blend mode values.
/// </summary>
public enum ImageBlendMode { Normal, Multiply, Screen, Darken, Lighten }
/// <summary>
/// Lists supported word art warp values.
/// </summary>
public enum WordArtWarp { None, ArchUp, ArchDown, Wave, Custom }
/// <summary>
/// Lists supported word art fill kind values.
/// </summary>
public enum WordArtFillKind { Solid, Gradient, Picture, Video }
/// <summary>
/// Lists supported publication animation phase values.
/// </summary>
public enum PublicationAnimationPhase { Entrance, Emphasis, Exit, Motion }
/// <summary>
/// Lists supported publication animation effect values.
/// </summary>
public enum PublicationAnimationEffect { Fade, Fly, Float, Zoom, Wipe, Bounce, Pulse, Spin, Shake, GrowShrink, Move, PlayMedia, PauseMedia, StopMedia }
/// <summary>
/// Lists supported publication animation trigger values.
/// </summary>
public enum PublicationAnimationTrigger { OnPageEnter, WithPrevious, AfterPrevious, OnClick }
/// <summary>
/// Lists supported publication animation easing values.
/// </summary>
public enum PublicationAnimationEasing { Linear, EaseIn, EaseOut, EaseInOut, BackOut, BounceOut }
/// <summary>
/// Lists supported publication animation direction values.
/// </summary>
public enum PublicationAnimationDirection { None, Left, Right, Up, Down }
/// <summary>
/// Lists supported publication page transition kind values.
/// </summary>
public enum PublicationPageTransitionKind { None, Fade, Push, Wipe, Zoom, Flip }
/// <summary>
/// Lists supported publication interaction action values.
/// </summary>
public enum PublicationInteractionAction { None, NextPage, PreviousPage, GoToPage, OpenUrl, ToggleVisibility, Show, Hide, ReplayAnimation, PlayMedia, PauseMedia, ToggleMediaPlayback }
/// <summary>
/// Lists supported publication media playback trigger values.
/// </summary>
public enum PublicationMediaPlaybackTrigger { OnPageEnter, OnClick, WithAnimation }
/// <summary>
/// Lists supported publication audio display kind values.
/// </summary>
public enum PublicationAudioDisplayKind { Waveform, Compact, Hidden }
/// <summary>
/// Lists supported publication video fit mode values.
/// </summary>
public enum PublicationVideoFitMode { Contain, Cover, Stretch }
/// <summary>
/// Lists supported publication barcode format values.
/// </summary>
public enum PublicationBarcodeFormat { QrCode, Code128, Code39, Ean13, UpcA, Itf14, Codabar }
/// <summary>
/// Lists supported publication barcode error correction values.
/// </summary>
public enum PublicationBarcodeErrorCorrection { L, M, Q, H }
/// <summary>
/// Lists supported publication barcode module shape values.
/// </summary>
public enum PublicationBarcodeModuleShape { Square, Rounded, Dots }

/// <summary>
/// Represents a canvas insert request.
/// </summary>
public sealed record CanvasInsertRequest(string Kind, double X, double Y);
/// <summary>
/// Represents an external file drop request.
/// </summary>
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

/// <summary>
/// Represents a publication connector port.
/// </summary>
public sealed class PublicationConnectorPort
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Connector point";
    /// <summary>
    /// Gets or sets xpercent.
    /// </summary>
    public double XPercent { get; set; } = .5;
    /// <summary>
    /// Gets or sets ypercent.
    /// </summary>
    public double YPercent { get; set; } = .5;
}

/// <summary>
/// Represents a publication element.
/// </summary>
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
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Element";
    /// <summary>
    /// Gets kind.
    /// </summary>
    public abstract PublicationElementKind Kind { get; }
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; } = 20;
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; } = 20;
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public double Width { get; set; } = 60;
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public double Height { get; set; } = 40;
    /// <summary>
    /// Gets or sets rotation.
    /// </summary>
    public double Rotation { get; set; }
    /// <summary>
    /// Gets or sets zindex.
    /// </summary>
    public int ZIndex { get; set; }
    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets whether the item is locked.
    /// </summary>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets hidden at presentation start.
    /// </summary>
    public bool HiddenAtPresentationStart { get; set; }
    /// <summary>
    /// Gets or sets group identifier.
    /// </summary>
    public Guid? GroupId { get; set; }
    /// <summary>
    /// Gets or sets animations.
    /// </summary>
    public List<PublicationAnimation> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets interaction.
    /// </summary>
    public PublicationInteraction Interaction { get; set; } = new();
    /// <summary>
    /// Gets or sets connector ports.
    /// </summary>
    public List<PublicationConnectorPort> ConnectorPorts { get; set; } = [];
}

/// <summary>
/// Represents a text frame element.
/// </summary>
public sealed class TextFrameElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Text;
    /// <summary>
    /// Gets or sets preview HTML.
    /// </summary>
    public string PreviewHtml { get; set; } = "<p>Text frame</p>";
    /// <summary>
    /// Gets or sets document content.
    /// </summary>
    public byte[] DocumentContent { get; set; } = [];
    /// <summary>
    /// Gets or sets story format.
    /// </summary>
    public StoryStorageFormat StoryFormat { get; set; } = StoryStorageFormat.OpenXml;
    /// <summary>
    /// Gets or sets document background.
    /// </summary>
    public string DocumentBackground { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets padding millimetres.
    /// </summary>
    public double PaddingMm { get; set; } = 2;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets border width.
    /// </summary>
    public double BorderWidth { get; set; }
    /// <summary>
    /// Gets or sets content fit.
    /// </summary>
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    /// <summary>
    /// Gets or sets content offset horizontal position.
    /// </summary>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets content offset vertical position.
    /// </summary>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets content scale.
    /// </summary>
    public double ContentScale { get; set; } = 1;
}

/// <summary>
/// Represents a spreadsheet element.
/// </summary>
public sealed class SpreadsheetElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Spreadsheet;
    /// <summary>
    /// Gets or sets workbook content.
    /// </summary>
    public byte[] WorkbookContent { get; set; } = [];
    /// <summary>
    /// Gets or sets workbook file name.
    /// </summary>
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets storage format.
    /// </summary>
    public SpreadsheetStorageFormat StorageFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    /// <summary>
    /// Gets or sets preview HTML.
    /// </summary>
    public string PreviewHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets active sheet name.
    /// </summary>
    public string ActiveSheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets show sheet name.
    /// </summary>
    public bool ShowSheetName { get; set; } = true;
    /// <summary>
    /// Gets or sets show grid lines.
    /// </summary>
    public bool ShowGridLines { get; set; } = true;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "#94a3b8";
    /// <summary>
    /// Gets or sets border width millimetres.
    /// </summary>
    public double BorderWidthMm { get; set; } = 0.25;
    /// <summary>
    /// Gets or sets content fit.
    /// </summary>
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    /// <summary>
    /// Gets or sets content offset horizontal position.
    /// </summary>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets content offset vertical position.
    /// </summary>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets content scale.
    /// </summary>
    public double ContentScale { get; set; } = 1;
}

/// <summary>
/// Represents an image frame element.
/// </summary>
public sealed class ImageFrameElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Image;
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets original data URL.
    /// </summary>
    public string OriginalDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets alt text.
    /// </summary>
    public string AltText { get; set; } = "Picture";
    /// <summary>
    /// Gets or sets crop horizontal position.
    /// </summary>
    public double CropX { get; set; }
    /// <summary>
    /// Gets or sets crop vertical position.
    /// </summary>
    public double CropY { get; set; }
    /// <summary>
    /// Gets or sets crop scale.
    /// </summary>
    public double CropScale { get; set; } = 1;
    /// <summary>
    /// Gets or sets image rotation.
    /// </summary>
    public double ImageRotation { get; set; }
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = 1;
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
    /// Gets or sets invert.
    /// </summary>
    public double Invert { get; set; }
    /// <summary>
    /// Gets or sets grayscale.
    /// </summary>
    public double Grayscale { get; set; }
    /// <summary>
    /// Gets or sets sepia.
    /// </summary>
    public double Sepia { get; set; }
    /// <summary>
    /// Gets or sets blur.
    /// </summary>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets flip horizontal.
    /// </summary>
    public bool FlipHorizontal { get; set; }
    /// <summary>
    /// Gets or sets flip vertical.
    /// </summary>
    public bool FlipVertical { get; set; }
    /// <summary>
    /// Gets or sets fit inside frame.
    /// </summary>
    public bool FitInsideFrame { get; set; }
    /// <summary>
    /// Gets or sets mask shape.
    /// </summary>
    public ImageMaskShape MaskShape { get; set; }
    /// <summary>
    /// Gets or sets corner radius millimetres.
    /// </summary>
    public double CornerRadiusMm { get; set; } = 4;
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets border width millimetres.
    /// </summary>
    public double BorderWidthMm { get; set; }
    /// <summary>
    /// Gets or sets shadow enabled.
    /// </summary>
    public bool ShadowEnabled { get; set; }
    /// <summary>
    /// Gets or sets tint mode.
    /// </summary>
    public ImageTintMode TintMode { get; set; }
    /// <summary>
    /// Gets or sets blend mode.
    /// </summary>
    public ImageBlendMode BlendMode { get; set; } = ImageBlendMode.Normal;
    /// <summary>
    /// Gets or sets tint color.
    /// </summary>
    public string TintColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets tint opacity.
    /// </summary>
    public double TintOpacity { get; set; }
    /// <summary>
    /// Gets or sets transparent color.
    /// </summary>
    public string TransparentColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets transparent color tolerance.
    /// </summary>
    public int TransparentColorTolerance { get; set; } = 24;
    /// <summary>
    /// Gets or sets picture source.
    /// </summary>
    public PictureDocument? PictureSource { get; set; }
}

/// <summary>
/// Represents a publication media element.
/// </summary>
public abstract class PublicationMediaElement : PublicationElement
{
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets trim start seconds.
    /// </summary>
    public double TrimStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets trim end seconds.
    /// </summary>
    public double TrimEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets timeline start seconds.
    /// </summary>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets volume.
    /// </summary>
    public double Volume { get; set; } = 1;
    /// <summary>
    /// Gets or sets playback rate.
    /// </summary>
    public double PlaybackRate { get; set; } = 1;
    /// <summary>
    /// Gets or sets fade in seconds.
    /// </summary>
    public double FadeInSeconds { get; set; }
    /// <summary>
    /// Gets or sets fade out seconds.
    /// </summary>
    public double FadeOutSeconds { get; set; }
    /// <summary>
    /// Gets or sets muted.
    /// </summary>
    public bool Muted { get; set; }
    /// <summary>
    /// Gets or sets loop.
    /// </summary>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets auto play.
    /// </summary>
    public bool AutoPlay { get; set; } = true;
    /// <summary>
    /// Gets or sets playback trigger.
    /// </summary>
    public PublicationMediaPlaybackTrigger PlaybackTrigger { get; set; } = PublicationMediaPlaybackTrigger.OnPageEnter;
    /// <summary>
    /// Gets or sets waveform samples.
    /// </summary>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets segments.
    /// </summary>
    public List<PublicationMediaSegment> Segments { get; set; } = [];

    /// <summary>
    /// Gets effective trim end seconds.
    /// </summary>
    [JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    /// <summary>
    /// Gets effective segments.
    /// </summary>
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

    /// <summary>
    /// Gets timeline length seconds.
    /// </summary>
    [JsonIgnore]
    public double TimelineLengthSeconds => Math.Max(.05, EffectiveSegments.Sum(segment =>
        segment.TimelineDurationSeconds > 0
            ? segment.TimelineDurationSeconds
            : segment.SourceLengthSeconds / Math.Max(.0001, Math.Abs(segment.Speed))) / Math.Max(.1, PlaybackRate));
}

/// <summary>
/// Represents a video element.
/// </summary>
public sealed class VideoElement : PublicationMediaElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Video;
    /// <summary>
    /// Gets or sets poster data URL.
    /// </summary>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets alt text.
    /// </summary>
    public string AltText { get; set; } = "Video";
    /// <summary>
    /// Gets or sets show controls.
    /// </summary>
    public bool ShowControls { get; set; } = true;
    /// <summary>
    /// Gets or sets fit mode.
    /// </summary>
    public PublicationVideoFitMode FitMode { get; set; } = PublicationVideoFitMode.Stretch;
    /// <summary>
    /// Gets or sets fit mode explicit.
    /// </summary>
    public bool FitModeExplicit { get; set; }
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets video project.
    /// </summary>
    public VideoProjectDocument? VideoProject { get; set; }
    /// <summary>
    /// Gets or sets frame clip polygon.
    /// </summary>
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
}

/// <summary>
/// Represents an audio element.
/// </summary>
public sealed class AudioElement : PublicationMediaElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Audio;
    /// <summary>
    /// Gets or sets display kind.
    /// </summary>
    public PublicationAudioDisplayKind DisplayKind { get; set; } = PublicationAudioDisplayKind.Waveform;
    /// <summary>
    /// Gets or sets accent color.
    /// </summary>
    public string AccentColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets show controls.
    /// </summary>
    public bool ShowControls { get; set; } = true;
}

/// <summary>
/// Represents a shape element.
/// </summary>
public sealed class ShapeElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Shape;
    /// <summary>
    /// Gets or sets shape.
    /// </summary>
    public PublicationShape Shape { get; set; } = PublicationShape.Rectangle;
    /// <summary>
    /// Gets or sets fill.
    /// </summary>
    public string Fill { get; set; } = "#dbeafe";
    /// <summary>
    /// Gets or sets stroke.
    /// </summary>
    public string Stroke { get; set; } = "#1d4ed8";
    /// <summary>
    /// Gets or sets stroke width.
    /// </summary>
    public double StrokeWidth { get; set; } = 0.4;
    /// <summary>
    /// Gets or sets corner radius millimetres.
    /// </summary>
    public double CornerRadiusMm { get; set; } = 3;
}

/// <summary>
/// Represents a barcode element.
/// </summary>
public sealed class BarcodeElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Barcode;
    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public PublicationBarcodeFormat Format { get; set; } = PublicationBarcodeFormat.QrCode;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public string Value { get; set; } = "https://github.com/Michi0403/BlazorPublisher";
    /// <summary>
    /// Gets or sets foreground color.
    /// </summary>
    public string ForegroundColor { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets transparent background.
    /// </summary>
    public bool TransparentBackground { get; set; }
    /// <summary>
    /// Gets or sets show text.
    /// </summary>
    public bool ShowText { get; set; } = true;
    /// <summary>
    /// Gets or sets margin.
    /// </summary>
    public int Margin { get; set; } = 8;
    /// <summary>
    /// Gets or sets line width.
    /// </summary>
    public int LineWidth { get; set; } = 2;
    /// <summary>
    /// Gets or sets bar height.
    /// </summary>
    public int BarHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets font size.
    /// </summary>
    public int FontSize { get; set; } = 16;
    /// <summary>
    /// Gets or sets error correction.
    /// </summary>
    public PublicationBarcodeErrorCorrection ErrorCorrection { get; set; } = PublicationBarcodeErrorCorrection.M;
    /// <summary>
    /// Gets or sets module shape.
    /// </summary>
    public PublicationBarcodeModuleShape ModuleShape { get; set; } = PublicationBarcodeModuleShape.Square;
    /// <summary>
    /// Gets or sets SVG markup.
    /// </summary>
    public string SvgMarkup { get; set; } = string.Empty;
}

/// <summary>
/// Represents a connector endpoint.
/// </summary>
public sealed class ConnectorEndpoint
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public ConnectorEndpointKind Kind { get; set; } = ConnectorEndpointKind.Element;
    /// <summary>
    /// Gets or sets element identifier.
    /// </summary>
    public Guid ElementId { get; set; }
    /// <summary>
    /// Gets or sets anchor.
    /// </summary>
    public ConnectorAnchor Anchor { get; set; } = ConnectorAnchor.Right;
    /// <summary>
    /// Gets or sets port identifier.
    /// </summary>
    public Guid? PortId { get; set; }
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets target selector.
    /// </summary>
    public string TargetSelector { get; set; } = string.Empty;
}

/// <summary>
/// Represents a signal connector settings.
/// </summary>
public sealed class SignalConnectorSettings
{
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets line visible.
    /// </summary>
    public bool LineVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets trigger.
    /// </summary>
    public SignalConnectorTrigger Trigger { get; set; } = SignalConnectorTrigger.OnPageEnter;
    /// <summary>
    /// Gets or sets visual.
    /// </summary>
    public SignalConnectorVisual Visual { get; set; } = SignalConnectorVisual.FlyingArrow;
    /// <summary>
    /// Gets or sets delay seconds.
    /// </summary>
    public double DelaySeconds { get; set; }
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; } = 1.5;
    /// <summary>
    /// Gets or sets repeat count.
    /// </summary>
    public int RepeatCount { get; set; } = 1;
    /// <summary>
    /// Gets or sets loop.
    /// </summary>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets auto reverse.
    /// </summary>
    public bool AutoReverse { get; set; }
    /// <summary>
    /// Gets or sets start gesture.
    /// </summary>
    public SignalGesture StartGesture { get; set; }
    /// <summary>
    /// Gets or sets end gesture.
    /// </summary>
    public SignalGesture EndGesture { get; set; }
    /// <summary>
    /// Gets or sets motion target element identifier.
    /// </summary>
    public Guid? MotionTargetElementId { get; set; }
    /// <summary>
    /// Gets or sets motion target selector.
    /// </summary>
    public string MotionTargetSelector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets translate xpercent.
    /// </summary>
    public double TranslateXPercent { get; set; }
    /// <summary>
    /// Gets or sets translate ypercent.
    /// </summary>
    public double TranslateYPercent { get; set; }
    /// <summary>
    /// Gets or sets scale.
    /// </summary>
    public double Scale { get; set; } = 1;
    /// <summary>
    /// Gets or sets resize width percent.
    /// </summary>
    public double ResizeWidthPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets resize height percent.
    /// </summary>
    public double ResizeHeightPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets rotation degrees.
    /// </summary>
    public double RotationDegrees { get; set; }
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets restore motion after run.
    /// </summary>
    public bool RestoreMotionAfterRun { get; set; }
    /// <summary>
    /// Gets or sets completion target element identifier.
    /// </summary>
    public Guid? CompletionTargetElementId { get; set; }
    /// <summary>
    /// Gets or sets completion target selector.
    /// </summary>
    public string CompletionTargetSelector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets completion action.
    /// </summary>
    public SignalCompletionAction CompletionAction { get; set; }
    /// <summary>
    /// Gets or sets completion value.
    /// </summary>
    public string CompletionValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets completion duration seconds.
    /// </summary>
    public double CompletionDurationSeconds { get; set; } = .8;
    /// <summary>
    /// Gets or sets next connector identifier.
    /// </summary>
    public Guid? NextConnectorId { get; set; }
}

/// <summary>
/// Represents a connector element.
/// </summary>
public sealed class ConnectorElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Connector;
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public ConnectorEndpoint Source { get; set; } = new();
    /// <summary>
    /// Gets or sets target.
    /// </summary>
    public ConnectorEndpoint Target { get; set; } = new();
    /// <summary>
    /// Gets or sets path kind.
    /// </summary>
    public ConnectorPathKind PathKind { get; set; } = ConnectorPathKind.Curved;
    /// <summary>
    /// Gets or sets start marker.
    /// </summary>
    public ConnectorMarker StartMarker { get; set; }
    /// <summary>
    /// Gets or sets end marker.
    /// </summary>
    public ConnectorMarker EndMarker { get; set; } = ConnectorMarker.Arrow;
    /// <summary>
    /// Gets or sets dash style.
    /// </summary>
    public ConnectorDashStyle DashStyle { get; set; }
    /// <summary>
    /// Gets or sets stroke.
    /// </summary>
    public string Stroke { get; set; } = "#245b85";
    /// <summary>
    /// Gets or sets stroke width millimetres.
    /// </summary>
    public double StrokeWidthMm { get; set; } = 0.7;
    /// <summary>
    /// Gets or sets control1 horizontal position.
    /// </summary>
    public double? Control1X { get; set; }
    /// <summary>
    /// Gets or sets control1 vertical position.
    /// </summary>
    public double? Control1Y { get; set; }
    /// <summary>
    /// Gets or sets control2 horizontal position.
    /// </summary>
    public double? Control2X { get; set; }
    /// <summary>
    /// Gets or sets control2 vertical position.
    /// </summary>
    public double? Control2Y { get; set; }
    /// <summary>
    /// Gets or sets signal.
    /// </summary>
    public SignalConnectorSettings Signal { get; set; } = new();
}

/// <summary>
/// Represents a word art element.
/// </summary>
public sealed class WordArtElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.WordArt;
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = "WordArt";
    /// <summary>
    /// Gets or sets font family.
    /// </summary>
    public string FontFamily { get; set; } = "Arial Black";
    /// <summary>
    /// Gets or sets font size pt.
    /// </summary>
    public double FontSizePt { get; set; } = 42;
    /// <summary>
    /// Gets or sets bold.
    /// </summary>
    public bool Bold { get; set; } = true;
    /// <summary>
    /// Gets or sets italic.
    /// </summary>
    public bool Italic { get; set; }
    /// <summary>
    /// Gets or sets letter spacing.
    /// </summary>
    public double LetterSpacing { get; set; }
    /// <summary>
    /// Gets or sets fill color.
    /// </summary>
    public string FillColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets secondary color.
    /// </summary>
    public string SecondaryColor { get; set; } = "#8ec5ff";
    /// <summary>
    /// Gets or sets gradient fill.
    /// </summary>
    public bool GradientFill { get; set; } = true;
    /// <summary>
    /// Gets or sets fill kind.
    /// </summary>
    public WordArtFillKind FillKind { get; set; } = WordArtFillKind.Gradient;
    /// <summary>
    /// Gets or sets fill media data URL.
    /// </summary>
    public string FillMediaDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fill media mime type.
    /// </summary>
    public string FillMediaMimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fill media poster data URL.
    /// </summary>
    public string FillMediaPosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fill media fit mode.
    /// </summary>
    public PublicationVideoFitMode FillMediaFitMode { get; set; } = PublicationVideoFitMode.Cover;
    /// <summary>
    /// Gets or sets fill media loop.
    /// </summary>
    public bool FillMediaLoop { get; set; } = true;
    /// <summary>
    /// Gets or sets fill media scale.
    /// </summary>
    public double FillMediaScale { get; set; } = 1;
    /// <summary>
    /// Gets or sets fill media offset xpercent.
    /// </summary>
    public double FillMediaOffsetXPercent { get; set; }
    /// <summary>
    /// Gets or sets fill media offset ypercent.
    /// </summary>
    public double FillMediaOffsetYPercent { get; set; }
    /// <summary>
    /// Gets or sets outline color.
    /// </summary>
    public string OutlineColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets outline width.
    /// </summary>
    public double OutlineWidth { get; set; } = 2;
    /// <summary>
    /// Gets or sets shadow enabled.
    /// </summary>
    public bool ShadowEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets shadow color.
    /// </summary>
    public string ShadowColor { get; set; } = "#00000080";
    /// <summary>
    /// Gets or sets extrude depth.
    /// </summary>
    public double ExtrudeDepth { get; set; } = 4;
    /// <summary>
    /// Gets or sets extrude color.
    /// </summary>
    public string ExtrudeColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets warp.
    /// </summary>
    public WordArtWarp Warp { get; set; }
    /// <summary>
    /// Gets or sets custom path points.
    /// </summary>
    public List<WordArtPathPoint> CustomPathPoints { get; set; } = [];
    /// <summary>
    /// Gets or sets path start offset percent.
    /// </summary>
    public double PathStartOffsetPercent { get; set; } = 50;
    /// <summary>
    /// Gets or sets path baseline offset.
    /// </summary>
    public double PathBaselineOffset { get; set; }
}

/// <summary>
/// Represents a publication playback settings.
/// </summary>
public sealed class PublicationPlaybackSettings
{
    /// <summary>
    /// Gets or sets loop presentation.
    /// </summary>
    public bool LoopPresentation { get; set; }
    /// <summary>
    /// Gets or sets start automatically.
    /// </summary>
    public bool StartAutomatically { get; set; } = true;
    /// <summary>
    /// Gets or sets show controls.
    /// </summary>
    public bool ShowControls { get; set; } = true;
}

/// <summary>
/// Represents a publication page transition.
/// </summary>
public sealed class PublicationPageTransition
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public PublicationPageTransitionKind Kind { get; set; } = PublicationPageTransitionKind.Fade;
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    /// <summary>
    /// Gets or sets easing.
    /// </summary>
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseInOut;
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; } = .55;
    /// <summary>
    /// Gets or sets advance on click.
    /// </summary>
    public bool AdvanceOnClick { get; set; } = true;
    /// <summary>
    /// Gets or sets auto advance.
    /// </summary>
    public bool AutoAdvance { get; set; }
    /// <summary>
    /// Gets or sets auto advance seconds.
    /// </summary>
    public double AutoAdvanceSeconds { get; set; } = 5;
}

/// <summary>
/// Represents a publication animation.
/// </summary>
public sealed class PublicationAnimation
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Animation";
    /// <summary>
    /// Gets or sets order.
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets phase.
    /// </summary>
    public PublicationAnimationPhase Phase { get; set; } = PublicationAnimationPhase.Entrance;
    /// <summary>
    /// Gets or sets effect.
    /// </summary>
    public PublicationAnimationEffect Effect { get; set; } = PublicationAnimationEffect.Fade;
    /// <summary>
    /// Gets or sets trigger.
    /// </summary>
    public PublicationAnimationTrigger Trigger { get; set; } = PublicationAnimationTrigger.AfterPrevious;
    /// <summary>
    /// Gets or sets easing.
    /// </summary>
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseOut;
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; } = .6;
    /// <summary>
    /// Gets or sets delay seconds.
    /// </summary>
    public double DelaySeconds { get; set; }
    /// <summary>
    /// Gets or sets timeline start seconds.
    /// </summary>
    public double? TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets distance percent.
    /// </summary>
    public double DistancePercent { get; set; } = 18;
    /// <summary>
    /// Gets or sets scale percent.
    /// </summary>
    public double ScalePercent { get; set; } = 20;
    /// <summary>
    /// Gets or sets rotation degrees.
    /// </summary>
    public double RotationDegrees { get; set; } = 360;
    /// <summary>
    /// Gets or sets repeat count.
    /// </summary>
    public int RepeatCount { get; set; } = 1;
    /// <summary>
    /// Gets or sets auto reverse.
    /// </summary>
    public bool AutoReverse { get; set; }
}

/// <summary>
/// Represents a publication interaction.
/// </summary>
public sealed class PublicationInteraction
{
    /// <summary>
    /// Gets or sets action.
    /// </summary>
    public PublicationInteractionAction Action { get; set; }
    /// <summary>
    /// Gets or sets target page identifier.
    /// </summary>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets target element identifier.
    /// </summary>
    public Guid? TargetElementId { get; set; }
    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open in new window.
    /// </summary>
    public bool OpenInNewWindow { get; set; } = true;
}


/// <summary>
/// Represents a publication field record.
/// </summary>
public sealed class PublicationFieldRecord
{
    /// <summary>
    /// Gets or sets publication name.
    /// </summary>
    public string PublicationName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets page name.
    /// </summary>
    public string PageName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets story name.
    /// </summary>
    public string StoryName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets page number.
    /// </summary>
    public int PageNumber { get; set; }
    /// <summary>
    /// Gets or sets page count.
    /// </summary>
    public int PageCount { get; set; }
    /// <summary>
    /// Gets or sets current date.
    /// </summary>
    public DateTime CurrentDate { get; set; }
    /// <summary>
    /// Gets or sets current date time.
    /// </summary>
    public DateTime CurrentDateTime { get; set; }
}
