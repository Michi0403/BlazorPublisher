using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Provides server-owned OpenAI-compatible chat without exposing credentials to publication files or exported JavaScript.</summary>
public interface IPublisherDirectAiService
{
    /// <summary>Reports whether the PublisherStudio host has an AI credential and model configured.</summary>
    /// <returns>Whether the hosted AI provider is configured.</returns>
    bool IsAvailable();

    /// <summary>Executes one bounded message through the host-configured OpenAI chat-completions endpoint.</summary>
    /// <param name="request">Public component request that never carries an API credential.</param>
    /// <param name="cancellationToken">Cancels the active provider request.</param>
    /// <returns>A correlated, human-readable provider answer.</returns>
    Task<PublisherAiChatResponse> ChatAsync(PublisherAiChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Owns the optional cloud-provider boundary independently of the LocalGPT 1-Wire transport.</summary>
/// <param name="httpClientFactory">Factory for short-lived client handles backed by pooled HTTP handlers.</param>
/// <param name="logger">Diagnostics without API keys or raw user prompts.</param>
public sealed class PublisherDirectAiService(
    IHttpClientFactory httpClientFactory,
    ILogger<PublisherDirectAiService> logger) : IPublisherDirectAiService
{
    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_OPENAI_API_KEY"))
                && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_OPENAI_MODEL"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not inspect the host's OpenAI provider readiness.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PublisherAiChatResponse> ChatAsync(PublisherAiChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var key = Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_OPENAI_API_KEY");
            var model = Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_OPENAI_MODEL");
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("The PublisherStudio host has no configured OpenAI API key or model.");
            var prompt = (request.Prompt ?? string.Empty).Trim();
            if (prompt.Length is 0 or > 32000)
                throw new ArgumentException("The AI prompt must contain between 1 and 32000 characters.", nameof(request));

            var endpointText = Environment.GetEnvironmentVariable("PUBLISHERSTUDIO_OPENAI_CHAT_COMPLETIONS_URL")
                ?? "https://api.openai.com/v1/chat/completions";
            if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp)))
                throw new InvalidOperationException("The configured OpenAI endpoint must use HTTPS (or HTTP loopback for a local compatible host).");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(120));
            using var client = httpClientFactory.CreateClient();
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            message.Content = JsonContent.Create(new
            {
                model,
                max_completion_tokens = Math.Clamp(request.MaxOutputTokens, 256, 8192),
                messages = string.IsNullOrWhiteSpace(request.SystemPrompt)
                    ? new[] { new { role = "user", content = prompt } }
                    : new[] { new { role = "system", content = request.SystemPrompt[..Math.Min(request.SystemPrompt.Length, 4000)] }, new { role = "user", content = prompt } }
            });
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PublisherStudio host OpenAI request failed with HTTP {StatusCode}.", (int)response.StatusCode);
                throw new HttpRequestException($"The configured OpenAI provider returned HTTP {(int)response.StatusCode}.");
            }
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            if (!json.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("message", out var answer)
                || !answer.TryGetProperty("content", out var content))
                throw new InvalidDataException("The AI provider returned no usable chat message.");
            var text = content.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException("The AI provider returned an empty chat message.");
            return new PublisherAiChatResponse { Text = text, CorrelationId = Guid.NewGuid() };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio's host-owned AI chat request failed.");
            throw;
        }
    }
}
