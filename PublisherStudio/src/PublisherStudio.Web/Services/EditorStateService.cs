using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class EditorStateService
{
    private readonly PublicationFileService _files;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private string? _clipboard;
    private string? _liveEditKey;

    public EditorStateService(PublicationFileService files)
    {
        _files = files;
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
    }

    public event Action? Changed;
    public PublicationDocument Document { get; private set; }
    public Guid SelectedPageId { get; private set; }
    public Guid? SelectedElementId { get; private set; }
    public bool CropMode { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public bool CanPaste => !string.IsNullOrWhiteSpace(_clipboard);
    public PublicationPage CurrentPage => Document.Pages.First(p => p.Id == SelectedPageId);
    public PublicationElement? SelectedElement => CurrentPage.Elements.FirstOrDefault(e => e.Id == SelectedElementId);

    public void NewDocument()
    {
        Document = PublicationDocument.CreateDefault();
        SelectedPageId = Document.Pages[0].Id;
        SelectedElementId = null;
        CropMode = false;
        _undo.Clear();
        _redo.Clear();
        _liveEditKey = null;
        Notify();
    }

    public void Load(string json)
    {
        Document = _files.Deserialize(json);
        SelectedPageId = Document.Pages[0].Id;
        SelectedElementId = null;
        CropMode = false;
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
        EndLiveEdit();
        Notify(false);
    }

    public void SelectElement(Guid? id)
    {
        SelectedElementId = id;
        if (SelectedElement is not ImageFrameElement)
            CropMode = false;
        EndLiveEdit();
        Notify(false);
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
            DocumentContent = System.Text.Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><p>New text box</p></body></html>")
        };
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
    }

    public ImageFrameElement AddImage(string dataUrl, string name)
    {
        Capture();
        var element = new ImageFrameElement
        {
            Name = NextName(name),
            DataUrl = dataUrl,
            AltText = name,
            X = 30,
            Y = 35,
            Width = 90,
            Height = 65,
            ZIndex = NextZ()
        };
        CurrentPage.Elements.Add(element);
        SelectedElementId = element.Id;
        Notify();
        return element;
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
            Background = source.Background
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
        foreach (var item in clone.Elements) item.Id = Guid.NewGuid();
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
        Document.Pages.RemoveAt(index);
        SelectedPageId = Document.Pages[Math.Clamp(index - 1, 0, Document.Pages.Count - 1)].Id;
        SelectedElementId = null;
        Notify();
    }

    public void DeleteSelected()
    {
        var element = SelectedElement;
        if (element is null || element.Locked) return;
        Capture();
        CurrentPage.Elements.Remove(element);
        SelectedElementId = null;
        CropMode = false;
        Notify();
    }

    public void CopySelected()
    {
        var element = SelectedElement;
        if (element is null) return;
        var wrapper = new PublicationDocument { Pages = [new PublicationPage { Elements = [element] }] };
        _clipboard = _files.Serialize(wrapper);
        Notify(false);
    }

    public void Paste()
    {
        if (string.IsNullOrWhiteSpace(_clipboard)) return;
        var wrapper = _files.Deserialize(_clipboard);
        var source = wrapper.Pages.SelectMany(item => item.Elements).FirstOrDefault();
        if (source is null) return;
        Capture();
        var clone = CloneElement(source);
        clone.Id = Guid.NewGuid();
        clone.Name = NextName(source.Name);
        clone.X = Math.Clamp(source.X + 5, -clone.Width + 2, CurrentPage.WidthMm - 2);
        clone.Y = Math.Clamp(source.Y + 5, -clone.Height + 2, CurrentPage.HeightMm - 2);
        clone.ZIndex = NextZ();
        CurrentPage.Elements.Add(clone);
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
        clone.Name = NextName(element.Name);
        clone.X += 5;
        clone.Y += 5;
        clone.ZIndex = NextZ();
        CurrentPage.Elements.Add(clone);
        SelectedElementId = clone.Id;
        Notify();
    }

    public void CommitBounds(Guid id, double x, double y, double width, double height)
    {
        var element = CurrentPage.Elements.FirstOrDefault(e => e.Id == id);
        if (element is null || element.Locked) return;
        Capture();
        element.Width = Math.Max(2, Math.Min(width, CurrentPage.WidthMm * 2));
        element.Height = Math.Max(2, Math.Min(height, CurrentPage.HeightMm * 2));
        element.X = Math.Clamp(x, -element.Width + 2, CurrentPage.WidthMm - 2);
        element.Y = Math.Clamp(y, -element.Height + 2, CurrentPage.HeightMm - 2);
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

    public void UpdateTextDocument(byte[] content, string previewHtml)
    {
        if (SelectedElement is not TextFrameElement text || text.Locked) return;
        Capture();
        text.DocumentContent = content;
        text.PreviewHtml = PublicationFileService.SanitizePreviewHtml(previewHtml);
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

    public void BringToFront() => ChangeZ(NextZ());
    public void SendToBack() => ChangeZ(CurrentPage.Elements.Select(e => e.ZIndex).DefaultIfEmpty(0).Min() - 1);
    public void BringForward() => ChangeZ((SelectedElement?.ZIndex ?? 0) + 1);
    public void SendBackward() => ChangeZ((SelectedElement?.ZIndex ?? 0) - 1);

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
        _liveEditKey = null;
        Notify();
    }

    private void ChangeZ(int value) => UpdateSelected(element => element.ZIndex = value);
    private int NextZ() => CurrentPage.Elements.Select(e => e.ZIndex).DefaultIfEmpty(0).Max() + 1;
    private string NextName(string basis) => $"{basis} {CurrentPage.Elements.Count + 1}";

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

    private PublicationElement CloneElement(PublicationElement element)
    {
        var wrapper = new PublicationDocument { Pages = [new PublicationPage { Elements = [element] }] };
        return _files.Deserialize(_files.Serialize(wrapper)).Pages[0].Elements[0];
    }

    private PublicationPage ClonePage(PublicationPage publicationPage)
    {
        var wrapper = new PublicationDocument { Pages = [publicationPage] };
        return _files.Deserialize(_files.Serialize(wrapper)).Pages[0];
    }

    private void Notify(bool markModified = true)
    {
        if (markModified) Document.ModifiedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }
}
