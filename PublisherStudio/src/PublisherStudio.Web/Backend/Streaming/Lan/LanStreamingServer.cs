using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Security.Cryptography;

namespace PublisherStudio.Backend.Streaming.Lan;

public sealed class LanStreamingServer : IAsyncDisposable
{
    private readonly MediaSession _session;
    private readonly SemaphoreSlim _viewerGate;
    private readonly CancellationTokenSource _cancellation = new();
    private WebApplication? _app;
    private Task? _runTask;
    private RtspLanServer? _rtspServer;

    public LanStreamingServer(MediaSession session)
    {
        _session = session;
        _viewerGate = new SemaphoreSlim(Math.Clamp(session.LanDefinition.ViewerLimit, 1, 10_000));
        AccessToken = session.LanDefinition.RequireAccessToken
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant()
            : string.Empty;
    }

    public string AccessToken { get; }
    public string Status { get; private set; } = "stopped";
    public string LastError { get; private set; } = string.Empty;

    public string AdvertisedHost
    {
        get
        {
            var configured = _session.LanDefinition.BindAddress;
            if (IPAddress.TryParse(configured, out var address)
                && !IPAddress.Any.Equals(address)
                && !IPAddress.IPv6Any.Equals(address))
                return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{address}]" : address.ToString();

            try
            {
                var candidate = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    .FirstOrDefault(item => item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(item));
                if (candidate is not null) return candidate.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }

    public string TokenQuery => string.IsNullOrWhiteSpace(AccessToken) ? string.Empty : $"?token={Uri.EscapeDataString(AccessToken)}";
    public string? BrowserUrl => _session.LanDefinition.EnableBrowserWebRtc
        ? $"http://{AdvertisedHost}:{_session.LanDefinition.Port}/watch/{_session.Id:D}{TokenQuery}"
        : null;
    public string? HlsUrl => _session.LanDefinition.EnableHls
        ? $"http://{AdvertisedHost}:{_session.LanDefinition.Port}/stream/{_session.Id:D}/index.m3u8{TokenQuery}"
        : null;
    public string? RtspUrl => _session.LanDefinition.EnableRtsp
        ? $"rtsp://{AdvertisedHost}:{_session.LanDefinition.RtspPort}/publisherstudio{TokenQuery}"
        : null;

    public void Start()
    {
        if (_runTask is not null || !_session.LanDefinition.Enabled) return;
        if (_session.LanDefinition.EnableRtsp && _rtspServer is null)
        {
            var address = ResolveAddress(_session.LanDefinition.BindAddress);
            _rtspServer = new RtspLanServer(address, _session.LanDefinition.RtspPort, AccessToken);
            _session.RtspRelayPort = _rtspServer.RtpInputPort;
            _session.RtspUrl = RtspUrl ?? string.Empty;
            _rtspServer.Start();
        }
        _runTask = Task.Run(StartCoreAsync);
    }

    private async Task StartCoreAsync()
    {
        try
        {
            var address = ResolveAddress(_session.LanDefinition.BindAddress);
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(LanStreamingServer).Assembly.GetName().Name,
                Args = []
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(address, _session.LanDefinition.Port));
            var app = builder.Build();
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (!Authorize(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "A valid PublisherStudio LAN access token is required." }, context.RequestAborted);
                    return;
                }
                if (!await _viewerGate.WaitAsync(TimeSpan.FromSeconds(2), context.RequestAborted))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new { error = "The configured LAN viewer limit has been reached." }, context.RequestAborted);
                    return;
                }
                try { await next(); }
                finally { _viewerGate.Release(); }
            });

            app.MapGet("/", () => Results.Redirect($"/watch/{_session.Id:D}{TokenQuery}"));
            app.MapGet("/health", () => Results.Ok(new { status = "ok", sessionId = _session.Id }));
            app.MapGet("/watch/{sessionId:guid}", (Guid sessionId) =>
            {
                if (sessionId != _session.Id || !_session.LanDefinition.EnableBrowserWebRtc) return Results.NotFound();
                return Results.Content(BuildWatchPage(), "text/html; charset=utf-8");
            });
            app.MapGet("/webrtc/{sessionId:guid}", async (HttpContext context, Guid sessionId) =>
            {
                if (sessionId != _session.Id || !_session.LanDefinition.EnableBrowserWebRtc || !context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                    return;
                }
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await _session.WebRtc.RunViewerAsync(socket, context.RequestAborted);
            });
            app.MapGet("/live/{sessionId:guid}", async (HttpContext context, Guid sessionId) =>
            {
                if (sessionId != _session.Id || !_session.LanDefinition.EnableBrowserWebRtc || !context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = context.WebSockets.IsWebSocketRequest ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                    return;
                }
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                var subscription = _session.SubscribeIngest();
                try
                {
                    if (subscription.InitializationChunk is { Length: > 0 } initialization)
                        await socket.SendAsync(initialization, WebSocketMessageType.Binary, true, context.RequestAborted);
                    await foreach (var chunk in subscription.Reader.ReadAllAsync(context.RequestAborted))
                    {
                        if (socket.State != WebSocketState.Open) break;
                        await socket.SendAsync(chunk, WebSocketMessageType.Binary, true, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
                catch (WebSocketException) { }
                finally
                {
                    _session.UnsubscribeIngest(subscription.Id);
                    if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "PublisherStudio stream ended", CancellationToken.None); }
                        catch { }
                    }
                }
            });
            app.MapGet("/stream/{sessionId:guid}/{**asset}", async (HttpContext context, Guid sessionId, string? asset) =>
            {
                if (sessionId != _session.Id || string.IsNullOrWhiteSpace(_session.HlsDirectory))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                var candidate = SafeAssetPath(_session.HlsDirectory, asset);
                if (candidate is null || !File.Exists(candidate))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                if (Path.GetExtension(candidate).Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    var playlist = await File.ReadAllLinesAsync(candidate, context.RequestAborted);
                    if (!string.IsNullOrWhiteSpace(AccessToken))
                    {
                        for (var index = 0; index < playlist.Length; index++)
                        {
                            var line = playlist[index].Trim();
                            if (line.Length == 0 || line.StartsWith('#')) continue;
                            playlist[index] = AppendToken(line);
                        }
                    }
                    context.Response.ContentType = "application/vnd.apple.mpegurl";
                    context.Response.Headers.CacheControl = "no-store";
                    await context.Response.WriteAsync(string.Join('\n', playlist), context.RequestAborted);
                    return;
                }
                context.Response.ContentType = Path.GetExtension(candidate).ToLowerInvariant() switch
                {
                    ".ts" => "video/mp2t",
                    ".m4s" => "video/iso.segment",
                    ".mp4" => "video/mp4",
                    _ => "application/octet-stream"
                };
                context.Response.Headers.CacheControl = "no-store";
                await using var fileStream = File.OpenRead(candidate);
                context.Response.ContentLength = fileStream.Length;
                await fileStream.CopyToAsync(context.Response.Body, context.RequestAborted);
            });

            _app = app;
            Status = "starting";
            await app.StartAsync(_cancellation.Token);
            Status = "listening";
            await app.WaitForShutdownAsync(_cancellation.Token);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            Status = "stopped";
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Status = "error";
        }
    }

    private string BuildWatchPage()
    {
        var playlist = $"/stream/{_session.Id:D}/index.m3u8{TokenQuery}";
        var websocket = $"/live/{_session.Id:D}{TokenQuery}";
        var webRtc = $"/webrtc/{_session.Id:D}{TokenQuery}";
        var hlsParagraph = _session.LanDefinition.EnableHls
            ? $"<p>VLC can open <a href=\"{WebUtility.HtmlEncode(playlist)}\">the HLS network stream</a>.</p>"
            : string.Empty;
        var rtspParagraph = _session.LanDefinition.EnableRtsp
            ? $"<p>VLC can also open <code>{WebUtility.HtmlEncode(RtspUrl)}</code>.</p>"
            : string.Empty;
        var announcedMime = string.IsNullOrWhiteSpace(_session.Ingest?.Codec)
            ? "video/webm;codecs=vp9,opus"
            : _session.Ingest.Codec;
        var mimeJson = JsonSerializer.Serialize(announcedMime);
        return $$"""
<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{{WebUtility.HtmlEncode(_session.Name)}}</title>
<style>html,body{margin:0;background:#050b16;color:#e5eefc;font:16px system-ui;height:100%}main{display:grid;place-items:center;min-height:100%}section{width:min(96vw,1200px)}video{display:block;width:100%;background:#000;box-shadow:0 18px 60px #0008}p{color:#a9bad2}a,code{color:#7dd3fc}#status[data-state=error]{color:#fda4af}</style></head>
<body><main><section><h1>{{WebUtility.HtmlEncode(_session.Name)}}</h1><video id="program" controls autoplay playsinline></video><p id="status">Connecting to PublisherStudio…</p>{{hlsParagraph}}{{rtspParagraph}}</section></main>
<script>
(() => {
  const video = document.getElementById('program');
  const status = document.getElementById('status');
  const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
  const setError = text => { status.textContent = text; status.dataset.state = 'error'; };
  const startWebM = () => {
    const announced = {{mimeJson}};
    const candidates = [announced, 'video/webm;codecs=vp9,opus', 'video/webm;codecs=vp8,opus', 'video/webm'];
    if (!window.MediaSource) { setError('This browser has no MediaSource support. Open the HLS or RTSP address in VLC.'); return; }
    const mime = candidates.find(value => value && MediaSource.isTypeSupported(value));
    if (!mime) { setError('This browser cannot play the fallback WebM profile. Open HLS or RTSP in VLC.'); return; }
    const mediaSource = new MediaSource(); const queue = []; let sourceBuffer;
    const pump = () => {
      if (!sourceBuffer || sourceBuffer.updating || mediaSource.readyState !== 'open') return;
      try {
        if (sourceBuffer.buffered.length && sourceBuffer.buffered.end(0) - sourceBuffer.buffered.start(0) > 45) {
          sourceBuffer.remove(sourceBuffer.buffered.start(0), Math.max(sourceBuffer.buffered.start(0), sourceBuffer.buffered.end(0) - 30)); return;
        }
        const next = queue.shift(); if (next) sourceBuffer.appendBuffer(next);
      } catch (error) { setError(error.message || String(error)); }
    };
    mediaSource.addEventListener('sourceopen', () => {
      sourceBuffer = mediaSource.addSourceBuffer(mime); try { sourceBuffer.mode = 'sequence'; } catch { } sourceBuffer.addEventListener('updateend', pump);
      const socket = new WebSocket(protocol + '//' + location.host + '{{websocket}}'); socket.binaryType = 'arraybuffer';
      socket.addEventListener('open', () => { status.textContent = 'Live · WebM fallback'; });
      socket.addEventListener('message', event => { queue.push(new Uint8Array(event.data)); pump(); });
      socket.addEventListener('close', () => { status.textContent = 'Stream ended.'; });
      socket.addEventListener('error', () => setError('The browser stream failed. VLC/HLS or RTSP may still be available.'));
    }, { once:true });
    video.src = URL.createObjectURL(mediaSource); video.play().catch(() => undefined);
  };
  const startWebRtc = async () => {
    if (typeof RTCPeerConnection === 'undefined') { startWebM(); return; }
    const peer = new RTCPeerConnection({ iceServers: [] });
    peer.addTransceiver('video', { direction:'recvonly' }); peer.addTransceiver('audio', { direction:'recvonly' });
    peer.addEventListener('track', event => { if (event.streams[0]) video.srcObject = event.streams[0]; else video.srcObject = new MediaStream([event.track]); video.play().catch(() => undefined); });
    const socket = new WebSocket(protocol + '//' + location.host + '{{webRtc}}');
    const pendingCandidates = [];
    const send = value => { if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(value)); };
    peer.addEventListener('icecandidate', event => { if (event.candidate) send({ type:'viewer-candidate', candidate:event.candidate }); });
    socket.addEventListener('open', async () => {
      try { const offer = await peer.createOffer(); await peer.setLocalDescription(offer); send({ type:'viewer-offer', sdp:offer.sdp }); status.textContent = 'Connecting · WebRTC'; }
      catch (error) { socket.close(); peer.close(); startWebM(); }
    });
    socket.addEventListener('message', async event => {
      let message; try { message = JSON.parse(event.data); } catch { return; }
      try {
        if (message.type === 'publisher-answer') { await peer.setRemoteDescription({ type:'answer', sdp:message.sdp }); while (pendingCandidates.length) await peer.addIceCandidate(pendingCandidates.shift()); status.textContent = 'Live · WebRTC'; }
        else if (message.type === 'publisher-candidate' && message.candidate) { if (peer.remoteDescription) await peer.addIceCandidate(message.candidate); else pendingCandidates.push(message.candidate); }
        else if (message.type === 'publisher-unavailable' || message.type === 'publisher-error') { throw new Error(message.message || 'Publisher unavailable'); }
      } catch { try { socket.close(); peer.close(); } catch { } startWebM(); }
    });
    socket.addEventListener('error', () => { try { peer.close(); } catch { } startWebM(); });
    peer.addEventListener('connectionstatechange', () => { if (peer.connectionState === 'failed') { try { socket.close(); peer.close(); } catch { } startWebM(); } });
  };
  startWebRtc();
})();
</script></body></html>
""";
    }

    private bool Authorize(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(AccessToken)) return true;
        var query = context.Request.Query["token"].ToString();
        if (CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(query),
            System.Text.Encoding.UTF8.GetBytes(AccessToken))) return true;
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(authorization[7..].Trim()),
                System.Text.Encoding.UTF8.GetBytes(AccessToken));
    }

    private string AppendToken(string uri)
    {
        var separator = uri.Contains('?') ? '&' : '?';
        return $"{uri}{separator}token={Uri.EscapeDataString(AccessToken)}";
    }

    private static IPAddress ResolveAddress(string value)
    {
        if (IPAddress.TryParse(value, out var address)) return address;
        if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)) return IPAddress.Loopback;
        throw new InvalidOperationException("LAN bind address must be an explicit IPv4 or IPv6 address.");
    }

    private static string? SafeAssetPath(string rootDirectory, string? asset)
    {
        var root = Path.GetFullPath(rootDirectory);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        var relative = string.IsNullOrWhiteSpace(asset) ? "index.m3u8" : asset.Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(root, relative));
        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        if (_app is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try { await _app.StopAsync(timeout.Token); } catch { }
            await _app.DisposeAsync();
        }
        if (_runTask is not null)
        {
            try { await _runTask.WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
        }
        if (_rtspServer is not null)
        {
            try { await _rtspServer.DisposeAsync(); } catch { }
            _rtspServer = null;
        }
        _viewerGate.Dispose();
        _cancellation.Dispose();
        Status = "stopped";
    }
}

public sealed class MediaLanDefinition
{
    public bool Enabled { get; set; }
    public string BindAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 17848;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 60;
    public int VideoBitrateKbps { get; set; } = 8000;
    public bool EnableBrowserWebRtc { get; set; } = true;
    public bool EnableHls { get; set; } = true;
    public bool EnableRtsp { get; set; }
    public int RtspPort { get; set; } = 8554;
    public bool RequireAccessToken { get; set; } = true;
    public int ViewerLimit { get; set; } = 50;
}
