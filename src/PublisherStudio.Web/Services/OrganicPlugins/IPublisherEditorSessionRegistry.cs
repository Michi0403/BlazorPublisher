using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Tracks the open PublisherStudio editor sessions (one per interactive browser circuit) so that singleton 1-Wire handlers
/// can list them and hand an approved creation request to the circuit that owns the publication.
/// </summary>
public interface IPublisherEditorSessionRegistry
{
    /// <summary>Registers one interactive editor session.</summary>
    /// <param name="describeAsync">Callback that builds a thread-safe snapshot on the owning circuit.</param>
    /// <param name="createAsync">Callback that creates a document on the owning circuit.</param>
    /// <param name="exportAsync">Callback that exports the publication through the owning circuit's editor export path.</param>
    /// <returns>The identifier under which the session is registered.</returns>
    Guid Register(
        Func<Guid, Task<PublisherEditorSessionSnapshot>> describeAsync,
        Func<PublisherDocumentCreateRequest, Task<PublisherDocumentCreateResult>> createAsync,
        Func<PublisherPublicationExportRequest, Task<PublisherPublicationExportResult>> exportAsync);

    /// <summary>Removes a session when its circuit goes away.</summary>
    /// <param name="sessionId">Identifier returned by <see cref="Register"/>.</param>
    void Unregister(Guid sessionId);

    /// <summary>Lists the currently registered sessions with their spreadsheet and text objects.</summary>
    /// <param name="cancellationToken">Cancels the listing.</param>
    /// <returns>One snapshot per reachable session; sessions that fail to answer are omitted.</returns>
    Task<IReadOnlyList<PublisherEditorSessionSnapshot>> GetSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a document in one session. Never guesses: with several sessions a sessionId is required.</summary>
    /// <param name="sessionId">Optional session identifier text.</param>
    /// <param name="request">The approved creation request.</param>
    /// <param name="cancellationToken">Cancels the wait for the owning circuit.</param>
    /// <returns>The creation result, or a failure result that says what the user must open or specify.</returns>
    Task<PublisherDocumentCreateResult> CreateAsync(string? sessionId, PublisherDocumentCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exports the publication of one session. Never guesses: with several sessions a sessionId is required.</summary>
    /// <param name="sessionId">Optional session identifier text.</param>
    /// <param name="request">The approved export request.</param>
    /// <param name="cancellationToken">Cancels the wait for the owning circuit.</param>
    /// <returns>The exported file, or a failure result that says what the user must open or specify.</returns>
    Task<PublisherPublicationExportResult> ExportAsync(string? sessionId, PublisherPublicationExportRequest request, CancellationToken cancellationToken = default);
}
