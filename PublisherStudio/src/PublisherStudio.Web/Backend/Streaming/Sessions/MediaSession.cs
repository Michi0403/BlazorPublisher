using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace PublisherStudio.Backend.Streaming.Sessions;

public sealed class MediaSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Publication";
    public bool DryRun { get; init; }
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StoppedUtc { get; set; }
    public bool Recording { get; set; }
    public Guid? ProgramPageId { get; set; }
    public bool LanEnabled => LanDefinition.Enabled;
    public MediaLanDefinition LanDefinition { get; } = new();
    public LanStreamingServer? LanServer { get; set; }
    public int MasterWidth { get; init; } = 3840;
    public int MasterHeight { get; init; } = 2160;
    public int MasterFrameRate { get; init; } = 60;
    public bool PreferDeviceTimestamps { get; init; } = true;
    public string FfmpegPath { get; init; } = string.Empty;
    public int HardwareEncoder { get; init; }
    public ConcurrentDictionary<Guid, bool> Outputs { get; } = new();
    public List<MediaOutputDefinition> OutputDefinitions { get; } = [];
    public MediaRecordingDefinition RecordingDefinition { get; } = new();
    public List<MediaHotkey> Hotkeys { get; } = [];
    public IngestAnnouncement? Ingest { get; private set; }
    public ConcurrentDictionary<Guid, IngestAnnouncement> OutputIngests { get; } = new();
    public EncoderSessionController? Encoder { get; set; }
    public string HlsDirectory { get; set; } = string.Empty;
    public string RtspUrl { get; set; } = string.Empty;
    public int RtspRelayPort { get; set; }
    public WebRtcSignalingHub WebRtc { get; } = new();
    public PlatformChatHub? Chat { get; set; }

    private readonly object _ingestSubscriberSync = new();
    private readonly Dictionary<Guid, Channel<byte[]>> _ingestSubscribers = [];
    private byte[]? _webmInitializationChunk;

    public void SetIngest(Guid? outputId, IngestAnnouncement announcement)
    {
        if (outputId is { } id) OutputIngests[id] = announcement;
        else Ingest = announcement;
    }

    public IngestAnnouncement? GetIngest(Guid? outputId) =>
        outputId is { } id && OutputIngests.TryGetValue(id, out var outputIngest) ? outputIngest : outputId is null ? Ingest : null;

    public bool HasIngest(Guid? outputId) => GetIngest(outputId) is not null;

    public (Guid Id, byte[]? InitializationChunk, ChannelReader<byte[]> Reader) SubscribeIngest()
    {
        lock (_ingestSubscriberSync)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(180)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _ingestSubscribers[id] = channel;
            return (id, _webmInitializationChunk?.ToArray(), channel.Reader);
        }
    }

    public void UnsubscribeIngest(Guid id)
    {
        Channel<byte[]>? channel;
        lock (_ingestSubscriberSync)
        {
            if (!_ingestSubscribers.Remove(id, out channel)) return;
        }
        channel.Writer.TryComplete();
    }

    public void PublishIngestChunk(byte[] chunk)
    {
        if (chunk.Length == 0) return;
        ChannelWriter<byte[]>[] writers;
        lock (_ingestSubscriberSync)
        {
            _webmInitializationChunk ??= chunk.ToArray();
            writers = _ingestSubscribers.Values.Select(item => item.Writer).ToArray();
        }
        foreach (var writer in writers) writer.TryWrite(chunk);
    }

    public void CompleteIngestSubscribers()
    {
        Channel<byte[]>[] channels;
        lock (_ingestSubscriberSync)
        {
            channels = _ingestSubscribers.Values.ToArray();
            _ingestSubscribers.Clear();
            _webmInitializationChunk = null;
        }
        foreach (var channel in channels) channel.Writer.TryComplete();
    }

    public static MediaSession From(JsonElement request)
    {
        var session = new MediaSession
        {
            Name = ReadString(request, "publicationName") ?? "Publication",
            DryRun = ReadBool(request, "dryRun"),
            Recording = request.TryGetProperty("recording", out var recording) && ReadBool(recording, "enabled"),
            MasterWidth = ReadInt(request, "masterWidth", 3840),
            MasterHeight = ReadInt(request, "masterHeight", 2160),
            MasterFrameRate = ReadInt(request, "masterFrameRate", 60),
            PreferDeviceTimestamps = !request.TryGetProperty("preferDeviceTimestamps", out var timestamps) || timestamps.ValueKind != JsonValueKind.False,
            FfmpegPath = ReadString(request, "ffmpegPath") ?? string.Empty,
            HardwareEncoder = ReadInt(request, "hardwareEncoder")
        };
        if (request.TryGetProperty("outputs", out var outputs) && outputs.ValueKind == JsonValueKind.Array)
            foreach (var output in outputs.EnumerateArray())
            {
                if (!output.TryGetProperty("outputId", out var id) || !id.TryGetGuid(out var outputId)) continue;
                session.Outputs[outputId] = !output.TryGetProperty("enabled", out var enabledProperty) || enabledProperty.ValueKind != JsonValueKind.False;
                session.OutputDefinitions.Add(new MediaOutputDefinition
                {
                    OutputId = outputId,
                    Name = ReadString(output, "name") ?? "Output",
                    Provider = ReadInt(output, "provider"),
                    Transport = ReadInt(output, "transport"),
                    Endpoint = ReadString(output, "endpoint") ?? string.Empty,
                    ChannelId = ReadString(output, "channelId") ?? string.Empty,
                    AccountName = ReadString(output, "accountName") ?? string.Empty,
                    Secret = ReadString(output, "secret") ?? string.Empty,
                    ChatEnabled = ReadBool(output, "chatEnabled"),
                    ChatSecret = ReadString(output, "chatSecret") ?? string.Empty,
                    TestMode = ReadBool(output, "testMode"),
                    Width = ReadInt(output, "width", 1920),
                    Height = ReadInt(output, "height", 1080),
                    FrameRate = ReadInt(output, "frameRate", 60),
                    VideoBitrateKbps = ReadInt(output, "videoBitrateKbps", 6000),
                    AudioBitrateKbps = ReadInt(output, "audioBitrateKbps", 160),
                    KeyFrameIntervalSeconds = ReadInt(output, "keyFrameIntervalSeconds", 2),
                    VideoCodec = ReadInt(output, "videoCodec"),
                    AudioCodec = ReadInt(output, "audioCodec")
                });
            }
        if (request.TryGetProperty("recording", out var recordingSettings))
        {
            session.RecordingDefinition.Enabled = ReadBool(recordingSettings, "enabled");
            session.RecordingDefinition.DestinationDirectory = ReadString(recordingSettings, "destinationDirectory") ?? string.Empty;
            session.RecordingDefinition.Variant = ReadInt(recordingSettings, "variant");
            session.RecordingDefinition.Container = ReadString(recordingSettings, "container") ?? "mkv";
            session.RecordingDefinition.SegmentSeconds = ReadInt(recordingSettings, "segmentSeconds", 10);
            session.RecordingDefinition.RemuxToMp4AfterStop = ReadBool(recordingSettings, "remuxToMp4AfterStop");
            if (recordingSettings.TryGetProperty("selectedOutputIds", out var selected) && selected.ValueKind == JsonValueKind.Array)
                foreach (var value in selected.EnumerateArray()) if (value.TryGetGuid(out var selectedId)) session.RecordingDefinition.SelectedOutputIds.Add(selectedId);
        }
        if (request.TryGetProperty("lan", out var lanSettings))
        {
            session.LanDefinition.Enabled = ReadBool(lanSettings, "enabled");
            session.LanDefinition.BindAddress = ReadString(lanSettings, "bindAddress") ?? "127.0.0.1";
            session.LanDefinition.Port = Math.Clamp(ReadInt(lanSettings, "port", 17848), 1024, 65535);
            session.LanDefinition.Width = Math.Clamp(ReadInt(lanSettings, "width", 1920), 320, 7680);
            session.LanDefinition.Height = Math.Clamp(ReadInt(lanSettings, "height", 1080), 180, 4320);
            session.LanDefinition.FrameRate = Math.Clamp(ReadInt(lanSettings, "frameRate", 60), 15, 120);
            session.LanDefinition.VideoBitrateKbps = Math.Clamp(ReadInt(lanSettings, "videoBitrateKbps", 8000), 250, 200000);
            session.LanDefinition.EnableBrowserWebRtc = !lanSettings.TryGetProperty("enableBrowserWebRtc", out var browserPlayback) || browserPlayback.ValueKind != JsonValueKind.False;
            session.LanDefinition.EnableHls = !lanSettings.TryGetProperty("enableHls", out var hls) || hls.ValueKind != JsonValueKind.False;
            session.LanDefinition.EnableRtsp = ReadBool(lanSettings, "enableRtsp");
            session.LanDefinition.RtspPort = Math.Clamp(ReadInt(lanSettings, "rtspPort", 8554), 1024, 65535);
            session.LanDefinition.RequireAccessToken = !lanSettings.TryGetProperty("requireAccessToken", out var tokenRequired) || tokenRequired.ValueKind != JsonValueKind.False;
            session.LanDefinition.ViewerLimit = Math.Clamp(ReadInt(lanSettings, "viewerLimit", 50), 1, 10_000);
        }
        if (request.TryGetProperty("hotkeys", out var hotkeys) && hotkeys.ValueKind == JsonValueKind.Array)
            foreach (var item in hotkeys.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProperty) && idProperty.TryGetGuid(out var parsedId) ? parsedId : Guid.NewGuid();
                var targetId = item.TryGetProperty("targetId", out var targetProperty) && targetProperty.TryGetGuid(out var parsedTarget) ? parsedTarget : (Guid?)null;
                session.Hotkeys.Add(new MediaHotkey(
                    id,
                    ReadString(item, "gesture") ?? string.Empty,
                    ReadString(item, "command") ?? string.Empty,
                    targetId,
                    ReadBool(item, "global")));
            }
        return session;
    }

    public object PublicView() => new
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
        status = Ingest is null && OutputIngests.IsEmpty ? "waiting-for-renderer" : Encoder?.Status ?? "ready"
    };

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static bool ReadBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False && property.GetBoolean();
    private static int ReadInt(JsonElement element, string name, int fallback = 0) =>
        element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value) ? value : fallback;
}
