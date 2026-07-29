using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/publications")]
public sealed class PublicationController(PublicationFileService files, ILogger<PublicationController> logger) : ControllerBase
{
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

    [HttpPost("validate")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<PublicationDocument>> Validate(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering PublicationController.Validate.");
                    if (file.Length == 0) return BadRequest("The uploaded publication is empty.");
                    await using var stream = file.OpenReadStream();
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
