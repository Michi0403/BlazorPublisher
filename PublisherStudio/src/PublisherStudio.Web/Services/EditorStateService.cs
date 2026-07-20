using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class EditorStateService : IDisposable
{
    private readonly PublicationFileService _files;
    private readonly PublicationDataService _data;
    private readonly PublicationComponentService _components;
    private readonly PublicationMediaAssetStore _mediaAssets;
    private readonly SpreadsheetDocumentService _spreadsheets;
    private readonly PublicationLiveDataRegistry _liveData;
    private readonly PublicationWebDataService _webData;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly List<PublicationElement> _clipboard = [];
    private readonly HashSet<Guid> _selectedElementIds = [];
    private string? _liveEditKey;
    private double? _lastInsertionX;
    private double? _lastInsertionY;

    public EditorStateService(
        PublicationFileService files,
        PublicationDataService data,
        PublicationComponentService components,
        PublicationMediaAssetStore mediaAssets,
        SpreadsheetDocumentService spreadsheets,
        PublicationLiveDataRegistry liveData,
        PublicationWebDataService webData)
    {
        _files = files;
        _data = data;
        _components = components;
        _mediaAssets = mediaAssets;
        _spreadsheets = spreadsheets;
        _liveData = liveData;
        _webData = webData;
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
        _liveData.Register(Document, _data, SelectedPageId);
    }

    public event Action? Changed;
    public PublicationDocument Document { get; private set; }
    public Guid SelectedPageId { get; private set; }
    public Guid? SelectedElementId { get; private set; }
    public bool IsDirty { get; private set; }
    public long Revision { get; private set; }
    public bool CropMode { get; private set; }
    public ConnectorToolKind ConnectorTool { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public bool CanPaste => _clipboard.Count > 0;
    public long ClipboardRevision { get; private set; }
    public PublicationPage CurrentPage => Document.Pages.First(p => p.Id == SelectedPageId);
    public PublicationElement? SelectedElement => CurrentPage.Elements.FirstOrDefault(e => e.Id == SelectedElementId);
    public IReadOnlyList<PublicationElement> SelectedElements => CurrentPage.Elements
        .Where(element => _selectedElementIds.Contains(element.Id))
        .OrderBy(element => element.ZIndex)
        .ToList();
    public IReadOnlyCollection<Guid> SelectedElementIds => _selectedElementIds;
    public bool HasMultipleSelection => _selectedElementIds.Count > 1;
    public bool CanGroupSelection => SelectedElements.Count(element => element is not ConnectorElement && !element.Locked) > 1;
    public bool CanUngroupSelection => SelectedElements.Any(element => element.GroupId is not null);
    public bool IsSelected(Guid id) => _selectedElementIds.Contains(id);

    public void NewDocument()
    {
        RemoveMediaAssets(Document);
        _liveData.Unregister(Document.Id);
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
        ClearSelectionCore();
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        _undo.Clear();
        _redo.Clear();
        _liveEditKey = null;
        _lastInsertionX = null;
        _lastInsertionY = null;
        Notify();
    }

    public void Load(string json)
    {
        RemoveMediaAssets(Document);
        _liveData.Unregister(Document.Id);
        Document = _files.Deserialize(json);
        _mediaAssets.RegisterDocument(Document);
        SelectedPageId = Document.Pages[0].Id;
        ClearSelectionCore();
        CropMode = false;
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

    public void LoadRecovery(string json)
    {
        Load(json);
        IsDirty = true;
        Revision++;
        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        IsDirty = false;
        Revision++;
        Changed?.Invoke();
    }

    public void RenameDocument(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Untitled Publication" : value.Trim();
        if (string.Equals(Document.Name, value, StringComparison.Ordinal)) return;
        Capture();
        Document.Name = value;
        Notify();
    }

    public void SetInsertionPoint(double x, double y)
    {
        _lastInsertionX = Math.Clamp(x, 0, CurrentPage.WidthMm);
        _lastInsertionY = Math.Clamp(y, 0, CurrentPage.HeightMm);
    }

    public void SelectPage(Guid id)
    {
        if (Document.Pages.All(p => p.Id != id)) return;
        SelectedPageId = id;
        ClearSelectionCore();
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        EndLiveEdit();
        _lastInsertionX = null;
        _lastInsertionY = null;
        Notify(false);
    }

    public void SelectElement(Guid? id, bool additive = false)
    {
        if (id is null)
        {
            if (_selectedElementIds.Count == 0 && SelectedElementId is null) return;
            ClearSelectionCore();
            CropMode = false;
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
        if (cropChanged) CropMode = false;
        EndLiveEdit();
        if (!previousSelection.SetEquals(_selectedElementIds) || previousPrimary != SelectedElementId || cropChanged) Notify(false);
    }

    public void SetSelection(IEnumerable<Guid> ids)
    {
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
        if (cropChanged) CropMode = false;
        EndLiveEdit();
        if (!previousSelection.SetEquals(_selectedElementIds) || previousPrimary != SelectedElementId || cropChanged)
            Notify(false);
    }

    public void SetPrimarySelection(Guid id)
    {
        if (!_selectedElementIds.Contains(id))
        {
            SelectElement(id);
            return;
        }
        if (SelectedElementId == id) return;
        SelectedElementId = id;
        if (CropMode && (_selectedElementIds.Count != 1 || SelectedElement is not ImageFrameElement)) CropMode = false;
        EndLiveEdit();
        Notify(false);
    }

    public void GroupSelected()
    {
        var elements = SelectedElements.Where(element => element is not ConnectorElement && !element.Locked).ToList();
        if (elements.Count < 2) return;
        Capture();
        var groupId = Guid.NewGuid();
        foreach (var element in elements) element.GroupId = groupId;
        SetSelectionCore(elements.Select(element => element.Id), SelectedElementId);
        Notify();
    }

    public void UngroupSelected()
    {
        var selectedIds = _selectedElementIds.ToArray();
        var groupIds = SelectedElements.Where(element => element.GroupId is not null).Select(element => element.GroupId!.Value).ToHashSet();
        if (groupIds.Count == 0) return;
        Capture();
        var affected = CurrentPage.Elements.Where(element => element.GroupId is { } groupId && groupIds.Contains(groupId)).ToList();
        foreach (var element in affected) element.GroupId = null;
        SetSelectionCore(selectedIds, SelectedElementId);
        Notify();
    }

    public TextFrameElement AddText(double? centerX = null, double? centerY = null)
    {
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
            DocumentContent = RichTextDocumentFactory.CreateOpenXml("New text box"),
            StoryFormat = StoryStorageFormat.OpenXml
        };
        PlaceAt(element, centerX, centerY);
        CurrentPage.Elements.Add(element);
        SetSelectionCore([element.Id], element.Id);
        Notify();
        return element;
    }

    public SpreadsheetElement AddSpreadsheet(byte[] content, string fileName, SpreadsheetStorageFormat format, double? centerX = null, double? centerY = null)
    {
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

    public ImageFrameElement AddImage(string dataUrl, string name, PictureDocument? pictureSource = null, double? centerX = null, double? centerY = null, int pixelWidth = 0, int pixelHeight = 0)
    {
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

    public VideoElement AddVideo(string dataUrl, string mimeType, string name, double durationSeconds, string posterDataUrl = "", double? centerX = null, double? centerY = null)
    {
        Capture();
        mimeType = PublicationMediaData.NormalizeMimeType(mimeType, "video/webm");
        dataUrl = PublicationMediaData.NormalizeDataUrl(dataUrl, mimeType);
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

    public AudioElement AddAudio(string dataUrl, string mimeType, string name, double durationSeconds, IReadOnlyList<double>? waveformSamples = null, double? centerX = null, double? centerY = null)
    {
        Capture();
        mimeType = PublicationMediaData.NormalizeMimeType(mimeType, "audio/webm");
        dataUrl = PublicationMediaData.NormalizeDataUrl(dataUrl, mimeType);
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

    public void UpdateMedia(Guid id, Action<PublicationMediaElement> update, bool capture = true)
    {
        var media = CurrentPage.Elements.OfType<PublicationMediaElement>().FirstOrDefault(item => item.Id == id);
        if (media is null || media.Locked) return;
        if (capture) Capture();
        update(media);
        NormalizeMedia(media);
        EnsureTimelineDuration();
        SetSelectionCore([media.Id], media.Id);
        Notify();
    }

    public void UpdateMediaLive(Guid id, string key, Action<PublicationMediaElement> update)
    {
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



    public WordArtElement AddWordArt(double? centerX = null, double? centerY = null)
    {
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

    public PublicationDataObject EnsureDataObject()
    {
        if (Document.DataObjects.Count > 0) return Document.DataObjects[0];
        var data = _data.CreateSample();
        Document.DataObjects.Add(data);
        return data;
    }

    public BarcodeElement AddBarcode(double? centerX = null, double? centerY = null)
    {
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

    public DataVisualElement AddDataVisual(DataVisualKind kind, double? centerX = null, double? centerY = null)
    {
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

    public DevExtremeComponentElement AddDevExtremeComponent(PublicationComponentKind kind, double? centerX = null, double? centerY = null)
    {
        Capture();
        var element = _components.Create(Document, kind);
        element.Name = NextName(PublicationComponentService.ComponentName(kind));
        element.ZIndex = NextZ();
        PlaceAt(element, centerX, centerY);
        CurrentPage.Elements.Add(element);
        SetSelectionCore([element.Id], element.Id);
        Notify();
        return element;
    }

    public void ApplySelectedComponent(DevExtremeComponentElement draft)
    {
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

    public void SetSelectedComponentScope(PublicationComponentScope scope)
    {
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

    public void UpsertDataObject(PublicationDataObject value)
    {
        Capture();
        var normalized = _data.Clone(value);
        _data.ParseInto(normalized);
        var index = Document.DataObjects.FindIndex(data => data.Id == normalized.Id);
        if (index < 0) Document.DataObjects.Add(normalized);
        else Document.DataObjects[index] = normalized;
        Notify();
    }

    public bool DeleteDataObject(Guid id)
    {
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

    public void RefreshDataVisuals() => Notify(false);

    public bool HasDueWebData => Document.DataObjects.Any(data => _webData.IsDue(data, DateTimeOffset.UtcNow));

    public async Task RefreshWebDataAsync(Guid? dataId = null, bool force = true, CancellationToken cancellationToken = default)
    {
        var candidates = Document.DataObjects
            .Where(data => data.SourceKind == PublicationDataSourceKind.Web
                && data.Web.Enabled
                && (dataId is null || data.Id == dataId.Value)
                && (force || _webData.IsDue(data, DateTimeOffset.UtcNow)))
            .ToArray();
        await RefreshWebDataObjectsAsync(candidates, cancellationToken);
    }

    public Task RefreshWebDataOnOpenAsync(CancellationToken cancellationToken = default)
        => RefreshWebDataObjectsAsync(Document.DataObjects
            .Where(data => data.SourceKind == PublicationDataSourceKind.Web
                && data.Web.Enabled
                && data.Web.RefreshOnOpen)
            .ToArray(), cancellationToken);

    private async Task RefreshWebDataObjectsAsync(IReadOnlyList<PublicationDataObject> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0) return;
        foreach (var data in candidates)
        {
            try { await _webData.RefreshAsync(data, cancellationToken); }
            catch when (data.Web.UseSnapshotOnFailure && data.Rows.Count > 0) { }
        }
        Notify(false);
    }

    private static string DataVisualName(DataVisualKind kind) => kind switch
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

    public ConnectorElement? AddConnector(Guid sourceElementId, ConnectorAnchor sourceAnchor, Guid targetElementId, ConnectorAnchor targetAnchor, ConnectorMarker endMarker = ConnectorMarker.Arrow)
    {
        if (sourceElementId == targetElementId) return null;
        var source = CurrentPage.Elements.FirstOrDefault(item => item.Id == sourceElementId && item is not ConnectorElement);
        var target = CurrentPage.Elements.FirstOrDefault(item => item.Id == targetElementId && item is not ConnectorElement);
        if (source is null || target is null) return null;
        Capture();
        var connector = new ConnectorElement
        {
            Name = NextName(endMarker == ConnectorMarker.None ? "Connector" : "Arrow Connector"),
            Source = new ConnectorEndpoint { ElementId = sourceElementId, Anchor = sourceAnchor },
            Target = new ConnectorEndpoint { ElementId = targetElementId, Anchor = targetAnchor },
            EndMarker = endMarker,
            PathKind = ConnectorPathKind.Curved,
            ZIndex = NextZ()
        };
        CurrentPage.Elements.Add(connector);
        SetSelectionCore([connector.Id], connector.Id);
        Notify();
        return connector;
    }

    public void ReconnectConnector(Guid connectorId, bool sourceEnd, Guid elementId, ConnectorAnchor anchor)
    {
        var connector = CurrentPage.Elements.OfType<ConnectorElement>().FirstOrDefault(item => item.Id == connectorId);
        var target = CurrentPage.Elements.FirstOrDefault(item => item.Id == elementId && item is not ConnectorElement);
        if (connector is null || target is null || connector.Locked) return;
        var otherId = sourceEnd ? connector.Target.ElementId : connector.Source.ElementId;
        if (otherId == elementId) return;
        Capture();
        var endpoint = sourceEnd ? connector.Source : connector.Target;
        endpoint.ElementId = elementId;
        endpoint.Anchor = anchor;
        SetSelectionCore([connector.Id], connector.Id);
        Notify();
    }

    public void SetConnectorTool(ConnectorToolKind tool)
    {
        ConnectorTool = ConnectorTool == tool ? ConnectorToolKind.None : tool;
        CropMode = false;
        Notify(false);
    }

    public void CancelActiveTool()
    {
        ConnectorTool = ConnectorToolKind.None;
        CropMode = false;
        Notify(false);
    }

    public ShapeElement AddShape(PublicationShape shape, double? centerX = null, double? centerY = null)
    {
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

    public void AddPage()
    {
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

    public void DuplicatePage()
    {
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

    public void DeletePage()
    {
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

    public void DeleteSelected()
    {
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
            if (element is PublicationMediaElement) _mediaAssets.Remove(element.Id);
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
        Notify();
    }

    public void CopySelected()
    {
        var sources = ClipboardSelection();
        if (sources.Count == 0) return;
        _clipboard.Clear();
        _clipboard.AddRange(sources.Select(CloneElement));
        ClipboardRevision++;
        Notify(false);
    }

    public void CutSelected()
    {
        if (SelectedElements.Count == 0) return;
        CopySelected();
        DeleteSelected();
    }

    public void Paste()
    {
        if (_clipboard.Count == 0) return;
        CloneSelection(_clipboard, useInsertionPoint: true);
    }

    public void DuplicateSelected()
    {
        var sources = ClipboardSelection();
        if (sources.Count == 0) return;
        CloneSelection(sources, useInsertionPoint: false);
    }

    public void SelectAll()
    {
        var ids = CurrentPage.Elements.Where(element => element.Visible).Select(element => element.Id).ToArray();
        if (ids.Length == 0) return;
        SetSelectionCore(ids, ids[0]);
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        EndLiveEdit();
        Notify(false);
    }

    public void NudgeSelection(double dx, double dy)
    {
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

    public PublicationAnimation? AddAnimation(
        PublicationAnimationEffect effect,
        PublicationAnimationPhase phase,
        PublicationAnimationTrigger? trigger = null)
    {
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

    public void UpdateAnimation(Guid animationId, Action<PublicationAnimation> update)
    {
        var animation = FindAnimation(animationId);
        if (animation is null) return;
        Capture();
        update(animation);
        NormalizeAnimation(animation);
        EnsureTimelineDuration();
        Notify();
    }

    public void UpdateAnimationLive(Guid animationId, string key, Action<PublicationAnimation> update)
    {
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

    public PublicationAnimation? DuplicateAnimation(Guid animationId)
    {
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

    public void DeleteAnimation(Guid animationId)
    {
        var owner = CurrentPage.Elements.FirstOrDefault(item => item.Animations.Any(animation => animation.Id == animationId));
        var animation = owner?.Animations.FirstOrDefault(item => item.Id == animationId);
        if (owner is null || animation is null) return;
        Capture();
        owner.Animations.Remove(animation);
        ReindexAnimations();
        EnsureTimelineDuration();
        Notify();
    }

    public void MoveAnimation(Guid animationId, int offset)
    {
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

    public void ClearSelectedAnimations()
    {
        var element = SelectedElement;
        if (element is null || element.Animations.Count == 0) return;
        Capture();
        element.Animations.Clear();
        ReindexAnimations();
        Notify();
    }

    public void UpdateInteraction(Action<PublicationInteraction> update)
    {
        var element = SelectedElement;
        if (element is null) return;
        Capture();
        element.Interaction ??= new PublicationInteraction();
        update(element.Interaction);
        Notify();
    }

    public void UpdatePageTransition(Action<PublicationPageTransition> update)
    {
        Capture();
        CurrentPage.Transition ??= new PublicationPageTransition();
        update(CurrentPage.Transition);
        CurrentPage.Transition.DurationSeconds = Math.Clamp(CurrentPage.Transition.DurationSeconds, .1, 8);
        CurrentPage.Transition.AutoAdvanceSeconds = Math.Clamp(CurrentPage.Transition.AutoAdvanceSeconds, .25, 3600);
        Notify();
    }

    public void UpdatePageTransitionLive(string key, Action<PublicationPageTransition> update)
    {
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

    public void SetAnimationTimelineRange(Guid animationId, double startSeconds, double durationSeconds)
    {
        var animation = FindAnimation(animationId);
        if (animation is null) return;
        Capture();
        animation.TimelineStartSeconds = Math.Clamp(startSeconds, 0, 3600);
        var playbackMultiplier = Math.Max(1, animation.RepeatCount) * (animation.AutoReverse ? 2 : 1);
        animation.DurationSeconds = Math.Clamp(durationSeconds / playbackMultiplier, .05, 60);
        EnsureTimelineDuration();
        Notify();
    }

    public void ClearAnimationTimelinePosition(Guid animationId)
    {
        var animation = FindAnimation(animationId);
        if (animation is null || animation.TimelineStartSeconds is null) return;
        Capture();
        animation.TimelineStartSeconds = null;
        Notify();
    }

    public void SetMediaTimelineRange(Guid elementId, string mode, double startSeconds, double lengthSeconds)
    {
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

    public void SetPageTimelineDuration(double seconds)
    {
        Capture();
        CurrentPage.TimelineDurationSeconds = Math.Clamp(seconds, 1, 3600);
        EnsureTimelineDuration();
        Notify();
    }

    public double EffectiveAnimationStart(PublicationAnimation target)
    {
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

    public double EffectivePageTimelineDuration()
    {
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

    public void CommitMove(Guid id, double x, double y, IReadOnlyCollection<Guid>? movingIds = null)
    {
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
        Notify();
    }

    public void CommitBounds(Guid id, double x, double y, double width, double height)
    {
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

    public void CommitCrop(Guid id, double cropX, double cropY, double cropScale)
    {
        var image = CurrentPage.Elements.OfType<ImageFrameElement>().FirstOrDefault(e => e.Id == id);
        if (image is null || image.Locked) return;
        Capture();
        image.CropX = Math.Clamp(cropX, -100, 100);
        image.CropY = Math.Clamp(cropY, -100, 100);
        image.CropScale = Math.Clamp(cropScale, .2, 8);
        Notify();
    }

    public void UpdateSelected(Action<PublicationElement> update, bool capture = true, bool allowLocked = false)
    {
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

    public void UpdateSelectedLive(string key, Action<PublicationElement> update)
    {
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

    public void EndLiveEdit() => _liveEditKey = null;

    public void UpdatePage(Action<PublicationPage> update)
    {
        Capture();
        update(CurrentPage);
        Notify();
    }

    public void SetPageSize(double widthMm, double heightMm)
    {
        if (widthMm is < 10 or > 2000 || heightMm is < 10 or > 2000) return;
        UpdatePage(publicationPage =>
        {
            publicationPage.WidthMm = widthMm;
            publicationPage.HeightMm = heightMm;
        });
    }

    public void SwapPageOrientation() => SetPageSize(CurrentPage.HeightMm, CurrentPage.WidthMm);

    public void UpdateSpreadsheetDocument(byte[] content, string fileName, SpreadsheetStorageFormat format, string previewHtml, string activeSheetName)
    {
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

    public void UpdateTextDocument(byte[] content, string previewHtml, string documentBackground, StoryStorageFormat format = StoryStorageFormat.OpenXml)
    {
        if (SelectedElement is not TextFrameElement text || text.Locked) return;
        Capture();
        text.DocumentContent = content;
        text.PreviewHtml = PublicationFileService.SanitizePreviewHtml(previewHtml);
        text.DocumentBackground = PublicationFileService.NormalizeCssBackground(documentBackground);
        text.StoryFormat = format;
        Notify();
    }

    public void ToggleCropMode()
    {
        if (SelectedElement is not ImageFrameElement) return;
        CropMode = !CropMode;
        Notify(false);
    }

    public void AddGuide(GuideOrientation orientation)
    {
        AddGuideAt(orientation, orientation == GuideOrientation.Vertical ? CurrentPage.WidthMm / 2 : CurrentPage.HeightMm / 2);
    }

    public void AddGuideAt(GuideOrientation orientation, double positionMm)
    {
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

    public void CommitGuide(Guid id, double positionMm)
    {
        var guide = CurrentPage.Guides.FirstOrDefault(item => item.Id == id);
        if (guide is null) return;
        var max = guide.Orientation == GuideOrientation.Vertical ? CurrentPage.WidthMm : CurrentPage.HeightMm;
        Capture();
        guide.PositionMm = Math.Clamp(positionMm, 0, max);
        Notify();
    }

    public void DeleteGuide(Guid id)
    {
        var guide = CurrentPage.Guides.FirstOrDefault(item => item.Id == id);
        if (guide is null) return;
        Capture();
        CurrentPage.Guides.Remove(guide);
        Notify();
    }

    public void ClearGuides()
    {
        if (CurrentPage.Guides.Count == 0) return;
        Capture();
        CurrentPage.Guides.Clear();
        Notify();
    }

    public void SetZoom(double zoom)
    {
        Document.Zoom = Math.Clamp(zoom, .2, 4);
        Notify(false);
    }

    public void ZoomBy(double factor) => SetZoom(Document.Zoom * factor);

    public void SetRulerUnit(MeasurementUnit unit)
    {
        Document.View.RulerUnit = unit;
        Notify(false);
    }

    public void CycleRulerUnit()
    {
        var values = Enum.GetValues<MeasurementUnit>();
        var index = Array.IndexOf(values, Document.View.RulerUnit);
        SetRulerUnit(values[(index + 1) % values.Length]);
    }

    public void SetViewOption(Action<PublicationViewSettings> update)
    {
        update(Document.View);
        Document.View.GridSpacingMm = Math.Clamp(Document.View.GridSpacingMm, .5, 100);
        Document.View.ExportDpi = Math.Clamp(Document.View.ExportDpi, 72, 600);
        Notify(false);
    }

    public void SetPlayback(PublicationPlaybackSettings value)
    {
        Capture();
        Document.Playback = value;
        Notify();
    }

    public void BringToFront() => ReorderSelected(int.MaxValue);
    public void SendToBack() => ReorderSelected(int.MinValue);
    public void BringForward() => ReorderSelected(1);
    public void SendBackward() => ReorderSelected(-1);

    public void SetSelectedLayerPosition(int position)
    {
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

    public void Align(string mode)
    {
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

    public void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Push(_files.Serialize(Document));
        Restore(_undo.Pop());
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Push(_files.Serialize(Document));
        Restore(_redo.Pop());
    }

    private void Restore(string json)
    {
        var selectedPageIndex = Math.Max(0, Document.Pages.FindIndex(p => p.Id == SelectedPageId));
        Document = _files.Deserialize(json);
        SelectedPageId = Document.Pages[Math.Min(selectedPageIndex, Document.Pages.Count - 1)].Id;
        ClearSelectionCore();
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        _liveEditKey = null;
        Notify();
    }

    private PublicationAnimation? FindAnimation(Guid id) =>
        CurrentPage.Elements.SelectMany(item => item.Animations).FirstOrDefault(item => item.Id == id);

    private int NextAnimationOrder() =>
        CurrentPage.Elements.SelectMany(item => item.Animations).Select(item => item.Order).DefaultIfEmpty(0).Max() + 1;

    private void ReindexAnimations()
    {
        var timeline = CurrentPage.Elements.SelectMany(item => item.Animations).OrderBy(item => item.Order).ToList();
        for (var index = 0; index < timeline.Count; index++) timeline[index].Order = index + 1;
    }

    private void RenewAnimationIds(PublicationElement element, bool preserveOrder)
    {
        var nextOrder = NextAnimationOrder();
        foreach (var animation in element.Animations)
        {
            animation.Id = Guid.NewGuid();
            if (!preserveOrder) animation.Order = nextOrder++;
        }
        if (!preserveOrder) ReindexAnimations();
    }

    private void EnsureTimelineDuration()
    {
        CurrentPage.TimelineDurationSeconds = Math.Clamp(Math.Max(CurrentPage.TimelineDurationSeconds, EffectivePageTimelineDuration()), 1, 3600);
    }

    private static double AnimationSpan(PublicationAnimation animation) =>
        Math.Max(.05, animation.DurationSeconds) * Math.Max(1, animation.RepeatCount) * (animation.AutoReverse ? 2 : 1);

    private static void NormalizeMedia(PublicationMediaElement media)
    {
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
        media.MimeType = PublicationMediaData.NormalizeMimeType(media.MimeType, fallbackMimeType);
        media.DataUrl = PublicationMediaData.NormalizeDataUrl(media.DataUrl, media.MimeType);
    }

    private static void NormalizeAnimation(PublicationAnimation animation)
    {
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

    private static PublicationPageTransition CloneTransition(PublicationPageTransition source) => new()
    {
        Kind = source.Kind,
        Direction = source.Direction,
        Easing = source.Easing,
        DurationSeconds = source.DurationSeconds,
        AdvanceOnClick = source.AdvanceOnClick,
        AutoAdvance = source.AutoAdvance,
        AutoAdvanceSeconds = source.AutoAdvanceSeconds
    };

    private void ReorderSelected(int movement)
    {
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

    private List<PublicationElement> ClipboardSelection()
    {
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

    private void CloneSelection(IReadOnlyList<PublicationElement> sources, bool useInsertionPoint)
    {
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
        ConnectorTool = ConnectorToolKind.None;
        Notify();
    }

    private List<PublicationElement> LayerSelectionBlock()
    {
        if (SelectedElement is null) return [];
        IEnumerable<PublicationElement> source = _selectedElementIds.Count > 1 ? SelectedElements : SelectionUnit(SelectedElement);
        var ids = source
            .Select(item => item.Id)
            .ToHashSet();
        return OrderedElements().Where(item => ids.Contains(item.Id)).ToList();
    }

    private List<PublicationElement> TransformSelectionBlock()
    {
        if (SelectedElement is null) return [];
        IEnumerable<PublicationElement> source = _selectedElementIds.Count > 1 ? SelectedElements : SelectionUnit(SelectedElement);
        return source.Where(item => !item.Locked && item is not ConnectorElement).DistinctBy(item => item.Id).ToList();
    }

    private static (double Width, double Height) MinimumElementSize(PublicationElement element) => element switch
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

    private List<PublicationElement> OrderedElements() => CurrentPage.Elements
        .Select((element, index) => new { Element = element, Index = index })
        .OrderBy(item => item.Element.ZIndex)
        .ThenBy(item => item.Index)
        .Select(item => item.Element)
        .ToList();

    private static bool HasNormalizedZOrder(IReadOnlyList<PublicationElement> ordered)
    {
        for (var index = 0; index < ordered.Count; index++)
            if (ordered[index].ZIndex != index + 1) return false;
        return true;
    }

    private static void ApplyNormalizedZOrder(IReadOnlyList<PublicationElement> ordered)
    {
        for (var index = 0; index < ordered.Count; index++) ordered[index].ZIndex = index + 1;
    }

    private static bool NearlyEqual(double first, double second) => Math.Abs(first - second) < .0001;

    private int NextZ() => CurrentPage.Elements.Select(e => e.ZIndex).DefaultIfEmpty(0).Max() + 1;
    private string NextName(string basis) => $"{basis} {CurrentPage.Elements.Count + 1}";

    private IEnumerable<PublicationElement> SelectionUnit(PublicationElement element)
    {
        if (element.GroupId is not { } groupId) return [element];
        return CurrentPage.Elements.Where(item => item.GroupId == groupId);
    }

    private IEnumerable<PublicationElement> MovableSelectionFor(PublicationElement element)
    {
        if (_selectedElementIds.Contains(element.Id) && _selectedElementIds.Count > 1)
            return SelectedElements;
        return SelectionUnit(element);
    }

    private void SetSelectionCore(IEnumerable<Guid> ids, Guid? primary)
    {
        _selectedElementIds.Clear();
        foreach (var id in ids)
            if (CurrentPage.Elements.Any(element => element.Id == id))
                _selectedElementIds.Add(id);
        SelectedElementId = primary is { } value && _selectedElementIds.Contains(value)
            ? value
            : _selectedElementIds.Count > 0 ? _selectedElementIds.Last() : null;
    }

    private void ClearSelectionCore()
    {
        _selectedElementIds.Clear();
        SelectedElementId = null;
    }

    private void PlaceAt(PublicationElement element, double? centerX, double? centerY)
    {
        var x = centerX ?? _lastInsertionX;
        var y = centerY ?? _lastInsertionY;
        if (x is null || y is null) return;
        element.X = Math.Clamp(x.Value - element.Width / 2, -element.Width + 2, CurrentPage.WidthMm - 2);
        element.Y = Math.Clamp(y.Value - element.Height / 2, -element.Height + 2, CurrentPage.HeightMm - 2);
    }

    private void RemoveMediaAssets(PublicationDocument document)
    {
        foreach (var media in document.Pages.SelectMany(page => page.Elements).OfType<PublicationMediaElement>())
            _mediaAssets.Remove(media.Id);
    }

    private void Capture()
    {
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

    private void SynchronizeDocumentComponent(DevExtremeComponentElement source)
    {
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

    private PublicationElement CloneElement(PublicationElement element) => _files.CloneElement(element);

    private PublicationPage ClonePage(PublicationPage publicationPage) => _files.ClonePage(publicationPage);

    private void Notify(bool markModified = true)
    {
        if (markModified)
        {
            Document.ModifiedUtc = DateTimeOffset.UtcNow;
            IsDirty = true;
            Revision++;
        }
        _liveData.Register(Document, _data, SelectedPageId);
        Changed?.Invoke();
    }

    public void Dispose() => _liveData.Unregister(Document.Id);
}
