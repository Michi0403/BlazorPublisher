using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace PublisherStudio.HostedServices.OrganicPlugins;

/// <summary>
/// Runs LocalGPT UDP discovery only while PublisherStudio policy and explicit frontend demand require it, then coordinates discovered peers with the optional 1-Wire connection service.
/// </summary>
/// <param name="options">Options controlling whether discovery is enabled, frontend-activated, and suspended after a connection is established.</param>
/// <param name="activation">Frontend discovery activation state that prevents background network activity before the user opens the LocalGPT connection workflow.</param>
/// <param name="registry">Authoritative LocalGPT discovery directory updated from accepted peer advertisements.</param>
/// <param name="connection">LocalGPT connection service used only when configured automatic connection is permitted for a discovered local peer.</param>
/// <param name="codec">Organic protocol codec that owns JSON settings for discovery advertisements.</param>
/// <param name="logger">Logger used to record discovery lifecycle, transport, and validation diagnostics.</param>
public sealed class LocalGptDiscoveryHostedService(
    IOptions<OrganicPluginOptions> options,
    ILocalGptDiscoveryActivationService activation,
    ILocalGptDiscoveryRegistry registry,
    ILocalGptConnectionService connection,
    IOrganicPluginProtocolCodec codec,
    ILogger<LocalGptDiscoveryHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Signals the background loop when frontend demand or LocalGPT connection state changes so socket ownership can be reevaluated without cancellation-driven receive polling.
    /// </summary>
    private readonly SemaphoreSlim discoveryStateSignal = new(0, 1);

    /// <summary>
    /// Prevents overlapping automatic connection attempts while a discovered local peer is being opened.
    /// </summary>
    private int autoConnectInProgress;

    /// <summary>
    /// Tracks peers already attempted during the current process so failed automatic connections can become retryable after their attempt completes.
    /// </summary>
    private readonly HashSet<string> automaticallyAttemptedPeers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the hosted discovery lifecycle, remaining network-idle until policy permits discovery and an explicit frontend request exists when frontend activation is required.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token supplied by the host when PublisherStudio is shutting down.</param>
    /// <returns>A task that completes when the hosted service stops.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!options.Value.Enabled || !options.Value.EnableDiscovery)
            {
                logger.LogInformation("LocalGPT discovery is disabled by PublisherStudio configuration; no discovery socket will be opened.");
                return;
            }

            activation.Changed += SignalDiscoveryStateChanged;
            connection.Changed += SignalDiscoveryStateChanged;
            try
            {
                logger.LogInformation(
                    "LocalGPT discovery service is network-idle until policy permits listening. Frontend activation required: {RequireFrontendActivation}; suspend while connected: {SuspendWhileConnected}.",
                    options.Value.RequireFrontendDiscoveryActivation,
                    options.Value.SuspendDiscoveryWhileConnected);

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!ShouldListen())
                    {
                        await discoveryStateSignal.WaitAsync(stoppingToken).ConfigureAwait(false);
                        continue;
                    }

                    await RunDiscoverySessionAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            finally
            {
                activation.Changed -= SignalDiscoveryStateChanged;
                connection.Changed -= SignalDiscoveryStateChanged;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("LocalGPT discovery hosted service stopped with PublisherStudio shutdown cancellation.");
        }
        catch (Exception __serviceMethodException)
        {
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(ExecuteAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether the discovery listener should currently own a UDP socket according to configuration, frontend demand, and existing connection state.
    /// </summary>
    /// <returns><see langword="true"/> when LocalGPT discovery should currently listen; otherwise <see langword="false"/>.</returns>
    private bool ShouldListen()
    {
        try
        {
            if (!options.Value.Enabled || !options.Value.EnableDiscovery)
                return false;
            if (options.Value.RequireFrontendDiscoveryActivation && !activation.IsRequested)
                return false;
            if (options.Value.SuspendDiscoveryWhileConnected && connection.State.IsConnected)
                return false;
            return true;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(ShouldListen)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(ShouldListen)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Wakes the hosted discovery loop after frontend activation or LocalGPT connection state changes.
    /// </summary>
    private void SignalDiscoveryStateChanged()
    {
        try
        {
            if (discoveryStateSignal.CurrentCount == 0)
                discoveryStateSignal.Release();
        }
        catch (SemaphoreFullException exception)
        {
            logger.LogDebug(exception, "LocalGPT discovery state signal was already queued; the duplicate wake-up was ignored.");
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(SignalDiscoveryStateChanged)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(SignalDiscoveryStateChanged)} failed.");
        }
    }

    /// <summary>
    /// Owns one bounded LocalGPT discovery socket session while discovery is actively requested, using signal-or-timeout waits instead of expected cancellation exceptions for normal polling.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token supplied by the host when PublisherStudio is shutting down.</param>
    /// <returns>A task that completes when discovery demand ends, a LocalGPT connection suspends discovery, or the host stops.</returns>
    private async Task RunDiscoverySessionAsync(CancellationToken stoppingToken)
    {
        try
        {
            var peerExpiry = TimeSpan.FromSeconds(Math.Clamp(options.Value.PeerExpirySeconds, 10, 300));
            var receivePoll = TimeSpan.FromSeconds(Math.Clamp(options.Value.DiscoveryReceivePollSeconds, 1, 30));
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, options.Value.DiscoveryPort));
            }
            catch (SocketException exception)
            {
                logger.LogWarning(
                    exception,
                    "Optional LocalGPT discovery could not bind UDP {Port}; PublisherStudio remains usable and will retry only while the frontend discovery workflow remains active.",
                    options.Value.DiscoveryPort);
                await discoveryStateSignal.WaitAsync(receivePoll, stoppingToken).ConfigureAwait(false);
                return;
            }

            logger.LogInformation(
                "PublisherStudio activated LocalGPT discovery on UDP {Port} for the explicit frontend connection workflow.",
                options.Value.DiscoveryPort);

            while (!stoppingToken.IsCancellationRequested && ShouldListen())
            {
                registry.RemoveExpired(peerExpiry);

                int available;
                try
                {
                    available = udp.Available;
                }
                catch (SocketException exception)
                {
                    logger.LogWarning(exception, "Transient LocalGPT discovery socket status failure on UDP {Port}; listening continues while requested.", options.Value.DiscoveryPort);
                    await discoveryStateSignal.WaitAsync(receivePoll, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                if (available <= 0)
                {
                    await discoveryStateSignal.WaitAsync(receivePoll, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                UdpReceiveResult received;
                try
                {
                    received = await udp.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (SocketException exception)
                {
                    logger.LogWarning(exception, "Transient LocalGPT discovery receive failure on UDP {Port}; listening continues while requested.", options.Value.DiscoveryPort);
                    await discoveryStateSignal.WaitAsync(receivePoll, stoppingToken).ConfigureAwait(false);
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
                    if (peer is null || !string.Equals(peer.Application, "LocalGPT", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.IsNullOrWhiteSpace(peer.Address) || peer.Address is "0.0.0.0" or "::")
                        peer.Address = received.RemoteEndPoint.Address.ToString();
                    peer.SeenUtc = DateTimeOffset.UtcNow;
                    registry.Upsert(peer);

                    var isLocalPeer = IPAddress.IsLoopback(received.RemoteEndPoint.Address)
                        || string.Equals(peer.HostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
                    if (options.Value.AutoConnectDiscoveredPeer
                        && isLocalPeer
                        && !connection.State.IsConnected
                        && !automaticallyAttemptedPeers.Contains(peer.PeerId)
                        && Interlocked.CompareExchange(ref autoConnectInProgress, 1, 0) == 0)
                    {
                        automaticallyAttemptedPeers.Add(peer.PeerId);
                        var connected = false;
                        try
                        {
                            logger.LogInformation(
                                "Automatically opening the local 1-Wire transport to explicitly discovered peer {PeerId}; human link/MFA approval remains required.",
                                peer.PeerId);
                            connected = (await connection.ConnectAsync(peer.PeerId, stoppingToken).ConfigureAwait(false)).IsConnected;
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            logger.LogWarning(exception, "Automatic 1-Wire transport connection to {PeerId} failed; a later explicitly requested discovery beacon may retry.", peer.PeerId);
                        }
                        finally
                        {
                            if (!connected)
                                automaticallyAttemptedPeers.Remove(peer.PeerId);
                            Interlocked.Exchange(ref autoConnectInProgress, 0);
                        }
                    }
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(exception, "Ignored malformed LocalGPT discovery data from {RemoteEndPoint}; listening continues while requested.", received.RemoteEndPoint);
                }
            }

            logger.LogInformation(
                "PublisherStudio released LocalGPT discovery UDP {Port}; no discovery socket remains active for the current frontend state.",
                options.Value.DiscoveryPort);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("LocalGPT discovery socket session stopped with PublisherStudio shutdown cancellation.");
            throw;
        }
        catch (Exception __serviceMethodException)
        {
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryHostedService)}.{nameof(RunDiscoverySessionAsync)} failed.");
            throw;
        }
    }
}
