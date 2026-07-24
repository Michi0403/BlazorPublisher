using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Ingest;

/// <summary>
/// Coordinates renderer ingest and WebRTC publication through reusable streaming services.
/// </summary>
public sealed class StreamingIngestUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    public bool Exists(Guid sessionId) => _sessions.TryGet(sessionId, out _);

    public bool Announce(Guid sessionId, Guid? outputId, IngestAnnouncement announcement) =>
        _sessions.AnnounceIngest(sessionId, outputId, announcement);

    public bool Push(Guid sessionId, Guid? outputId, byte[] payload) =>
        _sessions.PushIngest(sessionId, outputId, payload);

    public bool CanPublishWebRtc(Guid sessionId) =>
        _sessions.TryGet(sessionId, out var session) && session.LanDefinition.EnableBrowserWebRtc;

    public async Task<bool> RunWebRtcPublisherAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGet(sessionId, out var session) || !session.LanDefinition.EnableBrowserWebRtc)
            return false;
        await session.WebRtc.RunPublisherAsync(socket, cancellationToken);
        return true;
    }
}
