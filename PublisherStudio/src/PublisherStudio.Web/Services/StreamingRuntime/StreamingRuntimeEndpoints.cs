using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;

public static class PublisherStreamingRuntimeExtensions
{
    public static IServiceCollection AddPublisherStreamingRuntime(this IServiceCollection services)
    {
        services.AddSingleton<GlobalHotkeyService>();
        services.AddHostedService(provider => provider.GetRequiredService<GlobalHotkeyService>());
        services.AddSingleton<EncoderOrchestrator>();
        services.AddSingleton<NativeCaptureRegistry>();
        services.AddSingleton<MediaSessionRegistry>();
        return services;
    }

    public static WebApplication MapPublisherStreamingRuntime(this WebApplication app)
    {
        app.MapGet("/api/mediahost/capabilities", () => Results.Ok(new
        {
            version = "1.0.59",
            browserCapture = true,
            browserAudioMix = true,
            nativeDeviceDiscovery = true,
            nativeCameraCapture = true,
            processAudioLoopback = OperatingSystem.IsWindows(),
            browserWindowAudioFallback = true,
            deviceTimestamps = true,
            globalHotkeys = OperatingSystem.IsWindows(),
            recording = true,
            transports = new[] { "rtmp", "rtmps", "srt", "hls", "rtsp", "webrtc", "browser-webm" },
            hardwareEncoderProbe = true,
            note = "The integrated PublisherStudio streaming runtime owns encoder orchestration, recording, LAN delivery, native capture-card/device discovery and Windows global hotkeys. Windows process-tree audio loopback is built in on Windows 10 build 20348 or later; browser window-audio remains the cross-platform fallback."
        }));


        app.MapGet("/api/mediahost/devices", async (string? ffmpegPath, CancellationToken cancellationToken) =>
            Results.Ok(await NativeDeviceDiscovery.DiscoverAsync(ffmpegPath, cancellationToken)));

        app.MapPost("/api/mediahost/native-captures", (NativeCaptureRequest request, NativeCaptureRegistry registry) =>
        {
            try
            {
                var capture = registry.Create(request);
                return Results.Ok(new { captureId = capture.Id, mimeType = capture.MimeType, status = capture.Status });
            }
            catch (Exception exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        app.MapGet("/api/mediahost/native-captures/{captureId:guid}/websocket", async (HttpContext context, Guid captureId, NativeCaptureRegistry registry) =>
        {
            if (!context.WebSockets.IsWebSocketRequest || !registry.TryGet(captureId, out var capture))
            {
                context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return;
            }
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var subscription = capture.Subscribe();
            try
            {
                if (subscription.Initialization.Length > 0)
                    await socket.SendAsync(subscription.Initialization, System.Net.WebSockets.WebSocketMessageType.Binary, true, context.RequestAborted);
                await foreach (var chunk in subscription.Reader.ReadAllAsync(context.RequestAborted))
                {
                    if (socket.State != System.Net.WebSockets.WebSocketState.Open) break;
                    await socket.SendAsync(chunk, System.Net.WebSockets.WebSocketMessageType.Binary, true, context.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (System.Net.WebSockets.WebSocketException) { }
            finally
            {
                capture.Unsubscribe(subscription.Id);
                if (socket.State is System.Net.WebSockets.WebSocketState.Open or System.Net.WebSockets.WebSocketState.CloseReceived)
                    try { await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Native capture ended", CancellationToken.None); } catch { }
            }
        });

        app.MapDelete("/api/mediahost/native-captures/{captureId:guid}", (Guid captureId, NativeCaptureRegistry registry) =>
            registry.Stop(captureId) ? Results.NoContent() : Results.NotFound());

        app.MapPost("/api/mediahost/sessions", (JsonElement request, MediaSessionRegistry registry) =>
        {
            try
            {
                var session = registry.Create(request);
                return Results.Ok(new { sessionId = session.Id, status = session.DryRun ? "dry-run" : "prepared" });
            }
            catch (Exception exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}", (Guid sessionId, MediaSessionRegistry registry) =>
            registry.TryGet(sessionId, out var session) ? Results.Ok(session.PublicView()) : Results.NotFound());

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}/events", (Guid sessionId, MediaSessionRegistry registry) =>
            registry.TryGet(sessionId, out _) ? Results.Ok(registry.DrainEvents(sessionId)) : Results.NotFound());

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}/chat/{outputId:guid}/websocket", async (HttpContext context, Guid sessionId, Guid outputId, MediaSessionRegistry registry) =>
        {
            if (!context.WebSockets.IsWebSocketRequest || !registry.TryGet(sessionId, out var session) || session.Chat is null)
            {
                context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return;
            }
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await session.Chat.RunSubscriberAsync(outputId, socket, context.RequestAborted);
            if (socket.State is System.Net.WebSockets.WebSocketState.Open or System.Net.WebSockets.WebSocketState.CloseReceived)
                try { await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Chat subscription ended", CancellationToken.None); } catch { }
        });

        app.MapPost("/api/mediahost/sessions/{sessionId:guid}/chat/{outputId:guid}/send", async (Guid sessionId, Guid outputId, ChatSendRequest request, MediaSessionRegistry registry, CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(sessionId, out var session) || session.Chat is null) return Results.NotFound();
            try
            {
                return await session.Chat.SendAsync(outputId, request.Message, cancellationToken)
                    ? Results.Accepted()
                    : Results.BadRequest(new { error = "Chat is not configured for this output." });
            }
            catch (Exception exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        app.MapDelete("/api/mediahost/sessions/{sessionId:guid}", (Guid sessionId, MediaSessionRegistry registry) =>
            registry.Stop(sessionId) ? Results.NoContent() : Results.NotFound());

        app.MapPut("/api/mediahost/sessions/{sessionId:guid}/outputs/{outputId:guid}", (Guid sessionId, Guid outputId, ToggleRequest request, MediaSessionRegistry registry) =>
            registry.SetOutput(sessionId, outputId, request.Enabled) ? Results.NoContent() : Results.NotFound());

        app.MapPut("/api/mediahost/sessions/{sessionId:guid}/recording", (Guid sessionId, ToggleRequest request, MediaSessionRegistry registry) =>
            registry.SetRecording(sessionId, request.Enabled) ? Results.NoContent() : Results.NotFound());

        app.MapPut("/api/mediahost/sessions/{sessionId:guid}/program-page", (Guid sessionId, ProgramPageRequest request, MediaSessionRegistry registry) =>
            registry.SetProgramPage(sessionId, request.PageId) ? Results.NoContent() : Results.NotFound());

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}/ingest/websocket", async (HttpContext context, Guid sessionId, Guid? outputId, MediaSessionRegistry registry) =>
        {
            if (!context.WebSockets.IsWebSocketRequest || !registry.TryGet(sessionId, out _))
            {
                context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return;
            }
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var buffer = new byte[1024 * 1024];
            var message = new MemoryStream();
            try
            {
                while (socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) break;
                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage) continue;
                    var payload = message.ToArray();
                    message.SetLength(0);
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                    {
                        var announcement = JsonSerializer.Deserialize<IngestAnnouncement>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (announcement is not null) registry.AnnounceIngest(sessionId, outputId ?? announcement.OutputId, announcement);
                    }
                    else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                    {
                        registry.PushIngest(sessionId, outputId, payload);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (System.Net.WebSockets.WebSocketException) { }
            finally
            {
                message.Dispose();
                if (socket.State is System.Net.WebSockets.WebSocketState.Open or System.Net.WebSockets.WebSocketState.CloseReceived)
                    await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Ingest closed", CancellationToken.None);
            }
        });

        app.MapPost("/api/mediahost/sessions/{sessionId:guid}/ingest/announce", (Guid sessionId, IngestAnnouncement announcement, MediaSessionRegistry registry) =>
            registry.AnnounceIngest(sessionId, announcement.OutputId, announcement) ? Results.Accepted() : Results.NotFound());

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}/webrtc/publisher", async (HttpContext context, Guid sessionId, MediaSessionRegistry registry) =>
        {
            if (!context.WebSockets.IsWebSocketRequest || !registry.TryGet(sessionId, out var session) || !session.LanDefinition.EnableBrowserWebRtc)
            {
                context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return;
            }
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await session.WebRtc.RunPublisherAsync(socket, context.RequestAborted);
        });

        app.MapGet("/api/mediahost/sessions/{sessionId:guid}/lan", (Guid sessionId, MediaSessionRegistry registry) =>
        {
            if (!registry.TryGet(sessionId, out var session)) return Results.NotFound();
            return Results.Ok(new
            {
                sessionId,
                enabled = session.LanEnabled,
                status = session.LanServer?.Status ?? "disabled",
                error = session.LanServer?.LastError ?? string.Empty,
                browserUrl = session.LanServer?.BrowserUrl,
                hlsUrl = session.LanServer?.HlsUrl,
                rtspUrl = string.IsNullOrWhiteSpace(session.RtspUrl) ? session.LanServer?.RtspUrl : session.RtspUrl,
                accessToken = session.LanServer?.AccessToken
            });
        });

        app.MapGet("/stream/{sessionId:guid}/{**asset}", (Guid sessionId, string? asset, MediaSessionRegistry registry) =>
        {
            if (!registry.TryGet(sessionId, out var session) || string.IsNullOrWhiteSpace(session.HlsDirectory)) return Results.NotFound();
            var root = Path.GetFullPath(session.HlsDirectory);
            var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            var relative = string.IsNullOrWhiteSpace(asset) ? "index.m3u8" : asset.Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate)) return Results.NotFound();
            var contentType = Path.GetExtension(candidate).ToLowerInvariant() switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".m4s" => "video/iso.segment",
                _ => "application/octet-stream"
            };
            return Results.File(candidate, contentType, enableRangeProcessing: true);
        });

        app.MapGet("/watch/{sessionId:guid}", (Guid sessionId, MediaSessionRegistry registry) =>
        {
            if (!registry.TryGet(sessionId, out var session) || !session.LanEnabled) return Results.NotFound();
            return Results.Content($$"""
        <!doctype html><html><head><meta charset="utf-8"><title>PublisherStudio stream</title>
        <style>html,body{margin:0;background:#050b16;color:#fff;font:16px system-ui;height:100%}main{display:grid;place-items:center;height:100%}section{text-align:center;max-width:44rem}code{color:#93c5fd}</style></head>
        <body><main><section><h1>{{WebUtility.HtmlEncode(session.Name)}}</h1><p>The LAN output is prepared. The renderer/encoder must announce its WebRTC or HLS ingest before playback starts.</p><p>Session <code>{{sessionId:D}}</code></p></section></main></body></html>
        """, "text/html");
        });

        app.MapGet("/api/mediahost/now-playing", (string? directory) =>
        {
            if (string.IsNullOrWhiteSpace(directory)) return Results.BadRequest(new { error = "A directory is required." });
            var metadata = NowPlayingReader.Read(directory);
            return metadata is null ? Results.NoContent() : Results.Ok(metadata);
        });

        return app;
    }
}

public sealed record ToggleRequest(bool Enabled);
public sealed record ProgramPageRequest(Guid PageId);
public sealed record IngestAnnouncement(string Kind, string Url, string Codec, int Width, int Height, int FrameRate, Guid? OutputId = null);
public sealed record MediaHostHotkeyEvent(string Command, Guid? TargetId, DateTimeOffset TriggeredUtc);
public sealed record MediaHotkey(Guid Id, string Gesture, string Command, Guid? TargetId, bool Global);

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

public sealed class MediaSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Publication";
    public bool DryRun { get; init; }
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StoppedUtc { get; set; }
    public bool Recording { get; set; }
    public Guid? ProgramPageId { get; set; }
    public bool LanEnabled => LanDefinition.Enabled;
    public MediaLanDefinition LanDefinition { get; } = new();
    public LanStreamingServer? LanServer { get; set; }
    public int MasterWidth { get; init; } = 3840;
    public int MasterHeight { get; init; } = 2160;
    public int MasterFrameRate { get; init; } = 60;
    public bool PreferDeviceTimestamps { get; init; } = true;
    public string FfmpegPath { get; init; } = string.Empty;
    public int HardwareEncoder { get; init; }
    public ConcurrentDictionary<Guid, bool> Outputs { get; } = new();
    public List<MediaOutputDefinition> OutputDefinitions { get; } = [];
    public MediaRecordingDefinition RecordingDefinition { get; } = new();
    public List<MediaHotkey> Hotkeys { get; } = [];
    public IngestAnnouncement? Ingest { get; private set; }
    public ConcurrentDictionary<Guid, IngestAnnouncement> OutputIngests { get; } = new();
    public EncoderSessionController? Encoder { get; set; }
    public string HlsDirectory { get; set; } = string.Empty;
    public string RtspUrl { get; set; } = string.Empty;
    public int RtspRelayPort { get; set; }
    public WebRtcSignalingHub WebRtc { get; } = new();
    public PlatformChatHub? Chat { get; set; }

