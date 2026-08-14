using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Ingest;

/// <summary>
/// Coordinates renderer ingest and WebRTC publication through reusable streaming services.
/// </summary>
/// <param name="sessions">Media session registry dependency used by the streaming ingest use cases workflow to provide the corresponding application capability.</param>
public sealed class StreamingIngestUseCases(MediaSessionRegistry sessions)
{
    /// <summary>
    /// Stores the media session registry dependency used by <see cref="StreamingIngestUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Performs exists for <see cref="StreamingIngestUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming ingest use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs announce for <see cref="StreamingIngestUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming ingest use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="announcement">Ingest announcement dependency used by the streaming ingest use cases workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs push for <see cref="StreamingIngestUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming ingest use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="payload">Payload value supplied to the streaming ingest use cases operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Determines whether publish web rtc for <see cref="StreamingIngestUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming ingest use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs run web rtc publisher for <see cref="StreamingIngestUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming ingest use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="socket">Socket value supplied to the streaming ingest use cases operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> RunWebRtcPublisherAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
    try
    {
            if (!_sessions.TryGet(sessionId, out var session) || !session.LanDefinition.EnableBrowserWebRtc)
                return false;
            await session.WebRtc.RunPublisherAsync(socket, cancellationToken).ConfigureAwait(false);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingIngestUseCases.RunWebRtcPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
