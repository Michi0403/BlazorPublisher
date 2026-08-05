using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Ingest;

/// <summary>
/// Coordinates renderer ingest and WebRTC publication through reusable streaming services.
/// </summary>
public sealed class StreamingIngestUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Runs the exists operation.
    /// </summary>
    public bool Exists(Guid sessionId) => _sessions.TryGet(sessionId, out _);

    /// <summary>
    /// Runs the announce operation.
    /// </summary>
    public bool Announce(Guid sessionId, Guid? outputId, IngestAnnouncement announcement) =>
        _sessions.AnnounceIngest(sessionId, outputId, announcement);

    /// <summary>
    /// Runs the push operation.
    /// </summary>
    public bool Push(Guid sessionId, Guid? outputId, byte[] payload) =>
        _sessions.PushIngest(sessionId, outputId, payload);

    /// <summary>
    /// Determines whether publish web rtc.
    /// </summary>
    public bool CanPublishWebRtc(Guid sessionId) =>
        _sessions.TryGet(sessionId, out var session) && session.LanDefinition.EnableBrowserWebRtc;

    /// <summary>
    /// Runs the run web rtc publisher async operation.
    /// </summary>
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
