using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Threading.Channels;

public sealed class NativeCaptureRegistry : IDisposable
{
    private readonly ConcurrentDictionary<Guid, NativeCaptureSession> _captures = new();

    public NativeCaptureSession Create(NativeCaptureRequest request)
    {
        var session = new NativeCaptureSession(request);
        if (!_captures.TryAdd(session.Id, session)) throw new InvalidOperationException("Could not register native capture.");
        try { session.Start(); }
        catch { _captures.TryRemove(session.Id, out _); session.Dispose(); throw; }
        return session;
    }

    public bool TryGet(Guid id, out NativeCaptureSession session) => _captures.TryGetValue(id, out session!);

    public bool Stop(Guid id)
    {
        if (!_captures.TryRemove(id, out var session)) return false;
        session.Dispose();
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _captures.Keys.ToArray()) Stop(id);
    }
}

public sealed class NativeCaptureRequest
{
    public string Kind { get; set; } = "Camera";
    public string DeviceId { get; set; } = string.Empty;
    public string AudioDeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string NativeBackend { get; set; } = string.Empty;
    public string NetworkUrl { get; set; } = string.Empty;
    public bool IncludeAudio { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 60;
    public bool UseDeviceTimestamps { get; set; } = true;
    public string FfmpegPath { get; set; } = string.Empty;
}

public sealed class NativeCaptureSession : IDisposable
{
    private readonly NativeCaptureRequest _request;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Channel<byte[]>> _subscribers = [];
    private readonly CancellationTokenSource _cancellation = new();
    private Process? _process;
    private WindowsProcessLoopbackCapture? _processLoopback;
    private byte[] _initialization = [];
    private bool _disposed;

    public NativeCaptureSession(NativeCaptureRequest request)
    {
        _request = request;
        Id = Guid.NewGuid();
        IsAudioOnly = request.Kind.Equals("Microphone", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("SystemAudio", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("ApplicationAudio", StringComparison.OrdinalIgnoreCase);
        MimeType = IsAudioOnly
            ? "audio/webm;codecs=opus"
            : request.IncludeAudio ? "video/webm;codecs=vp9,opus" : "video/webm;codecs=vp9";
    }

    public Guid Id { get; }
    public bool IsAudioOnly { get; }
    public string MimeType { get; }
    public string Status { get; private set; } = "created";
    public string LastError { get; private set; } = string.Empty;

    public void Start()
    {
        var ffmpeg = FfmpegLocator.Resolve(_request.FfmpegPath)
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
        _ = Task.Run(() => PumpAsync(process.StandardOutput.BaseStream, _cancellation.Token));
        if (_request.Kind.Equals("ApplicationAudio", StringComparison.OrdinalIgnoreCase)
            && ResolveBackend("applicationaudio") == "wasapi-process-loopback")
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Per-application audio capture requires Windows.");
            var processText = string.IsNullOrWhiteSpace(_request.ApplicationId) ? _request.DeviceId : _request.ApplicationId;
            if (!uint.TryParse(processText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId) || processId <= 4)
                throw new InvalidOperationException("Select a valid Windows process for application audio capture.");
            _processLoopback = new WindowsProcessLoopbackCapture(processId, process.StandardInput.BaseStream, _cancellation.Token);
            _processLoopback.Start();
        }
    }

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

    public void Unsubscribe(Guid id)
    {
        Channel<byte[]>? channel;
        lock (_sync)
        {
            if (!_subscribers.Remove(id, out channel)) return;
        }
        channel.Writer.TryComplete();
    }

    private async Task PumpAsync(Stream stdout, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var count = await stdout.ReadAsync(buffer, cancellationToken);
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

    private IReadOnlyList<string> BuildArguments()
    {
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
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Per-application WASAPI capture requires Windows.");
            args.AddRange(["-f", "s16le", "-ar", "48000", "-ac", "2", "-i", "pipe:0"]);
        }
        else if (OperatingSystem.IsWindows() && backend == "dshow")
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
        else if (OperatingSystem.IsMacOS() && backend == "avfoundation")
        {
            if (string.IsNullOrWhiteSpace(_request.DeviceId)) throw new InvalidOperationException("Select an AVFoundation device.");
            var avInput = IsAudioOnly
                ? $":{_request.DeviceId}"
                : $"{_request.DeviceId}:{(_request.IncludeAudio && !string.IsNullOrWhiteSpace(_request.AudioDeviceId) ? _request.AudioDeviceId : "none")}";
            args.AddRange(["-f", "avfoundation", "-framerate", Math.Clamp(_request.FrameRate, 15, 120).ToString(CultureInfo.InvariantCulture), "-i", avInput]);
        }
        else if (OperatingSystem.IsLinux() && backend == "v4l2" && !IsAudioOnly)
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

    private string ResolveBackend(string kind)
    {
        if (!string.IsNullOrWhiteSpace(_request.NativeBackend)) return _request.NativeBackend.Trim().ToLowerInvariant();
        if (kind == "applicationaudio") return "wasapi-process-loopback";
        if (OperatingSystem.IsWindows()) return "dshow";
        if (OperatingSystem.IsMacOS()) return "avfoundation";
        if (OperatingSystem.IsLinux()) return "v4l2";
        return "unknown";
    }

    private static int ClampEven(int value)
    {
        var clamped = Math.Clamp(value, 2, 7680);
        return clamped % 2 == 0 ? clamped : clamped - 1;
    }

    private void CompleteSubscribers()
    {
        Channel<byte[]>[] channels;
        lock (_sync)
        {
            channels = _subscribers.Values.ToArray();
            _subscribers.Clear();
        }
        foreach (var channel in channels) channel.Writer.TryComplete();
    }

    public void Dispose()
    {
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
}
