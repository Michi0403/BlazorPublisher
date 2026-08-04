using System.Collections.Frozen;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

public sealed class PublisherRuntimePolicyDataService : IPublisherRuntimePolicyDataService
{
    private readonly PublisherRuntimePolicyOptions options;
    private readonly FrozenDictionary<PublisherRuntimeCollection, string[]> collections;
    private readonly ILogger<PublisherRuntimePolicyDataService> logger;

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

    public TimeSpan SpreadsheetSessionLifetime => TimeSpan.FromHours(options.SpreadsheetSessionLifetimeHours);
    public Guid AudioClientInterfaceId => Guid.Parse(options.AudioClientInterfaceId);
    public Guid AudioCaptureClientInterfaceId => Guid.Parse(options.AudioCaptureClientInterfaceId);
    public TimeSpan TwitchValidationInterval => TimeSpan.FromMinutes(options.TwitchValidationIntervalMinutes);
    public TimeSpan TwitchRefreshSafetyWindow => TimeSpan.FromMinutes(options.TwitchRefreshSafetyWindowMinutes);
    public double MinimumMediaSourceLength => options.MinimumMediaSourceLength;
    public double WordArtViewWidth => options.WordArtViewWidth;
    public double WordArtViewHeight => options.WordArtViewHeight;
    public double BasePixelsPerMillimeter => options.BasePixelsPerMillimeter;
    public int DefaultEditorViewportWidth => options.DefaultEditorViewportWidth;
    public int AudioSampleRate => options.AudioSampleRate;
    public int MaximumVideoArchiveEntries => options.MaximumVideoArchiveEntries;
    public int MaximumNotificationMessages => options.MaximumNotificationMessages;
    public int InstallerDownloadAttempts => options.InstallerDownloadAttempts;
    public int InstallerMoveAttempts => options.InstallerMoveAttempts;
    public string OrganicProtocolVersion => options.OrganicProtocolVersion;
    public int OrganicSecuritySchemaVersion => options.OrganicSecuritySchemaVersion;
    public int OrganicTotpPeriodSeconds => options.OrganicTotpPeriodSeconds;
    public string OrganicTotpAlphabet => options.OrganicTotpAlphabet;
    public string FfmpegEnvironmentVariable => options.FfmpegEnvironmentVariable;
    public PublisherTwitchEndpointPolicy TwitchEndpoints => options.TwitchEndpoints;
    public PublisherNativeInteropPolicy NativeInterop => options.NativeInterop;
    public PublisherPictureStudioPolicy PictureStudio => options.PictureStudio;
    public PublisherDocumentDefaultsPolicy DocumentDefaults => options.DocumentDefaults;
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults => options.MediaSessionDefaults;
    public IReadOnlyList<MediaConversionPreset> MediaConversionPresets => options.MediaConversionPresets;

    public IReadOnlyList<string> GetCollection(PublisherRuntimeCollection collection)
    {
        try
        {
            if (!collections.TryGetValue(collection, out var values))
                throw new KeyNotFoundException($"The runtime collection '{collection}' is not configured.");
            logger.LogTrace($"Resolved PublisherStudio runtime collection '{collection}' with {values.Length} values.");
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve PublisherStudio runtime collection '{collection}'.");
            throw;
        }
    }

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
