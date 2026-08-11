using System.Diagnostics;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.Streaming.Encoding;

/// <summary>
/// Represents a FFmpeg locator.
/// </summary>
public sealed class FfmpegLocator(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<FfmpegLocator> logger)
{
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    public string? Resolve(string? configuredPath = null)
    {
        try
        {
            logger.LogTrace($"Resolving the FFmpeg executable.");
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                var expanded = Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"'));
                if (File.Exists(expanded)) return Path.GetFullPath(expanded);
                if (TryResolveCommand(expanded, out var configuredCommand)) return configuredCommand;
                return null;
            }

            var bundledPaths = runtimePolicy.GetCollection(
                OperatingSystem.IsWindows()
                    ? PublisherRuntimeCollection.FfmpegWindowsBundledPaths
                    : PublisherRuntimeCollection.FfmpegUnixBundledPaths);
            foreach (var relativePath in bundledPaths)
            {
                var candidate = Path.Combine(
                    AppContext.BaseDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }

            foreach (var candidate in KnownInstallLocations())
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);

            var command = runtimePolicy
                .GetCollection(PublisherRuntimeCollection.AllowedFfmpegExecutableNames)
                .FirstOrDefault(name => OperatingSystem.IsWindows()
                    ? name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    : !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrWhiteSpace(command) && TryResolveCommand(command, out var resolved)
                ? resolved
                : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve the FFmpeg executable.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether available.
    /// </summary>
    public bool IsAvailable(string? configuredPath = null)
    {
        try
        {
            var available = Resolve(configuredPath) is not null;
            logger.LogTrace($"FFmpeg availability was resolved as {available}.");
            return available;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not determine FFmpeg availability.");
            throw;
        }
    }

    /// <summary>
    /// Reads version async.
    /// </summary>
    public async Task<string?> ReadVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var executable = Resolve(configuredPath);
            if (executable is null) return null;
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { "-version" }
            });
            if (process is null) return null;
            var firstLine = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var result = string.IsNullOrWhiteSpace(firstLine) ? null : firstLine.Trim();
            logger.LogTrace($"Read the FFmpeg version from '{executable}'.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read the FFmpeg version.");
            return null;
        }
    }

    /// <summary>
    /// Runs the known install locations operation.
    /// </summary>
    private IEnumerable<string> KnownInstallLocations()
    {
        logger.LogTrace($"Enumerating known FFmpeg installation locations.");
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var chocolatey = Environment.GetEnvironmentVariable("ChocolateyInstall");
                if (!string.IsNullOrWhiteSpace(local))
                {
                    yield return Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe");
                    foreach (var candidate in FindWinGetPackageExecutables(local))
                        yield return candidate;
                }
                if (!string.IsNullOrWhiteSpace(profile))
                    yield return Path.Combine(profile, "scoop", "shims", "ffmpeg.exe");
                if (!string.IsNullOrWhiteSpace(chocolatey))
                    yield return Path.Combine(chocolatey, "bin", "ffmpeg.exe");
                yield break;
            }

            foreach (var path in runtimePolicy.GetCollection(PublisherRuntimeCollection.FfmpegUnixInstallPaths))
                yield return path;
        }
        finally
        {
            logger.LogTrace($"Completed enumeration of known FFmpeg installation locations.");
        }
    }

    /// <summary>
    /// Finds win get package executables.
    /// </summary>
    private IEnumerable<string> FindWinGetPackageExecutables(string localAppData)
    {
        try
        {
            logger.LogTrace("Collecting WinGet FFmpeg package executables.");
            var packagesRoot = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
            if (!Directory.Exists(packagesRoot)) return Array.Empty<string>();

            string[] packageDirectories;
            try
            {
                packageDirectories = Directory
                    .EnumerateDirectories(packagesRoot, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly)
                    .ToArray();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not enumerate WinGet FFmpeg package directories.");
                return Array.Empty<string>();
            }

            var matches = new List<string>();
            foreach (var packageDirectory in packageDirectories)
            {
                try
                {
                    matches.AddRange(Directory
                        .EnumerateFiles(packageDirectory, "ffmpeg.exe", SearchOption.AllDirectories)
                        .OrderByDescending(path => path.Contains(
                            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.OrdinalIgnoreCase))
                        .Take(runtimePolicy.InstallerDownloadAttempts));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not enumerate FFmpeg executables under '{PackageDirectory}'.", packageDirectory);
                }
            }

            logger.LogTrace("Collected {ExecutableCount} WinGet FFmpeg executable candidates.", matches.Count);
            return matches;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not collect WinGet FFmpeg package executables.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to resolve command.
    /// </summary>
    private bool TryResolveCommand(string command, out string path)
    {
        try
        {
            path = string.Empty;
            if (Path.IsPathRooted(command) || command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar))
            {
                if (!File.Exists(command)) return false;
                path = Path.GetFullPath(command);
                logger.LogTrace($"Resolved rooted FFmpeg command '{path}'.");
                return true;
            }

            var extensions = OperatingSystem.IsWindows()
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : [string.Empty];
            var hasExtension = Path.HasExtension(command);
            foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                         .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (var extension in hasExtension ? [string.Empty] : extensions)
                {
                    var candidate = Path.Combine(directory.Trim('"'), command + extension);
                    if (!File.Exists(candidate)) continue;
                    path = candidate;
                    logger.LogTrace($"Resolved FFmpeg command '{command}' to '{path}'.");
                    return true;
                }
            }
            logger.LogTrace($"FFmpeg command '{command}' was not found.");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve FFmpeg command '{command}'.");
            path = string.Empty;
            return false;
        }
    }
}
