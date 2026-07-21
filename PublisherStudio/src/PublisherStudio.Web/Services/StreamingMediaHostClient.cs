using System.Net.Http.Json;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class StreamingMediaHostClient(IHttpClientFactory httpClientFactory, StreamingProfileStore profiles)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly StreamingProfileStore _profiles = profiles;

    public async Task<List<NativeMediaDeviceInfo>> DiscoverNativeDevicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _profiles.LoadAsync(cancellationToken);
            using var client = await CreateClientAsync(cancellationToken);
            var query = string.IsNullOrWhiteSpace(settings.FfmpegPath)
                ? string.Empty
                : $"?ffmpegPath={Uri.EscapeDataString(settings.FfmpegPath)}";
            return await client.GetFromJsonAsync<List<NativeMediaDeviceInfo>>($"api/mediahost/devices{query}", cancellationToken) ?? [];
        }
        catch { return []; }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = await CreateClientAsync(cancellationToken);
            using var response = await client.GetAsync("api/mediahost/capabilities", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<MediaHostSessionResponse?> StartAsync(PublicationDocument document, bool dryRun, CancellationToken cancellationToken = default)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var providers = new List<MediaHostOutputRequest>();
        foreach (var output in document.Streaming.Outputs)
        {
            var profile = settings.Providers.FirstOrDefault(item => item.Id == output.ProfileId && item.Enabled);
            providers.Add(new MediaHostOutputRequest
            {
                OutputId = output.Id,
                Name = output.Name,
                Enabled = output.Enabled && profile is not null,
                Provider = output.Provider,
                Transport = profile?.Transport ?? PublicationStreamTransport.Rtmps,
                Endpoint = profile?.Endpoint ?? string.Empty,
                ChannelId = string.IsNullOrWhiteSpace(output.ChatChannel) ? profile?.ChannelId ?? string.Empty : output.ChatChannel,
                AccountName = profile?.AccountName ?? string.Empty,
                Secret = profile is null ? string.Empty : await _profiles.ResolveSecretAsync(profile.Id, cancellationToken) ?? string.Empty,
                ChatEnabled = profile?.ChatEnabled == true,
                ChatSecret = profile is null ? string.Empty : await _profiles.ResolveChatSecretAsync(profile.Id, cancellationToken) ?? string.Empty,
                TestMode = dryRun || output.UseProviderTestMode,
                Width = output.Width,
                Height = output.Height,
                FrameRate = output.FrameRate,
                VideoBitrateKbps = output.VideoBitrateKbps,
                AudioBitrateKbps = output.AudioBitrateKbps,
                KeyFrameIntervalSeconds = output.KeyFrameIntervalSeconds,
                VideoCodec = output.VideoCodec,
                AudioCodec = output.AudioCodec
            });
        }
        var recording = new PublicationRecordingSettings
        {
            Enabled = document.Streaming.Recording.Enabled,
            DestinationDirectory = string.IsNullOrWhiteSpace(document.Streaming.Recording.DestinationDirectory)
                ? settings.DefaultRecordingDirectory
                : document.Streaming.Recording.DestinationDirectory,
            Variant = document.Streaming.Recording.Variant,
            SelectedOutputIds = [.. document.Streaming.Recording.SelectedOutputIds],
            Container = document.Streaming.Recording.Container,
            SegmentSeconds = document.Streaming.Recording.SegmentSeconds,
            RemuxToMp4AfterStop = document.Streaming.Recording.RemuxToMp4AfterStop
        };
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.PostAsJsonAsync("api/mediahost/sessions", new MediaHostStartSessionRequest
        {
            PublicationId = document.Id,
            PublicationName = document.Name,
            DryRun = dryRun,
            MasterWidth = document.Streaming.MasterWidth,
            MasterHeight = document.Streaming.MasterHeight,
            MasterFrameRate = document.Streaming.MasterFrameRate,
            PreferDeviceTimestamps = document.Streaming.PreferDeviceTimestamps,
            FfmpegPath = settings.FfmpegPath,
            HardwareEncoder = settings.HardwareEncoder,
            Outputs = providers,
            Recording = recording,
            Lan = document.Streaming.Lan,
            Hotkeys = document.Streaming.Hotkeys
        }, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MediaHostSessionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<bool> StopAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.DeleteAsync($"api/mediahost/sessions/{sessionId:D}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetOutputEnabledAsync(Guid sessionId, Guid outputId, bool enabled, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.PutAsJsonAsync($"api/mediahost/sessions/{sessionId:D}/outputs/{outputId:D}", new { enabled }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetProgramPageAsync(Guid sessionId, Guid pageId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.PutAsJsonAsync($"api/mediahost/sessions/{sessionId:D}/program-page", new { pageId }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetRecordingAsync(Guid sessionId, bool enabled, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.PutAsJsonAsync($"api/mediahost/sessions/{sessionId:D}/recording", new { enabled }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<MediaHostHotkeyEvent>> ReadEventsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var response = await client.GetAsync($"api/mediahost/sessions/{sessionId:D}/events", cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<MediaHostHotkeyEvent>>(cancellationToken: cancellationToken) ?? [];
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient(nameof(StreamingMediaHostClient));
        client.BaseAddress = new Uri($"http://127.0.0.1:{settings.MediaHostPort}/");
        client.Timeout = TimeSpan.FromSeconds(4);
        return client;
    }
}

public sealed class MediaHostStartSessionRequest
{
    public Guid PublicationId { get; set; }
    public string PublicationName { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public int MasterWidth { get; set; }
    public int MasterHeight { get; set; }
    public int MasterFrameRate { get; set; }
    public bool PreferDeviceTimestamps { get; set; }
    public string FfmpegPath { get; set; } = string.Empty;
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    public List<MediaHostOutputRequest> Outputs { get; set; } = [];
    public PublicationRecordingSettings Recording { get; set; } = new();
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } = [];
}

public sealed class MediaHostOutputRequest
{
    public Guid OutputId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public PublicationStreamProvider Provider { get; set; }
    public PublicationStreamTransport Transport { get; set; }
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
    public PublicationStreamVideoCodec VideoCodec { get; set; }
    public PublicationStreamAudioCodec AudioCodec { get; set; }
}

public sealed class MediaHostSessionResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
}


public sealed class MediaHostHotkeyEvent
{
    public string Command { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public DateTimeOffset TriggeredUtc { get; set; }
}
