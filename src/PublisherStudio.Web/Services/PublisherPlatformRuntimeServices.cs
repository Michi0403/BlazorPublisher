using System.Diagnostics;
using System.Runtime.InteropServices;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>Stable host platform identities used by platform-neutral PublisherStudio services.</summary>
public enum PublisherHostPlatformKind
{
    Other,
    Windows,
    MacOS,
    Linux
}

/// <summary>
/// Provides operating-system-specific filesystem, executable-discovery, font, capture and permission
/// behavior behind one injected boundary. Common services consume this contract instead of branching
/// on the operating system directly.
/// </summary>
public interface IPublisherPlatformRuntimeService
{
    PublisherHostPlatformKind HostPlatform { get; }
    StringComparer PathComparer { get; }
    StringComparison PathComparison { get; }
    bool SupportsGlobalHotkeys { get; }
    bool SupportsProcessAudioLoopback { get; }
    string DefaultNativeCaptureBackend { get; }
    IReadOnlyList<string> PreferredHardwareEncoderBackends { get; }
    PublisherRuntimeCollection FfmpegBundledPathCollection { get; }

    bool PathsEqual(string left, string right);
    bool IsSameOrDescendantPath(string root, string candidate);
    bool IsFfmpegExecutableNameForHost(string executableName);
    IReadOnlyList<string> GetCommandExtensions();
    IEnumerable<string> EnumerateKnownFfmpegInstallLocations(IPublisherRuntimePolicyDataService runtimePolicy);
    IReadOnlyList<string> EnumerateFontDirectories();
    IReadOnlyList<string> EnumeratePlatformFontFamilies();
    IReadOnlyList<string> EnumerateNativeVideoDevicePaths();
    bool SupportsNativeCaptureBackend(string backend);
    void RestrictSecretFilePermissions(string path, ILogger logger);
}

