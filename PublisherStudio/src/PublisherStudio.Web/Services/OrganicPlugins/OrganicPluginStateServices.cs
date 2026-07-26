using PublisherStudio.Domain;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class LocalGptDiscoveryRegistry(ILogger<LocalGptDiscoveryRegistry> logger) : ILocalGptDiscoveryRegistry
{
    private readonly ConcurrentDictionary<string, OrganicPeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);
    public event Action? Changed;

    public IReadOnlyList<OrganicPeerAdvertisement> GetPeers() => peers.Values
        .OrderByDescending(peer => peer.IsConnected).ThenByDescending(peer => peer.SeenUtc).Select(Clone).ToList();
    public OrganicPeerAdvertisement? GetPeer(string peerId) => peers.TryGetValue(peerId, out var peer) ? Clone(peer) : null;

    public void Upsert(OrganicPeerAdvertisement peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentException.ThrowIfNullOrWhiteSpace(peer.PeerId);
        peer.SeenUtc = DateTimeOffset.UtcNow;
        peers.AddOrUpdate(peer.PeerId, _ => Clone(peer), (_, existing) =>
        {
            var replacement = Clone(peer);
            replacement.IsConnected |= existing.IsConnected;
            return replacement;
        });
        logger.LogDebug("Discovered LocalGPT peer {PeerId} on {Address}:{Port}.", peer.PeerId, peer.Address, peer.ServicePort);
        Changed?.Invoke();
    }

    public void SetConnected(string peerId, bool connected)
    {
        if (peers.TryGetValue(peerId, out var peer))
        {
            peer.IsConnected = connected;
            peer.SeenUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
        }
    }

    public void RemoveExpired(TimeSpan maximumAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maximumAge;
        var changed = false;
        foreach (var pair in peers.Where(pair => !pair.Value.IsConnected && pair.Value.SeenUtc < cutoff).ToArray())
            changed |= peers.TryRemove(pair.Key, out _);
        if (changed) Changed?.Invoke();
    }

    private static OrganicPeerAdvertisement Clone(OrganicPeerAdvertisement peer) => new()
    {
        PeerId = peer.PeerId, DisplayName = peer.DisplayName, Application = peer.Application,
        ApplicationVersion = peer.ApplicationVersion, HostName = peer.HostName, Address = peer.Address,
        ServicePort = peer.ServicePort, DiscoveryPort = peer.DiscoveryPort, WebBaseUrl = peer.WebBaseUrl,
        SeenUtc = peer.SeenUtc, IsConnected = peer.IsConnected,
        Capabilities = peer.Capabilities.ToList(), Skills = peer.Skills.ToList(),
        UiFeatures = peer.UiFeatures.ToList(), Hardware = peer.Hardware.ToList()
    };
}

public sealed class OrganicPermissionStore : IOrganicPermissionStore
{
    private readonly ILogger<OrganicPermissionStore> logger;
    private readonly object gate = new();
    private readonly string filePath;
    private List<OrganicPermissionRule>? rules;

