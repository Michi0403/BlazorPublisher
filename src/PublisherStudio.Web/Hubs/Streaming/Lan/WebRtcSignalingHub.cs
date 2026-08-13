using System.Net.WebSockets;

namespace PublisherStudio.Hubs.Streaming.Lan;

/// <summary>
/// WebSocket entry point for the renderer-side WebRTC signaling connection. Signaling state
/// and session lookup remain reusable services; the hub exposes the persistent connection role.
/// </summary>
/// <param name="useCases">Use cases value supplied to the web rtc signaling hub operation and used when producing its result.</param>
public sealed class WebRtcSignalingHub(StreamingIngestUseCases useCases)
{
    /// <summary>
    /// Determines whether publish for <see cref="WebRtcSignalingHub"/>, keeping the operation consistent with the state and invariants of the surrounding web rtc signaling hub workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanPublish(Guid sessionId) => useCases.CanPublishWebRtc(sessionId);

    /// <summary>
    /// Performs run publisher for <see cref="WebRtcSignalingHub"/>, keeping the operation consistent with the state and invariants of the surrounding web rtc signaling hub workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="socket">Socket value supplied to the web rtc signaling hub operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> RunPublisherAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunWebRtcPublisherAsync(sessionId, socket, cancellationToken);
}
