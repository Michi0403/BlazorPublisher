namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents an organic plugin work item.
/// </summary>
public sealed class OrganicPluginWorkItem
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets message identifier.
    /// </summary>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public OrganicWorkStatus Status { get; set; } = OrganicWorkStatus.Queued;
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the UTC update time.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public OrganicWireEnvelope Request { get; set; } = new();
    /// <summary>
    /// Gets or sets result JSON.
    /// </summary>
    public string ResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an organic connection state.
/// </summary>
public sealed class OrganicConnectionState
{
    /// <summary>True while the TCP transport is alive.</summary>
    public bool IsConnected { get; set; }
    /// <summary>True only after both frontends participated: PublisherStudio initiated the link and LocalGPT approved it.</summary>
    public bool IsLinked { get; set; }
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets connected UTC.
    /// </summary>
    public DateTimeOffset? ConnectedUtc { get; set; }
    /// <summary>
    /// Gets or sets last error.
    /// </summary>
    public string LastError { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets remote capabilities.
    /// </summary>
    public List<OrganicCapabilityDescriptor> RemoteCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets remote skills.
    /// </summary>
    public List<OrganicSkillDescriptor> RemoteSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets remote UI features.
    /// </summary>
    public List<OrganicUiFeatureDescriptor> RemoteUiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets remote hardware.
    /// </summary>
    public List<OrganicHardwareDescriptor> RemoteHardware { get; set; } = [];

    /// <summary>
    /// Determines whether capability.
    /// </summary>
    public bool HasCapability(string key) => IsConnected && IsLinked && RemoteCapabilities.Any(item =>
        item.IsEnabled && item.IsOnline && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets UI feature state.
    /// </summary>
    public OrganicUiFeatureState GetUiFeatureState(string key, OrganicUiFeatureState fallback = OrganicUiFeatureState.Hidden)
    {
        if (!IsConnected || !IsLinked) return OrganicUiFeatureState.Hidden;
        return RemoteUiFeatures.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.State ?? fallback;
    }
}

/// <summary>
/// Represents an organic text insertion proposal.
/// </summary>
public sealed class OrganicTextInsertionProposal
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets target.
    /// </summary>
    public string Target { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets accepted.
    /// </summary>
    public bool Accepted { get; set; }
}

/// <summary>
/// Represents an organic plugin options.
/// </summary>
public sealed class OrganicPluginOptions
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "OrganicPlugins";
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets enable discovery.
    /// </summary>
    public bool EnableDiscovery { get; set; } = true;
    /// <summary>
    /// Gets or sets auto connect discovered peer.
    /// </summary>
    public bool AutoConnectDiscoveredPeer { get; set; } = false;
    /// <summary>
    /// Gets or sets discovery port.
    /// </summary>
    public int DiscoveryPort { get; set; } = OrganicWireProtocol.DefaultDiscoveryPort;
    /// <summary>
    /// Gets or sets peer expiry seconds.
    /// </summary>
    public int PeerExpirySeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets discovery receive poll seconds.
    /// </summary>
    public int DiscoveryReceivePollSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets maximum message bytes.
    /// </summary>
    public int MaximumMessageBytes { get; set; } = OrganicWireProtocol.MaximumMessageBytes;
    /// <summary>
    /// Gets or sets minimum recurring screen reader interval seconds.
    /// </summary>
    public int MinimumRecurringScreenReaderIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Represents an organic work decision request.
/// </summary>
public sealed class OrganicWorkDecisionRequest
{
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a recurring screen reader session.
/// </summary>
public sealed class RecurringScreenReaderSession
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets selector.
    /// </summary>
    public string Selector { get; set; } = "body";
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = "Describe meaningful screen changes and suggest the next safe action.";
    /// <summary>
    /// Gets or sets interval seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 15;
    /// <summary>
    /// Gets or sets is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets last queued UTC.
    /// </summary>
    public DateTimeOffset? LastQueuedUtc { get; set; }
    /// <summary>
    /// Gets or sets last completed UTC.
    /// </summary>
    public DateTimeOffset? LastCompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets active screenshot request identifier.
    /// </summary>
    public Guid? ActiveScreenshotRequestId { get; set; }
    /// <summary>
    /// Gets or sets last result JSON.
    /// </summary>
    public string LastResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last error.
    /// </summary>
    public string LastError { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets skipped busy ticks.
    /// </summary>
    public int SkippedBusyTicks { get; set; }
    /// <summary>
    /// Gets or sets completed executions.
    /// </summary>
    public int CompletedExecutions { get; set; }
}



/// <summary>
/// Represents an organic replay policy snapshot.
/// </summary>
public sealed class OrganicReplayPolicySnapshot
{
    /// <summary>
    /// Gets or sets retention.
    /// </summary>
    public TimeSpan Retention { get; init; }
    /// <summary>
    /// Gets or sets allowed future skew.
    /// </summary>
    public TimeSpan AllowedFutureSkew { get; init; }
    /// <summary>
    /// Gets or sets cleanup interval.
    /// </summary>
    public int CleanupInterval { get; init; }
    /// <summary>
    /// Gets or sets maximum tracked messages.
    /// </summary>
    public int MaximumTrackedMessages { get; init; }
}

/// <summary>
/// Represents an organic connection runtime snapshot.
/// </summary>
public sealed class OrganicConnectionRuntimeSnapshot
{
    /// <summary>
    /// Gets or sets connection identifier.
    /// </summary>
    public Guid ConnectionId { get; init; }
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets is loopback.
    /// </summary>
    public bool IsLoopback { get; init; } = true;
    /// <summary>
    /// Gets or sets is connected.
    /// </summary>
    public bool IsConnected { get; init; }
}

/// <summary>
/// Describes the active PublisherStudio 1-Wire protocol surface without exposing private runtime material.
/// </summary>
public sealed class OrganicProtocolProfile
{
    /// <summary>Gets or sets the active protocol version.</summary>
    public string ProtocolVersion { get; set; } = OrganicWireProtocol.Version;
    /// <summary>Gets or sets the oldest compatible protocol version.</summary>
    public string MinimumCompatibleVersion { get; set; } = OrganicWireProtocol.MinimumCompatibleVersion;
    /// <summary>Gets or sets the HTTP/JSON envelope endpoint.</summary>
    public string PostEnvelopeRoute { get; set; } = "/api/organic/onewire/http-json";
    /// <summary>Gets or sets the HTTP/JSON work polling route.</summary>
    public string PollWorkRoute { get; set; } = "/api/organic/onewire/http-json/work/{correlationId}";
    /// <summary>Gets or sets the configured runtime options safe for a local UI or linked peer.</summary>
    public OrganicProtocolSettings Settings { get; set; } = new();
    /// <summary>Gets or sets public security metadata.</summary>
    public OneWireSecurityDescriptor Security { get; set; } = new();
    /// <summary>Gets or sets active capabilities.</summary>
    public List<OrganicCapabilityDescriptor> Capabilities { get; set; } = [];
    /// <summary>Gets or sets active skills.</summary>
    public List<OrganicSkillDescriptor> Skills { get; set; } = [];
    /// <summary>Gets or sets active frontend feature descriptors.</summary>
    public List<OrganicUiFeatureDescriptor> UiFeatures { get; set; } = [];
    /// <summary>Gets or sets active hardware descriptors.</summary>
    public List<OrganicHardwareDescriptor> Hardware { get; set; } = [];
    /// <summary>Gets or sets maintained controller surfaces.</summary>
    public List<ApiSurfaceDescriptor> ControllerSurfaces { get; set; } = [];
}

/// <summary>Public PublisherStudio protocol settings used to explain and validate the current connection.</summary>
public sealed class OrganicProtocolSettings
{
    public bool Enabled { get; set; }
    public bool DiscoveryEnabled { get; set; }
    public bool AutoConnectDiscoveredPeer { get; set; }
    public int DiscoveryPort { get; set; }
    public int PeerExpirySeconds { get; set; }
    public int MaximumMessageBytes { get; set; }
    public int MinimumRecurringScreenReaderIntervalSeconds { get; set; }
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
}
