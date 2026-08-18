using PublisherStudio.BusinessObjects;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Tracks explicit frontend demand for LocalGPT discovery so PublisherStudio does not bind or poll discovery sockets before the user opens the connection workflow.
/// </summary>
/// <param name="logger">Logger used to record discovery activation lifecycle diagnostics.</param>
public sealed class LocalGptDiscoveryActivationService(ILogger<LocalGptDiscoveryActivationService> logger) : ILocalGptDiscoveryActivationService
{
    /// <summary>
    /// Synchronizes access to the active frontend owner set.
    /// </summary>
    private readonly object gate = new();

    /// <summary>
    /// Keeps the distinct frontend workflow keys whose presence authorizes the optional discovery listener to own a UDP socket.
    /// </summary>
    private readonly HashSet<string> owners = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether explicit frontend discovery is currently requested.
    /// </summary>
    /// <value><see langword="true"/> when at least one frontend owner is registered; otherwise <see langword="false"/>.</value>
    public bool IsRequested
    {
        get
        {
            lock (gate) return owners.Count > 0;
        }
    }

    /// <summary>
    /// Notifies the hosted discovery listener that frontend network demand transitioned so socket ownership can be reevaluated immediately.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Adds a named frontend workflow to the discovery-demand set and raises the transition event only when ownership actually changes.
    /// </summary>
    /// <param name="owner">Stable frontend owner key used to make repeated requests idempotent.</param>
    public void Request(string owner)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            bool changed;
            lock (gate) changed = owners.Add(owner);
            if (!changed) return;
            logger.LogInformation("LocalGPT discovery was requested by frontend owner {Owner}; discovery demand is now active.", owner);
            Changed?.Invoke();
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryActivationService)}.{nameof(Request)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryActivationService)}.{nameof(Request)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Releases a previously registered frontend discovery owner.
    /// </summary>
    /// <param name="owner">Stable frontend owner key previously supplied to <see cref="Request"/>.</param>
    public void Release(string owner)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            bool changed;
            bool stillRequested;
            lock (gate)
            {
                changed = owners.Remove(owner);
                stillRequested = owners.Count > 0;
            }
            if (!changed) return;
            logger.LogInformation(
                "LocalGPT discovery frontend owner {Owner} was released; discovery demand remains active: {IsRequested}.",
                owner,
                stillRequested);
            Changed?.Invoke();
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryActivationService)}.{nameof(Release)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptDiscoveryActivationService)}.{nameof(Release)} failed.");
            throw;
        }
    }
}

