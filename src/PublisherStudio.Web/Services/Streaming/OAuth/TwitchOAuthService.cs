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
/// Provides twitch OAuth service operations.
/// </summary>
public sealed class TwitchOAuthService
{
    private const string DeviceAuthorizationUrl = "https://id.twitch.tv/oauth2/device";
    private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
    private const string ValidateUrl = "https://id.twitch.tv/oauth2/validate";
    private const string RevokeUrl = "https://id.twitch.tv/oauth2/revoke";
    private const string StreamKeyUrl = "https://api.twitch.tv/helix/streams/key";
    private const string IngestUrl = "https://ingest.twitch.tv/ingests";
    private const string GlobalEndpoint = "rtmp://ingest.global-contribute.live-video.net/app/{streamKey}";
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPublisherRuntimePolicyDataService _runtimePolicy;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StreamingProfileStore _profiles;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    /// <summary>
    /// Runs the twitch OAuth service operation.
    /// </summary>
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
    /// Gets default client identifier.
    /// </summary>
    public string DefaultClientId =>
        (_configuration["Twitch:ClientId"]
            ?? Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_TWITCH_CLIENT_ID")
            ?? string.Empty).Trim();

    /// <summary>
    /// Resolves client identifier.
    /// </summary>
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
    /// Starts device authorization async.
    /// </summary>
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
            using var response = await SendAsync(request, cancellationToken);
            var payload = await ReadJsonAsync<TwitchDeviceAuthorizationResponse>(response, cancellationToken);
            if (!response.IsSuccessStatusCode || payload is null || string.IsNullOrWhiteSpace(payload.DeviceCode))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken));

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
    /// Runs the complete device authorization async operation.
    /// </summary>
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
            var token = await PollForTokenAsync(authorization, cancellationToken);
            var validation = await ValidateTokenAsync(token.AccessToken, cancellationToken)
                ?? throw new InvalidOperationException("Twitch returned an access token that could not be validated.");
            if (!string.Equals(validation.ClientId, authorization.ClientId, StringComparison.Ordinal))
                throw new InvalidOperationException("Twitch returned a token for a different Client ID.");
            if (string.IsNullOrWhiteSpace(validation.UserId) || string.IsNullOrWhiteSpace(validation.Login))
                throw new InvalidOperationException("Twitch did not return a broadcaster identity for this authorization.");

            var streamKey = await GetStreamKeyAsync(
                authorization.ClientId,
                token.AccessToken,
                validation.UserId,
                cancellationToken);

            List<TwitchIngestCandidate> candidates = [];
            TwitchIngestCandidate selected;
            if (autoSelectIngest)
            {
                candidates = await TestIngestEndpointsAsync(cancellationToken);
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
            }, cancellationToken);

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
    /// Runs the test ingest endpoints async operation.
    /// </summary>
    public async Task<List<TwitchIngestCandidate>> TestIngestEndpointsAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var candidates = await GetIngestCandidatesAsync(cancellationToken);
            using var concurrency = new SemaphoreSlim(8, 8);
            var tasks = candidates.Select(async candidate =>
            {
                await concurrency.WaitAsync(cancellationToken);
                try
                {
                    var samples = new List<double>(2);
                    for (var attempt = 0; attempt < 2; attempt++)
                    {
                        var sample = await MeasureTcpLatencyAsync(candidate.Host, 1935, cancellationToken);
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
            var tested = await Task.WhenAll(tasks);
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
    /// Applies ingest candidate async.
    /// </summary>
    public async Task<StreamingProviderProfile?> ApplyIngestCandidateAsync(
        Guid profileId,
        TwitchIngestCandidate candidate,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(candidate);
            await _profiles.UpdateTwitchIngestAsync(profileId, candidate, DateTimeOffset.UtcNow, cancellationToken);
            return (await _profiles.LoadAsync(cancellationToken)).Providers.FirstOrDefault(profile => profile.Id == profileId);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ApplyIngestCandidateAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Ensures valid access token async.
    /// </summary>
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
    /// Validates profile async.
    /// </summary>
    public async Task<bool> ValidateProfileAsync(Guid profileId, CancellationToken cancellationToken = default) {
    try
    {
        return !string.IsNullOrWhiteSpace(await EnsureValidAccessTokenCoreAsync(profileId, forceValidation: true, cancellationToken: cancellationToken));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ValidateProfileAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the disconnect async operation.
    /// </summary>
    public async Task DisconnectAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
    try
    {
            var credentials = await _profiles.ReadOAuthCredentialsAsync(profileId, cancellationToken);
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
                    using var response = await SendAsync(request, cancellationToken);
                }
                catch when (!cancellationToken.IsCancellationRequested)
                {
                    // Local disconnect must still work if Twitch is temporarily unreachable.
                }
            }
            await _profiles.ClearOAuthSessionAsync(profileId, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.DisconnectAsync failed: {__serviceMethodException}");
        throw;
    }
}

    private async Task<string?> EnsureValidAccessTokenCoreAsync(
        Guid profileId,
        bool forceValidation,
        CancellationToken cancellationToken)
    {
    try
    {
            await _tokenGate.WaitAsync(cancellationToken);
            try
            {
                var credentials = await _profiles.ReadOAuthCredentialsAsync(profileId, cancellationToken);
                if (credentials is null) return null;
                var now = DateTimeOffset.UtcNow;
                if (!forceValidation
                    && credentials.LastValidatedUtc is { } lastValidated
                    && now - lastValidated < _runtimePolicy.TwitchValidationInterval
                    && credentials.AccessTokenExpiresUtc is { } expiresUtc
                    && expiresUtc - now > _runtimePolicy.TwitchRefreshSafetyWindow)
                    return credentials.AccessToken;

                var validation = await ValidateTokenAsync(credentials.AccessToken, cancellationToken);
                if (validation is not null
                    && string.Equals(validation.ClientId, credentials.ClientId, StringComparison.Ordinal)
                    && validation.ExpiresIn > (int)_runtimePolicy.TwitchRefreshSafetyWindow.TotalSeconds)
                {
                    await _profiles.MarkOAuthValidatedAsync(
                        profileId,
                        now.AddSeconds(validation.ExpiresIn),
                        now,
                        string.Join(' ', validation.Scopes),
                        cancellationToken);
                    return credentials.AccessToken;
                }

                if (string.IsNullOrWhiteSpace(credentials.RefreshToken)) return null;
                var refreshed = await RefreshTokenAsync(credentials, cancellationToken);
                var refreshedValidation = await ValidateTokenAsync(refreshed.AccessToken, cancellationToken);
                if (refreshedValidation is null
                    || !string.Equals(refreshedValidation.ClientId, credentials.ClientId, StringComparison.Ordinal)) return null;

                await _profiles.SaveOAuthTokensAsync(
                    profileId,
                    refreshed.AccessToken,
                    refreshed.RefreshToken,
                    now.AddSeconds(Math.Max(60, refreshedValidation.ExpiresIn)),
                    now,
                    string.Join(' ', refreshedValidation.Scopes.Length > 0 ? refreshedValidation.Scopes : refreshed.Scope),
                    cancellationToken);
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

    private async Task<TwitchTokenResponse> PollForTokenAsync(
        TwitchDeviceAuthorization authorization,
        CancellationToken cancellationToken)
    {
    try
    {
            var interval = TimeSpan.FromSeconds(Math.Clamp(authorization.PollIntervalSeconds, 3, 30));
            while (DateTimeOffset.UtcNow < authorization.ExpiresUtc)
            {
                await Task.Delay(interval, cancellationToken);
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
                using var response = await SendAsync(request, cancellationToken);
                var payload = await ReadJsonAsync<TwitchTokenResponse>(response, cancellationToken);
                if (response.IsSuccessStatusCode && payload is not null && !string.IsNullOrWhiteSpace(payload.AccessToken)) return payload;

                var message = payload?.Message ?? await ReadResponseTextAsync(response, cancellationToken);
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
            using var response = await SendAsync(request, cancellationToken);
            var payload = await ReadJsonAsync<TwitchTokenResponse>(response, cancellationToken);
            if (!response.IsSuccessStatusCode || payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken));
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

    private async Task<TwitchValidationResponse?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(accessToken)) return null;
            using var request = new HttpRequestMessage(HttpMethod.Get, ValidateUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
            using var response = await SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
            if (!response.IsSuccessStatusCode) return null;
            return await ReadJsonAsync<TwitchValidationResponse>(response, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ValidateTokenAsync failed: {__serviceMethodException}");
        throw;
    }
}

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
            using var response = await SendAsync(request, cancellationToken);
            var payload = await ReadJsonAsync<TwitchStreamKeyResponse>(response, cancellationToken);
            var streamKey = payload?.Data.FirstOrDefault()?.StreamKey;
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(streamKey))
                throw new InvalidOperationException(await ReadTwitchErrorAsync(response, payload?.Message, cancellationToken));
            return streamKey;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.GetStreamKeyAsync failed: {__serviceMethodException}");
        throw;
    }
}

    private async Task<List<TwitchIngestCandidate>> GetIngestCandidatesAsync(CancellationToken cancellationToken)
    {
    try
    {
            var candidates = new List<TwitchIngestCandidate> { CreateGlobalCandidate() };
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, IngestUrl);
                using var response = await SendAsync(request, cancellationToken);
                var payload = await ReadJsonAsync<TwitchIngestResponse>(response, cancellationToken);
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
                await client.ConnectAsync(host, port, timeout.Token);
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

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
    try
    {
            var client = _httpClientFactory.CreateClient(nameof(TwitchOAuthService));
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.SendAsync failed: {__serviceMethodException}");
        throw;
    }
}

    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task<string> ReadTwitchErrorAsync(
        HttpResponseMessage response,
        string? parsedMessage,
        CancellationToken cancellationToken)
    {
    try
    {
            var message = parsedMessage;
            if (string.IsNullOrWhiteSpace(message)) message = await ReadResponseTextAsync(response, cancellationToken);
            if (string.IsNullOrWhiteSpace(message)) message = $"Twitch returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
            return NormalizeTwitchError(message);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ReadTwitchErrorAsync failed: {__serviceMethodException}");
        throw;
    }
}

    private async Task<string> ReadResponseTextAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
    try
    {
            try { return await response.Content.ReadAsStringAsync(cancellationToken); }
            catch { return string.Empty; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method TwitchOAuthService.ReadResponseTextAsync failed: {__serviceMethodException}");
        throw;
    }
}

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

    private sealed class TwitchDeviceAuthorizationResponse
    {
        /// <summary>
        /// Gets or sets device code.
        /// </summary>
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets user code.
        /// </summary>
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets verification URI.
        /// </summary>
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets expires in.
        /// </summary>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        /// <summary>
        /// Gets or sets interval.
        /// </summary>
        [JsonPropertyName("interval")] public int Interval { get; set; }
        /// <summary>
        /// Gets or sets message.
        /// </summary>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchTokenResponse
    {
        /// <summary>
        /// Gets or sets access token.
        /// </summary>
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets refresh token.
        /// </summary>
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets expires in.
        /// </summary>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        /// <summary>
        /// Gets or sets scope.
        /// </summary>
        [JsonPropertyName("scope")] public string[] Scope { get; set; } = [];
        /// <summary>
        /// Gets or sets token type.
        /// </summary>
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets message.
        /// </summary>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchValidationResponse
    {
        /// <summary>
        /// Gets or sets client identifier.
        /// </summary>
        [JsonPropertyName("client_id")] public string ClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets login.
        /// </summary>
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets scopes.
        /// </summary>
        [JsonPropertyName("scopes")] public string[] Scopes { get; set; } = [];
        /// <summary>
        /// Gets or sets user identifier.
        /// </summary>
        [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets expires in.
        /// </summary>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    private sealed class TwitchStreamKeyResponse
    {
        /// <summary>
        /// Gets or sets data.
        /// </summary>
        [JsonPropertyName("data")] public List<TwitchStreamKeyItem> Data { get; set; } = [];
        /// <summary>
        /// Gets or sets message.
        /// </summary>
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchStreamKeyItem
    {
        /// <summary>
        /// Gets or sets stream key.
        /// </summary>
        [JsonPropertyName("stream_key")] public string StreamKey { get; set; } = string.Empty;
    }

    private sealed class TwitchIngestResponse
    {
        /// <summary>
        /// Gets or sets ingests.
        /// </summary>
        [JsonPropertyName("ingests")] public List<TwitchIngestItem> Ingests { get; set; } = [];
    }

    private sealed class TwitchIngestItem
    {
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets URL template.
        /// </summary>
        [JsonPropertyName("url_template")] public string UrlTemplate { get; set; } = string.Empty;
    }
}

