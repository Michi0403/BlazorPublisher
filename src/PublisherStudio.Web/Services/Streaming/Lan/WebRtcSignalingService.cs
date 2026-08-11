using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Services.Streaming.Lan;

/// <summary>
/// Provides web rtc signaling service operations.
/// </summary>
public sealed class WebRtcSignalingService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ViewerConnection> _viewers = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly SemaphoreSlim _publisherSend = new(1, 1);
    private WebSocket? _publisher;

    /// <summary>
    /// Runs the run publisher async operation.
    /// </summary>
    public async Task RunPublisherAsync(WebSocket socket, CancellationToken cancellationToken)
    {
    try
    {
            var previous = Interlocked.Exchange(ref _publisher, socket);
            if (previous is not null && previous != socket)
                await CloseQuietlyAsync(previous, "A newer PublisherStudio renderer connected.");

            try
            {
                foreach (var viewerId in _viewers.Keys)
                    await SendPublisherAsync(new { type = "viewer-ready", viewerId }, cancellationToken);

                await ReceiveJsonAsync(socket, async document =>
                {
                    var root = document.RootElement;
                    var type = ReadString(root, "type");
                    if (!TryReadViewerId(root, out var viewerId) || !_viewers.TryGetValue(viewerId, out var viewer)) return;
                    var forwardedType = type switch
                    {
                        "publisher-answer" => "publisher-answer",
                        "publisher-candidate" => "publisher-candidate",
                        "publisher-error" => "publisher-error",
                        _ => string.Empty
                    };
                    if (forwardedType.Length == 0) return;
                    await viewer.SendAsync(CopyWithType(root, forwardedType, includeViewerId: false), cancellationToken);
                }, cancellationToken);
            }
            finally
            {
                Interlocked.CompareExchange(ref _publisher, null, socket);
                foreach (var viewer in _viewers.Values)
                    await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "publisher-unavailable" }), CancellationToken.None);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.RunPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the run viewer async operation.
    /// </summary>
    public async Task RunViewerAsync(WebSocket socket, CancellationToken cancellationToken)
    {
    try
    {
            var viewerId = Guid.NewGuid();
            var viewer = new ViewerConnection(socket);
            _viewers[viewerId] = viewer;
            try
            {
                await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "viewer-id", viewerId }), cancellationToken);
                if (_publisher is not null)
                    await SendPublisherAsync(new { type = "viewer-ready", viewerId }, cancellationToken);
                else
                    await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "publisher-unavailable" }), cancellationToken);

                await ReceiveJsonAsync(socket, async document =>
                {
                    var root = document.RootElement;
                    var type = ReadString(root, "type");
                    var forwardedType = type switch
                    {
                        "viewer-offer" => "viewer-offer",
                        "viewer-candidate" => "viewer-candidate",
                        _ => string.Empty
                    };
                    if (forwardedType.Length == 0) return;
                    await SendPublisherBytesAsync(CopyWithViewer(root, forwardedType, viewerId), cancellationToken);
                }, cancellationToken);
            }
            finally
            {
                _viewers.TryRemove(viewerId, out _);
                await SendPublisherAsync(new { type = "viewer-left", viewerId }, CancellationToken.None);
                await viewer.DisposeAsync();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.RunViewerAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Closes async.
    /// </summary>
    public async Task CloseAsync()
    {
    try
    {
            var publisher = Interlocked.Exchange(ref _publisher, null);
            if (publisher is not null) await CloseQuietlyAsync(publisher, "Session stopped.");
            foreach (var pair in _viewers.ToArray())
            {
                _viewers.TryRemove(pair.Key, out _);
                await pair.Value.DisposeAsync();
            }
            _publisherSend.Dispose();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.CloseAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the send publisher async operation.
    /// </summary>
    private async Task SendPublisherAsync(object message, CancellationToken cancellationToken) {
    try
    {
        await SendPublisherBytesAsync(JsonSerializer.SerializeToUtf8Bytes(message), cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.SendPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the send publisher bytes async operation.
    /// </summary>
    private async Task SendPublisherBytesAsync(byte[] payload, CancellationToken cancellationToken)
    {
    try
    {
            var publisher = _publisher;
            if (publisher?.State != WebSocketState.Open) return;
            await _publisherSend.WaitAsync(cancellationToken);
            try
            {
                if (publisher.State == WebSocketState.Open)
                    await publisher.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (WebSocketException) { }
            finally { _publisherSend.Release(); }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.SendPublisherBytesAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the receive JSON async operation.
    /// </summary>
    private async Task ReceiveJsonAsync(WebSocket socket, Func<JsonDocument, Task> onMessage, CancellationToken cancellationToken)
    {
    try
    {
            var buffer = new byte[64 * 1024];
            using var message = new MemoryStream();
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    if (result.MessageType != WebSocketMessageType.Text) continue;
                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage) continue;
                    message.Position = 0;
                    try
                    {
                        using var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);
                        await onMessage(document);
                    }
                    catch (JsonException) { }
                    finally { message.SetLength(0); }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (WebSocketException) { }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.ReceiveJsonAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the copy with viewer operation.
    /// </summary>
    private byte[] CopyWithViewer(JsonElement root, string type, Guid viewerId)
    {
    try
    {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("type", type);
                writer.WriteString("viewerId", viewerId);
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("type") || property.NameEquals("viewerId")) continue;
                    property.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            return stream.ToArray();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.CopyWithViewer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the copy with type operation.
    /// </summary>
    private byte[] CopyWithType(JsonElement root, string type, bool includeViewerId)
    {
    try
    {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("type", type);
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("type") || (!includeViewerId && property.NameEquals("viewerId"))) continue;
                    property.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            return stream.ToArray();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.CopyWithType failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads string.
    /// </summary>
    private string ReadString(JsonElement root, string property) {
    try
    {
        return root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.ReadString failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to read viewer identifier.
    /// </summary>
    private bool TryReadViewerId(JsonElement root, out Guid viewerId)
    {
    try
    {
            viewerId = Guid.Empty;
            return root.TryGetProperty("viewerId", out var value)
                && value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out viewerId);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.TryReadViewerId failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Closes quietly async.
    /// </summary>
    private async Task CloseQuietlyAsync(WebSocket socket, string reason)
    {
    try
    {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            }
            catch { }
            finally { socket.Dispose(); }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.CloseQuietlyAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Represents a viewer connection.
    /// </summary>
    private sealed class ViewerConnection(WebSocket socket) : IAsyncDisposable
    {
        private readonly WebSocket _socket = socket;
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly SemaphoreSlim _send = new(1, 1);

        /// <summary>
        /// Runs the send async operation.
        /// </summary>
        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
    try
    {
                if (_socket.State != WebSocketState.Open) return;
                await _send.WaitAsync(cancellationToken);
                try
                {
                    if (_socket.State == WebSocketState.Open)
                        await _socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
                }
                catch (WebSocketException) { }
                finally { _send.Release(); }
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ViewerConnection.SendAsync failed: {__serviceMethodException}");
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
                try
                {
                    if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Viewer disconnected.", CancellationToken.None);
                }
                catch { }
                _socket.Dispose();
                _send.Dispose();
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ViewerConnection.DisposeAsync failed: {__serviceMethodException}");
        throw;
    }
}
    }
}
