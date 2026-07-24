using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PublisherStudio.Backend.Streaming.Capture;

public sealed record DiscoveredNativeMediaDeviceInfo(string Id, string Name, string Kind, string Backend, string? ProcessId = null, string? WindowTitle = null);

public static class NativeDeviceDiscovery
{
    public static async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(string? ffmpegPath, CancellationToken cancellationToken)
    {
        var result = new List<DiscoveredNativeMediaDeviceInfo>();
        if (OperatingSystem.IsWindows())
        {
            result.AddRange(await DiscoverDirectShowAsync(ffmpegPath, cancellationToken));
            result.AddRange(DiscoverWindowsProcesses());
        }
        else if (OperatingSystem.IsMacOS())
        {
            result.AddRange(await DiscoverAvFoundationAsync(ffmpegPath, cancellationToken));
        }
        else if (OperatingSystem.IsLinux())
        {
            foreach (var path in Directory.Exists("/dev") ? Directory.EnumerateFiles("/dev", "video*") : [])
                result.Add(new DiscoveredNativeMediaDeviceInfo(path, Path.GetFileName(path), "CaptureDevice", "v4l2"));
        }
        return result
            .GroupBy(item => $"{item.Backend}|{item.Kind}|{item.Id}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverDirectShowAsync(string? ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFfmpegAsync(ffmpegPath, ["-hide_banner", "-list_devices", "true", "-f", "dshow", "-i", "dummy"], cancellationToken);
        var result = new List<DiscoveredNativeMediaDeviceInfo>();
        var kind = string.Empty;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Contains("DirectShow video devices", StringComparison.OrdinalIgnoreCase)) { kind = "CaptureDevice"; continue; }
            if (line.Contains("DirectShow audio devices", StringComparison.OrdinalIgnoreCase)) { kind = "Microphone"; continue; }
            if (line.Contains("Alternative name", StringComparison.OrdinalIgnoreCase)) continue;
            var match = Regex.Match(line, "\\\"(?<name>[^\\\"]+)\\\"");
            if (kind.Length == 0 || !match.Success) continue;
            var name = match.Groups["name"].Value;
            result.Add(new DiscoveredNativeMediaDeviceInfo(name, name, kind, "dshow"));
        }
        return result;
    }

    private static async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAvFoundationAsync(string? ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFfmpegAsync(ffmpegPath, ["-hide_banner", "-f", "avfoundation", "-list_devices", "true", "-i", ""], cancellationToken);
        var result = new List<DiscoveredNativeMediaDeviceInfo>();
        var kind = string.Empty;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Contains("AVFoundation video devices", StringComparison.OrdinalIgnoreCase)) { kind = "CaptureDevice"; continue; }
            if (line.Contains("AVFoundation audio devices", StringComparison.OrdinalIgnoreCase)) { kind = "Microphone"; continue; }
            var match = Regex.Match(line, @"\[(?<index>\d+)\]\s+(?<name>.+)$");
            if (kind.Length == 0 || !match.Success) continue;
            result.Add(new DiscoveredNativeMediaDeviceInfo(match.Groups["index"].Value, match.Groups["name"].Value.Trim(), kind, "avfoundation"));
        }
        return result;
    }

    private static IReadOnlyList<DiscoveredNativeMediaDeviceInfo> DiscoverWindowsProcesses()
    {
        var result = new List<DiscoveredNativeMediaDeviceInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id <= 4 || string.IsNullOrWhiteSpace(process.ProcessName)) continue;
                var title = process.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title)) continue;
                result.Add(new DiscoveredNativeMediaDeviceInfo(
                    process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    process.ProcessName,
                    "ApplicationAudio",
                    "wasapi-process-loopback",
                    process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    title));
            }
            catch { }
            finally { process.Dispose(); }
        }
        return result;
    }

    private static async Task<string> RunFfmpegAsync(string? ffmpegPath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var executable = FfmpegLocator.Resolve(ffmpegPath);
        if (executable is null) return string.Empty;
        try
        {
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
            await process.WaitForExitAsync(cancellationToken);
            return (await stdout) + "\n" + (await stderr);
        }
        catch { return string.Empty; }
    }
}
