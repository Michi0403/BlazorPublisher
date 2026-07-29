using PublisherStudio.Domain;
using PublisherStudio.Services.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace PublisherStudio.Services.Streaming.Sessions;

public interface IMediaSessionFactory
{
    MediaSession Create(JsonElement request);
}

public sealed class MediaSessionFactory(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILoggerFactory loggerFactory,
    ILogger<MediaSessionFactory> logger) : IMediaSessionFactory
{
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

public sealed class MediaSession
{
    private readonly PublisherMediaSessionDefaultsPolicy defaults;
    private readonly ILogger<MediaSession> logger;
    private readonly object ingestSubscriberSync = new();
    private readonly Dictionary<Guid, Channel<byte[]>> ingestSubscribers = [];
    private byte[]? webmInitializationChunk;

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

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool DryRun { get; private set; }
    public DateTimeOffset StartedUtc { get; private set; }
    public DateTimeOffset? StoppedUtc { get; set; }
    public bool Recording { get; set; }
    public Guid? ProgramPageId { get; set; }
    public bool LanEnabled => LanDefinition.Enabled;
    public MediaLanDefinition LanDefinition { get; } = new();
    public LanStreamingServer? LanServer { get; set; }
    public int MasterWidth { get; private set; }
    public int MasterHeight { get; private set; }
    public int MasterFrameRate { get; private set; }
    public bool PreferDeviceTimestamps { get; private set; }
    public string FfmpegPath { get; private set; } = string.Empty;
    public int HardwareEncoder { get; private set; }
    public ConcurrentDictionary<Guid, bool> Outputs { get; } = new();
    public List<MediaOutputDefinition> OutputDefinitions { get; } = [];
    public MediaRecordingDefinition RecordingDefinition { get; } = new();
    public List<MediaHotkey> Hotkeys { get; } = [];
    public IngestAnnouncement? Ingest { get; private set; }
    public ConcurrentDictionary<Guid, IngestAnnouncement> OutputIngests { get; } = new();
    public EncoderSessionService? Encoder { get; set; }
    public string HlsDirectory { get; set; } = string.Empty;
    public string RtspUrl { get; set; } = string.Empty;
    public int RtspRelayPort { get; set; }
    public WebRtcSignalingService WebRtc { get; }
    public PlatformChatService? Chat { get; set; }

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
