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
    /// <summary>
    /// Gets or sets the logger value that forms part of the operational error boundary state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logger value exposed by <see cref="OperationalErrorBoundary"/>.</value>
    [Inject] private ILogger<OperationalErrorBoundary> Logger { get; set; } = default!;
    /// <summary>
    /// Gets or sets the notifications value that forms part of the operational error boundary state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notifications value exposed by <see cref="OperationalErrorBoundary"/>.</value>
    [Inject] private IUserNotificationService Notifications { get; set; } = default!;

    /// <summary>
    /// Handles the error async lifecycle or event notification for <see cref="OperationalErrorBoundary"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="exception">Exception value supplied to the operational error boundary operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
