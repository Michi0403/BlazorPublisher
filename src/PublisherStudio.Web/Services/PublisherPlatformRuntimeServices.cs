using System.Diagnostics;
using System.Runtime.InteropServices;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>Stable host platform identities used by platform-neutral PublisherStudio services.</summary>
public enum PublisherHostPlatformKind
{
    /// <summary>
    /// Selects the other option for <see cref="PublisherHostPlatformKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Other,
    /// <summary>
    /// Selects the windows option for <see cref="PublisherHostPlatformKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Windows,
    /// <summary>
    /// Selects the mac OS option for <see cref="PublisherHostPlatformKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MacOS,
    /// <summary>
    /// Selects the linux option for <see cref="PublisherHostPlatformKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Linux
}

/// <summary>
/// Provides operating-system-specific filesystem, executable-discovery, font, capture and permission
/// behavior behind one injected boundary. Common services consume this contract instead of branching
/// on the operating system directly.
/// </summary>
public interface IPublisherPlatformRuntimeService
{
    /// <summary>
    /// Gets the host platform value that forms part of the publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The host platform value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    PublisherHostPlatformKind HostPlatform { get; }
    /// <summary>
    /// Gets the path comparer used by this publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparer value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    StringComparer PathComparer { get; }
    /// <summary>
    /// Gets the path comparison used by this publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparison value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    StringComparison PathComparison { get; }
    /// <summary>
    /// Gets a value indicating whether global hotkeys applies to the publisher platform runtime state.
    /// </summary>
    /// <value>The supports global hotkeys value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    bool SupportsGlobalHotkeys { get; }
    /// <summary>
    /// Gets a value indicating whether process audio loopback applies to the publisher platform runtime state.
    /// </summary>
    /// <value>The supports process audio loopback value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    bool SupportsProcessAudioLoopback { get; }
    /// <summary>
    /// Gets the default native capture backend value that forms part of the publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default native capture backend value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    string DefaultNativeCaptureBackend { get; }
    /// <summary>
    /// Gets the preferred hardware encoder backends collection maintained or exposed by this publisher platform runtime instance for downstream processing.
    /// </summary>
    /// <value>The preferred hardware encoder backends value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    IReadOnlyList<string> PreferredHardwareEncoderBackends { get; }
    /// <summary>
    /// Gets the FFmpeg bundled path collection used by this publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg bundled path collection value exposed by <see cref="IPublisherPlatformRuntimeService"/>.</value>
    PublisherRuntimeCollection FfmpegBundledPathCollection { get; }

    /// <summary>
    /// Performs paths equal as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="left">Left value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool PathsEqual(string left, string right);
    /// <summary>
    /// Determines whether same or descendant path as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <param name="candidate">Candidate value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsSameOrDescendantPath(string root, string candidate);
    /// <summary>
    /// Determines whether FFmpeg executable name for host as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executableName">Executable name value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsFfmpegExecutableNameForHost(string executableName);
    /// <summary>
    /// Retrieves command extensions as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> GetCommandExtensions();
    /// <summary>
    /// Performs enumerate known FFmpeg install locations as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the publisher platform runtime workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    IEnumerable<string> EnumerateKnownFfmpegInstallLocations(IPublisherRuntimePolicyDataService runtimePolicy);
    /// <summary>
    /// Performs enumerate font directories as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> EnumerateFontDirectories();
    /// <summary>
    /// Performs enumerate platform font families as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> EnumeratePlatformFontFamilies();
    /// <summary>
    /// Performs enumerate native video device paths as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> EnumerateNativeVideoDevicePaths();
    /// <summary>
    /// Performs supports native capture backend as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="backend">Backend value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool SupportsNativeCaptureBackend(string backend);
    /// <summary>
    /// Performs restrict secret file permissions as part of the publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the publisher platform runtime operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    void RestrictSecretFilePermissions(string path, ILogger logger);
}

