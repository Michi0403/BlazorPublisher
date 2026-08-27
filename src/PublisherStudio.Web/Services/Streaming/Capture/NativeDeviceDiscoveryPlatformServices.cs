using System.Diagnostics;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Streaming.Encoding;

namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Defines host-specific native media-device discovery so common capture workflows do not know
/// about DirectShow, AVFoundation, Linux /dev nodes, or Windows process-loopback enumeration.
/// </summary>
public interface INativeDeviceDiscoveryPlatformService
{
    /// <summary>
    /// Performs discover as part of the native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the native device discovery platform operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken);
}

/// <summary>Windows DirectShow and process-loopback discovery.</summary>
/// <param name="ffmpegLocator">Ffmpeg locator value supplied to the windows native device discovery platform operation and used when producing its result.</param>
/// <param name="runtimePatterns">Publisher runtime pattern service dependency used by the windows native device discovery platform workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WindowsNativeDeviceDiscoveryPlatformService(
    FfmpegLocator ffmpegLocator,
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<WindowsNativeDeviceDiscoveryPlatformService> logger) : INativeDeviceDiscoveryPlatformService
{
    /// <summary>
    /// Performs discover as part of the windows native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the windows native device discovery platform operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = new List<DiscoveredNativeMediaDeviceInfo>();
            result.AddRange(await DiscoverDirectShowAsync(ffmpegPath, cancellationToken).ConfigureAwait(false));
            result.AddRange(DiscoverProcessLoopbackTargets());
            return result
                .GroupBy(item => $"{item.Backend}|{item.Kind}|{item.Id}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Windows native-device discovery failed.");
            return [];
        }
    }

    /// <summary>
    /// Discovers direct show as part of the windows native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the windows native device discovery platform operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverDirectShowAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunFfmpegAsync(
                ffmpegPath,
                ["-hide_banner", "-list_devices", "true", "-f", "dshow", "-i", "dummy"],
                cancellationToken).ConfigureAwait(false);
            var result = new List<DiscoveredNativeMediaDeviceInfo>();
            var kind = string.Empty;
            var pattern = runtimePatterns.GetRegex(PublisherRuntimePattern.NativeDirectShowDevice);
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Contains("DirectShow video devices", StringComparison.OrdinalIgnoreCase))
                {
                    kind = "CaptureDevice";
                    continue;
                }
                if (line.Contains("DirectShow audio devices", StringComparison.OrdinalIgnoreCase))
                {
                    kind = "Microphone";
                    continue;
                }
                if (line.Contains("Alternative name", StringComparison.OrdinalIgnoreCase)) continue;
                var match = pattern.Match(line);
                if (kind.Length == 0 || !match.Success) continue;
                var name = match.Groups["name"].Value;
                result.Add(new DiscoveredNativeMediaDeviceInfo(name, name, kind, "dshow"));
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "DirectShow device discovery failed.");
            return [];
        }
    }

    /// <summary>
    /// Discovers process loopback targets as part of the windows native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<DiscoveredNativeMediaDeviceInfo> DiscoverProcessLoopbackTargets()
    {
        var result = new List<DiscoveredNativeMediaDeviceInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id <= 4 || string.IsNullOrWhiteSpace(process.ProcessName)) continue;
                var title = process.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title)) continue;
                var processId = process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                result.Add(new DiscoveredNativeMediaDeviceInfo(
                    processId,
                    process.ProcessName,
                    "ApplicationAudio",
                    "wasapi-process-loopback",
                    processId,
                    title));
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Could not inspect process {ProcessId} during Windows capture discovery.", process.Id);
            }
            finally
            {
                process.Dispose();
            }
        }
        return result;
    }

    /// <summary>
    /// Performs run FFmpeg as part of the windows native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the windows native device discovery platform operation and used when producing its result.</param>
    /// <param name="arguments">String dependency used by the windows native device discovery platform workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> RunFfmpegAsync(
        string? ffmpegPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var executable = ffmpegLocator.Resolve(ffmpegPath);
            if (executable is null) return string.Empty;
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo);
            if (process is null) return string.Empty;
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return (await stdout.ConfigureAwait(false)) + "\n" + (await stderr.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Windows native-device FFmpeg discovery failed.");
            return string.Empty;
        }
    }
}

