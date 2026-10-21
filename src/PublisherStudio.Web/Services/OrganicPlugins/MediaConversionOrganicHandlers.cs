using System.Text.Json;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Returns bounded media conversion job information through the normal 1-Wire permission gateway.</summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="logger">Service-owned structured diagnostics for the capability operation.</param>
public sealed class MediaConversionJobsOrganicHandler(IMediaConversionService media, ILogger<MediaConversionJobsOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the persisted-catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.jobs";

    /// <summary>Reads bounded job progress without exposing user-local physical file paths.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Up to fifty recent conversion job summaries.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            object result = media.GetJobs().OrderByDescending(job => job.CreatedUtc).Take(50)
                .Select(job => new
                {
                    job.Id, job.PresetId, job.Status, job.Progress, job.OutputMimeType,
                    job.OutputSize, job.CreatedUtc, job.CompletedUtc
                }).ToList();
            logger.LogDebug("Listed bounded conversion jobs for an approved organic request.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list organic media conversion jobs.");
            throw;
        }
    }
}

/// <summary>Exposes operator-owned media conversion presets without making them hardcoded AI policies.</summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="logger">Service-owned structured diagnostics for the capability operation.</param>
public sealed class MediaConversionProfilesOrganicHandler(IMediaConversionService media, ILogger<MediaConversionProfilesOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the persisted-catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.profiles";

    /// <summary>Reads the current PublisherStudio media conversion profiles.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Configured media conversion profile metadata.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            object result = media.GetProfiles().Select(profile => new
            {
                profile.Id, profile.Name, profile.Description, profile.PresetId,
                profile.BuiltIn, profile.ModifiedUtc
            }).ToList();
            logger.LogDebug("Listed approved media conversion profiles.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read organic media conversion profiles.");
            throw;
        }
    }
}

/// <summary>Cancels one current media job using the existing media service after PublisherStudio approval.</summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="logger">Service-owned structured diagnostics for the capability operation.</param>
public sealed class MediaConversionCancelOrganicHandler(IMediaConversionService media, ILogger<MediaConversionCancelOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the persisted-catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.cancel";

    /// <summary>Requests cancellation of one UUID-addressed PublisherStudio conversion job.</summary>
    /// <param name="envelope">Already approved and permission-checked 1-Wire work envelope.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Whether a matching in-memory conversion job was cancelled.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (envelope.Properties is null ||
                !envelope.Properties.TryGetValue("Parameters", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object ||
                !parameters.TryGetProperty("jobId", out var jobIdValue) ||
                jobIdValue.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(jobIdValue.GetString(), out var jobId))
                throw new ArgumentException("A valid jobId must be provided for the approved cancellation.");
            object result = new { JobId = jobId, Cancelled = media.Cancel(jobId) };
            logger.LogInformation("Organic media cancellation requested for job {JobId}.", jobId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Organic media cancellation failed.");
            throw;
        }
    }
}
