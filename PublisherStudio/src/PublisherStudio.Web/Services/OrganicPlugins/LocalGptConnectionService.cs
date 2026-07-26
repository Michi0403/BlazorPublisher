using PublisherStudio.Domain;
using PublisherStudio.Services;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class LocalGptConnectionService(
    ILocalGptDiscoveryRegistry discovery,
    IOrganicPluginProtocolCodec codec,
    IOrganicCapabilityCatalog capabilities,
    IOrganicWorkCoordinator work,
    IOrganicResultStore results,
    ILogger<LocalGptConnectionService> logger) : ILocalGptConnectionService
{
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Task> activeInvocations = new();
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;
    private CancellationTokenSource? connectionCancellation;
    private Task? readLoop;
    private string peerId = string.Empty;
    private Guid connectionId;
    private readonly string localPeerId = $"publisherstudio:{Environment.MachineName}";

    public event Action? Changed;
    public OrganicConnectionState State { get; } = new();

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
            var stream = tcp.GetStream();
            var connectedReader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);
            var connectedWriter = new StreamWriter(stream, new UTF8Encoding(false), 8192, leaveOpen: true) { AutoFlush = true };
            var connectedId = Guid.NewGuid();
            client = tcp;
            reader = connectedReader;
            writer = connectedWriter;
            connectionCancellation = new CancellationTokenSource();
            connectionId = connectedId;
            peerId = requestedPeerId;
            State.IsConnected = true;
            State.PeerId = requestedPeerId;
            State.DisplayName = peer.DisplayName;
            State.ConnectedUtc = DateTimeOffset.UtcNow;
            State.LastError = string.Empty;
            discovery.SetConnected(requestedPeerId, true);
            readLoop = ReadLoopAsync(connectedId, requestedPeerId, connectedReader, connectedWriter, connectionCancellation.Token);
            Changed?.Invoke();

            var localCapabilities = await capabilities.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
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
                        ApplicationVersion = "1.0.89-organic-wire",
                        HostName = Environment.MachineName,
                        Address = "0.0.0.0",
                        ServicePort = 0,
                        DiscoveryPort = OrganicWireProtocol.DefaultDiscoveryPort,
                        WebBaseUrl = RuntimeEndpointStore.BaseUrl,
                        IsConnected = true,
                        Capabilities = localCapabilities.ToList()
                    }, OrganicPluginProtocolCodec.JsonOptions)
                }
            };
            await SendEnvelopeAsync(hello, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Connected PublisherStudio organic plugin to LocalGPT peer {PeerId} at {Address}:{Port}.", requestedPeerId, address, peer.ServicePort);
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

    public async Task DisconnectAsync()
    {
        await lifecycleGate.WaitAsync().ConfigureAwait(false);
        try { await DisconnectCoreAsync().ConfigureAwait(false); }
        finally { lifecycleGate.Release(); }
    }

    public async Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default)
    {
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
                ["CouncilRequest"] = JsonSerializer.SerializeToElement(request, OrganicPluginProtocolCodec.JsonOptions)
            }
        };
        return await SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    public Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var connectedWriter = writer;
        if (connectedWriter is null || !State.IsConnected) throw new InvalidOperationException("PublisherStudio is not connected to LocalGPT.");
        return SendEnvelopeCoreAsync(envelope, connectedWriter, peerId, cancellationToken);
    }

    public async Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await SendEnvelopeAsync(CreateWorkResultEnvelope(item), cancellationToken).ConfigureAwait(false);
    }

    private async Task ReadLoopAsync(Guid connectedId, string connectedPeerId, StreamReader connectedReader, StreamWriter connectedWriter, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await connectedReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null) break;
                var envelope = codec.DeserializeAndValidate(line);
                await HandleIncomingAsync(envelope, connectedId, connectedPeerId, connectedWriter, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or SocketException or JsonException or InvalidDataException or ObjectDisposedException)
        {
            if (connectionId == connectedId)
                State.LastError = ex.Message;
            logger.LogWarning(ex, "PublisherStudio LocalGPT 1-Wire read loop stopped.");
        }
        finally
        {
            if (connectionId == connectedId)
            {
                State.IsConnected = false;
                discovery.SetConnected(connectedPeerId, false);
                Changed?.Invoke();
            }
        }
    }

    private async Task HandleIncomingAsync(OrganicWireEnvelope envelope, Guid connectedId, string connectedPeerId, StreamWriter connectedWriter, CancellationToken cancellationToken)
    {
        switch (envelope.MessageType)
        {
            case OrganicWireMessageType.HelloAck:
            case OrganicWireMessageType.CapabilityResponse:
                if (TryRead<List<OrganicCapabilityDescriptor>>(envelope, "Capabilities", out var remoteCapabilities) && discovery.GetPeer(connectedPeerId) is { } peer)
                {
                    peer.Capabilities = remoteCapabilities ?? [];
                    peer.IsConnected = true;
                    discovery.Upsert(peer);
                }
                results.RecordEnvelope(envelope);
                break;
            case OrganicWireMessageType.Invoke:
                StartInvoke(envelope, connectedId, connectedWriter, cancellationToken);
                break;
            default:
                results.RecordEnvelope(envelope);
                break;
        }
    }

    private void StartInvoke(OrganicWireEnvelope envelope, Guid connectedId, StreamWriter connectedWriter, CancellationToken cancellationToken)
    {
        var task = ProcessInvokeAsync(envelope, connectedId, connectedWriter, cancellationToken);
        if (!activeInvocations.TryAdd(envelope.MessageId, task))
            throw new InvalidOperationException($"Organic work message {envelope.MessageId} is already being processed.");

        task.GetAwaiter().OnCompleted(() => activeInvocations.TryRemove(envelope.MessageId, out _));
    }

    private async Task ProcessInvokeAsync(OrganicWireEnvelope envelope, Guid connectedId, StreamWriter connectedWriter, CancellationToken cancellationToken)
    {
        try
        {
            var item = await work.ReceiveAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (connectionId != connectedId || !State.IsConnected)
            {
                logger.LogDebug("Organic work {WorkItemId} completed after its LocalGPT connection ended; the result remains available in PublisherStudio.", item.Id);
                return;
            }
            await SendEnvelopeCoreAsync(CreateWorkResultEnvelope(item), connectedWriter, item.PeerId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not process or return organic work for {CapabilityKey}.", envelope.CapabilityKey);
        }
    }

    private async Task<Guid> SendEnvelopeCoreAsync(OrganicWireEnvelope envelope, StreamWriter connectedWriter, string targetPeerId, CancellationToken cancellationToken)
    {
        envelope.SourcePeerId = string.IsNullOrWhiteSpace(envelope.SourcePeerId) ? localPeerId : envelope.SourcePeerId;
        envelope.TargetPeerId = string.IsNullOrWhiteSpace(envelope.TargetPeerId) ? targetPeerId : envelope.TargetPeerId;
        var json = codec.Serialize(envelope);
        await writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await connectedWriter.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false); }
        finally { writeGate.Release(); }
        return envelope.MessageId;
    }

    private OrganicWireEnvelope CreateWorkResultEnvelope(OrganicPluginWorkItem item)
    {
        var type = item.Status == OrganicWorkStatus.PendingApproval ? OrganicWireMessageType.ApprovalRequired : OrganicWireMessageType.WorkResult;
        return new OrganicWireEnvelope
        {
            MessageType = type,
            CorrelationId = item.CorrelationId,
            ReplyToMessageId = item.MessageId,
            SourcePeerId = localPeerId,
            TargetPeerId = item.PeerId,
            CapabilityKey = item.CapabilityKey,
            Error = item.Error,
            Properties = new Dictionary<string, JsonElement>
            {
                ["WorkItemId"] = JsonSerializer.SerializeToElement(item.Id),
                ["Status"] = JsonSerializer.SerializeToElement(item.Status.ToString()),
                ["ResultJson"] = JsonSerializer.SerializeToElement(item.ResultJson)
            }
        };
    }

    private async Task DisconnectCoreAsync()
    {
        var oldPeerId = peerId;
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
        State.IsConnected = false;
        State.PeerId = string.Empty;
        State.DisplayName = string.Empty;
        State.ConnectedUtc = null;
        if (!string.IsNullOrWhiteSpace(oldPeerId)) discovery.SetConnected(oldPeerId, false);
        Changed?.Invoke();
    }

    private static bool TryRead<T>(OrganicWireEnvelope envelope, string key, out T? value)
    {
        value = default;
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(key, out var element)) return false;
        try { value = element.Deserialize<T>(OrganicPluginProtocolCodec.JsonOptions); return true; }
        catch (JsonException) { return false; }
    }

    private static string NormalizeAddress(string address, string hostName)
    {
        if (string.IsNullOrWhiteSpace(address) || address is "0.0.0.0" or "::")
            return string.Equals(hostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : hostName;
        return address;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        lifecycleGate.Dispose();
        writeGate.Dispose();
    }
}
