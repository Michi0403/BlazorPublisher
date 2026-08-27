using System.Diagnostics;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.Streaming.Encoding;

/// <summary>
/// Represents a FFmpeg locator application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the FFmpeg locator workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class FfmpegLocator(
    IPublisherRuntimePolicyDataService runtimePolicy,
    IPublisherPlatformRuntimeService platform,
    ILogger<FfmpegLocator> logger)
{
    /// <summary>
    /// Performs resolve for <see cref="FfmpegLocator"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg locator workflow.
    /// </summary>
    /// <param name="configuredPath">Configured path value supplied to the FFmpeg locator operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

            var bundledPaths = runtimePolicy.GetCollection(platform.FfmpegBundledPathCollection);
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
                .FirstOrDefault(platform.IsFfmpegExecutableNameForHost);
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
    /// Determines whether available for <see cref="FfmpegLocator"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg locator workflow.
    /// </summary>
    /// <param name="configuredPath">Configured path value supplied to the FFmpeg locator operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Reads version for <see cref="FfmpegLocator"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg locator workflow.
    /// </summary>
    /// <param name="configuredPath">Configured path value supplied to the FFmpeg locator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Enumerates platform-owned FFmpeg installation locations without leaking host-specific path policy into the common locator.
    /// </summary>
    private IEnumerable<string> KnownInstallLocations()
    {
        try
        {
            logger.LogTrace("Enumerating known FFmpeg installation locations for {HostPlatform}.", platform.HostPlatform);
            return platform.EnumerateKnownFfmpegInstallLocations(runtimePolicy);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not enumerate host-specific FFmpeg installation locations.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to resolve command for <see cref="FfmpegLocator"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg locator workflow.
    /// </summary>
    /// <param name="command">Command value supplied to the FFmpeg locator operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the FFmpeg locator operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

            var extensions = platform.GetCommandExtensions();
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
