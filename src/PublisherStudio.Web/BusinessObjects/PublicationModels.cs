using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents publication state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class PublicationDocument
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationDocument"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationDocument"/>.</value>
    public string Name { get; set; } = "Untitled Publication";
    /// <summary>
    /// Gets or sets the format version value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format version value exposed by <see cref="PublicationDocument"/>.</value>
    public string FormatVersion { get; set; } = "1.55";
    /// <summary>
    /// Gets or sets the modified UTC associated with this publication state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The modified UTC value exposed by <see cref="PublicationDocument"/>.</value>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the zoom value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The zoom value exposed by <see cref="PublicationDocument"/>.</value>
    public double Zoom { get; set; } = 0.8;
    /// <summary>
    /// Gets or sets the view value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The view value exposed by <see cref="PublicationDocument"/>.</value>
    public PublicationViewSettings View { get; set; } = new();
    /// <summary>
    /// Gets or sets the pages collection maintained or exposed by this publication instance for downstream processing.
    /// </summary>
    /// <value>The pages value exposed by <see cref="PublicationDocument"/>.</value>
    public List<PublicationPage> Pages { get; set; } = [];
    /// <summary>
    /// Gets or sets the data objects collection maintained or exposed by this publication instance for downstream processing.
    /// </summary>
    /// <value>The data objects value exposed by <see cref="PublicationDocument"/>.</value>
    public List<PublicationDataObject> DataObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets the component templates collection maintained or exposed by this publication instance for downstream processing.
    /// </summary>
    /// <value>The component templates value exposed by <see cref="PublicationDocument"/>.</value>
    public List<PublicationElementTemplate> ComponentTemplates { get; set; } = [];
    /// <summary>
    /// Gets or sets the playback value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The playback value exposed by <see cref="PublicationDocument"/>.</value>
    public PublicationPlaybackSettings Playback { get; set; } = new();
    /// <summary>
    /// Gets or sets the streaming value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The streaming value exposed by <see cref="PublicationDocument"/>.</value>
    public PublicationStreamingSettings Streaming { get; set; } = new();
    /// <summary>
    /// Gets or sets the project settings value that forms part of the publication state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project settings value exposed by <see cref="PublicationDocument"/>.</value>
    public PublicationProjectSettings ProjectSettings { get; set; } = new();

}

/// <summary>
/// Carries the configurable publication view settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationViewSettings
{
    /// <summary>
    /// Gets or sets the ruler unit value that forms part of the publication view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ruler unit value exposed by <see cref="PublicationViewSettings"/>.</value>
    public MeasurementUnit RulerUnit { get; set; } = MeasurementUnit.Millimeter;
    /// <summary>
    /// Gets or sets a value indicating whether rulers visible applies to the publication view state.
    /// </summary>
    /// <value>The rulers visible value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool RulersVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether grid visible applies to the publication view state.
    /// </summary>
    /// <value>The grid visible value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool GridVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether guides visible applies to the publication view state.
    /// </summary>
    /// <value>The guides visible value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool GuidesVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether snap to grid applies to the publication view state.
    /// </summary>
    /// <value>The snap to grid value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool SnapToGrid { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether snap to guides applies to the publication view state.
    /// </summary>
    /// <value>The snap to guides value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool SnapToGuides { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether snap to page applies to the publication view state.
    /// </summary>
    /// <value>The snap to page value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool SnapToPage { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether snap to objects applies to the publication view state.
    /// </summary>
    /// <value>The snap to objects value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool SnapToObjects { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether snap in objects applies to the publication view state.
    /// </summary>
    /// <value>The snap in objects value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool SnapInObjects { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether panel library visible applies to the publication view state.
    /// </summary>
    /// <value>The panel library visible value exposed by <see cref="PublicationViewSettings"/>.</value>
    public bool PanelLibraryVisible { get; set; }
    /// <summary>
    /// Gets or sets the grid spacing mm value that forms part of the publication view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grid spacing mm value exposed by <see cref="PublicationViewSettings"/>.</value>
    public double GridSpacingMm { get; set; } = 2.5;
    /// <summary>
    /// Gets or sets the export DPI value that forms part of the publication view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The export DPI value exposed by <see cref="PublicationViewSettings"/>.</value>
    public int ExportDpi { get; set; } = 150;
    /// <summary>
    /// Gets or sets the canvas zoom mode value that forms part of the publication view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas zoom mode value exposed by <see cref="PublicationViewSettings"/>.</value>
    public PublicationCanvasZoomMode CanvasZoomMode { get; set; } = PublicationCanvasZoomMode.CssLayout;
}

/// <summary>
/// Represents a publication page application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationPage
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication page instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationPage"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationPage"/>.</value>
    public string Name { get; set; } = "Page 1";
    /// <summary>
    /// Gets or sets the width mm value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width mm value exposed by <see cref="PublicationPage"/>.</value>
    public double WidthMm { get; set; } = 210;
    /// <summary>
    /// Gets or sets the height mm value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height mm value exposed by <see cref="PublicationPage"/>.</value>
    public double HeightMm { get; set; } = 297;
    /// <summary>
    /// Gets or sets the background value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="PublicationPage"/>.</value>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the elements collection maintained or exposed by this publication page instance for downstream processing.
    /// </summary>
    /// <value>The elements value exposed by <see cref="PublicationPage"/>.</value>
    public List<PublicationElement> Elements { get; set; } = [];
    /// <summary>
    /// Gets or sets the guides collection maintained or exposed by this publication page instance for downstream processing.
    /// </summary>
    /// <value>The guides value exposed by <see cref="PublicationPage"/>.</value>
    public List<GuideLine> Guides { get; set; } = [];
    /// <summary>
    /// Gets or sets the transition value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transition value exposed by <see cref="PublicationPage"/>.</value>
    public PublicationPageTransition Transition { get; set; } = new();
    /// <summary>
    /// Gets or sets the timeline duration seconds value that forms part of the publication page state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline duration seconds value exposed by <see cref="PublicationPage"/>.</value>
    public double TimelineDurationSeconds { get; set; } = 10;

}

/// <summary>
/// Represents a guide line application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class GuideLine
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this guide line instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="GuideLine"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the orientation value that forms part of the guide line state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The orientation value exposed by <see cref="GuideLine"/>.</value>
    public GuideOrientation Orientation { get; set; }
    /// <summary>
    /// Gets or sets the position mm value that forms part of the guide line state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The position mm value exposed by <see cref="GuideLine"/>.</value>
    public double PositionMm { get; set; }
}