    private readonly object _ingestSubscriberSync = new();
    private readonly Dictionary<Guid, Channel<byte[]>> _ingestSubscribers = [];
    private byte[]? _webmInitializationChunk;

    public void SetIngest(Guid? outputId, IngestAnnouncement announcement)
    {
        if (outputId is { } id) OutputIngests[id] = announcement;
        else Ingest = announcement;
    }

    public IngestAnnouncement? GetIngest(Guid? outputId) =>
        outputId is { } id && OutputIngests.TryGetValue(id, out var outputIngest) ? outputIngest : outputId is null ? Ingest : null;

    public bool HasIngest(Guid? outputId) => GetIngest(outputId) is not null;

    public (Guid Id, byte[]? InitializationChunk, ChannelReader<byte[]> Reader) SubscribeIngest()
    {
        lock (_ingestSubscriberSync)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(180)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _ingestSubscribers[id] = channel;
            return (id, _webmInitializationChunk?.ToArray(), channel.Reader);
        }
    }

    public void UnsubscribeIngest(Guid id)
    {
        Channel<byte[]>? channel;
        lock (_ingestSubscriberSync)
        {
            if (!_ingestSubscribers.Remove(id, out channel)) return;
        }
        channel.Writer.TryComplete();
    }

    public void PublishIngestChunk(byte[] chunk)
    {
        if (chunk.Length == 0) return;
        ChannelWriter<byte[]>[] writers;
        lock (_ingestSubscriberSync)
        {
            _webmInitializationChunk ??= chunk.ToArray();
            writers = _ingestSubscribers.Values.Select(item => item.Writer).ToArray();
        }
        foreach (var writer in writers) writer.TryWrite(chunk);
    }

    public void CompleteIngestSubscribers()
    {
        Channel<byte[]>[] channels;
        lock (_ingestSubscriberSync)
        {
            channels = _ingestSubscribers.Values.ToArray();
            _ingestSubscribers.Clear();
            _webmInitializationChunk = null;
        }
        foreach (var channel in channels) channel.Writer.TryComplete();
    }

    public static MediaSession From(JsonElement request)
    {
        var session = new MediaSession
        {
            Name = ReadString(request, "publicationName") ?? "Publication",
            DryRun = ReadBool(request, "dryRun"),
            Recording = request.TryGetProperty("recording", out var recording) && ReadBool(recording, "enabled"),
            MasterWidth = ReadInt(request, "masterWidth", 3840),
            MasterHeight = ReadInt(request, "masterHeight", 2160),
            MasterFrameRate = ReadInt(request, "masterFrameRate", 60),
            PreferDeviceTimestamps = !request.TryGetProperty("preferDeviceTimestamps", out var timestamps) || timestamps.ValueKind != JsonValueKind.False,
            FfmpegPath = ReadString(request, "ffmpegPath") ?? string.Empty,
            HardwareEncoder = ReadInt(request, "hardwareEncoder")
        };
        if (request.TryGetProperty("outputs", out var outputs) && outputs.ValueKind == JsonValueKind.Array)
            foreach (var output in outputs.EnumerateArray())
            {
                if (!output.TryGetProperty("outputId", out var id) || !id.TryGetGuid(out var outputId)) continue;
                session.Outputs[outputId] = !output.TryGetProperty("enabled", out var enabledProperty) || enabledProperty.ValueKind != JsonValueKind.False;
                session.OutputDefinitions.Add(new MediaOutputDefinition
                {
                    OutputId = outputId,
                    Name = ReadString(output, "name") ?? "Output",
                    Provider = ReadInt(output, "provider"),
                    Transport = ReadInt(output, "transport"),
                    Endpoint = ReadString(output, "endpoint") ?? string.Empty,
                    ChannelId = ReadString(output, "channelId") ?? string.Empty,
                    AccountName = ReadString(output, "accountName") ?? string.Empty,
                    Secret = ReadString(output, "secret") ?? string.Empty,
                    ChatEnabled = ReadBool(output, "chatEnabled"),
                    ChatSecret = ReadString(output, "chatSecret") ?? string.Empty,
                    TestMode = ReadBool(output, "testMode"),
                    Width = ReadInt(output, "width", 1920),
                    Height = ReadInt(output, "height", 1080),
                    FrameRate = ReadInt(output, "frameRate", 60),
                    VideoBitrateKbps = ReadInt(output, "videoBitrateKbps", 6000),
                    AudioBitrateKbps = ReadInt(output, "audioBitrateKbps", 160),
                    KeyFrameIntervalSeconds = ReadInt(output, "keyFrameIntervalSeconds", 2),
                    VideoCodec = ReadInt(output, "videoCodec"),
                    AudioCodec = ReadInt(output, "audioCodec")
                });
            }
        if (request.TryGetProperty("recording", out var recordingSettings))
        {
            session.RecordingDefinition.Enabled = ReadBool(recordingSettings, "enabled");
            session.RecordingDefinition.DestinationDirectory = ReadString(recordingSettings, "destinationDirectory") ?? string.Empty;
            session.RecordingDefinition.Variant = ReadInt(recordingSettings, "variant");
            session.RecordingDefinition.Container = ReadString(recordingSettings, "container") ?? "mkv";
            session.RecordingDefinition.SegmentSeconds = ReadInt(recordingSettings, "segmentSeconds", 10);
            session.RecordingDefinition.RemuxToMp4AfterStop = ReadBool(recordingSettings, "remuxToMp4AfterStop");
            if (recordingSettings.TryGetProperty("selectedOutputIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
                foreach (var value in selected.EnumerateArray()) if (value.TryGetGuid(out var selectedId)) session.RecordingDefinition.SelectedOutputIds.Add(selectedId);
        }
        if (request.TryGetProperty("lan", out var lanSettings))
        {
            session.LanDefinition.Enabled = ReadBool(lanSettings, "enabled");
            session.LanDefinition.BindAddress = ReadString(lanSettings, "bindAddress") ?? "127.0.0.1";
            session.LanDefinition.Port = Math.Clamp(ReadInt(lanSettings, "port", 17848), 1024, 65535);
            session.LanDefinition.Width = Math.Clamp(ReadInt(lanSettings, "width", 1920), 320, 7680);
            session.LanDefinition.Height = Math.Clamp(ReadInt(lanSettings, "height", 1080), 180, 4320);
            session.LanDefinition.FrameRate = Math.Clamp(ReadInt(lanSettings, "frameRate", 60), 15, 120);
            session.LanDefinition.VideoBitrateKbps = Math.Clamp(ReadInt(lanSettings, "videoBitrateKbps", 8000), 250, 200000);
            session.LanDefinition.EnableBrowserWebRtc = !lanSettings.TryGetProperty("enableBrowserWebRtc", out var browserPlayback) || browserPlayback.ValueKind != JsonValueKind.False;
            session.LanDefinition.EnableHls = !lanSettings.TryGetProperty("enableHls", out var hls) || hls.ValueKind != JsonValueKind.False;
            session.LanDefinition.EnableRtsp = ReadBool(lanSettings, "enableRtsp");
            session.LanDefinition.RtspPort = Math.Clamp(ReadInt(lanSettings, "rtspPort", 8554), 1024, 65535);
            session.LanDefinition.RequireAccessToken = !lanSettings.TryGetProperty("requireAccessToken", out var tokenRequired) || tokenRequired.ValueKind != JsonValueKind.False;
            session.LanDefinition.ViewerLimit = Math.Clamp(ReadInt(lanSettings, "viewerLimit", 50), 1, 10_000);
        }
        if (request.TryGetProperty("hotkeys", out var hotkeys) && hotkeys.ValueKind == JsonValueKind.Array)
            foreach (var item in hotkeys.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProperty) && idProperty.TryGetGuid(out var parsedId) ? parsedId : Guid.NewGuid();
                var targetId = item.TryGetProperty("targetId", out var targetProperty) && targetProperty.TryGetGuid(out var parsedTarget) ? parsedTarget : (Guid?)null;
                session.Hotkeys.Add(new MediaHotkey(
                    id,
                    ReadString(item, "gesture") ?? string.Empty,
                    ReadString(item, "command") ?? string.Empty,
                    targetId,
                    ReadBool(item, "global")));
            }
        return session;
    }

    public object PublicView() => new
    {
        id = Id,
        name = Name,
        dryRun = DryRun,
        startedUtc = StartedUtc,
        recording = Recording,
        programPageId = ProgramPageId,
        lanEnabled = LanEnabled,
        lanStatus = LanServer?.Status,
        lanBrowserUrl = LanServer?.BrowserUrl,
        lanHlsUrl = LanServer?.HlsUrl,
        lanRtspUrl = RtspUrl,
        outputs = Outputs,
        hotkeyCount = Hotkeys.Count,
        ingest = Ingest,
        outputIngests = OutputIngests.Keys,
        chatStatus = Chat?.Status,
        encoderStatus = Encoder?.Status,
        encoderError = Encoder?.LastError,
        status = Ingest is null && OutputIngests.IsEmpty ? "waiting-for-renderer" : Encoder?.Status ?? "ready"
    };

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static bool ReadBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False && property.GetBoolean();
    private static int ReadInt(JsonElement element, string name, int fallback = 0) =>
        element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value) ? value : fallback;
}

