namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported media studio mouse mode values.
/// </summary>
public enum MediaStudioMouseMode { SelectSection, PlacePlayhead, AddCutLine, FrameRegion }
/// <summary>
/// Lists supported video effect blend mode values.
/// </summary>
public enum VideoEffectBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
/// <summary>
/// Lists supported video effect layer kind values.
/// </summary>
public enum VideoEffectLayerKind { BaseVideo, Selection2D, Blob3D }
/// <summary>
/// Lists supported video effect filter kind values.
/// </summary>
public enum VideoEffectFilterKind { Brightness, Contrast, Saturation, HueRotation, Blur, Grayscale, Sepia, Invert, ChromaKey, Vignette, Grain, ColorWash }
/// <summary>
/// Lists supported media timeline track kind values.
/// </summary>
public enum MediaTimelineTrackKind { Video, Audio, Subtitle, Data }
/// <summary>
/// Lists supported media timeline transition kind values.
/// </summary>
public enum MediaTimelineTransitionKind { Cut, Dissolve, Wipe, Fade, Unknown }

/// <summary>
/// Represents a media frame point.
/// </summary>
public sealed class MediaFramePoint
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
/// Represents a media temporal section.
/// </summary>
public sealed class MediaTemporalSection
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Cut section";
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets start seconds.
    /// </summary>
    public double StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets end seconds.
    /// </summary>
    public double EndSeconds { get; set; }

    /// <summary>
    /// Gets length seconds.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double LengthSeconds => Math.Max(0, EndSeconds - StartSeconds);
}

/// <summary>
/// Represents a video frame region.
/// </summary>
public sealed class VideoFrameRegion
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Full frame";
    /// <summary>
    /// Gets or sets inverted.
    /// </summary>
    public bool Inverted { get; set; }
    /// <summary>
    /// Gets or sets points.
    /// </summary>
    public List<MediaFramePoint> Points { get; set; } = [];

    /// <summary>
    /// Gets is full frame.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFullFrame => Points.Count < 3;
}

/// <summary>
/// Represents a video effect filter.
/// </summary>
public sealed class VideoEffectFilter
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Filter";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public VideoEffectFilterKind Kind { get; set; } = VideoEffectFilterKind.Brightness;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets amount.
    /// </summary>
    public double Amount { get; set; } = 1;
    /// <summary>
    /// Gets or sets secondary amount.
    /// </summary>
    public double SecondaryAmount { get; set; } = .12;
    /// <summary>
    /// Gets or sets tertiary amount.
    /// </summary>
    public double TertiaryAmount { get; set; } = .3;
    /// <summary>
    /// Gets or sets residual opacity.
    /// </summary>
    public double ResidualOpacity { get; set; } = 0;
    /// <summary>
    /// Gets or sets color.
    /// </summary>
    public string Color { get; set; } = "#00ff00";
    /// <summary>
    /// Gets or sets HTML export support.
    /// </summary>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets HTML export note.
    /// </summary>
    public string HtmlExportNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents a video effect layer.
/// </summary>
public sealed class VideoEffectLayer
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Video layer";
    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets whether the item is locked.
    /// </summary>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public VideoEffectLayerKind Kind { get; set; } = VideoEffectLayerKind.BaseVideo;
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets blend mode.
    /// </summary>
    public VideoEffectBlendMode BlendMode { get; set; }
    /// <summary>
    /// Gets or sets has temporal range.
    /// </summary>
    public bool HasTemporalRange { get; set; }
    /// <summary>
    /// Gets or sets temporal start seconds.
    /// </summary>
    public double TemporalStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets temporal end seconds.
    /// </summary>
    public double TemporalEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets region.
    /// </summary>
    public VideoFrameRegion Region { get; set; } = new();
    /// <summary>
    /// Gets or sets morph region.
    /// </summary>
    public VideoFrameRegion MorphRegion { get; set; } = new() { Name = "Morph target" };
    /// <summary>
    /// Gets or sets morph enabled.
    /// </summary>
    public bool MorphEnabled { get; set; }
    /// <summary>
    /// Gets or sets animate morph.
    /// </summary>
    public bool AnimateMorph { get; set; } = true;
    /// <summary>
    /// Gets or sets morph amount.
    /// </summary>
    public double MorphAmount { get; set; }
    /// <summary>
    /// Gets or sets animation speed.
    /// </summary>
    public double AnimationSpeed { get; set; } = 1;
    /// <summary>
    /// Gets or sets depth.
    /// </summary>
    public double Depth { get; set; } = .18;
    /// <summary>
    /// Gets or sets roundness.
    /// </summary>
    public double Roundness { get; set; } = .12;
    /// <summary>
    /// Gets or sets open scad script.
    /// </summary>
    public string OpenScadScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets HTML export support.
    /// </summary>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets HTML export note.
    /// </summary>
    public string HtmlExportNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets filters.
    /// </summary>
    public List<VideoEffectFilter> Filters { get; set; } = [];
}

