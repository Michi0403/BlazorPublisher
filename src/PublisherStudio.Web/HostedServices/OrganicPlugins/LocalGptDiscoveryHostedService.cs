using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace PublisherStudio.HostedServices.OrganicPlugins;

/// <summary>
/// Coordinates LocalGPT discovery behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
/// <param name="registry">Local gpt discovery registry dependency used by the LocalGPT discovery workflow to provide the corresponding application capability.</param>
/// <param name="connection">Local gpt connection service dependency used by the LocalGPT discovery workflow to provide the corresponding application capability.</param>
/// <param name="codec">Organic plugin protocol codec dependency used by the LocalGPT discovery workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalGptDiscoveryHostedService(
    IOptions<OrganicPluginOptions> options,
    ILocalGptDiscoveryRegistry registry,
    ILocalGptConnectionService connection,
    IOrganicPluginProtocolCodec codec,
    ILogger<LocalGptDiscoveryHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Stores the internal auto connect in progress state used by <see cref="LocalGptDiscoveryHostedService"/> while executing its surrounding workflow.
    /// </summary>
    private int autoConnectInProgress;
    /// <summary>
    /// Stores the in-memory automatically attempted peers collection maintained internally by <see cref="LocalGptDiscoveryHostedService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<string> automaticallyAttemptedPeers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Performs execute as part of the LocalGPT discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || !options.Value.EnableDiscovery) return;

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        try
        {
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, options.Value.DiscoveryPort));
        }
        catch (SocketException ex)
        {
            logger.LogError(ex, "Optional LocalGPT discovery could not bind UDP {Port}. PublisherStudio remains usable without organic plugins.", options.Value.DiscoveryPort);
            return;
        }

        logger.LogInformation("PublisherStudio listening for LocalGPT discovery broadcasts on UDP {Port}.", options.Value.DiscoveryPort);
        var peerExpiry = TimeSpan.FromSeconds(Math.Clamp(options.Value.PeerExpirySeconds, 10, 300));
        var receivePoll = TimeSpan.FromSeconds(Math.Clamp(options.Value.DiscoveryReceivePollSeconds, 1, 30));

        while (!stoppingToken.IsCancellationRequested)
        {
            registry.RemoveExpired(peerExpiry);
            using var receiveCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            receiveCancellation.CancelAfter(receivePoll);

            UdpReceiveResult received;
            try
            {
                received = await udp.ReceiveAsync(receiveCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex)
            {
                logger.LogWarning(ex, "Transient LocalGPT discovery receive failure on UDP {Port}; listening continues.", options.Value.DiscoveryPort);
                try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                continue;
            }

            if (received.Buffer.Length > OrganicWireProtocol.MaximumDiscoveryBytes)
            {
                logger.LogWarning("Ignored oversized LocalGPT discovery datagram ({Length} bytes).", received.Buffer.Length);
                continue;
            }

            try
            {
                var peer = JsonSerializer.Deserialize<OrganicPeerAdvertisement>(received.Buffer, codec.JsonOptions);
                if (peer is null || !string.Equals(peer.Application, "LocalGPT", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(peer.Address) || peer.Address is "0.0.0.0" or "::")
                    peer.Address = received.RemoteEndPoint.Address.ToString();
                peer.SeenUtc = DateTimeOffset.UtcNow;
                registry.Upsert(peer);
                var isLocalPeer = IPAddress.IsLoopback(received.RemoteEndPoint.Address)
                    || string.Equals(peer.HostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
                if (options.Value.AutoConnectDiscoveredPeer && isLocalPeer && !connection.State.IsConnected &&
                    !automaticallyAttemptedPeers.Contains(peer.PeerId) &&
                    Interlocked.CompareExchange(ref autoConnectInProgress, 1, 0) == 0)
                {
                    automaticallyAttemptedPeers.Add(peer.PeerId);
                    var connected = false;
                    try
                    {
                        logger.LogInformation("Automatically opening the local 1-Wire transport to discovered peer {PeerId}; human link/MFA approval remains required.", peer.PeerId);
                        connected = (await connection.ConnectAsync(peer.PeerId, stoppingToken).ConfigureAwait(false)).IsConnected;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Automatic 1-Wire transport connection to {PeerId} failed; a later discovery beacon may retry.", peer.PeerId);
                    }
                    finally
                    {
                        if (!connected)
                            automaticallyAttemptedPeers.Remove(peer.PeerId);
                        Interlocked.Exchange(ref autoConnectInProgress, 0);
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Ignored malformed LocalGPT discovery data from {RemoteEndPoint}; listening continues.", received.RemoteEndPoint);
            }
        }
    }
}
