using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PublisherStudio.InstallerConsole.Installation;

internal sealed record PublisherStudioInstallLayout(
    string RootDirectory,
    string ApplicationDirectory,
    string SetupDirectory,
    string UpdateDirectory,
    string RuntimeFolderName,
    string SetupFolderName,
    string RuntimeIdentifier)
{
    private static readonly string[] KnownRuntimeFolders =
    [
        "winx64", "winx86", "winarm64",
        "linx64", "linarm64",
        "macosx64", "macosarm64"
    ];

    public static PublisherStudioInstallLayout Resolve(ILogger logger)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            throw new InvalidOperationException("LOCALAPPDATA could not be resolved.");

        var canonical = Path.Combine(localAppData, "Programs", "PublisherStudio");
        var legacyProgram = Path.Combine(localAppData, "Programs", "BlazorPublisher");
        var legacyLocal = Path.Combine(localAppData, "BlazorPublisher");
        var candidates = new[] { canonical, legacyProgram, legacyLocal };
        var root = Array.Find(candidates, ContainsInstallation)
            ?? Array.Find(candidates, Directory.Exists)
            ?? canonical;

        if (!string.Equals(root, canonical, StringComparison.OrdinalIgnoreCase))
            logger.LogInformation("Using existing legacy PublisherStudio install root {InstallRoot}. New installations use {CanonicalRoot}.", root, canonical);

        var preferredRuntimeFolder = GetPreferredRuntimeFolderName();
        var preferredSetupFolder = $"setup{preferredRuntimeFolder}";
        var runningSetupFolder = TryResolveRunningSetupFolder(root);
        var existingSetupFolder = runningSetupFolder
            ?? FindExistingSetupFolder(root, preferredSetupFolder)
            ?? preferredSetupFolder;

        var setupSuffix = existingSetupFolder.StartsWith("setup", StringComparison.OrdinalIgnoreCase)
            ? existingSetupFolder["setup".Length..]
            : string.Empty;
        var matchingRuntimeFolder = KnownRuntimeFolders.FirstOrDefault(folder =>
            string.Equals(folder, setupSuffix, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(Path.Combine(root, folder)));
        var existingRuntimeFolder = matchingRuntimeFolder
            ?? FindExistingRuntimeFolder(root, preferredRuntimeFolder)
            ?? preferredRuntimeFolder;

        var runtimeDirectory = Path.Combine(root, existingRuntimeFolder);
        var setupDirectory = Path.Combine(root, existingSetupFolder);

        // A short-lived repair candidate used app/setup. Keep those installations updateable
        // without moving or deleting their application directory or launcher directory.
        var compatibilityApplicationDirectory = Path.Combine(root, "app");
        if (!Directory.Exists(runtimeDirectory) && Directory.Exists(compatibilityApplicationDirectory))
        {
            runtimeDirectory = compatibilityApplicationDirectory;
            existingRuntimeFolder = "app";
            logger.LogInformation("Using compatibility application directory {ApplicationDirectory}.", runtimeDirectory);
        }

        var compatibilitySetupDirectory = Path.Combine(root, "setup");
        if (!Directory.Exists(setupDirectory) && Directory.Exists(compatibilitySetupDirectory))
        {
            setupDirectory = compatibilitySetupDirectory;
            existingSetupFolder = "setup";
            logger.LogInformation("Using compatibility setup directory {SetupDirectory}.", setupDirectory);
        }

        var runtimeIdentifier = RuntimeIdentifierFromFolder(existingRuntimeFolder, preferredRuntimeFolder);
        return new PublisherStudioInstallLayout(
            root,
            runtimeDirectory,
            setupDirectory,
            Path.Combine(root, ".updates"),
            existingRuntimeFolder,
            existingSetupFolder,
            runtimeIdentifier);
    }

    public string ApplicationExecutableName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "PublisherStudio.Web.exe"
        : "PublisherStudio.Web";

    public string SetupExecutableName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "PublisherStudio.Setup.exe"
        : "PublisherStudio.Setup";

    private static bool ContainsInstallation(string root)
    {
        if (!Directory.Exists(root)) return false;
        if (Directory.Exists(Path.Combine(root, "app")) || Directory.Exists(Path.Combine(root, "setup"))) return true;
        return Directory.EnumerateDirectories(root)
            .Select(Path.GetFileName)
            .Any(name => !string.IsNullOrWhiteSpace(name)
                && (KnownRuntimeFolders.Contains(name, StringComparer.OrdinalIgnoreCase)
                    || (name.StartsWith("setup", StringComparison.OrdinalIgnoreCase)
                        && KnownRuntimeFolders.Contains(name["setup".Length..], StringComparer.OrdinalIgnoreCase))));
    }

    private static string? TryResolveRunningSetupFolder(string root)
    {
        var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var runningPath = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!runningPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
        var name = Path.GetFileName(runningPath);
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (string.Equals(name, "setup", StringComparison.OrdinalIgnoreCase)) return name;
        return name.StartsWith("setup", StringComparison.OrdinalIgnoreCase)
               && KnownRuntimeFolders.Contains(name["setup".Length..], StringComparer.OrdinalIgnoreCase)
            ? name
            : null;
    }

    private static string? FindExistingSetupFolder(string root, string preferred)
    {
        if (Directory.Exists(Path.Combine(root, preferred))) return preferred;
        var platformPrefix = PlatformPrefixFromFolder(preferred);
        foreach (var runtimeFolder in KnownRuntimeFolders.Where(folder => folder.StartsWith(platformPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var name = $"setup{runtimeFolder}";
            var directory = Path.Combine(root, name);
            if (File.Exists(Path.Combine(directory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "PublisherStudio.Setup.exe" : "PublisherStudio.Setup"))
                || Directory.Exists(directory))
                return name;
        }
        return null;
    }

    private static string? FindExistingRuntimeFolder(string root, string preferred)
    {
        if (Directory.Exists(Path.Combine(root, preferred))) return preferred;
        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "PublisherStudio.Web.exe" : "PublisherStudio.Web";
        var platformPrefix = PlatformPrefixFromFolder(preferred);
        foreach (var name in KnownRuntimeFolders.Where(folder => folder.StartsWith(platformPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var directory = Path.Combine(root, name);
            if (File.Exists(Path.Combine(directory, executable)) || Directory.Exists(directory)) return name;
        }
        return null;
    }

    private static string PlatformPrefixFromFolder(string folder)
    {
        if (folder.StartsWith("win", StringComparison.OrdinalIgnoreCase)) return "win";
        if (folder.StartsWith("lin", StringComparison.OrdinalIgnoreCase)) return "lin";
        if (folder.StartsWith("macos", StringComparison.OrdinalIgnoreCase)) return "macos";
        throw new PlatformNotSupportedException($"PublisherStudio runtime folder '{folder}' is not recognized.");
    }

    private static string GetPreferredRuntimeFolderName()
    {
        var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "lin"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "macos"
                    : throw new PlatformNotSupportedException("PublisherStudio setup does not support this operating system.");
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"PublisherStudio setup does not support architecture {RuntimeInformation.OSArchitecture}.")
        };
        return $"{platform}{architecture}";
    }

    private static string RuntimeIdentifierFromFolder(string runtimeFolder, string preferredRuntimeFolder)
    {
        var effective = string.Equals(runtimeFolder, "app", StringComparison.OrdinalIgnoreCase)
            ? preferredRuntimeFolder
            : runtimeFolder;
        if (effective.StartsWith("win", StringComparison.OrdinalIgnoreCase)) return $"win-{effective[3..]}";
        if (effective.StartsWith("lin", StringComparison.OrdinalIgnoreCase)) return $"linux-{effective[3..]}";
        if (effective.StartsWith("macos", StringComparison.OrdinalIgnoreCase)) return $"osx-{effective[5..]}";
        throw new PlatformNotSupportedException($"PublisherStudio runtime folder '{runtimeFolder}' is not recognized.");
    }
}
