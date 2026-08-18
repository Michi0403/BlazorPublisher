using PublisherStudio.BusinessObjects;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Defines the contract for organic plugin protocol codec behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicPluginProtocolCodec
{
    /// <summary>
    /// Gets the JSON options value that forms part of the organic plugin protocol codec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON options value exposed by <see cref="IOrganicPluginProtocolCodec"/>.</value>
    JsonSerializerOptions JsonOptions { get; }
    /// <summary>
    /// Performs serialize for <see cref="IOrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <param name="seal">Value indicating whether seal should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string Serialize(OrganicWireEnvelope envelope, bool seal = true);
    /// <summary>
    /// Performs deserialize and validate for <see cref="IOrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="json">Json value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>The organic wire envelope produced by the operation.</returns>
    OrganicWireEnvelope DeserializeAndValidate(string json);
    /// <summary>
    /// Performs validate for <see cref="IOrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Validate(OrganicWireEnvelope envelope, out string error);
}

/// <summary>
/// Defines the explicit frontend-demand gate that owns when PublisherStudio may activate LocalGPT discovery network activity.
/// </summary>
public interface ILocalGptDiscoveryActivationService
{
    /// <summary>
    /// Defines the stable owner key used by the PublisherStudio frontend connection workflow so repeated activation requests remain idempotent across ribbon and routed-page transitions.
    /// </summary>
    const string FrontendConnectionWorkflowOwner = "PublisherStudio.OrganicPlugins.Frontend";
    /// <summary>
    /// Gets a value indicating whether at least one explicit PublisherStudio frontend workflow currently requests LocalGPT discovery.
    /// </summary>
    /// <value><see langword="true"/> only while an explicit frontend owner requests discovery; otherwise <see langword="false"/>.</value>
    bool IsRequested { get; }

    /// <summary>
    /// Occurs when the explicit frontend discovery request set changes so the hosted listener can start or stop without polling.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Adds a named PublisherStudio frontend workflow to the active discovery-demand set and signals the hosted listener only when that set changes.
    /// </summary>
    /// <param name="owner">Stable frontend owner key used to make repeated requests idempotent.</param>
    void Request(string owner);

    /// <summary>
    /// Releases a previously registered frontend discovery owner.
    /// </summary>
    /// <param name="owner">Stable frontend owner key previously supplied to <see cref="Request"/>.</param>
    void Release(string owner);
}

/// <summary>
/// Defines the contract for LocalGPT discovery behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptDiscoveryRegistry
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ILocalGptDiscoveryRegistry"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves peers in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicPeerAdvertisement> GetPeers();
    /// <summary>
    /// Retrieves peer in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>The organic peer advertisement produced by the operation.</returns>
    OrganicPeerAdvertisement? GetPeer(string peerId);
    /// <summary>
    /// Performs upsert in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the LocalGPT discovery operation and used when producing its result.</param>
    void Upsert(OrganicPeerAdvertisement peer);
    /// <summary>
    /// Sets connected in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="connected">Value indicating whether connected should apply to this operation.</param>
    void SetConnected(string peerId, bool connected);
    /// <summary>
    /// Removes expired in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="maximumAge">Maximum age value supplied to the LocalGPT discovery operation and used when producing its result.</param>
    void RemoveExpired(TimeSpan maximumAge);
}



/// <summary>
/// Defines the contract for organic transport security policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicTransportSecurityPolicy
{
    /// <summary>
    /// Performs requires protected transport for <see cref="IOrganicTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding organic transport security policy workflow.
    /// </summary>
    /// <param name="messageType">Message type value supplied to the organic transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool RequiresProtectedTransport(OrganicWireMessageType messageType);
    /// <summary>
    /// Determines whether protected for <see cref="IOrganicTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding organic transport security policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsProtected(OrganicWireEnvelope envelope);
}

/// <summary>
/// Defines the contract for organic connection runtime behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicConnectionRuntimeState
{
    /// <summary>
    /// Retrieves snapshot for <see cref="IOrganicConnectionRuntimeState"/>, keeping the operation consistent with the state and invariants of the surrounding organic connection runtime workflow.
    /// </summary>
    /// <returns>The organic connection runtime snapshot produced by the operation.</returns>
    OrganicConnectionRuntimeSnapshot GetSnapshot();
    /// <summary>
    /// Sets connected for <see cref="IOrganicConnectionRuntimeState"/>, keeping the operation consistent with the state and invariants of the surrounding organic connection runtime workflow.
    /// </summary>
    /// <param name="connectionId">Identifier of the connection to use for this operation.</param>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="isLoopback">Value indicating whether is loopback should apply to this operation.</param>
    void SetConnected(Guid connectionId, string peerId, bool isLoopback);
    /// <summary>
    /// Performs reset for <see cref="IOrganicConnectionRuntimeState"/>, keeping the operation consistent with the state and invariants of the surrounding organic connection runtime workflow.
    /// </summary>
    /// <param name="connectionId">Identifier of the connection to use for this operation.</param>
    void Reset(Guid connectionId);
}

/// <summary>
/// Defines the contract for organic wire envelope behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicWireEnvelopeFactory
{
    /// <summary>
    /// Creates work envelope using the configuration and dependencies owned by <see cref="IOrganicWireEnvelopeFactory"/>.
    /// </summary>
    /// <param name="item">Item value supplied to the organic wire envelope operation and used when producing its result.</param>
    /// <param name="sourcePeerId">Identifier of the source peer to use for this operation.</param>
    /// <returns>The organic wire envelope produced by the operation.</returns>
    OrganicWireEnvelope CreateWorkEnvelope(OrganicPluginWorkItem item, string sourcePeerId);
}

