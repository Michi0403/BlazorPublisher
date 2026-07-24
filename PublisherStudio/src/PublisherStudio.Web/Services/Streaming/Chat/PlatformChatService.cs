using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace PublisherStudio.Services.Streaming.Chat;

public sealed record PlatformChatMessage(
    string Id,
    Guid OutputId,
    string Platform,
    string Channel,
    string AuthorId,
    string AuthorName,
    string AuthorAvatar,
    string Text,
    DateTimeOffset Timestamp,
    string Color = "",
    string Badges = "");

public sealed class PlatformChatService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MediaSession _session;
    private readonly ConcurrentDictionary<Guid, IPlatformChatAdapter> _adapters = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<PlatformChatMessage>> _history = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<PlatformChatMessage>>> _subscribers = new();
    private readonly CancellationTokenSource _lifetime = new();

    public PlatformChatService(MediaSession session) => _session = session;

    public IReadOnlyDictionary<Guid, string> Status => _adapters.ToDictionary(pair => pair.Key, pair => pair.Value.Status);

    public void Start()
    {
        foreach (var output in _session.OutputDefinitions.Where(item => item.ChatEnabled))
        {
            IPlatformChatAdapter? adapter = output.Provider switch
            {
                0 when HasChatConfiguration(output) => new TwitchIrcChatAdapter(output, Publish, _lifetime.Token),
                1 when HasChatConfiguration(output) => new YouTubeLiveChatAdapter(output, Publish, _lifetime.Token),
                _ => null
            };
            if (adapter is null || !_adapters.TryAdd(output.OutputId, adapter)) continue;
            adapter.Start();
        }
    }

    public async Task<bool> SendAsync(Guid outputId, string message, CancellationToken cancellationToken)
    {
        if (!_adapters.TryGetValue(outputId, out var adapter) || string.IsNullOrWhiteSpace(message)) return false;
        await adapter.SendAsync(message.Trim(), cancellationToken);
        return true;
    }

    public async Task RunSubscriberAsync(Guid outputId, WebSocket socket, CancellationToken cancellationToken)
    {
        if (!_session.OutputDefinitions.Any(item => item.OutputId == outputId))
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unknown output", cancellationToken);
            return;
        }

        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateBounded<PlatformChatMessage>(new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        var outputSubscribers = _subscribers.GetOrAdd(outputId, _ => new ConcurrentDictionary<Guid, Channel<PlatformChatMessage>>());
        outputSubscribers[subscriberId] = channel;
        try
        {
            if (_history.TryGetValue(outputId, out var history))
                foreach (var item in history.ToArray()) channel.Writer.TryWrite(item);

            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (socket.State != WebSocketState.Open) break;
                var payload = JsonSerializer.SerializeToUtf8Bytes(item, JsonOptions);
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (WebSocketException) { }
        finally
        {
            outputSubscribers.TryRemove(subscriberId, out _);
            channel.Writer.TryComplete();
            if (outputSubscribers.IsEmpty) _subscribers.TryRemove(outputId, out _);
        }
    }

    private static bool HasChatConfiguration(MediaOutputDefinition output) =>
        !string.IsNullOrWhiteSpace(output.ChannelId)
        && !string.IsNullOrWhiteSpace(output.ChatSecret)
        && (output.Provider != 0 || !string.IsNullOrWhiteSpace(output.AccountName));

    private void Publish(PlatformChatMessage message)
    {
        var history = _history.GetOrAdd(message.OutputId, _ => new ConcurrentQueue<PlatformChatMessage>());
        history.Enqueue(message);
        while (history.Count > 200 && history.TryDequeue(out _)) { }
        if (!_subscribers.TryGetValue(message.OutputId, out var subscribers)) return;
        foreach (var subscriber in subscribers.Values) subscriber.Writer.TryWrite(message);
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        foreach (var subscribers in _subscribers.Values)
            foreach (var channel in subscribers.Values) channel.Writer.TryComplete();
        _subscribers.Clear();
        foreach (var adapter in _adapters.Values)
            try { await adapter.DisposeAsync(); } catch { }
        _adapters.Clear();
        _lifetime.Dispose();
    }
}

internal interface IPlatformChatAdapter : IAsyncDisposable
{
    string Status { get; }
    void Start();
    Task SendAsync(string message, CancellationToken cancellationToken);
}

