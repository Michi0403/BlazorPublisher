namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported media studio mouse mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum MediaStudioMouseMode { SelectSection, PlacePlayhead, AddCutLine, FrameRegion }
/// <summary>
/// Defines the supported video effect blend mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum VideoEffectBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
/// <summary>
/// Defines the supported video effect layer kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum VideoEffectLayerKind { BaseVideo, Selection2D, Blob3D }
/// <summary>
/// Defines the supported video effect filter kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum VideoEffectFilterKind { Brightness, Contrast, Saturation, HueRotation, Blur, Grayscale, Sepia, Invert, ChromaKey, Vignette, Grain, ColorWash }
/// <summary>
/// Defines the supported media timeline track kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum MediaTimelineTrackKind { Video, Audio, Subtitle, Data }
/// <summary>
/// Defines the supported media timeline transition kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum MediaTimelineTransitionKind { Cut, Dissolve, Wipe, Fade, Unknown }

/// <summary>
/// Represents a media frame point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaFramePoint
{
    /// <summary>
    /// Gets or sets the x value that forms part of the media frame point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="MediaFramePoint"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the media frame point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="MediaFramePoint"/>.</value>
    public double Y { get; set; }
}

/// <summary>
/// Represents a media temporal section application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaTemporalSection
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media temporal section instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaTemporalSection"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the media temporal section state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaTemporalSection"/>.</value>
    public string Name { get; set; } = "Cut section";
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the media temporal section state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="MediaTemporalSection"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the start seconds value that forms part of the media temporal section state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start seconds value exposed by <see cref="MediaTemporalSection"/>.</value>
    public double StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the end seconds value that forms part of the media temporal section state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The end seconds value exposed by <see cref="MediaTemporalSection"/>.</value>
    public double EndSeconds { get; set; }

    /// <summary>
    /// Gets the length seconds value that forms part of the media temporal section state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The length seconds value exposed by <see cref="MediaTemporalSection"/>.</value>
    [System.Text.Json.Serialization.JsonIgnore]
    public double LengthSeconds => Math.Max(0, EndSeconds - StartSeconds);
}

/// <summary>
/// Represents a video frame region application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class VideoFrameRegion
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this video frame region instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="VideoFrameRegion"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the video frame region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="VideoFrameRegion"/>.</value>
    public string Name { get; set; } = "Full frame";
    /// <summary>
    /// Gets or sets a value indicating whether inverted applies to the video frame region state.
    /// </summary>
    /// <value>The inverted value exposed by <see cref="VideoFrameRegion"/>.</value>
    public bool Inverted { get; set; }
    /// <summary>
    /// Gets or sets the points collection maintained or exposed by this video frame region instance for downstream processing.
    /// </summary>
    /// <value>The points value exposed by <see cref="VideoFrameRegion"/>.</value>
    public List<MediaFramePoint> Points { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether full frame applies to the video frame region state.
    /// </summary>
    /// <value>The is full frame value exposed by <see cref="VideoFrameRegion"/>.</value>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFullFrame => Points.Count < 3;
}

