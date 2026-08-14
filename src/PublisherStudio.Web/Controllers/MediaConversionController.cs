using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the media conversion application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="conversions">Media conversion service dependency used by the media conversion workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/media-conversion")]
public sealed class MediaConversionController(IMediaConversionService conversions) : ControllerBase
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="MediaConversionController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    /// <summary>
    /// Stores the media conversion service dependency used by <see cref="MediaConversionController"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IMediaConversionService _conversions = conversions;

    /// <summary>
    /// Returns the capabilities projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capabilities")]
    public async Task<ActionResult<MediaConversionCapabilities>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await _conversions.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the profiles projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("profiles")]
    public ActionResult<IReadOnlyList<MediaConversionProfile>> Profiles() => Ok(_conversions.GetProfiles());

    /// <summary>
    /// Persists profile for the media conversion API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="profile">Profile value supplied to the media conversion operation and used when producing its result.</param>
    [HttpPost("profiles")]
    public ActionResult<MediaConversionProfile> SaveProfile([FromBody] MediaConversionProfile profile) =>
        Ok(_conversions.SaveProfile(profile));

    /// <summary>
    /// Deletes profile for the media conversion API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    [HttpDelete("profiles/{id:guid}")]
    public IActionResult DeleteProfile(Guid id) => _conversions.DeleteProfile(id) ? NoContent() : NotFound();

    /// <summary>
    /// Returns the jobs projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("jobs")]
    public ActionResult<IReadOnlyList<MediaConversionJobInfo>> Jobs() => Ok(_conversions.GetJobs());

    /// <summary>
    /// Returns the job projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("jobs/{id:guid}")]
    public ActionResult<MediaConversionJobInfo> Job(Guid id) =>
        _conversions.GetJob(id) is { } job ? Ok(job) : NotFound();

    /// <summary>
    /// Returns the convert projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="file">Form file dependency used by the media conversion workflow to provide the corresponding application capability.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="optionsJson">Options json value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("jobs")]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<MediaConversionJobInfo>> Convert(
        [FromForm] IFormFile file,
        [FromForm] string presetId,
        [FromForm] string? optionsJson,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0) return BadRequest("Select a non-empty media file.");
        MediaConversionOptions options;
        try
        {
            options = string.IsNullOrWhiteSpace(optionsJson)
                ? new MediaConversionOptions()
                : JsonSerializer.Deserialize<MediaConversionOptions>(optionsJson, JsonOptions) ?? new MediaConversionOptions();
        }
        catch (JsonException exception)
        {
            return BadRequest($"The conversion options JSON is invalid: {exception.Message}");
        }

        var stream = file.OpenReadStream();
        await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
        var job = await _conversions.QueueAsync(stream, file.FileName, file.ContentType, presetId, options, cancellationToken).ConfigureAwait(false);
        return AcceptedAtAction(nameof(Job), new { id = job.Id }, job);
    }

    /// <summary>
    /// Returns the download projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("jobs/{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var job = _conversions.GetJob(id);
        if (job is null) return NotFound();
        if (job.Status != MediaConversionJobStatus.Completed) return Conflict("The conversion is not complete.");
        var stream = await _conversions.OpenOutputAsync(id, cancellationToken).ConfigureAwait(false);
        return stream is null ? NotFound() : File(stream, job.OutputMimeType, job.OutputFileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Returns the remove projection for the media conversion API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("jobs/{id:guid}")]
    public IActionResult Remove(Guid id)
    {
        if (_conversions.Cancel(id)) return Accepted();
        return _conversions.Remove(id) ? NoContent() : NotFound();
    }
}
