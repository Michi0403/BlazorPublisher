using PublisherStudio.BusinessObjects.Enums;

namespace PublisherStudio.BusinessObjects;

/// <summary>Groups optional PublisherStudio logging-provider configuration.</summary>
public sealed class LoggingCoreOptions
{
    /// <summary>Master switch/minimum severity for optional application logging.</summary>
    public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Information;

    /// <summary>File logging settings.</summary>
    public FileLoggerCoreOptions FileCore { get; set; } = new();
}
