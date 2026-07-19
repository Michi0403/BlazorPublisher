using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetController(PublicationMediaAssetStore mediaAssets) : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"
    };

    [HttpGet("media/{id:guid}")]
    public IActionResult GetMedia(Guid id)
    {
        if (!mediaAssets.TryGet(id, out var bytes, out var mimeType, out var version))
            return NotFound();

        Response.Headers[HeaderNames.CacheControl] = "private, max-age=31536000, immutable";
        Response.Headers[HeaderNames.ETag] = $"\"{version}\"";
        return File(bytes, mimeType, enableRangeProcessing: true);
    }

    [HttpPost("image")]
    [DisableRequestSizeLimit]
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

    [HttpPost("drop/{id:guid}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadDroppedAsset(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return BadRequest("A valid asset id is required.");
        var mimeType = string.IsNullOrWhiteSpace(Request.ContentType)
            ? "application/octet-stream"
            : Request.ContentType.Split(';', 2)[0].Trim();

        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0) return BadRequest("The dropped file is empty.");

        var url = mediaAssets.RegisterBytes(id, bytes, mimeType);
        return Ok(new { Id = id, MimeType = mimeType, Size = bytes.LongLength, Url = url });
    }
}
