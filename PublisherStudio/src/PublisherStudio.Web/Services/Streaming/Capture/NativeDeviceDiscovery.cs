using System.Diagnostics;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Streaming.Encoding;

namespace PublisherStudio.Services.Streaming.Capture;

public sealed record DiscoveredNativeMediaDeviceInfo(
    string Id,
    string Name,
    string Kind,
    string Backend,
    string? ProcessId = null,
    string? WindowTitle = null);

public sealed class NativeDeviceDiscovery(
    FfmpegLocator ffmpegLocator,
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<NativeDeviceDiscovery> logger)
{
    public async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = new List<DiscoveredNativeMediaDeviceInfo>();
            if (OperatingSystem.IsWindows())
            {
                result.AddRange(await DiscoverDirectShowAsync(ffmpegPath, cancellationToken).ConfigureAwait(false));
                result.AddRange(DiscoverWindowsProcesses());
            }
            else if (OperatingSystem.IsMacOS())
            {
                result.AddRange(await DiscoverAvFoundationAsync(ffmpegPath, cancellationToken).ConfigureAwait(false));
            }
            else if (OperatingSystem.IsLinux())
            {
                foreach (var path in Directory.Exists("/dev") ? Directory.EnumerateFiles("/dev", "video*") : [])
                    result.Add(new DiscoveredNativeMediaDeviceInfo(path, Path.GetFileName(path), "CaptureDevice", "v4l2"));
            }

            var discovered = result
                .GroupBy(item => $"{item.Backend}|{item.Kind}|{item.Id}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            logger.LogInformation($"Discovered {discovered.Length} native media devices.");
            return discovered;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not discover native media devices.");
            throw;
        }
    }

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
            logger.LogTrace($"Discovered {result.Count} DirectShow devices.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not discover DirectShow devices.");
            throw;
        }
    }

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
            logger.LogTrace($"Discovered {result.Count} AVFoundation devices.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not discover AVFoundation devices.");
            throw;
        }
    }

    private IReadOnlyList<DiscoveredNativeMediaDeviceInfo> DiscoverWindowsProcesses()
    {
        try
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
                    logger.LogDebug(exception, $"Could not inspect process {process.Id} during native-device discovery.");
                }
                finally
                {
                    process.Dispose();
                }
            }
            logger.LogTrace($"Discovered {result.Count} windowed Windows processes.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not discover Windows process capture targets.");
            throw;
        }
    }

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
            var output = (await stdout.ConfigureAwait(false)) + "\n" + (await stderr.ConfigureAwait(false));
            logger.LogTrace($"Executed FFmpeg native-device discovery with {arguments.Count} arguments.");
            return output;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not execute FFmpeg native-device discovery.");
            return string.Empty;
        }
    }
}
