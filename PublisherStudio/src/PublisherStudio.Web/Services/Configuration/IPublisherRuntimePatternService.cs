using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

public interface IPublisherRuntimePatternService
{
    Regex GetRegex(PublisherRuntimePattern pattern);
}
