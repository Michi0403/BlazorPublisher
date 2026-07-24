using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

[ApiController]
[Route("api/mediahost/native-captures")]
public sealed class NativeCaptureController(NativeCaptureUseCases useCases) : ControllerBase
{
    private readonly NativeCaptureUseCases _useCases = useCases;

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

    [HttpDelete("{captureId:guid}")]
    public IActionResult Stop(Guid captureId) =>
        _useCases.Stop(captureId) ? NoContent() : NotFound();
}
