using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.Streaming.Sessions;

public sealed class MediaSessionRegistry(GlobalHotkeyService hotkeys, EncoderOrchestrator encoder) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, MediaSession> _sessions = new();
    private readonly GlobalHotkeyService _hotkeys = hotkeys;
    private readonly EncoderOrchestrator _encoder = encoder;

    public MediaSession Create(JsonElement request)
    {
        var session = MediaSession.From(request);
        if (!_sessions.TryAdd(session.Id, session)) throw new InvalidOperationException("Could not register the media session.");
        try
        {
            _hotkeys.Configure(session.Id, session.Hotkeys.Where(item => item.Global));
            session.Chat = new PlatformChatHub(session);
            session.Chat.Start();
            if (session.LanEnabled)
            {
                session.LanServer = new LanStreamingServer(session);
                session.LanServer.Start();
            }
            return session;
        }
        catch
        {
            _sessions.TryRemove(session.Id, out _);
            _hotkeys.Remove(session.Id);
            if (session.Chat is not null)
            {
                try { session.Chat.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
                session.Chat = null;
            }
            if (session.LanServer is not null)
            {
                try { session.LanServer.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
                session.LanServer = null;
            }
            throw;
        }
    }

    public bool TryGet(Guid id, out MediaSession session) => _sessions.TryGetValue(id, out session!);

    public bool Stop(Guid id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        session.StoppedUtc = DateTimeOffset.UtcNow;
        _hotkeys.Remove(id);
        _encoder.Stop(session);
        session.CompleteIngestSubscribers();
        if (session.Chat is not null)
        {
            try { session.Chat.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
            session.Chat = null;
        }
        try { session.WebRtc.CloseAsync().GetAwaiter().GetResult(); } catch { }
        if (session.LanServer is not null)
        {
            try { session.LanServer.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
            session.LanServer = null;
        }
        return true;
    }

    public IReadOnlyList<MediaHostHotkeyEvent> DrainEvents(Guid sessionId) => _hotkeys.Drain(sessionId);

    public bool SetOutput(Guid sessionId, Guid outputId, bool enabled)
    {
        if (!TryGet(sessionId, out var session)) return false;
        session.Outputs[outputId] = enabled;
        session.Encoder?.SetOutput(outputId, enabled);
        return true;
    }

    public bool SetRecording(Guid sessionId, bool enabled)
    {
        if (!TryGet(sessionId, out var session)) return false;
        session.Recording = enabled;
        session.RecordingDefinition.Enabled = enabled;
        session.Encoder?.SetRecording(enabled);
        return true;
    }

    public bool SetProgramPage(Guid sessionId, Guid pageId)
    {
        if (!TryGet(sessionId, out var session)) return false;
        session.ProgramPageId = pageId;
        return true;
    }

    public bool AnnounceIngest(Guid sessionId, Guid? outputId, IngestAnnouncement announcement)
    {
        if (!TryGet(sessionId, out var session)) return false;
        session.SetIngest(outputId, announcement with { OutputId = outputId });
        _encoder.Attach(session, outputId);
        return true;
    }

    public bool PushIngest(Guid sessionId, Guid? outputId, byte[] chunk)
    {
        if (!TryGet(sessionId, out var session)) return false;
        if (outputId is null) session.PublishIngestChunk(chunk);
        session.Encoder?.PushChunk(outputId, chunk);
        return true;
    }

    public void Dispose()
    {
        foreach (var sessionId in _sessions.Keys.ToArray()) Stop(sessionId);
    }
}
