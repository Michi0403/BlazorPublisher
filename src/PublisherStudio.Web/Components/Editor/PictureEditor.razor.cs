using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using DevExpress.Blazor;
using Microsoft.JSInterop;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;
using PublisherStudio.Services.PictureStudio.Import;
using PublisherStudio.Services.OrganicPlugins;
using PublisherStudio.Services.UserExperience;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// Represents a picture editor.
/// </summary>
public partial class PictureEditor
{
    private readonly string[] PictureColors =
    [
        "#000000", "#ffffff", "#ef4444", "#f97316", "#eab308", "#22c55e", "#06b6d4", "#3b82f6", "#8b5cf6", "#ec4899", "#64748b", "#92400e"
    ];

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private SystemFontCatalog SystemFonts { get; set; } = default!;
    /// <summary>
    /// Gets or sets state.
    /// </summary>
    [Inject] public PictureEditorStateService State { get; set; } = default!;
    [Inject] private OpenRasterImportService OpenRasterImporter { get; set; } = default!;
    [Inject] private ILocalGptConnectionService LocalGptConnection { get; set; } = default!;
    [Inject] private IUserNotificationService Notifications { get; set; } = default!;
    [Inject] private ILogger<PictureEditor> Logger { get; set; } = default!;
    [Inject] private IPublisherRuntimePolicyDataService RuntimePolicy { get; set; } = default!;
    [Inject] private PublicationFileService Files { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    [Parameter] public bool Visible { get; set; }
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    [Parameter] public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets initial document.
    /// </summary>
    [Parameter] public PictureDocument? InitialDocument { get; set; }
    /// <summary>
    /// Gets or sets initial raster data URL.
    /// </summary>
    [Parameter] public string? InitialRasterDataUrl { get; set; }
    /// <summary>
    /// Gets or sets initial name.
    /// </summary>
    [Parameter] public string InitialName { get; set; } = "Picture";
    /// <summary>
    /// Gets or sets editing existing.
    /// </summary>
    [Parameter] public bool EditingExisting { get; set; }
    /// <summary>
    /// Gets or sets saved.
    /// </summary>
    [Parameter] public EventCallback<PictureEditorResult> Saved { get; set; }
    /// <summary>
    /// Gets or sets cancelled.
    /// </summary>
    [Parameter] public EventCallback Cancelled { get; set; }

    private IReadOnlyList<string> PictureFonts => SystemFonts.FontFamilies;

    private IJSObjectReference? _module;
    private DxContextMenu _pictureContextMenu = default!;
    private DotNetObjectReference<PictureEditor>? _self;
    private Guid _loadedSession;
    private bool _renderRequested;
    private bool _initialized;
    private bool _pendingRasterInitialization;
    private string? _error;
    private string? _notice;
    private bool _renderErrorActive;
    private PictureDrawTool _drawTool = PictureDrawTool.Select;
    private string _drawColor = "#111827";
    private string _drawSecondaryColor = "#ffffff";
    private double _drawWidth = 12;
    private double _drawOpacity = 1;
    private double _drawHardness = .8;
    private StringBuilder? _pictureExportBuffer;
    private string? _pictureExportId;
    private PictureDocument? _pictureExportSourceDocument;
    private string? _pictureExportName;
    private string _pictureExportPurpose = "save";
    private string _ocrText = string.Empty;
    private string _ocrStatus = string.Empty;
    private bool _ocrBusy;
    private int _pictureExportExpectedChunks;
    private int _pictureExportNextChunk;
    private int _pictureExportExpectedLength;
    private Guid? _replaceRasterLayerId;
    private double? _pendingDropX;
    private double? _pendingDropY;

    private bool HasSelection => State.SelectedLayer is not null;
    private bool CanDelete => State.SelectedLayer is { Locked: false };
    private bool HasLayerClip => State.SelectedLayer is { ClipPolygon.Count: >= 3 };
    private bool IsRenderSelected => State.SelectedLayer is RenderPictureLayer;
    private bool IsRasterSelected => State.SelectedLayer is RasterPictureLayer;
    private bool IsPaintSelected => State.SelectedLayer is PaintPictureLayer;
    private bool CanDraw => _drawTool != PictureDrawTool.Select;
    private bool IsPictureExporting => _pictureExportId is not null;
    private bool CanUseLocalGptOcr => Visible && LocalGptConnection.State.IsLinked && LocalGptConnection.State.HasCapability("localgpt.vision.ocr");
    private bool HasOcrText => !string.IsNullOrWhiteSpace(_ocrText);
    private string SelectToolText => ToolText(PictureDrawTool.Select, "Select");
    private string BrushToolText => ToolText(PictureDrawTool.Brush, "Brush");
    private string PencilToolText => ToolText(PictureDrawTool.Pencil, "Pencil");
    private string SprayToolText => ToolText(PictureDrawTool.Spray, "Spray can");
    private string ToothbrushToolText => ToolText(PictureDrawTool.Toothbrush, "Toothbrush");
    private string SquareToolText => ToolText(PictureDrawTool.Square, "Square");
    private string RectangleToolText => ToolText(PictureDrawTool.Rectangle, "Rectangle");
    private string EllipseToolText => ToolText(PictureDrawTool.Ellipse, "Ellipse");
    private string ArrowToolText => ToolText(PictureDrawTool.Arrow, "Arrow");
    private string LineToolText => ToolText(PictureDrawTool.Line, "Line");
    private string PathToolText => ToolText(PictureDrawTool.Path, "Path");
    private string EraserToolText => ToolText(PictureDrawTool.Eraser, "Eraser");
    private string EyedropperToolText => ToolText(PictureDrawTool.Eyedropper, "Eyedropper");
    private string RectangleSelectToolText => ToolText(PictureDrawTool.RectangleSelect, "Rectangle select");
    private string EllipseSelectToolText => ToolText(PictureDrawTool.EllipseSelect, "Ellipse select");
    private string FreeSelectToolText => ToolText(PictureDrawTool.FreeSelect, "Freehand select");
    private string MagneticSelectToolText => ToolText(PictureDrawTool.MagneticSelect, "Magnetic select");
    private string PolygonSelectToolText => ToolText(PictureDrawTool.PolygonSelect, "Polygon select");
    private string FillSolidToolText => ToolText(PictureDrawTool.FillSolid, "Solid fill");
    private string FillGradientToolText => ToolText(PictureDrawTool.FillGradient, "Gradient fill");
    private double BrushWidthSliderValue => WidthToSlider(_drawWidth);
    private string BrushWidthSliderStyle => $"--picture-range-progress: {Inv(BrushWidthSliderValue)}%;";
    private string DrawWidthDisplay => $"{_drawWidth:0.##} px";
    private string CanvasHint => _drawTool switch
    {
        PictureDrawTool.Select => "Drag layers directly. Corner handles resize; the round handle rotates. Right-click for layer commands.",
        PictureDrawTool.Eyedropper => "Click the rendered canvas to pick a color, then the Brush tool becomes active.",
        PictureDrawTool.Line => "Drag from the line start to its end. Hold the pointer down for a live preview.",
        PictureDrawTool.Path => "Click to place straight vector nodes. Double-click or press Enter to finish; hold Shift while finishing to close the path. Nodes remain editable.",
        PictureDrawTool.Eraser => "Draw over strokes on a paint layer to erase them non-destructively.",
        PictureDrawTool.RectangleSelect => "Drag a rectangular area selection. Use a fill tool to turn it into an editable layer.",
        PictureDrawTool.EllipseSelect => "Drag an elliptical area selection. Use a fill tool to turn it into an editable layer.",
        PictureDrawTool.FreeSelect => "Draw a freehand lasso around the area you want to fill.",
        PictureDrawTool.MagneticSelect => "Draw a lasso that snaps to nearby layer edges and corners.",
        PictureDrawTool.PolygonSelect => "Click/tap vertices in any angle. Double-click or press Enter to close the polygon, then keep, cut, copy, or fill that region.",
        PictureDrawTool.FillSolid => "Click to fill the current area selection, or drag a new rectangular filled area.",
        PictureDrawTool.FillGradient => "Click to gradient-fill the current area selection, or drag a new rectangular gradient area.",
        PictureDrawTool.Spray => "Spray paint scatters soft droplets around the pointer path for airbrush-like shading.",
        PictureDrawTool.Toothbrush => "Toothbrush lays down rough bristle streaks and splatter for textured paint effects.",
        PictureDrawTool.Square => "Drag a square directly onto the canvas. The result remains an editable shape layer.",
        PictureDrawTool.Rectangle => "Drag a rectangle directly onto the canvas. The result remains an editable shape layer.",
        PictureDrawTool.Ellipse => "Drag an ellipse directly onto the canvas. The result remains an editable shape layer.",
        PictureDrawTool.Arrow => "Drag from the arrow tail to its point. The result remains an editable, rotatable shape layer.",
        _ => "Draw directly on the canvas. A paint layer is created automatically when necessary. Right-click does not draw."
    };
    private string CanvasColor => State.Document.Background.StartsWith('#') && State.Document.Background.Length is 4 or 7
        ? State.Document.Background
        : "#ffffff";
    private string StatusText => _error ?? _notice ?? (IsPictureExporting
        ? "Rendering PNG for the publication…"
        : _drawTool != PictureDrawTool.Select
            ? $"{_drawTool} tool · {_drawWidth:0.#} px · {_drawColor}"
            : State.SelectedLayer is null ? "No layer selected" : $"{State.SelectedLayer.Kind}: {State.SelectedLayer.Name}");

    /// <summary>
    /// Runs the on initialized operation.
    /// </summary>
    protected override void OnInitialized()
    {
        State.Changed += StateChanged;
        LocalGptConnection.Changed += LocalGptConnectionChanged;
    }

    /// <summary>
    /// Runs the local gpt connection changed operation.
    /// </summary>
    private void LocalGptConnectionChanged() => _ = InvokeAsync(StateHasChanged);

    /// <summary>
    /// Runs the on parameters set operation.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (!Visible)
        {
            // The conditional Razor block removes the canvas from the DOM. Rebind the new canvas on the next open.
            _initialized = false;
            return;
        }
        if (SessionId == Guid.Empty || SessionId == _loadedSession) return;
        _loadedSession = SessionId;
        _error = null;
        _notice = null;
        _renderErrorActive = false;
        _drawTool = PictureDrawTool.Select;
        ClearPendingDropPosition();
        if (InitialDocument is not null)
        {
            _pendingRasterInitialization = false;
            State.StartFromDocument(InitialDocument);
        }
        else if (!string.IsNullOrWhiteSpace(InitialRasterDataUrl))
        {
            // Natural pixel dimensions are resolved after the JS module is available.
            _pendingRasterInitialization = true;
            State.StartNew();
            State.SetDocumentName(InitialName);
        }
        else
        {
            _pendingRasterInitialization = false;
            State.StartNew();
        }
        _renderRequested = true;
    }

