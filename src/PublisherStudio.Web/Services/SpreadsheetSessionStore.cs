using System.Collections.Concurrent;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Provides spreadsheet session store operations.
/// </summary>
public sealed class SpreadsheetSessionStore
{
    private readonly ConcurrentDictionary<Guid, SpreadsheetEditorSession> _sessions = new();
    private readonly SpreadsheetDocumentService _documents;
    private readonly IPublisherRuntimePolicyDataService runtimePolicy;

    /// <summary>
    /// Runs the spreadsheet session store operation.
    /// </summary>
    public SpreadsheetSessionStore(
        SpreadsheetDocumentService documents,
        IPublisherRuntimePolicyDataService runtimePolicy)
    {
        _documents = documents;
        this.runtimePolicy = runtimePolicy;
    }

    /// <summary>
    /// Runs the create operation.
    /// </summary>
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
    /// Attempts to get.
    /// </summary>
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
    /// Runs the replace operation.
    /// </summary>
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
    /// Runs the update operation.
    /// </summary>
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
    /// Runs the remove operation.
    /// </summary>
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
/// Represents a spreadsheet editor session.
/// </summary>
public sealed class SpreadsheetEditorSession
{
    internal object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; init; }
    /// <summary>
    /// Gets or sets element identifier.
    /// </summary>
    public Guid ElementId { get; init; }
    /// <summary>
    /// Gets or sets document identifier.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets file name.
    /// </summary>
    public string FileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets source format.
    /// </summary>
    public SpreadsheetStorageFormat SourceFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public byte[] Content { get; set; } = [];
    /// <summary>
    /// Gets or sets preview HTML.
    /// </summary>
    public string PreviewHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets active sheet name.
    /// </summary>
    public string ActiveSheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets the UTC update time.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// Runs the clone operation.
    /// </summary>
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
