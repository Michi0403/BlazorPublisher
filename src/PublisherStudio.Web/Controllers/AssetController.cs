using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the asset application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="mediaAssets">Publication media asset store dependency used by the asset workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/assets")]
public sealed class AssetController(PublicationMediaAssetStore mediaAssets) : ControllerBase
{
    /// <summary>
    /// Stores the in-memory allowed types collection maintained internally by <see cref="AssetController"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"
    };

    /// <summary>
    /// Retrieves media for the asset API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("media/{id:guid}")]
    public IActionResult GetMedia(Guid id)
    {
        if (!mediaAssets.TryGet(id, out var bytes, out var mimeType, out var version))
            return NotFound();

        Response.Headers[HeaderNames.CacheControl] = "private, max-age=31536000, immutable";
        Response.Headers[HeaderNames.ETag] = $"\"{version}\"";
        return File(bytes, mimeType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Returns the upload image projection for the asset API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="file">Form file dependency used by the asset workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the upload dropped asset projection for the asset API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
