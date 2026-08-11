using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Provides picture editor state service operations.
/// </summary>
public sealed class PictureEditorStateService
{
    private readonly PictureDocumentService _documents;
    private readonly IPublisherDocumentFactory _documentFactory;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly Stack<string> _undo = new();
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly Stack<string> _redo = new();
    private string? _liveEditKey;
    private PictureLayer? _clipboard;

    /// <summary>
    /// Runs the picture editor state service operation.
    /// </summary>
    public PictureEditorStateService(PictureDocumentService documents, IPublisherDocumentFactory documentFactory)
    {
        _documents = documents;
        _documentFactory = documentFactory;
        Document = _documentFactory.CreatePicture();
    }

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets or sets document.
    /// </summary>
    public PictureDocument Document { get; private set; }
    /// <summary>
    /// Gets or sets selected layer identifier.
    /// </summary>
    public Guid? SelectedLayerId { get; private set; }
    /// <summary>
    /// Gets selected layer.
    /// </summary>
    public PictureLayer? SelectedLayer => Document.Layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);
    /// <summary>
    /// Gets can undo.
    /// </summary>
    public bool CanUndo => _undo.Count > 0;
    /// <summary>
    /// Gets can redo.
    /// </summary>
    public bool CanRedo => _redo.Count > 0;
    /// <summary>
    /// Gets can paste.
    /// </summary>
    public bool CanPaste => _clipboard is not null;

    /// <summary>
    /// Starts new.
    /// </summary>
    public void StartNew(int widthPx = 1200, int heightPx = 800, bool transparent = true)
    {
    try
    {
            Document = _documentFactory.CreatePicture(widthPx, heightPx, transparent);
            SelectedLayerId = null;
            ResetHistory();
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.StartNew failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Starts from document.
    /// </summary>
    public void StartFromDocument(PictureDocument document)
    {
    try
    {
            Document = _documents.Clone(document);
            SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
            ResetHistory();
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.StartFromDocument failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Starts from raster.
    /// </summary>
    public void StartFromRaster(string dataUrl, string name, int widthPx = 1200, int heightPx = 800)
    {
    try
    {
            Document = _documentFactory.CreatePictureFromRaster(dataUrl, name, widthPx, heightPx);
            SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
            ResetHistory();
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.StartFromRaster failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the clone document operation.
    /// </summary>
    public PictureDocument CloneDocument() {
    try
    {
        return _documents.Clone(Document);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CloneDocument failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets document name.
    /// </summary>
    public void SetDocumentName(string name)
    {
    try
    {
            name = string.IsNullOrWhiteSpace(name) ? "Untitled Picture" : name.Trim();
            if (string.Equals(name, Document.Name, StringComparison.Ordinal)) return;
            Capture();
            Document.Name = name;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetDocumentName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets document size.
    /// </summary>
    public void SetDocumentSize(int widthPx, int heightPx)
    {
    try
    {
            widthPx = Math.Clamp(widthPx, 16, 8192);
            heightPx = Math.Clamp(heightPx, 16, 8192);
            if (widthPx == Document.WidthPx && heightPx == Document.HeightPx) return;
            Capture();
            var oldWidth = Document.WidthPx;
            var oldHeight = Document.HeightPx;
            Document.WidthPx = widthPx;
            Document.HeightPx = heightPx;
            foreach (var paint in Document.Layers.OfType<PaintPictureLayer>())
            {
                if (Math.Abs(paint.X) < .001 && Math.Abs(paint.Y) < .001 &&
                    Math.Abs(paint.Width - oldWidth) < .001 && Math.Abs(paint.Height - oldHeight) < .001)
                {
                    paint.Width = widthPx;
                    paint.Height = heightPx;
                }
            }
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetDocumentSize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets background.
    /// </summary>
    public void SetBackground(string value)
    {
    try
    {
            value = string.IsNullOrWhiteSpace(value) ? "transparent" : value;
            if (string.Equals(value, Document.Background, StringComparison.OrdinalIgnoreCase)) return;
            Capture();
            Document.Background = value;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetBackground failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets zoom.
    /// </summary>
    public void SetZoom(double zoom)
    {
    try
    {
            Document.Zoom = Math.Clamp(zoom, .05, 4);
            Notify(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetZoom failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets grid.
    /// </summary>
    public void SetGrid(bool visible) {
    try
    {
     Document.GridVisible = visible; Notify(false); 
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetGrid failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Sets snap.
    /// </summary>
    public void SetSnap(bool enabled) {
    try
    {
     Document.SnapToGrid = enabled; Notify(false); 
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetSnap failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Sets grid spacing.
    /// </summary>
    public void SetGridSpacing(int pixels) {
    try
    {
     Document.GridSpacingPx = Math.Clamp(pixels, 2, 1000); Notify(false); 
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SetGridSpacing failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the select layer operation.
    /// </summary>
    public void SelectLayer(Guid? id)
    {
    try
    {
            if (id is not null && Document.Layers.All(layer => layer.Id != id)) return;
            SelectedLayerId = id;
            EndLiveEdit();
            Notify(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SelectLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds raster.
    /// </summary>
    public RasterPictureLayer AddRaster(
        string dataUrl,
        string name,
        int naturalWidth = 0,
        int naturalHeight = 0,
        double? centerX = null,
        double? centerY = null)
    {
    try
    {
            Capture();
            var layer = _documents.AddRasterLayer(Document, dataUrl, name, naturalWidth, naturalHeight, centerX, centerY);
            SelectedLayerId = layer.Id;
            Notify();
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddRaster failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds imported layers.
    /// </summary>
    public int AddImportedLayers(
        PictureDocument importedDocument,
        string? groupName = null,
        double? centerX = null,
        double? centerY = null)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(importedDocument);
            var imported = _documents.Clone(importedDocument);
            if (imported.Layers.Count == 0) return 0;

            Capture();
            var scale = Math.Min(
                Document.WidthPx * .84 / Math.Max(1, imported.WidthPx),
                Document.HeightPx * .84 / Math.Max(1, imported.HeightPx));
            if (!double.IsFinite(scale) || scale <= 0) scale = 1;
            var offsetX = (centerX ?? Document.WidthPx / 2d) - imported.WidthPx * scale / 2d;
            var offsetY = (centerY ?? Document.HeightPx / 2d) - imported.HeightPx * scale / 2d;
            var group = string.IsNullOrWhiteSpace(groupName)
                ? string.IsNullOrWhiteSpace(imported.Name) ? "Imported" : imported.Name.Trim()
                : groupName.Trim();

            foreach (var layer in imported.Layers)
            {
                layer.Id = Guid.NewGuid();
                layer.Name = NextName(layer.Name);
                layer.GroupPath = string.IsNullOrWhiteSpace(layer.GroupPath) ? group : $"{group}/{layer.GroupPath}";
                layer.X = offsetX + layer.X * scale;
                layer.Y = offsetY + layer.Y * scale;
                layer.Width = Math.Max(1, layer.Width * scale);
                layer.Height = Math.Max(1, layer.Height * scale);
                Document.Layers.Add(layer);
                SelectedLayerId = layer.Id;
            }

            Notify();
            return imported.Layers.Count;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddImportedLayers failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the replace raster operation.
    /// </summary>
    public bool ReplaceRaster(Guid id, string dataUrl)
    {
    try
    {
            var layer = Document.Layers.OfType<RasterPictureLayer>().FirstOrDefault(item => item.Id == id);
            if (layer is null || layer.Locked || string.IsNullOrWhiteSpace(dataUrl)) return false;
            Capture();
            layer.DataUrl = dataUrl;
            Notify();
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ReplaceRaster failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds text.
    /// </summary>
    public TextPictureLayer AddText()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddText failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds shape.
    /// </summary>
    public ShapePictureLayer AddShape(PictureShapeKind shape = PictureShapeKind.Rectangle)
    {
    try
    {
            return AddShapeAt(
                shape,
                Document.WidthPx * .25,
                Document.HeightPx * .25,
                Document.WidthPx * .5,
                Document.HeightPx * .4,
                0);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddShape failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds shape at.
    /// </summary>
    public ShapePictureLayer AddShapeAt(PictureShapeKind shape, double x, double y, double width, double height, double rotation = 0)
    {
    try
    {
            Capture();
            var layer = new ShapePictureLayer
            {
                Name = NextName(shape.ToString()),
                Shape = shape,
                X = Math.Clamp(x, -Document.WidthPx * 2d, Document.WidthPx * 3d),
                Y = Math.Clamp(y, -Document.HeightPx * 2d, Document.HeightPx * 3d),
                Width = Math.Clamp(width, 1, Document.WidthPx * 4d),
                Height = Math.Clamp(height, 1, Document.HeightPx * 4d),
                Rotation = ((rotation % 360) + 360) % 360
            };
            Document.Layers.Add(layer);
            SelectedLayerId = layer.Id;
            Notify();
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddShapeAt failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds path.
    /// </summary>
    public ShapePictureLayer AddPath(IReadOnlyList<PicturePoint> points, string strokeColor, double strokeWidth, bool closed = false, bool smooth = true)
    {
    try
    {
            if (points.Count < 2) return AddShape(PictureShapeKind.Path);
            var minX = points.Min(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxX = points.Max(point => point.X);
            var maxY = points.Max(point => point.Y);
            var width = Math.Max(1, maxX - minX);
            var height = Math.Max(1, maxY - minY);
            Capture();
            var layer = new ShapePictureLayer
            {
                Name = NextName("Path"),
                Shape = PictureShapeKind.Path,
                FillColor = closed ? strokeColor : "transparent",
                StrokeColor = string.IsNullOrWhiteSpace(strokeColor) ? "#1d4ed8" : strokeColor,
                StrokeWidthPx = Math.Clamp(strokeWidth, .25, 512),
                X = minX,
                Y = minY,
                Width = width,
                Height = height,
                PathClosed = closed,
                PathSmooth = smooth,
                PathPoints = points.Select(point => new PicturePoint { X = point.X - minX, Y = point.Y - minY }).ToList()
            };
            Document.Layers.Add(layer);
            SelectedLayerId = layer.Id;
            Notify();
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddPath failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds area fill.
    /// </summary>
    public ShapePictureLayer AddAreaFill(string selectionKind, IReadOnlyList<PicturePoint> points, string primaryColor, string secondaryColor, bool gradient)
    {
    try
    {
            if (points.Count < 2) return AddShape(PictureShapeKind.Rectangle);
            var minX = points.Min(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxX = points.Max(point => point.X);
            var maxY = points.Max(point => point.Y);
            var width = Math.Max(1, maxX - minX);
            var height = Math.Max(1, maxY - minY);
            Capture();
            var kind = selectionKind?.Trim().ToLowerInvariant() switch
            {
                "ellipse" => PictureShapeKind.Ellipse,
                "free" or "magnetic" or "polygon" => PictureShapeKind.Freeform,
                _ => PictureShapeKind.Rectangle
            };
            var layer = new ShapePictureLayer
            {
                Name = NextName(gradient ? "Gradient Fill" : "Area Fill"),
                Shape = kind,
                FillKind = gradient ? PictureFillKind.LinearGradient : PictureFillKind.Solid,
                FillColor = string.IsNullOrWhiteSpace(primaryColor) ? "#111827" : primaryColor,
                SecondaryFillColor = string.IsNullOrWhiteSpace(secondaryColor) ? "#ffffff" : secondaryColor,
                StrokeColor = "transparent",
                StrokeWidthPx = 0,
                X = minX,
                Y = minY,
                Width = width,
                Height = height,
                PathPoints = kind == PictureShapeKind.Freeform
                    ? points.Select(point => new PicturePoint { X = point.X - minX, Y = point.Y - minY }).ToList()
                    : []
            };
            Document.Layers.Add(layer);
            SelectedLayerId = layer.Id;
            Notify();
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddAreaFill failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds fill.
    /// </summary>
    public FillPictureLayer AddFill(PictureFillKind fillKind = PictureFillKind.LinearGradient)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddFill failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds render.
    /// </summary>
    public RenderPictureLayer AddRender(PictureRenderKind renderKind = PictureRenderKind.Clouds)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddRender failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds paint.
    /// </summary>
    public PaintPictureLayer AddPaint(string name = "Paint")
    {
    try
    {
            Capture();
            var layer = CreatePaintLayer(name);
            Notify();
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddPaint failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds stroke.
    /// </summary>
    public void AddStroke(PictureStrokeKind kind, IReadOnlyList<PicturePoint> points, string color, double widthPx, double opacity, double hardness)
    {
    try
    {
            if (points.Count < 2) return;
            Capture();
            var layer = SelectedLayer as PaintPictureLayer;
            if ((layer is null || layer.Locked) && kind == PictureStrokeKind.Eraser)
                layer = Document.Layers.OfType<PaintPictureLayer>().LastOrDefault(candidate => !candidate.Locked);
            if (layer is null || layer.Locked) layer = CreatePaintLayer(kind == PictureStrokeKind.Eraser ? "Eraser" : "Paint");
            var stroke = new PictureStroke
            {
                Kind = kind,
                Color = string.IsNullOrWhiteSpace(color) ? "#111827" : color,
                WidthPx = Math.Clamp(widthPx, .25, 512),
                Opacity = Math.Clamp(opacity, 0, 1),
                Hardness = Math.Clamp(hardness, 0, 1),
                Points = points.Take(20000).Select(point => new PicturePoint
                {
                    X = Math.Clamp(point.X, -16384, 32768),
                    Y = Math.Clamp(point.Y, -16384, 32768)
                }).ToList()
            };
            layer.Strokes.Add(stroke);
            SelectedLayerId = layer.Id;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.AddStroke failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the clear selected paint operation.
    /// </summary>
    public void ClearSelectedPaint()
    {
    try
    {
            if (SelectedLayer is not PaintPictureLayer paint || paint.Locked || paint.Strokes.Count == 0) return;
            Capture();
            paint.Strokes.Clear();
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ClearSelectedPaint failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Deletes selected.
    /// </summary>
    public void DeleteSelected()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || layer.Locked) return;
            Capture();
            var index = Document.Layers.IndexOf(layer);
            Document.Layers.Remove(layer);
            SelectedLayerId = Document.Layers.Count == 0 ? null : Document.Layers[Math.Clamp(index - 1, 0, Document.Layers.Count - 1)].Id;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.DeleteSelected failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the copy selected operation.
    /// </summary>
    public void CopySelected()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null) return;
            _clipboard = CloneLayer(layer);
            Notify(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CopySelected failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the copy selected region operation.
    /// </summary>
    public bool CopySelectedRegion(IReadOnlyList<PicturePoint> points, bool inverted = false)
    {
    try
    {
            var layer = SelectedLayer;
            var polygon = NormalizeClipPolygon(points);
            if (layer is null || polygon.Count < 3) return false;
            _clipboard = CloneLayer(layer);
            _clipboard.ClipPolygon = polygon;
            _clipboard.ClipInverted = inverted;
            Notify(false);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CopySelectedRegion failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Applies selected clip.
    /// </summary>
    public bool ApplySelectedClip(IReadOnlyList<PicturePoint> points, bool inverted)
    {
    try
    {
            var layer = SelectedLayer;
            var polygon = NormalizeClipPolygon(points);
            if (layer is null || layer.Locked || polygon.Count < 3) return false;
            Capture();
            layer.ClipPolygon = polygon;
            layer.ClipInverted = inverted;
            NormalizeLayer(layer);
            Notify();
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ApplySelectedClip failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the clear selected clip operation.
    /// </summary>
    public bool ClearSelectedClip()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || layer.Locked || layer.ClipPolygon.Count < 3) return false;
            Capture();
            layer.ClipPolygon.Clear();
            layer.ClipInverted = false;
            Notify();
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ClearSelectedClip failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the paste operation.
    /// </summary>
    public void Paste()
    {
    try
    {
            if (_clipboard is null) return;
            Capture();
            var clone = CloneLayer(_clipboard);
            clone.Id = Guid.NewGuid();
            clone.Name = NextName(_clipboard.Name);
            clone.X += 18;
            clone.Y += 18;
            Document.Layers.Add(clone);
            SelectedLayerId = clone.Id;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Paste failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the duplicate selected operation.
    /// </summary>
    public void DuplicateSelected()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.DuplicateSelected failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the center selected operation.
    /// </summary>
    public void CenterSelected()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || layer.Locked) return;
            Capture();
            layer.X = (Document.WidthPx - layer.Width) / 2;
            layer.Y = (Document.HeightPx - layer.Height) / 2;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CenterSelected failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the fit selected to canvas operation.
    /// </summary>
    public void FitSelectedToCanvas()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || layer.Locked) return;
            Capture();
            layer.X = 0;
            layer.Y = 0;
            layer.Width = Document.WidthPx;
            layer.Height = Document.HeightPx;
            layer.Rotation = 0;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.FitSelectedToCanvas failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the move selected layer operation.
    /// </summary>
    public void MoveSelectedLayer(int delta)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.MoveSelectedLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the bring selected to front operation.
    /// </summary>
    public void BringSelectedToFront()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || Document.Layers.LastOrDefault() == layer) return;
            Capture();
            Document.Layers.Remove(layer);
            Document.Layers.Add(layer);
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.BringSelectedToFront failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the send selected to back operation.
    /// </summary>
    public void SendSelectedToBack()
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || Document.Layers.FirstOrDefault() == layer) return;
            Capture();
            Document.Layers.Remove(layer);
            Document.Layers.Insert(0, layer);
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.SendSelectedToBack failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the toggle visibility operation.
    /// </summary>
    public void ToggleVisibility(Guid id)
    {
    try
    {
            var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
            if (layer is null) return;
            Capture();
            layer.Visible = !layer.Visible;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ToggleVisibility failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the toggle lock operation.
    /// </summary>
    public void ToggleLock(Guid id)
    {
    try
    {
            var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
            if (layer is null) return;
            Capture();
            layer.Locked = !layer.Locked;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ToggleLock failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the commit transform operation.
    /// </summary>
    public void CommitTransform(Guid id, double x, double y, double width, double height, double rotation)
    {
    try
    {
            var layer = Document.Layers.FirstOrDefault(item => item.Id == id);
            if (layer is null || layer.Locked) return;
            var nextWidth = Math.Clamp(width, 1, 16384);
            var nextHeight = Math.Clamp(height, 1, 16384);
            var nextX = Math.Clamp(x, -nextWidth + 1, Document.WidthPx - 1);
            var nextY = Math.Clamp(y, -nextHeight + 1, Document.HeightPx - 1);
            var nextRotation = NormalizeAngle(rotation);
            if (NearlyEqual(layer.X, nextX) && NearlyEqual(layer.Y, nextY) &&
                NearlyEqual(layer.Width, nextWidth) && NearlyEqual(layer.Height, nextHeight) &&
                NearlyEqual(layer.Rotation, nextRotation))
            {
                if (SelectedLayerId != id)
                {
                    SelectedLayerId = id;
                    Notify(false);
                }
                return;
            }
            Capture();
            layer.X = nextX;
            layer.Y = nextY;
            layer.Width = nextWidth;
            layer.Height = nextHeight;
            layer.Rotation = nextRotation;
            SelectedLayerId = id;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CommitTransform failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Updates selected.
    /// </summary>
    public void UpdateSelected(Action<PictureLayer> update, bool capture = true, bool allowLocked = false)
    {
    try
    {
            var layer = SelectedLayer;
            if (layer is null || (layer.Locked && !allowLocked)) return;
            if (capture) Capture();
            update(layer);
            NormalizeLayer(layer);
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.UpdateSelected failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Updates selected live.
    /// </summary>
    public void UpdateSelectedLive(string key, Action<PictureLayer> update)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.UpdateSelectedLive failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the end live edit operation.
    /// </summary>
    public void EndLiveEdit() {
    try
    {
        _liveEditKey = null;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.EndLiveEdit failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the undo operation.
    /// </summary>
    public void Undo()
    {
    try
    {
            if (_undo.Count == 0) return;
            _redo.Push(_documents.Serialize(Document));
            Restore(_undo.Pop());
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Undo failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the redo operation.
    /// </summary>
    public void Redo()
    {
    try
    {
            if (_redo.Count == 0) return;
            _undo.Push(_documents.Serialize(Document));
            Restore(_redo.Pop());
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Redo failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the restore operation.
    /// </summary>
    private void Restore(string json)
    {
    try
    {
            Document = _documents.Deserialize(json);
            SelectedLayerId = Document.Layers.LastOrDefault()?.Id;
            _liveEditKey = null;
            Notify();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Restore failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the reset history operation.
    /// </summary>
    private void ResetHistory()
    {
    try
    {
            _undo.Clear();
            _redo.Clear();
            _liveEditKey = null;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.ResetHistory failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the capture operation.
    /// </summary>
    private void Capture()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Capture failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Creates paint layer.
    /// </summary>
    private PaintPictureLayer CreatePaintLayer(string name)
    {
    try
    {
            var layer = new PaintPictureLayer
            {
                Name = NextName(string.IsNullOrWhiteSpace(name) ? "Paint" : name),
                X = 0,
                Y = 0,
                Width = Document.WidthPx,
                Height = Document.HeightPx
            };
            Document.Layers.Add(layer);
            SelectedLayerId = layer.Id;
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CreatePaintLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the clone layer operation.
    /// </summary>
    private PictureLayer CloneLayer(PictureLayer layer)
    {
    try
    {
            var wrapper = _documentFactory.CreatePicture(Document.WidthPx, Document.HeightPx, true);
            wrapper.Layers.Add(layer);
            return _documents.Clone(wrapper).Layers[0];
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.CloneLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the next name operation.
    /// </summary>
    private string NextName(string basis)
    {
    try
    {
            basis = string.IsNullOrWhiteSpace(basis) ? "Layer" : basis.Trim();
            var name = basis;
            var suffix = 2;
            while (Document.Layers.Any(layer => string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = $"{basis} {suffix++}";
            return name;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.NextName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the fit size operation.
    /// </summary>
    private (double Width, double Height) FitSize(double width, double height, double maxWidth, double maxHeight)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var scale = Math.Min(maxWidth / width, maxHeight / height);
        scale = Math.Min(1, scale);
        return (Math.Max(1, width * scale), Math.Max(1, height * scale));
    }

    /// <summary>
    /// Normalizes layer.
    /// </summary>
    private void NormalizeLayer(PictureLayer layer)
    {
    try
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
            layer.ClipPolygon = NormalizeClipPolygon(layer.ClipPolygon);
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
            if (layer is PaintPictureLayer paint)
            {
                paint.Strokes ??= [];
                foreach (var stroke in paint.Strokes)
                {
                    stroke.WidthPx = Math.Clamp(stroke.WidthPx, .25, 512);
                    stroke.Opacity = Math.Clamp(stroke.Opacity, 0, 1);
                    stroke.Hardness = Math.Clamp(stroke.Hardness, 0, 1);
                    stroke.Points ??= [];
                }
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.NormalizeLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes clip polygon.
    /// </summary>
    private List<PicturePoint> NormalizeClipPolygon(IEnumerable<PicturePoint>? points)
    {
    try
    {
            var normalized = (points ?? [])
                .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Take(2048)
                .Select(point => new PicturePoint
                {
                    X = Math.Clamp(point.X, -16384, 32768),
                    Y = Math.Clamp(point.Y, -16384, 32768)
                })
                .ToList();

            while (normalized.Count > 1 && NearlyEqual(normalized[0].X, normalized[^1].X) && NearlyEqual(normalized[0].Y, normalized[^1].Y))
                normalized.RemoveAt(normalized.Count - 1);
            return normalized.Count >= 3 ? normalized : [];
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.NormalizeClipPolygon failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the nearly equal operation.
    /// </summary>
    private bool NearlyEqual(double first, double second) {
    try
    {
        return Math.Abs(first - second) < .0001;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.NearlyEqual failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Normalizes angle.
    /// </summary>
    private double NormalizeAngle(double value) {
    try
    {
        return (value % 360 + 360) % 360;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.NormalizeAngle failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Runs the notify operation.
    /// </summary>
    private void Notify(bool markChanged = true) {
    try
    {
        Changed?.Invoke();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.Notify failed: {__serviceMethodException}");
        throw;
    }
}
}
