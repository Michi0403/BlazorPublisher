using System.Collections.Frozen;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides publisher runtime policy data service operations.
/// </summary>
public sealed class PublisherRuntimePolicyDataService : IPublisherRuntimePolicyDataService
{
    private readonly PublisherRuntimePolicyOptions options;
    private readonly FrozenDictionary<PublisherRuntimeCollection, string[]> collections;
    private readonly ILogger<PublisherRuntimePolicyDataService> logger;

    /// <summary>
    /// Publishes er runtime policy data service.
    /// </summary>
    public PublisherRuntimePolicyDataService(
        PublisherRuntimePolicyOptions options,
        ILogger<PublisherRuntimePolicyDataService> logger)
    {
        try
        {
            this.options = options;
            this.logger = logger;
            collections = options.Collections.ToFrozenDictionary(
                item => item.Key,
                item => item.Value
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray());
            Validate();
            logger.LogInformation($"Loaded the PublisherStudio runtime policy with {collections.Count} configured collections.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not load the PublisherStudio runtime policy.");
            throw;
        }
    }

    /// <summary>
    /// Gets spreadsheet session lifetime.
    /// </summary>
    public TimeSpan SpreadsheetSessionLifetime => TimeSpan.FromHours(options.SpreadsheetSessionLifetimeHours);
    /// <summary>
    /// Gets audio client interface identifier.
    /// </summary>
    public Guid AudioClientInterfaceId => Guid.Parse(options.AudioClientInterfaceId);
    /// <summary>
    /// Gets audio capture client interface identifier.
    /// </summary>
    public Guid AudioCaptureClientInterfaceId => Guid.Parse(options.AudioCaptureClientInterfaceId);
    /// <summary>
    /// Gets twitch validation interval.
    /// </summary>
    public TimeSpan TwitchValidationInterval => TimeSpan.FromMinutes(options.TwitchValidationIntervalMinutes);
    /// <summary>
    /// Gets twitch refresh safety window.
    /// </summary>
    public TimeSpan TwitchRefreshSafetyWindow => TimeSpan.FromMinutes(options.TwitchRefreshSafetyWindowMinutes);
    /// <summary>
    /// Gets minimum media source length.
    /// </summary>
    public double MinimumMediaSourceLength => options.MinimumMediaSourceLength;
    /// <summary>
    /// Gets word art view width.
    /// </summary>
    public double WordArtViewWidth => options.WordArtViewWidth;
    /// <summary>
    /// Gets word art view height.
    /// </summary>
    public double WordArtViewHeight => options.WordArtViewHeight;
    /// <summary>
    /// Gets base pixels per millimeter.
    /// </summary>
    public double BasePixelsPerMillimeter => options.BasePixelsPerMillimeter;
    /// <summary>
    /// Gets default editor viewport width.
    /// </summary>
    public int DefaultEditorViewportWidth => options.DefaultEditorViewportWidth;
    /// <summary>
    /// Gets audio sample rate.
    /// </summary>
    public int AudioSampleRate => options.AudioSampleRate;
    /// <summary>
    /// Gets maximum video archive entries.
    /// </summary>
    public int MaximumVideoArchiveEntries => options.MaximumVideoArchiveEntries;
    /// <summary>
    /// Gets maximum notification messages.
    /// </summary>
    public int MaximumNotificationMessages => options.MaximumNotificationMessages;
    /// <summary>
    /// Gets installer download attempts.
    /// </summary>
    public int InstallerDownloadAttempts => options.InstallerDownloadAttempts;
    /// <summary>
    /// Gets installer move attempts.
    /// </summary>
    public int InstallerMoveAttempts => options.InstallerMoveAttempts;
    /// <summary>
    /// Gets organic protocol version.
    /// </summary>
    public string OrganicProtocolVersion => options.OrganicProtocolVersion;
    /// <summary>
    /// Gets organic security schema version.
    /// </summary>
    public int OrganicSecuritySchemaVersion => options.OrganicSecuritySchemaVersion;
    /// <summary>
    /// Gets organic totp period seconds.
    /// </summary>
    public int OrganicTotpPeriodSeconds => options.OrganicTotpPeriodSeconds;
    /// <summary>
    /// Gets organic totp alphabet.
    /// </summary>
    public string OrganicTotpAlphabet => options.OrganicTotpAlphabet;
    /// <summary>
    /// Gets FFmpeg environment variable.
    /// </summary>
    public string FfmpegEnvironmentVariable => options.FfmpegEnvironmentVariable;
    /// <summary>
    /// Gets twitch endpoints.
    /// </summary>
    public PublisherTwitchEndpointPolicy TwitchEndpoints => options.TwitchEndpoints;
    /// <summary>
    /// Gets native interop.
    /// </summary>
    public PublisherNativeInteropPolicy NativeInterop => options.NativeInterop;
    /// <summary>
    /// Gets picture studio.
    /// </summary>
    public PublisherPictureStudioPolicy PictureStudio => options.PictureStudio;
    /// <summary>
    /// Gets document defaults.
    /// </summary>
    public PublisherDocumentDefaultsPolicy DocumentDefaults => options.DocumentDefaults;
    /// <summary>
    /// Gets media session defaults.
    /// </summary>
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults => options.MediaSessionDefaults;
    /// <summary>
    /// Gets media conversion presets.
    /// </summary>
    public IReadOnlyList<MediaConversionPreset> MediaConversionPresets => options.MediaConversionPresets;

