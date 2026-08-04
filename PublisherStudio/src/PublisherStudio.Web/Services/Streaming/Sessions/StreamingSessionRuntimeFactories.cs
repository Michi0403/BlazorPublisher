namespace PublisherStudio.Services.Streaming.Sessions;

public interface IPlatformChatServiceFactory
{
    PlatformChatService Create(MediaSession session);
}

public sealed class PlatformChatServiceFactory(
    ILoggerFactory loggerFactory,
    ILogger<PlatformChatServiceFactory> logger) : IPlatformChatServiceFactory
{
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

public interface ILanStreamingServerFactory
{
    LanStreamingServer Create(MediaSession session);
}

public sealed class LanStreamingServerFactory(
    ILoggerFactory loggerFactory,
    ILogger<LanStreamingServerFactory> logger) : ILanStreamingServerFactory
{
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
