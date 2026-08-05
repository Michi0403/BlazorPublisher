using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported publication stream provider values.
/// </summary>
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

/// <summary>
/// Lists supported streaming provider authentication mode values.
/// </summary>
public enum StreamingProviderAuthenticationMode
{
    Manual,
    OAuth
}

/// <summary>
/// Lists supported publication stream transport values.
/// </summary>
public enum PublicationStreamTransport
{
    Rtmp,
    Rtmps,
    Srt,
    WebRtc,
    Hls,
    Rtsp
}

/// <summary>
/// Lists supported publication stream quality preset values.
/// </summary>
public enum PublicationStreamQualityPreset
{
    Recommended,
    HighQuality,
    LowLatency,
    BandwidthSaving,
    Custom
}

/// <summary>
/// Lists supported publication stream video codec values.
/// </summary>
public enum PublicationStreamVideoCodec
{
    H264,
    Hevc,
    Av1
}

/// <summary>
/// Lists supported publication stream audio codec values.
/// </summary>
public enum PublicationStreamAudioCodec
{
    Aac,
    Opus
}

/// <summary>
/// Lists supported streaming hardware encoder preference values.
/// </summary>
public enum StreamingHardwareEncoderPreference
{
    Auto,
    Software,
    NvidiaNvenc,
    IntelQuickSync,
    AmdAmf,
    AppleVideoToolbox
}

/// <summary>
/// Lists supported publication stream recording variant values.
/// </summary>
public enum PublicationStreamRecordingVariant
{
    CleanMaster,
    EachEnabledOutput,
    SelectedOutputs
}

/// <summary>
/// Lists supported publication live source kind values.
/// </summary>
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

/// <summary>
/// Lists supported publication live source fit mode values.
/// </summary>
public enum PublicationLiveSourceFitMode
{
    Contain,
    Cover,
    Stretch
}

/// <summary>
/// Lists supported publication capture backend values.
/// </summary>
public enum PublicationCaptureBackend
{
    Auto,
    Browser,
    Native
}

/// <summary>
/// Lists supported publication stream session mode values.
/// </summary>
public enum PublicationStreamSessionMode
{
    Idle,
    DryRun,
    Live
}

/// <summary>
/// Represents a publication streaming settings.
/// </summary>
public sealed class PublicationStreamingSettings
{
    /// <summary>
    /// Gets or sets follow selected page.
    /// </summary>
    public bool FollowSelectedPage { get; set; } = true;
    /// <summary>
    /// Gets or sets program page identifier.
    /// </summary>
    public Guid? ProgramPageId { get; set; }
    /// <summary>
    /// Gets or sets outputs.
    /// </summary>
    public List<PublicationStreamOutput> Outputs { get; set; } = [];
    /// <summary>
    /// Gets or sets recording.
    /// </summary>
    public PublicationRecordingSettings Recording { get; set; } = new();
    /// <summary>
    /// Gets or sets LAN.
    /// </summary>
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    /// <summary>
    /// Gets or sets hotkeys.
    /// </summary>
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } =
    [
        new() { Gesture = "F9", Command = "ToggleStreaming" },
        new() { Gesture = "F10", Command = "ToggleRecording" },
        new() { Gesture = "PageDown", Command = "NextPage" },
        new() { Gesture = "PageUp", Command = "PreviousPage" }
    ];
    /// <summary>
    /// Gets or sets prefer device timestamps.
    /// </summary>
    public bool PreferDeviceTimestamps { get; set; } = true;
    /// <summary>
    /// Gets or sets master width.
    /// </summary>
    public int MasterWidth { get; set; } = 3840;
    /// <summary>
    /// Gets or sets master height.
    /// </summary>
    public int MasterHeight { get; set; } = 2160;
    /// <summary>
    /// Gets or sets master frame rate.
    /// </summary>
    public int MasterFrameRate { get; set; } = 60;
}