public sealed class GlobalHotkeyService : IHostedService, IDisposable
{
    private const uint WmHotkey = 0x0312;
    private const uint WmCommand = 0x8000 + 47;
    private const uint WmQuit = 0x0012;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    private readonly ConcurrentQueue<Action> _commands = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<MediaHostHotkeyEvent>> _events = new();
    private readonly Dictionary<int, RegisteredHotkey> _registered = [];
    private readonly ManualResetEventSlim _started = new(false);
    private Thread? _thread;
    private uint _threadId;
    private int _nextNativeId = 100;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return Task.CompletedTask;
        _thread = new Thread(MessageLoop) { IsBackground = true, Name = "PublisherStudio global hotkeys" };
        _thread.Start();
        _started.Wait(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_thread is null) return Task.CompletedTask;
        PostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        _thread.Join(TimeSpan.FromSeconds(2));
        return Task.CompletedTask;
    }

    public void Configure(Guid sessionId, IEnumerable<MediaHotkey> hotkeys)
    {
        if (!OperatingSystem.IsWindows()) return;
        Enqueue(() =>
        {
            RemoveCore(sessionId);
            foreach (var hotkey in hotkeys.Where(item => !string.IsNullOrWhiteSpace(item.Gesture) && !string.IsNullOrWhiteSpace(item.Command)))
            {
                if (!TryParseGesture(hotkey.Gesture, out var modifiers, out var virtualKey)) continue;
                var nativeId = Interlocked.Increment(ref _nextNativeId);
                if (!RegisterHotKey(IntPtr.Zero, nativeId, modifiers | ModNoRepeat, virtualKey)) continue;
                _registered[nativeId] = new RegisteredHotkey(sessionId, hotkey.Command, hotkey.TargetId);
                _events.TryAdd(sessionId, new ConcurrentQueue<MediaHostHotkeyEvent>());
            }
        });
    }

    public void Remove(Guid sessionId)
    {
        if (!OperatingSystem.IsWindows()) return;
        Enqueue(() => RemoveCore(sessionId));
        _events.TryRemove(sessionId, out _);
    }

    public IReadOnlyList<MediaHostHotkeyEvent> Drain(Guid sessionId)
    {
        if (!_events.TryGetValue(sessionId, out var queue)) return [];
        var result = new List<MediaHostHotkeyEvent>();
        while (result.Count < 100 && queue.TryDequeue(out var item)) result.Add(item);
        return result;
    }

    private void Enqueue(Action command)
    {
        _commands.Enqueue(command);
        if (_threadId != 0) PostThreadMessage(_threadId, WmCommand, UIntPtr.Zero, IntPtr.Zero);
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _started.Set();
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Message == WmCommand)
            {
                while (_commands.TryDequeue(out var command)) command();
                continue;
            }
            if (message.Message != WmHotkey) continue;
            var nativeId = unchecked((int)message.WParam.ToUInt64());
            if (!_registered.TryGetValue(nativeId, out var hotkey)) continue;
            var queue = _events.GetOrAdd(hotkey.SessionId, _ => new ConcurrentQueue<MediaHostHotkeyEvent>());
            queue.Enqueue(new MediaHostHotkeyEvent(hotkey.Command, hotkey.TargetId, DateTimeOffset.UtcNow));
        }
        foreach (var nativeId in _registered.Keys.ToArray()) UnregisterHotKey(IntPtr.Zero, nativeId);
        _registered.Clear();
    }

    private void RemoveCore(Guid sessionId)
    {
        foreach (var pair in _registered.Where(pair => pair.Value.SessionId == sessionId).ToArray())
        {
            UnregisterHotKey(IntPtr.Zero, pair.Key);
            _registered.Remove(pair.Key);
        }
    }

    private static bool TryParseGesture(string gesture, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts[..^1])
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= ModControl;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= ModAlt;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= ModShift;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Meta", StringComparison.OrdinalIgnoreCase)) modifiers |= ModWin;
        }
        var key = parts[^1];
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            virtualKey = char.ToUpperInvariant(key[0]);
            return true;
        }
        if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(key[1..], out var functionKey) && functionKey is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + functionKey - 1);
            return true;
        }
        virtualKey = key.ToUpperInvariant() switch
        {
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "END" => 0x23,
            "HOME" => 0x24,
            "ARROWLEFT" or "LEFT" => 0x25,
            "ARROWUP" or "UP" => 0x26,
            "ARROWRIGHT" or "RIGHT" => 0x27,
            "ARROWDOWN" or "DOWN" => 0x28,
            "INSERT" => 0x2D,
            "DELETE" or "DEL" => 0x2E,
            "SPACE" => 0x20,
            "ESCAPE" or "ESC" => 0x1B,
            _ => 0
        };
        return virtualKey != 0;
    }

    public void Dispose()
    {
        try { StopAsync(CancellationToken.None).GetAwaiter().GetResult(); } catch { }
        _started.Dispose();
    }

    private sealed record RegisteredHotkey(Guid SessionId, string Command, Guid? TargetId);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr HWnd;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter);

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter, uint removeMessage);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}


