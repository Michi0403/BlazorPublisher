using PublisherStudio.Domain;

namespace PublisherStudio.Services.MediaConversion;

public interface IMediaConversionService
{
    Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default);
    MediaConversionJobInfo? GetJob(Guid id);
    IReadOnlyList<MediaConversionJobInfo> GetJobs();
    Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default);
    bool Cancel(Guid id);
    bool Remove(Guid id);
}