/// <summary>
/// Represents a video effect application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class VideoEffectFilter
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this video effect instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="VideoEffectFilter"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="VideoEffectFilter"/>.</value>
    public string Name { get; set; } = "Filter";
    /// <summary>
    /// Gets or sets the kind value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="VideoEffectFilter"/>.</value>
    public VideoEffectFilterKind Kind { get; set; } = VideoEffectFilterKind.Brightness;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the video effect state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="VideoEffectFilter"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the amount value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The amount value exposed by <see cref="VideoEffectFilter"/>.</value>
    public double Amount { get; set; } = 1;
    /// <summary>
    /// Gets or sets the secondary amount value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secondary amount value exposed by <see cref="VideoEffectFilter"/>.</value>
    public double SecondaryAmount { get; set; } = .12;
    /// <summary>
    /// Gets or sets the tertiary amount value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tertiary amount value exposed by <see cref="VideoEffectFilter"/>.</value>
    public double TertiaryAmount { get; set; } = .3;
    /// <summary>
    /// Gets or sets the residual opacity value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The residual opacity value exposed by <see cref="VideoEffectFilter"/>.</value>
    public double ResidualOpacity { get; set; } = 0;
    /// <summary>
    /// Gets or sets the color value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="VideoEffectFilter"/>.</value>
    public string Color { get; set; } = "#00ff00";
    /// <summary>
    /// Gets or sets the HTML export support value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export support value exposed by <see cref="VideoEffectFilter"/>.</value>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets the HTML export note value that forms part of the video effect state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export note value exposed by <see cref="VideoEffectFilter"/>.</value>
    public string HtmlExportNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents a video effect layer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class VideoEffectLayer
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this video effect layer instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="VideoEffectLayer"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="VideoEffectLayer"/>.</value>
    public string Name { get; set; } = "Video layer";
    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the video effect layer state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="VideoEffectLayer"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether locked applies to the video effect layer state.
    /// </summary>
    /// <value>The locked value exposed by <see cref="VideoEffectLayer"/>.</value>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets the kind value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="VideoEffectLayer"/>.</value>
    public VideoEffectLayerKind Kind { get; set; } = VideoEffectLayerKind.BaseVideo;
    /// <summary>
    /// Gets or sets the opacity value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets the blend mode value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blend mode value exposed by <see cref="VideoEffectLayer"/>.</value>
    public VideoEffectBlendMode BlendMode { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether temporal range applies to the video effect layer state.
    /// </summary>
    /// <value>The has temporal range value exposed by <see cref="VideoEffectLayer"/>.</value>
    public bool HasTemporalRange { get; set; }
    /// <summary>
    /// Gets or sets the temporal start seconds value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The temporal start seconds value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double TemporalStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the temporal end seconds value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The temporal end seconds value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double TemporalEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets the region value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The region value exposed by <see cref="VideoEffectLayer"/>.</value>
    public VideoFrameRegion Region { get; set; } = new();
    /// <summary>
    /// Gets or sets the morph region value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The morph region value exposed by <see cref="VideoEffectLayer"/>.</value>
    public VideoFrameRegion MorphRegion { get; set; } = new() { Name = "Morph target" };
    /// <summary>
    /// Gets or sets a value indicating whether morph enabled applies to the video effect layer state.
    /// </summary>
    /// <value>The morph enabled value exposed by <see cref="VideoEffectLayer"/>.</value>
    public bool MorphEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether animate morph applies to the video effect layer state.
    /// </summary>
    /// <value>The animate morph value exposed by <see cref="VideoEffectLayer"/>.</value>
    public bool AnimateMorph { get; set; } = true;
    /// <summary>
    /// Gets or sets the morph amount value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The morph amount value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double MorphAmount { get; set; }
    /// <summary>
    /// Gets or sets the animation speed value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The animation speed value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double AnimationSpeed { get; set; } = 1;
    /// <summary>
    /// Gets or sets the depth value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The depth value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double Depth { get; set; } = .18;
    /// <summary>
    /// Gets or sets the roundness value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The roundness value exposed by <see cref="VideoEffectLayer"/>.</value>
    public double Roundness { get; set; } = .12;
    /// <summary>
    /// Gets or sets the open OpenSCAD script value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The open OpenSCAD script value exposed by <see cref="VideoEffectLayer"/>.</value>
    public string OpenScadScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the HTML export support value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export support value exposed by <see cref="VideoEffectLayer"/>.</value>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets the HTML export note value that forms part of the video effect layer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export note value exposed by <see cref="VideoEffectLayer"/>.</value>
    public string HtmlExportNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the filters collection maintained or exposed by this video effect layer instance for downstream processing.
    /// </summary>
    /// <value>The filters value exposed by <see cref="VideoEffectLayer"/>.</value>
    public List<VideoEffectFilter> Filters { get; set; } = [];
}

