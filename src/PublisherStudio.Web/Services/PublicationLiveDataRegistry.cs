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
    /// Stores the in-memory documents collection maintained internally by <see cref="PublicationLiveDataRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, LivePublicationSnapshot> _documents = new();
    /// <summary>
    /// Stores the in-memory export tokens collection maintained internally by <see cref="PublicationLiveDataRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<(Guid DocumentId, Guid DataId), string> _exportTokens = new();
    /// <summary>
    /// Stores the in-memory document webhook bindings collection maintained internally by <see cref="PublicationLiveDataRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _documentWebhookBindings = new();
    /// <summary>
    /// Stores the publication webhook store dependency used by <see cref="PublicationLiveDataRegistry"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationWebhookStore _webhooks;

    /// <summary>
    /// Initializes a new <see cref="PublicationLiveDataRegistry"/> instance and captures the dependencies or initial state required by its publication live data workflow.
    /// </summary>
    /// <param name="webhooks">Publication webhook store dependency used by the publication live data workflow to provide the corresponding application capability.</param>
    public PublicationLiveDataRegistry(PublicationWebhookStore webhooks) => _webhooks = webhooks;

    /// <summary>
    /// Performs register in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="document">Document value supplied to the publication live data operation and used when producing its result.</param>
    /// <param name="dataService">Publication data service dependency used by the publication live data workflow to provide the corresponding application capability.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
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
    /// Determines whether reuse data snapshot in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="previous">Previous value supplied to the publication live data operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication live data operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs unregister in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
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
    /// Unregisters webhook when unused in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
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
    /// Attempts to get in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="snapshot">Snapshot value supplied to the publication live data operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Attempts to retrieve export rows in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="dataId">Identifier of the data to use for this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="rows">String dependency used by the publication live data workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs summaries in the publication live data directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
/// Represents a live publication summary application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the live publication summary operation and used when producing its result.</param>
/// <param name="ModifiedUtc">Modified utc value supplied to the live publication summary operation and used when producing its result.</param>
/// <param name="PageCount">Page count value supplied to the live publication summary operation and used when producing its result.</param>
/// <param name="DataObjectCount">Data object count value supplied to the live publication summary operation and used when producing its result.</param>
public sealed record LivePublicationSummary(Guid Id, string Name, DateTimeOffset ModifiedUtc, int PageCount, int DataObjectCount);
/// <summary>
/// Represents a live publication snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the live publication snapshot operation and used when producing its result.</param>
/// <param name="ModifiedUtc">Modified utc value supplied to the live publication snapshot operation and used when producing its result.</param>
/// <param name="DataObjects">Live data object snapshot dependency used by the live publication snapshot workflow to provide the corresponding application capability.</param>
/// <param name="Pages">Live page snapshot dependency used by the live publication snapshot workflow to provide the corresponding application capability.</param>
public sealed record LivePublicationSnapshot(Guid Id, string Name, DateTimeOffset ModifiedUtc,
    IReadOnlyDictionary<Guid, LiveDataObjectSnapshot> DataObjects, IReadOnlyList<LivePageSnapshot> Pages);
/// <summary>
/// Represents a live data object snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the live data object snapshot operation and used when producing its result.</param>
/// <param name="SourceKind">Source kind value supplied to the live data object snapshot operation and used when producing its result.</param>
/// <param name="ModifiedUtc">Modified utc value supplied to the live data object snapshot operation and used when producing its result.</param>
/// <param name="Columns">Live data column dependency used by the live data object snapshot workflow to provide the corresponding application capability.</param>
/// <param name="Rows">String dependency used by the live data object snapshot workflow to provide the corresponding application capability.</param>
public sealed record LiveDataObjectSnapshot(Guid Id, string Name, string SourceKind, DateTimeOffset ModifiedUtc,
    IReadOnlyList<LiveDataColumn> Columns, IReadOnlyList<Dictionary<string, string>> Rows);
/// <summary>
/// Represents a live data column application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Name">Name value supplied to the live data column operation and used when producing its result.</param>
/// <param name="ValueKind">Value kind value supplied to the live data column operation and used when producing its result.</param>
public sealed record LiveDataColumn(string Name, string ValueKind);
/// <summary>
/// Represents a live page snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the live page snapshot operation and used when producing its result.</param>
/// <param name="WidthMm">Width mm value supplied to the live page snapshot operation and used when producing its result.</param>
/// <param name="HeightMm">Height mm value supplied to the live page snapshot operation and used when producing its result.</param>
/// <param name="Elements">Live element snapshot dependency used by the live page snapshot workflow to provide the corresponding application capability.</param>
public sealed record LivePageSnapshot(Guid Id, string Name, double WidthMm, double HeightMm, IReadOnlyList<LiveElementSnapshot> Elements);
/// <summary>
/// Represents a live element snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Kind">Kind value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="X">X value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Y">Y value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Width">Width value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Height">Height value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Rotation">Rotation value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Layer">Layer value supplied to the live element snapshot operation and used when producing its result.</param>
/// <param name="Visible">Value indicating whether the value is visible should apply to this operation.</param>
/// <param name="Locked">Value indicating whether locked should apply to this operation.</param>
public sealed record LiveElementSnapshot(Guid Id, string Name, string Kind, double X, double Y, double Width, double Height,
    double Rotation, int Layer, bool Visible, bool Locked);