/// <summary>
/// Maintains the authoritative directory of LocalGPT discovery entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalGptDiscoveryRegistry(ILogger<LocalGptDiscoveryRegistry> logger) : ILocalGptDiscoveryRegistry
{
    /// <summary>
    /// Stores the in-memory peers collection maintained internally by <see cref="LocalGptDiscoveryRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, OrganicPeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="LocalGptDiscoveryRegistry"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Retrieves peers in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Retrieves peer in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>The organic peer advertisement produced by the operation.</returns>
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
    /// Performs upsert in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the LocalGPT discovery operation and used when producing its result.</param>
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
    /// Sets connected in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="connected">Value indicating whether connected should apply to this operation.</param>
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
    /// Removes expired in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="maximumAge">Maximum age value supplied to the LocalGPT discovery operation and used when producing its result.</param>
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

    /// <summary>
    /// Performs clone in the LocalGPT discovery directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the LocalGPT discovery operation and used when producing its result.</param>
    /// <returns>The organic peer advertisement produced by the operation.</returns>
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
/// Owns persistence and retrieval of organic permission state, keeping storage-specific behavior behind a focused application abstraction.
/// </summary>
public sealed class OrganicPermissionStore : IOrganicPermissionStore
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="OrganicPermissionStore"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Stores the logger used by <see cref="OrganicPermissionStore"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<OrganicPermissionStore> logger;
    /// <summary>
    /// Stores the organic plugin protocol codec dependency used by <see cref="OrganicPermissionStore"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IOrganicPluginProtocolCodec codec;
    /// <summary>
    /// Stores the internal gate state used by <see cref="OrganicPermissionStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object gate = new();
    /// <summary>
    /// Stores the internal file path state used by <see cref="OrganicPermissionStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string filePath;
    /// <summary>
    /// Stores the in-memory rules collection maintained internally by <see cref="OrganicPermissionStore"/> for its current workflow state.
    /// </summary>
    private List<OrganicPermissionRule>? rules;

    /// <summary>
    /// Initializes a new <see cref="OrganicPermissionStore"/> instance and captures the dependencies or initial state required by its organic permission workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="codec">Organic plugin protocol codec dependency used by the organic permission workflow to provide the corresponding application capability.</param>
    public OrganicPermissionStore(ILogger<OrganicPermissionStore> logger, IOrganicPluginProtocolCodec codec)
    {
        this.logger = logger;
        this.codec = codec;
        filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "OrganicPlugins", "permissions.json");
    }

    /// <summary>
    /// Gets the rules collection maintained or exposed by this organic permission instance for downstream processing.
    /// </summary>
    /// <value>The rules value exposed by <see cref="OrganicPermissionStore"/>.</value>
    private List<OrganicPermissionRule> Rules => rules ??= Load();

    /// <summary>
    /// Retrieves rules in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs save in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="rule">Rule value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
    public OrganicPermissionRule Save(OrganicPermissionRule rule)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentException.ThrowIfNullOrWhiteSpace(rule.PeerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(rule.CapabilityKey);
            OrganicPermissionRule saved;
            lock (gate)
            {
                var existing = Rules.FindIndex(candidate => SameKey(candidate, rule));
                rule.UpdatedUtc = DateTimeOffset.UtcNow;
                if (existing >= 0) Rules[existing] = Clone(rule); else Rules.Add(Clone(rule));
                Persist();
                saved = Clone(rule);
            }
            logger.LogInformation("Saved organic permission {ApprovalMode} for {PeerId}/{CapabilityKey}/{Organ}.", rule.ApprovalMode, rule.PeerId, rule.CapabilityKey, rule.Organ);
            Changed?.Invoke();
            return saved;
    
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
    /// Performs delete in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic permission operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Delete(string peerId, string capabilityKey, string organ)
    {
    try
    {
            bool removed;
            lock (gate)
            {
                removed = Rules.RemoveAll(rule =>
                    string.Equals(rule.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(rule.CapabilityKey, capabilityKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(rule.Organ, organ, StringComparison.OrdinalIgnoreCase)) > 0;
                if (removed) Persist();
            }
            if (removed)
                Changed?.Invoke();
            return removed;
    
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
    /// Determines whether allowed in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Determines whether denied in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Determines whether capability exposed in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capability">Capability value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs resolve in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic permission operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
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

    /// <summary>
    /// Performs matching rules in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Resolves rule in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="candidates">Organic permission rule dependency used by the organic permission workflow to provide the corresponding application capability.</param>
    /// <param name="organ">Organ value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
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

    /// <summary>
    /// Performs load in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs persist in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
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

    /// <summary>
    /// Performs same key in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="left">Left value supplied to the organic permission operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs clone in the organic permission persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicPermissionStore"/>.
    /// </summary>
    /// <param name="rule">Rule value supplied to the organic permission operation and used when producing its result.</param>
    /// <returns>The organic permission rule produced by the operation.</returns>
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
/// Represents an organic replay guard application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="policyData">Organic replay policy data service dependency used by the organic replay guard workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicReplayGuard(
    IOrganicReplayPolicyDataService policyData,
    ILogger<OrganicReplayGuard> logger) : IOrganicReplayGuard
{
    /// <summary>
    /// Stores the in-memory accepted collection maintained internally by <see cref="OrganicReplayGuard"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> accepted = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Stores the internal cleanup counter state used by <see cref="OrganicReplayGuard"/> while executing its surrounding workflow.
    /// </summary>
    private int cleanupCounter;

    /// <summary>
    /// Attempts to accept for <see cref="OrganicReplayGuard"/>, keeping the operation consistent with the state and invariants of the surrounding organic replay guard workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="messageId">Identifier of the message to use for this operation.</param>
    /// <param name="createdUtc">Created utc value supplied to the organic replay guard operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
/// Owns persistence and retrieval of organic result state, keeping storage-specific behavior behind a focused application abstraction.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the organic result workflow to provide the corresponding application capability.</param>
public sealed class OrganicResultStore(IOrganicPluginProtocolCodec codec) : IOrganicResultStore
{
    /// <summary>
    /// Stores the internal results state used by <see cref="OrganicResultStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<OrganicPluginWorkItem> results = new();
    /// <summary>
    /// Stores the in-memory text proposals collection maintained internally by <see cref="OrganicResultStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, OrganicTextInsertionProposal> textProposals = new();

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="OrganicResultStore"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Retrieves results in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs record envelope in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic result operation and used when producing its result.</param>
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
    /// Adds text proposal in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <param name="proposal">Proposal value supplied to the organic result operation and used when producing its result.</param>
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
    /// Retrieves text proposals in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Removes text proposal in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Resolves status in the organic result persistence workflow while keeping storage-specific behavior contained within <see cref="OrganicResultStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic result operation and used when producing its result.</param>
    /// <returns>The organic work status produced by the operation.</returns>
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
