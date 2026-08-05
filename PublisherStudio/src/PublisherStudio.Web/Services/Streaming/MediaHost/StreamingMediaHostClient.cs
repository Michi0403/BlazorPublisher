using System.Text.Json;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.MediaHost;

/// <summary>
/// In-process facade over PublisherStudio's integrated streaming runtime.
/// No second executable, loopback port, or HTTP client is involved. Browser-facing
/// capture and ingest sockets remain available as same-origin application endpoints.
/// </summary>
public sealed class StreamingMediaHostClient(
    StreamingProfileStore profiles,
    TwitchOAuthService twitchOAuth,
    StreamingRuntimeUseCases runtime,
    StreamingSessionUseCases sessions)
{
    private readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly StreamingProfileStore _profiles = profiles;
    private readonly TwitchOAuthService _twitchOAuth = twitchOAuth;
    private readonly StreamingRuntimeUseCases _runtime = runtime;
    private readonly StreamingSessionUseCases _sessions = sessions;

    /// <summary>
    /// Runs the discover native devices async operation.
    /// </summary>
    public async Task<List<PublisherStudio.BusinessObjects.NativeMediaDeviceInfo>> DiscoverNativeDevicesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var devices = await _runtime.DiscoverDevicesAsync(settings.FfmpegPath, cancellationToken);
        return devices.ToList();
    }

    /// <summary>
    /// Determines whether available async.
    /// </summary>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    /// <summary>
    /// Starts async.
    /// </summary>
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

    /// <summary>
    /// Stops async.
    /// </summary>
    public Task<bool> StopAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.Stop(sessionId));

    /// <summary>
    /// Sets output enabled async.
    /// </summary>
    public Task<bool> SetOutputEnabledAsync(Guid sessionId, Guid outputId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetOutput(sessionId, outputId, enabled));

    /// <summary>
    /// Sets program page async.
    /// </summary>
    public Task<bool> SetProgramPageAsync(Guid sessionId, Guid pageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetProgramPage(sessionId, pageId));

    /// <summary>
    /// Sets recording async.
    /// </summary>
    public Task<bool> SetRecordingAsync(Guid sessionId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.SetRecording(sessionId, enabled));

    /// <summary>
    /// Reads events async.
    /// </summary>
    public Task<IReadOnlyList<MediaHostHotkeyEvent>> ReadEventsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.DrainEvents(sessionId));
}

/// <summary>
/// Represents a media host start session request.
/// </summary>
public sealed class MediaHostStartSessionRequest
{
    /// <summary>
    /// Gets or sets publication identifier.
    /// </summary>
    public Guid PublicationId { get; set; }
    /// <summary>
    /// Gets or sets publication name.
    /// </summary>
    public string PublicationName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets dry run.
    /// </summary>
    public bool DryRun { get; set; }
    /// <summary>
    /// Gets or sets master width.
    /// </summary>
    public int MasterWidth { get; set; }
    /// <summary>
    /// Gets or sets master height.
    /// </summary>
    public int MasterHeight { get; set; }
    /// <summary>
    /// Gets or sets master frame rate.
    /// </summary>
    public int MasterFrameRate { get; set; }
    /// <summary>
    /// Gets or sets prefer device timestamps.
    /// </summary>
    public bool PreferDeviceTimestamps { get; set; }
    /// <summary>
    /// Gets or sets FFmpeg path.
    /// </summary>
    public string FfmpegPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets hardware encoder.
    /// </summary>
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    /// <summary>
    /// Gets or sets outputs.
    /// </summary>
    public List<MediaHostOutputRequest> Outputs { get; set; } = [];
    /// <summary>
    /// Gets or sets recording.
    /// </summary>
    public PublicationRecordingSettings Recording { get; set; } = new();
    /// <summary>
    /// Gets or sets LAN.
    /// </summary>
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    /// <summary>
    /// Gets or sets hotkeys.
    /// </summary>
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } = [];
}

/// <summary>
/// Represents a media host output request.
/// </summary>
public sealed class MediaHostOutputRequest
{
    /// <summary>
    /// Gets or sets output identifier.
    /// </summary>
    public Guid OutputId { get; set; }
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets provider.
    /// </summary>
    public PublicationStreamProvider Provider { get; set; }
    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public PublicationStreamTransport Transport { get; set; }
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets channel identifier.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets account name.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets secret.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets chat enabled.
    /// </summary>
    public bool ChatEnabled { get; set; }
    /// <summary>
    /// Gets or sets chat secret.
    /// </summary>
    public string ChatSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets test mode.
    /// </summary>
    public bool TestMode { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets frame rate.
    /// </summary>
    public int FrameRate { get; set; }
    /// <summary>
    /// Gets or sets video bitrate kbps.
    /// </summary>
    public int VideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets audio bitrate kbps.
    /// </summary>
    public int AudioBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets key frame interval seconds.
    /// </summary>
    public int KeyFrameIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets video codec.
    /// </summary>
    public PublicationStreamVideoCodec VideoCodec { get; set; }
    /// <summary>
    /// Gets or sets audio codec.
    /// </summary>
    public PublicationStreamAudioCodec AudioCodec { get; set; }
}

/// <summary>
/// Represents a media host session response.
/// </summary>
public sealed class MediaHostSessionResponse
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

