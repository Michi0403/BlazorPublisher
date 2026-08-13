using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the contract for application port behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IApplicationPortResolver
{
    /// <summary>
    /// Performs resolve for <see cref="IApplicationPortResolver"/>, keeping the operation consistent with the state and invariants of the surrounding application port workflow.
    /// </summary>
    /// <param name="args">String dependency used by the application port workflow to provide the corresponding application capability.</param>
    /// <returns>The int produced by the operation.</returns>
    int Resolve(IReadOnlyList<string> args);
}

/// <summary>
/// Resolves application port choices from the available runtime state and returns the application-appropriate result to callers.
/// </summary>
/// <param name="systemVariables">System variable store service dependency used by the application port workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ApplicationPortResolver(
    ISystemVariableStoreService systemVariables,
    ILogger<ApplicationPortResolver>? logger = null) : IApplicationPortResolver
{
    /// <summary>
    /// Stores the logger used by <see cref="ApplicationPortResolver"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<ApplicationPortResolver> logger = logger ?? NullLogger<ApplicationPortResolver>.Instance;

    /// <summary>
    /// Performs resolve for <see cref="ApplicationPortResolver"/>, keeping the operation consistent with the state and invariants of the surrounding application port workflow.
    /// </summary>
    /// <param name="args">String dependency used by the application port workflow to provide the corresponding application capability.</param>
    /// <returns>The int produced by the operation.</returns>
    public int Resolve(IReadOnlyList<string> args)
    {
        try
        {
            for (var index = 0; index < args.Count; index++)
            {
                if (!string.Equals(args[index], "--port", StringComparison.OrdinalIgnoreCase)) continue;
                if (index + 1 < args.Count && int.TryParse(args[index + 1], out var port) && port is >= 0 and <= 65535)
                {
                    logger.LogInformation("PublisherStudio port {Port} was selected from the command line.", port);
                    return port;
                }
            }

            var configured = Environment.GetEnvironmentVariable(systemVariables.PortEnvironmentVariableName);
            var resolved = int.TryParse(configured, out var environmentPort) && environmentPort is >= 0 and <= 65535
                ? environmentPort
                : systemVariables.DefaultPort;
            logger.LogInformation(
                "PublisherStudio loopback port {Port} was selected from {Source}.",
                resolved,
                string.IsNullOrWhiteSpace(configured) ? "the system-variable default" : systemVariables.PortEnvironmentVariableName);
            return resolved;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio port resolution failed.");
            throw;
        }
    }
}


