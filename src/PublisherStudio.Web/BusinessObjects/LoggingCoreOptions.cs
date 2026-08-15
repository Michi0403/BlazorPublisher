using PublisherStudio.BusinessObjects.Enums;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Groups PublisherStudio optional logging provider configuration without embedding provider policy in consumers.
/// </summary>
public sealed class LoggingCoreOptions
{
    /// <summary>Gets or sets the master minimum severity for optional PublisherStudio logging providers.</summary>
    /// <value>The configured master logging level.</value>
    public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Information;

    /// <summary>Supplies file-provider policy to the logging composition service when file persistence is enabled.</summary>
    /// <value>The file logging options, or <see langword="null"/> when file logging is not configured.</value>
    public FileLoggerCoreOptions? FileCore { get; set; }
}
