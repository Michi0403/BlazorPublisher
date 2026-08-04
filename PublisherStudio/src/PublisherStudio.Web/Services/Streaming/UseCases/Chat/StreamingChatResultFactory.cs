using PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
namespace PublisherStudio.Services.Streaming.UseCases.Chat;

public interface IStreamingChatResultFactory
{
    StreamingChatSendResult CreateNotFound();
    StreamingChatSendResult CreateAccepted();
    StreamingChatSendResult CreateNotConfigured();
    StreamingChatSendResult CreateFailure(string error);
}

public sealed class StreamingChatResultFactory(ILogger<StreamingChatResultFactory> logger) : IStreamingChatResultFactory
{
    public StreamingChatSendResult CreateNotFound()
    {
        logger.LogTrace("Creating a streaming-chat not-found result.");
        return new StreamingChatSendResult(false, false, string.Empty);
    }

    public StreamingChatSendResult CreateAccepted()
    {
        logger.LogTrace("Creating a streaming-chat accepted result.");
        return new StreamingChatSendResult(true, true, string.Empty);
    }

    public StreamingChatSendResult CreateNotConfigured()
    {
        logger.LogTrace("Creating a streaming-chat not-configured result.");
        return new StreamingChatSendResult(true, false, "Chat is not configured for this output.");
    }

    public StreamingChatSendResult CreateFailure(string error)
    {
        logger.LogTrace("Creating a streaming-chat failure result.");
        return new StreamingChatSendResult(true, false, error ?? string.Empty);
    }
}
