using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides organic connection runtime state service operations.
/// </summary>
public sealed class OrganicConnectionRuntimeStateService(
    ILogger<OrganicConnectionRuntimeStateService> logger) : IOrganicConnectionRuntimeState
{
    private readonly object gate = new();
    private Guid connectionId;
    private string peerId = string.Empty;
    private bool isLoopback = true;

    /// <summary>
    /// Gets snapshot.
    /// </summary>
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
    /// Sets connected.
    /// </summary>
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
    /// Runs the reset operation.
    /// </summary>
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
