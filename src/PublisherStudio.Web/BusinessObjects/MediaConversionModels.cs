namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported media conversion job status values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported media conversion scale mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum MediaConversionScaleMode
{
    Preserve,
    Fit,
    Fill,
    Stretch
}

/// <summary>
/// Defines the supported media conversion target values used to select or describe behavior in the surrounding workflow.
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
/// Represents a media conversion preset application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the media conversion preset operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the media conversion preset operation and used when producing its result.</param>
/// <param name="InputKind">Input kind value supplied to the media conversion preset operation and used when producing its result.</param>
/// <param name="OutputExtension">Output extension value supplied to the media conversion preset operation and used when producing its result.</param>
/// <param name="OutputMimeType">Output mime type value supplied to the media conversion preset operation and used when producing its result.</param>
/// <param name="Lossless">Value indicating whether lossless should apply to this operation.</param>
/// <param name="BrowserOriented">Value indicating whether browser oriented should apply to this operation.</param>
/// <param name="RequiredEncoders">String dependency used by the media conversion preset workflow to provide the corresponding application capability.</param>
/// <param name="Available">Value indicating whether the value is available should apply to this operation.</param>
/// <param name="UnavailableReason">Unavailable reason value supplied to the media conversion preset operation and used when producing its result.</param>
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
    /// Gets or sets the target value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target value exposed by <see cref="MediaConversionOptions"/>.</value>
    public MediaConversionTarget Target { get; set; } = MediaConversionTarget.PublisherStudioWeb;
    /// <summary>
    /// Gets or sets the start seconds value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start seconds value exposed by <see cref="MediaConversionOptions"/>.</value>
    public double? StartSeconds { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration seconds value exposed by <see cref="MediaConversionOptions"/>.</value>
    public double? DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? Height { get; set; }
    /// <summary>
    /// Gets or sets the scale mode value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scale mode value exposed by <see cref="MediaConversionOptions"/>.</value>
    public MediaConversionScaleMode ScaleMode { get; set; } = MediaConversionScaleMode.Preserve;
    /// <summary>
    /// Gets or sets a value indicating whether preserve aspect ratio applies to the media conversion state.
    /// </summary>
    /// <value>The preserve aspect ratio value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool PreserveAspectRatio { get; set; } = true;
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="MediaConversionOptions"/>.</value>
    public double? FrameRate { get; set; }
    /// <summary>
    /// Gets or sets the video codec value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video codec value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string VideoCodec { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the video encoder preset value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video encoder preset value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string VideoEncoderPreset { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the crf value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The crf value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? Crf { get; set; }
    /// <summary>
    /// Gets or sets the video bitrate kbps value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video bitrate kbps value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? VideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the maximum video bitrate kbps value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video bitrate kbps value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? MaximumVideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the video buffer kbps value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video buffer kbps value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? VideoBufferKbps { get; set; }
    /// <summary>
    /// Gets or sets the pixel format value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pixel format value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string PixelFormat { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether disable video applies to the media conversion state.
    /// </summary>
    /// <value>The disable video value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool DisableVideo { get; set; }
    /// <summary>
    /// Gets or sets the audio codec value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio codec value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string AudioCodec { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the audio bitrate kbps value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? AudioBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the audio sample rate value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio sample rate value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? AudioSampleRate { get; set; }
    /// <summary>
    /// Gets or sets the audio channels value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio channels value exposed by <see cref="MediaConversionOptions"/>.</value>
    public int? AudioChannels { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether disable audio applies to the media conversion state.
    /// </summary>
    /// <value>The disable audio value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool DisableAudio { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether normalize audio applies to the media conversion state.
    /// </summary>
    /// <value>The normalize audio value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool NormalizeAudio { get; set; }
    /// <summary>
    /// Gets or sets the loudness target lufs value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The loudness target lufs value exposed by <see cref="MediaConversionOptions"/>.</value>
    public double LoudnessTargetLufs { get; set; } = -16;
    /// <summary>
    /// Gets or sets a value indicating whether deinterlace applies to the media conversion state.
    /// </summary>
    /// <value>The deinterlace value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool Deinterlace { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether fast start applies to the media conversion state.
    /// </summary>
    /// <value>The fast start value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool FastStart { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether copy metadata applies to the media conversion state.
    /// </summary>
    /// <value>The copy metadata value exposed by <see cref="MediaConversionOptions"/>.</value>
    public bool CopyMetadata { get; set; } = true;
    /// <summary>
    /// Gets or sets the video filter value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video filter value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string VideoFilter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the audio filter value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio filter value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string AudioFilter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the advanced arguments value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The advanced arguments value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string AdvancedArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output extension value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output extension value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string OutputExtension { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output MIME type value that forms part of the media conversion state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output MIME type value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string OutputMimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output file name used by this media conversion instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The output file name value exposed by <see cref="MediaConversionOptions"/>.</value>
    public string OutputFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this media conversion instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="MediaConversionOptions"/>.</value>
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
/// Represents a media conversion profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaConversionProfile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media conversion profile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaConversionProfile"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the media conversion profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaConversionProfile"/>.</value>
    public string Name { get; set; } = "Custom profile";
    /// <summary>
    /// Gets or sets the description value that forms part of the media conversion profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="MediaConversionProfile"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable preset identifier used to identify or correlate this media conversion profile instance with related application state.
    /// </summary>
    /// <value>The preset identifier value exposed by <see cref="MediaConversionProfile"/>.</value>
    public string PresetId { get; set; } = "webm-vp9";
    /// <summary>
    /// Gets or sets a value indicating whether built in applies to the media conversion profile state.
    /// </summary>
    /// <value>The built in value exposed by <see cref="MediaConversionProfile"/>.</value>
    public bool BuiltIn { get; set; }
    /// <summary>
    /// Gets or sets the modified UTC associated with this media conversion profile state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The modified UTC value exposed by <see cref="MediaConversionProfile"/>.</value>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the options value that forms part of the media conversion profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="MediaConversionProfile"/>.</value>
    public MediaConversionOptions Options { get; set; } = new();
}

/// <summary>
/// Represents a media conversion capabilities application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Available">Value indicating whether the value is available should apply to this operation.</param>
/// <param name="Executable">Executable value supplied to the media conversion capabilities operation and used when producing its result.</param>
/// <param name="Version">Version value supplied to the media conversion capabilities operation and used when producing its result.</param>
/// <param name="Encoders">String dependency used by the media conversion capabilities workflow to provide the corresponding application capability.</param>
/// <param name="Presets">Media conversion preset dependency used by the media conversion capabilities workflow to provide the corresponding application capability.</param>
/// <param name="LicenseNotice">License notice value supplied to the media conversion capabilities operation and used when producing its result.</param>
/// <param name="InstallationHint">Installation hint value supplied to the media conversion capabilities operation and used when producing its result.</param>
public sealed record MediaConversionCapabilities(
    bool Available,
    string Executable,
    string Version,
    IReadOnlyList<string> Encoders,
    IReadOnlyList<MediaConversionPreset> Presets,
    string LicenseNotice,
    string InstallationHint);

/// <summary>
/// Represents a media conversion job info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="SourceFileName">Source file name value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="PresetId">Identifier of the preset to use for this operation.</param>
/// <param name="Status">Status value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="Progress">Progress value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="OutputFileName">Output file name value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="OutputMimeType">Output mime type value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="OutputSize">Output size value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="CreatedUtc">Created utc value supplied to the media conversion job info operation and used when producing its result.</param>
/// <param name="CompletedUtc">Completed utc value supplied to the media conversion job info operation and used when producing its result.</param>
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
    /// Gets or sets the options value that forms part of the media conversion job info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="MediaConversionJobInfo"/>.</value>
    public MediaConversionOptions Options { get; init; } = new();
}

/// <summary>
/// Represents the input contract for media conversion insert, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="FileName">File name value supplied to the media conversion insert operation and used when producing its result.</param>
/// <param name="MimeType">Mime type value supplied to the media conversion insert operation and used when producing its result.</param>
/// <param name="Content">Content value supplied to the media conversion insert operation and used when producing its result.</param>
/// <param name="Origin">Origin value supplied to the media conversion insert operation and used when producing its result.</param>
public sealed record MediaConversionInsertRequest(
    string FileName,
    string MimeType,
    byte[] Content,
    string Origin = "Media Converter Studio")
{
    /// <summary>
    /// Gets or sets the stable suggested preset identifier used to identify or correlate this media conversion insert instance with related application state.
    /// </summary>
    /// <value>The suggested preset identifier value exposed by <see cref="MediaConversionInsertRequest"/>.</value>
    public string SuggestedPresetId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the suggested options value that forms part of the media conversion insert state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested options value exposed by <see cref="MediaConversionInsertRequest"/>.</value>
    public MediaConversionOptions? SuggestedOptions { get; init; }
}
