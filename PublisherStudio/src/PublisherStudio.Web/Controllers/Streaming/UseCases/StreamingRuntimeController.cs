using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Provides streaming runtime controller operations.
/// </summary>
[ApiController]
[Route("api/mediahost")]
public sealed class StreamingRuntimeController(StreamingRuntimeUseCases useCases) : ControllerBase
{
    private readonly StreamingRuntimeUseCases _useCases = useCases;

    /// <summary>
    /// Gets capabilities.
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities() => Ok(_useCases.GetCapabilities());

    /// <summary>
    /// Runs the discover devices operation.
    /// </summary>
    [HttpGet("devices")]
    public async Task<IActionResult> DiscoverDevices(
        [FromQuery] string? ffmpegPath,
        CancellationToken cancellationToken) =>
        Ok(await _useCases.DiscoverDevicesAsync(ffmpegPath, cancellationToken));

    /// <summary>
    /// Gets now playing.
    /// </summary>
    [HttpGet("now-playing")]
    public IActionResult GetNowPlaying([FromQuery] string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return BadRequest(new { error = "A directory is required." });
        var metadata = _useCases.ReadNowPlaying(directory);
        return metadata is null ? NoContent() : Ok(metadata);
    }
}
