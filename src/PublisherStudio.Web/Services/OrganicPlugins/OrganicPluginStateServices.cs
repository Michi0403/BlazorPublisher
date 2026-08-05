using PublisherStudio.BusinessObjects;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Provides local gpt discovery registry operations.
/// </summary>
public sealed class LocalGptDiscoveryRegistry(ILogger<LocalGptDiscoveryRegistry> logger) : ILocalGptDiscoveryRegistry
{
    private readonly ConcurrentDictionary<string, OrganicPeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets peers.
    /// </summary>
    public IReadOnlyList<OrganicPeerAdvertisement> GetPeers() => peers.Values
        .OrderByDescending(peer => peer.IsConnected).ThenByDescending(peer => peer.SeenUtc).Select(Clone).ToList();
    /// <summary>
    /// Gets peer.
    /// </summary>
    public OrganicPeerAdvertisement? GetPeer(string peerId) => peers.TryGetValue(peerId, out var peer) ? Clone(peer) : null;

    /// <summary>
    /// Runs the upsert operation.
    /// </summary>
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

    /// <summary>
    /// Sets connected.
    /// </summary>
    public void SetConnected(string peerId, bool connected)
    {
        if (peers.TryGetValue(peerId, out var peer))
        {
            peer.IsConnected = connected;
            peer.SeenUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Removes expired.
    /// </summary>
    public void RemoveExpired(TimeSpan maximumAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maximumAge;
        var changed = false;
        foreach (var pair in peers.Where(pair => !pair.Value.IsConnected && pair.Value.SeenUtc < cutoff).ToArray())
            changed |= peers.TryRemove(pair.Key, out _);
        if (changed) Changed?.Invoke();
    }

    private OrganicPeerAdvertisement Clone(OrganicPeerAdvertisement peer) => new()
    {
        PeerId = peer.PeerId, DisplayName = peer.DisplayName, Application = peer.Application,
        ApplicationVersion = peer.ApplicationVersion, HostName = peer.HostName, Address = peer.Address,
        ServicePort = peer.ServicePort, DiscoveryPort = peer.DiscoveryPort, WebBaseUrl = peer.WebBaseUrl,
        SeenUtc = peer.SeenUtc, IsConnected = peer.IsConnected,
        TransportKind = peer.TransportKind, SupportedTransports = peer.SupportedTransports.ToList(),
        Security = new OneWireSecurityDescriptor
        {
            HasRuntimeSecret = peer.Security.HasRuntimeSecret,
            SupportsSigning = peer.Security.SupportsSigning,
            SupportsEncryption = peer.Security.SupportsEncryption,
            SupportsMfaPairing = peer.Security.SupportsMfaPairing,
            KeyId = peer.Security.KeyId,
            Fingerprint = peer.Security.Fingerprint,
            KeyAgreementPublicKey = peer.Security.KeyAgreementPublicKey,
            SigningPublicKey = peer.Security.SigningPublicKey,
            PairingScheme = peer.Security.PairingScheme
        },
        Capabilities = peer.Capabilities.ToList(), Skills = peer.Skills.ToList(),
        UiFeatures = peer.UiFeatures.ToList(), Hardware = peer.Hardware.ToList()
    };
}

/// <summary>
/// Provides organic permission store operations.
/// </summary>
public sealed class OrganicPermissionStore : IOrganicPermissionStore
{
    private readonly ILogger<OrganicPermissionStore> logger;
    private readonly IOrganicPluginProtocolCodec codec;
    private readonly object gate = new();
    private readonly string filePath;
    private List<OrganicPermissionRule>? rules;

    /// <summary>
    /// Runs the organic permission store operation.
    /// </summary>
    public OrganicPermissionStore(ILogger<OrganicPermissionStore> logger, IOrganicPluginProtocolCodec codec)
    {
        this.logger = logger;
        this.codec = codec;
        filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "OrganicPlugins", "permissions.json");
    }

    private List<OrganicPermissionRule> Rules => rules ??= Load();

    /// <summary>
    /// Gets rules.
    /// </summary>
    public IReadOnlyList<OrganicPermissionRule> GetRules()
    {
        lock (gate)
            return Rules.Select(Clone).OrderBy(rule => rule.CapabilityKey).ThenBy(rule => rule.Organ).ToList();
    }

    /// <summary>
    /// Runs the save operation.
    /// </summary>
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

    /// <summary>
    /// Runs the delete operation.
    /// </summary>
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

    /// <summary>
    /// Determines whether allowed.
    /// </summary>
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

    /// <summary>
    /// Determines whether denied.
    /// </summary>
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


    /// <summary>
    /// Determines whether capability exposed.
    /// </summary>
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

    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
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

    private OrganicPermissionRule? ResolveRule(IReadOnlyList<OrganicPermissionRule> candidates, string organ) =>
        candidates.FirstOrDefault(candidate => string.Equals(candidate.Organ, organ, StringComparison.OrdinalIgnoreCase))
        ?? candidates.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.Organ));

    private List<OrganicPermissionRule> Load()
    {
        try
        {
            if (!File.Exists(filePath)) return [];
            return JsonSerializer.Deserialize<List<OrganicPermissionRule>>(File.ReadAllText(filePath), codec.JsonOptions) ?? [];
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
        File.WriteAllText(temporary, JsonSerializer.Serialize(Rules, new JsonSerializerOptions(codec.JsonOptions) { WriteIndented = true }));
        File.Move(temporary, filePath, overwrite: true);
    }

    private bool SameKey(OrganicPermissionRule left, OrganicPermissionRule right) =>
        string.Equals(left.PeerId, right.PeerId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.CapabilityKey, right.CapabilityKey, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Organ, right.Organ, StringComparison.OrdinalIgnoreCase);

    private OrganicPermissionRule Clone(OrganicPermissionRule rule) => new()
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

/// <summary>
/// Represents an organic replay guard.
/// </summary>
public sealed class OrganicReplayGuard(
    IOrganicReplayPolicyDataService policyData,
    ILogger<OrganicReplayGuard> logger) : IOrganicReplayGuard
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> accepted = new(StringComparer.OrdinalIgnoreCase);
    private int cleanupCounter;

    /// <summary>
    /// Attempts to accept.
    /// </summary>
    public bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            if (messageId == Guid.Empty)
                return false;

            var now = DateTimeOffset.UtcNow;
            var policy = policyData.GetSnapshot();
            if (createdUtc < now - policy.Retention || createdUtc > now + policy.AllowedFutureSkew)
            {
                logger.LogWarning($"Rejected organic 1-Wire message {messageId} from {peerId} because its timestamp is outside the accepted replay window.");
                return false;
            }

            var key = $"{peerId}\n{messageId:N}";
            if (!accepted.TryAdd(key, now.Add(policy.Retention)))
            {
                logger.LogWarning($"Rejected replayed organic 1-Wire message {messageId} from {peerId}.");
                return false;
            }

            if (Interlocked.Increment(ref cleanupCounter) % policy.CleanupInterval == 0 || accepted.Count > policy.MaximumTrackedMessages)
            {
                foreach (var stale in accepted.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
                    accepted.TryRemove(stale, out _);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not evaluate replay protection for organic message {messageId} from {peerId}.");
            return false;
        }
    }
}