/// <summary>
/// Defines the supported guide orientation values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum GuideOrientation { Horizontal, Vertical }
/// <summary>
/// Defines the supported measurement unit values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum MeasurementUnit { Millimeter, Centimeter, Inch, Pixel }
/// <summary>
/// Defines the supported publication canvas zoom mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationCanvasZoomMode { CssLayout, Transform }
/// <summary>
/// Defines the supported publication HTML export support values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationHtmlExportSupport { Native, CanvasRuntime, RenderBeforeExport }
/// <summary>
/// Defines the supported publication element kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationElementKind { Text, Image, Video, Audio, Shape, WordArt, Connector, DataVisual, Barcode, Spreadsheet, DevExtremeComponent, LiveSource, Panel, HtmlEmbed }
/// <summary>
/// Defines the supported publication shape values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationShape { Rectangle, RoundedRectangle, Ellipse, Line }
/// <summary>
/// Defines the supported connector path kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorPathKind { Straight, Elbow, Curved }
/// <summary>
/// Defines the supported connector marker values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorMarker { None, Arrow, Triangle, Diamond }
/// <summary>
/// Defines the supported connector dash style values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorDashStyle { Solid, Dash, Dot }
/// <summary>
/// Defines the supported connector anchor values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorAnchor { TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Center }
/// <summary>
/// Defines the supported connector tool kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorToolKind { None, Line, Arrow, SignalConnector, SignalArrow }
/// <summary>
/// Defines the supported connector endpoint kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ConnectorEndpointKind { Element, Canvas }
/// <summary>
/// Defines the supported signal connector trigger values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SignalConnectorTrigger { OnPageEnter, OnClick, OnHover, Manual }
/// <summary>
/// Defines the supported signal connector visual values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SignalConnectorVisual { FlyingArrow, DrawPath, Pulse, None }
/// <summary>
/// Defines the supported signal gesture values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SignalGesture { None, Click, Hover }
/// <summary>
/// Defines the supported signal completion action values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SignalCompletionAction
{
    None, Click, Hover, Show, Hide, ToggleVisibility, SetOpacity, ReplayAnimation,
    PlayMedia, PauseMedia, ToggleMediaPlayback, Highlight, AddCssClass, RemoveCssClass,
    ToggleCssClass, RunSignal
}
/// <summary>
/// Defines the supported image mask shape values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ImageMaskShape { Rectangle, RoundedRectangle, Ellipse }
/// <summary>
/// Defines the supported story storage format values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum StoryStorageFormat { Html, OpenXml }
/// <summary>
/// Defines the supported spreadsheet storage format values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SpreadsheetStorageFormat { Xlsx, Xlsm, Xls, Csv, Text }
/// <summary>
/// Defines the supported publication content fit mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationContentFitMode { Clip, Fit, Fill, Stretch }
/// <summary>
/// Defines the supported image tint mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ImageTintMode { Overlay, Recolor }
/// <summary>
/// Defines the supported image blend mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum ImageBlendMode { Normal, Multiply, Screen, Darken, Lighten }
/// <summary>
/// Defines the supported word art warp values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum WordArtWarp { None, ArchUp, ArchDown, Wave, Custom }
/// <summary>
/// Defines the supported word art fill kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum WordArtFillKind { Solid, Gradient, Picture, Video }
/// <summary>
/// Defines the supported publication animation phase values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAnimationPhase { Entrance, Emphasis, Exit, Motion }
/// <summary>
/// Defines the supported publication animation effect values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAnimationEffect { Fade, Fly, Float, Zoom, Wipe, Bounce, Pulse, Spin, Shake, GrowShrink, Move, PlayMedia, PauseMedia, StopMedia }
/// <summary>
/// Defines the supported publication animation trigger values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAnimationTrigger { OnPageEnter, WithPrevious, AfterPrevious, OnClick }
/// <summary>
/// Defines the supported publication animation easing values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAnimationEasing { Linear, EaseIn, EaseOut, EaseInOut, BackOut, BounceOut }
/// <summary>
/// Defines the supported publication animation direction values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAnimationDirection { None, Left, Right, Up, Down }
/// <summary>
/// Defines the supported publication page transition kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationPageTransitionKind { None, Fade, Push, Wipe, Zoom, Flip }
/// <summary>
/// Defines the supported publication interaction action values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationInteractionAction { None, NextPage, PreviousPage, GoToPage, OpenUrl, ToggleVisibility, Show, Hide, ReplayAnimation, PlayMedia, PauseMedia, ToggleMediaPlayback }
/// <summary>
/// Defines the supported publication media playback trigger values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationMediaPlaybackTrigger { OnPageEnter, OnClick, WithAnimation }
/// <summary>
/// Defines the supported publication audio display kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAudioDisplayKind { Waveform, Compact, Hidden }
/// <summary>
/// Defines the supported publication video fit mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationVideoFitMode { Contain, Cover, Stretch }
/// <summary>
/// Defines the supported publication barcode format values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationBarcodeFormat { QrCode, Code128, Code39, Ean13, UpcA, Itf14, Codabar }
/// <summary>
/// Defines the supported publication barcode error correction values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationBarcodeErrorCorrection { L, M, Q, H }
/// <summary>
/// Defines the supported publication barcode module shape values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationBarcodeModuleShape { Square, Rounded, Dots }

/// <summary>
/// Represents the input contract for canvas insert, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="Kind">Kind value supplied to the canvas insert operation and used when producing its result.</param>
/// <param name="X">X value supplied to the canvas insert operation and used when producing its result.</param>
/// <param name="Y">Y value supplied to the canvas insert operation and used when producing its result.</param>
public sealed record CanvasInsertRequest(string Kind, double X, double Y);
/// <summary>
/// Represents the input contract for external file drop, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="AssetId">Identifier of the asset to use for this operation.</param>
/// <param name="Kind">Kind value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="MimeType">Mime type value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="Size">Size value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="DurationSeconds">Duration seconds value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="PixelWidth">Pixel width value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="PixelHeight">Pixel height value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="X">X value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="Y">Y value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="TargetElementId">Identifier of the target element to use for this operation.</param>
/// <param name="TargetElementKind">Target element kind value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="TargetX">Target x value supplied to the external file drop operation and used when producing its result.</param>
/// <param name="TargetY">Target y value supplied to the external file drop operation and used when producing its result.</param>
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
/// Represents a publication connector port application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationConnectorPort
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication connector port instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationConnectorPort"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication connector port state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationConnectorPort"/>.</value>
    public string Name { get; set; } = "Connector point";
    /// <summary>
    /// Gets or sets the x percent value that forms part of the publication connector port state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x percent value exposed by <see cref="PublicationConnectorPort"/>.</value>
    public double XPercent { get; set; } = .5;
    /// <summary>
    /// Gets or sets the y percent value that forms part of the publication connector port state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y percent value exposed by <see cref="PublicationConnectorPort"/>.</value>
    public double YPercent { get; set; } = .5;
}

