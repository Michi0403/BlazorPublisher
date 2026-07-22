using System.Text.Json;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// In-process facade over PublisherStudio's integrated streaming runtime.
/// No second executable, loopback port, or HTTP client is involved. Browser-facing
/// capture and ingest sockets remain available as same-origin application endpoints.
/// </summary>
public sealed class StreamingMediaHostClient(StreamingProfileStore profiles, TwitchOAuthService twitchOAuth, MediaSessionRegistry sessions)
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly StreamingProfileStore _profiles = profiles;
    private readonly TwitchOAuthService _twitchOAuth = twitchOAuth;
    private readonly MediaSessionRegistry _sessions = sessions;

    public async Task<List<PublisherStudio.Domain.NativeMediaDeviceInfo>> DiscoverNativeDevicesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var devices = await NativeDeviceDiscovery.DiscoverAsync(settings.FfmpegPath, cancellationToken);
        return devices
            .Select(device => new PublisherStudio.Domain.NativeMediaDeviceInfo
            {
                Id = device.Id,
                Name = device.Name,
                Kind = device.Kind,
                Backend = device.Backend,
                ProcessId = device.ProcessId,
                WindowTitle = device.WindowTitle
            })
            .ToList();
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public async Task<MediaHostSessionResponse?> StartAsync(PublicationDocument document, bool dryRun, CancellationToken cancellationToken = default)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var providers = new List<MediaHostOutputRequest>();
        foreach (var output in document.Streaming.Outputs)
        {
            var profile = settings.Providers.FirstOrDefault(item => item.Id == output.ProfileId && item.Enabled);
            var chatSecret = string.Empty;
            var twitchOAuthChat = profile?.Provider == PublicationStreamProvider.Twitch
                && profile.AuthenticationMode == StreamingProviderAuthenticationMode.OAuth;
            var twitchOAuthHasChatScopes = profile is not null
                && profile.OAuthScopes.Contains("chat:read", StringComparison.OrdinalIgnoreCase)
                && profile.OAuthScopes.Contains("chat:edit", StringComparison.OrdinalIgnoreCase);
            if (profile?.ChatEnabled == true && (!twitchOAuthChat || twitchOAuthHasChatScopes))
            {
                chatSecret = twitchOAuthChat
                    ? await _twitchOAuth.EnsureValidAccessTokenAsync(profile.Id, cancellationToken) ?? string.Empty
                    : await _profiles.ResolveChatSecretAsync(profile.Id, cancellationToken) ?? string.Empty;
            }
            providers.Add(new MediaHostOutputRequest
            {
                OutputId = output.Id,
                Name = output.Name,
                Enabled = output.Enabled && profile is not null,
                Provider = output.Provider,
                Transport = profile?.Transport ?? PublicationStreamTransport.Rtmp,
                Endpoint = profile?.Endpoint ?? string.Empty,
                ChannelId = string.IsNullOrWhiteSpace(output.ChatChannel) ? profile?.ChannelId ?? string.Empty : output.ChatChannel,
                AccountName = profile?.AccountName ?? string.Empty,
                Secret = profile is null ? string.Empty : await _profiles.ResolveSecretAsync(profile.Id, cancellationToken) ?? string.Empty,
                ChatEnabled = profile?.ChatEnabled == true && !string.IsNullOrWhiteSpace(chatSecret),
                ChatSecret = chatSecret,
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

        var request = new MediaHostStartSessionRequest
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
        };

        try
        {
            var session = _sessions.Create(JsonSerializer.SerializeToElement(request, WebJson));
            return new MediaHostSessionResponse
            {
                SessionId = session.Id,
                Status = session.DryRun ? "dry-run" : "prepared"
            };
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> StopAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.Stop(sessionId));

    public Task<bool> SetOutputEnabledAsync(Guid sessionId, Guid outputId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetOutput(sessionId, outputId, enabled));

    public Task<bool> SetProgramPageAsync(Guid sessionId, Guid pageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetProgramPage(sessionId, pageId));

    public Task<bool> SetRecordingAsync(Guid sessionId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetRecording(sessionId, enabled));

    public Task<List<MediaHostHotkeyEvent>> ReadEventsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var events = _sessions.DrainEvents(sessionId)
            .Select(item => new MediaHostHotkeyEvent
            {
                Command = item.Command,
                TargetId = item.TargetId,
                TriggeredUtc = item.TriggeredUtc
            })
            .ToList();
        return Task.FromResult(events);
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
