using PublisherStudio.Domain;

namespace PublisherStudio.HostedServices.Streaming;

public sealed class TwitchOAuthMaintenanceService(
    IServiceProvider services,
    ILogger<TwitchOAuthMaintenanceService> logger) : BackgroundService
{
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
