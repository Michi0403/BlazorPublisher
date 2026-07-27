using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace PublisherStudio.Services;

public interface IApplicationPortResolver
{
    int Resolve(IReadOnlyList<string> args);
}

public sealed class ApplicationPortResolver(ILogger<ApplicationPortResolver>? logger = null) : IApplicationPortResolver
{
    private readonly ILogger<ApplicationPortResolver> _logger = logger ?? NullLogger<ApplicationPortResolver>.Instance;

    public int Resolve(IReadOnlyList<string> args)
    {
        try
        {
            for (var index = 0; index < args.Count; index++)
            {
                if (!string.Equals(args[index], "--port", StringComparison.OrdinalIgnoreCase)) continue;
                if (index + 1 < args.Count && int.TryParse(args[index + 1], out var port) && port is >= 0 and <= 65535)
                {
                    _logger.LogInformation("PublisherStudio port {Port} was selected from the command line.", port);
                    return port;
                }
            }

            var configured = Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_PORT");
            var resolved = int.TryParse(configured, out var environmentPort) && environmentPort is >= 0 and <= 65535
                ? environmentPort
                : 0;
            _logger.LogInformation(
                resolved == 0
                    ? "PublisherStudio will let Kestrel select an available loopback port."
                    : "PublisherStudio port {Port} was selected from PUBLISHERSTUDIO_PORT.",
                resolved);
            return resolved;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PublisherStudio port resolution failed.");
            throw;
        }
    }
}

public interface IRuntimeEndpointWriter
{
    void Write(WebApplication app);
    void DeleteOwnedEndpoint();
}

public sealed class RuntimeEndpointWriter(ILogger<RuntimeEndpointWriter>? logger = null) : IRuntimeEndpointWriter
{
    private readonly ILogger<RuntimeEndpointWriter> _logger = logger ?? NullLogger<RuntimeEndpointWriter>.Instance;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void Write(WebApplication app)
    {
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
            var baseUrl = addresses?.FirstOrDefault(address => Uri.TryCreate(address, UriKind.Absolute, out _))
                ?? throw new InvalidOperationException("PublisherStudio started without a usable server address.");
            RuntimeEndpointStore.BaseUrl = baseUrl;
            var uri = new Uri(baseUrl);
            Directory.CreateDirectory(RuntimeDirectory);
            File.WriteAllText(RuntimeFilePath, System.Text.Json.JsonSerializer.Serialize(new
            {
                ProcessId = Environment.ProcessId,
                BaseUrl = baseUrl,
                Port = uri.Port,
                StartedAtUtc = DateTimeOffset.UtcNow
            }, JsonOptions));
            _logger.LogInformation("PublisherStudio runtime endpoint {BaseUrl} was written to {RuntimeFilePath}.", baseUrl, RuntimeFilePath);
            Console.WriteLine($"PublisherStudio listening on {baseUrl}");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PublisherStudio runtime endpoint publication failed.");
            throw;
        }
    }

    public void DeleteOwnedEndpoint()
    {
        try
        {
            if (!File.Exists(RuntimeFilePath))
            {
                _logger.LogDebug("PublisherStudio runtime endpoint file was already absent during shutdown.");
                return;
            }

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(RuntimeFilePath));
            if (!document.RootElement.TryGetProperty("ProcessId", out var processId) ||
                !processId.TryGetInt32(out var ownerProcessId) ||
                ownerProcessId != Environment.ProcessId)
            {
                _logger.LogWarning("PublisherStudio did not delete runtime endpoint {RuntimeFilePath} because another process owns it.", RuntimeFilePath);
                return;
            }

            File.Delete(RuntimeFilePath);
            RuntimeEndpointStore.BaseUrl = string.Empty;
            _logger.LogInformation("PublisherStudio removed its runtime endpoint file {RuntimeFilePath}.", RuntimeFilePath);
        }
        catch (IOException exception)
        {
            _logger.LogDebug(exception, "A newer process may already own or replace the PublisherStudio runtime endpoint file.");
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "PublisherStudio could not remove its diagnostic runtime endpoint file during shutdown.");
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "PublisherStudio did not delete a runtime endpoint file whose ownership could not be verified.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected PublisherStudio runtime endpoint cleanup failure.");
            throw;
        }
    }

    private static string RuntimeDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PublisherStudio", "runtime");

    private static string RuntimeFilePath => Path.Combine(RuntimeDirectory, "server.json");
}
