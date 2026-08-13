using PublisherStudio.Services.Automation;

namespace PublisherStudio.Diagnostics;

/// <summary>
/// Coordinates service registration logging behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="descriptors">Service architecture descriptor dependency used by the service registration logging workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ServiceRegistrationLoggingHostedService(
    IEnumerable<ServiceArchitectureDescriptor> descriptors,
    ILogger<ServiceRegistrationLoggingHostedService> logger) : IHostedService
{
    /// <summary>
    /// Performs start as part of the service registration logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs stop as part of the service registration logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
