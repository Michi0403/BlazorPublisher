using PublisherStudio.InstallerConsole.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace PublisherStudio.InstallerConsole;
internal static class Program
{
    private const string PublisherStudioRepo = "Michi0403/BlazorPublisher";
    private static readonly HttpClient Http = CreateHttpClient();
    private const string DetachedSetupEnvironmentVariable = "PUBLISHERSTUDIO_SETUP_DETACHED";

    private static bool TryStartDetachedSetup(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;
        if (string.Equals(Environment.GetEnvironmentVariable(DetachedSetupEnvironmentVariable), "1", StringComparison.Ordinal))
            return false;

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            return false;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            return false;

        var installRoot = Path.GetFullPath(Path.Combine(localAppData, "PublisherStudio"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var executablePath = Path.GetFullPath(processPath);
        if (!executablePath.StartsWith(installRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return false;

        var detachedDirectory = Path.Combine(Path.GetTempPath(), "PublisherStudio", "setup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(detachedDirectory);
        var detachedExecutable = Path.Combine(detachedDirectory, Path.GetFileName(executablePath));
        File.Copy(executablePath, detachedExecutable, overwrite: true);

        var startInfo = new ProcessStartInfo
        {
            FileName = detachedExecutable,
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = Environment.CurrentDirectory
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);
        startInfo.Environment[DetachedSetupEnvironmentVariable] = "1";

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("PublisherStudio detached setup process could not be started.");
        Console.WriteLine("PublisherStudio setup continued from a temporary copy so the installed setup can be replaced.");
        return true;
    }

    /// <summary>
    /// Runs the main operation.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        if (TryStartDetachedSetup(args))
            return 0;

        var launchedByDoubleClick = args.Length == 0 && Environment.UserInteractive;

        Console.WriteLine("PublisherStudio Setup 2.1.7");
        var options = CliOptions.Parse(args);
        if (args.Length == 0)
            Console.WriteLine("No command-line action was supplied. Running the default install, update, shortcut, and start routine.");
        else
            Console.WriteLine($"Requested setup actions:{Environment.NewLine}{options}");
        try
        {
            return await RunAsync(args, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (launchedByDoubleClick || options.WaitOnExit)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        
    }
    private static async Task<int> RunAsync(string[] args, CliOptions options)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ColorConsoleLoggerConfiguration colorLoggerProviderOptions = new ColorConsoleLoggerConfiguration() { EventId = 0 };
            ColorConsoleLoggerProvider colorLoggerProvider = new ColorConsoleLoggerProvider(colorLoggerProviderOptions);


            using var loggerFactory = LoggerFactory.Create(configure =>
            {
                configure.ClearProviders();
                configure.AddProvider(colorLoggerProvider);
                //configure.AddProvider()
            });
            var logger = loggerFactory.CreateLogger("Startup");
            logger.LogInformation("Configured app configuration.");

            if (options.ShowHelp)
            {
                CliOptions.PrintHelp(logger);
                return 0;
            }
            try
            {
                if (options.Uninstall)
                {
                    UninstallPublisherStudioWindows(options, logger);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UninstallPublisherStudioWindows.");
            }

            try
            {
                try
                {
                    if (options.InstallPublisherStudio || options.UpdatePublisherStudio)
                        await InstallPublisherStudioAsync(options, logger).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PublisherStudio installation/update failed before the existing installation was modified.");
                    return 1;
                }
                try
                {
                    if (options.CheckFfmpeg && !options.InstallFfmpeg)
                        FfmpegProvisioner.ReportStatus(logger);
                    else if (!options.SkipFfmpeg && (options.InstallFfmpeg || options.InstallPublisherStudio || options.UpdatePublisherStudio))
                        await FfmpegProvisioner.EnsureInstalledAsync(logger).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while checking or installing FFmpeg.");
                }
                try
                {
                    if (options.DesktopShortcuts || options.StartMenuShortcuts)
                        ProvisionWindowsShortcuts(options, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in ProvisionWindowsShortcuts.");
                }
                try
                {
                    if (options.StartPublisherStudio)
                        StartPublisherStudio(options, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in StartPublisherStudio.");
                }


                logger.LogDebug("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Setup: {ex.ToString()}");
                if (options.Verbose)
                    logger.LogWarning(ex.ToString());
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunAsync {ex.ToString()}");
            return -1;
        }
    }

    private static async Task InstallPublisherStudioAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var runtimeIdentifier = GetRuntimeIdentifier();
            var expectedApplicationAsset = GetExpectedReleaseAssetName(runtimeIdentifier, setupAsset: false);
            var expectedSetupAsset = GetExpectedReleaseAssetName(runtimeIdentifier, setupAsset: true);
            var zipPath = options.PublisherStudioZipPath ?? Path.Combine(Environment.CurrentDirectory, expectedApplicationAsset);
            var setupZipPath = options.PublisherStudioSetupZipPath ?? Path.Combine(Environment.CurrentDirectory, expectedSetupAsset);

            await EnsureReleaseAssetAsync(
                PublisherStudioRepo,
                expectedApplicationAsset,
                zipPath,
                options.PublisherStudioZipPath,
                logger,
                options,
                setupAsset: false,
                runtimeIdentifier: runtimeIdentifier).ConfigureAwait(false);

            await EnsureReleaseAssetAsync(
                PublisherStudioRepo,
                expectedSetupAsset,
                setupZipPath,
                options.PublisherStudioSetupZipPath,
                logger,
                options,
                setupAsset: true,
                runtimeIdentifier: runtimeIdentifier).ConfigureAwait(false);

            ValidateReleaseArchive(zipPath, GetRuntimeFolderName(), logger);
            ValidateReleaseArchive(setupZipPath, "setup" + GetRuntimeFolderName(), logger);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            var targetPath = Path.Combine(localAppData, "PublisherStudio");

            if (options.ForceDelete)
                DeleteIfExists(targetPath, logger);

            Directory.CreateDirectory(targetPath);

            logger.LogInformation(
                "PublisherStudio runtime {RuntimeIdentifier} uses release assets {ApplicationAsset} and {SetupAsset}.",
                runtimeIdentifier,
                expectedApplicationAsset,
                expectedSetupAsset);

            logger.LogInformation($"Extracting PublisherStudio app '{zipPath}' to '{targetPath}'");
            ExtractZipWithFallback(zipPath, targetPath, logger);

            logger.LogInformation($"Extracting PublisherStudio setup/bootstrap '{setupZipPath}' to '{targetPath}'");
            ExtractZipWithFallback(setupZipPath, targetPath, logger);

            logger.LogDebug($"PublisherStudio installed to '{targetPath}'.");
            logger.LogInformation($"PublisherStudio app and setup/bootstrap files now reside in '{targetPath}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in InstallPublisherStudioAsync. options {options}");
            throw;
        }
    }

    private static void UninstallPublisherStudioWindows(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(UninstallPublisherStudioWindows), logger);

            var targets = GetPublisherStudioUninstallTargets(options, logger);

            logger.LogWarning("PublisherStudio uninstall preview:");

            foreach (var target in targets)
            {
                var exists = File.Exists(target) || Directory.Exists(target);
                logger.LogInformation($"{(exists ? "[exists]" : "[missing]")} {target}");
            }

            if (!options.ForceDelete)
            {
                logger.LogWarning("Dry run only. Nothing was deleted.");
                logger.LogWarning("Run again with --uninstall --force-delete to delete the listed PublisherStudio files.");
                return;
            }

            logger.LogWarning("--force-delete was used. Removing listed PublisherStudio files.");

            foreach (var target in targets)
            {
                DeleteIfExists(target, logger);
            }

            logger.LogInformation("PublisherStudio uninstall finished.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in UninstallPublisherStudioWindows. options {options.ToString()}");
        }
    }
    private static List<string> GetPublisherStudioUninstallTargets(CliOptions options, ILogger logger)
    {
        try
        {
            var targets = new List<string>();

            var publisherStudioRoot = GetPublisherStudioInstallRoot(logger);
            targets.Add(publisherStudioRoot);

            var startMenuFolder = GetStartMenuFolder(options,logger);
            targets.Add(startMenuFolder);

            var desktop = GetDesktopFolder(logger);

            var shortcutDefinitions = GetShortcutTargets(publisherStudioRoot, logger);

            foreach (var shortcut in shortcutDefinitions)
            {
                var shortcutFileName = Path.ChangeExtension(shortcut.ShortcutName, ".url");
                targets.Add(Path.Combine(desktop, shortcutFileName));
            }

            return targets
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetPublisherStudioUninstallTargets. options {options.ToString()}");
            return new List<string>();
        }
    }

    private static void ProvisionWindowsShortcuts(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(ProvisionWindowsShortcuts), logger);

            var publisherStudioRoot = GetPublisherStudioInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(publisherStudioRoot) || !Directory.Exists(publisherStudioRoot))
                throw new DirectoryNotFoundException($"PublisherStudio directory was not found: {publisherStudioRoot}");

            logger.LogInformation($"Provisioning Windows shortcuts from PublisherStudio directory: {publisherStudioRoot}");

            var shortcuts = GetShortcutTargets(publisherStudioRoot, logger);

            if (shortcuts.Count == 0)
            {
                logger.LogWarning($"No shortcut targets found in PublisherStudio directory: {publisherStudioRoot}");
                return;
            }

            if (options.DesktopShortcuts)
            {
                var desktop = GetDesktopFolder(logger);
                logger.LogInformation($"Creating Desktop shortcuts in: {desktop}");
             
                CreateShortcutSet(shortcuts, desktop, logger);
            }

            if (options.StartMenuShortcuts)
            {
                var startMenuFolder = GetStartMenuFolder(options,logger);
                Directory.CreateDirectory(startMenuFolder);

                logger.LogInformation($"Creating Start Menu shortcuts in: {startMenuFolder}");
                CreateShortcutSet(shortcuts, startMenuFolder, logger);
            }

            logger.LogInformation("Windows shortcut provisioning finished.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ProvisionWindowsShortcuts. options {options}");
            throw;
        }
    }
    private static List<ShortcutDefinition> GetShortcutTargets(string publisherStudioRoot, ILogger logger)
    {
        try
        {
            var shortcuts = new List<ShortcutDefinition>();

            shortcuts.Add(new ShortcutDefinition(
                ShortcutName: "PublisherStudio Folder.lnk",
                TargetPath: publisherStudioRoot,
                Arguments: string.Empty,
                WorkingDirectory: publisherStudioRoot));

            AddCmdShortcutIfExists(
                shortcuts,
                publisherStudioRoot,
                "Install.cmd",
                "PublisherStudio Install.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                publisherStudioRoot,
                "Update.cmd",
                "PublisherStudio Update.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                publisherStudioRoot,
                "Start.cmd",
                "PublisherStudio Start.url",
                logger);

            return shortcuts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetShortcutTargets. publisherStudioRoot {publisherStudioRoot}");
            return new List<ShortcutDefinition>();
        }
    }
    private static void CreateShortcutSet(
    List<ShortcutDefinition> shortcuts,
    string targetDirectory,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new InvalidOperationException("Shortcut target directory is empty.");

            Directory.CreateDirectory(targetDirectory);

            var publisherStudioRoot = GetPublisherStudioInstallRoot(logger);
            var iconPath = FindPublisherStudioIcon(logger);

            foreach (var shortcut in shortcuts)
            {
                var shortcutPath = Path.Combine(
                    targetDirectory,
                    Path.ChangeExtension(shortcut.ShortcutName, ".url"));

                CreateWindowsUrlShortcut(
                    shortcutPath,
                    shortcut.TargetPath,
                    iconPath, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateShortcutSet. targetDirectory {targetDirectory}");
            throw;
        }
    }
    private static void CreateWindowsUrlShortcut(
    string shortcutPath,
    string targetPath,
    string? iconPath,
    ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(CreateWindowsUrlShortcut), logger);

            if (string.IsNullOrWhiteSpace(shortcutPath))
                throw new ArgumentException("Shortcut path is empty.", nameof(shortcutPath));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("Target path is empty.", nameof(targetPath));

            var fullTargetPath = Path.GetFullPath(targetPath);
            var targetUri = new Uri(fullTargetPath).AbsoluteUri;

            logger.LogInformation($"Creating URL shortcut: {shortcutPath}");
            logger.LogInformation($"URL shortcut target path: {fullTargetPath}");
            logger.LogInformation($"URL shortcut target uri: {targetUri}");
            logger.LogInformation($"adding shortcut to iconPath uri: {iconPath} if empty then not");
            var directory = Path.GetDirectoryName(Path.GetFullPath(shortcutPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            builder.AppendLine("[InternetShortcut]");
            builder.AppendLine($"URL={targetUri}");
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
            {
                var fullIconPath = Path.GetFullPath(iconPath);

                logger.LogInformation($"URL shortcut icon: {fullIconPath}");

                builder.AppendLine($"IconFile={fullIconPath}");
                builder.AppendLine("IconIndex=0");
            }
            else
            {
                logger.LogWarning($"Shortcut icon not found, creating shortcut without custom icon: {iconPath}");
            }
            File.WriteAllText(shortcutPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            logger.LogInformation($"URL shortcut created: {shortcutPath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateWindowsUrlShortcut. shortcutPath {shortcutPath} targetPath {targetPath}");
            throw;
        }
    }
    private static IEnumerable<string> EnumerateFilesSafe(
    string root,
    string searchPattern,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(
                root,
                searchPattern,
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnumerateFilesSafe. root {root} searchPattern {searchPattern}");
            return Enumerable.Empty<string>();
        }
    }
    private static string? FindPublisherStudioIcon(ILogger logger)
    {
        try
        {
            var publisherStudioRoot = GetPublisherStudioInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(publisherStudioRoot) || !Directory.Exists(publisherStudioRoot))
            {
                logger.LogWarning($"PublisherStudio root does not exist while resolving icon: {publisherStudioRoot}");
                return null;
            }

            var knownCandidates = new[]
            {
                Path.Combine(publisherStudioRoot, "PublisherStudio.ico"),
                Path.Combine(publisherStudioRoot, GetRuntimeFolderName(), "PublisherStudio.ico"),
                Path.Combine(publisherStudioRoot, $"setup{GetRuntimeFolderName()}", "PublisherStudio.ico")
            };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking PublisherStudio icon candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved PublisherStudio icon from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known PublisherStudio.ico paths failed. Searching recursively under: {publisherStudioRoot}");

            var publisherIcon = EnumerateFilesSafe(publisherStudioRoot, "PublisherStudio.ico", logger)
                .OrderBy(path => GetRelativePathDepth(publisherStudioRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(publisherIcon) && File.Exists(publisherIcon))
            {
                logger.LogInformation($"Resolved PublisherStudio PublisherStudio.ico recursively: {publisherIcon}");
                return publisherIcon;
            }

            logger.LogWarning($"Publisher icon not found. Falling back to any .ico under: {publisherStudioRoot}");

            var anyIcon = EnumerateFilesSafe(publisherStudioRoot, "*.ico", logger)
                .OrderBy(path => GetRelativePathDepth(publisherStudioRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(anyIcon) && File.Exists(anyIcon))
            {
                logger.LogInformation($"Resolved PublisherStudio icon recursively: {anyIcon}");
                return anyIcon;
            }

            logger.LogWarning($"No .ico file found under: {publisherStudioRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FindPublisherStudioIcon.");
            return null;
        }
    }
    private static string? FindPublisherStudioFile(
    string publisherStudioRoot,
    string fileName,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publisherStudioRoot) || !Directory.Exists(publisherStudioRoot))
            {
                logger.LogWarning($"PublisherStudio root does not exist while searching for file '{fileName}': {publisherStudioRoot}");
                return null;
            }

            foreach (var directPath in new[]
            {
                Path.Combine(publisherStudioRoot, fileName),
                Path.Combine(publisherStudioRoot, $"setup{GetRuntimeFolderName()}", fileName)
            })
            {
                logger.LogInformation("Checking PublisherStudio file candidate: {CandidatePath}", directPath);
                if (!File.Exists(directPath)) continue;
                logger.LogInformation("Resolved PublisherStudio file from direct path: {CandidatePath}", directPath);
                return directPath;
            }

            logger.LogWarning($"Direct PublisherStudio file candidate not found. Searching recursively for '{fileName}' under: {publisherStudioRoot}");

            var recursiveCandidate = EnumerateFilesSafe(publisherStudioRoot, fileName, logger)
                .OrderBy(path => GetRelativePathDepth(publisherStudioRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved PublisherStudio file recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find '{fileName}' under: {publisherStudioRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindPublisherStudioFile. publisherStudioRoot {publisherStudioRoot} fileName {fileName}");
            return null;
        }
    }
    private static string? FindPublisherStudioExecutable(CliOptions options, ILogger logger)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.PublisherStudioExePath))
            {
                var explicitPath = Environment.ExpandEnvironmentVariables(options.PublisherStudioExePath);

                logger.LogInformation($"Checking explicit PublisherStudio executable path: {explicitPath}");

                if (File.Exists(explicitPath))
                    return Path.GetFullPath(explicitPath);

                logger.LogWarning($"--publisherstudio-exe was provided but does not exist: {explicitPath}");
            }

            var publisherStudioRoot = GetPublisherStudioInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(publisherStudioRoot) || !Directory.Exists(publisherStudioRoot))
            {
                logger.LogWarning($"PublisherStudio root does not exist: {publisherStudioRoot}");
                return null;
            }

            var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "PublisherStudio.Web.exe"
                : "PublisherStudio.Web";

            var knownCandidates = new[]
            {
                Path.Combine(publisherStudioRoot, GetRuntimeFolderName(), executableName),
                Path.Combine(publisherStudioRoot, executableName)
            };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking PublisherStudio executable candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved PublisherStudio executable from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known PublisherStudio executable paths failed. Searching recursively under: {publisherStudioRoot}");

            var recursiveCandidate = EnumerateFilesSafe(publisherStudioRoot, executableName, logger)
                .OrderBy(path => GetRelativePathDepth(publisherStudioRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved PublisherStudio executable recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find {executableName} under: {publisherStudioRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindPublisherStudioExecutable. options {options}");
            return null;
        }
    }
    private static int GetRelativePathDepth(string root, string path)
    {
        try
        {
            var relative = Path.GetRelativePath(root, path);
            return relative.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return int.MaxValue;
        }
    }
    private static void AddCmdShortcutIfExists(
    List<ShortcutDefinition> shortcuts,
    string publisherStudioRoot,
    string cmdFileName,
    string shortcutName,
    ILogger logger)
    {
        try
        {
            var cmdPath = FindPublisherStudioFile(publisherStudioRoot, cmdFileName, logger);

            if (string.IsNullOrWhiteSpace(cmdPath) || !File.Exists(cmdPath))
            {
                logger.LogWarning($"Shortcut target CMD not found, skipping: {cmdFileName}");
                return;
            }

            var workingDirectory = Path.GetDirectoryName(cmdPath);

            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = publisherStudioRoot;

            shortcuts.Add(new ShortcutDefinition(
                ShortcutName: shortcutName,
                TargetPath: cmdPath,
                Arguments: string.Empty,
                WorkingDirectory: workingDirectory));

            logger.LogInformation($"Shortcut target found: {cmdPath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in AddCmdShortcutIfExists. cmdFileName {cmdFileName}");
        }
    }
    private static void EnsureWindowsOnly(string featureName, ILogger logger)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var exception = new PlatformNotSupportedException($"{featureName} is Windows-only.");
        logger.LogError(exception, "Windows-only setup feature {FeatureName} was requested on another platform.", featureName);
        throw exception;
    }

    private static string GetPublisherStudioInstallRoot(ILogger logger)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            return Path.Combine(localAppData, "PublisherStudio");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetPublisherStudioInstallRoot. {ex}");
            return string.Empty;
        }
    }

    private static string GetStartMenuFolder(CliOptions options, ILogger logger)
    {
        try
        {
            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

            if (string.IsNullOrWhiteSpace(startMenu))
                throw new InvalidOperationException("Start Menu folder could not be resolved.");

            var groupName = SanitizeShortcutGroupName(options.ShortcutGroupName, logger);

            if (string.IsNullOrWhiteSpace(groupName))
                groupName = "PublisherStudio by Michi0403";

            return Path.Combine(startMenu, "Programs", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetStartMenuFolder. {ex}");
            return string.Empty;
        }
    }
    private static string SanitizeShortcutGroupName(string value, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return "PublisherStudio by Michi0403";

            var invalid = Path.GetInvalidFileNameChars();

            foreach (var ch in invalid)
                value = value.Replace(ch, '_');

            return value.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in SanitizeShortcutGroupName. value {value}");
            return "PublisherStudio by Michi0403";
        }
    }
    private static string GetDesktopFolder(ILogger logger)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrWhiteSpace(desktop))
                throw new InvalidOperationException("Desktop folder could not be resolved.");

            return desktop;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetDesktopFolder. {ex.ToString()}");
            return string.Empty;
        }
    }

    private static void StartPublisherStudio(CliOptions options, ILogger logger)
    {
        try
        {
            var exePath = FindPublisherStudioExecutable(options, logger);


            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                throw new FileNotFoundException(
                    $"PublisherStudio executable not found at '{exePath}'. Install it first or pass --publisherstudio-exe.");

            var port = options.PublisherStudioPort <= 0 ? 58071 : options.PublisherStudioPort;

            logger.LogInformation($"Starting PublisherStudio: {exePath}");
            logger.LogInformation($"PublisherStudio requested loopback port: {port}");

            if (TryGetRunningEndpoint("PublisherStudio", "PublisherStudio", out var existingUrl, logger))
            {
                Console.WriteLine();
                Console.WriteLine($"PublisherStudio is already running: {existingUrl}");
                Console.WriteLine("Ctrl+click the URL above if your console does not open links on a normal click.");
                if (options.OpenBrowser)
                    OpenDefaultBrowser(existingUrl, logger);
                return;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                ArgumentList = { "--port", port.ToString() },
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            }) ?? throw new InvalidOperationException("PublisherStudio process could not be started.");

            var url = WaitForRuntimeEndpoint(
                productName: "PublisherStudio",
                runtimeProductDirectory: "PublisherStudio",
                process: process,
                fallbackPort: port,
                logger: logger);

            Console.WriteLine();
            Console.WriteLine($"PublisherStudio is ready: {url}");
            Console.WriteLine("Ctrl+click the URL above if your console does not open links on a normal click.");
            logger.LogInformation("PublisherStudio is ready at {BaseUrl}.", url);

            if (options.OpenBrowser)
            {
                logger.LogInformation($"Opening browser: {url}");
                OpenDefaultBrowser(url, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PublisherStudio startup failed.");
            throw;
        }
    }
    private static bool TryGetRunningEndpoint(
        string productName,
        string runtimeProductDirectory,
        out string url,
        ILogger logger)
    {
        url = string.Empty;
        var endpointPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            runtimeProductDirectory,
            "runtime",
            "server.json");
        try
        {
            if (!File.Exists(endpointPath))
                return false;

            using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
            var root = document.RootElement;
            if (!root.TryGetProperty("ProcessId", out var processIdElement)
                || !processIdElement.TryGetInt32(out var processId)
                || processId <= 0
                || !root.TryGetProperty("BaseUrl", out var baseUrlElement))
                return false;

            var baseUrl = baseUrlElement.GetString();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || !uri.IsLoopback
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            using var process = Process.GetProcessById(processId);
            process.Refresh();
            if (process.HasExited)
                return false;

            url = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            logger.LogInformation("Using already running {ProductName} process {ProcessId} at {BaseUrl}.", productName, processId, url);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "Could not inspect the existing {ProductName} runtime endpoint.", productName);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Could not inspect the existing {ProductName} runtime endpoint.", productName);
            return false;
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Ignored an invalid existing {ProductName} runtime endpoint file.", productName);
            return false;
        }
    }

    private static string WaitForRuntimeEndpoint(
        string productName,
        string runtimeProductDirectory,
        Process process,
        int fallbackPort,
        ILogger logger)
    {
        var endpointPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            runtimeProductDirectory,
            "runtime",
            "server.json");
        var fallbackUrl = $"http://127.0.0.1:{fallbackPort}";
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        Exception? lastReadFailure = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            process.Refresh();
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"{productName} exited with code {process.ExitCode} before publishing its runtime URL.");
            }

            if (File.Exists(endpointPath))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
                    var root = document.RootElement;
                    if (!root.TryGetProperty("ProcessId", out var processIdElement)
                        || !processIdElement.TryGetInt32(out var endpointProcessId)
                        || endpointProcessId != process.Id)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    if (!root.TryGetProperty("BaseUrl", out var baseUrlElement))
                    {
                        throw new JsonException("Runtime endpoint file does not contain BaseUrl.");
                    }

                    var baseUrl = baseUrlElement.GetString();
                    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                        || !uri.IsLoopback
                        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        throw new InvalidDataException(
                            $"{productName} published an invalid non-loopback runtime URL '{baseUrl}'.");
                    }

                    return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                }
                catch (IOException ex)
                {
                    lastReadFailure = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastReadFailure = ex;
                }
                catch (JsonException ex)
                {
                    lastReadFailure = ex;
                }
            }

            Thread.Sleep(250);
        }

        logger.LogError(
            lastReadFailure,
            "{ProductName} did not publish a usable runtime endpoint at {EndpointPath}. Requested fallback was {FallbackUrl}.",
            productName,
            endpointPath,
            fallbackUrl);
        throw new TimeoutException(
            $"{productName} did not become ready within 45 seconds. Requested URL: {fallbackUrl}");
    }

    private static void OpenDefaultBrowser(string url, ILogger logger)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not open default browser for URL: {url}");
            throw;
        }
    }


    private static async Task EnsureReleaseAssetAsync(
        string repo,
        string expectedAssetName,
        string destinationPath,
        string? explicitSourcePath,
        ILogger logger,
        CliOptions options,
        bool setupAsset,
        string runtimeIdentifier)
    {
        if (!string.IsNullOrWhiteSpace(explicitSourcePath))
        {
            var sourcePath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitSourcePath));
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"The explicitly supplied release archive does not exist: {sourcePath}", sourcePath);

            if (!string.Equals(sourcePath, Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }

            logger.LogInformation("Using explicitly supplied PublisherStudio release archive: {ArchivePath}", destinationPath);
            return;
        }

        var directUrl = $"https://github.com/{repo}/releases/latest/download/{Uri.EscapeDataString(expectedAssetName)}";
        try
        {
            logger.LogInformation("Downloading exact latest-release asset {AssetName} directly from GitHub.", expectedAssetName);
            await DownloadFileAsync(directUrl, destinationPath, logger, options, expectedSize: null).ConfigureAwait(false);
            return;
        }
        catch (Exception directException)
        {
            logger.LogWarning(directException,
                "Direct latest-release download for {AssetName} failed. Falling back to the GitHub release API.",
                expectedAssetName);
        }

        await DownloadLatestReleaseAssetAsync(
            repo,
            destinationPath,
            logger,
            options,
            setupAsset,
            runtimeIdentifier).ConfigureAwait(false);
    }

    private static void ValidateReleaseArchive(string archivePath, string expectedRootDirectory, ILogger logger)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException($"PublisherStudio release archive was not found: {archivePath}", archivePath);

        using var archive = ZipFile.OpenRead(archivePath);
        if (archive.Entries.Count == 0)
            throw new InvalidDataException($"PublisherStudio release archive is empty: {archivePath}");

        var expectedPrefix = expectedRootDirectory.TrimEnd('/', '\\') + "/";
        var hasExpectedRoot = archive.Entries.Any(entry =>
            entry.FullName.Replace('\\', '/').StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));

        if (!hasExpectedRoot)
            throw new InvalidDataException(
                $"PublisherStudio release archive '{archivePath}' does not contain the required wrapper directory '{expectedRootDirectory}'.");

        logger.LogInformation("Validated release archive {ArchivePath} with wrapper directory {WrapperDirectory}.", archivePath, expectedRootDirectory);
    }

    private static async Task DownloadLatestReleaseAssetAsync(
        string repo,
        string outFile,
        ILogger logger,
        CliOptions options,
        bool setupAsset,
        string runtimeIdentifier)
    {
        try
        {
            ValidateRepo(repo, logger);
            var latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";
            using var json = await GetJsonWithRetryAsync(latestUrl, logger).ConfigureAwait(false);

            var root = json.RootElement;
            var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : "unknown";
            logger.LogInformation($"Latest {repo} release: {tagName}");

            if (!root.TryGetProperty("assets", out var assets) || assets.GetArrayLength() == 0)
                throw new InvalidOperationException($"No downloadable release assets found for {repo}.");

            var expectedAssetName = GetExpectedReleaseAssetName(runtimeIdentifier, setupAsset);
            JsonElement? selected = null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                var isExactMatch = string.Equals(name, expectedAssetName, StringComparison.OrdinalIgnoreCase);
                logger.LogInformation(
                    "Checking release asset {AssetName}. ExactMatch={ExactMatch}; Expected={ExpectedAssetName}.",
                    name,
                    isExactMatch,
                    expectedAssetName);

                if (isExactMatch)
                {
                    selected = asset;
                    break;
                }
            }

            if (selected is null)
            {
                throw new InvalidOperationException(
                    $"The latest PublisherStudio release does not contain required asset '{expectedAssetName}'. Refusing to guess or deploy another runtime.");
            }

            var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
            var assetName = selected.Value.GetProperty("name").GetString();
            var expectedSize = selected.Value.TryGetProperty("size", out var sizeElement) && sizeElement.TryGetInt64(out var parsedSize)
                ? parsedSize
                : (long?)null;

            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException($"Selected release asset for {repo} has no download URL.");

            logger.LogInformation($"Selected asset: {assetName}");
            logger.LogInformation($"Downloading {assetName} to {outFile}");

            await DownloadFileAsync(downloadUrl, outFile, logger, options, expectedSize).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadLatestReleaseAssetAsync. repo {repo} outFile {outFile} setupAsset={setupAsset}");
            throw;
        }
    }

    private static async Task<JsonDocument> GetJsonWithRetryAsync(string url, ILogger logger)
    {
        const int maxAttempts = 4;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                lastError = exception;
                if (attempt == maxAttempts) break;
                var delay = TimeSpan.FromSeconds(attempt * 2);
                logger.LogWarning(exception, "GitHub release lookup attempt {Attempt}/{Attempts} failed. Retrying in {Seconds} seconds.", attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        throw new HttpRequestException($"Could not retrieve release information from {url} after {maxAttempts} attempts.", lastError);
    }

    private static async Task DownloadFileAsync(
        string url,
        string outFile,
        ILogger logger,
        CliOptions options,
        long? expectedSize)
    {
        const int maxAttempts = 5;
        var tempFile = outFile + ".part";
        Exception? lastError = null;

        var directory = Path.GetDirectoryName(Path.GetFullPath(outFile));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(outFile) && expectedSize is > 0 && new FileInfo(outFile).Length == expectedSize.Value)
        {
            try
            {
                using var cachedArchive = ZipFile.OpenRead(outFile);
                if (cachedArchive.Entries.Count == 0)
                    throw new InvalidDataException("Cached archive contains no entries.");
                logger.LogInformation("Reusing complete cached download '{Path}' ({Size}).", outFile, FormatBytes(expectedSize.Value, logger));
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Cached download '{Path}' is invalid and will be downloaded again.", outFile);
                File.Delete(outFile);
            }
        }

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var resumeAt = File.Exists(tempFile) ? new FileInfo(tempFile).Length : 0L;
                if (expectedSize is > 0 && resumeAt > expectedSize.Value)
                {
                    logger.LogWarning("Discarding oversized partial download '{Path}'.", tempFile);
                    File.Delete(tempFile);
                    resumeAt = 0;
                }

                if (expectedSize is > 0 && resumeAt == expectedSize.Value)
                {
                    await MoveFileWithRetryAsync(tempFile, outFile, logger, options).ConfigureAwait(false);
                    logger.LogInformation("Recovered complete cached partial download: {Path} ({Size}).", outFile, FormatBytes(expectedSize.Value, logger));
                    return;
                }

                logger.LogInformation("Downloading attempt {Attempt}/{Attempts}: {Url}", attempt, maxAttempts, url);
                logger.LogInformation("Target: {Target}", outFile);
                if (resumeAt > 0)
                    logger.LogInformation("Resuming at {Offset} instead of restarting from zero.", FormatBytes(resumeAt, logger));

                using var totalTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(45));
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("PublisherStudioSetupTool/1.0");
                request.Headers.Accept.ParseAdd("*/*");
                if (resumeAt > 0)
                    request.Headers.Range = new RangeHeaderValue(resumeAt, null);

                using var response = await Http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    totalTimeout.Token).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable && resumeAt > 0)
                {
                    logger.LogWarning("The server rejected the resume offset. Restarting this asset once from zero.");
                    File.Delete(tempFile);
                    throw new IOException("Remote server rejected the partial-download resume offset.");
                }

                response.EnsureSuccessStatusCode();

                var resumed = response.StatusCode == System.Net.HttpStatusCode.PartialContent && resumeAt > 0;
                if (!resumed) resumeAt = 0;

                var responseLength = response.Content.Headers.ContentLength;
                var expectedTotal = expectedSize
                    ?? response.Content.Headers.ContentRange?.Length
                    ?? (responseLength.HasValue ? resumeAt + responseLength.Value : (long?)null);

                logger.LogInformation(expectedTotal.HasValue
                    ? $"Remote size: {FormatBytes(expectedTotal.Value, logger)}"
                    : "Remote size: unknown");

                long totalRead = resumeAt;
                var fileMode = resumed ? FileMode.Append : FileMode.Create;
                var transferStarted = Stopwatch.StartNew();

                await using (var input = await response.Content.ReadAsStreamAsync(totalTimeout.Token).ConfigureAwait(false))
                await using (var output = new FileStream(
                    tempFile,
                    fileMode,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024 * 1024,
                    useAsync: true))
                {
                    var buffer = new byte[1024 * 1024];
                    var lastLog = DateTimeOffset.UtcNow;
                    long lastLoggedBytes = totalRead;

                    while (true)
                    {
                        var read = await ReadWithStallTimeoutAsync(input, buffer, totalTimeout.Token).ConfigureAwait(false);
                        if (read == 0) break;

                        await output.WriteAsync(buffer.AsMemory(0, read), totalTimeout.Token).ConfigureAwait(false);
                        totalRead += read;

                        var now = DateTimeOffset.UtcNow;
                        if (now - lastLog >= TimeSpan.FromSeconds(5))
                        {
                            var intervalSeconds = Math.Max(0.001, (now - lastLog).TotalSeconds);
                            var intervalRate = (totalRead - lastLoggedBytes) / intervalSeconds;
                            if (expectedTotal is > 0)
                            {
                                var percent = Math.Min(100, totalRead * 100.0 / expectedTotal.Value);
                                logger.LogInformation(
                                    "Downloaded {Current} / {Total} ({Percent:F1}%) at {Rate}/s",
                                    FormatBytes(totalRead, logger),
                                    FormatBytes(expectedTotal.Value, logger),
                                    percent,
                                    FormatBytes((long)intervalRate, logger));
                            }
                            else
                            {
                                logger.LogInformation("Downloaded {Current} at {Rate}/s", FormatBytes(totalRead, logger), FormatBytes((long)intervalRate, logger));
                            }

                            lastLog = now;
                            lastLoggedBytes = totalRead;
                        }
                    }

                    await output.FlushAsync(totalTimeout.Token).ConfigureAwait(false);
                }

                if (!File.Exists(tempFile))
                    throw new FileNotFoundException($"Temporary download file does not exist after download: {tempFile}");

                var actualSize = new FileInfo(tempFile).Length;
                if (actualSize == 0)
                    throw new IOException("Downloaded file is empty.");

                if (expectedTotal.HasValue && actualSize != expectedTotal.Value)
                    throw new IOException($"Incomplete download. Got {actualSize:N0} bytes, expected {expectedTotal.Value:N0} bytes.");

                await MoveFileWithRetryAsync(tempFile, outFile, logger, options).ConfigureAwait(false);

                logger.LogInformation("Download complete: {Path} ({Size}) in {Elapsed:mm\\:ss}.", outFile, FormatBytes(actualSize, logger), transferStarted.Elapsed);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                logger.LogWarning(ex, "Download attempt {Attempt}/{Attempts} failed. The partial file is retained for resume.", attempt, maxAttempts);

                if (attempt == maxAttempts)
                    break;

                var delay = TimeSpan.FromSeconds(Math.Min(20, 2 * attempt * attempt));
                logger.LogInformation("Retrying in {Seconds} seconds...", delay.TotalSeconds);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        logger.LogError(lastError, "Download failed permanently. url {Url} outFile {OutFile}", url, outFile);
        throw new IOException($"Download failed after {maxAttempts} attempts: {url}", lastError);
    }

    private static async Task<int> ReadWithStallTimeoutAsync(Stream input, byte[] buffer, CancellationToken totalCancellationToken)
    {
        using var stallTimeout = CancellationTokenSource.CreateLinkedTokenSource(totalCancellationToken);
        stallTimeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            return await input.ReadAsync(buffer.AsMemory(0, buffer.Length), stallTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!totalCancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The download produced no data for two minutes.");
        }
    }

    private static async Task MoveFileWithRetryAsync(string source, string destination, ILogger logger, CliOptions options)
    {
        try
        {
            _ = options;
            for (var i = 1; i <= 10; i++)
            {
                try
                {
                    if (!File.Exists(source))
                        throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

                    // This replaces only the installer download cache. Installation-directory
                    // deletion remains governed by --force-delete in the deployment workflow.
                    File.Move(source, destination, overwrite: true);
                    return;
                }
                catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && i < 10)
                {
                    logger.LogWarning(ex, "Could not finalize downloaded file {Source} as {Destination}. Retry {Attempt}/10.", source, destination, i);
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * i)).ConfigureAwait(false);
                }
            }

            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

            File.Move(source, destination, overwrite: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in MoveFileWithRetryAsync. source {Source} destination {Destination}", source, destination);
            throw;
        }
    }
    private static string FormatBytes(long bytes, ILogger logger)
    {
        try
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double value = bytes;
            var unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return $"{value:F2} {units[unit]}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FormatBytes. bytes {bytes.ToString()}");
            throw;
        }
      
    }

    private static void DeleteIfExists(string path, ILogger logger)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return;

            logger.LogWarning($"Deleting existing path because --force-delete was used: {path}");

            var attrs = File.GetAttributes(path);
            if (attrs.HasFlag(FileAttributes.Directory))
                Directory.Delete(path, recursive: true);
            else
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DeleteIfExists. path {path.ToString()}");
        }
    }
    private static void ExtractZipWithFallback(string zipPath, string targetPath, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(targetPath);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, targetPath, overwriteFiles: true);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $".NET ZIP extraction failed: {ex.Message}");
            }

            var sevenZip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");
            if (!File.Exists(sevenZip))
                throw new InvalidOperationException("ZIP extraction failed and 7-Zip was not found. Install 7-Zip or enable long paths.");

            RunProcessAsync(sevenZip, $"x \"{zipPath}\" -o\"{targetPath}\" -y", logger).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ExtractZipWithFallback. zipPath {zipPath} targetPath {targetPath}");
            throw;
        }
    }

    private static async Task RunProcessAsync(string fileName, string arguments, ILogger logger)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) logger.LogInformation(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logger.LogWarning(e.Data); };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start process: {fileName}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {arguments}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in RunProcessAsync. fileName {fileName.ToString()} arguments {arguments.ToString()}");
            throw;
        }

    }

    private static string GetExpectedReleaseAssetName(string runtimeIdentifier, bool setupAsset)
    {
        var runtimeFolder = runtimeIdentifier.Trim().ToLowerInvariant() switch
        {
            "win-x64" => "winx64",
            "win-x86" => "winx86",
            "win-arm64" => "winarm64",
            "linux-x64" => "linx64",
            "linux-arm64" => "linarm64",
            "osx-x64" => "macosx64",
            "osx-arm64" => "macosarm64",
            _ => throw new PlatformNotSupportedException(
                $"PublisherStudio release runtime '{runtimeIdentifier}' is not supported.")
        };

        return $"{(setupAsset ? "setup" : string.Empty)}{runtimeFolder}.zip";
    }

    private static string GetPlatformToken()
    {

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "lin";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        return "";
    }

    private static string GetArchitectureToken() => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm => "arm",
        Architecture.Arm64 => "arm64",
        _ => ""
    };

    private static string GetRuntimeIdentifier()
    {
        var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "linux"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "osx"
                    : throw new PlatformNotSupportedException("PublisherStudio setup does not support this operating system.");
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"PublisherStudio setup does not support architecture {RuntimeInformation.OSArchitecture}.")
        };
        return $"{platform}-{architecture}";
    }

    private static string GetRuntimeFolderName()
    {
        var platform = GetPlatformToken();
        var architecture = GetArchitectureToken();
        return $"{platform}{architecture}";
    }

    private static void ValidateRepo(string repo, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repo) || repo.Count(c => c == '/') != 1)
                throw new ArgumentException($"Invalid GitHub repo '{repo}'. Expected format: owner/repository");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ValidateRepo. repo {repo.ToString()}");
            throw;
        }

    }

    private static HttpClient CreateHttpClient()
    {
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PublisherStudioSetupTool", "1.0"));
            client.Timeout = Timeout.InfiniteTimeSpan;
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateHttpClient. {ex.ToString()}");
            throw;
        }
    }
}

