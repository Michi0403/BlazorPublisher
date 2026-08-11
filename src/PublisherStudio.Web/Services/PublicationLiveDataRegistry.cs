using System.Collections.Concurrent;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Keeps immutable snapshots of open publications available to the local monolith
/// API. This is also the first transport boundary for future LAN presentation and
/// streaming providers: consumers see DTOs instead of mutable editor state.
/// </summary>
public sealed class PublicationLiveDataRegistry
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, LivePublicationSnapshot> _documents = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<(Guid DocumentId, Guid DataId), string> _exportTokens = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _documentWebhookBindings = new();
    private readonly PublicationWebhookStore _webhooks;

    /// <summary>
    /// Runs the publication live data registry operation.
    /// </summary>
    public PublicationLiveDataRegistry(PublicationWebhookStore webhooks) => _webhooks = webhooks;

    /// <summary>
    /// Runs the register operation.
    /// </summary>
    public void Register(PublicationDocument document, PublicationDataService dataService, Guid? currentPageId = null)
    {
    try
    {
            foreach (var key in _exportTokens.Keys.Where(key => key.DocumentId == document.Id))
                _exportTokens.TryRemove(key, out _);

            var currentWebhookBindings = document.DataObjects
                .Where(item => item.SourceKind == PublicationDataSourceKind.Web)
                .Select(item => item.Web.Id)
                .Where(id => id != Guid.Empty)
                .ToHashSet();
            _documentWebhookBindings.TryGetValue(document.Id, out var previousWebhookBindings);
            _documentWebhookBindings[document.Id] = currentWebhookBindings;

            foreach (var removed in (previousWebhookBindings ?? []).Except(currentWebhookBindings))
                UnregisterWebhookWhenUnused(removed);

            foreach (var item in document.DataObjects.Where(item => item.SourceKind == PublicationDataSourceKind.Web))
            {
                _webhooks.Register(item.Web.Id, item.Web.WebhookToken);
                if (!string.IsNullOrWhiteSpace(item.Web.ExportAccessToken))
                    _exportTokens[(document.Id, item.Id)] = item.Web.ExportAccessToken;
            }

            var resolvedPageId = currentPageId is Guid selected && document.Pages.Any(page => page.Id == selected)
                ? selected
                : document.Pages.FirstOrDefault()?.Id ?? Guid.Empty;
            _documents.TryGetValue(document.Id, out var previous);
            var data = document.DataObjects.ToDictionary(
                item => item.Id,
                item => CanReuseDataSnapshot(previous, item)
                    ? previous!.DataObjects[item.Id]
                    : new LiveDataObjectSnapshot(
                        item.Id,
                        item.Name,
                        item.SourceKind.ToString(),
                        item.ModifiedUtc,
                        dataService.ResolveColumns(item)
                            .Select(column => new LiveDataColumn(column.Name, column.ValueKind.ToString()))
                            .ToArray(),
                        dataService.ResolveRows(document, item, resolvedPageId)
                            .Select(row => row.Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase))
                            .ToArray()));

            var pages = document.Pages.Select(page => new LivePageSnapshot(
                page.Id,
                page.Name,
                page.WidthMm,
                page.HeightMm,
                page.Elements.Select(element => new LiveElementSnapshot(
                    element.Id,
                    element.Name,
                    element.Kind.ToString(),
                    element.X,
                    element.Y,
                    element.Width,
                    element.Height,
                    element.Rotation,
                    element.ZIndex,
                    element.Visible,
                    element.Locked)).ToArray())).ToArray();

            _documents[document.Id] = new LivePublicationSnapshot(
                document.Id,
                document.Name,
                document.ModifiedUtc,
                data,
                pages);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.Register failed: {__serviceMethodException}");
        throw;
    }
}


    /// <summary>
    /// Determines whether reuse data snapshot.
    /// </summary>
    private bool CanReuseDataSnapshot(LivePublicationSnapshot? previous, PublicationDataObject item)
        {
    try
    {
        return (item.SourceKind is not PublicationDataSourceKind.DocumentObjects
            and not PublicationDataSourceKind.PublicationPages
            and not PublicationDataSourceKind.PublicationDocument
            and not PublicationDataSourceKind.PublicationMedia)
            && previous is not null
            && previous.DataObjects.TryGetValue(item.Id, out var snapshot)
            && snapshot.ModifiedUtc == item.ModifiedUtc
            && string.Equals(snapshot.Name, item.Name, StringComparison.Ordinal)
            && string.Equals(snapshot.SourceKind, item.SourceKind.ToString(), StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.CanReuseDataSnapshot failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the unregister operation.
    /// </summary>
    public void Unregister(Guid documentId)
    {
    try
    {
            _documents.TryRemove(documentId, out _);
            foreach (var key in _exportTokens.Keys.Where(key => key.DocumentId == documentId))
                _exportTokens.TryRemove(key, out _);
            if (_documentWebhookBindings.TryRemove(documentId, out var bindings))
                foreach (var bindingId in bindings) UnregisterWebhookWhenUnused(bindingId);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.Unregister failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Unregisters webhook when unused.
    /// </summary>
    private void UnregisterWebhookWhenUnused(Guid bindingId)
    {
    try
    {
            if (_documentWebhookBindings.Values.Any(bindings => bindings.Contains(bindingId))) return;
            _webhooks.Unregister(bindingId);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.UnregisterWebhookWhenUnused failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get.
    /// </summary>
    public bool TryGet(Guid documentId, out LivePublicationSnapshot snapshot)
        {
    try
    {
        return _documents.TryGetValue(documentId, out snapshot!);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.TryGet failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get export rows.
    /// </summary>
    public bool TryGetExportRows(Guid documentId, Guid dataId, string token, out IReadOnlyList<Dictionary<string, string>> rows)
    {
    try
    {
            rows = [];
            if (!_exportTokens.TryGetValue((documentId, dataId), out var expected)
                || !string.Equals(expected, token, StringComparison.Ordinal)
                || !_documents.TryGetValue(documentId, out var document)
                || !document.DataObjects.TryGetValue(dataId, out var data)) return false;
            rows = data.Rows;
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.TryGetExportRows failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the summaries operation.
    /// </summary>
    public IReadOnlyList<LivePublicationSummary> Summaries()
        {
    try
    {
        return _documents.Values
            .OrderByDescending(item => item.ModifiedUtc)
            .Select(item => new LivePublicationSummary(item.Id, item.Name, item.ModifiedUtc, item.Pages.Count, item.DataObjects.Count))
            .ToArray();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationLiveDataRegistry.Summaries failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a live publication summary.
/// </summary>
public sealed record LivePublicationSummary(Guid Id, string Name, DateTimeOffset ModifiedUtc, int PageCount, int DataObjectCount);
/// <summary>
/// Represents a live publication snapshot.
/// </summary>
public sealed record LivePublicationSnapshot(Guid Id, string Name, DateTimeOffset ModifiedUtc,
    IReadOnlyDictionary<Guid, LiveDataObjectSnapshot> DataObjects, IReadOnlyList<LivePageSnapshot> Pages);
/// <summary>
/// Represents a live data object snapshot.
/// </summary>
public sealed record LiveDataObjectSnapshot(Guid Id, string Name, string SourceKind, DateTimeOffset ModifiedUtc,
    IReadOnlyList<LiveDataColumn> Columns, IReadOnlyList<Dictionary<string, string>> Rows);
/// <summary>
/// Represents a live data column.
/// </summary>
public sealed record LiveDataColumn(string Name, string ValueKind);
/// <summary>
/// Represents a live page snapshot.
/// </summary>
public sealed record LivePageSnapshot(Guid Id, string Name, double WidthMm, double HeightMm, IReadOnlyList<LiveElementSnapshot> Elements);
/// <summary>
/// Represents a live element snapshot.
/// </summary>
public sealed record LiveElementSnapshot(Guid Id, string Name, string Kind, double X, double Y, double Width, double Height,
    double Rotation, int Layer, bool Visible, bool Locked);
