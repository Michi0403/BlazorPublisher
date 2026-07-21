using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public enum PublicationStreamProvider
{
    Twitch,
    YouTube,
    Kick,
    TikTok,
    CustomRtmp,
    CustomSrt,
    LocalNetwork
}

public enum PublicationStreamTransport
{
    Rtmp,
    Rtmps,
    Srt,
    WebRtc,
    Hls,
    Rtsp
}

public enum PublicationStreamQualityPreset
{
    Recommended,
    HighQuality,
    LowLatency,
    BandwidthSaving,
    Custom
}

public enum PublicationStreamVideoCodec
{
    H264,
    Hevc,
    Av1
}

public enum PublicationStreamAudioCodec
{
    Aac,
    Opus
}

public enum StreamingHardwareEncoderPreference
{
    Auto,
    Software,
    NvidiaNvenc,
    IntelQuickSync,
    AmdAmf,
    AppleVideoToolbox
}

public enum PublicationStreamRecordingVariant
{
    CleanMaster,
    EachEnabledOutput,
    SelectedOutputs
}

public enum PublicationLiveSourceKind
{
    Camera,
    Screen,
    Window,
    BrowserTab,
    CaptureDevice,
    Microphone,
    SystemAudio,
    ApplicationAudio,
    NetworkMedia,
    NowPlaying
}

public enum PublicationLiveSourceFitMode
{
    Contain,
    Cover,
    Stretch
}

public enum PublicationCaptureBackend
{
    Auto,
    Browser,
    Native
}

public enum PublicationStreamSessionMode
{
    Idle,
    DryRun,
    Live
}

public sealed class PublicationStreamingSettings
{
    public bool FollowSelectedPage { get; set; } = true;
    public Guid? ProgramPageId { get; set; }
    public List<PublicationStreamOutput> Outputs { get; set; } = [];
    public PublicationRecordingSettings Recording { get; set; } = new();
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } =
    [
        new() { Gesture = "F9", Command = "ToggleStreaming" },
        new() { Gesture = "F10", Command = "ToggleRecording" },
        new() { Gesture = "PageDown", Command = "NextPage" },
        new() { Gesture = "PageUp", Command = "PreviousPage" }
    ];
    public bool PreferDeviceTimestamps { get; set; } = true;
    public int MasterWidth { get; set; } = 3840;
    public int MasterHeight { get; set; } = 2160;
    public int MasterFrameRate { get; set; } = 60;
}

public sealed class PublicationStreamOutput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = "Streaming output";
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    public bool Enabled { get; set; } = true;
    public bool UseProviderTestMode { get; set; }
    public PublicationStreamQualityPreset QualityPreset { get; set; } = PublicationStreamQualityPreset.Recommended;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 60;
    public int VideoBitrateKbps { get; set; } = 6000;
    public int AudioBitrateKbps { get; set; } = 160;
    public int KeyFrameIntervalSeconds { get; set; } = 2;
    public PublicationStreamVideoCodec VideoCodec { get; set; } = PublicationStreamVideoCodec.H264;
    public PublicationStreamAudioCodec AudioCodec { get; set; } = PublicationStreamAudioCodec.Aac;
    public string ChatChannel { get; set; } = string.Empty;
}

public sealed class PublicationRecordingSettings
{
    public bool Enabled { get; set; } = true;
    public string DestinationDirectory { get; set; } = string.Empty;
    public PublicationStreamRecordingVariant Variant { get; set; } = PublicationStreamRecordingVariant.CleanMaster;
    public List<Guid> SelectedOutputIds { get; set; } = [];
    public string Container { get; set; } = "mkv";
    public int SegmentSeconds { get; set; } = 10;
    public bool RemuxToMp4AfterStop { get; set; } = true;
}

public sealed class PublicationLanStreamingSettings
{
    public bool Enabled { get; set; }
    public string BindAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 17848;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 60;
    public int VideoBitrateKbps { get; set; } = 8000;
    public bool EnableBrowserWebRtc { get; set; } = true;
    public bool EnableHls { get; set; } = true;
    public bool EnableRtsp { get; set; }
    public int RtspPort { get; set; } = 8554;
    public bool RequireAccessToken { get; set; } = true;
    public string AccessTokenReference { get; set; } = string.Empty;
    public int ViewerLimit { get; set; } = 50;
}

