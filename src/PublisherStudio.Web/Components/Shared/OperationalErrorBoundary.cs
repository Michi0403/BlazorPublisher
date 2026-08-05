using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PublisherStudio.Services.UserExperience;

namespace PublisherStudio.Components.Shared;

/// <summary>
/// Logs unhandled component failures and surfaces one circuit-scoped notification.
/// Dispose paths are intentionally not instrumented because circuit shutdown is expected.
/// </summary>
public sealed class OperationalErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<OperationalErrorBoundary> Logger { get; set; } = default!;
    [Inject] private IUserNotificationService Notifications { get; set; } = default!;

    /// <summary>
    /// Runs the on error async operation.
    /// </summary>
    protected override Task OnErrorAsync(Exception exception)
    {
        if (exception is OperationCanceledException or TaskCanceledException or JSDisconnectedException)
        {
            Logger.LogDebug(exception, "PublisherStudio interactive work ended during normal cancellation or circuit shutdown.");
            return Task.CompletedTask;
        }

        Logger.LogError(exception, "Unhandled PublisherStudio component failure in the active interactive circuit.");
        Notifications.Error(
            "The current view encountered an error. Review the local application log for the full exception.",
            "PublisherStudio component error",
            nameof(OperationalErrorBoundary),
            persistent: true);
        return Task.CompletedTask;
    }
}
