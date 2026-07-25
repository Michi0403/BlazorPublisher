namespace PublisherStudio.Domain;

public enum MediaStudioMouseMode { SelectSection, PlacePlayhead, AddCutLine, FrameRegion }
public enum VideoEffectBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten }
public enum VideoEffectFilterKind { Brightness, Contrast, Saturation, HueRotation, Blur, Grayscale, Sepia, Invert, ChromaKey, Vignette, Grain, ColorWash }
public enum MediaTimelineTrackKind { Video, Audio, Subtitle, Data }
public enum MediaTimelineTransitionKind { Cut, Dissolve, Wipe, Fade, Unknown }

public sealed class MediaFramePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class MediaTemporalSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Cut section";
    public bool Enabled { get; set; } = true;
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public double LengthSeconds => Math.Max(0, EndSeconds - StartSeconds);
}

public sealed class VideoFrameRegion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Full frame";
    public bool Inverted { get; set; }
    public List<MediaFramePoint> Points { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFullFrame => Points.Count < 3;
}

public sealed class VideoEffectFilter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Filter";
    public VideoEffectFilterKind Kind { get; set; } = VideoEffectFilterKind.Brightness;
    public bool Enabled { get; set; } = true;
    public double Amount { get; set; } = 1;
    public double SecondaryAmount { get; set; } = .12;
    public double TertiaryAmount { get; set; } = .3;
    public double ResidualOpacity { get; set; } = 0;
    public string Color { get; set; } = "#00ff00";
}

public sealed class VideoEffectLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Video layer";
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public double Opacity { get; set; } = 1;
    public VideoEffectBlendMode BlendMode { get; set; }
    public bool HasTemporalRange { get; set; }
    public double TemporalStartSeconds { get; set; }
    public double TemporalEndSeconds { get; set; }
    public VideoFrameRegion Region { get; set; } = new();
    public List<VideoEffectFilter> Filters { get; set; } = [];
}


public sealed class MediaSourceReference
{
    public string Id { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string ReelName { get; set; } = string.Empty;
    public bool Missing { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public sealed class MediaProjectMarker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Marker";
    public string Color { get; set; } = "#f59e0b";
    public double StartSeconds { get; set; }
    public double DurationSeconds { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class MediaTimelineTransition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Transition";
    public MediaTimelineTransitionKind Kind { get; set; } = MediaTimelineTransitionKind.Unknown;
    public Guid TrackId { get; set; }
    public Guid? FromSegmentId { get; set; }
    public Guid? ToSegmentId { get; set; }
    public double TimelineStartSeconds { get; set; }
    public double DurationSeconds { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public sealed class MediaTimelineTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Track";
    public MediaTimelineTrackKind Kind { get; set; } = MediaTimelineTrackKind.Video;
    public int Order { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Muted { get; set; }
    public bool Locked { get; set; }
    public List<PublicationMediaSegment> Segments { get; set; } = [];
}

public sealed class VideoProjectDocument
{
    public string FormatVersion { get; set; } = "1.0";
    public string Name { get; set; } = "Video project";
    public string SourceFormat { get; set; } = "PublisherStudio";
    public string SourceFormatVersion { get; set; } = string.Empty;
    public double FrameRate { get; set; } = 30;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public Guid ActiveTrackId { get; set; }
    public List<MediaTimelineTrack> Tracks { get; set; } = [];
    public List<MediaTimelineTransition> Transitions { get; set; } = [];
    public List<MediaProjectMarker> Markers { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public sealed class PublicationMediaSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Clip";
    public string DataUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string PosterDataUrl { get; set; } = string.Empty;
    public MediaSourceReference SourceReference { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public bool IsGap { get; set; }
    public double TimelineStartSeconds { get; set; }
    public double TimelineDurationSeconds { get; set; }
    public double SourceRate { get; set; }
    public double Speed { get; set; } = 1;
    public double DurationSeconds { get; set; }
    public double TrimStartSeconds { get; set; }
    public double TrimEndSeconds { get; set; }
    public bool HasTemporalSelection { get; set; }
    public bool TemporalSelectionCommitted { get; set; }
    public bool TemporalSelectionIsPoint { get; set; }
    public double TemporalSelectionStartSeconds { get; set; }
    public double TemporalSelectionEndSeconds { get; set; }
    public List<MediaTemporalSection> CutSections { get; set; } = [];
    public List<VideoEffectLayer> VideoLayers { get; set; } = [];
    public List<double> WaveformSamples { get; set; } = [];
    public Dictionary<string, string> ImportMetadata { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public double EffectiveTrimEndSeconds => TrimEndSeconds > TrimStartSeconds
        ? TrimEndSeconds
        : Math.Max(TrimStartSeconds, DurationSeconds);

    [System.Text.Json.Serialization.JsonIgnore]
    public double SourceLengthSeconds => Math.Max(.01, EffectiveTrimEndSeconds - TrimStartSeconds);
}

public sealed class MediaEditorResult
{
    public PublicationElementKind Kind { get; set; }
    public string Name { get; set; } = "Media";
    public string DataUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string PosterDataUrl { get; set; } = string.Empty;
    public Guid PreviewAssetId { get; set; }
    public double DurationSeconds { get; set; }
    public double TrimStartSeconds { get; set; }
    public double TrimEndSeconds { get; set; }
    public double Volume { get; set; } = 1;
    public double PlaybackRate { get; set; } = 1;
    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
    public bool Muted { get; set; }
    public bool Loop { get; set; }
    public bool AutoPlay { get; set; } = true;
    public PublicationMediaPlaybackTrigger PlaybackTrigger { get; set; } = PublicationMediaPlaybackTrigger.OnPageEnter;
    public PublicationVideoFitMode VideoFitMode { get; set; } = PublicationVideoFitMode.Contain;
    public List<double> WaveformSamples { get; set; } = [];
    public List<PublicationMediaSegment> Segments { get; set; } = [];
    public VideoProjectDocument? VideoProject { get; set; }

    // Legacy projection retained for older publication renderers. The canonical regions live on VideoEffectLayer.
    public List<MediaFramePoint> FrameClipPolygon { get; set; } = [];
}

public sealed class MediaSourceInfo
{
    public string MimeType { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public string PosterDataUrl { get; set; } = string.Empty;
    public List<double> WaveformSamples { get; set; } = [];
}

public static class PublicationMediaData
{
    public static string NormalizeMimeType(string? mimeType, string fallback)
    {
        var value = mimeType?.Trim() ?? string.Empty;
        var separator = value.IndexOf(';');
        if (separator >= 0) value = value[..separator].Trim();
        return value.Contains('/') ? value.ToLowerInvariant() : fallback;
    }

    public static string NormalizeDataUrl(string? dataUrl, string fallbackMimeType)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return dataUrl ?? string.Empty;

        var marker = dataUrl.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return dataUrl;

        var header = dataUrl.Substring(5, marker - 5);
        var mimeType = NormalizeMimeType(header, fallbackMimeType);
        return $"data:{mimeType};base64,{dataUrl[(marker + 8)..]}";
    }
}
