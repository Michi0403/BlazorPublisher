using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.Profiles;

/// <summary>
/// Owns persistence and retrieval of streaming profile state, keeping storage-specific behavior behind a focused application abstraction.
/// </summary>
public sealed class StreamingProfileStore
{
    /// <summary>
    /// Stores the data protector dependency used by <see cref="StreamingProfileStore"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDataProtector _protector;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="StreamingProfileStore"/>.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>
    /// Stores the internal file path state used by <see cref="StreamingProfileStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _filePath;
    /// <summary>
    /// Stores the internal JSON state used by <see cref="StreamingProfileStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new <see cref="StreamingProfileStore"/> instance and captures the dependencies or initial state required by its streaming profile workflow.
    /// </summary>
    /// <param name="protectionProvider">Data protection provider dependency used by the streaming profile workflow to provide the corresponding application capability.</param>
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
    /// Performs load in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming machine settings produced by the operation.</returns>
    public async Task<StreamingMachineSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.LoadAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Persists provider in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profile">Profile value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming provider profile produced by the operation.</returns>
    public async Task<StreamingProviderProfile> SaveProviderAsync(StreamingProviderProfile profile, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(profile);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
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
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
                return ToPublic(replacement);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.SaveProviderAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Deletes provider in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
                stored.Providers.RemoveAll(item => item.Id == id);
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.DeleteProviderAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Persists machine options in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task SaveMachineOptionsAsync(StreamingMachineSettings settings, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
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
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.SaveMachineOptionsAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Resolves secret in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public Task<string?> ResolveSecretAsync(Guid profileId, CancellationToken cancellationToken = default) {
    try
    {
        return ResolveProtectedValueAsync(profileId, ProtectedValueKind.StreamSecret, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ResolveSecretAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Resolves chat secret in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public Task<string?> ResolveChatSecretAsync(Guid profileId, CancellationToken cancellationToken = default) {
    try
    {
        return ResolveProtectedValueAsync(profileId, ProtectedValueKind.ChatSecret, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ResolveChatSecretAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads o auth credentials in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming o auth credentials produced by the operation.</returns>
    internal async Task<StreamingOAuthCredentials?> ReadOAuthCredentialsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var profile = (await LoadStoredAsync(cancellationToken).ConfigureAwait(false)).Providers.FirstOrDefault(item => item.Id == profileId);
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ReadOAuthCredentialsAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Persists twitch o auth connection in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="update">Update value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming provider profile produced by the operation.</returns>
    internal async Task<StreamingProviderProfile> SaveTwitchOAuthConnectionAsync(
        TwitchOAuthCredentialUpdate update,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(update);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
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
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
                return ToPublic(profile);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.SaveTwitchOAuthConnectionAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Persists o auth tokens in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="accessToken">Access token value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="refreshToken">Refresh token value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="accessTokenExpiresUtc">Access token expires utc value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="lastValidatedUtc">Last validated utc value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="scopes">Scopes value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    internal async Task SaveOAuthTokensAsync(
        Guid profileId,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresUtc,
        DateTimeOffset lastValidatedUtc,
        string scopes,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
                var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
                if (profile is null) return;
                profile.ProtectedOAuthAccessToken = _protector.Protect(accessToken);
                if (!string.IsNullOrWhiteSpace(refreshToken)) profile.ProtectedOAuthRefreshToken = _protector.Protect(refreshToken);
                profile.OAuthAccessTokenExpiresUtc = accessTokenExpiresUtc;
                profile.OAuthLastValidatedUtc = lastValidatedUtc;
                profile.OAuthScopes = scopes.Trim();
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.SaveOAuthTokensAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs mark o auth validated in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="accessTokenExpiresUtc">Access token expires utc value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="lastValidatedUtc">Last validated utc value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="scopes">Scopes value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    internal async Task MarkOAuthValidatedAsync(
        Guid profileId,
        DateTimeOffset accessTokenExpiresUtc,
        DateTimeOffset lastValidatedUtc,
        string scopes,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
                var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
                if (profile is null) return;
                profile.OAuthAccessTokenExpiresUtc = accessTokenExpiresUtc;
                profile.OAuthLastValidatedUtc = lastValidatedUtc;
                profile.OAuthScopes = scopes.Trim();
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.MarkOAuthValidatedAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Updates twitch ingest in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="candidate">Candidate value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="testedUtc">Tested utc value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    internal async Task UpdateTwitchIngestAsync(
        Guid profileId,
        TwitchIngestCandidate candidate,
        DateTimeOffset testedUtc,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
                var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
                if (profile is null) return;
                profile.Transport = PublicationStreamTransport.Rtmp;
                profile.Endpoint = candidate.Endpoint.Trim();
                profile.IngestServerName = candidate.Name.Trim();
                profile.IngestLatencyMilliseconds = candidate.LatencyMilliseconds;
                profile.IngestLastTestedUtc = testedUtc;
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.UpdateTwitchIngestAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs clear o auth session in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    internal async Task ClearOAuthSessionAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
                var profile = stored.Providers.FirstOrDefault(item => item.Id == profileId);
                if (profile is null) return;
                profile.AuthenticationMode = StreamingProviderAuthenticationMode.Manual;
                profile.ProtectedOAuthAccessToken = string.Empty;
                profile.ProtectedOAuthRefreshToken = string.Empty;
                profile.OAuthAccessTokenExpiresUtc = null;
                profile.OAuthLastValidatedUtc = null;
                profile.OAuthScopes = string.Empty;
                await SaveStoredAsync(stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ClearOAuthSessionAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Resolves protected value in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="kind">Kind value supplied to the streaming profile operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string?> ResolveProtectedValueAsync(
        Guid profileId,
        ProtectedValueKind kind,
        CancellationToken cancellationToken)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var storedProfile = (await LoadStoredAsync(cancellationToken).ConfigureAwait(false)).Providers.FirstOrDefault(item => item.Id == profileId);
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ResolveProtectedValueAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Loads core in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming machine settings produced by the operation.</returns>
    private async Task<StreamingMachineSettings> LoadCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.LoadCoreAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs to public in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="profile">Profile value supplied to the streaming profile operation and used when producing its result.</param>
    /// <returns>The streaming provider profile produced by the operation.</returns>
    private StreamingProviderProfile ToPublic(StoredProviderProfile profile) {
    try
    {
        return new()
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
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.ToPublic failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Loads stored in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The stored streaming machine settings produced by the operation.</returns>
    private async Task<StoredStreamingMachineSettings> LoadStoredAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (!File.Exists(_filePath)) return new StoredStreamingMachineSettings();
            try
            {
                var stream = File.OpenRead(_filePath);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<StoredStreamingMachineSettings>(stream, _json, cancellationToken).ConfigureAwait(false)
                    ?? new StoredStreamingMachineSettings();
            }
            catch
            {
                var backup = _filePath + ".invalid-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
                try { File.Move(_filePath, backup, overwrite: true); } catch { }
                return new StoredStreamingMachineSettings();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.LoadStoredAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Persists stored in the streaming profile persistence workflow while keeping storage-specific behavior contained within <see cref="StreamingProfileStore"/>.
    /// </summary>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveStoredAsync(StoredStreamingMachineSettings settings, CancellationToken cancellationToken)
    {
    try
    {
            var temporary = _filePath + ".tmp";
            var stream = File.Create(temporary);
            await using (stream.ConfigureAwait(false))
                await JsonSerializer.SerializeAsync(stream, settings, _json, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, _filePath, overwrite: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingProfileStore.SaveStoredAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Defines the supported protected value kind values used to select or describe behavior in the surrounding workflow.
    /// </summary>
    private enum ProtectedValueKind
    {
        StreamSecret,
        ChatSecret
    }

    /// <summary>
    /// Carries the configurable stored streaming machine settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    private sealed class StoredStreamingMachineSettings
    {
        /// <summary>
        /// Gets or sets the providers collection maintained or exposed by this stored streaming machine instance for downstream processing.
        /// </summary>
        /// <value>The providers value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public List<StoredProviderProfile> Providers { get; set; } = [];
        /// <summary>
        /// Gets or sets the devices collection maintained or exposed by this stored streaming machine instance for downstream processing.
        /// </summary>
        /// <value>The devices value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public List<StreamingDeviceProfile> Devices { get; set; } = [];
        /// <summary>
        /// Gets or sets the FFmpeg path used by this stored streaming machine instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The FFmpeg path value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public string FfmpegPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the default recording directory used by this stored streaming machine instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The default recording directory value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public string DefaultRecordingDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the media host port value that forms part of the stored streaming machine state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The media host port value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public int MediaHostPort { get; set; } = 17847;
        /// <summary>
        /// Gets or sets the hardware encoder value that forms part of the stored streaming machine state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The hardware encoder value exposed by <see cref="StoredStreamingMachineSettings"/>.</value>
        public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    }

    /// <summary>
    /// Represents a stored provider profile helper type nested within <see cref="StreamingProfileStore"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class StoredProviderProfile
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this stored provider profile instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="StoredProviderProfile"/>.</value>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the provider value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The provider value exposed by <see cref="StoredProviderProfile"/>.</value>
        public PublicationStreamProvider Provider { get; set; }
        /// <summary>
        /// Gets or sets the authentication mode value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The authentication mode value exposed by <see cref="StoredProviderProfile"/>.</value>
        public StreamingProviderAuthenticationMode AuthenticationMode { get; set; }
        /// <summary>
        /// Gets or sets the transport value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The transport value exposed by <see cref="StoredProviderProfile"/>.</value>
        public PublicationStreamTransport Transport { get; set; }
        /// <summary>
        /// Gets or sets the endpoint that identifies the network or application endpoint associated with this stored provider profile state.
        /// </summary>
        /// <value>The endpoint value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stable channel identifier used to identify or correlate this stored provider profile instance with related application state.
        /// </summary>
        /// <value>The channel identifier value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string ChannelId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the account name value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The account name value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string AccountName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the protected secret value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The protected secret value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string ProtectedSecret { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether chat enabled applies to the stored provider profile state.
        /// </summary>
        /// <value>The chat enabled value exposed by <see cref="StoredProviderProfile"/>.</value>
        public bool ChatEnabled { get; set; }
        /// <summary>
        /// Gets or sets the protected chat secret value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The protected chat secret value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string ProtectedChatSecret { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stable o auth client identifier used to identify or correlate this stored provider profile instance with related application state.
        /// </summary>
        /// <value>The o auth client identifier value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string OAuthClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the protected o auth access token value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The protected o auth access token value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string ProtectedOAuthAccessToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the protected o auth refresh token value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The protected o auth refresh token value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string ProtectedOAuthRefreshToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the o auth access token expires UTC associated with this stored provider profile state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The o auth access token expires UTC value exposed by <see cref="StoredProviderProfile"/>.</value>
        public DateTimeOffset? OAuthAccessTokenExpiresUtc { get; set; }
        /// <summary>
        /// Gets or sets the o auth last validated UTC associated with this stored provider profile state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The o auth last validated UTC value exposed by <see cref="StoredProviderProfile"/>.</value>
        public DateTimeOffset? OAuthLastValidatedUtc { get; set; }
        /// <summary>
        /// Gets or sets the o auth scopes value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The o auth scopes value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string OAuthScopes { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether auto select ingest applies to the stored provider profile state.
        /// </summary>
        /// <value>The auto select ingest value exposed by <see cref="StoredProviderProfile"/>.</value>
        public bool AutoSelectIngest { get; set; } = true;
        /// <summary>
        /// Gets or sets the ingest server name value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The ingest server name value exposed by <see cref="StoredProviderProfile"/>.</value>
        public string IngestServerName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the ingest latency milliseconds value that forms part of the stored provider profile state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The ingest latency milliseconds value exposed by <see cref="StoredProviderProfile"/>.</value>
        public double? IngestLatencyMilliseconds { get; set; }
        /// <summary>
        /// Gets or sets the ingest last tested UTC associated with this stored provider profile state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The ingest last tested UTC value exposed by <see cref="StoredProviderProfile"/>.</value>
        public DateTimeOffset? IngestLastTestedUtc { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the option is enabled applies to the stored provider profile state.
        /// </summary>
        /// <value>The enabled value exposed by <see cref="StoredProviderProfile"/>.</value>
        public bool Enabled { get; set; } = true;
    }
}

/// <summary>
/// Represents a streaming o auth credentials application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class StreamingOAuthCredentials
{
    /// <summary>
    /// Gets or sets the stable profile identifier used to identify or correlate this streaming o auth credentials instance with related application state.
    /// </summary>
    /// <value>The profile identifier value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets the stable client identifier used to identify or correlate this streaming o auth credentials instance with related application state.
    /// </summary>
    /// <value>The client identifier value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the access token value that forms part of the streaming o auth credentials state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access token value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the refresh token value that forms part of the streaming o auth credentials state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The refresh token value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the access token expires UTC associated with this streaming o auth credentials state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The access token expires UTC value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public DateTimeOffset? AccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets the last validated UTC associated with this streaming o auth credentials state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last validated UTC value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public DateTimeOffset? LastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the scopes value that forms part of the streaming o auth credentials state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scopes value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the account name value that forms part of the streaming o auth credentials state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The account name value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable channel identifier used to identify or correlate this streaming o auth credentials instance with related application state.
    /// </summary>
    /// <value>The channel identifier value exposed by <see cref="StreamingOAuthCredentials"/>.</value>
    public string ChannelId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a twitch o auth credential update application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class TwitchOAuthCredentialUpdate
{
    /// <summary>
    /// Gets or sets the stable profile identifier used to identify or correlate this twitch o auth credential update instance with related application state.
    /// </summary>
    /// <value>The profile identifier value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public Guid ProfileId { get; set; }
    /// <summary>
    /// Gets or sets the stable client identifier used to identify or correlate this twitch o auth credential update instance with related application state.
    /// </summary>
    /// <value>The client identifier value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the access token value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access token value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the refresh token value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The refresh token value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the access token expires UTC associated with this twitch o auth credential update state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The access token expires UTC value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public DateTimeOffset AccessTokenExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets the last validated UTC associated with this twitch o auth credential update state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last validated UTC value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public DateTimeOffset LastValidatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the scopes value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scopes value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string Scopes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable user identifier used to identify or correlate this twitch o auth credential update instance with related application state.
    /// </summary>
    /// <value>The user identifier value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the login value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The login value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string Login { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable stream key used to identify or correlate this twitch o auth credential update instance with related application state.
    /// </summary>
    /// <value>The stream key value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string StreamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this twitch o auth credential update state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ingest server name value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest server name value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public string IngestServerName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ingest latency milliseconds value that forms part of the twitch o auth credential update state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ingest latency milliseconds value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public double? IngestLatencyMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the ingest last tested UTC associated with this twitch o auth credential update state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The ingest last tested UTC value exposed by <see cref="TwitchOAuthCredentialUpdate"/>.</value>
    public DateTimeOffset? IngestLastTestedUtc { get; set; }
}
