using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PublisherStudio.Controllers.Streaming.UseCases;

[ApiController]
[Route("api/mediahost/sessions/{sessionId:guid}")]
public sealed class StreamingIngestController(StreamingIngestUseCases useCases) : ControllerBase
{
    private static readonly JsonSerializerOptions IngestJson = new() { PropertyNameCaseInsensitive = true };
    private readonly StreamingIngestUseCases _useCases = useCases;

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

    [HttpPost("ingest/announce")]
    public IActionResult Announce(Guid sessionId, [FromBody] IngestAnnouncement announcement) =>
        _useCases.Announce(sessionId, announcement.OutputId, announcement) ? Accepted() : NotFound();

    [HttpGet("webrtc/publisher")]
    public async Task PublishWebRtc(Guid sessionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest || !_useCases.CanPublishWebRtc(sessionId))
        {
            Response.StatusCode = HttpContext.WebSockets.IsWebSocketRequest
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _useCases.RunWebRtcPublisherAsync(sessionId, socket, HttpContext.RequestAborted);
    }
}
