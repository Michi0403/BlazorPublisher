namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported publisher runtime pattern values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported publisher runtime collection values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublisherRuntimeCollection
{
    VideoProjectExtensions,
    NowPlayingExtensions,
    AllowedFfmpegExecutableNames,
    ForbiddenFfmpegAdvancedOptions,
    FfmpegWindowsBundledPaths,
    FfmpegUnixBundledPaths,
    FfmpegUnixInstallPaths,
    FfmpegEncoderPresetSuggestions,
    FfmpegPixelFormatSuggestions
}

/// <summary>
/// Represents a publisher regex policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherRegexPolicy
{
    /// <summary>
    /// Gets or sets the pattern value that forms part of the publisher regex policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pattern value exposed by <see cref="PublisherRegexPolicy"/>.</value>
    public string Pattern { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the options value that forms part of the publisher regex policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The options value exposed by <see cref="PublisherRegexPolicy"/>.</value>
    public string Options { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the timeout milliseconds value that forms part of the publisher regex policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout milliseconds value exposed by <see cref="PublisherRegexPolicy"/>.</value>
    public int TimeoutMilliseconds { get; init; }
}

/// <summary>
/// Represents a publisher twitch endpoint policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherTwitchEndpointPolicy
{
    /// <summary>
    /// Gets or sets the device authorization URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The device authorization URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string DeviceAuthorizationUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the token URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The token URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string TokenUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the validate URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The validate URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string ValidateUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the revoke URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The revoke URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string RevokeUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stream key URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The stream key URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string StreamKeyUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the ingest URL that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The ingest URL value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string IngestUrl { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the global endpoint that identifies the network or application endpoint associated with this publisher twitch endpoint policy state.
    /// </summary>
    /// <value>The global endpoint value exposed by <see cref="PublisherTwitchEndpointPolicy"/>.</value>
    public string GlobalEndpoint { get; init; } = string.Empty;
}

/// <summary>
/// Represents a publisher native interop policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherNativeInteropPolicy
{
    /// <summary>
    /// Gets or sets the virtual audio device process loopback value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The virtual audio device process loopback value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public string VirtualAudioDeviceProcessLoopback { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the audio client stream flags loopback value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio client stream flags loopback value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint AudioClientStreamFlagsLoopback { get; init; }
    /// <summary>
    /// Gets or sets the audio client stream flags event callback value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio client stream flags event callback value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint AudioClientStreamFlagsEventCallback { get; init; }
    /// <summary>
    /// Gets or sets the audio client stream flags source default quality value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio client stream flags source default quality value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint AudioClientStreamFlagsSourceDefaultQuality { get; init; }
    /// <summary>
    /// Gets or sets the audio client stream flags auto convert pcm value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio client stream flags auto convert pcm value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint AudioClientStreamFlagsAutoConvertPcm { get; init; }
    /// <summary>
    /// Gets or sets the audio client buffer flags silent value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio client buffer flags silent value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint AudioClientBufferFlagsSilent { get; init; }
    /// <summary>
    /// Gets or sets the variant blob type value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The variant blob type value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public ushort VariantBlobType { get; init; }
    /// <summary>
    /// Gets or sets the window message hotkey value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window message hotkey value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint WindowMessageHotkey { get; init; }
    /// <summary>
    /// Gets or sets the window message command value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window message command value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint WindowMessageCommand { get; init; }
    /// <summary>
    /// Gets or sets the window message quit value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The window message quit value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint WindowMessageQuit { get; init; }
    /// <summary>
    /// Gets or sets the modifier alt value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The modifier alt value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint ModifierAlt { get; init; }
    /// <summary>
    /// Gets or sets the modifier control value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The modifier control value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint ModifierControl { get; init; }
    /// <summary>
    /// Gets or sets the modifier shift value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The modifier shift value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint ModifierShift { get; init; }
    /// <summary>
    /// Gets or sets the modifier windows value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The modifier windows value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint ModifierWindows { get; init; }
    /// <summary>
    /// Gets or sets the modifier no repeat value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The modifier no repeat value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public uint ModifierNoRepeat { get; init; }
    /// <summary>
    /// Gets or sets the audio activation success result value that forms part of the publisher native interop policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio activation success result value exposed by <see cref="PublisherNativeInteropPolicy"/>.</value>
    public int AudioActivationSuccessResult { get; init; }
}

/// <summary>
/// Represents a publisher picture studio policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherPictureStudioPolicy
{
    /// <summary>
    /// Gets or sets the stable canvas identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The canvas identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string CanvasId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable canvas host identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The canvas host identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string CanvasHostId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable studio root identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The studio root identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string StudioRootId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable image input identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The image input identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string ImageInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable image drop input identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The image drop input identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string ImageDropInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable layered input identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The layered input identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string LayeredInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable layer drop input identifier used to identify or correlate this publisher picture studio policy instance with related application state.
    /// </summary>
    /// <value>The layer drop input identifier value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public string LayerDropInputId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the minimum draw width value that forms part of the publisher picture studio policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum draw width value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public double MinimumDrawWidth { get; init; }
    /// <summary>
    /// Gets or sets the maximum draw width value that forms part of the publisher picture studio policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum draw width value exposed by <see cref="PublisherPictureStudioPolicy"/>.</value>
    public double MaximumDrawWidth { get; init; }
}

/// <summary>
/// Carries the configurable publisher twitch settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed record PublisherTwitchConfiguration
{
    /// <summary>
    /// Gets or sets the stable client identifier used to identify or correlate this publisher twitch instance with related application state.
    /// </summary>
    /// <value>The client identifier value exposed by <see cref="PublisherTwitchConfiguration"/>.</value>
    public string ClientId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the client identifier environment variable value that forms part of the publisher twitch state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The client identifier environment variable value exposed by <see cref="PublisherTwitchConfiguration"/>.</value>
    public string ClientIdEnvironmentVariable { get; init; } = string.Empty;
}


/// <summary>
/// Represents a publisher document defaults policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherDocumentDefaultsPolicy
{
    /// <summary>
    /// Gets or sets the publication name value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication name value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PublicationName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the publication format version value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication format version value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PublicationFormatVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the publication zoom value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication zoom value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double PublicationZoom { get; init; }
    /// <summary>
    /// Gets or sets the page name value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page name value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PageName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the page width millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page width millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double PageWidthMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the page height millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page height millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double PageHeightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the page background value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The page background value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PageBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the title name value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title name value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string TitleName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the title preview HTML value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title preview HTML value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string TitlePreviewHtml { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the title x value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title x value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double TitleX { get; init; }
    /// <summary>
    /// Gets or sets the title y value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title y value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double TitleY { get; init; }
    /// <summary>
    /// Gets or sets the title width value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title width value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double TitleWidth { get; init; }
    /// <summary>
    /// Gets or sets the title height value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title height value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double TitleHeight { get; init; }
    /// <summary>
    /// Gets or sets the accent name value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent name value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string AccentName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the accent x value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent x value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double AccentX { get; init; }
    /// <summary>
    /// Gets or sets the accent y value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent y value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double AccentY { get; init; }
    /// <summary>
    /// Gets or sets the accent width value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent width value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double AccentWidth { get; init; }
    /// <summary>
    /// Gets or sets the accent height value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent height value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double AccentHeight { get; init; }
    /// <summary>
    /// Gets or sets the accent fill value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent fill value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string AccentFill { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the accent stroke value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accent stroke value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string AccentStroke { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the picture name value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture name value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PictureName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the picture format version value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture format version value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PictureFormatVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the picture width pixels value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture width pixels value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public int PictureWidthPixels { get; init; }
    /// <summary>
    /// Gets or sets the picture height pixels value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture height pixels value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public int PictureHeightPixels { get; init; }
    /// <summary>
    /// Gets or sets the picture minimum dimension value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture minimum dimension value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public int PictureMinimumDimension { get; init; }
    /// <summary>
    /// Gets or sets the picture maximum dimension value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture maximum dimension value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public int PictureMaximumDimension { get; init; }
    /// <summary>
    /// Gets or sets the picture transparent background value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture transparent background value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PictureTransparentBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the picture opaque background value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture opaque background value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public string PictureOpaqueBackground { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the picture zoom value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture zoom value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double PictureZoom { get; init; }
    /// <summary>
    /// Gets or sets the picture grid spacing pixels value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture grid spacing pixels value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public int PictureGridSpacingPixels { get; init; }
    /// <summary>
    /// Gets or sets the story page width millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story page width millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryPageWidthMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the story page height millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story page height millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryPageHeightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the story margin top millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story margin top millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryMarginTopMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the story margin right millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story margin right millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryMarginRightMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the story margin bottom millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story margin bottom millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryMarginBottomMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the story margin left millimeters value that forms part of the publisher document defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story margin left millimeters value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public double StoryMarginLeftMillimeters { get; init; }
    /// <summary>
    /// Gets or sets the page presets collection maintained or exposed by this publisher document defaults policy instance for downstream processing.
    /// </summary>
    /// <value>The page presets value exposed by <see cref="PublisherDocumentDefaultsPolicy"/>.</value>
    public List<PagePreset> PagePresets { get; init; } = [];
}

/// <summary>
/// Represents a publisher media session defaults policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherStreamQualityTierPolicy
{
    /// <summary>Gets or sets the publication quality preset name represented by this policy tier.</summary>
    /// <value>The preset value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public string Preset { get; init; } = string.Empty;
    /// <summary>Gets or sets the preferred width for this provider quality tier.</summary>
    /// <value>The width value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int Width { get; init; }
    /// <summary>Gets or sets the preferred height for this provider quality tier.</summary>
    /// <value>The height value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int Height { get; init; }
    /// <summary>Gets or sets the preferred frame rate for this provider quality tier.</summary>
    /// <value>The frame rate value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int FrameRate { get; init; }
    /// <summary>Gets or sets the maximum video bitrate in kilobits per second for this provider quality tier.</summary>
    /// <value>The maximum video bitrate kbps value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int MaximumVideoBitrateKbps { get; init; }
    /// <summary>Gets or sets the preferred audio bitrate in kilobits per second for this provider quality tier.</summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int AudioBitrateKbps { get; init; }
    /// <summary>Gets or sets the preferred key-frame interval in seconds for this provider quality tier.</summary>
    /// <value>The key frame interval seconds value exposed by <see cref="PublisherStreamQualityTierPolicy"/>.</value>
    public int KeyFrameIntervalSeconds { get; init; }
}

/// <summary>Describes configurable provider quality knowledge used by PublisherStudio's adaptive media advisor.</summary>
public sealed record PublisherStreamProviderQualityPolicy
{
    /// <summary>
    /// Gets or sets the provider value that forms part of the publisher stream provider quality policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="PublisherStreamProviderQualityPolicy"/>.</value>
    public string Provider { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the tiers collection maintained or exposed by this publisher stream provider quality policy instance for downstream processing.
    /// </summary>
    /// <value>The tiers value exposed by <see cref="PublisherStreamProviderQualityPolicy"/>.</value>
    public List<PublisherStreamQualityTierPolicy> Tiers { get; init; } = [];
}

/// <summary>Configures PublisherStudio's resolution-, frame-rate-, codec-, and audio-aware adaptive media quality advisor.</summary>
public sealed record PublisherMediaQualityAdaptationPolicy
{
    /// <summary>Gets or sets whether smart adaptive quality is enabled by default.</summary>
    /// <value>The enabled value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// Gets or sets the default profile value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default profile value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public string DefaultProfile { get; init; } = "Quality";
    /// <summary>Gets or sets whether native source resolution should be preserved by default.</summary>
    /// <value>The preserve native resolution value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool PreserveNativeResolution { get; init; } = true;
    /// <summary>Gets or sets whether the browser may reduce frame rate when its capability probe reports non-smooth encoding.</summary>
    /// <value>The allow frame rate reduction value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool AllowFrameRateReduction { get; init; } = true;
    /// <summary>Gets or sets whether the browser may reduce resolution as a last-resort smoothness fallback.</summary>
    /// <value>The allow resolution reduction value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool AllowResolutionReduction { get; init; }
    /// <summary>Gets or sets whether browser capability selection should prefer encoders reported as power efficient.</summary>
    /// <value>The prefer power efficient codec value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool PreferPowerEfficientCodec { get; init; } = true;
    /// <summary>Gets or sets whether browser capability selection should prefer encoders reported as smooth.</summary>
    /// <value>The prefer smooth encoding value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool PreferSmoothEncoding { get; init; } = true;
    /// <summary>Gets or sets whether browser MediaCapabilities probing participates in automatic recording codec and smoothness selection.</summary>
    /// <value>The browser capability probe enabled value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public bool BrowserCapabilityProbeEnabled { get; init; } = true;
    /// <summary>Gets or sets the configurable codec-family tie-break order used after browser support and smoothness/power-efficiency evidence.</summary>
    /// <value>The browser codec preference order value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public List<string> BrowserCodecPreferenceOrder { get; init; } = ["vp9", "vp8"];
    /// <summary>Gets or sets the bits-per-pixel-per-frame target for detailed screen content.</summary>
    /// <value>The screen bits per pixel value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double ScreenBitsPerPixel { get; init; }
    /// <summary>Gets or sets the bits-per-pixel-per-frame target for camera/motion content.</summary>
    /// <value>The camera bits per pixel value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double CameraBitsPerPixel { get; init; }
    /// <summary>Gets or sets the bits-per-pixel-per-frame target for mixed publication program content.</summary>
    /// <value>The mixed bits per pixel value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double MixedBitsPerPixel { get; init; }
    /// <summary>Gets or sets the bits-per-pixel-per-frame target used for provider output recommendations.</summary>
    /// <value>The provider bits per pixel value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double ProviderBitsPerPixel { get; init; }
    /// <summary>Gets or sets the bits-per-pixel-per-frame target used for LAN output recommendations.</summary>
    /// <value>The LAN bits per pixel value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double LanBitsPerPixel { get; init; }
    /// <summary>
    /// Gets or sets the efficiency multiplier value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The efficiency multiplier value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double EfficiencyMultiplier { get; init; }
    /// <summary>
    /// Gets or sets the balanced multiplier value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The balanced multiplier value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double BalancedMultiplier { get; init; }
    /// <summary>
    /// Gets or sets the quality multiplier value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality multiplier value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double QualityMultiplier { get; init; }
    /// <summary>Gets or sets the VP9 bitrate-efficiency factor relative to VP8.</summary>
    /// <value>The vp9 bitrate factor value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double Vp9BitrateFactor { get; init; }
    /// <summary>
    /// Gets or sets the vp8 bitrate factor value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vp8 bitrate factor value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double Vp8BitrateFactor { get; init; }
    /// <summary>
    /// Gets or sets the h264 bitrate factor value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The h264 bitrate factor value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double H264BitrateFactor { get; init; }
    /// <summary>
    /// Gets or sets the hevc bitrate factor value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hevc bitrate factor value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double HevcBitrateFactor { get; init; }
    /// <summary>
    /// Gets or sets the av1 bitrate factor value that forms part of the publisher media quality adaptation policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The av1 bitrate factor value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double Av1BitrateFactor { get; init; }
    /// <summary>Gets or sets the minimum adaptive audio bitrate in kilobits per second.</summary>
    /// <value>The minimum audio bitrate kbps value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MinimumAudioBitrateKbps { get; init; }
    /// <summary>Gets or sets the adaptive audio bitrate contribution per channel in kilobits per second.</summary>
    /// <value>The audio bitrate per channel kbps value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int AudioBitratePerChannelKbps { get; init; }
    /// <summary>Gets or sets the maximum adaptive audio bitrate in kilobits per second.</summary>
    /// <value>The maximum audio bitrate kbps value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MaximumAudioBitrateKbps { get; init; }
    /// <summary>Gets or sets the default channel count used when a browser cannot report audio track settings.</summary>
    /// <value>The default audio channels value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int DefaultAudioChannels { get; init; }
    /// <summary>Gets or sets the maximum frame rate selected automatically; zero keeps the source-reported rate up to the global policy maximum.</summary>
    /// <value>The maximum automatic frame rate value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MaximumAutomaticFrameRate { get; init; }
    /// <summary>Gets or sets the multiplicative frame-rate fallback applied when browser capability probing reports non-smooth encoding.</summary>
    /// <value>The frame rate fallback ratio value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double FrameRateFallbackRatio { get; init; }
    /// <summary>Gets or sets the multiplicative resolution fallback applied only when last-resort resolution adaptation is allowed.</summary>
    /// <value>The resolution fallback ratio value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double ResolutionFallbackRatio { get; init; }
    /// <summary>Gets or sets the maximum number of browser smoothness fallback attempts.</summary>
    /// <value>The maximum adaptation attempts value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MaximumAdaptationAttempts { get; init; }
    /// <summary>Gets or sets the configurable bitrate headroom applied to browser ingest that will be transcoded again downstream.</summary>
    /// <value>The ingest headroom multiplier value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public double IngestHeadroomMultiplier { get; init; }
    /// <summary>Gets or sets the browser MediaRecorder chunk interval in milliseconds used by adaptive streaming ingest.</summary>
    /// <value>The recorder chunk milliseconds value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int RecorderChunkMilliseconds { get; init; }
    /// <summary>Gets or sets the maximum pixel count used for generated recording poster thumbnails; zero keeps source-sized poster analysis.</summary>
    /// <value>The metadata poster maximum pixels value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MetadataPosterMaximumPixels { get; init; }
    /// <summary>Gets or sets the delay before nonessential recording metadata/poster analysis begins after capture resources have been released.</summary>
    /// <value>The metadata analysis delay milliseconds value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int MetadataAnalysisDelayMilliseconds { get; init; }
    /// <summary>Gets or sets the LAN maximum width selected automatically; zero uses the global media maximum.</summary>
    /// <value>The LAN maximum width value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int LanMaximumWidth { get; init; }
    /// <summary>Gets or sets the LAN maximum height selected automatically; zero uses the global media maximum.</summary>
    /// <value>The LAN maximum height value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int LanMaximumHeight { get; init; }
    /// <summary>Gets or sets the LAN maximum frame rate selected automatically; zero uses the global media maximum.</summary>
    /// <value>The LAN maximum frame rate value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public int LanMaximumFrameRate { get; init; }
    /// <summary>Gets or sets provider-specific quality knowledge. A provider named Default is used as the fallback.</summary>
    /// <value>The provider profiles value exposed by <see cref="PublisherMediaQualityAdaptationPolicy"/>.</value>
    public List<PublisherStreamProviderQualityPolicy> ProviderProfiles { get; init; } = [];
}

/// <summary>
/// Represents a publisher media session defaults policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherMediaSessionDefaultsPolicy
{
    /// <summary>
    /// Gets or sets the publication name value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication name value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string PublicationName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the output name value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output name value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string OutputName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether prefer device timestamps applies to the publisher media session defaults policy state.
    /// </summary>
    /// <value>The prefer device timestamps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public bool PreferDeviceTimestamps { get; init; }
    /// <summary>
    /// Gets or sets the hardware encoder value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware encoder value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int HardwareEncoder { get; init; }
    /// <summary>
    /// Gets or sets the output provider value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output provider value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int OutputProvider { get; init; }
    /// <summary>
    /// Gets or sets the output transport value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output transport value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int OutputTransport { get; init; }
    /// <summary>
    /// Gets or sets the video codec value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video codec value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int VideoCodec { get; init; }
    /// <summary>
    /// Gets or sets the audio codec value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio codec value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int AudioCodec { get; init; }
    /// <summary>
    /// Gets or sets the recording variant value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording variant value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RecordingVariant { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether browser web rtc applies to the publisher media session defaults policy state.
    /// </summary>
    /// <value>The enable browser web rtc value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public bool EnableBrowserWebRtc { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether hls applies to the publisher media session defaults policy state.
    /// </summary>
    /// <value>The enable hls value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public bool EnableHls { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether rtsp applies to the publisher media session defaults policy state.
    /// </summary>
    /// <value>The enable rtsp value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public bool EnableRtsp { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether access token applies to the publisher media session defaults policy state.
    /// </summary>
    /// <value>The require access token value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public bool RequireAccessToken { get; init; }
    /// <summary>
    /// Gets or sets the waiting for renderer status value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The waiting for renderer status value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string WaitingForRendererStatus { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the ready status value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ready status value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string ReadyStatus { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the master width value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master width value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MasterWidth { get; init; }
    /// <summary>
    /// Gets or sets the master height value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master height value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MasterHeight { get; init; }
    /// <summary>
    /// Gets or sets the master frame rate value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master frame rate value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MasterFrameRate { get; init; }
    /// <summary>
    /// Gets or sets the output width value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output width value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int OutputWidth { get; init; }
    /// <summary>
    /// Gets or sets the output height value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output height value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int OutputHeight { get; init; }
    /// <summary>
    /// Gets or sets the output frame rate value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output frame rate value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int OutputFrameRate { get; init; }
    /// <summary>
    /// Gets or sets the video bitrate kbps value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video bitrate kbps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int VideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the browser Media Studio recording video bitrate in kilobits per second.
    /// </summary>
    /// <value>The bitrate target passed to browser MediaRecorder video recordings.</value>
    public int BrowserRecordingVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the browser Media Studio recording audio bitrate in kilobits per second.
    /// </summary>
    /// <value>The bitrate target passed to browser MediaRecorder audio tracks.</value>
    public int BrowserRecordingAudioBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the preferred browser Media Studio video codec family.
    /// </summary>
    /// <value><c>auto</c>, <c>vp9</c>, or <c>vp8</c>; unsupported choices fall back through the browser capability probe.</value>
    public string BrowserRecordingCodecPreference { get; init; } = "auto";
    /// <summary>Gets or sets the adaptive media-quality knowledge and selection policy.</summary>
    /// <value>The configurable adaptive media-quality policy shared by recording and streaming workflows.</value>
    public PublisherMediaQualityAdaptationPolicy AdaptiveQuality { get; init; } = new();
    /// <summary>
    /// Gets or sets the LAN video bitrate kbps value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN video bitrate kbps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int LanVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the audio bitrate kbps value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int AudioBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the key frame interval seconds value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key frame interval seconds value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int KeyFrameIntervalSeconds { get; init; }
    /// <summary>
    /// Gets or sets the recording container value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording container value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string RecordingContainer { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the recording segment seconds value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording segment seconds value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RecordingSegmentSeconds { get; init; }
    /// <summary>
    /// Gets or sets the LAN bind address that identifies the network or application endpoint associated with this publisher media session defaults policy state.
    /// </summary>
    /// <value>The LAN bind address value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string LanBindAddress { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the LAN port value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN port value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int LanPort { get; init; }
    /// <summary>
    /// Gets or sets the rtsp port value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp port value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RtspPort { get; init; }
    /// <summary>
    /// Gets or sets the viewer limit value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The viewer limit value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int ViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets the ingest channel capacity value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest channel capacity value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int IngestChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets the minimum port value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum port value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumPort { get; init; }
    /// <summary>
    /// Gets or sets the maximum port value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum port value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumPort { get; init; }
    /// <summary>
    /// Gets or sets the minimum width value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum width value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumWidth { get; init; }
    /// <summary>
    /// Gets or sets the maximum width value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum width value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumWidth { get; init; }
    /// <summary>
    /// Gets or sets the minimum height value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum height value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumHeight { get; init; }
    /// <summary>
    /// Gets or sets the maximum height value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum height value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumHeight { get; init; }
    /// <summary>
    /// Gets or sets the minimum frame rate value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum frame rate value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumFrameRate { get; init; }
    /// <summary>
    /// Gets or sets the maximum frame rate value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum frame rate value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumFrameRate { get; init; }
    /// <summary>
    /// Gets or sets the minimum video bitrate kbps value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum video bitrate kbps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the maximum video bitrate kbps value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video bitrate kbps value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumVideoBitrateKbps { get; init; }
    /// <summary>
    /// Gets or sets the minimum viewer limit value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum viewer limit value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MinimumViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets the maximum viewer limit value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum viewer limit value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int MaximumViewerLimit { get; init; }
    /// <summary>
    /// Gets or sets the rtsp channel capacity value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp channel capacity value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RtspChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets the rtsp client shutdown seconds value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp client shutdown seconds value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RtspClientShutdownSeconds { get; init; }
    /// <summary>
    /// Gets or sets the rtsp interleaved frame marker value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp interleaved frame marker value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public byte RtspInterleavedFrameMarker { get; init; }
    /// <summary>
    /// Gets or sets the rtsp length high byte shift value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp length high byte shift value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RtspLengthHighByteShift { get; init; }
    /// <summary>
    /// Gets or sets the rtsp length low byte mask value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp length low byte mask value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int RtspLengthLowByteMask { get; init; }
    /// <summary>
    /// Gets or sets the encoder pipeline channel capacity value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encoder pipeline channel capacity value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int EncoderPipelineChannelCapacity { get; init; }
    /// <summary>
    /// Gets or sets the encoder pipeline flush interval duration used to control timing in the publisher media session defaults policy workflow.
    /// </summary>
    /// <value>The encoder pipeline flush interval value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int EncoderPipelineFlushInterval { get; init; }
    /// <summary>
    /// Gets or sets the encoder pipeline shutdown seconds value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encoder pipeline shutdown seconds value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int EncoderPipelineShutdownSeconds { get; init; }
    /// <summary>
    /// Gets or sets the success exit code value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The success exit code value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public int SuccessExitCode { get; init; }
    /// <summary>
    /// Gets or sets the recording remux arguments before input value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording remux arguments before input value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string[] RecordingRemuxArgumentsBeforeInput { get; init; } = [];
    /// <summary>
    /// Gets or sets the recording remux arguments after input value that forms part of the publisher media session defaults policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording remux arguments after input value exposed by <see cref="PublisherMediaSessionDefaultsPolicy"/>.</value>
    public string[] RecordingRemuxArgumentsAfterInput { get; init; } = [];
}

/// <summary>
/// Carries the configurable publisher runtime policy settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed record PublisherRuntimePolicyOptions
{
    /// <summary>
    /// Gets or sets the spreadsheet session lifetime hours value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The spreadsheet session lifetime hours value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double SpreadsheetSessionLifetimeHours { get; init; }
    /// <summary>
    /// Gets or sets the stable audio client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio client interface identifier value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public string AudioClientInterfaceId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable audio capture client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio capture client interface identifier value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public string AudioCaptureClientInterfaceId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the twitch validation interval minutes value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch validation interval minutes value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double TwitchValidationIntervalMinutes { get; init; }
    /// <summary>
    /// Gets or sets the twitch refresh safety window minutes value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch refresh safety window minutes value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double TwitchRefreshSafetyWindowMinutes { get; init; }
    /// <summary>
    /// Gets or sets the minimum media source length that quantifies the associated publisher runtime policy data.
    /// </summary>
    /// <value>The minimum media source length value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double MinimumMediaSourceLength { get; init; }
    /// <summary>
    /// Gets or sets the word art view width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view width value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double WordArtViewWidth { get; init; }
    /// <summary>
    /// Gets or sets the word art view height value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view height value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double WordArtViewHeight { get; init; }
    /// <summary>
    /// Gets or sets the base pixels per millimeter value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The base pixels per millimeter value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public double BasePixelsPerMillimeter { get; init; }
    /// <summary>
    /// Gets or sets the default editor viewport width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default editor viewport width value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int DefaultEditorViewportWidth { get; init; }
    /// <summary>
    /// Gets or sets the audio sample rate value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio sample rate value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int AudioSampleRate { get; init; }
    /// <summary>
    /// Gets or sets the maximum video archive entries value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video archive entries value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int MaximumVideoArchiveEntries { get; init; }
    /// <summary>
    /// Gets or sets the maximum notification messages value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum notification messages value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int MaximumNotificationMessages { get; init; }
    /// <summary>
    /// Gets or sets the installer download attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer download attempts value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int InstallerDownloadAttempts { get; init; }
    /// <summary>
    /// Gets or sets the installer move attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer move attempts value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int InstallerMoveAttempts { get; init; }
    /// <summary>
    /// Gets or sets the organic protocol version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic protocol version value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the organic security schema version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic security schema version value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int OrganicSecuritySchemaVersion { get; init; }
    /// <summary>
    /// Gets or sets the organic totp period seconds value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp period seconds value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public int OrganicTotpPeriodSeconds { get; init; }
    /// <summary>
    /// Gets or sets the organic totp alphabet value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp alphabet value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public string OrganicTotpAlphabet { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the FFmpeg environment variable value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The FFmpeg environment variable value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public string FfmpegEnvironmentVariable { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the twitch endpoints value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch endpoints value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public PublisherTwitchEndpointPolicy TwitchEndpoints { get; init; } = new();
    /// <summary>
    /// Gets or sets the native interop value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native interop value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public PublisherNativeInteropPolicy NativeInterop { get; init; } = new();
    /// <summary>
    /// Gets or sets the picture studio value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture studio value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public PublisherPictureStudioPolicy PictureStudio { get; init; } = new();
    /// <summary>
    /// Gets or sets the document defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document defaults value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public PublisherDocumentDefaultsPolicy DocumentDefaults { get; init; } = new();
    /// <summary>
    /// Gets or sets the media session defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media session defaults value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults { get; init; } = new();
    /// <summary>
    /// Gets or sets the media conversion presets collection maintained or exposed by this publisher runtime policy instance for downstream processing.
    /// </summary>
    /// <value>The media conversion presets value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public List<MediaConversionPreset> MediaConversionPresets { get; init; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this publisher runtime policy instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public Dictionary<PublisherRuntimePattern, PublisherRegexPolicy> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this publisher runtime policy instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="PublisherRuntimePolicyOptions"/>.</value>
    public Dictionary<PublisherRuntimeCollection, string[]> Collections { get; init; } = [];
}

/// <summary>
/// Represents a publisher runtime policy snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherRuntimePolicySnapshot
{
    /// <summary>
    /// Gets or sets the spreadsheet session lifetime duration used to control timing in the publisher runtime policy snapshot workflow.
    /// </summary>
    /// <value>The spreadsheet session lifetime value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public TimeSpan SpreadsheetSessionLifetime { get; init; }
    /// <summary>
    /// Gets or sets the stable audio client interface identifier used to identify or correlate this publisher runtime policy snapshot instance with related application state.
    /// </summary>
    /// <value>The audio client interface identifier value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public Guid AudioClientInterfaceId { get; init; }
    /// <summary>
    /// Gets or sets the stable audio capture client interface identifier used to identify or correlate this publisher runtime policy snapshot instance with related application state.
    /// </summary>
    /// <value>The audio capture client interface identifier value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public Guid AudioCaptureClientInterfaceId { get; init; }
    /// <summary>
    /// Gets or sets the twitch validation interval duration used to control timing in the publisher runtime policy snapshot workflow.
    /// </summary>
    /// <value>The twitch validation interval value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public TimeSpan TwitchValidationInterval { get; init; }
    /// <summary>
    /// Gets or sets the twitch refresh safety window duration used to control timing in the publisher runtime policy snapshot workflow.
    /// </summary>
    /// <value>The twitch refresh safety window value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public TimeSpan TwitchRefreshSafetyWindow { get; init; }
    /// <summary>
    /// Gets or sets the minimum media source length that quantifies the associated publisher runtime policy snapshot data.
    /// </summary>
    /// <value>The minimum media source length value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public double MinimumMediaSourceLength { get; init; }
    /// <summary>
    /// Gets or sets the word art view width value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view width value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public double WordArtViewWidth { get; init; }
    /// <summary>
    /// Gets or sets the word art view height value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view height value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public double WordArtViewHeight { get; init; }
    /// <summary>
    /// Gets or sets the base pixels per millimeter value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The base pixels per millimeter value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public double BasePixelsPerMillimeter { get; init; }
    /// <summary>
    /// Gets or sets the default editor viewport width value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default editor viewport width value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int DefaultEditorViewportWidth { get; init; }
    /// <summary>
    /// Gets or sets the audio sample rate value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio sample rate value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int AudioSampleRate { get; init; }
    /// <summary>
    /// Gets or sets the maximum video archive entries value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video archive entries value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int MaximumVideoArchiveEntries { get; init; }
    /// <summary>
    /// Gets or sets the maximum notification messages value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum notification messages value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int MaximumNotificationMessages { get; init; }
    /// <summary>
    /// Gets or sets the installer download attempts value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer download attempts value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int InstallerDownloadAttempts { get; init; }
    /// <summary>
    /// Gets or sets the installer move attempts value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer move attempts value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int InstallerMoveAttempts { get; init; }
    /// <summary>
    /// Gets or sets the organic protocol version value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic protocol version value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public string OrganicProtocolVersion { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the organic security schema version value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic security schema version value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int OrganicSecuritySchemaVersion { get; init; }
    /// <summary>
    /// Gets or sets the organic totp period seconds value that forms part of the publisher runtime policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp period seconds value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public int OrganicTotpPeriodSeconds { get; init; }
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this publisher runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public IReadOnlyList<PublisherRuntimePattern> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this publisher runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="PublisherRuntimePolicySnapshot"/>.</value>
    public IReadOnlyList<PublisherRuntimeCollection> Collections { get; init; } = [];
}

/// <summary>
/// Represents PublisherStudio configuration state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed record PublisherStudioConfigurationDocument
{
    /// <summary>
    /// Gets or sets the twitch value that forms part of the PublisherStudio configuration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch value exposed by <see cref="PublisherStudioConfigurationDocument"/>.</value>
    public PublisherTwitchConfiguration Twitch { get; init; } = new();
    /// <summary>
    /// Gets or sets the organic plugins value that forms part of the PublisherStudio configuration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic plugins value exposed by <see cref="PublisherStudioConfigurationDocument"/>.</value>
    public OrganicPluginOptions OrganicPlugins { get; init; } = new();
    /// <summary>
    /// Gets or sets the PublisherStudio value that forms part of the PublisherStudio configuration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The PublisherStudio value exposed by <see cref="PublisherStudioConfigurationDocument"/>.</value>
    public PublisherStudioConfigurationNode PublisherStudio { get; init; } = new();
}

/// <summary>
/// Carries the configurable publisher runtime value store settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed record PublisherRuntimeValueStoreOptions
{
    /// <summary>
    /// Gets or sets the panel text patterns value that forms part of the publisher runtime value store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The panel text patterns value exposed by <see cref="PublisherRuntimeValueStoreOptions"/>.</value>
    public PanelTextPatternStoreOptions PanelTextPatterns { get; init; } = new();
}

/// <summary>
/// Represents a PublisherStudio configuration node application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record PublisherStudioConfigurationNode
{
    /// <summary>
    /// Gets or sets the f fmpeg path used by this PublisherStudio configuration node instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The f fmpeg path value exposed by <see cref="PublisherStudioConfigurationNode"/>.</value>
    public string FFmpegPath { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the paths used by this PublisherStudio configuration node instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The paths value exposed by <see cref="PublisherStudioConfigurationNode"/>.</value>
    public PublisherStudioPathOptions Paths { get; init; } = new();
    /// <summary>
    /// Gets or sets the runtime value stores value that forms part of the PublisherStudio configuration node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime value stores value exposed by <see cref="PublisherStudioConfigurationNode"/>.</value>
    public PublisherRuntimeValueStoreOptions RuntimeValueStores { get; init; } = new();
    /// <summary>
    /// Gets or sets the runtime policy value that forms part of the PublisherStudio configuration node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime policy value exposed by <see cref="PublisherStudioConfigurationNode"/>.</value>
    public PublisherRuntimePolicyOptions RuntimePolicy { get; init; } = new();
}
