namespace PublisherStudio.HostedServices.Streaming;

/// <summary>
/// Owns application-lifetime startup and shutdown for the reusable global-hotkey service.
/// Hotkey registration, event queues and Windows interop remain in Services so controllers,
/// UI orchestration and other hosted services can reuse the same capability.
/// </summary>
public sealed class GlobalHotkeyHostedService(GlobalHotkeyService hotkeys) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => hotkeys.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => hotkeys.StopAsync(cancellationToken);
}
