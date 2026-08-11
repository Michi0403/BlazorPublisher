namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the system variable store service contract.
/// </summary>
public interface ISystemVariableStoreService
{
    int DefaultPort { get; }
    string PortEnvironmentVariableName { get; }
    string DefaultCulture { get; }
    string CorsPolicyName { get; }
    string DataProtectionDirectoryName { get; }
    string DataProtectionApplicationName { get; }
    string SpreadsheetHibernationDirectoryName { get; }
    TimeSpan SpreadsheetHibernationTimeout { get; }
    TimeSpan SpreadsheetDocumentsDisposeTimeout { get; }
    TimeSpan TwitchHttpTimeout { get; }
    string RuntimeDirectoryName { get; }
    string RuntimeEndpointFileName { get; }
    string DefaultDocumentName { get; }
    /// <summary>
    /// Gets string.
    /// </summary>
    string GetString(string name, string fallback);
    /// <summary>
    /// Gets int.
    /// </summary>
    int GetInt(string name, int fallback);
    /// <summary>
    /// Gets time span.
    /// </summary>
    TimeSpan GetTimeSpan(string name, TimeSpan fallback);
    /// <summary>
    /// Runs the set operation.
    /// </summary>
    void Set<T>(string name, T value);
    /// <summary>
    /// Runs the snapshot operation.
    /// </summary>
    IReadOnlyDictionary<string, string> Snapshot();
    /// <summary>
    /// Runs the attach logger operation.
    /// </summary>
    void AttachLogger(ILogger<SystemVariableStoreService> logger);
}
