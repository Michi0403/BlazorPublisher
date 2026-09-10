using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
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
        WebApplication app;
        try
        {
            TryAppendBootstrapDiagnostic($"PublisherStudio process starting. assembly={typeof(Program).Assembly.GetName().Version}; executable={Environment.ProcessPath ?? "unknown"}; base={AppContext.BaseDirectory}");
            app = BuildWebApp(args);
        }
        catch (Exception exception)
        {
            TryAppendBootstrapDiagnostic("PublisherStudio failed before the configured application logger became available.", exception);
            Console.Error.WriteLine(exception);
            throw;
        }

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
            TryAppendBootstrapDiagnostic("PublisherStudio host terminated unexpectedly.", exception);
            Console.Error.WriteLine(exception);
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

    /// <summary>Appends an early-start or fatal diagnostic directly to the durable PublisherStudio log without depending on DI or the configured logger pipeline.</summary>
    /// <param name="message">Diagnostic message to persist.</param>
    /// <param name="exception">Optional exception to include in full.</param>
    private static void TryAppendBootstrapDiagnostic(string message, Exception? exception = null)
    {
        try
        {
            var logPath = PublisherApplicationDataPaths.ResolveUserPath("PublisherStudio.log");
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var entry = $"{DateTimeOffset.UtcNow:O} [Bootstrap] {message}";
            if (exception is not null)
                entry += Environment.NewLine + exception;
            using var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(entry);
        }
        catch (Exception diagnosticException) when (diagnosticException is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio could not persist bootstrap diagnostics: {diagnosticException}");
        }
    }

    /// <summary>
    /// Builds web app for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the program operation and used when producing its result.</param>
    /// <returns>The web application produced by the operation.</returns>
    public static WebApplication BuildWebApp(string[]? args = null)
    {
        var repairedCurrentDirectory = PublisherApplicationDataPaths.RepairInvalidCurrentDirectory();
        var effectiveArgs = args ?? [];
        using var startupLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var startupLogger = startupLoggerFactory.CreateLogger("PublisherStudio.Startup");
        if (repairedCurrentDirectory)
            startupLogger.LogWarning("The inherited process working directory no longer existed; PublisherStudio repaired it to the per-user runtime directory before startup continued.");
        startupLogger.LogInformation(
            "PublisherStudio runtime identity: assembly={AssemblyVersion}; executable={ExecutablePath}; base={BaseDirectory}; workingDirectory={WorkingDirectory}.",
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            Environment.ProcessPath ?? "unknown",
            AppContext.BaseDirectory,
            PublisherApplicationDataPaths.ResolveSafeCurrentDirectory());

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Args = effectiveArgs
        });
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        var userSettingsFile = PublisherApplicationDataPaths.ResolveUserPath("Configuration", "appsettings.user.json");
        Directory.CreateDirectory(Path.GetDirectoryName(userSettingsFile)!);
        builder.Configuration
            .AddJsonFile(userSettingsFile, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

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

        var dataProtectionPath = PublisherApplicationDataPaths.ResolveUserPath(systemVariables.DataProtectionDirectoryName);
        Directory.CreateDirectory(dataProtectionPath);
        var dataProtection = builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName(systemVariables.DataProtectionApplicationName);
        if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();

        builder.Services.AddCors(options => options.AddPolicy(systemVariables.CorsPolicyName, policy =>
            policy.AllowAnyOrigin().WithMethods("GET").AllowAnyHeader()));
        builder.Services.AddDevExpressBlazor(options => options.SizeMode = SizeMode.Small).AddSpellCheck();

        var spreadsheetHibernationPath = PublisherApplicationDataPaths.ResolveUserPath(systemVariables.SpreadsheetHibernationDirectoryName);
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
        app.Services.GetRequiredService<IApplicationPathService>().EnsureAndDocumentLayout();
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

/// <summary>
/// Resolves PublisherStudio application-data roots while keeping writable state per-user by default.
/// Portable and system-wide roots are discovery/install candidates only unless the user explicitly configures them.
/// </summary>
internal static class PublisherApplicationDataPaths
{
    /// <summary>Defines the stable product directory name used beneath per-user and system discovery roots.</summary>
    public const string ProductName = "PublisherStudio";

    /// <summary>Resolves the platform-native per-user application-data base without selecting a system-wide writable location.</summary>
    /// <returns>The absolute per-user application-data base for the current host.</returns>
    public static string ResolveUserDataBase()
    {
        var userProfile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolderOption.DoNotVerify);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrWhiteSpace(localAppData))
                return Path.GetFullPath(localAppData);

            var windowsLocal = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);
            if (!string.IsNullOrWhiteSpace(windowsLocal))
                return Path.GetFullPath(windowsLocal);
            if (!string.IsNullOrWhiteSpace(userProfile))
                return Path.GetFullPath(Path.Combine(userProfile, "AppData", "Local"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var macLocal = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);
            if (!string.IsNullOrWhiteSpace(macLocal))
                return Path.GetFullPath(macLocal);
            if (!string.IsNullOrWhiteSpace(userProfile))
                return Path.GetFullPath(Path.Combine(userProfile, "Library", "Application Support"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrWhiteSpace(xdgDataHome))
                return Path.GetFullPath(xdgDataHome);

            var linuxLocal = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);
            if (!string.IsNullOrWhiteSpace(linuxLocal))
                return Path.GetFullPath(linuxLocal);
            if (!string.IsNullOrWhiteSpace(userProfile))
                return Path.GetFullPath(Path.Combine(userProfile, ".local", "share"));
        }

        var configured = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        if (!string.IsNullOrWhiteSpace(applicationData))
            return Path.GetFullPath(applicationData);
        if (!string.IsNullOrWhiteSpace(userProfile))
            return Path.GetFullPath(Path.Combine(userProfile, ".local", "share"));

        throw new InvalidOperationException("PublisherStudio could not resolve a durable per-user application-data directory.");
    }

    /// <summary>Resolves the canonical per-user PublisherStudio writable root used by all application-owned mutable state.</summary>
    /// <returns>The absolute per-user PublisherStudio data root.</returns>
    public static string ResolveUserRoot() => Path.Combine(ResolveUserDataBase(), ProductName);

    /// <summary>Combines path segments beneath the canonical per-user PublisherStudio writable root.</summary>
    /// <param name="segments">Relative path segments to append beneath the per-user root.</param>
    /// <returns>The absolute per-user path containing the requested segments.</returns>
    public static string ResolveUserPath(params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = ResolveUserRoot();
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    /// <summary>Returns an existing durable working directory for child processes and packaged launches.</summary>
    /// <returns>The canonical per-user PublisherStudio runtime directory, or a per-user temporary fallback when it cannot be created.</returns>
    public static string ResolveProcessWorkingDirectory()
    {
        try
        {
            var runtimeDirectory = ResolveUserPath("runtime");
            Directory.CreateDirectory(runtimeDirectory);
            return runtimeDirectory;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var fallback = Path.Combine(Path.GetTempPath(), ProductName, "runtime");
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    /// <summary>Returns the current directory when valid, otherwise a durable per-user runtime directory.</summary>
    /// <returns>A directory that exists and can safely be used by runtime services.</returns>
    public static string ResolveSafeCurrentDirectory()
    {
        try
        {
            var current = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                return current;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Fall through to the durable runtime directory.
        }
        return ResolveProcessWorkingDirectory();
    }

    /// <summary>Repairs an invalid inherited current directory without changing normal development launches.</summary>
    /// <returns><see langword="true"/> when the current directory had to be repaired; otherwise <see langword="false"/>.</returns>
    public static bool RepairInvalidCurrentDirectory()
    {
        try
        {
            _ = Directory.GetCurrentDirectory();
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Directory.SetCurrentDirectory(ResolveProcessWorkingDirectory());
            return true;
        }
    }

    /// <summary>Resolves the application base directory used for explicit portable tool/content discovery.</summary>
    /// <returns>The absolute portable application root.</returns>
    public static string ResolvePortableRoot() => Path.GetFullPath(AppContext.BaseDirectory);

    /// <summary>Enumerates platform-appropriate system-wide PublisherStudio discovery roots without making them writable defaults.</summary>
    /// <returns>The ordered, de-duplicated system-wide discovery roots for the current host.</returns>
    public static IReadOnlyList<string> EnumerateSystemWideRoots()
    {
        var candidates = new List<string>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AddFolderCandidate(candidates, Environment.SpecialFolder.CommonApplicationData);
            AddFolderCandidate(candidates, Environment.SpecialFolder.ProgramFiles);
            AddFolderCandidate(candidates, Environment.SpecialFolder.ProgramFilesX86);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            candidates.Add("/Library/Application Support/PublisherStudio");
            candidates.Add("/Applications/PublisherStudio.app/Contents/Resources");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            candidates.Add("/var/lib/PublisherStudio");
            candidates.Add("/usr/local/share/PublisherStudio");
            candidates.Add("/usr/share/PublisherStudio");
            candidates.Add("/opt/PublisherStudio");
        }

        return candidates
            .Select(Path.GetFullPath)
            .Distinct(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Adds a platform special-folder PublisherStudio candidate when the folder resolves to a usable path.</summary>
    /// <param name="candidates">Collection receiving the resolved PublisherStudio discovery path.</param>
    /// <param name="folder">Platform special folder to resolve without verifying its existence.</param>
    private static void AddFolderCandidate(ICollection<string> candidates, Environment.SpecialFolder folder)
    {
        var value = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
        if (!string.IsNullOrWhiteSpace(value))
            candidates.Add(Path.Combine(value, ProductName));
    }
}
