namespace PublisherStudio.Domain;

public enum MediaConversionJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum MediaConversionScaleMode
{
    Preserve,
    Fit,
    Fill,
    Stretch
}

public enum MediaConversionTarget
{
    PublisherStudioWeb,
    GeneralWeb,
    VideoEditing,
    Streaming,
    Archive,
    Custom
}

public sealed record MediaConversionPreset(
    string Id,
    string Name,
    string Description,
    string InputKind,
    string OutputExtension,
    string OutputMimeType,
    bool Lossless,
    bool BrowserOriented,
    IReadOnlyList<string> RequiredEncoders,
    bool Available = true,
    string UnavailableReason = "");

/// <summary>
/// Canonical FFmpeg options shared by the Mainframe UI, VideoStudio hand-offs,
/// controller clients, saved profiles, and other frontend components.
/// Empty values keep the selected preset's defaults.
/// </summary>
public sealed class MediaConversionOptions
{
    public MediaConversionTarget Target { get; set; } = MediaConversionTarget.PublisherStudioWeb;
    public double? StartSeconds { get; set; }
    public double? DurationSeconds { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public MediaConversionScaleMode ScaleMode { get; set; } = MediaConversionScaleMode.Preserve;
    public bool PreserveAspectRatio { get; set; } = true;
    public double? FrameRate { get; set; }
    public string VideoCodec { get; set; } = string.Empty;
    public string VideoEncoderPreset { get; set; } = string.Empty;
    public int? Crf { get; set; }
    public int? VideoBitrateKbps { get; set; }
    public int? MaximumVideoBitrateKbps { get; set; }
    public int? VideoBufferKbps { get; set; }
    public string PixelFormat { get; set; } = string.Empty;
    public bool DisableVideo { get; set; }
    public string AudioCodec { get; set; } = string.Empty;
    public int? AudioBitrateKbps { get; set; }
    public int? AudioSampleRate { get; set; }
    public int? AudioChannels { get; set; }
    public bool DisableAudio { get; set; }
    public bool NormalizeAudio { get; set; }
    public double LoudnessTargetLufs { get; set; } = -16;
    public bool Deinterlace { get; set; }
    public bool FastStart { get; set; } = true;
    public bool CopyMetadata { get; set; } = true;
    public string VideoFilter { get; set; } = string.Empty;
    public string AudioFilter { get; set; } = string.Empty;
    public string AdvancedArguments { get; set; } = string.Empty;
    public string OutputExtension { get; set; } = string.Empty;
    public string OutputMimeType { get; set; } = string.Empty;
    public string OutputFileName { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public MediaConversionOptions Clone() => new()
    {
        Target = Target,
        StartSeconds = StartSeconds,
        DurationSeconds = DurationSeconds,
        Width = Width,
        Height = Height,
        ScaleMode = ScaleMode,
        PreserveAspectRatio = PreserveAspectRatio,
        FrameRate = FrameRate,
        VideoCodec = VideoCodec,
        VideoEncoderPreset = VideoEncoderPreset,
        Crf = Crf,
        VideoBitrateKbps = VideoBitrateKbps,
        MaximumVideoBitrateKbps = MaximumVideoBitrateKbps,
        VideoBufferKbps = VideoBufferKbps,
        PixelFormat = PixelFormat,
        DisableVideo = DisableVideo,
        AudioCodec = AudioCodec,
        AudioBitrateKbps = AudioBitrateKbps,
        AudioSampleRate = AudioSampleRate,
        AudioChannels = AudioChannels,
        DisableAudio = DisableAudio,
        NormalizeAudio = NormalizeAudio,
        LoudnessTargetLufs = LoudnessTargetLufs,
        Deinterlace = Deinterlace,
        FastStart = FastStart,
        CopyMetadata = CopyMetadata,
        VideoFilter = VideoFilter,
        AudioFilter = AudioFilter,
        AdvancedArguments = AdvancedArguments,
        OutputExtension = OutputExtension,
        OutputMimeType = OutputMimeType,
        OutputFileName = OutputFileName,
        Metadata = new Dictionary<string, string>(Metadata ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
    };
}

public sealed class MediaConversionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Custom profile";
    public string Description { get; set; } = string.Empty;
    public string PresetId { get; set; } = "webm-vp9";
    public bool BuiltIn { get; set; }
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public MediaConversionOptions Options { get; set; } = new();
}

public sealed record MediaConversionCapabilities(
    bool Available,
    string Executable,
    string Version,
    IReadOnlyList<string> Encoders,
    IReadOnlyList<MediaConversionPreset> Presets,
    string LicenseNotice,
    string InstallationHint);

public sealed record MediaConversionJobInfo(
    Guid Id,
    string SourceFileName,
    string PresetId,
    MediaConversionJobStatus Status,
    double Progress,
    string Message,
    string OutputFileName,
    string OutputMimeType,
    long OutputSize,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc)
{
    public MediaConversionOptions Options { get; init; } = new();
}

public sealed record MediaConversionInsertRequest(
    string FileName,
    string MimeType,
    byte[] Content,
    string Origin = "Media Converter Studio")
{
    public string SuggestedPresetId { get; init; } = string.Empty;
    public MediaConversionOptions? SuggestedOptions { get; init; }
}
