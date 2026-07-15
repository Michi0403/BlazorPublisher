using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/publications")]
public sealed class PublicationController(PublicationFileService files) : ControllerBase
{
    [HttpPost("download")]
    public IActionResult Download([FromBody] PublicationDocument document)
    {
        var json = files.Serialize(document);
        var name = PublicationFileService.SafeFileName(document.Name) + ".pubstudio.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", name);
    }

    [HttpPost("validate")]
    [RequestSizeLimit(64 * 1024 * 1024)]
    public async Task<ActionResult<PublicationDocument>> Validate(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest("The uploaded publication is empty.");
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return Ok(files.Deserialize(json));
    }
}
