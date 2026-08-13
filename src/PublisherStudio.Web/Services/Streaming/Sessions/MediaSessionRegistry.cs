using PublisherStudio.Services.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.Streaming.Sessions;

/// <summary>
/// Maintains the authoritative directory of media session entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="hotkeys">Global hotkey service dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="encoder">Encoder value supplied to the media session operation and used when producing its result.</param>
/// <param name="mediaSessionFactory">Media session factory dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="chatFactory">Platform chat service factory dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="lanServerFactory">Lan streaming server factory dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class MediaSessionRegistry(
    GlobalHotkeyService hotkeys,
    EncoderOrchestrator encoder,
    IMediaSessionFactory mediaSessionFactory,
    IPlatformChatServiceFactory chatFactory,
    ILanStreamingServerFactory lanServerFactory,
    ILogger<MediaSessionRegistry> logger) : IDisposable
{
    /// <summary>
    /// Stores the in-memory sessions collection maintained internally by <see cref="MediaSessionRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, MediaSession> _sessions = new();
    /// <summary>
    /// Stores the global hotkey service dependency used by <see cref="MediaSessionRegistry"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly GlobalHotkeyService _hotkeys = hotkeys;
    /// <summary>
    /// Stores the internal encoder state used by <see cref="MediaSessionRegistry"/> while executing its surrounding workflow.
    /// </summary>
    private readonly EncoderOrchestrator _encoder = encoder;

    /// <summary>
    /// Performs create in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The media session produced by the operation.</returns>
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

    /// <summary>
    /// Attempts to get in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="session">Session value supplied to the media session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs stop in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs drain events in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Sets output in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Sets recording in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Sets program page in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="pageId">Identifier of the page to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs announce ingest in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="announcement">Ingest announcement dependency used by the media session workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs push ingest in the media session directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="chunk">Chunk value supplied to the media session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Releases resources owned by <see cref="MediaSessionRegistry"/> and leaves the media session workflow in a safely disposed state.
    /// </summary>
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
