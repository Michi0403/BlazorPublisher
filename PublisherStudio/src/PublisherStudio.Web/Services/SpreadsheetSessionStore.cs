using System.Collections.Concurrent;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class SpreadsheetSessionStore
{
    private readonly ConcurrentDictionary<Guid, SpreadsheetEditorSession> _sessions = new();
    private readonly SpreadsheetDocumentService _documents;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(4);

    public SpreadsheetSessionStore(SpreadsheetDocumentService documents) => _documents = documents;

    public SpreadsheetEditorSession Create(Guid elementId, string fileName, SpreadsheetStorageFormat format, byte[] content)
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

    public bool TryGet(Guid id, out SpreadsheetEditorSession session)
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

    public SpreadsheetEditorSession Replace(Guid id, string fileName, SpreadsheetStorageFormat format, byte[] workbookContent)
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

    public SpreadsheetEditorSession Update(Guid id, byte[] workbookContent, SpreadsheetStorageFormat format, string? activeSheetName = null)
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

    public bool Remove(Guid id) => _sessions.TryRemove(id, out _);

    private void CleanupExpired()
    {
        var cutoff = DateTimeOffset.UtcNow - SessionLifetime;
        foreach (var session in _sessions.Where(item => item.Value.UpdatedUtc < cutoff).Select(item => item.Key).ToArray())
            _sessions.TryRemove(session, out _);
    }
}

public sealed class SpreadsheetEditorSession
{
    internal object SyncRoot { get; } = new();
    public Guid Id { get; init; }
    public Guid ElementId { get; init; }
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = "Spreadsheet.xlsx";
    public SpreadsheetStorageFormat SourceFormat { get; set; } = SpreadsheetStorageFormat.Xlsx;
    public byte[] Content { get; set; } = [];
    public string PreviewHtml { get; set; } = string.Empty;
    public string ActiveSheetName { get; set; } = "Sheet1";
    public DateTimeOffset UpdatedUtc { get; set; }

    public SpreadsheetEditorSession Clone() => new()
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
