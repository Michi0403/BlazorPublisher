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
}

public sealed class RuntimeEndpointWriter : IRuntimeEndpointWriter
{
    public void Write(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        var baseUrl = addresses?.FirstOrDefault() ?? $"http://{IPAddress.Loopback}";
        RuntimeEndpointStore.BaseUrl = baseUrl;
        var uri = new Uri(baseUrl);
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "runtime");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "server.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            ProcessId = Environment.ProcessId, BaseUrl = baseUrl, Port = uri.Port, StartedAtUtc = DateTimeOffset.UtcNow
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PublisherStudio listening on {baseUrl}");
    }
}
