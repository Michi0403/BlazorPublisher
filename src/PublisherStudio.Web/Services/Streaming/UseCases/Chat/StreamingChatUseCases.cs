using PublisherStudio.BusinessObjects;
using System.Net.WebSockets;

namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Keeps provider-chat session lookup and send/subscription orchestration outside MVC controllers.
/// </summary>
/// <param name="sessions">Media session registry dependency used by the streaming chat use cases workflow to provide the corresponding application capability.</param>
/// <param name="results">Streaming chat result factory dependency used by the streaming chat use cases workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class StreamingChatUseCases(
    MediaSessionRegistry sessions,
    IStreamingChatResultFactory results,
    ILogger<StreamingChatUseCases> logger)
{
    /// <summary>
    /// Stores the media session registry dependency used by <see cref="StreamingChatUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Determines whether open for <see cref="StreamingChatUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming chat use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs run subscriber for <see cref="StreamingChatUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming chat use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="socket">Socket value supplied to the streaming chat use cases operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs send for <see cref="StreamingChatUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming chat use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="message">Message value supplied to the streaming chat use cases operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming chat send result produced by the operation.</returns>
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
