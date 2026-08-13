using PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
namespace PublisherStudio.Services.Streaming.UseCases.Chat;

/// <summary>
/// Defines the contract for streaming chat result behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IStreamingChatResultFactory
{
    /// <summary>
    /// Creates not found using the configuration and dependencies owned by <see cref="IStreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
    StreamingChatSendResult CreateNotFound();
    /// <summary>
    /// Creates accepted using the configuration and dependencies owned by <see cref="IStreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
    StreamingChatSendResult CreateAccepted();
    /// <summary>
    /// Creates not configured using the configuration and dependencies owned by <see cref="IStreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
    StreamingChatSendResult CreateNotConfigured();
    /// <summary>
    /// Creates failure using the configuration and dependencies owned by <see cref="IStreamingChatResultFactory"/>.
    /// </summary>
    /// <param name="error">Error value supplied to the streaming chat result operation and used when producing its result.</param>
    /// <returns>The streaming chat send result produced by the operation.</returns>
    StreamingChatSendResult CreateFailure(string error);
}

/// <summary>
/// Creates configured streaming chat result instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class StreamingChatResultFactory(ILogger<StreamingChatResultFactory> logger) : IStreamingChatResultFactory
{
    /// <summary>
    /// Creates not found using the configuration and dependencies owned by <see cref="StreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
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
    /// Creates accepted using the configuration and dependencies owned by <see cref="StreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
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
    /// Creates not configured using the configuration and dependencies owned by <see cref="StreamingChatResultFactory"/>.
    /// </summary>
    /// <returns>The streaming chat send result produced by the operation.</returns>
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
    /// Creates failure using the configuration and dependencies owned by <see cref="StreamingChatResultFactory"/>.
    /// </summary>
    /// <param name="error">Error value supplied to the streaming chat result operation and used when producing its result.</param>
    /// <returns>The streaming chat send result produced by the operation.</returns>
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
