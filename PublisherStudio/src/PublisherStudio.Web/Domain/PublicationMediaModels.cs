namespace PublisherStudio.Domain;

public sealed class MediaEditorResult
{
    public PublicationElementKind Kind { get; set; }
    public string Name { get; set; } = "Media";
    public string DataUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string PosterDataUrl { get; set; } = string.Empty;
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
    public List<double> WaveformSamples { get; set; } = [];
}

public sealed class MediaSourceInfo
{
    public double DurationSeconds { get; set; }
    public string PosterDataUrl { get; set; } = string.Empty;
    public List<double> WaveformSamples { get; set; } = [];
}
