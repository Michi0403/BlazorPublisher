using System.Collections.Concurrent;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Owns persistence and retrieval of spreadsheet session state, keeping storage-specific behavior behind a focused application abstraction.
/// </summary>
public sealed class SpreadsheetSessionStore
{
    /// <summary>
    /// Stores the in-memory sessions collection maintained internally by <see cref="SpreadsheetSessionStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, SpreadsheetEditorSession> _sessions = new();
    /// <summary>
    /// Stores the spreadsheet document service dependency used by <see cref="SpreadsheetSessionStore"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly SpreadsheetDocumentService _documents;
    /// <summary>
    /// Stores the publisher runtime policy data service dependency used by <see cref="SpreadsheetSessionStore"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherRuntimePolicyDataService runtimePolicy;

    /// <summary>
    /// Initializes a new <see cref="SpreadsheetSessionStore"/> instance and captures the dependencies or initial state required by its spreadsheet session workflow.
    /// </summary>
    /// <param name="documents">Spreadsheet document service dependency used by the spreadsheet session workflow to provide the corresponding application capability.</param>
    /// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the spreadsheet session workflow to provide the corresponding application capability.</param>
    public SpreadsheetSessionStore(
        SpreadsheetDocumentService documents,
        IPublisherRuntimePolicyDataService runtimePolicy)
    {
        _documents = documents;
        this.runtimePolicy = runtimePolicy;
    }

    /// <summary>
    /// Performs create in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="fileName">File name value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <returns>The spreadsheet editor session produced by the operation.</returns>
    public SpreadsheetEditorSession Create(Guid elementId, string fileName, SpreadsheetStorageFormat format, byte[] content)
    {
    try
    {
            CleanupExpired();
            _documents.ValidateWorkbookContent(content, format);
            var preview = _documents.RenderPreviewHtml(content, format, out var activeSheet);
            var session = new SpreadsheetEditorSession
            {
                Id = Guid.NewGuid(),
                ElementId = elementId,
                DocumentId = $"publisher-spreadsheet-{Guid.NewGuid():N}",
                FileName = _documents.NormalizeWorkbookFileName(fileName, format),
                SourceFormat = format,
                Content = content.ToArray(),
                PreviewHtml = preview,
                ActiveSheetName = activeSheet,
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            _sessions[session.Id] = session;
            return session.Clone();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.Create failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="session">Session value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid id, out SpreadsheetEditorSession session)
    {
    try
    {
            CleanupExpired();
            if (_sessions.TryGetValue(id, out var stored))
            {
                stored.UpdatedUtc = DateTimeOffset.UtcNow;
                session = stored.Clone();
                return true;
            }
            session = default!;
            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.TryGet failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs replace in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="fileName">File name value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="workbookContent">Workbook content value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <returns>The spreadsheet editor session produced by the operation.</returns>
    public SpreadsheetEditorSession Replace(Guid id, string fileName, SpreadsheetStorageFormat format, byte[] workbookContent)
    {
    try
    {
            if (!_sessions.TryGetValue(id, out var session)) throw new KeyNotFoundException("Spreadsheet editing session expired.");
            _documents.ValidateWorkbookContent(workbookContent, format);
            lock (session.SyncRoot)
            {
                session.DocumentId = $"publisher-spreadsheet-{Guid.NewGuid():N}";
                session.FileName = _documents.NormalizeWorkbookFileName(fileName, format);
                session.SourceFormat = format;
                session.Content = workbookContent.ToArray();
                session.PreviewHtml = _documents.RenderPreviewHtml(session.Content, format, out var activeSheetName);
                session.ActiveSheetName = activeSheetName;
                session.UpdatedUtc = DateTimeOffset.UtcNow;
                return session.Clone();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.Replace failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs update in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="workbookContent">Workbook content value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <param name="activeSheetName">Active sheet name value supplied to the spreadsheet session operation and used when producing its result.</param>
    /// <returns>The spreadsheet editor session produced by the operation.</returns>
    public SpreadsheetEditorSession Update(Guid id, byte[] workbookContent, SpreadsheetStorageFormat format, string? activeSheetName = null)
    {
    try
    {
            if (!_sessions.TryGetValue(id, out var session)) throw new KeyNotFoundException("Spreadsheet editing session expired.");
            _documents.ValidateWorkbookContent(workbookContent, format);
            lock (session.SyncRoot)
            {
                session.Content = workbookContent.ToArray();
                session.SourceFormat = format;
                session.FileName = _documents.NormalizeWorkbookFileName(session.FileName, format);
                session.PreviewHtml = _documents.RenderPreviewHtml(session.Content, format, out var parsedSheetName);
                session.ActiveSheetName = string.IsNullOrWhiteSpace(activeSheetName) ? parsedSheetName : activeSheetName;
                session.UpdatedUtc = DateTimeOffset.UtcNow;
                return session.Clone();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.Update failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs remove in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Remove(Guid id) {
    try
    {
        return _sessions.TryRemove(id, out _);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.Remove failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs cleanup expired in the spreadsheet session persistence workflow while keeping storage-specific behavior contained within <see cref="SpreadsheetSessionStore"/>.
    /// </summary>
    private void CleanupExpired()
    {
    try
    {
            var cutoff = DateTimeOffset.UtcNow - runtimePolicy.SpreadsheetSessionLifetime;
            foreach (var session in _sessions.Where(item => item.Value.UpdatedUtc < cutoff).Select(item => item.Key).ToArray())
                _sessions.TryRemove(session, out _);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetSessionStore.CleanupExpired failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a spreadsheet editor session application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SpreadsheetEditorSession
{
    /// <summary>
    /// Gets the sync root value that forms part of the spreadsheet editor session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    internal object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this spreadsheet editor session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public Guid Id { get; init; }
    /// <summary>
    /// Gets or sets the stable element identifier used to identify or correlate this spreadsheet editor session instance with related application state.
    /// </summary>
    /// <value>The element identifier value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public Guid ElementId { get; init; }
    /// <summary>
    /// Gets or sets the stable document identifier used to identify or correlate this spreadsheet editor session instance with related application state.
    /// </summary>
    /// <value>The document identifier value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public string DocumentId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file name used by this spreadsheet editor session instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file name value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public string FileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets the source format value that forms part of the spreadsheet editor session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public SpreadsheetStorageFormat SourceFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    /// <summary>
    /// Gets or sets the content value that forms part of the spreadsheet editor session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public byte[] Content { get; set; } = [];
    /// <summary>
    /// Gets or sets the preview HTML value that forms part of the spreadsheet editor session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preview HTML value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public string PreviewHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the active sheet name value that forms part of the spreadsheet editor session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active sheet name value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public string ActiveSheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets the updated UTC associated with this spreadsheet editor session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="SpreadsheetEditorSession"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// Performs clone for <see cref="SpreadsheetEditorSession"/>, keeping the operation consistent with the state and invariants of the surrounding spreadsheet editor session workflow.
    /// </summary>
    /// <returns>The spreadsheet editor session produced by the operation.</returns>
    public SpreadsheetEditorSession Clone() {
    try
    {
        return new()
    {
        Id = Id,
        ElementId = ElementId,
        DocumentId = DocumentId,
        FileName = FileName,
        SourceFormat = SourceFormat,
        Content = Content.ToArray(),
        PreviewHtml = PreviewHtml,
        ActiveSheetName = ActiveSheetName,
        UpdatedUtc = UpdatedUtc
    };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetEditorSession.Clone failed: {__serviceMethodException}");
        throw;
    }
}
}
