using System.Text.Json;

namespace PublisherStudio.Domain;

public static class OrganicWireProtocol
{
    public const string Version = "1.0";
    public const int DefaultLocalGptServicePort = 51140;
    public const int DefaultDiscoveryPort = 51141;
    public const int MaximumMessageBytes = 1024 * 1024;
}

public enum OrganicWireMessageType
{
    Hello, HelloAck, CapabilityRequest, CapabilityResponse, Invoke, CouncilRequest,
    WorkAccepted, WorkStatusRequest, WorkResult, ApprovalRequired, PermissionUpdate,
    Error, Ping, Pong
}

public enum OrganicExecutionMode { Once, SequentialSpool, Scheduled }
public enum OrganicWorkStatus { PendingApproval, Queued, Running, Completed, Failed, Declined, Cancelled }
public enum OrganicApprovalMode { AskEveryTime, SameCapability, CurrentWorkOrder, AlwaysAllow, Deny }

public sealed class OrganicWireEnvelope
{
    public string ProtocolVersion { get; set; } = OrganicWireProtocol.Version;
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid? ReplyToMessageId { get; set; }
    public OrganicWireMessageType MessageType { get; set; }
    public string SourcePeerId { get; set; } = string.Empty;
    public string TargetPeerId { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresUtc { get; set; }
    public int Sequence { get; set; }
    public OrganicExecutionMode ExecutionMode { get; set; } = OrganicExecutionMode.Once;
    public string Controller { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string CapabilityKey { get; set; } = string.Empty;
    public List<string> Organs { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public Dictionary<string, JsonElement>? Properties { get; set; }
    public string? EncryptedPayload { get; set; }
    public string? Signature { get; set; }
    public string? Hash { get; set; }
    public string? ErrorCheck { get; set; }
    public bool UserConfirmed { get; set; }
    public OrganicApprovalMode ApprovalMode { get; set; } = OrganicApprovalMode.AskEveryTime;
    public string WorkOrderKey { get; set; } = string.Empty;
    public DateTimeOffset? NotBeforeUtc { get; set; }
    public string WorkflowJson { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class OrganicCapabilityDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    public List<string> Organs { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public bool IsOnline { get; set; } = true;
    public bool IsReadOnly { get; set; }
    public bool RequiresHumanConfirmation { get; set; } = true;
    public bool SupportsScheduling { get; set; }
    public string Source { get; set; } = "PublisherStudio";
}

public sealed class OrganicPeerAdvertisement
{
    public string PeerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int ServicePort { get; set; }
    public int DiscoveryPort { get; set; }
    public string WebBaseUrl { get; set; } = string.Empty;
    public DateTimeOffset SeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsConnected { get; set; }
    public List<OrganicCapabilityDescriptor> Capabilities { get; set; } = [];
}

public sealed class OrganicPermissionRule
{
    public string PeerId { get; set; } = "localgpt";
    public string CapabilityKey { get; set; } = string.Empty;
    public string Organ { get; set; } = string.Empty;
    public OrganicApprovalMode ApprovalMode { get; set; } = OrganicApprovalMode.AskEveryTime;
    public string WorkOrderKey { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string UpdatedBy { get; set; } = "CurrentUser";
}

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
    public bool IsConnected { get; set; }
    public string PeerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset? ConnectedUtc { get; set; }
    public string LastError { get; set; } = string.Empty;
}

public sealed class OrganicCouncilPromptRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string TeamKey { get; set; } = "general";
    public string LeaderModelName { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = [];
    public List<string> RequestedOrganicCapabilities { get; set; } = [];
    public string ExternalProjectContextJson { get; set; } = "{}";
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTopicId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public int MaxRounds { get; set; } = 1;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MaxContextTokens { get; set; } = 32768;
    public int MaxParallelModels { get; set; } = 1;
    public bool IncludeMemory { get; set; } = true;
    public bool SaveToMemory { get; set; } = true;
    public bool GenerateImplementationArtifact { get; set; }
    public bool UserConfirmedArtifactBuild { get; set; }
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
}

public sealed class OrganicWorkDecisionRequest
{
    public string Reason { get; set; } = string.Empty;
}
