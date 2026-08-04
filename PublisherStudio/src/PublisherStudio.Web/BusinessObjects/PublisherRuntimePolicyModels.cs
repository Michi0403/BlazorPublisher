namespace PublisherStudio.BusinessObjects;

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

public sealed record PublisherRegexPolicy
{
    public string Pattern { get; init; } = string.Empty;
    public string Options { get; init; } = string.Empty;
    public int TimeoutMilliseconds { get; init; }
}

public sealed record PublisherTwitchEndpointPolicy
{
    public string DeviceAuthorizationUrl { get; init; } = string.Empty;
    public string TokenUrl { get; init; } = string.Empty;
    public string ValidateUrl { get; init; } = string.Empty;
    public string RevokeUrl { get; init; } = string.Empty;
    public string StreamKeyUrl { get; init; } = string.Empty;
    public string IngestUrl { get; init; } = string.Empty;
    public string GlobalEndpoint { get; init; } = string.Empty;
}

public sealed record PublisherNativeInteropPolicy
{
    public string VirtualAudioDeviceProcessLoopback { get; init; } = string.Empty;
    public uint AudioClientStreamFlagsLoopback { get; init; }
    public uint AudioClientStreamFlagsEventCallback { get; init; }
    public uint AudioClientStreamFlagsSourceDefaultQuality { get; init; }
    public uint AudioClientStreamFlagsAutoConvertPcm { get; init; }
    public uint AudioClientBufferFlagsSilent { get; init; }
    public ushort VariantBlobType { get; init; }
    public uint WindowMessageHotkey { get; init; }
    public uint WindowMessageCommand { get; init; }
    public uint WindowMessageQuit { get; init; }
    public uint ModifierAlt { get; init; }
    public uint ModifierControl { get; init; }
    public uint ModifierShift { get; init; }
    public uint ModifierWindows { get; init; }
    public uint ModifierNoRepeat { get; init; }
    public int AudioActivationSuccessResult { get; init; }
}

public sealed record PublisherPictureStudioPolicy
{
    public string CanvasId { get; init; } = string.Empty;
    public string CanvasHostId { get; init; } = string.Empty;
    public string StudioRootId { get; init; } = string.Empty;
    public string ImageInputId { get; init; } = string.Empty;
    public string ImageDropInputId { get; init; } = string.Empty;
    public string LayeredInputId { get; init; } = string.Empty;
    public string LayerDropInputId { get; init; } = string.Empty;
    public double MinimumDrawWidth { get; init; }
    public double MaximumDrawWidth { get; init; }
}

public sealed record PublisherTwitchConfiguration
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientIdEnvironmentVariable { get; init; } = string.Empty;
}


public sealed record PublisherDocumentDefaultsPolicy
{
    public string PublicationName { get; init; } = string.Empty;
    public string PublicationFormatVersion { get; init; } = string.Empty;
    public double PublicationZoom { get; init; }
    public string PageName { get; init; } = string.Empty;
    public double PageWidthMillimeters { get; init; }
    public double PageHeightMillimeters { get; init; }
    public string PageBackground { get; init; } = string.Empty;
    public string TitleName { get; init; } = string.Empty;
    public string TitlePreviewHtml { get; init; } = string.Empty;
    public double TitleX { get; init; }
    public double TitleY { get; init; }
    public double TitleWidth { get; init; }
    public double TitleHeight { get; init; }
    public string AccentName { get; init; } = string.Empty;
    public double AccentX { get; init; }
    public double AccentY { get; init; }
    public double AccentWidth { get; init; }
    public double AccentHeight { get; init; }
    public string AccentFill { get; init; } = string.Empty;
    public string AccentStroke { get; init; } = string.Empty;
    public string PictureName { get; init; } = string.Empty;
    public string PictureFormatVersion { get; init; } = string.Empty;
    public int PictureWidthPixels { get; init; }
    public int PictureHeightPixels { get; init; }
    public int PictureMinimumDimension { get; init; }
    public int PictureMaximumDimension { get; init; }
    public string PictureTransparentBackground { get; init; } = string.Empty;
    public string PictureOpaqueBackground { get; init; } = string.Empty;
    public double PictureZoom { get; init; }
    public int PictureGridSpacingPixels { get; init; }
    public double StoryPageWidthMillimeters { get; init; }
    public double StoryPageHeightMillimeters { get; init; }
    public double StoryMarginTopMillimeters { get; init; }
    public double StoryMarginRightMillimeters { get; init; }
    public double StoryMarginBottomMillimeters { get; init; }
    public double StoryMarginLeftMillimeters { get; init; }
    public List<PagePreset> PagePresets { get; init; } = [];
}

