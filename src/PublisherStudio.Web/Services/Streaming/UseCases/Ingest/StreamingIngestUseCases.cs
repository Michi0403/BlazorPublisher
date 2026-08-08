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
    public bool Exists(Guid sessionId) {
    try
    {
        return _sessions.TryGet(sessionId, out _);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.Exists failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the announce operation.
    /// </summary>
    public bool Announce(Guid sessionId, Guid? outputId, IngestAnnouncement announcement) {
    try
    {
        return _sessions.AnnounceIngest(sessionId, outputId, announcement);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.Announce failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the push operation.
    /// </summary>
    public bool Push(Guid sessionId, Guid? outputId, byte[] payload) {
    try
    {
        return _sessions.PushIngest(sessionId, outputId, payload);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.Push failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether publish web rtc.
    /// </summary>
    public bool CanPublishWebRtc(Guid sessionId) {
    try
    {
        return _sessions.TryGet(sessionId, out var session) && session.LanDefinition.EnableBrowserWebRtc;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.CanPublishWebRtc failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the run web rtc publisher async operation.
    /// </summary>
    public async Task<bool> RunWebRtcPublisherAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
    try
    {
            if (!_sessions.TryGet(sessionId, out var session) || !session.LanDefinition.EnableBrowserWebRtc)
                return false;
            await session.WebRtc.RunPublisherAsync(socket, cancellationToken);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.RunWebRtcPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
