using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

[ApiController]
[Route("api/mediahost/sessions/{sessionId:guid}/chat")]
public sealed class StreamingChatController(StreamingChatUseCases useCases) : ControllerBase
{
    private readonly StreamingChatUseCases _useCases = useCases;

    [HttpGet("{outputId:guid}/websocket")]
    public async Task Subscribe(Guid sessionId, Guid outputId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_useCases.CanOpen(sessionId))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _useCases.RunSubscriberAsync(sessionId, outputId, socket, HttpContext.RequestAborted);
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chat subscription ended", CancellationToken.None); } catch { }
    }

    [HttpPost("{outputId:guid}/send")]
    public async Task<IActionResult> Send(
        Guid sessionId,
        Guid outputId,
        [FromBody] ChatSendRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _useCases.SendAsync(sessionId, outputId, request.Message, cancellationToken);
        if (!result.Exists) return NotFound();
        if (result.Sent) return Accepted();
        return BadRequest(new { error = result.Error });
    }
}
