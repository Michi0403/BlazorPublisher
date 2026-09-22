using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Services;

/// <summary>Builds the maintained PublisherStudio file-format contract from its existing editor and media capabilities.</summary>
/// <param name="mediaConversion">Media conversion service used to report current audio/video runtime availability.</param>
/// <param name="logger">Logger used to record diagnostics produced while capability discovery runs.</param>
public sealed class PublisherFileFormatCapabilityService(
    IMediaConversionService mediaConversion,
    ILogger<PublisherFileFormatCapabilityService> logger) : IPublisherFileFormatCapabilityService
{
    /// <summary>Builds the live file-format capability snapshot from PublisherStudio's maintained import families and current media-runtime availability.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop capability discovery.</param>
    /// <returns>A snapshot whose format families and runtime flags reflect this PublisherStudio node at the time of the call.</returns>
    public async Task<PublisherFileFormatCapabilitySnapshot> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var media = await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = new PublisherFileFormatCapabilitySnapshot
            {
                MediaConversionAvailable = media.Available,
                UpdatedUtc = DateTimeOffset.UtcNow,
                Families =
                [
                    Family("word-openxml", "Word/OpenXML document", [".docx"], directImport: true),
                    Family("spreadsheet", "Spreadsheet workbook", [".xlsx", ".xlsm", ".xls", ".csv", ".tsv", ".txt"], directImport: true),
                    Family("picture", "Picture/image", [".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp", ".tif", ".tiff"], directImport: true),
                    Family("video", "Video media", [".mp4", ".m4v", ".webm", ".ogv", ".ogg", ".mov", ".mkv", ".avi", ".mxf", ".mts", ".m2ts", ".vob"], directImport: true, requiresMediaRuntime: true, runtimeAvailable: media.Available),
                    Family("audio", "Audio media", [".mp3", ".m4a", ".aac", ".wav", ".flac", ".ogg", ".oga"], directImport: true, requiresMediaRuntime: true, runtimeAvailable: media.Available)
                ]
            };
            logger.LogDebug("Published {FamilyCount} PublisherStudio file-format families; media runtime available={MediaAvailable}.", snapshot.Families.Count, media.Available);
            return snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("PublisherStudio file-format capability discovery was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio file-format capability discovery failed.");
            throw;
        }
    }

    /// <summary>Creates a normalized format-family record while preserving PublisherStudio as the authoritative owner of its extension mapping.</summary>
    /// <param name="key">Stable machine-readable family key advertised to linked clients.</param>
    /// <param name="displayName">Human-readable family label used by capability consumers.</param>
    /// <param name="extensions">Extensions that belong to the family and will be normalized and de-duplicated.</param>
    /// <param name="directImport">Whether PublisherStudio can ingest the family without the optional media conversion runtime.</param>
    /// <param name="requiresMediaRuntime">Whether processing the family depends on the optional media runtime.</param>
    /// <param name="runtimeAvailable">Whether the required runtime is currently available on this node.</param>
    /// <returns>A normalized PublisherStudio format-family capability record.</returns>
    private PublisherFileFormatFamily Family(
        string key,
        string displayName,
        IEnumerable<string> extensions,
        bool directImport,
        bool requiresMediaRuntime = false,
        bool runtimeAvailable = true)
    {
        try
        {
            return new PublisherFileFormatFamily
            {
                Key = key,
                DisplayName = displayName,
                Extensions = extensions.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(),
                DirectImportAvailable = directImport,
                RequiresMediaRuntime = requiresMediaRuntime,
                RuntimeAvailable = runtimeAvailable
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building PublisherStudio file-format family {FamilyKey} failed.", key);
            throw;
        }
    }
}
