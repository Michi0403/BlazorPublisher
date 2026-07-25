using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PublisherStudio.Domain;
using PublisherStudio.Services.Streaming.Encoding;
using TextEncoding = global::System.Text.Encoding;

namespace PublisherStudio.Services.MediaConversion;

public sealed class MediaConversionService : IMediaConversionService, IDisposable
{
    private sealed class JobState
    {
        public required Guid Id { get; init; }
        public required string SourceFileName { get; init; }
        public required string SourcePath { get; init; }
        public required string OutputPath { get; init; }
        public required string OutputMimeType { get; init; }
        public required MediaConversionPreset Preset { get; init; }
        public required MediaConversionOptions Options { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public MediaConversionJobStatus Status { get; set; } = MediaConversionJobStatus.Queued;
        public double Progress { get; set; }
        public string Message { get; set; } = "Queued";
        public long OutputSize { get; set; }
        public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedUtc { get; set; }
        public Process? Process { get; set; }
        public object Sync { get; } = new();
    }

    private static readonly MediaConversionPreset[] Definitions =
    [
        new("webm-vp9", "Web video · WebM VP9/Opus", "Open web video for PublisherStudio HTML, dashboards and modern browsers.", "video", ".webm", "video/webm", false, true, ["libvpx-vp9", "libopus"]),
        new("webm-vp8", "Web video · WebM VP8/Opus", "Compatibility-oriented open WebM conversion.", "video", ".webm", "video/webm", false, true, ["libvpx", "libopus"]),
        new("mp4-h264", "Web video · MP4 H.264/AAC", "Broad browser/device compatibility when the installed FFmpeg build provides H.264.", "video", ".mp4", "video/mp4", false, true, ["libx264", "aac"]),
        new("video-lossless-ffv1", "Editing/archive · Matroska FFV1/FLAC", "Lossless intra-frame video and lossless audio for local editing and preservation.", "video", ".mkv", "video/x-matroska", true, false, ["ffv1", "flac"]),
        new("video-prores", "Editing · ProRes MOV/PCM", "High-quality intraframe editing intermediate where prores_ks is available.", "video", ".mov", "video/quicktime", false, false, ["prores_ks", "pcm_s16le"]),
        new("audio-opus", "Web audio · Ogg Opus", "Open, efficient audio for browsers and streaming workflows.", "audio", ".ogg", "audio/ogg", false, true, ["libopus"]),
        new("audio-webm-opus", "Web audio · WebM Opus", "WebM audio for PublisherStudio HTML exports.", "audio", ".webm", "audio/webm", false, true, ["libopus"]),
        new("audio-wav", "Audio · WAV PCM", "Lossless uncompressed PCM audio for editing and interchange.", "audio", ".wav", "audio/wav", true, false, ["pcm_s16le"]),
        new("audio-flac", "Audio · FLAC", "Lossless compressed audio for archive and supported browser workflows.", "audio", ".flac", "audio/flac", true, false, ["flac"]),
        new("image-png", "Picture · PNG", "Lossless browser-compatible raster image.", "image", ".png", "image/png", true, true, ["png"]),
        new("image-webp-lossless", "Picture · lossless WebP", "Lossless WebP with alpha support.", "image", ".webp", "image/webp", true, true, ["libwebp"]),
        new("image-webp", "Picture · compact WebP", "High-quality lossy WebP for websites and dashboards.", "image", ".webp", "image/webp", false, true, ["libwebp"]),
        new("image-avif", "Picture · AVIF", "High-efficiency AVIF still image where an AV1 encoder is available.", "image", ".avif", "image/avif", false, true, ["libaom-av1"]),
        new("image-jpeg", "Picture · JPEG", "Widely compatible photographic image output.", "image", ".jpg", "image/jpeg", false, true, ["mjpeg"])
    ];

    private static readonly Regex DurationPattern = new(@"Duration:\s*(?<h>\d+):(?<m>\d+):(?<s>\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly HashSet<string> ForbiddenAdvancedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "-i", "-progress", "-nostats", "-y", "-n", "-report", "-filter_script", "-filter_complex_script",
        "-vstats_file", "-passlogfile", "-attach", "-dump_attachment"
    };

    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();
    private readonly ILogger<MediaConversionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _root;
    private readonly string _profilesPath;
    private readonly object _profilesSync = new();
    private readonly SemaphoreSlim _capabilityLock = new(1, 1);
    private MediaConversionCapabilities? _capabilities;
    private List<MediaConversionProfile>? _userProfiles;
    private bool _disposed;

