using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides system variable store service operations.
/// </summary>
public sealed class SystemVariableStoreService : ISystemVariableStoreService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _storagePath;
    private ILogger<SystemVariableStoreService> logger = NullLogger<SystemVariableStoreService>.Instance;

    private readonly string _defaultPortName = "Application.DefaultPort";
    private readonly string _portEnvironmentVariableName = "Application.PortEnvironmentVariable";
    private readonly string _defaultCultureName = "Application.DefaultCulture";
    private readonly string _corsPolicyName = "Application.CorsPolicyName";
    private readonly string _dataProtectionDirectoryName = "Application.DataProtectionDirectoryName";
    private readonly string _dataProtectionApplicationName = "Application.DataProtectionApplicationName";
    private readonly string _spreadsheetHibernationDirectoryName = "Spreadsheet.HibernationDirectoryName";
    private readonly string _spreadsheetHibernationMinutesName = "Spreadsheet.HibernationMinutes";
    private readonly string _spreadsheetDocumentsDisposeHoursName = "Spreadsheet.DocumentsDisposeHours";
    private readonly string _twitchHttpTimeoutSecondsName = "Networking.TwitchTimeoutSeconds";
    private readonly string _runtimeDirectoryName = "Application.RuntimeDirectoryName";
    private readonly string _runtimeEndpointFileName = "Application.RuntimeEndpointFileName";
    private readonly string _defaultDocumentName = "Editor.DefaultDocumentName";

    /// <summary>
    /// Runs the system variable store service operation.
    /// </summary>
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
    /// Gets default port.
    /// </summary>
    public int DefaultPort => GetInt(_defaultPortName, 58071);
    /// <summary>
    /// Gets port environment variable name.
    /// </summary>
    public string PortEnvironmentVariableName => GetString(_portEnvironmentVariableName, "PUBLISHERSTUDIO_PORT");
    /// <summary>
    /// Gets default culture.
    /// </summary>
    public string DefaultCulture => GetString(_defaultCultureName, "en-US");
    /// <summary>
    /// Gets cors policy name.
    /// </summary>
    public string CorsPolicyName => GetString(_corsPolicyName, "PublisherExport");
    /// <summary>
    /// Gets data protection directory name.
    /// </summary>
    public string DataProtectionDirectoryName => GetString(_dataProtectionDirectoryName, "DataProtection");
    /// <summary>
    /// Gets data protection application name.
    /// </summary>
    public string DataProtectionApplicationName => GetString(_dataProtectionApplicationName, "PublisherStudio");
    /// <summary>
    /// Gets spreadsheet hibernation directory name.
    /// </summary>
    public string SpreadsheetHibernationDirectoryName => GetString(_spreadsheetHibernationDirectoryName, "SpreadsheetHibernation");
    /// <summary>
    /// Gets spreadsheet hibernation timeout.
    /// </summary>
    public TimeSpan SpreadsheetHibernationTimeout => GetTimeSpan(_spreadsheetHibernationMinutesName, TimeSpan.FromMinutes(20));
    /// <summary>
    /// Gets spreadsheet documents dispose timeout.
    /// </summary>
    public TimeSpan SpreadsheetDocumentsDisposeTimeout => GetTimeSpan(_spreadsheetDocumentsDisposeHoursName, TimeSpan.FromHours(4));
    /// <summary>
    /// Gets twitch HTTP timeout.
    /// </summary>
    public TimeSpan TwitchHttpTimeout => TimeSpan.FromSeconds(GetInt(_twitchHttpTimeoutSecondsName, 20));
    /// <summary>
    /// Gets runtime directory name.
    /// </summary>
    public string RuntimeDirectoryName => GetString(_runtimeDirectoryName, "runtime");
    /// <summary>
    /// Gets runtime endpoint file name.
    /// </summary>
    public string RuntimeEndpointFileName => GetString(_runtimeEndpointFileName, "server.json");
    /// <summary>
    /// Gets default document name.
    /// </summary>
    public string DefaultDocumentName => GetString(_defaultDocumentName, "Untitled Publication");

    /// <summary>
    /// Runs the attach logger operation.
    /// </summary>
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
    /// Gets string.
    /// </summary>
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
    /// Gets int.
    /// </summary>
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
    /// Gets time span.
    /// </summary>
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
    /// Runs the set operation.
    /// </summary>
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
    /// Runs the snapshot operation.
    /// </summary>
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
    /// Loads configuration.
    /// </summary>
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
    /// Loads persisted.
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
    /// Runs the persist operation.
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
