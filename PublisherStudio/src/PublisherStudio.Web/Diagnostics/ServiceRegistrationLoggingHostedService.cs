using PublisherStudio.Services.Automation;

namespace PublisherStudio.Diagnostics;

/// <summary>
/// Provides service registration logging hosted service operations.
/// </summary>
public sealed class ServiceRegistrationLoggingHostedService(
    IEnumerable<ServiceArchitectureDescriptor> descriptors,
    ILogger<ServiceRegistrationLoggingHostedService> logger) : IHostedService
{
    /// <summary>
    /// Starts async.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var services = descriptors
                .OrderBy(descriptor => descriptor.ImplementationType.FullName, StringComparer.Ordinal)
                .ToArray();
            logger.LogInformation("PublisherStudio registered {ServiceCount} application services.", services.Length);
            foreach (var descriptor in services)
            {
                logger.LogDebug(
                    "Application service {ImplementationType} exposes {ContractType} with lifetime {Lifetime}.",
                    descriptor.ImplementationType.FullName,
                    descriptor.InterfaceType?.FullName ?? descriptor.ImplementationType.FullName,
                    descriptor.Lifetime);
            }
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio service-registration diagnostics failed.");
            throw;
        }
    }

    /// <summary>
    /// Stops async.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("PublisherStudio service-registration diagnostics stopped.");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio service-registration shutdown diagnostics failed.");
            throw;
        }
    }
}
