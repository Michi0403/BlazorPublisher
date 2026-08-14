using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the publication application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="files">Publication file service dependency used by the publication workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/publications")]
public sealed class PublicationController(PublicationFileService files, ILogger<PublicationController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the download projection for the publication API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("download")]
    public IActionResult Download([FromBody] PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationController.Download.");
                    var json = files.Serialize(document);
                    var name = files.SafeFileName(document.Name) + ".pubstudio.json";
                    return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", name);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationController.Download failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Returns the validate projection for the publication API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="file">Form file dependency used by the publication workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("validate")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<PublicationDocument>> Validate(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering PublicationController.Validate.");
                    if (file.Length == 0) return BadRequest("The uploaded publication is empty.");
                    var stream = file.OpenReadStream();
                    await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    return Ok(files.Deserialize(json));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationController.Validate failed: {exception.Message}");
            throw;
        }
    }
}
