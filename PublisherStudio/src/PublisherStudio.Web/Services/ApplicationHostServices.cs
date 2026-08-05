using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the application port resolver contract.
/// </summary>
public interface IApplicationPortResolver
{
    int Resolve(IReadOnlyList<string> args);
}

/// <summary>
/// Provides application port resolver operations.
/// </summary>
public sealed class ApplicationPortResolver(
    ISystemVariableStoreService systemVariables,
    ILogger<ApplicationPortResolver>? logger = null) : IApplicationPortResolver
{
    private readonly ILogger<ApplicationPortResolver> logger = logger ?? NullLogger<ApplicationPortResolver>.Instance;

    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
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
/// Defines the runtime endpoint state contract.
/// </summary>
public interface IRuntimeEndpointState
{
    string BaseUrl { get; }
    void SetBaseUrl(string baseUrl);
    void Clear();
}

/// <summary>
/// Represents a runtime endpoint state.
/// </summary>
public sealed class RuntimeEndpointState(ILogger<RuntimeEndpointState> logger) : IRuntimeEndpointState
{
    private readonly System.Threading.Lock sync = new();
    private string baseUrl = string.Empty;

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
    /// Sets base URL.
    /// </summary>
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
    /// Runs the clear operation.
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
/// Defines the runtime endpoint writer contract.
/// </summary>
public interface IRuntimeEndpointWriter
{
    void Write(WebApplication app);
    void DeleteOwnedEndpoint();
}

/// <summary>
/// Provides runtime endpoint writer operations.
/// </summary>
public sealed class RuntimeEndpointWriter : IRuntimeEndpointWriter
{
    private readonly ILogger<RuntimeEndpointWriter> logger;
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _runtimeDirectory;
    private readonly string _runtimeFilePath;
    private readonly IRuntimeEndpointState _runtimeEndpointState;

    /// <summary>
    /// Runs the runtime endpoint writer operation.
    /// </summary>
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
    /// Runs the write operation.
    /// </summary>
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
    /// Deletes owned endpoint.
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
