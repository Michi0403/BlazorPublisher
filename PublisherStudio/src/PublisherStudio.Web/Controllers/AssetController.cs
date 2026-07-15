using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"
    };

    [HttpPost("image")]
    [RequestSizeLimit(64 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || !AllowedTypes.Contains(file.ContentType))
            return BadRequest("Select a supported image file.");
        await using var source = file.OpenReadStream();
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        return Ok(new
        {
            file.FileName,
            file.ContentType,
            DataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}"
        });
    }
}
