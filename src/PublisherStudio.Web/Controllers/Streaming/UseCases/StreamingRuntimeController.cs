using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the streaming runtime application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the streaming runtime operation and used when producing its result.</param>
[ApiController]
[Route("api/mediahost")]
public sealed class StreamingRuntimeController(StreamingRuntimeUseCases useCases) : ControllerBase
{
    /// <summary>
    /// Stores the internal use cases state used by <see cref="StreamingRuntimeController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingRuntimeUseCases _useCases = useCases;

    /// <summary>
    /// Retrieves capabilities for the streaming runtime API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities() => Ok(_useCases.GetCapabilities());

    /// <summary>
    /// Discovers devices for the streaming runtime API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the streaming runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("devices")]
    public async Task<IActionResult> DiscoverDevices(
        [FromQuery] string? ffmpegPath,
        CancellationToken cancellationToken) =>
        Ok(await _useCases.DiscoverDevicesAsync(ffmpegPath, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Retrieves now playing for the streaming runtime API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="directory">Directory value supplied to the streaming runtime operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("now-playing")]
    public IActionResult GetNowPlaying([FromQuery] string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return BadRequest(new { error = "A directory is required." });
        var metadata = _useCases.ReadNowPlaying(directory);
        return metadata is null ? NoContent() : Ok(metadata);
    }
}