/// <summary>Windows implementation for PublisherStudio host-sensitive behavior.</summary>
public sealed class WindowsPublisherPlatformRuntimeService(
    ILogger<WindowsPublisherPlatformRuntimeService> logger) : IPublisherPlatformRuntimeService
{
    public PublisherHostPlatformKind HostPlatform => PublisherHostPlatformKind.Windows;
    public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
    public StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;
    public bool SupportsGlobalHotkeys => true;
    public bool SupportsProcessAudioLoopback => true;
    public string DefaultNativeCaptureBackend => "dshow";
    public IReadOnlyList<string> PreferredHardwareEncoderBackends { get; } = ["nvenc", "qsv", "amf", "videotoolbox", "software"];
    public PublisherRuntimeCollection FfmpegBundledPathCollection => PublisherRuntimeCollection.FfmpegWindowsBundledPaths;

    public bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                PathComparison);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not compare Windows filesystem paths.");
            throw;
        }
    }

    public bool IsSameOrDescendantPath(string root, string candidate)
    {
        try
        {
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
            if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison)) return true;
            var rootPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(rootPrefix, PathComparison);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not validate Windows path containment for root {RootPath}.", root);
            throw;
        }
    }

    public bool IsFfmpegExecutableNameForHost(string executableName)
    {
        try
        {
            return executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not evaluate the Windows FFmpeg executable name.");
            throw;
        }
    }

    public IReadOnlyList<string> GetCommandExtensions()
    {
        try
        {
            return (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve Windows executable extensions.");
            throw;
        }
    }

    public IEnumerable<string> EnumerateKnownFfmpegInstallLocations(IPublisherRuntimePolicyDataService runtimePolicy)
    {
        try
        {
            var result = new List<string>();
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var chocolatey = Environment.GetEnvironmentVariable("ChocolateyInstall");
            if (!string.IsNullOrWhiteSpace(local))
            {
                result.Add(Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe"));
                var packagesRoot = Path.Combine(local, "Microsoft", "WinGet", "Packages");
                if (Directory.Exists(packagesRoot))
                {
                    try
                    {
                        result.AddRange(Directory.EnumerateDirectories(packagesRoot, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly)
                            .SelectMany(directory =>
                            {
                                try { return Directory.EnumerateFiles(directory, "ffmpeg.exe", SearchOption.AllDirectories); }
                                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                                {
                                    logger.LogDebug(exception, "Could not inspect WinGet FFmpeg package directory {PackageDirectory}.", directory);
                                    return [];
                                }
                            })
                            .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                            .Take(Math.Max(1, runtimePolicy.InstallerDownloadAttempts)));
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        logger.LogDebug(exception, "Could not enumerate WinGet FFmpeg package locations.");
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(profile)) result.Add(Path.Combine(profile, "scoop", "shims", "ffmpeg.exe"));
            if (!string.IsNullOrWhiteSpace(chocolatey)) result.Add(Path.Combine(chocolatey, "bin", "ffmpeg.exe"));
            logger.LogTrace("Resolved {CandidateCount} Windows FFmpeg installation candidate(s).", result.Count);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not enumerate Windows FFmpeg installation locations.");
            throw;
        }
    }

    public IReadOnlyList<string> EnumerateFontDirectories()
    {
        try
        {
            var result = new List<string>();
            var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");
            if (string.IsNullOrWhiteSpace(windowsDirectory))
            {
                var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
                windowsDirectory = string.IsNullOrWhiteSpace(systemDirectory) ? null : Directory.GetParent(systemDirectory)?.FullName;
            }
            if (!string.IsNullOrWhiteSpace(windowsDirectory)) result.Add(Path.Combine(windowsDirectory, "Fonts"));
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localData)) result.Add(Path.Combine(localData, "Microsoft", "Windows", "Fonts"));
            return result.Distinct(PathComparer).ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not enumerate Windows font directories.");
            throw;
        }
    }

    public IReadOnlyList<string> EnumeratePlatformFontFamilies()
    {
        try
        {
            logger.LogTrace("Windows font families are discovered from font files rather than a secondary native command.");
            return [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve the Windows platform font-family fallback.");
            throw;
        }
    }

    public IReadOnlyList<string> EnumerateNativeVideoDevicePaths()
    {
        try
        {
            logger.LogTrace("Windows native capture device discovery uses DirectShow rather than filesystem device nodes.");
            return [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve Windows native video device paths.");
            throw;
        }
    }

    public bool SupportsNativeCaptureBackend(string backend)
    {
        try
        {
            return backend.Equals("dshow", StringComparison.OrdinalIgnoreCase) ||
                   backend.Equals("wasapi-process-loopback", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not evaluate Windows native capture backend {Backend}.", backend);
            throw;
        }
    }

    public void RestrictSecretFilePermissions(string path, ILogger callerLogger)
    {
        try
        {
            logger.LogTrace("Windows secret file permissions remain governed by Windows ACL inheritance for {SecretPath}.", path);
        }
        catch (Exception exception)
        {
            callerLogger.LogError(exception, "Could not apply the Windows secret-file permission policy for {SecretPath}.", path);
            throw;
        }
    }
}

/// <summary>Unix implementation for macOS/Linux host-sensitive behavior.</summary>
public sealed class UnixPublisherPlatformRuntimeService(
    ILogger<UnixPublisherPlatformRuntimeService> logger) : IPublisherPlatformRuntimeService
{
    private readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public PublisherHostPlatformKind HostPlatform => isMacOS ? PublisherHostPlatformKind.MacOS : isLinux ? PublisherHostPlatformKind.Linux : PublisherHostPlatformKind.Other;
    public StringComparer PathComparer => StringComparer.Ordinal;
    public StringComparison PathComparison => StringComparison.Ordinal;
    public bool SupportsGlobalHotkeys => false;
    public bool SupportsProcessAudioLoopback => false;
    public string DefaultNativeCaptureBackend => isMacOS ? "avfoundation" : isLinux ? "v4l2" : "unknown";
    public IReadOnlyList<string> PreferredHardwareEncoderBackends => isMacOS
        ? ["videotoolbox", "nvenc", "qsv", "amf", "software"]
        : ["nvenc", "qsv", "amf", "videotoolbox", "software"];
    public PublisherRuntimeCollection FfmpegBundledPathCollection => PublisherRuntimeCollection.FfmpegUnixBundledPaths;

    public bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                PathComparison);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not compare Unix filesystem paths.");
            throw;
        }
    }

    public bool IsSameOrDescendantPath(string root, string candidate)
    {
        try
        {
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
            if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison)) return true;
            var rootPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(rootPrefix, PathComparison);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not validate Unix path containment for root {RootPath}.", root);
            throw;
        }
    }

    public bool IsFfmpegExecutableNameForHost(string executableName)
    {
        try
        {
            return !executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not evaluate the Unix FFmpeg executable name.");
            throw;
        }
    }

    public IReadOnlyList<string> GetCommandExtensions()
    {
        try
        {
            logger.LogTrace("Unix executable command resolution does not append PATHEXT suffixes.");
            return [string.Empty];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve Unix command extensions.");
            throw;
        }
    }

    public IEnumerable<string> EnumerateKnownFfmpegInstallLocations(IPublisherRuntimePolicyDataService runtimePolicy)
    {
        try
        {
            var locations = runtimePolicy.GetCollection(PublisherRuntimeCollection.FfmpegUnixInstallPaths);
            logger.LogTrace("Resolved {CandidateCount} Unix FFmpeg installation candidate(s).", locations.Count);
            return locations;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not enumerate Unix FFmpeg installation locations.");
            throw;
        }
    }

    public IReadOnlyList<string> EnumerateFontDirectories()
    {
        try
        {
            var result = new List<string>();
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (isMacOS)
            {
                result.Add("/System/Library/Fonts");
                result.Add("/Library/Fonts");
                if (!string.IsNullOrWhiteSpace(home)) result.Add(Path.Combine(home, "Library", "Fonts"));
            }
            else
            {
                result.Add("/usr/share/fonts");
                result.Add("/usr/local/share/fonts");
                if (!string.IsNullOrWhiteSpace(home))
                {
                    result.Add(Path.Combine(home, ".fonts"));
                    result.Add(Path.Combine(home, ".local", "share", "fonts"));
                }
            }
            return result.Distinct(PathComparer).ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not enumerate Unix font directories.");
            throw;
        }
    }

    public IReadOnlyList<string> EnumeratePlatformFontFamilies()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "fc-list",
                Arguments = "--format=%{family}\\n",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null) return [];

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(4000))
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception exception) { logger.LogDebug(exception, "Could not terminate the timed-out fontconfig process."); }
                return [];
            }

            var output = outputTask.GetAwaiter().GetResult();
            _ = errorTask.GetAwaiter().GetResult();
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .SelectMany(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogDebug(exception, "fontconfig is unavailable; PublisherStudio will use filesystem-discovered Unix fonts.");
            return [];
        }
    }

    public IReadOnlyList<string> EnumerateNativeVideoDevicePaths()
    {
        try
        {
            if (!isLinux || !Directory.Exists("/dev")) return [];
            return Directory.EnumerateFiles("/dev", "video*").ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Could not enumerate Linux /dev/video* capture devices.");
            return [];
        }
    }

    public bool SupportsNativeCaptureBackend(string backend)
    {
        try
        {
            return (isMacOS && backend.Equals("avfoundation", StringComparison.OrdinalIgnoreCase)) ||
                   (isLinux && backend.Equals("v4l2", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not evaluate Unix native capture backend {Backend}.", backend);
            throw;
        }
    }

    public void RestrictSecretFilePermissions(string path, ILogger callerLogger)
    {
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            logger.LogTrace("Restricted Unix secret-file permissions for {SecretPath}.", path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            callerLogger.LogWarning(exception, "Could not restrict private runtime file permissions at {PrivatePath}; private material was not logged.", path);
        }
    }
}
