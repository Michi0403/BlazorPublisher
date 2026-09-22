using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

/// <summary>Exposes PublisherStudio file-format capability discovery through the HTTP boundary while keeping capability logic service-owned.</summary>
/// <param name="formats">File-format capability service used to produce the current projection.</param>
/// <param name="logger">Logger used to record controller-boundary diagnostics without duplicating capability details.</param>
[ApiController]
[Route("api/publisher/file-formats")]
public sealed class PublisherFileFormatsController(
    IPublisherFileFormatCapabilityService formats,
    ILogger<PublisherFileFormatsController> logger) : ControllerBase
{
    /// <summary>Returns the current PublisherStudio file-format capability snapshot supplied by the authoritative capability service.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The current PublisherStudio file-format capability snapshot.</returns>
    [HttpGet]
    public async Task<ActionResult<PublisherFileFormatCapabilitySnapshot>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await formats.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug(
                "Returned PublisherStudio file-format capability snapshot with {FamilyCount} families; media runtime available={MediaAvailable}.",
                snapshot.Families.Count,
                snapshot.MediaConversionAvailable);
            return Ok(snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("PublisherStudio file-format capability request was cancelled by the caller.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio file-format capability request failed at the controller boundary.");
            throw;
        }
    }
}
