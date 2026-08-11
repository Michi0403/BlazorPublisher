using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides media conversion controller operations.
/// </summary>
[ApiController]
[Route("api/media-conversion")]
public sealed class MediaConversionController(IMediaConversionService conversions) : ControllerBase
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMediaConversionService _conversions = conversions;

    /// <summary>
    /// Runs the capabilities operation.
    /// </summary>
    [HttpGet("capabilities")]
    public async Task<ActionResult<MediaConversionCapabilities>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await _conversions.GetCapabilitiesAsync(cancellationToken));

    /// <summary>
    /// Runs the profiles operation.
    /// </summary>
    [HttpGet("profiles")]
    public ActionResult<IReadOnlyList<MediaConversionProfile>> Profiles() => Ok(_conversions.GetProfiles());

    /// <summary>
    /// Saves profile.
    /// </summary>
    [HttpPost("profiles")]
    public ActionResult<MediaConversionProfile> SaveProfile([FromBody] MediaConversionProfile profile) =>
        Ok(_conversions.SaveProfile(profile));

    /// <summary>
    /// Deletes profile.
    /// </summary>
    [HttpDelete("profiles/{id:guid}")]
    public IActionResult DeleteProfile(Guid id) => _conversions.DeleteProfile(id) ? NoContent() : NotFound();

    /// <summary>
    /// Runs the jobs operation.
    /// </summary>
    [HttpGet("jobs")]
    public ActionResult<IReadOnlyList<MediaConversionJobInfo>> Jobs() => Ok(_conversions.GetJobs());

    /// <summary>
    /// Runs the job operation.
    /// </summary>
    [HttpGet("jobs/{id:guid}")]
    public ActionResult<MediaConversionJobInfo> Job(Guid id) =>
        _conversions.GetJob(id) is { } job ? Ok(job) : NotFound();

    /// <summary>
    /// Runs the convert operation.
    /// </summary>
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

        await using var stream = file.OpenReadStream();
        var job = await _conversions.QueueAsync(stream, file.FileName, file.ContentType, presetId, options, cancellationToken);
        return AcceptedAtAction(nameof(Job), new { id = job.Id }, job);
    }

    /// <summary>
    /// Runs the download operation.
    /// </summary>
    [HttpGet("jobs/{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var job = _conversions.GetJob(id);
        if (job is null) return NotFound();
        if (job.Status != MediaConversionJobStatus.Completed) return Conflict("The conversion is not complete.");
        var stream = await _conversions.OpenOutputAsync(id, cancellationToken);
        return stream is null ? NotFound() : File(stream, job.OutputMimeType, job.OutputFileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Runs the remove operation.
    /// </summary>
    [HttpDelete("jobs/{id:guid}")]
    public IActionResult Remove(Guid id)
    {
        if (_conversions.Cancel(id)) return Accepted();
        return _conversions.Remove(id) ? NoContent() : NotFound();
    }
}
