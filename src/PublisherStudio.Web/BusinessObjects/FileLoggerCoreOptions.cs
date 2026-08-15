using PublisherStudio.BusinessObjects.Enums;
using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>Configures PublisherStudio file logging.</summary>
public sealed class FileLoggerCoreOptions
{
    /// <summary>Optional explicit log path. Blank uses the PublisherStudio local-application-data log file.</summary>
    [JsonInclude]
    public string? FilePath { get; set; }

    /// <summary>Minimum severity written to the file.</summary>
    [JsonInclude]
    public CoreLogLevel CoreLogLevel { get; set; } = CoreLogLevel.Information;

    /// <summary>Maximum number of pending lines retained when producers temporarily outpace disk IO.</summary>
    [JsonInclude]
    public int MaxQueueLength { get; set; } = 8192;

    /// <summary>Returns the configured path or PublisherStudio's stable per-user default.</summary>
    public string ResolvePath()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(FilePath))
                return Path.GetFullPath(Environment.ExpandEnvironmentVariables(FilePath));

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local))
                local = AppContext.BaseDirectory;
            return Path.Combine(local, "PublisherStudio", "PublisherStudio.log");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file-log path resolution failed: {exception}");
            throw;
        }
    }
}
