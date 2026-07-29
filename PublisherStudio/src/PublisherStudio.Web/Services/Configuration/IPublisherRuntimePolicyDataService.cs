using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public interface IPublisherRuntimePolicyDataService
{
    TimeSpan SpreadsheetSessionLifetime { get; }
    Guid AudioClientInterfaceId { get; }
    Guid AudioCaptureClientInterfaceId { get; }
    TimeSpan TwitchValidationInterval { get; }
    TimeSpan TwitchRefreshSafetyWindow { get; }
    PublisherRuntimePolicySnapshot GetSnapshot();
}
