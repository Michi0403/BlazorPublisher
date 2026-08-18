using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported publication stream values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported streaming provider authentication mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum StreamingProviderAuthenticationMode
{
    Manual,
    OAuth
}

/// <summary>
/// Defines the supported publication stream transport values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported publication stream quality preset values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported publication stream video codec values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationStreamVideoCodec
{
    H264,
    Hevc,
    Av1
}

/// <summary>
/// Defines the supported publication stream audio codec values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationStreamAudioCodec
{
    Aac,
    Opus
}

/// <summary>
/// Defines the supported streaming hardware encoder preference values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported publication stream recording variant values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationStreamRecordingVariant
{
    CleanMaster,
    EachEnabledOutput,
    SelectedOutputs
}

/// <summary>
/// Defines the supported publication live source kind values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported publication live source fit mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationLiveSourceFitMode
{
    Contain,
    Cover,
    Stretch
}

/// <summary>
/// Defines the supported publication capture backend values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationCaptureBackend
{
    Auto,
    Browser,
    Native
}

/// <summary>
/// Defines the supported publication stream session mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationStreamSessionMode
{
    Idle,
    DryRun,
    Live
}

/// <summary>
/// Defines the supported publication adaptive quality profile values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationAdaptiveQualityProfile
{
    Efficiency,
    Balanced,
    Quality
}

/// <summary>Stores per-publication smart media adaptation choices while keeping manual streaming controls available.</summary>
public sealed class PublicationAdaptiveMediaSettings
{
    /// <summary>Gets or sets whether adaptive quality recommendations are active for this publication.</summary>
    /// <value>The enabled value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets whether video geometry, codec and bitrate are smart-selected while leaving manual controls available.</summary>
    /// <value>The adapt video value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool AdaptVideo { get; set; } = true;
    /// <summary>Gets or sets whether audio bitrate is smart-selected from source/channel and provider knowledge.</summary>
    /// <value>The adapt audio value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool AdaptAudio { get; set; } = true;
    /// <summary>Gets or sets whether provider-specific quality knowledge participates in smart output preselection.</summary>
    /// <value>The use provider knowledge value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool UseProviderKnowledge { get; set; } = true;
    /// <summary>Gets or sets whether browser MediaCapabilities evidence may refine recording codec/FPS choices.</summary>
    /// <value>The use browser capability probe value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool UseBrowserCapabilityProbe { get; set; } = true;
    /// <summary>Gets or sets the publication quality priority used by the adaptive advisor.</summary>
    /// <value>The profile value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public PublicationAdaptiveQualityProfile Profile { get; set; } = PublicationAdaptiveQualityProfile.Quality;
    /// <summary>Gets or sets whether the browser should preserve the selected capture surface's native resolution.</summary>
    /// <value>The preserve native resolution value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool PreserveNativeResolution { get; set; } = true;
    /// <summary>Gets or sets whether automatic smoothness recovery may lower frame rate before compromising resolution.</summary>
    /// <value>The allow frame rate reduction value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool AllowFrameRateReduction { get; set; } = true;
    /// <summary>Gets or sets whether automatic smoothness recovery may lower resolution as a last resort.</summary>
    /// <value>The allow resolution reduction value exposed by <see cref="PublicationAdaptiveMediaSettings"/>.</value>
    public bool AllowResolutionReduction { get; set; }
}


