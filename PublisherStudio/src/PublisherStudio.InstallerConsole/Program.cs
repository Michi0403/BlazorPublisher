using PublisherStudio.Helper;
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

internal static class Program
{
    private const string BlazorPublisherRepo = "Michi0403/BlazorPublisher";
    private const string BlazorPublisherZipName = "BlazorPublisherByMichi0403.zip";
    private const string BlazorPublisherSetupZipName = "BlazorPublisherSetupByMichi0403.zip";
    private static readonly HttpClient Http = CreateHttpClient();

    public static async Task<int> Main(string[] args)
    {
        var launchedByDoubleClick = args.Length == 0 && Environment.UserInteractive;

        Console.WriteLine($"Your args to string {ArgsToString(args)}");
        var options = CliOptions.Parse(args);
        if(args.Length<=0)
        {
            Console.WriteLine($"args were initially empty !");
        }
        Console.WriteLine($"Parsed options:{Environment.NewLine}{options}");
        try
        {
            Console.WriteLine($"Starting RunAsync");
            return await RunAsync(args, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (launchedByDoubleClick | options.WaitOnExit)
            {
                Console.WriteLine($"Wait for Exit send me to Doomland.");
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
        
    }
    private static string ArgsToString(string[]? args)
    {
        if (args is null)
            return "args=null";

        if (args.Length == 0)
            return "args=[]";

        var builder = new StringBuilder();
        builder.AppendLine($"args.Length={args.Length}");

        for (var i = 0; i < args.Length; i++)
            builder.AppendLine($"args[{i}]=\"{args[i]}\"");

        return builder.ToString().TrimEnd();
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
                    UninstallBlazorPublisherWindows(options, logger);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UninstallBlazorPublisherWindows.");
            }

            try
            {
                try
                {
                    if (options.InstallBlazorPublisher || options.UpdateBlazorPublisher)
                        await InstallBlazorPublisherAsync(options, logger).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in InstallBlazorPublisher.");
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
                    if (options.StartBlazorPublisher)
                        StartBlazorPublisher(options, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in StartBlazorPublisher.");
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

    private static async Task InstallBlazorPublisherAsync(CliOptions options, ILogger logger)
    {
        try
        {
            var zipPath = options.BlazorPublisherZipPath ?? Path.Combine(Environment.CurrentDirectory, BlazorPublisherZipName);

            await DownloadLatestReleaseAssetAsync(
                BlazorPublisherRepo,
                zipPath,
                logger,
                options,
                setupAsset: false).ConfigureAwait(false);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            var targetPath = Path.Combine(localAppData, "BlazorPublisher");

            if (options.ForceDelete)
                DeleteIfExists(targetPath, logger);

            Directory.CreateDirectory(targetPath);

            logger.LogInformation($"Extracting BlazorPublisher app '{zipPath}' to '{targetPath}'");
            ExtractZipWithFallback(zipPath, targetPath, logger);

            var setupZipPath = options.BlazorPublisherSetupZipPath ?? Path.Combine(Environment.CurrentDirectory, BlazorPublisherSetupZipName);

            await DownloadLatestReleaseAssetAsync(
                BlazorPublisherRepo,
                setupZipPath,
                logger,
                options,
                setupAsset: true).ConfigureAwait(false);

            logger.LogInformation($"Extracting BlazorPublisher setup/bootstrap '{setupZipPath}' to '{targetPath}'");
            ExtractZipWithFallback(setupZipPath, targetPath, logger);

            logger.LogDebug($"BlazorPublisher installed to '{targetPath}'.");
            logger.LogInformation($"BlazorPublisher app and setup/bootstrap files now reside in '{targetPath}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in InstallBlazorPublisherAsync. options {options}");
        }
    }

    private static void UninstallBlazorPublisherWindows(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(UninstallBlazorPublisherWindows), logger);

            var targets = GetBlazorPublisherUninstallTargets(options, logger);

            logger.LogWarning("BlazorPublisher uninstall preview:");

            foreach (var target in targets)
            {
                var exists = File.Exists(target) || Directory.Exists(target);
                logger.LogInformation($"{(exists ? "[exists]" : "[missing]")} {target}");
            }

            if (!options.ForceDelete)
            {
                logger.LogWarning("Dry run only. Nothing was deleted.");
                logger.LogWarning("Run again with --uninstall --force-delete to delete the listed BlazorPublisher files.");
                return;
            }

            logger.LogWarning("--force-delete was used. Removing listed BlazorPublisher files.");

            foreach (var target in targets)
            {
                DeleteIfExists(target, logger);
            }

            logger.LogInformation("BlazorPublisher uninstall finished.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in UninstallBlazorPublisherWindows. options {options.ToString()}");
        }
    }
    private static List<string> GetBlazorPublisherUninstallTargets(CliOptions options, ILogger logger)
    {
        try
        {
            var targets = new List<string>();

            var blazorPublisherRoot = GetBlazorPublisherInstallRoot(logger);
            targets.Add(blazorPublisherRoot);

            var startMenuFolder = GetStartMenuFolder(options,logger);
            targets.Add(startMenuFolder);

            var desktop = GetDesktopFolder(logger);

            var shortcutDefinitions = GetShortcutTargets(blazorPublisherRoot, logger);

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
            logger.LogError(ex, $"Error in GetBlazorPublisherUninstallTargets. options {options.ToString()}");
            return new List<string>();
        }
    }

    private static void ProvisionWindowsShortcuts(CliOptions options, ILogger logger)
    {
        try
        {
            EnsureWindowsOnly(nameof(ProvisionWindowsShortcuts), logger);

            var blazorPublisherRoot = GetBlazorPublisherInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(blazorPublisherRoot) || !Directory.Exists(blazorPublisherRoot))
                throw new DirectoryNotFoundException($"BlazorPublisher directory was not found: {blazorPublisherRoot}");

            logger.LogInformation($"Provisioning Windows shortcuts from BlazorPublisher directory: {blazorPublisherRoot}");

            var shortcuts = GetShortcutTargets(blazorPublisherRoot, logger);

            if (shortcuts.Count == 0)
            {
                logger.LogWarning($"No shortcut targets found in BlazorPublisher directory: {blazorPublisherRoot}");
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
    private static List<ShortcutDefinition> GetShortcutTargets(string blazorPublisherRoot, ILogger logger)
    {
        try
        {
            var shortcuts = new List<ShortcutDefinition>();

            AddCmdShortcutIfExists(
                shortcuts,
                blazorPublisherRoot,
                "Install.cmd",
                "BlazorPublisher Install.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                blazorPublisherRoot,
                "Update.cmd",
                "BlazorPublisher Update.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                blazorPublisherRoot,
                "Start.cmd",
                "BlazorPublisher Start.url",
                logger);

            AddCmdShortcutIfExists(
                shortcuts,
                blazorPublisherRoot,
                "Uninstall.cmd",
                "BlazorPublisher Uninstall.url",
                logger);

            return shortcuts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetShortcutTargets. blazorPublisherRoot {blazorPublisherRoot}");
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

            var blazorPublisherRoot = GetBlazorPublisherInstallRoot(logger);
            var iconPath = FindBlazorPublisherIcon(logger);

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
    private static string? FindBlazorPublisherIcon(ILogger logger)
    {
        try
        {
            var blazorPublisherRoot = GetBlazorPublisherInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(blazorPublisherRoot) || !Directory.Exists(blazorPublisherRoot))
            {
                logger.LogWarning($"BlazorPublisher root does not exist while resolving icon: {blazorPublisherRoot}");
                return null;
            }

            var knownCandidates = new[]
            {
            Path.Combine(blazorPublisherRoot, "PublisherStudio.ico"),
            Path.Combine(blazorPublisherRoot, "BlazorPublisher.ico"),
            Path.Combine(blazorPublisherRoot, GetRuntimeFolderName(), "PublisherStudio.ico")
        };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking BlazorPublisher icon candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved BlazorPublisher icon from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known PublisherStudio.ico paths failed. Searching recursively under: {blazorPublisherRoot}");

            var publisherIcon = EnumerateFilesSafe(blazorPublisherRoot, "PublisherStudio.ico", logger)
                .OrderBy(path => GetRelativePathDepth(blazorPublisherRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(publisherIcon) && File.Exists(publisherIcon))
            {
                logger.LogInformation($"Resolved BlazorPublisher PublisherStudio.ico recursively: {publisherIcon}");
                return publisherIcon;
            }

            var blazorPublisherIcon = EnumerateFilesSafe(blazorPublisherRoot, "BlazorPublisher.ico", logger)
                .OrderBy(path => GetRelativePathDepth(blazorPublisherRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(blazorPublisherIcon) && File.Exists(blazorPublisherIcon))
            {
                logger.LogInformation($"Resolved BlazorPublisher BlazorPublisher.ico recursively: {blazorPublisherIcon}");
                return blazorPublisherIcon;
            }

            logger.LogWarning($"Publisher icon not found. Falling back to any .ico under: {blazorPublisherRoot}");

            var anyIcon = EnumerateFilesSafe(blazorPublisherRoot, "*.ico", logger)
                .OrderBy(path => GetRelativePathDepth(blazorPublisherRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(anyIcon) && File.Exists(anyIcon))
            {
                logger.LogInformation($"Resolved BlazorPublisher icon recursively: {anyIcon}");
                return anyIcon;
            }

            logger.LogWarning($"No .ico file found under: {blazorPublisherRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FindBlazorPublisherIcon.");
            return null;
        }
    }
    private static string? FindBlazorPublisherFile(
    string blazorPublisherRoot,
    string fileName,
    ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blazorPublisherRoot) || !Directory.Exists(blazorPublisherRoot))
            {
                logger.LogWarning($"BlazorPublisher root does not exist while searching for file '{fileName}': {blazorPublisherRoot}");
                return null;
            }

            var directPath = Path.Combine(blazorPublisherRoot, fileName);

            logger.LogInformation($"Checking direct BlazorPublisher file candidate: {directPath}");

            if (File.Exists(directPath))
            {
                logger.LogInformation($"Resolved BlazorPublisher file from direct path: {directPath}");
                return directPath;
            }

            logger.LogWarning($"Direct BlazorPublisher file candidate not found. Searching recursively for '{fileName}' under: {blazorPublisherRoot}");

            var recursiveCandidate = EnumerateFilesSafe(blazorPublisherRoot, fileName, logger)
                .OrderBy(path => GetRelativePathDepth(blazorPublisherRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved BlazorPublisher file recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find '{fileName}' under: {blazorPublisherRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindBlazorPublisherFile. blazorPublisherRoot {blazorPublisherRoot} fileName {fileName}");
            return null;
        }
    }
    private static string? FindBlazorPublisherExecutable(CliOptions options, ILogger logger)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.BlazorPublisherExePath))
            {
                var explicitPath = Environment.ExpandEnvironmentVariables(options.BlazorPublisherExePath);

                logger.LogInformation($"Checking explicit BlazorPublisher executable path: {explicitPath}");

                if (File.Exists(explicitPath))
                    return Path.GetFullPath(explicitPath);

                logger.LogWarning($"--blazorpublisher-exe was provided but does not exist: {explicitPath}");
            }

            var blazorPublisherRoot = GetBlazorPublisherInstallRoot(logger);

            if (string.IsNullOrWhiteSpace(blazorPublisherRoot) || !Directory.Exists(blazorPublisherRoot))
            {
                logger.LogWarning($"BlazorPublisher root does not exist: {blazorPublisherRoot}");
                return null;
            }

            var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "PublisherStudio.Web.exe"
                : "PublisherStudio.Web";

            var knownCandidates = new[]
            {
            Path.Combine(blazorPublisherRoot, GetRuntimeFolderName(), executableName),
            Path.Combine(blazorPublisherRoot, executableName)
        };

            foreach (var candidate in knownCandidates)
            {
                logger.LogInformation($"Checking BlazorPublisher executable candidate: {candidate}");

                if (File.Exists(candidate))
                {
                    logger.LogInformation($"Resolved BlazorPublisher executable from known path: {candidate}");
                    return candidate;
                }
            }

            logger.LogWarning($"Known BlazorPublisher executable paths failed. Searching recursively under: {blazorPublisherRoot}");

            var recursiveCandidate = EnumerateFilesSafe(blazorPublisherRoot, executableName, logger)
                .OrderBy(path => GetRelativePathDepth(blazorPublisherRoot, path))
                .ThenBy(path => path.Length)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate) && File.Exists(recursiveCandidate))
            {
                logger.LogInformation($"Resolved BlazorPublisher executable recursively: {recursiveCandidate}");
                return recursiveCandidate;
            }

            logger.LogWarning($"Could not find {executableName} under: {blazorPublisherRoot}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in FindBlazorPublisherExecutable. options {options}");
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
    string blazorPublisherRoot,
    string cmdFileName,
    string shortcutName,
    ILogger logger)
    {
        try
        {
            var cmdPath = FindBlazorPublisherFile(blazorPublisherRoot, cmdFileName, logger);

            if (string.IsNullOrWhiteSpace(cmdPath) || !File.Exists(cmdPath))
            {
                logger.LogWarning($"Shortcut target CMD not found, skipping: {cmdFileName}");
                return;
            }

            var workingDirectory = Path.GetDirectoryName(cmdPath);

            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = blazorPublisherRoot;

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
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException($"{featureName} is Windows-only.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in EnsureWindowsOnly. featureName {featureName.ToString()}");
        }
    }

    private static string GetBlazorPublisherInstallRoot(ILogger logger)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

            return Path.Combine(localAppData, "BlazorPublisher");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetBlazorPublisherInstallRoot.  {ex.ToString()}");
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
                groupName = "BlazorPublisher by Michi0403";

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
                return "BlazorPublisher by Michi0403";

            var invalid = Path.GetInvalidFileNameChars();

            foreach (var ch in invalid)
                value = value.Replace(ch, '_');

            return value.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in SanitizeShortcutGroupName. value {value}");
            return "BlazorPublisher by Michi0403";
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

    private static void StartBlazorPublisher(CliOptions options, ILogger logger)
    {
        try
        {
            var exePath = FindBlazorPublisherExecutable(options, logger);


            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                throw new FileNotFoundException(
                    $"BlazorPublisher executable not found at '{exePath}'. Install it first or pass --blazorpublisher-exe.");

            var port = options.BlazorPublisherPort <= 0 ? 58071 : options.BlazorPublisherPort;
            var url = $"http://127.0.0.1:{port}";

            logger.LogInformation($"Starting BlazorPublisher: {exePath}");
            logger.LogInformation($"BlazorPublisher port: {port}");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                ArgumentList = { "--port", port.ToString() },
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            });

            Thread.Sleep(TimeSpan.FromSeconds(2));

            if (options.OpenBrowser)
            {
                logger.LogInformation($"Opening browser: {url}");
                OpenDefaultBrowser(url, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in StartBlazorPublisher. options {options}");
        }
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

    private static async Task DownloadLatestReleaseAssetAsync(
    string repo,
    string outFile,
    ILogger logger,
    CliOptions options,
    bool setupAsset)
    {
        try
        {
            ValidateRepo(repo, logger);
            var latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";
            using var stream = await Http.GetStreamAsync(latestUrl).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            var root = json.RootElement;
            var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : "unknown";
            logger.LogInformation($"Latest {repo} release: {tagName}");

            if (!root.TryGetProperty("assets", out var assets) || assets.GetArrayLength() == 0)
                throw new InvalidOperationException($"No downloadable release assets found for {repo}.");

            var platform = GetPlatformToken();
            var arch = GetArchitectureToken();

            JsonElement? selected = null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;

                var isPlatformMatch =
                    name.Contains(platform, StringComparison.OrdinalIgnoreCase)
                    && name.Contains(arch, StringComparison.OrdinalIgnoreCase);

                var isSetupAsset =
                    name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                logger.LogInformation(
                    $"Checking asset '{name}'. PlatformMatch={isPlatformMatch}, SetupAsset={isSetupAsset}, WantedSetupAsset={setupAsset}");

                if (isPlatformMatch && isSetupAsset == setupAsset)
                {
                    selected = asset;
                    break;
                }
            }

            if (selected is null)
            {
                logger.LogWarning(
                    $"No exact asset match found for setupAsset={setupAsset}, platform={platform}, arch={arch}. Falling back to first matching setup mode.");

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? string.Empty;

                    var isSetupAsset =
                        name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                    if (isSetupAsset == setupAsset)
                    {
                        selected = asset;
                        break;
                    }
                }
            }

            selected ??= assets.EnumerateArray().First();

            var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
            var assetName = selected.Value.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException($"Selected release asset for {repo} has no download URL.");

            logger.LogInformation($"Selected asset: {assetName}");
            logger.LogInformation($"Downloading {assetName} to {outFile}");

            await DownloadFileAsync(downloadUrl, outFile, logger, options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadLatestReleaseAssetAsync. repo {repo} outFile {outFile} setupAsset={setupAsset}");
            throw;
        }
    }

    private static async Task DownloadFileAsync(string url, string outFile, ILogger logger, CliOptions options)
    {
        try
        {
            const int maxAttempts = 3;
            var tempFile = outFile + ".part";

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(outFile));
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    if (File.Exists(tempFile))
                        File.Delete(tempFile);

                    logger.LogInformation($"Downloading attempt {attempt}/{maxAttempts}: {url}");
                    logger.LogInformation($"Target: {outFile}");

                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.UserAgent.ParseAdd("BlazorPublisherSetupTool/1.0");
                    request.Headers.Accept.ParseAdd("*/*");

                    using var response = await Http.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    logger.LogInformation(contentLength.HasValue
                        ? $"Remote size: {FormatBytes(contentLength.Value, logger)}"
                        : "Remote size: unknown");

                    long totalRead = 0;

                    await using (var input = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false))
                    await using (var output = new FileStream(
                        tempFile,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 4 * 1024 * 1024,
                        useAsync: true))
                    {
                        var buffer = new byte[4* 1024 * 1024];
                        var lastLog = DateTimeOffset.UtcNow;

                        while (true)
                        {
                            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token)
                                .ConfigureAwait(false);

                            if (read == 0)
                                break;

                            await output.WriteAsync(buffer.AsMemory(0, read), cts.Token)
                                .ConfigureAwait(false);

                            totalRead += read;

                            var now = DateTimeOffset.UtcNow;
                            if (now - lastLog >= TimeSpan.FromSeconds(5))
                            {
                                if (contentLength.HasValue && contentLength.Value > 0)
                                {
                                    var percent = totalRead * 100.0 / contentLength.Value;
                                    logger.LogInformation(
                                        $"Downloaded {FormatBytes(totalRead, logger)} / {FormatBytes(contentLength.Value, logger)} ({percent:F1}%)");
                                }
                                else
                                {
                                    logger.LogInformation($"Downloaded {FormatBytes(totalRead, logger)}");
                                }

                                lastLog = now;
                            }
                        }

                        await output.FlushAsync(cts.Token).ConfigureAwait(false);
                    }

                    if (!File.Exists(tempFile))
                        throw new FileNotFoundException($"Temporary download file does not exist after download: {tempFile}");

                    var actualSize = new FileInfo(tempFile).Length;

                    if (actualSize == 0)
                        throw new IOException("Downloaded file is empty.");

                    if (contentLength.HasValue && actualSize != contentLength.Value)
                    {
                        var missing = contentLength.Value - actualSize;
                        throw new IOException(
                            $"Incomplete download. Got {actualSize:N0} bytes, expected {contentLength.Value:N0} bytes. Missing {missing:N0} bytes.");
                    }

                    await MoveFileWithRetryAsync(tempFile, outFile, logger, options).ConfigureAwait(false);

                    logger.LogInformation($"Download complete: {outFile} ({FormatBytes(actualSize, logger)})");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Download attempt {attempt}/{maxAttempts} failed.");

                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch
                    {
                    }

                    if (attempt == maxAttempts)
                    {
                        logger.LogError(ex, $"Download failed permanently. url {url} outFile {outFile}");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DownloadFileAsync. url {url} outFile {outFile}");
        }


    }
    private static async Task MoveFileWithRetryAsync(string source, string destination, ILogger logger, CliOptions options)
    {
        try
        {
            for (var i = 1; i <= 10; i++)
            {
                try
                {
                    if (!File.Exists(source))
                        throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

                    if (File.Exists(destination))
                        File.Delete(destination);
                    if (options.ForceDelete)
                    {
                        DeleteIfExists(destination, logger);
                        File.Move(source, destination, overwrite: true);
                    }
                    else
                    {
                        File.Move(source, destination, overwrite: false);
                    }
                    return;
                }
                catch (IOException ex) when (i < 10)
                {
                    logger.LogWarning(ex, $"Move failed because file is locked. Retry {i}/10...");
                    await Task.Delay(300).ConfigureAwait(false);
                }
            }

            if (File.Exists(destination))
                File.Delete(destination);
            if (options.ForceDelete)
            {
                DeleteIfExists(destination, logger);
                File.Move(source, destination, overwrite: true);
            }
            else
            {
                File.Move(source, destination, overwrite: false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in MoveFileWithRetryAsync. source {source} destination {destination}");
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
            logger.LogError(ex, $"Error in ExtractZipWithFallback. zipPath {zipPath.ToString()} targetPath {targetPath.ToString()}");
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
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BlazorPublisherSetupTool", "1.0"));
            client.Timeout = TimeSpan.FromMinutes(20);
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
    public bool ShowHelp { get; private set; }
    public bool InstallBlazorPublisher { get; private set; }
    public bool UpdateBlazorPublisher { get; private set; }
    public bool StartBlazorPublisher { get; private set; }
    public bool Verbose { get; private set; }
    public string? BlazorPublisherZipPath { get; private set; }
    public string? BlazorPublisherSetupZipPath { get; private set; }
    public string? BlazorPublisherExePath { get; private set; }
    public int BlazorPublisherPort { get; private set; } = 58071;
    public bool OpenBrowser { get; private set; } = true;
    public bool ForceDelete { get; private set; }
    public bool WaitOnExit { get; private set; }
    public bool Uninstall { get; private set; }
    public bool DesktopShortcuts { get; private set; }
    public bool StartMenuShortcuts { get; private set; }
    public string ShortcutGroupName { get; private set; } = "BlazorPublisher by Michi0403";
    public static CliOptions Parse(string[] args)
    {
        List<string> argsList = args.ToList();
        var options = new CliOptions();
        if (argsList.Count == 0)
        {
            argsList.Add("--install-blazorpublisher");
            argsList.Add("--force-delete");
            argsList.Add("--start-blazorpublisher");
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
                case "--install-blazorpublisher":
                    options.InstallBlazorPublisher = true;
                    break;
                case "--update":
                case "--update-blazorpublisher":
                    options.UpdateBlazorPublisher = true;
                    break;
                case "--start":
                case "--start-blazorpublisher":
                    options.StartBlazorPublisher = true;
                    break;
                case "--wait":
                case "--pause":
                    options.WaitOnExit = true;
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "--all":
                    options.InstallBlazorPublisher = true;
                    options.StartBlazorPublisher = true;
                    options.DesktopShortcuts = true;
                    options.StartMenuShortcuts = true;
                    break;
                case "--blazorpublisher-zip":
                    options.BlazorPublisherZipPath = NextValue(argsList, ref i, arg);
                    break;
                case "--blazorpublisher-setup-zip":
                    options.BlazorPublisherSetupZipPath = NextValue(argsList, ref i, arg);
                    break;
                case "--blazorpublisher-exe":
                    options.BlazorPublisherExePath = NextValue(argsList, ref i, arg);
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
                    options.BlazorPublisherPort = int.Parse(NextValue(argsList, ref i, arg));
                    if (options.BlazorPublisherPort <= 0 || options.BlazorPublisherPort > 65535)
                        throw new ArgumentOutOfRangeException(nameof(options.BlazorPublisherPort), "Port must be between 1 and 65535.");
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
    public override string ToString()
    {
        return string.Join(Environment.NewLine,
        [
            $"{nameof(ShowHelp)}={ShowHelp}",
        $"{nameof(InstallBlazorPublisher)}={InstallBlazorPublisher}",
        $"{nameof(UpdateBlazorPublisher)}={UpdateBlazorPublisher}",
        $"{nameof(StartBlazorPublisher)}={StartBlazorPublisher}",
        $"{nameof(ForceDelete)}={ForceDelete}",
        $"{nameof(Verbose)}={Verbose}",
        $"{nameof(BlazorPublisherZipPath)}={BlazorPublisherZipPath}",
        $"{nameof(BlazorPublisherSetupZipPath)}={BlazorPublisherSetupZipPath}",
        $"{nameof(BlazorPublisherExePath)}={BlazorPublisherExePath}",
        $"{nameof(BlazorPublisherPort)}={BlazorPublisherPort}",
        $"{nameof(OpenBrowser)}={OpenBrowser}",
        $"{nameof(WaitOnExit)}={WaitOnExit}",
        $"{nameof(Uninstall)}={Uninstall}",
        $"{nameof(DesktopShortcuts)}={DesktopShortcuts}",
        $"{nameof(StartMenuShortcuts)}={StartMenuShortcuts}",
        $"{nameof(ShortcutGroupName)}={ShortcutGroupName}"
        ]);
    }
    public static void PrintHelp(ILogger logger)
    {
        logger.LogInformation("""
BlazorPublisher setup helper

Usage:
  PublisherStudio.Setup [options]

Common examples:
  PublisherStudio.Setup --install-blazorpublisher --force-delete --start-blazorpublisher --shortcuts
  PublisherStudio.Setup --update-blazorpublisher --start-blazorpublisher --shortcuts
  PublisherStudio.Setup --start-blazorpublisher --port 58071
  PublisherStudio.Setup --uninstall --force-delete

Options:
  --install-blazorpublisher         Download and install the latest BlazorPublisher release.
  --update-blazorpublisher          Download and extract the latest BlazorPublisher application and setup release.
  --start-blazorpublisher           Start PublisherStudio.Web from %LOCALAPPDATA%\BlazorPublisher.
  --blazorpublisher-zip <path>      Override BlazorPublisher application ZIP download path.
  --blazorpublisher-setup-zip <path> Override BlazorPublisher setup ZIP download path.
  --blazorpublisher-exe <path>      Override PublisherStudio.Web executable path.
  --port <number>                   Port for BlazorPublisher. Default: 58071.
  --wait                            An option beside opening with mouse to keep it running.
  --no-browser                      Start BlazorPublisher without opening the browser.
  --force-delete                    Delete the existing installation before extracting. Not used by default.
  --all                             Install BlazorPublisher, create shortcuts, and start BlazorPublisher.
  --verbose                         Print full exception details on failure.
  --help                            Show this help.
  --desktop-shortcuts               Create Desktop shortcuts to selected BlazorPublisher command files.
  --startmenu-shortcuts             Create Start Menu shortcuts to selected BlazorPublisher command files.
  --shortcuts                       Create both Desktop and Start Menu shortcuts.
  --uninstall                       Preview BlazorPublisher uninstall. Shows what would be removed, deletes nothing.
  --uninstall --force-delete        Actually remove BlazorPublisher files and shortcuts.
""");
    }

    private static string NextValue(List<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
            throw new ArgumentException($"Missing value for {optionName}.");
        return args[++index];
    }
}