public sealed record PublisherMediaSessionDefaultsPolicy
{
    public string PublicationName { get; init; } = string.Empty;
    public string OutputName { get; init; } = string.Empty;
    public bool PreferDeviceTimestamps { get; init; }
    public int HardwareEncoder { get; init; }
    public int OutputProvider { get; init; }
    public int OutputTransport { get; init; }
    public int VideoCodec { get; init; }
    public int AudioCodec { get; init; }
    public int RecordingVariant { get; init; }
    public bool EnableBrowserWebRtc { get; init; }
    public bool EnableHls { get; init; }
    public bool EnableRtsp { get; init; }
    public bool RequireAccessToken { get; init; }
    public string WaitingForRendererStatus { get; init; } = string.Empty;
    public string ReadyStatus { get; init; } = string.Empty;
    public int MasterWidth { get; init; }
    public int MasterHeight { get; init; }
    public int MasterFrameRate { get; init; }
    public int OutputWidth { get; init; }
    public int OutputHeight { get; init; }
    public int OutputFrameRate { get; init; }
    public int VideoBitrateKbps { get; init; }
    public int LanVideoBitrateKbps { get; init; }
    public int AudioBitrateKbps { get; init; }
    public int KeyFrameIntervalSeconds { get; init; }
    public string RecordingContainer { get; init; } = string.Empty;
    public int RecordingSegmentSeconds { get; init; }
    public string LanBindAddress { get; init; } = string.Empty;
    public int LanPort { get; init; }
    public int RtspPort { get; init; }
    public int ViewerLimit { get; init; }
    public int IngestChannelCapacity { get; init; }
    public int MinimumPort { get; init; }
    public int MaximumPort { get; init; }
    public int MinimumWidth { get; init; }
    public int MaximumWidth { get; init; }
    public int MinimumHeight { get; init; }
    public int MaximumHeight { get; init; }
    public int MinimumFrameRate { get; init; }
    public int MaximumFrameRate { get; init; }
    public int MinimumVideoBitrateKbps { get; init; }
    public int MaximumVideoBitrateKbps { get; init; }
    public int MinimumViewerLimit { get; init; }
    public int MaximumViewerLimit { get; init; }
    public int RtspChannelCapacity { get; init; }
    public int RtspClientShutdownSeconds { get; init; }
    public byte RtspInterleavedFrameMarker { get; init; }
    public int RtspLengthHighByteShift { get; init; }
    public int RtspLengthLowByteMask { get; init; }
    public int EncoderPipelineChannelCapacity { get; init; }
    public int EncoderPipelineFlushInterval { get; init; }
    public int EncoderPipelineShutdownSeconds { get; init; }
    public int SuccessExitCode { get; init; }
    public string[] RecordingRemuxArgumentsBeforeInput { get; init; } = [];
    public string[] RecordingRemuxArgumentsAfterInput { get; init; } = [];
}

public sealed record PublisherRuntimePolicyOptions
{
    public double SpreadsheetSessionLifetimeHours { get; init; }
    public string AudioClientInterfaceId { get; init; } = string.Empty;
    public string AudioCaptureClientInterfaceId { get; init; } = string.Empty;
    public double TwitchValidationIntervalMinutes { get; init; }
    public double TwitchRefreshSafetyWindowMinutes { get; init; }
    public double MinimumMediaSourceLength { get; init; }
    public double WordArtViewWidth { get; init; }
    public double WordArtViewHeight { get; init; }
    public double BasePixelsPerMillimeter { get; init; }
    public int DefaultEditorViewportWidth { get; init; }
    public int AudioSampleRate { get; init; }
    public int MaximumVideoArchiveEntries { get; init; }
    public int MaximumNotificationMessages { get; init; }
    public int InstallerDownloadAttempts { get; init; }
    public int InstallerMoveAttempts { get; init; }
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    public int OrganicSecuritySchemaVersion { get; init; }
    public int OrganicTotpPeriodSeconds { get; init; }
    public string OrganicTotpAlphabet { get; init; } = string.Empty;
    public string FfmpegEnvironmentVariable { get; init; } = string.Empty;
    public PublisherTwitchEndpointPolicy TwitchEndpoints { get; init; } = new();
    public PublisherNativeInteropPolicy NativeInterop { get; init; } = new();
    public PublisherPictureStudioPolicy PictureStudio { get; init; } = new();
    public PublisherDocumentDefaultsPolicy DocumentDefaults { get; init; } = new();
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults { get; init; } = new();
    public List<MediaConversionPreset> MediaConversionPresets { get; init; } = [];
    public Dictionary<PublisherRuntimePattern, PublisherRegexPolicy> RegexPatterns { get; init; } = [];
    public Dictionary<PublisherRuntimeCollection, string[]> Collections { get; init; } = [];
}

public sealed record PublisherRuntimePolicySnapshot
{
    public TimeSpan SpreadsheetSessionLifetime { get; init; }
    public Guid AudioClientInterfaceId { get; init; }
    public Guid AudioCaptureClientInterfaceId { get; init; }
    public TimeSpan TwitchValidationInterval { get; init; }
    public TimeSpan TwitchRefreshSafetyWindow { get; init; }
    public double MinimumMediaSourceLength { get; init; }
    public double WordArtViewWidth { get; init; }
    public double WordArtViewHeight { get; init; }
    public double BasePixelsPerMillimeter { get; init; }
    public int DefaultEditorViewportWidth { get; init; }
    public int AudioSampleRate { get; init; }
    public int MaximumVideoArchiveEntries { get; init; }
    public int MaximumNotificationMessages { get; init; }
    public int InstallerDownloadAttempts { get; init; }
    public int InstallerMoveAttempts { get; init; }
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    public int OrganicSecuritySchemaVersion { get; init; }
    public int OrganicTotpPeriodSeconds { get; init; }
    public IReadOnlyList<PublisherRuntimePattern> RegexPatterns { get; init; } = [];
    public IReadOnlyList<PublisherRuntimeCollection> Collections { get; init; } = [];
}

public sealed record PublisherStudioConfigurationDocument
{
    public PublisherTwitchConfiguration Twitch { get; init; } = new();
    public OrganicPluginOptions OrganicPlugins { get; init; } = new();
    public PublisherStudioConfigurationNode PublisherStudio { get; init; } = new();
}

public sealed record PublisherRuntimeValueStoreOptions
{
    public PanelTextPatternStoreOptions PanelTextPatterns { get; init; } = new();
}

public sealed record PublisherStudioConfigurationNode
{
    public string FFmpegPath { get; init; } = string.Empty;
    public PublisherStudioPathOptions Paths { get; init; } = new();
    public PublisherRuntimeValueStoreOptions RuntimeValueStores { get; init; } = new();
    public PublisherRuntimePolicyOptions RuntimePolicy { get; init; } = new();
}