/// <summary>
/// Carries the configurable publication streaming settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationStreamingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether follow selected page applies to the publication streaming state.
    /// </summary>
    /// <value>The follow selected page value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public bool FollowSelectedPage { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable program page identifier used to identify or correlate this publication streaming instance with related application state.
    /// </summary>
    /// <value>The program page identifier value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public Guid? ProgramPageId { get; set; }
    /// <summary>
    /// Gets or sets the outputs collection maintained or exposed by this publication streaming instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public List<PublicationStreamOutput> Outputs { get; set; } = [];
    /// <summary>
    /// Gets or sets the recording value that forms part of the publication streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public PublicationRecordingSettings Recording { get; set; } = new();
    /// <summary>
    /// Gets or sets the LAN value that forms part of the publication streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    /// <summary>
    /// Gets or sets the hotkeys collection maintained or exposed by this publication streaming instance for downstream processing.
    /// </summary>
    /// <value>The hotkeys value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } =
    [
        new() { Gesture = "F9", Command = "ToggleStreaming" },
        new() { Gesture = "F10", Command = "ToggleRecording" },
        new() { Gesture = "PageDown", Command = "NextPage" },
        new() { Gesture = "PageUp", Command = "PreviousPage" }
    ];
    /// <summary>
    /// Gets or sets a value indicating whether prefer device timestamps applies to the publication streaming state.
    /// </summary>
    /// <value>The prefer device timestamps value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public bool PreferDeviceTimestamps { get; set; } = true;
    /// <summary>Gets or sets the smart adaptive media quality choices shared by this publication's recording and streaming paths.</summary>
    /// <value>The adaptive media value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public PublicationAdaptiveMediaSettings AdaptiveMedia { get; set; } = new();
    /// <summary>
    /// Gets or sets the master width value that forms part of the publication streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master width value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public int MasterWidth { get; set; } = 3840;
    /// <summary>
    /// Gets or sets the master height value that forms part of the publication streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master height value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public int MasterHeight { get; set; } = 2160;
    /// <summary>
    /// Gets or sets the master frame rate value that forms part of the publication streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master frame rate value exposed by <see cref="PublicationStreamingSettings"/>.</value>
    public int MasterFrameRate { get; set; } = 60;
}

/// <summary>
/// Represents a publication stream output application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationStreamOutput
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication stream output instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable profile identifier used to identify or correlate this publication stream output instance with related application state.
    /// </summary>
    /// <value>The profile identifier value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public string Name { get; set; } = "Streaming output";
    /// <summary>
    /// Gets or sets the provider value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication stream output state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether provider test mode applies to the publication stream output state.
    /// </summary>
    /// <value>The use provider test mode value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public bool UseProviderTestMode { get; set; }
    /// <summary>
    /// Gets or sets the quality preset value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality preset value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public PublicationStreamQualityPreset QualityPreset { get; set; } = PublicationStreamQualityPreset.Recommended;
    /// <summary>
    /// Gets or sets the width value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets the height value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int FrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets the video bitrate kbps value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video bitrate kbps value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int VideoBitrateKbps { get; set; } = 6000;
    /// <summary>
    /// Gets or sets the audio bitrate kbps value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int AudioBitrateKbps { get; set; } = 160;
    /// <summary>
    /// Gets or sets the key frame interval seconds value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key frame interval seconds value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public int KeyFrameIntervalSeconds { get; set; } = 2;
    /// <summary>
    /// Gets or sets the video codec value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video codec value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public PublicationStreamVideoCodec VideoCodec { get; set; } = PublicationStreamVideoCodec.H264;
    /// <summary>
    /// Gets or sets the audio codec value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio codec value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public PublicationStreamAudioCodec AudioCodec { get; set; } = PublicationStreamAudioCodec.Aac;
    /// <summary>
    /// Gets or sets the chat channel value that forms part of the publication stream output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat channel value exposed by <see cref="PublicationStreamOutput"/>.</value>
    public string ChatChannel { get; set; } = string.Empty;
}

/// <summary>
/// Carries the configurable publication recording settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationRecordingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication recording state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the destination directory used by this publication recording instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The destination directory value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public string DestinationDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the variant value that forms part of the publication recording state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The variant value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public PublicationStreamRecordingVariant Variant { get; set; } = PublicationStreamRecordingVariant.CleanMaster;
    /// <summary>
    /// Gets or sets the selected output identifiers collection maintained or exposed by this publication recording instance for downstream processing.
    /// </summary>
    /// <value>The selected output identifiers value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public List<Guid> SelectedOutputIds { get; set; } = [];
    /// <summary>
    /// Gets or sets the container value that forms part of the publication recording state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The container value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public string Container { get; set; } = "mkv";
    /// <summary>
    /// Gets or sets the segment seconds value that forms part of the publication recording state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The segment seconds value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public int SegmentSeconds { get; set; } = 10;
    /// <summary>
    /// Gets or sets a value indicating whether remux to mp4 after stop applies to the publication recording state.
    /// </summary>
    /// <value>The remux to mp4 after stop value exposed by <see cref="PublicationRecordingSettings"/>.</value>
    public bool RemuxToMp4AfterStop { get; set; } = true;
}

