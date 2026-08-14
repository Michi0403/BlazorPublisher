using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the same-origin PublisherStudio AI bridge used by AI-enabled DevExtreme publication components while the editor/runtime is hosted by PublisherStudio.
/// </summary>
/// <param name="bridge">AI bridge service that owns the secured LocalGPT 1-Wire interaction.</param>
/// <param name="logger">Logger used to record controller-boundary diagnostics.</param>
[ApiController]
[Route("api/publisher-ai")]
public sealed class PublisherAiController(
    IPublisherAiBridgeService bridge,
    ILogger<PublisherAiController> logger) : ControllerBase
{
    /// <summary>Returns whether the currently running PublisherStudio instance can route AI component requests to LocalGPT.</summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("profile")]
    public ActionResult<object> Profile()
    {
        try
        {
            return Ok(new { available = bridge.IsAvailable(), provider = "LocalGPT 1-Wire Council" });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio AI profile request failed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { available = false, error = exception.Message });
        }
    }

    /// <summary>Routes one publication Chat message to the paired LocalGPT Council and returns the final answer.</summary>
    /// <param name="request">Browser-facing AI chat request.</param>
    /// <param name="cancellationToken">Cancellation token bound to the HTTP request lifetime.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("chat")]
    public async Task<ActionResult<PublisherAiChatResponse>> Chat([FromBody] PublisherAiChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!bridge.IsAvailable())
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "LocalGPT is not linked or Council execution is unavailable." });
            return Ok(await bridge.ChatAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "PublisherStudio AI HTTP request was canceled by the browser.");
            return new StatusCodeResult(499);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio AI HTTP request failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
    }
}
