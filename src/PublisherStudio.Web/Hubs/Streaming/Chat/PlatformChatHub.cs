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
    public bool CanOpen(Guid sessionId) => useCases.CanOpen(sessionId);

    /// <summary>
    /// Performs run subscriber for <see cref="PlatformChatHub"/>, keeping the operation consistent with the state and invariants of the surrounding platform chat hub workflow.
    /// </summary>
    /// <returns>The task bool run subscriber async GUID session identifier GUID output identifier web socket socket cancellation token cancellation token use cases produced by the operation.</returns>
    public Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunSubscriberAsync(sessionId, outputId, socket, cancellationToken);
}
