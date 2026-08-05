namespace PublisherStudio.Services.Streaming.Sessions;

/// <summary>
/// Defines the platform chat service factory contract.
/// </summary>
public interface IPlatformChatServiceFactory
{
    PlatformChatService Create(MediaSession session);
}

/// <summary>
/// Provides platform chat service factory operations.
/// </summary>
public sealed class PlatformChatServiceFactory(
    ILoggerFactory loggerFactory,
    ILogger<PlatformChatServiceFactory> logger) : IPlatformChatServiceFactory
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    public PlatformChatService Create(MediaSession session)
    {
        try
        {
            logger.LogTrace("Creating the platform-chat service for media session {SessionId}.", session.Id);
            return new PlatformChatService(session, loggerFactory.CreateLogger<PlatformChatService>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create the platform-chat service for media session {SessionId}.", session.Id);
            throw;
        }
    }
}

/// <summary>
/// Defines the LAN streaming server factory contract.
/// </summary>
public interface ILanStreamingServerFactory
{
    LanStreamingServer Create(MediaSession session);
}

/// <summary>
/// Provides LAN streaming server factory operations.
/// </summary>
public sealed class LanStreamingServerFactory(
    ILoggerFactory loggerFactory,
    ILogger<LanStreamingServerFactory> logger) : ILanStreamingServerFactory
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    public LanStreamingServer Create(MediaSession session)
    {
        try
        {
            logger.LogTrace("Creating the LAN streaming server for media session {SessionId}.", session.Id);
            return new LanStreamingServer(session, loggerFactory.CreateLogger<LanStreamingServer>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create the LAN streaming server for media session {SessionId}.", session.Id);
            throw;
        }
    }
}
