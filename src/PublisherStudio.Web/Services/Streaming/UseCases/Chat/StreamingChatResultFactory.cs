using PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Defines the streaming chat result factory contract.
/// </summary>
public interface IStreamingChatResultFactory
{
    StreamingChatSendResult CreateNotFound();
    StreamingChatSendResult CreateAccepted();
    StreamingChatSendResult CreateNotConfigured();
    StreamingChatSendResult CreateFailure(string error);
}

/// <summary>
/// Provides streaming chat result factory operations.
/// </summary>
public sealed class StreamingChatResultFactory(ILogger<StreamingChatResultFactory> logger) : IStreamingChatResultFactory
{
    /// <summary>
    /// Creates not found.
    /// </summary>
    public StreamingChatSendResult CreateNotFound()
    {
        logger.LogTrace("Creating a streaming-chat not-found result.");
        return new StreamingChatSendResult(false, false, string.Empty);
    }

    /// <summary>
    /// Creates accepted.
    /// </summary>
    public StreamingChatSendResult CreateAccepted()
    {
        logger.LogTrace("Creating a streaming-chat accepted result.");
        return new StreamingChatSendResult(true, true, string.Empty);
    }

    /// <summary>
    /// Creates not configured.
    /// </summary>
    public StreamingChatSendResult CreateNotConfigured()
    {
        logger.LogTrace("Creating a streaming-chat not-configured result.");
        return new StreamingChatSendResult(true, false, "Chat is not configured for this output.");
    }

    /// <summary>
    /// Creates failure.
    /// </summary>
    public StreamingChatSendResult CreateFailure(string error)
    {
        logger.LogTrace("Creating a streaming-chat failure result.");
        return new StreamingChatSendResult(true, false, error ?? string.Empty);
    }
}
