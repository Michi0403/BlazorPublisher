using System.Text.Json;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.MediaHost;

/// <summary>
/// In-process facade over PublisherStudio's integrated streaming runtime.
/// No second executable, loopback port, or HTTP client is involved. Browser-facing
/// capture and ingest sockets remain available as same-origin application endpoints.
/// </summary>
/// <param name="profiles">Streaming profile store dependency used by the streaming media host workflow to provide the corresponding application capability.</param>
/// <param name="twitchOAuth">Twitch o auth service dependency used by the streaming media host workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Runtime value supplied to the streaming media host operation and used when producing its result.</param>
/// <param name="sessions">Sessions value supplied to the streaming media host operation and used when producing its result.</param>
public sealed class StreamingMediaHostClient(
    StreamingProfileStore profiles,
    TwitchOAuthService twitchOAuth,
    StreamingRuntimeUseCases runtime,
    StreamingSessionUseCases sessions)
{
    /// <summary>
    /// Stores the internal web JSON state used by <see cref="StreamingMediaHostClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    /// <summary>
    /// Stores the streaming profile store dependency used by <see cref="StreamingMediaHostClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly StreamingProfileStore _profiles = profiles;
    /// <summary>
    /// Stores the twitch o auth service dependency used by <see cref="StreamingMediaHostClient"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly TwitchOAuthService _twitchOAuth = twitchOAuth;
    /// <summary>
    /// Stores the internal runtime state used by <see cref="StreamingMediaHostClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingRuntimeUseCases _runtime = runtime;
    /// <summary>
    /// Stores the internal sessions state used by <see cref="StreamingMediaHostClient"/> while executing its surrounding workflow.
    /// </summary>
    private readonly StreamingSessionUseCases _sessions = sessions;

    /// <summary>
    /// Discovers native devices for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<List<PublisherStudio.BusinessObjects.NativeMediaDeviceInfo>> DiscoverNativeDevicesAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var settings = await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
            var devices = await _runtime.DiscoverDevicesAsync(settings.FfmpegPath, cancellationToken).ConfigureAwait(false);
            return devices.ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.DiscoverNativeDevicesAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether available for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(true);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.IsAvailableAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs start for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="document">Document value supplied to the streaming media host operation and used when producing its result.</param>
    /// <param name="dryRun">Value indicating whether dry run should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The media host session response produced by the operation.</returns>
    public async Task<MediaHostSessionResponse?> StartAsync(PublicationDocument document, bool dryRun, CancellationToken cancellationToken = default)
    {
    try
    {
            var settings = await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
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
                        ? await _twitchOAuth.EnsureValidAccessTokenAsync(profile.Id, cancellationToken).ConfigureAwait(false) ?? string.Empty
                        : await _profiles.ResolveChatSecretAsync(profile.Id, cancellationToken).ConfigureAwait(false) ?? string.Empty;
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
                    Secret = profile is null ? string.Empty : await _profiles.ResolveSecretAsync(profile.Id, cancellationToken).ConfigureAwait(false) ?? string.Empty,
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
                AdaptiveMedia = document.Streaming.AdaptiveMedia,
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.StartAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs stop for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> StopAsync(Guid sessionId, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(_sessions.Stop(sessionId));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.StopAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets output enabled for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> SetOutputEnabledAsync(Guid sessionId, Guid outputId, bool enabled, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(_sessions.SetOutput(sessionId, outputId, enabled));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.SetOutputEnabledAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets program page for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="pageId">Identifier of the page to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> SetProgramPageAsync(Guid sessionId, Guid pageId, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(_sessions.SetProgramPage(sessionId, pageId));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.SetProgramPageAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets recording for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public Task<bool> SetRecordingAsync(Guid sessionId, bool enabled, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(_sessions.SetRecording(sessionId, enabled));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.SetRecordingAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads events for <see cref="StreamingMediaHostClient"/>, keeping the operation consistent with the state and invariants of the surrounding streaming media host workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<MediaHostHotkeyEvent>> ReadEventsAsync(Guid sessionId, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(_sessions.DrainEvents(sessionId));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingMediaHostClient.ReadEventsAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents the input contract for media host start session, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class MediaHostStartSessionRequest
{
    /// <summary>
    /// Gets or sets the stable publication identifier used to identify or correlate this media host start session instance with related application state.
    /// </summary>
    /// <value>The publication identifier value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public Guid PublicationId { get; set; }
    /// <summary>
    /// Gets or sets the publication name value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publication name value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public string PublicationName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether dry run applies to the media host start session state.
    /// </summary>
    /// <value>The dry run value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public bool DryRun { get; set; }
    /// <summary>
    /// Gets or sets the master width value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master width value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public int MasterWidth { get; set; }
    /// <summary>
    /// Gets or sets the master height value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master height value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public int MasterHeight { get; set; }
    /// <summary>
    /// Gets or sets the master frame rate value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The master frame rate value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public int MasterFrameRate { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether prefer device timestamps applies to the media host start session state.
    /// </summary>
    /// <value>The prefer device timestamps value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public bool PreferDeviceTimestamps { get; set; }
    /// <summary>Gets or sets the per-publication adaptive media quality choices forwarded to the media host.</summary>
    /// <value>The adaptive media settings used by recording and streaming encoder paths.</value>
    public PublicationAdaptiveMediaSettings AdaptiveMedia { get; set; } = new();
    /// <summary>
    /// Gets or sets the FFmpeg path used by this media host start session instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg path value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public string FfmpegPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hardware encoder value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware encoder value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    /// <summary>
    /// Gets or sets the outputs collection maintained or exposed by this media host start session instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public List<MediaHostOutputRequest> Outputs { get; set; } = [];
    /// <summary>
    /// Gets or sets the recording value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public PublicationRecordingSettings Recording { get; set; } = new();
    /// <summary>
    /// Gets or sets the LAN value that forms part of the media host start session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LAN value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public PublicationLanStreamingSettings Lan { get; set; } = new();
    /// <summary>
    /// Gets or sets the hotkeys collection maintained or exposed by this media host start session instance for downstream processing.
    /// </summary>
    /// <value>The hotkeys value exposed by <see cref="MediaHostStartSessionRequest"/>.</value>
    public List<PublicationStreamingHotkey> Hotkeys { get; set; } = [];
}

/// <summary>
/// Represents the input contract for media host output, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class MediaHostOutputRequest
{
    /// <summary>
    /// Gets or sets the stable output identifier used to identify or correlate this media host output instance with related application state.
    /// </summary>
    /// <value>The output identifier value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public Guid OutputId { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the media host output state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the provider value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public PublicationStreamProvider Provider { get; set; }
    /// <summary>
    /// Gets or sets the transport value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public PublicationStreamTransport Transport { get; set; }
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this media host output state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable channel identifier used to identify or correlate this media host output instance with related application state.
    /// </summary>
    /// <value>The channel identifier value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string ChannelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the account name value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The account name value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the secret value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The secret value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string Secret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether chat enabled applies to the media host output state.
    /// </summary>
    /// <value>The chat enabled value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public bool ChatEnabled { get; set; }
    /// <summary>
    /// Gets or sets the chat secret value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat secret value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public string ChatSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether test mode applies to the media host output state.
    /// </summary>
    /// <value>The test mode value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public bool TestMode { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int FrameRate { get; set; }
    /// <summary>
    /// Gets or sets the video bitrate kbps value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video bitrate kbps value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int VideoBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the audio bitrate kbps value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio bitrate kbps value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int AudioBitrateKbps { get; set; }
    /// <summary>
    /// Gets or sets the key frame interval seconds value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key frame interval seconds value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public int KeyFrameIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets the video codec value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video codec value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public PublicationStreamVideoCodec VideoCodec { get; set; }
    /// <summary>
    /// Gets or sets the audio codec value that forms part of the media host output state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio codec value exposed by <see cref="MediaHostOutputRequest"/>.</value>
    public PublicationStreamAudioCodec AudioCodec { get; set; }
}

/// <summary>
/// Represents the outcome of media host session, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class MediaHostSessionResponse
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this media host session instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="MediaHostSessionResponse"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the media host session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="MediaHostSessionResponse"/>.</value>
    public string Status { get; set; } = string.Empty;
}

