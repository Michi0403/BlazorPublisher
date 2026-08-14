using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Services.Streaming.Lan;

/// <summary>
/// Coordinates web rtc signaling behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class WebRtcSignalingService
{
    /// <summary>
    /// Stores the in-memory viewers collection maintained internally by <see cref="WebRtcSignalingService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ViewerConnection> _viewers = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to publisher send state owned by <see cref="WebRtcSignalingService"/>.
    /// </summary>
    private readonly SemaphoreSlim _publisherSend = new(1, 1);
    /// <summary>
    /// Stores the internal publisher state used by <see cref="WebRtcSignalingService"/> while executing its surrounding workflow.
    /// </summary>
    private WebSocket? _publisher;

    /// <summary>
    /// Performs run publisher as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="socket">Socket value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RunPublisherAsync(WebSocket socket, CancellationToken cancellationToken)
    {
    try
    {
            var previous = Interlocked.Exchange(ref _publisher, socket);
            if (previous is not null && previous != socket)
                await CloseQuietlyAsync(previous, "A newer PublisherStudio renderer connected.").ConfigureAwait(false);

            try
            {
                foreach (var viewerId in _viewers.Keys)
                    await SendPublisherAsync(new { type = "viewer-ready", viewerId }, cancellationToken).ConfigureAwait(false);

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
                    await viewer.SendAsync(CopyWithType(root, forwardedType, includeViewerId: false), cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.CompareExchange(ref _publisher, null, socket);
                foreach (var viewer in _viewers.Values)
                    await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "publisher-unavailable" }), CancellationToken.None).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.RunPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs run viewer as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="socket">Socket value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RunViewerAsync(WebSocket socket, CancellationToken cancellationToken)
    {
    try
    {
            var viewerId = Guid.NewGuid();
            var viewer = new ViewerConnection(socket);
            _viewers[viewerId] = viewer;
            try
            {
                await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "viewer-id", viewerId }), cancellationToken).ConfigureAwait(false);
                if (_publisher is not null)
                    await SendPublisherAsync(new { type = "viewer-ready", viewerId }, cancellationToken).ConfigureAwait(false);
                else
                    await viewer.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "publisher-unavailable" }), cancellationToken).ConfigureAwait(false);

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
                    await SendPublisherBytesAsync(CopyWithViewer(root, forwardedType, viewerId), cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _viewers.TryRemove(viewerId, out _);
                await SendPublisherAsync(new { type = "viewer-left", viewerId }, CancellationToken.None).ConfigureAwait(false);
                await viewer.DisposeAsync().ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.RunViewerAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs close as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task CloseAsync()
    {
    try
    {
            var publisher = Interlocked.Exchange(ref _publisher, null);
            if (publisher is not null) await CloseQuietlyAsync(publisher, "Session stopped.").ConfigureAwait(false);
            foreach (var pair in _viewers.ToArray())
            {
                _viewers.TryRemove(pair.Key, out _);
                await pair.Value.DisposeAsync().ConfigureAwait(false);
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
    /// Performs send publisher as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SendPublisherAsync(object message, CancellationToken cancellationToken) {
    try
    {
        await SendPublisherBytesAsync(JsonSerializer.SerializeToUtf8Bytes(message), cancellationToken).ConfigureAwait(false);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method WebRtcSignalingService.SendPublisherAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs send publisher bytes as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="payload">Payload value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SendPublisherBytesAsync(byte[] payload, CancellationToken cancellationToken)
    {
    try
    {
            var publisher = _publisher;
            if (publisher?.State != WebSocketState.Open) return;
            await _publisherSend.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (publisher.State == WebSocketState.Open)
                    await publisher.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
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
    /// Performs receive JSON as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="socket">Socket value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="onMessage">On message value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
                    var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    if (result.MessageType != WebSocketMessageType.Text) continue;
                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage) continue;
                    message.Position = 0;
                    try
                    {
                        using var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                        await onMessage(document).ConfigureAwait(false);
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
    /// Performs copy with viewer as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="type">Type value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="viewerId">Identifier of the viewer to use for this operation.</param>
    /// <returns>The byte produced by the operation.</returns>
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
    /// Performs copy with type as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="type">Type value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="includeViewerId">Identifier of the include viewer to use for this operation.</param>
    /// <returns>The byte produced by the operation.</returns>
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
    /// Reads string as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="property">Property value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Attempts to read viewer identifier as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="viewerId">Identifier of the viewer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Closes quietly as part of the web rtc signaling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="socket">Socket value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <param name="reason">Reason value supplied to the web rtc signaling operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CloseQuietlyAsync(WebSocket socket, string reason)
    {
    try
    {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None).ConfigureAwait(false);
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
    /// Represents a viewer connection helper type nested within <see cref="WebRtcSignalingService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="socket">Socket value supplied to the web rtc signaling operation and used when producing its result.</param>
    private sealed class ViewerConnection(WebSocket socket) : IAsyncDisposable
    {
        /// <summary>
        /// Stores the internal socket state used by <see cref="ViewerConnection"/> while executing its surrounding workflow.
        /// </summary>
        private readonly WebSocket _socket = socket;
        /// <summary>
        /// Stores the synchronization primitive that protects concurrent access to send state owned by <see cref="ViewerConnection"/>.
        /// </summary>
        private readonly SemaphoreSlim _send = new(1, 1);

        /// <summary>
        /// Performs send for <see cref="ViewerConnection"/>, keeping the operation consistent with the state and invariants of the surrounding viewer connection workflow.
        /// </summary>
        /// <param name="payload">Payload value supplied to the viewer connection operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken)
        {
    try
    {
                if (_socket.State != WebSocketState.Open) return;
                await _send.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_socket.State == WebSocketState.Open)
                        await _socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
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
        /// Releases resources owned by <see cref="ViewerConnection"/> and leaves the viewer connection workflow in a safely disposed state.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
        public async ValueTask DisposeAsync()
        {
    try
    {
                try
                {
                    if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Viewer disconnected.", CancellationToken.None).ConfigureAwait(false);
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
