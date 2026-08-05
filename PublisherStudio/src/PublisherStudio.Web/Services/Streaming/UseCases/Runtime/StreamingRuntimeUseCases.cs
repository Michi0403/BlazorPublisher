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

    /// <summary>
    /// Runs the streaming runtime use cases operation.
    /// </summary>
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
    /// <summary>
    /// Gets capabilities.
    /// </summary>
    public StreamingRuntimeCapabilities GetCapabilities() {
        try
        {
            logger.LogTrace($"Entering StreamingRuntimeUseCases.GetCapabilities.");
            return new()
    {
        Version = "2.1.9",
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

    /// <summary>
    /// Runs the discover devices async operation.
    /// </summary>
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

    /// <summary>
    /// Reads now playing.
    /// </summary>
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

/// <summary>
/// Represents a streaming runtime capabilities.
/// </summary>
public sealed class StreamingRuntimeCapabilities
{
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public string Version { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets browser capture.
    /// </summary>
    public bool BrowserCapture { get; init; }
    /// <summary>
    /// Gets or sets browser audio mix.
    /// </summary>
    public bool BrowserAudioMix { get; init; }
    /// <summary>
    /// Gets or sets native device discovery.
    /// </summary>
    public bool NativeDeviceDiscovery { get; init; }
    /// <summary>
    /// Gets or sets native camera capture.
    /// </summary>
    public bool NativeCameraCapture { get; init; }
    /// <summary>
    /// Gets or sets process audio loopback.
    /// </summary>
    public bool ProcessAudioLoopback { get; init; }
    /// <summary>
    /// Gets or sets browser window audio fallback.
    /// </summary>
    public bool BrowserWindowAudioFallback { get; init; }
    /// <summary>
    /// Gets or sets device timestamps.
    /// </summary>
    public bool DeviceTimestamps { get; init; }
    /// <summary>
    /// Gets or sets global hotkeys.
    /// </summary>
    public bool GlobalHotkeys { get; init; }
    /// <summary>
    /// Gets or sets recording.
    /// </summary>
    public bool Recording { get; init; }
    /// <summary>
    /// Gets or sets transports.
    /// </summary>
    public string[] Transports { get; init; } = [];
    /// <summary>
    /// Gets or sets hardware encoder probe.
    /// </summary>
    public bool HardwareEncoderProbe { get; init; }
    /// <summary>
    /// Gets or sets note.
    /// </summary>
    public string Note { get; init; } = string.Empty;
}
