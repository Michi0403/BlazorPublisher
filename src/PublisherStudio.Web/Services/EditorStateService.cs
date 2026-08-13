using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Panels;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Coordinates editor state behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class EditorStateService : IDisposable
{
    /// <summary>
    /// Stores the publication file service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationFileService _files;
    /// <summary>
    /// Stores the publication data service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationDataService _data;
    /// <summary>
    /// Stores the publication component service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationComponentService _components;
    /// <summary>
    /// Stores the publication media asset store dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationMediaAssetStore _mediaAssets;
    /// <summary>
    /// Stores the spreadsheet document service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly SpreadsheetDocumentService _spreadsheets;
    /// <summary>
    /// Stores the publication live data registry dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationLiveDataRegistry _liveData;
    /// <summary>
    /// Stores the publication web data service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationWebDataService _webData;
    /// <summary>
    /// Stores the publication streaming settings store dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationStreamingSettingsStore _streamingSettings;
    /// <summary>
    /// Stores the panel document service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PanelDocumentService _panels;
    /// <summary>
    /// Stores the system variable store service dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ISystemVariableStoreService _systemVariables;
    /// <summary>
    /// Stores the internal media data state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PublicationMediaData _mediaData;
    /// <summary>
    /// Stores the internal element traversal state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PublicationElementTraversal _elementTraversal;
    /// <summary>
    /// Stores the logger used by <see cref="EditorStateService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<EditorStateService> logger;
    /// <summary>
    /// Stores the rich text document factory dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly RichTextDocumentFactory _richTextFactory;
    /// <summary>
    /// Stores the publisher document factory dependency used by <see cref="EditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherDocumentFactory _documentFactory;
    /// <summary>
    /// Stores the internal undo state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Stack<string> _undo = new();
    /// <summary>
    /// Stores the internal redo state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Stack<string> _redo = new();
    /// <summary>
    /// Stores the in-memory clipboard collection maintained internally by <see cref="EditorStateService"/> for its current workflow state.
    /// </summary>
    private readonly List<PublicationElement> _clipboard = [];
    /// <summary>
    /// Stores the in-memory selected element identifiers collection maintained internally by <see cref="EditorStateService"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<Guid> _selectedElementIds = [];
    /// <summary>
    /// Stores the internal live edit key state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private string? _liveEditKey;
    /// <summary>
    /// Stores the internal last insertion x state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private double? _lastInsertionX;
    /// <summary>
    /// Stores the internal last insertion y state used by <see cref="EditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private double? _lastInsertionY;

    /// <summary>
    /// Initializes a new <see cref="EditorStateService"/> instance and captures the dependencies or initial state required by its editor state workflow.
    /// </summary>
    /// <param name="files">Publication file service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="data">Publication data service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="components">Publication component service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="mediaAssets">Publication media asset store dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="spreadsheets">Spreadsheet document service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="liveData">Publication live data registry dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="webData">Publication web data service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="streamingSettings">Publication streaming settings store dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="panels">Panel document service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="systemVariables">System variable store service dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="mediaData">Media data value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="elementTraversal">Element traversal value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="richTextFactory">Rich text document factory dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="documentFactory">Publisher document factory dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public EditorStateService(
        PublicationFileService files,
        PublicationDataService data,
        PublicationComponentService components,
        PublicationMediaAssetStore mediaAssets,
        SpreadsheetDocumentService spreadsheets,
        PublicationLiveDataRegistry liveData,
        PublicationWebDataService webData,
        PublicationStreamingSettingsStore streamingSettings,
        PanelDocumentService panels,
        ISystemVariableStoreService systemVariables,
        PublicationMediaData mediaData,
        PublicationElementTraversal elementTraversal,
        RichTextDocumentFactory richTextFactory,
        IPublisherDocumentFactory documentFactory,
        ILogger<EditorStateService> logger)
    {
        _files = files;
        _data = data;
        _components = components;
        _mediaAssets = mediaAssets;
        _spreadsheets = spreadsheets;
        _liveData = liveData;
        _webData = webData;
        _streamingSettings = streamingSettings;
        _panels = panels;
        _systemVariables = systemVariables;
        _mediaData = mediaData;
        _elementTraversal = elementTraversal;
        this.logger = logger;
        _richTextFactory = richTextFactory;
        _documentFactory = documentFactory;
        Document = _documentFactory.CreatePublication();
        Document.Streaming = _streamingSettings.LoadOrDefault(Document.Id);
        _files.NormalizeStreamingSettings(Document);
        SelectedPageId = Document.Pages[0].Id;
        _liveData.Register(Document, _data, SelectedPageId);
    }

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="EditorStateService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets or sets the document value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document value exposed by <see cref="EditorStateService"/>.</value>
    public PublicationDocument Document { get; private set; }
    /// <summary>
    /// Gets or sets the stable selected page identifier used to identify or correlate this editor state instance with related application state.
    /// </summary>
    /// <value>The selected page identifier value exposed by <see cref="EditorStateService"/>.</value>
    public Guid SelectedPageId { get; private set; }
    /// <summary>
    /// Gets or sets the stable selected element identifier used to identify or correlate this editor state instance with related application state.
    /// </summary>
    /// <value>The selected element identifier value exposed by <see cref="EditorStateService"/>.</value>
    public Guid? SelectedElementId { get; private set; }
    /// <summary>
    /// Gets or sets a value indicating whether dirty applies to the editor state state.
    /// </summary>
    /// <value>The is dirty value exposed by <see cref="EditorStateService"/>.</value>
    public bool IsDirty { get; private set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="EditorStateService"/>.</value>
    public long Revision { get; private set; }
    /// <summary>
    /// Gets or sets a value indicating whether crop mode applies to the editor state state.
    /// </summary>
    /// <value>The crop mode value exposed by <see cref="EditorStateService"/>.</value>
    public bool CropMode { get; private set; }
    /// <summary>
    /// Gets or sets a value indicating whether content pan mode applies to the editor state state.
    /// </summary>
    /// <value>The content pan mode value exposed by <see cref="EditorStateService"/>.</value>
    public bool ContentPanMode { get; private set; }
    /// <summary>
    /// Gets or sets the connector tool value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The connector tool value exposed by <see cref="EditorStateService"/>.</value>
    public ConnectorToolKind ConnectorTool { get; private set; }
    /// <summary>
    /// Gets a value indicating whether undo applies to the editor state state.
    /// </summary>
    /// <value>The can undo value exposed by <see cref="EditorStateService"/>.</value>
    public bool CanUndo => _undo.Count > 0;
    /// <summary>
    /// Gets a value indicating whether redo applies to the editor state state.
    /// </summary>
    /// <value>The can redo value exposed by <see cref="EditorStateService"/>.</value>
    public bool CanRedo => _redo.Count > 0;
    /// <summary>
    /// Gets a value indicating whether paste applies to the editor state state.
    /// </summary>
    /// <value>The can paste value exposed by <see cref="EditorStateService"/>.</value>
    public bool CanPaste => _clipboard.Count > 0;
    /// <summary>
    /// Gets or sets the clipboard revision value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The clipboard revision value exposed by <see cref="EditorStateService"/>.</value>
    public long ClipboardRevision { get; private set; }
    /// <summary>
    /// Gets the current page value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current page value exposed by <see cref="EditorStateService"/>.</value>
    public PublicationPage CurrentPage => Document.Pages.First(p => p.Id == SelectedPageId);
    /// <summary>
    /// Gets the selected element value that forms part of the editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected element value exposed by <see cref="EditorStateService"/>.</value>
    public PublicationElement? SelectedElement => CurrentPage.Elements.FirstOrDefault(e => e.Id == SelectedElementId);
    /// <summary>
    /// Gets the selected elements collection maintained or exposed by this editor state instance for downstream processing.
    /// </summary>
    /// <value>The selected elements value exposed by <see cref="EditorStateService"/>.</value>
    public IReadOnlyList<PublicationElement> SelectedElements => CurrentPage.Elements
        .Where(element => _selectedElementIds.Contains(element.Id))
        .OrderBy(element => element.ZIndex)
        .ToList();
    /// <summary>
    /// Gets the selected element identifiers collection maintained or exposed by this editor state instance for downstream processing.
    /// </summary>
    /// <value>The selected element identifiers value exposed by <see cref="EditorStateService"/>.</value>
    public IReadOnlyCollection<Guid> SelectedElementIds => _selectedElementIds;
    /// <summary>
    /// Gets a value indicating whether multiple selection applies to the editor state state.
    /// </summary>
    /// <value>The has multiple selection value exposed by <see cref="EditorStateService"/>.</value>
    public bool HasMultipleSelection => _selectedElementIds.Count > 1;
    /// <summary>
    /// Gets a value indicating whether group selection applies to the editor state state.
    /// </summary>
    /// <value>The can group selection value exposed by <see cref="EditorStateService"/>.</value>
    public bool CanGroupSelection => SelectedElements.Count(element => element is not ConnectorElement && !element.Locked) > 1;
    /// <summary>
    /// Gets a value indicating whether ungroup selection applies to the editor state state.
    /// </summary>
    /// <value>The can ungroup selection value exposed by <see cref="EditorStateService"/>.</value>
    public bool CanUngroupSelection => SelectedElements.Any(element => element.GroupId is not null);
    /// <summary>
    /// Determines whether selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsSelected(Guid id) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.IsSelected.");
            return _selectedElementIds.Contains(id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.IsSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs new document as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void NewDocument()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NewDocument.");
                    PersistStreamingSettings();
                    RemoveMediaAssets(Document);
                    _liveData.Unregister(Document.Id);
                    Document = _documentFactory.CreatePublication();
                    Document.Streaming = _streamingSettings.LoadOrDefault(Document.Id);
                    _files.NormalizeStreamingSettings(Document);
                    SelectedPageId = Document.Pages[0].Id;
                    ClearSelectionCore();
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    _undo.Clear();
                    _redo.Clear();
                    _liveEditKey = null;
                    _lastInsertionX = null;
                    _lastInsertionY = null;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NewDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs load as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the editor state operation and used when producing its result.</param>
    public void Load(string json)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Load.");
                    PersistStreamingSettings();
                    RemoveMediaAssets(Document);
                    _liveData.Unregister(Document.Id);
                    var hasEmbeddedStreaming = _files.HasEmbeddedStreamingSettings(json);
                    var loaded = _files.Deserialize(json);
                    if (_streamingSettings.TryLoad(loaded.Id, out var localStreaming))
                        loaded.Streaming = localStreaming;
                    else if (hasEmbeddedStreaming)
                    {
                        try { _streamingSettings.Save(loaded.Id, loaded.Streaming); }
                        catch { }
                    }
                    else
                        loaded.Streaming = new PublicationStreamingSettings();
                    _files.NormalizeStreamingSettings(loaded);
                    Document = loaded;
                    _mediaAssets.RegisterDocument(Document);
                    SelectedPageId = Document.Pages[0].Id;
                    ClearSelectionCore();
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    _undo.Clear();
                    _redo.Clear();
                    _liveEditKey = null;
                    _lastInsertionX = null;
                    _lastInsertionY = null;
                    IsDirty = false;
                    Revision++;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Load failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads recovery as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the editor state operation and used when producing its result.</param>
    public void LoadRecovery(string json)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.LoadRecovery.");
                    Load(json);
                    IsDirty = true;
                    Revision++;
                    Changed?.Invoke();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.LoadRecovery failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs mark saved as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void MarkSaved()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.MarkSaved.");
                    IsDirty = false;
                    Revision++;
                    Changed?.Invoke();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.MarkSaved failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs rename document as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the editor state operation and used when producing its result.</param>
    public void RenameDocument(string value)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RenameDocument.");
                    value = string.IsNullOrWhiteSpace(value) ? _systemVariables.DefaultDocumentName : value.Trim();
                    if (string.Equals(Document.Name, value, StringComparison.Ordinal)) return;
                    Capture();
                    Document.Name = value;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RenameDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets publication culture as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the editor state operation and used when producing its result.</param>
    public void SetPublicationCulture(string culture)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPublicationCulture.");
                    culture = string.IsNullOrWhiteSpace(culture) ? _systemVariables.DefaultCulture : culture.Trim();
                    if (string.Equals(Document.ProjectSettings.Culture, culture, StringComparison.OrdinalIgnoreCase)) return;
                    Capture();
                    Document.ProjectSettings.Culture = culture;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPublicationCulture failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets insertion point as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="x">X value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the editor state operation and used when producing its result.</param>
    public void SetInsertionPoint(double x, double y)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetInsertionPoint.");
                    _lastInsertionX = Math.Clamp(x, 0, CurrentPage.WidthMm);
                    _lastInsertionY = Math.Clamp(y, 0, CurrentPage.HeightMm);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetInsertionPoint failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs select page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void SelectPage(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SelectPage.");
                    if (Document.Pages.All(p => p.Id != id)) return;
                    SelectedPageId = id;
                    ClearSelectionCore();
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    EndLiveEdit();
                    _lastInsertionX = null;
                    _lastInsertionY = null;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SelectPage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs select element as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="additive">Value indicating whether additive should apply to this operation.</param>
    public void SelectElement(Guid? id, bool additive = false)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SelectElement.");
                    if (id is null)
                    {
                        if (_selectedElementIds.Count == 0 && SelectedElementId is null) return;
                        ClearSelectionCore();
                        CropMode = false;
                        ContentPanMode = false;
                        EndLiveEdit();
                        Notify(false);
                        return;
                    }

                    var element = CurrentPage.Elements.FirstOrDefault(item => item.Id == id.Value);
                    if (element is null) return;
                    var selection = SelectionUnit(element).Select(item => item.Id).ToHashSet();
                    var previousPrimary = SelectedElementId;
                    var previousSelection = _selectedElementIds.ToHashSet();

                    if (additive)
                    {
                        if (selection.All(_selectedElementIds.Contains))
                            _selectedElementIds.ExceptWith(selection);
                        else
                            _selectedElementIds.UnionWith(selection);
                    }
                    else
                    {
                        _selectedElementIds.Clear();
                        _selectedElementIds.UnionWith(selection);
                    }

                    if (_selectedElementIds.Contains(id.Value)) SelectedElementId = id.Value;
                    else if (previousPrimary is { } previous && _selectedElementIds.Contains(previous)) SelectedElementId = previous;
                    else SelectedElementId = _selectedElementIds.Count > 0 ? _selectedElementIds.Last() : null;

                    var cropChanged = CropMode && (_selectedElementIds.Count != 1 || SelectedElement is not ImageFrameElement);
                    var contentPanChanged = ContentPanMode && (previousPrimary != SelectedElementId || _selectedElementIds.Count != 1 || !CanPanContent(SelectedElement));
                    if (cropChanged) CropMode = false;
                    if (contentPanChanged) ContentPanMode = false;
                    EndLiveEdit();
                    if (!previousSelection.SetEquals(_selectedElementIds) || previousPrimary != SelectedElementId || cropChanged || contentPanChanged) Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SelectElement failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets selection as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ids">Guid dependency used by the editor state workflow to provide the corresponding application capability.</param>
    public void SetSelection(IEnumerable<Guid> ids)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetSelection.");
                    var requested = ids
                        .Distinct()
                        .Select(id => CurrentPage.Elements.FirstOrDefault(element => element.Id == id))
                        .Where(element => element is not null)
                        .Cast<PublicationElement>()
                        .ToList();
                    var expanded = requested
                        .SelectMany(SelectionUnit)
                        .DistinctBy(element => element.Id)
                        .ToList();
                    var previousPrimary = SelectedElementId;
                    var previousSelection = _selectedElementIds.ToHashSet();
                    SetSelectionCore(expanded.Select(element => element.Id), requested.LastOrDefault()?.Id);
                    var cropChanged = CropMode && (_selectedElementIds.Count != 1 || SelectedElement is not ImageFrameElement);
                    var contentPanChanged = ContentPanMode && (previousPrimary != SelectedElementId || _selectedElementIds.Count != 1 || !CanPanContent(SelectedElement));
                    if (cropChanged) CropMode = false;
                    if (contentPanChanged) ContentPanMode = false;
                    EndLiveEdit();
                    if (!previousSelection.SetEquals(_selectedElementIds) || previousPrimary != SelectedElementId || cropChanged || contentPanChanged)
                        Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetSelection failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets primary selection as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void SetPrimarySelection(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPrimarySelection.");
                    if (!_selectedElementIds.Contains(id))
                    {
                        SelectElement(id);
                        return;
                    }
                    if (SelectedElementId == id) return;
                    SelectedElementId = id;
                    if (CropMode && (_selectedElementIds.Count != 1 || SelectedElement is not ImageFrameElement)) CropMode = false;
                    if (ContentPanMode) ContentPanMode = false;
                    EndLiveEdit();
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPrimarySelection failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs group selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void GroupSelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.GroupSelected.");
                    var elements = SelectedElements.Where(element => element is not ConnectorElement && !element.Locked).ToList();
                    if (elements.Count < 2) return;
                    Capture();
                    var groupId = Guid.NewGuid();
                    foreach (var element in elements) element.GroupId = groupId;
                    SetSelectionCore(elements.Select(element => element.Id), SelectedElementId);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.GroupSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs ungroup selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void UngroupSelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UngroupSelected.");
                    var selectedIds = _selectedElementIds.ToArray();
                    var groupIds = SelectedElements.Where(element => element.GroupId is not null).Select(element => element.GroupId!.Value).ToHashSet();
                    if (groupIds.Count == 0) return;
                    Capture();
                    var affected = CurrentPage.Elements.Where(element => element.GroupId is { } groupId && groupIds.Contains(groupId)).ToList();
                    foreach (var element in affected) element.GroupId = null;
                    SetSelectionCore(selectedIds, SelectedElementId);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UngroupSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds text as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The text frame element produced by the operation.</returns>
    public TextFrameElement AddText(double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddText.");
                    Capture();
                    var element = new TextFrameElement
                    {
                        Name = NextName("Text Box"),
                        X = 25,
                        Y = 25,
                        Width = 90,
                        Height = 45,
                        ZIndex = NextZ(),
                        PreviewHtml = "<p style=\"margin:0;font:12pt Segoe UI\">New text box</p>",
                        DocumentContent = _richTextFactory.CreateOpenXml("New text box"),
                        StoryFormat = StoryStorageFormat.OpenXml
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddText failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds spreadsheet as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The spreadsheet element produced by the operation.</returns>
    public SpreadsheetElement AddSpreadsheet(byte[] content, string fileName, SpreadsheetStorageFormat format, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddSpreadsheet.");
                    _spreadsheets.ValidateWorkbookContent(content, format);
                    Capture();
                    var preview = _spreadsheets.RenderPreviewHtml(content, format, out var activeSheetName);
                    var element = new SpreadsheetElement
                    {
                        Name = NextName(string.IsNullOrWhiteSpace(fileName) ? "Spreadsheet" : Path.GetFileNameWithoutExtension(fileName)),
                        WorkbookContent = content.ToArray(),
                        WorkbookFileName = _spreadsheets.NormalizeWorkbookFileName(fileName, format),
                        StorageFormat = format,
                        PreviewHtml = preview,
                        ActiveSheetName = activeSheetName,
                        X = 28,
                        Y = 35,
                        Width = 125,
                        Height = 78,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddSpreadsheet failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds panel as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The panel element produced by the operation.</returns>
    public PanelElement AddPanel(string presetId = "blank", double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddPanel.");
                    Capture();
                    var element = _panels.CreatePreset(Document, presetId);
                    element.Name = NextName(element.Name);
                    element.ZIndex = NextZ();
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddPanel failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds HTML embed as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The HTML embed element produced by the operation.</returns>
    public HtmlEmbedElement AddHtmlEmbed(double? centerX = null, double? centerY = null) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddHtmlEmbed.");
            return AddHtmlEmbed(_ => { }, centerX, centerY);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddHtmlEmbed failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds HTML embed as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configure">Configure value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The HTML embed element produced by the operation.</returns>
    public HtmlEmbedElement AddHtmlEmbed(Action<HtmlEmbedElement> configure, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddHtmlEmbed.");
                    ArgumentNullException.ThrowIfNull(configure);
                    Capture();
                    var element = new HtmlEmbedElement
                    {
                        Name = NextName("Web Content"),
                        X = 30,
                        Y = 35,
                        Width = 120,
                        Height = 72,
                        ZIndex = NextZ()
                    };
                    configure(element);
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddHtmlEmbed failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies selected panel as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="draft">Draft value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool ApplySelectedPanel(PanelElement draft)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ApplySelectedPanel.");
                    if (SelectedElement is not PanelElement selected || selected.Locked) return false;
                    Capture();
                    var replacement = (PanelElement)_files.CloneElement(draft);
                    replacement.Id = selected.Id;
                    replacement.X = selected.X;
                    replacement.Y = selected.Y;
                    replacement.Width = selected.Width;
                    replacement.Height = selected.Height;
                    replacement.Rotation = selected.Rotation;
                    replacement.ZIndex = selected.ZIndex;
                    replacement.Visible = selected.Visible;
                    replacement.Locked = selected.Locked;
                    replacement.HiddenAtPresentationStart = selected.HiddenAtPresentationStart;
                    replacement.GroupId = selected.GroupId;
                    replacement.Animations = selected.Animations;
                    replacement.Interaction = selected.Interaction;
                    replacement.ConnectorPorts = selected.ConnectorPorts;
                    _panels.Normalize(Document, replacement);
                    var index = CurrentPage.Elements.FindIndex(element => element.Id == selected.Id);
                    if (index < 0) return false;
                    CurrentPage.Elements[index] = replacement;
                    SetSelectionCore([replacement.Id], replacement.Id);
                    Notify();
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ApplySelectedPanel failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets panel library visible as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="visible">Value indicating whether visible should apply to this operation.</param>
    public void SetPanelLibraryVisible(bool visible)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPanelLibraryVisible.");
                    if (Document.View.PanelLibraryVisible == visible) return;
                    Capture();
                    Document.View.PanelLibraryVisible = visible;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPanelLibraryVisible failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies selected HTML embed as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="draft">Draft value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool ApplySelectedHtmlEmbed(HtmlEmbedElement draft)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ApplySelectedHtmlEmbed.");
                    if (SelectedElement is not HtmlEmbedElement selected || selected.Locked) return false;
                    Capture();
                    selected.Name = draft.Name;
                    selected.Html = draft.Html;
                    selected.Css = draft.Css;
                    selected.JavaScript = draft.JavaScript;
                    selected.AllowScripts = draft.AllowScripts;
                    selected.AllowForms = draft.AllowForms;
                    selected.AllowPopups = draft.AllowPopups;
                    selected.AllowSameOrigin = draft.AllowSameOrigin;
                    selected.AllowTopNavigation = draft.AllowTopNavigation;
                    selected.Background = draft.Background;
                    selected.HtmlExportSupport = draft.HtmlExportSupport;
                    selected.HtmlExportNote = draft.HtmlExportNote;
                    selected.InterchangeFormat = draft.InterchangeFormat;
                    Notify();
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ApplySelectedHtmlEmbed failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Promotes a standalone HTML/DIV object to a full reusable panel when Panel Studio
    /// contains additional authored objects or meaningful panel-local geometry. The outer
    /// Mainframe bounds and interaction metadata stay stable while the complete panel graph
    /// replaces the original object.
    /// </summary>
    /// <param name="draft">Draft value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool PromoteSelectedHtmlEmbedToPanel(PanelElement draft)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.PromoteSelectedHtmlEmbedToPanel.");
                    if (SelectedElement is not HtmlEmbedElement selected || selected.Locked) return false;
                    Capture();
                    var replacement = (PanelElement)_files.CloneElement(draft);
                    replacement.Id = selected.Id;
                    replacement.Name = string.IsNullOrWhiteSpace(draft.Name) ? selected.Name : draft.Name;
                    replacement.X = selected.X;
                    replacement.Y = selected.Y;
                    replacement.Width = selected.Width;
                    replacement.Height = selected.Height;
                    replacement.Rotation = selected.Rotation;
                    replacement.ZIndex = selected.ZIndex;
                    replacement.Visible = selected.Visible;
                    replacement.Locked = selected.Locked;
                    replacement.HiddenAtPresentationStart = selected.HiddenAtPresentationStart;
                    replacement.GroupId = selected.GroupId;
                    replacement.Animations = selected.Animations;
                    replacement.Interaction = selected.Interaction;
                    replacement.ConnectorPorts = selected.ConnectorPorts;
                    _panels.Normalize(Document, replacement);
                    var index = CurrentPage.Elements.FindIndex(element => element.Id == selected.Id);
                    if (index < 0) return false;
                    CurrentPage.Elements[index] = replacement;
                    SetSelectionCore([replacement.Id], replacement.Id);
                    Notify();
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.PromoteSelectedHtmlEmbedToPanel failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds image as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pictureSource">Picture source value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pixelWidth">Pixel width value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pixelHeight">Pixel height value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The image frame element produced by the operation.</returns>
    public ImageFrameElement AddImage(string dataUrl, string name, PictureDocument? pictureSource = null, double? centerX = null, double? centerY = null, int pixelWidth = 0, int pixelHeight = 0)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddImage.");
                    Capture();
                    var element = new ImageFrameElement
                    {
                        Name = NextName(name),
                        DataUrl = dataUrl,
                        OriginalDataUrl = dataUrl,
                        PictureSource = pictureSource,
                        AltText = name,
                        X = 30,
                        Y = 35,
                        Width = 90,
                        Height = pictureSource is { WidthPx: > 0, HeightPx: > 0 }
                            ? Math.Clamp(90d * pictureSource.HeightPx / pictureSource.WidthPx, 20, 140)
                            : pixelWidth > 0 && pixelHeight > 0
                                ? Math.Clamp(90d * pixelHeight / pixelWidth, 20, 140)
                                : 65,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddImage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds video as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="durationSeconds">Duration seconds value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="posterDataUrl">Poster data url value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The video element produced by the operation.</returns>
    public VideoElement AddVideo(string dataUrl, string mimeType, string name, double durationSeconds, string posterDataUrl = "", double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddVideo.");
                    Capture();
                    mimeType = _mediaData.NormalizeMimeType(mimeType, "video/webm");
                    dataUrl = _mediaData.NormalizeDataUrl(dataUrl, mimeType);
                    var element = new VideoElement
                    {
                        Name = NextName(string.IsNullOrWhiteSpace(name) ? "Video" : name),
                        DataUrl = dataUrl,
                        MimeType = mimeType,
                        DurationSeconds = Math.Max(0, durationSeconds),
                        TrimEndSeconds = Math.Max(0, durationSeconds),
                        PosterDataUrl = posterDataUrl,
                        AltText = string.IsNullOrWhiteSpace(name) ? "Video" : name,
                        X = 28,
                        Y = 32,
                        Width = 120,
                        Height = 67.5,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    EnsureTimelineDuration();
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddVideo failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds audio as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="durationSeconds">Duration seconds value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="waveformSamples">Double dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The audio element produced by the operation.</returns>
    public AudioElement AddAudio(string dataUrl, string mimeType, string name, double durationSeconds, IReadOnlyList<double>? waveformSamples = null, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddAudio.");
                    Capture();
                    mimeType = _mediaData.NormalizeMimeType(mimeType, "audio/webm");
                    dataUrl = _mediaData.NormalizeDataUrl(dataUrl, mimeType);
                    var element = new AudioElement
                    {
                        Name = NextName(string.IsNullOrWhiteSpace(name) ? "Audio" : name),
                        DataUrl = dataUrl,
                        MimeType = mimeType,
                        DurationSeconds = Math.Max(0, durationSeconds),
                        TrimEndSeconds = Math.Max(0, durationSeconds),
                        WaveformSamples = waveformSamples?.Select(value => Math.Clamp(value, 0, 1)).Take(256).ToList() ?? [],
                        X = 28,
                        Y = 42,
                        Width = 120,
                        Height = 28,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    EnsureTimelineDuration();
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddAudio failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds live source as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The live source element produced by the operation.</returns>
    public LiveSourceElement AddLiveSource(PublicationLiveSourceKind kind, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddLiveSource.");
                    Capture();
                    var visual = kind is PublicationLiveSourceKind.Camera or PublicationLiveSourceKind.Screen or PublicationLiveSourceKind.Window
                        or PublicationLiveSourceKind.BrowserTab or PublicationLiveSourceKind.CaptureDevice or PublicationLiveSourceKind.NetworkMedia;
                    var element = new LiveSourceElement
                    {
                        Name = NextName(kind switch
                        {
                            PublicationLiveSourceKind.BrowserTab => "Browser Tab",
                            PublicationLiveSourceKind.CaptureDevice => "Capture Device",
                            PublicationLiveSourceKind.ApplicationAudio => "Application Audio",
                            PublicationLiveSourceKind.SystemAudio => "System Audio",
                            PublicationLiveSourceKind.NetworkMedia => "Network Media",
                            PublicationLiveSourceKind.NowPlaying => "Now Playing",
                            _ => kind.ToString()
                        }),
                        SourceKind = kind,
                        IncludeAudio = kind is PublicationLiveSourceKind.Screen or PublicationLiveSourceKind.Window or PublicationLiveSourceKind.BrowserTab
                            or PublicationLiveSourceKind.CaptureDevice or PublicationLiveSourceKind.NetworkMedia,
                        Muted = visual,
                        X = 28,
                        Y = 32,
                        Width = visual ? 120 : 90,
                        Height = visual ? 67.5 : 20,
                        ZIndex = NextZ(),
                        UseDeviceTimestamp = Document.Streaming.PreferDeviceTimestamps,
                        CaptureWidth = Document.Streaming.MasterWidth,
                        CaptureHeight = Document.Streaming.MasterHeight,
                        CaptureFrameRate = Document.Streaming.MasterFrameRate
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddLiveSource failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates live source as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="capture">Value indicating whether capture should apply to this operation.</param>
    public void UpdateLiveSource(Guid id, Action<LiveSourceElement> update, bool capture = true)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateLiveSource.");
                    var source = CurrentPage.Elements.OfType<LiveSourceElement>().FirstOrDefault(item => item.Id == id);
                    if (source is null || source.Locked) return;
                    if (capture) Capture();
                    update(source);
                    source.CaptureWidth = Math.Clamp(source.CaptureWidth, 320, 7680);
                    source.CaptureHeight = Math.Clamp(source.CaptureHeight, 180, 4320);
                    source.CaptureFrameRate = Math.Clamp(source.CaptureFrameRate, 15, 120);
                    source.Volume = Math.Clamp(source.Volume, 0, 1);
                    source.AudioDelayMilliseconds = Math.Clamp(source.AudioDelayMilliseconds, -10000, 10000);
                    source.Brightness = Math.Clamp(source.Brightness, 0, 4);
                    source.Contrast = Math.Clamp(source.Contrast, 0, 4);
                    source.Saturation = Math.Clamp(source.Saturation, 0, 4);
                    source.HueRotation = Math.Clamp(source.HueRotation, -360, 360);
                    source.Blur = Math.Clamp(source.Blur, 0, 64);
                    source.ChromaSimilarity = Math.Clamp(source.ChromaSimilarity, 0, 1);
                    source.ChromaSmoothness = Math.Clamp(source.ChromaSmoothness, 0, 1);
                    source.ChromaSpill = Math.Clamp(source.ChromaSpill, 0, 1);
                    source.ChromaResidualOpacity = Math.Clamp(source.ChromaResidualOpacity, 0, 1);
                    SetSelectionCore([source.Id], source.Id);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateLiveSource failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies streaming settings as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    public void ApplyStreamingSettings(PublicationStreamingSettings settings)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ApplyStreamingSettings.");
                    ArgumentNullException.ThrowIfNull(settings);
                    Document.Streaming = settings;
                    _files.NormalizeStreamingSettings(Document);
                    PersistStreamingSettings();
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ApplyStreamingSettings failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates media as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="capture">Value indicating whether capture should apply to this operation.</param>
    public void UpdateMedia(Guid id, Action<PublicationMediaElement> update, bool capture = true)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateMedia.");
                    var media = CurrentPage.Elements.OfType<PublicationMediaElement>().FirstOrDefault(item => item.Id == id);
                    if (media is null || media.Locked) return;
                    if (capture) Capture();
                    update(media);
                    NormalizeMedia(media);
                    EnsureTimelineDuration();
                    SetSelectionCore([media.Id], media.Id);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateMedia failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates media live as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="key">Key value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateMediaLive(Guid id, string key, Action<PublicationMediaElement> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateMediaLive.");
                    var media = CurrentPage.Elements.OfType<PublicationMediaElement>().FirstOrDefault(item => item.Id == id);
                    if (media is null || media.Locked) return;
                    var liveKey = $"media:{id}:{key}";
                    if (!string.Equals(_liveEditKey, liveKey, StringComparison.Ordinal))
                    {
                        Capture();
                        _liveEditKey = liveKey;
                    }
                    update(media);
                    NormalizeMedia(media);
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateMediaLive failed: {exception.Message}");
            throw;
        }
    }



    /// <summary>
    /// Adds word art as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The word art element produced by the operation.</returns>
    public WordArtElement AddWordArt(double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddWordArt.");
                    Capture();
                    var element = new WordArtElement
                    {
                        Name = NextName("WordArt"),
                        Text = "Your headline",
                        X = 25,
                        Y = 28,
                        Width = 120,
                        Height = 35,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddWordArt failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures data object as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The publication data object produced by the operation.</returns>
    public PublicationDataObject EnsureDataObject()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.EnsureDataObject.");
                    if (Document.DataObjects.Count > 0) return Document.DataObjects[0];
                    var data = _data.CreateSample();
                    Document.DataObjects.Add(data);
                    return data;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.EnsureDataObject failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds barcode as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The barcode element produced by the operation.</returns>
    public BarcodeElement AddBarcode(double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddBarcode.");
                    Capture();
                    var element = new BarcodeElement
                    {
                        Name = NextName("Barcode"),
                        X = 42,
                        Y = 42,
                        Width = 70,
                        Height = 70,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddBarcode failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds data visual as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The data visual element produced by the operation.</returns>
    public DataVisualElement AddDataVisual(DataVisualKind kind, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddDataVisual.");
                    Capture();
                    var data = EnsureDataObject();
                    var columns = _data.ResolveColumns(data);
                    var argument = columns.FirstOrDefault()?.Name ?? string.Empty;
                    var numericColumns = columns.Where(column => column.ValueKind is PublicationDataValueKind.Number or PublicationDataValueKind.Boolean).Select(column => column.Name).ToArray();
                    var numeric = numericColumns.FirstOrDefault()
                        ?? columns.Skip(1).FirstOrDefault()?.Name
                        ?? argument;
                    var element = new DataVisualElement
                    {
                        Name = NextName(DataVisualName(kind)),
                        Title = DataVisualName(kind),
                        VisualKind = kind,
                        DataObjectId = data.Id,
                        ArgumentField = argument,
                        TargetField = columns.Skip(1).FirstOrDefault()?.Name ?? argument,
                        ValueFields = string.IsNullOrWhiteSpace(numeric) ? [] : [numeric],
                        OpenValueField = numericColumns.ElementAtOrDefault(0) ?? numeric,
                        HighValueField = numericColumns.ElementAtOrDefault(1) ?? numeric,
                        LowValueField = numericColumns.ElementAtOrDefault(2) ?? numeric,
                        CloseValueField = numericColumns.ElementAtOrDefault(3) ?? numeric,
                        SizeField = numericColumns.ElementAtOrDefault(1) ?? numeric,
                        X = 28,
                        Y = 30,
                        Width = kind switch
                        {
                            DataVisualKind.Sparkline => 120,
                            DataVisualKind.KpiProgress => 120,
                            DataVisualKind.LinearGauge => 145,
                            DataVisualKind.DataTable => 150,
                            _ => 145
                        },
                        Height = kind switch
                        {
                            DataVisualKind.Sparkline => 34,
                            DataVisualKind.KpiProgress => 40,
                            DataVisualKind.LinearGauge => 42,
                            DataVisualKind.DataTable => 90,
                            _ => 95
                        },
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddDataVisual failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds dev extreme component as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The dev extreme component element produced by the operation.</returns>
    public DevExtremeComponentElement AddDevExtremeComponent(PublicationComponentKind kind, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddDevExtremeComponent.");
                    Capture();
                    var element = _components.Create(Document, kind);
                    element.Name = NextName(_components.ComponentName(kind));
                    element.ZIndex = NextZ();
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddDevExtremeComponent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies selected component as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="draft">Draft value supplied to the editor state operation and used when producing its result.</param>
    public void ApplySelectedComponent(DevExtremeComponentElement draft)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ApplySelectedComponent.");
                    if (SelectedElement is not DevExtremeComponentElement selected || selected.Locked) return;
                    Capture();
                    var priorSharedId = selected.SharedComponentId;
                    _components.CopyConfiguration(draft, selected, preservePlacement: true);
                    _components.Normalize(Document, selected);
                    if (selected.Scope == PublicationComponentScope.Document)
                    {
                        selected.SharedComponentId ??= priorSharedId ?? Guid.NewGuid();
                        SynchronizeDocumentComponent(selected);
                    }
                    else
                    {
                        if (priorSharedId is { } sharedId)
                        {
                            foreach (var page in Document.Pages.Where(page => page.Id != CurrentPage.Id))
                                page.Elements.RemoveAll(element => element is DevExtremeComponentElement component && component.SharedComponentId == sharedId);
                        }
                        selected.SharedComponentId = null;
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ApplySelectedComponent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets selected component scope as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="scope">Scope value supplied to the editor state operation and used when producing its result.</param>
    public void SetSelectedComponentScope(PublicationComponentScope scope)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetSelectedComponentScope.");
                    if (SelectedElement is not DevExtremeComponentElement selected || selected.Locked || selected.Scope == scope) return;
                    Capture();
                    var priorSharedId = selected.SharedComponentId;
                    selected.Scope = scope;
                    if (scope == PublicationComponentScope.Document)
                    {
                        selected.SharedComponentId ??= priorSharedId ?? Guid.NewGuid();
                        _components.Normalize(Document, selected);
                        SynchronizeDocumentComponent(selected);
                    }
                    else
                    {
                        if (priorSharedId is { } sharedId)
                        {
                            foreach (var page in Document.Pages.Where(page => page.Id != CurrentPage.Id))
                                page.Elements.RemoveAll(element => element is DevExtremeComponentElement component && component.SharedComponentId == sharedId);
                        }
                        selected.SharedComponentId = null;
                        _components.Normalize(Document, selected);
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetSelectedComponentScope failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs upsert data object as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the editor state operation and used when producing its result.</param>
    public void UpsertDataObject(PublicationDataObject value)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpsertDataObject.");
                    Capture();
                    var normalized = _data.Clone(value);
                    _data.ParseInto(normalized);
                    var index = Document.DataObjects.FindIndex(data => data.Id == normalized.Id);
                    if (index < 0) Document.DataObjects.Add(normalized);
                    else Document.DataObjects[index] = normalized;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpsertDataObject failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes data object as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool DeleteDataObject(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DeleteDataObject.");
                    if (Document.Pages.SelectMany(page => page.Elements).OfType<DataVisualElement>().Any(item => item.DataObjectId == id)) return false;
                    if (Document.Pages.SelectMany(page => page.Elements).OfType<DevExtremeComponentElement>().Any(item =>
                        item.Connection.DataObjectId == id
                        || item.Panels.Any(panel => panel.DataObjectId == id)
                        || item.Fields.Any(field => field.LookupDataObjectId == id))) return false;
                    var index = Document.DataObjects.FindIndex(data => data.Id == id);
                    if (index < 0) return false;
                    Capture();
                    Document.DataObjects.RemoveAt(index);
                    Notify();
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DeleteDataObject failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Refreshes data visuals as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void RefreshDataVisuals() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RefreshDataVisuals.");
            Notify(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RefreshDataVisuals failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a value indicating whether due web data applies to the editor state state.
    /// </summary>
    /// <value>The has due web data value exposed by <see cref="EditorStateService"/>.</value>
    public bool HasDueWebData => Document.DataObjects.Any(data => _webData.IsDue(data, DateTimeOffset.UtcNow));

    /// <summary>
    /// Refreshes web data as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataId">Identifier of the data to use for this operation.</param>
    /// <param name="force">Value indicating whether force should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RefreshWebDataAsync(Guid? dataId = null, bool force = true, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RefreshWebDataAsync.");
                    var candidates = Document.DataObjects
                        .Where(data => data.SourceKind == PublicationDataSourceKind.Web
                            && data.Web.Enabled
                            && (dataId is null || data.Id == dataId.Value)
                            && (force || _webData.IsDue(data, DateTimeOffset.UtcNow)))
                        .ToArray();
                    await RefreshWebDataObjectsAsync(candidates, cancellationToken).ConfigureAwait(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RefreshWebDataAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Refreshes web data on open as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task RefreshWebDataOnOpenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogTrace($"Entering EditorStateService.RefreshWebDataOnOpenAsync.");
                return RefreshWebDataObjectsAsync(Document.DataObjects
            .Where(data => data.SourceKind == PublicationDataSourceKind.Web
                && data.Web.Enabled
                && data.Web.RefreshOnOpen)
            .ToArray(), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"EditorStateService.RefreshWebDataOnOpenAsync failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Refreshes web data objects as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="candidates">Publication data object dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RefreshWebDataObjectsAsync(IReadOnlyList<PublicationDataObject> candidates, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RefreshWebDataObjectsAsync.");
                    if (candidates.Count == 0) return;
                    foreach (var data in candidates)
                    {
                        try { await _webData.RefreshAsync(data, cancellationToken).ConfigureAwait(false); }
                        catch when (data.Web.UseSnapshotOnFailure && data.Rows.Count > 0) { }
                    }
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RefreshWebDataObjectsAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs data visual name as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DataVisualName(DataVisualKind kind) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DataVisualName.");
            return kind switch
    {
        DataVisualKind.CartesianChart => "Chart",
        DataVisualKind.PieChart => "Pie Chart",
        DataVisualKind.PolarChart => "Polar Chart",
        DataVisualKind.Sparkline => "Sparkline",
        DataVisualKind.BarGauge => "Bar Gauge",
        DataVisualKind.CircularGauge => "Circular Gauge",
        DataVisualKind.LinearGauge => "Linear Gauge",
        DataVisualKind.RangeSelector => "Range Selector",
        DataVisualKind.Sankey => "Sankey Diagram",
        DataVisualKind.Funnel => "Funnel",
        DataVisualKind.Pyramid => "Pyramid",
        DataVisualKind.TreeMap => "Tree Map",
        DataVisualKind.DataTable => "Data Table",
        DataVisualKind.KpiProgress => "KPI",
        _ => "Data Visual"
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DataVisualName failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds connector port as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="xPercent">X percent value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="yPercent">Y percent value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The publication connector port produced by the operation.</returns>
    public PublicationConnectorPort? AddConnectorPort(Guid elementId, double xPercent = .5, double yPercent = .5)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddConnectorPort.");
                    var element = CurrentPage.Elements.FirstOrDefault(item => item.Id == elementId && item is not ConnectorElement);
                    if (element is null || element.Locked) return null;
                    Capture();
                    element.ConnectorPorts ??= [];
                    var port = new PublicationConnectorPort
                    {
                        Name = $"Connector point {element.ConnectorPorts.Count + 1}",
                        XPercent = Math.Clamp(xPercent, 0, 1),
                        YPercent = Math.Clamp(yPercent, 0, 1)
                    };
                    element.ConnectorPorts.Add(port);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return port;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddConnectorPort failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Removes connector port as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="portId">Identifier of the port to use for this operation.</param>
    public void RemoveConnectorPort(Guid elementId, Guid portId)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RemoveConnectorPort.");
                    var element = CurrentPage.Elements.FirstOrDefault(item => item.Id == elementId && item is not ConnectorElement);
                    var port = element?.ConnectorPorts.FirstOrDefault(candidate => candidate.Id == portId);
                    if (element is null || port is null || element.Locked) return;
                    Capture();
                    element.ConnectorPorts.Remove(port);
                    foreach (var connector in CurrentPage.Elements.OfType<ConnectorElement>())
                    {
                        if (connector.Source.ElementId == elementId && connector.Source.PortId == portId)
                        {
                            connector.Source.PortId = null;
                            connector.Source.Anchor = ConnectorAnchor.Center;
                        }
                        if (connector.Target.ElementId == elementId && connector.Target.PortId == portId)
                        {
                            connector.Target.PortId = null;
                            connector.Target.Anchor = ConnectorAnchor.Center;
                        }
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RemoveConnectorPort failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds connector as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceElementId">Identifier of the source element to use for this operation.</param>
    /// <param name="sourceAnchor">Source anchor value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="targetElementId">Identifier of the target element to use for this operation.</param>
    /// <param name="targetAnchor">Target anchor value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="endMarker">End marker value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The connector element produced by the operation.</returns>
    public ConnectorElement? AddConnector(Guid sourceElementId, ConnectorAnchor sourceAnchor, Guid targetElementId, ConnectorAnchor targetAnchor, ConnectorMarker endMarker = ConnectorMarker.Arrow)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddConnector.");
                    if (sourceElementId == targetElementId) return null;
                    return AddConnectorAdvanced(
                        new ConnectorEndpoint { Kind = ConnectorEndpointKind.Element, ElementId = sourceElementId, Anchor = sourceAnchor },
                        new ConnectorEndpoint { Kind = ConnectorEndpointKind.Element, ElementId = targetElementId, Anchor = targetAnchor },
                        endMarker,
                        signal: false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddConnector failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds signal connector as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="endMarker">End marker value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The connector element produced by the operation.</returns>
    public ConnectorElement? AddSignalConnector(ConnectorEndpoint source, ConnectorEndpoint target, ConnectorMarker endMarker = ConnectorMarker.Arrow)
        {
            try
            {
                logger.LogTrace($"Entering EditorStateService.AddSignalConnector.");
                return AddConnectorAdvanced(source, target, endMarker, signal: true);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"EditorStateService.AddSignalConnector failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Adds connector advanced as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="endMarker">End marker value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="signal">Value indicating whether signal should apply to this operation.</param>
    /// <param name="sourcePort">Source port value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="targetPort">Target port value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The connector element produced by the operation.</returns>
    public ConnectorElement? AddConnectorAdvanced(
        ConnectorEndpoint source,
        ConnectorEndpoint target,
        ConnectorMarker endMarker,
        bool signal,
        PublicationConnectorPort? sourcePort = null,
        PublicationConnectorPort? targetPort = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddConnectorAdvanced.");
                    if (!TryPrepareEndpoint(source, sourcePort, out var sourceOwner) ||
                        !TryPrepareEndpoint(target, targetPort, out var targetOwner)) return null;
                    if (source.Kind == ConnectorEndpointKind.Element && target.Kind == ConnectorEndpointKind.Element &&
                        source.ElementId != Guid.Empty && source.ElementId == target.ElementId) return null;

                    Capture();
                    AttachPendingPort(source, sourceOwner, sourcePort);
                    AttachPendingPort(target, targetOwner, targetPort);
                    var connector = new ConnectorElement
                    {
                        Name = NextName(signal ? (endMarker == ConnectorMarker.None ? "Signal Connector" : "Signal Arrow") : (endMarker == ConnectorMarker.None ? "Connector" : "Arrow Connector")),
                        Source = source,
                        Target = target,
                        EndMarker = endMarker,
                        PathKind = ConnectorPathKind.Curved,
                        ZIndex = NextZ(),
                        Signal = new SignalConnectorSettings
                        {
                            Enabled = signal,
                            LineVisible = true,
                            Trigger = signal ? SignalConnectorTrigger.OnPageEnter : SignalConnectorTrigger.Manual,
                            Visual = signal ? SignalConnectorVisual.FlyingArrow : SignalConnectorVisual.None
                        }
                    };
                    CurrentPage.Elements.Add(connector);
                    SetSelectionCore([connector.Id], connector.Id);
                    Notify();
                    return connector;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddConnectorAdvanced failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to prepare endpoint as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pendingPort">Pending port value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="owner">Owner value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryPrepareEndpoint(ConnectorEndpoint endpoint, PublicationConnectorPort? pendingPort, out PublicationElement? owner)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.TryPrepareEndpoint.");
                    owner = null;
                    if (endpoint.Kind == ConnectorEndpointKind.Canvas)
                    {
                        endpoint.ElementId = Guid.Empty;
                        endpoint.PortId = null;
                        endpoint.X = Math.Clamp(endpoint.X, 0, CurrentPage.WidthMm);
                        endpoint.Y = Math.Clamp(endpoint.Y, 0, CurrentPage.HeightMm);
                        return true;
                    }

                    owner = CurrentPage.Elements.FirstOrDefault(item => item.Id == endpoint.ElementId && item is not ConnectorElement);
                    if (owner is null || owner.Locked) return false;
                    owner.ConnectorPorts ??= [];
                    if (pendingPort is not null)
                    {
                        pendingPort.Id = pendingPort.Id == Guid.Empty ? Guid.NewGuid() : pendingPort.Id;
                        pendingPort.XPercent = Math.Clamp(pendingPort.XPercent, 0, 1);
                        pendingPort.YPercent = Math.Clamp(pendingPort.YPercent, 0, 1);
                        endpoint.PortId = pendingPort.Id;
                        endpoint.Anchor = ConnectorAnchor.Center;
                        return true;
                    }
                    if (endpoint.PortId is { } portId && owner.ConnectorPorts.All(port => port.Id != portId)) endpoint.PortId = null;
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.TryPrepareEndpoint failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs attach pending port as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="owner">Owner value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pendingPort">Pending port value supplied to the editor state operation and used when producing its result.</param>
    private void AttachPendingPort(ConnectorEndpoint endpoint, PublicationElement? owner, PublicationConnectorPort? pendingPort)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AttachPendingPort.");
                    if (endpoint.Kind != ConnectorEndpointKind.Element || owner is null || pendingPort is null) return;
                    owner.ConnectorPorts ??= [];
                    if (owner.ConnectorPorts.All(port => port.Id != pendingPort.Id)) owner.ConnectorPorts.Add(pendingPort);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AttachPendingPort failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs reconnect connector as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to use for this operation.</param>
    /// <param name="sourceEnd">Value indicating whether source end should apply to this operation.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="anchor">Anchor value supplied to the editor state operation and used when producing its result.</param>
    public void ReconnectConnector(Guid connectorId, bool sourceEnd, Guid elementId, ConnectorAnchor anchor)
        {
            try
            {
                logger.LogTrace($"Entering EditorStateService.ReconnectConnector.");
                ReconnectConnectorEndpoint(connectorId, sourceEnd, new ConnectorEndpoint
        {
            Kind = ConnectorEndpointKind.Element,
            ElementId = elementId,
            Anchor = anchor
        });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"EditorStateService.ReconnectConnector failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Performs reconnect connector endpoint as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to use for this operation.</param>
    /// <param name="sourceEnd">Value indicating whether source end should apply to this operation.</param>
    /// <param name="replacement">Replacement value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="pendingPort">Pending port value supplied to the editor state operation and used when producing its result.</param>
    public void ReconnectConnectorEndpoint(Guid connectorId, bool sourceEnd, ConnectorEndpoint replacement, PublicationConnectorPort? pendingPort = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ReconnectConnectorEndpoint.");
                    var connector = CurrentPage.Elements.OfType<ConnectorElement>().FirstOrDefault(item => item.Id == connectorId);
                    if (connector is null || connector.Locked || !TryPrepareEndpoint(replacement, pendingPort, out var owner)) return;
                    var other = sourceEnd ? connector.Target : connector.Source;
                    if (replacement.Kind == ConnectorEndpointKind.Element &&
                        other.Kind == ConnectorEndpointKind.Element &&
                        replacement.ElementId != Guid.Empty &&
                        replacement.ElementId == other.ElementId) return;

                    Capture();
                    AttachPendingPort(replacement, owner, pendingPort);
                    if (sourceEnd) connector.Source = replacement;
                    else connector.Target = replacement;
                    SetSelectionCore([connector.Id], connector.Id);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ReconnectConnectorEndpoint failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit connector control as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to use for this operation.</param>
    /// <param name="controlIndex">Control index value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the editor state operation and used when producing its result.</param>
    public void CommitConnectorControl(Guid connectorId, int controlIndex, double x, double y)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitConnectorControl.");
                    var connector = CurrentPage.Elements.OfType<ConnectorElement>().FirstOrDefault(item => item.Id == connectorId);
                    if (connector is null || connector.Locked || controlIndex is < 1 or > 2) return;
                    Capture();
                    connector.PathKind = ConnectorPathKind.Curved;
                    if (controlIndex == 1)
                    {
                        connector.Control1X = Math.Clamp(x, 0, CurrentPage.WidthMm);
                        connector.Control1Y = Math.Clamp(y, 0, CurrentPage.HeightMm);
                    }
                    else
                    {
                        connector.Control2X = Math.Clamp(x, 0, CurrentPage.WidthMm);
                        connector.Control2Y = Math.Clamp(y, 0, CurrentPage.HeightMm);
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitConnectorControl failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit connector route as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to use for this operation.</param>
    /// <param name="control1X">Control1 x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="control1Y">Control1 y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="control2X">Control2 x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="control2Y">Control2 y value supplied to the editor state operation and used when producing its result.</param>
    public void CommitConnectorRoute(Guid connectorId, double control1X, double control1Y, double control2X, double control2Y)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitConnectorRoute.");
                    var connector = CurrentPage.Elements.OfType<ConnectorElement>().FirstOrDefault(item => item.Id == connectorId);
                    if (connector is null || connector.Locked) return;
                    Capture();
                    connector.PathKind = ConnectorPathKind.Curved;
                    connector.Control1X = Math.Clamp(control1X, 0, CurrentPage.WidthMm);
                    connector.Control1Y = Math.Clamp(control1Y, 0, CurrentPage.HeightMm);
                    connector.Control2X = Math.Clamp(control2X, 0, CurrentPage.WidthMm);
                    connector.Control2Y = Math.Clamp(control2Y, 0, CurrentPage.HeightMm);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitConnectorRoute failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs reset connector route as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to use for this operation.</param>
    public void ResetConnectorRoute(Guid connectorId)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ResetConnectorRoute.");
                    var connector = CurrentPage.Elements.OfType<ConnectorElement>().FirstOrDefault(item => item.Id == connectorId);
                    if (connector is null || connector.Locked) return;
                    Capture();
                    connector.Control1X = connector.Control1Y = connector.Control2X = connector.Control2Y = null;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ResetConnectorRoute failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets connector tool as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tool">Tool value supplied to the editor state operation and used when producing its result.</param>
    public void SetConnectorTool(ConnectorToolKind tool)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetConnectorTool.");
                    ConnectorTool = ConnectorTool == tool ? ConnectorToolKind.None : tool;
                    CropMode = false;
                    ContentPanMode = false;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetConnectorTool failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel active tool as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void CancelActiveTool()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CancelActiveTool.");
                    ConnectorTool = ConnectorToolKind.None;
                    CropMode = false;
                    ContentPanMode = false;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CancelActiveTool failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds shape as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shape">Shape value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The shape element produced by the operation.</returns>
    public ShapeElement AddShape(PublicationShape shape, double? centerX = null, double? centerY = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddShape.");
                    Capture();
                    var element = new ShapeElement
                    {
                        Name = NextName(shape.ToString()),
                        Shape = shape,
                        X = 30,
                        Y = 40,
                        Width = shape == PublicationShape.Line ? 80 : 55,
                        Height = shape == PublicationShape.Line ? 1 : 40,
                        ZIndex = NextZ()
                    };
                    PlaceAt(element, centerX, centerY);
                    CurrentPage.Elements.Add(element);
                    SetSelectionCore([element.Id], element.Id);
                    Notify();
                    return element;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddShape failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void AddPage()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddPage.");
                    Capture();
                    var source = CurrentPage;
                    var publicationPage = new PublicationPage
                    {
                        Name = $"Page {Document.Pages.Count + 1}",
                        WidthMm = source.WidthMm,
                        HeightMm = source.HeightMm,
                        Background = source.Background,
                        Transition = CloneTransition(source.Transition),
                        TimelineDurationSeconds = source.TimelineDurationSeconds
                    };
                    foreach (var shared in Document.Pages.SelectMany(page => page.Elements).OfType<DevExtremeComponentElement>()
                                 .Where(component => component.Scope == PublicationComponentScope.Document && component.SharedComponentId is not null)
                                 .GroupBy(component => component.SharedComponentId).Select(group => group.First()))
                    {
                        var clone = _components.Clone(shared);
                        clone.Id = Guid.NewGuid();
                        clone.X = Math.Clamp(clone.X, -clone.Width + 2, publicationPage.WidthMm - 2);
                        clone.Y = Math.Clamp(clone.Y, -clone.Height + 2, publicationPage.HeightMm - 2);
                        publicationPage.Elements.Add(clone);
                    }
                    Document.Pages.Add(publicationPage);
                    SelectedPageId = publicationPage.Id;
                    ClearSelectionCore();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddPage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs duplicate page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void DuplicatePage()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DuplicatePage.");
                    Capture();
                    var clone = ClonePage(CurrentPage);
                    clone.Id = Guid.NewGuid();
                    clone.Name = $"Page {Document.Pages.Count + 1}";
                    var idMap = clone.Elements.ToDictionary(item => item.Id, _ => Guid.NewGuid());
                    foreach (var item in clone.Elements)
                    {
                        item.Id = idMap[item.Id];
                        RenewAnimationIds(item, preserveOrder: true);
                        if (item.Interaction.TargetElementId is { } interactionTarget && idMap.TryGetValue(interactionTarget, out var mappedTarget))
                            item.Interaction.TargetElementId = mappedTarget;
                        if (item.Interaction.TargetPageId == CurrentPage.Id)
                            item.Interaction.TargetPageId = clone.Id;
                        if (item is DevExtremeComponentElement component)
                        {
                            foreach (var action in component.Actions)
                            {
                                if (action.TargetElementId is { } actionTarget && idMap.TryGetValue(actionTarget, out var mappedActionTarget))
                                    action.TargetElementId = mappedActionTarget;
                                if (action.TargetPageId == CurrentPage.Id)
                                    action.TargetPageId = clone.Id;
                            }
                        }
                    }
                    foreach (var connector in clone.Elements.OfType<ConnectorElement>())
                    {
                        if (idMap.TryGetValue(connector.Source.ElementId, out var sourceId)) connector.Source.ElementId = sourceId;
                        if (idMap.TryGetValue(connector.Target.ElementId, out var targetId)) connector.Target.ElementId = targetId;
                    }
                    foreach (var guide in clone.Guides) guide.Id = Guid.NewGuid();
                    Document.Pages.Insert(Document.Pages.IndexOf(CurrentPage) + 1, clone);
                    SelectedPageId = clone.Id;
                    ClearSelectionCore();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DuplicatePage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void DeletePage()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DeletePage.");
                    if (Document.Pages.Count <= 1) return;
                    Capture();
                    var index = Document.Pages.IndexOf(CurrentPage);
                    var deletedPageId = CurrentPage.Id;
                    Document.Pages.RemoveAt(index);
                    foreach (var item in Document.Pages.SelectMany(page => page.Elements))
                        if (item.Interaction.TargetPageId == deletedPageId)
                            item.Interaction.TargetPageId = null;
                    SelectedPageId = Document.Pages[Math.Clamp(index - 1, 0, Document.Pages.Count - 1)].Id;
                    ClearSelectionCore();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DeletePage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void DeleteSelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DeleteSelected.");
                    var elements = SelectedElements.Where(element => !element.Locked).ToList();
                    if (elements.Count == 0) return;
                    Capture();
                    var removedIds = new HashSet<Guid>();
                    var removedSharedIds = elements.OfType<DevExtremeComponentElement>()
                        .Where(component => component.Scope == PublicationComponentScope.Document && component.SharedComponentId is not null)
                        .Select(component => component.SharedComponentId!.Value)
                        .ToHashSet();

                    foreach (var element in elements)
                    {
                        if (element is DevExtremeComponentElement component && component.SharedComponentId is { } sharedId && removedSharedIds.Contains(sharedId))
                            continue;
                        if (CurrentPage.Elements.Remove(element)) removedIds.Add(element.Id);
                        if (element is PublicationMediaElement media)
                        {
                            _mediaAssets.Remove(media.Id);
                            foreach (var segment in media.Segments) _mediaAssets.Remove(segment.Id);
                        }
                    }

                    if (removedSharedIds.Count > 0)
                    {
                        foreach (var page in Document.Pages)
                        {
                            foreach (var component in page.Elements.OfType<DevExtremeComponentElement>()
                                         .Where(component => component.SharedComponentId is { } sharedId && removedSharedIds.Contains(sharedId))
                                         .ToList())
                            {
                                page.Elements.Remove(component);
                                removedIds.Add(component.Id);
                            }
                        }
                    }

                    foreach (var page in Document.Pages)
                    {
                        foreach (var connector in page.Elements.OfType<ConnectorElement>()
                                     .Where(connector => removedIds.Contains(connector.Source.ElementId) || removedIds.Contains(connector.Target.ElementId))
                                     .ToList())
                        {
                            removedIds.Add(connector.Id);
                            page.Elements.Remove(connector);
                        }

                        foreach (var item in page.Elements)
                        {
                            if (item.Interaction.TargetElementId is { } targetId && removedIds.Contains(targetId))
                                item.Interaction.TargetElementId = null;
                            if (item is not DevExtremeComponentElement targetComponent) continue;
                            foreach (var action in targetComponent.Actions)
                            {
                                if (action.TargetElementId is { } actionTargetId && removedIds.Contains(actionTargetId)) action.TargetElementId = null;
                                if (action.TargetSharedComponentId is { } actionSharedId && removedSharedIds.Contains(actionSharedId)) action.TargetSharedComponentId = null;
                            }
                        }

                        var ordered = page.Elements.OrderBy(element => element.ZIndex).ToList();
                        for (var index = 0; index < ordered.Count; index++) ordered[index].ZIndex = index + 1;
                    }

                    ReindexAnimations();
                    ClearSelectionCore();
                    CropMode = false;
                    ContentPanMode = false;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DeleteSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs copy selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void CopySelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CopySelected.");
                    var sources = ClipboardSelection();
                    if (sources.Count == 0) return;
                    _clipboard.Clear();
                    _clipboard.AddRange(sources.Select(CloneElement));
                    ClipboardRevision++;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CopySelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs cut selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void CutSelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CutSelected.");
                    if (SelectedElements.Count == 0) return;
                    CopySelected();
                    DeleteSelected();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CutSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs paste as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void Paste()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Paste.");
                    if (_clipboard.Count == 0) return;
                    CloneSelection(_clipboard, useInsertionPoint: true);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Paste failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs duplicate selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void DuplicateSelected()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DuplicateSelected.");
                    var sources = ClipboardSelection();
                    if (sources.Count == 0) return;
                    CloneSelection(sources, useInsertionPoint: false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DuplicateSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs select all as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void SelectAll()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SelectAll.");
                    var ids = CurrentPage.Elements.Where(element => element.Visible).Select(element => element.Id).ToArray();
                    if (ids.Length == 0) return;
                    SetSelectionCore(ids, ids[0]);
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    EndLiveEdit();
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SelectAll failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs nudge selection as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dx">Devexpress value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="dy">Dy value supplied to the editor state operation and used when producing its result.</param>
    public void NudgeSelection(double dx, double dy)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NudgeSelection.");
                    var elements = TransformSelectionBlock();
                    if (elements.Count == 0 || (NearlyEqual(dx, 0) && NearlyEqual(dy, 0))) return;
                    var left = elements.Min(element => element.X);
                    var top = elements.Min(element => element.Y);
                    var right = elements.Max(element => element.X + element.Width);
                    var bottom = elements.Max(element => element.Y + element.Height);
                    dx = Math.Clamp(dx, -left, CurrentPage.WidthMm - right);
                    dy = Math.Clamp(dy, -top, CurrentPage.HeightMm - bottom);
                    if (NearlyEqual(dx, 0) && NearlyEqual(dy, 0)) return;
                    Capture();
                    foreach (var element in elements)
                    {
                        element.X += dx;
                        element.Y += dy;
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NudgeSelection failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="effect">Effect value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="trigger">Trigger value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The publication animation produced by the operation.</returns>
    public PublicationAnimation? AddAnimation(
        PublicationAnimationEffect effect,
        PublicationAnimationPhase phase,
        PublicationAnimationTrigger? trigger = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddAnimation.");
                    var element = SelectedElement;
                    if (element is null) return null;
                    Capture();
                    var animation = new PublicationAnimation
                    {
                        Name = $"{effect} {phase}",
                        Effect = effect,
                        Phase = phase,
                        Trigger = trigger ?? (CurrentPage.Elements.SelectMany(item => item.Animations).Any()
                            ? PublicationAnimationTrigger.AfterPrevious
                            : phase == PublicationAnimationPhase.Entrance
                                ? PublicationAnimationTrigger.OnPageEnter
                                : PublicationAnimationTrigger.OnClick),
                        Order = NextAnimationOrder(),
                        Direction = effect is PublicationAnimationEffect.Fly or PublicationAnimationEffect.Float or PublicationAnimationEffect.Wipe or PublicationAnimationEffect.Move
                            ? PublicationAnimationDirection.Left
                            : PublicationAnimationDirection.None,
                        DurationSeconds = effect is PublicationAnimationEffect.PlayMedia or PublicationAnimationEffect.PauseMedia or PublicationAnimationEffect.StopMedia ? .05 : .6,
                        AutoReverse = phase == PublicationAnimationPhase.Emphasis && effect is not (PublicationAnimationEffect.PlayMedia or PublicationAnimationEffect.PauseMedia or PublicationAnimationEffect.StopMedia)
                    };
                    element.Animations.Add(animation);
                    EnsureTimelineDuration();
                    Notify();
                    return animation;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateAnimation(Guid animationId, Action<PublicationAnimation> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateAnimation.");
                    var animation = FindAnimation(animationId);
                    if (animation is null) return;
                    Capture();
                    update(animation);
                    NormalizeAnimation(animation);
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates animation live as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    /// <param name="key">Key value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateAnimationLive(Guid animationId, string key, Action<PublicationAnimation> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateAnimationLive.");
                    var animation = FindAnimation(animationId);
                    if (animation is null) return;
                    var liveKey = $"animation:{animationId}:{key}";
                    if (!string.Equals(_liveEditKey, liveKey, StringComparison.Ordinal))
                    {
                        Capture();
                        _liveEditKey = liveKey;
                    }
                    update(animation);
                    NormalizeAnimation(animation);
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateAnimationLive failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs duplicate animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    /// <returns>The publication animation produced by the operation.</returns>
    public PublicationAnimation? DuplicateAnimation(Guid animationId)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DuplicateAnimation.");
                    var owner = CurrentPage.Elements.FirstOrDefault(item => item.Animations.Any(animation => animation.Id == animationId));
                    var source = owner?.Animations.FirstOrDefault(item => item.Id == animationId);
                    if (owner is null || source is null) return null;
                    Capture();
                    var clone = new PublicationAnimation
                    {
                        Id = Guid.NewGuid(),
                        Name = NextName(source.Name),
                        Order = NextAnimationOrder(),
                        Phase = source.Phase,
                        Effect = source.Effect,
                        Trigger = source.Trigger,
                        Easing = source.Easing,
                        Direction = source.Direction,
                        DurationSeconds = source.DurationSeconds,
                        DelaySeconds = source.DelaySeconds,
                        TimelineStartSeconds = source.TimelineStartSeconds is { } start ? start + .25 : null,
                        DistancePercent = source.DistancePercent,
                        ScalePercent = source.ScalePercent,
                        RotationDegrees = source.RotationDegrees,
                        RepeatCount = source.RepeatCount,
                        AutoReverse = source.AutoReverse
                    };
                    owner.Animations.Add(clone);
                    ReindexAnimations();
                    EnsureTimelineDuration();
                    SetSelectionCore([owner.Id], owner.Id);
                    Notify();
                    return clone;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DuplicateAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    public void DeleteAnimation(Guid animationId)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DeleteAnimation.");
                    var owner = CurrentPage.Elements.FirstOrDefault(item => item.Animations.Any(animation => animation.Id == animationId));
                    var animation = owner?.Animations.FirstOrDefault(item => item.Id == animationId);
                    if (owner is null || animation is null) return;
                    Capture();
                    owner.Animations.Remove(animation);
                    ReindexAnimations();
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DeleteAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs move animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    /// <param name="offset">Offset value supplied to the editor state operation and used when producing its result.</param>
    public void MoveAnimation(Guid animationId, int offset)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.MoveAnimation.");
                    var timeline = CurrentPage.Elements
                        .SelectMany(element => element.Animations.Select(animation => (element, animation)))
                        .OrderBy(item => item.animation.Order)
                        .ToList();
                    var index = timeline.FindIndex(item => item.animation.Id == animationId);
                    if (index < 0) return;
                    var target = Math.Clamp(index + offset, 0, timeline.Count - 1);
                    if (index == target) return;
                    Capture();
                    var moving = timeline[index];
                    timeline.RemoveAt(index);
                    timeline.Insert(target, moving);
                    for (var order = 0; order < timeline.Count; order++)
                        timeline[order].animation.Order = order + 1;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.MoveAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clear selected animations as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ClearSelectedAnimations()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClearSelectedAnimations.");
                    var element = SelectedElement;
                    if (element is null || element.Animations.Count == 0) return;
                    Capture();
                    element.Animations.Clear();
                    ReindexAnimations();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClearSelectedAnimations failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates interaction as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateInteraction(Action<PublicationInteraction> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateInteraction.");
                    var element = SelectedElement;
                    if (element is null) return;
                    Capture();
                    element.Interaction ??= new PublicationInteraction();
                    update(element.Interaction);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateInteraction failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates page transition as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdatePageTransition(Action<PublicationPageTransition> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdatePageTransition.");
                    Capture();
                    CurrentPage.Transition ??= new PublicationPageTransition();
                    update(CurrentPage.Transition);
                    CurrentPage.Transition.DurationSeconds = Math.Clamp(CurrentPage.Transition.DurationSeconds, .1, 8);
                    CurrentPage.Transition.AutoAdvanceSeconds = Math.Clamp(CurrentPage.Transition.AutoAdvanceSeconds, .25, 3600);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdatePageTransition failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates page transition live as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdatePageTransitionLive(string key, Action<PublicationPageTransition> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdatePageTransitionLive.");
                    var liveKey = $"page-transition:{SelectedPageId}:{key}";
                    if (!string.Equals(_liveEditKey, liveKey, StringComparison.Ordinal))
                    {
                        Capture();
                        _liveEditKey = liveKey;
                    }
                    CurrentPage.Transition ??= new PublicationPageTransition();
                    update(CurrentPage.Transition);
                    CurrentPage.Transition.DurationSeconds = Math.Clamp(CurrentPage.Transition.DurationSeconds, .1, 8);
                    CurrentPage.Transition.AutoAdvanceSeconds = Math.Clamp(CurrentPage.Transition.AutoAdvanceSeconds, .25, 3600);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdatePageTransitionLive failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets animation timeline range as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    /// <param name="startSeconds">Start seconds value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="durationSeconds">Duration seconds value supplied to the editor state operation and used when producing its result.</param>
    public void SetAnimationTimelineRange(Guid animationId, double startSeconds, double durationSeconds)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetAnimationTimelineRange.");
                    var animation = FindAnimation(animationId);
                    if (animation is null) return;
                    Capture();
                    animation.TimelineStartSeconds = Math.Clamp(startSeconds, 0, 3600);
                    var playbackMultiplier = Math.Max(1, animation.RepeatCount) * (animation.AutoReverse ? 2 : 1);
                    animation.DurationSeconds = Math.Clamp(durationSeconds / playbackMultiplier, .05, 60);
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetAnimationTimelineRange failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clear animation timeline position as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animationId">Identifier of the animation to use for this operation.</param>
    public void ClearAnimationTimelinePosition(Guid animationId)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClearAnimationTimelinePosition.");
                    var animation = FindAnimation(animationId);
                    if (animation is null || animation.TimelineStartSeconds is null) return;
                    Capture();
                    animation.TimelineStartSeconds = null;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClearAnimationTimelinePosition failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets media timeline range as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <param name="mode">Mode value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="startSeconds">Start seconds value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="lengthSeconds">Length seconds value supplied to the editor state operation and used when producing its result.</param>
    public void SetMediaTimelineRange(Guid elementId, string mode, double startSeconds, double lengthSeconds)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetMediaTimelineRange.");
                    var media = CurrentPage.Elements.OfType<PublicationMediaElement>().FirstOrDefault(item => item.Id == elementId);
                    if (media is null || media.Locked) return;
                    Capture();
                    NormalizeMedia(media);
                    var oldStart = media.TimelineStartSeconds;
                    var oldLength = media.TimelineLengthSeconds;
                    var sourceRate = Math.Max(.1, media.PlaybackRate);
                    startSeconds = Math.Clamp(startSeconds, 0, 3600);
                    lengthSeconds = Math.Clamp(lengthSeconds, .05, 3600);
                    switch (mode)
                    {
                        case "trim-left":
                        {
                            var oldTimelineEnd = oldStart + oldLength;
                            var newTimelineEnd = startSeconds + lengthSeconds;
                            if (Math.Abs(newTimelineEnd - oldTimelineEnd) > .15)
                                startSeconds = Math.Max(0, oldTimelineEnd - lengthSeconds);
                            media.TrimStartSeconds = Math.Clamp(media.EffectiveTrimEndSeconds - lengthSeconds * sourceRate, 0, media.EffectiveTrimEndSeconds - .01);
                            media.TimelineStartSeconds = startSeconds;
                            break;
                        }
                        case "trim-right":
                            media.TrimEndSeconds = Math.Clamp(media.TrimStartSeconds + lengthSeconds * sourceRate, media.TrimStartSeconds + .01, Math.Max(media.DurationSeconds, media.TrimStartSeconds + .01));
                            break;
                        default:
                            media.TimelineStartSeconds = startSeconds;
                            break;
                    }
                    NormalizeMedia(media);
                    EnsureTimelineDuration();
                    SetSelectionCore([media.Id], media.Id);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetMediaTimelineRange failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets page timeline duration as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="seconds">Seconds value supplied to the editor state operation and used when producing its result.</param>
    public void SetPageTimelineDuration(double seconds)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPageTimelineDuration.");
                    Capture();
                    CurrentPage.TimelineDurationSeconds = Math.Clamp(seconds, 1, 3600);
                    EnsureTimelineDuration();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPageTimelineDuration failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs effective animation start as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="target">Target value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    public double EffectiveAnimationStart(PublicationAnimation target)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.EffectiveAnimationStart.");
                    if (target.TimelineStartSeconds is { } explicitStart) return Math.Max(0, explicitStart);
                    var timeline = CurrentPage.Elements.SelectMany(item => item.Animations).OrderBy(item => item.Order).ToList();
                    double previousStart = 0;
                    double previousEnd = 0;
                    foreach (var animation in timeline)
                    {
                        var start = Math.Max(0, animation.DelaySeconds);
                        if (animation.Trigger == PublicationAnimationTrigger.WithPrevious) start = previousStart + Math.Max(0, animation.DelaySeconds);
                        else if (animation.Trigger == PublicationAnimationTrigger.AfterPrevious) start = previousEnd + Math.Max(0, animation.DelaySeconds);
                        else if (animation.Trigger == PublicationAnimationTrigger.OnClick) start = animation.TimelineStartSeconds ?? 0;
                        if (animation.Id == target.Id) return start;
                        previousStart = start;
                        previousEnd = start + AnimationSpan(animation);
                    }
                    return 0;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.EffectiveAnimationStart failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs effective page timeline duration as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The double produced by the operation.</returns>
    public double EffectivePageTimelineDuration()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.EffectivePageTimelineDuration.");
                    var animationEnd = CurrentPage.Elements.SelectMany(item => item.Animations)
                        .Select(item => EffectiveAnimationStart(item) + AnimationSpan(item))
                        .DefaultIfEmpty(0)
                        .Max();
                    var mediaEnd = CurrentPage.Elements.OfType<PublicationMediaElement>()
                        .Select(item => item.TimelineStartSeconds + item.TimelineLengthSeconds)
                        .DefaultIfEmpty(0)
                        .Max();
                    return Math.Clamp(Math.Max(CurrentPage.TimelineDurationSeconds, Math.Max(animationEnd, mediaEnd) + .5), 1, 3600);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.EffectivePageTimelineDuration failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit move as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="x">X value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="movingIds">Guid dependency used by the editor state workflow to provide the corresponding application capability.</param>
    public void CommitMove(Guid id, double x, double y, IReadOnlyCollection<Guid>? movingIds = null)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitMove.");
                    var element = CurrentPage.Elements.FirstOrDefault(item => item.Id == id);
                    if (element is null || element.Locked || element is ConnectorElement) return;
                    var requestedIds = movingIds is { Count: > 0 }
                        ? movingIds.ToHashSet()
                        : new HashSet<Guid>();

                    // Browser-side movement is optimistic; the server-side selection remains
                    // authoritative. Merge every selected object and the persistent group unit so
                    // lower z-level members cannot be omitted by stale DOM selection markers.
                    if (_selectedElementIds.Contains(element.Id))
                        requestedIds.UnionWith(_selectedElementIds);
                    foreach (var grouped in SelectionUnit(element))
                        requestedIds.Add(grouped.Id);
                    requestedIds.Add(element.Id);

                    var moving = CurrentPage.Elements
                        .Where(item => requestedIds.Contains(item.Id))
                        .Where(item => !item.Locked && item is not ConnectorElement)
                        .DistinctBy(item => item.Id)
                        .ToList();
                    if (moving.All(item => item.Id != element.Id)) moving.Insert(0, element);
                    if (moving.Count == 0) return;

                    var requestedDx = x - element.X;
                    var requestedDy = y - element.Y;
                    var minDx = moving.Max(item => -item.Width + 2 - item.X);
                    var maxDx = moving.Min(item => CurrentPage.WidthMm - 2 - item.X);
                    var minDy = moving.Max(item => -item.Height + 2 - item.Y);
                    var maxDy = moving.Min(item => CurrentPage.HeightMm - 2 - item.Y);
                    var dx = Math.Clamp(requestedDx, minDx, maxDx);
                    var dy = Math.Clamp(requestedDy, minDy, maxDy);
                    if (NearlyEqual(dx, 0) && NearlyEqual(dy, 0)) return;

                    Capture();
                    foreach (var item in moving)
                    {
                        item.X += dx;
                        item.Y += dy;
                    }
                    if (!_selectedElementIds.Contains(element.Id))
                        SetSelectionCore(moving.Select(item => item.Id), element.Id);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitMove failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit bounds as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="x">X value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the editor state operation and used when producing its result.</param>
    public void CommitBounds(Guid id, double x, double y, double width, double height)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitBounds.");
                    var element = CurrentPage.Elements.FirstOrDefault(e => e.Id == id);
                    if (element is null || element.Locked || element is ConnectorElement) return;
                    var (minimumWidth, minimumHeight) = MinimumElementSize(element);
                    var nextWidth = Math.Max(minimumWidth, Math.Min(width, CurrentPage.WidthMm * 2));
                    var nextHeight = Math.Max(minimumHeight, Math.Min(height, CurrentPage.HeightMm * 2));
                    var nextX = Math.Clamp(x, -nextWidth + 2, CurrentPage.WidthMm - 2);
                    var nextY = Math.Clamp(y, -nextHeight + 2, CurrentPage.HeightMm - 2);
                    if (NearlyEqual(element.X, nextX) && NearlyEqual(element.Y, nextY) &&
                        NearlyEqual(element.Width, nextWidth) && NearlyEqual(element.Height, nextHeight)) return;
                    Capture();
                    element.Width = nextWidth;
                    element.Height = nextHeight;
                    element.X = nextX;
                    element.Y = nextY;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitBounds failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit crop as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cropX">Crop x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="cropY">Crop y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="cropScale">Crop scale value supplied to the editor state operation and used when producing its result.</param>
    public void CommitCrop(Guid id, double cropX, double cropY, double cropScale)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitCrop.");
                    var image = CurrentPage.Elements.OfType<ImageFrameElement>().FirstOrDefault(e => e.Id == id);
                    if (image is null || image.Locked) return;
                    Capture();
                    image.CropX = Math.Clamp(cropX, -100, 100);
                    image.CropY = Math.Clamp(cropY, -100, 100);
                    image.CropScale = Math.Clamp(cropScale, .2, 8);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitCrop failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="capture">Value indicating whether capture should apply to this operation.</param>
    /// <param name="allowLocked">Value indicating whether allow locked should apply to this operation.</param>
    public void UpdateSelected(Action<PublicationElement> update, bool capture = true, bool allowLocked = false)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateSelected.");
                    var element = SelectedElement;
                    if (element is null || (element.Locked && !allowLocked)) return;
                    if (capture) Capture();
                    update(element);
                    if (element is DevExtremeComponentElement component)
                    {
                        _components.Normalize(Document, component);
                        SynchronizeDocumentComponent(component);
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates selected live as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateSelectedLive(string key, Action<PublicationElement> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateSelectedLive.");
                    var element = SelectedElement;
                    if (element is null || element.Locked) return;
                    if (!string.Equals(_liveEditKey, key, StringComparison.Ordinal))
                    {
                        Capture();
                        _liveEditKey = key;
                    }
                    update(element);
                    if (element is DevExtremeComponentElement component)
                    {
                        _components.Normalize(Document, component);
                        SynchronizeDocumentComponent(component);
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateSelectedLive failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs end live edit as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void EndLiveEdit() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.EndLiveEdit.");
            _liveEditKey = null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.EndLiveEdit failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void UpdatePage(Action<PublicationPage> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdatePage.");
                    Capture();
                    update(CurrentPage);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdatePage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets page size as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="widthMm">Width mm value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="heightMm">Height mm value supplied to the editor state operation and used when producing its result.</param>
    public void SetPageSize(double widthMm, double heightMm)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPageSize.");
                    if (widthMm is < 10 or > 2000 || heightMm is < 10 or > 2000) return;
                    UpdatePage(publicationPage =>
                    {
                        publicationPage.WidthMm = widthMm;
                        publicationPage.HeightMm = heightMm;
                    });
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPageSize failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs swap page orientation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void SwapPageOrientation() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SwapPageOrientation.");
            SetPageSize(CurrentPage.HeightMm, CurrentPage.WidthMm);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SwapPageOrientation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates spreadsheet document as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="previewHtml">Preview html value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="activeSheetName">Active sheet name value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateSpreadsheetDocument(byte[] content, string fileName, SpreadsheetStorageFormat format, string previewHtml, string activeSheetName)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateSpreadsheetDocument.");
                    if (SelectedElement is not SpreadsheetElement spreadsheet || spreadsheet.Locked) return;
                    _spreadsheets.ValidateWorkbookContent(content, format);
                    Capture();
                    spreadsheet.WorkbookContent = content.ToArray();
                    spreadsheet.WorkbookFileName = _spreadsheets.NormalizeWorkbookFileName(fileName, format);
                    spreadsheet.StorageFormat = format;
                    spreadsheet.PreviewHtml = previewHtml;
                    spreadsheet.ActiveSheetName = string.IsNullOrWhiteSpace(activeSheetName) ? "Sheet1" : activeSheetName;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateSpreadsheetDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates text document as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="previewHtml">Preview html value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="documentBackground">Document background value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the editor state operation and used when producing its result.</param>
    public void UpdateTextDocument(byte[] content, string previewHtml, string documentBackground, StoryStorageFormat format = StoryStorageFormat.OpenXml)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.UpdateTextDocument.");
                    if (SelectedElement is not TextFrameElement text || text.Locked) return;
                    Capture();
                    text.DocumentContent = content;
                    text.PreviewHtml = _files.SanitizePreviewHtml(previewHtml);
                    text.DocumentBackground = _files.NormalizeCssBackground(documentBackground);
                    text.StoryFormat = format;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.UpdateTextDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle crop mode as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ToggleCropMode()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ToggleCropMode.");
                    if (SelectedElement is not ImageFrameElement) return;
                    CropMode = !CropMode;
                    if (CropMode) ContentPanMode = false;
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ToggleCropMode failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle content pan mode as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ToggleContentPanMode()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ToggleContentPanMode.");
                    if (!CanPanContent(SelectedElement)) return;
                    ContentPanMode = !ContentPanMode;
                    if (ContentPanMode)
                    {
                        CropMode = false;
                        ConnectorTool = ConnectorToolKind.None;
                    }
                    Notify(false);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ToggleContentPanMode failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit content viewport as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="offsetX">Offset x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="offsetY">Offset y value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="scale">Scale value supplied to the editor state operation and used when producing its result.</param>
    public void CommitContentViewport(Guid id, double offsetX, double offsetY, double scale)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitContentViewport.");
                    var element = CurrentPage.Elements.FirstOrDefault(candidate => candidate.Id == id);
                    if (!CanPanContent(element) || element!.Locked) return;
                    Capture();
                    var x = Math.Clamp(offsetX, -500, 500);
                    var y = Math.Clamp(offsetY, -500, 500);
                    var zoom = Math.Clamp(scale, .1, 12);
                    switch (element)
                    {
                        case TextFrameElement text: text.ContentOffsetX = x; text.ContentOffsetY = y; text.ContentScale = zoom; break;
                        case SpreadsheetElement sheet: sheet.ContentOffsetX = x; sheet.ContentOffsetY = y; sheet.ContentScale = zoom; break;
                        case DevExtremeComponentElement component: component.ContentOffsetX = x; component.ContentOffsetY = y; component.ContentScale = zoom; break;
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitContentViewport failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit map viewport as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="longitude">Longitude value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="latitude">Latitude value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="zoom">Zoom value supplied to the editor state operation and used when producing its result.</param>
    public void CommitMapViewport(Guid id, double longitude, double latitude, double zoom)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitMapViewport.");
                    var component = CurrentPage.Elements.OfType<DevExtremeComponentElement>().FirstOrDefault(candidate => candidate.Id == id);
                    if (component is null || component.Locked || SelectedElementId != id || !ContentPanMode ||
                        (component.ComponentKind != PublicationComponentKind.Map && component.ComponentKind != PublicationComponentKind.VectorMap)) return;

                    var nextLongitude = Math.Clamp(longitude, -180, 180);
                    var nextLatitude = Math.Clamp(latitude, -90, 90);
                    var zoomMaximum = component.ComponentKind == PublicationComponentKind.VectorMap ? 256d : 20d;
                    var nextZoom = Math.Clamp(zoom, 1, zoomMaximum);
                    if (Math.Abs(component.MapCenterLongitude - nextLongitude) < .000001 &&
                        Math.Abs(component.MapCenterLatitude - nextLatitude) < .000001 &&
                        Math.Abs(component.MapZoom - nextZoom) < .0001) return;

                    Capture();
                    component.MapCenterLongitude = nextLongitude;
                    component.MapCenterLatitude = nextLatitude;
                    component.MapZoom = nextZoom;
                    if (component.ComponentKind == PublicationComponentKind.Map) component.MapAutoAdjust = false;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitMapViewport failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs reset content viewport as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ResetContentViewport()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ResetContentViewport.");
                    if (!CanPanContent(SelectedElement)) return;
                    CommitContentViewport(SelectedElement!.Id, 0, 0, 1);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ResetContentViewport failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether pan content as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanPanContent(PublicationElement? element) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CanPanContent.");
            return element is TextFrameElement or SpreadsheetElement
        or DevExtremeComponentElement { ComponentKind: PublicationComponentKind.Map or PublicationComponentKind.VectorMap };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CanPanContent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds guide as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="orientation">Orientation value supplied to the editor state operation and used when producing its result.</param>
    public void AddGuide(GuideOrientation orientation)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddGuide.");
                    AddGuideAt(orientation, orientation == GuideOrientation.Vertical ? CurrentPage.WidthMm / 2 : CurrentPage.HeightMm / 2);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddGuide failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds guide at as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="orientation">Orientation value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="positionMm">Position mm value supplied to the editor state operation and used when producing its result.</param>
    public void AddGuideAt(GuideOrientation orientation, double positionMm)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AddGuideAt.");
                    var max = orientation == GuideOrientation.Vertical ? CurrentPage.WidthMm : CurrentPage.HeightMm;
                    if (positionMm < 0 || positionMm > max) return;
                    Capture();
                    CurrentPage.Guides.Add(new GuideLine
                    {
                        Orientation = orientation,
                        PositionMm = Math.Clamp(positionMm, 0, max)
                    });
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AddGuideAt failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs commit guide as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="positionMm">Position mm value supplied to the editor state operation and used when producing its result.</param>
    public void CommitGuide(Guid id, double positionMm)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CommitGuide.");
                    var guide = CurrentPage.Guides.FirstOrDefault(item => item.Id == id);
                    if (guide is null) return;
                    var max = guide.Orientation == GuideOrientation.Vertical ? CurrentPage.WidthMm : CurrentPage.HeightMm;
                    Capture();
                    guide.PositionMm = Math.Clamp(positionMm, 0, max);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CommitGuide failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes guide as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void DeleteGuide(Guid id)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.DeleteGuide.");
                    var guide = CurrentPage.Guides.FirstOrDefault(item => item.Id == id);
                    if (guide is null) return;
                    Capture();
                    CurrentPage.Guides.Remove(guide);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.DeleteGuide failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clear guides as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ClearGuides()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClearGuides.");
                    if (CurrentPage.Guides.Count == 0) return;
                    Capture();
                    CurrentPage.Guides.Clear();
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClearGuides failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets zoom as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="zoom">Zoom value supplied to the editor state operation and used when producing its result.</param>
    public void SetZoom(double zoom)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetZoom.");
                    var normalizedPercent = Math.Clamp(Math.Round(zoom * 100d, MidpointRounding.AwayFromZero), 20d, 400d);
                    var normalized = normalizedPercent / 100d;
                    if (NearlyEqual(Document.Zoom, normalized)) return;
                    Document.Zoom = normalized;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetZoom failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets zoom percent as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="percent">Percent value supplied to the editor state operation and used when producing its result.</param>
    public void SetZoomPercent(double percent) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetZoomPercent.");
            SetZoom(percent / 100d);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetZoomPercent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs step zoom percent as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deltaPercent">Delta percent value supplied to the editor state operation and used when producing its result.</param>
    public void StepZoomPercent(int deltaPercent)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.StepZoomPercent.");
                    var currentPercent = Math.Round(Document.Zoom * 100d, MidpointRounding.AwayFromZero);
                    SetZoomPercent(currentPercent + deltaPercent);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.StepZoomPercent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets canvas zoom mode as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the editor state operation and used when producing its result.</param>
    public void SetCanvasZoomMode(PublicationCanvasZoomMode mode)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetCanvasZoomMode.");
                    if (Document.View.CanvasZoomMode == mode) return;
                    Document.View.CanvasZoomMode = mode;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetCanvasZoomMode failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets ruler unit as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="unit">Unit value supplied to the editor state operation and used when producing its result.</param>
    public void SetRulerUnit(MeasurementUnit unit)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetRulerUnit.");
                    if (Document.View.RulerUnit == unit) return;
                    Document.View.RulerUnit = unit;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetRulerUnit failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs cycle ruler unit as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void CycleRulerUnit()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CycleRulerUnit.");
                    var values = Enum.GetValues<MeasurementUnit>();
                    var index = Array.IndexOf(values, Document.View.RulerUnit);
                    SetRulerUnit(values[(index + 1) % values.Length]);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CycleRulerUnit failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets view option as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the editor state operation and used when producing its result.</param>
    public void SetViewOption(Action<PublicationViewSettings> update)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetViewOption.");
                    update(Document.View);
                    Document.View.GridSpacingMm = Math.Clamp(Document.View.GridSpacingMm, .5, 100);
                    Document.View.ExportDpi = Math.Clamp(Document.View.ExportDpi, 72, 600);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetViewOption failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets playback as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the editor state operation and used when producing its result.</param>
    public void SetPlayback(PublicationPlaybackSettings value)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetPlayback.");
                    Capture();
                    Document.Playback = value;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetPlayback failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs bring to front as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void BringToFront() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.BringToFront.");
            ReorderSelected(int.MaxValue);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.BringToFront failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs send to back as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void SendToBack() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SendToBack.");
            ReorderSelected(int.MinValue);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SendToBack failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs bring forward as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void BringForward() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.BringForward.");
            ReorderSelected(1);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.BringForward failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs send backward as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void SendBackward() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SendBackward.");
            ReorderSelected(-1);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SendBackward failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets selected layer position as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="position">Position value supplied to the editor state operation and used when producing its result.</param>
    public void SetSelectedLayerPosition(int position)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetSelectedLayerPosition.");
                    var block = LayerSelectionBlock();
                    if (block.Count == 0) return;
                    var ordered = OrderedElements();
                    var selectedIds = block.Select(item => item.Id).ToHashSet();
                    var currentIndex = ordered.FindIndex(item => selectedIds.Contains(item.Id));
                    if (currentIndex < 0) return;
                    var remaining = ordered.Where(item => !selectedIds.Contains(item.Id)).ToList();
                    var targetIndex = Math.Clamp(position - 1, 0, remaining.Count);
                    if (targetIndex == currentIndex && HasNormalizedZOrder(ordered)) return;
                    Capture();
                    remaining.InsertRange(targetIndex, block);
                    ApplyNormalizedZOrder(remaining);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetSelectedLayerPosition failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs align as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mode">Mode value supplied to the editor state operation and used when producing its result.</param>
    public void Align(string mode)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Align.");
                    var elements = TransformSelectionBlock();
                    if (elements.Count == 0) return;
                    var left = elements.Min(item => item.X);
                    var top = elements.Min(item => item.Y);
                    var right = elements.Max(item => item.X + item.Width);
                    var bottom = elements.Max(item => item.Y + item.Height);
                    var width = right - left;
                    var height = bottom - top;
                    var dx = 0d;
                    var dy = 0d;
                    switch (mode)
                    {
                        case "left": dx = -left; break;
                        case "center": dx = (CurrentPage.WidthMm - width) / 2 - left; break;
                        case "right": dx = CurrentPage.WidthMm - right; break;
                        case "top": dy = -top; break;
                        case "middle": dy = (CurrentPage.HeightMm - height) / 2 - top; break;
                        case "bottom": dy = CurrentPage.HeightMm - bottom; break;
                        default: return;
                    }
                    if (NearlyEqual(dx, 0) && NearlyEqual(dy, 0)) return;
                    Capture();
                    foreach (var element in elements)
                    {
                        element.X += dx;
                        element.Y += dy;
                    }
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Align failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs undo as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void Undo()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Undo.");
                    if (_undo.Count == 0) return;
                    _redo.Push(_files.Serialize(Document));
                    Restore(_undo.Pop());
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Undo failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs redo as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void Redo()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Redo.");
                    if (_redo.Count == 0) return;
                    _undo.Push(_files.Serialize(Document));
                    Restore(_redo.Pop());
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Redo failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs restore as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the editor state operation and used when producing its result.</param>
    private void Restore(string json)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Restore.");
                    var selectedPageIndex = Math.Max(0, Document.Pages.FindIndex(p => p.Id == SelectedPageId));
                    var streaming = Document.Streaming;
                    Document = _files.Deserialize(json);
                    Document.Streaming = streaming;
                    _files.NormalizeStreamingSettings(Document);
                    SelectedPageId = Document.Pages[Math.Min(selectedPageIndex, Document.Pages.Count - 1)].Id;
                    ClearSelectionCore();
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    _liveEditKey = null;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Restore failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Finds animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The publication animation produced by the operation.</returns>
    private PublicationAnimation? FindAnimation(Guid id) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.FindAnimation.");
            return CurrentPage.Elements.SelectMany(item => item.Animations).FirstOrDefault(item => item.Id == id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.FindAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs next animation order as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The int produced by the operation.</returns>
    private int NextAnimationOrder() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NextAnimationOrder.");
            return CurrentPage.Elements.SelectMany(item => item.Animations).Select(item => item.Order).DefaultIfEmpty(0).Max() + 1;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NextAnimationOrder failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs reindex animations as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ReindexAnimations()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ReindexAnimations.");
                    var timeline = CurrentPage.Elements.SelectMany(item => item.Animations).OrderBy(item => item.Order).ToList();
                    for (var index = 0; index < timeline.Count; index++) timeline[index].Order = index + 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ReindexAnimations failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs renew animation identifiers as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="preserveOrder">Value indicating whether preserve order should apply to this operation.</param>
    private void RenewAnimationIds(PublicationElement element, bool preserveOrder)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RenewAnimationIds.");
                    var nextOrder = NextAnimationOrder();
                    foreach (var animation in element.Animations)
                    {
                        animation.Id = Guid.NewGuid();
                        if (!preserveOrder) animation.Order = nextOrder++;
                    }
                    if (!preserveOrder) ReindexAnimations();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RenewAnimationIds failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures timeline duration as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void EnsureTimelineDuration()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.EnsureTimelineDuration.");
                    CurrentPage.TimelineDurationSeconds = Math.Clamp(Math.Max(CurrentPage.TimelineDurationSeconds, EffectivePageTimelineDuration()), 1, 3600);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.EnsureTimelineDuration failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs animation span as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animation">Animation value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double AnimationSpan(PublicationAnimation animation) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.AnimationSpan.");
            return Math.Max(.05, animation.DurationSeconds) * Math.Max(1, animation.RepeatCount) * (animation.AutoReverse ? 2 : 1);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.AnimationSpan failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes media as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="media">Media value supplied to the editor state operation and used when producing its result.</param>
    private void NormalizeMedia(PublicationMediaElement media)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NormalizeMedia.");
                    media.DurationSeconds = Math.Clamp(media.DurationSeconds, 0, 24 * 60 * 60);
                    media.TrimStartSeconds = Math.Clamp(media.TrimStartSeconds, 0, Math.Max(0, media.DurationSeconds));
                    var end = media.TrimEndSeconds <= media.TrimStartSeconds ? media.DurationSeconds : media.TrimEndSeconds;
                    media.TrimEndSeconds = Math.Clamp(end, media.TrimStartSeconds, Math.Max(media.TrimStartSeconds, media.DurationSeconds));
                    media.TimelineStartSeconds = Math.Clamp(media.TimelineStartSeconds, 0, 3600);
                    media.Volume = Math.Clamp(media.Volume, 0, 1);
                    media.PlaybackRate = Math.Clamp(media.PlaybackRate <= 0 ? 1 : media.PlaybackRate, .25, 4);
                    media.FadeInSeconds = Math.Clamp(media.FadeInSeconds, 0, Math.Max(0, media.TimelineLengthSeconds / 2));
                    media.FadeOutSeconds = Math.Clamp(media.FadeOutSeconds, 0, Math.Max(0, media.TimelineLengthSeconds / 2));
                    media.WaveformSamples ??= [];
                    if (media.WaveformSamples.Count > 256) media.WaveformSamples = media.WaveformSamples.Take(256).ToList();
                    var fallbackMimeType = media is VideoElement ? "video/webm" : "audio/webm";
                    media.MimeType = _mediaData.NormalizeMimeType(media.MimeType, fallbackMimeType);
                    media.DataUrl = _mediaData.NormalizeDataUrl(media.DataUrl, media.MimeType);
                    media.Segments ??= [];
                    foreach (var segment in media.Segments)
                    {
                        segment.Id = segment.Id == Guid.Empty ? Guid.NewGuid() : segment.Id;
                        segment.Name = string.IsNullOrWhiteSpace(segment.Name) ? media.Name : segment.Name.Trim();
                        segment.DurationSeconds = Math.Clamp(segment.DurationSeconds, .01, 24 * 60 * 60);
                        segment.TrimStartSeconds = Math.Clamp(segment.TrimStartSeconds, 0, Math.Max(0, segment.DurationSeconds - .01));
                        segment.TrimEndSeconds = Math.Clamp(segment.TrimEndSeconds > segment.TrimStartSeconds ? segment.TrimEndSeconds : segment.DurationSeconds, segment.TrimStartSeconds + .01, segment.DurationSeconds);
                        segment.MimeType = _mediaData.NormalizeMimeType(segment.MimeType, fallbackMimeType);
                        segment.DataUrl = _mediaData.NormalizeDataUrl(segment.DataUrl, segment.MimeType);
                        segment.WaveformSamples ??= [];
                        if (segment.WaveformSamples.Count > 256) segment.WaveformSamples = segment.WaveformSamples.Take(256).ToList();
                    }
                    if (media is VideoElement video)
                    {
                        video.FrameClipPolygon ??= [];
                        if (video.FrameClipPolygon.Count > 256) video.FrameClipPolygon = video.FrameClipPolygon.Take(256).ToList();
                        foreach (var point in video.FrameClipPolygon)
                        {
                            point.X = Math.Clamp(point.X, 0, 1);
                            point.Y = Math.Clamp(point.Y, 0, 1);
                        }
                        if (video.FrameClipPolygon.Count is > 0 and < 3) video.FrameClipPolygon.Clear();
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NormalizeMedia failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes animation as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="animation">Animation value supplied to the editor state operation and used when producing its result.</param>
    private void NormalizeAnimation(PublicationAnimation animation)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NormalizeAnimation.");
                    animation.DurationSeconds = Math.Clamp(animation.DurationSeconds <= 0 ? .6 : animation.DurationSeconds, .05, 60);
                    animation.DelaySeconds = Math.Clamp(animation.DelaySeconds, 0, 60);
                    if (animation.TimelineStartSeconds is { } timelineStart)
                        animation.TimelineStartSeconds = Math.Clamp(timelineStart, 0, 3600);
                    animation.DistancePercent = Math.Clamp(animation.DistancePercent, 0, 500);
                    animation.ScalePercent = Math.Clamp(animation.ScalePercent, 0, 500);
                    animation.RotationDegrees = Math.Clamp(animation.RotationDegrees, -3600, 3600);
                    animation.RepeatCount = Math.Clamp(animation.RepeatCount <= 0 ? 1 : animation.RepeatCount, 1, 100);
                    if (string.IsNullOrWhiteSpace(animation.Name)) animation.Name = $"{animation.Effect} {animation.Phase}";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NormalizeAnimation failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone transition as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The publication page transition produced by the operation.</returns>
    private PublicationPageTransition CloneTransition(PublicationPageTransition source) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CloneTransition.");
            return new()
    {
        Kind = source.Kind,
        Direction = source.Direction,
        Easing = source.Easing,
        DurationSeconds = source.DurationSeconds,
        AdvanceOnClick = source.AdvanceOnClick,
        AutoAdvance = source.AutoAdvance,
        AutoAdvanceSeconds = source.AutoAdvanceSeconds
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CloneTransition failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs reorder selected as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="movement">Movement value supplied to the editor state operation and used when producing its result.</param>
    private void ReorderSelected(int movement)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ReorderSelected.");
                    var block = LayerSelectionBlock();
                    if (block.Count == 0) return;
                    var ordered = OrderedElements();
                    var selectedIds = block.Select(item => item.Id).ToHashSet();
                    var currentIndex = ordered.FindIndex(item => selectedIds.Contains(item.Id));
                    if (currentIndex < 0) return;
                    var remaining = ordered.Where(item => !selectedIds.Contains(item.Id)).ToList();
                    var targetIndex = movement switch
                    {
                        int.MaxValue => remaining.Count,
                        int.MinValue => 0,
                        > 0 => Math.Min(remaining.Count, currentIndex + 1),
                        < 0 => Math.Max(0, currentIndex - 1),
                        _ => currentIndex
                    };
                    if (targetIndex == currentIndex && HasNormalizedZOrder(ordered)) return;
                    Capture();
                    remaining.InsertRange(targetIndex, block);
                    ApplyNormalizedZOrder(remaining);
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ReorderSelected failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clipboard selection as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationElement> ClipboardSelection()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClipboardSelection.");
                    if (SelectedElement is null) return [];
                    var selected = SelectedElements.ToList();
                    var selectedObjectIds = selected
                        .Where(element => element is not ConnectorElement)
                        .Select(element => element.Id)
                        .ToHashSet();
                    var connected = CurrentPage.Elements
                        .OfType<ConnectorElement>()
                        .Where(connector => selectedObjectIds.Contains(connector.Source.ElementId)
                            && selectedObjectIds.Contains(connector.Target.ElementId));
                    return selected
                        .Concat(connected)
                        .DistinctBy(element => element.Id)
                        .OrderBy(element => element.ZIndex)
                        .ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClipboardSelection failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone selection as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sources">Publication element dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="useInsertionPoint">Value indicating whether use insertion point should apply to this operation.</param>
    private void CloneSelection(IReadOnlyList<PublicationElement> sources, bool useInsertionPoint)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CloneSelection.");
                    if (sources.Count == 0) return;
                    var objectSources = sources.Where(source => source is not ConnectorElement).ToList();
                    if (objectSources.Count == 0 && sources.All(source => source is ConnectorElement)) return;

                    Capture();
                    var idMap = sources.ToDictionary(source => source.Id, _ => Guid.NewGuid());
                    var groupMap = sources
                        .Where(source => source.GroupId is not null)
                        .Select(source => source.GroupId!.Value)
                        .Distinct()
                        .ToDictionary(groupId => groupId, _ => Guid.NewGuid());
                    var sharedComponentMap = sources.OfType<DevExtremeComponentElement>()
                        .Where(component => component.Scope == PublicationComponentScope.Document && component.SharedComponentId is not null)
                        .Select(component => component.SharedComponentId!.Value)
                        .Distinct()
                        .ToDictionary(sharedId => sharedId, _ => Guid.NewGuid());

                    var left = objectSources.Count > 0 ? objectSources.Min(source => source.X) : 0;
                    var top = objectSources.Count > 0 ? objectSources.Min(source => source.Y) : 0;
                    var right = objectSources.Count > 0 ? objectSources.Max(source => source.X + source.Width) : left;
                    var bottom = objectSources.Count > 0 ? objectSources.Max(source => source.Y + source.Height) : top;
                    var offsetX = 5d;
                    var offsetY = 5d;
                    if (useInsertionPoint && _lastInsertionX is { } insertionX && _lastInsertionY is { } insertionY)
                    {
                        offsetX = insertionX - (left + right) / 2;
                        offsetY = insertionY - (top + bottom) / 2;
                    }

                    var nextZ = NextZ();
                    var nextAnimationOrder = NextAnimationOrder();
                    var clones = new List<PublicationElement>();
                    foreach (var source in sources.OrderBy(source => source.ZIndex))
                    {
                        var clone = CloneElement(source);
                        clone.Id = idMap[source.Id];
                        clone.GroupId = source.GroupId is { } groupId && groupMap.TryGetValue(groupId, out var newGroupId)
                            ? newGroupId
                            : null;
                        clone.Name = NextName(source.Name);
                        clone.ZIndex = nextZ++;
                        if (clone is DevExtremeComponentElement componentClone)
                        {
                            componentClone.SharedComponentId = componentClone.Scope == PublicationComponentScope.Document
                                && source is DevExtremeComponentElement sourceComponent
                                && sourceComponent.SharedComponentId is { } sourceSharedId
                                && sharedComponentMap.TryGetValue(sourceSharedId, out var newSharedId)
                                    ? newSharedId
                                    : null;
                            foreach (var action in componentClone.Actions)
                            {
                                if (action.TargetElementId is { } actionTargetId && idMap.TryGetValue(actionTargetId, out var mappedActionTarget))
                                    action.TargetElementId = mappedActionTarget;
                                if (action.TargetSharedComponentId is { } actionSharedId && sharedComponentMap.TryGetValue(actionSharedId, out var mappedSharedTarget))
                                    action.TargetSharedComponentId = mappedSharedTarget;
                            }
                        }

                        if (clone is ConnectorElement connector)
                        {
                            if (idMap.TryGetValue(connector.Source.ElementId, out var mappedSource))
                                connector.Source.ElementId = mappedSource;
                            else if (CurrentPage.Elements.All(element => element.Id != connector.Source.ElementId))
                                continue;
                            if (idMap.TryGetValue(connector.Target.ElementId, out var mappedTarget))
                                connector.Target.ElementId = mappedTarget;
                            else if (CurrentPage.Elements.All(element => element.Id != connector.Target.ElementId))
                                continue;
                        }
                        else
                        {
                            clone.X = Math.Clamp(source.X + offsetX, -clone.Width + 2, CurrentPage.WidthMm - 2);
                            clone.Y = Math.Clamp(source.Y + offsetY, -clone.Height + 2, CurrentPage.HeightMm - 2);
                        }

                        if (clone.Interaction.TargetElementId is { } targetId)
                        {
                            if (idMap.TryGetValue(targetId, out var mappedTarget)) clone.Interaction.TargetElementId = mappedTarget;
                            else if (CurrentPage.Elements.All(element => element.Id != targetId)) clone.Interaction.TargetElementId = null;
                        }
                        if (clone.Interaction.TargetPageId is { } targetPageId && Document.Pages.All(page => page.Id != targetPageId))
                            clone.Interaction.TargetPageId = null;

                        foreach (var animation in clone.Animations)
                        {
                            animation.Id = Guid.NewGuid();
                            animation.Order = nextAnimationOrder++;
                        }

                        CurrentPage.Elements.Add(clone);
                        if (source is PublicationMediaElement) _mediaAssets.Copy(source.Id, clone.Id);
                        clones.Add(clone);
                    }

                    foreach (var component in clones.OfType<DevExtremeComponentElement>().Where(component => component.Scope == PublicationComponentScope.Document))
                        SynchronizeDocumentComponent(component);
                    ReindexAnimations();
                    ApplyNormalizedZOrder(OrderedElements());
                    SetSelectionCore(clones.Select(clone => clone.Id), clones.FirstOrDefault()?.Id);
                    CropMode = false;
                    ContentPanMode = false;
                    ConnectorTool = ConnectorToolKind.None;
                    Notify();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CloneSelection failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs layer selection block as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationElement> LayerSelectionBlock()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.LayerSelectionBlock.");
                    if (SelectedElement is null) return [];
                    IEnumerable<PublicationElement> source = _selectedElementIds.Count > 1 ? SelectedElements : SelectionUnit(SelectedElement);
                    var ids = source
                        .Select(item => item.Id)
                        .ToHashSet();
                    return OrderedElements().Where(item => ids.Contains(item.Id)).ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.LayerSelectionBlock failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs transform selection block as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationElement> TransformSelectionBlock()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.TransformSelectionBlock.");
                    if (SelectedElement is null) return [];
                    IEnumerable<PublicationElement> source = _selectedElementIds.Count > 1 ? SelectedElements : SelectionUnit(SelectedElement);
                    return source.Where(item => !item.Locked && item is not ConnectorElement).DistinctBy(item => item.Id).ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.TransformSelectionBlock failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs minimum element size as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The double width double height produced by the operation.</returns>
    private (double Width, double Height) MinimumElementSize(PublicationElement element) => element switch
    {
        DataVisualElement { VisualKind: DataVisualKind.Sparkline } => (55, 18),
        DataVisualElement { VisualKind: DataVisualKind.KpiProgress } => (60, 24),
        DataVisualElement { VisualKind: DataVisualKind.LinearGauge } => (70, 24),
        DataVisualElement { VisualKind: DataVisualKind.DataTable } => (80, 48),
        DataVisualElement => (75, 55),
        VideoElement => (35, 22),
        AudioElement => (45, 16),
        TextFrameElement => (15, 10),
        WordArtElement => (25, 12),
        BarcodeElement => (22, 22),
        SpreadsheetElement => (35, 24),
        _ => (5, 5)
    };

    /// <summary>
    /// Performs ordered elements as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationElement> OrderedElements() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.OrderedElements.");
            return CurrentPage.Elements
        .Select((element, index) => new { Element = element, Index = index })
        .OrderBy(item => item.Element.ZIndex)
        .ThenBy(item => item.Index)
        .Select(item => item.Element)
        .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.OrderedElements failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether normalized z order as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ordered">Publication element dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool HasNormalizedZOrder(IReadOnlyList<PublicationElement> ordered)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.HasNormalizedZOrder.");
                    for (var index = 0; index < ordered.Count; index++)
                        if (ordered[index].ZIndex != index + 1) return false;
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.HasNormalizedZOrder failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies normalized z order as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ordered">Publication element dependency used by the editor state workflow to provide the corresponding application capability.</param>
    private void ApplyNormalizedZOrder(IReadOnlyList<PublicationElement> ordered)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ApplyNormalizedZOrder.");
                    for (var index = 0; index < ordered.Count; index++) ordered[index].ZIndex = index + 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ApplyNormalizedZOrder failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs nearly equal as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="first">First value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool NearlyEqual(double first, double second) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NearlyEqual.");
            return Math.Abs(first - second) < .0001;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NearlyEqual failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs next z as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The int produced by the operation.</returns>
    private int NextZ() {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NextZ.");
            return CurrentPage.Elements.Select(e => e.ZIndex).DefaultIfEmpty(0).Max() + 1;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NextZ failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs next name as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="basis">Basis value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NextName(string basis) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.NextName.");
            return $"{basis} {CurrentPage.Elements.Count + 1}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.NextName failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs selection unit as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<PublicationElement> SelectionUnit(PublicationElement element)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SelectionUnit.");
                    if (element.GroupId is not { } groupId) return [element];
                    return CurrentPage.Elements.Where(item => item.GroupId == groupId);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SelectionUnit failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs movable selection for as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<PublicationElement> MovableSelectionFor(PublicationElement element)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.MovableSelectionFor.");
                    if (_selectedElementIds.Contains(element.Id) && _selectedElementIds.Count > 1)
                        return SelectedElements;
                    return SelectionUnit(element);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.MovableSelectionFor failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets selection core as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ids">Guid dependency used by the editor state workflow to provide the corresponding application capability.</param>
    /// <param name="primary">Primary value supplied to the editor state operation and used when producing its result.</param>
    private void SetSelectionCore(IEnumerable<Guid> ids, Guid? primary)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SetSelectionCore.");
                    _selectedElementIds.Clear();
                    foreach (var id in ids)
                        if (CurrentPage.Elements.Any(element => element.Id == id))
                            _selectedElementIds.Add(id);
                    SelectedElementId = primary is { } value && _selectedElementIds.Contains(value)
                        ? value
                        : _selectedElementIds.Count > 0 ? _selectedElementIds.Last() : null;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SetSelectionCore failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clear selection core as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ClearSelectionCore()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClearSelectionCore.");
                    _selectedElementIds.Clear();
                    SelectedElementId = null;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClearSelectionCore failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs place at as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the editor state operation and used when producing its result.</param>
    private void PlaceAt(PublicationElement element, double? centerX, double? centerY)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.PlaceAt.");
                    var x = centerX ?? _lastInsertionX;
                    var y = centerY ?? _lastInsertionY;
                    if (x is null || y is null) return;
                    element.X = Math.Clamp(x.Value - element.Width / 2, -element.Width + 2, CurrentPage.WidthMm - 2);
                    element.Y = Math.Clamp(y.Value - element.Height / 2, -element.Height + 2, CurrentPage.HeightMm - 2);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.PlaceAt failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Removes media assets as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the editor state operation and used when producing its result.</param>
    private void RemoveMediaAssets(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.RemoveMediaAssets.");
                    foreach (var media in _elementTraversal.Descendants(document).OfType<PublicationMediaElement>())
                    {
                        _mediaAssets.Remove(media.Id);
                        foreach (var segment in media.Segments) _mediaAssets.Remove(segment.Id);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.RemoveMediaAssets failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs capture as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void Capture()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Capture.");
                    _liveEditKey = null;
                    _undo.Push(_files.Serialize(Document));
                    if (_undo.Count > 100)
                    {
                        var newest = _undo.Take(100).Reverse().ToArray();
                        _undo.Clear();
                        foreach (var item in newest) _undo.Push(item);
                    }
                    _redo.Clear();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Capture failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs synchronize document component as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the editor state operation and used when producing its result.</param>
    private void SynchronizeDocumentComponent(DevExtremeComponentElement source)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.SynchronizeDocumentComponent.");
                    if (source.Scope != PublicationComponentScope.Document) return;
                    source.SharedComponentId ??= Guid.NewGuid();
                    foreach (var page in Document.Pages)
                    {
                        var target = page.Elements.OfType<DevExtremeComponentElement>()
                            .FirstOrDefault(component => component.Id != source.Id && component.SharedComponentId == source.SharedComponentId);
                        if (target is null)
                        {
                            if (page.Elements.Contains(source)) continue;
                            target = _components.Clone(source);
                            target.Id = Guid.NewGuid();
                            target.X = Math.Clamp(target.X, -target.Width + 2, page.WidthMm - 2);
                            target.Y = Math.Clamp(target.Y, -target.Height + 2, page.HeightMm - 2);
                            target.ZIndex = page.Elements.Count == 0 ? 1 : page.Elements.Max(element => element.ZIndex) + 1;
                            page.Elements.Add(target);
                        }
                        else _components.CopyConfiguration(source, target, preservePlacement: true);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.SynchronizeDocumentComponent failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone element as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The publication element produced by the operation.</returns>
    private PublicationElement CloneElement(PublicationElement element) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.CloneElement.");
            return _files.CloneElement(element);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.CloneElement failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone page as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="publicationPage">Publication page value supplied to the editor state operation and used when producing its result.</param>
    /// <returns>The publication page produced by the operation.</returns>
    private PublicationPage ClonePage(PublicationPage publicationPage) {
        try
        {
            logger.LogTrace($"Entering EditorStateService.ClonePage.");
            return _files.ClonePage(publicationPage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.ClonePage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs notify as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="markModified">Value indicating whether mark modified should apply to this operation.</param>
    private void Notify(bool markModified = true)
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Notify.");
                    if (markModified)
                    {
                        Document.ModifiedUtc = DateTimeOffset.UtcNow;
                        IsDirty = true;
                        Revision++;
                    }
                    _liveData.Register(Document, _data, SelectedPageId);
                    Changed?.Invoke();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Notify failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Persists streaming settings as part of the editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void PersistStreamingSettings()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.PersistStreamingSettings.");
                    try { _streamingSettings.Save(Document.Id, Document.Streaming); }
                    catch { }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.PersistStreamingSettings failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="EditorStateService"/> and leaves the editor state workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            logger.LogTrace($"Entering EditorStateService.Dispose.");
                    PersistStreamingSettings();
                    _liveData.Unregister(Document.Id);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"EditorStateService.Dispose failed: {exception.Message}");
            throw;
        }
    }
}
