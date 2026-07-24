using System.Net.WebSockets;

namespace PublisherStudio.Hubs.Streaming.Lan;

/// <summary>
/// WebSocket entry point for the renderer-side WebRTC signaling connection. Signaling state
/// and session lookup remain reusable services; the hub exposes the persistent connection role.
/// </summary>
public sealed class WebRtcSignalingHub(StreamingIngestUseCases useCases)
{
    public bool CanPublish(Guid sessionId) => useCases.CanPublishWebRtc(sessionId);

    public Task<bool> RunPublisherAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunWebRtcPublisherAsync(sessionId, socket, cancellationToken);
}
