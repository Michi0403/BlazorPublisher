using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class EditorStateService
{
    private readonly PublicationFileService _files;
    private readonly PublicationDataService _data;
    private readonly PublicationMediaAssetStore _mediaAssets;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private PublicationElement? _clipboard;
    private string? _liveEditKey;

    public EditorStateService(PublicationFileService files, PublicationDataService data, PublicationMediaAssetStore mediaAssets)
    {
        _files = files;
        _data = data;
        _mediaAssets = mediaAssets;
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
    }

    public event Action? Changed;
    public PublicationDocument Document { get; private set; }
    public Guid SelectedPageId { get; private set; }
    public Guid? SelectedElementId { get; private set; }
    public bool CropMode { get; private set; }
    public ConnectorToolKind ConnectorTool { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public bool CanPaste => _clipboard is not null;
    public PublicationPage CurrentPage => Document.Pages.First(p => p.Id == SelectedPageId);
    public PublicationElement? SelectedElement => CurrentPage.Elements.FirstOrDefault(e => e.Id == SelectedElementId);

    public void NewDocument()
    {
        RemoveMediaAssets(Document);
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
        SelectedElementId = null;
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        _undo.Clear();
        _redo.Clear();
        _liveEditKey = null;
        Notify();
    }

    public void Load(string json)
    {
        RemoveMediaAssets(Document);
        Document = _files.Deserialize(json);
        _mediaAssets.RegisterDocument(Document);
        SelectedPageId = Document.Pages[0].Id;
        SelectedElementId = null;
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        _undo.Clear();
        _redo.Clear();
        _liveEditKey = null;
        Notify();
    }

    public void SelectPage(Guid id)
    {
        if (Document.Pages.All(p => p.Id != id)) return;
        SelectedPageId = id;
        SelectedElementId = null;
        CropMode = false;
        ConnectorTool = ConnectorToolKind.None;
        EndLiveEdit();
        Notify(false);
    }

    public void SelectElement(Guid? id)
    {
        var selectionChanged = SelectedElementId != id;
        SelectedElementId = id;
        var cropChanged = CropMode && SelectedElement is not ImageFrameElement;
        if (cropChanged) CropMode = false;
        EndLiveEdit();
        // Connector mode is an explicit mouse mode. It is ended only by Done, Escape,
        // or toggling the connector command, never as a side effect of selection.
        if (selectionChanged || cropChanged) Notify(false);
    }

    public TextFrameElement AddText()
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
    }

    public ImageFrameElement AddImage(string dataUrl, string name, PictureDocument? pictureSource = null)
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
                : 65,
            ZIndex = NextZ()
        };
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
    }

    public VideoElement AddVideo(string dataUrl, string mimeType, string name, double durationSeconds, string posterDataUrl = "")
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        EnsureTimelineDuration();
        Notify();
        return element;
    }

    public AudioElement AddAudio(string dataUrl, string mimeType, string name, double durationSeconds, IReadOnlyList<double>? waveformSamples = null)
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
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
        SelectedElementId = media.Id;
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



    public WordArtElement AddWordArt()
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
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

    public BarcodeElement AddBarcode()
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
    }

    public DataVisualElement AddDataVisual(DataVisualKind kind)
    {
        Capture();
        var data = EnsureDataObject();
        var columns = _data.ResolveColumns(data);
        var argument = columns.FirstOrDefault()?.Name ?? string.Empty;
        var numeric = columns.FirstOrDefault(column => column.ValueKind == PublicationDataValueKind.Number)?.Name
            ?? columns.Skip(1).FirstOrDefault()?.Name
            ?? argument;
        var element = new DataVisualElement
        {
            Name = NextName(DataVisualName(kind)),
            Title = DataVisualName(kind),
            VisualKind = kind,
            DataObjectId = data.Id,
            ArgumentField = argument,
            ValueFields = string.IsNullOrWhiteSpace(numeric) ? [] : [numeric],
            X = 28,
            Y = 30,
            Width = kind is DataVisualKind.Sparkline or DataVisualKind.KpiProgress ? 110 : 130,
            Height = kind switch
            {
                DataVisualKind.Sparkline => 28,
                DataVisualKind.KpiProgress => 35,
                DataVisualKind.DataTable => 75,
                _ => 80
            },
            ZIndex = NextZ()
        };
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
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
        var index = Document.DataObjects.FindIndex(data => data.Id == id);
        if (index < 0) return false;
        Capture();
        Document.DataObjects.RemoveAt(index);
        Notify();
        return true;
    }

    public void RefreshDataVisuals() => Notify(false);

    private static string DataVisualName(DataVisualKind kind) => kind switch
    {
        DataVisualKind.CartesianChart => "Chart",
        DataVisualKind.PieChart => "Pie Chart",
        DataVisualKind.PolarChart => "Polar Chart",
        DataVisualKind.Sparkline => "Sparkline",
        DataVisualKind.BarGauge => "Gauge",
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
        SelectedElementId = connector.Id;
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
        SelectedElementId = connector.Id;
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

    public ShapeElement AddShape(PublicationShape shape)
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
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
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
        Document.Pages.Add(publicationPage);
        SelectedPageId = publicationPage.Id;
        SelectedElementId = null;
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
        }
        foreach (var connector in clone.Elements.OfType<ConnectorElement>())
        {
            if (idMap.TryGetValue(connector.Source.ElementId, out var sourceId)) connector.Source.ElementId = sourceId;
            if (idMap.TryGetValue(connector.Target.ElementId, out var targetId)) connector.Target.ElementId = targetId;
        }
        foreach (var guide in clone.Guides) guide.Id = Guid.NewGuid();
        Document.Pages.Insert(Document.Pages.IndexOf(CurrentPage) + 1, clone);
        SelectedPageId = clone.Id;
        SelectedElementId = null;
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
        SelectedElementId = null;
        Notify();
    }

    public void DeleteSelected()
    {
        var element = SelectedElement;
        if (element is null || element.Locked) return;
        Capture();
        var removedIds = new HashSet<Guid> { element.Id };
        CurrentPage.Elements.Remove(element);
        if (element is PublicationMediaElement)
            _mediaAssets.Remove(element.Id);
        if (element is not ConnectorElement)
        {
            foreach (var connector in CurrentPage.Elements.OfType<ConnectorElement>()
                         .Where(connector => connector.Source.ElementId == element.Id || connector.Target.ElementId == element.Id)
                         .ToList())
            {
                removedIds.Add(connector.Id);
                CurrentPage.Elements.Remove(connector);
            }
        }
        foreach (var item in CurrentPage.Elements)
            if (item.Interaction.TargetElementId is { } targetId && removedIds.Contains(targetId))
                item.Interaction.TargetElementId = null;
        ApplyNormalizedZOrder(OrderedElements());
        ReindexAnimations();
        SelectedElementId = null;
        CropMode = false;
        Notify();
    }

    public void CopySelected()
    {
        var element = SelectedElement;
        if (element is null) return;
        _clipboard = _files.CloneElement(element);
        Notify(false);
    }

    public void Paste()
    {
        var source = _clipboard;
        if (source is null) return;
        if (source is ConnectorElement connectorSource &&
            (!CurrentPage.Elements.Any(item => item.Id == connectorSource.Source.ElementId) ||
             !CurrentPage.Elements.Any(item => item.Id == connectorSource.Target.ElementId))) return;
        Capture();
        var clone = CloneElement(source);
        clone.Id = Guid.NewGuid();
        if (clone.Interaction.TargetElementId == source.Id) clone.Interaction.TargetElementId = clone.Id;
        else if (clone.Interaction.TargetElementId is { } targetId && CurrentPage.Elements.All(item => item.Id != targetId)) clone.Interaction.TargetElementId = null;
        if (clone.Interaction.TargetPageId is { } targetPageId && Document.Pages.All(page => page.Id != targetPageId)) clone.Interaction.TargetPageId = null;
        RenewAnimationIds(clone, preserveOrder: false);
        clone.Name = NextName(source.Name);
        clone.X = Math.Clamp(source.X + 5, -clone.Width + 2, CurrentPage.WidthMm - 2);
        clone.Y = Math.Clamp(source.Y + 5, -clone.Height + 2, CurrentPage.HeightMm - 2);
        clone.ZIndex = NextZ();
        CurrentPage.Elements.Add(clone);
        ReindexAnimations();
        SelectedElementId = clone.Id;
        Notify();
    }

    public void DuplicateSelected()
    {
        var element = SelectedElement;
        if (element is null) return;
        Capture();
        var clone = CloneElement(element);
        clone.Id = Guid.NewGuid();
        if (clone.Interaction.TargetElementId == element.Id) clone.Interaction.TargetElementId = clone.Id;
        RenewAnimationIds(clone, preserveOrder: false);
        clone.Name = NextName(element.Name);
        clone.X += 5;
        clone.Y += 5;
        clone.ZIndex = NextZ();
        CurrentPage.Elements.Add(clone);
        ReindexAnimations();
        SelectedElementId = clone.Id;
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
        SelectedElementId = owner.Id;
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
        SelectedElementId = media.Id;
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

    public void CommitBounds(Guid id, double x, double y, double width, double height)
    {
        var element = CurrentPage.Elements.FirstOrDefault(e => e.Id == id);
        if (element is null || element.Locked || element is ConnectorElement) return;
        var nextWidth = Math.Max(2, Math.Min(width, CurrentPage.WidthMm * 2));
        var nextHeight = Math.Max(2, Math.Min(height, CurrentPage.HeightMm * 2));
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

    public void UpdateTextDocument(byte[] content, string previewHtml, StoryStorageFormat format = StoryStorageFormat.OpenXml)
    {
        if (SelectedElement is not TextFrameElement text || text.Locked) return;
        Capture();
        text.DocumentContent = content;
        text.PreviewHtml = PublicationFileService.SanitizePreviewHtml(previewHtml);
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
        var selected = SelectedElement;
        if (selected is null) return;
        var ordered = OrderedElements();
        var currentIndex = ordered.IndexOf(selected);
        if (currentIndex < 0) return;
        var targetIndex = Math.Clamp(position - 1, 0, ordered.Count - 1);
        if (targetIndex == currentIndex && HasNormalizedZOrder(ordered)) return;
        Capture();
        ordered.RemoveAt(currentIndex);
        ordered.Insert(targetIndex, selected);
        ApplyNormalizedZOrder(ordered);
        Notify();
    }

    public void Align(string mode)
    {
        UpdateSelected(element =>
        {
            switch (mode)
            {
                case "left": element.X = 0; break;
                case "center": element.X = (CurrentPage.WidthMm - element.Width) / 2; break;
                case "right": element.X = CurrentPage.WidthMm - element.Width; break;
                case "top": element.Y = 0; break;
                case "middle": element.Y = (CurrentPage.HeightMm - element.Height) / 2; break;
                case "bottom": element.Y = CurrentPage.HeightMm - element.Height; break;
            }
        });
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
        SelectedElementId = null;
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
        var selected = SelectedElement;
        if (selected is null) return;
        var ordered = OrderedElements();
        var currentIndex = ordered.IndexOf(selected);
        if (currentIndex < 0) return;
        var targetIndex = movement switch
        {
            int.MaxValue => ordered.Count - 1,
            int.MinValue => 0,
            > 0 => Math.Min(ordered.Count - 1, currentIndex + 1),
            < 0 => Math.Max(0, currentIndex - 1),
            _ => currentIndex
        };
        if (targetIndex == currentIndex && HasNormalizedZOrder(ordered)) return;
        Capture();
        ordered.RemoveAt(currentIndex);
        ordered.Insert(targetIndex, selected);
        ApplyNormalizedZOrder(ordered);
        Notify();
    }

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

    private PublicationElement CloneElement(PublicationElement element) => _files.CloneElement(element);

    private PublicationPage ClonePage(PublicationPage publicationPage) => _files.ClonePage(publicationPage);

    private void Notify(bool markModified = true)
    {
        if (markModified) Document.ModifiedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }
}
