namespace PublisherStudio.Domain;

public sealed class OrganicPluginWorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid CorrelationId { get; set; }
    public string PeerId { get; set; } = string.Empty;
    public string CapabilityKey { get; set; } = string.Empty;
    public OrganicWorkStatus Status { get; set; } = OrganicWorkStatus.Queued;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public OrganicWireEnvelope Request { get; set; } = new();
    public string ResultJson { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class OrganicConnectionState
{
    /// <summary>True while the TCP transport is alive.</summary>
    public bool IsConnected { get; set; }
    /// <summary>True only after both frontends participated: PublisherStudio initiated the link and LocalGPT approved it.</summary>
    public bool IsLinked { get; set; }
    public string PeerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset? ConnectedUtc { get; set; }
    public string LastError { get; set; } = string.Empty;
    public List<OrganicCapabilityDescriptor> RemoteCapabilities { get; set; } = [];
    public List<OrganicSkillDescriptor> RemoteSkills { get; set; } = [];
    public List<OrganicUiFeatureDescriptor> RemoteUiFeatures { get; set; } = [];
    public List<OrganicHardwareDescriptor> RemoteHardware { get; set; } = [];

    public bool HasCapability(string key) => IsConnected && IsLinked && RemoteCapabilities.Any(item =>
        item.IsEnabled && item.IsOnline && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    public OrganicUiFeatureState GetUiFeatureState(string key, OrganicUiFeatureState fallback = OrganicUiFeatureState.Hidden)
    {
        if (!IsConnected || !IsLinked) return OrganicUiFeatureState.Hidden;
        return RemoteUiFeatures.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.State ?? fallback;
    }
}

public sealed class OrganicTextInsertionProposal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Target { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool Accepted { get; set; }
}

public sealed class OrganicPluginOptions
{
    public const string SectionName = "OrganicPlugins";
    public bool Enabled { get; set; } = true;
    public bool EnableDiscovery { get; set; } = true;
    public int DiscoveryPort { get; set; } = OrganicWireProtocol.DefaultDiscoveryPort;
    public int PeerExpirySeconds { get; set; } = 30;
    public int DiscoveryReceivePollSeconds { get; set; } = 5;
    public int MaximumMessageBytes { get; set; } = OrganicWireProtocol.MaximumMessageBytes;
    public int MinimumRecurringScreenReaderIntervalSeconds { get; set; } = 15;
}

public sealed class OrganicWorkDecisionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class RecurringScreenReaderSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PeerId { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public string Selector { get; set; } = "body";
    public string Prompt { get; set; } = "Describe meaningful screen changes and suggest the next safe action.";
    public int IntervalSeconds { get; set; } = 15;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastQueuedUtc { get; set; }
    public DateTimeOffset? LastCompletedUtc { get; set; }
    public Guid? ActiveScreenshotRequestId { get; set; }
    public string LastResultJson { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;
    public int SkippedBusyTicks { get; set; }
    public int CompletedExecutions { get; set; }
}
