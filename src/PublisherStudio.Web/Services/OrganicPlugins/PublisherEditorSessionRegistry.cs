using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Singleton registry of open editor sessions. Each interactive circuit registers callbacks that run on its own
/// synchronization context, so no circuit-scoped state is read or changed from another thread.
/// </summary>
/// <param name="options">Organic plugin settings; the editor timeouts are read from <c>OrganicPlugins:DocumentCreation</c> and <c>OrganicPlugins:MediaTools</c>.</param>
/// <param name="logger">Structured diagnostics for registration, listing and creation.</param>
public sealed class PublisherEditorSessionRegistry(IOptions<OrganicPluginOptions> options, ILogger<PublisherEditorSessionRegistry> logger) : IPublisherEditorSessionRegistry
{
    /// <summary>One registered circuit.</summary>
    /// <param name="DescribeAsync">Builds the session snapshot on the owning circuit.</param>
    /// <param name="CreateAsync">Creates a document on the owning circuit.</param>
    /// <param name="ExportAsync">Exports the publication on the owning circuit.</param>
    private sealed record SessionEntry(
        Func<Guid, Task<PublisherEditorSessionSnapshot>> DescribeAsync,
        Func<PublisherDocumentCreateRequest, Task<PublisherDocumentCreateResult>> CreateAsync,
        Func<PublisherPublicationExportRequest, Task<PublisherPublicationExportResult>> ExportAsync);

    /// <summary>Registered sessions by identifier.</summary>
    private readonly ConcurrentDictionary<Guid, SessionEntry> sessions = new();

    /// <inheritdoc/>
    public Guid Register(
        Func<Guid, Task<PublisherEditorSessionSnapshot>> describeAsync,
        Func<PublisherDocumentCreateRequest, Task<PublisherDocumentCreateResult>> createAsync,
        Func<PublisherPublicationExportRequest, Task<PublisherPublicationExportResult>> exportAsync)
    {
        try
        {
            var id = Guid.NewGuid();
            sessions[id] = new SessionEntry(describeAsync, createAsync, exportAsync);
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
            if (SelectSession(sessionId, out var failure) is not { } target)
                return Failure(failure);

            var work = target.CreateAsync(request);
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

    /// <inheritdoc/>
    public async Task<PublisherPublicationExportResult> ExportAsync(string? sessionId, PublisherPublicationExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (SelectSession(sessionId, out var failure) is not { } target)
            {
                logger.LogInformation("Publication export not performed: {Message}", failure);
                return new PublisherPublicationExportResult { Success = false, Scope = request.Scope, Message = failure };
            }

            var work = target.ExportAsync(request);
            var exportTimeoutSeconds = Math.Max(1, options.Value.MediaTools.EditorExportTimeoutSeconds);
            var finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(exportTimeoutSeconds), cancellationToken)).ConfigureAwait(false);
            if (finished != work)
            {
                logger.LogWarning("The editor did not finish the {Scope} export within {Seconds} seconds.", request.Scope, exportTimeoutSeconds);
                return new PublisherPublicationExportResult
                {
                    Success = false,
                    Scope = request.Scope,
                    Message = $"The PublisherStudio editor did not finish the export within {exportTimeoutSeconds} seconds (OrganicPlugins:MediaTools:EditorExportTimeoutSeconds)."
                };
            }
            return await work.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Publication export was canceled.");
            else
                logger.LogError(exception, "Exporting the publication through the editor session failed.");
            throw;
        }
    }

    /// <summary>Selects the session addressed by a request without guessing between several open editors.</summary>
    /// <param name="sessionId">Optional session identifier text.</param>
    /// <param name="failure">Explains what the caller must open or specify when no session is selected.</param>
    /// <returns>The selected session, or <see langword="null"/>.</returns>
    private SessionEntry? SelectSession(string? sessionId, out string failure)
    {
        try
        {
            failure = string.Empty;
            KeyValuePair<Guid, SessionEntry>[] open = sessions.ToArray();
            if (open.Length == 0)
            {
                failure = "No PublisherStudio editor is open. Ask the user to open PublisherStudio in the browser (a publication must be loaded in the editor), then retry.";
                return null;
            }
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                if (Guid.TryParse(sessionId, out var wanted) && sessions.TryGetValue(wanted, out var found))
                    return found;
                failure = "The sessionId does not match an open PublisherStudio editor session. Call publisher.sessions.list for current ids.";
                return null;
            }
            if (open.Length == 1)
                return open[0].Value;
            failure = $"{open.Length} editor sessions are open; call publisher.sessions.list and pass a sessionId.";
            logger.LogDebug("Editor session selection needs a sessionId because {Count} sessions are open.", open.Length);
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not select the PublisherStudio editor session.");
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
