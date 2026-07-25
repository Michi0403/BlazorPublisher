using PublisherStudio.Domain;

namespace PublisherStudio.Services.MediaConversion;

public interface IMediaConversionService
{
    Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default);
    Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, MediaConversionOptions options, CancellationToken cancellationToken = default);
    MediaConversionJobInfo? GetJob(Guid id);
    IReadOnlyList<MediaConversionJobInfo> GetJobs();
    Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default);
    IReadOnlyList<MediaConversionProfile> GetProfiles();
    MediaConversionProfile SaveProfile(MediaConversionProfile profile);
    bool DeleteProfile(Guid id);
    bool Cancel(Guid id);
    bool Remove(Guid id);
}
