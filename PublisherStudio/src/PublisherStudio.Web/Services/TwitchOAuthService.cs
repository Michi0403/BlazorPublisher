using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class TwitchOAuthService
{
    private const string DeviceAuthorizationUrl = "https://id.twitch.tv/oauth2/device";
    private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
    private const string ValidateUrl = "https://id.twitch.tv/oauth2/validate";
    private const string RevokeUrl = "https://id.twitch.tv/oauth2/revoke";
    private const string StreamKeyUrl = "https://api.twitch.tv/helix/streams/key";
    private const string IngestUrl = "https://ingest.twitch.tv/ingests";
    private const string GlobalEndpoint = "rtmp://ingest.global-contribute.live-video.net/app/{streamKey}";
    private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan RefreshSafetyWindow = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StreamingProfileStore _profiles;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    public TwitchOAuthService(
        IHttpClientFactory httpClientFactory,
        StreamingProfileStore profiles,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _profiles = profiles;
        _configuration = configuration;
    }

    public string DefaultClientId =>
        (_configuration["Twitch:ClientId"]
            ?? Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_TWITCH_CLIENT_ID")
            ?? string.Empty).Trim();

    public string ResolveClientId(string? profileClientId) =>
        string.IsNullOrWhiteSpace(profileClientId) ? DefaultClientId : profileClientId.Trim();

    public async Task<TwitchDeviceAuthorization> StartDeviceAuthorizationAsync(
        string clientId,
        bool includeChat,
        CancellationToken cancellationToken = default)
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

    public async Task<TwitchOAuthConnectionResult> CompleteDeviceAuthorizationAsync(
        Guid profileId,
        TwitchDeviceAuthorization authorization,
        bool autoSelectIngest,
        string currentEndpoint,
        CancellationToken cancellationToken = default)
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

    public async Task<List<TwitchIngestCandidate>> TestIngestEndpointsAsync(CancellationToken cancellationToken = default)
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

    public async Task<StreamingProviderProfile?> ApplyIngestCandidateAsync(
        Guid profileId,
        TwitchIngestCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        await _profiles.UpdateTwitchIngestAsync(profileId, candidate, DateTimeOffset.UtcNow, cancellationToken);
        return (await _profiles.LoadAsync(cancellationToken)).Providers.FirstOrDefault(profile => profile.Id == profileId);
    }

    public Task<string?> EnsureValidAccessTokenAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        EnsureValidAccessTokenCoreAsync(profileId, forceValidation: false, cancellationToken: cancellationToken);

    public async Task<bool> ValidateProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        !string.IsNullOrWhiteSpace(await EnsureValidAccessTokenCoreAsync(profileId, forceValidation: true, cancellationToken: cancellationToken));

    public async Task DisconnectAsync(Guid profileId, CancellationToken cancellationToken = default)
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

    private async Task<string?> EnsureValidAccessTokenCoreAsync(
        Guid profileId,
        bool forceValidation,
        CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            var credentials = await _profiles.ReadOAuthCredentialsAsync(profileId, cancellationToken);
            if (credentials is null) return null;
            var now = DateTimeOffset.UtcNow;
            if (!forceValidation
                && credentials.LastValidatedUtc is { } lastValidated
                && now - lastValidated < ValidationInterval
                && credentials.AccessTokenExpiresUtc is { } expiresUtc
                && expiresUtc - now > RefreshSafetyWindow)
                return credentials.AccessToken;

            var validation = await ValidateTokenAsync(credentials.AccessToken, cancellationToken);
            if (validation is not null
                && string.Equals(validation.ClientId, credentials.ClientId, StringComparison.Ordinal)
                && validation.ExpiresIn > (int)RefreshSafetyWindow.TotalSeconds)
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

    private async Task<TwitchTokenResponse> PollForTokenAsync(
        TwitchDeviceAuthorization authorization,
        CancellationToken cancellationToken)
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

    private async Task<TwitchTokenResponse> RefreshTokenAsync(
        StreamingOAuthCredentials credentials,
        CancellationToken cancellationToken)
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

    private async Task<TwitchValidationResponse?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, ValidateUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
        if (!response.IsSuccessStatusCode) return null;
        return await ReadJsonAsync<TwitchValidationResponse>(response, cancellationToken);
    }

    private async Task<string> GetStreamKeyAsync(
        string clientId,
        string accessToken,
        string broadcasterId,
        CancellationToken cancellationToken)
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

    private async Task<List<TwitchIngestCandidate>> GetIngestCandidatesAsync(CancellationToken cancellationToken)
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

    private static async Task<double?> MeasureTcpLatencyAsync(string host, int port, CancellationToken cancellationToken)
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

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(TwitchOAuthService));
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
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

    private static async Task<string> ReadTwitchErrorAsync(
        HttpResponseMessage response,
        string? parsedMessage,
        CancellationToken cancellationToken)
    {
        var message = parsedMessage;
        if (string.IsNullOrWhiteSpace(message)) message = await ReadResponseTextAsync(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(message)) message = $"Twitch returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
        return NormalizeTwitchError(message);
    }

    private static async Task<string> ReadResponseTextAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadAsStringAsync(cancellationToken); }
        catch { return string.Empty; }
    }

    private static string NormalizeTwitchError(string message)
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

    private static string BuildScopes(bool includeChat) => includeChat
        ? "channel:read:stream_key chat:read chat:edit"
        : "channel:read:stream_key";

    private static TwitchIngestCandidate CreateGlobalCandidate() => new()
    {
        Name = "Twitch Global (automatic routing)",
        Endpoint = GlobalEndpoint,
        Host = "ingest.global-contribute.live-video.net",
        IsGlobal = true
    };

    private static string NormalizeEndpoint(string? endpoint)
    {
        var value = endpoint?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Replace("{stream_key}", "{streamKey}", StringComparison.OrdinalIgnoreCase);
        if (!value.Contains("{streamKey}", StringComparison.OrdinalIgnoreCase))
            value = value.TrimEnd('/') + "/{streamKey}";
        return value;
    }

    private static string TryReadHost(string? endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return uri.Host;
        return string.Empty;
    }

    private sealed class TwitchDeviceAuthorizationResponse
    {
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = string.Empty;
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = string.Empty;
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")] public string[] Scope { get; set; } = [];
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchValidationResponse
    {
        [JsonPropertyName("client_id")] public string ClientId { get; set; } = string.Empty;
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        [JsonPropertyName("scopes")] public string[] Scopes { get; set; } = [];
        [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    private sealed class TwitchStreamKeyResponse
    {
        [JsonPropertyName("data")] public List<TwitchStreamKeyItem> Data { get; set; } = [];
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    private sealed class TwitchStreamKeyItem
    {
        [JsonPropertyName("stream_key")] public string StreamKey { get; set; } = string.Empty;
    }

    private sealed class TwitchIngestResponse
    {
        [JsonPropertyName("ingests")] public List<TwitchIngestItem> Ingests { get; set; } = [];
    }

    private sealed class TwitchIngestItem
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("url_template")] public string UrlTemplate { get; set; } = string.Empty;
    }
}

public sealed class TwitchOAuthMaintenanceService(
    IServiceProvider services,
    ILogger<TwitchOAuthMaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<StreamingProfileStore>();
                var twitch = scope.ServiceProvider.GetRequiredService<TwitchOAuthService>();
                var profiles = (await store.LoadAsync(stoppingToken)).Providers
                    .Where(profile => profile.Provider == PublicationStreamProvider.Twitch
                        && profile.AuthenticationMode == StreamingProviderAuthenticationMode.OAuth
                        && profile.HasStoredOAuthSession)
                    .ToList();
                foreach (var profile in profiles)
                {
                    try { await twitch.ValidateProfileAsync(profile.Id, stoppingToken); }
                    catch (Exception exception) { logger.LogWarning(exception, "Twitch OAuth validation failed for profile {ProfileId}.", profile.Id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The Twitch OAuth maintenance cycle failed.");
            }

            try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
