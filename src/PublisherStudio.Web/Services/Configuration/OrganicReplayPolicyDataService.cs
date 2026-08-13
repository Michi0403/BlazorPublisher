using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates organic replay policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicReplayPolicyDataService(
    ILogger<OrganicReplayPolicyDataService> logger) : IOrganicReplayPolicyDataService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly OrganicReplayPolicySnapshot snapshot = new()
    {
        Retention = TimeSpan.FromMinutes(15),
        AllowedFutureSkew = TimeSpan.FromMinutes(2),
        CleanupInterval = 64,
        MaximumTrackedMessages = 4096
    };

    /// <summary>
    /// Retrieves snapshot as part of the organic replay policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The organic replay policy snapshot produced by the operation.</returns>
    public OrganicReplayPolicySnapshot GetSnapshot()
    {
        try
        {
            logger.LogTrace($"Returned the configured organic replay policy.");
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the configured organic replay policy.");
            throw;
        }
    }
}
