namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported publisher runtime pattern values.
/// </summary>
public enum PublisherRuntimePattern
{
    PublicationBody,
    PublicationStyle,
    PublicationDangerousElements,
    PublicationEventAttribute,
    PublicationJavascriptUrl,
    CodeString,
    CodeNumber,
    OpenDocumentLength,
    OpenDocumentRotate,
    OpenDocumentColor,
    VideoEdlEvent,
    VideoEdlClipName,
    VideoEdlSourceFile,
    MediaDuration,
    NativeDirectShowDevice,
    NativeAvFoundationDevice
}

/// <summary>
/// Lists supported publisher runtime collection values.
/// </summary>
public enum PublisherRuntimeCollection
{
    VideoProjectExtensions,
    NowPlayingExtensions,
    AllowedFfmpegExecutableNames,
    ForbiddenFfmpegAdvancedOptions,
    FfmpegWindowsBundledPaths,
    FfmpegUnixBundledPaths,
    FfmpegUnixInstallPaths
}

/// <summary>
/// Represents a publisher regex policy.
/// </summary>
public sealed record PublisherRegexPolicy
{
    /// <summary>
    /// Gets or sets pattern.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets options.
    /// </summary>
    public string Options { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets timeout milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; init; }
}

/// <summary>
/// Represents a publisher twitch endpoint policy.
/// </summary>
public sealed record PublisherTwitchEndpointPolicy
{
    /// <summary>
    /// Gets or sets device authorization URL.
    /// </summary>
    public string DeviceAuthorizationUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets token URL.
    /// </summary>
    public string TokenUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets validate URL.
    /// </summary>
    public string ValidateUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets revoke URL.
    /// </summary>
    public string RevokeUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets stream key URL.
    /// </summary>
    public string StreamKeyUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets ingest URL.
    /// </summary>
    public string IngestUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets global endpoint.
    /// </summary>
    public string GlobalEndpoint { get; init; } = string.Empty;
}

