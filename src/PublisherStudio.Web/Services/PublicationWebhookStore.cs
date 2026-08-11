using System.Collections.Concurrent;

namespace PublisherStudio.Services;

/// <summary>
/// In-memory webhook mailbox for the loopback PublisherStudio process. Tokens are
/// registered from the currently open publications so unknown binding ids cannot
/// inject data into the application.
/// </summary>
public sealed class PublicationWebhookStore
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, string> _tokens = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, WebhookPayload> _payloads = new();

    /// <summary>
    /// Runs the register operation.
    /// </summary>
    public void Register(Guid bindingId, string token)
    {
    try
    {
            if (bindingId == Guid.Empty || string.IsNullOrWhiteSpace(token)) return;
            _tokens[bindingId] = token.Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationWebhookStore.Register failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the unregister operation.
    /// </summary>
    public void Unregister(Guid bindingId)
    {
    try
    {
            _tokens.TryRemove(bindingId, out _);
            _payloads.TryRemove(bindingId, out _);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationWebhookStore.Unregister failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to put.
    /// </summary>
    public bool TryPut(Guid bindingId, string token, string content, string contentType)
    {
    try
    {
            if (!_tokens.TryGetValue(bindingId, out var expected)
                || !string.Equals(expected, token, StringComparison.Ordinal)) return false;
            _payloads[bindingId] = new WebhookPayload(content ?? string.Empty, contentType ?? string.Empty, DateTimeOffset.UtcNow);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationWebhookStore.TryPut failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get.
    /// </summary>
    public bool TryGet(Guid bindingId, out WebhookPayload payload)
        {
    try
    {
        return _payloads.TryGetValue(bindingId, out payload!);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationWebhookStore.TryGet failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether registered.
    /// </summary>
    public bool IsRegistered(Guid bindingId) {
    try
    {
        return _tokens.ContainsKey(bindingId);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationWebhookStore.IsRegistered failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a webhook payload.
/// </summary>
public sealed record WebhookPayload(string Content, string ContentType, DateTimeOffset ReceivedUtc);