/// <summary>
/// Provides organic result store operations.
/// </summary>
public sealed class OrganicResultStore(IOrganicPluginProtocolCodec codec) : IOrganicResultStore
{
    private readonly ConcurrentQueue<OrganicPluginWorkItem> results = new();
    private readonly ConcurrentDictionary<Guid, OrganicTextInsertionProposal> textProposals = new();

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets results.
    /// </summary>
    public IReadOnlyList<OrganicPluginWorkItem> GetResults() => results.Reverse().Take(200).ToList();
    /// <summary>
    /// Runs the record envelope operation.
    /// </summary>
    public void RecordEnvelope(OrganicWireEnvelope envelope)
    {
        results.Enqueue(new OrganicPluginWorkItem
        {
            MessageId = envelope.MessageId, CorrelationId = envelope.CorrelationId, PeerId = envelope.SourcePeerId,
            CapabilityKey = envelope.CapabilityKey, Status = ResolveStatus(envelope),
            Request = envelope, ResultJson = envelope.Properties is null ? string.Empty : JsonSerializer.Serialize(envelope.Properties, codec.JsonOptions), Error = envelope.Error
        });
        while (results.Count > 200) results.TryDequeue(out _);
        Changed?.Invoke();
    }
    /// <summary>
    /// Adds text proposal.
    /// </summary>
    public void AddTextProposal(OrganicTextInsertionProposal proposal)
    {
        textProposals[proposal.Id] = proposal;
        Changed?.Invoke();
    }
    /// <summary>
    /// Gets text proposals.
    /// </summary>
    public IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals() => textProposals.Values.OrderByDescending(item => item.CreatedUtc).ToList();
    /// <summary>
    /// Removes text proposal.
    /// </summary>
    public bool RemoveTextProposal(Guid id)
    {
        var removed = textProposals.TryRemove(id, out _);
        if (removed) Changed?.Invoke();
        return removed;
    }

    private OrganicWorkStatus ResolveStatus(OrganicWireEnvelope envelope)
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