    /// <summary>
    /// Gets collection.
    /// </summary>
    public IReadOnlyList<string> GetCollection(PublisherRuntimeCollection collection)
    {
        try
        {
            if (!collections.TryGetValue(collection, out var values))
               /// <summary>
               /// Runs the key not found exception operation.
               /// </summary>
                throw new KeyNotFoundException($"The runtime collection '{collection}' is not configured.");
            /// <summary>
            /// Runs the log trace operation.
            /// </summary>
            logger.LogTrace($"Resolved PublisherStudio runtime collection '{collection}' with {values.Length} values.");
            return values;
        }
        catch (Exception exception)
        {
            /// <summary>
            /// Runs the log error operation.
            /// </summary>
            logger.LogError(exception, $"Could not resolve PublisherStudio runtime collection '{collection}'.");
            throw;
        }
    }

    /// <summary>
    /// Gets snapshot.
    /// </summary>
    public PublisherRuntimePolicySnapshot GetSnapshot()
    {
        try
        {
            var snapshot = new PublisherRuntimePolicySnapshot
            {
                SpreadsheetSessionLifetime = SpreadsheetSessionLifetime,
                AudioClientInterfaceId = AudioClientInterfaceId,
                AudioCaptureClientInterfaceId = AudioCaptureClientInterfaceId,
                TwitchValidationInterval = TwitchValidationInterval,
                TwitchRefreshSafetyWindow = TwitchRefreshSafetyWindow,
                MinimumMediaSourceLength = MinimumMediaSourceLength,
                WordArtViewWidth = WordArtViewWidth,
                WordArtViewHeight = WordArtViewHeight,
                BasePixelsPerMillimeter = BasePixelsPerMillimeter,
                DefaultEditorViewportWidth = DefaultEditorViewportWidth,
                AudioSampleRate = AudioSampleRate,
                MaximumVideoArchiveEntries = MaximumVideoArchiveEntries,
                MaximumNotificationMessages = MaximumNotificationMessages,
                InstallerDownloadAttempts = InstallerDownloadAttempts,
                InstallerMoveAttempts = InstallerMoveAttempts,
                OrganicProtocolVersion = OrganicProtocolVersion,
                OrganicSecuritySchemaVersion = OrganicSecuritySchemaVersion,
                OrganicTotpPeriodSeconds = OrganicTotpPeriodSeconds,
                RegexPatterns = options.RegexPatterns.Keys.OrderBy(item => item).ToArray(),
                Collections = collections.Keys.OrderBy(item => item).ToArray()
            };
            logger.LogTrace($"Returned the PublisherStudio runtime policy snapshot.");
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the PublisherStudio runtime policy snapshot.");
            throw;
        }
    }

    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    private void Validate()
    {
        try
        {
            if (options.SpreadsheetSessionLifetimeHours <= 0 ||
                options.TwitchValidationIntervalMinutes <= 0 ||
                options.TwitchRefreshSafetyWindowMinutes < 0 ||
                options.MinimumMediaSourceLength <= 0 ||
                options.WordArtViewWidth <= 0 ||
                options.WordArtViewHeight <= 0 ||
                options.MaximumVideoArchiveEntries <= 0 ||
                options.MaximumNotificationMessages <= 0 ||
                options.InstallerDownloadAttempts <= 0 ||
                options.InstallerMoveAttempts <= 0 ||
                string.IsNullOrWhiteSpace(options.OrganicProtocolVersion) ||
                options.OrganicSecuritySchemaVersion <= 0 ||
                options.OrganicTotpPeriodSeconds <= 0 ||
                string.IsNullOrWhiteSpace(options.OrganicTotpAlphabet) ||
                string.IsNullOrWhiteSpace(options.FfmpegEnvironmentVariable) ||
                options.MediaConversionPresets.Count == 0 ||
                string.IsNullOrWhiteSpace(options.TwitchEndpoints.TokenUrl) ||
                string.IsNullOrWhiteSpace(options.NativeInterop.VirtualAudioDeviceProcessLoopback) ||
                string.IsNullOrWhiteSpace(options.DocumentDefaults.PublicationName) ||
                options.DocumentDefaults.PageWidthMillimeters <= 0 ||
                options.DocumentDefaults.PictureWidthPixels <= 0 ||
                options.DocumentDefaults.StoryPageWidthMillimeters <= 0 ||
                options.DocumentDefaults.StoryPageHeightMillimeters <= 0 ||
                options.DocumentDefaults.PagePresets.Count == 0 ||
                options.MediaSessionDefaults.MasterWidth <= 0 ||
                options.MediaSessionDefaults.IngestChannelCapacity <= 0)
                throw new InvalidDataException("The PublisherStudio runtime policy contains invalid numeric, text, endpoint, native, or media-preset values.");

            _ = AudioClientInterfaceId;
            _ = AudioCaptureClientInterfaceId;
            logger.LogTrace($"Validated the PublisherStudio runtime policy.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not validate the PublisherStudio runtime policy.");
            throw;
        }
    }
}
