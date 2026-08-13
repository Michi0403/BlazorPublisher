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
    /// Stores the in-memory tokens collection maintained internally by <see cref="PublicationWebhookStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, string> _tokens = new();
    /// <summary>
    /// Stores the in-memory payloads collection maintained internally by <see cref="PublicationWebhookStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, WebhookPayload> _payloads = new();

    /// <summary>
    /// Performs register in the publication webhook persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationWebhookStore"/>.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
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
    /// Performs unregister in the publication webhook persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationWebhookStore"/>.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
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
    /// Attempts to put in the publication webhook persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationWebhookStore"/>.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="content">Content value supplied to the publication webhook operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the publication webhook operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Attempts to get in the publication webhook persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationWebhookStore"/>.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <param name="payload">Payload value supplied to the publication webhook operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Determines whether registered in the publication webhook persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationWebhookStore"/>.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
/// Represents a webhook payload application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Content">Content value supplied to the webhook payload operation and used when producing its result.</param>
/// <param name="ContentType">Content type value supplied to the webhook payload operation and used when producing its result.</param>
/// <param name="ReceivedUtc">Received utc value supplied to the webhook payload operation and used when producing its result.</param>
public sealed record WebhookPayload(string Content, string ContentType, DateTimeOffset ReceivedUtc);
