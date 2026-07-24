namespace PublisherStudio.Services.Streaming.UseCases.Runtime;

/// <summary>
/// Orchestrates read-only runtime information used by the desktop host UI.
/// Provider, capture and metadata implementation details remain in Backend/Streaming.
/// </summary>
public sealed class StreamingRuntimeUseCases
{
    public StreamingRuntimeCapabilities GetCapabilities() => new()
    {
        Version = "1.0.62",
        BrowserCapture = true,
        BrowserAudioMix = true,
        NativeDeviceDiscovery = true,
        NativeCameraCapture = true,
        ProcessAudioLoopback = OperatingSystem.IsWindows(),
        BrowserWindowAudioFallback = true,
        DeviceTimestamps = true,
        GlobalHotkeys = OperatingSystem.IsWindows(),
        Recording = true,
        Transports = ["rtmp", "rtmps", "srt", "hls", "rtsp", "webrtc", "browser-webm"],
        HardwareEncoderProbe = true,
        Note = "The integrated PublisherStudio streaming runtime owns encoder orchestration, recording, LAN delivery, native capture-card/device discovery and Windows global hotkeys. Windows process-tree audio loopback is built in on Windows 10 build 20348 or later; browser window-audio remains the cross-platform fallback."
    };

    public async Task<IReadOnlyList<PublisherStudio.Domain.NativeMediaDeviceInfo>> DiscoverDevicesAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        var devices = await NativeDeviceDiscovery.DiscoverAsync(ffmpegPath, cancellationToken);
        return devices.Select(device => new PublisherStudio.Domain.NativeMediaDeviceInfo
        {
            Id = device.Id,
            Name = device.Name,
            Kind = device.Kind,
            Backend = device.Backend,
            ProcessId = device.ProcessId,
            WindowTitle = device.WindowTitle
        }).ToList();
    }

    public object? ReadNowPlaying(string directory) => NowPlayingReader.Read(directory);
}

public sealed class StreamingRuntimeCapabilities
{
    public string Version { get; init; } = string.Empty;
    public bool BrowserCapture { get; init; }
    public bool BrowserAudioMix { get; init; }
    public bool NativeDeviceDiscovery { get; init; }
    public bool NativeCameraCapture { get; init; }
    public bool ProcessAudioLoopback { get; init; }
    public bool BrowserWindowAudioFallback { get; init; }
    public bool DeviceTimestamps { get; init; }
    public bool GlobalHotkeys { get; init; }
    public bool Recording { get; init; }
    public string[] Transports { get; init; } = [];
    public bool HardwareEncoderProbe { get; init; }
    public string Note { get; init; } = string.Empty;
}
