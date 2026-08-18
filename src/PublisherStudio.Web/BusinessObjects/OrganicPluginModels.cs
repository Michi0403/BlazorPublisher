namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents an organic plugin work item application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicPluginWorkItem
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this organic plugin work item instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable message identifier used to identify or correlate this organic plugin work item instance with related application state.
    /// </summary>
    /// <value>The message identifier value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this organic plugin work item instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this organic plugin work item instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this organic plugin work item instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the organic plugin work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public OrganicWorkStatus Status { get; set; } = OrganicWorkStatus.Queued;
    /// <summary>
    /// Gets or sets the created UTC associated with this organic plugin work item state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the updated UTC associated with this organic plugin work item state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the request value that forms part of the organic plugin work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public OrganicWireEnvelope Request { get; set; } = new();
    /// <summary>
    /// Gets or sets the result JSON value that forms part of the organic plugin work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The result JSON value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public string ResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the organic plugin work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="OrganicPluginWorkItem"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents organic connection state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OrganicConnectionState
{
    /// <summary>True while the TCP transport is alive.</summary>
    /// <value>The is connected value exposed by <see cref="OrganicConnectionState"/>.</value>
    public bool IsConnected { get; set; }
    /// <summary>True only after both frontends participated: PublisherStudio initiated the link and LocalGPT approved it.</summary>
    /// <value>The is linked value exposed by <see cref="OrganicConnectionState"/>.</value>
    public bool IsLinked { get; set; }
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this organic connection instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OrganicConnectionState"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the organic connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OrganicConnectionState"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the connected UTC associated with this organic connection state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The connected UTC value exposed by <see cref="OrganicConnectionState"/>.</value>
    public DateTimeOffset? ConnectedUtc { get; set; }
    /// <summary>
    /// Gets or sets the last error value that forms part of the organic connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="OrganicConnectionState"/>.</value>
    public string LastError { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote capabilities collection maintained or exposed by this organic connection instance for downstream processing.
    /// </summary>
    /// <value>The remote capabilities value exposed by <see cref="OrganicConnectionState"/>.</value>
    public List<OrganicCapabilityDescriptor> RemoteCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the remote skills collection maintained or exposed by this organic connection instance for downstream processing.
    /// </summary>
    /// <value>The remote skills value exposed by <see cref="OrganicConnectionState"/>.</value>
    public List<OrganicSkillDescriptor> RemoteSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets the remote UI features collection maintained or exposed by this organic connection instance for downstream processing.
    /// </summary>
    /// <value>The remote UI features value exposed by <see cref="OrganicConnectionState"/>.</value>
    public List<OrganicUiFeatureDescriptor> RemoteUiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets the remote hardware collection maintained or exposed by this organic connection instance for downstream processing.
    /// </summary>
    /// <value>The remote hardware value exposed by <see cref="OrganicConnectionState"/>.</value>
    public List<OrganicHardwareDescriptor> RemoteHardware { get; set; } = [];

    /// <summary>
    /// Determines whether capability for <see cref="OrganicConnectionState"/>, keeping the operation consistent with the state and invariants of the surrounding organic connection workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the organic connection operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool HasCapability(string key) => IsConnected && IsLinked && RemoteCapabilities.Any(item =>
        item.IsEnabled && item.IsOnline && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets UI feature state.
    /// </summary>
    /// <param name="key">Key value supplied to the organic connection operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic connection operation and used when producing its result.</param>
    /// <returns>The organic UI feature state produced by the operation.</returns>
    public OrganicUiFeatureState GetUiFeatureState(string key, OrganicUiFeatureState fallback = OrganicUiFeatureState.Hidden)
    {
        if (!IsConnected || !IsLinked) return OrganicUiFeatureState.Hidden;
        return RemoteUiFeatures.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.State ?? fallback;
    }
}

/// <summary>
/// Represents an organic text insertion proposal application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicTextInsertionProposal
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this organic text insertion proposal instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the target value that forms part of the organic text insertion proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public string Target { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the text value that forms part of the organic text insertion proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reason value that forms part of the organic text insertion proposal state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this organic text insertion proposal state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether accepted applies to the organic text insertion proposal state.
    /// </summary>
    /// <value>The accepted value exposed by <see cref="OrganicTextInsertionProposal"/>.</value>
    public bool Accepted { get; set; }
}

/// <summary>
/// Carries the configurable organic plugin settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class OrganicPluginOptions
{
    /// <summary>
    /// Defines the section name constant used by <see cref="OrganicPluginOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "OrganicPlugins";
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the organic plugin state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether discovery applies to the organic plugin state.
    /// </summary>
    /// <value>The enable discovery value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public bool EnableDiscovery { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether LocalGPT discovery sockets may be activated only by an explicit PublisherStudio frontend connection workflow.
    /// </summary>
    /// <value><see langword="true"/> keeps discovery network activity idle until the frontend requests it; <see langword="false"/> permits legacy always-on discovery.</value>
    public bool RequireFrontendDiscoveryActivation { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether discovery listening is suspended while a LocalGPT transport is already connected.
    /// </summary>
    /// <value><see langword="true"/> stops unnecessary discovery socket activity for an already connected LocalGPT peer.</value>
    public bool SuspendDiscoveryWhileConnected { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether auto connect discovered peer applies to the organic plugin state.
    /// </summary>
    /// <value>The auto connect discovered peer value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public bool AutoConnectDiscoveredPeer { get; set; } = false;
    /// <summary>
    /// Gets or sets the discovery port value that forms part of the organic plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery port value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public int DiscoveryPort { get; set; } = OrganicWireProtocol.DefaultDiscoveryPort;
    /// <summary>
    /// Gets or sets the peer expiry seconds value that forms part of the organic plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The peer expiry seconds value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public int PeerExpirySeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets the discovery receive poll seconds value that forms part of the organic plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery receive poll seconds value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public int DiscoveryReceivePollSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets the maximum message bytes value that forms part of the organic plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum message bytes value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public int MaximumMessageBytes { get; set; } = OrganicWireProtocol.MaximumMessageBytes;
    /// <summary>
    /// Gets or sets the minimum recurring screen reader interval seconds value that forms part of the organic plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum recurring screen reader interval seconds value exposed by <see cref="OrganicPluginOptions"/>.</value>
    public int MinimumRecurringScreenReaderIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Represents the input contract for organic work decision, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OrganicWorkDecisionRequest
{
    /// <summary>
    /// Gets or sets the reason value that forms part of the organic work decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="OrganicWorkDecisionRequest"/>.</value>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a recurring screen reader session application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RecurringScreenReaderSession
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this recurring screen reader session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this recurring screen reader session instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this recurring screen reader session instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the selector value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selector value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public string Selector { get; set; } = "body";
    /// <summary>
    /// Gets or sets the prompt value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public string Prompt { get; set; } = "Describe meaningful screen changes and suggest the next safe action.";
    /// <summary>
    /// Gets or sets the interval seconds value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interval seconds value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public int IntervalSeconds { get; set; } = 15;
    /// <summary>
    /// Gets or sets a value indicating whether active applies to the recurring screen reader session state.
    /// </summary>
    /// <value>The is active value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the created UTC associated with this recurring screen reader session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the last queued UTC associated with this recurring screen reader session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last queued UTC value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public DateTimeOffset? LastQueuedUtc { get; set; }
    /// <summary>
    /// Gets or sets the last completed UTC associated with this recurring screen reader session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last completed UTC value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public DateTimeOffset? LastCompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets the stable active screenshot request identifier used to identify or correlate this recurring screen reader session instance with related application state.
    /// </summary>
    /// <value>The active screenshot request identifier value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public Guid? ActiveScreenshotRequestId { get; set; }
    /// <summary>
    /// Gets or sets the last result JSON value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last result JSON value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public string LastResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last error value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public string LastError { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the skipped busy ticks value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The skipped busy ticks value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public int SkippedBusyTicks { get; set; }
    /// <summary>
    /// Gets or sets the completed executions value that forms part of the recurring screen reader session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The completed executions value exposed by <see cref="RecurringScreenReaderSession"/>.</value>
    public int CompletedExecutions { get; set; }
}



/// <summary>
/// Represents an organic replay policy snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicReplayPolicySnapshot
{
    /// <summary>
    /// Gets or sets the retention duration used to control timing in the organic replay policy snapshot workflow.
    /// </summary>
    /// <value>The retention value exposed by <see cref="OrganicReplayPolicySnapshot"/>.</value>
    public TimeSpan Retention { get; init; }
    /// <summary>
    /// Gets or sets the allowed future skew duration used to control timing in the organic replay policy snapshot workflow.
    /// </summary>
    /// <value>The allowed future skew value exposed by <see cref="OrganicReplayPolicySnapshot"/>.</value>
    public TimeSpan AllowedFutureSkew { get; init; }
    /// <summary>
    /// Gets or sets the cleanup interval duration used to control timing in the organic replay policy snapshot workflow.
    /// </summary>
    /// <value>The cleanup interval value exposed by <see cref="OrganicReplayPolicySnapshot"/>.</value>
    public int CleanupInterval { get; init; }
    /// <summary>
    /// Gets or sets the maximum tracked messages value that forms part of the organic replay policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum tracked messages value exposed by <see cref="OrganicReplayPolicySnapshot"/>.</value>
    public int MaximumTrackedMessages { get; init; }
}

/// <summary>
/// Represents an organic connection runtime snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicConnectionRuntimeSnapshot
{
    /// <summary>
    /// Gets or sets the stable connection identifier used to identify or correlate this organic connection runtime snapshot instance with related application state.
    /// </summary>
    /// <value>The connection identifier value exposed by <see cref="OrganicConnectionRuntimeSnapshot"/>.</value>
    public Guid ConnectionId { get; init; }
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this organic connection runtime snapshot instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OrganicConnectionRuntimeSnapshot"/>.</value>
    public string PeerId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether loopback applies to the organic connection runtime snapshot state.
    /// </summary>
    /// <value>The is loopback value exposed by <see cref="OrganicConnectionRuntimeSnapshot"/>.</value>
    public bool IsLoopback { get; init; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether connected applies to the organic connection runtime snapshot state.
    /// </summary>
    /// <value>The is connected value exposed by <see cref="OrganicConnectionRuntimeSnapshot"/>.</value>
    public bool IsConnected { get; init; }
}

/// <summary>
/// Describes the active PublisherStudio 1-Wire protocol surface without exposing private runtime material.
/// </summary>
public sealed class OrganicProtocolProfile
{
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the organic protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public string ProtocolVersion { get; set; } = OrganicWireProtocol.Version;
    /// <summary>
    /// Gets or sets the minimum compatible version value that forms part of the organic protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum compatible version value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public string MinimumCompatibleVersion { get; set; } = OrganicWireProtocol.MinimumCompatibleVersion;
    /// <summary>
    /// Gets or sets the post envelope route value that forms part of the organic protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The post envelope route value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public string PostEnvelopeRoute { get; set; } = "/api/organic/onewire/http-json";
    /// <summary>
    /// Gets or sets the poll work route value that forms part of the organic protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The poll work route value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public string PollWorkRoute { get; set; } = "/api/organic/onewire/http-json/work/{correlationId}";
    /// <summary>Gets or sets the configured runtime options safe for a local UI or linked peer.</summary>
    /// <value>The settings value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public OrganicProtocolSettings Settings { get; set; } = new();
    /// <summary>
    /// Gets or sets the security value that forms part of the organic protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The security value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public OneWireSecurityDescriptor Security { get; set; } = new();
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this organic protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public List<OrganicCapabilityDescriptor> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this organic protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public List<OrganicSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI features collection maintained or exposed by this organic protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The UI features value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public List<OrganicUiFeatureDescriptor> UiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets the hardware collection maintained or exposed by this organic protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The hardware value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public List<OrganicHardwareDescriptor> Hardware { get; set; } = [];
    /// <summary>
    /// Gets or sets the controller surfaces collection maintained or exposed by this organic protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The controller surfaces value exposed by <see cref="OrganicProtocolProfile"/>.</value>
    public List<ApiSurfaceDescriptor> ControllerSurfaces { get; set; } = [];
}

/// <summary>Public PublisherStudio protocol settings used to explain and validate the current connection.</summary>
public sealed class OrganicProtocolSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the organic protocol state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether discovery enabled applies to the organic protocol state.
    /// </summary>
    /// <value>The discovery enabled value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public bool DiscoveryEnabled { get; set; }
    /// <summary>Gets or sets whether an already approved discovered peer may be connected automatically.</summary>
    /// <value>The auto connect discovered peer value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public bool AutoConnectDiscoveredPeer { get; set; }
    /// <summary>Gets or sets the UDP discovery port exposed by the public runtime profile.</summary>
    /// <value>The discovery port value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public int DiscoveryPort { get; set; }
    /// <summary>Gets or sets how long a discovered peer may remain idle before it expires.</summary>
    /// <value>The peer expiry seconds value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public int PeerExpirySeconds { get; set; }
    /// <summary>Gets or sets the maximum accepted organic transport message size in bytes.</summary>
    /// <value>The maximum message bytes value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public int MaximumMessageBytes { get; set; }
    /// <summary>Gets or sets the minimum allowed interval for recurring screen-reader requests.</summary>
    /// <value>The minimum recurring screen reader interval seconds value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public int MinimumRecurringScreenReaderIntervalSeconds { get; set; }
    /// <summary>Gets or sets the transport names advertised by the public runtime profile.</summary>
    /// <value>The supported transports value exposed by <see cref="OrganicProtocolSettings"/>.</value>
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
}
