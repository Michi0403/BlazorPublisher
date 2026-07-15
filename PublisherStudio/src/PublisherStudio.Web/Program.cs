using DevExpress.Blazor;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using PublisherStudio.Components;
using PublisherStudio.Services;

namespace PublisherStudio;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = BuildWebApp(args);
        await app.StartAsync();
        RuntimeEndpointWriter.Write(app);
        await app.WaitForShutdownAsync();
    }

    public static WebApplication BuildWebApp(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Args = args ?? []
        });
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        var requestedPort = ResolvePort(args ?? []);
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, requestedPort));

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Small);
        builder.Services.AddSingleton<PublicationFileService>();
        builder.Services.AddScoped<EditorStateService>();

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        return app;
    }

    private static int ResolvePort(IReadOnlyList<string> args)
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

internal static class RuntimeEndpointWriter
{
    public static void Write(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        var baseUrl = addresses?.FirstOrDefault() ?? "http://127.0.0.1";
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
