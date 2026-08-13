using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Channels;
using TextEncoding = global::System.Text.Encoding;

namespace PublisherStudio.Services.Streaming.Lan;

/// <summary>
/// Represents a rtsp LAN server application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RtspLanServer : IAsyncDisposable
{
    /// <summary>
    /// Stores the internal listener state used by <see cref="RtspLanServer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TcpListener _listener;
    /// <summary>
    /// Stores the UDP client dependency used by <see cref="RtspLanServer"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly UdpClient _rtpInput;
    /// <summary>
    /// Stores the cancellation source used by <see cref="RtspLanServer"/> to stop its current background or asynchronous operation.
    /// </summary>
    private readonly CancellationTokenSource _cancellation = new();
    /// <summary>
    /// Stores the in-memory clients collection maintained internally by <see cref="RtspLanServer"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<Guid, RtspClient> _clients = [];
    /// <summary>
    /// Stores the internal sync state used by <see cref="RtspLanServer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Stores the internal accept task state used by <see cref="RtspLanServer"/> while executing its surrounding workflow.
    /// </summary>
    private Task? _acceptTask;
    /// <summary>
    /// Stores the internal relay task state used by <see cref="RtspLanServer"/> while executing its surrounding workflow.
    /// </summary>
    private Task? _relayTask;
    /// <summary>
    /// Stores the internal access token state used by <see cref="RtspLanServer"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _accessToken;

    /// <summary>
    /// Initializes a new <see cref="RtspLanServer"/> instance and captures the dependencies or initial state required by its rtsp LAN server workflow.
    /// </summary>
    /// <param name="bindAddress">P address dependency used by the rtsp LAN server workflow to provide the corresponding application capability.</param>
    /// <param name="port">Port value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="accessToken">Access token value supplied to the rtsp LAN server operation and used when producing its result.</param>
    public RtspLanServer(IPAddress bindAddress, int port, string? accessToken = null)
    {
        _accessToken = accessToken?.Trim() ?? string.Empty;
        _listener = new TcpListener(bindAddress, port);
        _rtpInput = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        RtpInputPort = ((IPEndPoint)_rtpInput.Client.LocalEndPoint!).Port;
    }

    /// <summary>
    /// Gets the rtp input port value that forms part of the rtsp LAN server state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtp input port value exposed by <see cref="RtspLanServer"/>.</value>
    public int RtpInputPort { get; }
    /// <summary>
    /// Gets or sets the status value that forms part of the rtsp LAN server state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="RtspLanServer"/>.</value>
    public string Status { get; private set; } = "stopped";
    /// <summary>
    /// Gets or sets the last error value that forms part of the rtsp LAN server state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="RtspLanServer"/>.</value>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Performs start for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
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
    /// Performs accept loop for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs relay loop for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Handles client for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="tcpClient">Tcp client dependency used by the rtsp LAN server workflow to provide the corresponding application capability.</param>
    /// <param name="serverCancellation">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs request base for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="requestUri">Request uri value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs authorize for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="requestUri">Request uri value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Parses interleaved channel for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="transport">Transport value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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
    /// Reads request for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="stream">Stream value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The rtsp request produced by the operation.</returns>
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
    /// Performs response for <see cref="RtspLanServer"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp LAN server workflow.
    /// </summary>
    /// <param name="status">Status value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="cseq">Cseq value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="headers">String dependency used by the rtsp LAN server workflow to provide the corresponding application capability.</param>
    /// <param name="body">Body value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
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
    /// Releases resources owned by <see cref="RtspLanServer"/> and leaves the rtsp LAN server workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Represents the input contract for rtsp, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    /// <param name="Method">Method value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="Uri">Uri value supplied to the rtsp LAN server operation and used when producing its result.</param>
    /// <param name="Headers">Headers value supplied to the rtsp LAN server operation and used when producing its result.</param>
    private sealed record RtspRequest(string Method, string Uri, Dictionary<string, string> Headers);

    /// <summary>
    /// Represents a rtsp helper type nested within <see cref="RtspLanServer"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class RtspClient : IAsyncDisposable
    {
        /// <summary>
        /// Stores the TCP client dependency used by <see cref="RtspClient"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly TcpClient _client;
        /// <summary>
        /// Stores the synchronization primitive that protects concurrent access to control send state owned by <see cref="RtspClient"/>.
        /// </summary>
        private readonly SemaphoreSlim _controlSend = new(1, 1);
        /// <summary>
        /// Stores the cancellation source used by <see cref="RtspClient"/> to stop its current background or asynchronous operation.
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
        /// <summary>
        /// Stores the internal sender state used by <see cref="RtspClient"/> while executing its surrounding workflow.
        /// </summary>
        private Task? _sender;
        /// <summary>
        /// Stores the internal disposed state used by <see cref="RtspClient"/> while executing its surrounding workflow.
        /// </summary>
        private int _disposed;

        /// <summary>
        /// Initializes a new <see cref="RtspClient"/> instance and captures the dependencies or initial state required by its rtsp workflow.
        /// </summary>
        /// <param name="client">Tcp client dependency used by the rtsp workflow to provide the corresponding application capability.</param>
        public RtspClient(TcpClient client) { _client = client; Stream = client.GetStream(); }
        /// <summary>
        /// Gets the stream value that forms part of the rtsp state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The stream value exposed by <see cref="RtspClient"/>.</value>
        public NetworkStream Stream { get; }
        /// <summary>
        /// Gets or sets a value indicating whether playing applies to the rtsp state.
        /// </summary>
        /// <value>The playing value exposed by <see cref="RtspClient"/>.</value>
        public bool Playing { get; set; }
        /// <summary>
        /// Gets or sets the rtp channel value that forms part of the rtsp state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The rtp channel value exposed by <see cref="RtspClient"/>.</value>
        public int RtpChannel { get; set; }
        /// <summary>
        /// Gets the cancellation signal used to stop or abandon work associated with this rtsp operation.
        /// </summary>
        /// <value>The disconnected value exposed by <see cref="RtspClient"/>.</value>
        public CancellationToken Disconnected => _disconnected.Token;

        /// <summary>
        /// Starts sender for <see cref="RtspClient"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp workflow.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
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
        /// Performs enqueue for <see cref="RtspClient"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp workflow.
        /// </summary>
        /// <param name="packet">Packet value supplied to the rtsp operation and used when producing its result.</param>
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
        /// Performs send control for <see cref="RtspClient"/>, keeping the operation consistent with the state and invariants of the surrounding rtsp workflow.
        /// </summary>
        /// <param name="payload">Payload value supplied to the rtsp operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
        /// Releases resources owned by <see cref="RtspClient"/> and leaves the rtsp workflow in a safely disposed state.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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