/// <summary>
/// Represents a media source reference application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaSourceReference
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media source reference instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaSourceReference"/>.</value>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URI that identifies the network or application endpoint associated with this media source reference state.
    /// </summary>
    /// <value>The URI value exposed by <see cref="MediaSourceReference"/>.</value>
    public string Uri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the original path used by this media source reference instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The original path value exposed by <see cref="MediaSourceReference"/>.</value>
    public string OriginalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the media source reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="MediaSourceReference"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reel name value that forms part of the media source reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reel name value exposed by <see cref="MediaSourceReference"/>.</value>
    public string ReelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether missing applies to the media source reference state.
    /// </summary>
    /// <value>The missing value exposed by <see cref="MediaSourceReference"/>.</value>
    public bool Missing { get; set; }
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this media source reference instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="MediaSourceReference"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a media project marker application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaProjectMarker
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media project marker instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaProjectMarker"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the media project marker state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaProjectMarker"/>.</value>
    public string Name { get; set; } = "Marker";
    /// <summary>
    /// Gets or sets the color value that forms part of the media project marker state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="MediaProjectMarker"/>.</value>
    public string Color { get; set; } = "#f59e0b";
    /// <summary>
    /// Gets or sets the start seconds value that forms part of the media project marker state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start seconds value exposed by <see cref="MediaProjectMarker"/>.</value>
    public double StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the media project marker state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="MediaProjectMarker"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the comment value that forms part of the media project marker state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The comment value exposed by <see cref="MediaProjectMarker"/>.</value>
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Represents a media timeline transition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaTimelineTransition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media timeline transition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the media timeline transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public string Name { get; set; } = "Transition";
    /// <summary>
    /// Gets or sets the kind value that forms part of the media timeline transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public MediaTimelineTransitionKind Kind { get; set; } = MediaTimelineTransitionKind.Unknown;
    /// <summary>
    /// Gets or sets the stable track identifier used to identify or correlate this media timeline transition instance with related application state.
    /// </summary>
    /// <value>The track identifier value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public Guid TrackId { get; set; }
    /// <summary>
    /// Gets or sets from segment identifier.
    /// </summary>
    /// <value>The from segment identifier value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public Guid? FromSegmentId { get; set; }
    /// <summary>
    /// Gets or sets the stable to segment identifier used to identify or correlate this media timeline transition instance with related application state.
    /// </summary>
    /// <value>The to segment identifier value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public Guid? ToSegmentId { get; set; }
    /// <summary>
    /// Gets or sets the timeline start seconds value that forms part of the media timeline transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline start seconds value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the media timeline transition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this media timeline transition instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="MediaTimelineTransition"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a media timeline track application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaTimelineTrack
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media timeline track instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the media timeline track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public string Name { get; set; } = "Track";
    /// <summary>
    /// Gets or sets the kind value that forms part of the media timeline track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public MediaTimelineTrackKind Kind { get; set; } = MediaTimelineTrackKind.Video;
    /// <summary>
    /// Gets or sets the order value that forms part of the media timeline track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The order value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the media timeline track state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether muted applies to the media timeline track state.
    /// </summary>
    /// <value>The muted value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public bool Muted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether locked applies to the media timeline track state.
    /// </summary>
    /// <value>The locked value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets the segments collection maintained or exposed by this media timeline track instance for downstream processing.
    /// </summary>
    /// <value>The segments value exposed by <see cref="MediaTimelineTrack"/>.</value>
    public List<PublicationMediaSegment> Segments { get; set; } = [];
}

