using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using PublisherStudio.Services.Streaming.Encoding;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.MediaConversion;

public sealed class MediaConversionService : IMediaConversionService, IDisposable
{
    private sealed class JobState
    {
        public required Guid Id { get; init; }
        public required string SourceFileName { get; init; }
        public required string SourcePath { get; init; }
        public required string OutputPath { get; init; }
        public required MediaConversionPreset Preset { get; init; }
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
        new("webm-vp9", "Web video · WebM VP9/Opus", "Open, royalty-free web video for modern browsers. Keeps transparency when the source and encoder support it.", "video", ".webm", "video/webm", false, true, ["libvpx-vp9", "libopus"]),
        new("webm-vp8", "Web video · WebM VP8/Opus", "Compatibility-oriented open WebM conversion for devices without VP9 encoding.", "video", ".webm", "video/webm", false, true, ["libvpx", "libopus"]),
        new("mp4-h264", "Web video · MP4 H.264/AAC", "Broad browser and device compatibility. Availability and redistribution obligations depend on the installed FFmpeg build.", "video", ".mp4", "video/mp4", false, true, ["libx264", "aac"]),
        new("audio-opus", "Web audio · Ogg Opus", "Open, efficient audio for modern browsers and streaming workflows.", "audio", ".ogg", "audio/ogg", false, true, ["libopus"]),
        new("audio-wav", "Audio · WAV PCM", "Lossless uncompressed PCM audio for editing and interchange.", "audio", ".wav", "audio/wav", true, false, ["pcm_s16le"]),
        new("image-png", "Picture · PNG", "Lossless browser-compatible raster image. Animated or multi-page sources are flattened to the first frame.", "image", ".png", "image/png", true, true, ["png"]),
        new("image-webp-lossless", "Picture · lossless WebP", "Lossless WebP with alpha support and smaller files than PNG for many sources.", "image", ".webp", "image/webp", true, true, ["libwebp"]),
        new("image-webp", "Picture · compact WebP", "High-quality lossy WebP for websites and dashboards.", "image", ".webp", "image/webp", false, true, ["libwebp"]),
        new("image-avif", "Picture · AVIF", "High-efficiency AVIF still image when the installed FFmpeg build provides an AV1 encoder.", "image", ".avif", "image/avif", false, true, ["libaom-av1"])
    ];

    private static readonly Regex DurationPattern = new(@"Duration:\s*(?<h>\d+):(?<m>\d+):(?<s>\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();
    private readonly ILogger<MediaConversionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _root;
    private readonly SemaphoreSlim _capabilityLock = new(1, 1);
    private MediaConversionCapabilities? _capabilities;
    private bool _disposed;

    public MediaConversionService(ILogger<MediaConversionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "MediaConversions");
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

    public async Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MediaConversionService));
        ArgumentNullException.ThrowIfNull(source);
        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        if (!capabilities.Available) throw new InvalidOperationException("FFmpeg is not available.");
        var preset = capabilities.Presets.FirstOrDefault(candidate => string.Equals(candidate.Id, presetId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Unknown media conversion preset.", nameof(presetId));
        if (!preset.Available) throw new InvalidOperationException(preset.UnavailableReason);

        var id = Guid.NewGuid();
        var directory = Path.Combine(_root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeSourceName = SafeFileName(fileName, "source.bin");
        var sourcePath = Path.Combine(directory, safeSourceName);
        await using (var output = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, true))
            await source.CopyToAsync(output, cancellationToken);
        var outputName = Path.GetFileNameWithoutExtension(safeSourceName) + preset.OutputExtension;
        var outputPath = Path.Combine(directory, outputName);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var job = new JobState
        {
            Id = id,
            SourceFileName = safeSourceName,
            SourcePath = sourcePath,
            OutputPath = outputPath,
            Preset = preset,
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
        double durationSeconds = 0;
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
            foreach (var argument in Arguments(job.Preset, job.SourcePath, job.OutputPath)) startInfo.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            lock (job.Sync) job.Process = process;
            if (!process.Start()) throw new InvalidOperationException("FFmpeg could not be started.");

            var stdoutTask = Task.Run(async () =>
            {
                while (await process.StandardOutput.ReadLineAsync(job.Cancellation.Token) is { } line)
                {
                    if (line.StartsWith("out_time_us=", StringComparison.Ordinal) && long.TryParse(line.AsSpan("out_time_us=".Length), out var microseconds) && durationSeconds > 0)
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

            var stderrTask = Task.Run(async () =>
            {
                while (await process.StandardError.ReadLineAsync(job.Cancellation.Token) is { } line)
                {
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
                    if (line.Contains("Error", StringComparison.OrdinalIgnoreCase))
                        _logger.LogDebug("FFmpeg conversion {JobId}: {Message}", job.Id, line);
                }
            }, job.Cancellation.Token);

            await process.WaitForExitAsync(job.Cancellation.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
            if (process.ExitCode != 0) throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}.");
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

    private static IEnumerable<string> Arguments(MediaConversionPreset preset, string inputPath, string outputPath)
    {
        var arguments = new List<string> { "-hide_banner", "-y", "-i", inputPath, "-progress", "pipe:1", "-nostats" };
        arguments.AddRange(preset.Id switch
        {
            "webm-vp9" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libvpx-vp9", "-crf", "31", "-b:v", "0", "-row-mt", "1", "-pix_fmt", "yuv420p", "-c:a", "libopus", "-b:a", "128k"],
            "webm-vp8" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libvpx", "-crf", "10", "-b:v", "2M", "-pix_fmt", "yuv420p", "-c:a", "libopus", "-b:a", "128k"],
            "mp4-h264" => ["-map", "0:v:0?", "-map", "0:a:0?", "-c:v", "libx264", "-preset", "medium", "-crf", "21", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart"],
            "audio-opus" => ["-vn", "-c:a", "libopus", "-b:a", "128k"],
            "audio-wav" => ["-vn", "-c:a", "pcm_s16le"],
            "image-png" => ["-frames:v", "1", "-c:v", "png"],
            "image-webp-lossless" => ["-frames:v", "1", "-c:v", "libwebp", "-lossless", "1", "-compression_level", "6"],
            "image-webp" => ["-frames:v", "1", "-c:v", "libwebp", "-q:v", "82", "-compression_level", "6"],
            "image-avif" => ["-frames:v", "1", "-c:v", "libaom-av1", "-still-picture", "1", "-crf", "28", "-cpu-used", "6"],
            _ => throw new InvalidOperationException("The selected conversion preset is not implemented.")
        });
        arguments.Add(outputPath);
        return arguments;
    }

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
                job.Preset.OutputMimeType,
                job.OutputSize,
                job.CreatedUtc,
                job.CompletedUtc);
        }
    }

    private static string SafeFileName(string? fileName, string fallback)
    {
        var candidate = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? fallback : fileName.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars()) candidate = candidate.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate[..Math.Min(candidate.Length, 180)];
    }

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
