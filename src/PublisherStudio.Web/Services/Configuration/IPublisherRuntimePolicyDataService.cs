using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the contract for publisher runtime policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublisherRuntimePolicyDataService
{
    /// <summary>
    /// Gets the spreadsheet session lifetime duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The spreadsheet session lifetime value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    TimeSpan SpreadsheetSessionLifetime { get; }
    /// <summary>
    /// Gets the stable audio client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio client interface identifier value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    Guid AudioClientInterfaceId { get; }
    /// <summary>
    /// Gets the stable audio capture client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio capture client interface identifier value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    Guid AudioCaptureClientInterfaceId { get; }
    /// <summary>
    /// Gets the twitch validation interval duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The twitch validation interval value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    TimeSpan TwitchValidationInterval { get; }
    /// <summary>
    /// Gets the twitch refresh safety window duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The twitch refresh safety window value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    TimeSpan TwitchRefreshSafetyWindow { get; }
    /// <summary>
    /// Gets the minimum media source length that quantifies the associated publisher runtime policy data.
    /// </summary>
    /// <value>The minimum media source length value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    double MinimumMediaSourceLength { get; }
    /// <summary>
    /// Gets the word art view width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view width value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    double WordArtViewWidth { get; }
    /// <summary>
    /// Gets the word art view height value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view height value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    double WordArtViewHeight { get; }
    /// <summary>
    /// Gets the base pixels per millimeter value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The base pixels per millimeter value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    double BasePixelsPerMillimeter { get; }
    /// <summary>
    /// Gets the default editor viewport width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default editor viewport width value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int DefaultEditorViewportWidth { get; }
    /// <summary>
    /// Gets the audio sample rate value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio sample rate value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int AudioSampleRate { get; }
    /// <summary>
    /// Gets the maximum video archive entries value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video archive entries value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int MaximumVideoArchiveEntries { get; }
    /// <summary>
    /// Gets the maximum notification messages value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum notification messages value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int MaximumNotificationMessages { get; }
    /// <summary>
    /// Gets the maximum organic payload characters value governed by the persisted operator runtime policy.
    /// </summary>
    /// <value>The maximum organic payload characters value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int MaximumOrganicPayloadCharacters { get; }
    /// <summary>
    /// Gets the installer download attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer download attempts value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int InstallerDownloadAttempts { get; }
    /// <summary>
    /// Gets the installer move attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer move attempts value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int InstallerMoveAttempts { get; }
    /// <summary>
    /// Gets the organic protocol version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic protocol version value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    string OrganicProtocolVersion { get; }
    /// <summary>
    /// Gets the organic security schema version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic security schema version value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int OrganicSecuritySchemaVersion { get; }
    /// <summary>
    /// Gets the organic totp period seconds value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp period seconds value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    int OrganicTotpPeriodSeconds { get; }
    /// <summary>
    /// Gets the organic totp alphabet value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp alphabet value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    string OrganicTotpAlphabet { get; }
    /// <summary>
    /// Gets the FFmpeg environment variable value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The FFmpeg environment variable value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    string FfmpegEnvironmentVariable { get; }
    /// <summary>
    /// Gets the twitch endpoints value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch endpoints value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    PublisherTwitchEndpointPolicy TwitchEndpoints { get; }
    /// <summary>
    /// Gets the native interop value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native interop value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    PublisherNativeInteropPolicy NativeInterop { get; }
    /// <summary>
    /// Gets the picture studio value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture studio value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    PublisherPictureStudioPolicy PictureStudio { get; }
    /// <summary>
    /// Gets the document defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document defaults value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    PublisherDocumentDefaultsPolicy DocumentDefaults { get; }
    /// <summary>
    /// Gets the media session defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media session defaults value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    PublisherMediaSessionDefaultsPolicy MediaSessionDefaults { get; }
    /// <summary>
    /// Gets the media conversion presets collection maintained or exposed by this publisher runtime policy instance for downstream processing.
    /// </summary>
    /// <value>The media conversion presets value exposed by <see cref="IPublisherRuntimePolicyDataService"/>.</value>
    IReadOnlyList<MediaConversionPreset> MediaConversionPresets { get; }
    /// <summary>
    /// Retrieves collection as part of the publisher runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="collection">Collection value supplied to the publisher runtime policy operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> GetCollection(PublisherRuntimeCollection collection);
    /// <summary>
    /// Retrieves snapshot as part of the publisher runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The publisher runtime policy snapshot produced by the operation.</returns>
    PublisherRuntimePolicySnapshot GetSnapshot();
}