/// <summary>
/// Represents video project state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class VideoProjectDocument
{
    /// <summary>
    /// Gets or sets the format version value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format version value exposed by <see cref="VideoProjectDocument"/>.</value>
    public string FormatVersion { get; set; } = "1.0";
    /// <summary>
    /// Gets or sets the name value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="VideoProjectDocument"/>.</value>
    public string Name { get; set; } = "Video project";
    /// <summary>
    /// Gets or sets the source format value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format value exposed by <see cref="VideoProjectDocument"/>.</value>
    public string SourceFormat { get; set; } = "PublisherStudio";
    /// <summary>
    /// Gets or sets the source format version value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format version value exposed by <see cref="VideoProjectDocument"/>.</value>
    public string SourceFormatVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="VideoProjectDocument"/>.</value>
    public double FrameRate { get; set; } = 30;
    /// <summary>
    /// Gets or sets the width value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="VideoProjectDocument"/>.</value>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets the height value that forms part of the video project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="VideoProjectDocument"/>.</value>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets the stable active track identifier used to identify or correlate this video project instance with related application state.
    /// </summary>
    /// <value>The active track identifier value exposed by <see cref="VideoProjectDocument"/>.</value>
    public Guid ActiveTrackId { get; set; }
    /// <summary>
    /// Gets or sets the tracks collection maintained or exposed by this video project instance for downstream processing.
    /// </summary>
    /// <value>The tracks value exposed by <see cref="VideoProjectDocument"/>.</value>
    public List<MediaTimelineTrack> Tracks { get; set; } = [];
    /// <summary>
    /// Gets or sets the transitions collection maintained or exposed by this video project instance for downstream processing.
    /// </summary>
    /// <value>The transitions value exposed by <see cref="VideoProjectDocument"/>.</value>
    public List<MediaTimelineTransition> Transitions { get; set; } = [];
    /// <summary>
    /// Gets or sets the markers collection maintained or exposed by this video project instance for downstream processing.
    /// </summary>
    /// <value>The markers value exposed by <see cref="VideoProjectDocument"/>.</value>
    public List<MediaProjectMarker> Markers { get; set; } = [];
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this video project instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="VideoProjectDocument"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a publication media segment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationMediaSegment
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication media segment instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public string Name { get; set; } = "Clip";
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this publication media segment state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the poster data URL that identifies the network or application endpoint associated with this publication media segment state.
    /// </summary>
    /// <value>The poster data URL value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source reference value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source reference value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public MediaSourceReference SourceReference { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication media segment state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether gap applies to the publication media segment state.
    /// </summary>
    /// <value>The is gap value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public bool IsGap { get; set; }
    /// <summary>
    /// Gets or sets the timeline start seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline start seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the timeline duration seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeline duration seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TimelineDurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the source rate value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source rate value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double SourceRate { get; set; }
    /// <summary>
    /// Gets or sets the speed value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The speed value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double Speed { get; set; } = 1;
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim start seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim start seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TrimStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim end seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim end seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TrimEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether temporal selection applies to the publication media segment state.
    /// </summary>
    /// <value>The has temporal selection value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public bool HasTemporalSelection { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether temporal selection committed applies to the publication media segment state.
    /// </summary>
    /// <value>The temporal selection committed value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public bool TemporalSelectionCommitted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether temporal selection is point applies to the publication media segment state.
    /// </summary>
    /// <value>The temporal selection is point value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public bool TemporalSelectionIsPoint { get; set; }
    /// <summary>
    /// Gets or sets the temporal selection start seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The temporal selection start seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TemporalSelectionStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the temporal selection end seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The temporal selection end seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public double TemporalSelectionEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets the cut sections collection maintained or exposed by this publication media segment instance for downstream processing.
    /// </summary>
    /// <value>The cut sections value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public List<MediaTemporalSection> CutSections { get; set; } = [];
    /// <summary>
    /// Gets or sets the video layers collection maintained or exposed by this publication media segment instance for downstream processing.
    /// </summary>
    /// <value>The video layers value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public List<VideoEffectLayer> VideoLayers { get; set; } = [];
    /// <summary>
    /// Gets or sets the waveform samples collection maintained or exposed by this publication media segment instance for downstream processing.
    /// </summary>
    /// <value>The waveform samples value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets the import metadata collection maintained or exposed by this publication media segment instance for downstream processing.
    /// </summary>
    /// <value>The import metadata value exposed by <see cref="PublicationMediaSegment"/>.</value>
    public Dictionary<string, string> ImportMetadata { get; set; } = [];

    /// <summary>
    /// Gets the effective trim end seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effective trim end seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    [System.Text.Json.Serialization.JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    /// <summary>
    /// Gets the source length seconds value that forms part of the publication media segment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source length seconds value exposed by <see cref="PublicationMediaSegment"/>.</value>
    [System.Text.Json.Serialization.JsonIgnore]
    public double SourceLengthSeconds => Math.Max(.01, EffectiveTrimEndSeconds - TrimStartSeconds);
}

