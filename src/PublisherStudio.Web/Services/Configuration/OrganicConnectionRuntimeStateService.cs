using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates organic connection runtime state behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicConnectionRuntimeStateService(
    ILogger<OrganicConnectionRuntimeStateService> logger) : IOrganicConnectionRuntimeState
{
    /// <summary>
    /// Stores the internal gate state used by <see cref="OrganicConnectionRuntimeStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object gate = new();
    /// <summary>
    /// Stores the internal connection identifier state used by <see cref="OrganicConnectionRuntimeStateService"/> while executing its surrounding workflow.
    /// </summary>
    private Guid connectionId;
    /// <summary>
    /// Stores the internal peer identifier state used by <see cref="OrganicConnectionRuntimeStateService"/> while executing its surrounding workflow.
    /// </summary>
    private string peerId = string.Empty;
    /// <summary>
    /// Stores the internal is loopback state used by <see cref="OrganicConnectionRuntimeStateService"/> while executing its surrounding workflow.
    /// </summary>
    private bool isLoopback = true;

    /// <summary>
    /// Retrieves snapshot as part of the organic connection runtime state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The organic connection runtime snapshot produced by the operation.</returns>
    public OrganicConnectionRuntimeSnapshot GetSnapshot()
    {
        try
        {
            lock (gate)
            {
                return new OrganicConnectionRuntimeSnapshot
                {
                    ConnectionId = connectionId,
                    PeerId = peerId,
                    IsLoopback = isLoopback,
                    IsConnected = connectionId != Guid.Empty
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not read the organic connection runtime state.");
            throw;
        }
    }

    /// <summary>
    /// Sets connected as part of the organic connection runtime state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="newConnectionId">Identifier of the new connection to use for this operation.</param>
    /// <param name="newPeerId">Identifier of the new peer to use for this operation.</param>
    /// <param name="newIsLoopback">Value indicating whether new is loopback should apply to this operation.</param>
    public void SetConnected(Guid newConnectionId, string newPeerId, bool newIsLoopback)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newPeerId);
            if (newConnectionId == Guid.Empty)
                throw new ArgumentException("A connection id is required.", nameof(newConnectionId));
            lock (gate)
            {
                connectionId = newConnectionId;
                peerId = newPeerId;
                isLoopback = newIsLoopback;
            }
            logger.LogDebug($"Recorded organic connection {newConnectionId} for peer {newPeerId}; loopback={newIsLoopback}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not record organic connection {newConnectionId} for peer {newPeerId}.");
            throw;
        }
    }

    /// <summary>
    /// Performs reset as part of the organic connection runtime state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expectedConnectionId">Identifier of the expected connection to use for this operation.</param>
    public void Reset(Guid expectedConnectionId)
    {
        try
        {
            lock (gate)
            {
                if (expectedConnectionId != Guid.Empty && connectionId != expectedConnectionId)
                    return;
                connectionId = Guid.Empty;
                peerId = string.Empty;
                isLoopback = true;
            }
            logger.LogDebug($"Cleared organic connection runtime state for {expectedConnectionId}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not clear organic connection runtime state for {expectedConnectionId}.");
            throw;
        }
    }
}
