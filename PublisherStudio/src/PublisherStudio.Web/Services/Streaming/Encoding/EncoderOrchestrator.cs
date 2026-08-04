using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;

namespace PublisherStudio.Services.Streaming.Encoding;

public sealed class EncoderOrchestrator(
    FfmpegLocator ffmpegLocator,
    FfmpegEncoderResolver encoderResolver,
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILoggerFactory loggerFactory,
    ILogger<EncoderOrchestrator> logger)
{
    public void Attach(MediaSession session, Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderOrchestrator.Attach.");
                    session.Encoder ??= new EncoderSessionService(
                        session,
                        ffmpegLocator,
                        encoderResolver,
                        runtimePolicy.MediaSessionDefaults,
                        loggerFactory.CreateLogger<EncoderSessionService>());
                    session.Encoder.NotifyIngest(inputId);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderOrchestrator.Attach failed: {exception.Message}");
            throw;
        }
    }

    public void Stop(MediaSession session)
    {
        try
        {
            logger.LogTrace($"Entering EncoderOrchestrator.Stop.");
                    session.Encoder?.Dispose();
                    session.Encoder = null;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderOrchestrator.Stop failed: {exception.Message}");
            throw;
        }
    }
}

public sealed class EncoderSessionService : IDisposable
{
    private readonly MediaSession _session;
    private readonly ILogger<EncoderSessionService> logger;
    private readonly FfmpegLocator _ffmpegLocator;
    private readonly FfmpegEncoderResolver _encoderResolver;
    private readonly PublisherMediaSessionDefaultsPolicy _defaults;
    private readonly ConcurrentDictionary<string, Process> _processes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PipelineInputWriter> _inputWriters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string[]> _processArguments = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _processInputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte[]> _initializationChunks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _restartAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _manualStops = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private readonly FfmpegEncoderSet _videoEncoders;
    private readonly List<string> _recordingPatterns = [];
    private bool _disposed;

    public EncoderSessionService(
        MediaSession session,
        FfmpegLocator ffmpegLocator,
        FfmpegEncoderResolver encoderResolver,
        PublisherMediaSessionDefaultsPolicy defaults,
        ILogger<EncoderSessionService> logger)
    {
        _session = session;
        _ffmpegLocator = ffmpegLocator;
        _encoderResolver = encoderResolver;
        _defaults = defaults;
        this.logger = logger;
        _videoEncoders = _encoderResolver.Resolve(session.FfmpegPath, session.HardwareEncoder);
    }

    public string Status { get; private set; } = "waiting-for-renderer";
    public string LastError { get; private set; } = string.Empty;

