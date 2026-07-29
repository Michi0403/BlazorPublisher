namespace PublisherStudio.Domain;

public sealed record PublisherRuntimePolicySnapshot
{
    public TimeSpan SpreadsheetSessionLifetime { get; init; }
    public Guid AudioClientInterfaceId { get; init; }
    public Guid AudioCaptureClientInterfaceId { get; init; }
    public TimeSpan TwitchValidationInterval { get; init; }
    public TimeSpan TwitchRefreshSafetyWindow { get; init; }
}
