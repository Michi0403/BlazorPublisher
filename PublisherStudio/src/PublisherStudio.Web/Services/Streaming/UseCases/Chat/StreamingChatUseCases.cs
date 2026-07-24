using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Keeps provider-chat session lookup and send/subscription orchestration outside MVC controllers.
/// </summary>
public sealed class StreamingChatUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    public bool CanOpen(Guid sessionId) =>
        _sessions.TryGet(sessionId, out var session) && session.Chat is not null;

    public async Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGet(sessionId, out var session) || session.Chat is null) return false;
        await session.Chat.RunSubscriberAsync(outputId, socket, cancellationToken);
        return true;
    }

    public async Task<StreamingChatSendResult> SendAsync(
        Guid sessionId,
        Guid outputId,
        string message,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGet(sessionId, out var session) || session.Chat is null)
            return StreamingChatSendResult.NotFound;
        try
        {
            return await session.Chat.SendAsync(outputId, message, cancellationToken)
                ? StreamingChatSendResult.Accepted
                : StreamingChatSendResult.NotConfigured;
        }
        catch (Exception exception)
        {
            return StreamingChatSendResult.Failed(exception.Message);
        }
    }
}

public sealed record StreamingChatSendResult(bool Exists, bool Sent, string Error)
{
    public static StreamingChatSendResult NotFound { get; } = new(false, false, string.Empty);
    public static StreamingChatSendResult Accepted { get; } = new(true, true, string.Empty);
    public static StreamingChatSendResult NotConfigured { get; } = new(true, false, "Chat is not configured for this output.");
    public static StreamingChatSendResult Failed(string error) => new(true, false, error);
}
