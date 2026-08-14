using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Streaming.Encoding;
using PublisherStudio.Services.Configuration;
using TextEncoding = global::System.Text.Encoding;

namespace PublisherStudio.Services.MediaConversion;

/// <summary>
/// Coordinates media conversion behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class MediaConversionService : IMediaConversionService, IDisposable
{
    /// <summary>
    /// Represents job state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    private sealed class JobState
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this job instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="JobState"/>.</value>
        public required Guid Id { get; init; }
        /// <summary>
        /// Gets or sets the source file name used by this job instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The source file name value exposed by <see cref="JobState"/>.</value>
        public required string SourceFileName { get; init; }
        /// <summary>
        /// Gets or sets the source path used by this job instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The source path value exposed by <see cref="JobState"/>.</value>
        public required string SourcePath { get; init; }
        /// <summary>
        /// Gets or sets the output path used by this job instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The output path value exposed by <see cref="JobState"/>.</value>
        public required string OutputPath { get; init; }
        /// <summary>
        /// Gets or sets the output MIME type value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The output MIME type value exposed by <see cref="JobState"/>.</value>
        public required string OutputMimeType { get; init; }
        /// <summary>
        /// Gets or sets the preset value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The preset value exposed by <see cref="JobState"/>.</value>
        public required MediaConversionPreset Preset { get; init; }
        /// <summary>
        /// Gets or sets the options value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The options value exposed by <see cref="JobState"/>.</value>
        public required MediaConversionOptions Options { get; init; }
        /// <summary>
        /// Gets or sets the cancellation signal used to stop or abandon work associated with this job operation.
        /// </summary>
        /// <value>The cancellation value exposed by <see cref="JobState"/>.</value>
        public required CancellationTokenSource Cancellation { get; init; }
        /// <summary>
        /// Gets or sets the status value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The status value exposed by <see cref="JobState"/>.</value>
        public MediaConversionJobStatus Status { get; set; } = MediaConversionJobStatus.Queued;
        /// <summary>
        /// Gets or sets the progress value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The progress value exposed by <see cref="JobState"/>.</value>
        public double Progress { get; set; }
        /// <summary>
        /// Gets or sets the message value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The message value exposed by <see cref="JobState"/>.</value>
        public string Message { get; set; } = "Queued";
        /// <summary>
        /// Gets or sets the output size that quantifies the associated job data.
        /// </summary>
        /// <value>The output size value exposed by <see cref="JobState"/>.</value>
        public long OutputSize { get; set; }
        /// <summary>
        /// Gets or sets the created UTC associated with this job state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The created UTC value exposed by <see cref="JobState"/>.</value>
        public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// Gets or sets the completed UTC associated with this job state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed UTC value exposed by <see cref="JobState"/>.</value>
        public DateTimeOffset? CompletedUtc { get; set; }
        /// <summary>
        /// Gets or sets the process value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The process value exposed by <see cref="JobState"/>.</value>
        public Process? Process { get; set; }
        /// <summary>
        /// Gets the sync value that forms part of the job state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sync value exposed by <see cref="JobState"/>.</value>
        public object Sync { get; } = new();
    }

    /// <summary>
    /// Stores the internal JSON options state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    /// <summary>
    /// Stores the in-memory jobs collection maintained internally by <see cref="MediaConversionService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();
    /// <summary>
    /// Stores the logger used by <see cref="MediaConversionService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<MediaConversionService> logger;
    /// <summary>
    /// Stores the internal FFmpeg locator state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly FfmpegLocator _ffmpegLocator;
    /// <summary>
    /// Stores the publisher runtime policy data service dependency used by <see cref="MediaConversionService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherRuntimePolicyDataService _runtimePolicy;
    /// <summary>
    /// Stores the publisher runtime pattern service dependency used by <see cref="MediaConversionService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherRuntimePatternService _runtimePatterns;
    /// <summary>
    /// Stores the internal publisher configuration state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PublisherStudioConfigurationNode _publisherConfiguration;
    /// <summary>
    /// Stores the internal root state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _root;
    /// <summary>
    /// Stores the internal profiles path state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _profilesPath;
    /// <summary>
    /// Stores the internal profiles sync state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _profilesSync = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to capability lock state owned by <see cref="MediaConversionService"/>.
    /// </summary>
    private readonly SemaphoreSlim _capabilityLock = new(1, 1);
    /// <summary>
    /// Stores the internal capabilities state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private MediaConversionCapabilities? _capabilities;
    /// <summary>
    /// Stores the in-memory user profiles collection maintained internally by <see cref="MediaConversionService"/> for its current workflow state.
    /// </summary>
    private List<MediaConversionProfile>? _userProfiles;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="MediaConversionService"/> while executing its surrounding workflow.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="MediaConversionService"/> instance and captures the dependencies or initial state required by its media conversion workflow.
    /// </summary>
    /// <param name="ffmpegLocator">Ffmpeg locator value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the media conversion workflow to provide the corresponding application capability.</param>
    /// <param name="runtimePatterns">Publisher runtime pattern service dependency used by the media conversion workflow to provide the corresponding application capability.</param>
    /// <param name="publisherConfiguration">Publisher configuration value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public MediaConversionService(
        FfmpegLocator ffmpegLocator,
        IPublisherRuntimePolicyDataService runtimePolicy,
        IPublisherRuntimePatternService runtimePatterns,
        PublisherStudioConfigurationNode publisherConfiguration,
        ILogger<MediaConversionService> logger)
    {
        this.logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _runtimePolicy = runtimePolicy;
        _runtimePatterns = runtimePatterns;
        _publisherConfiguration = publisherConfiguration;
        var publisherRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio");
        _root = Path.Combine(publisherRoot, "MediaConversions");
        _profilesPath = Path.Combine(publisherRoot, "MediaConversionProfiles.json");
        Directory.CreateDirectory(_root);
        CleanupOldDirectories();
    }

    /// <summary>
    /// Retrieves capabilities as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion capabilities produced by the operation.</returns>
    public async Task<MediaConversionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.GetCapabilitiesAsync.");
                    if (_capabilities is not null) return _capabilities;
                    await _capabilityLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (_capabilities is not null) return _capabilities;
                        var configuredPath = string.IsNullOrWhiteSpace(_publisherConfiguration.FFmpegPath)
                            ? Environment.GetEnvironmentVariable(_runtimePolicy.FfmpegEnvironmentVariable)
                            : _publisherConfiguration.FFmpegPath;
                        var executable = _ffmpegLocator.Resolve(configuredPath);
                        if (executable is null)
                        {
                            _capabilities = new MediaConversionCapabilities(
                                false,
                                string.Empty,
                                string.Empty,
                                [],
                                _runtimePolicy.MediaConversionPresets.Select(preset => preset with { Available = false, UnavailableReason = "FFmpeg was not found." }).ToArray(),
                                "PublisherStudio does not bundle FFmpeg. The executable you install remains a separate program under its own LGPL/GPL build terms.",
                                "Install FFmpeg and place it on PATH, in PublisherStudio/tools/ffmpeg, or configure PublisherStudio:FFmpegPath / PUBLISHERSTUDIO_FFMPEG.");
                            return _capabilities;
                        }

                        var version = await _ffmpegLocator.ReadVersionAsync(executable, cancellationToken).ConfigureAwait(false) ?? "FFmpeg detected";
                        var encoders = await ReadEncodersAsync(executable, cancellationToken).ConfigureAwait(false);
                        var encoderSet = encoders.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        var presets = _runtimePolicy.MediaConversionPresets.Select(definition =>
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.GetCapabilitiesAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs queue as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    public Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, CancellationToken cancellationToken = default) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.QueueAsync.");
            return QueueAsync(source, fileName, mimeType, presetId, new MediaConversionOptions(), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.QueueAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs queue as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    public async Task<MediaConversionJobInfo> QueueAsync(Stream source, string fileName, string mimeType, string presetId, MediaConversionOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.QueueAsync.");
                    if (_disposed) throw new ObjectDisposedException(nameof(MediaConversionService));
                    ArgumentNullException.ThrowIfNull(source);
                    options ??= new MediaConversionOptions();
                    var normalizedOptions = NormalizeOptions(options);
                    var capabilities = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
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
                    var output = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, true);
                    await using (output.ConfigureAwait(false))
                        await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.QueueAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves job as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    public MediaConversionJobInfo? GetJob(Guid id) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.GetJob.");
            return _jobs.TryGetValue(id, out var state) ? Snapshot(state) : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.GetJob failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves jobs as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<MediaConversionJobInfo> GetJobs() {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.GetJobs.");
            return _jobs.Values
        .OrderByDescending(job => job.CreatedUtc)
        .Select(Snapshot)
        .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.GetJobs failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Opens output as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The stream produced by the operation.</returns>
    public Task<Stream?> OpenOutputAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.OpenOutputAsync.");
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!_jobs.TryGetValue(id, out var job) || job.Status != MediaConversionJobStatus.Completed || !File.Exists(job.OutputPath))
                        return Task.FromResult<Stream?>(null);
                    Stream stream = new FileStream(job.OutputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, true);
                    return Task.FromResult<Stream?>(stream);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.OpenOutputAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves profiles as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<MediaConversionProfile> GetProfiles()
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.GetProfiles.");
                    lock (_profilesSync)
                    {
                        var profiles = BuiltInProfiles().Concat(LoadUserProfiles()).Select(CloneProfile).ToArray();
                        return profiles;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.GetProfiles failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Persists profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The media conversion profile produced by the operation.</returns>
    public MediaConversionProfile SaveProfile(MediaConversionProfile profile)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.SaveProfile.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.SaveProfile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool DeleteProfile(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.DeleteProfile.");
                    lock (_profilesSync)
                    {
                        var profiles = LoadUserProfiles();
                        var removed = profiles.RemoveAll(profile => profile.Id == id) > 0;
                        if (removed) PersistProfiles(profiles);
                        return removed;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.DeleteProfile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Cancel(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Cancel.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Cancel failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs remove as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Remove(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Remove.");
                    if (!_jobs.TryRemove(id, out var job)) return false;
                    CancelState(job);
                    try { Directory.Delete(Path.GetDirectoryName(job.SourcePath)!, true); } catch { }
                    job.Cancellation.Dispose();
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Remove failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs execute as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="job">Job value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
                while (await process.StandardOutput.ReadLineAsync(job.Cancellation.Token).ConfigureAwait(false) is { } line)
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
                while (await process.StandardError.ReadLineAsync(job.Cancellation.Token).ConfigureAwait(false) is { } line)
                {
                    if (stderr.Length < 16_384) stderr.AppendLine(line);
                    if (durationSeconds <= 0)
                    {
                        var match = _runtimePatterns.GetRegex(PublisherRuntimePattern.MediaDuration).Match(line);
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

            await process.WaitForExitAsync(job.Cancellation.Token).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
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
            logger.LogWarning(exception, "Media conversion {JobId} failed.", job.Id);
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

    /// <summary>
    /// Builds arguments as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preset">Preset value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="inputPath">Input path value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="outputPath">Output path value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    internal IReadOnlyList<string> BuildArguments(MediaConversionPreset preset, MediaConversionOptions options, string inputPath, string outputPath)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.BuildArguments.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.BuildArguments failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs preset arguments as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> PresetArguments(string presetId) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.PresetArguments.");
            return presetId switch
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
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.PresetArguments failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies stream overrides as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    private void ApplyStreamOverrides(List<string> arguments, MediaConversionOptions options)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ApplyStreamOverrides.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ApplyStreamOverrides failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies filters as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    private void ApplyFilters(List<string> arguments, MediaConversionOptions options)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ApplyFilters.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ApplyFilters failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs scale filter as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ScaleFilter(MediaConversionOptions options)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ScaleFilter.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ScaleFilter failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies metadata as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    private void ApplyMetadata(List<string> arguments, MediaConversionOptions options)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ApplyMetadata.");
                    if (!options.CopyMetadata) arguments.AddRange(["-map_metadata", "-1"]);
                    foreach (var pair in options.Metadata ?? new Dictionary<string, string>())
                    {
                        var key = pair.Key.Trim();
                        if (string.IsNullOrWhiteSpace(key) || key.Any(character => char.IsControl(character) || character == '=')) continue;
                        arguments.AddRange(["-metadata", $"{key}={pair.Value ?? string.Empty}"]);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ApplyMetadata failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds override as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    private void AddOverride(List<string> arguments, string key, string? value)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.AddOverride.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.AddOverride failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses advanced arguments as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    internal IReadOnlyList<string> ParseAdvancedArguments(string? source)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ParseAdvancedArguments.");
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
                        if (_runtimePolicy.GetCollection(PublisherRuntimeCollection.ForbiddenFfmpegAdvancedOptions).Contains(option, StringComparer.OrdinalIgnoreCase))
                            throw new ArgumentException($"Advanced argument '{option}' is managed by PublisherStudio and cannot be overridden.");
                        if (!value.StartsWith("-", StringComparison.Ordinal) && index == 0)
                            throw new ArgumentException("Advanced FFmpeg arguments must start with an option.");
                    }
                    return result;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ParseAdvancedArguments failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes options as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The media conversion options produced by the operation.</returns>
    private MediaConversionOptions NormalizeOptions(MediaConversionOptions source)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.NormalizeOptions.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.NormalizeOptions failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates requested encoders as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="capabilities">Capabilities value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    private void ValidateRequestedEncoders(MediaConversionCapabilities capabilities, MediaConversionOptions options)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ValidateRequestedEncoders.");
                    var encoders = capabilities.Encoders.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var requested in new[] { options.VideoCodec, options.AudioCodec }.Where(value => !string.IsNullOrWhiteSpace(value) && !value.Equals("copy", StringComparison.OrdinalIgnoreCase)))
                        if (!encoders.Contains(requested)) throw new InvalidOperationException($"The installed FFmpeg build does not provide encoder '{requested}'.");
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ValidateRequestedEncoders failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs built in profiles as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<MediaConversionProfile> BuiltInProfiles() {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.BuiltInProfiles.");
            return [
        Profile("PublisherStudio HTML · balanced", "PublisherStudio-compatible WebM with 1080p fit, VP9/Opus and browser-safe pixel format.", "webm-vp9", new MediaConversionOptions { Target = MediaConversionTarget.PublisherStudioWeb, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 31, PixelFormat = "yuv420p", AudioBitrateKbps = 128 }),
        Profile("PublisherStudio HTML · compact", "Compact 720p WebM for structured websites and dashboards.", "webm-vp9", new MediaConversionOptions { Target = MediaConversionTarget.PublisherStudioWeb, Width = 1280, Height = 720, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 36, PixelFormat = "yuv420p", AudioBitrateKbps = 96 }),
        Profile("Browser compatibility · MP4", "H.264/AAC MP4 with fast-start for broad web playback.", "mp4-h264", new MediaConversionOptions { Target = MediaConversionTarget.GeneralWeb, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, Crf = 21, PixelFormat = "yuv420p", FastStart = true, AudioBitrateKbps = 160 }),
        Profile("Editing intermediate · ProRes", "Intraframe MOV for VideoStudio editing workflows.", "video-prores", new MediaConversionOptions { Target = MediaConversionTarget.VideoEditing, PixelFormat = "yuv422p10le", AudioCodec = "pcm_s16le" }),
        Profile("Lossless archive · FFV1", "Lossless Matroska output for local preservation.", "video-lossless-ffv1", new MediaConversionOptions { Target = MediaConversionTarget.Archive }),
        Profile("Streaming · 1080p30", "CBR-like H.264/AAC streaming profile.", "mp4-h264", new MediaConversionOptions { Target = MediaConversionTarget.Streaming, Width = 1920, Height = 1080, ScaleMode = MediaConversionScaleMode.Fit, FrameRate = 30, VideoEncoderPreset = "veryfast", VideoBitrateKbps = 6000, MaximumVideoBitrateKbps = 6000, VideoBufferKbps = 12000, PixelFormat = "yuv420p", AudioBitrateKbps = 160 })
    ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.BuiltInProfiles failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The media conversion profile produced by the operation.</returns>
    private MediaConversionProfile Profile(string name, string description, string presetId, MediaConversionOptions options) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Profile.");
            return new()
    {
        Id = StableProfileId(name),
        Name = name,
        Description = description,
        PresetId = presetId,
        BuiltIn = true,
        ModifiedUtc = DateTimeOffset.UnixEpoch,
        Options = options
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Profile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs stable profile identifier as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
    private Guid StableProfileId(string value)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.StableProfileId.");
                    var bytes = System.Security.Cryptography.SHA256.HashData(TextEncoding.UTF8.GetBytes(value));
                    return new Guid(bytes.AsSpan(0, 16));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.StableProfileId failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads user profiles as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
            logger.LogWarning(exception, "Media conversion profiles could not be loaded.");
            _userProfiles = [];
        }
        foreach (var profile in _userProfiles)
        {
            profile.BuiltIn = false;
            profile.Options = NormalizeOptions(profile.Options ?? new MediaConversionOptions());
        }
        return _userProfiles;
    }

    /// <summary>
    /// Persists profiles as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profiles">Profiles value supplied to the media conversion operation and used when producing its result.</param>
    private void PersistProfiles(List<MediaConversionProfile> profiles)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.PersistProfiles.");
                    Directory.CreateDirectory(Path.GetDirectoryName(_profilesPath)!);
                    var temporary = _profilesPath + ".tmp";
                    File.WriteAllText(temporary, JsonSerializer.Serialize(profiles, JsonOptions));
                    File.Move(temporary, _profilesPath, true);
                    _userProfiles = profiles;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.PersistProfiles failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone profile as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The media conversion profile produced by the operation.</returns>
    private MediaConversionProfile CloneProfile(MediaConversionProfile profile) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.CloneProfile.");
            return new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Description = profile.Description,
        PresetId = profile.PresetId,
        BuiltIn = profile.BuiltIn,
        ModifiedUtc = profile.ModifiedUtc,
        Options = profile.Options?.Clone() ?? new MediaConversionOptions()
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.CloneProfile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads encoders as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executable">Executable value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<string>> ReadEncodersAsync(string executable, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.ReadEncodersAsync.");
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
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    var output = await outputTask.ConfigureAwait(false);
                    _ = await errorTask.ConfigureAwait(false);
                    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => Regex.Match(line, @"^\s*[A-Z\.]{6}\s+(?<name>\S+)", RegexOptions.CultureInvariant))
                        .Where(match => match.Success)
                        .Select(match => match.Groups["name"].Value)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.ReadEncodersAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs snapshot as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="job">Job value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The media conversion job info produced by the operation.</returns>
    private MediaConversionJobInfo Snapshot(JobState job)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Snapshot.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Snapshot failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs safe file name as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeFileName(string? fileName, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.SafeFileName.");
                    var candidate = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? fallback : fileName.Trim());
                    foreach (var invalid in Path.GetInvalidFileNameChars()) candidate = candidate.Replace(invalid, '_');
                    if (string.IsNullOrWhiteSpace(candidate)) return fallback;
                    return candidate[..Math.Min(candidate.Length, 180)];
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.SafeFileName failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes extension as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requested">Requested value supplied to the media conversion operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeExtension(string? requested, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.NormalizeExtension.");
                    var value = string.IsNullOrWhiteSpace(requested) ? fallback : requested.Trim();
                    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
                    value = value.StartsWith('.') ? value : "." + value;
                    return value.All(character => char.IsLetterOrDigit(character) || character == '.') && value.Length <= 16 ? value.ToLowerInvariant() : fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.NormalizeExtension failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clean option value as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CleanOptionValue(string? value)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.CleanOptionValue.");
                    value = value?.Trim() ?? string.Empty;
                    return value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.') ? value : string.Empty;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.CleanOptionValue failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs positive as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int? Positive(int? value) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Positive.");
            return value is > 0 ? value : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Positive failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs finite positive as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double? FinitePositive(double? value) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.FinitePositive.");
            return value.HasValue && value.Value > 0 && double.IsFinite(value.Value) ? value.Value : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.FinitePositive failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs finite positive or zero as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double? FinitePositiveOrZero(double? value) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.FinitePositiveOrZero.");
            return value.HasValue && value.Value >= 0 && double.IsFinite(value.Value) ? value.Value : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.FinitePositiveOrZero failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs number as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media conversion operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Number(double value) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Number.");
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Number failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs cleanup old directories as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void CleanupOldDirectories()
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.CleanupOldDirectories.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.CleanupOldDirectories failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to delete as part of the media conversion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the media conversion operation and used when producing its result.</param>
    private void TryDelete(string path) {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.TryDelete.");
             try { if (File.Exists(path)) File.Delete(path); } catch { } 
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.TryDelete failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel state.
    /// </summary>
    /// <param name="job">Job value supplied to the media conversion operation and used when producing its result.</param>
    private void CancelState(JobState job)
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.CancelState.");
                    try { job.Cancellation.Cancel(); } catch { }
                    try { if (job.Process is { HasExited: false }) job.Process.Kill(entireProcessTree: true); } catch { }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.CancelState failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="MediaConversionService"/> and leaves the media conversion workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            logger.LogTrace($"Entering MediaConversionService.Dispose.");
                    if (_disposed) return;
                    _disposed = true;
                    foreach (var job in _jobs.Values) CancelState(job);
                    foreach (var job in _jobs.Values) job.Cancellation.Dispose();
                    _capabilityLock.Dispose();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaConversionService.Dispose failed: {exception.Message}");
            throw;
        }
    }
}
