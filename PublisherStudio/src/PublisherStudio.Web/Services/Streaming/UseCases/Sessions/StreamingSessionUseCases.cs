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
    public MediaSession Create(JsonElement request) => _sessions.Create(request);

    /// <summary>
    /// Attempts to get.
    /// </summary>
    public bool TryGet(Guid sessionId, out MediaSession session) =>
        _sessions.TryGet(sessionId, out session!);

    /// <summary>
    /// Runs the drain events operation.
    /// </summary>
    public IReadOnlyList<MediaHostHotkeyEvent> DrainEvents(Guid sessionId) =>
        _sessions.DrainEvents(sessionId);

    /// <summary>
    /// Runs the stop operation.
    /// </summary>
    public bool Stop(Guid sessionId) => _sessions.Stop(sessionId);

    /// <summary>
    /// Sets output.
    /// </summary>
    public bool SetOutput(Guid sessionId, Guid outputId, bool enabled) =>
        _sessions.SetOutput(sessionId, outputId, enabled);

    /// <summary>
    /// Sets recording.
    /// </summary>
    public bool SetRecording(Guid sessionId, bool enabled) =>
        _sessions.SetRecording(sessionId, enabled);

    /// <summary>
    /// Sets program page.
    /// </summary>
    public bool SetProgramPage(Guid sessionId, Guid pageId) =>
        _sessions.SetProgramPage(sessionId, pageId);
}
