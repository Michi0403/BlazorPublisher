namespace PublisherStudio.Services.Streaming.Sessions;

/// <summary>
/// Defines the contract for platform chat service behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPlatformChatServiceFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="IPlatformChatServiceFactory"/>.
    /// </summary>
    /// <param name="session">Session value supplied to the platform chat service operation and used when producing its result.</param>
    /// <returns>The platform chat service produced by the operation.</returns>
    PlatformChatService Create(MediaSession session);
}

/// <summary>
/// Creates configured platform chat service instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="loggerFactory">Logger factory dependency used by the platform chat service workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PlatformChatServiceFactory(
    ILoggerFactory loggerFactory,
    ILogger<PlatformChatServiceFactory> logger) : IPlatformChatServiceFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="PlatformChatServiceFactory"/>.
    /// </summary>
    /// <param name="session">Session value supplied to the platform chat service operation and used when producing its result.</param>
    /// <returns>The platform chat service produced by the operation.</returns>
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
/// Defines the contract for LAN streaming server behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILanStreamingServerFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="ILanStreamingServerFactory"/>.
    /// </summary>
    /// <param name="session">Session value supplied to the LAN streaming server operation and used when producing its result.</param>
    /// <returns>The LAN streaming server produced by the operation.</returns>
    LanStreamingServer Create(MediaSession session);
}

/// <summary>
/// Creates configured LAN streaming server instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="loggerFactory">Logger factory dependency used by the LAN streaming server workflow to provide the corresponding application capability.</param>
/// <param name="taskRunner">Supervised task runner used by the LAN and RTSP server lifetimes.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LanStreamingServerFactory(
    ILoggerFactory loggerFactory,
    ISupervisedTaskRunner taskRunner,
    IPublisherPlatformRuntimeService platform,
    ILogger<LanStreamingServerFactory> logger) : ILanStreamingServerFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="LanStreamingServerFactory"/>.
    /// </summary>
    /// <param name="session">Session value supplied to the LAN streaming server operation and used when producing its result.</param>
    /// <returns>The LAN streaming server produced by the operation.</returns>
    public LanStreamingServer Create(MediaSession session)
    {
        try
        {
            logger.LogTrace("Creating the LAN streaming server for media session {SessionId}.", session.Id);
            return new LanStreamingServer(session, taskRunner, platform, loggerFactory.CreateLogger<LanStreamingServer>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create the LAN streaming server for media session {SessionId}.", session.Id);
            throw;
        }
    }
}