    public MediaConversionService(ILogger<MediaConversionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var publisherRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio");
        _root = Path.Combine(publisherRoot, "MediaConversions");
        _profilesPath = Path.Combine(publisherRoot, "MediaConversionProfiles.json");
        Directory.CreateDirectory(_root);
        CleanupOldDirectories();
    }

    public async Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        if (_capabilities is not null) return _capabilities;
        await _capabilityLock.WaitAsync(cancellationToken);
        try
        {
            if (_capabilities is not null) return _capabilities;
            var configuredPath = _configuration["PublisherStudio:FFmpegPath"] ?? Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_FFMPEG");
            var executable = FfmpegLocator.Resolve(configuredPath);
            if (executable is null)
            {
                _capabilities = new MediaConversionCapabilities(
                    false,
                    string.Empty,
                    string.Empty,
                    [],
                    Definitions.Select(preset => preset with { Available = false, UnavailableReason = "FFmpeg was not found." }).ToArray(),
                    "PublisherStudio does not bundle FFmpeg. The executable you install remains a separate program under its own LGPL/GPL build terms.",
                    "Install FFmpeg and place it on PATH, in PublisherStudio/tools/ffmpeg, or configure PublisherStudio:FFmpegPath / PUBLISHERSTUDIO_FFMPEG.");
                return _capabilities;
            }

            var version = await FfmpegLocator.ReadVersionAsync(executable, cancellationToken) ?? "FFmpeg detected";
            var encoders = await ReadEncodersAsync(executable, cancellationToken);
            var encoderSet = encoders.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var presets = Definitions.Select(definition =>
            {
                var missing = definition.RequiredEncoders.Where(required => !encoderSet.Contains(required)).ToArray();
                return missing.Length == 0
                    ? definition
                    : definition with { Available = false, UnavailableReason = $"Installed FFmpeg build is missing: {string.Join(", ", missing)}." };
            }).ToArray();
            _capabilities = new MediaConversionCapabilities(
                true,
                executable,
                version,
                encoders,
                presets,
                "FFmpeg is normally LGPL 2.1-or-later, but optional GPL components can make a particular build GPL. PublisherStudio invokes the separately installed executable and does not redistribute it.",
                "Use an FFmpeg build whose codec and license configuration matches your intended distribution.");
            return _capabilities;
        }
        finally
        {
            _capabilityLock.Release();
        }
    }

