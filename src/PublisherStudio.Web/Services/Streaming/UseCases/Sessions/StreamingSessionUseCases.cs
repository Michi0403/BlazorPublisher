using System.Text.Json;

namespace PublisherStudio.Services.Streaming.UseCases.Sessions;

/// <summary>
/// Application-level session orchestration used by both MVC controllers and the local UI facade.
/// </summary>
public sealed class StreamingSessionUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Runs the create operation.
    /// </summary>
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
    /// Attempts to get.
    /// </summary>
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
    /// Runs the drain events operation.
    /// </summary>
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
    /// Runs the stop operation.
    /// </summary>
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
    /// Sets output.
    /// </summary>
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
    /// Sets recording.
    /// </summary>
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
    /// Sets program page.
    /// </summary>
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
