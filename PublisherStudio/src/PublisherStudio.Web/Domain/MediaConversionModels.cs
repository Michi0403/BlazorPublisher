namespace PublisherStudio.Domain;

public enum MediaConversionJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
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
    DateTimeOffset? CompletedUtc);

public sealed record MediaConversionInsertRequest(
    string FileName,
    string MimeType,
    byte[] Content);
