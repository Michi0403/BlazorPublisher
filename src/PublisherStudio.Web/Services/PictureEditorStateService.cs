using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Coordinates picture editor state behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class PictureEditorStateService
{
    /// <summary>
    /// Stores the picture document service dependency used by <see cref="PictureEditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PictureDocumentService _documents;
    /// <summary>
    /// Stores the publisher document factory dependency used by <see cref="PictureEditorStateService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublisherDocumentFactory _documentFactory;
    /// <summary>
    /// Stores the internal undo state used by <see cref="PictureEditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Stack<string> _undo = new();
    /// <summary>
    /// Stores the internal redo state used by <see cref="PictureEditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Stack<string> _redo = new();
    /// <summary>
    /// Stores the internal live edit key state used by <see cref="PictureEditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private string? _liveEditKey;
    /// <summary>
    /// Stores the internal clipboard state used by <see cref="PictureEditorStateService"/> while executing its surrounding workflow.
    /// </summary>
    private PictureLayer? _clipboard;

    /// <summary>
    /// Initializes a new <see cref="PictureEditorStateService"/> instance and captures the dependencies or initial state required by its picture editor state workflow.
    /// </summary>
    /// <param name="documents">Picture document service dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="documentFactory">Publisher document factory dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    public PictureEditorStateService(PictureDocumentService documents, IPublisherDocumentFactory documentFactory)
    {
        _documents = documents;
        _documentFactory = documentFactory;
        Document = _documentFactory.CreatePicture();
    }

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="PictureEditorStateService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets or sets the document value that forms part of the picture editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document value exposed by <see cref="PictureEditorStateService"/>.</value>
    public PictureDocument Document { get; private set; }
    /// <summary>
    /// Gets or sets the stable selected layer identifier used to identify or correlate this picture editor state instance with related application state.
    /// </summary>
    /// <value>The selected layer identifier value exposed by <see cref="PictureEditorStateService"/>.</value>
    public Guid? SelectedLayerId { get; private set; }
    /// <summary>
    /// Gets the selected layer value that forms part of the picture editor state state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected layer value exposed by <see cref="PictureEditorStateService"/>.</value>
    public PictureLayer? SelectedLayer => Document.Layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);
    /// <summary>
    /// Gets a value indicating whether undo applies to the picture editor state state.
    /// </summary>
    /// <value>The can undo value exposed by <see cref="PictureEditorStateService"/>.</value>
    public bool CanUndo => _undo.Count > 0;
    /// <summary>
    /// Gets a value indicating whether redo applies to the picture editor state state.
    /// </summary>
    /// <value>The can redo value exposed by <see cref="PictureEditorStateService"/>.</value>
    public bool CanRedo => _redo.Count > 0;
    /// <summary>
    /// Gets a value indicating whether paste applies to the picture editor state state.
    /// </summary>
    /// <value>The can paste value exposed by <see cref="PictureEditorStateService"/>.</value>
    public bool CanPaste => _clipboard is not null;

    /// <summary>
    /// Starts new as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="widthPx">Width px value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="heightPx">Height px value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="transparent">Value indicating whether transparent should apply to this operation.</param>
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
    /// <param name="document">Document value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Starts from raster as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="widthPx">Width px value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="heightPx">Height px value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Performs clone document as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The picture document produced by the operation.</returns>
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
    /// Sets document name as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Sets document size as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="widthPx">Width px value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="heightPx">Height px value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Sets background as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Sets zoom as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="zoom">Zoom value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Sets grid as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="visible">Value indicating whether visible should apply to this operation.</param>
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
    /// Sets snap as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
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
    /// Sets grid spacing as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pixels">Pixels value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Performs select layer as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
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
    /// Adds raster as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="naturalWidth">Natural width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="naturalHeight">Natural height value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The raster picture layer produced by the operation.</returns>
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
    /// Adds imported layers as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="importedDocument">Imported document value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="groupName">Group name value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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
    /// Performs replace raster as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="dataUrl">Data url value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Adds text as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The text picture layer produced by the operation.</returns>
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
    /// Adds shape as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shape">Shape value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The shape picture layer produced by the operation.</returns>
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
    /// Adds shape at as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shape">Shape value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="rotation">Rotation value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The shape picture layer produced by the operation.</returns>
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
    /// Adds path as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="strokeColor">Stroke color value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="strokeWidth">Stroke width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="closed">Value indicating whether closed should apply to this operation.</param>
    /// <param name="smooth">Value indicating whether smooth should apply to this operation.</param>
    /// <returns>The shape picture layer produced by the operation.</returns>
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
    /// Adds area fill as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="selectionKind">Selection kind value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="primaryColor">Primary color value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="secondaryColor">Secondary color value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="gradient">Value indicating whether gradient should apply to this operation.</param>
    /// <returns>The shape picture layer produced by the operation.</returns>
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
    /// Adds fill as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fillKind">Fill kind value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The fill picture layer produced by the operation.</returns>
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
    /// Adds render as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="renderKind">Render kind value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The render picture layer produced by the operation.</returns>
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
    /// Adds paint as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The paint picture layer produced by the operation.</returns>
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
    /// Adds stroke as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="color">Color value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="widthPx">Width px value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="opacity">Opacity value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="hardness">Hardness value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Performs clear selected paint as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Deletes selected as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs copy selected as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs copy selected region as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="inverted">Value indicating whether inverted should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Applies selected clip as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <param name="inverted">Value indicating whether inverted should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs clear selected clip as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs paste as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs duplicate selected as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs center selected as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs fit selected to canvas as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs move selected layer as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="delta">Delta value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Moves one picture layer to an explicit back-to-front index while preserving the current editable document and undo history.
    /// </summary>
    /// <param name="layerId">Identifier of the picture layer to move.</param>
    /// <param name="targetIndex">Zero-based destination in the document's back-to-front layer collection.</param>
    public void MoveLayerToIndex(Guid layerId, int targetIndex)
    {
        try
        {
            var layer = Document.Layers.FirstOrDefault(item => item.Id == layerId);
            if (layer is null || Document.Layers.Count <= 1) return;
            var currentIndex = Document.Layers.IndexOf(layer);
            var destination = Math.Clamp(targetIndex, 0, Document.Layers.Count - 1);
            if (destination == currentIndex) return;
            Capture();
            Document.Layers.RemoveAt(currentIndex);
            Document.Layers.Insert(Math.Clamp(destination, 0, Document.Layers.Count), layer);
            SelectedLayerId = layer.Id;
            Notify();
        }
        catch (Exception __serviceMethodException)
        {
            System.Diagnostics.Trace.TraceError($"Service method PictureEditorStateService.MoveLayerToIndex failed: {__serviceMethodException}");
            throw;
        }
    }

    /// <summary>
    /// Performs bring selected to front as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs send selected to back as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs toggle visibility as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
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
    /// Performs toggle lock as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
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
    /// Performs commit transform as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="x">X value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="rotation">Rotation value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Updates selected as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="capture">Value indicating whether capture should apply to this operation.</param>
    /// <param name="allowLocked">Value indicating whether allow locked should apply to this operation.</param>
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
    /// Updates selected live as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Performs end live edit as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs undo as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs redo as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs restore as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the picture editor state operation and used when producing its result.</param>
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
    /// Performs reset history as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Performs capture as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
    /// Creates paint layer as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The paint picture layer produced by the operation.</returns>
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
    /// Performs clone layer as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The picture layer produced by the operation.</returns>
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
    /// Performs next name as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="basis">Basis value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs fit size as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="maxWidth">Max width value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="maxHeight">Max height value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The double width double height produced by the operation.</returns>
    private (double Width, double Height) FitSize(double width, double height, double maxWidth, double maxHeight)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var scale = Math.Min(maxWidth / width, maxHeight / height);
        scale = Math.Min(1, scale);
        return (Math.Max(1, width * scale), Math.Max(1, height * scale));
    }

    /// <summary>
    /// Normalizes layer as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor state operation and used when producing its result.</param>
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
            if (layer is RasterPictureLayer raster)
            {
                raster.TintOpacity = Math.Clamp(raster.TintOpacity, 0, 1);
                raster.ColorizeTolerance = Math.Clamp(raster.ColorizeTolerance, 0, 255);
                raster.ColorizeStrength = Math.Clamp(raster.ColorizeStrength, 0, 1);
                raster.ColorizeSourceColor = string.IsNullOrWhiteSpace(raster.ColorizeSourceColor) ? "#ffffff" : raster.ColorizeSourceColor;
                raster.ColorizeTargetColor = string.IsNullOrWhiteSpace(raster.ColorizeTargetColor) ? "#dc2626" : raster.ColorizeTargetColor;
            }
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
    /// Normalizes clip polygon as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="points">Picture point dependency used by the picture editor state workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs nearly equal as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor state operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Normalizes angle as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor state operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
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
    /// Performs notify as part of the picture editor state service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="markChanged">Value indicating whether mark changed should apply to this operation.</param>
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