/// <summary>
/// Defines the contract for organic runtime security behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicRuntimeSecurityService
{
    /// <summary>
    /// Retrieves status as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire runtime security status produced by the operation.</returns>
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Ensures created as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs regenerate as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeleteAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves public descriptor as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire security descriptor produced by the operation.</returns>
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates pairing ticket as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="lifetime">Lifetime value supplied to the organic runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire pairing ticket produced by the operation.</returns>
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves otp auth URI as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs establish trust as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs revoke trust as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves trusted peers as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs protect outgoing as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ProtectOutgoingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs unprotect incoming as part of the organic runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UnprotectIncomingAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for organic capability behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicCapabilityCatalog : LocalGPT.WireProtocol.IOneWireCapabilityProvider
{
    /// <summary>
    /// Occurs when the effective local capability directory changes and linked peers should refresh it.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves capabilities in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    new Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves skills in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    new Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves UI features in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    new Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves hardware in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    new Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for organic permission behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicPermissionStore
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IOrganicPermissionStore"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves rules in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicPermissionRule> GetRules();
    /// <summary>
    /// Performs save in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="rule">Rule value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
    OrganicPermissionRule Save(OrganicPermissionRule rule);
    /// <summary>
    /// Performs delete in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic permission operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Delete(string peerId, string capabilityKey, string organ);
    /// <summary>
    /// Determines whether allowed in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsAllowed(OrganicWireEnvelope envelope);
    /// <summary>
    /// Determines whether denied in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsDenied(OrganicWireEnvelope envelope);
    /// <summary>
    /// Determines whether capability exposed in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capability">Capability value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsCapabilityExposed(string peerId, OrganicCapabilityDescriptor capability);
    /// <summary>
    /// Performs resolve in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic permission operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
    OrganicPermissionRule? Resolve(string peerId, string capabilityKey, string organ = "");
}

/// <summary>
/// Defines the contract for organic work behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicWorkCoordinator
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IOrganicWorkCoordinator"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves work for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicPluginWorkItem> GetWork();
    /// <summary>
    /// Performs get for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    OrganicPluginWorkItem? Get(Guid id);
    /// <summary>
    /// Performs receive for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs approve for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates interaction value for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="value">Value value supplied to the organic work operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool UpdateInteractionValue(Guid id, string value);
    /// <summary>
    /// Performs decline for <see cref="IOrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="reason">Reason value supplied to the organic work operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Decline(Guid id, string reason);
}

/// <summary>
/// Defines the contract for organic work executor behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicWorkExecutor
{
    /// <summary>
    /// Performs execute for <see cref="IOrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the contract for organic replay policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicReplayPolicyDataService
{
    /// <summary>
    /// Retrieves snapshot as part of the organic replay policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The organic replay policy snapshot produced by the operation.</returns>
    OrganicReplayPolicySnapshot GetSnapshot();
}

/// <summary>
/// Defines the contract for organic replay guard behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicReplayGuard
{
    /// <summary>
    /// Attempts to accept for <see cref="IOrganicReplayGuard"/>, keeping the operation consistent with the state and invariants of the surrounding organic replay guard workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="messageId">Identifier of the message to use for this operation.</param>
    /// <param name="createdUtc">Created utc value supplied to the organic replay guard operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

/// <summary>
/// Defines the contract for organic result behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicResultStore
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IOrganicResultStore"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves results in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicResultStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicPluginWorkItem> GetResults();
    /// <summary>
    /// Performs record envelope in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicResultStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic result operation and used when producing its result.</param>
    void RecordEnvelope(OrganicWireEnvelope envelope);
    /// <summary>
    /// Adds text proposal in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicResultStore"/>.
    /// </summary>
    /// <param name="proposal">Proposal value supplied to the organic result operation and used when producing its result.</param>
    void AddTextProposal(OrganicTextInsertionProposal proposal);
    /// <summary>
    /// Retrieves text proposals in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicResultStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals();
    /// <summary>
    /// Removes text proposal in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="IOrganicResultStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool RemoveTextProposal(Guid id);
}

/// <summary>
/// Defines the contract for LocalGPT connection behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptConnectionService : IAsyncDisposable
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ILocalGptConnectionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Gets the state value that forms part of the LocalGPT connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The state value exposed by <see cref="ILocalGptConnectionService"/>.</value>
    OrganicConnectionState State { get; }
    /// <summary>
    /// Performs connect as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic connection state produced by the operation.</returns>
    Task<OrganicConnectionState> ConnectAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs disconnect as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DisconnectAsync();
    /// <summary>
    /// Performs send council request as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The GUID produced by the operation.</returns>
    Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs send envelope as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the LocalGPT connection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The GUID produced by the operation.</returns>
    Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs wait for result as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="timeout">Timeout value supplied to the LocalGPT connection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic wire envelope produced by the operation.</returns>
    Task<OrganicWireEnvelope> WaitForResultAsync(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs send work result as part of the LocalGPT connection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="item">Item value supplied to the LocalGPT connection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for recurring screen reader behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRecurringScreenReaderService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IRecurringScreenReaderService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves sessions as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<RecurringScreenReaderSession> GetSessions();
    /// <summary>
    /// Performs start as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="selector">Selector value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="intervalSeconds">Interval seconds value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The recurring screen reader session produced by the operation.</returns>
    Task<RecurringScreenReaderSession> StartAsync(string peerId, string selector, string prompt, int intervalSeconds, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs stop as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> StopAsync(Guid sessionId);
}
