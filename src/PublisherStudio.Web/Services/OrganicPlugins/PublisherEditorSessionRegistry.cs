using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Singleton registry of open editor sessions. Each interactive circuit registers callbacks that run on its own
/// synchronization context, so no circuit-scoped state is read or changed from another thread.
/// </summary>
/// <param name="options">Organic plugin settings; the editor timeouts are read from <c>OrganicPlugins:DocumentCreation</c>.</param>
/// <param name="logger">Structured diagnostics for registration, listing and creation.</param>
public sealed class PublisherEditorSessionRegistry(IOptions<OrganicPluginOptions> options, ILogger<PublisherEditorSessionRegistry> logger) : IPublisherEditorSessionRegistry
{
    /// <summary>One registered circuit.</summary>
    /// <param name="DescribeAsync">Builds the session snapshot on the owning circuit.</param>
    /// <param name="CreateAsync">Creates a document on the owning circuit.</param>
    private sealed record SessionEntry(
        Func<Guid, Task<PublisherEditorSessionSnapshot>> DescribeAsync,
        Func<PublisherDocumentCreateRequest, Task<PublisherDocumentCreateResult>> CreateAsync);

    /// <summary>Registered sessions by identifier.</summary>
    private readonly ConcurrentDictionary<Guid, SessionEntry> sessions = new();

    /// <inheritdoc/>
    public Guid Register(
        Func<Guid, Task<PublisherEditorSessionSnapshot>> describeAsync,
        Func<PublisherDocumentCreateRequest, Task<PublisherDocumentCreateResult>> createAsync)
    {
        try
        {
            var id = Guid.NewGuid();
            sessions[id] = new SessionEntry(describeAsync, createAsync);
            logger.LogInformation("Registered PublisherStudio editor session {SessionId}; {Count} open.", id, sessions.Count);
            return id;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not register a PublisherStudio editor session.");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Unregister(Guid sessionId)
    {
        try
        {
            if (sessions.TryRemove(sessionId, out _))
                logger.LogInformation("Unregistered PublisherStudio editor session {SessionId}; {Count} open.", sessionId, sessions.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not unregister a PublisherStudio editor session.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PublisherEditorSessionSnapshot>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new List<PublisherEditorSessionSnapshot>();
            foreach (var pair in sessions.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var answer = pair.Value.DescribeAsync(pair.Key);
                    var finished = await Task.WhenAny(answer, Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.Value.DocumentCreation.SessionDescriptionTimeoutSeconds)), cancellationToken)).ConfigureAwait(false);
                    if (finished == answer) result.Add(await answer.ConfigureAwait(false));
                    else logger.LogWarning("Editor session {SessionId} did not answer the session list in time.", pair.Key);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Editor session {SessionId} could not be described and was omitted.", pair.Key);
                    sessions.TryRemove(pair.Key, out _);
                }
            }
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Listing editor sessions was canceled.");
            else
                logger.LogError(exception, "Listing PublisherStudio editor sessions failed.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PublisherDocumentCreateResult> CreateAsync(string? sessionId, PublisherDocumentCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            KeyValuePair<Guid, SessionEntry>[] open = sessions.ToArray();
            if (open.Length == 0)
                return Failure("No PublisherStudio editor is open. Ask the user to open PublisherStudio in the browser (a publication must be loaded in the editor), then retry.");
            KeyValuePair<Guid, SessionEntry> target;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                if (!Guid.TryParse(sessionId, out var wanted) || !sessions.TryGetValue(wanted, out var found))
                    return Failure("The sessionId does not match an open PublisherStudio editor session. Call publisher.sessions.list for current ids.");
                target = new KeyValuePair<Guid, SessionEntry>(wanted, found);
            }
            else if (open.Length == 1)
                target = open[0];
            else
                return Failure($"{open.Length} editor sessions are open; call publisher.sessions.list and pass a sessionId.");

            var work = target.Value.CreateAsync(request);
            var creationTimeoutSeconds = Math.Max(1, options.Value.DocumentCreation.EditorCreationTimeoutSeconds);
            var finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(creationTimeoutSeconds), cancellationToken)).ConfigureAwait(false);
            if (finished != work)
                return Failure($"The PublisherStudio editor did not answer within {creationTimeoutSeconds} seconds; nothing was confirmed.");
            return await work.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Document creation was canceled.");
            else
                logger.LogError(exception, "Creating a document through the editor session failed.");
            throw;
        }
    }

    /// <summary>Builds a failed creation result.</summary>
    /// <param name="message">Explanation shown to the peer.</param>
    /// <returns>A failure result.</returns>
    private PublisherDocumentCreateResult Failure(string message)
    {
        try
        {
            logger.LogInformation("Document creation not performed: {Message}", message);
            return new PublisherDocumentCreateResult { Success = false, Message = message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not build the failure result.");
            throw;
        }
    }
}
