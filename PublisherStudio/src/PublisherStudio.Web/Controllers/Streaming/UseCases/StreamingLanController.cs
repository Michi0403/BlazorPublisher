using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

[ApiController]
public sealed class StreamingLanController(StreamingLanUseCases useCases) : ControllerBase
{
    private readonly StreamingLanUseCases _useCases = useCases;

    [HttpGet("api/mediahost/sessions/{sessionId:guid}/lan")]
    public IActionResult GetStatus(Guid sessionId)
    {
        var status = _useCases.GetStatus(sessionId);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpGet("stream/{sessionId:guid}/{**asset}")]
    public IActionResult GetAsset(Guid sessionId, string? asset)
    {
        var resolved = _useCases.ResolveAsset(sessionId, asset);
        return resolved is null
            ? NotFound()
            : PhysicalFile(resolved.Path, resolved.ContentType, enableRangeProcessing: true);
    }

    [HttpGet("watch/{sessionId:guid}")]
    public IActionResult Watch(Guid sessionId)
    {
        var html = _useCases.BuildWatchPage(sessionId);
        return html is null ? NotFound() : Content(html, "text/html");
    }
}