    public Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default) =>
        QueueAsync(source, fileName, mimeType, presetId, new MediaConversionOptions(), cancellationToken);

    public async Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, MediaConversionOptions options, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MediaConversionService));
        ArgumentNullException.ThrowIfNull(source);
        options ??= new MediaConversionOptions();
        var normalizedOptions = NormalizeOptions(options);
        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        if (!capabilities.Available) throw new InvalidOperationException("FFmpeg is not available.");
        var preset = capabilities.Presets.FirstOrDefault(candidate => string.Equals(candidate.Id, presetId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Unknown media conversion preset.", nameof(presetId));
        if (!preset.Available) throw new InvalidOperationException(preset.UnavailableReason);

        ValidateRequestedEncoders(capabilities, normalizedOptions);
        var id = Guid.NewGuid();
        var directory = Path.Combine(_root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeSourceName = SafeFileName(fileName, "source.bin");
        var sourcePath = Path.Combine(directory, safeSourceName);
        await using (var output = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, true))
            await source.CopyToAsync(output, cancellationToken);

        var extension = NormalizeExtension(normalizedOptions.OutputExtension, preset.OutputExtension);
        var outputName = SafeFileName(normalizedOptions.OutputFileName, Path.GetFileNameWithoutExtension(safeSourceName) + extension);
        if (!outputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) outputName += extension;
        var outputPath = Path.Combine(directory, outputName);
        var outputMimeType = string.IsNullOrWhiteSpace(normalizedOptions.OutputMimeType) ? preset.OutputMimeType : normalizedOptions.OutputMimeType.Trim();
        normalizedOptions.OutputExtension = extension;
        normalizedOptions.OutputMimeType = outputMimeType;
        normalizedOptions.OutputFileName = outputName;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var job = new JobState
        {
            Id = id,
            SourceFileName = safeSourceName,
            SourcePath = sourcePath,
            OutputPath = outputPath,
            OutputMimeType = outputMimeType,
            Preset = preset,
            Options = normalizedOptions,
            Cancellation = linked
        };
        if (!_jobs.TryAdd(id, job)) throw new InvalidOperationException("The conversion job could not be registered.");
        _ = Task.Run(() => ExecuteAsync(job, capabilities.Executable), CancellationToken.None);
        return Snapshot(job);
    }

    public MediaConversionJobInfo? GetJob(Guid id) => _jobs.TryGetValue(id, out var state) ? Snapshot(state) : null;

    public IReadOnlyList<MediaConversionJobInfo> GetJobs() => _jobs.Values
        .OrderByDescending(job => job.CreatedUtc)
        .Select(Snapshot)
        .ToArray();

    public Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_jobs.TryGetValue(id, out var job) || job.Status != MediaConversionJobStatus.Completed || !File.Exists(job.OutputPath))
            return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(job.OutputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, true);
        return Task.FromResult<Stream?>(stream);
    }

    public IReadOnlyList<MediaConversionProfile> GetProfiles()
    {
        lock (_profilesSync)
        {
            var profiles = BuiltInProfiles().Concat(LoadUserProfiles()).Select(CloneProfile).ToArray();
            return profiles;
        }
    }

    public MediaConversionProfile SaveProfile(MediaConversionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        lock (_profilesSync)
        {
            var profiles = LoadUserProfiles();
            var saved = CloneProfile(profile);
            saved.Id = saved.Id == Guid.Empty ? Guid.NewGuid() : saved.Id;
            saved.Name = string.IsNullOrWhiteSpace(saved.Name) ? "Custom profile" : saved.Name.Trim();
            saved.Description = saved.Description?.Trim() ?? string.Empty;
            saved.PresetId = string.IsNullOrWhiteSpace(saved.PresetId) ? "webm-vp9" : saved.PresetId.Trim();
            saved.BuiltIn = false;
            saved.ModifiedUtc = DateTimeOffset.UtcNow;
            saved.Options = NormalizeOptions(saved.Options ?? new MediaConversionOptions());
            var index = profiles.FindIndex(candidate => candidate.Id == saved.Id);
            if (index >= 0) profiles[index] = saved; else profiles.Add(saved);
            PersistProfiles(profiles);
            return CloneProfile(saved);
        }
    }

    public bool DeleteProfile(Guid id)
    {
        lock (_profilesSync)
        {
            var profiles = LoadUserProfiles();
            var removed = profiles.RemoveAll(profile => profile.Id == id) > 0;
            if (removed) PersistProfiles(profiles);
            return removed;
        }
    }

    public bool Cancel(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job)) return false;
        lock (job.Sync)
        {
            if (job.Status is MediaConversionJobStatus.Completed or MediaConversionJobStatus.Failed or MediaConversionJobStatus.Cancelled) return false;
            job.Message = "Cancelling…";
            job.Cancellation.Cancel();
            try { if (job.Process is { HasExited: false }) job.Process.Kill(entireProcessTree: true); } catch { }
            return true;
        }
    }

    public bool Remove(Guid id)
    {
        if (!_jobs.TryRemove(id, out var job)) return false;
        CancelState(job);
        try { Directory.Delete(Path.GetDirectoryName(job.SourcePath)!, true); } catch { }
        job.Cancellation.Dispose();
        return true;
    }

    private async Task ExecuteAsync(JobState job, string executable)
    {
        double durationSeconds = job.Options.DurationSeconds.GetValueOrDefault();
        try
        {
            lock (job.Sync)
            {
                job.Status = MediaConversionJobStatus.Running;
                job.Message = "Starting FFmpeg…";
            }
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in BuildArguments(job.Preset, job.Options, job.SourcePath, job.OutputPath)) startInfo.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            lock (job.Sync) job.Process = process;
            if (!process.Start()) throw new InvalidOperationException("FFmpeg could not be started.");

            var stdoutTask = Task.Run(async () =>
            {
                while (await process.StandardOutput.ReadLineAsync(job.Cancellation.Token) is { } line)
                {
                    if (line.StartsWith("out_time_us=", StringComparison.Ordinal)
                        && long.TryParse(line.AsSpan("out_time_us=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds)
                        && durationSeconds > 0)
                    {
                        lock (job.Sync)
                        {
                            job.Progress = Math.Clamp(microseconds / 1_000_000d / durationSeconds, 0, .99);
                            job.Message = $"Converting… {job.Progress:P0}";
                        }
                    }
                    else if (line.Equals("progress=end", StringComparison.Ordinal))
                    {
                        lock (job.Sync) job.Progress = 1;
                    }
                }
            }, job.Cancellation.Token);

            var stderr = new StringBuilder();
            var stderrTask = Task.Run(async () =>
            {
                while (await process.StandardError.ReadLineAsync(job.Cancellation.Token) is { } line)
                {
                    if (stderr.Length < 16_384) stderr.AppendLine(line);
                    if (durationSeconds <= 0)
                    {
                        var match = DurationPattern.Match(line);
                        if (match.Success)
                        {
                            durationSeconds = TimeSpan.FromHours(double.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture))
                                .Add(TimeSpan.FromMinutes(double.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture)))
                                .Add(TimeSpan.FromSeconds(double.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture)))
                                .TotalSeconds;
                        }
                    }
                }
            }, job.Cancellation.Token);

            await process.WaitForExitAsync(job.Cancellation.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
            if (process.ExitCode != 0)
            {
                var detail = stderr.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(4);
                throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}. {string.Join(" ", detail)}".Trim());
            }
            if (!File.Exists(job.OutputPath) || new FileInfo(job.OutputPath).Length == 0) throw new InvalidDataException("FFmpeg produced no output file.");
            lock (job.Sync)
            {
                job.Status = MediaConversionJobStatus.Completed;
                job.Progress = 1;
                job.OutputSize = new FileInfo(job.OutputPath).Length;
                job.Message = "Conversion complete";
                job.CompletedUtc = DateTimeOffset.UtcNow;
                job.Process = null;
            }
        }
        catch (OperationCanceledException)
        {
            lock (job.Sync)
            {
                job.Status = MediaConversionJobStatus.Cancelled;
                job.Message = "Conversion cancelled";
                job.CompletedUtc = DateTimeOffset.UtcNow;
                job.Process = null;
            }
            TryDelete(job.OutputPath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Media conversion {JobId} failed.", job.Id);
            lock (job.Sync)
            {
                job.Status = MediaConversionJobStatus.Failed;
                job.Message = exception.Message;
                job.CompletedUtc = DateTimeOffset.UtcNow;
                job.Process = null;
            }
            TryDelete(job.OutputPath);
        }
    }

    internal static IReadOnlyList<string> BuildArguments(MediaConversionPreset preset, MediaConversionOptions options, string inputPath, string outputPath)
    {
        options = NormalizeOptions(options);
        var arguments = new List<string> { "-hide_banner", "-y" };
        if (options.StartSeconds is > 0) arguments.AddRange(["-ss", Number(options.StartSeconds.Value)]);
        arguments.AddRange(["-i", inputPath]);
        if (options.DurationSeconds is > 0) arguments.AddRange(["-t", Number(options.DurationSeconds.Value)]);
        arguments.AddRange(["-progress", "pipe:1", "-nostats"]);

        arguments.AddRange(PresetArguments(preset.Id));
        ApplyStreamOverrides(arguments, options);
        ApplyFilters(arguments, options);
        ApplyMetadata(arguments, options);
        arguments.AddRange(ParseAdvancedArguments(options.AdvancedArguments));
        arguments.Add(outputPath);
        return arguments;
    }

    private static IReadOnlyList<string> PresetArguments(string presetId) => presetId switch
    {
        "webm-vp9" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libvpx-vp9", "-crf", "31", "-b:v", "0", "-row-mt", "1", "-pix_fmt", "yuv420p", "-c:a", "libopus", "-b:a", "128k"],
        "webm-vp8" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libvpx", "-crf", "10", "-b:v", "2M", "-pix_fmt", "yuv420p", "-c:a", "libopus", "-b:a", "128k"],
        "mp4-h264" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libx264", "-preset", "medium", "-crf", "21", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "160k"],
        "video-lossless-ffv1" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "ffv1", "-level", "3", "-c:a", "flac"],
        "video-prores" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "prores_ks", "-profile:v", "3", "-pix_fmt", "yuv422p10le", "-c:a", "pcm_s16le"],
        "audio-opus" => ["-vn", "-c:a", "libopus", "-b:a", "128k"],
        "audio-webm-opus" => ["-vn", "-c:a", "libopus", "-b:a", "128k"],
        "audio-wav" => ["-vn", "-c:a", "pcm_s16le"],
        "audio-flac" => ["-vn", "-c:a", "flac"],
        "image-png" => ["-frames:v", "1", "-c:v", "png"],
        "image-webp-lossless" => ["-frames:v", "1", "-c:v", "libwebp", "-lossless", "1", "-compression_level", "6"],
        "image-webp" => ["-frames:v", "1", "-c:v", "libwebp", "-q:v", "82", "-compression_level", "6"],
        "image-avif" => ["-frames:v", "1", "-c:v", "libaom-av1", "-still-picture", "1", "-crf", "28", "-cpu-used", "6"],
        "image-jpeg" => ["-frames:v", "1", "-c:v", "mjpeg", "-q:v", "2"],
        _ => throw new InvalidOperationException("The selected conversion preset is not implemented.")
    };

    private static void ApplyStreamOverrides(List<string> arguments, MediaConversionOptions options)
    {
        if (options.DisableVideo) arguments.Add("-vn");
        else
        {
            AddOverride(arguments, "-c:v", options.VideoCodec);
            AddOverride(arguments, "-preset", options.VideoEncoderPreset);
            if (options.Crf is not null) AddOverride(arguments, "-crf", options.Crf.Value.ToString(CultureInfo.InvariantCulture));
            if (options.VideoBitrateKbps is > 0) AddOverride(arguments, "-b:v", $"{options.VideoBitrateKbps.Value}k");
            if (options.MaximumVideoBitrateKbps is > 0) AddOverride(arguments, "-maxrate", $"{options.MaximumVideoBitrateKbps.Value}k");
            if (options.VideoBufferKbps is > 0) AddOverride(arguments, "-bufsize", $"{options.VideoBufferKbps.Value}k");
            AddOverride(arguments, "-pix_fmt", options.PixelFormat);
            if (options.FrameRate is > 0) AddOverride(arguments, "-r", Number(options.FrameRate.Value));
        }

        if (options.DisableAudio) arguments.Add("-an");
        else
        {
            AddOverride(arguments, "-c:a", options.AudioCodec);
            if (options.AudioBitrateKbps is > 0) AddOverride(arguments, "-b:a", $"{options.AudioBitrateKbps.Value}k");
            if (options.AudioSampleRate is > 0) AddOverride(arguments, "-ar", options.AudioSampleRate.Value.ToString(CultureInfo.InvariantCulture));
            if (options.AudioChannels is > 0) AddOverride(arguments, "-ac", options.AudioChannels.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (options.FastStart && options.OutputExtension.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            AddOverride(arguments, "-movflags", "+faststart");
    }

    private static void ApplyFilters(List<string> arguments, MediaConversionOptions options)
    {
        var videoFilters = new List<string>();
        if (options.Deinterlace) videoFilters.Add("bwdif");
        var scale = ScaleFilter(options);
        if (!string.IsNullOrWhiteSpace(scale)) videoFilters.Add(scale);
        if (!string.IsNullOrWhiteSpace(options.VideoFilter)) videoFilters.Add(options.VideoFilter.Trim());
        if (videoFilters.Count > 0) AddOverride(arguments, "-vf", string.Join(',', videoFilters));

        var audioFilters = new List<string>();
        if (options.NormalizeAudio) audioFilters.Add($"loudnorm=I={Number(options.LoudnessTargetLufs)}:TP=-1.5:LRA=11");
        if (!string.IsNullOrWhiteSpace(options.AudioFilter)) audioFilters.Add(options.AudioFilter.Trim());
        if (audioFilters.Count > 0) AddOverride(arguments, "-af", string.Join(',', audioFilters));
    }

    private static string ScaleFilter(MediaConversionOptions options)
    {
        if (options.Width is not > 0 && options.Height is not > 0) return string.Empty;
        var width = options.Width is > 0 ? options.Width.Value.ToString(CultureInfo.InvariantCulture) : "-2";
        var height = options.Height is > 0 ? options.Height.Value.ToString(CultureInfo.InvariantCulture) : "-2";
        if (!options.PreserveAspectRatio || options.ScaleMode == MediaConversionScaleMode.Stretch)
            return $"scale={width}:{height}";
        if (options.Width is not > 0 || options.Height is not > 0)
            return $"scale={width}:{height}";
        return options.ScaleMode switch
        {
            MediaConversionScaleMode.Fill => $"scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height}",
            MediaConversionScaleMode.Fit => $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=black",
            _ => $"scale={width}:{height}:force_original_aspect_ratio=decrease"
        };
    }

    private static void ApplyMetadata(List<string> arguments, MediaConversionOptions options)
    {
        if (!options.CopyMetadata) arguments.AddRange(["-map_metadata", "-1"]);
        foreach (var pair in options.Metadata ?? new Dictionary<string, string>())
        {
            var key = pair.Key.Trim();
            if (string.IsNullOrWhiteSpace(key) || key.Any(character => char.IsControl(character) || character == '=')) continue;
            arguments.AddRange(["-metadata", $"{key}={pair.Value ?? string.Empty}"]);
        }
    }

    private static void AddOverride(List<string> arguments, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        for (var index = arguments.Count - 2; index >= 0; index--)
        {
            if (!arguments[index].Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
            arguments.RemoveAt(index + 1);
            arguments.RemoveAt(index);
            break;
        }
        arguments.Add(key);
        arguments.Add(value.Trim());
    }

    internal static IReadOnlyList<string> ParseAdvancedArguments(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return [];
        var result = new List<string>();
        var token = new StringBuilder();
        var quote = '\0';
        var escape = false;
        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            if (escape)
            {
                token.Append(character);
                escape = false;
                continue;
            }
            if (character == '\\')
            {
                var next = index + 1 < source.Length ? source[index + 1] : '\0';
                // The text box is a tokenizer, not a command shell. Preserve FFmpeg's
                // own filter/path escapes (for example \: and C:\\Media) and only
                // consume the slash when it escapes a quote, whitespace or another slash.
                if (next == '\\' || next is '\'' or '"' || char.IsWhiteSpace(next))
                {
                    escape = true;
                    continue;
                }
                token.Append(character);
                continue;
            }
            if (quote != '\0')
            {
                if (character == quote) quote = '\0'; else token.Append(character);
                continue;
            }
            if (character is '\'' or '"')
            {
                quote = character;
                continue;
            }
            if (char.IsWhiteSpace(character))
            {
                if (token.Length > 0) { result.Add(token.ToString()); token.Clear(); }
                continue;
            }
            token.Append(character);
        }
        if (escape) token.Append('\\');
        if (quote != '\0') throw new ArgumentException("Advanced FFmpeg arguments contain an unterminated quote.");
        if (token.Length > 0) result.Add(token.ToString());
        for (var index = 0; index < result.Count; index++)
        {
            var value = result[index];
            var option = value.Contains('=') ? value[..value.IndexOf('=')] : value;
            if (ForbiddenAdvancedOptions.Contains(option))
                throw new ArgumentException($"Advanced argument '{option}' is managed by PublisherStudio and cannot be overridden.");
            if (!value.StartsWith("-", StringComparison.Ordinal) && index == 0)
                throw new ArgumentException("Advanced FFmpeg arguments must start with an option.");
        }
        return result;
    }

    private static MediaConversionOptions NormalizeOptions(MediaConversionOptions source)
    {
        var options = source.Clone();
        options.StartSeconds = FinitePositiveOrZero(options.StartSeconds);
        options.DurationSeconds = FinitePositive(options.DurationSeconds);
        options.Width = Positive(options.Width);
        options.Height = Positive(options.Height);
        options.FrameRate = FinitePositive(options.FrameRate);
        options.Crf = options.Crf is null ? null : Math.Clamp(options.Crf.Value, 0, 63);
        options.VideoBitrateKbps = Positive(options.VideoBitrateKbps);
        options.MaximumVideoBitrateKbps = Positive(options.MaximumVideoBitrateKbps);
        options.VideoBufferKbps = Positive(options.VideoBufferKbps);
        options.AudioBitrateKbps = Positive(options.AudioBitrateKbps);
        options.AudioSampleRate = Positive(options.AudioSampleRate);
        options.AudioChannels = options.AudioChannels is null ? null : Math.Clamp(options.AudioChannels.Value, 1, 32);
        options.LoudnessTargetLufs = double.IsFinite(options.LoudnessTargetLufs) ? Math.Clamp(options.LoudnessTargetLufs, -70, -5) : -16;
        options.VideoCodec = CleanOptionValue(options.VideoCodec);
        options.AudioCodec = CleanOptionValue(options.AudioCodec);
        options.VideoEncoderPreset = CleanOptionValue(options.VideoEncoderPreset);
        options.PixelFormat = CleanOptionValue(options.PixelFormat);
        options.OutputExtension = NormalizeExtension(options.OutputExtension, string.Empty);
        options.OutputMimeType = options.OutputMimeType?.Trim() ?? string.Empty;
        options.OutputFileName = SafeFileName(options.OutputFileName, string.Empty);
        options.VideoFilter = options.VideoFilter?.Trim() ?? string.Empty;
        options.AudioFilter = options.AudioFilter?.Trim() ?? string.Empty;
        options.AdvancedArguments = options.AdvancedArguments?.Trim() ?? string.Empty;
        _ = ParseAdvancedArguments(options.AdvancedArguments);
        return options;
    }

    private static void ValidateRequestedEncoders(MediaConversionCapabilities capabilities, MediaConversionOptions options)
    {
        var encoders = capabilities.Encoders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var requested in new[] { options.VideoCodec, options.AudioCodec }.Where(value => !string.IsNullOrWhiteSpace(value) && !value.Equals("copy", StringComparison.OrdinalIgnoreCase)))
            if (!encoders.Contains(requested)) throw new InvalidOperationException($"The installed FFmpeg build does not provide encoder '{requested}'.");
    }

    private static IReadOnlyList<MediaConversionProfile> BuiltInProfiles() =>
    [
        Profile("PublisherStudio HTML · balanced", "PublisherStudio-compatible WebM with 1080p fit, VP9/Opus and browser-safe pixel format.", "webm-vp9", new MediaConversionOptions { Target = MediaConversionTarget.PublisherStudioWeb, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 31, PixelFormat = "yuv420p", AudioBitrateKbps = 128 }),
        Profile("PublisherStudio HTML · compact", "Compact 720p WebM for structured websites and dashboards.", "webm-vp9", new MediaConversionOptions { Target = MediaConversionTarget.PublisherStudioWeb, Width = 1280, Height = 720, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 36, PixelFormat = "yuv420p", AudioBitrateKbps = 96 }),
        Profile("Browser compatibility · MP4", "H.264/AAC MP4 with fast-start for broad web playback.", "mp4-h264", new MediaConversionOptions { Target = MediaConversionTarget.GeneralWeb, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 21, PixelFormat = "yuv420p", FastStart = true, AudioBitrateKbps = 160 }),
        Profile("Editing intermediate · ProRes", "Intraframe MOV for VideoStudio editing workflows.", "video-prores", new MediaConversionOptions { Target = MediaConversionTarget.VideoEditing, PixelFormat = "yuv422p10le", AudioCodec = "pcm_s16le" }),
        Profile("Lossless archive · FFV1", "Lossless Matroska output for local preservation.", "video-lossless-ffv1", new MediaConversionOptions { Target = MediaConversionTarget.Archive }),
        Profile("Streaming · 1080p30", "CBR-like H.264/AAC streaming profile.", "mp4-h264", new MediaConversionOptions { Target = MediaConversionTarget.Streaming, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, VideoEncoderPreset = "veryfast", VideoBitrateKbps = 6000, MaximumVideoBitrateKbps = 6000, VideoBufferKbps = 12000, PixelFormat = "yuv420p", AudioBitrateKbps = 160 })
    ];

    private static MediaConversionProfile Profile(string name, string description, string presetId, MediaConversionOptions options) => new()
    {
        Id = StableProfileId(name),
        Name = name,
        Description = description,
        PresetId = presetId,
        BuiltIn = true,
        ModifiedUtc = DateTimeOffset.UnixEpoch,
        Options = options
    };

    private static Guid StableProfileId(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(TextEncoding.UTF8.GetBytes(value));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private List<MediaConversionProfile> LoadUserProfiles()
    {
        if (_userProfiles is not null) return _userProfiles;
        try
        {
            _userProfiles = File.Exists(_profilesPath)
                ? JsonSerializer.Deserialize<List<MediaConversionProfile>>(File.ReadAllText(_profilesPath), JsonOptions) ?? []
                : [];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Media conversion profiles could not be loaded.");
            _userProfiles = [];
        }
        foreach (var profile in _userProfiles)
        {
            profile.BuiltIn = false;
            profile.Options = NormalizeOptions(profile.Options ?? new MediaConversionOptions());
        }
        return _userProfiles;
    }

    private void PersistProfiles(List<MediaConversionProfile> profiles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_profilesPath)!);
        var temporary = _profilesPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(profiles, JsonOptions));
        File.Move(temporary, _profilesPath, true);
        _userProfiles = profiles;
    }

    private static MediaConversionProfile CloneProfile(MediaConversionProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Description = profile.Description,
        PresetId = profile.PresetId,
        BuiltIn = profile.BuiltIn,
        ModifiedUtc = profile.ModifiedUtc,
        Options = profile.Options?.Clone() ?? new MediaConversionOptions()
    };

    private static async Task<IReadOnlyList<string>> ReadEncodersAsync(string executable, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { "-hide_banner", "-encoders" }
        });
        if (process is null) return [];
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        _ = await errorTask;
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Match(line, @"^\s*[A-Z\.]{6}\s+(?<name>\S+)", RegexOptions.CultureInvariant))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static MediaConversionJobInfo Snapshot(JobState job)
    {
        lock (job.Sync)
        {
            return new MediaConversionJobInfo(
                job.Id,
                job.SourceFileName,
                job.Preset.Id,
                job.Status,
                job.Progress,
                job.Message,
                Path.GetFileName(job.OutputPath),
                job.OutputMimeType,
                job.OutputSize,
                job.CreatedUtc,
                job.CompletedUtc)
            {
                Options = job.Options.Clone()
            };
        }
    }

    private static string SafeFileName(string? fileName, string fallback)
    {
        var candidate = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? fallback : fileName.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars()) candidate = candidate.Replace(invalid, '_');
        if (string.IsNullOrWhiteSpace(candidate)) return fallback;
        return candidate[..Math.Min(candidate.Length, 180)];
    }

    private static string NormalizeExtension(string? requested, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(requested) ? fallback : requested.Trim();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.StartsWith('.') ? value : "." + value;
        return value.All(character => char.IsLetterOrDigit(character) || character == '.') && value.Length <= 16 ? value.ToLowerInvariant() : fallback;
    }

    private static string CleanOptionValue(string? value)
    {
        value = value?.Trim() ?? string.Empty;
        return value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.') ? value : string.Empty;
    }

    private static int? Positive(int? value) => value is > 0 ? value : null;
    private static double? FinitePositive(double? value) => value.HasValue && value.Value > 0 && double.IsFinite(value.Value) ? value.Value : null;
    private static double? FinitePositiveOrZero(double? value) => value.HasValue && value.Value >= 0 && double.IsFinite(value.Value) ? value.Value : null;
    private static string Number(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);

    private void CleanupOldDirectories()
    {
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(_root))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(directory) < DateTime.UtcNow.AddDays(-3)) Directory.Delete(directory, true);
                }
                catch { }
            }
        }
        catch { }
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

    private static void CancelState(JobState job)
    {
        try { job.Cancellation.Cancel(); } catch { }
        try { if (job.Process is { HasExited: false }) job.Process.Kill(entireProcessTree: true); } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var job in _jobs.Values) CancelState(job);
        foreach (var job in _jobs.Values) job.Cancellation.Dispose();
        _capabilityLock.Dispose();
    }
}
