namespace PublisherStudio.BusinessObjects.Streaming;

/// <summary>
/// Represents a toggle request.
/// </summary>
public sealed record ToggleRequest(bool Enabled);
/// <summary>
/// Represents a program page request.
/// </summary>
public sealed record ProgramPageRequest(Guid PageId);
/// <summary>
/// Represents an ingest announcement.
/// </summary>
public sealed record IngestAnnouncement(string Kind, string Url, string Codec, int Width, int Height, int FrameRate, Guid? OutputId = null);
/// <summary>
/// Represents a media host hotkey event.
/// </summary>
public sealed record MediaHostHotkeyEvent(string Command, Guid? TargetId, DateTimeOffset TriggeredUtc);
/// <summary>
/// Represents a media hotkey.
/// </summary>
public sealed record MediaHotkey(Guid Id, string Gesture, string Command, Guid? TargetId, bool Global);

/// <summary>
/// Represents a native capture request.
/// </summary>
public sealed class NativeCaptureRequest
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = "Camera";
    /// <summary>
    /// Gets or sets device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets audio device identifier.
    /// </summary>
    public string AudioDeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets application identifier.
    /// </summary>
    public string ApplicationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets native backend.
    /// </summary>
    public string NativeBackend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets network URL.
    /// </summary>
    public string NetworkUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets include audio.
    /// </summary>
    public bool IncludeAudio { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets frame rate.
    /// </summary>
    public int FrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets use device timestamps.
    /// </summary>
    public bool UseDeviceTimestamps { get; set; } = true;
    /// <summary>
    /// Gets or sets FFmpeg path.
    /// </summary>
    public string FfmpegPath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a chat send request.
/// </summary>
public sealed record ChatSendRequest(string Message);
