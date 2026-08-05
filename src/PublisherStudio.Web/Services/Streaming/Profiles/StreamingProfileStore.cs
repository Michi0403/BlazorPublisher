using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.Profiles;

/// <summary>
/// Provides streaming profile store operations.
/// </summary>
public sealed class StreamingProfileStore
{
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Runs the streaming profile store operation.
    /// </summary>
    public StreamingProfileStore(IDataProtectionProvider protectionProvider)
    {
        // Keep the original purpose string so existing v1 streaming secrets remain readable.
        _protector = protectionProvider.CreateProtector("PublisherStudio.StreamingProfiles.v1");
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "Streaming");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "profiles.json");
    }

    /// <summary>
    /// Loads async.
    /// </summary>
    public async Task<StreamingMachineSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Saves provider async.
    /// </summary>
    public async Task<StreamingProviderProfile> SaveProviderAsync(StreamingProviderProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var existing = stored.Providers.FirstOrDefault(item => item.Id == profile.Id);
            var normalizedClientId = profile.OAuthClientId?.Trim() ?? string.Empty;
            var oauthClientChanged = existing is not null
                && !string.Equals(existing.OAuthClientId, normalizedClientId, StringComparison.Ordinal);
            var retainOAuthSession = profile.Provider == PublicationStreamProvider.Twitch && !oauthClientChanged;
            var secret = string.IsNullOrWhiteSpace(profile.Secret)
                ? existing?.ProtectedSecret ?? string.Empty
                : _protector.Protect(profile.Secret);
            var chatSecret = string.IsNullOrWhiteSpace(profile.ChatSecret)
                ? existing?.ProtectedChatSecret ?? string.Empty
                : _protector.Protect(profile.ChatSecret);
            var replacement = new StoredProviderProfile
            {
                Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id,
                Name = string.IsNullOrWhiteSpace(profile.Name) ? "Streaming profile" : profile.Name.Trim(),
                Provider = profile.Provider,
                AuthenticationMode = profile.Provider == PublicationStreamProvider.Twitch
                    ? profile.AuthenticationMode
                    : StreamingProviderAuthenticationMode.Manual,
                Transport = profile.Transport,
                Endpoint = profile.Endpoint?.Trim() ?? string.Empty,
                ChannelId = profile.ChannelId?.Trim() ?? string.Empty,
                AccountName = profile.AccountName?.Trim() ?? string.Empty,
                ProtectedSecret = secret,
                ChatEnabled = profile.ChatEnabled,
                ProtectedChatSecret = chatSecret,
                OAuthClientId = normalizedClientId,
                ProtectedOAuthAccessToken = retainOAuthSession ? existing?.ProtectedOAuthAccessToken ?? string.Empty : string.Empty,
                ProtectedOAuthRefreshToken = retainOAuthSession ? existing?.ProtectedOAuthRefreshToken ?? string.Empty : string.Empty,
                OAuthAccessTokenExpiresUtc = retainOAuthSession ? existing?.OAuthAccessTokenExpiresUtc : null,
                OAuthLastValidatedUtc = retainOAuthSession ? existing?.OAuthLastValidatedUtc : null,
                OAuthScopes = retainOAuthSession
                    ? existing?.OAuthScopes ?? profile.OAuthScopes?.Trim() ?? string.Empty
                    : string.Empty,
                AutoSelectIngest = profile.AutoSelectIngest,
                IngestServerName = profile.IngestServerName?.Trim() ?? string.Empty,
                IngestLatencyMilliseconds = profile.IngestLatencyMilliseconds,
                IngestLastTestedUtc = profile.IngestLastTestedUtc,
                Enabled = profile.Enabled
            };
            if (existing is null) stored.Providers.Add(replacement);
            else stored.Providers[stored.Providers.IndexOf(existing)] = replacement;
            await SaveStoredAsync(stored, cancellationToken);
            return ToPublic(replacement);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Deletes provider async.
    /// </summary>
    public async Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            stored.Providers.RemoveAll(item => item.Id == id);
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Saves machine options async.
    /// </summary>
    public async Task SaveMachineOptionsAsync(StreamingMachineSettings settings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            stored.FfmpegPath = settings.FfmpegPath?.Trim() ?? string.Empty;
            stored.DefaultRecordingDirectory = settings.DefaultRecordingDirectory?.Trim() ?? string.Empty;
            stored.MediaHostPort = Math.Clamp(settings.MediaHostPort, 1024, 65535);
            stored.HardwareEncoder = settings.HardwareEncoder;
            stored.Devices = (settings.Devices ?? [])
                .Select(profile => new StreamingDeviceProfile
                {
                    Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id,
                    Name = string.IsNullOrWhiteSpace(profile.Name) ? profile.Kind.ToString() : profile.Name.Trim(),
                    Kind = profile.Kind,
                    DeviceId = profile.DeviceId?.Trim() ?? string.Empty,
                    AudioDeviceId = profile.AudioDeviceId?.Trim() ?? string.Empty,
                    ApplicationId = profile.ApplicationId?.Trim() ?? string.Empty,
                    WindowTitle = profile.WindowTitle?.Trim() ?? string.Empty,
                    CaptureBackend = profile.CaptureBackend,
                    NativeBackend = profile.NativeBackend?.Trim() ?? string.Empty,
                    UseDeviceTimestamps = profile.UseDeviceTimestamps
                })
                .GroupBy(profile => profile.Id)
                .Select(group => group.First())
                .ToList();
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Resolves secret async.
    /// </summary>
    public Task<string?> ResolveSecretAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        ResolveProtectedValueAsync(profileId, ProtectedValueKind.StreamSecret, cancellationToken);

    /// <summary>
    /// Resolves chat secret async.
    /// </summary>
    public Task<string?> ResolveChatSecretAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        ResolveProtectedValueAsync(profileId, ProtectedValueKind.ChatSecret, cancellationToken);

    internal async Task<StreamingOAuthCredentials?> ReadOAuthCredentialsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var profile = (await LoadStoredAsync(cancellationToken)).Providers.FirstOrDefault(item => item.Id == profileId);
            if (profile is null
                || string.IsNullOrWhiteSpace(profile.OAuthClientId)
                || string.IsNullOrWhiteSpace(profile.ProtectedOAuthAccessToken)) return null;
            try
            {
                return new StreamingOAuthCredentials
                {
                    ProfileId = profile.Id,
                    ClientId = profile.OAuthClientId,
                    AccessToken = _protector.Unprotect(profile.ProtectedOAuthAccessToken),
                    RefreshToken = string.IsNullOrWhiteSpace(profile.ProtectedOAuthRefreshToken)
                        ? string.Empty
                        : _protector.Unprotect(profile.ProtectedOAuthRefreshToken),
                    AccessTokenExpiresUtc = profile.OAuthAccessTokenExpiresUtc,
                    LastValidatedUtc = profile.OAuthLastValidatedUtc,
                    Scopes = profile.OAuthScopes,
                    AccountName = profile.AccountName,
                    ChannelId = profile.ChannelId
                };
            }
            catch
            {
                return null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<StreamingProviderProfile> SaveTwitchOAuthConnectionAsync(
        TwitchOAuthCredentialUpdate update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var profile = stored.Providers.FirstOrDefault(item => item.Id == update.ProfileId)
                ?? throw new InvalidOperationException("Save the Twitch provider profile before connecting it.");
            profile.Provider = PublicationStreamProvider.Twitch;
            profile.AuthenticationMode = StreamingProviderAuthenticationMode.OAuth;
            profile.Transport = PublicationStreamTransport.Rtmp;
            profile.OAuthClientId = update.ClientId.Trim();
            profile.ProtectedOAuthAccessToken = _protector.Protect(update.AccessToken);
            profile.ProtectedOAuthRefreshToken = string.IsNullOrWhiteSpace(update.RefreshToken)
                ? string.Empty
                : _protector.Protect(update.RefreshToken);
            profile.OAuthAccessTokenExpiresUtc = update.AccessTokenExpiresUtc;
            profile.OAuthLastValidatedUtc = update.LastValidatedUtc;
            profile.OAuthScopes = update.Scopes.Trim();
            profile.ChannelId = update.UserId.Trim();
            profile.AccountName = update.Login.Trim();
            profile.ProtectedSecret = _protector.Protect(update.StreamKey);
            profile.Endpoint = update.Endpoint.Trim();
            profile.IngestServerName = update.IngestServerName.Trim();
            profile.IngestLatencyMilliseconds = update.IngestLatencyMilliseconds;
            profile.IngestLastTestedUtc = update.IngestLastTestedUtc;
            await SaveStoredAsync(stored, cancellationToken);
            return ToPublic(profile);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task SaveOAuthTokensAsync(
        Guid profileId,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresUtc,
        DateTimeOffset lastValidatedUtc,
        string scopes,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
            if (profile is null) return;
            profile.ProtectedOAuthAccessToken = _protector.Protect(accessToken);
            if (!string.IsNullOrWhiteSpace(refreshToken)) profile.ProtectedOAuthRefreshToken = _protector.Protect(refreshToken);
            profile.OAuthAccessTokenExpiresUtc = accessTokenExpiresUtc;
            profile.OAuthLastValidatedUtc = lastValidatedUtc;
            profile.OAuthScopes = scopes.Trim();
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task MarkOAuthValidatedAsync(
        Guid profileId,
        DateTimeOffset accessTokenExpiresUtc,
        DateTimeOffset lastValidatedUtc,
        string scopes,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
            if (profile is null) return;
            profile.OAuthAccessTokenExpiresUtc = accessTokenExpiresUtc;
            profile.OAuthLastValidatedUtc = lastValidatedUtc;
            profile.OAuthScopes = scopes.Trim();
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task UpdateTwitchIngestAsync(
        Guid profileId,
        TwitchIngestCandidate candidate,
        DateTimeOffset testedUtc,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
            if (profile is null) return;
            profile.Transport = PublicationStreamTransport.Rtmp;
            profile.Endpoint = candidate.Endpoint.Trim();
            profile.IngestServerName = candidate.Name.Trim();
            profile.IngestLatencyMilliseconds = candidate.LatencyMilliseconds;
            profile.IngestLastTestedUtc = testedUtc;
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task ClearOAuthSessionAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
            if (profile is null) return;
            profile.AuthenticationMode = StreamingProviderAuthenticationMode.Manual;
            profile.ProtectedOAuthAccessToken = string.Empty;
            profile.ProtectedOAuthRefreshToken = string.Empty;
            profile.OAuthAccessTokenExpiresUtc = null;
            profile.OAuthLastValidatedUtc = null;
            profile.OAuthScopes = string.Empty;
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string?> ResolveProtectedValueAsync(
        Guid profileId,
        ProtectedValueKind kind,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var storedProfile = (await LoadStoredAsync(cancellationToken)).Providers.FirstOrDefault(item => item.Id == profileId);
            var protectedValue = kind switch
            {
                ProtectedValueKind.StreamSecret => storedProfile?.ProtectedSecret,
                ProtectedValueKind.ChatSecret => storedProfile?.ProtectedChatSecret,
                _ => null
            };
            if (string.IsNullOrWhiteSpace(protectedValue)) return null;
            try { return _protector.Unprotect(protectedValue); }
            catch { return null; }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<StreamingMachineSettings> LoadCoreAsync(CancellationToken cancellationToken)
    {
        var stored = await LoadStoredAsync(cancellationToken);
        return new StreamingMachineSettings
        {
            Providers = stored.Providers.Select(ToPublic).ToList(),
            Devices = stored.Devices ?? [],
            FfmpegPath = stored.FfmpegPath,
            DefaultRecordingDirectory = stored.DefaultRecordingDirectory,
            MediaHostPort = stored.MediaHostPort,
            HardwareEncoder = stored.HardwareEncoder
        };
    }

    private StreamingProviderProfile ToPublic(StoredProviderProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Provider = profile.Provider,
        AuthenticationMode = profile.AuthenticationMode,
        Transport = profile.Transport,
        Endpoint = profile.Endpoint,
        ChannelId = profile.ChannelId,
        AccountName = profile.AccountName,
        HasStoredSecret = !string.IsNullOrWhiteSpace(profile.ProtectedSecret),
        ChatEnabled = profile.ChatEnabled,
        HasStoredChatSecret = !string.IsNullOrWhiteSpace(profile.ProtectedChatSecret),
        OAuthClientId = profile.OAuthClientId,
        HasStoredOAuthSession = !string.IsNullOrWhiteSpace(profile.ProtectedOAuthAccessToken)
            && !string.IsNullOrWhiteSpace(profile.ProtectedOAuthRefreshToken),
        OAuthAccessTokenExpiresUtc = profile.OAuthAccessTokenExpiresUtc,
        OAuthLastValidatedUtc = profile.OAuthLastValidatedUtc,
        OAuthScopes = profile.OAuthScopes,
        AutoSelectIngest = profile.AutoSelectIngest,
        IngestServerName = profile.IngestServerName,
        IngestLatencyMilliseconds = profile.IngestLatencyMilliseconds,
        IngestLastTestedUtc = profile.IngestLastTestedUtc,
        Enabled = profile.Enabled
    };

    private async Task<StoredStreamingMachineSettings> LoadStoredAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath)) return new StoredStreamingMachineSettings();
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<StoredStreamingMachineSettings>(stream, _json, cancellationToken)
                ?? new StoredStreamingMachineSettings();
        }
        catch
        {
            var backup = _filePath + ".invalid-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Move(_filePath, backup, overwrite: true); } catch { }
            return new StoredStreamingMachineSettings();
        }
    }

    private async Task SaveStoredAsync(StoredStreamingMachineSettings settings, CancellationToken cancellationToken)
    {
        var temporary = _filePath + ".tmp";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, settings, _json, cancellationToken);
        File.Move(temporary, _filePath, overwrite: true);
    }

    private enum ProtectedValueKind
    {
        StreamSecret,
        ChatSecret
    }

    private sealed class StoredStreamingMachineSettings
    {
        /// <summary>
        /// Gets or sets providers.
        /// </summary>
        public List<StoredProviderProfile> Providers { get; set; } = [];
        /// <summary>
        /// Gets or sets devices.
        /// </summary>
        public List<StreamingDeviceProfile> Devices { get; set; } = [];
        /// <summary>
        /// Gets or sets FFmpeg path.
        /// </summary>
        public string FfmpegPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets default recording directory.
        /// </summary>
        public string DefaultRecordingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets media host port.
        /// </summary>
        public int MediaHostPort { get; set; } = 17847;
        /// <summary>
        /// Gets or sets hardware encoder.
        /// </summary>
        public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    }

    private sealed class StoredProviderProfile
    {
        /// <summary>
        /// Gets or sets the stable identifier.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets provider.
        /// </summary>
        public PublicationStreamProvider Provider { get; set; }
        /// <summary>
        /// Gets or sets authentication mode.
        /// </summary>
        public StreamingProviderAuthenticationMode AuthenticationMode { get; set; }
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
        /// Gets or sets protected secret.
        /// </summary>
        public string ProtectedSecret { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets chat enabled.
        /// </summary>
        public bool ChatEnabled { get; set; }
        /// <summary>
        /// Gets or sets protected chat secret.
        /// </summary>
        public string ProtectedChatSecret { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets OAuth client identifier.
        /// </summary>
        public string OAuthClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets protected OAuth access token.
        /// </summary>
        public string ProtectedOAuthAccessToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets protected OAuth refresh token.
        /// </summary>
        public string ProtectedOAuthRefreshToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets OAuth access token expires UTC.
        /// </summary>
        public DateTimeOffset? OAuthAccessTokenExpiresUtc { get; set; }
        /// <summary>
        /// Gets or sets OAuth last validated UTC.
        /// </summary>
        public DateTimeOffset? OAuthLastValidatedUtc { get; set; }
        /// <summary>
        /// Gets or sets OAuth scopes.
        /// </summary>
        public string OAuthScopes { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets auto select ingest.
        /// </summary>
        public bool AutoSelectIngest { get; set; } = true;
        /// <summary>
        /// Gets or sets ingest server name.
        /// </summary>
        public string IngestServerName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets ingest latency milliseconds.
        /// </summary>
        public double? IngestLatencyMilliseconds { get; set; }
        /// <summary>
        /// Gets or sets ingest last tested UTC.
        /// </summary>
        public DateTimeOffset? IngestLastTestedUtc { get; set; }
        /// <summary>
        /// Gets or sets whether the feature is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}

internal sealed class StreamingOAuthCredentials
{
    /// <summary>
    /// Gets or sets profile identifier.
    /// </summary>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets access token expires UTC.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets last validated UTC.
    /// </summary>
    public DateTimeOffset? LastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets scopes.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets account name.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets channel identifier.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;
}

internal sealed class TwitchOAuthCredentialUpdate
{
    /// <summary>
    /// Gets or sets profile identifier.
    /// </summary>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets access token expires UTC.
    /// </summary>
    public DateTimeOffset AccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets last validated UTC.
    /// </summary>
    public DateTimeOffset LastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets scopes.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets login.
    /// </summary>
    public string Login { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets stream key.
    /// </summary>
    public string StreamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets ingest server name.
    /// </summary>
    public string IngestServerName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets ingest latency milliseconds.
    /// </summary>
    public double? IngestLatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets ingest last tested UTC.
    /// </summary>
    public DateTimeOffset? IngestLastTestedUtc { get; set; }
}
