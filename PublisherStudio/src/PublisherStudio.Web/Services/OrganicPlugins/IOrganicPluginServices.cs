using PublisherStudio.BusinessObjects;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Defines the organic plugin protocol codec contract.
/// </summary>
public interface IOrganicPluginProtocolCodec
{
    JsonSerializerOptions JsonOptions { get; }
    string Serialize(OrganicWireEnvelope envelope, bool seal = true);
    OrganicWireEnvelope DeserializeAndValidate(string json);
    bool Validate(OrganicWireEnvelope envelope, out string error);
}

/// <summary>
/// Defines the local gpt discovery registry contract.
/// </summary>
public interface ILocalGptDiscoveryRegistry
{
    event Action? Changed;
    IReadOnlyList<OrganicPeerAdvertisement> GetPeers();
    OrganicPeerAdvertisement? GetPeer(string peerId);
    void Upsert(OrganicPeerAdvertisement peer);
    void SetConnected(string peerId, bool connected);
    void RemoveExpired(TimeSpan maximumAge);
}



/// <summary>
/// Defines the organic transport security policy contract.
/// </summary>
public interface IOrganicTransportSecurityPolicy
{
    bool RequiresProtectedTransport(OrganicWireMessageType messageType);
    bool IsProtected(OrganicWireEnvelope envelope);
}

/// <summary>
/// Defines the organic connection runtime state contract.
/// </summary>
public interface IOrganicConnectionRuntimeState
{
    OrganicConnectionRuntimeSnapshot GetSnapshot();
    void SetConnected(Guid connectionId, string peerId, bool isLoopback);
    void Reset(Guid connectionId);
}

/// <summary>
/// Defines the organic wire envelope factory contract.
/// </summary>
public interface IOrganicWireEnvelopeFactory
{
    OrganicWireEnvelope CreateWorkEnvelope(OrganicPluginWorkItem item, string sourcePeerId);
}

/// <summary>
/// Defines the organic runtime security service contract.
/// </summary>
public interface IOrganicRuntimeSecurityService
{
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(CancellationToken cancellationToken = default);
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    Task ProtectOutgoingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task UnprotectIncomingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic capability catalog contract.
/// </summary>
public interface IOrganicCapabilityCatalog : LocalGPT.WireProtocol.IOneWireCapabilityProvider
{
    new Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    new Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    new Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default);
    new Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic permission store contract.
/// </summary>
public interface IOrganicPermissionStore
{
    IReadOnlyList<OrganicPermissionRule> GetRules();
    OrganicPermissionRule Save(OrganicPermissionRule rule);
    bool Delete(string peerId, string capabilityKey, string organ);
    bool IsAllowed(OrganicWireEnvelope envelope);
    bool IsDenied(OrganicWireEnvelope envelope);
    bool IsCapabilityExposed(string peerId, OrganicCapabilityDescriptor capability);
    OrganicPermissionRule? Resolve(string peerId, string capabilityKey, string organ = "");
}

/// <summary>
/// Defines the organic work coordinator contract.
/// </summary>
public interface IOrganicWorkCoordinator
{
    event Action? Changed;
    IReadOnlyList<OrganicPluginWorkItem> GetWork();
    OrganicPluginWorkItem? Get(Guid id);
    Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    bool UpdateInteractionValue(Guid id, string value);
    bool Decline(Guid id, string reason);
}

/// <summary>
/// Defines the organic work executor contract.
/// </summary>
public interface IOrganicWorkExecutor
{
    Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the organic replay policy data service contract.
/// </summary>
public interface IOrganicReplayPolicyDataService
{
    OrganicReplayPolicySnapshot GetSnapshot();
}

/// <summary>
/// Defines the organic replay guard contract.
/// </summary>
public interface IOrganicReplayGuard
{
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

/// <summary>
/// Defines the organic result store contract.
/// </summary>
public interface IOrganicResultStore
{
    event Action? Changed;
    IReadOnlyList<OrganicPluginWorkItem> GetResults();
    void RecordEnvelope(OrganicWireEnvelope envelope);
    void AddTextProposal(OrganicTextInsertionProposal proposal);
    IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals();
    bool RemoveTextProposal(Guid id);
}

/// <summary>
/// Defines the local gpt connection service contract.
/// </summary>
public interface ILocalGptConnectionService : IAsyncDisposable
{
    event Action? Changed;
    OrganicConnectionState State { get; }
    Task<OrganicConnectionState> ConnectAsync(string peerId, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default);
    Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task<OrganicWireEnvelope> WaitForResultAsync(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the recurring screen reader service contract.
/// </summary>
public interface IRecurringScreenReaderService
{
    event Action? Changed;
    IReadOnlyList<RecurringScreenReaderSession> GetSessions();
    Task<RecurringScreenReaderSession> StartAsync(string peerId, string selector, string prompt, int intervalSeconds, CancellationToken cancellationToken = default);
    Task<bool> StopAsync(Guid sessionId);
}
