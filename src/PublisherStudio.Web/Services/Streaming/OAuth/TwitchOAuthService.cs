using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.Streaming.OAuth;

/// <summary>
/// Coordinates twitch o auth behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class TwitchOAuthService
{
    /// <summary>
    /// Defines the device authorization URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string DeviceAuthorizationUrl = "https://id.twitch.tv/oauth2/device";
    /// <summary>
    /// Defines the token URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
    /// <summary>
    /// Defines the validate URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string ValidateUrl = "https://id.twitch.tv/oauth2/validate";
    /// <summary>
    /// Defines the revoke URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string RevokeUrl = "https://id.twitch.tv/oauth2/revoke";
    /// <summary>
    /// Defines the stream key URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string StreamKeyUrl = "https://api.twitch.tv/helix/streams/key";
    /// <summary>
    /// Defines the ingest URL constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string IngestUrl = "https://ingest.twitch.tv/ingests";
    /// <summary>
    /// Defines the global endpoint constant used by <see cref="TwitchOAuthService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string GlobalEndpoint = "rtmp://ingest.global-contribute.live-video.net/app/{streamKey}";
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="TwitchOAuthService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Stores the publisher runtime policy data service dependency used by <see cref="TwitchOAuthService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherRuntimePolicyDataService _runtimePolicy;
    /// <summary>
    /// Stores the HTTP client factory dependency used by <see cref="TwitchOAuthService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;
    /// <summary>
    /// Stores the streaming profile store dependency used by <see cref="TwitchOAuthService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly StreamingProfileStore _profiles;
    /// <summary>
    /// Stores the configuration dependency used by <see cref="TwitchOAuthService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IConfiguration _configuration;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to token gate state owned by <see cref="TwitchOAuthService"/>.
    /// </summary>
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    /// <summary>
    /// Initializes a new <see cref="TwitchOAuthService"/> instance and captures the dependencies or initial state required by its twitch o auth workflow.
    /// </summary>
    /// <param name="httpClientFactory">Http client factory dependency used by the twitch o auth workflow to provide the corresponding application capability.</param>
    /// <param name="profiles">Streaming profile store dependency used by the twitch o auth workflow to provide the corresponding application capability.</param>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the twitch o auth workflow to provide the corresponding application capability.</param>
    public TwitchOAuthService(
        IHttpClientFactory httpClientFactory,
        StreamingProfileStore profiles,
        IConfiguration configuration,
        IPublisherRuntimePolicyDataService runtimePolicy)
    {
        _runtimePolicy = runtimePolicy;
        _httpClientFactory = httpClientFactory;
        _profiles = profiles;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the stable default client identifier used to identify or correlate this twitch o auth instance with related application state.
    /// </summary>
    /// <value>The default client identifier value exposed by <see cref="TwitchOAuthService"/>.</value>
    public string DefaultClientId =>
        (_configuration["Twitch:ClientId"]
            ?? Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_TWITCH_CLIENT_ID")
            ?? string.Empty).Trim();

    /// <summary>
    /// Resolves client identifier as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileClientId">Identifier of the profile client to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ResolveClientId(string? profileClientId) {
    try
    {
        return string.IsNullOrWhiteSpace(profileClientId) ? DefaultClientId : profileClientId.Trim();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ResolveClientId failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Starts device authorization as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="clientId">Identifier of the client to use for this operation.</param>
    /// <param name="includeChat">Value indicating whether include chat should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The twitch device authorization produced by the operation.</returns>
    public async Task<TwitchDeviceAuthorization> StartDeviceAuthorizationAsync(
        string clientId,
        bool includeChat,
        CancellationToken cancellationToken = default)
    {
    try
    {
            clientId = ResolveClientId(clientId);
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("A Twitch application Client ID is required. Register PublisherStudio as a public Twitch application, then enter its Client ID.");

            var scopes = BuildScopes(includeChat);
            using var request = new HttpRequestMessage(HttpMethod.Post, DeviceAuthorizationUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["scopes"] = scopes
                })
            };
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await ReadJsonAsync<TwitchDeviceAuthorizationResponse>(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || payload is null || string.IsNullOrWhiteSpace(payload.DeviceCode))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken).ConfigureAwait(false));

            var expiresIn = Math.Clamp(payload.ExpiresIn, 60, 3600);
            return new TwitchDeviceAuthorization
            {
                DeviceCode = payload.DeviceCode,
                UserCode = payload.UserCode,
                VerificationUri = payload.VerificationUri,
                ExpiresInSeconds = expiresIn,
                PollIntervalSeconds = Math.Clamp(payload.Interval, 3, 30),
                ClientId = clientId,
                Scopes = scopes,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.StartDeviceAuthorizationAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Completes device authorization as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="authorization">Authorization value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="autoSelectIngest">Value indicating whether auto select ingest should apply to this operation.</param>
    /// <param name="currentEndpoint">Current endpoint value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The twitch o auth connection result produced by the operation.</returns>
    public async Task<TwitchOAuthConnectionResult> CompleteDeviceAuthorizationAsync(
        Guid profileId,
        TwitchDeviceAuthorization authorization,
        bool autoSelectIngest,
        string currentEndpoint,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(authorization);
            var token = await PollForTokenAsync(authorization, cancellationToken).ConfigureAwait(false);
            var validation = await ValidateTokenAsync(token.AccessToken, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Twitch returned an access token that could not be validated.");
            if (!string.Equals(validation.ClientId, authorization.ClientId, StringComparison.Ordinal))
                throw new InvalidOperationException("Twitch returned a token for a different Client ID.");
            if (string.IsNullOrWhiteSpace(validation.UserId) || string.IsNullOrWhiteSpace(validation.Login))
                throw new InvalidOperationException("Twitch did not return a broadcaster identity for this authorization.");

            var streamKey = await GetStreamKeyAsync(
                authorization.ClientId,
                token.AccessToken,
                validation.UserId,
                cancellationToken).ConfigureAwait(false);

            List<TwitchIngestCandidate> candidates = [];
            TwitchIngestCandidate selected;
            if (autoSelectIngest)
            {
                candidates = await TestIngestEndpointsAsync(cancellationToken).ConfigureAwait(false);
                selected = candidates.FirstOrDefault(candidate => candidate.Reachable)
                    ?? candidates.FirstOrDefault(candidate => candidate.IsGlobal)
                    ?? CreateGlobalCandidate();
            }
            else
            {
                selected = new TwitchIngestCandidate
                {
                    Name = "Manual Twitch endpoint",
                    Endpoint = NormalizeEndpoint(currentEndpoint),
                    Host = TryReadHost(currentEndpoint),
                    Reachable = true
                };
                if (string.IsNullOrWhiteSpace(selected.Endpoint)) selected = CreateGlobalCandidate();
            }

            var now = DateTimeOffset.UtcNow;
            var expiresUtc = now.AddSeconds(Math.Max(60, validation.ExpiresIn > 0 ? validation.ExpiresIn : token.ExpiresIn));
            var scopes = string.Join(' ', validation.Scopes.Length > 0 ? validation.Scopes : token.Scope);
            var profile = await _profiles.SaveTwitchOAuthConnectionAsync(new TwitchOAuthCredentialUpdate
            {
                ProfileId = profileId,
                ClientId = authorization.ClientId,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                AccessTokenExpiresUtc = expiresUtc,
                LastValidatedUtc = now,
                Scopes = scopes,
                UserId = validation.UserId,
                Login = validation.Login,
                StreamKey = streamKey,
                Endpoint = selected.Endpoint,
                IngestServerName = selected.Name,
                IngestLatencyMilliseconds = selected.LatencyMilliseconds,
                IngestLastTestedUtc = autoSelectIngest ? now : null
            }, cancellationToken).ConfigureAwait(false);

            return new TwitchOAuthConnectionResult
            {
                Success = true,
                Message = $"Connected Twitch account {validation.Login} and stored the stream key and refresh session securely.",
                Profile = profile,
                IngestCandidates = candidates
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.CompleteDeviceAuthorizationAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs test ingest endpoints as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<List<TwitchIngestCandidate>> TestIngestEndpointsAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var candidates = await GetIngestCandidatesAsync(cancellationToken).ConfigureAwait(false);
            using var concurrency = new SemaphoreSlim(8, 8);
            var tasks = candidates.Select(async candidate =>
            {
                await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var samples = new List<double>(2);
                    for (var attempt = 0; attempt < 2; attempt++)
                    {
                        var sample = await MeasureTcpLatencyAsync(candidate.Host, 1935, cancellationToken).ConfigureAwait(false);
                        if (sample is { } milliseconds) samples.Add(milliseconds);
                    }
                    candidate.Reachable = samples.Count > 0;
                    candidate.LatencyMilliseconds = samples.Count > 0 ? samples.Average() : null;
                    return candidate;
                }
                finally
                {
                    concurrency.Release();
                }
            });
            var tested = await Task.WhenAll(tasks).ConfigureAwait(false);
            return tested
                .OrderBy(candidate => candidate.Reachable ? 0 : 1)
                .ThenBy(candidate => candidate.LatencyMilliseconds ?? double.MaxValue)
                .ThenBy(candidate => candidate.IsGlobal ? 0 : 1)
                .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                .Take(16)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.TestIngestEndpointsAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Applies ingest candidate as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="candidate">Candidate value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The streaming provider profile produced by the operation.</returns>
    public async Task<StreamingProviderProfile?> ApplyIngestCandidateAsync(
        Guid profileId,
        TwitchIngestCandidate candidate,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(candidate);
            await _profiles.UpdateTwitchIngestAsync(profileId, candidate, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            return (await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false)).Providers.FirstOrDefault(profile => profile.Id == profileId);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ApplyIngestCandidateAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Ensures valid access token as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public Task<string?> EnsureValidAccessTokenAsync(Guid profileId, CancellationToken cancellationToken = default) {
    try
    {
        return EnsureValidAccessTokenCoreAsync(profileId, forceValidation: false, cancellationToken: cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.EnsureValidAccessTokenAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Validates profile as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> ValidateProfileAsync(Guid profileId, CancellationToken cancellationToken = default) {
    try
    {
        return !string.IsNullOrWhiteSpace(await EnsureValidAccessTokenCoreAsync(profileId, forceValidation: true, cancellationToken: cancellationToken).ConfigureAwait(false));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ValidateProfileAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs disconnect as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DisconnectAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
    try
    {
            var credentials = await _profiles.ReadOAuthCredentialsAsync(profileId, cancellationToken).ConfigureAwait(false);
            if (credentials is not null)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, RevokeUrl)
                    {
                        Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["client_id"] = credentials.ClientId,
                            ["token"] = credentials.AccessToken
                        })
                    };
                    using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch when (!cancellationToken.IsCancellationRequested)
                {
                    // Local disconnect must still work if Twitch is temporarily unreachable.
                }
            }
            await _profiles.ClearOAuthSessionAsync(profileId, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.DisconnectAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Ensures valid access token core as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profileId">Identifier of the profile to use for this operation.</param>
    /// <param name="forceValidation">Value indicating whether force validation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string?> EnsureValidAccessTokenCoreAsync(
        Guid profileId,
        bool forceValidation,
        CancellationToken cancellationToken)
    {
    try
    {
            await _tokenGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var credentials = await _profiles.ReadOAuthCredentialsAsync(profileId, cancellationToken).ConfigureAwait(false);
                if (credentials is null) return null;
                var now = DateTimeOffset.UtcNow;
                if (!forceValidation
                    && credentials.LastValidatedUtc is { } lastValidated
                    && now - lastValidated < _runtimePolicy.TwitchValidationInterval
                    && credentials.AccessTokenExpiresUtc is { } expiresUtc
                    && expiresUtc - now > _runtimePolicy.TwitchRefreshSafetyWindow)
                    return credentials.AccessToken;

                var validation = await ValidateTokenAsync(credentials.AccessToken, cancellationToken).ConfigureAwait(false);
                if (validation is not null
                    && string.Equals(validation.ClientId, credentials.ClientId, StringComparison.Ordinal)
                    && validation.ExpiresIn > (int)_runtimePolicy.TwitchRefreshSafetyWindow.TotalSeconds)
                {
                    await _profiles.MarkOAuthValidatedAsync(
                        profileId,
                        now.AddSeconds(validation.ExpiresIn),
                        now,
                        string.Join(' ', validation.Scopes),
                        cancellationToken).ConfigureAwait(false);
                    return credentials.AccessToken;
                }

                if (string.IsNullOrWhiteSpace(credentials.RefreshToken)) return null;
                var refreshed = await RefreshTokenAsync(credentials, cancellationToken).ConfigureAwait(false);
                var refreshedValidation = await ValidateTokenAsync(refreshed.AccessToken, cancellationToken).ConfigureAwait(false);
                if (refreshedValidation is null
                    || !string.Equals(refreshedValidation.ClientId, credentials.ClientId, StringComparison.Ordinal)) return null;

                await _profiles.SaveOAuthTokensAsync(
                    profileId,
                    refreshed.AccessToken,
                    refreshed.RefreshToken,
                    now.AddSeconds(Math.Max(60, refreshedValidation.ExpiresIn)),
                    now,
                    string.Join(' ', refreshedValidation.Scopes.Length > 0 ? refreshedValidation.Scopes : refreshed.Scope),
                    cancellationToken).ConfigureAwait(false);
                return refreshed.AccessToken;
            }
            finally
            {
                _tokenGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.EnsureValidAccessTokenCoreAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs poll for token as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="authorization">Authorization value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The twitch token response produced by the operation.</returns>
    private async Task<TwitchTokenResponse> PollForTokenAsync(
        TwitchDeviceAuthorization authorization,
        CancellationToken cancellationToken)
    {
    try
    {
            var interval = TimeSpan.FromSeconds(Math.Clamp(authorization.PollIntervalSeconds, 3, 30));
            while (DateTimeOffset.UtcNow < authorization.ExpiresUtc)
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = authorization.ClientId,
                        ["scopes"] = authorization.Scopes,
                        ["device_code"] = authorization.DeviceCode,
                        ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                    })
                };
                using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
                var payload = await ReadJsonAsync<TwitchTokenResponse>(response, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && payload is not null && !string.IsNullOrWhiteSpace(payload.AccessToken)) return payload;

                var message = payload?.Message ?? await ReadResponseTextAsync(response, cancellationToken).ConfigureAwait(false);
                if (message.Contains("authorization_pending", StringComparison.OrdinalIgnoreCase)) continue;
                if (message.Contains("slow_down", StringComparison.OrdinalIgnoreCase))
                {
                    interval += TimeSpan.FromSeconds(5);
                    continue;
                }
                throw new InvalidOperationException(NormalizeTwitchError(message));
            }
            throw new TimeoutException("The Twitch authorization window expired before sign-in was completed.");
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.PollForTokenAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Refreshes token as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="credentials">Credentials value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The twitch token response produced by the operation.</returns>
    private async Task<TwitchTokenResponse> RefreshTokenAsync(
        StreamingOAuthCredentials credentials,
        CancellationToken cancellationToken)
    {
    try
    {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = credentials.RefreshToken,
                    ["client_id"] = credentials.ClientId
                })
            };
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await ReadJsonAsync<TwitchTokenResponse>(response, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken).ConfigureAwait(false));
            if (string.IsNullOrWhiteSpace(payload.RefreshToken))
                throw new InvalidOperationException("Twitch did not return the required rotated refresh token. Reconnect the account.");
            return payload;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.RefreshTokenAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Validates token as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="accessToken">Access token value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The twitch validation response produced by the operation.</returns>
    private async Task<TwitchValidationResponse?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(accessToken)) return null;
            using var request = new HttpRequestMessage(HttpMethod.Get, ValidateUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
            if (!response.IsSuccessStatusCode) return null;
            return await ReadJsonAsync<TwitchValidationResponse>(response, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ValidateTokenAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Retrieves stream key as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="clientId">Identifier of the client to use for this operation.</param>
    /// <param name="accessToken">Access token value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="broadcasterId">Identifier of the broadcaster to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> GetStreamKeyAsync(
        string clientId,
        string accessToken,
        string broadcasterId,
        CancellationToken cancellationToken)
    {
    try
    {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{StreamKeyUrl}?broadcaster_id={Uri.EscapeDataString(broadcasterId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Client-Id", clientId);
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await ReadJsonAsync<TwitchStreamKeyResponse>(response, cancellationToken).ConfigureAwait(false);
            var streamKey = payload?.Data.FirstOrDefault()?.StreamKey;
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(streamKey))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken).ConfigureAwait(false));
            return streamKey;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.GetStreamKeyAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Retrieves ingest candidates as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<TwitchIngestCandidate>> GetIngestCandidatesAsync(CancellationToken cancellationToken)
    {
    try
    {
            var candidates = new List<TwitchIngestCandidate> { CreateGlobalCandidate() };
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, IngestUrl);
                using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
                var payload = await ReadJsonAsync<TwitchIngestResponse>(response, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && payload?.Ingests is { Count: > 0 })
                {
                    candidates.AddRange(payload.Ingests.Select(ingest =>
                    {
                        var endpoint = NormalizeEndpoint(ingest.UrlTemplate);
                        var host = TryReadHost(endpoint);
                        return new TwitchIngestCandidate
                        {
                            Name = string.IsNullOrWhiteSpace(ingest.Name) ? host : ingest.Name.Trim(),
                            Endpoint = endpoint,
                            Host = host,
                            IsGlobal = host.Contains("global-contribute", StringComparison.OrdinalIgnoreCase)
                        };
                    }));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                // Ingest discovery is an optimization, not a prerequisite for Twitch sign-in.
                // Keep Twitch Global available when the public ingest-list service is unreachable.
            }
            catch (OperationCanceledException)
            {
                // A named HttpClient timeout must not invalidate a completed OAuth connection.
            }

            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Host))
                .GroupBy(candidate => candidate.Host, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.GetIngestCandidatesAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs measure TCP latency as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="host">Host value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="port">Port value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The double produced by the operation.</returns>
    private async Task<double?> MeasureTcpLatencyAsync(string host, int port, CancellationToken cancellationToken)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(host)) return null;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(1500));
            using var client = new TcpClient();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
                stopwatch.Stop();
                return stopwatch.Elapsed.TotalMilliseconds;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.MeasureTcpLatencyAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs send as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
    try
    {
            var client = _httpClientFactory.CreateClient(nameof(TwitchOAuthService));
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.SendAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads JSON as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="TwitchOAuthService"/>.</typeparam>
    /// <param name="response">Response value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The t produced by the operation.</returns>
    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Reads twitch error as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="parsedMessage">Parsed message value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ReadTwitchErrorAsync(
        HttpResponseMessage response,
        string? parsedMessage,
        CancellationToken cancellationToken)
    {
    try
    {
            var message = parsedMessage;
            if (string.IsNullOrWhiteSpace(message)) message = await ReadResponseTextAsync(response, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(message)) message = $"Twitch returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
            return NormalizeTwitchError(message);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ReadTwitchErrorAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads response text as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ReadResponseTextAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
    try
    {
            try { return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
            catch { return string.Empty; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ReadResponseTextAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes twitch error as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeTwitchError(string message)
    {
    try
    {
            message = message.Trim();
            if (message.StartsWith('{'))
            {
                try
                {
                    using var json = JsonDocument.Parse(message);
                    if (json.RootElement.TryGetProperty("message", out var property)) message = property.GetString() ?? message;
                }
                catch (JsonException) { }
            }
            return string.IsNullOrWhiteSpace(message) ? "Twitch authorization failed." : message;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.NormalizeTwitchError failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Builds scopes as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeChat">Value indicating whether include chat should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildScopes(bool includeChat) {
    try
    {
        return includeChat
        ? "channel:read:stream_key chat:read chat:edit"
        : "channel:read:stream_key";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.BuildScopes failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Creates global candidate as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The twitch ingest candidate produced by the operation.</returns>
    private TwitchIngestCandidate CreateGlobalCandidate() {
    try
    {
        return new()
    {
        Name = "Twitch Global (automatic routing)",
        Endpoint = GlobalEndpoint,
        Host = "ingest.global-contribute.live-video.net",
        IsGlobal = true
    };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.CreateGlobalCandidate failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes endpoint as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeEndpoint(string? endpoint)
    {
    try
    {
            var value = endpoint?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value.Replace("{stream_key}", "{streamKey}", StringComparison.OrdinalIgnoreCase);
            if (!value.Contains("{streamKey}", StringComparison.OrdinalIgnoreCase))
                value = value.TrimEnd('/') + "/{streamKey}";
            return value;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.NormalizeEndpoint failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to read host as part of the twitch o auth service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the twitch o auth operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string TryReadHost(string? endpoint)
    {
    try
    {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return uri.Host;
            return string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.TryReadHost failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Represents the outcome of twitch device authorization, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class TwitchDeviceAuthorizationResponse
    {
        /// <summary>
        /// Gets or sets the device code value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The device code value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the user code value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The user code value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the verification URI that identifies the network or application endpoint associated with this twitch device authorization state.
        /// </summary>
        /// <value>The verification URI value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expires in value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The expires in value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        /// <summary>
        /// Gets or sets the interval duration used to control timing in the twitch device authorization workflow.
        /// </summary>
        /// <value>The interval value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("interval")] public int Interval { get; set; }
        /// <summary>
        /// Gets or sets the message value that forms part of the twitch device authorization state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The message value exposed by <see cref="TwitchDeviceAuthorizationResponse"/>.</value>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the outcome of twitch token, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class TwitchTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The access token value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the refresh token value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The refresh token value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expires in value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The expires in value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        /// <summary>
        /// Gets or sets the scope value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The scope value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("scope")] public string[] Scope { get; set; } = [];
        /// <summary>
        /// Gets or sets the token type value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The token type value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the message value that forms part of the twitch token state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The message value exposed by <see cref="TwitchTokenResponse"/>.</value>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the outcome of twitch validation, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class TwitchValidationResponse
    {
        /// <summary>
        /// Gets or sets the stable client identifier used to identify or correlate this twitch validation instance with related application state.
        /// </summary>
        /// <value>The client identifier value exposed by <see cref="TwitchValidationResponse"/>.</value>
        [JsonPropertyName("client_id")] public string ClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the login value that forms part of the twitch validation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The login value exposed by <see cref="TwitchValidationResponse"/>.</value>
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the scopes value that forms part of the twitch validation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The scopes value exposed by <see cref="TwitchValidationResponse"/>.</value>
        [JsonPropertyName("scopes")] public string[] Scopes { get; set; } = [];
        /// <summary>
        /// Gets or sets the stable user identifier used to identify or correlate this twitch validation instance with related application state.
        /// </summary>
        /// <value>The user identifier value exposed by <see cref="TwitchValidationResponse"/>.</value>
        [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expires in value that forms part of the twitch validation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The expires in value exposed by <see cref="TwitchValidationResponse"/>.</value>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    /// <summary>
    /// Represents the outcome of twitch stream key, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class TwitchStreamKeyResponse
    {
        /// <summary>
        /// Gets or sets the data collection maintained or exposed by this twitch stream key instance for downstream processing.
        /// </summary>
        /// <value>The data value exposed by <see cref="TwitchStreamKeyResponse"/>.</value>
        [JsonPropertyName("data")] public List<TwitchStreamKeyItem> Data { get; set; } = [];
        /// <summary>
        /// Gets or sets the message value that forms part of the twitch stream key state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The message value exposed by <see cref="TwitchStreamKeyResponse"/>.</value>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a twitch stream key item helper type nested within <see cref="TwitchOAuthService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class TwitchStreamKeyItem
    {
        /// <summary>
        /// Gets or sets the stable stream key used to identify or correlate this twitch stream key item instance with related application state.
        /// </summary>
        /// <value>The stream key value exposed by <see cref="TwitchStreamKeyItem"/>.</value>
        [JsonPropertyName("stream_key")] public string StreamKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the outcome of twitch ingest, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class TwitchIngestResponse
    {
        /// <summary>
        /// Gets or sets the ingests collection maintained or exposed by this twitch ingest instance for downstream processing.
        /// </summary>
        /// <value>The ingests value exposed by <see cref="TwitchIngestResponse"/>.</value>
        [JsonPropertyName("ingests")] public List<TwitchIngestItem> Ingests { get; set; } = [];
    }

    /// <summary>
    /// Represents a twitch ingest item helper type nested within <see cref="TwitchOAuthService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class TwitchIngestItem
    {
        /// <summary>
        /// Gets or sets the name value that forms part of the twitch ingest item state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="TwitchIngestItem"/>.</value>
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the URL template value that forms part of the twitch ingest item state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The URL template value exposed by <see cref="TwitchIngestItem"/>.</value>
        [JsonPropertyName("url_template")] public string UrlTemplate { get; set; } = string.Empty;
    }
}

