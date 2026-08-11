using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.MediaConversion;

/// <summary>
/// Defines the media conversion service contract.
/// </summary>
public interface IMediaConversionService
{
    /// <summary>
    /// Gets capabilities async.
    /// </summary>
    Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the queue async operation.
    /// </summary>
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the queue async operation.
    /// </summary>
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, MediaConversionOptions options, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets job.
    /// </summary>
    MediaConversionJobInfo? GetJob(Guid id);
    /// <summary>
    /// Gets jobs.
    /// </summary>
    IReadOnlyList<MediaConversionJobInfo> GetJobs();
    /// <summary>
    /// Opens output async.
    /// </summary>
    Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets profiles.
    /// </summary>
    IReadOnlyList<MediaConversionProfile> GetProfiles();
    /// <summary>
    /// Saves profile.
    /// </summary>
    MediaConversionProfile SaveProfile(MediaConversionProfile profile);
    /// <summary>
    /// Deletes profile.
    /// </summary>
    bool DeleteProfile(Guid id);
    /// <summary>
    /// Determines whether cel.
    /// </summary>
    bool Cancel(Guid id);
    /// <summary>
    /// Runs the remove operation.
    /// </summary>
    bool Remove(Guid id);
}
