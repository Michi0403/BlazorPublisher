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
    public IReadOnlyList<OrganicPeerAdvertisement> GetPeers() {
    try
    {
        return peers.Values
        .OrderByDescending(peer => peer.IsConnected).ThenByDescending(peer => peer.SeenUtc).Select(Clone).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(GetPeers)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(GetPeers)} failed.");
        throw;
    }
}
    /// <summary>
    /// Gets peer.
    /// </summary>
    public OrganicPeerAdvertisement? GetPeer(string peerId) {
    try
    {
        return peers.TryGetValue(peerId, out var peer) ? Clone(peer) : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(GetPeer)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(GetPeer)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the upsert operation.
    /// </summary>
    public void Upsert(OrganicPeerAdvertisement peer)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(Upsert)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(Upsert)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets connected.
    /// </summary>
    public void SetConnected(string peerId, bool connected)
    {
    try
    {
            if (peers.TryGetValue(peerId, out var peer))
            {
                peer.IsConnected = connected;
                peer.SeenUtc = DateTimeOffset.UtcNow;
                Changed?.Invoke();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(SetConnected)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(SetConnected)} failed.");
        throw;
    }
}

    /// <summary>
    /// Removes expired.
    /// </summary>
    public void RemoveExpired(TimeSpan maximumAge)
    {
    try
    {
            var cutoff = DateTimeOffset.UtcNow - maximumAge;
            var changed = false;
            foreach (var pair in peers.Where(pair => !pair.Value.IsConnected && pair.Value.SeenUtc < cutoff).ToArray())
                changed |= peers.TryRemove(pair.Key, out _);
            if (changed) Changed?.Invoke();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(RemoveExpired)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(RemoveExpired)} failed.");
        throw;
    }
}

    private OrganicPeerAdvertisement Clone(OrganicPeerAdvertisement peer) {
    try
    {
        return new()
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(Clone)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryRegistry)}.{nameof(Clone)} failed.");
        throw;
    }
}
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
    try
    {
            lock (gate)
                return Rules.Select(Clone).OrderBy(rule => rule.CapabilityKey).ThenBy(rule => rule.Organ).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(GetRules)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(GetRules)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the save operation.
    /// </summary>
    public OrganicPermissionRule Save(OrganicPermissionRule rule)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Save)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Save)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the delete operation.
    /// </summary>
    public bool Delete(string peerId, string capabilityKey, string organ)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Delete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Delete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether allowed.
    /// </summary>
    public bool IsAllowed(OrganicWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsAllowed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsAllowed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether denied.
    /// </summary>
    public bool IsDenied(OrganicWireEnvelope envelope)
    {
    try
    {
            lock (gate)
            {
                var candidates = MatchingRules(envelope);
                return envelope.Organs.DefaultIfEmpty(string.Empty)
                    .Select(organ => ResolveRule(candidates, organ))
                    .Any(rule => rule is not null && (!rule.AllowInvocation || rule.ApprovalMode == OrganicApprovalMode.Deny));
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsDenied)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsDenied)} failed.");
        throw;
    }
}


    /// <summary>
    /// Determines whether capability exposed.
    /// </summary>
    public bool IsCapabilityExposed(string peerId, OrganicCapabilityDescriptor capability)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsCapabilityExposed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(IsCapabilityExposed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    public OrganicPermissionRule? Resolve(string peerId, string capabilityKey, string organ = "")
    {
    try
    {
            lock (gate)
            {
                var candidates = Rules.Where(rule =>
                    string.Equals(rule.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(rule.CapabilityKey, capabilityKey, StringComparison.OrdinalIgnoreCase)).ToList();
                return ResolveRule(candidates, organ) is { } rule ? Clone(rule) : null;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Resolve)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Resolve)} failed.");
        throw;
    }
}

    private List<OrganicPermissionRule> MatchingRules(OrganicWireEnvelope envelope) {
    try
    {
        return Rules.Where(rule =>
        string.Equals(rule.PeerId, envelope.SourcePeerId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(rule.CapabilityKey, envelope.CapabilityKey, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(MatchingRules)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(MatchingRules)} failed.");
        throw;
    }
}

    private OrganicPermissionRule? ResolveRule(IReadOnlyList<OrganicPermissionRule> candidates, string organ) {
    try
    {
        return candidates.FirstOrDefault(candidate => string.Equals(candidate.Organ, organ, StringComparison.OrdinalIgnoreCase))
        ?? candidates.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.Organ));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(ResolveRule)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(ResolveRule)} failed.");
        throw;
    }
}

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
    try
    {
            var directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);
            var temporary = filePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(Rules, new JsonSerializerOptions(codec.JsonOptions) { WriteIndented = true }));
            File.Move(temporary, filePath, overwrite: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Persist)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Persist)} failed.");
        throw;
    }
}

    private bool SameKey(OrganicPermissionRule left, OrganicPermissionRule right) {
    try
    {
        return string.Equals(left.PeerId, right.PeerId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.CapabilityKey, right.CapabilityKey, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Organ, right.Organ, StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(SameKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(SameKey)} failed.");
        throw;
    }
}

    private OrganicPermissionRule Clone(OrganicPermissionRule rule) {
    try
    {
        return new()
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Clone)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicPermissionStore)}.{nameof(Clone)} failed.");
        throw;
    }
}
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
    public IReadOnlyList<OrganicPluginWorkItem> GetResults() {
    try
    {
        return results.Reverse().Take(200).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.GetResults failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Runs the record envelope operation.
    /// </summary>
    public void RecordEnvelope(OrganicWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.RecordEnvelope failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Adds text proposal.
    /// </summary>
    public void AddTextProposal(OrganicTextInsertionProposal proposal)
    {
    try
    {
            textProposals[proposal.Id] = proposal;
            Changed?.Invoke();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.AddTextProposal failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Gets text proposals.
    /// </summary>
    public IReadOnlyList<OrganicTextInsertionProposal> GetTextProposals() {
    try
    {
        return textProposals.Values.OrderByDescending(item => item.CreatedUtc).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.GetTextProposals failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Removes text proposal.
    /// </summary>
    public bool RemoveTextProposal(Guid id)
    {
    try
    {
            var removed = textProposals.TryRemove(id, out _);
            if (removed) Changed?.Invoke();
            return removed;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.RemoveTextProposal failed: {__serviceMethodException}");
        throw;
    }
}

    private OrganicWorkStatus ResolveStatus(OrganicWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicResultStore.ResolveStatus failed: {__serviceMethodException}");
        throw;
    }
}
}