/// <summary>macOS AVFoundation and Linux V4L2 discovery.</summary>
/// <param name="ffmpegLocator">Ffmpeg locator value supplied to the unix native device discovery platform operation and used when producing its result.</param>
/// <param name="runtimePatterns">Publisher runtime pattern service dependency used by the unix native device discovery platform workflow to provide the corresponding application capability.</param>
/// <param name="platform">Publisher platform runtime service dependency used by the unix native device discovery platform workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixNativeDeviceDiscoveryPlatformService(
    FfmpegLocator ffmpegLocator,
    IPublisherRuntimePatternService runtimePatterns,
    IPublisherPlatformRuntimeService platform,
    ILogger<UnixNativeDeviceDiscoveryPlatformService> logger) : INativeDeviceDiscoveryPlatformService
{
    /// <summary>
    /// Performs discover as part of the unix native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the unix native device discovery platform operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<DiscoveredNativeMediaDeviceInfo> result = platform.HostPlatform switch
            {
                PublisherHostPlatformKind.MacOS => await DiscoverAvFoundationAsync(ffmpegPath, cancellationToken).ConfigureAwait(false),
                PublisherHostPlatformKind.Linux => platform.EnumerateNativeVideoDevicePaths()
                    .Select(path => new DiscoveredNativeMediaDeviceInfo(path, Path.GetFileName(path), "CaptureDevice", "v4l2"))
                    .ToArray(),
                _ => []
            };

            return result
                .GroupBy(item => $"{item.Backend}|{item.Kind}|{item.Id}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unix native-device discovery failed on {HostPlatform}.", platform.HostPlatform);
            return [];
        }
    }

    /// <summary>
    /// Discovers av foundation as part of the unix native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the unix native device discovery platform operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAvFoundationAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunFfmpegAsync(
                ffmpegPath,
                ["-hide_banner", "-f", "avfoundation", "-list_devices", "true", "-i", string.Empty],
                cancellationToken).ConfigureAwait(false);
            var result = new List<DiscoveredNativeMediaDeviceInfo>();
            var kind = string.Empty;
            var pattern = runtimePatterns.GetRegex(PublisherRuntimePattern.NativeAvFoundationDevice);
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Contains("AVFoundation video devices", StringComparison.OrdinalIgnoreCase))
                {
                    kind = "CaptureDevice";
                    continue;
                }
                if (line.Contains("AVFoundation audio devices", StringComparison.OrdinalIgnoreCase))
                {
                    kind = "Microphone";
                    continue;
                }
                var match = pattern.Match(line);
                if (kind.Length == 0 || !match.Success) continue;
                result.Add(new DiscoveredNativeMediaDeviceInfo(
                    match.Groups["index"].Value,
                    match.Groups["name"].Value.Trim(),
                    kind,
                    "avfoundation"));
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "AVFoundation device discovery failed.");
            return [];
        }
    }

    /// <summary>
    /// Performs run FFmpeg as part of the unix native device discovery platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the unix native device discovery platform operation and used when producing its result.</param>
    /// <param name="arguments">String dependency used by the unix native device discovery platform workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> RunFfmpegAsync(
        string? ffmpegPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var executable = ffmpegLocator.Resolve(ffmpegPath);
            if (executable is null) return string.Empty;
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo);
            if (process is null) return string.Empty;
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return (await stdout.ConfigureAwait(false)) + "\n" + (await stderr.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unix native-device FFmpeg discovery failed.");
            return string.Empty;
        }
    }
}
