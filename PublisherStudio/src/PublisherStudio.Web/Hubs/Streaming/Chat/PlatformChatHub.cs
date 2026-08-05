using System.Net.WebSockets;

namespace PublisherStudio.Hubs.Streaming.Chat;

/// <summary>
/// WebSocket entry point for platform-chat subscriptions. Reusable provider, history and
/// session processing stays in Services/Streaming; controllers only own HTTP negotiation.
/// </summary>
public sealed class PlatformChatHub(StreamingChatUseCases useCases)
{
    /// <summary>
    /// Determines whether open.
    /// </summary>
    public bool CanOpen(Guid sessionId) => useCases.CanOpen(sessionId);

    /// <summary>
    /// Runs the run subscriber async operation.
    /// </summary>
    public Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunSubscriberAsync(sessionId, outputId, socket, cancellationToken);
}
