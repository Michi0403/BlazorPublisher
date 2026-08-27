using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Threading.Channels;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Streaming.Encoding;

namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Defines the contract for native capture session behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface INativeCaptureSessionFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="INativeCaptureSessionFactory"/>.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The native capture session produced by the operation.</returns>
    NativeCaptureSession Create(NativeCaptureRequest request);
}

/// <summary>
/// Creates configured native capture session instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the native capture session workflow to provide the corresponding application capability.</param>
/// <param name="ffmpegLocator">Ffmpeg locator value supplied to the native capture session operation and used when producing its result.</param>
/// <param name="processLoopbackFactory">Windows process loopback capture factory dependency used by the native capture session workflow to provide the corresponding application capability.</param>
/// <param name="taskRunner">Supervised task runner used for intentionally concurrent native capture pumps.</param>
/// <param name="loggerFactory">Logger factory dependency used by the native capture session workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="platform">Publisher platform runtime service dependency used by the native capture session workflow to provide the corresponding application capability.</param>
public sealed class NativeCaptureSessionFactory(
    IPublisherRuntimePolicyDataService runtimePolicy,
    FfmpegLocator ffmpegLocator,
    IProcessLoopbackCaptureFactory processLoopbackFactory,
    IPublisherPlatformRuntimeService platform,
    ISupervisedTaskRunner taskRunner,
    ILoggerFactory loggerFactory,
    ILogger<NativeCaptureSessionFactory> logger) : INativeCaptureSessionFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="NativeCaptureSessionFactory"/>.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The native capture session produced by the operation.</returns>
    public NativeCaptureSession Create(NativeCaptureRequest request)
    {
        try
        {
            logger.LogTrace("Creating a native capture session for {CaptureKind}.", request.Kind);
            return new NativeCaptureSession(
                request,
                runtimePolicy,
                ffmpegLocator,
                processLoopbackFactory,
                platform,
                taskRunner,
                loggerFactory.CreateLogger<NativeCaptureSession>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a native capture session.");
            throw;
        }
    }
}