internal sealed class TwitchIrcChatAdapter : IPlatformChatAdapter
{
    private readonly MediaOutputDefinition _output;
    private readonly Action<PlatformChatMessage> _publish;
    private readonly CancellationTokenSource _lifetime;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private Task? _runTask;
    private StreamWriter? _writer;

    public TwitchIrcChatAdapter(MediaOutputDefinition output, Action<PlatformChatMessage> publish, CancellationToken sessionToken)
    {
        _output = output;
        _publish = publish;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
    }

    public string Status { get; private set; } = "configured";
    public void Start() => _runTask ??= Task.Run(RunAsync);

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        var writer = _writer ?? throw new InvalidOperationException("Twitch Chat is not connected.");
        await _sendGate.WaitAsync(cancellationToken);
        try { await writer.WriteLineAsync($"PRIVMSG #{NormalizeChannel(_output.ChannelId)} :{SanitizeMessage(message)}"); }
        finally { _sendGate.Release(); }
    }

    private async Task RunAsync()
    {
        var attempt = 0;
        while (!_lifetime.IsCancellationRequested)
        {
            try
            {
                Status = "connecting";
                using var client = new TcpClient();
                await client.ConnectAsync("irc.chat.twitch.tv", 6697, _lifetime.Token);
                await using var ssl = new SslStream(client.GetStream(), false);
                await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "irc.chat.twitch.tv",
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }, _lifetime.Token);
                using var reader = new StreamReader(ssl, Encoding.UTF8, false, 1024, leaveOpen: true);
                await using var writer = new StreamWriter(ssl, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
                _writer = writer;
                var token = _output.ChatSecret.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase)
                    ? _output.ChatSecret
                    : "oauth:" + _output.ChatSecret;
                await writer.WriteLineAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
                await writer.WriteLineAsync("PASS " + token);
                await writer.WriteLineAsync("NICK " + NormalizeAccount(_output.AccountName));
                await writer.WriteLineAsync("JOIN #" + NormalizeChannel(_output.ChannelId));
                Status = "connected";
                attempt = 0;

                while (!_lifetime.IsCancellationRequested && await reader.ReadLineAsync(_lifetime.Token) is { } line)
                {
                    if (line.StartsWith("PING ", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("PONG " + line[5..]);
                        continue;
                    }
                    if (TryParsePrivMsg(line, out var message)) _publish(message);
                }
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                Status = "error: " + exception.Message;
            }
            finally { _writer = null; }

            attempt = Math.Min(6, attempt + 1);
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), _lifetime.Token); }
            catch (OperationCanceledException) { break; }
        }
        Status = "stopped";
    }

    private bool TryParsePrivMsg(string line, out PlatformChatMessage message)
    {
        message = default!;
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cursor = line;
        if (cursor.StartsWith('@'))
        {
            var separator = cursor.IndexOf(' ');
            if (separator < 0) return false;
            foreach (var pair in cursor[1..separator].Split(';'))
            {
                var equals = pair.IndexOf('=');
                tags[equals < 0 ? pair : pair[..equals]] = equals < 0 ? string.Empty : DecodeTag(pair[(equals + 1)..]);
            }
            cursor = cursor[(separator + 1)..];
        }
        var marker = " PRIVMSG #";
        var command = cursor.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (command < 0) return false;
        var messageSeparator = cursor.IndexOf(" :", command, StringComparison.Ordinal);
        if (messageSeparator < 0) return false;
        var prefixEnd = cursor.IndexOf('!');
        var fallbackName = prefixEnd > 1 ? cursor[1..prefixEnd] : "Viewer";
        var author = tags.GetValueOrDefault("display-name", fallbackName);
        var timestamp = DateTimeOffset.UtcNow;
        if (long.TryParse(tags.GetValueOrDefault("tmi-sent-ts"), out var milliseconds))
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        message = new PlatformChatMessage(
            tags.GetValueOrDefault("id", Guid.NewGuid().ToString("N")),
            _output.OutputId,
            "Twitch",
            _output.ChannelId,
            tags.GetValueOrDefault("user-id", fallbackName),
            author,
            string.Empty,
            cursor[(messageSeparator + 2)..],
            timestamp,
            tags.GetValueOrDefault("color", string.Empty),
            tags.GetValueOrDefault("badges", string.Empty));
        return true;
    }

    private static string NormalizeAccount(string value) => new(value.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
    private static string NormalizeChannel(string value) => NormalizeAccount(value.TrimStart('#'));
    private static string SanitizeMessage(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    private static string DecodeTag(string value) => value.Replace("\\s", " ").Replace("\\:", ";").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\\", "\\");

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        if (_runTask is not null) try { await _runTask; } catch { }
        _sendGate.Dispose();
        _lifetime.Dispose();
    }
}

