using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace PublisherStudio.Services;

public interface IApplicationPortResolver
{
    int Resolve(IReadOnlyList<string> args);
}

public sealed class ApplicationPortResolver : IApplicationPortResolver
{
    public int Resolve(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], "--port", StringComparison.OrdinalIgnoreCase)) continue;
            if (index + 1 < args.Count && int.TryParse(args[index + 1], out var port) && port is >= 0 and <= 65535) return port;
        }
        var configured = Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_PORT");
        return int.TryParse(configured, out var environmentPort) && environmentPort is >= 0 and <= 65535 ? environmentPort : 0;
    }
}

public interface IRuntimeEndpointWriter
{
    void Write(WebApplication app);
    void DeleteOwnedEndpoint();
}

public sealed class RuntimeEndpointWriter : IRuntimeEndpointWriter
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void Write(WebApplication app)
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
        Console.WriteLine($"PublisherStudio listening on {baseUrl}");
    }

    public void DeleteOwnedEndpoint()
    {
        try
        {
            if (!File.Exists(RuntimeFilePath)) return;
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(RuntimeFilePath));
            if (!document.RootElement.TryGetProperty("ProcessId", out var processId) ||
                !processId.TryGetInt32(out var ownerProcessId) ||
                ownerProcessId != Environment.ProcessId)
                return;

            File.Delete(RuntimeFilePath);
            RuntimeEndpointStore.BaseUrl = string.Empty;
        }
        catch (IOException)
        {
            // A newer process may already own or replace the runtime endpoint file.
        }
        catch (UnauthorizedAccessException)
        {
            // Shutdown must not fail because a diagnostic endpoint file could not be removed.
        }
        catch (System.Text.Json.JsonException)
        {
            // Never delete an endpoint file whose ownership cannot be verified.
        }
    }

    private static string RuntimeDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PublisherStudio", "runtime");

    private static string RuntimeFilePath => Path.Combine(RuntimeDirectory, "server.json");
}
