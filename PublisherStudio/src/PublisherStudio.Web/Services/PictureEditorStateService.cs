using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class PictureEditorStateService
{
    private readonly PictureDocumentService _documents;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private string? _liveEditKey;

    public PictureEditorStateService(PictureDocumentService documents)
    {
        _documents = documents;
        Document = PictureDocument.CreateDefault();
    }

    public event Action? Changed;
    public PictureDocument Document { get; private set; }
    public Guid? SelectedLayerId { get; private set; }
    public PictureLayer? SelectedLayer => Document.Layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void StartNew(int widthPx = 1200, int heightPx = 800, bool transparent = true)
    {
        Document = PictureDocument.CreateDefault(widthPx, heightPx, transparent);
        SelectedLayerId = null;
        ResetHistory();
        Notify();
    }

    public void StartFromDocument(PictureDocument document)
    {
        Document = _documents.Clone(document);
        SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
        ResetHistory();
        Notify();
    }

    public void StartFromRaster(string dataUrl, string name, int widthPx = 1200, int heightPx = 800)
    {
        Document = PictureDocument.FromRaster(dataUrl, name, widthPx, heightPx);
        SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
        ResetHistory();
        Notify();
    }

    public PictureDocument CloneDocument() => _documents.Clone(Document);

    public void SetDocumentName(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Untitled Picture" : name.Trim();
        if (string.Equals(name, Document.Name, StringComparison.Ordinal)) return;
        Capture();
        Document.Name = name;
        Notify();
    }

    public void SetDocumentSize(int widthPx, int heightPx)
    {
        widthPx = Math.Clamp(widthPx, 16, 8192);
        heightPx = Math.Clamp(heightPx, 16, 8192);
        if (widthPx == Document.WidthPx && heightPx == Document.HeightPx) return;
        Capture();
        Document.WidthPx = widthPx;
        Document.HeightPx = heightPx;
        Notify();
    }

    public void SetBackground(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "transparent" : value;
        if (string.Equals(value, Document.Background, StringComparison.OrdinalIgnoreCase)) return;
        Capture();
        Document.Background = value;
        Notify();
    }

    public void SetZoom(double zoom)
    {
        Document.Zoom = Math.Clamp(zoom, .05, 4);
        Notify(false);
    }

    public void SetGrid(bool visible) { Document.GridVisible = visible; Notify(false); }
    public void SetSnap(bool enabled) { Document.SnapToGrid = enabled; Notify(false); }
    public void SetGridSpacing(int pixels) { Document.GridSpacingPx = Math.Clamp(pixels, 2, 1000); Notify(false); }

    public void SelectLayer(Guid? id)
    {
        if (id is not null && Document.Layers.All(layer => layer.Id != id)) return;
        SelectedLayerId = id;
        EndLiveEdit();
        Notify(false);
    }

    public RasterPictureLayer AddRaster(string dataUrl, string name, int naturalWidth = 0, int naturalHeight = 0)
    {
        Capture();
        var size = FitSize(naturalWidth > 0 ? naturalWidth : Document.WidthPx, naturalHeight > 0 ? naturalHeight : Document.HeightPx, Document.WidthPx * .72, Document.HeightPx * .72);
        var layer = new RasterPictureLayer
        {
            Name = NextName(string.IsNullOrWhiteSpace(name) ? "Picture" : Path.GetFileNameWithoutExtension(name)),
            DataUrl = dataUrl,
            Width = size.Width,
            Height = size.Height,
            X = (Document.WidthPx - size.Width) / 2,
            Y = (Document.HeightPx - size.Height) / 2
        };
        Document.Layers.Add(layer);
        SelectedLayerId = layer.Id;
        Notify();
        return layer;
    }

    public TextPictureLayer AddText()
    {
        Capture();
        var layer = new TextPictureLayer
        {
            Name = NextName("Text"),
            X = Document.WidthPx * .15,
            Y = Document.HeightPx * .35,
            Width = Document.WidthPx * .7,
            Height = Math.Max(100, Document.HeightPx * .2),
            FontSizePx = Math.Clamp(Document.WidthPx / 14d, 24, 140)
        };
        Document.Layers.Add(layer);
        SelectedLayerId = layer.Id;
        Notify();
        return layer;
    }

    public ShapePictureLayer AddShape(PictureShapeKind shape = PictureShapeKind.Rectangle)
    {
        Capture();
        var layer = new ShapePictureLayer
        {
            Name = NextName(shape.ToString()),
            Shape = shape,
            X = Document.WidthPx * .25,
            Y = Document.HeightPx * .25,
            Width = Document.WidthPx * .5,
            Height = Document.HeightPx * .4
        };
        Document.Layers.Add(layer);
        SelectedLayerId = layer.Id;
        Notify();
        return layer;
    }

    public FillPictureLayer AddFill(PictureFillKind fillKind = PictureFillKind.LinearGradient)
    {
        Capture();
        var layer = new FillPictureLayer
        {
            Name = NextName(fillKind == PictureFillKind.Solid ? "Color Fill" : "Gradient"),
            FillKind = fillKind,
            X = 0,
            Y = 0,
            Width = Document.WidthPx,
            Height = Document.HeightPx
        };
        Document.Layers.Add(layer);
        SelectedLayerId = layer.Id;
        Notify();
        return layer;
    }

    public RenderPictureLayer AddRender(PictureRenderKind renderKind = PictureRenderKind.Clouds)
    {
        Capture();
        var layer = new RenderPictureLayer
        {
            Name = NextName(renderKind.ToString()),
            RenderKind = renderKind,
            X = 0,
            Y = 0,
            Width = Document.WidthPx,
            Height = Document.HeightPx,
            Seed = Random.Shared.Next(1, int.MaxValue)
        };
        Document.Layers.Add(layer);
        SelectedLayerId = layer.Id;
        Notify();
        return layer;
    }

    public void DeleteSelected()
    {
        var layer = SelectedLayer;
        if (layer is null || layer.Locked) return;
        Capture();
        var index = Document.Layers.IndexOf(layer);
        Document.Layers.Remove(layer);
        SelectedLayerId = Document.Layers.Count == 0 ? null : Document.Layers[Math.Clamp(index - 1, 0, Document.Layers.Count - 1)].Id;
        Notify();
    }

    public void DuplicateSelected()
    {
        var layer = SelectedLayer;
        if (layer is null) return;
        Capture();
        var clone = CloneLayer(layer);
        clone.Id = Guid.NewGuid();
        clone.Name = NextName(layer.Name);
        clone.X += 18;
        clone.Y += 18;
        Document.Layers.Insert(Document.Layers.IndexOf(layer) + 1, clone);
        SelectedLayerId = clone.Id;
        Notify();
    }

    public void MoveSelectedLayer(int delta)
    {
        var layer = SelectedLayer;
        if (layer is null) return;
        var index = Document.Layers.IndexOf(layer);
        var target = Math.Clamp(index + delta, 0, Document.Layers.Count - 1);
        if (target == index) return;
        Capture();
        Document.Layers.RemoveAt(index);
        Document.Layers.Insert(target, layer);
        Notify();
    }

    public void BringSelectedToFront()
    {
        var layer = SelectedLayer;
        if (layer is null || Document.Layers.LastOrDefault() == layer) return;
        Capture();
        Document.Layers.Remove(layer);
        Document.Layers.Add(layer);
        Notify();
    }

    public void SendSelectedToBack()
    {
        var layer = SelectedLayer;
        if (layer is null || Document.Layers.FirstOrDefault() == layer) return;
        Capture();
        Document.Layers.Remove(layer);
        Document.Layers.Insert(0, layer);
        Notify();
    }

    public void ToggleVisibility(Guid id)
    {
        var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
        if (layer is null) return;
        Capture();
        layer.Visible = !layer.Visible;
        Notify();
    }

    public void ToggleLock(Guid id)
    {
        var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
        if (layer is null) return;
        Capture();
        layer.Locked = !layer.Locked;
        Notify();
    }

    public void CommitTransform(Guid id, double x, double y, double width, double height, double rotation)
    {
        var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
        if (layer is null || layer.Locked) return;
        Capture();
        layer.X = Math.Clamp(x, -width + 1, Document.WidthPx - 1);
        layer.Y = Math.Clamp(y, -height + 1, Document.HeightPx - 1);
        layer.Width = Math.Clamp(width, 1, 16384);
        layer.Height = Math.Clamp(height, 1, 16384);
        layer.Rotation = NormalizeAngle(rotation);
        SelectedLayerId = id;
        Notify();
    }

    public void UpdateSelected(Action<PictureLayer> update, bool capture = true, bool allowLocked = false)
    {
        var layer = SelectedLayer;
        if (layer is null || (layer.Locked && !allowLocked)) return;
        if (capture) Capture();
        update(layer);
        NormalizeLayer(layer);
        Notify();
    }

    public void UpdateSelectedLive(string key, Action<PictureLayer> update)
    {
        var layer = SelectedLayer;
        if (layer is null || layer.Locked) return;
        if (!string.Equals(_liveEditKey, key, StringComparison.Ordinal))
        {
            Capture();
            _liveEditKey = key;
        }
        update(layer);
        NormalizeLayer(layer);
        Notify();
    }

    public void EndLiveEdit() => _liveEditKey = null;

    public void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Push(_documents.Serialize(Document));
        Restore(_undo.Pop());
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Push(_documents.Serialize(Document));
        Restore(_redo.Pop());
    }

    private void Restore(string json)
    {
        Document = _documents.Deserialize(json);
        SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
        _liveEditKey = null;
        Notify();
    }

    private void ResetHistory()
    {
        _undo.Clear();
        _redo.Clear();
        _liveEditKey = null;
    }

    private void Capture()
    {
        _liveEditKey = null;
        _undo.Push(_documents.Serialize(Document));
        if (_undo.Count > 80)
        {
            var newest = _undo.Take(80).Reverse().ToArray();
            _undo.Clear();
            foreach (var item in newest) _undo.Push(item);
        }
        _redo.Clear();
    }

    private PictureLayer CloneLayer(PictureLayer layer)
    {
        var wrapper = PictureDocument.CreateDefault(Document.WidthPx, Document.HeightPx, true);
        wrapper.Layers.Add(layer);
        return _documents.Clone(wrapper).Layers[0];
    }

    private string NextName(string basis)
    {
        basis = string.IsNullOrWhiteSpace(basis) ? "Layer" : basis.Trim();
        var name = basis;
        var suffix = 2;
        while (Document.Layers.Any(layer => string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = $"{basis} {suffix++}";
        return name;
    }

    private static (double Width, double Height) FitSize(double width, double height, double maxWidth, double maxHeight)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var scale = Math.Min(maxWidth / width, maxHeight / height);
        scale = Math.Min(1, scale);
        return (Math.Max(1, width * scale), Math.Max(1, height * scale));
    }

    private static void NormalizeLayer(PictureLayer layer)
    {
        layer.Width = Math.Clamp(layer.Width, 1, 16384);
        layer.Height = Math.Clamp(layer.Height, 1, 16384);
        layer.Rotation = NormalizeAngle(layer.Rotation);
        layer.Opacity = Math.Clamp(layer.Opacity, 0, 1);
        layer.Brightness = Math.Clamp(layer.Brightness, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation, 0, 3);
        layer.HueRotation = Math.Clamp(layer.HueRotation, -360, 360);
        layer.Blur = Math.Clamp(layer.Blur, 0, 100);
        layer.Grayscale = Math.Clamp(layer.Grayscale, 0, 1);
        layer.Sepia = Math.Clamp(layer.Sepia, 0, 1);
        layer.Invert = Math.Clamp(layer.Invert, 0, 1);
        if (layer is RasterPictureLayer raster) raster.TintOpacity = Math.Clamp(raster.TintOpacity, 0, 1);
        if (layer is TextPictureLayer text)
        {
            text.FontSizePx = Math.Clamp(text.FontSizePx, 4, 1024);
            text.OutlineWidthPx = Math.Clamp(text.OutlineWidthPx, 0, 64);
        }
        if (layer is ShapePictureLayer shape)
        {
            shape.StrokeWidthPx = Math.Clamp(shape.StrokeWidthPx, 0, 200);
            shape.CornerRadiusPx = Math.Clamp(shape.CornerRadiusPx, 0, 2000);
        }
        if (layer is RenderPictureLayer render)
        {
            render.Detail = Math.Clamp(render.Detail, 1, 8);
            render.Scale = Math.Clamp(render.Scale, 4, 2000);
            render.Softness = Math.Clamp(render.Softness, 0, 1);
            render.RenderContrast = Math.Clamp(render.RenderContrast, .1, 5);
            render.StripeWidthPx = Math.Clamp(render.StripeWidthPx, 1, 1000);
        }
    }

    private static double NormalizeAngle(double value) => (value % 360 + 360) % 360;
    private void Notify(bool markChanged = true) => Changed?.Invoke();
}
