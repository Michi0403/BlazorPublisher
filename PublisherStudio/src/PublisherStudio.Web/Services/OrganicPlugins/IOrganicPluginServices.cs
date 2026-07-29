using PublisherStudio.Domain;

namespace PublisherStudio.Services.OrganicPlugins;

public interface IOrganicPluginProtocolCodec
{
    string Serialize(OrganicWireEnvelope envelope, bool seal = true);
    OrganicWireEnvelope DeserializeAndValidate(string json);
    bool Validate(OrganicWireEnvelope envelope, out string error);
}

public interface ILocalGptDiscoveryRegistry
{
    event Action? Changed;
    IReadOnlyList<OrganicPeerAdvertisement> GetPeers();
    OrganicPeerAdvertisement? GetPeer(string peerId);
    void Upsert(OrganicPeerAdvertisement peer);
    void SetConnected(string peerId, bool connected);
    void RemoveExpired(TimeSpan maximumAge);
}


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

public interface IOrganicCapabilityCatalog
{
    Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

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

public interface IOrganicWorkExecutor
{
    Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IOrganicReplayGuard
{
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

public interface IOrganicResultStore
{
    event Action? Changed;
    IReadOnlyList<OrganicPluginWorkItem> GetResults();
    void RecordEnvelope(OrganicWireEnvelope envelope);
    void AddTextProposal(OrganicTextInsertionProposal proposal);
    IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals();
    bool RemoveTextProposal(Guid id);
}

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

public interface IRecurringScreenReaderService
{
    event Action? Changed;
    IReadOnlyList<RecurringScreenReaderSession> GetSessions();
    Task<RecurringScreenReaderSession> StartAsync(string peerId, string selector, string prompt, int intervalSeconds, CancellationToken cancellationToken = default);
    Task<bool> StopAsync(Guid sessionId);
}
