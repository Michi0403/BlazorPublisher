using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Channels;

public sealed class RtspLanServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly UdpClient _rtpInput;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Dictionary<Guid, RtspClient> _clients = [];
    private readonly object _sync = new();
    private Task? _acceptTask;
    private Task? _relayTask;
    private readonly string _accessToken;

    public RtspLanServer(IPAddress bindAddress, int port, string? accessToken = null)
    {
        _accessToken = accessToken?.Trim() ?? string.Empty;
        _listener = new TcpListener(bindAddress, port);
        _rtpInput = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        RtpInputPort = ((IPEndPoint)_rtpInput.Client.LocalEndPoint!).Port;
    }

    public int RtpInputPort { get; }
    public string Status { get; private set; } = "stopped";
    public string LastError { get; private set; } = string.Empty;

    public void Start()
    {
        if (_acceptTask is not null) return;
        _listener.Start();
        Status = "listening";
        _acceptTask = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
        _relayTask = Task.Run(() => RelayLoopAsync(_cancellation.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
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

    private async Task RelayLoopAsync(CancellationToken cancellationToken)
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

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken serverCancellation)
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
                        await client.SendControlAsync(Response(200, cseq,
                        [
                            "Content-Type: application/sdp",
                            $"Content-Base: {RequestBase(request.Uri)}/",
                            $"Content-Length: {Encoding.ASCII.GetByteCount(sdp)}"
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

    private static string RequestBase(string requestUri)
    {
        var separator = requestUri.IndexOf('?');
        return (separator >= 0 ? requestUri[..separator] : requestUri).TrimEnd('/');
    }

    private bool Authorize(string requestUri)
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) return true;
        if (!Uri.TryCreate(requestUri, UriKind.Absolute, out var uri)) return false;
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var token = query.Select(part => part.Split('=', 2))
            .FirstOrDefault(part => part.Length > 0 && part[0].Equals("token", StringComparison.OrdinalIgnoreCase));
        var supplied = token is { Length: > 1 } ? Uri.UnescapeDataString(token[1]) : string.Empty;
        var expectedBytes = Encoding.UTF8.GetBytes(_accessToken);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return suppliedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private static int ParseInterleavedChannel(string transport)
    {
        var marker = "interleaved=";
        var index = transport.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return 0;
        var value = transport[(index + marker.Length)..].Split(';', 2)[0].Split('-', 2)[0];
        return int.TryParse(value, out var channel) ? Math.Clamp(channel, 0, 254) : 0;
    }

    private static async Task<RtspRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
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
        var text = Encoding.ASCII.GetString(buffer.ToArray());
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

    private static byte[] Response(int status, string cseq, IReadOnlyList<string>? headers = null, string body = "")
    {
        var reason = status switch { 200 => "OK", 401 => "Unauthorized", 405 => "Method Not Allowed", 461 => "Unsupported Transport", _ => "Error" };
        var builder = new StringBuilder($"RTSP/1.0 {status} {reason}\r\nCSeq: {cseq}\r\nServer: PublisherStudio.MediaHost\r\n");
        if (headers is not null) foreach (var header in headers) builder.Append(header).Append("\r\n");
        builder.Append("\r\n").Append(body);
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    public async ValueTask DisposeAsync()
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

    private sealed record RtspRequest(string Method, string Uri, Dictionary<string, string> Headers);

    private sealed class RtspClient : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly SemaphoreSlim _controlSend = new(1, 1);
        private readonly Channel<byte[]> _rtp = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(180)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        private readonly CancellationTokenSource _disconnected = new();
        private Task? _sender;
        private int _disposed;

        public RtspClient(TcpClient client) { _client = client; Stream = client.GetStream(); }
        public NetworkStream Stream { get; }
        public bool Playing { get; set; }
        public int RtpChannel { get; set; }
        public CancellationToken Disconnected => _disconnected.Token;

        public void StartSender(CancellationToken cancellationToken) => _sender = Task.Run(async () =>
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

        public void Enqueue(byte[] packet)
        {
            if (Volatile.Read(ref _disposed) == 0) _rtp.Writer.TryWrite(packet);
        }

        public async Task SendControlAsync(byte[] payload, CancellationToken cancellationToken)
        {
            await _controlSend.WaitAsync(cancellationToken);
            try { await Stream.WriteAsync(payload, cancellationToken); await Stream.FlushAsync(cancellationToken); }
            finally { _controlSend.Release(); }
        }

        public async ValueTask DisposeAsync()
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
    }
}