internal sealed record ShortcutDefinition(
    string ShortcutName,
    string TargetPath,
    string Arguments,
    string WorkingDirectory
);

internal sealed class CliOptions
{
    /// <summary>
    /// Gets or sets show help.
    /// </summary>
    public bool ShowHelp { get; private set; }
    /// <summary>
    /// Gets or sets install blazor publisher.
    /// </summary>
    public bool InstallPublisherStudio { get; private set; }
    /// <summary>
    /// Gets or sets update blazor publisher.
    /// </summary>
    public bool UpdatePublisherStudio { get; private set; }
    /// <summary>
    /// Gets or sets start blazor publisher.
    /// </summary>
    public bool StartPublisherStudio { get; private set; }
    /// <summary>
    /// Gets or sets verbose.
    /// </summary>
    public bool Verbose { get; private set; }
    /// <summary>
    /// Gets or sets blazor publisher zip path.
    /// </summary>
    public string? PublisherStudioZipPath { get; private set; }
    /// <summary>
    /// Gets or sets blazor publisher setup zip path.
    /// </summary>
    public string? PublisherStudioSetupZipPath { get; private set; }
    /// <summary>
    /// Gets or sets blazor publisher exe path.
    /// </summary>
    public string? PublisherStudioExePath { get; private set; }
    /// <summary>
    /// Gets or sets blazor publisher port.
    /// </summary>
    public int PublisherStudioPort { get; private set; } = 58071;
    /// <summary>
    /// Gets or sets open browser.
    /// </summary>
    public bool OpenBrowser { get; private set; } = true;
    /// <summary>
    /// Gets or sets force delete.
    /// </summary>
    public bool ForceDelete { get; private set; }
    /// <summary>
    /// Gets or sets wait on exit.
    /// </summary>
    public bool WaitOnExit { get; private set; }
    /// <summary>
    /// Gets or sets uninstall.
    /// </summary>
    public bool Uninstall { get; private set; }
    /// <summary>
    /// Gets or sets desktop shortcuts.
    /// </summary>
    public bool DesktopShortcuts { get; private set; }
    /// <summary>
    /// Gets or sets start menu shortcuts.
    /// </summary>
    public bool StartMenuShortcuts { get; private set; }
    /// <summary>
    /// Gets or sets install FFmpeg.
    /// </summary>
    public bool InstallFfmpeg { get; private set; }
    /// <summary>
    /// Gets or sets skip FFmpeg.
    /// </summary>
    public bool SkipFfmpeg { get; private set; }
    /// <summary>
    /// Gets or sets check FFmpeg.
    /// </summary>
    public bool CheckFfmpeg { get; private set; }
    /// <summary>
    /// Gets or sets shortcut group name.
    /// </summary>
    public string ShortcutGroupName { get; private set; } = "PublisherStudio by Michi0403";
    /// <summary>
    /// Runs the parse operation.
    /// </summary>
    public static CliOptions Parse(string[] args)
    {
        List<string> argsList = args.ToList();
        var options = new CliOptions();
        if (argsList.Count == 0)
        {
            argsList.Add("--install-publisherstudio");
            argsList.Add("--update-publisherstudio");
            argsList.Add("--install-ffmpeg");
            argsList.Add("--start-publisherstudio");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                argsList.Add("--shortcuts");
        }
        for (var i = 0; i < argsList.Count; i++)
        {
            var arg = argsList[i];
            switch (arg.ToLowerInvariant().TrimStart())
            {
                case "-h":
                case "--help":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--install":
                case "--install-publisherstudio":
                case "--install-blazorpublisher":
                    options.InstallPublisherStudio = true;
                    break;
                case "--update":
                case "--update-publisherstudio":
                case "--update-blazorpublisher":
                    options.UpdatePublisherStudio = true;
                    break;
                case "--start":
                case "--start-publisherstudio":
                case "--start-blazorpublisher":
                    options.StartPublisherStudio = true;
                    break;
                case "--wait":
                case "--pause":
                    options.WaitOnExit = true;
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "--all":
                    options.InstallPublisherStudio = true;
                    options.UpdatePublisherStudio = true;
                    options.StartPublisherStudio = true;
                    options.DesktopShortcuts = true;
                    options.StartMenuShortcuts = true;
                    options.InstallFfmpeg = true;
                    break;
                case "--install-ffmpeg":
                    options.InstallFfmpeg = true;
                    options.SkipFfmpeg = false;
                    break;
                case "--skip-ffmpeg":
                case "--no-ffmpeg":
                    options.SkipFfmpeg = true;
                    options.InstallFfmpeg = false;
                    break;
                case "--check-ffmpeg":
                    options.CheckFfmpeg = true;
                    break;
                case "--publisherstudio-zip":
                case "--blazorpublisher-zip":
                    options.PublisherStudioZipPath = NextValue(argsList, ref i, arg);
                    break;
                case "--publisherstudio-setup-zip":
                case "--blazorpublisher-setup-zip":
                    options.PublisherStudioSetupZipPath = NextValue(argsList, ref i, arg);
                    break;
                case "--publisherstudio-exe":
                case "--blazorpublisher-exe":
                    options.PublisherStudioExePath = NextValue(argsList, ref i, arg);
                    break;
                case "--desktop-shortcuts":
                    options.DesktopShortcuts = true;
                    break;

                case "--startmenu-shortcuts":
                    options.StartMenuShortcuts = true;
                    break;
                case "--shortcut-group-name":
                case "--startmenu-name":
                    options.ShortcutGroupName = NextValue(argsList, ref i, arg);
                    break;
                case "--shortcuts":
                    options.DesktopShortcuts = true;
                    options.StartMenuShortcuts = true;
                    break;
                case "--port":
                    options.PublisherStudioPort = int.Parse(NextValue(argsList, ref i, arg));
                    if (options.PublisherStudioPort <= 0 || options.PublisherStudioPort > 65535)
                        throw new ArgumentOutOfRangeException(nameof(options.PublisherStudioPort), "Port must be between 1 and 65535.");
                    break;

                case "--no-browser":
                    options.OpenBrowser = false;
                    break;

                case "--force-delete":
                    options.ForceDelete = true;
                    break;

                case "--uninstall":
                    options.Uninstall = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}. Use --help.");
            }
        }

