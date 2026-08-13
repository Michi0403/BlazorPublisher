using System.Net.WebSockets;

namespace PublisherStudio.Hubs.Streaming.Chat;

/// <summary>
/// WebSocket entry point for platform-chat subscriptions. Reusable provider, history and
/// session processing stays in Services/Streaming; controllers only own HTTP negotiation.
/// </summary>
/// <param name="useCases">Use cases value supplied to the platform chat hub operation and used when producing its result.</param>
public sealed class PlatformChatHub(StreamingChatUseCases useCases)
{
    /// <summary>
    /// Determines whether open for <see cref="PlatformChatHub"/>, keeping the operation consistent with the state and invariants of the surrounding platform chat hub workflow.
    /// </summary>
    /// <returns>The bool can open GUID session identifier use cases produced by the operation.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    public bool CanOpen(Guid sessionId) => useCases.CanOpen(sessionId);

    /// <summary>
    /// Performs run subscriber for <see cref="PlatformChatHub"/>, keeping the operation consistent with the state and invariants of the surrounding platform chat hub workflow.
    /// </summary>
    /// <returns>The task bool run subscriber async GUID session identifier GUID output identifier web socket socket cancellation token cancellation token use cases produced by the operation.</returns>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="socket">Socket value supplied to the platform chat hub operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    public Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunSubscriberAsync(sessionId, outputId, socket, cancellationToken);
}
