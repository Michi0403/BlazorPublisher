using System.Globalization;
using System.Net;
using DevExpress.AspNetCore;
using DevExpress.Blazor;
using DevExpress.Blazor.RichEdit;
using DevExpress.Blazor.RichEdit.SpellCheck;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using PublisherStudio.Components;
using PublisherStudio.Diagnostics;
using PublisherStudio.Services;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await using var app = BuildWebApp(args);
        var endpointWriter = app.Services.GetRequiredService<IRuntimeEndpointWriter>();
        try
        {
            await app.StartAsync();
            endpointWriter.Write(app);
            await app.WaitForShutdownAsync();
        }
        finally
        {
            endpointWriter.DeleteOwnedEndpoint();
        }
    }

    public static WebApplication BuildWebApp(string[]? args = null)
    {
        var effectiveArgs = args ?? [];
        using var startupLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var startupLogger = startupLoggerFactory.CreateLogger("PublisherStudio.Startup");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Args = effectiveArgs
        });
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        var systemVariables = new SystemVariableStoreService(builder.Configuration);
        builder.Services.AddSingleton<ISystemVariableStoreService>(systemVariables);
        builder.Services.AddSingleton(systemVariables);

        var requestedPort = new ApplicationPortResolver(systemVariables).Resolve(effectiveArgs);
        builder.WebHost.ConfigureKestrel(options =>
        {
            if (requestedPort > 0)
                options.Listen(IPAddress.Loopback, requestedPort);

            options.Limits.MaxRequestBodySize = null;
        });

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.Configure<CircuitOptions>(options =>
            options.JSInteropDefaultCallTimeout = Timeout.InfiniteTimeSpan);
        builder.Services.AddScoped<ControllerRequestLoggingFilter>();
        builder.Services.AddControllersWithViews(options =>
            options.Filters.AddService<ControllerRequestLoggingFilter>());
        builder.Services.AddLocalization();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = long.MaxValue;
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient(nameof(TwitchOAuthService), client => client.Timeout = systemVariables.TwitchHttpTimeout);

        var dataProtectionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", systemVariables.DataProtectionDirectoryName);
        Directory.CreateDirectory(dataProtectionPath);
        var dataProtection = builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName(systemVariables.DataProtectionApplicationName);
        if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();

        builder.Services.AddCors(options => options.AddPolicy(systemVariables.CorsPolicyName, policy =>
            policy.AllowAnyOrigin().WithMethods("GET").AllowAnyHeader()));
        builder.Services.AddDevExpressBlazor(options => options.SizeMode = SizeMode.Small).AddSpellCheck();

        var spreadsheetHibernationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", systemVariables.SpreadsheetHibernationDirectoryName);
        Directory.CreateDirectory(spreadsheetHibernationPath);
        builder.Services.AddDevExpressControls(options =>
        {
            options.AddSpreadsheet(spreadsheetOptions =>
                spreadsheetOptions.AddHibernation(hibernation =>
                {
                    hibernation.StoragePath = spreadsheetHibernationPath;
                    hibernation.Timeout = systemVariables.SpreadsheetHibernationTimeout;
                    hibernation.DocumentsDisposeTimeout = systemVariables.SpreadsheetDocumentsDisposeTimeout;
                    hibernation.AllDocumentsOnApplicationEnd = true;
                }));
        });

        builder.Services.AddPublisherStudioApplication(builder.Configuration, startupLogger);
        if (!builder.Environment.IsDevelopment())
            builder.Logging.AddFilter((category, level) => level >= LogLevel.Warning);

        var app = builder.Build();
        systemVariables.AttachLogger(app.Services.GetRequiredService<ILogger<SystemVariableStoreService>>());
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        var supportedCultures = app.Services.GetRequiredService<IFileLocalizationService>()
            .GetAvailableCultures()
            .Select(CultureInfo.GetCultureInfo)
            .ToList();
        if (supportedCultures.Count == 0) supportedCultures.Add(CultureInfo.GetCultureInfo(systemVariables.DefaultCulture));
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(systemVariables.DefaultCulture),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            RequestCultureProviders =
            [
                // Keep the shell in one language. Browser Accept-Language no longer partially
                // translates an otherwise English UI; users choose a reviewed culture explicitly.
                new CookieRequestCultureProvider()
            ]
        });
        app.Services.GetRequiredService<IApplicationPathService>().EnsureDirectories();

        app.UseDevExpressControls();
        app.UseStaticFiles();
        app.UseCors();
        app.UseWebSockets();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        return app;
    }
}