/// <summary>
/// Defines the contract for runtime endpoint behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRuntimeEndpointState
{
    /// <summary>
    /// Gets the base URL that identifies the network or application endpoint associated with this runtime endpoint state.
    /// </summary>
    /// <value>The base URL value exposed by <see cref="IRuntimeEndpointState"/>.</value>
    string BaseUrl { get; }
    /// <summary>
    /// Sets base URL for <see cref="IRuntimeEndpointState"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    /// <param name="baseUrl">Base url value supplied to the runtime endpoint operation and used when producing its result.</param>
    void SetBaseUrl(string baseUrl);
    /// <summary>
    /// Performs clear for <see cref="IRuntimeEndpointState"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents runtime endpoint state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RuntimeEndpointState(ILogger<RuntimeEndpointState> logger) : IRuntimeEndpointState
{
    /// <summary>
    /// Stores the internal sync state used by <see cref="RuntimeEndpointState"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Threading.Lock sync = new();
    /// <summary>
    /// Stores the internal base URL state used by <see cref="RuntimeEndpointState"/> while executing its surrounding workflow.
    /// </summary>
    private string baseUrl = string.Empty;

    /// <summary>
    /// Gets the base URL that identifies the network or application endpoint associated with this runtime endpoint state.
    /// </summary>
    /// <value>The base URL value exposed by <see cref="RuntimeEndpointState"/>.</value>
    public string BaseUrl
    {
        get
        {
            try
            {
                lock (sync)
                {
                    logger.LogTrace($"Read the PublisherStudio runtime endpoint state.");
                    return baseUrl;
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Reading the PublisherStudio runtime endpoint state failed: {exception.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Sets base URL for <see cref="RuntimeEndpointState"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    /// <param name="baseUrl">Base url value supplied to the runtime endpoint operation and used when producing its result.</param>
    public void SetBaseUrl(string baseUrl)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            lock (sync) this.baseUrl = baseUrl;
            logger.LogInformation($"Updated the PublisherStudio runtime endpoint state.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Updating the PublisherStudio runtime endpoint state failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clear for <see cref="RuntimeEndpointState"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    public void Clear()
    {
        try
        {
            lock (sync) baseUrl = string.Empty;
            logger.LogInformation($"Cleared the PublisherStudio runtime endpoint state.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Clearing the PublisherStudio runtime endpoint state failed: {exception.Message}");
            throw;
        }
    }
}

/// <summary>
/// Defines the contract for runtime endpoint behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRuntimeEndpointWriter
{
    /// <summary>
    /// Performs write for <see cref="IRuntimeEndpointWriter"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    /// <param name="app">App value supplied to the runtime endpoint operation and used when producing its result.</param>
    void Write(WebApplication app);
    /// <summary>
    /// Deletes owned endpoint for <see cref="IRuntimeEndpointWriter"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    void DeleteOwnedEndpoint();
}

/// <summary>
/// Represents a runtime endpoint application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RuntimeEndpointWriter : IRuntimeEndpointWriter
{
    /// <summary>
    /// Stores the logger used by <see cref="RuntimeEndpointWriter"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<RuntimeEndpointWriter> logger;
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="RuntimeEndpointWriter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    /// <summary>
    /// Stores the internal runtime directory state used by <see cref="RuntimeEndpointWriter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _runtimeDirectory;
    /// <summary>
    /// Stores the internal runtime file path state used by <see cref="RuntimeEndpointWriter"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _runtimeFilePath;
    /// <summary>
    /// Stores the runtime endpoint state dependency used by <see cref="RuntimeEndpointWriter"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IRuntimeEndpointState _runtimeEndpointState;

    /// <summary>
    /// Initializes a new <see cref="RuntimeEndpointWriter"/> instance and captures the dependencies or initial state required by its runtime endpoint workflow.
    /// </summary>
    /// <param name="systemVariables">System variable store service dependency used by the runtime endpoint workflow to provide the corresponding application capability.</param>
    /// <param name="runtimeEndpointState">Runtime endpoint state dependency used by the runtime endpoint workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public RuntimeEndpointWriter(
        ISystemVariableStoreService systemVariables,
        IRuntimeEndpointState runtimeEndpointState,
        ILogger<RuntimeEndpointWriter> logger)
    {
        this.logger = logger;
        _runtimeEndpointState = runtimeEndpointState;
        _runtimeDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            systemVariables.DataProtectionApplicationName,
            systemVariables.RuntimeDirectoryName);
        _runtimeFilePath = Path.Combine(_runtimeDirectory, systemVariables.RuntimeEndpointFileName);
    }

    /// <summary>
    /// Performs write for <see cref="RuntimeEndpointWriter"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    /// <param name="app">App value supplied to the runtime endpoint operation and used when producing its result.</param>
    public void Write(WebApplication app)
    {
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
            var baseUrl = addresses?.FirstOrDefault(address => Uri.TryCreate(address, UriKind.Absolute, out _))
                ?? throw new InvalidOperationException("PublisherStudio started without a usable server address.");
            _runtimeEndpointState.SetBaseUrl(baseUrl);
            var uri = new Uri(baseUrl);
            Directory.CreateDirectory(_runtimeDirectory);
            File.WriteAllText(_runtimeFilePath, System.Text.Json.JsonSerializer.Serialize(new
            {
                ProcessId = Environment.ProcessId,
                BaseUrl = baseUrl,
                Port = uri.Port,
                StartedAtUtc = DateTimeOffset.UtcNow
            }, _jsonOptions));
            logger.LogInformation("PublisherStudio runtime endpoint {BaseUrl} was written to {RuntimeFilePath}.", baseUrl, _runtimeFilePath);
            Console.WriteLine($"PublisherStudio listening on {baseUrl}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio runtime endpoint publication failed.");
            throw;
        }
    }

    /// <summary>
    /// Deletes owned endpoint for <see cref="RuntimeEndpointWriter"/>, keeping the operation consistent with the state and invariants of the surrounding runtime endpoint workflow.
    /// </summary>
    public void DeleteOwnedEndpoint()
    {
        try
        {
            if (!File.Exists(_runtimeFilePath))
            {
                logger.LogDebug("PublisherStudio runtime endpoint file was already absent during shutdown.");
                return;
            }

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(_runtimeFilePath));
            if (!document.RootElement.TryGetProperty("ProcessId", out var processId) ||
                !processId.TryGetInt32(out var ownerProcessId) ||
                ownerProcessId != Environment.ProcessId)
            {
                logger.LogWarning("PublisherStudio did not delete runtime endpoint {RuntimeFilePath} because another process owns it.", _runtimeFilePath);
                return;
            }

            File.Delete(_runtimeFilePath);
            _runtimeEndpointState.Clear();
            logger.LogInformation("PublisherStudio removed its runtime endpoint file {RuntimeFilePath}.", _runtimeFilePath);
        }
        catch (IOException exception)
        {
            logger.LogDebug(exception, "A newer process may already own or replace the PublisherStudio runtime endpoint file.");
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "PublisherStudio could not remove its diagnostic runtime endpoint file during shutdown.");
        }
        catch (System.Text.Json.JsonException exception)
        {
            logger.LogWarning(exception, "PublisherStudio did not delete a runtime endpoint file whose ownership could not be verified.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected PublisherStudio runtime endpoint cleanup failure.");
            throw;
        }
    }

}
