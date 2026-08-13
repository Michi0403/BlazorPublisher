namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the contract for system variable store behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ISystemVariableStoreService
{
    /// <summary>
    /// Gets the default port value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default port value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    int DefaultPort { get; }
    /// <summary>
    /// Gets the port environment variable name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port environment variable name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string PortEnvironmentVariableName { get; }
    /// <summary>
    /// Gets the default culture value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default culture value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string DefaultCulture { get; }
    /// <summary>
    /// Gets the cors policy name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cors policy name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string CorsPolicyName { get; }
    /// <summary>
    /// Gets the data protection directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The data protection directory name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string DataProtectionDirectoryName { get; }
    /// <summary>
    /// Gets the data protection application name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data protection application name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string DataProtectionApplicationName { get; }
    /// <summary>
    /// Gets the spreadsheet hibernation directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The spreadsheet hibernation directory name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string SpreadsheetHibernationDirectoryName { get; }
    /// <summary>
    /// Gets the spreadsheet hibernation timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The spreadsheet hibernation timeout value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    TimeSpan SpreadsheetHibernationTimeout { get; }
    /// <summary>
    /// Gets the spreadsheet documents dispose timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The spreadsheet documents dispose timeout value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    TimeSpan SpreadsheetDocumentsDisposeTimeout { get; }
    /// <summary>
    /// Gets the twitch HTTP timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The twitch HTTP timeout value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    TimeSpan TwitchHttpTimeout { get; }
    /// <summary>
    /// Gets the runtime directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The runtime directory name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string RuntimeDirectoryName { get; }
    /// <summary>
    /// Gets the runtime endpoint file name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The runtime endpoint file name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string RuntimeEndpointFileName { get; }
    /// <summary>
    /// Gets the default document name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default document name value exposed by <see cref="ISystemVariableStoreService"/>.</value>
    string DefaultDocumentName { get; }
    /// <summary>
    /// Retrieves string as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetString(string name, string fallback);
    /// <summary>
    /// Retrieves int as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int GetInt(string name, int fallback);
    /// <summary>
    /// Retrieves time span as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The time span produced by the operation.</returns>
    TimeSpan GetTimeSpan(string name, TimeSpan fallback);
    /// <summary>
    /// Performs set as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ISystemVariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the system variable store operation and used when producing its result.</param>
    void Set<T>(string name, T value);
    /// <summary>
    /// Performs snapshot as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    IReadOnlyDictionary<string, string> Snapshot();
    /// <summary>
    /// Performs attach logger as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    void AttachLogger(ILogger<SystemVariableStoreService> logger);
}