/// <summary>
/// Represents a media source reference.
/// </summary>
public sealed class MediaSourceReference
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets URI.
    /// </summary>
    public string Uri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets original path.
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets reel name.
    /// </summary>
    public string ReelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets missing.
    /// </summary>
    public bool Missing { get; set; }
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a media project marker.
/// </summary>
public sealed class MediaProjectMarker
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Marker";
    /// <summary>
    /// Gets or sets color.
    /// </summary>
    public string Color { get; set; } = "#f59e0b";
    /// <summary>
    /// Gets or sets start seconds.
    /// </summary>
    public double StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets comment.
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Represents a media timeline transition.
/// </summary>
public sealed class MediaTimelineTransition
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Transition";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public MediaTimelineTransitionKind Kind { get; set; } = MediaTimelineTransitionKind.Unknown;
    /// <summary>
    /// Gets or sets track identifier.
    /// </summary>
    public Guid TrackId { get; set; }
    /// <summary>
    /// Gets or sets from segment identifier.
    /// </summary>
    public Guid? FromSegmentId { get; set; }
    /// <summary>
    /// Gets or sets to segment identifier.
    /// </summary>
    public Guid? ToSegmentId { get; set; }
    /// <summary>
    /// Gets or sets timeline start seconds.
    /// </summary>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a media timeline track.
/// </summary>
public sealed class MediaTimelineTrack
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Track";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public MediaTimelineTrackKind Kind { get; set; } = MediaTimelineTrackKind.Video;
    /// <summary>
    /// Gets or sets order.
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets muted.
    /// </summary>
    public bool Muted { get; set; }
    /// <summary>
    /// Gets or sets whether the item is locked.
    /// </summary>
    public bool Locked { get; set; }
    /// <summary>
    /// Gets or sets segments.
    /// </summary>
    public List<PublicationMediaSegment> Segments { get; set; } = [];
}