/// <summary>
/// Carries the configurable publication LAN streaming settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationLanStreamingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication LAN streaming state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets the bind address that identifies the network or application endpoint associated with this publication LAN streaming state.
    /// </summary>
    /// <value>The bind address value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public string BindAddress { get; set; } = "127.0.0.1";
    /// <summary>
    /// Gets or sets the port value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int Port { get; set; } = 17848;
    /// <summary>
    /// Gets or sets the width value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets the height value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int FrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets the video bitrate kbps value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video bitrate kbps value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int VideoBitrateKbps { get; set; } = 8000;
    /// <summary>Gets or sets the LAN audio bitrate in kilobits per second.</summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int AudioBitrateKbps { get; set; } = 160;
    /// <summary>Gets or sets the LAN quality preset; Recommended is refreshed by the adaptive advisor while Custom preserves manual values.</summary>
    /// <value>The quality preset value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public PublicationStreamQualityPreset QualityPreset { get; set; } = PublicationStreamQualityPreset.Recommended;
    /// <summary>
    /// Gets or sets a value indicating whether browser web rtc applies to the publication LAN streaming state.
    /// </summary>
    /// <value>The enable browser web rtc value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public bool EnableBrowserWebRtc { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether hls applies to the publication LAN streaming state.
    /// </summary>
    /// <value>The enable hls value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public bool EnableHls { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether rtsp applies to the publication LAN streaming state.
    /// </summary>
    /// <value>The enable rtsp value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public bool EnableRtsp { get; set; }
    /// <summary>
    /// Gets or sets the rtsp port value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp port value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int RtspPort { get; set; } = 8554;
    /// <summary>
    /// Gets or sets a value indicating whether access token applies to the publication LAN streaming state.
    /// </summary>
    /// <value>The require access token value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public bool RequireAccessToken { get; set; } = true;
    /// <summary>
    /// Gets or sets the access token reference value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access token reference value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public string AccessTokenReference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the viewer limit value that forms part of the publication LAN streaming state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The viewer limit value exposed by <see cref="PublicationLanStreamingSettings"/>.</value>
    public int ViewerLimit { get; set; } = 50;
}

/// <summary>
/// Represents a publication streaming hotkey application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationStreamingHotkey
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication streaming hotkey instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationStreamingHotkey"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the gesture value that forms part of the publication streaming hotkey state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The gesture value exposed by <see cref="PublicationStreamingHotkey"/>.</value>
    public string Gesture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the command value that forms part of the publication streaming hotkey state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The command value exposed by <see cref="PublicationStreamingHotkey"/>.</value>
    public string Command { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable target identifier used to identify or correlate this publication streaming hotkey instance with related application state.
    /// </summary>
    /// <value>The target identifier value exposed by <see cref="PublicationStreamingHotkey"/>.</value>
    public Guid? TargetId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether global applies to the publication streaming hotkey state.
    /// </summary>
    /// <value>The global value exposed by <see cref="PublicationStreamingHotkey"/>.</value>
    public bool Global { get; set; }
}

