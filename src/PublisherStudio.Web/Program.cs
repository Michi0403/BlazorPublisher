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
using PublisherStudio.Services.Publication;

namespace PublisherStudio;

/// <summary>
/// Represents a program application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public static class Program
{
    /// <summary>
    /// Performs main for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public static async Task Main(string[] args)
    {
        var app = BuildWebApp(args);
        await using var configuredAppAsyncDisposal = app.ConfigureAwait(false);
        var endpointWriter = app.Services.GetRequiredService<IRuntimeEndpointWriter>();
        var hostLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PublisherStudio.Host");
        try
        {
            hostLogger.LogInformation("Starting PublisherStudio host with persistent application logging enabled.");
            await app.StartAsync().ConfigureAwait(false);
            endpointWriter.Write(app);
            await app.WaitForShutdownAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            hostLogger.LogDebug(exception, "PublisherStudio host shutdown was canceled as part of the requested application stop.");
        }
        catch (Exception exception)
        {
            hostLogger.LogCritical(exception, "PublisherStudio host terminated unexpectedly.");
            throw;
        }
        finally
        {
            try
            {
                endpointWriter.DeleteOwnedEndpoint();
            }
            catch (Exception exception)
            {
                hostLogger.LogError(exception, "PublisherStudio could not remove its owned runtime endpoint during shutdown.");
            }
        }
    }

    /// <summary>
    /// Builds web app for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
    /// <returns>The web application produced by the operation.</returns>
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

        new LoggingConfigurationService(builder.Services, builder.Configuration, startupLogger).Configure(builder.Logging);
        builder.Services.AddPublisherStudioApplication(builder.Configuration, startupLogger);
        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.Logging.AddFilter("System", LogLevel.Warning);
        }

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
                new QueryStringRequestCultureProvider
                {
                    QueryStringKey = "culture",
                    UIQueryStringKey = "ui-culture"
                },
                // Keep the shell in one language. Browser Accept-Language no longer partially
                // translates an otherwise English UI; users choose a reviewed culture explicitly.
                new CookieRequestCultureProvider()
            ]
        });
        app.Services.GetRequiredService<IApplicationPathService>().EnsureDirectories();
        app.Services.GetRequiredService<IPublisherTemplateLibraryService>().EnsureTemplateDirectories();

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
