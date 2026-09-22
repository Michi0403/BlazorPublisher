using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>Defines PublisherStudio's authoritative file-format capability projection for local and 1-Wire callers.</summary>
public interface IPublisherFileFormatCapabilityService
{
    /// <summary>Returns the file-format families PublisherStudio can currently process and their runtime availability.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The current file-format capability snapshot.</returns>
    Task<PublisherFileFormatCapabilitySnapshot> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}