/// <summary>
/// Represents a streaming provider profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class StreamingProviderProfile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this streaming provider profile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string Name { get; set; } = "New streaming profile";
    /// <summary>
    /// Gets or sets the provider value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public PublicationStreamProvider Provider { get; set; } = PublicationStreamProvider.Twitch;
    /// <summary>
    /// Gets or sets the authentication mode value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authentication mode value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public StreamingProviderAuthenticationMode AuthenticationMode { get; set; } = StreamingProviderAuthenticationMode.Manual;
    /// <summary>
    /// Gets or sets the transport value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public PublicationStreamTransport Transport { get; set; } = PublicationStreamTransport.Rtmp;
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this streaming provider profile state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable channel identifier used to identify or correlate this streaming provider profile instance with related application state.
    /// </summary>
    /// <value>The channel identifier value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string ChannelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the account name value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The account name value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether stored secret applies to the streaming provider profile state.
    /// </summary>
    /// <value>The has stored secret value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool HasStoredSecret { get; set; }
    /// <summary>
    /// Gets or sets the secret value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secret value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string Secret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether chat enabled applies to the streaming provider profile state.
    /// </summary>
    /// <value>The chat enabled value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool ChatEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether stored chat secret applies to the streaming provider profile state.
    /// </summary>
    /// <value>The has stored chat secret value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool HasStoredChatSecret { get; set; }
    /// <summary>
    /// Gets or sets the chat secret value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat secret value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string ChatSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable o auth client identifier used to identify or correlate this streaming provider profile instance with related application state.
    /// </summary>
    /// <value>The o auth client identifier value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string OAuthClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether stored o auth session applies to the streaming provider profile state.
    /// </summary>
    /// <value>The has stored o auth session value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool HasStoredOAuthSession { get; set; }
    /// <summary>
    /// Gets or sets the o auth access token expires UTC associated with this streaming provider profile state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The o auth access token expires UTC value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public DateTimeOffset? OAuthAccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets the o auth last validated UTC associated with this streaming provider profile state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The o auth last validated UTC value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public DateTimeOffset? OAuthLastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the o auth scopes value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The o auth scopes value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string OAuthScopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether auto select ingest applies to the streaming provider profile state.
    /// </summary>
    /// <value>The auto select ingest value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool AutoSelectIngest { get; set; } = true;
    /// <summary>
    /// Gets or sets the ingest server name value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest server name value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public string IngestServerName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ingest latency milliseconds value that forms part of the streaming provider profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest latency milliseconds value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public double? IngestLatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the ingest last tested UTC associated with this streaming provider profile state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The ingest last tested UTC value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public DateTimeOffset? IngestLastTestedUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the streaming provider profile state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="StreamingProviderProfile"/>.</value>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Represents a twitch ingest candidate application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class TwitchIngestCandidate
{
    /// <summary>
    /// Gets or sets the name value that forms part of the twitch ingest candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this twitch ingest candidate state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the host value that forms part of the twitch ingest candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The host value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public string Host { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the latency milliseconds value that forms part of the twitch ingest candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The latency milliseconds value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public double? LatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether reachable applies to the twitch ingest candidate state.
    /// </summary>
    /// <value>The reachable value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public bool Reachable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether global applies to the twitch ingest candidate state.
    /// </summary>
    /// <value>The is global value exposed by <see cref="TwitchIngestCandidate"/>.</value>
    public bool IsGlobal { get; set; }
}

/// <summary>
/// Represents a twitch device authorization application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class TwitchDeviceAuthorization
{
    /// <summary>
    /// Gets or sets the device code value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device code value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public string DeviceCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user code value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user code value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public string UserCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verification URI that identifies the network or application endpoint associated with this twitch device authorization state.
    /// </summary>
    /// <value>The verification URI value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public string VerificationUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the expires in seconds value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expires in seconds value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public int ExpiresInSeconds { get; set; }
    /// <summary>
    /// Gets or sets the poll interval seconds value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The poll interval seconds value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public int PollIntervalSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets the stable client identifier used to identify or correlate this twitch device authorization instance with related application state.
    /// </summary>
    /// <value>The client identifier value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scopes value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scopes value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the expires UTC associated with this twitch device authorization state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The expires UTC value exposed by <see cref="TwitchDeviceAuthorization"/>.</value>
    public DateTimeOffset ExpiresUtc { get; set; }
}

