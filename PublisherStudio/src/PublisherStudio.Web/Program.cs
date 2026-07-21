using DevExpress.Blazor;
using DevExpress.AspNetCore;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http.Features;
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
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, requestedPort);
            // PublisherStudio is a loopback-only desktop application. Do not impose an
            // application-defined workbook upload ceiling; the user's available memory,
            // disk space, and the spreadsheet engine are the natural limits.
            options.Limits.MaxRequestBodySize = null;
        });

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        // Large offline-first media recordings are transferred in many small JS interop
        // chunks. The operation may legitimately outlive the default interop timeout.
        builder.Services.Configure<CircuitOptions>(options =>
            options.JSInteropDefaultCallTimeout = Timeout.InfiniteTimeSpan);
        builder.Services.AddControllersWithViews();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = long.MaxValue;
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        var dataProtectionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "DataProtection");
        Directory.CreateDirectory(dataProtectionPath);
        var dataProtection = builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName("PublisherStudio");
        if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();
        builder.Services.AddCors(options => options.AddPolicy("PublisherExport", policy =>
            policy.AllowAnyOrigin().WithMethods("GET").AllowAnyHeader()));
        builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Small);

        var spreadsheetHibernationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "SpreadsheetHibernation");
        if (!Directory.Exists(spreadsheetHibernationPath))
        {
            Directory.CreateDirectory(spreadsheetHibernationPath);
        }

        builder.Services.AddDevExpressControls(options =>
        {
            options.AddSpreadsheet(spreadsheetOptions =>
                spreadsheetOptions.AddHibernation(hibernation =>
                {
                    hibernation.StoragePath = spreadsheetHibernationPath;
                    hibernation.Timeout = TimeSpan.FromMinutes(20);
                    hibernation.DocumentsDisposeTimeout = TimeSpan.FromHours(4);
                    hibernation.AllDocumentsOnApplicationEnd = true;
                }));
        });
        builder.Services.AddSingleton<PictureDocumentService>();
        builder.Services.AddSingleton<SpreadsheetDocumentService>();
        builder.Services.AddSingleton<SpreadsheetSessionStore>();
        builder.Services.AddSingleton<PublicationDataService>();
        builder.Services.AddSingleton<PublicationComponentService>();
        builder.Services.AddSingleton<PublicationWebhookStore>();
        builder.Services.AddSingleton<PublicationLiveDataRegistry>();
        builder.Services.AddSingleton<PublicationWebDataService>();
        builder.Services.AddSingleton<PublicationFileService>();
        builder.Services.AddSingleton<PublicationMediaAssetStore>();
        builder.Services.AddSingleton<PublicationRecoveryService>();
        builder.Services.AddSingleton<StreamingProfileStore>();
        builder.Services.AddSingleton<StreamingMediaHostClient>();
        builder.Services.AddSingleton<StreamingSessionService>();
        builder.Services.AddHostedService<StreamingMediaHostLauncher>();
        builder.Services.AddScoped<EditorStateService>();
        builder.Services.AddScoped<PictureEditorStateService>();

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }
        app.UseDevExpressControls();
        app.UseStaticFiles();
        app.UseCors();
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
