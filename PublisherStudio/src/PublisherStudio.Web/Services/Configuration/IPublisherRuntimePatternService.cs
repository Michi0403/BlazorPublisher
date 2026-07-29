using System.Text.RegularExpressions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public interface IPublisherRuntimePatternService
{
    Regex GetRegex(PublisherRuntimePattern pattern);
}