/// <summary>
/// Represents a publication element application type, grouping the state and behavior that belong to that domain concept.
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
    /// Gets or sets the stable identifier used to identify or correlate this publication element instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationElement"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationElement"/>.</value>
    public string Name { get; set; } = "Element";
    /// <summary>
    /// Gets the kind value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PublicationElement"/>.</value>
    public abstract PublicationElementKind Kind { get; }
    /// <summary>
    /// Gets or sets the x value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="PublicationElement"/>.</value>
    public double X { get; set; } = 20;
    /// <summary>
    /// Gets or sets the y value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="PublicationElement"/>.</value>
    public double Y { get; set; } = 20;
    /// <summary>
    /// Gets or sets the width value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationElement"/>.</value>
    public double Width { get; set; } = 60;
    /// <summary>
    /// Gets or sets the height value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PublicationElement"/>.</value>
    public double Height { get; set; } = 40;
    /// <summary>
    /// Gets or sets the rotation value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rotation value exposed by <see cref="PublicationElement"/>.</value>
    public double Rotation { get; set; }
    /// <summary>
    /// Gets or sets the z index value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The z index value exposed by <see cref="PublicationElement"/>.</value>
    public int ZIndex { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the publication element state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="PublicationElement"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether locked applies to the publication element state.
    /// </summary>
    /// <value>The locked value exposed by <see cref="PublicationElement"/>.</value>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether hidden at presentation start applies to the publication element state.
    /// </summary>
    /// <value>The hidden at presentation start value exposed by <see cref="PublicationElement"/>.</value>
    public bool HiddenAtPresentationStart { get; set; }
    /// <summary>
    /// Gets or sets the stable group identifier used to identify or correlate this publication element instance with related application state.
    /// </summary>
    /// <value>The group identifier value exposed by <see cref="PublicationElement"/>.</value>
    public Guid? GroupId { get; set; }
    /// <summary>
    /// Gets or sets the animations collection maintained or exposed by this publication element instance for downstream processing.
    /// </summary>
    /// <value>The animations value exposed by <see cref="PublicationElement"/>.</value>
    public List<PublicationAnimation> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets the interaction value that forms part of the publication element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value exposed by <see cref="PublicationElement"/>.</value>
    public PublicationInteraction Interaction { get; set; } = new();
    /// <summary>
    /// Gets or sets the connector ports collection maintained or exposed by this publication element instance for downstream processing.
    /// </summary>
    /// <value>The connector ports value exposed by <see cref="PublicationElement"/>.</value>
    public List<PublicationConnectorPort> ConnectorPorts { get; set; } = [];
}

/// <summary>
/// Represents a text frame element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class TextFrameElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="TextFrameElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Text;
    /// <summary>
    /// Gets or sets the preview HTML value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preview HTML value exposed by <see cref="TextFrameElement"/>.</value>
    public string PreviewHtml { get; set; } = "<p>Text frame</p>";
    /// <summary>
    /// Gets or sets the document content value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document content value exposed by <see cref="TextFrameElement"/>.</value>
    public byte[] DocumentContent { get; set; } = [];
    /// <summary>
    /// Gets or sets the story format value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story format value exposed by <see cref="TextFrameElement"/>.</value>
    public StoryStorageFormat StoryFormat { get; set; } = StoryStorageFormat.OpenXml;
    /// <summary>
    /// Gets or sets the document background value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document background value exposed by <see cref="TextFrameElement"/>.</value>
    public string DocumentBackground { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the padding mm value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The padding mm value exposed by <see cref="TextFrameElement"/>.</value>
    public double PaddingMm { get; set; } = 2;
    /// <summary>
    /// Gets or sets the background value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="TextFrameElement"/>.</value>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the border color value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="TextFrameElement"/>.</value>
    public string BorderColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the border width value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width value exposed by <see cref="TextFrameElement"/>.</value>
    public double BorderWidth { get; set; }
    /// <summary>
    /// Gets or sets the content fit value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content fit value exposed by <see cref="TextFrameElement"/>.</value>
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    /// <summary>
    /// Gets or sets the content offset x value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset x value exposed by <see cref="TextFrameElement"/>.</value>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets the content offset y value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset y value exposed by <see cref="TextFrameElement"/>.</value>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets the content scale value that forms part of the text frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content scale value exposed by <see cref="TextFrameElement"/>.</value>
    public double ContentScale { get; set; } = 1;
}

/// <summary>
/// Represents a spreadsheet element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SpreadsheetElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="SpreadsheetElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Spreadsheet;
    /// <summary>
    /// Gets or sets the workbook content value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workbook content value exposed by <see cref="SpreadsheetElement"/>.</value>
    public byte[] WorkbookContent { get; set; } = [];
    /// <summary>
    /// Gets or sets the workbook file name used by this spreadsheet element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The workbook file name value exposed by <see cref="SpreadsheetElement"/>.</value>
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets the storage format value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The storage format value exposed by <see cref="SpreadsheetElement"/>.</value>
    public SpreadsheetStorageFormat StorageFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    /// <summary>
    /// Gets or sets the preview HTML value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preview HTML value exposed by <see cref="SpreadsheetElement"/>.</value>
    public string PreviewHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the active sheet name value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active sheet name value exposed by <see cref="SpreadsheetElement"/>.</value>
    public string ActiveSheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets a value indicating whether show sheet name applies to the spreadsheet element state.
    /// </summary>
    /// <value>The show sheet name value exposed by <see cref="SpreadsheetElement"/>.</value>
    public bool ShowSheetName { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show grid lines applies to the spreadsheet element state.
    /// </summary>
    /// <value>The show grid lines value exposed by <see cref="SpreadsheetElement"/>.</value>
    public bool ShowGridLines { get; set; } = true;
    /// <summary>
    /// Gets or sets the background value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="SpreadsheetElement"/>.</value>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the border color value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="SpreadsheetElement"/>.</value>
    public string BorderColor { get; set; } = "#94a3b8";
    /// <summary>
    /// Gets or sets the border width mm value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width mm value exposed by <see cref="SpreadsheetElement"/>.</value>
    public double BorderWidthMm { get; set; } = 0.25;
    /// <summary>
    /// Gets or sets the content fit value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content fit value exposed by <see cref="SpreadsheetElement"/>.</value>
    public PublicationContentFitMode ContentFit { get; set; } = PublicationContentFitMode.Clip;
    /// <summary>
    /// Gets or sets the content offset x value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset x value exposed by <see cref="SpreadsheetElement"/>.</value>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets the content offset y value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset y value exposed by <see cref="SpreadsheetElement"/>.</value>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets the content scale value that forms part of the spreadsheet element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content scale value exposed by <see cref="SpreadsheetElement"/>.</value>
    public double ContentScale { get; set; } = 1;
}

