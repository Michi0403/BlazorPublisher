namespace PublisherStudio.Domain.Streaming;

public sealed record ToggleRequest(bool Enabled);
public sealed record ProgramPageRequest(Guid PageId);
public sealed record IngestAnnouncement(string Kind, string Url, string Codec, int Width, int Height, int FrameRate, Guid? OutputId = null);
public sealed record MediaHostHotkeyEvent(string Command, Guid? TargetId, DateTimeOffset TriggeredUtc);
public sealed record MediaHotkey(Guid Id, string Gesture, string Command, Guid? TargetId, bool Global);

public sealed class NativeCaptureRequest
{
    public string Kind { get; set; } = "Camera";
    public string DeviceId { get; set; } = string.Empty;
    public string AudioDeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string NativeBackend { get; set; } = string.Empty;
    public string NetworkUrl { get; set; } = string.Empty;
    public bool IncludeAudio { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 60;
    public bool UseDeviceTimestamps { get; set; } = true;
    public string FfmpegPath { get; set; } = string.Empty;
}

public sealed record ChatSendRequest(string Message);