/// <summary>
/// Represents the outcome of twitch o auth connection, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class TwitchOAuthConnectionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the twitch o auth connection state.
    /// </summary>
    /// <value>The success value exposed by <see cref="TwitchOAuthConnectionResult"/>.</value>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the message value that forms part of the twitch o auth connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="TwitchOAuthConnectionResult"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the profile value that forms part of the twitch o auth connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The profile value exposed by <see cref="TwitchOAuthConnectionResult"/>.</value>
    public StreamingProviderProfile? Profile { get; set; }
    /// <summary>
    /// Gets or sets the ingest candidates collection maintained or exposed by this twitch o auth connection instance for downstream processing.
    /// </summary>
    /// <value>The ingest candidates value exposed by <see cref="TwitchOAuthConnectionResult"/>.</value>
    public List<TwitchIngestCandidate> IngestCandidates { get; set; } = [];
}

/// <summary>
/// Represents a streaming device profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class StreamingDeviceProfile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this streaming device profile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the streaming device profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string Name { get; set; } = "Device";
    /// <summary>
    /// Gets or sets the kind value that forms part of the streaming device profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public PublicationLiveSourceKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this streaming device profile instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable audio device identifier used to identify or correlate this streaming device profile instance with related application state.
    /// </summary>
    /// <value>The audio device identifier value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string AudioDeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable application identifier used to identify or correlate this streaming device profile instance with related application state.
    /// </summary>
    /// <value>The application identifier value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string ApplicationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the window title value that forms part of the streaming device profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window title value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capture backend value that forms part of the streaming device profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capture backend value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
    /// <summary>
    /// Gets or sets the native backend value that forms part of the streaming device profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native backend value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public string NativeBackend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether device timestamps applies to the streaming device profile state.
    /// </summary>
    /// <value>The use device timestamps value exposed by <see cref="StreamingDeviceProfile"/>.</value>
    public bool UseDeviceTimestamps { get; set; } = true;
}

/// <summary>
/// Carries the configurable streaming machine settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class StreamingMachineSettings
{
    /// <summary>
    /// Gets or sets the providers collection maintained or exposed by this streaming machine instance for downstream processing.
    /// </summary>
    /// <value>The providers value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public List<StreamingProviderProfile> Providers { get; set; } = [];
    /// <summary>
    /// Gets or sets the devices collection maintained or exposed by this streaming machine instance for downstream processing.
    /// </summary>
    /// <value>The devices value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public List<StreamingDeviceProfile> Devices { get; set; } = [];
    /// <summary>
    /// Gets or sets the FFmpeg path used by this streaming machine instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg path value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public string FfmpegPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the default recording directory used by this streaming machine instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The default recording directory value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public string DefaultRecordingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the media host port value that forms part of the streaming machine state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media host port value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public int MediaHostPort { get; set; } = 17847;
    /// <summary>
    /// Gets or sets the hardware encoder value that forms part of the streaming machine state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware encoder value exposed by <see cref="StreamingMachineSettings"/>.</value>
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
}

/// <summary>
/// Represents a native media device info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class NativeMediaDeviceInfo
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this native media device info instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name value that forms part of the native media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the native media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the backend value that forms part of the native media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The backend value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string Backend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable process identifier used to identify or correlate this native media device info instance with related application state.
    /// </summary>
    /// <value>The process identifier value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string? ProcessId { get; set; }
    /// <summary>
    /// Gets or sets the window title value that forms part of the native media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window title value exposed by <see cref="NativeMediaDeviceInfo"/>.</value>
    public string? WindowTitle { get; set; }
}

/// <summary>
/// Represents a browser media device info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class BrowserMediaDeviceInfo
{
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this browser media device info instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="BrowserMediaDeviceInfo"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the browser media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="BrowserMediaDeviceInfo"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the label value that forms part of the browser media device info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The label value exposed by <see cref="BrowserMediaDeviceInfo"/>.</value>
    public string Label { get; set; } = "Permission required";
}

