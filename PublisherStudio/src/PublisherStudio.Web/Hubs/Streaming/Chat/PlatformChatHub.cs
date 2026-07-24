using System.Net.WebSockets;

namespace PublisherStudio.Hubs.Streaming.Chat;

/// <summary>
/// WebSocket entry point for platform-chat subscriptions. Reusable provider, history and
/// session processing stays in Services/Streaming; controllers only own HTTP negotiation.
/// </summary>
public sealed class PlatformChatHub(StreamingChatUseCases useCases)
{
    public bool CanOpen(Guid sessionId) => useCases.CanOpen(sessionId);

    public Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken) =>
        useCases.RunSubscriberAsync(sessionId, outputId, socket, cancellationToken);
}
