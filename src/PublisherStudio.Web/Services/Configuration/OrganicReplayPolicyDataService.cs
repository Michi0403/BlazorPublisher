using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates organic replay policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="systemVariables">Persisted operator system-variable policy store.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicReplayPolicyDataService(
    ISystemVariableStoreService systemVariables,
    ILogger<OrganicReplayPolicyDataService> logger) : IOrganicReplayPolicyDataService
{
    /// <summary>
    /// Stores the internal snapshot state used by <see cref="OrganicReplayPolicyDataService"/> while executing its surrounding workflow.
    /// </summary>
    /// <value>The snapshot value exposed by <see cref="OrganicReplayPolicyDataService"/>.</value>
    private OrganicReplayPolicySnapshot Snapshot => new()
    {
        Retention = TimeSpan.FromMinutes(15),
        AllowedFutureSkew = TimeSpan.FromMinutes(2),
        CleanupInterval = 64,
        MaximumTrackedMessages = systemVariables.OrganicReplayMaximumTrackedMessages
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
            return Snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the configured organic replay policy.");
            throw;
        }
    }
}