public static class NowPlayingReader
{
    private static readonly string[] SupportedExtensions = [".mp3", ".flac", ".m4a", ".aac", ".wav", ".ogg", ".opus"];

    public static object? Read(string directory)
    {
        try
        {
            if (!Directory.Exists(directory)) return null;
            var file = new DirectoryInfo(directory)
                .EnumerateFiles()
                .Where(item => SupportedExtensions.Contains(item.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(item => item.LastWriteTimeUtc)
                .FirstOrDefault();
            if (file is null) return null;
            var tags = file.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ? ReadMp3(file.FullName) : new AudioTags();
            var fallbackTitle = Path.GetFileNameWithoutExtension(file.Name);
            return new
            {
                fileName = file.Name,
                fullPath = file.FullName,
                title = string.IsNullOrWhiteSpace(tags.Title) ? fallbackTitle : tags.Title,
                artist = tags.Artist,
                album = tags.Album,
                year = tags.Year,
                track = tags.Track,
                genre = tags.Genre,
                coverImage = tags.CoverImage,
                lastWriteUtc = file.LastWriteTimeUtc
            };
        }
        catch { return null; }
    }

    private static AudioTags ReadMp3(string path)
    {
        var tags = new AudioTags();
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length >= 10)
        {
            Span<byte> header = stackalloc byte[10];
            stream.ReadExactly(header);
            if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
            {
                var version = header[3];
                var tagSize = Synchsafe(header[6], header[7], header[8], header[9]);
                ReadId3V2(stream, Math.Min(tagSize, (int)Math.Min(stream.Length - 10, 16 * 1024 * 1024)), version, tags);
            }
        }
        if (stream.Length >= 128)
        {
            stream.Position = stream.Length - 128;
            var legacy = new byte[128];
            stream.ReadExactly(legacy);
            if (legacy[0] == (byte)'T' && legacy[1] == (byte)'A' && legacy[2] == (byte)'G')
            {
                tags.Title = First(tags.Title, DecodeLatin1(legacy.AsSpan(3, 30)));
                tags.Artist = First(tags.Artist, DecodeLatin1(legacy.AsSpan(33, 30)));
                tags.Album = First(tags.Album, DecodeLatin1(legacy.AsSpan(63, 30)));
                tags.Year = First(tags.Year, DecodeLatin1(legacy.AsSpan(93, 4)));
                if (string.IsNullOrWhiteSpace(tags.Track) && legacy[125] == 0 && legacy[126] != 0) tags.Track = legacy[126].ToString();
                if (string.IsNullOrWhiteSpace(tags.Genre) && legacy[127] != 255) tags.Genre = legacy[127].ToString();
            }
        }
        return tags;
    }