public sealed class PublicationStreamingHotkey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Gesture { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public bool Global { get; set; }
}

public sealed class StreamingProviderProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New streaming profile";
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    public PublicationStreamTransport Transport { get; set; } = PublicationStreamTransport.Rtmps;
    public string Endpoint { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool HasStoredSecret { get; set; }
    public string Secret { get; set; } = string.Empty;
    public bool ChatEnabled { get; set; }
    public bool HasStoredChatSecret { get; set; }
    public string ChatSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public sealed class StreamingDeviceProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Device";
    public PublicationLiveSourceKind Kind { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string AudioDeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
    public string NativeBackend { get; set; } = string.Empty;
    public bool UseDeviceTimestamps { get; set; } = true;
}

public sealed class StreamingMachineSettings
{
    public List<StreamingProviderProfile> Providers { get; set; } = [];
    public List<StreamingDeviceProfile> Devices { get; set; } = [];
    public string FfmpegPath { get; set; } = string.Empty;
    public string DefaultRecordingDirectory { get; set; } = string.Empty;
    public int MediaHostPort { get; set; } = 17847;
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
}

public sealed class NativeMediaDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Backend { get; set; } = string.Empty;
    public string? ProcessId { get; set; }
    public string? WindowTitle { get; set; }
}

public sealed class BrowserMediaDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = "Permission required";
}

public sealed class StreamingSessionSnapshot
{
    public PublicationStreamSessionMode Mode { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? ProgramPageId { get; set; }
    public bool Recording { get; set; }
    public bool MediaHostConnected { get; set; }
    public string StatusText { get; set; } = "Ready";
    public DateTimeOffset? StartedUtc { get; set; }
    public long DroppedFrames { get; set; }
    public double CurrentBitrateKbps { get; set; }
    public Dictionary<Guid, bool> OutputEnabled { get; set; } = [];
}

public sealed class LiveSourceElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.LiveSource;
    public PublicationLiveSourceKind SourceKind { get; set; } = PublicationLiveSourceKind.Camera;
    public Guid? DeviceProfileId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string AudioDeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
    public string NativeBackend { get; set; } = string.Empty;
    public string NetworkUrl { get; set; } = string.Empty;
    public bool IncludeAudio { get; set; }
    public bool UseDeviceTimestamp { get; set; } = true;
    public int CaptureWidth { get; set; } = 1920;
    public int CaptureHeight { get; set; } = 1080;
    public int CaptureFrameRate { get; set; } = 60;
    public PublicationLiveSourceFitMode FitMode { get; set; } = PublicationLiveSourceFitMode.Cover;
    public bool Muted { get; set; } = true;
    public double Volume { get; set; } = 1;
    public double AudioDelayMilliseconds { get; set; }
    public double Brightness { get; set; } = 1;
    public double Contrast { get; set; } = 1;
    public double Saturation { get; set; } = 1;
    public double HueRotation { get; set; }
    public double Blur { get; set; }
    public bool ChromaKeyEnabled { get; set; }
    public string ChromaKeyColor { get; set; } = "#00ff00";
    public double ChromaSimilarity { get; set; } = .35;
    public double ChromaSmoothness { get; set; } = .12;
    public double ChromaSpill { get; set; } = .3;
    public double ChromaResidualOpacity { get; set; } = 1;
    public string NowPlayingDirectory { get; set; } = string.Empty;
    public string Background { get; set; } = "#111827";

    [JsonIgnore]
    public bool IsVisual => SourceKind is PublicationLiveSourceKind.Camera
        or PublicationLiveSourceKind.Screen
        or PublicationLiveSourceKind.Window
        or PublicationLiveSourceKind.BrowserTab
        or PublicationLiveSourceKind.CaptureDevice
        or PublicationLiveSourceKind.NetworkMedia;
}