/// <summary>
/// Represents a publisher native interop policy.
/// </summary>
public sealed record PublisherNativeInteropPolicy
{
    /// <summary>
    /// Gets or sets virtual audio device process loopback.
    /// </summary>
    public string VirtualAudioDeviceProcessLoopback { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets audio client stream flags loopback.
    /// </summary>
    public uint AudioClientStreamFlagsLoopback { get; init; }
    /// <summary>
    /// Gets or sets audio client stream flags event callback.
    /// </summary>
    public uint AudioClientStreamFlagsEventCallback { get; init; }
    /// <summary>
    /// Gets or sets audio client stream flags source default quality.
    /// </summary>
    public uint AudioClientStreamFlagsSourceDefaultQuality { get; init; }
    /// <summary>
    /// Gets or sets audio client stream flags auto convert pcm.
    /// </summary>
    public uint AudioClientStreamFlagsAutoConvertPcm { get; init; }
    /// <summary>
    /// Gets or sets audio client buffer flags silent.
    /// </summary>
    public uint AudioClientBufferFlagsSilent { get; init; }
    /// <summary>
    /// Gets or sets variant blob type.
    /// </summary>
    public ushort VariantBlobType { get; init; }
    /// <summary>
    /// Gets or sets window message hotkey.
    /// </summary>
    public uint WindowMessageHotkey { get; init; }
    /// <summary>
    /// Gets or sets window message command.
    /// </summary>
    public uint WindowMessageCommand { get; init; }
    /// <summary>
    /// Gets or sets window message quit.
    /// </summary>
    public uint WindowMessageQuit { get; init; }
    /// <summary>
    /// Gets or sets modifier alt.
    /// </summary>
    public uint ModifierAlt { get; init; }
    /// <summary>
    /// Gets or sets modifier control.
    /// </summary>
    public uint ModifierControl { get; init; }
    /// <summary>
    /// Gets or sets modifier shift.
    /// </summary>
    public uint ModifierShift { get; init; }
    /// <summary>
    /// Gets or sets modifier windows.
    /// </summary>
    public uint ModifierWindows { get; init; }
    /// <summary>
    /// Gets or sets modifier no repeat.
    /// </summary>
    public uint ModifierNoRepeat { get; init; }
    /// <summary>
    /// Gets or sets audio activation success result.
    /// </summary>
    public int AudioActivationSuccessResult { get; init; }
}

/// <summary>
/// Represents a publisher picture studio policy.
/// </summary>
public sealed record PublisherPictureStudioPolicy
{
    /// <summary>
    /// Gets or sets canvas identifier.
    /// </summary>
    public string CanvasId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets canvas host identifier.
    /// </summary>
    public string CanvasHostId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets studio root identifier.
    /// </summary>
    public string StudioRootId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets image input identifier.
    /// </summary>
    public string ImageInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets image drop input identifier.
    /// </summary>
    public string ImageDropInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets layered input identifier.
    /// </summary>
    public string LayeredInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets layer drop input identifier.
    /// </summary>
    public string LayerDropInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets minimum draw width.
    /// </summary>
    public double MinimumDrawWidth { get; init; }
    /// <summary>
    /// Gets or sets maximum draw width.
    /// </summary>
    public double MaximumDrawWidth { get; init; }
}

/// <summary>
/// Represents a publisher twitch configuration.
/// </summary>
public sealed record PublisherTwitchConfiguration
{
    /// <summary>
    /// Gets or sets client identifier.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets client identifier environment variable.
    /// </summary>
    public string ClientIdEnvironmentVariable { get; init; } = string.Empty;
}


/// <summary>
/// Represents a publisher document defaults policy.
/// </summary>
public sealed record PublisherDocumentDefaultsPolicy
{
    /// <summary>
    /// Gets or sets publication name.
    /// </summary>
    public string PublicationName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets publication format version.
    /// </summary>
    public string PublicationFormatVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets publication zoom.
    /// </summary>
    public double PublicationZoom { get; init; }
    /// <summary>
    /// Gets or sets page name.
    /// </summary>
    public string PageName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets page width millimeters.
    /// </summary>
    public double PageWidthMillimeters { get; init; }
    /// <summary>
    /// Gets or sets page height millimeters.
    /// </summary>
    public double PageHeightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets page background.
    /// </summary>
    public string PageBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets title name.
    /// </summary>
    public string TitleName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets title preview HTML.
    /// </summary>
    public string TitlePreviewHtml { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets title horizontal position.
    /// </summary>
    public double TitleX { get; init; }
    /// <summary>
    /// Gets or sets title vertical position.
    /// </summary>
    public double TitleY { get; init; }
    /// <summary>
    /// Gets or sets title width.
    /// </summary>
    public double TitleWidth { get; init; }
    /// <summary>
    /// Gets or sets title height.
    /// </summary>
    public double TitleHeight { get; init; }
    /// <summary>
    /// Gets or sets accent name.
    /// </summary>
    public string AccentName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets accent horizontal position.
    /// </summary>
    public double AccentX { get; init; }
    /// <summary>
    /// Gets or sets accent vertical position.
    /// </summary>
    public double AccentY { get; init; }
    /// <summary>
    /// Gets or sets accent width.
    /// </summary>
    public double AccentWidth { get; init; }
    /// <summary>
    /// Gets or sets accent height.
    /// </summary>
    public double AccentHeight { get; init; }
    /// <summary>
    /// Gets or sets accent fill.
    /// </summary>
    public string AccentFill { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets accent stroke.
    /// </summary>
    public string AccentStroke { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets picture name.
    /// </summary>
    public string PictureName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets picture format version.
    /// </summary>
    public string PictureFormatVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets picture width pixels.
    /// </summary>
    public int PictureWidthPixels { get; init; }
    /// <summary>
    /// Gets or sets picture height pixels.
    /// </summary>
    public int PictureHeightPixels { get; init; }
    /// <summary>
    /// Gets or sets picture minimum dimension.
    /// </summary>
    public int PictureMinimumDimension { get; init; }
    /// <summary>
    /// Gets or sets picture maximum dimension.
    /// </summary>
    public int PictureMaximumDimension { get; init; }
    /// <summary>
    /// Gets or sets picture transparent background.
    /// </summary>
    public string PictureTransparentBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets picture opaque background.
    /// </summary>
    public string PictureOpaqueBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets picture zoom.
    /// </summary>
    public double PictureZoom { get; init; }
    /// <summary>
    /// Gets or sets picture grid spacing pixels.
    /// </summary>
    public int PictureGridSpacingPixels { get; init; }
    /// <summary>
    /// Gets or sets story page width millimeters.
    /// </summary>
    public double StoryPageWidthMillimeters { get; init; }
    /// <summary>
    /// Gets or sets story page height millimeters.
    /// </summary>
    public double StoryPageHeightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets story margin top millimeters.
    /// </summary>
    public double StoryMarginTopMillimeters { get; init; }
    /// <summary>
    /// Gets or sets story margin right millimeters.
    /// </summary>
    public double StoryMarginRightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets story margin bottom millimeters.
    /// </summary>
    public double StoryMarginBottomMillimeters { get; init; }
    /// <summary>
    /// Gets or sets story margin left millimeters.
    /// </summary>
    public double StoryMarginLeftMillimeters { get; init; }
    /// <summary>
    /// Gets or sets page presets.
    /// </summary>
    public List<PagePreset> PagePresets { get; init; } = [];
}

/// <summary>
/// Represents a publisher media session defaults policy.
/// </summary>
public sealed record PublisherMediaSessionDefaultsPolicy
{
    /// <summary>
    /// Gets or sets publication name.
    /// </summary>
    public string PublicationName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets output name.
    /// </summary>
    public string OutputName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets prefer device timestamps.
    /// </summary>
    public bool PreferDeviceTimestamps { get; init; }
    /// <summary>
    /// Gets or sets hardware encoder.
    /// </summary>
    public int HardwareEncoder { get; init; }
    /// <summary>
    /// Gets or sets output provider.
    /// </summary>
    public int OutputProvider { get; init; }
    /// <summary>
    /// Gets or sets output transport.
    /// </summary>
    public int OutputTransport { get; init; }
    /// <summary>
    /// Gets or sets video codec.
    /// </summary>
    public int VideoCodec { get; init; }
    /// <summary>
    /// Gets or sets audio codec.
    /// </summary>
    public int AudioCodec { get; init; }
    /// <summary>
    /// Gets or sets recording variant.
    /// </summary>
    public int RecordingVariant { get; init; }
    /// <summary>
    /// Gets or sets enable browser web rtc.
    /// </summary>
    public bool EnableBrowserWebRtc { get; init; }
    /// <summary>
    /// Gets or sets enable hls.
    /// </summary>
    public bool EnableHls { get; init; }
    /// <summary>
    /// Gets or sets enable rtsp.
    /// </summary>
    public bool EnableRtsp { get; init; }
    /// <summary>
    /// Gets or sets require access token.
    /// </summary>
    public bool RequireAccessToken { get; init; }
    /// <summary>
    /// Gets or sets waiting for renderer status.
    /// </summary>
    public string WaitingForRendererStatus { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets ready status.
    /// </summary>
    public string ReadyStatus { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets master width.
    /// </summary>
    public int MasterWidth { get; init; }
    /// <summary>
    /// Gets or sets master height.
    /// </summary>
    public int MasterHeight { get; init; }
    /// <summary>
    /// Gets or sets master frame rate.
    /// </summary>
    public int MasterFrameRate { get; init; }
    /// <summary>
    /// Gets or sets output width.
    /// </summary>
    public int OutputWidth { get; init; }
    /// <summary>
    /// Gets or sets output height.
    /// </summary>
    public int OutputHeight { get; init; }
    /// <summary>
    /// Gets or sets output frame rate.
    /// </summary>
    public int OutputFrameRate { get; init; }
    /// <summary>
    /// Gets or sets video bitrate kbps.
    /// </summary>
    public int VideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets LAN video bitrate kbps.
    /// </summary>
    public int LanVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets audio bitrate kbps.
    /// </summary>
    public int AudioBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets key frame interval seconds.
    /// </summary>
    public int KeyFrameIntervalSeconds { get; init; }
    /// <summary>
    /// Gets or sets recording container.
    /// </summary>
    public string RecordingContainer { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets recording segment seconds.
    /// </summary>
    public int RecordingSegmentSeconds { get; init; }
    /// <summary>
    /// Gets or sets LAN bind address.
    /// </summary>
    public string LanBindAddress { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets LAN port.
    /// </summary>
    public int LanPort { get; init; }
    /// <summary>
    /// Gets or sets rtsp port.
    /// </summary>
    public int RtspPort { get; init; }
    /// <summary>
    /// Gets or sets viewer limit.
    /// </summary>
    public int ViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets ingest channel capacity.
    /// </summary>
    public int IngestChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets minimum port.
    /// </summary>
    public int MinimumPort { get; init; }
    /// <summary>
    /// Gets or sets maximum port.
    /// </summary>
    public int MaximumPort { get; init; }
    /// <summary>
    /// Gets or sets minimum width.
    /// </summary>
    public int MinimumWidth { get; init; }
    /// <summary>
    /// Gets or sets maximum width.
    /// </summary>
    public int MaximumWidth { get; init; }
    /// <summary>
    /// Gets or sets minimum height.
    /// </summary>
    public int MinimumHeight { get; init; }
    /// <summary>
    /// Gets or sets maximum height.
    /// </summary>
    public int MaximumHeight { get; init; }
    /// <summary>
    /// Gets or sets minimum frame rate.
    /// </summary>
    public int MinimumFrameRate { get; init; }
    /// <summary>
    /// Gets or sets maximum frame rate.
    /// </summary>
    public int MaximumFrameRate { get; init; }
    /// <summary>
    /// Gets or sets minimum video bitrate kbps.
    /// </summary>
    public int MinimumVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets maximum video bitrate kbps.
    /// </summary>
    public int MaximumVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets minimum viewer limit.
    /// </summary>
    public int MinimumViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets maximum viewer limit.
    /// </summary>
    public int MaximumViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets rtsp channel capacity.
    /// </summary>
    public int RtspChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets rtsp client shutdown seconds.
    /// </summary>
    public int RtspClientShutdownSeconds { get; init; }
    /// <summary>
    /// Gets or sets rtsp interleaved frame marker.
    /// </summary>
    public byte RtspInterleavedFrameMarker { get; init; }
    /// <summary>
    /// Gets or sets rtsp length high byte shift.
    /// </summary>
    public int RtspLengthHighByteShift { get; init; }
    /// <summary>
    /// Gets or sets rtsp length low byte mask.
    /// </summary>
    public int RtspLengthLowByteMask { get; init; }
    /// <summary>
    /// Gets or sets encoder pipeline channel capacity.
    /// </summary>
    public int EncoderPipelineChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets encoder pipeline flush interval.
    /// </summary>
    public int EncoderPipelineFlushInterval { get; init; }
    /// <summary>
    /// Gets or sets encoder pipeline shutdown seconds.
    /// </summary>
    public int EncoderPipelineShutdownSeconds { get; init; }
    /// <summary>
    /// Gets or sets success exit code.
    /// </summary>
    public int SuccessExitCode { get; init; }
    /// <summary>
    /// Gets or sets recording remux arguments before input.
    /// </summary>
    public string[] RecordingRemuxArgumentsBeforeInput { get; init; } = [];
    /// <summary>
    /// Gets or sets recording remux arguments after input.
    /// </summary>
    public string[] RecordingRemuxArgumentsAfterInput { get; init; } = [];
}

/// <summary>
/// Represents a publisher runtime policy options.
/// </summary>
public sealed record PublisherRuntimePolicyOptions
{
    /// <summary>
    /// Gets or sets spreadsheet session lifetime hours.
    /// </summary>
    public double SpreadsheetSessionLifetimeHours { get; init; }
    /// <summary>
    /// Gets or sets audio client interface identifier.
    /// </summary>
    public string AudioClientInterfaceId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets audio capture client interface identifier.
    /// </summary>
    public string AudioCaptureClientInterfaceId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets twitch validation interval minutes.
    /// </summary>
    public double TwitchValidationIntervalMinutes { get; init; }
    /// <summary>
    /// Gets or sets twitch refresh safety window minutes.
    /// </summary>
    public double TwitchRefreshSafetyWindowMinutes { get; init; }
    /// <summary>
    /// Gets or sets minimum media source length.
    /// </summary>
    public double MinimumMediaSourceLength { get; init; }
    /// <summary>
    /// Gets or sets word art view width.
    /// </summary>
    public double WordArtViewWidth { get; init; }
    /// <summary>
    /// Gets or sets word art view height.
    /// </summary>
    public double WordArtViewHeight { get; init; }
    /// <summary>
    /// Gets or sets base pixels per millimeter.
    /// </summary>
    public double BasePixelsPerMillimeter { get; init; }
    /// <summary>
    /// Gets or sets default editor viewport width.
    /// </summary>
    public int DefaultEditorViewportWidth { get; init; }
    /// <summary>
    /// Gets or sets audio sample rate.
    /// </summary>
    public int AudioSampleRate { get; init; }
    /// <summary>
    /// Gets or sets maximum video archive entries.
    /// </summary>
    public int MaximumVideoArchiveEntries { get; init; }
    /// <summary>
    /// Gets or sets maximum notification messages.
    /// </summary>
    public int MaximumNotificationMessages { get; init; }
    /// <summary>
    /// Gets or sets installer download attempts.
    /// </summary>
    public int InstallerDownloadAttempts { get; init; }
    /// <summary>
    /// Gets or sets installer move attempts.
    /// </summary>
    public int InstallerMoveAttempts { get; init; }
    /// <summary>
    /// Gets or sets organic protocol version.
    /// </summary>
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets organic security schema version.
    /// </summary>
    public int OrganicSecuritySchemaVersion { get; init; }
    /// <summary>
    /// Gets or sets organic totp period seconds.
    /// </summary>
    public int OrganicTotpPeriodSeconds { get; init; }
    /// <summary>
    /// Gets or sets organic totp alphabet.
    /// </summary>
    public string OrganicTotpAlphabet { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets FFmpeg environment variable.
    /// </summary>
    public string FfmpegEnvironmentVariable { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets twitch endpoints.
    /// </summary>
    public PublisherTwitchEndpointPolicy TwitchEndpoints { get; init; } = new();
    /// <summary>
    /// Gets or sets native interop.
    /// </summary>
    public PublisherNativeInteropPolicy NativeInterop { get; init; } = new();
    /// <summary>
    /// Gets or sets picture studio.
    /// </summary>
    public PublisherPictureStudioPolicy PictureStudio { get; init; } = new();
    /// <summary>
    /// Gets or sets document defaults.
    /// </summary>
    public PublisherDocumentDefaultsPolicy DocumentDefaults { get; init; } = new();
    /// <summary>
    /// Gets or sets media session defaults.
    /// </summary>
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults { get; init; } = new();
    /// <summary>
    /// Gets or sets media conversion presets.
    /// </summary>
    public List<MediaConversionPreset> MediaConversionPresets { get; init; } = [];
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public Dictionary<PublisherRuntimePattern, PublisherRegexPolicy> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets or sets collections.
    /// </summary>
    public Dictionary<PublisherRuntimeCollection, string[]> Collections { get; init; } = [];
}

/// <summary>
/// Represents a publisher runtime policy snapshot.
/// </summary>
public sealed record PublisherRuntimePolicySnapshot
{
    /// <summary>
    /// Gets or sets spreadsheet session lifetime.
    /// </summary>
    public TimeSpan SpreadsheetSessionLifetime { get; init; }
    /// <summary>
    /// Gets or sets audio client interface identifier.
    /// </summary>
    public Guid AudioClientInterfaceId { get; init; }
    /// <summary>
    /// Gets or sets audio capture client interface identifier.
    /// </summary>
    public Guid AudioCaptureClientInterfaceId { get; init; }
    /// <summary>
    /// Gets or sets twitch validation interval.
    /// </summary>
    public TimeSpan TwitchValidationInterval { get; init; }
    /// <summary>
    /// Gets or sets twitch refresh safety window.
    /// </summary>
    public TimeSpan TwitchRefreshSafetyWindow { get; init; }
    /// <summary>
    /// Gets or sets minimum media source length.
    /// </summary>
    public double MinimumMediaSourceLength { get; init; }
    /// <summary>
    /// Gets or sets word art view width.
    /// </summary>
    public double WordArtViewWidth { get; init; }
    /// <summary>
    /// Gets or sets word art view height.
    /// </summary>
    public double WordArtViewHeight { get; init; }
    /// <summary>
    /// Gets or sets base pixels per millimeter.
    /// </summary>
    public double BasePixelsPerMillimeter { get; init; }
    /// <summary>
    /// Gets or sets default editor viewport width.
    /// </summary>
    public int DefaultEditorViewportWidth { get; init; }
    /// <summary>
    /// Gets or sets audio sample rate.
    /// </summary>
    public int AudioSampleRate { get; init; }
    /// <summary>
    /// Gets or sets maximum video archive entries.
    /// </summary>
    public int MaximumVideoArchiveEntries { get; init; }
    /// <summary>
    /// Gets or sets maximum notification messages.
    /// </summary>
    public int MaximumNotificationMessages { get; init; }
    /// <summary>
    /// Gets or sets installer download attempts.
    /// </summary>
    public int InstallerDownloadAttempts { get; init; }
    /// <summary>
    /// Gets or sets installer move attempts.
    /// </summary>
    public int InstallerMoveAttempts { get; init; }
    /// <summary>
    /// Gets or sets organic protocol version.
    /// </summary>
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets organic security schema version.
    /// </summary>
    public int OrganicSecuritySchemaVersion { get; init; }
    /// <summary>
    /// Gets or sets organic totp period seconds.
    /// </summary>
    public int OrganicTotpPeriodSeconds { get; init; }
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public IReadOnlyList<PublisherRuntimePattern> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets or sets collections.
    /// </summary>
    public IReadOnlyList<PublisherRuntimeCollection> Collections { get; init; } = [];
}

/// <summary>
/// Represents a publisher studio configuration document.
/// </summary>
public sealed record PublisherStudioConfigurationDocument
{
    /// <summary>
    /// Gets or sets twitch.
    /// </summary>
    public PublisherTwitchConfiguration Twitch { get; init; } = new();
    /// <summary>
    /// Gets or sets organic plugins.
    /// </summary>
    public OrganicPluginOptions OrganicPlugins { get; init; } = new();
    /// <summary>
    /// Gets or sets publisher studio.
    /// </summary>
    public PublisherStudioConfigurationNode PublisherStudio { get; init; } = new();
}

/// <summary>
/// Represents a publisher runtime value store options.
/// </summary>
public sealed record PublisherRuntimeValueStoreOptions
{
    /// <summary>
    /// Gets or sets panel text patterns.
    /// </summary>
    public PanelTextPatternStoreOptions PanelTextPatterns { get; init; } = new();
}

/// <summary>
/// Represents a publisher studio configuration node.
/// </summary>
public sealed record PublisherStudioConfigurationNode
{
    /// <summary>
    /// Gets or sets FFmpeg path.
    /// </summary>
    public string FFmpegPath { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets paths.
    /// </summary>
    public PublisherStudioPathOptions Paths { get; init; } = new();
    /// <summary>
    /// Gets or sets runtime value stores.
    /// </summary>
    public PublisherRuntimeValueStoreOptions RuntimeValueStores { get; init; } = new();
    /// <summary>
    /// Gets or sets runtime policy.
    /// </summary>
    public PublisherRuntimePolicyOptions RuntimePolicy { get; init; } = new();
}
