namespace PublisherStudio.Services.Streaming;

/// <summary>
/// Registers the streaming monolith using PublisherStudio's established architectural roots.
/// MVC controllers expose transport routes, use-case services orchestrate operations, backend
/// services own provider/media details, and hosted services own long-running maintenance.
/// </summary>
public static class StreamingServiceCollectionExtensions
{
    public static IServiceCollection AddPublisherStreaming(this IServiceCollection services)
    {
        services.AddSingleton<GlobalHotkeyService>();
        services.AddHostedService(provider => provider.GetRequiredService<GlobalHotkeyService>());
        services.AddSingleton<EncoderOrchestrator>();
        services.AddSingleton<NativeCaptureRegistry>();
        services.AddSingleton<MediaSessionRegistry>();

        services.AddSingleton<StreamingRuntimeUseCases>();
        services.AddSingleton<NativeCaptureUseCases>();
        services.AddSingleton<StreamingSessionUseCases>();
        services.AddSingleton<StreamingChatUseCases>();
        services.AddSingleton<StreamingIngestUseCases>();
        services.AddSingleton<StreamingLanUseCases>();
        return services;
    }
}