        if (argsList.Count == 0)
            options.ShowHelp = true;

        return options;
    }
    /// <summary>
    /// Runs the to string operation.
    /// </summary>
    public override string ToString()
    {
        return string.Join(Environment.NewLine,
        [
            $"{nameof(ShowHelp)}={ShowHelp}",
        $"{nameof(InstallPublisherStudio)}={InstallPublisherStudio}",
        $"{nameof(UpdatePublisherStudio)}={UpdatePublisherStudio}",
        $"{nameof(StartPublisherStudio)}={StartPublisherStudio}",
        $"{nameof(ForceDelete)}={ForceDelete}",
        $"{nameof(Verbose)}={Verbose}",
        $"{nameof(PublisherStudioZipPath)}={PublisherStudioZipPath}",
        $"{nameof(PublisherStudioSetupZipPath)}={PublisherStudioSetupZipPath}",
        $"{nameof(PublisherStudioExePath)}={PublisherStudioExePath}",
        $"{nameof(PublisherStudioPort)}={PublisherStudioPort}",
        $"{nameof(OpenBrowser)}={OpenBrowser}",
        $"{nameof(WaitOnExit)}={WaitOnExit}",
        $"{nameof(Uninstall)}={Uninstall}",
        $"{nameof(DesktopShortcuts)}={DesktopShortcuts}",
        $"{nameof(StartMenuShortcuts)}={StartMenuShortcuts}",
        $"{nameof(InstallFfmpeg)}={InstallFfmpeg}",
        $"{nameof(SkipFfmpeg)}={SkipFfmpeg}",
        $"{nameof(CheckFfmpeg)}={CheckFfmpeg}",
        $"{nameof(ShortcutGroupName)}={ShortcutGroupName}"
        ]);
    }
    /// <summary>
    /// Runs the print help operation.
    /// </summary>
    public static void PrintHelp(ILogger logger)
    {
        logger.LogInformation("""
PublisherStudio setup helper

Usage:
  PublisherStudio.Setup [options]

Double-click behavior:
  Installs or updates PublisherStudio in %LOCALAPPDATA%\PublisherStudio,
  ensures FFmpeg is available, creates Desktop and Start Menu shortcuts,
  and starts PublisherStudio.

Common examples:
  PublisherStudio.Setup --install-publisherstudio --start-publisherstudio --shortcuts
  PublisherStudio.Setup --update-publisherstudio --start-publisherstudio --shortcuts
  PublisherStudio.Setup --start-publisherstudio --port 58071
  PublisherStudio.Setup --uninstall --force-delete

Options:
  --install-publisherstudio          Download and install the latest PublisherStudio release.
  --update-publisherstudio           Download and extract the latest application and setup release over the PublisherStudio AppData installation.
  --start-publisherstudio            Start PublisherStudio.Web from %LOCALAPPDATA%\PublisherStudio.
  --install-ffmpeg                   Check for FFmpeg and install it with an available OS package manager.
  --check-ffmpeg                     Report whether FFmpeg is available without installing it.
  --skip-ffmpeg                      Skip the automatic FFmpeg check/install during application installation or update.
  --publisherstudio-zip <path>       Override the local PublisherStudio application ZIP download path.
  --publisherstudio-setup-zip <path> Override the local PublisherStudio setup ZIP download path.
  --publisherstudio-exe <path>       Override the PublisherStudio.Web executable path.
  --port <number>                    Port for PublisherStudio. Default: 58071.
  --wait                             Keep the setup console open after command-line execution.
  --no-browser                       Start PublisherStudio without opening the browser.
  --force-delete                     Delete the existing PublisherStudio AppData folder before installation, or confirm uninstall deletion.
  --all                              Install/update PublisherStudio, ensure FFmpeg, create shortcuts, and start PublisherStudio.
  --verbose                          Print full exception details on failure.
  --help                             Show this help.
  --desktop-shortcuts                Create Desktop shortcuts for Install, Update, Start, and the PublisherStudio folder.
  --startmenu-shortcuts              Create Start Menu shortcuts for Install, Update, Start, and the PublisherStudio folder.
  --shortcuts                        Create both Desktop and Start Menu shortcuts.
  --uninstall                        Preview PublisherStudio uninstall. Shows what would be removed, deletes nothing.
  --uninstall --force-delete         Actually remove PublisherStudio files and shortcuts.

Compatibility aliases using the former --*-blazorpublisher names remain accepted.
""");
    }

    private static string NextValue(List<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
            throw new ArgumentException($"Missing value for {optionName}.");
        return args[++index];
    }
}
