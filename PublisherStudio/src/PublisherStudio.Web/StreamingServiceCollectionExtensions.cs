using PublisherStudio.HostedServices.Streaming;
using PublisherStudio.Hubs.Streaming.Chat;
using PublisherStudio.Hubs.Streaming.Lan;

namespace PublisherStudio;

/// <summary>
/// Registers the streaming monolith using PublisherStudio's established architectural roots.
/// Controllers and hubs are backend entry points, reusable services own data processing and
/// technical I/O, and hosted services only own application-lifetime scheduling/lifecycle.
/// </summary>
public static class StreamingServiceCollectionExtensions
{
    public static IServiceCollection AddPublisherStreaming(this IServiceCollection services, ILogger logger)
    {
        try
        {
        services.AddSingleton<IWindowsHotkeyNativeService, WindowsHotkeyNativeService>();
        services.AddSingleton<IWindowsProcessLoopbackNativeService, WindowsProcessLoopbackNativeService>();
        services.AddSingleton<IWindowsProcessLoopbackCaptureFactory, WindowsProcessLoopbackCaptureFactory>();
        services.AddSingleton<GlobalHotkeyService>();
        services.AddHostedService<GlobalHotkeyHostedService>();
        services.AddSingleton<EncoderOrchestrator>();
        services.AddSingleton<INativeCaptureSessionFactory, NativeCaptureSessionFactory>();
        services.AddSingleton<NativeCaptureRegistry>();
        services.AddSingleton<IMediaSessionFactory, MediaSessionFactory>();
        services.AddSingleton<IPlatformChatServiceFactory, PlatformChatServiceFactory>();
        services.AddSingleton<ILanStreamingServerFactory, LanStreamingServerFactory>();
        services.AddSingleton<MediaSessionRegistry>();

        services.AddSingleton<StreamingRuntimeUseCases>();
        services.AddSingleton<NativeCaptureUseCases>();
        services.AddSingleton<StreamingSessionUseCases>();
        services.AddSingleton<IStreamingChatResultFactory, StreamingChatResultFactory>();
        services.AddSingleton<StreamingChatUseCases>();
        services.AddSingleton<StreamingIngestUseCases>();
        services.AddSingleton<StreamingLanUseCases>();
        services.AddSingleton<PlatformChatHub>();
        services.AddSingleton<WebRtcSignalingHub>();
        logger.LogInformation($"Registered PublisherStudio streaming services.");
        return services;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio streaming service registration failed: {exception.Message}");
            throw;
        }
    }
}
