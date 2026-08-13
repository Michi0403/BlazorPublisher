using System.Collections.Frozen;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates publisher runtime policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class PublisherRuntimePolicyDataService : IPublisherRuntimePolicyDataService
{
    /// <summary>
    /// Stores the internal options state used by <see cref="PublisherRuntimePolicyDataService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PublisherRuntimePolicyOptions options;
    /// <summary>
    /// Stores the in-memory collections collection maintained internally by <see cref="PublisherRuntimePolicyDataService"/> for its current workflow state.
    /// </summary>
    private readonly FrozenDictionary<PublisherRuntimeCollection, string[]> collections;
    /// <summary>
    /// Stores the logger used by <see cref="PublisherRuntimePolicyDataService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<PublisherRuntimePolicyDataService> logger;

    /// <summary>
    /// Initializes a new <see cref="PublisherRuntimePolicyDataService"/> instance and captures the dependencies or initial state required by its publisher runtime policy workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// Gets the spreadsheet session lifetime duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The spreadsheet session lifetime value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public TimeSpan SpreadsheetSessionLifetime => TimeSpan.FromHours(options.SpreadsheetSessionLifetimeHours);
    /// <summary>
    /// Gets the stable audio client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio client interface identifier value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public Guid AudioClientInterfaceId => Guid.Parse(options.AudioClientInterfaceId);
    /// <summary>
    /// Gets the stable audio capture client interface identifier used to identify or correlate this publisher runtime policy instance with related application state.
    /// </summary>
    /// <value>The audio capture client interface identifier value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public Guid AudioCaptureClientInterfaceId => Guid.Parse(options.AudioCaptureClientInterfaceId);
    /// <summary>
    /// Gets the twitch validation interval duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The twitch validation interval value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public TimeSpan TwitchValidationInterval => TimeSpan.FromMinutes(options.TwitchValidationIntervalMinutes);
    /// <summary>
    /// Gets the twitch refresh safety window duration used to control timing in the publisher runtime policy workflow.
    /// </summary>
    /// <value>The twitch refresh safety window value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public TimeSpan TwitchRefreshSafetyWindow => TimeSpan.FromMinutes(options.TwitchRefreshSafetyWindowMinutes);
    /// <summary>
    /// Gets the minimum media source length that quantifies the associated publisher runtime policy data.
    /// </summary>
    /// <value>The minimum media source length value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public double MinimumMediaSourceLength => options.MinimumMediaSourceLength;
    /// <summary>
    /// Gets the word art view width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view width value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public double WordArtViewWidth => options.WordArtViewWidth;
    /// <summary>
    /// Gets the word art view height value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The word art view height value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public double WordArtViewHeight => options.WordArtViewHeight;
    /// <summary>
    /// Gets the base pixels per millimeter value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The base pixels per millimeter value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public double BasePixelsPerMillimeter => options.BasePixelsPerMillimeter;
    /// <summary>
    /// Gets the default editor viewport width value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default editor viewport width value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int DefaultEditorViewportWidth => options.DefaultEditorViewportWidth;
    /// <summary>
    /// Gets the audio sample rate value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio sample rate value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int AudioSampleRate => options.AudioSampleRate;
    /// <summary>
    /// Gets the maximum video archive entries value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum video archive entries value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int MaximumVideoArchiveEntries => options.MaximumVideoArchiveEntries;
    /// <summary>
    /// Gets the maximum notification messages value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum notification messages value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int MaximumNotificationMessages => options.MaximumNotificationMessages;
    /// <summary>
    /// Gets the installer download attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer download attempts value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int InstallerDownloadAttempts => options.InstallerDownloadAttempts;
    /// <summary>
    /// Gets the installer move attempts value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer move attempts value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int InstallerMoveAttempts => options.InstallerMoveAttempts;
    /// <summary>
    /// Gets the organic protocol version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic protocol version value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public string OrganicProtocolVersion => options.OrganicProtocolVersion;
    /// <summary>
    /// Gets the organic security schema version value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic security schema version value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int OrganicSecuritySchemaVersion => options.OrganicSecuritySchemaVersion;
    /// <summary>
    /// Gets the organic totp period seconds value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp period seconds value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public int OrganicTotpPeriodSeconds => options.OrganicTotpPeriodSeconds;
    /// <summary>
    /// Gets the organic totp alphabet value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic totp alphabet value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public string OrganicTotpAlphabet => options.OrganicTotpAlphabet;
    /// <summary>
    /// Gets the FFmpeg environment variable value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The FFmpeg environment variable value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public string FfmpegEnvironmentVariable => options.FfmpegEnvironmentVariable;
    /// <summary>
    /// Gets the twitch endpoints value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The twitch endpoints value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public PublisherTwitchEndpointPolicy TwitchEndpoints => options.TwitchEndpoints;
    /// <summary>
    /// Gets the native interop value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native interop value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public PublisherNativeInteropPolicy NativeInterop => options.NativeInterop;
    /// <summary>
    /// Gets the picture studio value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The picture studio value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public PublisherPictureStudioPolicy PictureStudio => options.PictureStudio;
    /// <summary>
    /// Gets the document defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document defaults value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public PublisherDocumentDefaultsPolicy DocumentDefaults => options.DocumentDefaults;
    /// <summary>
    /// Gets the media session defaults value that forms part of the publisher runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media session defaults value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public PublisherMediaSessionDefaultsPolicy MediaSessionDefaults => options.MediaSessionDefaults;
    /// <summary>
    /// Gets the media conversion presets collection maintained or exposed by this publisher runtime policy instance for downstream processing.
    /// </summary>
    /// <value>The media conversion presets value exposed by <see cref="PublisherRuntimePolicyDataService"/>.</value>
    public IReadOnlyList<MediaConversionPreset> MediaConversionPresets => options.MediaConversionPresets;

    /// <summary>
    /// Retrieves collection as part of the publisher runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="collection">Collection value supplied to the publisher runtime policy operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves snapshot as part of the publisher runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The publisher runtime policy snapshot produced by the operation.</returns>
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
    /// Performs validate as part of the publisher runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