    private static void ReadId3V2(Stream stream, int tagSize, byte version, AudioTags tags)
    {
        var remaining = tagSize;
        var frameHeaderSize = version == 2 ? 6 : 10;
        while (remaining >= frameHeaderSize)
        {
            var header = new byte[frameHeaderSize];
            stream.ReadExactly(header);
            remaining -= frameHeaderSize;
            if (header.All(value => value == 0)) break;
            var frameId = System.Text.Encoding.ASCII.GetString(header, 0, version == 2 ? 3 : 4);
            var size = version switch
            {
                2 => header[3] << 16 | header[4] << 8 | header[5],
                4 => Synchsafe(header[4], header[5], header[6], header[7]),
                _ => ReadBigEndianInt(header.AsSpan(4, 4))
            };
            if (size <= 0 || size > remaining || size > 8 * 1024 * 1024) break;
            var payload = new byte[size];
            stream.ReadExactly(payload);
            remaining -= size;
            switch (frameId)
            {
                case "TIT2" or "TT2": tags.Title = First(tags.Title, DecodeTextFrame(payload)); break;
                case "TPE1" or "TP1": tags.Artist = First(tags.Artist, DecodeTextFrame(payload)); break;
                case "TALB" or "TAL": tags.Album = First(tags.Album, DecodeTextFrame(payload)); break;
                case "TYER" or "TDRC" or "TYE": tags.Year = First(tags.Year, DecodeTextFrame(payload)); break;
                case "TRCK" or "TRK": tags.Track = First(tags.Track, DecodeTextFrame(payload)); break;
                case "TCON" or "TCO": tags.Genre = First(tags.Genre, DecodeTextFrame(payload)); break;
                case "APIC" or "PIC": tags.CoverImage = First(tags.CoverImage, DecodePictureFrame(payload, version)); break;
            }
        }
    }

