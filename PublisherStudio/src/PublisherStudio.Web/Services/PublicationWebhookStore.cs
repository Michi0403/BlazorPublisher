using System.Collections.Concurrent;

namespace PublisherStudio.Services;

/// <summary>
/// In-memory webhook mailbox for the loopback PublisherStudio process. Tokens are
/// registered from the currently open publications so unknown binding ids cannot
/// inject data into the application.
/// </summary>
public sealed class PublicationWebhookStore
{
    private readonly ConcurrentDictionary<Guid, string> _tokens = new();
    private readonly ConcurrentDictionary<Guid, WebhookPayload> _payloads = new();

    public void Register(Guid bindingId, string token)
    {
        if (bindingId == Guid.Empty || string.IsNullOrWhiteSpace(token)) return;
        _tokens[bindingId] = token.Trim();
    }

    public void Unregister(Guid bindingId)
    {
        _tokens.TryRemove(bindingId, out _);
        _payloads.TryRemove(bindingId, out _);
    }

    public bool TryPut(Guid bindingId, string token, string content, string contentType)
    {
        if (!_tokens.TryGetValue(bindingId, out var expected)
            || !string.Equals(expected, token, StringComparison.Ordinal)) return false;
        _payloads[bindingId] = new WebhookPayload(content ?? string.Empty, contentType ?? string.Empty, DateTimeOffset.UtcNow);
        return true;
    }

    public bool TryGet(Guid bindingId, out WebhookPayload payload)
        => _payloads.TryGetValue(bindingId, out payload!);

    public bool IsRegistered(Guid bindingId) => _tokens.ContainsKey(bindingId);
}

public sealed record WebhookPayload(string Content, string ContentType, DateTimeOffset ReceivedUtc);
