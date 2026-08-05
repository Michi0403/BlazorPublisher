using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Provides streaming session controller operations.
/// </summary>
[ApiController]
[Route("api/mediahost/sessions")]
public sealed class StreamingSessionController(StreamingSessionUseCases useCases) : ControllerBase
{
    private readonly StreamingSessionUseCases _useCases = useCases;

    /// <summary>
    /// Runs the create operation.
    /// </summary>
    [HttpPost]
    public IActionResult Create([FromBody] JsonElement request)
    {
        try
        {
            var session = _useCases.Create(request);
            return Ok(new { sessionId = session.Id, status = session.DryRun ? "dry-run" : "prepared" });
        }
        catch (Exception exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Runs the get operation.
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    public IActionResult Get(Guid sessionId) =>
        _useCases.TryGet(sessionId, out var session) ? Ok(session.PublicView()) : NotFound();

    /// <summary>
    /// Gets events.
    /// </summary>
    [HttpGet("{sessionId:guid}/events")]
    public IActionResult GetEvents(Guid sessionId) =>
        _useCases.TryGet(sessionId, out _) ? Ok(_useCases.DrainEvents(sessionId)) : NotFound();

    /// <summary>
    /// Runs the stop operation.
    /// </summary>
    [HttpDelete("{sessionId:guid}")]
    public IActionResult Stop(Guid sessionId) =>
        _useCases.Stop(sessionId) ? NoContent() : NotFound();

    /// <summary>
    /// Sets output.
    /// </summary>
    [HttpPut("{sessionId:guid}/outputs/{outputId:guid}")]
    public IActionResult SetOutput(Guid sessionId, Guid outputId, [FromBody] ToggleRequest request) =>
        _useCases.SetOutput(sessionId, outputId, request.Enabled) ? NoContent() : NotFound();

    /// <summary>
    /// Sets recording.
    /// </summary>
    [HttpPut("{sessionId:guid}/recording")]
    public IActionResult SetRecording(Guid sessionId, [FromBody] ToggleRequest request) =>
        _useCases.SetRecording(sessionId, request.Enabled) ? NoContent() : NotFound();

    /// <summary>
    /// Sets program page.
    /// </summary>
    [HttpPut("{sessionId:guid}/program-page")]
    public IActionResult SetProgramPage(Guid sessionId, [FromBody] ProgramPageRequest request) =>
        _useCases.SetProgramPage(sessionId, request.PageId) ? NoContent() : NotFound();
}
