using System.Text.Json;

namespace PublisherStudio.Services.Streaming.UseCases.Sessions;

/// <summary>
/// Application-level session orchestration used by both MVC controllers and the local UI facade.
/// </summary>
public sealed class StreamingSessionUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    public MediaSession Create(JsonElement request) => _sessions.Create(request);

    public bool TryGet(Guid sessionId, out MediaSession session) =>
        _sessions.TryGet(sessionId, out session!);

    public IReadOnlyList<MediaHostHotkeyEvent> DrainEvents(Guid sessionId) =>
        _sessions.DrainEvents(sessionId);

    public bool Stop(Guid sessionId) => _sessions.Stop(sessionId);

    public bool SetOutput(Guid sessionId, Guid outputId, bool enabled) =>
        _sessions.SetOutput(sessionId, outputId, enabled);

    public bool SetRecording(Guid sessionId, bool enabled) =>
        _sessions.SetRecording(sessionId, enabled);

    public bool SetProgramPage(Guid sessionId, Guid pageId) =>
        _sessions.SetProgramPage(sessionId, pageId);
}
