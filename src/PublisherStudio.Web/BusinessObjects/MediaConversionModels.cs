namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported media conversion job status values.
/// </summary>
public enum MediaConversionJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Lists supported media conversion scale mode values.
/// </summary>
public enum MediaConversionScaleMode
{
    Preserve,
    Fit,
    Fill,
    Stretch
}

/// <summary>
/// Lists supported media conversion target values.
/// </summary>
public enum MediaConversionTarget
{
    PublisherStudioWeb,
    GeneralWeb,
    VideoEditing,
    Streaming,
    Archive,
    Custom
}

/// <summary>
/// Represents a media conversion preset.
/// </summary>
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
    /// <summary>
    /// Gets or sets target.
    /// </summary>
    public MediaConversionTarget Target { get; set; } = MediaConversionTarget.PublisherStudioWeb;
    /// <summary>
    /// Gets or sets start seconds.
    /// </summary>
    public double? StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets duration seconds.
    /// </summary>
    public double? DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public int? Width { get; set; }
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public int? Height { get; set; }
    /// <summary>
    /// Gets or sets scale mode.
    /// </summary>
    public MediaConversionScaleMode ScaleMode { get; set; } = MediaConversionScaleMode.Preserve;
    /// <summary>
    /// Gets or sets preserve aspect ratio.
    /// </summary>
    public bool PreserveAspectRatio { get; set; } = true;
    /// <summary>
    /// Gets or sets frame rate.
    /// </summary>
    public double? FrameRate { get; set; }
    /// <summary>
    /// Gets or sets video codec.
    /// </summary>
    public string VideoCodec { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets video encoder preset.
    /// </summary>
    public string VideoEncoderPreset { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets crf.
    /// </summary>
    public int? Crf { get; set; }
    /// <summary>
    /// Gets or sets video bitrate kbps.
    /// </summary>
    public int? VideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets maximum video bitrate kbps.
    /// </summary>
    public int? MaximumVideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets video buffer kbps.
    /// </summary>
    public int? VideoBufferKbps { get; set; }
    /// <summary>
    /// Gets or sets pixel format.
    /// </summary>
    public string PixelFormat { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets disable video.
    /// </summary>
    public bool DisableVideo { get; set; }
    /// <summary>
    /// Gets or sets audio codec.
    /// </summary>
    public string AudioCodec { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets audio bitrate kbps.
    /// </summary>
    public int? AudioBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets audio sample rate.
    /// </summary>
    public int? AudioSampleRate { get; set; }
    /// <summary>
    /// Gets or sets audio channels.
    /// </summary>
    public int? AudioChannels { get; set; }
    /// <summary>
    /// Gets or sets disable audio.
    /// </summary>
    public bool DisableAudio { get; set; }
    /// <summary>
    /// Gets or sets normalize audio.
    /// </summary>
    public bool NormalizeAudio { get; set; }
    /// <summary>
    /// Gets or sets loudness target lufs.
    /// </summary>
    public double LoudnessTargetLufs { get; set; } = -16;
    /// <summary>
    /// Gets or sets deinterlace.
    /// </summary>
    public bool Deinterlace { get; set; }
    /// <summary>
    /// Gets or sets fast start.
    /// </summary>
    public bool FastStart { get; set; } = true;
    /// <summary>
    /// Gets or sets copy metadata.
    /// </summary>
    public bool CopyMetadata { get; set; } = true;
    /// <summary>
    /// Gets or sets video filter.
    /// </summary>
    public string VideoFilter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets audio filter.
    /// </summary>
    public string AudioFilter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets advanced arguments.
    /// </summary>
    public string AdvancedArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets output extension.
    /// </summary>
    public string OutputExtension { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets output mime type.
    /// </summary>
    public string OutputMimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets output file name.
    /// </summary>
    public string OutputFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the clone operation.
    /// </summary>
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

/// <summary>
/// Represents a media conversion profile.
/// </summary>
public sealed class MediaConversionProfile
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Custom profile";
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preset identifier.
    /// </summary>
    public string PresetId { get; set; } = "webm-vp9";
    /// <summary>
    /// Gets or sets built in.
    /// </summary>
    public bool BuiltIn { get; set; }
    /// <summary>
    /// Gets or sets the UTC modification time.
    /// </summary>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets options.
    /// </summary>
    public MediaConversionOptions Options { get; set; } = new();
}

/// <summary>
/// Represents a media conversion capabilities.
/// </summary>
public sealed record MediaConversionCapabilities(
    bool Available,
    string Executable,
    string Version,
    IReadOnlyList<string> Encoders,
    IReadOnlyList<MediaConversionPreset> Presets,
    string LicenseNotice,
    string InstallationHint);

/// <summary>
/// Represents a media conversion job info.
/// </summary>
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
    /// <summary>
    /// Gets or sets options.
    /// </summary>
    public MediaConversionOptions Options { get; init; } = new();
}

/// <summary>
/// Represents a media conversion insert request.
/// </summary>
public sealed record MediaConversionInsertRequest(
    string FileName,
    string MimeType,
    byte[] Content,
    string Origin = "Media Converter Studio")
{
    /// <summary>
    /// Gets or sets suggested preset identifier.
    /// </summary>
    public string SuggestedPresetId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets suggested options.
    /// </summary>
    public MediaConversionOptions? SuggestedOptions { get; init; }
}
