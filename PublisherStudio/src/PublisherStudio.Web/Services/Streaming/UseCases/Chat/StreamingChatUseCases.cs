using PublisherStudio.BusinessObjects;
using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Keeps provider-chat session lookup and send/subscription orchestration outside MVC controllers.
/// </summary>
public sealed class StreamingChatUseCases(
    MediaSessionRegistry sessions,
    IStreamingChatResultFactory results,
    ILogger<StreamingChatUseCases> logger)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    public bool CanOpen(Guid sessionId) {
        try
        {
            logger.LogTrace($"Entering StreamingChatUseCases.CanOpen.");
            return _sessions.TryGet(sessionId, out var session) && session.Chat is not null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingChatUseCases.CanOpen failed: {exception.Message}");
            throw;
        }
    }

    public async Task<bool> RunSubscriberAsync(
        Guid sessionId,
        Guid outputId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering StreamingChatUseCases.RunSubscriberAsync.");
                    if (!_sessions.TryGet(sessionId, out var session) || session.Chat is null) return false;
                    await session.Chat.RunSubscriberAsync(outputId, socket, cancellationToken).ConfigureAwait(false);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingChatUseCases.RunSubscriberAsync failed: {exception.Message}");
            throw;
        }
    }

    public async Task<StreamingChatSendResult> SendAsync(
        Guid sessionId,
        Guid outputId,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering StreamingChatUseCases.SendAsync.");
                    if (!_sessions.TryGet(sessionId, out var session) || session.Chat is null)
                        return results.CreateNotFound();
                    try
                    {
                        return await session.Chat.SendAsync(outputId, message, cancellationToken).ConfigureAwait(false)
                            ? results.CreateAccepted()
                            : results.CreateNotConfigured();
                    }
                    catch (Exception exception)
                    {
                        return results.CreateFailure(exception.Message);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingChatUseCases.SendAsync failed: {exception.Message}");
            throw;
        }
    }
}
