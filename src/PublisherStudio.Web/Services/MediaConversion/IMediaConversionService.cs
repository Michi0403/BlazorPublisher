using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.MediaConversion;

/// <summary>
/// Defines the contract for media conversion behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IMediaConversionService
{
    /// <summary>
    /// Retrieves capabilities as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion capabilities produced by the operation.</returns>
    Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs queue as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs queue as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, MediaConversionOptions options, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves job as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    MediaConversionJobInfo? GetJob(Guid id);
    /// <summary>
    /// Retrieves jobs as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<MediaConversionJobInfo> GetJobs();
    /// <summary>
    /// Opens output as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The stream produced by the operation.</returns>
    Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves profiles as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<MediaConversionProfile> GetProfiles();
    /// <summary>
    /// Persists profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The media conversion profile produced by the operation.</returns>
    MediaConversionProfile SaveProfile(MediaConversionProfile profile);
    /// <summary>
    /// Deletes profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool DeleteProfile(Guid id);
    /// <summary>
    /// Determines whether cel as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Cancel(Guid id);
    /// <summary>
    /// Performs remove as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Remove(Guid id);
}
