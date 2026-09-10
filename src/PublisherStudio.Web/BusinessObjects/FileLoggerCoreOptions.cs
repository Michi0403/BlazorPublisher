using PublisherStudio.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Carries PublisherStudio file logger settings used by the optional file logging provider.
/// </summary>
public sealed class FileLoggerCoreOptions
{
    /// <summary>Allows callers to override the durable per-user log destination when a specific file is required.</summary>
    /// <value>An explicit log file path, or an empty value to use the durable per-user PublisherStudio root.</value>
    [JsonInclude]
    public string? FilePath { get; set; }

    /// <summary>Gets or sets the minimum severity accepted by the file logging provider.</summary>
    /// <value>The minimum configured file log level.</value>
    [JsonInclude]
    public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Information;
}
