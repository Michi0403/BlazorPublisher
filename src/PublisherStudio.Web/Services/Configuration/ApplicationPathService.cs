using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates PublisherStudio path behavior. Per-user application data is the default for all mutable
/// application/configuration/content paths; project/configured overrides remain authoritative when supplied.
/// </summary>
/// <param name="options">Configured application path options whose explicit values override per-user defaults.</param>
/// <param name="platform">Platform runtime service used for host/platform semantics without leaking OS behavior into callers.</param>
/// <param name="systemVariables">System-variable service providing maintained PublisherStudio runtime directory names.</param>
/// <param name="logger">Logger used to record path-resolution and first-boot diagnostics.</param>
public sealed class ApplicationPathService(
    IOptions<PublisherStudioPathOptions> options,
    IPublisherPlatformRuntimeService platform,
    ISystemVariableStoreService systemVariables,
    ILogger<ApplicationPathService> logger) : IApplicationPathService
{
    /// <summary>Gets the resolved default PublisherStudio content paths for the current user.</summary>
    /// <returns>The resolved per-user default path options.</returns>
    public PublisherStudioPathOptions GetDefaults()
    {
        try
        {
            return Resolve();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.GetDefaults failed.");
            throw;
        }
    }

    /// <summary>Resolves application path options using explicit project/configuration overrides before per-user defaults.</summary>
    /// <param name="projectOverrides">Optional project-specific path overrides.</param>
    /// <returns>The resolved path options.</returns>
    public PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            var configured = options.Value;
            var userDataRoot = PublisherApplicationDataPaths.ResolveUserRoot();
            return new PublisherStudioPathOptions
            {
                Images = Choose(projectOverrides?.Images, configured.Images, Path.Combine(userDataRoot, "Images")),
                Video = Choose(projectOverrides?.Video, configured.Video, Path.Combine(userDataRoot, "Video")),
                Audio = Choose(projectOverrides?.Audio, configured.Audio, Path.Combine(userDataRoot, "Audio")),
                Documents = Choose(projectOverrides?.Documents, configured.Documents, Path.Combine(userDataRoot, "Documents")),
                Exports = Choose(projectOverrides?.Exports, configured.Exports, Path.Combine(userDataRoot, "Exports")),
                OpenScad = Choose(projectOverrides?.OpenScad, configured.OpenScad, Path.Combine(userDataRoot, "OpenSCAD")),
                Projects = Choose(projectOverrides?.Projects, configured.Projects, Path.Combine(userDataRoot, "Projects"))
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.Resolve failed.");
            throw;
        }
    }

    /// <summary>Resolves the effective path for a requested media kind using the same override/default contract.</summary>
    /// <param name="mediaKind">Media category whose effective directory should be resolved.</param>
    /// <param name="projectOverrides">Optional project-specific path overrides.</param>
    /// <returns>The absolute effective media directory.</returns>
    public string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            var paths = Resolve(projectOverrides);
            return (mediaKind ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "image" or "images" or "picture" => paths.Images,
                "video" or "videos" => paths.Video,
                "audio" or "music" => paths.Audio,
                "openscad" or "3d" => paths.OpenScad,
                "export" or "exports" => paths.Exports,
                "project" or "projects" => paths.Projects,
                _ => paths.Documents
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.ResolveMediaPath failed.");
            throw;
        }
    }

    /// <summary>Creates the resolved PublisherStudio application-owned content directories for the current user/project.</summary>
    /// <param name="projectOverrides">Optional project-specific path overrides used when creating directories.</param>
    public void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            var paths = Resolve(projectOverrides);
            foreach (var path in new[]
            {
                PublisherApplicationDataPaths.ResolveUserRoot(),
                PublisherApplicationDataPaths.ResolveUserPath("Configuration"),
                PublisherApplicationDataPaths.ResolveUserPath(systemVariables.RuntimeDirectoryName),
                paths.Images, paths.Video, paths.Audio, paths.Documents, paths.Exports, paths.OpenScad, paths.Projects
            }.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.EnsureDirectories failed.");
            throw;
        }
    }

    /// <summary>Builds the current per-user, portable, and system-wide path-layout snapshot for support and first-boot guidance.</summary>
    /// <returns>The detected path-layout snapshot.</returns>
    public PublisherStudioApplicationPathLayout GetLayout()
    {
        try
        {
            var reportFile = PublisherApplicationDataPaths.ResolveUserPath("Configuration", "path-layout.json");
            return new PublisherStudioApplicationPathLayout
            {
                Platform = platform.HostPlatform.ToString(),
                UserDataRoot = PublisherApplicationDataPaths.ResolveUserRoot(),
                UserConfigurationFile = PublisherApplicationDataPaths.ResolveUserPath("Configuration", "appsettings.user.json"),
                SystemVariablesFile = PublisherApplicationDataPaths.ResolveUserPath("Configuration", "system-variables.json"),
                RuntimeDirectory = PublisherApplicationDataPaths.ResolveUserPath(systemVariables.RuntimeDirectoryName),
                DataProtectionDirectory = PublisherApplicationDataPaths.ResolveUserPath(systemVariables.DataProtectionDirectoryName),
                SpreadsheetHibernationDirectory = PublisherApplicationDataPaths.ResolveUserPath(systemVariables.SpreadsheetHibernationDirectoryName),
                DefaultContentPaths = Resolve(),
                PortableApplicationRoot = PublisherApplicationDataPaths.ResolvePortableRoot(),
                SystemWideDiscoveryRoots = PublisherApplicationDataPaths.EnumerateSystemWideRoots(),
                LayoutReportFile = reportFile,
                FirstBootDetected = !File.Exists(reportFile),
                GeneratedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.GetLayout failed.");
            throw;
        }
    }

    /// <summary>Ensures the per-user PublisherStudio folder structure and atomically persists the current path-layout report.</summary>
    /// <returns>The path-layout snapshot that was persisted.</returns>
    public PublisherStudioApplicationPathLayout EnsureAndDocumentLayout()
    {
        var layout = GetLayout();
        try
        {
            EnsureDirectories();
            Directory.CreateDirectory(layout.DataProtectionDirectory);
            Directory.CreateDirectory(layout.SpreadsheetHibernationDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(layout.LayoutReportFile)!);

            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            var temporary = layout.LayoutReportFile + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, layout.LayoutReportFile, overwrite: true);
            logger.LogInformation("PublisherStudio user-data layout: {UserDataRoot}. Path report: {ReportFile}", layout.UserDataRoot, layout.LayoutReportFile);
            return layout;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService.EnsureAndDocumentLayout failed.");
            throw;
        }
    }

    /// <summary>Selects the first configured non-empty path candidate and normalizes it to an absolute path.</summary>
    /// <param name="candidates">Ordered path candidates from highest to lowest precedence.</param>
    /// <returns>The selected absolute path.</returns>
    private string Choose(params string?[] candidates)
    {
        try
        {
            var selected = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
            selected ??= PublisherApplicationDataPaths.ResolveUserRoot();
            return Environment.ExpandEnvironmentVariables(Path.GetFullPath(selected));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ApplicationPathService could not resolve a configured/default path candidate.");
            throw;
        }
    }
}
