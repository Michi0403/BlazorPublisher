using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Channels;
using TextEncoding = global::System.Text.Encoding;

namespace PublisherStudio.Services.Streaming.Lan;

/// <summary>
/// Represents a rtsp LAN server.
/// </summary>
public sealed class RtspLanServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly UdpClient _rtpInput;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Dictionary<Guid, RtspClient> _clients = [];
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly object _sync = new();
    private Task? _acceptTask;
    private Task? _relayTask;
    private readonly string _accessToken;

    /// <summary>
    /// Runs the rtsp LAN server operation.
    /// </summary>
    public RtspLanServer(IPAddress bindAddress, int port, string? accessToken = null)
    {
        _accessToken = accessToken?.Trim() ?? string.Empty;
        _listener = new TcpListener(bindAddress, port);
        _rtpInput = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        RtpInputPort = ((IPEndPoint)_rtpInput.Client.LocalEndPoint!).Port;
    }

    /// <summary>
    /// Gets rtp input port.
    /// </summary>
    public int RtpInputPort { get; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; private set; } = "stopped";
    /// <summary>
    /// Gets or sets last error.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Runs the start operation.
    /// </summary>
    public void Start()
    {
    try
    {
            if (_acceptTask is not null) return;
            _listener.Start();
            Status = "listening";
            _acceptTask = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
            _relayTask = Task.Run(() => RelayLoopAsync(_cancellation.Token));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.Start failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the accept loop async operation.
    /// </summary>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
    try
    {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception) { LastError = exception.Message; Status = "error"; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.AcceptLoopAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the relay loop async operation.
    /// </summary>
    private async Task RelayLoopAsync(CancellationToken cancellationToken)
    {
    try
    {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _rtpInput.ReceiveAsync(cancellationToken);
                    RtspClient[] clients;
                    lock (_sync) clients = _clients.Values.Where(item => item.Playing).ToArray();
                    foreach (var client in clients) client.Enqueue(result.Buffer);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception) { LastError = exception.Message; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.RelayLoopAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Handles client async.
    /// </summary>
    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken serverCancellation)
    {
    try
    {
            var id = Guid.NewGuid();
            await using var client = new RtspClient(tcpClient);
            lock (_sync) _clients[id] = client;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(serverCancellation, client.Disconnected);
            var sessionId = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant()[..16];
            var authenticated = string.IsNullOrWhiteSpace(_accessToken);
            try
            {
                client.StartSender(linked.Token);
                while (!linked.IsCancellationRequested)
                {
                    var request = await ReadRequestAsync(client.Stream, linked.Token);
                    if (request is null) break;
                    var cseq = request.Headers.GetValueOrDefault("CSeq", "1");
                    authenticated = authenticated || Authorize(request.Uri);
                    if (!authenticated)
                    {
                        await client.SendControlAsync(Response(401, cseq, ["WWW-Authenticate: Bearer realm=PublisherStudio"]), linked.Token);
                        continue;
                    }
                    switch (request.Method)
                    {
                        case "OPTIONS":
                            await client.SendControlAsync(Response(200, cseq, ["Public: OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, GET_PARAMETER, TEARDOWN"]), linked.Token);
                            break;
                        case "DESCRIBE":
                        {
                            var sdp = "v=0\r\n"
                                + $"o=- {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} 1 IN IP4 127.0.0.1\r\n"
                                + "s=PublisherStudio\r\n"
                                + "t=0 0\r\n"
                                + "a=control:*\r\n"
                                + "m=video 0 RTP/AVP 33\r\n"
                                + "c=IN IP4 0.0.0.0\r\n"
                                + "a=rtpmap:33 MP2T/90000\r\n"
                                + "a=control:trackID=0\r\n";
                            var contentLength = TextEncoding.ASCII.GetByteCount(sdp);
                            await client.SendControlAsync(Response(200, cseq,
                            [
                                "Content-Type: application/sdp",
                                $"Content-Base: {RequestBase(request.Uri)}/",
                                $"Content-Length: {contentLength}"
                            ], sdp), linked.Token);
                            break;
                        }
                        case "SETUP":
                        {
                            var transport = request.Headers.GetValueOrDefault("Transport", "RTP/AVP/TCP;unicast;interleaved=0-1");
                            if (!transport.Contains("RTP/AVP/TCP", StringComparison.OrdinalIgnoreCase))
                            {
                                await client.SendControlAsync(Response(461, cseq), linked.Token);
                                break;
                            }
                            client.RtpChannel = ParseInterleavedChannel(transport);
                            await client.SendControlAsync(Response(200, cseq,
                            [
                                $"Transport: RTP/AVP/TCP;unicast;interleaved={client.RtpChannel}-{client.RtpChannel + 1}",
                                $"Session: {sessionId};timeout=60"
                            ]), linked.Token);
                            break;
                        }
                        case "PLAY":
                            client.Playing = true;
                            await client.SendControlAsync(Response(200, cseq,
                            [
                                $"Session: {sessionId};timeout=60",
                                $"RTP-Info: url={RequestBase(request.Uri)}/trackID=0"
                            ]), linked.Token);
                            break;
                        case "PAUSE":
                            client.Playing = false;
                            await client.SendControlAsync(Response(200, cseq, [$"Session: {sessionId}"]), linked.Token);
                            break;
                        case "GET_PARAMETER":
                            await client.SendControlAsync(Response(200, cseq, [$"Session: {sessionId}"]), linked.Token);
                            break;
                        case "TEARDOWN":
                            await client.SendControlAsync(Response(200, cseq, [$"Session: {sessionId}"]), linked.Token);
                            return;
                        default:
                            await client.SendControlAsync(Response(405, cseq), linked.Token);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
            catch (IOException) { }
            catch (SocketException) { }
            finally
            {
                lock (_sync) _clients.Remove(id);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.HandleClientAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the request base operation.
    /// </summary>
    private string RequestBase(string requestUri)
    {
    try
    {
            var separator = requestUri.IndexOf('?');
            return (separator >= 0 ? requestUri[..separator] : requestUri).TrimEnd('/');
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.RequestBase failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the authorize operation.
    /// </summary>
    private bool Authorize(string requestUri)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(_accessToken)) return true;
            if (!Uri.TryCreate(requestUri, UriKind.Absolute, out var uri)) return false;
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var token = query.Select(part => part.Split('=', 2))
                .FirstOrDefault(part => part.Length > 0 && part[0].Equals("token", StringComparison.OrdinalIgnoreCase));
            var supplied = token is { Length: > 1 } ? Uri.UnescapeDataString(token[1]) : string.Empty;
            var expectedBytes = TextEncoding.UTF8.GetBytes(_accessToken);
            var suppliedBytes = TextEncoding.UTF8.GetBytes(supplied);
            return suppliedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.Authorize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses interleaved channel.
    /// </summary>
    private int ParseInterleavedChannel(string transport)
    {
    try
    {
            var marker = "interleaved=";
            var index = transport.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return 0;
            var value = transport[(index + marker.Length)..].Split(';', 2)[0].Split('-', 2)[0];
            return int.TryParse(value, out var channel) ? Math.Clamp(channel, 0, 254) : 0;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.ParseInterleavedChannel failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads request async.
    /// </summary>
    private async Task<RtspRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
    try
    {
            var buffer = new List<byte>(2048);
            var one = new byte[1];
            while (buffer.Count < 64 * 1024)
            {
                var read = await stream.ReadAsync(one, cancellationToken);
                if (read == 0) return null;
                buffer.Add(one[0]);
                var count = buffer.Count;
                if (count >= 4 && buffer[count - 4] == 13 && buffer[count - 3] == 10 && buffer[count - 2] == 13 && buffer[count - 1] == 10) break;
            }
            var text = TextEncoding.ASCII.GetString(buffer.ToArray());
            var lines = text.Split("\r\n", StringSplitOptions.None);
            var first = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (first.Length < 2) return null;
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Skip(1))
            {
                var separator = line.IndexOf(':');
                if (separator <= 0) continue;
                headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
            return new RtspRequest(first[0].ToUpperInvariant(), first[1], headers);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.ReadRequestAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the response operation.
    /// </summary>
    private byte[] Response(int status, string cseq, IReadOnlyList<string>? headers = null, string body = "")
    {
    try
    {
            var reason = status switch { 200 => "OK", 401 => "Unauthorized", 405 => "Method Not Allowed", 461 => "Unsupported Transport", _ => "Error" };
            var builder = new StringBuilder($"RTSP/1.0 {status} {reason}\r\nCSeq: {cseq}\r\nServer: PublisherStudio\r\n");
            if (headers is not null) foreach (var header in headers) builder.Append(header).Append("\r\n");
            builder.Append("\r\n").Append(body);
            return TextEncoding.ASCII.GetBytes(builder.ToString());
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.Response failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the dispose async operation.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
    try
    {
            _cancellation.Cancel();
            _listener.Stop();
            _rtpInput.Dispose();
            RtspClient[] clients;
            lock (_sync) { clients = _clients.Values.ToArray(); _clients.Clear(); }
            foreach (var client in clients) await client.DisposeAsync();
            foreach (var task in new[] { _acceptTask, _relayTask }.Where(item => item is not null))
                try { await task!.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
            _cancellation.Dispose();
            Status = "stopped";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspLanServer.DisposeAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Represents a rtsp request.
    /// </summary>
    private sealed record RtspRequest(string Method, string Uri, Dictionary<string, string> Headers);

    /// <summary>
    /// Represents a rtsp client.
    /// </summary>
    private sealed class RtspClient : IAsyncDisposable
    {
        private readonly TcpClient _client;
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly SemaphoreSlim _controlSend = new(1, 1);
        /// <summary>
        /// Creates bounded.
        /// </summary>
        private readonly Channel<byte[]> _rtp = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(180)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly CancellationTokenSource _disconnected = new();
        private Task? _sender;
        private int _disposed;

        /// <summary>
        /// Runs the rtsp client operation.
        /// </summary>
        public RtspClient(TcpClient client) { _client = client; Stream = client.GetStream(); }
        /// <summary>
        /// Gets stream.
        /// </summary>
        public NetworkStream Stream { get; }
        /// <summary>
        /// Gets or sets playing.
        /// </summary>
        public bool Playing { get; set; }
        /// <summary>
        /// Gets or sets rtp channel.
        /// </summary>
        public int RtpChannel { get; set; }
        /// <summary>
        /// Gets disconnected.
        /// </summary>
        public CancellationToken Disconnected => _disconnected.Token;

        /// <summary>
        /// Starts sender.
        /// </summary>
        public void StartSender(CancellationToken cancellationToken) {
    try
    {
        _sender = Task.Run(async () =>
        {
            try
            {
                await foreach (var packet in _rtp.Reader.ReadAllAsync(cancellationToken))
                {
                    var header = new byte[] { 0x24, (byte)RtpChannel, (byte)(packet.Length >> 8), (byte)(packet.Length & 0xff) };
                    await _controlSend.WaitAsync(cancellationToken);
                    try { await Stream.WriteAsync(header, cancellationToken); await Stream.WriteAsync(packet, cancellationToken); await Stream.FlushAsync(cancellationToken); }
                    finally { _controlSend.Release(); }
                }
            }
            catch { _disconnected.Cancel(); }
        }, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspClient.StartSender failed: {__serviceMethodException}");
        throw;
    }
}

        /// <summary>
        /// Runs the enqueue operation.
        /// </summary>
        public void Enqueue(byte[] packet)
        {
    try
    {
                if (Volatile.Read(ref _disposed) == 0) _rtp.Writer.TryWrite(packet);
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspClient.Enqueue failed: {__serviceMethodException}");
        throw;
    }
}

        /// <summary>
        /// Runs the send control async operation.
        /// </summary>
        public async Task SendControlAsync(byte[] payload, CancellationToken cancellationToken)
        {
    try
    {
                await _controlSend.WaitAsync(cancellationToken);
                try { await Stream.WriteAsync(payload, cancellationToken); await Stream.FlushAsync(cancellationToken); }
                finally { _controlSend.Release(); }
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspClient.SendControlAsync failed: {__serviceMethodException}");
        throw;
    }
}

        /// <summary>
        /// Runs the dispose async operation.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
    try
    {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                _rtp.Writer.TryComplete();
                _disconnected.Cancel();
                try { _client.Close(); } catch { }
                if (_sender is not null) try { await _sender.WaitAsync(TimeSpan.FromSeconds(1)); } catch { }
                Stream.Dispose();
                _client.Dispose();
                _controlSend.Dispose();
                _disconnected.Dispose();
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method RtspClient.DisposeAsync failed: {__serviceMethodException}");
        throw;
    }
}
    }
}
