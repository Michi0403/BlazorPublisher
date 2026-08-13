using PublisherStudio.Hubs.Streaming.Chat;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the streaming chat application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the streaming chat operation and used when producing its result.</param>
/// <param name="hub">Hub value supplied to the streaming chat operation and used when producing its result.</param>
[ApiController]
[Route("api/mediahost/sessions/{sessionId:guid}/chat")]
public sealed class StreamingChatController(StreamingChatUseCases useCases, PlatformChatHub hub) : ControllerBase
{
    /// <summary>
    /// Stores the internal use cases state used by <see cref="StreamingChatController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingChatUseCases _useCases = useCases;
    /// <summary>
    /// Stores the internal hub state used by <see cref="StreamingChatController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PlatformChatHub _hub = hub;

    /// <summary>
    /// Returns the subscribe projection for the streaming chat API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [HttpGet("{outputId:guid}/websocket")]
    public async Task Subscribe(Guid sessionId, Guid outputId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_hub.CanOpen(sessionId))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _hub.RunSubscriberAsync(sessionId, outputId, socket, HttpContext.RequestAborted);
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Chat subscription ended", CancellationToken.None); } catch { }
    }

    /// <summary>
    /// Returns the send projection for the streaming chat API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