    /// <summary>
    /// Runs the on after render async operation.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!Visible) return;
        _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/pictureStudioInterop.js");
        _self ??= DotNetObjectReference.Create(this);
        if (_pendingRasterInitialization && !string.IsNullOrWhiteSpace(InitialRasterDataUrl))
        {
            _pendingRasterInitialization = false;
            if (!IsSupportedImageDataUrl(InitialRasterDataUrl))
            {
                _error = "The selected picture does not contain a valid embedded image source.";
                State.StartNew();
                State.SetDocumentName(InitialName);
            }
            else
            {
                try
                {
                    var natural = await _module.InvokeAsync<PictureImageSize>("getPictureImageSize", InitialRasterDataUrl);
                    var fitted = FitRasterCanvasSize(natural.Width, natural.Height);
                    State.StartFromRaster(InitialRasterDataUrl, InitialName, fitted.Width, fitted.Height);
                }
                catch (Exception ex)
                {
                    _error = $"The source image could not be decoded: {ex.Message}";
                    State.StartNew();
                    State.SetDocumentName(InitialName);
                }
            }
        }
        if (!_initialized)
        {
            await _module.InvokeVoidAsync(
                "initializePictureStudio",
                RuntimePolicy.PictureStudio.CanvasId,
                _self,
                RuntimePolicy.PictureStudio.StudioRootId,
                RuntimePolicy.PictureStudio.ImageDropInputId,
                RuntimePolicy.PictureStudio.LayerDropInputId);
            _initialized = true;
            _renderRequested = true;
        }
        if (_renderRequested)
        {
            _renderRequested = false;
            await RenderCanvasAsync();
        }
    }

    /// <summary>
    /// Runs the state changed operation.
    /// </summary>
    private void StateChanged()
    {
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Runs the render canvas async operation.
    /// </summary>
    private async Task RenderCanvasAsync()
    {
        if (_module is null || !Visible) return;
        try
        {
            await _module.InvokeVoidAsync("renderPictureStudio", RuntimePolicy.PictureStudio.CanvasId, State.Document, State.SelectedLayerId?.ToString(), State.Document.Zoom, new
            {
                Tool = _drawTool.ToString(),
                Color = _drawColor,
                SecondaryColor = _drawSecondaryColor,
                Width = _drawWidth,
                Opacity = _drawOpacity,
                Hardness = _drawHardness
            });
        }
        catch (JSDisconnectedException)
        {
            // The browser circuit is closing.
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Runs the picture layer selected operation.
    /// </summary>
    [JSInvokable]
    public void PictureLayerSelected(string? id)
    {
        State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
    }

    /// <summary>
    /// Runs the picture transform committed operation.
    /// </summary>
    [JSInvokable]
    public void PictureTransformCommitted(string id, double x, double y, double width, double height, double rotation)
    {
        if (Guid.TryParse(id, out var parsed))
            State.CommitTransform(parsed, x, y, width, height, rotation);
    }

    /// <summary>
    /// Runs the picture stroke committed operation.
    /// </summary>
    [JSInvokable]
    public void PictureStrokeCommitted(string tool, double[] coordinates, string color, double width, double opacity, double hardness)
    {
        if (!Enum.TryParse<PictureStrokeKind>(tool, true, out var kind) || coordinates.Length < 4) return;
        var points = new List<PicturePoint>(coordinates.Length / 2);
        for (var index = 0; index + 1 < coordinates.Length; index += 2)
            points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
        State.AddStroke(kind, points, color, width, opacity, hardness);
    }

    /// <summary>
    /// Runs the picture shape committed operation.
    /// </summary>
    [JSInvokable]
    public void PictureShapeCommitted(string tool, double x, double y, double width, double height, double rotation)
    {
        var shape = tool?.Trim().ToLowerInvariant() switch
        {
            "ellipse" => PictureShapeKind.Ellipse,
            "arrow" => PictureShapeKind.Arrow,
            _ => PictureShapeKind.Rectangle
        };
        State.AddShapeAt(shape, x, y, width, height, rotation);
        SetDrawTool(PictureDrawTool.Select);
    }

    /// <summary>
    /// Runs the picture path committed operation.
    /// </summary>
    [JSInvokable]
    public void PicturePathCommitted(double[] coordinates, bool closed, bool smooth)
    {
        if (coordinates.Length < 4) return;
        var points = new List<PicturePoint>(coordinates.Length / 2);
        for (var index = 0; index + 1 < coordinates.Length; index += 2)
            points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
        State.AddPath(points, _drawColor, _drawWidth, closed, smooth);
        SetDrawTool(PictureDrawTool.Select);
    }

    /// <summary>
    /// Runs the picture area fill committed operation.
    /// </summary>
    [JSInvokable]
    public void PictureAreaFillCommitted(string selectionKind, double[] coordinates, string primaryColor, string secondaryColor, bool gradient)
    {
        if (coordinates.Length < 4) return;
        var points = new List<PicturePoint>(coordinates.Length / 2);
        for (var index = 0; index + 1 < coordinates.Length; index += 2)
            points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
        State.AddAreaFill(selectionKind, points, primaryColor, secondaryColor, gradient);
        SetDrawTool(PictureDrawTool.Select);
    }

    /// <summary>
    /// Runs the picture color picked operation.
    /// </summary>
    [JSInvokable]
    public void PictureColorPicked(string color)
    {
        if (!string.IsNullOrWhiteSpace(color)) _drawColor = color;
        _drawTool = PictureDrawTool.Brush;
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Runs the picture shortcut requested operation.
    /// </summary>
    [JSInvokable]
    public async Task PictureShortcutRequested(string command)
    {
        switch (command?.Trim().ToLowerInvariant())
        {
            case "undo": State.Undo(); break;
            case "redo": State.Redo(); break;
            case "copy":
                if (!await CopyAreaSelectionToClipboardAsync()) State.CopySelected();
                break;
            case "paste": State.Paste(); break;
            case "duplicate": State.DuplicateSelected(); break;
            case "delete":
                if (!await ApplyAreaClipAsync(inverted: true, quietWhenMissing: true)) State.DeleteSelected();
                break;
            case "front": State.BringSelectedToFront(); break;
            case "back": State.SendSelectedToBack(); break;
            case "select": SetDrawTool(PictureDrawTool.Select); break;
        }
    }

    /// <summary>
    /// Runs the picture render failed operation.
    /// </summary>
    [JSInvokable]
    public void PictureRenderFailed(string message)
    {
        _renderErrorActive = true;
        _error = string.IsNullOrWhiteSpace(message) ? "A picture layer could not be rendered." : message;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Runs the picture render recovered operation.
    /// </summary>
    [JSInvokable]
    public void PictureRenderRecovered()
    {
        if (!_renderErrorActive) return;
        _renderErrorActive = false;
        _error = null;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Runs the show canvas context menu operation.
    /// </summary>
    private async Task ShowCanvasContextMenu(MouseEventArgs args)
    {
        if (_module is not null && args.Button == 2)
        {
            var id = await _module.InvokeAsync<string?>("hitTestPictureStudioLayer", RuntimePolicy.PictureStudio.CanvasId, args.ClientX, args.ClientY);
            State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
        }
        await InvokeAsync(StateHasChanged);
        await _pictureContextMenu.ShowAsync(args);
    }

    /// <summary>
    /// Runs the show layer context menu operation.
    /// </summary>
    private async Task ShowLayerContextMenu(PictureLayer layer, MouseEventArgs args)
    {
        State.SelectLayer(layer.Id);
        await InvokeAsync(StateHasChanged);
        await _pictureContextMenu.ShowAsync(args);
    }

    /// <summary>
    /// Runs the show layer list context menu operation.
    /// </summary>
    private async Task ShowLayerListContextMenu(MouseEventArgs args)
    {
        State.SelectLayer(null);
        await InvokeAsync(StateHasChanged);
        await _pictureContextMenu.ShowAsync(args);
    }

    /// <summary>
    /// Runs the request image operation.
    /// </summary>
    private async Task RequestImage()
    {
        _replaceRasterLayerId = null;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId);
    }

    /// <summary>
    /// Runs the request layered import operation.
    /// </summary>
    private async Task RequestLayeredImport()
    {
        _replaceRasterLayerId = null;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.LayeredInputId);
    }

    /// <summary>
    /// Runs the request raster replacement operation.
    /// </summary>
    private async Task RequestRasterReplacement()
    {
        if (State.SelectedLayer is not RasterPictureLayer { Locked: false } raster) return;
        _replaceRasterLayerId = raster.Id;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId);
    }

    /// <summary>
    /// Imports image.
    /// </summary>
    private Task ImportImage(InputFileChangeEventArgs args) => ImportImageCore(args, forceAdd: false);

    /// <summary>
    /// Imports dropped image.
    /// </summary>
    private Task ImportDroppedImage(InputFileChangeEventArgs args) => ImportImageCore(args, forceAdd: true);

    /// <summary>
    /// Imports image core.
    /// </summary>
    private async Task ImportImageCore(InputFileChangeEventArgs args, bool forceAdd)
    {
        try
        {
            var file = args.File;
            var allowed = new[] { "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml" };
            if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported picture format.");

            await using var stream = file.OpenReadStream(long.MaxValue);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var dataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
            var size = _module is null
                ? new PictureImageSize()
                : await _module.InvokeAsync<PictureImageSize>("getPictureImageSize", dataUrl);
            if (!forceAdd && _replaceRasterLayerId is Guid targetId && State.ReplaceRaster(targetId, dataUrl))
                State.SelectLayer(targetId);
            else
                State.AddRaster(dataUrl, file.Name, size.Width, size.Height,
                    forceAdd ? _pendingDropX : null, forceAdd ? _pendingDropY : null);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _replaceRasterLayerId = null;
            if (forceAdd) ClearPendingDropPosition();
        }
    }

    /// <summary>
    /// Imports layered document.
    /// </summary>
    private Task ImportLayeredDocument(InputFileChangeEventArgs args) => ImportLayeredDocumentCore(args, append: false);

    /// <summary>
    /// Imports dropped layered document.
    /// </summary>
    private Task ImportDroppedLayeredDocument(InputFileChangeEventArgs args) => ImportLayeredDocumentCore(args, append: true);

    /// <summary>
    /// Imports layered document core.
    /// </summary>
    private async Task ImportLayeredDocumentCore(InputFileChangeEventArgs args, bool append)
    {
        _error = null;
        _notice = null;
        try
        {
            var file = args.File;
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            PictureImportResult result;
            await using var stream = file.OpenReadStream(long.MaxValue);
            if (extension == ".ora")
            {
                result = await OpenRasterImporter.ImportAsync(stream, file.Name);
            }
            else if (extension is ".svg" or ".svgz" || file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                if (_module is null) throw new InvalidOperationException("Picture Studio is not ready yet.");
                string svgText;
                if (extension == ".svgz" || file.ContentType.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                    svgText = await ReadSvgTextAsync(gzip);
                }
                else
                {
                    svgText = await ReadSvgTextAsync(stream);
                }
                var dataUrl = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svgText))}";
                result = await _module.InvokeAsync<PictureImportResult>("importPictureStudioSvg", dataUrl, file.Name);
            }
            else
            {
                throw new InvalidDataException("Use an SVG, compressed SVGZ, or OpenRaster ORA document.");
            }

            if (append)
            {
                var added = State.AddImportedLayers(result.Document, Path.GetFileNameWithoutExtension(file.Name),
                    _pendingDropX, _pendingDropY);
                if (added == 0) throw new InvalidDataException("The dropped picture document does not contain importable layers.");
            }
            else
            {
                State.StartFromDocument(result.Document);
                State.SetDocumentName(Path.GetFileNameWithoutExtension(file.Name));
            }
            var losses = result.Issues.Count(item => item.Severity == InterchangeIssueSeverity.Loss);
            var warnings = result.Issues.Count(item => item.Severity == InterchangeIssueSeverity.Warning);
            var importedCount = append ? result.Document.Layers.Count : State.Document.Layers.Count;
            _notice = $"{(append ? "Added" : "Imported")} {importedCount} editable layer{(importedCount == 1 ? string.Empty : "s")}." +
                (losses + warnings > 0 ? $" {warnings} warning(s), {losses} compatibility loss(es)." : string.Empty);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            if (append) ClearPendingDropPosition();
        }
    }

    /// <summary>
    /// Runs the picture studio file drop positioned operation.
    /// </summary>
    [JSInvokable]
    public void PictureStudioFileDropPositioned(double? x, double? y)
    {
        _pendingDropX = x is double px && double.IsFinite(px) ? Math.Clamp(px, 0, State.Document.WidthPx) : null;
        _pendingDropY = y is double py && double.IsFinite(py) ? Math.Clamp(py, 0, State.Document.HeightPx) : null;
    }

    /// <summary>
    /// Runs the clear pending drop position operation.
    /// </summary>
    private void ClearPendingDropPosition()
    {
        _pendingDropX = null;
        _pendingDropY = null;
    }

    /// <summary>
    /// Runs the picture studio file drop rejected operation.
    /// </summary>
    [JSInvokable]
    public void PictureStudioFileDropRejected(string? message)
    {
        ClearPendingDropPosition();
        _error = string.IsNullOrWhiteSpace(message)
            ? "Drop a PNG, JPEG, GIF, WebP, SVG, SVGZ, or OpenRaster picture into Picture Studio."
            : message;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Adds text layer.
    /// </summary>
    private void AddTextLayer() => State.AddText();
    /// <summary>
    /// Adds rectangle.
    /// </summary>
    private void AddRectangle() => State.AddShape(PictureShapeKind.Rectangle);
    /// <summary>
    /// Adds ellipse.
    /// </summary>
    private void AddEllipse() => State.AddShape(PictureShapeKind.Ellipse);
    /// <summary>
    /// Adds arrow shape.
    /// </summary>
    private void AddArrowShape() => State.AddShape(PictureShapeKind.Arrow);
    /// <summary>
    /// Adds line shape.
    /// </summary>
    private void AddLineShape() => State.AddShape(PictureShapeKind.Line);
    /// <summary>
    /// Adds gradient.
    /// </summary>
    private void AddGradient() => State.AddFill(PictureFillKind.LinearGradient);
    /// <summary>
    /// Adds solid fill.
    /// </summary>
    private void AddSolidFill() => State.AddFill(PictureFillKind.Solid);
    /// <summary>
    /// Adds clouds.
    /// </summary>
    private void AddClouds() => State.AddRender(PictureRenderKind.Clouds);
    /// <summary>
    /// Adds noise.
    /// </summary>
    private void AddNoise() => State.AddRender(PictureRenderKind.Noise);
    /// <summary>
    /// Adds stripes.
    /// </summary>
    private void AddStripes() => State.AddRender(PictureRenderKind.Stripes);
    /// <summary>
    /// Adds vignette.
    /// </summary>
    private void AddVignette() => State.AddRender(PictureRenderKind.Vignette);
    /// <summary>
    /// Adds bloom.
    /// </summary>
    private void AddBloom() => State.AddRender(PictureRenderKind.Bloom);
    /// <summary>
    /// Adds neon.
    /// </summary>
    private void AddNeon() => State.AddRender(PictureRenderKind.Neon);
    /// <summary>
    /// Adds lens flare.
    /// </summary>
    private void AddLensFlare() => State.AddRender(PictureRenderKind.LensFlare);
    /// <summary>
    /// Adds grain noise.
    /// </summary>
    private void AddGrainNoise() => State.AddRender(PictureRenderKind.GrainNoise);
    /// <summary>
    /// Adds motion blur.
    /// </summary>
    private void AddMotionBlur() => State.AddRender(PictureRenderKind.MotionBlur);
    /// <summary>
    /// Adds wind.
    /// </summary>
    private void AddWind() => State.AddRender(PictureRenderKind.Wind);
    /// <summary>
    /// Adds ocean waves.
    /// </summary>
    private void AddOceanWaves() => State.AddRender(PictureRenderKind.OceanWaves);
    /// <summary>
    /// Adds paint layer.
    /// </summary>
    private void AddPaintLayer() => State.AddPaint();
    /// <summary>
    /// Runs the move up operation.
    /// </summary>
    private void MoveUp() => State.MoveSelectedLayer(1);
    /// <summary>
    /// Runs the move down operation.
    /// </summary>
    private void MoveDown() => State.MoveSelectedLayer(-1);
    /// <summary>
    /// Runs the zoom100 operation.
    /// </summary>
    private void Zoom100() => State.SetZoom(1);

    /// <summary>
    /// Runs the fit canvas operation.
    /// </summary>
    private async Task FitCanvas()
    {
        if (_module is null) return;
        var zoom = await _module.InvokeAsync<double>("fitPictureStudio", RuntimePolicy.PictureStudio.CanvasHostId, State.Document.WidthPx, State.Document.HeightPx);
        State.SetZoom(zoom);
    }

    /// <summary>
    /// Runs the apply operation.
    /// </summary>
    private async Task Apply()
    {
        if (_module is null || _self is null || _pictureExportId is not null) return;

        var exportId = Guid.NewGuid().ToString("N");
        var sourceDocument = State.CloneDocument();
        _pictureExportId = exportId;
        _pictureExportSourceDocument = sourceDocument;
        _pictureExportName = State.Document.Name;
        _pictureExportPurpose = "save";
        _pictureExportBuffer = null;
        _pictureExportExpectedChunks = 0;
        _pictureExportNextChunk = 0;
        _pictureExportExpectedLength = 0;
        _error = null;

        try
        {
            // Generate the same PNG that the Download button produces, but feed the
            // data URL back in small chunks. This avoids the failing Blob stream
            // reference and the 32 KB Interactive Server message-size ceiling.
            // The JavaScript function starts the export and returns immediately;
            // CompletePictureExport performs the actual insert after all chunks arrive.
            await _module.InvokeVoidAsync(
                "startPictureStudioDataUrlExport",
                sourceDocument,
                "image/png",
                1d,
                _self,
                exportId);
        }
        catch (Exception ex)
        {
            if (IsCurrentPictureExport(exportId))
            {
                ResetPictureExport();
                _error = ex.Message;
            }
        }
    }

    /// <summary>
    /// Runs the begin picture export operation.
    /// </summary>
    [JSInvokable]
    public bool BeginPictureExport(string exportId, int totalLength, int chunkCount)
    {
        if (!IsCurrentPictureExport(exportId)) return false;
        if (totalLength <= 0)
        {
            FailPictureExport(exportId, "The rendered picture export reported an invalid length.");
            return false;
        }
        if (chunkCount <= 0)
        {
            FailPictureExport(exportId, "The rendered picture export contains an invalid chunk count.");
            return false;
        }

        _pictureExportExpectedLength = totalLength;
        _pictureExportExpectedChunks = chunkCount;
        _pictureExportNextChunk = 0;
        _pictureExportBuffer = new StringBuilder(Math.Min(totalLength, 1024 * 1024));
        return true;
    }

    /// <summary>
    /// Runs the append picture export chunk operation.
    /// </summary>
    [JSInvokable]
    public bool AppendPictureExportChunk(string exportId, int chunkIndex, string chunk)
    {
        if (!IsCurrentPictureExport(exportId) || _pictureExportBuffer is null) return false;
        if (chunkIndex != _pictureExportNextChunk)
        {
            FailPictureExport(exportId, "The rendered picture chunks arrived out of order.");
            return false;
        }
        _pictureExportBuffer.Append(chunk);
        _pictureExportNextChunk++;
        return true;
    }

    /// <summary>
    /// Runs the complete picture export operation.
    /// </summary>
    [JSInvokable]
    public async Task CompletePictureExport(string exportId)
    {
        if (!IsCurrentPictureExport(exportId) || _pictureExportBuffer is null) return;
        if (_pictureExportNextChunk != _pictureExportExpectedChunks ||
            _pictureExportBuffer.Length != _pictureExportExpectedLength)
        {
            FailPictureExport(exportId, "The rendered picture export was incomplete.");
            return;
        }

        var dataUrl = _pictureExportBuffer.ToString();
        if (!dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
            !dataUrl.Contains(",", StringComparison.Ordinal))
        {
            FailPictureExport(exportId, "The browser returned an invalid rendered picture.");
            return;
        }

        var sourceDocument = _pictureExportSourceDocument ?? State.CloneDocument();
        var name = string.IsNullOrWhiteSpace(_pictureExportName) ? State.Document.Name : _pictureExportName!;
        var purpose = _pictureExportPurpose;
        ResetPictureExport();
        if (string.Equals(purpose, "ocr", StringComparison.Ordinal))
        {
            await RequestLocalGptOcrAsync(dataUrl);
            return;
        }
        await DisposePictureRuntimeAsync();
        await InvokeAsync(() => Saved.InvokeAsync(new PictureEditorResult(dataUrl, sourceDocument, name)));
    }

    /// <summary>
    /// Runs the fail picture export operation.
    /// </summary>
    [JSInvokable]
    public void FailPictureExport(string exportId, string? message)
    {
        if (!IsCurrentPictureExport(exportId)) return;
        ResetPictureExport();
        _error = string.IsNullOrWhiteSpace(message) ? "The browser could not render the picture." : message;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Determines whether current picture export.
    /// </summary>
    private bool IsCurrentPictureExport(string exportId) =>
        _pictureExportId is not null &&
        string.Equals(_pictureExportId, exportId, StringComparison.Ordinal);

    /// <summary>
    /// Runs the reset picture export operation.
    /// </summary>
    private void ResetPictureExport()
    {
        _pictureExportBuffer = null;
        _pictureExportId = null;
        _pictureExportSourceDocument = null;
        _pictureExportName = null;
        _pictureExportPurpose = "save";
        _pictureExportExpectedChunks = 0;
        _pictureExportNextChunk = 0;
        _pictureExportExpectedLength = 0;
    }

    /// <summary>
    /// Starts local gpt ocr async.
    /// </summary>
    private async Task StartLocalGptOcrAsync()
    {
        if (!CanUseLocalGptOcr || _module is null || _self is null || _pictureExportId is not null || _ocrBusy) return;
        var exportId = Guid.NewGuid().ToString("N");
        _pictureExportId = exportId;
        _pictureExportSourceDocument = State.CloneDocument();
        _pictureExportName = State.Document.Name;
        _pictureExportPurpose = "ocr";
        _pictureExportBuffer = null;
        _pictureExportExpectedChunks = 0;
        _pictureExportNextChunk = 0;
        _pictureExportExpectedLength = 0;
        _ocrStatus = "Rendering the current picture for LocalGPT OCR…";
        _error = null;
        try
        {
            await _module.InvokeVoidAsync("startPictureStudioDataUrlExport", _pictureExportSourceDocument, "image/jpeg", .9d, _self, exportId);
        }
        catch (Exception ex)
        {
            if (IsCurrentPictureExport(exportId)) ResetPictureExport();
            Logger.LogError(ex, "Could not render the Picture Studio canvas for LocalGPT OCR.");
            _error = ex.Message;
        }
    }

    /// <summary>
    /// Runs the request local gpt ocr async operation.
    /// </summary>
    private async Task RequestLocalGptOcrAsync(string dataUrl)
    {
        _ocrBusy = true;
        try
        {
            _ocrStatus = "Waiting for LocalGPT approval and local OCR…";
            await InvokeAsync(StateHasChanged);
            var envelope = new OrganicWireEnvelope
            {
                MessageType = OrganicWireMessageType.Invoke,
                TargetPeerId = LocalGptConnection.State.PeerId,
                CapabilityKey = "localgpt.vision.ocr",
                Controller = "OneWire",
                Method = "POST",
                Route = "/api/onewire/capabilities/localgpt.vision.ocr",
                Organs = ["eyes"],
                Skills = ["ocr", "vision", "text-recognition"],
                UserConfirmed = true,
                RequiresHumanInteractionOnTargetSystem = true,
                Properties = new Dictionary<string, JsonElement>
                {
                    ["Parameters"] = JsonSerializer.SerializeToElement(new
                    {
                        imageDataUrl = dataUrl,
                        modelName = "deepseek-ocr",
                        prompt = "Recognize all visible text in this PublisherStudio picture. Preserve reading order and line breaks. Return only recognized text and mark uncertainty with [?].",
                        maximumOutputTokens = 1600
                    })
                }
            };
            envelope.NormalizeInteractionKind();
            var correlationId = await LocalGptConnection.SendEnvelopeAsync(envelope);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(6);
            while (true)
            {
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero) throw new TimeoutException("LocalGPT OCR did not finish within six minutes.");
                var response = await LocalGptConnection.WaitForResultAsync(correlationId, remaining);
                if (response.MessageType == OrganicWireMessageType.ApprovalRequired)
                {
                    _ocrStatus = "Approve the OCR request in the LocalGPT frontend; PublisherStudio will keep waiting for the same request.";
                    await InvokeAsync(StateHasChanged);
                    continue;
                }
                if (response.MessageType == OrganicWireMessageType.Error)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Error) ? "LocalGPT rejected the OCR request." : response.Error);
                if (response.MessageType != OrganicWireMessageType.WorkResult) continue;
                var resultJson = ReadWireString(response, "ResultJson");
                var result = JsonSerializer.Deserialize<PictureOcrResult>(resultJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new JsonException("The LocalGPT OCR result was empty.");
                if (string.IsNullOrWhiteSpace(result.Text)) throw new InvalidOperationException("LocalGPT returned no recognized text.");
                _ocrText = result.Text.Trim();
                _ocrStatus = $"OCR completed with {result.ModelName}. Review the text before inserting it as a layer.";
                Notifications.Success(_ocrStatus, "Picture Studio OCR", nameof(PictureEditor));
                break;
            }
        }
        catch (OperationCanceledException)
        {
            _ocrStatus = "LocalGPT OCR was cancelled.";
            Logger.LogInformation("Picture Studio LocalGPT OCR was cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Picture Studio LocalGPT OCR failed.");
            _ocrStatus = string.Empty;
            _error = ex.Message;
            Notifications.Error(ex.Message, "Picture Studio OCR failed", nameof(PictureEditor));
        }
        finally
        {
            _ocrBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Runs the insert ocr text layer operation.
    /// </summary>
    private void InsertOcrTextLayer()
    {
        if (string.IsNullOrWhiteSpace(_ocrText)) return;
        var layer = State.AddText();
        layer.Name = "OCR text";
        layer.Text = _ocrText;
        _notice = "Recognized text was inserted as an editable Picture Studio text layer.";
        Notifications.Success(_notice, "Picture Studio OCR", nameof(PictureEditor));
    }

    /// <summary>
    /// Runs the clear ocr text operation.
    /// </summary>
    private void ClearOcrText()
    {
        _ocrText = string.Empty;
        _ocrStatus = string.Empty;
    }

    /// <summary>
    /// Reads wire string.
    /// </summary>
    private string ReadWireString(OrganicWireEnvelope envelope, string key)
    {
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(key, out var value)) return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
    }

    /// <summary>
    /// Runs the download png operation.
    /// </summary>
    private async Task DownloadPng() => await Download("image/png", "png", 1d);
    /// <summary>
    /// Runs the download jpeg operation.
    /// </summary>
    private async Task DownloadJpeg() => await Download("image/jpeg", "jpg", .92d);
    /// <summary>
    /// Runs the download SVG operation.
    /// </summary>
    private async Task DownloadSvg()
    {
        if (_module is null) return;
        var fileName = $"{Files.SafeFileName(State.Document.Name)}.svg";
        await _module.InvokeVoidAsync("downloadPictureStudioSvg", State.Document, fileName);
    }

    /// <summary>
    /// Runs the download operation.
    /// </summary>
    private async Task Download(string mimeType, string extension, double quality)
    {
        if (_module is null) return;
        try
        {
            var name = Files.SafeFileName(State.Document.Name) + "." + extension;
            await _module.InvokeVoidAsync("downloadPictureStudio", State.Document, name, mimeType, quality);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>
    /// Determines whether cel.
    /// </summary>
    private async Task Cancel()
    {
        await CancelPictureInteractionAsync();
        await DisposePictureRuntimeAsync();
        await Cancelled.InvokeAsync();
    }

    /// <summary>
    /// Runs the select tool operation.
    /// </summary>
    private void SelectTool() => SetDrawTool(PictureDrawTool.Select);
    /// <summary>
    /// Runs the brush tool operation.
    /// </summary>
    private void BrushTool() => SetDrawTool(PictureDrawTool.Brush);
    /// <summary>
    /// Runs the pencil tool operation.
    /// </summary>
    private void PencilTool() => SetDrawTool(PictureDrawTool.Pencil);
    /// <summary>
    /// Runs the spray tool operation.
    /// </summary>
    private void SprayTool() => SetDrawTool(PictureDrawTool.Spray);
    /// <summary>
    /// Runs the toothbrush tool operation.
    /// </summary>
    private void ToothbrushTool() => SetDrawTool(PictureDrawTool.Toothbrush);
    /// <summary>
    /// Runs the square tool operation.
    /// </summary>
    private void SquareTool() => SetDrawTool(PictureDrawTool.Square);
    /// <summary>
    /// Runs the rectangle tool operation.
    /// </summary>
    private void RectangleTool() => SetDrawTool(PictureDrawTool.Rectangle);
    /// <summary>
    /// Runs the ellipse tool operation.
    /// </summary>
    private void EllipseTool() => SetDrawTool(PictureDrawTool.Ellipse);
    /// <summary>
    /// Runs the arrow tool operation.
    /// </summary>
    private void ArrowTool() => SetDrawTool(PictureDrawTool.Arrow);
    /// <summary>
    /// Runs the line tool operation.
    /// </summary>
    private void LineTool() => SetDrawTool(PictureDrawTool.Line);
    /// <summary>
    /// Runs the path tool operation.
    /// </summary>
    private void PathTool() => SetDrawTool(PictureDrawTool.Path);
    /// <summary>
    /// Runs the eraser tool operation.
    /// </summary>
    private void EraserTool() => SetDrawTool(PictureDrawTool.Eraser);
    /// <summary>
    /// Runs the eyedropper tool operation.
    /// </summary>
    private void EyedropperTool() => SetDrawTool(PictureDrawTool.Eyedropper);
    /// <summary>
    /// Runs the rectangle select tool operation.
    /// </summary>
    private void RectangleSelectTool() => SetDrawTool(PictureDrawTool.RectangleSelect);
    /// <summary>
    /// Runs the ellipse select tool operation.
    /// </summary>
    private void EllipseSelectTool() => SetDrawTool(PictureDrawTool.EllipseSelect);
    /// <summary>
    /// Runs the free select tool operation.
    /// </summary>
    private void FreeSelectTool() => SetDrawTool(PictureDrawTool.FreeSelect);
    /// <summary>
    /// Runs the magnetic select tool operation.
    /// </summary>
    private void MagneticSelectTool() => SetDrawTool(PictureDrawTool.MagneticSelect);
    /// <summary>
    /// Runs the polygon select tool operation.
    /// </summary>
    private void PolygonSelectTool() => SetDrawTool(PictureDrawTool.PolygonSelect);
    /// <summary>
    /// Runs the fill solid tool operation.
    /// </summary>
    private void FillSolidTool() => SetDrawTool(PictureDrawTool.FillSolid);
    /// <summary>
    /// Runs the fill gradient tool operation.
    /// </summary>
    private void FillGradientTool() => SetDrawTool(PictureDrawTool.FillGradient);
    /// <summary>
    /// Runs the clear area selection operation.
    /// </summary>
    private async Task ClearAreaSelection()
    {
        if (_module is not null) await _module.InvokeVoidAsync("clearPictureStudioAreaSelection", RuntimePolicy.PictureStudio.CanvasId);
        _renderRequested = true;
    }

    /// <summary>
    /// Reads area selection async.
    /// </summary>
    private async Task<PictureAreaSelection?> ReadAreaSelectionAsync()
    {
        if (_module is null || State.SelectedLayer is null) return null;
        try
        {
            return await _module.InvokeAsync<PictureAreaSelection?>("getPictureStudioAreaSelection", RuntimePolicy.PictureStudio.CanvasId);
        }
        catch (JSDisconnectedException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JSException) { return null; }
    }

    /// <summary>
    /// Runs the selection polygon operation.
    /// </summary>
    private List<PicturePoint> SelectionPolygon(PictureAreaSelection selection)
    {
        var points = selection.Points
            .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
            .Take(2048)
            .Select(point => new PicturePoint { X = point.X, Y = point.Y })
            .ToList();
        if (points.Count < 2) return [];

        var kind = selection.Kind?.Trim().ToLowerInvariant();
        if (kind == "rectangle")
        {
            var first = points[0];
            var last = points[^1];
            var left = Math.Min(first.X, last.X);
            var top = Math.Min(first.Y, last.Y);
            var right = Math.Max(first.X, last.X);
            var bottom = Math.Max(first.Y, last.Y);
            return
            [
                new PicturePoint { X = left, Y = top },
                new PicturePoint { X = right, Y = top },
                new PicturePoint { X = right, Y = bottom },
                new PicturePoint { X = left, Y = bottom }
            ];
        }

        if (kind == "ellipse")
        {
            var first = points[0];
            var last = points[^1];
            var centerX = (first.X + last.X) / 2;
            var centerY = (first.Y + last.Y) / 2;
            var radiusX = Math.Abs(last.X - first.X) / 2;
            var radiusY = Math.Abs(last.Y - first.Y) / 2;
            if (radiusX < .5 || radiusY < .5) return [];
            return Enumerable.Range(0, 48)
                .Select(index => index * Math.PI * 2 / 48)
                .Select(angle => new PicturePoint
                {
                    X = centerX + Math.Cos(angle) * radiusX,
                    Y = centerY + Math.Sin(angle) * radiusY
                })
                .ToList();
        }

        while (points.Count > 1 && Distance(points[0], points[^1]) < .25) points.RemoveAt(points.Count - 1);
        return points.Count >= 3 ? points : [];
    }

    /// <summary>
    /// Applies area clip async.
    /// </summary>
    private async Task<bool> ApplyAreaClipAsync(bool inverted, bool quietWhenMissing = false)
    {
        var selection = await ReadAreaSelectionAsync();
        var polygon = selection is null ? [] : SelectionPolygon(selection);
        if (polygon.Count < 3)
        {
            if (!quietWhenMissing) _notice = "Create an area selection first. Polygon select accepts any number of angled lines.";
            return false;
        }
        if (!State.ApplySelectedClip(polygon, inverted))
        {
            if (!quietWhenMissing) _notice = "Select an unlocked layer before applying the area cut.";
            return false;
        }
        _notice = inverted ? "The selected area was cut from the layer non-destructively." : "The layer now keeps only the selected area.";
        await ClearAreaSelection();
        SetDrawTool(PictureDrawTool.Select);
        return true;
    }

    /// <summary>
    /// Runs the keep selected area operation.
    /// </summary>
    private Task KeepSelectedArea() => ApplyAreaClipAsync(inverted: false);
    /// <summary>
    /// Runs the cut selected area operation.
    /// </summary>
    private Task CutSelectedArea() => ApplyAreaClipAsync(inverted: true);

    /// <summary>
    /// Runs the copy area selection to clipboard async operation.
    /// </summary>
    private async Task<bool> CopyAreaSelectionToClipboardAsync()
    {
        var selection = await ReadAreaSelectionAsync();
        var polygon = selection is null ? [] : SelectionPolygon(selection);
        if (polygon.Count < 3 || !State.CopySelectedRegion(polygon)) return false;
        _notice = "Selected picture region copied. Paste inserts it as an independently editable clipped layer.";
        return true;
    }

    /// <summary>
    /// Runs the copy selected area operation.
    /// </summary>
    private async Task CopySelectedArea()
    {
        if (!await CopyAreaSelectionToClipboardAsync())
            _notice = "Create an area selection on a layer before copying a region.";
    }

    /// <summary>
    /// Runs the copy selected area as layer operation.
    /// </summary>
    private async Task CopySelectedAreaAsLayer()
    {
        if (!await CopyAreaSelectionToClipboardAsync())
        {
            _notice = "Create an area selection on a layer before copying a region.";
            return;
        }
        State.Paste();
        await ClearAreaSelection();
        SetDrawTool(PictureDrawTool.Select);
        _notice = "The selected region was inserted as a new clipped layer.";
    }

    /// <summary>
    /// Runs the clear layer cut operation.
    /// </summary>
    private void ClearLayerCut()
    {
        if (State.ClearSelectedClip()) _notice = "The layer cut was cleared.";
    }

    /// <summary>
    /// Runs the distance operation.
    /// </summary>
    private double Distance(PicturePoint first, PicturePoint second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }
    /// <summary>
    /// Sets draw tool.
    /// </summary>
    private void SetDrawTool(PictureDrawTool tool)
    {
        _ = CancelPictureInteractionAsync();
        _drawTool = tool;
        _renderRequested = true;
        StateHasChanged();
    }

    /// <summary>
    /// Determines whether cel picture interaction async.
    /// </summary>
    private async Task CancelPictureInteractionAsync()
    {
        if (_module is null) return;
        try
        {
            await _module.InvokeVoidAsync("cancelPictureStudioInteraction", RuntimePolicy.PictureStudio.CanvasId);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (JSException) { }
    }

    /// <summary>
    /// Runs the dispose picture runtime async operation.
    /// </summary>
    private async Task DisposePictureRuntimeAsync()
    {
        if (_module is null || !_initialized) return;
        try
        {
            await _module.InvokeVoidAsync("disposePictureStudio", RuntimePolicy.PictureStudio.CanvasId);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (JSException) { }
        finally
        {
            _initialized = false;
        }
    }
    /// <summary>
    /// Runs the tool text operation.
    /// </summary>
    private string ToolText(PictureDrawTool tool, string text) => _drawTool == tool ? $"✓ {text}" : text;
    /// <summary>
    /// Determines whether draw width.
    /// </summary>
    private bool IsDrawWidth(double value) => Math.Abs(_drawWidth - value) < .001;
    /// <summary>
    /// Runs the draw width text operation.
    /// </summary>
    private string DrawWidthText(double value) => IsDrawWidth(value) ? $"✓ {value:0.##} px" : $"{value:0.##} px";
    /// <summary>
    /// Runs the draw width button class operation.
    /// </summary>
    private string DrawWidthButtonClass(double value) => IsDrawWidth(value) ? "selected" : string.Empty;
    /// <summary>
    /// Runs the change draw color operation.
    /// </summary>
    private void ChangeDrawColor(string value) { if (!string.IsNullOrWhiteSpace(value)) _drawColor = value; _renderRequested = true; }
    /// <summary>
    /// Runs the change draw secondary color operation.
    /// </summary>
    private void ChangeDrawSecondaryColor(string value) { if (!string.IsNullOrWhiteSpace(value)) _drawSecondaryColor = value; _renderRequested = true; }
    /// <summary>
    /// Sets draw width.
    /// </summary>
    private void SetDrawWidth(double value)
    {
        _drawWidth = Math.Clamp(value, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
        _renderRequested = true;
    }
    /// <summary>
    /// Runs the width to slider operation.
    /// </summary>
    private double WidthToSlider(double width)
    {
        var clamped = Math.Clamp(width, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
        return Math.Log(clamped / RuntimePolicy.PictureStudio.MinimumDrawWidth) / Math.Log(RuntimePolicy.PictureStudio.MaximumDrawWidth / RuntimePolicy.PictureStudio.MinimumDrawWidth) * 100;
    }
    /// <summary>
    /// Runs the slider to width operation.
    /// </summary>
    private double SliderToWidth(double slider)
    {
        var normalized = Math.Clamp(slider, 0, 100) / 100;
        var width = RuntimePolicy.PictureStudio.MinimumDrawWidth * Math.Pow(RuntimePolicy.PictureStudio.MaximumDrawWidth / RuntimePolicy.PictureStudio.MinimumDrawWidth, normalized);
        var step = width switch
        {
            < 4 => .25,
            < 16 => .5,
            < 64 => 1,
            < 128 => 2,
            _ => 4
        };
        return Math.Round(width / step) * step;
    }
    /// <summary>
    /// Runs the draw width1 operation.
    /// </summary>
    private void DrawWidth1() => SetDrawWidth(1);
    /// <summary>
    /// Runs the draw width3 operation.
    /// </summary>
    private void DrawWidth3() => SetDrawWidth(3);
    /// <summary>
    /// Runs the draw width8 operation.
    /// </summary>
    private void DrawWidth8() => SetDrawWidth(8);
    /// <summary>
    /// Runs the draw width16 operation.
    /// </summary>
    private void DrawWidth16() => SetDrawWidth(16);
    /// <summary>
    /// Runs the draw width32 operation.
    /// </summary>
    private void DrawWidth32() => SetDrawWidth(32);
    /// <summary>
    /// Runs the toggle grid ribbon operation.
    /// </summary>
    private void ToggleGridRibbon() => State.SetGrid(!State.Document.GridVisible);
    /// <summary>
    /// Runs the toggle snap ribbon operation.
    /// </summary>
    private void ToggleSnapRibbon() => State.SetSnap(!State.Document.SnapToGrid);
    private string GridText => State.Document.GridVisible ? "✓ Grid" : "Grid";
    private string SnapText => State.Document.SnapToGrid ? "✓ Snap" : "Snap";
    /// <summary>
    /// Runs the make render clouds operation.
    /// </summary>
    private void MakeRenderClouds() => WithRender(layer => layer.RenderKind = PictureRenderKind.Clouds);
    /// <summary>
    /// Runs the make render noise operation.
    /// </summary>
    private void MakeRenderNoise() => WithRender(layer => layer.RenderKind = PictureRenderKind.Noise);
    /// <summary>
    /// Runs the make render stripes operation.
    /// </summary>
    private void MakeRenderStripes() => WithRender(layer => layer.RenderKind = PictureRenderKind.Stripes);
    /// <summary>
    /// Runs the make render vignette operation.
    /// </summary>
    private void MakeRenderVignette() => WithRender(layer => layer.RenderKind = PictureRenderKind.Vignette);
    /// <summary>
    /// Runs the make render bloom operation.
    /// </summary>
    private void MakeRenderBloom() => WithRender(layer => layer.RenderKind = PictureRenderKind.Bloom);
    /// <summary>
    /// Runs the make render neon operation.
    /// </summary>
    private void MakeRenderNeon() => WithRender(layer => layer.RenderKind = PictureRenderKind.Neon);
    /// <summary>
    /// Runs the make render lens flare operation.
    /// </summary>
    private void MakeRenderLensFlare() => WithRender(layer => layer.RenderKind = PictureRenderKind.LensFlare);
    /// <summary>
    /// Runs the make render grain noise operation.
    /// </summary>
    private void MakeRenderGrainNoise() => WithRender(layer => layer.RenderKind = PictureRenderKind.GrainNoise);
    /// <summary>
    /// Runs the make render motion blur operation.
    /// </summary>
    private void MakeRenderMotionBlur() => WithRender(layer => layer.RenderKind = PictureRenderKind.MotionBlur);
    /// <summary>
    /// Runs the make render wind operation.
    /// </summary>
    private void MakeRenderWind() => WithRender(layer => layer.RenderKind = PictureRenderKind.Wind);
    /// <summary>
    /// Runs the make render ocean waves operation.
    /// </summary>
    private void MakeRenderOceanWaves() => WithRender(layer => layer.RenderKind = PictureRenderKind.OceanWaves);
    /// <summary>
    /// Runs the raster contain operation.
    /// </summary>
    private void RasterContain() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Contain);
    /// <summary>
    /// Runs the raster cover operation.
    /// </summary>
    private void RasterCover() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Cover);
    /// <summary>
    /// Runs the raster stretch operation.
    /// </summary>
    private void RasterStretch() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Stretch);
    /// <summary>
    /// Runs the raster flip horizontal operation.
    /// </summary>
    private void RasterFlipHorizontal() => WithRaster(layer => layer.FlipHorizontal = !layer.FlipHorizontal);
    /// <summary>
    /// Runs the raster flip vertical operation.
    /// </summary>
    private void RasterFlipVertical() => WithRaster(layer => layer.FlipVertical = !layer.FlipVertical);
    /// <summary>
    /// Runs the raster rotate left operation.
    /// </summary>
    private void RasterRotateLeft() => WithRaster(layer => layer.Rotation = (layer.Rotation - 90 + 360) % 360);
    /// <summary>
    /// Runs the raster rotate right operation.
    /// </summary>
    private void RasterRotateRight() => WithRaster(layer => layer.Rotation = (layer.Rotation + 90) % 360);
    /// <summary>
    /// Runs the raster reset rotation operation.
    /// </summary>
    private void RasterResetRotation() => WithRaster(layer => layer.Rotation = 0);
    /// <summary>
    /// Runs the raster no tint operation.
    /// </summary>
    private void RasterNoTint() => WithRaster(layer => layer.TintOpacity = 0);
    /// <summary>
    /// Runs the raster blue tint operation.
    /// </summary>
    private void RasterBlueTint() => WithRaster(layer => { layer.TintColor = "#2563eb"; layer.TintOpacity = .28; });
    /// <summary>
    /// Runs the raster warm tint operation.
    /// </summary>
    private void RasterWarmTint() => WithRaster(layer => { layer.TintColor = "#f97316"; layer.TintOpacity = .24; });
    /// <summary>
    /// Runs the soften light operation.
    /// </summary>
    private void SoftenLight() => State.UpdateSelected(layer => layer.Blur = 2);
    /// <summary>
    /// Runs the soften medium operation.
    /// </summary>
    private void SoftenMedium() => State.UpdateSelected(layer => layer.Blur = 6);
    /// <summary>
    /// Removes softening.
    /// </summary>
    private void RemoveSoftening() => State.UpdateSelected(layer => layer.Blur = 0);
    /// <summary>
    /// Runs the brighten operation.
    /// </summary>
    private void Brighten() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness + .1, 0, 3));
    /// <summary>
    /// Runs the darken operation.
    /// </summary>
    private void Darken() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness - .1, 0, 3));
    /// <summary>
    /// Runs the more contrast operation.
    /// </summary>
    private void MoreContrast() => State.UpdateSelected(layer => layer.Contrast = Math.Clamp(layer.Contrast + .1, 0, 3));
    /// <summary>
    /// Runs the more saturation operation.
    /// </summary>
    private void MoreSaturation() => State.UpdateSelected(layer => layer.Saturation = Math.Clamp(layer.Saturation + .1, 0, 3));
    /// <summary>
    /// Runs the toggle grayscale preset operation.
    /// </summary>
    private void ToggleGrayscalePreset() => State.UpdateSelected(layer => layer.Grayscale = layer.Grayscale > .5 ? 0 : 1);
    /// <summary>
    /// Runs the toggle sepia preset operation.
    /// </summary>
    private void ToggleSepiaPreset() => State.UpdateSelected(layer => layer.Sepia = layer.Sepia > .5 ? 0 : 1);
    /// <summary>
    /// Runs the toggle invert preset operation.
    /// </summary>
    private void ToggleInvertPreset() => State.UpdateSelected(layer => layer.Invert = layer.Invert > .5 ? 0 : 1);
    /// <summary>
    /// Applies bloom effect.
    /// </summary>
    private void ApplyBloomEffect() => State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .18, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .06, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .12, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, 4), 0, 50);
        layer.Opacity = Math.Clamp(layer.Opacity, .82, 1);
        layer.BlendMode = PictureBlendMode.Screen;
    });
    /// <summary>
    /// Applies neon effect.
    /// </summary>
    private void ApplyNeonEffect() => State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .22, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .25, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .6, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, 1.5), 0, 50);
        layer.BlendMode = PictureBlendMode.Screen;
    });
    /// <summary>
    /// Applies lens flare effect.
    /// </summary>
    private void ApplyLensFlareEffect() => State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .28, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .12, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .18, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, .75), 0, 50);
        layer.Opacity = Math.Clamp(layer.Opacity, .9, 1);
        layer.BlendMode = PictureBlendMode.Screen;
    });

    /// <summary>
    /// Runs the shape rectangle operation.
    /// </summary>
    private void ShapeRectangle() => WithShape(layer => layer.Shape = PictureShapeKind.Rectangle);
    /// <summary>
    /// Runs the shape rounded rectangle operation.
    /// </summary>
    private void ShapeRoundedRectangle() => WithShape(layer => layer.Shape = PictureShapeKind.RoundedRectangle);
    /// <summary>
    /// Runs the shape ellipse operation.
    /// </summary>
    private void ShapeEllipse() => WithShape(layer => layer.Shape = PictureShapeKind.Ellipse);
    /// <summary>
    /// Runs the shape arrow operation.
    /// </summary>
    private void ShapeArrow() => WithShape(layer => layer.Shape = PictureShapeKind.Arrow);
    /// <summary>
    /// Runs the shape line operation.
    /// </summary>
    private void ShapeLine() => WithShape(layer => layer.Shape = PictureShapeKind.Line);
    /// <summary>
    /// Runs the shape path operation.
    /// </summary>
    private void ShapePath() => WithShape(layer => layer.Shape = PictureShapeKind.Path);
    /// <summary>
    /// Runs the fill solid operation.
    /// </summary>
    private void FillSolid() => WithFill(layer => layer.FillKind = PictureFillKind.Solid);
    /// <summary>
    /// Runs the fill linear gradient operation.
    /// </summary>
    private void FillLinearGradient() => WithFill(layer => layer.FillKind = PictureFillKind.LinearGradient);
    /// <summary>
    /// Runs the fill radial gradient operation.
    /// </summary>
    private void FillRadialGradient() => WithFill(layer => layer.FillKind = PictureFillKind.RadialGradient);
    /// <summary>
    /// Sets picture text font.
    /// </summary>
    private void SetPictureTextFont(string font) => WithText(layer => layer.FontFamily = font);
    /// <summary>
    /// Sets picture text size.
    /// </summary>
    private void SetPictureTextSize(double value) => WithText(layer => layer.FontSizePx = value);
    /// <summary>
    /// Runs the text size24 operation.
    /// </summary>
    private void TextSize24() => SetPictureTextSize(24);
    /// <summary>
    /// Runs the text size48 operation.
    /// </summary>
    private void TextSize48() => SetPictureTextSize(48);
    /// <summary>
    /// Runs the text size72 operation.
    /// </summary>
    private void TextSize72() => SetPictureTextSize(72);
    /// <summary>
    /// Runs the text size120 operation.
    /// </summary>
    private void TextSize120() => SetPictureTextSize(120);
    /// <summary>
    /// Runs the text size180 operation.
    /// </summary>
    private void TextSize180() => SetPictureTextSize(180);
    /// <summary>
    /// Runs the toggle picture text bold operation.
    /// </summary>
    private void TogglePictureTextBold() => WithText(layer => layer.Bold = !layer.Bold);
    /// <summary>
    /// Runs the toggle picture text italic operation.
    /// </summary>
    private void TogglePictureTextItalic() => WithText(layer => layer.Italic = !layer.Italic);
    /// <summary>
    /// Runs the toggle picture text shadow operation.
    /// </summary>
    private void TogglePictureTextShadow() => WithText(layer => layer.ShadowEnabled = !layer.ShadowEnabled);
    /// <summary>
    /// Runs the text align left operation.
    /// </summary>
    private void TextAlignLeft() => WithText(layer => layer.Alignment = PictureTextAlignment.Left);
    /// <summary>
    /// Runs the text align center operation.
    /// </summary>
    private void TextAlignCenter() => WithText(layer => layer.Alignment = PictureTextAlignment.Center);
    /// <summary>
    /// Runs the text align right operation.
    /// </summary>
    private void TextAlignRight() => WithText(layer => layer.Alignment = PictureTextAlignment.Right);
    /// <summary>
    /// Runs the text color blue operation.
    /// </summary>
    private void TextColorBlue() => WithText(layer => layer.FillColor = "#17365d");
    /// <summary>
    /// Runs the text color black operation.
    /// </summary>
    private void TextColorBlack() => WithText(layer => layer.FillColor = "#000000");
    /// <summary>
    /// Runs the text color white operation.
    /// </summary>
    private void TextColorWhite() => WithText(layer => layer.FillColor = "#ffffff");
    /// <summary>
    /// Runs the text color red operation.
    /// </summary>
    private void TextColorRed() => WithText(layer => layer.FillColor = "#dc2626");
    /// <summary>
    /// Runs the text outline none operation.
    /// </summary>
    private void TextOutlineNone() => WithText(layer => { layer.OutlineColor = "transparent"; layer.OutlineWidthPx = 0; });
    /// <summary>
    /// Runs the text outline thin operation.
    /// </summary>
    private void TextOutlineThin() => WithText(layer => { layer.OutlineColor = "#111827"; layer.OutlineWidthPx = 1; });
    /// <summary>
    /// Runs the text outline thick operation.
    /// </summary>
    private void TextOutlineThick() => WithText(layer => { layer.OutlineColor = "#ffffff"; layer.OutlineWidthPx = 4; });
    /// <summary>
    /// Runs the shape fill solid operation.
    /// </summary>
    private void ShapeFillSolid() => WithShape(layer => layer.FillKind = PictureFillKind.Solid);
    /// <summary>
    /// Runs the shape fill linear operation.
    /// </summary>
    private void ShapeFillLinear() => WithShape(layer => layer.FillKind = PictureFillKind.LinearGradient);
    /// <summary>
    /// Runs the shape fill radial operation.
    /// </summary>
    private void ShapeFillRadial() => WithShape(layer => layer.FillKind = PictureFillKind.RadialGradient);
    /// <summary>
    /// Sets shape colors.
    /// </summary>
    private void SetShapeColors(string first, string second, string stroke) => WithShape(layer => { layer.FillColor = first; layer.SecondaryFillColor = second; layer.StrokeColor = stroke; });
    /// <summary>
    /// Runs the shape colors blue operation.
    /// </summary>
    private void ShapeColorsBlue() => SetShapeColors("#60a5fa", "#dbeafe", "#1d4ed8");
    /// <summary>
    /// Runs the shape colors green operation.
    /// </summary>
    private void ShapeColorsGreen() => SetShapeColors("#4ade80", "#dcfce7", "#15803d");
    /// <summary>
    /// Runs the shape colors orange operation.
    /// </summary>
    private void ShapeColorsOrange() => SetShapeColors("#fb923c", "#ffedd5", "#c2410c");
    /// <summary>
    /// Runs the shape colors mono operation.
    /// </summary>
    private void ShapeColorsMono() => SetShapeColors("#111827", "#ffffff", "#000000");
    /// <summary>
    /// Sets shape stroke.
    /// </summary>
    private void SetShapeStroke(double width) => WithShape(layer => layer.StrokeWidthPx = width);
    /// <summary>
    /// Runs the shape stroke0 operation.
    /// </summary>
    private void ShapeStroke0() => SetShapeStroke(0);
    /// <summary>
    /// Runs the shape stroke1 operation.
    /// </summary>
    private void ShapeStroke1() => SetShapeStroke(1);
    /// <summary>
    /// Runs the shape stroke3 operation.
    /// </summary>
    private void ShapeStroke3() => SetShapeStroke(3);
    /// <summary>
    /// Runs the shape stroke8 operation.
    /// </summary>
    private void ShapeStroke8() => SetShapeStroke(8);
    /// <summary>
    /// Sets fill colors.
    /// </summary>
    private void SetFillColors(string first, string second) => WithFill(layer => { layer.PrimaryColor = first; layer.SecondaryColor = second; });
    /// <summary>
    /// Runs the fill colors blue operation.
    /// </summary>
    private void FillColorsBlue() => SetFillColors("#dbeafe", "#6366f1");
    /// <summary>
    /// Runs the fill colors green operation.
    /// </summary>
    private void FillColorsGreen() => SetFillColors("#dcfce7", "#16a34a");
    /// <summary>
    /// Runs the fill colors sunset operation.
    /// </summary>
    private void FillColorsSunset() => SetFillColors("#fde68a", "#f97316");
    /// <summary>
    /// Runs the fill colors mono operation.
    /// </summary>
    private void FillColorsMono() => SetFillColors("#ffffff", "#111827");
    /// <summary>
    /// Sets fill angle.
    /// </summary>
    private void SetFillAngle(double value) => WithFill(layer => layer.AngleDegrees = value);
    /// <summary>
    /// Runs the fill angle0 operation.
    /// </summary>
    private void FillAngle0() => SetFillAngle(0);
    /// <summary>
    /// Runs the fill angle45 operation.
    /// </summary>
    private void FillAngle45() => SetFillAngle(45);
    /// <summary>
    /// Runs the fill angle90 operation.
    /// </summary>
    private void FillAngle90() => SetFillAngle(90);
    /// <summary>
    /// Runs the fill angle180 operation.
    /// </summary>
    private void FillAngle180() => SetFillAngle(180);
    /// <summary>
    /// Runs the fill angle270 operation.
    /// </summary>
    private void FillAngle270() => SetFillAngle(270);
    /// <summary>
    /// Sets layer opacity.
    /// </summary>
    private void SetLayerOpacity(double value) => State.UpdateSelected(layer => layer.Opacity = value);
    /// <summary>
    /// Runs the layer opacity100 operation.
    /// </summary>
    private void LayerOpacity100() => SetLayerOpacity(1);
    /// <summary>
    /// Runs the layer opacity75 operation.
    /// </summary>
    private void LayerOpacity75() => SetLayerOpacity(.75);
    /// <summary>
    /// Runs the layer opacity50 operation.
    /// </summary>
    private void LayerOpacity50() => SetLayerOpacity(.5);
    /// <summary>
    /// Runs the layer opacity25 operation.
    /// </summary>
    private void LayerOpacity25() => SetLayerOpacity(.25);
    /// <summary>
    /// Runs the toggle selected lock menu operation.
    /// </summary>
    private void ToggleSelectedLockMenu()
    {
        if (State.SelectedLayer is PictureLayer layer) State.ToggleLock(layer.Id);
    }
    /// <summary>
    /// Runs the toggle selected visibility menu operation.
    /// </summary>
    private void ToggleSelectedVisibilityMenu()
    {
        if (State.SelectedLayer is PictureLayer layer) State.ToggleVisibility(layer.Id);
    }
    /// <summary>
    /// Runs the checked text operation.
    /// </summary>
    private string CheckedText(bool selected, string text) => selected ? $"✓ {text}" : text;

    /// <summary>
    /// Runs the change document name operation.
    /// </summary>
    private void ChangeDocumentName(ChangeEventArgs args) => State.SetDocumentName(Text(args));
    /// <summary>
    /// Runs the change canvas width operation.
    /// </summary>
    private void ChangeCanvasWidth(ChangeEventArgs args) => State.SetDocumentSize(Int(args, State.Document.WidthPx), State.Document.HeightPx);
    /// <summary>
    /// Runs the change canvas height operation.
    /// </summary>
    private void ChangeCanvasHeight(ChangeEventArgs args) => State.SetDocumentSize(State.Document.WidthPx, Int(args, State.Document.HeightPx));
    /// <summary>
    /// Runs the change background preset operation.
    /// </summary>
    private void ChangeBackgroundPreset(ChangeEventArgs args) => State.SetBackground(Text(args));
    /// <summary>
    /// Runs the change canvas color operation.
    /// </summary>
    private void ChangeCanvasColor(ChangeEventArgs args) => State.SetBackground(Text(args));
    /// <summary>
    /// Runs the change grid spacing operation.
    /// </summary>
    private void ChangeGridSpacing(ChangeEventArgs args) => State.SetGridSpacing(Int(args, State.Document.GridSpacingPx));
    /// <summary>
    /// Runs the toggle grid operation.
    /// </summary>
    private void ToggleGrid(ChangeEventArgs args) => State.SetGrid(Bool(args));
    /// <summary>
    /// Runs the toggle snap operation.
    /// </summary>
    private void ToggleSnap(ChangeEventArgs args) => State.SetSnap(Bool(args));
    /// <summary>
    /// Runs the change zoom operation.
    /// </summary>
    private void ChangeZoom(ChangeEventArgs args) => State.SetZoom(Number(args, State.Document.Zoom));
    /// <summary>
    /// Runs the change draw tool operation.
    /// </summary>
    private void ChangeDrawTool(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureDrawTool>(Text(args), true, out var tool)) SetDrawTool(tool);
    }
    /// <summary>
    /// Runs the change draw color input operation.
    /// </summary>
    private void ChangeDrawColorInput(ChangeEventArgs args) => ChangeDrawColor(Text(args));
    /// <summary>
    /// Runs the change draw secondary color input operation.
    /// </summary>
    private void ChangeDrawSecondaryColorInput(ChangeEventArgs args) => ChangeDrawSecondaryColor(Text(args));
    /// <summary>
    /// Runs the change draw width operation.
    /// </summary>
    private void ChangeDrawWidth(ChangeEventArgs args) => SetDrawWidth(Number(args, _drawWidth));
    /// <summary>
    /// Runs the change draw width slider operation.
    /// </summary>
    private void ChangeDrawWidthSlider(ChangeEventArgs args) => SetDrawWidth(SliderToWidth(Number(args, BrushWidthSliderValue)));
    /// <summary>
    /// Runs the change draw opacity operation.
    /// </summary>
    private void ChangeDrawOpacity(ChangeEventArgs args) { _drawOpacity = Math.Clamp(Number(args, _drawOpacity), 0, 1); _renderRequested = true; }
    /// <summary>
    /// Runs the change draw hardness operation.
    /// </summary>
    private void ChangeDrawHardness(ChangeEventArgs args) { _drawHardness = Math.Clamp(Number(args, _drawHardness), 0, 1); _renderRequested = true; }

    /// <summary>
    /// Runs the preset square operation.
    /// </summary>
    private void PresetSquare() => State.SetDocumentSize(1200, 1200);
    /// <summary>
    /// Runs the preset landscape operation.
    /// </summary>
    private void PresetLandscape() => State.SetDocumentSize(1600, 1000);
    /// <summary>
    /// Runs the preset full hd operation.
    /// </summary>
    private void PresetFullHd() => State.SetDocumentSize(1920, 1080);
    /// <summary>
    /// Runs the preset a4 operation.
    /// </summary>
    private void PresetA4() => State.SetDocumentSize(2480, 3508);

    /// <summary>
    /// Runs the change layer name operation.
    /// </summary>
    private void ChangeLayerName(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Name = Text(args));
    /// <summary>
    /// Runs the change layer x operation.
    /// </summary>
    private void ChangeLayerX(ChangeEventArgs args) => State.UpdateSelected(layer => layer.X = Number(args, layer.X));
    /// <summary>
    /// Runs the change layer y operation.
    /// </summary>
    private void ChangeLayerY(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Y = Number(args, layer.Y));
    /// <summary>
    /// Runs the change layer width operation.
    /// </summary>
    private void ChangeLayerWidth(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Width = Number(args, layer.Width));
    /// <summary>
    /// Runs the change layer height operation.
    /// </summary>
    private void ChangeLayerHeight(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Height = Number(args, layer.Height));
    /// <summary>
    /// Runs the change layer rotation operation.
    /// </summary>
    private void ChangeLayerRotation(ChangeEventArgs args) => State.UpdateSelectedLive("layer-rotation", layer => layer.Rotation = Number(args, layer.Rotation));
    /// <summary>
    /// Runs the change layer opacity operation.
    /// </summary>
    private void ChangeLayerOpacity(ChangeEventArgs args) => State.UpdateSelectedLive("layer-opacity", layer => layer.Opacity = Number(args, layer.Opacity));
    /// <summary>
    /// Runs the change blend mode operation.
    /// </summary>
    private void ChangeBlendMode(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureBlendMode>(Text(args), true, out var value))
            State.UpdateSelected(layer => layer.BlendMode = value);
    }
    /// <summary>
    /// Runs the toggle selected visibility operation.
    /// </summary>
    private void ToggleSelectedVisibility(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Visible = Bool(args), allowLocked: true);
    /// <summary>
    /// Runs the toggle selected lock operation.
    /// </summary>
    private void ToggleSelectedLock(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Locked = Bool(args), allowLocked: true);
    /// <summary>
    /// Runs the end live edit operation.
    /// </summary>
    private void EndLiveEdit(ChangeEventArgs _) => State.EndLiveEdit();

    /// <summary>
    /// Runs the change raster fit operation.
    /// </summary>
    private void ChangeRasterFit(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRasterFitMode>(Text(args), true, out var value))
            WithRaster(layer => layer.FitMode = value);
    }
    /// <summary>
    /// Runs the toggle raster flip horizontal operation.
    /// </summary>
    private void ToggleRasterFlipHorizontal(ChangeEventArgs args) => WithRaster(layer => layer.FlipHorizontal = Bool(args));
    /// <summary>
    /// Runs the toggle raster flip vertical operation.
    /// </summary>
    private void ToggleRasterFlipVertical(ChangeEventArgs args) => WithRaster(layer => layer.FlipVertical = Bool(args));
    /// <summary>
    /// Runs the change raster tint color operation.
    /// </summary>
    private void ChangeRasterTintColor(ChangeEventArgs args) => WithRaster(layer => layer.TintColor = Text(args));
    /// <summary>
    /// Runs the change raster tint opacity operation.
    /// </summary>
    private void ChangeRasterTintOpacity(ChangeEventArgs args) => WithRasterLive("raster-tint", layer => layer.TintOpacity = Number(args, layer.TintOpacity));

    /// <summary>
    /// Runs the change text content operation.
    /// </summary>
    private void ChangeTextContent(ChangeEventArgs args) => WithText(layer => layer.Text = Text(args));
    /// <summary>
    /// Runs the change text font operation.
    /// </summary>
    private void ChangeTextFont(ChangeEventArgs args) => WithText(layer => layer.FontFamily = Text(args));
    /// <summary>
    /// Runs the change text size operation.
    /// </summary>
    private void ChangeTextSize(ChangeEventArgs args) => WithText(layer => layer.FontSizePx = Number(args, layer.FontSizePx));
    /// <summary>
    /// Runs the change text alignment operation.
    /// </summary>
    private void ChangeTextAlignment(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureTextAlignment>(Text(args), true, out var value))
            WithText(layer => layer.Alignment = value);
    }
    /// <summary>
    /// Runs the toggle text bold operation.
    /// </summary>
    private void ToggleTextBold(ChangeEventArgs args) => WithText(layer => layer.Bold = Bool(args));
    /// <summary>
    /// Runs the toggle text italic operation.
    /// </summary>
    private void ToggleTextItalic(ChangeEventArgs args) => WithText(layer => layer.Italic = Bool(args));
    /// <summary>
    /// Runs the toggle text shadow operation.
    /// </summary>
    private void ToggleTextShadow(ChangeEventArgs args) => WithText(layer => layer.ShadowEnabled = Bool(args));
    /// <summary>
    /// Runs the change text fill operation.
    /// </summary>
    private void ChangeTextFill(ChangeEventArgs args) => WithText(layer => layer.FillColor = Text(args));
    /// <summary>
    /// Runs the change text outline operation.
    /// </summary>
    private void ChangeTextOutline(ChangeEventArgs args) => WithText(layer => layer.OutlineColor = Text(args));
    /// <summary>
    /// Runs the change text outline width operation.
    /// </summary>
    private void ChangeTextOutlineWidth(ChangeEventArgs args) => WithText(layer => layer.OutlineWidthPx = Number(args, layer.OutlineWidthPx));
    /// <summary>
    /// Runs the change text shadow blur operation.
    /// </summary>
    private void ChangeTextShadowBlur(ChangeEventArgs args) => WithText(layer => layer.ShadowBlurPx = Number(args, layer.ShadowBlurPx));

    /// <summary>
    /// Runs the change shape kind operation.
    /// </summary>
    private void ChangeShapeKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureShapeKind>(Text(args), true, out var value))
            WithShape(layer => layer.Shape = value);
    }
    /// <summary>
    /// Runs the change shape fill kind operation.
    /// </summary>
    private void ChangeShapeFillKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value)) WithShape(layer => layer.FillKind = value);
    }
    /// <summary>
    /// Runs the change shape fill operation.
    /// </summary>
    private void ChangeShapeFill(ChangeEventArgs args) => WithShape(layer => layer.FillColor = Text(args));
    /// <summary>
    /// Runs the change shape secondary fill operation.
    /// </summary>
    private void ChangeShapeSecondaryFill(ChangeEventArgs args) => WithShape(layer => layer.SecondaryFillColor = Text(args));
    /// <summary>
    /// Runs the change shape fill angle operation.
    /// </summary>
    private void ChangeShapeFillAngle(ChangeEventArgs args) => State.UpdateSelectedLive("shape-fill-angle", layer => { if (layer is ShapePictureLayer shape) shape.FillAngleDegrees = Number(args, shape.FillAngleDegrees); });
    /// <summary>
    /// Runs the change shape stroke operation.
    /// </summary>
    private void ChangeShapeStroke(ChangeEventArgs args) => WithShape(layer => layer.StrokeColor = Text(args));
    /// <summary>
    /// Runs the change shape stroke width operation.
    /// </summary>
    private void ChangeShapeStrokeWidth(ChangeEventArgs args) => WithShape(layer => layer.StrokeWidthPx = Number(args, layer.StrokeWidthPx));
    /// <summary>
    /// Runs the change shape radius operation.
    /// </summary>
    private void ChangeShapeRadius(ChangeEventArgs args) => WithShape(layer => layer.CornerRadiusPx = Number(args, layer.CornerRadiusPx));
    /// <summary>
    /// Runs the toggle shape path closed operation.
    /// </summary>
    private void ToggleShapePathClosed(ChangeEventArgs args) => WithShape(layer => layer.PathClosed = Bool(args));
    /// <summary>
    /// Runs the toggle shape path smooth operation.
    /// </summary>
    private void ToggleShapePathSmooth(ChangeEventArgs args) => WithShape(layer => layer.PathSmooth = Bool(args));
    /// <summary>
    /// Adds shape path point.
    /// </summary>
    private void AddShapePathPoint() => WithShape(layer =>
    {
        layer.PathPoints ??= [];
        var previous = layer.PathPoints.LastOrDefault();
        layer.PathPoints.Add(new PicturePoint
        {
            X = Math.Clamp((previous?.X ?? layer.Width / 2) + 20, 0, Math.Max(1, layer.Width)),
            Y = Math.Clamp(previous?.Y ?? layer.Height / 2, 0, Math.Max(1, layer.Height))
        });
    });
    /// <summary>
    /// Removes shape path point.
    /// </summary>
    private void RemoveShapePathPoint(int index) => WithShape(layer =>
    {
        if (layer.PathPoints is { Count: > 2 } && index >= 0 && index < layer.PathPoints.Count)
            layer.PathPoints.RemoveAt(index);
    });
    /// <summary>
    /// Runs the reverse shape path operation.
    /// </summary>
    private void ReverseShapePath() => WithShape(layer => { layer.PathPoints?.Reverse(); });
    /// <summary>
    /// Runs the change shape path point x operation.
    /// </summary>
    private void ChangeShapePathPointX(int index, ChangeEventArgs args) => ChangeShapePathPoint(index, args, true);
    /// <summary>
    /// Runs the change shape path point y operation.
    /// </summary>
    private void ChangeShapePathPointY(int index, ChangeEventArgs args) => ChangeShapePathPoint(index, args, false);
    /// <summary>
    /// Runs the change shape path point operation.
    /// </summary>
    private void ChangeShapePathPoint(int index, ChangeEventArgs args, bool horizontal) => WithShape(layer =>
    {
        if (layer.PathPoints is null || index < 0 || index >= layer.PathPoints.Count) return;
        var point = layer.PathPoints[index];
        if (horizontal) point.X = Math.Clamp(Number(args, point.X), -16384, 32768);
        else point.Y = Math.Clamp(Number(args, point.Y), -16384, 32768);
    });

    /// <summary>
    /// Runs the change fill kind operation.
    /// </summary>
    private void ChangeFillKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value))
            WithFill(layer => layer.FillKind = value);
    }
    /// <summary>
    /// Runs the change fill primary operation.
    /// </summary>
    private void ChangeFillPrimary(ChangeEventArgs args) => WithFill(layer => layer.PrimaryColor = Text(args));
    /// <summary>
    /// Runs the change fill secondary operation.
    /// </summary>
    private void ChangeFillSecondary(ChangeEventArgs args) => WithFill(layer => layer.SecondaryColor = Text(args));
    /// <summary>
    /// Runs the change fill angle operation.
    /// </summary>
    private void ChangeFillAngle(ChangeEventArgs args) => WithFillLive("fill-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));

    /// <summary>
    /// Runs the change render kind operation.
    /// </summary>
    private void ChangeRenderKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRenderKind>(Text(args), true, out var value))
            WithRender(layer => layer.RenderKind = value);
    }
    /// <summary>
    /// Runs the change render primary operation.
    /// </summary>
    private void ChangeRenderPrimary(ChangeEventArgs args) => WithRender(layer => layer.PrimaryColor = Text(args));
    /// <summary>
    /// Runs the change render secondary operation.
    /// </summary>
    private void ChangeRenderSecondary(ChangeEventArgs args) => WithRender(layer => layer.SecondaryColor = Text(args));
    /// <summary>
    /// Runs the change render seed operation.
    /// </summary>
    private void ChangeRenderSeed(ChangeEventArgs args) => WithRender(layer => layer.Seed = Int(args, layer.Seed));
    /// <summary>
    /// Runs the change render scale operation.
    /// </summary>
    private void ChangeRenderScale(ChangeEventArgs args) => WithRender(layer => layer.Scale = Number(args, layer.Scale));
    /// <summary>
    /// Runs the change render detail operation.
    /// </summary>
    private void ChangeRenderDetail(ChangeEventArgs args) => WithRender(layer => layer.Detail = Int(args, layer.Detail));
    /// <summary>
    /// Runs the change render softness operation.
    /// </summary>
    private void ChangeRenderSoftness(ChangeEventArgs args) => WithRender(layer => layer.Softness = Number(args, layer.Softness));
    /// <summary>
    /// Runs the change render contrast operation.
    /// </summary>
    private void ChangeRenderContrast(ChangeEventArgs args) => WithRender(layer => layer.RenderContrast = Number(args, layer.RenderContrast));
    /// <summary>
    /// Runs the change render stripe width operation.
    /// </summary>
    private void ChangeRenderStripeWidth(ChangeEventArgs args) => WithRender(layer => layer.StripeWidthPx = Number(args, layer.StripeWidthPx));
    /// <summary>
    /// Runs the change render angle operation.
    /// </summary>
    private void ChangeRenderAngle(ChangeEventArgs args) => WithRenderLive("render-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));
    /// <summary>
    /// Runs the randomize render operation.
    /// </summary>
    private void RandomizeRender() => WithRender(layer => layer.Seed = Random.Shared.Next(1, int.MaxValue));
    /// <summary>
    /// Runs the focus render properties operation.
    /// </summary>
    private Task FocusRenderProperties() => JS.InvokeVoidAsync("publisherStudio.focusElement", "picture-render-properties").AsTask();
    /// <summary>
    /// Runs the render primary white operation.
    /// </summary>
    private void RenderPrimaryWhite() => WithRender(layer => layer.PrimaryColor = "#ffffff");
    /// <summary>
    /// Runs the render primary black operation.
    /// </summary>
    private void RenderPrimaryBlack() => WithRender(layer => layer.PrimaryColor = "#000000");
    /// <summary>
    /// Runs the render primary blue operation.
    /// </summary>
    private void RenderPrimaryBlue() => WithRender(layer => layer.PrimaryColor = "#2563eb");
    /// <summary>
    /// Runs the render secondary white operation.
    /// </summary>
    private void RenderSecondaryWhite() => WithRender(layer => layer.SecondaryColor = "#ffffff");
    /// <summary>
    /// Runs the render secondary black operation.
    /// </summary>
    private void RenderSecondaryBlack() => WithRender(layer => layer.SecondaryColor = "#000000");
    /// <summary>
    /// Runs the render secondary blue operation.
    /// </summary>
    private void RenderSecondaryBlue() => WithRender(layer => layer.SecondaryColor = "#60a5fa");
    /// <summary>
    /// Sets render scale.
    /// </summary>
    private void SetRenderScale(double value) => WithRender(layer => layer.Scale = value);
    /// <summary>
    /// Runs the render scale24 operation.
    /// </summary>
    private void RenderScale24() => SetRenderScale(24);
    /// <summary>
    /// Runs the render scale64 operation.
    /// </summary>
    private void RenderScale64() => SetRenderScale(64);
    /// <summary>
    /// Runs the render scale128 operation.
    /// </summary>
    private void RenderScale128() => SetRenderScale(128);
    /// <summary>
    /// Runs the render scale256 operation.
    /// </summary>
    private void RenderScale256() => SetRenderScale(256);
    /// <summary>
    /// Sets render detail.
    /// </summary>
    private void SetRenderDetail(int value) => WithRender(layer => layer.Detail = value);
    /// <summary>
    /// Runs the render detail1 operation.
    /// </summary>
    private void RenderDetail1() => SetRenderDetail(1);
    /// <summary>
    /// Runs the render detail2 operation.
    /// </summary>
    private void RenderDetail2() => SetRenderDetail(2);
    /// <summary>
    /// Runs the render detail4 operation.
    /// </summary>
    private void RenderDetail4() => SetRenderDetail(4);
    /// <summary>
    /// Runs the render detail6 operation.
    /// </summary>
    private void RenderDetail6() => SetRenderDetail(6);
    /// <summary>
    /// Runs the render detail8 operation.
    /// </summary>
    private void RenderDetail8() => SetRenderDetail(8);
    /// <summary>
    /// Sets render softness.
    /// </summary>
    private void SetRenderSoftness(double value) => WithRender(layer => layer.Softness = value);
    /// <summary>
    /// Runs the render softness0 operation.
    /// </summary>
    private void RenderSoftness0() => SetRenderSoftness(0);
    /// <summary>
    /// Runs the render softness25 operation.
    /// </summary>
    private void RenderSoftness25() => SetRenderSoftness(.25);
    /// <summary>
    /// Runs the render softness50 operation.
    /// </summary>
    private void RenderSoftness50() => SetRenderSoftness(.5);
    /// <summary>
    /// Runs the render softness75 operation.
    /// </summary>
    private void RenderSoftness75() => SetRenderSoftness(.75);
    /// <summary>
    /// Runs the render softness100 operation.
    /// </summary>
    private void RenderSoftness100() => SetRenderSoftness(1);
    /// <summary>
    /// Sets render contrast.
    /// </summary>
    private void SetRenderContrast(double value) => WithRender(layer => layer.RenderContrast = value);
    /// <summary>
    /// Runs the render contrast05 operation.
    /// </summary>
    private void RenderContrast05() => SetRenderContrast(.5);
    /// <summary>
    /// Runs the render contrast10 operation.
    /// </summary>
    private void RenderContrast10() => SetRenderContrast(1);
    /// <summary>
    /// Runs the render contrast15 operation.
    /// </summary>
    private void RenderContrast15() => SetRenderContrast(1.5);
    /// <summary>
    /// Runs the render contrast20 operation.
    /// </summary>
    private void RenderContrast20() => SetRenderContrast(2);
    /// <summary>
    /// Runs the render contrast30 operation.
    /// </summary>
    private void RenderContrast30() => SetRenderContrast(3);
    /// <summary>
    /// Sets render angle.
    /// </summary>
    private void SetRenderAngle(double value) => WithRender(layer => layer.AngleDegrees = value);
    /// <summary>
    /// Runs the render angle0 operation.
    /// </summary>
    private void RenderAngle0() => SetRenderAngle(0);
    /// <summary>
    /// Runs the render angle45 operation.
    /// </summary>
    private void RenderAngle45() => SetRenderAngle(45);
    /// <summary>
    /// Runs the render angle90 operation.
    /// </summary>
    private void RenderAngle90() => SetRenderAngle(90);
    /// <summary>
    /// Runs the render angle180 operation.
    /// </summary>
    private void RenderAngle180() => SetRenderAngle(180);
    /// <summary>
    /// Runs the render angle270 operation.
    /// </summary>
    private void RenderAngle270() => SetRenderAngle(270);
    /// <summary>
    /// Sets render stripe width.
    /// </summary>
    private void SetRenderStripeWidth(double value) => WithRender(layer => layer.StripeWidthPx = value);
    /// <summary>
    /// Runs the render stripe8 operation.
    /// </summary>
    private void RenderStripe8() => SetRenderStripeWidth(8);
    /// <summary>
    /// Runs the render stripe16 operation.
    /// </summary>
    private void RenderStripe16() => SetRenderStripeWidth(16);
    /// <summary>
    /// Runs the render stripe32 operation.
    /// </summary>
    private void RenderStripe32() => SetRenderStripeWidth(32);
    /// <summary>
    /// Runs the render stripe64 operation.
    /// </summary>
    private void RenderStripe64() => SetRenderStripeWidth(64);
    /// <summary>
    /// Runs the render stripe128 operation.
    /// </summary>
    private void RenderStripe128() => SetRenderStripeWidth(128);
    /// <summary>
    /// Runs the reset render settings operation.
    /// </summary>
    private void ResetRenderSettings() => WithRender(layer =>
    {
        layer.Scale = 90;
        layer.Detail = 4;
        layer.Softness = .6;
        layer.RenderContrast = 1;
        layer.AngleDegrees = 45;
        layer.StripeWidthPx = 32;
    });

    /// <summary>
    /// Runs the change brightness operation.
    /// </summary>
    private void ChangeBrightness(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-brightness", layer => layer.Brightness = Number(args, layer.Brightness));
    /// <summary>
    /// Runs the change contrast operation.
    /// </summary>
    private void ChangeContrast(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-contrast", layer => layer.Contrast = Number(args, layer.Contrast));
    /// <summary>
    /// Runs the change saturation operation.
    /// </summary>
    private void ChangeSaturation(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-saturation", layer => layer.Saturation = Number(args, layer.Saturation));
    /// <summary>
    /// Runs the change hue operation.
    /// </summary>
    private void ChangeHue(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-hue", layer => layer.HueRotation = Number(args, layer.HueRotation));
    /// <summary>
    /// Runs the change blur operation.
    /// </summary>
    private void ChangeBlur(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-blur", layer => layer.Blur = Number(args, layer.Blur));
    /// <summary>
    /// Runs the change grayscale operation.
    /// </summary>
    private void ChangeGrayscale(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-grayscale", layer => layer.Grayscale = Number(args, layer.Grayscale));
    /// <summary>
    /// Runs the change sepia operation.
    /// </summary>
    private void ChangeSepia(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-sepia", layer => layer.Sepia = Number(args, layer.Sepia));
    /// <summary>
    /// Runs the change invert operation.
    /// </summary>
    private void ChangeInvert(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-invert", layer => layer.Invert = Number(args, layer.Invert));

    /// <summary>
    /// Runs the reset adjustments operation.
    /// </summary>
    private void ResetAdjustments()
    {
        State.UpdateSelected(layer =>
        {
            layer.Brightness = 1;
            layer.Contrast = 1;
            layer.Saturation = 1;
            layer.HueRotation = 0;
            layer.Blur = 0;
            layer.Grayscale = 0;
            layer.Sepia = 0;
            layer.Invert = 0;
        });
    }

    /// <summary>
    /// Runs the with raster operation.
    /// </summary>
    private void WithRaster(Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Runs the with raster live operation.
    /// </summary>
    private void WithRasterLive(string key, Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    /// <summary>
    /// Runs the toggle SVG preserve aspect ratio operation.
    /// </summary>
    private void ToggleSvgPreserveAspectRatio(ChangeEventArgs args)
    {
        if (State.SelectedLayer is SvgPictureLayer svg)
            State.UpdateSelected(_ => svg.PreserveAspectRatio = Bool(args));
    }

    /// <summary>
    /// Runs the with text operation.
    /// </summary>
    private void WithText(Action<TextPictureLayer> update)
    {
        if (State.SelectedLayer is TextPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Runs the with shape operation.
    /// </summary>
    private void WithShape(Action<ShapePictureLayer> update)
    {
        if (State.SelectedLayer is ShapePictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Runs the with fill operation.
    /// </summary>
    private void WithFill(Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Runs the with fill live operation.
    /// </summary>
    private void WithFillLive(string key, Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    /// <summary>
    /// Runs the with render operation.
    /// </summary>
    private void WithRender(Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Runs the with render live operation.
    /// </summary>
    private void WithRenderLive(string key, Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }


    /// <summary>
    /// Reads SVG text async.
    /// </summary>
    private async Task<string> ReadSvgTextAsync(Stream input)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    }

    /// <summary>
    /// Determines whether supported image data URL.
    /// </summary>
    private bool IsSupportedImageDataUrl(string value) =>
        value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && value.Contains(",", StringComparison.Ordinal);

    /// <summary>
    /// Runs the fit raster canvas size operation.
    /// </summary>
    private PictureImageSize FitRasterCanvasSize(int width, int height)
    {
        if (width <= 0 || height <= 0) return new PictureImageSize { Width = 1200, Height = 800 };
        var scale = Math.Min(1d, 8192d / Math.Max(width, height));
        return new PictureImageSize
        {
            Width = Math.Clamp((int)Math.Round(width * scale), 16, 8192),
            Height = Math.Clamp((int)Math.Round(height * scale), 16, 8192)
        };
    }

    /// <summary>
    /// Runs the layer icon operation.
    /// </summary>
    private string LayerIcon(PictureLayer layer) => layer.Kind switch
    {
        PictureLayerKind.Raster => "▧",
        PictureLayerKind.Text => "T",
        PictureLayerKind.Shape => "◇",
        PictureLayerKind.Fill => "◩",
        PictureLayerKind.Render => "☁",
        PictureLayerKind.Paint => "✎",
        PictureLayerKind.Vector => "⌘",
        _ => "•"
    };

    /// <summary>
    /// Runs the picture text font size menu text operation.
    /// </summary>
    private string PictureTextFontSizeMenuText(TextPictureLayer text) =>
        $"Font size · {Math.Round(text.FontSizePx).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Runs the render scale menu text operation.
    /// </summary>
    private string RenderScaleMenuText(RenderPictureLayer render) =>
        $"Scale · {Math.Round(render.Scale).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Runs the render detail menu text operation.
    /// </summary>
    private string RenderDetailMenuText(RenderPictureLayer render) =>
        $"Detail · {render.Detail.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Runs the render softness menu text operation.
    /// </summary>
    private string RenderSoftnessMenuText(RenderPictureLayer render) =>
        $"Softness · {Math.Round(render.Softness * 100).ToString(CultureInfo.InvariantCulture)}%";

    /// <summary>
    /// Runs the render contrast menu text operation.
    /// </summary>
    private string RenderContrastMenuText(RenderPictureLayer render) =>
        $"Contrast · {render.RenderContrast.ToString("0.0", CultureInfo.InvariantCulture)}×";

    /// <summary>
    /// Runs the render angle menu text operation.
    /// </summary>
    private string RenderAngleMenuText(RenderPictureLayer render) =>
        $"Angle · {Math.Round(render.AngleDegrees).ToString(CultureInfo.InvariantCulture)}°";

    /// <summary>
    /// Runs the render stripe width menu text operation.
    /// </summary>
    private string RenderStripeWidthMenuText(RenderPictureLayer render) =>
        $"Stripe width · {Math.Round(render.StripeWidthPx).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Runs the layer description operation.
    /// </summary>
    private string LayerDescription(PictureLayer layer) => layer switch
    {
        RasterPictureLayer raster => raster.FitMode.ToString(),
        SvgPictureLayer svg => string.IsNullOrWhiteSpace(svg.GroupPath) ? svg.SourceFormat : $"{svg.SourceFormat} · {svg.GroupPath}",
        TextPictureLayer text => Truncate(text.Text, 28),
        ShapePictureLayer shape => shape.Shape.ToString(),
        FillPictureLayer fill => fill.FillKind.ToString(),
        RenderPictureLayer render => render.RenderKind.ToString(),
        PaintPictureLayer paint => $"{paint.Strokes.Count} stroke{(paint.Strokes.Count == 1 ? string.Empty : "s")}",
        _ => layer.Kind.ToString()
    };

    /// <summary>
    /// Runs the truncate operation.
    /// </summary>
    private string Truncate(string value, int length) => string.IsNullOrWhiteSpace(value)
        ? "Empty"
        : value.Length <= length ? value : value[..length] + "…";

    /// <summary>
    /// Runs the text operation.
    /// </summary>
    private string Text(ChangeEventArgs args) => Convert.ToString(args.Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    /// <summary>
    /// Runs the bool operation.
    /// </summary>
    private bool Bool(ChangeEventArgs args) => args.Value is bool value && value;
    /// <summary>
    /// Runs the number operation.
    /// </summary>
    private double Number(ChangeEventArgs args, double fallback) => double.TryParse(Text(args), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    /// <summary>
    /// Runs the int operation.
    /// </summary>
    private int Int(ChangeEventArgs args, int fallback) => int.TryParse(Text(args), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    /// <summary>
    /// Runs the inv operation.
    /// </summary>
    private string Inv(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    /// <summary>
    /// Runs the safe color operation.
    /// </summary>
    private string SafeColor(string value) => value.StartsWith('#') && value.Length is 4 or 7 ? value : "#000000";

    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
    public void Dispose()
    {
        State.Changed -= StateChanged;
        LocalGptConnection.Changed -= LocalGptConnectionChanged;
    }

    /// <summary>
    /// Runs the dispose async operation.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        State.Changed -= StateChanged;
        LocalGptConnection.Changed -= LocalGptConnectionChanged;
        _self?.Dispose();
        try
        {
            await DisposePictureRuntimeAsync();
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Represents a picture area selection.
    /// </summary>
    private sealed class PictureAreaSelection
    {
        /// <summary>
        /// Gets or sets kind.
        /// </summary>
        public string Kind { get; set; } = "polygon";
        /// <summary>
        /// Gets or sets points.
        /// </summary>
        public List<PicturePoint> Points { get; set; } = [];
    }

    /// <summary>
    /// Represents a picture ocr result.
    /// </summary>
    private sealed class PictureOcrResult
    {
        /// <summary>
        /// Gets or sets text.
        /// </summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets provider URI.
        /// </summary>
        public string ProviderUri { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets media type.
        /// </summary>
        public string MediaType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets needs human review.
        /// </summary>
        public bool NeedsHumanReview { get; set; } = true;
    }

    /// <summary>
    /// Represents a picture image size.
    /// </summary>
    private sealed class PictureImageSize
    {
        /// <summary>
        /// Runs the picture image size operation.
        /// </summary>
        public PictureImageSize() { }
        /// <summary>
        /// Gets or sets width.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Gets or sets height.
        /// </summary>
        public int Height { get; set; }
    }
}
