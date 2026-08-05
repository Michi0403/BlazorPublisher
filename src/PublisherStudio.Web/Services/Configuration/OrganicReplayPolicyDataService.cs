using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides organic replay policy data service operations.
/// </summary>
public sealed class OrganicReplayPolicyDataService(
    ILogger<OrganicReplayPolicyDataService> logger) : IOrganicReplayPolicyDataService
{
    private readonly OrganicReplayPolicySnapshot snapshot = new()
    {
        Retention = TimeSpan.FromMinutes(15),
        AllowedFutureSkew = TimeSpan.FromMinutes(2),
        CleanupInterval = 64,
        MaximumTrackedMessages = 4096
    };

    /// <summary>
    /// Gets snapshot.
    /// </summary>
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
