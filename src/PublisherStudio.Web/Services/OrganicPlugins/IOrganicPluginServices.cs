using PublisherStudio.BusinessObjects;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Defines the organic plugin protocol codec contract.
/// </summary>
public interface IOrganicPluginProtocolCodec
{
    JsonSerializerOptions JsonOptions { get; }
    /// <summary>
    /// Runs the serialize operation.
    /// </summary>
    string Serialize(OrganicWireEnvelope envelope, bool seal = true);
    /// <summary>
    /// Runs the deserialize and validate operation.
    /// </summary>
    OrganicWireEnvelope DeserializeAndValidate(string json);
    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    bool Validate(OrganicWireEnvelope envelope, out string error);
}

/// <summary>
/// Defines the local gpt discovery registry contract.
/// </summary>
public interface ILocalGptDiscoveryRegistry
{
    event Action? Changed;
    /// <summary>
    /// Gets peers.
    /// </summary>
    IReadOnlyList<OrganicPeerAdvertisement> GetPeers();
    /// <summary>
    /// Gets peer.
    /// </summary>
    OrganicPeerAdvertisement? GetPeer(string peerId);
    /// <summary>
    /// Runs the upsert operation.
    /// </summary>
    void Upsert(OrganicPeerAdvertisement peer);
    /// <summary>
    /// Sets connected.
    /// </summary>
    void SetConnected(string peerId, bool connected);
    /// <summary>
    /// Removes expired.
    /// </summary>
    void RemoveExpired(TimeSpan maximumAge);
}



/// <summary>
/// Defines the organic transport security policy contract.
/// </summary>
public interface IOrganicTransportSecurityPolicy
{
    /// <summary>
    /// Runs the requires protected transport operation.
    /// </summary>
    bool RequiresProtectedTransport(OrganicWireMessageType messageType);
    /// <summary>
    /// Determines whether protected.
    /// </summary>
    bool IsProtected(OrganicWireEnvelope envelope);
}

/// <summary>
/// Defines the organic connection runtime state contract.
/// </summary>
public interface IOrganicConnectionRuntimeState
{
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    OrganicConnectionRuntimeSnapshot GetSnapshot();
    /// <summary>
    /// Sets connected.
    /// </summary>
    void SetConnected(Guid connectionId, string peerId, bool isLoopback);
    /// <summary>
    /// Runs the reset operation.
    /// </summary>
    void Reset(Guid connectionId);
}

/// <summary>
/// Defines the organic wire envelope factory contract.
/// </summary>
public interface IOrganicWireEnvelopeFactory
{
    /// <summary>
    /// Creates work envelope.
    /// </summary>
    OrganicWireEnvelope CreateWorkEnvelope(OrganicPluginWorkItem item, string sourcePeerId);
}

