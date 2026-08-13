using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the streaming LAN application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the streaming LAN operation and used when producing its result.</param>
[ApiController]
public sealed class StreamingLanController(StreamingLanUseCases useCases) : ControllerBase
{
    /// <summary>
    /// Stores the internal use cases state used by <see cref="StreamingLanController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingLanUseCases _useCases = useCases;

    /// <summary>
    /// Retrieves status for the streaming LAN API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("api/mediahost/sessions/{sessionId:guid}/lan")]
    public IActionResult GetStatus(Guid sessionId)
    {
        var status = _useCases.GetStatus(sessionId);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>
    /// Retrieves asset for the streaming LAN API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="asset">Asset value supplied to the streaming LAN operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("stream/{sessionId:guid}/{**asset}")]
    public IActionResult GetAsset(Guid sessionId, string? asset)
    {
        var resolved = _useCases.ResolveAsset(sessionId, asset);
        return resolved is null
            ? NotFound()
            : PhysicalFile(resolved.Path, resolved.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Returns the watch projection for the streaming LAN API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("watch/{sessionId:guid}")]
    public IActionResult Watch(Guid sessionId)
    {
        var html = _useCases.BuildWatchPage(sessionId);
        return html is null ? NotFound() : Content(html, "text/html");
    }
}
