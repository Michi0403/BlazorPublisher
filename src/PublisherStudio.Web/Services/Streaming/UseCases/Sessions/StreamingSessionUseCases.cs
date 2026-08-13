using System.Text.Json;

namespace PublisherStudio.Services.Streaming.UseCases.Sessions;

/// <summary>
/// Application-level session orchestration used by both MVC controllers and the local UI facade.
/// </summary>
/// <param name="sessions">Media session registry dependency used by the streaming session use cases workflow to provide the corresponding application capability.</param>
public sealed class StreamingSessionUseCases(MediaSessionRegistry sessions)
{
    /// <summary>
    /// Stores the media session registry dependency used by <see cref="StreamingSessionUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Performs create for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The media session produced by the operation.</returns>
    public MediaSession Create(JsonElement request) {
    try
    {
        return _sessions.Create(request);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.Create failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="session">Session value supplied to the streaming session use cases operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid sessionId, out MediaSession session) {
    try
    {
        return _sessions.TryGet(sessionId, out session!);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.TryGet failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs drain events for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<MediaHostHotkeyEvent> DrainEvents(Guid sessionId) {
    try
    {
        return _sessions.DrainEvents(sessionId);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.DrainEvents failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs stop for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Stop(Guid sessionId) {
    try
    {
        return _sessions.Stop(sessionId);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.Stop failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets output for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool SetOutput(Guid sessionId, Guid outputId, bool enabled) {
    try
    {
        return _sessions.SetOutput(sessionId, outputId, enabled);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.SetOutput failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets recording for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool SetRecording(Guid sessionId, bool enabled) {
    try
    {
        return _sessions.SetRecording(sessionId, enabled);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.SetRecording failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets program page for <see cref="StreamingSessionUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming session use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="pageId">Identifier of the page to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool SetProgramPage(Guid sessionId, Guid pageId) {
    try
    {
        return _sessions.SetProgramPage(sessionId, pageId);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionUseCases.SetProgramPage failed: {__serviceMethodException}");
        throw;
    }
}
}