/// <summary>
/// Represents a streaming session snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class StreamingSessionSnapshot
{
    /// <summary>
    /// Gets or sets the mode value that forms part of the streaming session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mode value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public PublicationStreamSessionMode Mode { get; set; }
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this streaming session snapshot instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public Guid? SessionId { get; set; }
    /// <summary>
    /// Gets or sets the stable program page identifier used to identify or correlate this streaming session snapshot instance with related application state.
    /// </summary>
    /// <value>The program page identifier value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public Guid? ProgramPageId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether recording applies to the streaming session snapshot state.
    /// </summary>
    /// <value>The recording value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public bool Recording { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether media host connected applies to the streaming session snapshot state.
    /// </summary>
    /// <value>The media host connected value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public bool MediaHostConnected { get; set; }
    /// <summary>
    /// Gets or sets the status text value that forms part of the streaming session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status text value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public string StatusText { get; set; } = "Ready";
    /// <summary>
    /// Gets or sets the started UTC associated with this streaming session snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started UTC value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public DateTimeOffset? StartedUtc { get; set; }
    /// <summary>
    /// Gets or sets the dropped frames value that forms part of the streaming session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dropped frames value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public long DroppedFrames { get; set; }
    /// <summary>
    /// Gets or sets the current bitrate kbps value that forms part of the streaming session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current bitrate kbps value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public double CurrentBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the output enabled collection maintained or exposed by this streaming session snapshot instance for downstream processing.
    /// </summary>
    /// <value>The output enabled value exposed by <see cref="StreamingSessionSnapshot"/>.</value>
    public Dictionary<Guid, bool> OutputEnabled { get; set; } = [];
}

