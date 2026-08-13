using PublisherStudio.BusinessObjects;

namespace PublisherStudio.HostedServices.Streaming;

/// <summary>
/// Coordinates twitch o auth maintenance behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="services">Service provider dependency used by the twitch o auth maintenance workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class TwitchOAuthMaintenanceService(
    IServiceProvider services,
    ILogger<TwitchOAuthMaintenanceService> logger) : BackgroundService
{
    /// <summary>
    /// Performs execute as part of the twitch o auth maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<StreamingProfileStore>();
                var twitch = scope.ServiceProvider.GetRequiredService<TwitchOAuthService>();
                var profiles = (await store.LoadAsync(stoppingToken)).Providers
                    .Where(profile => profile.Provider == PublicationStreamProvider.Twitch
                        && profile.AuthenticationMode == StreamingProviderAuthenticationMode.OAuth
                        && profile.HasStoredOAuthSession)
                    .ToList();
                foreach (var profile in profiles)
                {
                    try { await twitch.ValidateProfileAsync(profile.Id, stoppingToken); }
                    catch (Exception exception) { logger.LogWarning(exception, "Twitch OAuth validation failed for profile {ProfileId}.", profile.Id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The Twitch OAuth maintenance cycle failed.");
            }

            try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
