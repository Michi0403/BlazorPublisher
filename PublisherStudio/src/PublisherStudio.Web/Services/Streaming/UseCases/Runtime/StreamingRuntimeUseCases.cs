namespace PublisherStudio.Services.Streaming.UseCases.Runtime;

/// <summary>
/// Orchestrates read-only runtime information used by the desktop host UI.
/// Provider, capture and metadata implementation details remain in Services/Streaming.
/// </summary>
public sealed class StreamingRuntimeUseCases
{
    private readonly IWindowsHotkeyNativeService hotkeyNativeService;
    private readonly IWindowsProcessLoopbackNativeService processLoopbackNativeService;
    private readonly NativeDeviceDiscovery nativeDeviceDiscovery;
    private readonly NowPlayingReader nowPlayingReader;
    private readonly ILogger<StreamingRuntimeUseCases> logger;

    public StreamingRuntimeUseCases(
        IWindowsHotkeyNativeService hotkeyNativeService,
        IWindowsProcessLoopbackNativeService processLoopbackNativeService,
        NativeDeviceDiscovery nativeDeviceDiscovery,
        NowPlayingReader nowPlayingReader,
        ILogger<StreamingRuntimeUseCases> logger)
    {
        this.hotkeyNativeService = hotkeyNativeService;
        this.processLoopbackNativeService = processLoopbackNativeService;
        this.nativeDeviceDiscovery = nativeDeviceDiscovery;
        this.nowPlayingReader = nowPlayingReader;
        this.logger = logger;
    }
    public StreamingRuntimeCapabilities GetCapabilities() {
        try
        {
            logger.LogTrace($"Entering StreamingRuntimeUseCases.GetCapabilities.");
            return new()
    {
        Version = "2.0.5",
        BrowserCapture = true,
        BrowserAudioMix = true,
        NativeDeviceDiscovery = true,
        NativeCameraCapture = true,
        ProcessAudioLoopback = processLoopbackNativeService.IsAvailable,
        BrowserWindowAudioFallback = true,
        DeviceTimestamps = true,
        GlobalHotkeys = hotkeyNativeService.IsAvailable,
        Recording = true,
        Transports = ["rtmp", "rtmps", "srt", "hls", "rtsp", "webrtc", "browser-webm"],
        HardwareEncoderProbe = true,
        Note = "The integrated PublisherStudio streaming runtime owns encoder orchestration, recording, LAN delivery, native capture-card/device discovery and Windows global hotkeys. Windows process-tree audio loopback is built in on Windows 10 build 20348 or later; browser window-audio remains the cross-platform fallback."
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingRuntimeUseCases.GetCapabilities failed: {exception.Message}");
            throw;
        }
    }

    public async Task<IReadOnlyList<PublisherStudio.BusinessObjects.NativeMediaDeviceInfo>> DiscoverDevicesAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering StreamingRuntimeUseCases.DiscoverDevicesAsync.");
                    var devices = await nativeDeviceDiscovery.DiscoverAsync(ffmpegPath, cancellationToken).ConfigureAwait(false);
                    return devices.Select(device => new PublisherStudio.BusinessObjects.NativeMediaDeviceInfo
                    {
                        Id = device.Id,
                        Name = device.Name,
                        Kind = device.Kind,
                        Backend = device.Backend,
                        ProcessId = device.ProcessId,
                        WindowTitle = device.WindowTitle
                    }).ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingRuntimeUseCases.DiscoverDevicesAsync failed: {exception.Message}");
            throw;
        }
    }

    public object? ReadNowPlaying(string directory) {
        try
        {
            logger.LogTrace($"Entering StreamingRuntimeUseCases.ReadNowPlaying.");
            return nowPlayingReader.Read(directory);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"StreamingRuntimeUseCases.ReadNowPlaying failed: {exception.Message}");
            throw;
        }
    }
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
