namespace PublisherStudio.Services.Streaming.UseCases.Runtime;

/// <summary>
/// Orchestrates read-only runtime information used by the desktop host UI.
/// Provider, capture and metadata implementation details remain in Services/Streaming.
/// </summary>
public sealed class StreamingRuntimeUseCases
{
    /// <summary>
    /// Stores the windows hotkey native service dependency used by <see cref="StreamingRuntimeUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IGlobalHotkeyNativeService hotkeyNativeService;
    /// <summary>
    /// Stores the windows process loopback native service dependency used by <see cref="StreamingRuntimeUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IProcessLoopbackNativeService processLoopbackNativeService;
    /// <summary>
    /// Stores the internal native device discovery state used by <see cref="StreamingRuntimeUseCases"/> while executing its surrounding workflow.
    /// </summary>
    private readonly NativeDeviceDiscovery nativeDeviceDiscovery;
    /// <summary>
    /// Stores the internal now playing reader state used by <see cref="StreamingRuntimeUseCases"/> while executing its surrounding workflow.
    /// </summary>
    private readonly NowPlayingReader nowPlayingReader;
    /// <summary>
    /// Stores the logger used by <see cref="StreamingRuntimeUseCases"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<StreamingRuntimeUseCases> logger;

    /// <summary>
    /// Initializes a new <see cref="StreamingRuntimeUseCases"/> instance and captures the dependencies or initial state required by its streaming runtime use cases workflow.
    /// </summary>
    /// <param name="hotkeyNativeService">Windows hotkey native service dependency used by the streaming runtime use cases workflow to provide the corresponding application capability.</param>
    /// <param name="processLoopbackNativeService">Windows process loopback native service dependency used by the streaming runtime use cases workflow to provide the corresponding application capability.</param>
    /// <param name="nativeDeviceDiscovery">Native device discovery value supplied to the streaming runtime use cases operation and used when producing its result.</param>
    /// <param name="nowPlayingReader">Now playing reader value supplied to the streaming runtime use cases operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public StreamingRuntimeUseCases(
        IGlobalHotkeyNativeService hotkeyNativeService,
        IProcessLoopbackNativeService processLoopbackNativeService,
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
    /// Retrieves capabilities for <see cref="StreamingRuntimeUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming runtime use cases workflow.
    /// </summary>
    /// <returns>The streaming runtime capabilities produced by the operation.</returns>
    public StreamingRuntimeCapabilities GetCapabilities() {
        try
        {
            logger.LogTrace($"Entering StreamingRuntimeUseCases.GetCapabilities.");
            return new()
    {
        Version = "2.2.4",
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
    /// Discovers devices for <see cref="StreamingRuntimeUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming runtime use cases workflow.
    /// </summary>
    /// <param name="ffmpegPath">Ffmpeg path value supplied to the streaming runtime use cases operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Reads now playing for <see cref="StreamingRuntimeUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming runtime use cases workflow.
    /// </summary>
    /// <param name="directory">Directory value supplied to the streaming runtime use cases operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
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
/// Represents a streaming runtime capabilities application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class StreamingRuntimeCapabilities
{
    /// <summary>
    /// Gets or sets the version value that forms part of the streaming runtime capabilities state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public string Version { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether browser capture applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The browser capture value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool BrowserCapture { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether browser audio mix applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The browser audio mix value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool BrowserAudioMix { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether native device discovery applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The native device discovery value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool NativeDeviceDiscovery { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether native camera capture applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The native camera capture value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool NativeCameraCapture { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether process audio loopback applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The process audio loopback value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool ProcessAudioLoopback { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether browser window audio fallback applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The browser window audio fallback value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool BrowserWindowAudioFallback { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether device timestamps applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The device timestamps value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool DeviceTimestamps { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether global hotkeys applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The global hotkeys value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool GlobalHotkeys { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether recording applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The recording value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool Recording { get; init; }
    /// <summary>
    /// Gets or sets the transports value that forms part of the streaming runtime capabilities state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transports value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public string[] Transports { get; init; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether hardware encoder probe applies to the streaming runtime capabilities state.
    /// </summary>
    /// <value>The hardware encoder probe value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public bool HardwareEncoderProbe { get; init; }
    /// <summary>
    /// Gets or sets the note value that forms part of the streaming runtime capabilities state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The note value exposed by <see cref="StreamingRuntimeCapabilities"/>.</value>
    public string Note { get; init; } = string.Empty;
}
