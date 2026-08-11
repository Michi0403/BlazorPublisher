using PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Defines the streaming chat result factory contract.
/// </summary>
public interface IStreamingChatResultFactory
{
    /// <summary>
    /// Creates not found.
    /// </summary>
    StreamingChatSendResult CreateNotFound();
    /// <summary>
    /// Creates accepted.
    /// </summary>
    StreamingChatSendResult CreateAccepted();
    /// <summary>
    /// Creates not configured.
    /// </summary>
    StreamingChatSendResult CreateNotConfigured();
    /// <summary>
    /// Creates failure.
    /// </summary>
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
    try
    {
            logger.LogTrace("Creating a streaming-chat not-found result.");
            return new StreamingChatSendResult(false, false, string.Empty);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateNotFound)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateNotFound)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates accepted.
    /// </summary>
    public StreamingChatSendResult CreateAccepted()
    {
    try
    {
            logger.LogTrace("Creating a streaming-chat accepted result.");
            return new StreamingChatSendResult(true, true, string.Empty);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateAccepted)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateAccepted)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates not configured.
    /// </summary>
    public StreamingChatSendResult CreateNotConfigured()
    {
    try
    {
            logger.LogTrace("Creating a streaming-chat not-configured result.");
            return new StreamingChatSendResult(true, false, "Chat is not configured for this output.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateNotConfigured)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateNotConfigured)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates failure.
    /// </summary>
    public StreamingChatSendResult CreateFailure(string error)
    {
    try
    {
            logger.LogTrace("Creating a streaming-chat failure result.");
            return new StreamingChatSendResult(true, false, error ?? string.Empty);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateFailure)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StreamingChatResultFactory)}.{nameof(CreateFailure)} failed.");
        throw;
    }
}
}