/// <summary>
/// Represents a publication stream output.
/// </summary>
public sealed class PublicationStreamOutput
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets profile identifier.
    /// </summary>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Streaming output";
    /// <summary>
    /// Gets or sets provider.
    /// </summary>
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets use provider test mode.
    /// </summary>
    public bool UseProviderTestMode { get; set; }
    /// <summary>
    /// Gets or sets quality preset.
    /// </summary>
    public PublicationStreamQualityPreset QualityPreset { get; set; } = PublicationStreamQualityPreset.Recommended;
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
    /// Gets or sets video bitrate kbps.
    /// </summary>
    public int VideoBitrateKbps { get; set; } = 6000;
    /// <summary>
    /// Gets or sets audio bitrate kbps.
    /// </summary>
    public int AudioBitrateKbps { get; set; } = 160;
    /// <summary>
    /// Gets or sets key frame interval seconds.
    /// </summary>
    public int KeyFrameIntervalSeconds { get; set; } = 2;
    /// <summary>
    /// Gets or sets video codec.
    /// </summary>
    public PublicationStreamVideoCodec VideoCodec { get; set; } = PublicationStreamVideoCodec.H264;
    /// <summary>
    /// Gets or sets audio codec.
    /// </summary>
    public PublicationStreamAudioCodec AudioCodec { get; set; } = PublicationStreamAudioCodec.Aac;
    /// <summary>
    /// Gets or sets chat channel.
    /// </summary>
    public string ChatChannel { get; set; } = string.Empty;
}

/// <summary>
/// Represents a publication recording settings.
/// </summary>
public sealed class PublicationRecordingSettings
{
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets destination directory.
    /// </summary>
    public string DestinationDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets variant.
    /// </summary>
    public PublicationStreamRecordingVariant Variant { get; set; } = PublicationStreamRecordingVariant.CleanMaster;
    /// <summary>
    /// Gets or sets selected output identifiers.
    /// </summary>
    public List<Guid> SelectedOutputIds { get; set; } = [];
    /// <summary>
    /// Gets or sets container.
    /// </summary>
    public string Container { get; set; } = "mkv";
    /// <summary>
    /// Gets or sets segment seconds.
    /// </summary>
    public int SegmentSeconds { get; set; } = 10;
    /// <summary>
    /// Gets or sets remux to mp4 after stop.
    /// </summary>
    public bool RemuxToMp4AfterStop { get; set; } = true;
}

/// <summary>
/// Represents a publication LAN streaming settings.
/// </summary>
public sealed class PublicationLanStreamingSettings
{
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets bind address.
    /// </summary>
    public string BindAddress { get; set; } = "127.0.0.1";
    /// <summary>
    /// Gets or sets port.
    /// </summary>
    public int Port { get; set; } = 17848;
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
    /// Gets or sets video bitrate kbps.
    /// </summary>
    public int VideoBitrateKbps { get; set; } = 8000;
    /// <summary>
    /// Gets or sets enable browser web rtc.
    /// </summary>
    public bool EnableBrowserWebRtc { get; set; } = true;
    /// <summary>
    /// Gets or sets enable hls.
    /// </summary>
    public bool EnableHls { get; set; } = true;
    /// <summary>
    /// Gets or sets enable rtsp.
    /// </summary>
    public bool EnableRtsp { get; set; }
    /// <summary>
    /// Gets or sets rtsp port.
    /// </summary>
    public int RtspPort { get; set; } = 8554;
    /// <summary>
    /// Gets or sets require access token.
    /// </summary>
    public bool RequireAccessToken { get; set; } = true;
    /// <summary>
    /// Gets or sets access token reference.
    /// </summary>
    public string AccessTokenReference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets viewer limit.
    /// </summary>
    public int ViewerLimit { get; set; } = 50;
}

/// <summary>
/// Represents a publication streaming hotkey.
/// </summary>
public sealed class PublicationStreamingHotkey
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets gesture.
    /// </summary>
    public string Gesture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets command.
    /// </summary>
    public string Command { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target identifier.
    /// </summary>
    public Guid? TargetId { get; set; }
    /// <summary>
    /// Gets or sets global.
    /// </summary>
    public bool Global { get; set; }
}