/// <summary>
/// Represents the outcome of media editor, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class MediaEditorResult
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="MediaEditorResult"/>.</value>
    public PublicationElementKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaEditorResult"/>.</value>
    public string Name { get; set; } = "Media";
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this media editor state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="MediaEditorResult"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="MediaEditorResult"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the poster data URL that identifies the network or application endpoint associated with this media editor state.
    /// </summary>
    /// <value>The poster data URL value exposed by <see cref="MediaEditorResult"/>.</value>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable preview asset identifier used to identify or correlate this media editor instance with related application state.
    /// </summary>
    /// <value>The preview asset identifier value exposed by <see cref="MediaEditorResult"/>.</value>
    public Guid PreviewAssetId { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="MediaEditorResult"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim start seconds value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim start seconds value exposed by <see cref="MediaEditorResult"/>.</value>
    public double TrimStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the trim end seconds value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trim end seconds value exposed by <see cref="MediaEditorResult"/>.</value>
    public double TrimEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets the volume value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The volume value exposed by <see cref="MediaEditorResult"/>.</value>
    public double Volume { get; set; } = 1;
    /// <summary>
    /// Gets or sets the playback rate value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The playback rate value exposed by <see cref="MediaEditorResult"/>.</value>
    public double PlaybackRate { get; set; } = 1;
    /// <summary>
    /// Gets or sets the fade in seconds value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fade in seconds value exposed by <see cref="MediaEditorResult"/>.</value>
    public double FadeInSeconds { get; set; }
    /// <summary>
    /// Gets or sets the fade out seconds value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fade out seconds value exposed by <see cref="MediaEditorResult"/>.</value>
    public double FadeOutSeconds { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether muted applies to the media editor state.
    /// </summary>
    /// <value>The muted value exposed by <see cref="MediaEditorResult"/>.</value>
    public bool Muted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether loop applies to the media editor state.
    /// </summary>
    /// <value>The loop value exposed by <see cref="MediaEditorResult"/>.</value>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether auto play applies to the media editor state.
    /// </summary>
    /// <value>The auto play value exposed by <see cref="MediaEditorResult"/>.</value>
    public bool AutoPlay { get; set; } = true;
    /// <summary>
    /// Gets or sets the playback trigger value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The playback trigger value exposed by <see cref="MediaEditorResult"/>.</value>
    public PublicationMediaPlaybackTrigger PlaybackTrigger { get; set; } = PublicationMediaPlaybackTrigger.OnPageEnter;
    /// <summary>
    /// Gets or sets the video fit mode value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video fit mode value exposed by <see cref="MediaEditorResult"/>.</value>
    public PublicationVideoFitMode VideoFitMode { get; set; } = PublicationVideoFitMode.Contain;
    /// <summary>
    /// Gets or sets the waveform samples collection maintained or exposed by this media editor instance for downstream processing.
    /// </summary>
    /// <value>The waveform samples value exposed by <see cref="MediaEditorResult"/>.</value>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets the segments collection maintained or exposed by this media editor instance for downstream processing.
    /// </summary>
    /// <value>The segments value exposed by <see cref="MediaEditorResult"/>.</value>
    public List<PublicationMediaSegment> Segments { get; set; } = [];
    /// <summary>
    /// Gets or sets the video project value that forms part of the media editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project value exposed by <see cref="MediaEditorResult"/>.</value>
    public VideoProjectDocument? VideoProject { get; set; }

    // Legacy projection retained for older publication renderers. The canonical regions live on VideoEffectLayer.
    /// <summary>
    /// Gets or sets the frame clip polygon collection maintained or exposed by this media editor instance for downstream processing.
    /// </summary>
    /// <value>The frame clip polygon value exposed by <see cref="MediaEditorResult"/>.</value>
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
}

/// <summary>
/// Represents a media source info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaSourceInfo
{
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the media source info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="MediaSourceInfo"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the media source info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="MediaSourceInfo"/>.</value>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the poster data URL that identifies the network or application endpoint associated with this media source info state.
    /// </summary>
    /// <value>The poster data URL value exposed by <see cref="MediaSourceInfo"/>.</value>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the waveform samples collection maintained or exposed by this media source info instance for downstream processing.
    /// </summary>
    /// <value>The waveform samples value exposed by <see cref="MediaSourceInfo"/>.</value>
    public List<double> WaveformSamples { get; set; } = [];
}



/// <summary>
/// Represents the input contract for video layer mainframe insert, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class VideoLayerMainframeInsertRequest
{
    /// <summary>
    /// Gets or sets the name value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string Name { get; set; } = "3D video object";
    /// <summary>
    /// Gets or sets the HTML value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string Html { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the CSS value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The CSS value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string Css { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the java script value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The java script value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string JavaScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the open OpenSCAD script value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The open OpenSCAD script value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string OpenScadScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the HTML export support value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export support value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.CanvasRuntime;
    /// <summary>
    /// Gets or sets the HTML export note value that forms part of the video layer mainframe insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export note value exposed by <see cref="VideoLayerMainframeInsertRequest"/>.</value>
    public string HtmlExportNote { get; set; } = string.Empty;
}
