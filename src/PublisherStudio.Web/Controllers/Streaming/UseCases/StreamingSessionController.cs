using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the streaming session application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the streaming session operation and used when producing its result.</param>
[ApiController]
[Route("api/mediahost/sessions")]
public sealed class StreamingSessionController(StreamingSessionUseCases useCases) : ControllerBase
{
    /// <summary>
    /// Stores the internal use cases state used by <see cref="StreamingSessionController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingSessionUseCases _useCases = useCases;

    /// <summary>
    /// Returns the create projection for the streaming session API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
    /// Returns the get projection for the streaming session API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{sessionId:guid}")]
    public IActionResult Get(Guid sessionId) =>
        _useCases.TryGet(sessionId, out var session) ? Ok(session.PublicView()) : NotFound();

    /// <summary>
    /// Retrieves events for the streaming session API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{sessionId:guid}/events")]
    public IActionResult GetEvents(Guid sessionId) =>
        _useCases.TryGet(sessionId, out _) ? Ok(_useCases.DrainEvents(sessionId)) : NotFound();

    /// <summary>
    /// Returns the stop projection for the streaming session API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    [HttpDelete("{sessionId:guid}")]
    public IActionResult Stop(Guid sessionId) =>
        _useCases.Stop(sessionId) ? NoContent() : NotFound();

    /// <summary>
    /// Sets output for the streaming session API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPut("{sessionId:guid}/outputs/{outputId:guid}")]
    public IActionResult SetOutput(Guid sessionId, Guid outputId, [FromBody] ToggleRequest request) =>
        _useCases.SetOutput(sessionId, outputId, request.Enabled) ? NoContent() : NotFound();

    /// <summary>
    /// Sets recording for the streaming session API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPut("{sessionId:guid}/recording")]
    public IActionResult SetRecording(Guid sessionId, [FromBody] ToggleRequest request) =>
        _useCases.SetRecording(sessionId, request.Enabled) ? NoContent() : NotFound();

    /// <summary>
    /// Sets program page for the streaming session API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPut("{sessionId:guid}/program-page")]
    public IActionResult SetProgramPage(Guid sessionId, [FromBody] ProgramPageRequest request) =>
        _useCases.SetProgramPage(sessionId, request.PageId) ? NoContent() : NotFound();
}