    public void Start()
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.Start.");
                    if (_session.HasIngest(null)) NotifyIngest(null);
                    foreach (var outputId in _session.OutputIngests.Keys) NotifyIngest(outputId);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.Start failed: {exception.Message}");
            throw;
        }
    }

    public void NotifyIngest(Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.NotifyIngest.");
                    lock (_sync)
                    {
                        if (_disposed || !_session.HasIngest(inputId)) return;
                        Status = "starting";
                        if (_session.DryRun)
                        {
                            StartValidation(inputId);
                        }
                        else if (inputId is { } outputId)
                        {
                            var output = _session.OutputDefinitions.FirstOrDefault(item => item.OutputId == outputId);
                            if (output is not null && _session.Outputs.GetValueOrDefault(outputId)) StartOutput(output);
                        }

                        if (_session.Recording) StartRecordingForInput(inputId);
                        if (inputId is null)
                        {
                            if (_session.LanEnabled && _session.LanDefinition.EnableHls) StartLanHls();
                            if (_session.LanEnabled && _session.LanDefinition.EnableRtsp) StartLanRtsp();
                        }
                        Status = _processes.Count == 0 ? "prepared-no-ffmpeg-output" : "encoding";
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.NotifyIngest failed: {exception.Message}");
            throw;
        }
    }

    public void PushChunk(Guid? inputId, ReadOnlySpan<byte> chunk)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.PushChunk.");
                    if (chunk.IsEmpty) return;
                    var inputKey = InputKey(inputId);
                    var payload = chunk.ToArray();
                    lock (_sync)
                    {
                        if (_disposed) return;
                        if (IsPipedInput(inputId)) _initializationChunks.TryAdd(inputKey, payload);
                        foreach (var pair in _inputWriters.ToArray())
                        {
                            if (!_processInputs.TryGetValue(pair.Key, out var processInput)
                                || !string.Equals(processInput, inputKey, StringComparison.OrdinalIgnoreCase)) continue;
                            if (pair.Value.TryWrite(payload)) continue;
                            LastError = $"FFmpeg pipeline '{pair.Key}' could not keep up with the browser ingest and will reconnect.";
                            pair.Value.AbortForBackpressure();
                        }
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.PushChunk failed: {exception.Message}");
            throw;
        }
    }

    public void SetOutput(Guid outputId, bool enabled)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.SetOutput.");
                    lock (_sync)
                    {
                        if (_disposed || _session.DryRun) return;
                        var key = OutputKey(outputId);
                        if (!enabled) { StopProcess(key); return; }
                        var output = _session.OutputDefinitions.FirstOrDefault(item => item.OutputId == outputId);
                        if (output is not null && _session.HasIngest(outputId)) StartOutput(output);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.SetOutput failed: {exception.Message}");
            throw;
        }
    }

    public void SetRecording(bool enabled)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.SetRecording.");
                    lock (_sync)
                    {
                        if (_disposed) return;
                        if (enabled)
                        {
                            foreach (var variant in ResolveRecordingVariants())
                                if (_session.HasIngest(variant.InputOutputId)) StartRecordingVariant(variant);
                        }
                        else
                        {
                            StopProcessesWithPrefix("recording:");
                            ScheduleRecordingRemux();
                        }
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.SetRecording failed: {exception.Message}");
            throw;
        }
    }

    private void StartValidation(Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartValidation.");
                    var suffix = inputId?.ToString("N") ?? "master";
                    var output = inputId is { } id ? _session.OutputDefinitions.FirstOrDefault(item => item.OutputId == id) : null;
                    var width = output?.Width ?? _session.MasterWidth;
                    var height = output?.Height ?? _session.MasterHeight;
                    var frameRate = output?.FrameRate ?? _session.MasterFrameRate;
                    var bitrate = output?.VideoBitrateKbps ?? 8000;
                    StartProcess($"dry-run:{suffix}", inputId, BuildValidationArguments(inputId, width, height, frameRate, bitrate));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartValidation failed: {exception.Message}");
            throw;
        }
    }

    private void StartOutput(MediaOutputDefinition output)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartOutput.");
                    if (!_session.HasIngest(output.OutputId) || string.IsNullOrWhiteSpace(output.Endpoint)) return;
                    var destination = BuildDestination(output);
                    if (string.IsNullOrWhiteSpace(destination)) return;
                    StartProcess(OutputKey(output.OutputId), output.OutputId, BuildOutputArguments(output, destination));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartOutput failed: {exception.Message}");
            throw;
        }
    }

    private void StartRecordingForInput(Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartRecordingForInput.");
                    foreach (var variant in ResolveRecordingVariants().Where(item => item.InputOutputId == inputId))
                        StartRecordingVariant(variant);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartRecordingForInput failed: {exception.Message}");
            throw;
        }
    }

    private void StartRecordingVariant(RecordingVariant variant)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartRecordingVariant.");
                    if (!_session.HasIngest(variant.InputOutputId)) return;
                    var directory = string.IsNullOrWhiteSpace(_session.RecordingDefinition.DestinationDirectory)
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "PublisherStudio")
                        : _session.RecordingDefinition.DestinationDirectory;
                    try { Directory.CreateDirectory(directory); }
                    catch (Exception exception) { LastError = exception.Message; return; }

                    var safeName = SafeFileName($"{_session.Name}-{variant.Name}");
                    var extension = NormalizeContainer(_session.RecordingDefinition.Container);
                    var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                    var path = _session.RecordingDefinition.SegmentSeconds > 0
                        ? Path.Combine(directory, $"{safeName}-{stamp}-%05d.{extension}")
                        : Path.Combine(directory, $"{safeName}-{stamp}.{extension}");
                    StartProcess($"recording:{variant.Id}", variant.InputOutputId, BuildRecordingArguments(variant, path));
                    if (!_recordingPatterns.Contains(path, StringComparer.OrdinalIgnoreCase)) _recordingPatterns.Add(path);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartRecordingVariant failed: {exception.Message}");
            throw;
        }
    }

    private IReadOnlyList<RecordingVariant> ResolveRecordingVariants()
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.ResolveRecordingVariants.");
                    if (_session.RecordingDefinition.Variant == 0)
                        return
                        [
                            new RecordingVariant(
                                "clean-master",
                                "Clean Master",
                                null,
                                _session.MasterWidth,
                                _session.MasterHeight,
                                _session.MasterFrameRate,
                                RecommendedRecordingBitrateKbps(_session.MasterWidth, _session.MasterHeight, _session.MasterFrameRate),
                                192,
                                0,
                                0)
                        ];
                    IEnumerable<MediaOutputDefinition> outputs = _session.RecordingDefinition.Variant == 2
                        ? _session.OutputDefinitions.Where(item => _session.RecordingDefinition.SelectedOutputIds.Contains(item.OutputId))
                        : _session.OutputDefinitions.Where(item => _session.Outputs.GetValueOrDefault(item.OutputId));
                    return outputs.Select(item => new RecordingVariant(
                        item.OutputId.ToString("N"),
                        item.Name,
                        item.OutputId,
                        item.Width,
                        item.Height,
                        item.FrameRate,
                        item.VideoBitrateKbps,
                        item.AudioBitrateKbps,
                        item.VideoCodec,
                        item.AudioCodec)).ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.ResolveRecordingVariants failed: {exception.Message}");
            throw;
        }
    }

    private void StartLanHls()
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartLanHls.");
                    if (!_session.HasIngest(null)) return;
                    var directory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PublisherStudio", "Streaming", "Hls", _session.Id.ToString("N"));
                    Directory.CreateDirectory(directory);
                    _session.HlsDirectory = directory;
                    var playlist = Path.Combine(directory, "index.m3u8");
                    var args = BaseInputArguments(null);
                    AddVideoEncoding(args, _session.LanDefinition.Width, _session.LanDefinition.Height, _session.LanDefinition.FrameRate, _session.LanDefinition.VideoBitrateKbps, 2, 0);
                    AddAudioEncoding(args, 160, 0);
                    args.AddRange(["-f", "hls", "-hls_time", "2", "-hls_list_size", "8", "-hls_flags", "delete_segments+append_list+independent_segments", playlist]);
                    StartProcess("lan:hls", null, args);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartLanHls failed: {exception.Message}");
            throw;
        }
    }

    private void StartLanRtsp()
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StartLanRtsp.");
                    if (!_session.HasIngest(null)) return;
                    if (_session.RtspRelayPort <= 0) return;
                    var args = BaseInputArguments(null);
                    AddVideoEncoding(args, _session.LanDefinition.Width, _session.LanDefinition.Height, _session.LanDefinition.FrameRate, _session.LanDefinition.VideoBitrateKbps, 2, 0);
                    AddAudioEncoding(args, 160, 0);
                    args.AddRange(["-f", "rtp_mpegts", $"rtp://127.0.0.1:{_session.RtspRelayPort}?pkt_size=1316"]);
                    StartProcess("lan:rtsp", null, args);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StartLanRtsp failed: {exception.Message}");
            throw;
        }
    }

    private List<string> BuildValidationArguments(Guid? inputId, int width, int height, int frameRate, int bitrateKbps)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.BuildValidationArguments.");
                    var args = BaseInputArguments(inputId);
                    AddVideoEncoding(args, width, height, frameRate, bitrateKbps, 2, 0);
                    AddAudioEncoding(args, 160, 0);
                    args.AddRange(["-f", "null", "-"]);
                    return args;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.BuildValidationArguments failed: {exception.Message}");
            throw;
        }
    }

    private List<string> BuildOutputArguments(MediaOutputDefinition output, string destination)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.BuildOutputArguments.");
                    var args = BaseInputArguments(output.OutputId);
                    AddVideoEncoding(args, output.Width, output.Height, output.FrameRate, output.VideoBitrateKbps, output.KeyFrameIntervalSeconds, output.VideoCodec);
                    AddAudioEncoding(args, output.AudioBitrateKbps, output.AudioCodec);
                    if (output.Transport == 2)
                        args.AddRange(["-f", "mpegts", destination]);
                    else if (output.Transport == 5)
                        args.AddRange(["-rtsp_transport", "tcp", "-f", "rtsp", destination]);
                    else
                        args.AddRange(["-f", "flv", destination]);
                    return args;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.BuildOutputArguments failed: {exception.Message}");
            throw;
        }
    }

    private List<string> BuildRecordingArguments(RecordingVariant variant, string path)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.BuildRecordingArguments.");
                    var args = BaseInputArguments(variant.InputOutputId);
                    var container = NormalizeContainer(_session.RecordingDefinition.Container);
                    if (container == "webm")
                    {
                        AddWebmVideoEncoding(args, variant.Width, variant.Height, variant.FrameRate, variant.VideoBitrateKbps, 2);
                        AddAudioEncoding(args, variant.AudioBitrateKbps, 1);
                    }
                    else
                    {
                        AddVideoEncoding(args, variant.Width, variant.Height, variant.FrameRate, variant.VideoBitrateKbps, 2, variant.VideoCodec);
                        AddAudioEncoding(args, variant.AudioBitrateKbps, variant.AudioCodec);
                    }
                    if (_session.RecordingDefinition.SegmentSeconds > 0)
                        args.AddRange(["-f", "segment", "-segment_time", _session.RecordingDefinition.SegmentSeconds.ToString(CultureInfo.InvariantCulture), "-reset_timestamps", "1", path]);
                    else
                        args.Add(path);
                    return args;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.BuildRecordingArguments failed: {exception.Message}");
            throw;
        }
    }

    private List<string> BaseInputArguments(Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.BaseInputArguments.");
                    var ingest = _session.GetIngest(inputId) ?? throw new InvalidOperationException("The requested ingest stream is not available.");
                    var args = new List<string> { "-hide_banner", "-loglevel", "warning", "-fflags", "+genpts" };
                    if (_session.PreferDeviceTimestamps) args.AddRange(["-copyts", "-start_at_zero"]);
                    if (string.Equals(ingest.Kind, "webm-websocket", StringComparison.OrdinalIgnoreCase)) args.AddRange(["-f", "webm", "-i", "pipe:0"]);
                    else args.AddRange(["-i", ingest.Url]);
                    args.AddRange(["-map", "0:v:0", "-map", "0:a?"]);
                    return args;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.BaseInputArguments failed: {exception.Message}");
            throw;
        }
    }

    private void AddVideoEncoding(List<string> args, int width, int height, int frameRate, int bitrateKbps, int keyframeSeconds, int codec)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.AddVideoEncoding.");
                    var encoder = _videoEncoders.ForCodec(codec);
                    args.AddRange([
                        "-vf", $"scale={Math.Max(2, width)}:{Math.Max(2, height)}:force_original_aspect_ratio=decrease,pad={Math.Max(2, width)}:{Math.Max(2, height)}:(ow-iw)/2:(oh-ih)/2",
                        "-r", Math.Clamp(frameRate, 15, 120).ToString(CultureInfo.InvariantCulture),
                        "-c:v", encoder.Name
                    ]);
                    args.AddRange(encoder.Options);
                    args.AddRange([
                        "-b:v", $"{Math.Max(250, bitrateKbps)}k",
                        "-maxrate", $"{Math.Max(250, bitrateKbps)}k",
                        "-bufsize", $"{Math.Max(500, bitrateKbps * 2)}k",
                        "-g", Math.Max(1, frameRate * Math.Max(1, keyframeSeconds)).ToString(CultureInfo.InvariantCulture),
                        "-pix_fmt", "yuv420p"
                    ]);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.AddVideoEncoding failed: {exception.Message}");
            throw;
        }
    }

    private void AddWebmVideoEncoding(List<string> args, int width, int height, int frameRate, int bitrateKbps, int keyframeSeconds)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.AddWebmVideoEncoding.");
                    args.AddRange([
                        "-vf", $"scale={Math.Max(2, width)}:{Math.Max(2, height)}:force_original_aspect_ratio=decrease,pad={Math.Max(2, width)}:{Math.Max(2, height)}:(ow-iw)/2:(oh-ih)/2",
                        "-r", Math.Clamp(frameRate, 15, 120).ToString(CultureInfo.InvariantCulture),
                        "-c:v", "libvpx-vp9",
                        "-deadline", "realtime",
                        "-cpu-used", "4",
                        "-row-mt", "1",
                        "-b:v", $"{Math.Max(250, bitrateKbps)}k",
                        "-maxrate", $"{Math.Max(250, bitrateKbps)}k",
                        "-bufsize", $"{Math.Max(500, bitrateKbps * 2)}k",
                        "-g", Math.Max(1, frameRate * Math.Max(1, keyframeSeconds)).ToString(CultureInfo.InvariantCulture),
                        "-pix_fmt", "yuv420p"
                    ]);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.AddWebmVideoEncoding failed: {exception.Message}");
            throw;
        }
    }

    private int RecommendedRecordingBitrateKbps(int width, int height, int frameRate)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.RecommendedRecordingBitrateKbps.");
                    var bitsPerSecond = (long)Math.Round(Math.Max(2, width) * (double)Math.Max(2, height) * Math.Clamp(frameRate, 15, 120) * 0.16, MidpointRounding.AwayFromZero);
                    return (int)Math.Clamp(bitsPerSecond / 1000L, 12_000L, 120_000L);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.RecommendedRecordingBitrateKbps failed: {exception.Message}");
            throw;
        }
    }

    private void AddAudioEncoding(List<string> args, int bitrateKbps, int codec) {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.AddAudioEncoding.");
            args.AddRange(["-c:a", codec == 1 ? "libopus" : "aac", "-b:a", $"{Math.Max(32, bitrateKbps)}k", "-ar", "48000"]);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.AddAudioEncoding failed: {exception.Message}");
            throw;
        }
    }

    private string BuildDestination(MediaOutputDefinition output)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.BuildDestination.");
                    var destination = output.Endpoint.Trim();
                    if (!string.IsNullOrWhiteSpace(output.Secret))
                    {
                        var encodedSecret = Uri.EscapeDataString(output.Secret);
                        if (destination.Contains("{streamKey}", StringComparison.OrdinalIgnoreCase))
                            destination = destination.Replace("{streamKey}", encodedSecret, StringComparison.OrdinalIgnoreCase);
                        else if (destination.Contains("{stream_key}", StringComparison.OrdinalIgnoreCase))
                            destination = destination.Replace("{stream_key}", encodedSecret, StringComparison.OrdinalIgnoreCase);
                        else
                            destination = destination.TrimEnd('/') + "/" + output.Secret.TrimStart('/');
                    }
                    if (output.Provider == 0 && output.TestMode && !destination.Contains("bandwidthtest=true", StringComparison.OrdinalIgnoreCase))
                        destination += destination.Contains('?') ? "&bandwidthtest=true" : "?bandwidthtest=true";
                    return destination;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.BuildDestination failed: {exception.Message}");
            throw;
        }
    }

    private void StartProcess(string key, Guid? inputId, IReadOnlyList<string> arguments, bool restart = false)
    {
        try
        {
            logger.LogTrace($"Starting FFmpeg pipeline {key} for input {inputId}.");
            if (!_session.HasIngest(inputId) || _processes.ContainsKey(key)) return;
            var executable = _ffmpegLocator.Resolve(_session.FfmpegPath) ?? "ffmpeg";
            var argumentCopy = arguments.ToArray();
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in argumentCopy) startInfo.ArgumentList.Add(argument);
            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (string.IsNullOrWhiteSpace(eventArgs.Data)) return;
                LastError = eventArgs.Data;
                logger.LogWarning("FFmpeg {Pipeline}: {Line}", key, eventArgs.Data);
            };
            process.Exited += (_, _) => HandleProcessExit(key, inputId, process, argumentCopy);
            if (!process.Start()) return;
            _processArguments[key] = argumentCopy;
            _processInputs[key] = InputKey(inputId);
            lock (_sync) _manualStops.Remove(key);
            if (!restart) _restartAttempts.TryRemove(key, out _);
            _processes[key] = process;
            var inputWriter = new PipelineInputWriter(process, key, message => LastError = message, _defaults, logger);
            _inputWriters[key] = inputWriter;
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            if (IsPipedInput(inputId) && _initializationChunks.TryGetValue(InputKey(inputId), out var initialization) && initialization.Length > 0)
                inputWriter.TryWrite(initialization);
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Status = "ffmpeg-unavailable";
            logger.LogError(exception, "Could not start FFmpeg pipeline {Pipeline}.", key);
            ScheduleRestart(key, inputId, arguments.ToArray());
        }
    }

    private void HandleProcessExit(string key, Guid? inputId, Process process, string[] arguments)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.HandleProcessExit.");
                    int exitCode;
                    try { exitCode = process.ExitCode; }
                    catch { exitCode = -1; }
                    _processes.TryRemove(key, out _);
                    if (_inputWriters.TryRemove(key, out var writer)) writer.Dispose();
                    try { process.Dispose(); } catch { }

                    bool manual;
                    lock (_sync) manual = _manualStops.Remove(key);
                    if (_disposed || manual || !ShouldRun(key, inputId))
                    {
                        _processArguments.TryRemove(key, out _);
                        _processInputs.TryRemove(key, out _);
                        _restartAttempts.TryRemove(key, out _);
                        return;
                    }

                    Status = "reconnecting";
                    LastError = $"FFmpeg pipeline '{key}' exited with code {exitCode}.";
                    ScheduleRestart(key, inputId, arguments);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.HandleProcessExit failed: {exception.Message}");
            throw;
        }
    }

    private void ScheduleRestart(string key, Guid? inputId, string[] arguments)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.ScheduleRestart.");
                    if (_disposed || !ShouldRun(key, inputId)) return;
                    var attempt = _restartAttempts.AddOrUpdate(key, 1, (_, value) => Math.Min(20, value + 1));
                    var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(5, attempt))));
                    _ = Task.Run(async () =>
                    {
                        try { await Task.Delay(delay).ConfigureAwait(false); }
                        catch { return; }
                        lock (_sync)
                        {
                            if (_disposed || !ShouldRun(key, inputId) || _processes.ContainsKey(key)) return;
                            StartProcess(key, inputId, arguments, restart: true);
                            if (_processes.ContainsKey(key)) Status = "encoding";
                        }
                    });
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.ScheduleRestart failed: {exception.Message}");
            throw;
        }
    }

    private bool ShouldRun(string key, Guid? inputId)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.ShouldRun.");
                    if (_disposed || !_session.HasIngest(inputId)) return false;
                    if (key.StartsWith("output:", StringComparison.OrdinalIgnoreCase)
                        && Guid.TryParseExact(key[7..], "N", out var outputId))
                        return !_session.DryRun && _session.Outputs.GetValueOrDefault(outputId);
                    if (key.StartsWith("recording:", StringComparison.OrdinalIgnoreCase)) return _session.Recording;
                    if (string.Equals(key, "lan:hls", StringComparison.OrdinalIgnoreCase)) return _session.LanEnabled && _session.LanDefinition.EnableHls;
                    if (string.Equals(key, "lan:rtsp", StringComparison.OrdinalIgnoreCase)) return _session.LanEnabled && _session.LanDefinition.EnableRtsp;
                    return key.StartsWith("dry-run:", StringComparison.OrdinalIgnoreCase) && _session.DryRun;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.ShouldRun failed: {exception.Message}");
            throw;
        }
    }

    private void StopProcessesWithPrefix(string prefix)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StopProcessesWithPrefix.");
                    foreach (var key in _processes.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray()) StopProcess(key);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StopProcessesWithPrefix failed: {exception.Message}");
            throw;
        }
    }

    private void StopProcess(string key)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.StopProcess.");
                    lock (_sync) _manualStops.Add(key);
                    _processArguments.TryRemove(key, out _);
                    _processInputs.TryRemove(key, out _);
                    _restartAttempts.TryRemove(key, out _);
                    if (_inputWriters.TryRemove(key, out var writer)) writer.Complete();
                    if (!_processes.TryRemove(key, out var process)) { writer?.Dispose(); return; }
                    try
                    {
                        writer?.Wait(TimeSpan.FromSeconds(2));
                        if (!process.HasExited)
                        {
                            try { process.StandardInput.Close(); } catch { }
                            if (!process.WaitForExit(8000)) process.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                    finally { writer?.Dispose(); process.Dispose(); }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.StopProcess failed: {exception.Message}");
            throw;
        }
    }

    private void ScheduleRecordingRemux()
    {
        try
        {
            logger.LogTrace($"Scheduling recording remux work for media session {_session.Id}.");
            if (!_session.RecordingDefinition.RemuxToMp4AfterStop
                || !string.Equals(NormalizeContainer(_session.RecordingDefinition.Container), "mkv", StringComparison.OrdinalIgnoreCase)
                || _recordingPatterns.Count == 0)
            {
                return;
            }

            var patterns = _recordingPatterns.ToArray();
            _recordingPatterns.Clear();
            var executable = _ffmpegLocator.Resolve(_session.FfmpegPath) ?? "ffmpeg";
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var pattern in patterns)
                    {
                        foreach (var input in ResolveRecordingFiles(pattern))
                        {
                            var output = Path.ChangeExtension(input, ".mp4");
                            if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
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
                                foreach (var argument in _defaults.RecordingRemuxArgumentsBeforeInput)
                                {
                                    startInfo.ArgumentList.Add(argument);
                                }
                                startInfo.ArgumentList.Add(input);
                                foreach (var argument in _defaults.RecordingRemuxArgumentsAfterInput)
                                {
                                    startInfo.ArgumentList.Add(argument);
                                }
                                startInfo.ArgumentList.Add(output);
                                using var process = Process.Start(startInfo);
                                if (process is null)
                                {
                                    continue;
                                }
                                var stdout = process.StandardOutput.ReadToEndAsync();
                                var stderr = process.StandardError.ReadToEndAsync();
                                await process.WaitForExitAsync().ConfigureAwait(false);
                                _ = await stdout.ConfigureAwait(false);
                                var error = await stderr.ConfigureAwait(false);
                                if (process.ExitCode == _defaults.SuccessExitCode)
                                {
                                    logger.LogInformation("Remuxed recording {Input} to {Output}.", input, output);
                                }
                                else
                                {
                                    logger.LogWarning("Could not remux recording {Input}: {Error}", input, error);
                                }
                            }
                            catch (Exception exception)
                            {
                                logger.LogWarning(exception, "Could not remux recording {Input}.", input);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Recording remux background work failed for media session {_session.Id}.");
                }
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not schedule recording remux work for media session {_session.Id}.");
            throw;
        }
    }

    private IEnumerable<string> ResolveRecordingFiles(string pattern)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.ResolveRecordingFiles.");
                    var directory = Path.GetDirectoryName(pattern);
                    var fileName = Path.GetFileName(pattern);
                    if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory) || string.IsNullOrWhiteSpace(fileName)) return [];
                    if (!fileName.Contains('%')) return File.Exists(pattern) ? [pattern] : [];
                    var wildcard = System.Text.RegularExpressions.Regex.Replace(fileName, @"%0?\d*d", "*");
                    return Directory.EnumerateFiles(directory, wildcard).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.ResolveRecordingFiles failed: {exception.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.Dispose.");
                    lock (_sync)
                    {
                        if (_disposed) return;
                        _disposed = true;
                        foreach (var key in _processes.Keys.ToArray()) StopProcess(key);
                        ScheduleRecordingRemux();
                        Status = "stopped";
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.Dispose failed: {exception.Message}");
            throw;
        }
    }

    private bool IsPipedInput(Guid? inputId) {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.IsPipedInput.");
            return string.Equals(_session.GetIngest(inputId)?.Kind, "webm-websocket", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.IsPipedInput failed: {exception.Message}");
            throw;
        }
    }

    private string InputKey(Guid? id) {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.InputKey.");
            return id?.ToString("N") ?? "master";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.InputKey failed: {exception.Message}");
            throw;
        }
    }
    private string OutputKey(Guid id) {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.OutputKey.");
            return $"output:{id:N}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.OutputKey failed: {exception.Message}");
            throw;
        }
    }
    private string NormalizeContainer(string value) {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.NormalizeContainer.");
            return value.Trim().ToLowerInvariant() switch { "mp4" => "mp4", "webm" => "webm", _ => "mkv" };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.NormalizeContainer failed: {exception.Message}");
            throw;
        }
    }
    private string SafeFileName(string value)
    {
        try
        {
            logger.LogTrace($"Entering EncoderSessionService.SafeFileName.");
                    var invalid = Path.GetInvalidFileNameChars();
                    var clean = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
                    return string.IsNullOrWhiteSpace(clean) ? "PublisherStudio" : clean;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EncoderSessionService.SafeFileName failed: {exception.Message}");
            throw;
        }
    }

    private sealed class PipelineInputWriter : IDisposable
    {
        private readonly Process process;
        private readonly string key;
        private readonly Action<string> reportError;
        private readonly Channel<byte[]> queue;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task pump;
        private readonly PublisherMediaSessionDefaultsPolicy defaults;
        private readonly ILogger logger;
        private int completed;
        private int aborting;

        public PipelineInputWriter(
            Process process,
            string key,
            Action<string> reportError,
            PublisherMediaSessionDefaultsPolicy defaults,
            ILogger logger)
        {
            try
            {
                this.process = process;
                this.key = key;
                this.reportError = reportError;
                this.defaults = defaults;
                this.logger = logger;
                queue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(defaults.EncoderPipelineChannelCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
                pump = Task.Run(PumpAsync);
                logger.LogTrace($"Initialized FFmpeg pipeline writer {key}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not initialize FFmpeg pipeline writer {key}.");
                throw;
            }
        }

        public bool TryWrite(byte[] payload)
        {
            try
            {
                var written = Volatile.Read(ref completed) == 0 && queue.Writer.TryWrite(payload);
                logger.LogTrace($"FFmpeg pipeline {key} accepted payload state is {written}.");
                return written;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not queue payload data for FFmpeg pipeline {key}.");
                throw;
            }
        }

        public void AbortForBackpressure()
        {
            try
            {
                if (Interlocked.Exchange(ref aborting, 1) != 0)
                {
                    return;
                }
                reportError($"FFmpeg pipeline '{key}' exceeded its ingest buffer and is restarting instead of blocking the other outputs.");
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, $"Could not stop backpressured FFmpeg pipeline {key}.");
                    }
                });
                logger.LogWarning($"Aborted FFmpeg pipeline {key} because of backpressure.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not abort backpressured FFmpeg pipeline {key}.");
                throw;
            }
        }

        public void Complete()
        {
            try
            {
                if (Interlocked.Exchange(ref completed, 1) != 0)
                {
                    return;
                }
                queue.Writer.TryComplete();
                logger.LogTrace($"Completed FFmpeg pipeline writer {key}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not complete FFmpeg pipeline writer {key}.");
                throw;
            }
        }

        public void Wait(TimeSpan timeout)
        {
            try
            {
                pump.Wait(timeout);
                logger.LogTrace($"Waited for FFmpeg pipeline writer {key}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not wait for FFmpeg pipeline writer {key}.");
                throw;
            }
        }

        private async Task PumpAsync()
        {
            try
            {
                logger.LogTrace($"Started pumping FFmpeg pipeline {key}.");
                var flushCounter = 0;
                await foreach (var payload in queue.Reader.ReadAllAsync(cancellation.Token))
                {
                    if (process.HasExited)
                    {
                        break;
                    }
                    await process.StandardInput.BaseStream.WriteAsync(payload, cancellation.Token).ConfigureAwait(false);
                    flushCounter++;
                    if (flushCounter % defaults.EncoderPipelineFlushInterval == 0)
                    {
                        await process.StandardInput.BaseStream.FlushAsync(cancellation.Token).ConfigureAwait(false);
                    }
                }
                if (!process.HasExited)
                {
                    await process.StandardInput.BaseStream.FlushAsync(cancellation.Token).ConfigureAwait(false);
                }
                logger.LogTrace($"Stopped pumping FFmpeg pipeline {key}.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                logger.LogTrace($"FFmpeg pipeline {key} was cancelled.");
            }
            catch (Exception exception)
            {
                reportError($"FFmpeg pipeline '{key}' ingest failed: {exception.Message}");
                logger.LogError(exception, $"FFmpeg pipeline {key} ingest failed.");
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception stopException)
                {
                    logger.LogWarning(stopException, $"Could not stop failed FFmpeg pipeline {key}.");
                }
            }
        }

        public void Dispose()
        {
            try
            {
                Complete();
                cancellation.Cancel();
                pump.Wait(TimeSpan.FromSeconds(defaults.EncoderPipelineShutdownSeconds));
                cancellation.Dispose();
                logger.LogTrace($"Disposed FFmpeg pipeline writer {key}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Could not dispose FFmpeg pipeline writer {key}.");
                throw;
            }
        }
    }

    private sealed record RecordingVariant(
        string Id,
        string Name,
        Guid? InputOutputId,
        int Width,
        int Height,
        int FrameRate,
        int VideoBitrateKbps,
        int AudioBitrateKbps,
        int VideoCodec,
        int AudioCodec);
}

