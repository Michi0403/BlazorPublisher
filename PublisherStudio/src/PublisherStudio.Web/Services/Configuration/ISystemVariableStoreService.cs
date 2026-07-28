namespace PublisherStudio.Services.Configuration;

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
    string GetString(string name, string fallback);
    int GetInt(string name, int fallback);
    TimeSpan GetTimeSpan(string name, TimeSpan fallback);
    void Set<T>(string name, T value);
    IReadOnlyDictionary<string, string> Snapshot();
    void AttachLogger(ILogger<SystemVariableStoreService> logger);
}
