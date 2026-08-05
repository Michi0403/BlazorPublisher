using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the publisher runtime policy data service contract.
/// </summary>
public interface IPublisherRuntimePolicyDataService
{
    TimeSpan SpreadsheetSessionLifetime { get; }
    Guid AudioClientInterfaceId { get; }
    Guid AudioCaptureClientInterfaceId { get; }
    TimeSpan TwitchValidationInterval { get; }
    TimeSpan TwitchRefreshSafetyWindow { get; }
    double MinimumMediaSourceLength { get; }
    double WordArtViewWidth { get; }
    double WordArtViewHeight { get; }
    double BasePixelsPerMillimeter { get; }
    int DefaultEditorViewportWidth { get; }
    int AudioSampleRate { get; }
    int MaximumVideoArchiveEntries { get; }
    int MaximumNotificationMessages { get; }
    int InstallerDownloadAttempts { get; }
    int InstallerMoveAttempts { get; }
    string OrganicProtocolVersion { get; }
    int OrganicSecuritySchemaVersion { get; }
    int OrganicTotpPeriodSeconds { get; }
    string OrganicTotpAlphabet { get; }
    string FfmpegEnvironmentVariable { get; }
    PublisherTwitchEndpointPolicy TwitchEndpoints { get; }
    PublisherNativeInteropPolicy NativeInterop { get; }
    PublisherPictureStudioPolicy PictureStudio { get; }
    PublisherDocumentDefaultsPolicy DocumentDefaults { get; }
    PublisherMediaSessionDefaultsPolicy MediaSessionDefaults { get; }
    IReadOnlyList<MediaConversionPreset> MediaConversionPresets { get; }
    IReadOnlyList<string> GetCollection(PublisherRuntimeCollection collection);
    PublisherRuntimePolicySnapshot GetSnapshot();
}
