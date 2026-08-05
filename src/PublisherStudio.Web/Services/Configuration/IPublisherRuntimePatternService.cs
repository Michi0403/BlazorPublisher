using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the publisher runtime pattern service contract.
/// </summary>
public interface IPublisherRuntimePatternService
{
    Regex GetRegex(PublisherRuntimePattern pattern);
}
