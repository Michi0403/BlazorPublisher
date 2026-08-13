namespace PublisherStudio.HostedServices.Streaming;

/// <summary>
/// Owns application-lifetime startup and shutdown for the reusable global-hotkey service.
/// Hotkey registration, event queues and Windows interop remain in Services so controllers,
/// UI orchestration and other hosted services can reuse the same capability.
/// </summary>
/// <param name="hotkeys">Global hotkey service dependency used by the global hotkey workflow to provide the corresponding application capability.</param>
public sealed class GlobalHotkeyHostedService(GlobalHotkeyService hotkeys) : IHostedService
{
    /// <summary>
    /// Performs start as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The task start async cancellation token cancellation token hotkeys produced by the operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken) => hotkeys.StartAsync(cancellationToken);

    /// <summary>
    /// Performs stop as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The task stop async cancellation token cancellation token hotkeys produced by the operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => hotkeys.StopAsync(cancellationToken);
}
