using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates system variable store behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class SystemVariableStoreService : ISystemVariableStoreService
{
    /// <summary>
    /// Stores the internal sync state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Stores the in-memory values collection maintained internally by <see cref="SystemVariableStoreService"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Stores the internal storage path state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _storagePath;
    /// <summary>
    /// Stores the logger used by <see cref="SystemVariableStoreService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private ILogger<SystemVariableStoreService> logger = NullLogger<SystemVariableStoreService>.Instance;

    /// <summary>
    /// Stores the internal default port name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _defaultPortName = "Application.DefaultPort";
    /// <summary>
    /// Stores the internal port environment variable name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _portEnvironmentVariableName = "Application.PortEnvironmentVariable";
    /// <summary>
    /// Stores the internal default culture name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _defaultCultureName = "Application.DefaultCulture";
    /// <summary>
    /// Stores the internal cors policy name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _corsPolicyName = "Application.CorsPolicyName";
    /// <summary>
    /// Stores the internal data protection directory name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _dataProtectionDirectoryName = "Application.DataProtectionDirectoryName";
    /// <summary>
    /// Stores the internal data protection application name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _dataProtectionApplicationName = "Application.DataProtectionApplicationName";
    /// <summary>
    /// Stores the internal spreadsheet hibernation directory name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _spreadsheetHibernationDirectoryName = "Spreadsheet.HibernationDirectoryName";
    /// <summary>
    /// Stores the internal spreadsheet hibernation minutes name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _spreadsheetHibernationMinutesName = "Spreadsheet.HibernationMinutes";
    /// <summary>
    /// Stores the internal spreadsheet documents dispose hours name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _spreadsheetDocumentsDisposeHoursName = "Spreadsheet.DocumentsDisposeHours";
    /// <summary>
    /// Stores the internal twitch HTTP timeout seconds name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _twitchHttpTimeoutSecondsName = "Networking.TwitchTimeoutSeconds";
    /// <summary>
    /// Stores the internal runtime directory name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _runtimeDirectoryName = "Application.RuntimeDirectoryName";
    /// <summary>
    /// Stores the internal runtime endpoint file name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _runtimeEndpointFileName = "Application.RuntimeEndpointFileName";
    /// <summary>
    /// Stores the internal default document name state used by <see cref="SystemVariableStoreService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _defaultDocumentName = "Editor.DefaultDocumentName";

    /// <summary>
    /// Initializes a new <see cref="SystemVariableStoreService"/> instance and captures the dependencies or initial state required by its system variable store workflow.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    public SystemVariableStoreService(IConfiguration configuration)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio",
            "Configuration");
        _storagePath = Path.Combine(root, "system-variables.json");

        _values[_defaultPortName] = "58071";
        _values[_portEnvironmentVariableName] = "PUBLISHERSTUDIO_PORT";
        _values[_defaultCultureName] = "en-US";
        _values[_corsPolicyName] = "PublisherExport";
        _values[_dataProtectionDirectoryName] = "DataProtection";
        _values[_dataProtectionApplicationName] = "PublisherStudio";
        _values[_spreadsheetHibernationDirectoryName] = "SpreadsheetHibernation";
        _values[_spreadsheetHibernationMinutesName] = "20";
        _values[_spreadsheetDocumentsDisposeHoursName] = "4";
        _values[_twitchHttpTimeoutSecondsName] = "20";
        _values[_runtimeDirectoryName] = "runtime";
        _values[_runtimeEndpointFileName] = "server.json";
        _values[_defaultDocumentName] = "Untitled Publication";

        LoadConfiguration(configuration);
        LoadPersisted();
    }

    /// <summary>
    /// Gets the default port value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default port value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public int DefaultPort => GetInt(_defaultPortName, 58071);
    /// <summary>
    /// Gets the port environment variable name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The port environment variable name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string PortEnvironmentVariableName => GetString(_portEnvironmentVariableName, "PUBLISHERSTUDIO_PORT");
    /// <summary>
    /// Gets the default culture value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default culture value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string DefaultCulture => GetString(_defaultCultureName, "en-US");
    /// <summary>
    /// Gets the cors policy name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cors policy name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string CorsPolicyName => GetString(_corsPolicyName, "PublisherExport");
    /// <summary>
    /// Gets the data protection directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The data protection directory name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string DataProtectionDirectoryName => GetString(_dataProtectionDirectoryName, "DataProtection");
    /// <summary>
    /// Gets the data protection application name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data protection application name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string DataProtectionApplicationName => GetString(_dataProtectionApplicationName, "PublisherStudio");
    /// <summary>
    /// Gets the spreadsheet hibernation directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The spreadsheet hibernation directory name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string SpreadsheetHibernationDirectoryName => GetString(_spreadsheetHibernationDirectoryName, "SpreadsheetHibernation");
    /// <summary>
    /// Gets the spreadsheet hibernation timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The spreadsheet hibernation timeout value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public TimeSpan SpreadsheetHibernationTimeout => GetTimeSpan(_spreadsheetHibernationMinutesName, TimeSpan.FromMinutes(20));
    /// <summary>
    /// Gets the spreadsheet documents dispose timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The spreadsheet documents dispose timeout value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public TimeSpan SpreadsheetDocumentsDisposeTimeout => GetTimeSpan(_spreadsheetDocumentsDisposeHoursName, TimeSpan.FromHours(4));
    /// <summary>
    /// Gets the twitch HTTP timeout duration used to control timing in the system variable store workflow.
    /// </summary>
    /// <value>The twitch HTTP timeout value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public TimeSpan TwitchHttpTimeout => TimeSpan.FromSeconds(GetInt(_twitchHttpTimeoutSecondsName, 20));
    /// <summary>
    /// Gets the runtime directory name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The runtime directory name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string RuntimeDirectoryName => GetString(_runtimeDirectoryName, "runtime");
    /// <summary>
    /// Gets the runtime endpoint file name used by this system variable store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The runtime endpoint file name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string RuntimeEndpointFileName => GetString(_runtimeEndpointFileName, "server.json");
    /// <summary>
    /// Gets the default document name value that forms part of the system variable store state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default document name value exposed by <see cref="SystemVariableStoreService"/>.</value>
    public string DefaultDocumentName => GetString(_defaultDocumentName, "Untitled Publication");

    /// <summary>
    /// Performs attach logger as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public void AttachLogger(ILogger<SystemVariableStoreService> logger)
    {
        try
        {
            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogInformation($"PublisherStudio system-variable store attached logging with {_values.Count} collected values.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio system-variable logger attachment failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves string as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetString(string name, string fallback)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            lock (_sync)
            {
                var value = _values.TryGetValue(name, out var configured) && !string.IsNullOrWhiteSpace(configured)
                    ? configured
                    : fallback;
                logger.LogDebug($"Resolved PublisherStudio system variable {name}; value omitted from logs.");
                return value;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio system variable {name} could not be resolved: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves int as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int GetInt(string name, int fallback)
    {
        try
        {
            var raw = GetString(name, fallback.ToString(CultureInfo.InvariantCulture));
            var value = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
            logger.LogDebug($"Resolved integer PublisherStudio system variable {name}; value omitted from logs.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Integer PublisherStudio system variable {name} could not be resolved: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves time span as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the system variable store operation and used when producing its result.</param>
    /// <returns>The time span produced by the operation.</returns>
    public TimeSpan GetTimeSpan(string name, TimeSpan fallback)
    {
        try
        {
            var raw = GetString(name, fallback.TotalMinutes.ToString(CultureInfo.InvariantCulture));
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return fallback;
            var value = name.Equals(_spreadsheetDocumentsDisposeHoursName, StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromHours(numeric)
                : TimeSpan.FromMinutes(numeric);
            logger.LogDebug($"Resolved duration PublisherStudio system variable {name}; value omitted from logs.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Duration PublisherStudio system variable {name} could not be resolved: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs set as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="SystemVariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the system variable store operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the system variable store operation and used when producing its result.</param>
    public void Set<T>(string name, T value)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var serialized = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            lock (_sync)
            {
                _values[name] = serialized;
                Persist();
            }
            logger.LogInformation($"Stored PublisherStudio system variable {name}; value omitted from logs.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio system variable {name} could not be stored: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs snapshot as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    public IReadOnlyDictionary<string, string> Snapshot()
    {
        try
        {
            lock (_sync)
            {
                logger.LogDebug($"Created PublisherStudio system-variable snapshot with {_values.Count} values.");
                return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio system-variable snapshot failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads configuration as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    private void LoadConfiguration(IConfiguration configuration)
    {
        try
        {
            foreach (var child in configuration.GetSection("PublisherStudio:SystemVariables").GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                    _values[child.Key] = child.Value;
            }
            logger.LogDebug($"Loaded PublisherStudio system variables from application configuration.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio configured system variables could not be loaded: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads persisted as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void LoadPersisted()
    {
        try
        {
            if (!File.Exists(_storagePath))
                return;
            var persisted = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_storagePath));
            if (persisted is null)
                return;
            foreach (var item in persisted)
                _values[item.Key] = item.Value;
            logger.LogDebug($"Loaded {persisted.Count} persisted PublisherStudio system variables.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Persisted PublisherStudio system variables could not be loaded: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs persist as part of the system variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void Persist()
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath) ?? throw new InvalidOperationException("System-variable storage directory is unavailable.");
            Directory.CreateDirectory(directory);
            var temporaryPath = _storagePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _storagePath, true);
            logger.LogDebug($"Persisted {_values.Count} PublisherStudio system variables.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio system variables could not be persisted: {exception.Message}");
            throw;
        }
    }
}
