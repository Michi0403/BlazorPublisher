using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the native capture application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the native capture operation and used when producing its result.</param>
[ApiController]
[Route("api/mediahost/native-captures")]
public sealed class NativeCaptureController(NativeCaptureUseCases useCases) : ControllerBase
{
    /// <summary>
    /// Stores the internal use cases state used by <see cref="NativeCaptureController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly NativeCaptureUseCases _useCases = useCases;

    /// <summary>
    /// Returns the create projection for the native capture API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    public IActionResult Create([FromBody] NativeCaptureRequest request)
    {
        try
        {
            var capture = _useCases.Create(request);
            return Ok(new { captureId = capture.Id, mimeType = capture.MimeType, status = capture.Status });
        }
        catch (Exception exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Returns the stream projection for the native capture API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="captureId">Identifier of the capture to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [HttpGet("{captureId:guid}/websocket")]
    public async Task Stream(Guid captureId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_useCases.TryGet(captureId, out var capture))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var subscription = capture.Subscribe();
        try
        {
            if (subscription.Initialization.Length > 0)
                await socket.SendAsync(subscription.Initialization, WebSocketMessageType.Binary, true, HttpContext.RequestAborted);
            await foreach (var chunk in subscription.Reader.ReadAllAsync(HttpContext.RequestAborted))
            {
                if (socket.State != WebSocketState.Open) break;
                await socket.SendAsync(chunk, WebSocketMessageType.Binary, true, HttpContext.RequestAborted);
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { }
        catch (WebSocketException) { }
        finally
        {
            capture.Unsubscribe(subscription.Id);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Native capture ended", CancellationToken.None); } catch { }
        }
    }

    /// <summary>
    /// Returns the stop projection for the native capture API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="captureId">Identifier of the capture to use for this operation.</param>
    [HttpDelete("{captureId:guid}")]
    public IActionResult Stop(Guid captureId) =>
        _useCases.Stop(captureId) ? NoContent() : NotFound();
}