public sealed record FfmpegVideoEncoder(string Name, IReadOnlyList<string> Options);

public sealed class FfmpegEncoderSet(
    FfmpegVideoEncoder h264,
    FfmpegVideoEncoder hevc,
    FfmpegVideoEncoder av1,
    ILogger<FfmpegEncoderResolver> logger)
{
    private readonly FfmpegVideoEncoder h264Encoder = h264;
    private readonly FfmpegVideoEncoder hevcEncoder = hevc;
    private readonly FfmpegVideoEncoder av1Encoder = av1;

    public FfmpegVideoEncoder ForCodec(int codec)
    {
        try
        {
            logger.LogTrace($"Resolving FFmpeg encoder for codec {codec}.");
            return codec switch
            {
                1 => hevcEncoder,
                2 => av1Encoder,
                _ => h264Encoder
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve FFmpeg encoder for codec {codec}.");
            throw;
        }
    }
}

public sealed class FfmpegEncoderResolver(
    FfmpegLocator ffmpegLocator,
    ILogger<FfmpegEncoderResolver> logger)
{
    private readonly ConcurrentDictionary<string, HashSet<string>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> HardwareProbeCache = new(StringComparer.OrdinalIgnoreCase);

    public FfmpegEncoderSet Resolve(string configuredPath, int preference)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.Resolve.");
                    var executable = ffmpegLocator.Resolve(configuredPath) ?? "ffmpeg";
                    var available = Cache.GetOrAdd(executable, Probe);
                    var h264 = Choose(0, preference, available, executable);
                    var hevc = Choose(1, preference, available, executable);
                    var av1 = Choose(2, preference, available, executable);
                    logger.LogInformation(
                        "PublisherStudio encoder selection: H.264={H264}, HEVC={Hevc}, AV1={Av1} (preference {Preference}).",
                        h264.Name,
                        hevc.Name,
                        av1.Name,
                        preference);
                    return new FfmpegEncoderSet(h264, hevc, av1, logger);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.Resolve failed: {exception.Message}");
            throw;
        }
    }

    private FfmpegVideoEncoder Choose(int codec, int preference, HashSet<string> available, string executable)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.Choose.");
                    if (preference == 0 && codec != 0) return SoftwareFallback(codec, available);
                    var families = preference switch
                    {
                        1 => new[] { "software" },
                        2 => new[] { "nvenc", "software" },
                        3 => new[] { "qsv", "software" },
                        4 => new[] { "amf", "software" },
                        5 => new[] { "videotoolbox", "software" },
                        _ when OperatingSystem.IsMacOS() => new[] { "videotoolbox", "nvenc", "qsv", "amf", "software" },
                        _ => new[] { "nvenc", "qsv", "amf", "videotoolbox", "software" }
                    };

                    foreach (var family in families)
                    {
                        var candidate = Candidate(codec, family, available, executable);
                        if (candidate is not null) return candidate;
                    }
                    return SoftwareFallback(codec, available);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.Choose failed: {exception.Message}");
            throw;
        }
    }

    private FfmpegVideoEncoder? Candidate(int codec, string family, HashSet<string> available, string executable)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.Candidate.");
                    var name = (codec, family) switch
                    {
                        (0, "nvenc") => "h264_nvenc",
                        (1, "nvenc") => "hevc_nvenc",
                        (2, "nvenc") => "av1_nvenc",
                        (0, "qsv") => "h264_qsv",
                        (1, "qsv") => "hevc_qsv",
                        (2, "qsv") => "av1_qsv",
                        (0, "amf") => "h264_amf",
                        (1, "amf") => "hevc_amf",
                        (2, "amf") => "av1_amf",
                        (0, "videotoolbox") => "h264_videotoolbox",
                        (1, "videotoolbox") => "hevc_videotoolbox",
                        (2, "videotoolbox") => "av1_videotoolbox",
                        _ => string.Empty
                    };
                    if (family == "software") return SoftwareFallback(codec, available);
                    if (name.Length == 0 || !available.Contains(name) || !CanInitializeHardwareEncoder(executable, name)) return null;
                    return new FfmpegVideoEncoder(name, OptionsFor(name));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.Candidate failed: {exception.Message}");
            throw;
        }
    }

    private FfmpegVideoEncoder SoftwareFallback(int codec, HashSet<string> available)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.SoftwareFallback.");
                    var candidates = codec switch
                    {
                        1 => new[] { "libx265", "hevc_videotoolbox", "hevc_qsv", "hevc_nvenc" },
                        2 => new[] { "libsvtav1", "libaom-av1", "av1_qsv", "av1_nvenc" },
                        _ => new[] { "libx264", "h264_videotoolbox", "h264_qsv", "h264_nvenc" }
                    };
                    var name = candidates.FirstOrDefault(available.Contains) ?? candidates[0];
                    return new FfmpegVideoEncoder(name, OptionsFor(name));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.SoftwareFallback failed: {exception.Message}");
            throw;
        }
    }

    private bool CanInitializeHardwareEncoder(string executable, string encoder)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.CanInitializeHardwareEncoder.");
                    var cacheKey = $"{executable}|{encoder}";
                    return HardwareProbeCache.GetOrAdd(cacheKey, _ =>
                    {
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
                            foreach (var argument in new[]
                            {
                                "-hide_banner", "-loglevel", "error",
                                "-f", "lavfi", "-i", "color=size=128x128:rate=1",
                                "-frames:v", "1", "-an", "-c:v", encoder,
                                "-f", "null", "-"
                            }) startInfo.ArgumentList.Add(argument);
                            using var process = Process.Start(startInfo);
                            if (process is null) return false;
                            var stdout = process.StandardOutput.ReadToEndAsync();
                            var stderr = process.StandardError.ReadToEndAsync();
                            if (!process.WaitForExit(3500))
                            {
                                try { process.Kill(entireProcessTree: true); } catch { }
                                return false;
                            }
                            _ = stdout.GetAwaiter().GetResult();
                            _ = stderr.GetAwaiter().GetResult();
                            return process.ExitCode == 0;
                        }
                        catch
                        {
                            return false;
                        }
                    });
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.CanInitializeHardwareEncoder failed: {exception.Message}");
            throw;
        }
    }

    private IReadOnlyList<string> OptionsFor(string encoder) {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.OptionsFor.");
            return encoder switch
    {
        "libsvtav1" => ["-preset", "8", "-svtav1-params", "tune=0"],
        "libaom-av1" => ["-deadline", "realtime", "-cpu-used", "6", "-row-mt", "1"],
        var name when name.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase) => ["-preset", "p5", "-tune", "hq", "-rc", "cbr"],
        var name when name.EndsWith("_qsv", StringComparison.OrdinalIgnoreCase) => ["-preset", "faster"],
        var name when name.EndsWith("_amf", StringComparison.OrdinalIgnoreCase) => ["-quality", "balanced", "-rc", "cbr"],
        var name when name.EndsWith("_videotoolbox", StringComparison.OrdinalIgnoreCase) => ["-realtime", "1"],
        _ => ["-preset", "veryfast"]
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.OptionsFor failed: {exception.Message}");
            throw;
        }
    }

    private HashSet<string> Probe(string executable)
    {
        try
        {
            logger.LogTrace($"Entering FfmpegEncoderResolver.Probe.");
                    var known = new[]
                    {
                        "libx264", "libx265", "libsvtav1", "libaom-av1", "libvpx-vp9",
                        "h264_nvenc", "hevc_nvenc", "av1_nvenc",
                        "h264_qsv", "hevc_qsv", "av1_qsv",
                        "h264_amf", "hevc_amf", "av1_amf",
                        "h264_videotoolbox", "hevc_videotoolbox", "av1_videotoolbox"
                    };
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
                        startInfo.ArgumentList.Add("-hide_banner");
                        startInfo.ArgumentList.Add("-encoders");
                        using var process = Process.Start(startInfo);
                        if (process is null) return [];
                        var stdout = process.StandardOutput.ReadToEndAsync();
                        var stderr = process.StandardError.ReadToEndAsync();
                        if (!process.WaitForExit(4000))
                        {
                            try { process.Kill(entireProcessTree: true); } catch { }
                            return [];
                        }
                        var output = stdout.GetAwaiter().GetResult() + "\n" + stderr.GetAwaiter().GetResult();
                        return known.Where(name => output.Contains(name, StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return [];
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FfmpegEncoderResolver.Probe failed: {exception.Message}");
            throw;
        }
    }
}

public sealed class MediaOutputDefinition
{
    public Guid OutputId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Provider { get; set; }
    public int Transport { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool ChatEnabled { get; set; }
    public string ChatSecret { get; set; } = string.Empty;
    public bool TestMode { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameRate { get; set; }
    public int VideoBitrateKbps { get; set; }
    public int AudioBitrateKbps { get; set; }
    public int KeyFrameIntervalSeconds { get; set; }
    public int VideoCodec { get; set; }
    public int AudioCodec { get; set; }
}

public sealed class MediaRecordingDefinition
{
    public bool Enabled { get; set; }
    public string DestinationDirectory { get; set; } = string.Empty;
    public int Variant { get; set; }
    public HashSet<Guid> SelectedOutputIds { get; set; } = [];
    public string Container { get; set; } = string.Empty;
    public int SegmentSeconds { get; set; }
    public bool RemuxToMp4AfterStop { get; set; }
}