internal sealed class YouTubeLiveChatAdapter : IPlatformChatAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MediaOutputDefinition _output;
    private readonly Action<PlatformChatMessage> _publish;
    private readonly CancellationTokenSource _lifetime;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private Task? _runTask;
    private string _pageToken = string.Empty;
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    public YouTubeLiveChatAdapter(MediaOutputDefinition output, Action<PlatformChatMessage> publish, CancellationToken sessionToken)
    {
        _output = output;
        _publish = publish;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", output.ChatSecret);
    }

    public string Status { get; private set; } = "configured";
    public void Start() => _runTask ??= Task.Run(RunAsync);

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            snippet = new
            {
                liveChatId = _output.ChannelId,
                type = "textMessageEvent",
                textMessageDetails = new { messageText = message }
            }
        }, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"YouTube Chat send failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }

    private async Task RunAsync()
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!_lifetime.IsCancellationRequested)
        {
            try
            {
                Status = "connecting";
                var url = new StringBuilder("https://www.googleapis.com/youtube/v3/liveChat/messages?part=id,snippet,authorDetails&maxResults=200&profileImageSize=64&liveChatId=")
                    .Append(Uri.EscapeDataString(_output.ChannelId));
                if (!string.IsNullOrWhiteSpace(_pageToken)) url.Append("&pageToken=").Append(Uri.EscapeDataString(_pageToken));
                using var response = await _http.GetAsync(url.ToString(), _lifetime.Token);
                var json = await response.Content.ReadAsStringAsync(_lifetime.Token);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"YouTube Chat read failed ({(int)response.StatusCode}): {json}");
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                _pageToken = root.TryGetProperty("nextPageToken", out var pageToken) ? pageToken.GetString() ?? string.Empty : _pageToken;
                var interval = root.TryGetProperty("pollingIntervalMillis", out var polling) && polling.TryGetInt32(out var milliseconds)
                    ? Math.Clamp(milliseconds, 1000, 30000)
                    : 2000;
                delay = TimeSpan.FromMilliseconds(interval);
                if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    foreach (var item in items.EnumerateArray()) PublishItem(item);
                Status = "connected";
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                Status = "error: " + exception.Message;
                delay = TimeSpan.FromSeconds(Math.Min(30, Math.Max(3, delay.TotalSeconds * 1.5)));
            }
            try { await Task.Delay(delay, _lifetime.Token); }
            catch (OperationCanceledException) { break; }
        }
        Status = "stopped";
    }

    private void PublishItem(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProperty) ? idProperty.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(id) || !_seen.Add(id)) return;
        if (_seen.Count > 4000) _seen.Clear();
        if (!item.TryGetProperty("snippet", out var snippet)) return;
        var type = snippet.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : string.Empty;
        if (!string.Equals(type, "textMessageEvent", StringComparison.OrdinalIgnoreCase)) return;
        var text = snippet.TryGetProperty("displayMessage", out var displayMessage) ? displayMessage.GetString() ?? string.Empty : string.Empty;
        var timestamp = DateTimeOffset.UtcNow;
        if (snippet.TryGetProperty("publishedAt", out var publishedAt)
            && DateTimeOffset.TryParse(publishedAt.GetString(), out var parsedTimestamp)) timestamp = parsedTimestamp;
        var author = item.TryGetProperty("authorDetails", out var details) ? details : default;
        var authorId = author.ValueKind == JsonValueKind.Object && author.TryGetProperty("channelId", out var channelId) ? channelId.GetString() ?? string.Empty : string.Empty;
        var authorName = author.ValueKind == JsonValueKind.Object && author.TryGetProperty("displayName", out var displayName) ? displayName.GetString() ?? "Viewer" : "Viewer";
        var avatar = author.ValueKind == JsonValueKind.Object && author.TryGetProperty("profileImageUrl", out var profileImage) ? profileImage.GetString() ?? string.Empty : string.Empty;
        _publish(new PlatformChatMessage(id, _output.OutputId, "YouTube", _output.ChannelId, authorId, authorName, avatar, text, timestamp));
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        if (_runTask is not null) try { await _runTask; } catch { }
        _http.Dispose();
        _lifetime.Dispose();
    }
}