/// <summary>
/// Defines the organic runtime security service contract.
/// </summary>
public interface IOrganicRuntimeSecurityService
{
    /// <summary>
    /// Gets status async.
    /// </summary>
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Ensures created async.
    /// </summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the regenerate async operation.
    /// </summary>
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes async.
    /// </summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets public descriptor async.
    /// </summary>
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates pairing ticket async.
    /// </summary>
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets otp auth URI async.
    /// </summary>
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the establish trust async operation.
    /// </summary>
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the revoke trust async operation.
    /// </summary>
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets trusted peers async.
    /// </summary>
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the protect outgoing async operation.
    /// </summary>
    Task ProtectOutgoingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the unprotect incoming async operation.
    /// </summary>
    Task UnprotectIncomingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic capability catalog contract.
/// </summary>
public interface IOrganicCapabilityCatalog : LocalGPT.WireProtocol.IOneWireCapabilityProvider
{
    /// <summary>
    /// Occurs when the effective local capability directory changes and linked peers should refresh it.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Gets capabilities async.
    /// </summary>
    new Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets skills async.
    /// </summary>
    new Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets UI features async.
    /// </summary>
    new Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets hardware async.
    /// </summary>
    new Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic permission store contract.
/// </summary>
public interface IOrganicPermissionStore
{
    /// <summary>
    /// Occurs when peer exposure or invocation policy changes.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Gets rules.
    /// </summary>
    IReadOnlyList<OrganicPermissionRule> GetRules();
    /// <summary>
    /// Runs the save operation.
    /// </summary>
    OrganicPermissionRule Save(OrganicPermissionRule rule);
    /// <summary>
    /// Runs the delete operation.
    /// </summary>
    bool Delete(string peerId, string capabilityKey, string organ);
    /// <summary>
    /// Determines whether allowed.
    /// </summary>
    bool IsAllowed(OrganicWireEnvelope envelope);
    /// <summary>
    /// Determines whether denied.
    /// </summary>
    bool IsDenied(OrganicWireEnvelope envelope);
    /// <summary>
    /// Determines whether capability exposed.
    /// </summary>
    bool IsCapabilityExposed(string peerId, OrganicCapabilityDescriptor capability);
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    OrganicPermissionRule? Resolve(string peerId, string capabilityKey, string organ = "");
}

/// <summary>
/// Defines the organic work coordinator contract.
/// </summary>
public interface IOrganicWorkCoordinator
{
    event Action? Changed;
    /// <summary>
    /// Gets work.
    /// </summary>
    IReadOnlyList<OrganicPluginWorkItem> GetWork();
    /// <summary>
    /// Runs the get operation.
    /// </summary>
    OrganicPluginWorkItem? Get(Guid id);
    /// <summary>
    /// Runs the receive async operation.
    /// </summary>
    Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the approve async operation.
    /// </summary>
    Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates interaction value.
    /// </summary>
    bool UpdateInteractionValue(Guid id, string value);
    /// <summary>
    /// Runs the decline operation.
    /// </summary>
    bool Decline(Guid id, string reason);
}

/// <summary>
/// Defines the organic work executor contract.
/// </summary>
public interface IOrganicWorkExecutor
{
    /// <summary>
    /// Runs the execute async operation.
    /// </summary>
    Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the organic replay policy data service contract.
/// </summary>
public interface IOrganicReplayPolicyDataService
{
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    OrganicReplayPolicySnapshot GetSnapshot();
}

/// <summary>
/// Defines the organic replay guard contract.
/// </summary>
public interface IOrganicReplayGuard
{
    /// <summary>
    /// Attempts to accept.
    /// </summary>
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

/// <summary>
/// Defines the organic result store contract.
/// </summary>
public interface IOrganicResultStore
{
    event Action? Changed;
    /// <summary>
    /// Gets results.
    /// </summary>
    IReadOnlyList<OrganicPluginWorkItem> GetResults();
    /// <summary>
    /// Runs the record envelope operation.
    /// </summary>
    void RecordEnvelope(OrganicWireEnvelope envelope);
    /// <summary>
    /// Adds text proposal.
    /// </summary>
    void AddTextProposal(OrganicTextInsertionProposal proposal);
    /// <summary>
    /// Gets text proposals.
    /// </summary>
    IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals();
    /// <summary>
    /// Removes text proposal.
    /// </summary>
    bool RemoveTextProposal(Guid id);
}

/// <summary>
/// Defines the local gpt connection service contract.
/// </summary>
public interface ILocalGptConnectionService : IAsyncDisposable
{
    event Action? Changed;
    OrganicConnectionState State { get; }
    /// <summary>
    /// Runs the connect async operation.
    /// </summary>
    Task<OrganicConnectionState> ConnectAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the disconnect async operation.
    /// </summary>
    Task DisconnectAsync();
    /// <summary>
    /// Runs the send council request async operation.
    /// </summary>
    Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the send envelope async operation.
    /// </summary>
    Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the wait for result async operation.
    /// </summary>
    Task<OrganicWireEnvelope> WaitForResultAsync(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the send work result async operation.
    /// </summary>
    Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the recurring screen reader service contract.
/// </summary>
public interface IRecurringScreenReaderService
{
    event Action? Changed;
    /// <summary>
    /// Gets sessions.
    /// </summary>
    IReadOnlyList<RecurringScreenReaderSession> GetSessions();
    /// <summary>
    /// Starts async.
    /// </summary>
    Task<RecurringScreenReaderSession> StartAsync(string peerId, string selector, string prompt, int intervalSeconds, CancellationToken cancellationToken = default);
    /// <summary>
    /// Stops async.
    /// </summary>
    Task<bool> StopAsync(Guid sessionId);
}