/// <summary>Windows implementation for PublisherStudio host-sensitive behavior.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WindowsPublisherPlatformRuntimeService(
    ILogger<WindowsPublisherPlatformRuntimeService> logger) : IPublisherPlatformRuntimeService
{
    /// <summary>
    /// Gets the host platform value that forms part of the windows publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The host platform value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public PublisherHostPlatformKind HostPlatform => PublisherHostPlatformKind.Windows;
    /// <summary>
    /// Gets the path comparer used by this windows publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparer value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
    /// <summary>
    /// Gets the path comparison used by this windows publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparison value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;
    /// <summary>
    /// Gets a value indicating whether global hotkeys applies to the windows publisher platform runtime state.
    /// </summary>
    /// <value>The supports global hotkeys value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public bool SupportsGlobalHotkeys => true;
    /// <summary>
    /// Gets a value indicating whether process audio loopback applies to the windows publisher platform runtime state.
    /// </summary>
    /// <value>The supports process audio loopback value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public bool SupportsProcessAudioLoopback => true;
    /// <summary>
    /// Gets the default native capture backend value that forms part of the windows publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default native capture backend value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public string DefaultNativeCaptureBackend => "dshow";
    /// <summary>
    /// Gets the preferred hardware encoder backends collection maintained or exposed by this windows publisher platform runtime instance for downstream processing.
    /// </summary>
    /// <value>The preferred hardware encoder backends value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public IReadOnlyList<string> PreferredHardwareEncoderBackends { get; } = ["nvenc", "qsv", "amf", "videotoolbox", "software"];
    /// <summary>
    /// Gets the FFmpeg bundled path collection used by this windows publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg bundled path collection value exposed by <see cref="WindowsPublisherPlatformRuntimeService"/>.</value>
    public PublisherRuntimeCollection FfmpegBundledPathCollection => PublisherRuntimeCollection.FfmpegWindowsBundledPaths;

    /// <summary>
    /// Performs paths equal as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="left">Left value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Determines whether same or descendant path as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <param name="candidate">Candidate value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Determines whether FFmpeg executable name for host as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executableName">Executable name value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Retrieves command extensions as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate known FFmpeg install locations as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the windows publisher platform runtime workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate font directories as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate platform font families as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate native video device paths as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs supports native capture backend as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="backend">Backend value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs restrict secret file permissions as part of the windows publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the windows publisher platform runtime operation and used when producing its result.</param>
    /// <param name="callerLogger">Logger dependency used by the windows publisher platform runtime workflow to provide the corresponding application capability.</param>
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
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixPublisherPlatformRuntimeService(
    ILogger<UnixPublisherPlatformRuntimeService> logger) : IPublisherPlatformRuntimeService
{
    /// <summary>
    /// Stores the internal is mac OS state used by <see cref="UnixPublisherPlatformRuntimeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    /// <summary>
    /// Stores the internal is linux state used by <see cref="UnixPublisherPlatformRuntimeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    /// <summary>
    /// Gets the host platform value that forms part of the unix publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The host platform value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public PublisherHostPlatformKind HostPlatform => isMacOS ? PublisherHostPlatformKind.MacOS : isLinux ? PublisherHostPlatformKind.Linux : PublisherHostPlatformKind.Other;
    /// <summary>
    /// Gets the path comparer used by this unix publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparer value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public StringComparer PathComparer => StringComparer.Ordinal;
    /// <summary>
    /// Gets the path comparison used by this unix publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path comparison value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public StringComparison PathComparison => StringComparison.Ordinal;
    /// <summary>
    /// Gets a value indicating whether global hotkeys applies to the unix publisher platform runtime state.
    /// </summary>
    /// <value>The supports global hotkeys value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public bool SupportsGlobalHotkeys => false;
    /// <summary>
    /// Gets a value indicating whether process audio loopback applies to the unix publisher platform runtime state.
    /// </summary>
    /// <value>The supports process audio loopback value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public bool SupportsProcessAudioLoopback => false;
    /// <summary>
    /// Gets the default native capture backend value that forms part of the unix publisher platform runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default native capture backend value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public string DefaultNativeCaptureBackend => isMacOS ? "avfoundation" : isLinux ? "v4l2" : "unknown";
    /// <summary>
    /// Gets the preferred hardware encoder backends collection maintained or exposed by this unix publisher platform runtime instance for downstream processing.
    /// </summary>
    /// <value>The preferred hardware encoder backends value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public IReadOnlyList<string> PreferredHardwareEncoderBackends => isMacOS
        ? ["videotoolbox", "nvenc", "qsv", "amf", "software"]
        : ["nvenc", "qsv", "amf", "videotoolbox", "software"];
    /// <summary>
    /// Gets the FFmpeg bundled path collection used by this unix publisher platform runtime instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg bundled path collection value exposed by <see cref="UnixPublisherPlatformRuntimeService"/>.</value>
    public PublisherRuntimeCollection FfmpegBundledPathCollection => PublisherRuntimeCollection.FfmpegUnixBundledPaths;

    /// <summary>
    /// Performs paths equal as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="left">Left value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Determines whether same or descendant path as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <param name="candidate">Candidate value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Determines whether FFmpeg executable name for host as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executableName">Executable name value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Retrieves command extensions as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate known FFmpeg install locations as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the unix publisher platform runtime workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate font directories as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate platform font families as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs enumerate native video device paths as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs supports native capture backend as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="backend">Backend value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs restrict secret file permissions as part of the unix publisher platform runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the unix publisher platform runtime operation and used when producing its result.</param>
    /// <param name="callerLogger">Logger dependency used by the unix publisher platform runtime workflow to provide the corresponding application capability.</param>
    public void RestrictSecretFilePermissions(string path, ILogger callerLogger)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                logger.LogTrace("Restricted Unix secret-file permissions for {SecretPath}.", path);
                return;
            }

            throw new PlatformNotSupportedException("Unix secret-file permissions are unavailable on Windows.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            callerLogger.LogWarning(exception, "Could not restrict private runtime file permissions at {PrivatePath}; private material was not logged.", path);
        }
    }
}