    public OrganicPermissionStore(ILogger<OrganicPermissionStore> logger)
    {
        this.logger = logger;
        filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "OrganicPlugins", "permissions.json");
    }

    private List<OrganicPermissionRule> Rules => rules ??= Load();

    public IReadOnlyList<OrganicPermissionRule> GetRules()
    {
        lock (gate)
            return Rules.Select(Clone).OrderBy(rule => rule.CapabilityKey).ThenBy(rule => rule.Organ).ToList();
    }

    public OrganicPermissionRule Save(OrganicPermissionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.PeerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.CapabilityKey);
        lock (gate)
        {
            var existing = Rules.FindIndex(candidate => SameKey(candidate, rule));
            rule.UpdatedUtc = DateTimeOffset.UtcNow;
            if (existing >= 0) Rules[existing] = Clone(rule); else Rules.Add(Clone(rule));
            Persist();
            logger.LogInformation("Saved organic permission {ApprovalMode} for {PeerId}/{CapabilityKey}/{Organ}.", rule.ApprovalMode, rule.PeerId, rule.CapabilityKey, rule.Organ);
            return Clone(rule);
        }
    }

    public bool Delete(string peerId, string capabilityKey, string organ)
    {
        lock (gate)
        {
            var removed = Rules.RemoveAll(rule =>
                string.Equals(rule.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(rule.CapabilityKey, capabilityKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(rule.Organ, organ, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed) Persist();
            return removed;
        }
    }

    public bool IsAllowed(OrganicWireEnvelope envelope)
    {
        lock (gate)
        {
            var candidates = MatchingRules(envelope);
            foreach (var organ in envelope.Organs.DefaultIfEmpty(string.Empty))
            {
                var rule = ResolveRule(candidates, organ);
                if (rule is null || !rule.AllowInvocation || rule.RequiresFrontendConfirmation ||
                    rule.ApprovalMode is OrganicApprovalMode.AskEveryTime or OrganicApprovalMode.Deny)
                    return false;
                if (rule.ApprovalMode == OrganicApprovalMode.CurrentWorkOrder &&
                    (string.IsNullOrWhiteSpace(rule.WorkOrderKey) ||
                     string.IsNullOrWhiteSpace(envelope.WorkOrderKey) ||
                     !string.Equals(rule.WorkOrderKey, envelope.WorkOrderKey, StringComparison.Ordinal)))
                    return false;
            }
            return candidates.Count > 0;
        }
    }

    public bool IsDenied(OrganicWireEnvelope envelope)
    {
        lock (gate)
        {
            var candidates = MatchingRules(envelope);
            return envelope.Organs.DefaultIfEmpty(string.Empty)
                .Select(organ => ResolveRule(candidates, organ))
                .Any(rule => rule is not null && (!rule.AllowInvocation || rule.ApprovalMode == OrganicApprovalMode.Deny));
        }
    }


    public bool IsCapabilityExposed(string peerId, OrganicCapabilityDescriptor capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        lock (gate)
        {
            var matching = Rules.Where(rule =>
                string.Equals(rule.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(rule.CapabilityKey, capability.Key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matching.Count == 0)
                return capability.IsExposedToPeer;
            return matching.Any(rule => rule.IsExposed);
        }
    }

    public OrganicPermissionRule? Resolve(string peerId, string capabilityKey, string organ = "")
    {
        lock (gate)
        {
            var candidates = Rules.Where(rule =>
                string.Equals(rule.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(rule.CapabilityKey, capabilityKey, StringComparison.OrdinalIgnoreCase)).ToList();
            return ResolveRule(candidates, organ) is { } rule ? Clone(rule) : null;
        }
    }

    private List<OrganicPermissionRule> MatchingRules(OrganicWireEnvelope envelope) => Rules.Where(rule =>
        string.Equals(rule.PeerId, envelope.SourcePeerId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(rule.CapabilityKey, envelope.CapabilityKey, StringComparison.OrdinalIgnoreCase)).ToList();

    private static OrganicPermissionRule? ResolveRule(IReadOnlyList<OrganicPermissionRule> candidates, string organ) =>
        candidates.FirstOrDefault(candidate => string.Equals(candidate.Organ, organ, StringComparison.OrdinalIgnoreCase))
        ?? candidates.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.Organ));

    private List<OrganicPermissionRule> Load()
    {
        try
        {
            if (!File.Exists(filePath)) return [];
            return JsonSerializer.Deserialize<List<OrganicPermissionRule>>(File.ReadAllText(filePath), OrganicPluginProtocolCodec.JsonOptions) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not load organic plugin permissions. AskEveryTime remains the safe default.");
            return [];
        }
    }

    private void Persist()
    {
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);
        var temporary = filePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(Rules, new JsonSerializerOptions(OrganicPluginProtocolCodec.JsonOptions) { WriteIndented = true }));
        File.Move(temporary, filePath, overwrite: true);
    }

    private static bool SameKey(OrganicPermissionRule left, OrganicPermissionRule right) =>
        string.Equals(left.PeerId, right.PeerId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.CapabilityKey, right.CapabilityKey, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Organ, right.Organ, StringComparison.OrdinalIgnoreCase);

    private static OrganicPermissionRule Clone(OrganicPermissionRule rule) => new()
    {
        PeerId = rule.PeerId,
        CapabilityKey = rule.CapabilityKey,
        Organ = rule.Organ,
        ApprovalMode = rule.ApprovalMode,
        IsExposed = rule.IsExposed,
        AllowInvocation = rule.AllowInvocation,
        RequiresFrontendConfirmation = rule.RequiresFrontendConfirmation,
        InteractionEditor = rule.InteractionEditor,
        RequireLinkedPeer = rule.RequireLinkedPeer,
        WorkOrderKey = rule.WorkOrderKey,
        UpdatedUtc = rule.UpdatedUtc,
        UpdatedBy = rule.UpdatedBy
    };
}

public sealed class OrganicResultStore : IOrganicResultStore
{
    private readonly ConcurrentQueue<OrganicPluginWorkItem> results = new();
    private readonly ConcurrentDictionary<Guid, OrganicTextInsertionProposal> textProposals = new();

    public IReadOnlyList<OrganicPluginWorkItem> GetResults() => results.Reverse().Take(200).ToList();
    public void RecordEnvelope(OrganicWireEnvelope envelope)
    {
        results.Enqueue(new OrganicPluginWorkItem
        {
            MessageId = envelope.MessageId, CorrelationId = envelope.CorrelationId, PeerId = envelope.SourcePeerId,
            CapabilityKey = envelope.CapabilityKey, Status = ResolveStatus(envelope),
            Request = envelope, ResultJson = envelope.Properties is null ? string.Empty : JsonSerializer.Serialize(envelope.Properties, OrganicPluginProtocolCodec.JsonOptions), Error = envelope.Error
        });
        while (results.Count > 200) results.TryDequeue(out _);
    }
    public void AddTextProposal(OrganicTextInsertionProposal proposal) => textProposals[proposal.Id] = proposal;
    public IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals() => textProposals.Values.OrderByDescending(item => item.CreatedUtc).ToList();

    private static OrganicWorkStatus ResolveStatus(OrganicWireEnvelope envelope)
    {
        if (envelope.MessageType == OrganicWireMessageType.WorkResult &&
            envelope.Properties is not null &&
            envelope.Properties.TryGetValue("Status", out var statusElement) &&
            statusElement.ValueKind == JsonValueKind.String &&
            Enum.TryParse<OrganicWorkStatus>(statusElement.GetString(), true, out var status))
            return status;
        return envelope.MessageType switch
        {
            OrganicWireMessageType.Error => OrganicWorkStatus.Failed,
            OrganicWireMessageType.ApprovalRequired => OrganicWorkStatus.PendingApproval,
            OrganicWireMessageType.WorkAccepted => OrganicWorkStatus.Queued,
            _ => OrganicWorkStatus.Completed
        };
    }
}