/// <summary>
/// Represents a streaming provider profile.
/// </summary>
public sealed class StreamingProviderProfile
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "New streaming profile";
    /// <summary>
    /// Gets or sets provider.
    /// </summary>
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    /// <summary>
    /// Gets or sets authentication mode.
    /// </summary>
    public StreamingProviderAuthenticationMode AuthenticationMode { get; set; } = StreamingProviderAuthenticationMode.Manual;
    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public PublicationStreamTransport Transport { get; set; } = PublicationStreamTransport.Rtmp;
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets channel identifier.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets account name.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets has stored secret.
    /// </summary>
    public bool HasStoredSecret { get; set; }
    /// <summary>
    /// Gets or sets secret.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets chat enabled.
    /// </summary>
    public bool ChatEnabled { get; set; }
    /// <summary>
    /// Gets or sets has stored chat secret.
    /// </summary>
    public bool HasStoredChatSecret { get; set; }
    /// <summary>
    /// Gets or sets chat secret.
    /// </summary>
    public string ChatSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets OAuth client identifier.
    /// </summary>
    public string OAuthClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets has stored OAuth session.
    /// </summary>
    public bool HasStoredOAuthSession { get; set; }
    /// <summary>
    /// Gets or sets OAuth access token expires UTC.
    /// </summary>
    public DateTimeOffset? OAuthAccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets OAuth last validated UTC.
    /// </summary>
    public DateTimeOffset? OAuthLastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets OAuth scopes.
    /// </summary>
    public string OAuthScopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets auto select ingest.
    /// </summary>
    public bool AutoSelectIngest { get; set; } = true;
    /// <summary>
    /// Gets or sets ingest server name.
    /// </summary>
    public string IngestServerName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets ingest latency milliseconds.
    /// </summary>
    public double? IngestLatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets ingest last tested UTC.
    /// </summary>
    public DateTimeOffset? IngestLastTestedUtc { get; set; }
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Represents a twitch ingest candidate.
/// </summary>
public sealed class TwitchIngestCandidate
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets host.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets latency milliseconds.
    /// </summary>
    public double? LatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets reachable.
    /// </summary>
    public bool Reachable { get; set; }
    /// <summary>
    /// Gets or sets is global.
    /// </summary>
    public bool IsGlobal { get; set; }
}

/// <summary>
/// Represents a twitch device authorization.
/// </summary>
public sealed class TwitchDeviceAuthorization
{
    /// <summary>
    /// Gets or sets device code.
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user code.
    /// </summary>
    public string UserCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verification URI.
    /// </summary>
    public string VerificationUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets expires in seconds.
    /// </summary>
    public int ExpiresInSeconds { get; set; }
    /// <summary>
    /// Gets or sets poll interval seconds.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets scopes.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets expires UTC.
    /// </summary>
    public DateTimeOffset ExpiresUtc { get; set; }
}

/// <summary>
/// Represents a twitch OAuth connection result.
/// </summary>
public sealed class TwitchOAuthConnectionResult
{
    /// <summary>
    /// Gets or sets success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets profile.
    /// </summary>
    public StreamingProviderProfile? Profile { get; set; }
    /// <summary>
    /// Gets or sets ingest candidates.
    /// </summary>
    public List<TwitchIngestCandidate> IngestCandidates { get; set; } = [];
}

/// <summary>
/// Represents a streaming device profile.
/// </summary>
public sealed class StreamingDeviceProfile
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Device";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public PublicationLiveSourceKind Kind { get; set; }
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
    /// Gets or sets window title.
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capture backend.
    /// </summary>
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
    /// <summary>
    /// Gets or sets native backend.
    /// </summary>
    public string NativeBackend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets use device timestamps.
    /// </summary>
    public bool UseDeviceTimestamps { get; set; } = true;
}

/// <summary>
/// Represents a streaming machine settings.
/// </summary>
public sealed class StreamingMachineSettings
{
    /// <summary>
    /// Gets or sets providers.
    /// </summary>
    public List<StreamingProviderProfile> Providers { get; set; } = [];
    /// <summary>
    /// Gets or sets devices.
    /// </summary>
    public List<StreamingDeviceProfile> Devices { get; set; } = [];
    /// <summary>
    /// Gets or sets FFmpeg path.
    /// </summary>
    public string FfmpegPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets default recording directory.
    /// </summary>
    public string DefaultRecordingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets media host port.
    /// </summary>
    public int MediaHostPort { get; set; } = 17847;
    /// <summary>
    /// Gets or sets hardware encoder.
    /// </summary>
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
}

/// <summary>
/// Represents a native media device info.
/// </summary>
public sealed class NativeMediaDeviceInfo
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets backend.
    /// </summary>
    public string Backend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets process identifier.
    /// </summary>
    public string? ProcessId { get; set; }
    /// <summary>
    /// Gets or sets window title.
    /// </summary>
    public string? WindowTitle { get; set; }
}

/// <summary>
/// Represents a browser media device info.
/// </summary>
public sealed class BrowserMediaDeviceInfo
{
    /// <summary>
    /// Gets or sets device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets label.
    /// </summary>
    public string Label { get; set; } = "Permission required";
}

