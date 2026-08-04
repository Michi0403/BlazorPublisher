using PublisherStudio.Services.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.Streaming.Sessions;

public sealed class MediaSessionRegistry(
    GlobalHotkeyService hotkeys,
    EncoderOrchestrator encoder,
    IMediaSessionFactory mediaSessionFactory,
    IPlatformChatServiceFactory chatFactory,
    ILanStreamingServerFactory lanServerFactory,
    ILogger<MediaSessionRegistry> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, MediaSession> _sessions = new();
    private readonly GlobalHotkeyService _hotkeys = hotkeys;
    private readonly EncoderOrchestrator _encoder = encoder;

    public MediaSession Create(JsonElement request)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.Create.");
                    var session = mediaSessionFactory.Create(request);
                    if (!_sessions.TryAdd(session.Id, session)) throw new InvalidOperationException("Could not register the media session.");
                    try
                    {
                        _hotkeys.Configure(session.Id, session.Hotkeys.Where(item => item.Global));
                        session.Chat = chatFactory.Create(session);
                        session.Chat.Start();
                        if (session.LanEnabled)
                        {
                            session.LanServer = lanServerFactory.Create(session);
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.Create failed: {exception.Message}");
            throw;
        }
    }

    public bool TryGet(Guid id, out MediaSession session) {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.TryGet.");
            return _sessions.TryGetValue(id, out session!);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.TryGet failed: {exception.Message}");
            throw;
        }
    }

    public bool Stop(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.Stop.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.Stop failed: {exception.Message}");
            throw;
        }
    }

    public IReadOnlyList<MediaHostHotkeyEvent> DrainEvents(Guid sessionId) {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.DrainEvents.");
            return _hotkeys.Drain(sessionId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.DrainEvents failed: {exception.Message}");
            throw;
        }
    }

    public bool SetOutput(Guid sessionId, Guid outputId, bool enabled)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.SetOutput.");
                    if (!TryGet(sessionId, out var session)) return false;
                    session.Outputs[outputId] = enabled;
                    session.Encoder?.SetOutput(outputId, enabled);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.SetOutput failed: {exception.Message}");
            throw;
        }
    }

    public bool SetRecording(Guid sessionId, bool enabled)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.SetRecording.");
                    if (!TryGet(sessionId, out var session)) return false;
                    session.Recording = enabled;
                    session.RecordingDefinition.Enabled = enabled;
                    session.Encoder?.SetRecording(enabled);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.SetRecording failed: {exception.Message}");
            throw;
        }
    }

    public bool SetProgramPage(Guid sessionId, Guid pageId)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.SetProgramPage.");
                    if (!TryGet(sessionId, out var session)) return false;
                    session.ProgramPageId = pageId;
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.SetProgramPage failed: {exception.Message}");
            throw;
        }
    }

    public bool AnnounceIngest(Guid sessionId, Guid? outputId, IngestAnnouncement announcement)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.AnnounceIngest.");
                    if (!TryGet(sessionId, out var session)) return false;
                    session.SetIngest(outputId, announcement with { OutputId = outputId });
                    _encoder.Attach(session, outputId);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.AnnounceIngest failed: {exception.Message}");
            throw;
        }
    }

    public bool PushIngest(Guid sessionId, Guid? outputId, byte[] chunk)
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.PushIngest.");
                    if (!TryGet(sessionId, out var session)) return false;
                    if (outputId is null) session.PublishIngestChunk(chunk);
                    session.Encoder?.PushChunk(outputId, chunk);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.PushIngest failed: {exception.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            logger.LogTrace($"Entering MediaSessionRegistry.Dispose.");
                    foreach (var sessionId in _sessions.Keys.ToArray()) Stop(sessionId);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaSessionRegistry.Dispose failed: {exception.Message}");
            throw;
        }
    }
}