/// <summary>
/// Represents a video project document.
/// </summary>
public sealed class VideoProjectDocument
{
    /// <summary>
    /// Gets or sets format version.
    /// </summary>
    public string FormatVersion { get; set; } = "1.0";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Video project";
    /// <summary>
    /// Gets or sets source format.
    /// </summary>
    public string SourceFormat { get; set; } = "PublisherStudio";
    /// <summary>
    /// Gets or sets source format version.
    /// </summary>
    public string SourceFormatVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame rate.
    /// </summary>
    public double FrameRate { get; set; } = 30;
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets active track identifier.
    /// </summary>
    public Guid ActiveTrackId { get; set; }
    /// <summary>
    /// Gets or sets tracks.
    /// </summary>
    public List<MediaTimelineTrack> Tracks { get; set; } = [];
    /// <summary>
    /// Gets or sets transitions.
    /// </summary>
    public List<MediaTimelineTransition> Transitions { get; set; } = [];
    /// <summary>
    /// Gets or sets markers.
    /// </summary>
    public List<MediaProjectMarker> Markers { get; set; } = [];
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a publication media segment.
/// </summary>
public sealed class PublicationMediaSegment
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Clip";
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets poster data URL.
    /// </summary>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source reference.
    /// </summary>
    public MediaSourceReference SourceReference { get; set; } = new();
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is gap.
    /// </summary>
    public bool IsGap { get; set; }
    /// <summary>
    /// Gets or sets timeline start seconds.
    /// </summary>
    public double TimelineStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets timeline duration seconds.
    /// </summary>
    public double TimelineDurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets source rate.
    /// </summary>
    public double SourceRate { get; set; }
    /// <summary>
    /// Gets or sets speed.
    /// </summary>
    public double Speed { get; set; } = 1;
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
    /// Gets or sets has temporal selection.
    /// </summary>
    public bool HasTemporalSelection { get; set; }
    /// <summary>
    /// Gets or sets temporal selection committed.
    /// </summary>
    public bool TemporalSelectionCommitted { get; set; }
    /// <summary>
    /// Gets or sets temporal selection is point.
    /// </summary>
    public bool TemporalSelectionIsPoint { get; set; }
    /// <summary>
    /// Gets or sets temporal selection start seconds.
    /// </summary>
    public double TemporalSelectionStartSeconds { get; set; }
    /// <summary>
    /// Gets or sets temporal selection end seconds.
    /// </summary>
    public double TemporalSelectionEndSeconds { get; set; }
    /// <summary>
    /// Gets or sets cut sections.
    /// </summary>
    public List<MediaTemporalSection> CutSections { get; set; } = [];
    /// <summary>
    /// Gets or sets video layers.
    /// </summary>
    public List<VideoEffectLayer> VideoLayers { get; set; } = [];
    /// <summary>
    /// Gets or sets waveform samples.
    /// </summary>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets import metadata.
    /// </summary>
    public Dictionary<string, string> ImportMetadata { get; set; } = [];

    /// <summary>
    /// Gets effective trim end seconds.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    /// <summary>
    /// Gets source length seconds.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double SourceLengthSeconds => Math.Max(.01, EffectiveTrimEndSeconds - TrimStartSeconds);
}

/// <summary>
/// Represents a media editor result.
/// </summary>
public sealed class MediaEditorResult
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public PublicationElementKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Media";
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets poster data URL.
    /// </summary>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preview asset identifier.
    /// </summary>
    public Guid PreviewAssetId { get; set; }
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
    /// Gets or sets video fit mode.
    /// </summary>
    public PublicationVideoFitMode VideoFitMode { get; set; } = PublicationVideoFitMode.Contain;
    /// <summary>
    /// Gets or sets waveform samples.
    /// </summary>
    public List<double> WaveformSamples { get; set; } = [];
    /// <summary>
    /// Gets or sets segments.
    /// </summary>
    public List<PublicationMediaSegment> Segments { get; set; } = [];
    /// <summary>
    /// Gets or sets video project.
    /// </summary>
    public VideoProjectDocument? VideoProject { get; set; }

    // Legacy projection retained for older publication renderers. The canonical regions live on VideoEffectLayer.
    /// <summary>
    /// Gets or sets frame clip polygon.
    /// </summary>
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
}

/// <summary>
/// Represents a media source info.
/// </summary>
public sealed class MediaSourceInfo
{
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets poster data URL.
    /// </summary>
    public string PosterDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets waveform samples.
    /// </summary>
    public List<double> WaveformSamples { get; set; } = [];
}



/// <summary>
/// Represents a video layer mainframe insert request.
/// </summary>
public sealed class VideoLayerMainframeInsertRequest
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "3D video object";
    /// <summary>
    /// Gets or sets HTML.
    /// </summary>
    public string Html { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets CSS.
    /// </summary>
    public string Css { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets java script.
    /// </summary>
    public string JavaScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open scad script.
    /// </summary>
    public string OpenScadScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets HTML export support.
    /// </summary>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.CanvasRuntime;
    /// <summary>
    /// Gets or sets HTML export note.
    /// </summary>
    public string HtmlExportNote { get; set; } = string.Empty;
}
