using PublisherStudio.Hubs.Streaming.Lan;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

/// <summary>
/// Exposes the streaming ingest application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="useCases">Use cases value supplied to the streaming ingest operation and used when producing its result.</param>
/// <param name="webRtcHub">Web rtc hub value supplied to the streaming ingest operation and used when producing its result.</param>
[ApiController]
[Route("api/mediahost/sessions/{sessionId:guid}")]
public sealed class StreamingIngestController(StreamingIngestUseCases useCases, WebRtcSignalingHub webRtcHub) : ControllerBase
{
    /// <summary>
    /// Stores the internal ingest JSON state used by <see cref="StreamingIngestController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions IngestJson = new() { PropertyNameCaseInsensitive = true };
    /// <summary>
    /// Stores the internal use cases state used by <see cref="StreamingIngestController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingIngestUseCases _useCases = useCases;
    /// <summary>
    /// Stores the internal web rtc hub state used by <see cref="StreamingIngestController"/> while executing its surrounding workflow.
    /// </summary>
    private readonly WebRtcSignalingHub _webRtcHub = webRtcHub;

    /// <summary>
    /// Returns the ingest projection for the streaming ingest API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [HttpGet("ingest/websocket")]
    public async Task Ingest(Guid sessionId, [FromQuery] Guid? outputId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_useCases.Exists(sessionId))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 1024];
        using var message = new MemoryStream();
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, HttpContext.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) break;
                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;
                var payload = message.ToArray();
                message.SetLength(0);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var announcement = JsonSerializer.Deserialize<IngestAnnouncement>(payload, IngestJson);
                    if (announcement is not null)
                        _useCases.Announce(sessionId, outputId ?? announcement.OutputId, announcement);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    _useCases.Push(sessionId, outputId, payload);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ingest closed", CancellationToken.None); } catch { }
        }
    }

    /// <summary>
    /// Returns the announce projection for the streaming ingest API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="announcement">Ingest announcement dependency used by the streaming ingest workflow to provide the corresponding application capability.</param>
    [HttpPost("ingest/announce")]
    public IActionResult Announce(Guid sessionId, [FromBody] IngestAnnouncement announcement) =>
        _useCases.Announce(sessionId, announcement.OutputId, announcement) ? Accepted() : NotFound();

    /// <summary>
    /// Publishes web rtc for the streaming ingest API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [HttpGet("webrtc/publisher")]
    public async Task PublishWebRtc(Guid sessionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_webRtcHub.CanPublish(sessionId))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _webRtcHub.RunPublisherAsync(sessionId, socket, HttpContext.RequestAborted);
    }
}