    private static string DecodeTextFrame(byte[] payload)
    {
        if (payload.Length < 2) return string.Empty;
        return DecodeEncodedText(payload[0], payload.AsSpan(1));
    }

    private static string DecodePictureFrame(byte[] payload, byte version)
    {
        if (payload.Length < 8) return string.Empty;
        var encoding = payload[0];
        var index = 1;
        string mime;
        if (version == 2)
        {
            mime = payload.Length >= 4 ? System.Text.Encoding.ASCII.GetString(payload, 1, 3) : "jpeg";
            mime = mime.Equals("PNG", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            index = 4;
        }
        else
        {
            var mimeEnd = Array.IndexOf(payload, (byte)0, index);
            if (mimeEnd < 0) return string.Empty;
            mime = System.Text.Encoding.ASCII.GetString(payload, index, mimeEnd - index);
            index = mimeEnd + 1;
        }
        if (index >= payload.Length) return string.Empty;
        index++; // picture type
        index = SkipTerminatedText(payload, index, encoding);
        if (index >= payload.Length) return string.Empty;
        return $"data:{(string.IsNullOrWhiteSpace(mime) ? "image/jpeg" : mime)};base64,{Convert.ToBase64String(payload.AsSpan(index))}";
    }

    private static int SkipTerminatedText(byte[] bytes, int index, byte encoding)
    {
        var doubleNull = encoding is 1 or 2;
        while (index < bytes.Length)
        {
            if (!doubleNull && bytes[index] == 0) return index + 1;
            if (doubleNull && index + 1 < bytes.Length && bytes[index] == 0 && bytes[index + 1] == 0) return index + 2;
            index += doubleNull ? 2 : 1;
        }
        return bytes.Length;
    }

    private static string DecodeEncodedText(byte encoding, ReadOnlySpan<byte> bytes)
    {
        try
        {
            var value = encoding switch
            {
                0 => System.Text.Encoding.Latin1.GetString(bytes),
                1 => System.Text.Encoding.Unicode.GetString(bytes),
                2 => System.Text.Encoding.BigEndianUnicode.GetString(bytes),
                3 => System.Text.Encoding.UTF8.GetString(bytes),
                _ => System.Text.Encoding.UTF8.GetString(bytes)
            };
            return value.Trim('\0', ' ', '\r', '\n', '\t', '\ufeff');
        }
        catch { return string.Empty; }
    }

    private static string DecodeLatin1(ReadOnlySpan<byte> bytes) =>
        System.Text.Encoding.Latin1.GetString(bytes).Trim('\0', ' ', '\r', '\n', '\t');

    private static int Synchsafe(byte a, byte b, byte c, byte d) => (a & 0x7f) << 21 | (b & 0x7f) << 14 | (c & 0x7f) << 7 | d & 0x7f;
    private static int ReadBigEndianInt(ReadOnlySpan<byte> bytes) => bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
    private static string First(string current, string fallback) => string.IsNullOrWhiteSpace(current) ? fallback : current;

    private sealed class AudioTags
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Track { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string CoverImage { get; set; } = string.Empty;
    }
}
