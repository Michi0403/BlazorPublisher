namespace PublisherStudio.Domain;

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
    public List<double> WaveformSamples { get; set; } = [];
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
