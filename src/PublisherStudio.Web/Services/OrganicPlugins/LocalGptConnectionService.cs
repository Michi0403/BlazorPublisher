using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Provides local gpt connection service operations.
/// </summary>
public sealed class LocalGptConnectionService(
    ILocalGptDiscoveryRegistry discovery,
    IOrganicPluginProtocolCodec codec,
    IOrganicRuntimeSecurityService security,
    IOrganicCapabilityCatalog capabilities,
    IOrganicPermissionStore permissions,
    IOrganicWorkCoordinator work,
    IOrganicResultStore results,
    IOrganicReplayGuard replayGuard,
    IOrganicTransportSecurityPolicy transportSecurityPolicy,
    IOrganicConnectionRuntimeState runtimeState,
    IOrganicWireEnvelopeFactory envelopeFactory,
    IRuntimeEndpointState runtimeEndpointState,
    ILogger<LocalGptConnectionService> logger) : ILocalGptConnectionService
{
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Task> activeInvocations = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<OrganicWireEnvelope>> responseWaiters = new();
    private readonly ConcurrentDictionary<Guid, OrganicWireEnvelope> recentResponses = new();
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;
    private CancellationTokenSource? connectionCancellation;
    private Task? readLoop;
    private string peerId = string.Empty;
    private Guid connectionId;
    private readonly string localPeerId = $"publisherstudio:{Environment.MachineName}";

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets state.
    /// </summary>
    public OrganicConnectionState State { get; } = new();

    /// <summary>
    /// Runs the connect async operation.
    /// </summary>
    public async Task<OrganicConnectionState> ConnectAsync(string requestedPeerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedPeerId);
        await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await DisconnectCoreAsync().ConfigureAwait(false);
            var peer = discovery.GetPeer(requestedPeerId) ?? throw new KeyNotFoundException("The selected LocalGPT discovery entry is no longer available.");
            var address = NormalizeAddress(peer.Address, peer.HostName);
            var tcp = new TcpClient();
            await tcp.ConnectAsync(address, peer.ServicePort, cancellationToken).ConfigureAwait(false);
            var remoteAddress = (tcp.Client.RemoteEndPoint as IPEndPoint)?.Address;
            var isLoopback = remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);
            var stream = tcp.GetStream();
            var connectedReader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);
            var connectedWriter = new StreamWriter(stream, new UTF8Encoding(false), 8192, leaveOpen: true) { AutoFlush = true };
            var connectedId = Guid.NewGuid();
            client = tcp;
            reader = connectedReader;
            writer = connectedWriter;
            connectionCancellation = new CancellationTokenSource();
            connectionId = connectedId;
            runtimeState.SetConnected(connectedId, requestedPeerId, isLoopback);
            peerId = requestedPeerId;
            State.IsConnected = true;
            State.IsLinked = false;
            State.PeerId = requestedPeerId;
            State.DisplayName = peer.DisplayName;
            State.ConnectedUtc = DateTimeOffset.UtcNow;
            State.LastError = "Waiting for LocalGPT frontend link approval.";
            discovery.SetConnected(requestedPeerId, true);
            readLoop = ReadLoopAsync(connectedId, requestedPeerId, connectedReader, connectedWriter, isLoopback, connectionCancellation.Token);
            Changed?.Invoke();

            var localCapabilities = (await capabilities.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false))
                .Select(capability => ApplyPermissionPolicy(requestedPeerId, capability))
                .Where(capability => capability.IsExposedToPeer)
                .ToList();
            var localSkills = await capabilities.GetSkillsAsync(cancellationToken).ConfigureAwait(false);
            var localUiFeatures = await capabilities.GetUiFeaturesAsync(cancellationToken).ConfigureAwait(false);
            var localHardware = await capabilities.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            var localSecurity = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false);
            var hello = new OrganicWireEnvelope
            {
                MessageType = OrganicWireMessageType.Hello,
                SourcePeerId = localPeerId,
                TargetPeerId = requestedPeerId,
                Properties = new Dictionary<string, JsonElement>
                {
                    ["Peer"] = JsonSerializer.SerializeToElement(new OrganicPeerAdvertisement
                    {
                        PeerId = localPeerId,
                        DisplayName = "PublisherStudio / BlazorPublisher",
                        Application = "PublisherStudio",
                        ApplicationVersion = "2.2.3-organic-wire",
                        HostName = Environment.MachineName,
                        Address = "0.0.0.0",
                        ServicePort = 0,
                        DiscoveryPort = OrganicWireProtocol.DefaultDiscoveryPort,
                        WebBaseUrl = runtimeEndpointState.BaseUrl,
                        IsConnected = true,
                        TransportKind = OneWireTransportKind.Tcp,
                        SupportedTransports = ["tcp", "http-json"],
                        Security = localSecurity,
                        Capabilities = localCapabilities.ToList(),
                        Skills = localSkills.ToList(),
                        UiFeatures = localUiFeatures.ToList(),
                        Hardware = localHardware.ToList()
                    }, codec.JsonOptions)
                }
            };
            await SendEnvelopeAsync(hello, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Connected PublisherStudio organic plugin to LocalGPT peer {PeerId} at {Address}:{Port}.", requestedPeerId, address, peer.ServicePort);
            return State;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            State.IsConnected = false;
            State.LastError = string.Empty;
            Changed?.Invoke();
            await DisconnectCoreAsync().ConfigureAwait(false);
            return State;
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException or KeyNotFoundException)
        {
            State.IsConnected = false;
            State.LastError = ex.Message;
            Changed?.Invoke();
            logger.LogError(ex, "Could not connect PublisherStudio to LocalGPT peer {PeerId}.", requestedPeerId);
            await DisconnectCoreAsync().ConfigureAwait(false);
            return State;
        }
        finally { lifecycleGate.Release(); }
    }

    /// <summary>
    /// Runs the disconnect async operation.
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.DisconnectAsync.");
                    await lifecycleGate.WaitAsync().ConfigureAwait(false);
                    try { await DisconnectCoreAsync().ConfigureAwait(false); }
                    finally { lifecycleGate.Release(); }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.DisconnectAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the send council request async operation.
    /// </summary>
    public async Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.SendCouncilRequestAsync.");
                    ArgumentNullException.ThrowIfNull(request);
                    if (string.IsNullOrWhiteSpace(request.Prompt)) throw new ArgumentException("A council prompt is required.", nameof(request));
                    var envelope = new OrganicWireEnvelope
                    {
                        MessageType = OrganicWireMessageType.CouncilRequest,
                        SourcePeerId = localPeerId,
                        TargetPeerId = peerId,
                        CapabilityKey = "council.run",
                        Organs = ["brain"],
                        Skills = [request.TeamKey],
                        UserConfirmed = true,
                        Properties = new Dictionary<string, JsonElement>
                        {
                            ["CouncilRequest"] = JsonSerializer.SerializeToElement(request, codec.JsonOptions)
                        }
                    };
                    return await SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.SendCouncilRequestAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the send envelope async operation.
    /// </summary>
    public Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.SendEnvelopeAsync.");
                    ArgumentNullException.ThrowIfNull(envelope);
                    var connectedWriter = writer;
                    if (connectedWriter is null || !State.IsConnected) throw new InvalidOperationException("PublisherStudio is not connected to LocalGPT.");
                    if (!State.IsLinked && envelope.MessageType != OrganicWireMessageType.Hello)
                        throw new InvalidOperationException("The 1-Wire transport is waiting for LocalGPT frontend link approval.");
                    return SendEnvelopeCoreAsync(envelope, connectedWriter, peerId, runtimeState.GetSnapshot().IsLoopback, cancellationToken);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.SendEnvelopeAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the wait for result async operation.
    /// </summary>
    public async Task<OrganicWireEnvelope> WaitForResultAsync(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.WaitForResultAsync.");
                    if (correlationId == Guid.Empty) throw new ArgumentException("A correlation id is required.", nameof(correlationId));
                    if (recentResponses.TryRemove(correlationId, out var cached)) return cached;
                    var waiter = responseWaiters.GetOrAdd(correlationId, _ => new TaskCompletionSource<OrganicWireEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously));
                    if (recentResponses.TryRemove(correlationId, out cached))
                    {
                        responseWaiters.TryRemove(correlationId, out _);
                        return cached;
                    }
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeout);
                    try
                    {
                        return await waiter.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        responseWaiters.TryRemove(correlationId, out _);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.WaitForResultAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the send work result async operation.
    /// </summary>
    public async Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.SendWorkResultAsync.");
                    ArgumentNullException.ThrowIfNull(item);
                    await SendEnvelopeAsync(envelopeFactory.CreateWorkEnvelope(item, localPeerId), cancellationToken).ConfigureAwait(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.SendWorkResultAsync failed: {exception.Message}");
            throw;
        }
    }

    private async Task ReadLoopAsync(Guid connectedId, string connectedPeerId, StreamReader connectedReader, StreamWriter connectedWriter, bool isLoopback, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await connectedReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null) break;
                var envelope = codec.DeserializeAndValidate(line);
                await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(envelope.SourcePeerId, connectedPeerId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The organic 1-Wire SourcePeerId does not match the peer identity owned by this connection.");
                if (string.Equals(envelope.SourcePeerId, localPeerId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A remote connection cannot claim PublisherStudio's local peer identity.");
                if (!isLoopback && transportSecurityPolicy.RequiresProtectedTransport(envelope.MessageType) &&
                    !transportSecurityPolicy.IsProtected(envelope))
                    throw new CryptographicException("A non-loopback organic 1-Wire connection requires MFA-verified message protection before application data can be received.");
                if (!replayGuard.TryAccept(connectedPeerId, envelope.MessageId, envelope.CreatedUtc))
                    throw new InvalidDataException("This organic 1-Wire message id has already been processed or is outside the accepted replay window.");
                await HandleIncomingAsync(envelope, connectedId, connectedPeerId, connectedWriter, isLoopback, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or SocketException or JsonException or InvalidDataException or ObjectDisposedException or CryptographicException)
        {
            if (connectionId == connectedId)
                State.LastError = ex.Message;
            logger.LogWarning(ex, $"PublisherStudio LocalGPT 1-Wire read loop stopped for peer {connectedPeerId}.");
        }
        finally
        {
            if (connectionId == connectedId)
            {
                State.IsConnected = false;
                State.IsLinked = false;
                discovery.SetConnected(connectedPeerId, false);
                Changed?.Invoke();
            }
        }
    }

    private async Task HandleIncomingAsync(OrganicWireEnvelope envelope, Guid connectedId, string connectedPeerId, StreamWriter connectedWriter, bool isLoopback, CancellationToken cancellationToken)
    {
        try
        {
            if (envelope.MessageType is OrganicWireMessageType.WorkResult or OrganicWireMessageType.Error or OrganicWireMessageType.ApprovalRequired)
            {
                // A live waiter owns the response exclusively. Caching the same ApprovalRequired envelope
                // as well caused the next wait cycle to consume that stale intermediate response instead of
                // waiting for the final WorkResult on the same correlation id.
                if (responseWaiters.TryGetValue(envelope.CorrelationId, out var waiter))
                    waiter.TrySetResult(envelope);
                else
                    recentResponses[envelope.CorrelationId] = envelope;

                foreach (var stale in recentResponses.Where(pair => pair.Value.CreatedUtc < DateTimeOffset.UtcNow - TimeSpan.FromMinutes(10)).Select(pair => pair.Key).ToArray())
                    recentResponses.TryRemove(stale, out _);
            }

            if (envelope.MessageType == OrganicWireMessageType.ApprovalRequired &&
                envelope.Properties is not null &&
                envelope.Properties.TryGetValue("LinkApproval", out var linkApproval) &&
                linkApproval.ValueKind is JsonValueKind.True)
            {
                State.IsLinked = false;
                State.LastError = "Waiting for LocalGPT frontend link approval.";
                Changed?.Invoke();
            }

            switch (envelope.MessageType)
            {
                case OrganicWireMessageType.HelloAck:
                    State.IsLinked = true;
                    State.LastError = string.Empty;
                    await SendEnvelopeCoreAsync(new OrganicWireEnvelope
                    {
                        MessageType = OrganicWireMessageType.CapabilityRequest,
                        CorrelationId = envelope.CorrelationId,
                        SourcePeerId = localPeerId,
                        TargetPeerId = connectedPeerId
                    }, connectedWriter, connectedPeerId, isLoopback, cancellationToken).ConfigureAwait(false);
                    goto case OrganicWireMessageType.CapabilityResponse;
                case OrganicWireMessageType.CapabilityResponse:
                case OrganicWireMessageType.SkillResponse:
                case OrganicWireMessageType.SkillStateUpdate:
                    if (discovery.GetPeer(connectedPeerId) is { } peer)
                    {
                        if (TryRead<OrganicPeerAdvertisement>(envelope, "Peer", out var advertisedPeer) && advertisedPeer is not null)
                        {
                            advertisedPeer.Address = string.IsNullOrWhiteSpace(advertisedPeer.Address) ? peer.Address : advertisedPeer.Address;
                            advertisedPeer.ServicePort = advertisedPeer.ServicePort <= 0 ? peer.ServicePort : advertisedPeer.ServicePort;
                            advertisedPeer.DiscoveryPort = advertisedPeer.DiscoveryPort <= 0 ? peer.DiscoveryPort : advertisedPeer.DiscoveryPort;
                            peer = advertisedPeer;
                        }
                        if (TryRead<List<OrganicCapabilityDescriptor>>(envelope, "Capabilities", out var remoteCapabilities))
                            peer.Capabilities = remoteCapabilities ?? [];
                        if (TryRead<List<OrganicSkillDescriptor>>(envelope, "Skills", out var remoteSkills))
                            peer.Skills = remoteSkills ?? [];
                        if (TryRead<List<OrganicUiFeatureDescriptor>>(envelope, "UiFeatures", out var remoteUiFeatures))
                            peer.UiFeatures = remoteUiFeatures ?? [];
                        if (TryRead<List<OrganicHardwareDescriptor>>(envelope, "Hardware", out var remoteHardware))
                            peer.Hardware = remoteHardware ?? [];
                        peer.IsConnected = true;
                        discovery.Upsert(peer);
                        State.RemoteCapabilities = peer.Capabilities.ToList();
                        State.RemoteSkills = peer.Skills.ToList();
                        State.RemoteUiFeatures = peer.UiFeatures.ToList();
                        State.RemoteHardware = peer.Hardware.ToList();
                        Changed?.Invoke();
                    }
                    results.RecordEnvelope(envelope);
                    break;
                case OrganicWireMessageType.Invoke:
                    StartInvoke(envelope, connectedId, connectedWriter, isLoopback, cancellationToken);
                    break;
                default:
                    results.RecordEnvelope(envelope);
                    break;
            }

        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not handle organic message {envelope.MessageId} from {connectedPeerId}.");
            throw;
        }
    }

    private void StartInvoke(OrganicWireEnvelope envelope, Guid connectedId, StreamWriter connectedWriter, bool isLoopback, CancellationToken cancellationToken)
    {
        try
        {
            var task = ProcessInvokeAsync(envelope, connectedId, connectedWriter, isLoopback, cancellationToken);
            if (!activeInvocations.TryAdd(envelope.MessageId, task))
                throw new InvalidOperationException($"Organic work message {envelope.MessageId} is already being processed.");

            task.GetAwaiter().OnCompleted(() => activeInvocations.TryRemove(envelope.MessageId, out _));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not start organic work message {envelope.MessageId}.");
            throw;
        }
    }

    private async Task ProcessInvokeAsync(OrganicWireEnvelope envelope, Guid connectedId, StreamWriter connectedWriter, bool isLoopback, CancellationToken cancellationToken)
    {
        try
        {
            var item = await work.ReceiveAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (connectionId != connectedId || !State.IsConnected)
            {
                logger.LogDebug($"Organic work {item.Id} completed after its LocalGPT connection ended; the result remains available in PublisherStudio.");
                return;
            }
            await SendEnvelopeCoreAsync(envelopeFactory.CreateWorkEnvelope(item, localPeerId), connectedWriter, item.PeerId, isLoopback, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not process or return organic work for {envelope.CapabilityKey}.");
        }
    }

    private async Task<Guid> SendEnvelopeCoreAsync(OrganicWireEnvelope envelope, StreamWriter connectedWriter, string targetPeerId, bool isLoopback, CancellationToken cancellationToken)
    {
        try
        {
            envelope.SourcePeerId = string.IsNullOrWhiteSpace(envelope.SourcePeerId) ? localPeerId : envelope.SourcePeerId;
            envelope.TargetPeerId = string.IsNullOrWhiteSpace(envelope.TargetPeerId) ? targetPeerId : envelope.TargetPeerId;
            await security.ProtectOutgoingAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (!isLoopback && transportSecurityPolicy.RequiresProtectedTransport(envelope.MessageType) &&
                !transportSecurityPolicy.IsProtected(envelope))
                throw new CryptographicException("A non-loopback organic 1-Wire connection requires MFA-verified message protection before application data can be sent.");
            var json = codec.Serialize(envelope);
            await writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { await connectedWriter.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false); }
            finally { writeGate.Release(); }
            return envelope.CorrelationId;

        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not send organic message {envelope.MessageId} to {targetPeerId}.");
            throw;
        }
    }


    private async Task DisconnectCoreAsync()
    {
        var oldPeerId = peerId;
        var oldConnectionId = connectionId;
        connectionId = Guid.Empty;
        connectionCancellation?.Cancel();
        try
        {
            if (readLoop is not null && !readLoop.IsCompleted) await readLoop.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            var runningInvocations = activeInvocations.Values.ToArray();
            if (runningInvocations.Length > 0)
                await Task.WhenAll(runningInvocations).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException) { logger.LogDebug(ex, "Organic connection work ended during disconnect."); }
        reader?.Dispose();
        writer?.Dispose();
        client?.Dispose();
        connectionCancellation?.Dispose();
        client = null; reader = null; writer = null; connectionCancellation = null; readLoop = null; peerId = string.Empty;
        runtimeState.Reset(oldConnectionId);
        State.IsConnected = false;
        State.IsLinked = false;
        State.PeerId = string.Empty;
        State.DisplayName = string.Empty;
        State.ConnectedUtc = null;
        State.LastError = string.Empty;
        State.RemoteCapabilities = [];
        State.RemoteSkills = [];
        State.RemoteUiFeatures = [];
        State.RemoteHardware = [];
        foreach (var waiter in responseWaiters.Values)
            waiter.TrySetException(new IOException("PublisherStudio disconnected from LocalGPT before the 1-Wire result arrived."));
        responseWaiters.Clear();
        if (!string.IsNullOrWhiteSpace(oldPeerId)) discovery.SetConnected(oldPeerId, false);
        Changed?.Invoke();
    }

    private bool TryRead<T>(OrganicWireEnvelope envelope, string key, out T? value)
    {
        value = default;
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(key, out var element)) return false;
        try { value = element.Deserialize<T>(codec.JsonOptions); return true; }
        catch (JsonException) { return false; }
    }

    private OrganicCapabilityDescriptor ApplyPermissionPolicy(string requestedPeerId, OrganicCapabilityDescriptor capability)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.ApplyPermissionPolicy.");
                    var rule = permissions.Resolve(requestedPeerId, capability.Key);
                    if (rule is null)
                        return capability;
                    capability.IsExposedToPeer = rule.IsExposed;
                    capability.AllowPeerInvocation = rule.AllowInvocation;
                    capability.RequiresFrontendUserConfirmation = rule.RequiresFrontendConfirmation;
                    capability.RequiresHumanConfirmation = rule.RequiresFrontendConfirmation;
                    capability.RequiresHumanInteractionOnTargetSystem = rule.RequiresFrontendConfirmation;
                    capability.InteractionEditor = rule.InteractionEditor;
                    capability.ConfigurationKey = $"publisher:{requestedPeerId}:{capability.Key}:{rule.Organ}";
                    return capability;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.ApplyPermissionPolicy failed: {exception.Message}");
            throw;
        }
    }

    private string NormalizeAddress(string address, string hostName)
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.NormalizeAddress.");
                    if (string.IsNullOrWhiteSpace(address) || address is "0.0.0.0" or "::")
                        return string.Equals(hostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : hostName;
                    return address;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.NormalizeAddress failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the dispose async operation.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            logger.LogTrace($"Entering LocalGptConnectionService.DisposeAsync.");
                    await DisconnectAsync().ConfigureAwait(false);
                    lifecycleGate.Dispose();
                    writeGate.Dispose();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"LocalGptConnectionService.DisposeAsync failed: {exception.Message}");
            throw;
        }
    }
}