/// <summary>
/// Represents a streaming session snapshot.
/// </summary>
public sealed class StreamingSessionSnapshot
{
    /// <summary>
    /// Gets or sets mode.
    /// </summary>
    public PublicationStreamSessionMode Mode { get; set; }
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid? SessionId { get; set; }
    /// <summary>
    /// Gets or sets program page identifier.
    /// </summary>
    public Guid? ProgramPageId { get; set; }
    /// <summary>
    /// Gets or sets recording.
    /// </summary>
    public bool Recording { get; set; }
    /// <summary>
    /// Gets or sets media host connected.
    /// </summary>
    public bool MediaHostConnected { get; set; }
    /// <summary>
    /// Gets or sets status text.
    /// </summary>
    public string StatusText { get; set; } = "Ready";
    /// <summary>
    /// Gets or sets started UTC.
    /// </summary>
    public DateTimeOffset? StartedUtc { get; set; }
    /// <summary>
    /// Gets or sets dropped frames.
    /// </summary>
    public long DroppedFrames { get; set; }
    /// <summary>
    /// Gets or sets current bitrate kbps.
    /// </summary>
    public double CurrentBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets output enabled.
    /// </summary>
    public Dictionary<Guid, bool> OutputEnabled { get; set; } = [];
}

/// <summary>
/// Represents a live source element.
/// </summary>
public sealed class LiveSourceElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.LiveSource;
    /// <summary>
    /// Gets or sets source kind.
    /// </summary>
    public PublicationLiveSourceKind SourceKind { get; set; } = PublicationLiveSourceKind.Camera;
    /// <summary>
    /// Gets or sets device profile identifier.
    /// </summary>
    public Guid? DeviceProfileId { get; set; }
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
    /// Gets or sets window title.
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capture backend.
    /// </summary>
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
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
    /// Gets or sets use device timestamp.
    /// </summary>
    public bool UseDeviceTimestamp { get; set; } = true;
    /// <summary>
    /// Gets or sets capture width.
    /// </summary>
    public int CaptureWidth { get; set; } = 1920;
    /// <summary>
    /// Gets or sets capture height.
    /// </summary>
    public int CaptureHeight { get; set; } = 1080;
    /// <summary>
    /// Gets or sets capture frame rate.
    /// </summary>
    public int CaptureFrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets fit mode.
    /// </summary>
    public PublicationLiveSourceFitMode FitMode { get; set; } = PublicationLiveSourceFitMode.Cover;
    /// <summary>
    /// Gets or sets muted.
    /// </summary>
    public bool Muted { get; set; } = true;
    /// <summary>
    /// Gets or sets volume.
    /// </summary>
    public double Volume { get; set; } = 1;
    /// <summary>
    /// Gets or sets audio delay milliseconds.
    /// </summary>
    public double AudioDelayMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets brightness.
    /// </summary>
    public double Brightness { get; set; } = 1;
    /// <summary>
    /// Gets or sets contrast.
    /// </summary>
    public double Contrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets saturation.
    /// </summary>
    public double Saturation { get; set; } = 1;
    /// <summary>
    /// Gets or sets hue rotation.
    /// </summary>
    public double HueRotation { get; set; }
    /// <summary>
    /// Gets or sets blur.
    /// </summary>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets chroma key enabled.
    /// </summary>
    public bool ChromaKeyEnabled { get; set; }
    /// <summary>
    /// Gets or sets chroma key color.
    /// </summary>
    public string ChromaKeyColor { get; set; } = "#00ff00";
    /// <summary>
    /// Gets or sets chroma similarity.
    /// </summary>
    public double ChromaSimilarity { get; set; } = .35;
    /// <summary>
    /// Gets or sets chroma smoothness.
    /// </summary>
    public double ChromaSmoothness { get; set; } = .12;
    /// <summary>
    /// Gets or sets chroma spill.
    /// </summary>
    public double ChromaSpill { get; set; } = .3;
    /// <summary>
    /// Gets or sets chroma residual opacity.
    /// </summary>
    public double ChromaResidualOpacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets video layers.
    /// </summary>
    public List<VideoEffectLayer> VideoLayers { get; set; } = [];
    /// <summary>
    /// Gets or sets now playing directory.
    /// </summary>
    public string NowPlayingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#111827";

    /// <summary>
    /// Gets is visual.
    /// </summary>
    [JsonIgnore]
    public bool IsVisual => SourceKind is PublicationLiveSourceKind.Camera
        or PublicationLiveSourceKind.Screen
        or PublicationLiveSourceKind.Window
        or PublicationLiveSourceKind.BrowserTab
        or PublicationLiveSourceKind.CaptureDevice
        or PublicationLiveSourceKind.NetworkMedia;
}