/// <summary>
/// Maintains the authoritative directory of native capture entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="sessionFactory">Native capture session factory dependency used by the native capture workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class NativeCaptureRegistry(
    INativeCaptureSessionFactory sessionFactory,
    ILogger<NativeCaptureRegistry> logger) : IDisposable
{
    /// <summary>
    /// Stores the in-memory captures collection maintained internally by <see cref="NativeCaptureRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, NativeCaptureSession> _captures = new();

    /// <summary>
    /// Performs create in the native capture directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The native capture session produced by the operation.</returns>
    public NativeCaptureSession Create(NativeCaptureRequest request)
    {
        try
        {
            logger.LogTrace("Entering NativeCaptureRegistry.Create.");
            var session = sessionFactory.Create(request);
            if (!_captures.TryAdd(session.Id, session))
                throw new InvalidOperationException("Could not register native capture.");
            try
            {
                session.Start();
                return session;
            }
            catch
            {
                _captures.TryRemove(session.Id, out _);
                session.Dispose();
                throw;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NativeCaptureRegistry.Create failed: {Message}", exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Attempts to get in the native capture directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="session">Session value supplied to the native capture operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid id, out NativeCaptureSession session)
    {
        try
        {
            logger.LogTrace("Entering NativeCaptureRegistry.TryGet.");
            return _captures.TryGetValue(id, out session!);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NativeCaptureRegistry.TryGet failed: {Message}", exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Performs stop in the native capture directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Stop(Guid id)
    {
        try
        {
            logger.LogTrace("Entering NativeCaptureRegistry.Stop.");
            if (!_captures.TryRemove(id, out var session)) return false;
            session.Dispose();
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NativeCaptureRegistry.Stop failed: {Message}", exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="NativeCaptureRegistry"/> and leaves the native capture workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            logger.LogTrace("Entering NativeCaptureRegistry.Dispose.");
            foreach (var id in _captures.Keys.ToArray()) Stop(id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NativeCaptureRegistry.Dispose failed: {Message}", exception.Message);
            throw;
        }
    }
}

/// <summary>
/// Represents a native capture session application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class NativeCaptureSession : IDisposable
{
    /// <summary>
    /// Stores the publisher runtime policy data service dependency used by <see cref="NativeCaptureSession"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherRuntimePolicyDataService _runtimePolicy;
    /// <summary>
    /// Stores the internal request state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private readonly NativeCaptureRequest _request;
    /// <summary>
    /// Stores the internal FFmpeg locator state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private readonly FfmpegLocator _ffmpegLocator;
    /// <summary>
    /// Stores the windows process loopback capture factory dependency used by <see cref="NativeCaptureSession"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IProcessLoopbackCaptureFactory _processLoopbackFactory;
    /// <summary>Owns host-specific capture availability and backend selection.</summary>
    private readonly IPublisherPlatformRuntimeService _platform;
    /// <summary>
    /// Stores the logger used by <see cref="NativeCaptureSession"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<NativeCaptureSession> logger;
    /// <summary>
    /// Stores the internal sync state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Stores the in-memory subscribers collection maintained internally by <see cref="NativeCaptureSession"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<Guid, Channel<byte[]>> _subscribers = [];
    /// <summary>
    /// Stores the cancellation source used by <see cref="NativeCaptureSession"/> to stop its current background or asynchronous operation.
    /// </summary>
    private readonly CancellationTokenSource _cancellation = new();
    /// <summary>
    /// Stores the internal process state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private Process? _process;
    /// <summary>
    /// Stores the windows process loopback capture dependency used by <see cref="NativeCaptureSession"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IProcessLoopbackCapture? _processLoopback;
    /// <summary>
    /// Stores the internal initialization state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private byte[] _initialization = [];
    /// <summary>
    /// Stores the internal disposed state used by <see cref="NativeCaptureSession"/> while executing its surrounding workflow.
    /// </summary>
    private bool _disposed;
    /// <summary>Observes the native capture pump so it cannot become discarded asynchronous work.</summary>
    private readonly ISupervisedTaskRunner _taskRunner;

    /// <summary>
    /// Initializes a new <see cref="NativeCaptureSession"/> instance and captures the dependencies or initial state required by its native capture session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the native capture session workflow to provide the corresponding application capability.</param>
    /// <param name="ffmpegLocator">Ffmpeg locator value supplied to the native capture session operation and used when producing its result.</param>
    /// <param name="processLoopbackFactory">Windows process loopback capture factory dependency used by the native capture session workflow to provide the corresponding application capability.</param>
    /// <param name="taskRunner">Supervised task runner used to observe the capture pump.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="platform">Publisher platform runtime service dependency used by the native capture session workflow to provide the corresponding application capability.</param>
    public NativeCaptureSession(
        NativeCaptureRequest request,
        IPublisherRuntimePolicyDataService runtimePolicy,
        FfmpegLocator ffmpegLocator,
        IProcessLoopbackCaptureFactory processLoopbackFactory,
        IPublisherPlatformRuntimeService platform,
        ISupervisedTaskRunner taskRunner,
        ILogger<NativeCaptureSession> logger)
    {
        _request = request;
        _runtimePolicy = runtimePolicy;
        _ffmpegLocator = ffmpegLocator;
        _processLoopbackFactory = processLoopbackFactory;
        _platform = platform;
        _taskRunner = taskRunner;
        this.logger = logger;
        Id = Guid.NewGuid();
        IsAudioOnly = request.Kind.Equals("Microphone", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("SystemAudio", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("ApplicationAudio", StringComparison.OrdinalIgnoreCase);
        MimeType = IsAudioOnly
            ? "audio/webm;codecs=opus"
            : request.IncludeAudio ? "video/webm;codecs=vp9,opus" : "video/webm;codecs=vp9";
    }

    /// <summary>
    /// Gets the stable identifier used to identify or correlate this native capture session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="NativeCaptureSession"/>.</value>
    public Guid Id { get; }
    /// <summary>
    /// Gets a value indicating whether audio only applies to the native capture session state.
    /// </summary>
    /// <value>The is audio only value exposed by <see cref="NativeCaptureSession"/>.</value>
    public bool IsAudioOnly { get; }
    /// <summary>
    /// Gets the MIME type value that forms part of the native capture session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="NativeCaptureSession"/>.</value>
    public string MimeType { get; }
    /// <summary>
    /// Gets or sets the status value that forms part of the native capture session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="NativeCaptureSession"/>.</value>
    public string Status { get; private set; } = "created";
    /// <summary>
    /// Gets or sets the last error value that forms part of the native capture session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="NativeCaptureSession"/>.</value>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Performs start for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    public void Start()
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.Start.");
                    var ffmpeg = _ffmpegLocator.Resolve(_request.FfmpegPath)
                        ?? throw new FileNotFoundException("FFmpeg is required for native capture. Install it with PublisherStudio.Setup or configure its path in Streaming Studio.");
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = _request.Kind.Equals("ApplicationAudio", StringComparison.OrdinalIgnoreCase)
                            && ResolveBackend("applicationaudio") == "wasapi-process-loopback"
                    };
                    foreach (var argument in BuildArguments()) startInfo.ArgumentList.Add(argument);
                    var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                    process.ErrorDataReceived += (_, eventArgs) => { if (!string.IsNullOrWhiteSpace(eventArgs.Data)) LastError = eventArgs.Data; };
                    process.Exited += (_, _) => { Status = _disposed ? "stopped" : "ended"; CompleteSubscribers(); };
                    if (!process.Start()) throw new InvalidOperationException("FFmpeg did not start the native capture.");
                    _process = process;
                    process.BeginErrorReadLine();
                    Status = "capturing";
                    _taskRunner.Run(nameof(NativeCaptureSession), $"Pump:{Id:D}", _ => PumpAsync(process.StandardOutput.BaseStream, _cancellation.Token), _cancellation.Token);
                    if (_request.Kind.Equals("ApplicationAudio", StringComparison.OrdinalIgnoreCase)
                        && ResolveBackend("applicationaudio") == "wasapi-process-loopback")
                    {
                        if (!_platform.SupportsProcessAudioLoopback) throw new PlatformNotSupportedException("Per-application audio capture is not supported by this host platform.");
                        var processText = string.IsNullOrWhiteSpace(_request.ApplicationId) ? _request.DeviceId : _request.ApplicationId;
                        if (!uint.TryParse(processText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId) || processId <= 4)
                            throw new InvalidOperationException("Select a valid Windows process for application audio capture.");
                        _processLoopback = _processLoopbackFactory.Create(
                            processId,
                            process.StandardInput.BaseStream,
                            _cancellation.Token);
                        _processLoopback.Start();
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.Start failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs subscribe for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <returns>The GUID identifier byte initialization channel reader byte reader produced by the operation.</returns>
    public (Guid Id, byte[] Initialization, ChannelReader<byte[]> Reader) Subscribe()
    {
        lock (_sync)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(240)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _subscribers[id] = channel;
            return (id, _initialization.ToArray(), channel.Reader);
        }
    }

    /// <summary>
    /// Performs unsubscribe for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void Unsubscribe(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.Unsubscribe.");
                    Channel<byte[]>? channel;
                    lock (_sync)
                    {
                        if (!_subscribers.Remove(id, out channel)) return;
                    }
                    channel.Writer.TryComplete();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.Unsubscribe failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs pump for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <param name="stdout">Stdout value supplied to the native capture session operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PumpAsync(Stream stdout, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.PumpAsync.");
                    var buffer = new byte[64 * 1024];
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var count = await stdout.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            if (count <= 0) break;
                            var chunk = buffer.AsSpan(0, count).ToArray();
                            ChannelWriter<byte[]>[] writers;
                            lock (_sync)
                            {
                                if (_initialization.Length < 512 * 1024)
                                {
                                    var remaining = 512 * 1024 - _initialization.Length;
                                    var append = Math.Min(remaining, chunk.Length);
                                    var combined = new byte[_initialization.Length + append];
                                    _initialization.CopyTo(combined, 0);
                                    chunk.AsSpan(0, append).CopyTo(combined.AsSpan(_initialization.Length));
                                    _initialization = combined;
                                }
                                writers = _subscribers.Values.Select(item => item.Writer).ToArray();
                            }
                            foreach (var writer in writers) writer.TryWrite(chunk);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                    catch (Exception exception) { LastError = exception.Message; }
                    finally { CompleteSubscribers(); }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.PumpAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds arguments for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> BuildArguments()
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.BuildArguments.");
                    var kind = _request.Kind.Trim().ToLowerInvariant();
                    var backend = ResolveBackend(kind);
                    var args = new List<string> { "-hide_banner", "-loglevel", "warning" };
                    if (_request.UseDeviceTimestamps) args.AddRange(["-use_wallclock_as_timestamps", "1"]);

                    if (kind == "networkmedia")
                    {
                        if (string.IsNullOrWhiteSpace(_request.NetworkUrl)) throw new InvalidOperationException("A network media URL is required.");
                        args.AddRange(["-i", _request.NetworkUrl]);
                    }
                    else if (kind == "applicationaudio" && backend == "wasapi-process-loopback")
                    {
                        if (!_platform.SupportsProcessAudioLoopback) throw new PlatformNotSupportedException("Per-application process loopback is not supported by this host platform.");
                        args.AddRange(["-f", "s16le", "-ar", "48000", "-ac", "2", "-i", "pipe:0"]);
                    }
                    else if (backend == "dshow" && _platform.SupportsNativeCaptureBackend(backend))
                    {
                        if (string.IsNullOrWhiteSpace(_request.DeviceId)) throw new InvalidOperationException("Select a native DirectShow device.");
                        args.AddRange(["-f", "dshow"]);
                        if (!IsAudioOnly)
                        {
                            args.AddRange(["-video_size", $"{ClampEven(_request.Width)}x{ClampEven(_request.Height)}", "-framerate", Math.Clamp(_request.FrameRate, 15, 120).ToString(CultureInfo.InvariantCulture)]);
                            var input = $"video={_request.DeviceId}";
                            if (_request.IncludeAudio && !string.IsNullOrWhiteSpace(_request.AudioDeviceId)) input += $":audio={_request.AudioDeviceId}";
                            args.AddRange(["-i", input]);
                        }
                        else args.AddRange(["-i", $"audio={_request.DeviceId}"]);
                    }
                    else if (backend == "avfoundation" && _platform.SupportsNativeCaptureBackend(backend))
                    {
                        if (string.IsNullOrWhiteSpace(_request.DeviceId)) throw new InvalidOperationException("Select an AVFoundation device.");
                        var avInput = IsAudioOnly
                            ? $":{_request.DeviceId}"
                            : $"{_request.DeviceId}:{(_request.IncludeAudio && !string.IsNullOrWhiteSpace(_request.AudioDeviceId) ? _request.AudioDeviceId : "none")}";
                        args.AddRange(["-f", "avfoundation", "-framerate", Math.Clamp(_request.FrameRate, 15, 120).ToString(CultureInfo.InvariantCulture), "-i", avInput]);
                    }
                    else if (backend == "v4l2" && _platform.SupportsNativeCaptureBackend(backend) && !IsAudioOnly)
                    {
                        if (string.IsNullOrWhiteSpace(_request.DeviceId)) throw new InvalidOperationException("Select a V4L2 device.");
                        args.AddRange(["-f", "v4l2", "-video_size", $"{ClampEven(_request.Width)}x{ClampEven(_request.Height)}", "-framerate", Math.Clamp(_request.FrameRate, 15, 120).ToString(CultureInfo.InvariantCulture), "-i", _request.DeviceId]);
                    }
                    else
                    {
                        throw new NotSupportedException($"Native capture backend '{backend}' is not available for {_request.Kind} on this operating system.");
                    }

                    if (IsAudioOnly)
                    {
                        args.AddRange(["-vn", "-c:a", "libopus", "-b:a", "192k", "-ar", "48000", "-f", "webm", "pipe:1"]);
                    }
                    else
                    {
                        args.AddRange(["-vf", $"scale={ClampEven(_request.Width)}:{ClampEven(_request.Height)}:force_original_aspect_ratio=decrease,pad={ClampEven(_request.Width)}:{ClampEven(_request.Height)}:(ow-iw)/2:(oh-ih)/2", "-r", Math.Clamp(_request.FrameRate, 15, 120).ToString(CultureInfo.InvariantCulture), "-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "5", "-row-mt", "1", "-g", Math.Max(30, _request.FrameRate * 2).ToString(CultureInfo.InvariantCulture)]);
                        var carriesAudio = _request.IncludeAudio
                            && (kind == "networkmedia" || !string.IsNullOrWhiteSpace(_request.AudioDeviceId));
                        if (carriesAudio)
                            args.AddRange(["-c:a", "libopus", "-b:a", "192k", "-ar", "48000"]);
                        else
                            args.Add("-an");
                        args.AddRange(["-f", "webm", "pipe:1"]);
                    }
                    return args;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.BuildArguments failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resolves backend for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the native capture session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveBackend(string kind)
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.ResolveBackend.");
                    if (!string.IsNullOrWhiteSpace(_request.NativeBackend)) return _request.NativeBackend.Trim().ToLowerInvariant();
                    if (kind == "applicationaudio")
                        return _platform.SupportsProcessAudioLoopback ? "wasapi-process-loopback" : "unsupported-process-loopback";
                    return _platform.DefaultNativeCaptureBackend;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.ResolveBackend failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clamp even for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the native capture session operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ClampEven(int value)
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.ClampEven.");
                    var clamped = Math.Clamp(value, 2, 7680);
                    return clamped % 2 == 0 ? clamped : clamped - 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.ClampEven failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Completes subscribers for <see cref="NativeCaptureSession"/>, keeping the operation consistent with the state and invariants of the surrounding native capture session workflow.
    /// </summary>
    private void CompleteSubscribers()
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.CompleteSubscribers.");
                    Channel<byte[]>[] channels;
                    lock (_sync)
                    {
                        channels = _subscribers.Values.ToArray();
                        _subscribers.Clear();
                    }
                    foreach (var channel in channels) channel.Writer.TryComplete();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.CompleteSubscribers failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="NativeCaptureSession"/> and leaves the native capture session workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            logger.LogTrace($"Entering NativeCaptureSession.Dispose.");
                    lock (_sync)
                    {
                        if (_disposed) return;
                        _disposed = true;
                    }
                    _cancellation.Cancel();
                    try { _processLoopback?.Dispose(); } catch { }
                    _processLoopback = null;
                    try
                    {
                        if (_process is { HasExited: false })
                        {
                            _process.Kill(entireProcessTree: true);
                            _process.WaitForExit(2000);
                        }
                    }
                    catch { }
                    _process?.Dispose();
                    CompleteSubscribers();
                    _cancellation.Dispose();
                    Status = "stopped";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NativeCaptureSession.Dispose failed: {exception.Message}");
            throw;
        }
    }
}
