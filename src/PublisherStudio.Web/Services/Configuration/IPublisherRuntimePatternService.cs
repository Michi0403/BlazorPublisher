using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the contract for publisher runtime pattern behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublisherRuntimePatternService
{
    /// <summary>
    /// Retrieves regex as part of the publisher runtime pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the publisher runtime pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Regex GetRegex(PublisherRuntimePattern pattern);
}
