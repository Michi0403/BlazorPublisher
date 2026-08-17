using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace PublisherStudio.Services.Streaming.Sessions;

/// <summary>
/// Defines the contract for media session behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IMediaSessionFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="IMediaSessionFactory"/>.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The media session produced by the operation.</returns>
    MediaSession Create(JsonElement request);
}

/// <summary>
/// Creates configured media session instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="loggerFactory">Logger factory dependency used by the media session workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class MediaSessionFactory(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILoggerFactory loggerFactory,
    ILogger<MediaSessionFactory> logger) : IMediaSessionFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="MediaSessionFactory"/>.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The media session produced by the operation.</returns>
    public MediaSession Create(JsonElement request)
    {
        try
        {
            logger.LogTrace($"Creating a media session from the supplied request.");
            var session = new MediaSession(
                runtimePolicy.MediaSessionDefaults,
                loggerFactory.CreateLogger<MediaSession>(),
                loggerFactory.CreateLogger<WebRtcSignalingService>());
            session.Apply(request);
            logger.LogInformation($"Created media session {session.Id} for {session.Name}.");
            return session;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a media session from the supplied request.");
            throw;
        }
    }
}

/// <summary>
/// Represents a media session application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class MediaSession
{
    /// <summary>
    /// Stores the internal defaults state used by <see cref="MediaSession"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PublisherMediaSessionDefaultsPolicy defaults;
    /// <summary>
    /// Stores the logger used by <see cref="MediaSession"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<MediaSession> logger;
    /// <summary>
    /// Stores the internal ingest subscriber sync state used by <see cref="MediaSession"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object ingestSubscriberSync = new();
    /// <summary>
    /// Stores the in-memory ingest subscribers collection maintained internally by <see cref="MediaSession"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<Guid, Channel<byte[]>> ingestSubscribers = [];
    /// <summary>
    /// Stores the internal webm initialization chunk state used by <see cref="MediaSession"/> while executing its surrounding workflow.
    /// </summary>
    private byte[]? webmInitializationChunk;

    /// <summary>
    /// Initializes a new <see cref="MediaSession"/> instance and captures the dependencies or initial state required by its media session workflow.
    /// </summary>
    /// <param name="defaults">Defaults value supplied to the media session operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="webRtcLogger">Web rtc signaling service dependency used by the media session workflow to provide the corresponding application capability.</param>
    public MediaSession(
        PublisherMediaSessionDefaultsPolicy defaults,
        ILogger<MediaSession> logger,
        ILogger<WebRtcSignalingService> webRtcLogger)
    {
        try
        {
            this.defaults = defaults;
            this.logger = logger;
            Id = Guid.NewGuid();
            Name = defaults.PublicationName;
            StartedUtc = DateTimeOffset.UtcNow;
            MasterWidth = defaults.MasterWidth;
            MasterHeight = defaults.MasterHeight;
            MasterFrameRate = defaults.MasterFrameRate;
            AdaptiveMedia = new PublicationAdaptiveMediaSettings
            {
                Enabled = defaults.AdaptiveQuality.Enabled,
                AdaptVideo = true,
                AdaptAudio = true,
                UseProviderKnowledge = true,
                UseBrowserCapabilityProbe = defaults.AdaptiveQuality.BrowserCapabilityProbeEnabled,
                Profile = Enum.TryParse<PublicationAdaptiveQualityProfile>(defaults.AdaptiveQuality.DefaultProfile, true, out var defaultProfile)
                    ? defaultProfile
                    : PublicationAdaptiveQualityProfile.Quality,
                PreserveNativeResolution = defaults.AdaptiveQuality.PreserveNativeResolution,
                AllowFrameRateReduction = defaults.AdaptiveQuality.AllowFrameRateReduction,
                AllowResolutionReduction = defaults.AdaptiveQuality.AllowResolutionReduction
            };
            PreferDeviceTimestamps = defaults.PreferDeviceTimestamps;
            HardwareEncoder = defaults.HardwareEncoder;
            RecordingDefinition.Container = defaults.RecordingContainer;
            RecordingDefinition.SegmentSeconds = defaults.RecordingSegmentSeconds;
            LanDefinition.BindAddress = defaults.LanBindAddress;
            LanDefinition.Port = defaults.LanPort;
            LanDefinition.Width = defaults.OutputWidth;
            LanDefinition.Height = defaults.OutputHeight;
            LanDefinition.FrameRate = defaults.OutputFrameRate;
            LanDefinition.VideoBitrateKbps = defaults.LanVideoBitrateKbps;
            LanDefinition.AudioBitrateKbps = defaults.AudioBitrateKbps;
            LanDefinition.EnableBrowserWebRtc = defaults.EnableBrowserWebRtc;
            LanDefinition.EnableHls = defaults.EnableHls;
            LanDefinition.EnableRtsp = defaults.EnableRtsp;
            LanDefinition.RtspPort = defaults.RtspPort;
            LanDefinition.RequireAccessToken = defaults.RequireAccessToken;
            LanDefinition.ViewerLimit = defaults.ViewerLimit;
            WebRtc = new WebRtcSignalingService();
            logger.LogTrace($"Initialized media session {Id} with policy-owned defaults.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not initialize a media session.");
            throw;
        }
    }

    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this media session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="MediaSession"/>.</value>
    public Guid Id { get; private set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaSession"/>.</value>
    public string Name { get; private set; }
    /// <summary>
    /// Gets or sets a value indicating whether dry run applies to the media session state.
    /// </summary>
    /// <value>The dry run value exposed by <see cref="MediaSession"/>.</value>
    public bool DryRun { get; private set; }
    /// <summary>
    /// Gets or sets the started UTC associated with this media session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started UTC value exposed by <see cref="MediaSession"/>.</value>
    public DateTimeOffset StartedUtc { get; private set; }
    /// <summary>
    /// Gets or sets the stopped UTC associated with this media session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The stopped UTC value exposed by <see cref="MediaSession"/>.</value>
    public DateTimeOffset? StoppedUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether recording applies to the media session state.
    /// </summary>
    /// <value>The recording value exposed by <see cref="MediaSession"/>.</value>
    public bool Recording { get; set; }
    /// <summary>
    /// Gets or sets the stable program page identifier used to identify or correlate this media session instance with related application state.
    /// </summary>
    /// <value>The program page identifier value exposed by <see cref="MediaSession"/>.</value>
    public Guid? ProgramPageId { get; set; }
    /// <summary>
    /// Gets a value indicating whether LAN enabled applies to the media session state.
    /// </summary>
    /// <value>The LAN enabled value exposed by <see cref="MediaSession"/>.</value>
    public bool LanEnabled => LanDefinition.Enabled;
    /// <summary>
    /// Gets the LAN definition value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN definition value exposed by <see cref="MediaSession"/>.</value>
    public MediaLanDefinition LanDefinition { get; } = new();
    /// <summary>
    /// Gets or sets the LAN server value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN server value exposed by <see cref="MediaSession"/>.</value>
    public LanStreamingServer? LanServer { get; set; }
    /// <summary>
    /// Gets or sets the master width value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master width value exposed by <see cref="MediaSession"/>.</value>
    public int MasterWidth { get; private set; }
    /// <summary>
    /// Gets or sets the master height value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master height value exposed by <see cref="MediaSession"/>.</value>
    public int MasterHeight { get; private set; }
    /// <summary>
    /// Gets or sets the master frame rate value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master frame rate value exposed by <see cref="MediaSession"/>.</value>
    public int MasterFrameRate { get; private set; }
    /// <summary>Gets the per-publication adaptive media choices used by encoder and transport paths in this session.</summary>
    /// <value>The adaptive media settings applied to this media session.</value>
    public PublicationAdaptiveMediaSettings AdaptiveMedia { get; private set; }
    /// <summary>
    /// Gets or sets a value indicating whether prefer device timestamps applies to the media session state.
    /// </summary>
    /// <value>The prefer device timestamps value exposed by <see cref="MediaSession"/>.</value>
    public bool PreferDeviceTimestamps { get; private set; }
    /// <summary>
    /// Gets or sets the FFmpeg path used by this media session instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg path value exposed by <see cref="MediaSession"/>.</value>
    public string FfmpegPath { get; private set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hardware encoder value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware encoder value exposed by <see cref="MediaSession"/>.</value>
    public int HardwareEncoder { get; private set; }
    /// <summary>
    /// Gets the outputs collection maintained or exposed by this media session instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="MediaSession"/>.</value>
    public ConcurrentDictionary<Guid, bool> Outputs { get; } = new();
    /// <summary>
    /// Gets the output definitions collection maintained or exposed by this media session instance for downstream processing.
    /// </summary>
    /// <value>The output definitions value exposed by <see cref="MediaSession"/>.</value>
    public List<MediaOutputDefinition> OutputDefinitions { get; } = [];
    /// <summary>
    /// Gets the recording definition value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording definition value exposed by <see cref="MediaSession"/>.</value>
    public MediaRecordingDefinition RecordingDefinition { get; } = new();
    /// <summary>
    /// Gets the hotkeys collection maintained or exposed by this media session instance for downstream processing.
    /// </summary>
    /// <value>The hotkeys value exposed by <see cref="MediaSession"/>.</value>
    public List<MediaHotkey> Hotkeys { get; } = [];
    /// <summary>
    /// Gets or sets the ingest value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest value exposed by <see cref="MediaSession"/>.</value>
    public IngestAnnouncement? Ingest { get; private set; }
    /// <summary>
    /// Gets the output ingests collection maintained or exposed by this media session instance for downstream processing.
    /// </summary>
    /// <value>The output ingests value exposed by <see cref="MediaSession"/>.</value>
    public ConcurrentDictionary<Guid, IngestAnnouncement> OutputIngests { get; } = new();
    /// <summary>
    /// Gets or sets the encoder value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encoder value exposed by <see cref="MediaSession"/>.</value>
    public EncoderSessionService? Encoder { get; set; }
    /// <summary>
    /// Gets or sets the hls directory used by this media session instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The hls directory value exposed by <see cref="MediaSession"/>.</value>
    public string HlsDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the rtsp URL that identifies the network or application endpoint associated with this media session state.
    /// </summary>
    /// <value>The rtsp URL value exposed by <see cref="MediaSession"/>.</value>
    public string RtspUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the rtsp relay port value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rtsp relay port value exposed by <see cref="MediaSession"/>.</value>
    public int RtspRelayPort { get; set; }
    /// <summary>
    /// Gets the web rtc value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The web rtc value exposed by <see cref="MediaSession"/>.</value>
    public WebRtcSignalingService WebRtc { get; }
    /// <summary>
    /// Gets or sets the chat value that forms part of the media session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat value exposed by <see cref="MediaSession"/>.</value>
    public PlatformChatService? Chat { get; set; }

    /// <summary>
    /// Performs apply for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    public void Apply(JsonElement request)
    {
        try
        {
            logger.LogTrace($"Applying a media-session request to session {Id}.");
            Name = ReadString(request, "publicationName") ?? defaults.PublicationName;
            DryRun = ReadBool(request, "dryRun");
            Recording = request.TryGetProperty("recording", out var recording) && ReadBool(recording, "enabled");
            MasterWidth = ReadInt(request, "masterWidth", defaults.MasterWidth);
            MasterHeight = ReadInt(request, "masterHeight", defaults.MasterHeight);
            MasterFrameRate = ReadInt(request, "masterFrameRate", defaults.MasterFrameRate);
            if (request.TryGetProperty("adaptiveMedia", out var adaptiveMedia) && adaptiveMedia.ValueKind == JsonValueKind.Object)
            {
                AdaptiveMedia.Enabled = !adaptiveMedia.TryGetProperty("enabled", out var adaptiveEnabled) || adaptiveEnabled.ValueKind != JsonValueKind.False;
                AdaptiveMedia.AdaptVideo = !adaptiveMedia.TryGetProperty("adaptVideo", out var adaptVideo) || adaptVideo.ValueKind != JsonValueKind.False;
                AdaptiveMedia.AdaptAudio = !adaptiveMedia.TryGetProperty("adaptAudio", out var adaptAudio) || adaptAudio.ValueKind != JsonValueKind.False;
                AdaptiveMedia.UseProviderKnowledge = !adaptiveMedia.TryGetProperty("useProviderKnowledge", out var providerKnowledge) || providerKnowledge.ValueKind != JsonValueKind.False;
                AdaptiveMedia.UseBrowserCapabilityProbe = !adaptiveMedia.TryGetProperty("useBrowserCapabilityProbe", out var capabilityProbe) || capabilityProbe.ValueKind != JsonValueKind.False;
                var profileName = ReadString(adaptiveMedia, "profile");
                if (Enum.TryParse<PublicationAdaptiveQualityProfile>(profileName, true, out var profile)) AdaptiveMedia.Profile = profile;
                AdaptiveMedia.PreserveNativeResolution = !adaptiveMedia.TryGetProperty("preserveNativeResolution", out var preserveNative) || preserveNative.ValueKind != JsonValueKind.False;
                AdaptiveMedia.AllowFrameRateReduction = !adaptiveMedia.TryGetProperty("allowFrameRateReduction", out var allowFrameRate) || allowFrameRate.ValueKind != JsonValueKind.False;
                AdaptiveMedia.AllowResolutionReduction = adaptiveMedia.TryGetProperty("allowResolutionReduction", out var allowResolution) && allowResolution.ValueKind == JsonValueKind.True;
            }
            PreferDeviceTimestamps = !request.TryGetProperty("preferDeviceTimestamps", out var timestamps)
                ? defaults.PreferDeviceTimestamps
                : timestamps.ValueKind != JsonValueKind.False;
            FfmpegPath = ReadString(request, "ffmpegPath") ?? string.Empty;
            HardwareEncoder = ReadInt(request, "hardwareEncoder", defaults.HardwareEncoder);
            ApplyOutputs(request);
            ApplyRecording(request);
            ApplyLan(request);
            ApplyHotkeys(request);
            logger.LogInformation($"Applied media-session request to session {Id} with {OutputDefinitions.Count} output definitions.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not apply a media-session request to session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Sets ingest for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="announcement">Ingest announcement dependency used by the media session workflow to provide the corresponding application capability.</param>
    public void SetIngest(Guid? outputId, IngestAnnouncement announcement)
    {
        try
        {
            if (outputId is { } id)
            {
                OutputIngests[id] = announcement;
            }
            else
            {
                Ingest = announcement;
            }
            logger.LogTrace($"Stored ingest announcement for session {Id} and output {outputId}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not store an ingest announcement for session {Id} and output {outputId}.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves ingest for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <returns>The ingest announcement produced by the operation.</returns>
    public IngestAnnouncement? GetIngest(Guid? outputId)
    {
        try
        {
            var announcement = outputId is { } id && OutputIngests.TryGetValue(id, out var outputIngest)
                ? outputIngest
                : outputId is null ? Ingest : null;
            logger.LogTrace($"Resolved ingest announcement for session {Id} and output {outputId}.");
            return announcement;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve an ingest announcement for session {Id} and output {outputId}.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether ingest for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool HasIngest(Guid? outputId)
    {
        try
        {
            var hasIngest = GetIngest(outputId) is not null;
            logger.LogTrace($"Session {Id} ingest availability for output {outputId} is {hasIngest}.");
            return hasIngest;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not determine ingest availability for session {Id} and output {outputId}.");
            throw;
        }
    }

    /// <summary>
    /// Performs subscribe ingest for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <returns>The GUID identifier byte initialization chunk channel reader byte reader produced by the operation.</returns>
    public (Guid Id, byte[]? InitializationChunk, ChannelReader<byte[]> Reader) SubscribeIngest()
    {
        try
        {
            lock (ingestSubscriberSync)
            {
                var subscriberId = Guid.NewGuid();
                var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(defaults.IngestChannelCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
                ingestSubscribers[subscriberId] = channel;
                logger.LogInformation($"Registered ingest subscriber {subscriberId} for media session {Id}.");
                return (subscriberId, webmInitializationChunk?.ToArray(), channel.Reader);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not register an ingest subscriber for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Performs unsubscribe ingest for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="subscriberId">Identifier of the subscriber to use for this operation.</param>
    public void UnsubscribeIngest(Guid subscriberId)
    {
        try
        {
            Channel<byte[]>? channel;
            lock (ingestSubscriberSync)
            {
                if (!ingestSubscribers.Remove(subscriberId, out channel))
                {
                    logger.LogTrace($"Ingest subscriber {subscriberId} was not registered for media session {Id}.");
                    return;
                }
            }
            channel.Writer.TryComplete();
            logger.LogInformation($"Removed ingest subscriber {subscriberId} from media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not remove ingest subscriber {subscriberId} from media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Publishes ingest chunk for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="chunk">Chunk value supplied to the media session operation and used when producing its result.</param>
    public void PublishIngestChunk(byte[] chunk)
    {
        try
        {
            if (chunk.Length == 0)
            {
                logger.LogTrace($"Ignored an empty ingest chunk for media session {Id}.");
                return;
            }

            ChannelWriter<byte[]>[] writers;
            lock (ingestSubscriberSync)
            {
                webmInitializationChunk ??= chunk.ToArray();
                writers = ingestSubscribers.Values.Select(item => item.Writer).ToArray();
            }
            foreach (var writer in writers)
            {
                writer.TryWrite(chunk);
            }
            logger.LogTrace($"Published an ingest chunk to {writers.Length} subscribers for media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not publish an ingest chunk for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Completes ingest subscribers for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    public void CompleteIngestSubscribers()
    {
        try
        {
            Channel<byte[]>[] channels;
            lock (ingestSubscriberSync)
            {
                channels = ingestSubscribers.Values.ToArray();
                ingestSubscribers.Clear();
                webmInitializationChunk = null;
            }
            foreach (var channel in channels)
            {
                channel.Writer.TryComplete();
            }
            logger.LogInformation($"Completed {channels.Length} ingest subscribers for media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not complete ingest subscribers for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Performs public view for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <returns>The object produced by the operation.</returns>
    public object PublicView()
    {
        try
        {
            var view = new
            {
                id = Id,
                name = Name,
                dryRun = DryRun,
                startedUtc = StartedUtc,
                recording = Recording,
                programPageId = ProgramPageId,
                lanEnabled = LanEnabled,
                lanStatus = LanServer?.Status,
                lanBrowserUrl = LanServer?.BrowserUrl,
                lanHlsUrl = LanServer?.HlsUrl,
                lanRtspUrl = RtspUrl,
                outputs = Outputs,
                hotkeyCount = Hotkeys.Count,
                ingest = Ingest,
                outputIngests = OutputIngests.Keys,
                chatStatus = Chat?.Status,
                encoderStatus = Encoder?.Status,
                encoderError = Encoder?.LastError,
                status = Ingest is null && OutputIngests.IsEmpty ? defaults.WaitingForRendererStatus : Encoder?.Status ?? defaults.ReadyStatus
            };
            logger.LogTrace($"Created the public view for media session {Id}.");
            return view;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create the public view for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Applies outputs for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void ApplyOutputs(JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("outputs", out var outputs) || outputs.ValueKind != JsonValueKind.Array)
            {
                logger.LogTrace($"No output definitions were supplied for media session {Id}.");
                return;
            }

            foreach (var output in outputs.EnumerateArray())
            {
                if (!output.TryGetProperty("outputId", out var id) || !id.TryGetGuid(out var outputId))
                {
                    continue;
                }
                Outputs[outputId] = !output.TryGetProperty("enabled", out var enabledProperty) || enabledProperty.ValueKind != JsonValueKind.False;
                OutputDefinitions.Add(new MediaOutputDefinition
                {
                    OutputId = outputId,
                    Name = ReadString(output, "name") ?? defaults.OutputName,
                    Provider = ReadInt(output, "provider", defaults.OutputProvider),
                    Transport = ReadInt(output, "transport", defaults.OutputTransport),
                    Endpoint = ReadString(output, "endpoint") ?? string.Empty,
                    ChannelId = ReadString(output, "channelId") ?? string.Empty,
                    AccountName = ReadString(output, "accountName") ?? string.Empty,
                    Secret = ReadString(output, "secret") ?? string.Empty,
                    ChatEnabled = ReadBool(output, "chatEnabled"),
                    ChatSecret = ReadString(output, "chatSecret") ?? string.Empty,
                    TestMode = ReadBool(output, "testMode"),
                    Width = ReadInt(output, "width", defaults.OutputWidth),
                    Height = ReadInt(output, "height", defaults.OutputHeight),
                    FrameRate = ReadInt(output, "frameRate", defaults.OutputFrameRate),
                    VideoBitrateKbps = ReadInt(output, "videoBitrateKbps", defaults.VideoBitrateKbps),
                    AudioBitrateKbps = ReadInt(output, "audioBitrateKbps", defaults.AudioBitrateKbps),
                    KeyFrameIntervalSeconds = ReadInt(output, "keyFrameIntervalSeconds", defaults.KeyFrameIntervalSeconds),
                    VideoCodec = ReadInt(output, "videoCodec", defaults.VideoCodec),
                    AudioCodec = ReadInt(output, "audioCodec", defaults.AudioCodec)
                });
            }
            logger.LogTrace($"Applied {OutputDefinitions.Count} output definitions to media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not apply output definitions to media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Applies recording for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void ApplyRecording(JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("recording", out var recordingSettings))
            {
                logger.LogTrace($"No recording definition was supplied for media session {Id}.");
                return;
            }
            RecordingDefinition.Enabled = ReadBool(recordingSettings, "enabled");
            RecordingDefinition.DestinationDirectory = ReadString(recordingSettings, "destinationDirectory") ?? string.Empty;
            RecordingDefinition.Variant = ReadInt(recordingSettings, "variant", defaults.RecordingVariant);
            RecordingDefinition.Container = ReadString(recordingSettings, "container") ?? defaults.RecordingContainer;
            RecordingDefinition.SegmentSeconds = ReadInt(recordingSettings, "segmentSeconds", defaults.RecordingSegmentSeconds);
            RecordingDefinition.RemuxToMp4AfterStop = ReadBool(recordingSettings, "remuxToMp4AfterStop");
            if (recordingSettings.TryGetProperty("selectedOutputIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
            {
                foreach (var value in selected.EnumerateArray())
                {
                    if (value.TryGetGuid(out var selectedId))
                    {
                        RecordingDefinition.SelectedOutputIds.Add(selectedId);
                    }
                }
            }
            logger.LogTrace($"Applied the recording definition to media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not apply the recording definition to media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Applies LAN for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void ApplyLan(JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("lan", out var lanSettings))
            {
                logger.LogTrace($"No LAN definition was supplied for media session {Id}.");
                return;
            }
            LanDefinition.Enabled = ReadBool(lanSettings, "enabled");
            LanDefinition.BindAddress = ReadString(lanSettings, "bindAddress") ?? defaults.LanBindAddress;
            LanDefinition.Port = Math.Clamp(ReadInt(lanSettings, "port", defaults.LanPort), defaults.MinimumPort, defaults.MaximumPort);
            LanDefinition.Width = Math.Clamp(ReadInt(lanSettings, "width", defaults.OutputWidth), defaults.MinimumWidth, defaults.MaximumWidth);
            LanDefinition.Height = Math.Clamp(ReadInt(lanSettings, "height", defaults.OutputHeight), defaults.MinimumHeight, defaults.MaximumHeight);
            LanDefinition.FrameRate = Math.Clamp(ReadInt(lanSettings, "frameRate", defaults.OutputFrameRate), defaults.MinimumFrameRate, defaults.MaximumFrameRate);
            LanDefinition.VideoBitrateKbps = Math.Clamp(ReadInt(lanSettings, "videoBitrateKbps", defaults.LanVideoBitrateKbps), defaults.MinimumVideoBitrateKbps, defaults.MaximumVideoBitrateKbps);
            LanDefinition.AudioBitrateKbps = Math.Clamp(ReadInt(lanSettings, "audioBitrateKbps", defaults.AudioBitrateKbps), 32, Math.Max(32, defaults.AdaptiveQuality.MaximumAudioBitrateKbps));
            LanDefinition.EnableBrowserWebRtc = !lanSettings.TryGetProperty("enableBrowserWebRtc", out var browserPlayback)
                ? defaults.EnableBrowserWebRtc
                : browserPlayback.ValueKind != JsonValueKind.False;
            LanDefinition.EnableHls = !lanSettings.TryGetProperty("enableHls", out var hls)
                ? defaults.EnableHls
                : hls.ValueKind != JsonValueKind.False;
            LanDefinition.EnableRtsp = lanSettings.TryGetProperty("enableRtsp", out var rtsp)
                ? rtsp.ValueKind == JsonValueKind.True
                : defaults.EnableRtsp;
            LanDefinition.RtspPort = Math.Clamp(ReadInt(lanSettings, "rtspPort", defaults.RtspPort), defaults.MinimumPort, defaults.MaximumPort);
            LanDefinition.RequireAccessToken = !lanSettings.TryGetProperty("requireAccessToken", out var tokenRequired)
                ? defaults.RequireAccessToken
                : tokenRequired.ValueKind != JsonValueKind.False;
            LanDefinition.ViewerLimit = Math.Clamp(ReadInt(lanSettings, "viewerLimit", defaults.ViewerLimit), defaults.MinimumViewerLimit, defaults.MaximumViewerLimit);
            logger.LogTrace($"Applied the LAN definition to media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not apply the LAN definition to media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Applies hotkeys for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void ApplyHotkeys(JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("hotkeys", out var hotkeys) || hotkeys.ValueKind != JsonValueKind.Array)
            {
                logger.LogTrace($"No hotkeys were supplied for media session {Id}.");
                return;
            }
            foreach (var item in hotkeys.EnumerateArray())
            {
                var hotkeyId = item.TryGetProperty("id", out var idProperty) && idProperty.TryGetGuid(out var parsedId) ? parsedId : Guid.NewGuid();
                var targetId = item.TryGetProperty("targetId", out var targetProperty) && targetProperty.TryGetGuid(out var parsedTarget) ? parsedTarget : (Guid?)null;
                Hotkeys.Add(new MediaHotkey(
                    hotkeyId,
                    ReadString(item, "gesture") ?? string.Empty,
                    ReadString(item, "command") ?? string.Empty,
                    targetId,
                    ReadBool(item, "global")));
            }
            logger.LogTrace($"Applied {Hotkeys.Count} hotkeys to media session {Id}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not apply hotkeys to media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Reads string for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the media session operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the media session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? ReadString(JsonElement element, string name)
    {
        try
        {
            var value = element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
            logger.LogTrace($"Read string property {name} for media session {Id}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read string property {name} for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Reads bool for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the media session operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the media session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ReadBool(JsonElement element, string name)
    {
        try
        {
            var value = element.TryGetProperty(name, out var property)
                && property.ValueKind is JsonValueKind.True or JsonValueKind.False
                && property.GetBoolean();
            logger.LogTrace($"Read Boolean property {name} for media session {Id}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read Boolean property {name} for media session {Id}.");
            throw;
        }
    }

    /// <summary>
    /// Reads int for <see cref="MediaSession"/>, keeping the operation consistent with the state and invariants of the surrounding media session workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the media session operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the media session operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the media session operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(JsonElement element, string name, int fallback)
    {
        try
        {
            var value = element.TryGetProperty(name, out var property) && property.TryGetInt32(out var parsedValue)
                ? parsedValue
                : fallback;
            logger.LogTrace($"Read integer property {name} for media session {Id}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read integer property {name} for media session {Id}.");
            throw;
        }
    }
}
