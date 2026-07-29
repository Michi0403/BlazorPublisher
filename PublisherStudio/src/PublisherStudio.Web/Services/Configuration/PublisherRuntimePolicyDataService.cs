using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public sealed class PublisherRuntimePolicyDataService(
    ILogger<PublisherRuntimePolicyDataService> logger) : IPublisherRuntimePolicyDataService
{
    public TimeSpan SpreadsheetSessionLifetime { get; } = TimeSpan.FromHours(4);
    public Guid AudioClientInterfaceId { get; } = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    public Guid AudioCaptureClientInterfaceId { get; } = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
    public TimeSpan TwitchValidationInterval { get; } = TimeSpan.FromMinutes(55);
    public TimeSpan TwitchRefreshSafetyWindow { get; } = TimeSpan.FromMinutes(5);

    public PublisherRuntimePolicySnapshot GetSnapshot()
    {
        try
        {
            var snapshot = new PublisherRuntimePolicySnapshot
            {
                SpreadsheetSessionLifetime = SpreadsheetSessionLifetime,
                AudioClientInterfaceId = AudioClientInterfaceId,
                AudioCaptureClientInterfaceId = AudioCaptureClientInterfaceId,
                TwitchValidationInterval = TwitchValidationInterval,
                TwitchRefreshSafetyWindow = TwitchRefreshSafetyWindow
            };
            logger.LogTrace($"Returned the PublisherStudio runtime policy snapshot.");
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the PublisherStudio runtime policy snapshot.");
            throw;
        }
    }
}