/// <summary>
/// Represents an image frame element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ImageFrameElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ImageFrameElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Image;
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this image frame element state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="ImageFrameElement"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the original data URL that identifies the network or application endpoint associated with this image frame element state.
    /// </summary>
    /// <value>The original data URL value exposed by <see cref="ImageFrameElement"/>.</value>
    public string OriginalDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the alt text value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The alt text value exposed by <see cref="ImageFrameElement"/>.</value>
    public string AltText { get; set; } = "Picture";
    /// <summary>
    /// Gets or sets the crop x value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The crop x value exposed by <see cref="ImageFrameElement"/>.</value>
    public double CropX { get; set; }
    /// <summary>
    /// Gets or sets the crop y value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The crop y value exposed by <see cref="ImageFrameElement"/>.</value>
    public double CropY { get; set; }
    /// <summary>
    /// Gets or sets the crop scale value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The crop scale value exposed by <see cref="ImageFrameElement"/>.</value>
    public double CropScale { get; set; } = 1;
    /// <summary>
    /// Gets or sets the image rotation value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The image rotation value exposed by <see cref="ImageFrameElement"/>.</value>
    public double ImageRotation { get; set; }
    /// <summary>
    /// Gets or sets the opacity value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets the brightness value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brightness value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Brightness { get; set; } = 1;
    /// <summary>
    /// Gets or sets the contrast value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contrast value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Contrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets the saturation value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saturation value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Saturation { get; set; } = 1;
    /// <summary>
    /// Gets or sets the hue rotation value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hue rotation value exposed by <see cref="ImageFrameElement"/>.</value>
    public double HueRotation { get; set; }
    /// <summary>
    /// Gets or sets the invert value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The invert value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Invert { get; set; }
    /// <summary>
    /// Gets or sets the grayscale value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grayscale value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Grayscale { get; set; }
    /// <summary>
    /// Gets or sets the sepia value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sepia value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Sepia { get; set; }
    /// <summary>
    /// Gets or sets the blur value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blur value exposed by <see cref="ImageFrameElement"/>.</value>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether flip horizontal applies to the image frame element state.
    /// </summary>
    /// <value>The flip horizontal value exposed by <see cref="ImageFrameElement"/>.</value>
    public bool FlipHorizontal { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether flip vertical applies to the image frame element state.
    /// </summary>
    /// <value>The flip vertical value exposed by <see cref="ImageFrameElement"/>.</value>
    public bool FlipVertical { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether fit inside frame applies to the image frame element state.
    /// </summary>
    /// <value>The fit inside frame value exposed by <see cref="ImageFrameElement"/>.</value>
    public bool FitInsideFrame { get; set; }
    /// <summary>
    /// Gets or sets the mask shape value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mask shape value exposed by <see cref="ImageFrameElement"/>.</value>
    public ImageMaskShape MaskShape { get; set; }
    /// <summary>
    /// Gets or sets the corner radius mm value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The corner radius mm value exposed by <see cref="ImageFrameElement"/>.</value>
    public double CornerRadiusMm { get; set; } = 4;
    /// <summary>
    /// Gets or sets the border color value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="ImageFrameElement"/>.</value>
    public string BorderColor { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the border width mm value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width mm value exposed by <see cref="ImageFrameElement"/>.</value>
    public double BorderWidthMm { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether shadow enabled applies to the image frame element state.
    /// </summary>
    /// <value>The shadow enabled value exposed by <see cref="ImageFrameElement"/>.</value>
    public bool ShadowEnabled { get; set; }
    /// <summary>
    /// Gets or sets the tint mode value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tint mode value exposed by <see cref="ImageFrameElement"/>.</value>
    public ImageTintMode TintMode { get; set; }
    /// <summary>
    /// Gets or sets the blend mode value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blend mode value exposed by <see cref="ImageFrameElement"/>.</value>
    public ImageBlendMode BlendMode { get; set; } = ImageBlendMode.Normal;
    /// <summary>
    /// Gets or sets the tint color value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tint color value exposed by <see cref="ImageFrameElement"/>.</value>
    public string TintColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets the tint opacity value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tint opacity value exposed by <see cref="ImageFrameElement"/>.</value>
    public double TintOpacity { get; set; }
    /// <summary>
    /// Gets or sets the transparent color value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transparent color value exposed by <see cref="ImageFrameElement"/>.</value>
    public string TransparentColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the transparent color tolerance value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transparent color tolerance value exposed by <see cref="ImageFrameElement"/>.</value>
    public int TransparentColorTolerance { get; set; } = 24;
    /// <summary>
    /// Gets or sets the picture source value that forms part of the image frame element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture source value exposed by <see cref="ImageFrameElement"/>.</value>
    public PictureDocument? PictureSource { get; set; }
}

/// <summary>
/// Represents a publication media element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public abstract class PublicationMediaElement : PublicationElement
{
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this publication media element state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="PublicationMediaElement"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="PublicationMediaElement"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim start seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim start seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double TrimStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim end seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim end seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double TrimEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets the timeline start seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline start seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the volume value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The volume value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double Volume { get; set; } = 1;
    /// <summary>
    /// Gets or sets the playback rate value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The playback rate value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double PlaybackRate { get; set; } = 1;
    /// <summary>
    /// Gets or sets the fade in seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fade in seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double FadeInSeconds { get; set; }
    /// <summary>
    /// Gets or sets the fade out seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fade out seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    public double FadeOutSeconds { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether muted applies to the publication media element state.
    /// </summary>
    /// <value>The muted value exposed by <see cref="PublicationMediaElement"/>.</value>
    public bool Muted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether loop applies to the publication media element state.
    /// </summary>
    /// <value>The loop value exposed by <see cref="PublicationMediaElement"/>.</value>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether auto play applies to the publication media element state.
    /// </summary>
    /// <value>The auto play value exposed by <see cref="PublicationMediaElement"/>.</value>
    public bool AutoPlay { get; set; } = true;
    /// <summary>
    /// Gets or sets the playback trigger value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The playback trigger value exposed by <see cref="PublicationMediaElement"/>.</value>
    public PublicationMediaPlaybackTrigger PlaybackTrigger { get; set; } = PublicationMediaPlaybackTrigger.OnPageEnter;
    /// <summary>
    /// Gets or sets the waveform samples collection maintained or exposed by this publication media element instance for downstream processing.
    /// </summary>
    /// <value>The waveform samples value exposed by <see cref="PublicationMediaElement"/>.</value>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets the segments collection maintained or exposed by this publication media element instance for downstream processing.
    /// </summary>
    /// <value>The segments value exposed by <see cref="PublicationMediaElement"/>.</value>
    public List<PublicationMediaSegment> Segments { get; set; } = [];

    /// <summary>
    /// Gets the effective trim end seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effective trim end seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    [JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    /// <summary>
    /// Gets the effective segments collection maintained or exposed by this publication media element instance for downstream processing.
    /// </summary>
    /// <value>The effective segments value exposed by <see cref="PublicationMediaElement"/>.</value>
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
    /// Gets the timeline length seconds value that forms part of the publication media element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline length seconds value exposed by <see cref="PublicationMediaElement"/>.</value>
    [JsonIgnore]
    public double TimelineLengthSeconds => Math.Max(.05, EffectiveSegments.Sum(segment =>
        segment.TimelineDurationSeconds > 0
            ? segment.TimelineDurationSeconds
            : segment.SourceLengthSeconds / Math.Max(.0001, Math.Abs(segment.Speed))) / Math.Max(.1, PlaybackRate));
}

/// <summary>
/// Represents a video element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class VideoElement : PublicationMediaElement
{
    /// <summary>
    /// Gets the kind value that forms part of the video element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="VideoElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Video;
    /// <summary>
    /// Gets or sets the poster data URL that identifies the network or application endpoint associated with this video element state.
    /// </summary>
    /// <value>The poster data URL value exposed by <see cref="VideoElement"/>.</value>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the alt text value that forms part of the video element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The alt text value exposed by <see cref="VideoElement"/>.</value>
    public string AltText { get; set; } = "Video";
    /// <summary>
    /// Gets or sets a value indicating whether show controls applies to the video element state.
    /// </summary>
    /// <value>The show controls value exposed by <see cref="VideoElement"/>.</value>
    public bool ShowControls { get; set; } = true;
    /// <summary>
    /// Gets or sets the fit mode value that forms part of the video element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fit mode value exposed by <see cref="VideoElement"/>.</value>
    public PublicationVideoFitMode FitMode { get; set; } = PublicationVideoFitMode.Stretch;
    /// <summary>
    /// Gets or sets a value indicating whether fit mode explicit applies to the video element state.
    /// </summary>
    /// <value>The fit mode explicit value exposed by <see cref="VideoElement"/>.</value>
    public bool FitModeExplicit { get; set; }
    /// <summary>
    /// Gets or sets the background value that forms part of the video element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="VideoElement"/>.</value>
    public string Background { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets the video project value that forms part of the video element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project value exposed by <see cref="VideoElement"/>.</value>
    public VideoProjectDocument? VideoProject { get; set; }
    /// <summary>
    /// Gets or sets the frame clip polygon collection maintained or exposed by this video element instance for downstream processing.
    /// </summary>
    /// <value>The frame clip polygon value exposed by <see cref="VideoElement"/>.</value>
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
}

/// <summary>
/// Represents an audio element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class AudioElement : PublicationMediaElement
{
    /// <summary>
    /// Gets the kind value that forms part of the audio element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="AudioElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Audio;
    /// <summary>
    /// Gets or sets the display kind value that forms part of the audio element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display kind value exposed by <see cref="AudioElement"/>.</value>
    public PublicationAudioDisplayKind DisplayKind { get; set; } = PublicationAudioDisplayKind.Waveform;
    /// <summary>
    /// Gets or sets the accent color value that forms part of the audio element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent color value exposed by <see cref="AudioElement"/>.</value>
    public string AccentColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets a value indicating whether show controls applies to the audio element state.
    /// </summary>
    /// <value>The show controls value exposed by <see cref="AudioElement"/>.</value>
    public bool ShowControls { get; set; } = true;
}

/// <summary>
/// Represents a shape element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ShapeElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ShapeElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Shape;
    /// <summary>
    /// Gets or sets the shape value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shape value exposed by <see cref="ShapeElement"/>.</value>
    public PublicationShape Shape { get; set; } = PublicationShape.Rectangle;
    /// <summary>
    /// Gets or sets the fill value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill value exposed by <see cref="ShapeElement"/>.</value>
    public string Fill { get; set; } = "#dbeafe";
    /// <summary>
    /// Gets or sets the stroke value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke value exposed by <see cref="ShapeElement"/>.</value>
    public string Stroke { get; set; } = "#1d4ed8";
    /// <summary>
    /// Gets or sets the stroke width value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke width value exposed by <see cref="ShapeElement"/>.</value>
    public double StrokeWidth { get; set; } = 0.4;
    /// <summary>
    /// Gets or sets the corner radius mm value that forms part of the shape element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The corner radius mm value exposed by <see cref="ShapeElement"/>.</value>
    public double CornerRadiusMm { get; set; } = 3;
}

/// <summary>
/// Represents a barcode element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class BarcodeElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="BarcodeElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Barcode;
    /// <summary>
    /// Gets or sets the format value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="BarcodeElement"/>.</value>
    public PublicationBarcodeFormat Format { get; set; } = PublicationBarcodeFormat.QrCode;
    /// <summary>
    /// Gets or sets the value value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="BarcodeElement"/>.</value>
    public string Value { get; set; } = "https://github.com/Michi0403/BlazorPublisher";
    /// <summary>
    /// Gets or sets the foreground color value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The foreground color value exposed by <see cref="BarcodeElement"/>.</value>
    public string ForegroundColor { get; set; } = "#111827";
    /// <summary>
    /// Gets or sets the background color value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background color value exposed by <see cref="BarcodeElement"/>.</value>
    public string BackgroundColor { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets a value indicating whether transparent background applies to the barcode element state.
    /// </summary>
    /// <value>The transparent background value exposed by <see cref="BarcodeElement"/>.</value>
    public bool TransparentBackground { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether show text applies to the barcode element state.
    /// </summary>
    /// <value>The show text value exposed by <see cref="BarcodeElement"/>.</value>
    public bool ShowText { get; set; } = true;
    /// <summary>
    /// Gets or sets the margin value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The margin value exposed by <see cref="BarcodeElement"/>.</value>
    public int Margin { get; set; } = 8;
    /// <summary>
    /// Gets or sets the line width value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The line width value exposed by <see cref="BarcodeElement"/>.</value>
    public int LineWidth { get; set; } = 2;
    /// <summary>
    /// Gets or sets the bar height value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The bar height value exposed by <see cref="BarcodeElement"/>.</value>
    public int BarHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets the font size that quantifies the associated barcode element data.
    /// </summary>
    /// <value>The font size value exposed by <see cref="BarcodeElement"/>.</value>
    public int FontSize { get; set; } = 16;
    /// <summary>
    /// Gets or sets the error correction value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error correction value exposed by <see cref="BarcodeElement"/>.</value>
    public PublicationBarcodeErrorCorrection ErrorCorrection { get; set; } = PublicationBarcodeErrorCorrection.M;
    /// <summary>
    /// Gets or sets the module shape value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The module shape value exposed by <see cref="BarcodeElement"/>.</value>
    public PublicationBarcodeModuleShape ModuleShape { get; set; } = PublicationBarcodeModuleShape.Square;
    /// <summary>
    /// Gets or sets the SVG markup value that forms part of the barcode element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The SVG markup value exposed by <see cref="BarcodeElement"/>.</value>
    public string SvgMarkup { get; set; } = string.Empty;
}

/// <summary>
/// Represents a connector endpoint application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ConnectorEndpoint
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the connector endpoint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public ConnectorEndpointKind Kind { get; set; } = ConnectorEndpointKind.Element;
    /// <summary>
    /// Gets or sets the stable element identifier used to identify or correlate this connector endpoint instance with related application state.
    /// </summary>
    /// <value>The element identifier value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public Guid ElementId { get; set; }
    /// <summary>
    /// Gets or sets the anchor value that forms part of the connector endpoint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The anchor value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public ConnectorAnchor Anchor { get; set; } = ConnectorAnchor.Right;
    /// <summary>
    /// Gets or sets the stable port identifier used to identify or correlate this connector endpoint instance with related application state.
    /// </summary>
    /// <value>The port identifier value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public Guid? PortId { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the connector endpoint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the connector endpoint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the target selector value that forms part of the connector endpoint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target selector value exposed by <see cref="ConnectorEndpoint"/>.</value>
    public string TargetSelector { get; set; } = string.Empty;
}

/// <summary>
/// Carries the configurable signal connector settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class SignalConnectorSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the signal connector state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether line visible applies to the signal connector state.
    /// </summary>
    /// <value>The line visible value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public bool LineVisible { get; set; } = true;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public SignalConnectorTrigger Trigger { get; set; } = SignalConnectorTrigger.OnPageEnter;
    /// <summary>
    /// Gets or sets the visual value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The visual value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public SignalConnectorVisual Visual { get; set; } = SignalConnectorVisual.FlyingArrow;
    /// <summary>
    /// Gets or sets the delay seconds value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delay seconds value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double DelaySeconds { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double DurationSeconds { get; set; } = 1.5;
    /// <summary>
    /// Gets or sets the repeat count that quantifies the associated signal connector data.
    /// </summary>
    /// <value>The repeat count value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public int RepeatCount { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether loop applies to the signal connector state.
    /// </summary>
    /// <value>The loop value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether auto reverse applies to the signal connector state.
    /// </summary>
    /// <value>The auto reverse value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public bool AutoReverse { get; set; }
    /// <summary>
    /// Gets or sets the start gesture value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start gesture value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public SignalGesture StartGesture { get; set; }
    /// <summary>
    /// Gets or sets the end gesture value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The end gesture value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public SignalGesture EndGesture { get; set; }
    /// <summary>
    /// Gets or sets the stable motion target element identifier used to identify or correlate this signal connector instance with related application state.
    /// </summary>
    /// <value>The motion target element identifier value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public Guid? MotionTargetElementId { get; set; }
    /// <summary>
    /// Gets or sets the motion target selector value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The motion target selector value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public string MotionTargetSelector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the translate x percent value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The translate x percent value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double TranslateXPercent { get; set; }
    /// <summary>
    /// Gets or sets the translate y percent value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The translate y percent value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double TranslateYPercent { get; set; }
    /// <summary>
    /// Gets or sets the scale value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scale value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double Scale { get; set; } = 1;
    /// <summary>
    /// Gets or sets the resize width percent value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The resize width percent value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double ResizeWidthPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets the resize height percent value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The resize height percent value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double ResizeHeightPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets the rotation degrees value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rotation degrees value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double RotationDegrees { get; set; }
    /// <summary>
    /// Gets or sets the opacity value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether restore motion after run applies to the signal connector state.
    /// </summary>
    /// <value>The restore motion after run value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public bool RestoreMotionAfterRun { get; set; }
    /// <summary>
    /// Gets or sets the stable completion target element identifier used to identify or correlate this signal connector instance with related application state.
    /// </summary>
    /// <value>The completion target element identifier value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public Guid? CompletionTargetElementId { get; set; }
    /// <summary>
    /// Gets or sets the completion target selector value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The completion target selector value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public string CompletionTargetSelector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the completion action value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The completion action value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public SignalCompletionAction CompletionAction { get; set; }
    /// <summary>
    /// Gets or sets the completion value value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The completion value value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public string CompletionValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the completion duration seconds value that forms part of the signal connector state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The completion duration seconds value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public double CompletionDurationSeconds { get; set; } = .8;
    /// <summary>
    /// Gets or sets the stable next connector identifier used to identify or correlate this signal connector instance with related application state.
    /// </summary>
    /// <value>The next connector identifier value exposed by <see cref="SignalConnectorSettings"/>.</value>
    public Guid? NextConnectorId { get; set; }
}

/// <summary>
/// Represents a connector element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ConnectorElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ConnectorElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Connector;
    /// <summary>
    /// Gets or sets the source value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorEndpoint Source { get; set; } = new();
    /// <summary>
    /// Gets or sets the target value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorEndpoint Target { get; set; } = new();
    /// <summary>
    /// Gets or sets the path kind used by this connector element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path kind value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorPathKind PathKind { get; set; } = ConnectorPathKind.Curved;
    /// <summary>
    /// Gets or sets the start marker value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start marker value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorMarker StartMarker { get; set; }
    /// <summary>
    /// Gets or sets the end marker value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The end marker value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorMarker EndMarker { get; set; } = ConnectorMarker.Arrow;
    /// <summary>
    /// Gets or sets the dash style value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dash style value exposed by <see cref="ConnectorElement"/>.</value>
    public ConnectorDashStyle DashStyle { get; set; }
    /// <summary>
    /// Gets or sets the stroke value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke value exposed by <see cref="ConnectorElement"/>.</value>
    public string Stroke { get; set; } = "#245b85";
    /// <summary>
    /// Gets or sets the stroke width mm value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The stroke width mm value exposed by <see cref="ConnectorElement"/>.</value>
    public double StrokeWidthMm { get; set; } = 0.7;
    /// <summary>
    /// Gets or sets the control1 x value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control1 x value exposed by <see cref="ConnectorElement"/>.</value>
    public double? Control1X { get; set; }
    /// <summary>
    /// Gets or sets the control1 y value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control1 y value exposed by <see cref="ConnectorElement"/>.</value>
    public double? Control1Y { get; set; }
    /// <summary>
    /// Gets or sets the control2 x value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control2 x value exposed by <see cref="ConnectorElement"/>.</value>
    public double? Control2X { get; set; }
    /// <summary>
    /// Gets or sets the control2 y value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control2 y value exposed by <see cref="ConnectorElement"/>.</value>
    public double? Control2Y { get; set; }
    /// <summary>
    /// Gets or sets the signal value that forms part of the connector element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The signal value exposed by <see cref="ConnectorElement"/>.</value>
    public SignalConnectorSettings Signal { get; set; } = new();
}

/// <summary>
/// Represents a word art element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class WordArtElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="WordArtElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.WordArt;
    /// <summary>
    /// Gets or sets the text value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="WordArtElement"/>.</value>
    public string Text { get; set; } = "WordArt";
    /// <summary>
    /// Gets or sets the font family value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font family value exposed by <see cref="WordArtElement"/>.</value>
    public string FontFamily { get; set; } = "Arial Black";
    /// <summary>
    /// Gets or sets the font size pt value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The font size pt value exposed by <see cref="WordArtElement"/>.</value>
    public double FontSizePt { get; set; } = 42;
    /// <summary>
    /// Gets or sets a value indicating whether bold applies to the word art element state.
    /// </summary>
    /// <value>The bold value exposed by <see cref="WordArtElement"/>.</value>
    public bool Bold { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether italic applies to the word art element state.
    /// </summary>
    /// <value>The italic value exposed by <see cref="WordArtElement"/>.</value>
    public bool Italic { get; set; }
    /// <summary>
    /// Gets or sets the letter spacing value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The letter spacing value exposed by <see cref="WordArtElement"/>.</value>
    public double LetterSpacing { get; set; }
    /// <summary>
    /// Gets or sets the fill color value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill color value exposed by <see cref="WordArtElement"/>.</value>
    public string FillColor { get; set; } = "#2f75b5";
    /// <summary>
    /// Gets or sets the secondary color value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secondary color value exposed by <see cref="WordArtElement"/>.</value>
    public string SecondaryColor { get; set; } = "#8ec5ff";
    /// <summary>
    /// Gets or sets a value indicating whether gradient fill applies to the word art element state.
    /// </summary>
    /// <value>The gradient fill value exposed by <see cref="WordArtElement"/>.</value>
    public bool GradientFill { get; set; } = true;
    /// <summary>
    /// Gets or sets the fill kind value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill kind value exposed by <see cref="WordArtElement"/>.</value>
    public WordArtFillKind FillKind { get; set; } = WordArtFillKind.Gradient;
    /// <summary>
    /// Gets or sets the fill media data URL that identifies the network or application endpoint associated with this word art element state.
    /// </summary>
    /// <value>The fill media data URL value exposed by <see cref="WordArtElement"/>.</value>
    public string FillMediaDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fill media MIME type value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill media MIME type value exposed by <see cref="WordArtElement"/>.</value>
    public string FillMediaMimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fill media poster data URL that identifies the network or application endpoint associated with this word art element state.
    /// </summary>
    /// <value>The fill media poster data URL value exposed by <see cref="WordArtElement"/>.</value>
    public string FillMediaPosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fill media fit mode value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill media fit mode value exposed by <see cref="WordArtElement"/>.</value>
    public PublicationVideoFitMode FillMediaFitMode { get; set; } = PublicationVideoFitMode.Cover;
    /// <summary>
    /// Gets or sets a value indicating whether fill media loop applies to the word art element state.
    /// </summary>
    /// <value>The fill media loop value exposed by <see cref="WordArtElement"/>.</value>
    public bool FillMediaLoop { get; set; } = true;
    /// <summary>
    /// Gets or sets the fill media scale value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill media scale value exposed by <see cref="WordArtElement"/>.</value>
    public double FillMediaScale { get; set; } = 1;
    /// <summary>
    /// Gets or sets the fill media offset x percent value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill media offset x percent value exposed by <see cref="WordArtElement"/>.</value>
    public double FillMediaOffsetXPercent { get; set; }
    /// <summary>
    /// Gets or sets the fill media offset y percent value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill media offset y percent value exposed by <see cref="WordArtElement"/>.</value>
    public double FillMediaOffsetYPercent { get; set; }
    /// <summary>
    /// Gets or sets the outline color value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The outline color value exposed by <see cref="WordArtElement"/>.</value>
    public string OutlineColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets the outline width value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The outline width value exposed by <see cref="WordArtElement"/>.</value>
    public double OutlineWidth { get; set; } = 2;
    /// <summary>
    /// Gets or sets a value indicating whether shadow enabled applies to the word art element state.
    /// </summary>
    /// <value>The shadow enabled value exposed by <see cref="WordArtElement"/>.</value>
    public bool ShadowEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the shadow color value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shadow color value exposed by <see cref="WordArtElement"/>.</value>
    public string ShadowColor { get; set; } = "#00000080";
    /// <summary>
    /// Gets or sets the extrude depth value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The extrude depth value exposed by <see cref="WordArtElement"/>.</value>
    public double ExtrudeDepth { get; set; } = 4;
    /// <summary>
    /// Gets or sets the extrude color value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The extrude color value exposed by <see cref="WordArtElement"/>.</value>
    public string ExtrudeColor { get; set; } = "#17365d";
    /// <summary>
    /// Gets or sets the warp value that forms part of the word art element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The warp value exposed by <see cref="WordArtElement"/>.</value>
    public WordArtWarp Warp { get; set; }
    /// <summary>
    /// Gets or sets the custom path points used by this word art element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The custom path points value exposed by <see cref="WordArtElement"/>.</value>
    public List<WordArtPathPoint> CustomPathPoints { get; set; } = [];
    /// <summary>
    /// Gets or sets the path start offset percent used by this word art element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path start offset percent value exposed by <see cref="WordArtElement"/>.</value>
    public double PathStartOffsetPercent { get; set; } = 50;
    /// <summary>
    /// Gets or sets the path baseline offset used by this word art element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path baseline offset value exposed by <see cref="WordArtElement"/>.</value>
    public double PathBaselineOffset { get; set; }
}

/// <summary>
/// Carries the configurable publication playback settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationPlaybackSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether loop presentation applies to the publication playback state.
    /// </summary>
    /// <value>The loop presentation value exposed by <see cref="PublicationPlaybackSettings"/>.</value>
    public bool LoopPresentation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether start automatically applies to the publication playback state.
    /// </summary>
    /// <value>The start automatically value exposed by <see cref="PublicationPlaybackSettings"/>.</value>
    public bool StartAutomatically { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show controls applies to the publication playback state.
    /// </summary>
    /// <value>The show controls value exposed by <see cref="PublicationPlaybackSettings"/>.</value>
    public bool ShowControls { get; set; } = true;
}

/// <summary>
/// Represents a publication page transition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationPageTransition
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the publication page transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PublicationPageTransition"/>.</value>
    public PublicationPageTransitionKind Kind { get; set; } = PublicationPageTransitionKind.Fade;
    /// <summary>
    /// Gets or sets the direction value that forms part of the publication page transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="PublicationPageTransition"/>.</value>
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    /// <summary>
    /// Gets or sets the easing value that forms part of the publication page transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The easing value exposed by <see cref="PublicationPageTransition"/>.</value>
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseInOut;
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the publication page transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="PublicationPageTransition"/>.</value>
    public double DurationSeconds { get; set; } = .55;
    /// <summary>
    /// Gets or sets a value indicating whether advance on click applies to the publication page transition state.
    /// </summary>
    /// <value>The advance on click value exposed by <see cref="PublicationPageTransition"/>.</value>
    public bool AdvanceOnClick { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether auto advance applies to the publication page transition state.
    /// </summary>
    /// <value>The auto advance value exposed by <see cref="PublicationPageTransition"/>.</value>
    public bool AutoAdvance { get; set; }
    /// <summary>
    /// Gets or sets the auto advance seconds value that forms part of the publication page transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The auto advance seconds value exposed by <see cref="PublicationPageTransition"/>.</value>
    public double AutoAdvanceSeconds { get; set; } = 5;
}

/// <summary>
/// Represents a publication animation application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationAnimation
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication animation instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationAnimation"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationAnimation"/>.</value>
    public string Name { get; set; } = "Animation";
    /// <summary>
    /// Gets or sets the order value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The order value exposed by <see cref="PublicationAnimation"/>.</value>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets the phase value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The phase value exposed by <see cref="PublicationAnimation"/>.</value>
    public PublicationAnimationPhase Phase { get; set; } = PublicationAnimationPhase.Entrance;
    /// <summary>
    /// Gets or sets the effect value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effect value exposed by <see cref="PublicationAnimation"/>.</value>
    public PublicationAnimationEffect Effect { get; set; } = PublicationAnimationEffect.Fade;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="PublicationAnimation"/>.</value>
    public PublicationAnimationTrigger Trigger { get; set; } = PublicationAnimationTrigger.AfterPrevious;
    /// <summary>
    /// Gets or sets the easing value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The easing value exposed by <see cref="PublicationAnimation"/>.</value>
    public PublicationAnimationEasing Easing { get; set; } = PublicationAnimationEasing.EaseOut;
    /// <summary>
    /// Gets or sets the direction value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="PublicationAnimation"/>.</value>
    public PublicationAnimationDirection Direction { get; set; } = PublicationAnimationDirection.Left;
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="PublicationAnimation"/>.</value>
    public double DurationSeconds { get; set; } = .6;
    /// <summary>
    /// Gets or sets the delay seconds value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delay seconds value exposed by <see cref="PublicationAnimation"/>.</value>
    public double DelaySeconds { get; set; }
    /// <summary>
    /// Gets or sets the timeline start seconds value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline start seconds value exposed by <see cref="PublicationAnimation"/>.</value>
    public double? TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the distance percent value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The distance percent value exposed by <see cref="PublicationAnimation"/>.</value>
    public double DistancePercent { get; set; } = 18;
    /// <summary>
    /// Gets or sets the scale percent value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scale percent value exposed by <see cref="PublicationAnimation"/>.</value>
    public double ScalePercent { get; set; } = 20;
    /// <summary>
    /// Gets or sets the rotation degrees value that forms part of the publication animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rotation degrees value exposed by <see cref="PublicationAnimation"/>.</value>
    public double RotationDegrees { get; set; } = 360;
    /// <summary>
    /// Gets or sets the repeat count that quantifies the associated publication animation data.
    /// </summary>
    /// <value>The repeat count value exposed by <see cref="PublicationAnimation"/>.</value>
    public int RepeatCount { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether auto reverse applies to the publication animation state.
    /// </summary>
    /// <value>The auto reverse value exposed by <see cref="PublicationAnimation"/>.</value>
    public bool AutoReverse { get; set; }
}

/// <summary>
/// Represents a publication interaction application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationInteraction
{
    /// <summary>
    /// Gets or sets the action value that forms part of the publication interaction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="PublicationInteraction"/>.</value>
    public PublicationInteractionAction Action { get; set; }
    /// <summary>
    /// Gets or sets the stable target page identifier used to identify or correlate this publication interaction instance with related application state.
    /// </summary>
    /// <value>The target page identifier value exposed by <see cref="PublicationInteraction"/>.</value>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets the stable target element identifier used to identify or correlate this publication interaction instance with related application state.
    /// </summary>
    /// <value>The target element identifier value exposed by <see cref="PublicationInteraction"/>.</value>
    public Guid? TargetElementId { get; set; }
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication interaction state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationInteraction"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether open in new window applies to the publication interaction state.
    /// </summary>
    /// <value>The open in new window value exposed by <see cref="PublicationInteraction"/>.</value>
    public bool OpenInNewWindow { get; set; } = true;
}


/// <summary>
/// Represents publication field state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class PublicationFieldRecord
{
    /// <summary>
    /// Gets or sets the publication name value that forms part of the publication field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication name value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public string PublicationName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the page name value that forms part of the publication field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page name value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public string PageName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the story name value that forms part of the publication field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story name value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public string StoryName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the page number value that forms part of the publication field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page number value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public int PageNumber { get; set; }
    /// <summary>
    /// Gets or sets the page count that quantifies the associated publication field data.
    /// </summary>
    /// <value>The page count value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public int PageCount { get; set; }
    /// <summary>
    /// Gets or sets the current date associated with this publication field state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The current date value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public DateTime CurrentDate { get; set; }
    /// <summary>
    /// Gets or sets the current date time associated with this publication field state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The current date time value exposed by <see cref="PublicationFieldRecord"/>.</value>
    public DateTime CurrentDateTime { get; set; }
}
