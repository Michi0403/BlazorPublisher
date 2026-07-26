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

public interface IOrganicCapabilityCatalog
{
    Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}

public interface IOrganicPermissionStore
{
    IReadOnlyList<OrganicPermissionRule> GetRules();
    OrganicPermissionRule Save(OrganicPermissionRule rule);
    bool Delete(string peerId, string capabilityKey, string organ);
    bool IsAllowed(OrganicWireEnvelope envelope);
    bool IsDenied(OrganicWireEnvelope envelope);
}

public interface IOrganicWorkCoordinator
{
    event Action? Changed;
    IReadOnlyList<OrganicPluginWorkItem> GetWork();
    OrganicPluginWorkItem? Get(Guid id);
    Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    bool Decline(Guid id, string reason);
}

public interface IOrganicWorkExecutor
{
    Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IOrganicResultStore
{
    IReadOnlyList<OrganicPluginWorkItem> GetResults();
    void RecordEnvelope(OrganicWireEnvelope envelope);
    void AddTextProposal(OrganicTextInsertionProposal proposal);
    IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals();
}

public interface ILocalGptConnectionService : IAsyncDisposable
{
    event Action? Changed;
    OrganicConnectionState State { get; }
    Task<OrganicConnectionState> ConnectAsync(string peerId, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<Guid> SendCouncilRequestAsync(OrganicCouncilPromptRequest request, CancellationToken cancellationToken = default);
    Task<Guid> SendEnvelopeAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task SendWorkResultAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken = default);
}