/// <summary>
/// Represents a live source element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LiveSourceElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="LiveSourceElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.LiveSource;
    /// <summary>
    /// Gets or sets the source kind value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source kind value exposed by <see cref="LiveSourceElement"/>.</value>
    public PublicationLiveSourceKind SourceKind { get; set; } = PublicationLiveSourceKind.Camera;
    /// <summary>
    /// Gets or sets the stable device profile identifier used to identify or correlate this live source element instance with related application state.
    /// </summary>
    /// <value>The device profile identifier value exposed by <see cref="LiveSourceElement"/>.</value>
    public Guid? DeviceProfileId { get; set; }
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this live source element instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="LiveSourceElement"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable audio device identifier used to identify or correlate this live source element instance with related application state.
    /// </summary>
    /// <value>The audio device identifier value exposed by <see cref="LiveSourceElement"/>.</value>
    public string AudioDeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable application identifier used to identify or correlate this live source element instance with related application state.
    /// </summary>
    /// <value>The application identifier value exposed by <see cref="LiveSourceElement"/>.</value>
    public string ApplicationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the window title value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window title value exposed by <see cref="LiveSourceElement"/>.</value>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capture backend value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capture backend value exposed by <see cref="LiveSourceElement"/>.</value>
    public PublicationCaptureBackend CaptureBackend { get; set; } = PublicationCaptureBackend.Auto;
    /// <summary>
    /// Gets or sets the native backend value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native backend value exposed by <see cref="LiveSourceElement"/>.</value>
    public string NativeBackend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the network URL that identifies the network or application endpoint associated with this live source element state.
    /// </summary>
    /// <value>The network URL value exposed by <see cref="LiveSourceElement"/>.</value>
    public string NetworkUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether audio applies to the live source element state.
    /// </summary>
    /// <value>The include audio value exposed by <see cref="LiveSourceElement"/>.</value>
    public bool IncludeAudio { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether device timestamp applies to the live source element state.
    /// </summary>
    /// <value>The use device timestamp value exposed by <see cref="LiveSourceElement"/>.</value>
    public bool UseDeviceTimestamp { get; set; } = true;
    /// <summary>
    /// Gets or sets the capture width value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capture width value exposed by <see cref="LiveSourceElement"/>.</value>
    public int CaptureWidth { get; set; } = 1920;
    /// <summary>
    /// Gets or sets the capture height value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capture height value exposed by <see cref="LiveSourceElement"/>.</value>
    public int CaptureHeight { get; set; } = 1080;
    /// <summary>
    /// Gets or sets the capture frame rate value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capture frame rate value exposed by <see cref="LiveSourceElement"/>.</value>
    public int CaptureFrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets the fit mode value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fit mode value exposed by <see cref="LiveSourceElement"/>.</value>
    public PublicationLiveSourceFitMode FitMode { get; set; } = PublicationLiveSourceFitMode.Cover;
    /// <summary>
    /// Gets or sets a value indicating whether muted applies to the live source element state.
    /// </summary>
    /// <value>The muted value exposed by <see cref="LiveSourceElement"/>.</value>
    public bool Muted { get; set; } = true;
    /// <summary>
    /// Gets or sets the volume value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The volume value exposed by <see cref="LiveSourceElement"/>.</value>
    public double Volume { get; set; } = 1;
    /// <summary>
    /// Gets or sets the audio delay milliseconds value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio delay milliseconds value exposed by <see cref="LiveSourceElement"/>.</value>
    public double AudioDelayMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the brightness value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brightness value exposed by <see cref="LiveSourceElement"/>.</value>
    public double Brightness { get; set; } = 1;
    /// <summary>
    /// Gets or sets the contrast value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contrast value exposed by <see cref="LiveSourceElement"/>.</value>
    public double Contrast { get; set; } = 1;
    /// <summary>
    /// Gets or sets the saturation value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saturation value exposed by <see cref="LiveSourceElement"/>.</value>
    public double Saturation { get; set; } = 1;
    /// <summary>
    /// Gets or sets the hue rotation value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hue rotation value exposed by <see cref="LiveSourceElement"/>.</value>
    public double HueRotation { get; set; }
    /// <summary>
    /// Gets or sets the blur value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blur value exposed by <see cref="LiveSourceElement"/>.</value>
    public double Blur { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether chroma key enabled applies to the live source element state.
    /// </summary>
    /// <value>The chroma key enabled value exposed by <see cref="LiveSourceElement"/>.</value>
    public bool ChromaKeyEnabled { get; set; }
    /// <summary>
    /// Gets or sets the chroma key color value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chroma key color value exposed by <see cref="LiveSourceElement"/>.</value>
    public string ChromaKeyColor { get; set; } = "#00ff00";
    /// <summary>
    /// Gets or sets the chroma similarity value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chroma similarity value exposed by <see cref="LiveSourceElement"/>.</value>
    public double ChromaSimilarity { get; set; } = .35;
    /// <summary>
    /// Gets or sets the chroma smoothness value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chroma smoothness value exposed by <see cref="LiveSourceElement"/>.</value>
    public double ChromaSmoothness { get; set; } = .12;
    /// <summary>
    /// Gets or sets the chroma spill value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chroma spill value exposed by <see cref="LiveSourceElement"/>.</value>
    public double ChromaSpill { get; set; } = .3;
    /// <summary>
    /// Gets or sets the chroma residual opacity value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chroma residual opacity value exposed by <see cref="LiveSourceElement"/>.</value>
    public double ChromaResidualOpacity { get; set; } = 1;
    /// <summary>
    /// Gets or sets the video layers collection maintained or exposed by this live source element instance for downstream processing.
    /// </summary>
    /// <value>The video layers value exposed by <see cref="LiveSourceElement"/>.</value>
    public List<VideoEffectLayer> VideoLayers { get; set; } = [];
    /// <summary>
    /// Gets or sets the now playing directory used by this live source element instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The now playing directory value exposed by <see cref="LiveSourceElement"/>.</value>
    public string NowPlayingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the background value that forms part of the live source element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="LiveSourceElement"/>.</value>
    public string Background { get; set; } = "#111827";

    /// <summary>
    /// Gets a value indicating whether visual applies to the live source element state.
    /// </summary>
    /// <value>The is visual value exposed by <see cref="LiveSourceElement"/>.</value>
    [JsonIgnore]
    public bool IsVisual => SourceKind is PublicationLiveSourceKind.Camera
        or PublicationLiveSourceKind.Screen
        or PublicationLiveSourceKind.Window
        or PublicationLiveSourceKind.BrowserTab
        or PublicationLiveSourceKind.CaptureDevice
        or PublicationLiveSourceKind.NetworkMedia;
}
