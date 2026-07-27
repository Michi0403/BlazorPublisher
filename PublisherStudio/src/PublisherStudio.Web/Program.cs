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
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Args = effectiveArgs
        });
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        var requestedPort = new ApplicationPortResolver().Resolve(effectiveArgs);
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
        builder.Services.AddHttpClient(nameof(TwitchOAuthService), client => client.Timeout = TimeSpan.FromSeconds(20));

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
        builder.Services.AddDevExpressBlazor(options => options.SizeMode = SizeMode.Small).AddSpellCheck();

        var spreadsheetHibernationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "SpreadsheetHibernation");
        Directory.CreateDirectory(spreadsheetHibernationPath);
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

        builder.Services.AddPublisherStudioApplication(builder.Configuration);

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        var supportedCultures = app.Services.GetRequiredService<IFileLocalizationService>()
            .GetAvailableCultures()
            .Select(CultureInfo.GetCultureInfo)
            .ToList();
        if (supportedCultures.Count == 0) supportedCultures.Add(CultureInfo.GetCultureInfo("en-US"));
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("en-US"),
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
