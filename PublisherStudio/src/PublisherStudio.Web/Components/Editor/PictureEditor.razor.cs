using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PublisherStudio.Domain;
using PublisherStudio.Services;

namespace PublisherStudio.Components.Editor;

public partial class PictureEditor
{
    private const string CanvasId = "picture-studio-canvas";
    private const string CanvasHostId = "picture-studio-canvas-host";
    private static readonly string[] PictureFonts =
    [
        "Segoe UI", "Arial", "Arial Black", "Calibri", "Cambria", "Georgia", "Impact", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"
    ];
    private static readonly string[] PictureColors =
    [
        "#000000", "#ffffff", "#ef4444", "#f97316", "#eab308", "#22c55e", "#06b6d4", "#3b82f6", "#8b5cf6", "#ec4899", "#64748b", "#92400e"
    ];

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] public PictureEditorStateService State { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public Guid SessionId { get; set; }
    [Parameter] public PictureDocument? InitialDocument { get; set; }
    [Parameter] public string? InitialRasterDataUrl { get; set; }
    [Parameter] public string InitialName { get; set; } = "Picture";
    [Parameter] public bool EditingExisting { get; set; }
    [Parameter] public EventCallback<PictureEditorResult> Saved { get; set; }
    [Parameter] public EventCallback Cancelled { get; set; }

    private IJSObjectReference? _module;
    private DotNetObjectReference<PictureEditor>? _self;
    private Guid _loadedSession;
    private bool _renderRequested;
    private bool _initialized;
    private bool _pendingRasterInitialization;
    private string? _error;
    private bool _renderErrorActive;
    private PictureDrawTool _drawTool = PictureDrawTool.Select;
    private string _drawColor = "#111827";
    private double _drawWidth = 12;
    private double _drawOpacity = 1;
    private double _drawHardness = .8;

    private bool HasSelection => State.SelectedLayer is not null;
    private bool CanDelete => State.SelectedLayer is { Locked: false };
    private bool IsRenderSelected => State.SelectedLayer is RenderPictureLayer;
    private bool IsRasterSelected => State.SelectedLayer is RasterPictureLayer;
    private bool IsPaintSelected => State.SelectedLayer is PaintPictureLayer;
    private bool CanDraw => _drawTool != PictureDrawTool.Select;
    private string SelectToolText => ToolText(PictureDrawTool.Select, "Select");
    private string BrushToolText => ToolText(PictureDrawTool.Brush, "Brush");
    private string PencilToolText => ToolText(PictureDrawTool.Pencil, "Pencil");
    private string LineToolText => ToolText(PictureDrawTool.Line, "Line");
    private string EraserToolText => ToolText(PictureDrawTool.Eraser, "Eraser");
    private string EyedropperToolText => ToolText(PictureDrawTool.Eyedropper, "Eyedropper");
    private string CanvasHint => _drawTool switch
    {
        PictureDrawTool.Select => "Drag layers directly. Corner handles resize; the round handle rotates.",
        PictureDrawTool.Eyedropper => "Click the rendered canvas to pick a color, then the Brush tool becomes active.",
        PictureDrawTool.Line => "Drag from the line start to its end. Hold the pointer down for a live preview.",
        PictureDrawTool.Eraser => "Draw over strokes on a paint layer to erase them non-destructively.",
        _ => "Draw directly on the canvas. A paint layer is created automatically when necessary."
    };
    private string CanvasColor => State.Document.Background.StartsWith('#') && State.Document.Background.Length is 4 or 7
        ? State.Document.Background
        : "#ffffff";
    private string StatusText => _error ?? (_drawTool != PictureDrawTool.Select
        ? $"{_drawTool} tool · {_drawWidth:0.#} px · {_drawColor}"
        : State.SelectedLayer is null ? "No layer selected" : $"{State.SelectedLayer.Kind}: {State.SelectedLayer.Name}");

    protected override void OnInitialized() => State.Changed += StateChanged;

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
        _renderErrorActive = false;
        _drawTool = PictureDrawTool.Select;
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
            await _module.InvokeVoidAsync("initializePictureStudio", CanvasId, _self);
            _initialized = true;
            _renderRequested = true;
        }
        if (_renderRequested)
        {
            _renderRequested = false;
            await RenderCanvasAsync();
        }
    }

    private void StateChanged()
    {
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RenderCanvasAsync()
    {
        if (_module is null || !Visible) return;
        try
        {
            await _module.InvokeVoidAsync("renderPictureStudio", CanvasId, State.Document, State.SelectedLayerId?.ToString(), State.Document.Zoom, new
            {
                Tool = _drawTool.ToString(),
                Color = _drawColor,
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

    [JSInvokable]
    public void PictureLayerSelected(string? id)
    {
        State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
    }

    [JSInvokable]
    public void PictureTransformCommitted(string id, double x, double y, double width, double height, double rotation)
    {
        if (Guid.TryParse(id, out var parsed))
            State.CommitTransform(parsed, x, y, width, height, rotation);
    }

    [JSInvokable]
    public void PictureStrokeCommitted(string tool, double[] coordinates, string color, double width, double opacity, double hardness)
    {
        if (!Enum.TryParse<PictureStrokeKind>(tool, true, out var kind) || coordinates.Length < 4) return;
        var points = new List<PicturePoint>(coordinates.Length / 2);
        for (var index = 0; index + 1 < coordinates.Length; index += 2)
            points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
        State.AddStroke(kind, points, color, width, opacity, hardness);
    }

    [JSInvokable]
    public void PictureColorPicked(string color)
    {
        if (!string.IsNullOrWhiteSpace(color)) _drawColor = color;
        _drawTool = PictureDrawTool.Brush;
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void PictureRenderFailed(string message)
    {
        _renderErrorActive = true;
        _error = string.IsNullOrWhiteSpace(message) ? "A picture layer could not be rendered." : message;
        _ = InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void PictureRenderRecovered()
    {
        if (!_renderErrorActive) return;
        _renderErrorActive = false;
        _error = null;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RequestImage()
    {
        await JS.InvokeVoidAsync("publisherStudio.clickElement", "picture-studio-image-input");
    }

    private async Task ImportImage(InputFileChangeEventArgs args)
    {
        try
        {
            var file = args.File;
            var allowed = new[] { "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml" };
            if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported picture format.");

            await using var stream = file.OpenReadStream(64 * 1024 * 1024);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var dataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
            var size = _module is null
                ? new PictureImageSize()
                : await _module.InvokeAsync<PictureImageSize>("getPictureImageSize", dataUrl);
            State.AddRaster(dataUrl, file.Name, size.Width, size.Height);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private void AddTextLayer() => State.AddText();
    private void AddRectangle() => State.AddShape(PictureShapeKind.Rectangle);
    private void AddEllipse() => State.AddShape(PictureShapeKind.Ellipse);
    private void AddLineShape() => State.AddShape(PictureShapeKind.Line);
    private void AddGradient() => State.AddFill(PictureFillKind.LinearGradient);
    private void AddSolidFill() => State.AddFill(PictureFillKind.Solid);
    private void AddClouds() => State.AddRender(PictureRenderKind.Clouds);
    private void AddNoise() => State.AddRender(PictureRenderKind.Noise);
    private void AddStripes() => State.AddRender(PictureRenderKind.Stripes);
    private void AddVignette() => State.AddRender(PictureRenderKind.Vignette);
    private void AddPaintLayer() => State.AddPaint();
    private void MoveUp() => State.MoveSelectedLayer(1);
    private void MoveDown() => State.MoveSelectedLayer(-1);
    private void Zoom100() => State.SetZoom(1);

    private async Task FitCanvas()
    {
        if (_module is null) return;
        var zoom = await _module.InvokeAsync<double>("fitPictureStudio", CanvasHostId, State.Document.WidthPx, State.Document.HeightPx);
        State.SetZoom(zoom);
    }

    private async Task Apply()
    {
        if (_module is null) return;
        try
        {
            await using var streamReference = await _module.InvokeAsync<IJSStreamReference>("exportPictureStudioBlob", State.Document, "image/png", 1d);
            await using var stream = await streamReference.OpenReadStreamAsync(128L * 1024 * 1024);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(buffer.ToArray())}";
            await Saved.InvokeAsync(new PictureEditorResult(dataUrl, State.CloneDocument(), State.Document.Name));
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private async Task DownloadPng() => await Download("image/png", "png", 1d);
    private async Task DownloadJpeg() => await Download("image/jpeg", "jpg", .92d);

    private async Task Download(string mimeType, string extension, double quality)
    {
        if (_module is null) return;
        try
        {
            var name = PublicationFileService.SafeFileName(State.Document.Name) + "." + extension;
            await _module.InvokeVoidAsync("downloadPictureStudio", State.Document, name, mimeType, quality);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private Task Cancel() => Cancelled.InvokeAsync();

    private void SelectTool() => SetDrawTool(PictureDrawTool.Select);
    private void BrushTool() => SetDrawTool(PictureDrawTool.Brush);
    private void PencilTool() => SetDrawTool(PictureDrawTool.Pencil);
    private void LineTool() => SetDrawTool(PictureDrawTool.Line);
    private void EraserTool() => SetDrawTool(PictureDrawTool.Eraser);
    private void EyedropperTool() => SetDrawTool(PictureDrawTool.Eyedropper);
    private void SetDrawTool(PictureDrawTool tool)
    {
        _drawTool = tool;
        _renderRequested = true;
        StateHasChanged();
    }
    private string ToolText(PictureDrawTool tool, string text) => _drawTool == tool ? $"✓ {text}" : text;
    private void ChangeDrawColor(string value) { if (!string.IsNullOrWhiteSpace(value)) _drawColor = value; _renderRequested = true; }
    private void SetDrawWidth(double value) { _drawWidth = Math.Clamp(value, .25, 512); _renderRequested = true; }
    private void DrawWidth1() => SetDrawWidth(1);
    private void DrawWidth3() => SetDrawWidth(3);
    private void DrawWidth8() => SetDrawWidth(8);
    private void DrawWidth16() => SetDrawWidth(16);
    private void DrawWidth32() => SetDrawWidth(32);
    private void ToggleGridRibbon() => State.SetGrid(!State.Document.GridVisible);
    private void ToggleSnapRibbon() => State.SetSnap(!State.Document.SnapToGrid);
    private string GridText => State.Document.GridVisible ? "✓ Grid" : "Grid";
    private string SnapText => State.Document.SnapToGrid ? "✓ Snap" : "Snap";
    private void MakeRenderClouds() => WithRender(layer => layer.RenderKind = PictureRenderKind.Clouds);
    private void MakeRenderNoise() => WithRender(layer => layer.RenderKind = PictureRenderKind.Noise);
    private void MakeRenderStripes() => WithRender(layer => layer.RenderKind = PictureRenderKind.Stripes);
    private void MakeRenderVignette() => WithRender(layer => layer.RenderKind = PictureRenderKind.Vignette);
    private void RasterContain() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Contain);
    private void RasterCover() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Cover);
    private void RasterFlipHorizontal() => WithRaster(layer => layer.FlipHorizontal = !layer.FlipHorizontal);
    private void RasterFlipVertical() => WithRaster(layer => layer.FlipVertical = !layer.FlipVertical);
    private void SoftenLight() => State.UpdateSelected(layer => layer.Blur = 2);
    private void SoftenMedium() => State.UpdateSelected(layer => layer.Blur = 6);
    private void RemoveSoftening() => State.UpdateSelected(layer => layer.Blur = 0);
    private void Brighten() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness + .1, 0, 3));
    private void Darken() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness - .1, 0, 3));
    private void MoreContrast() => State.UpdateSelected(layer => layer.Contrast = Math.Clamp(layer.Contrast + .1, 0, 3));
    private void MoreSaturation() => State.UpdateSelected(layer => layer.Saturation = Math.Clamp(layer.Saturation + .1, 0, 3));
    private void ToggleGrayscalePreset() => State.UpdateSelected(layer => layer.Grayscale = layer.Grayscale > .5 ? 0 : 1);
    private void ToggleSepiaPreset() => State.UpdateSelected(layer => layer.Sepia = layer.Sepia > .5 ? 0 : 1);
    private void ToggleInvertPreset() => State.UpdateSelected(layer => layer.Invert = layer.Invert > .5 ? 0 : 1);

    private void ChangeDocumentName(ChangeEventArgs args) => State.SetDocumentName(Text(args));
    private void ChangeCanvasWidth(ChangeEventArgs args) => State.SetDocumentSize(Int(args, State.Document.WidthPx), State.Document.HeightPx);
    private void ChangeCanvasHeight(ChangeEventArgs args) => State.SetDocumentSize(State.Document.WidthPx, Int(args, State.Document.HeightPx));
    private void ChangeBackgroundPreset(ChangeEventArgs args) => State.SetBackground(Text(args));
    private void ChangeCanvasColor(ChangeEventArgs args) => State.SetBackground(Text(args));
    private void ChangeGridSpacing(ChangeEventArgs args) => State.SetGridSpacing(Int(args, State.Document.GridSpacingPx));
    private void ToggleGrid(ChangeEventArgs args) => State.SetGrid(Bool(args));
    private void ToggleSnap(ChangeEventArgs args) => State.SetSnap(Bool(args));
    private void ChangeZoom(ChangeEventArgs args) => State.SetZoom(Number(args, State.Document.Zoom));
    private void ChangeDrawTool(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureDrawTool>(Text(args), true, out var tool)) SetDrawTool(tool);
    }
    private void ChangeDrawColorInput(ChangeEventArgs args) => ChangeDrawColor(Text(args));
    private void ChangeDrawWidth(ChangeEventArgs args) => SetDrawWidth(Number(args, _drawWidth));
    private void ChangeDrawOpacity(ChangeEventArgs args) { _drawOpacity = Math.Clamp(Number(args, _drawOpacity), 0, 1); _renderRequested = true; }
    private void ChangeDrawHardness(ChangeEventArgs args) { _drawHardness = Math.Clamp(Number(args, _drawHardness), 0, 1); _renderRequested = true; }

    private void PresetSquare() => State.SetDocumentSize(1200, 1200);
    private void PresetLandscape() => State.SetDocumentSize(1600, 1000);
    private void PresetFullHd() => State.SetDocumentSize(1920, 1080);
    private void PresetA4() => State.SetDocumentSize(2480, 3508);

    private void ChangeLayerName(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Name = Text(args));
    private void ChangeLayerX(ChangeEventArgs args) => State.UpdateSelected(layer => layer.X = Number(args, layer.X));
    private void ChangeLayerY(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Y = Number(args, layer.Y));
    private void ChangeLayerWidth(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Width = Number(args, layer.Width));
    private void ChangeLayerHeight(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Height = Number(args, layer.Height));
    private void ChangeLayerRotation(ChangeEventArgs args) => State.UpdateSelectedLive("layer-rotation", layer => layer.Rotation = Number(args, layer.Rotation));
    private void ChangeLayerOpacity(ChangeEventArgs args) => State.UpdateSelectedLive("layer-opacity", layer => layer.Opacity = Number(args, layer.Opacity));
    private void ChangeBlendMode(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureBlendMode>(Text(args), true, out var value))
            State.UpdateSelected(layer => layer.BlendMode = value);
    }
    private void ToggleSelectedVisibility(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Visible = Bool(args), allowLocked: true);
    private void ToggleSelectedLock(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Locked = Bool(args), allowLocked: true);
    private void EndLiveEdit(ChangeEventArgs _) => State.EndLiveEdit();

    private void ChangeRasterFit(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRasterFitMode>(Text(args), true, out var value))
            WithRaster(layer => layer.FitMode = value);
    }
    private void ToggleRasterFlipHorizontal(ChangeEventArgs args) => WithRaster(layer => layer.FlipHorizontal = Bool(args));
    private void ToggleRasterFlipVertical(ChangeEventArgs args) => WithRaster(layer => layer.FlipVertical = Bool(args));
    private void ChangeRasterTintColor(ChangeEventArgs args) => WithRaster(layer => layer.TintColor = Text(args));
    private void ChangeRasterTintOpacity(ChangeEventArgs args) => WithRasterLive("raster-tint", layer => layer.TintOpacity = Number(args, layer.TintOpacity));

    private void ChangeTextContent(ChangeEventArgs args) => WithText(layer => layer.Text = Text(args));
    private void ChangeTextFont(ChangeEventArgs args) => WithText(layer => layer.FontFamily = Text(args));
    private void ChangeTextSize(ChangeEventArgs args) => WithText(layer => layer.FontSizePx = Number(args, layer.FontSizePx));
    private void ChangeTextAlignment(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureTextAlignment>(Text(args), true, out var value))
            WithText(layer => layer.Alignment = value);
    }
    private void ToggleTextBold(ChangeEventArgs args) => WithText(layer => layer.Bold = Bool(args));
    private void ToggleTextItalic(ChangeEventArgs args) => WithText(layer => layer.Italic = Bool(args));
    private void ToggleTextShadow(ChangeEventArgs args) => WithText(layer => layer.ShadowEnabled = Bool(args));
    private void ChangeTextFill(ChangeEventArgs args) => WithText(layer => layer.FillColor = Text(args));
    private void ChangeTextOutline(ChangeEventArgs args) => WithText(layer => layer.OutlineColor = Text(args));
    private void ChangeTextOutlineWidth(ChangeEventArgs args) => WithText(layer => layer.OutlineWidthPx = Number(args, layer.OutlineWidthPx));
    private void ChangeTextShadowBlur(ChangeEventArgs args) => WithText(layer => layer.ShadowBlurPx = Number(args, layer.ShadowBlurPx));

    private void ChangeShapeKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureShapeKind>(Text(args), true, out var value))
            WithShape(layer => layer.Shape = value);
    }
    private void ChangeShapeFill(ChangeEventArgs args) => WithShape(layer => layer.FillColor = Text(args));
    private void ChangeShapeStroke(ChangeEventArgs args) => WithShape(layer => layer.StrokeColor = Text(args));
    private void ChangeShapeStrokeWidth(ChangeEventArgs args) => WithShape(layer => layer.StrokeWidthPx = Number(args, layer.StrokeWidthPx));
    private void ChangeShapeRadius(ChangeEventArgs args) => WithShape(layer => layer.CornerRadiusPx = Number(args, layer.CornerRadiusPx));

    private void ChangeFillKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value))
            WithFill(layer => layer.FillKind = value);
    }
    private void ChangeFillPrimary(ChangeEventArgs args) => WithFill(layer => layer.PrimaryColor = Text(args));
    private void ChangeFillSecondary(ChangeEventArgs args) => WithFill(layer => layer.SecondaryColor = Text(args));
    private void ChangeFillAngle(ChangeEventArgs args) => WithFillLive("fill-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));

    private void ChangeRenderKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRenderKind>(Text(args), true, out var value))
            WithRender(layer => layer.RenderKind = value);
    }
    private void ChangeRenderPrimary(ChangeEventArgs args) => WithRender(layer => layer.PrimaryColor = Text(args));
    private void ChangeRenderSecondary(ChangeEventArgs args) => WithRender(layer => layer.SecondaryColor = Text(args));
    private void ChangeRenderSeed(ChangeEventArgs args) => WithRender(layer => layer.Seed = Int(args, layer.Seed));
    private void ChangeRenderScale(ChangeEventArgs args) => WithRender(layer => layer.Scale = Number(args, layer.Scale));
    private void ChangeRenderDetail(ChangeEventArgs args) => WithRender(layer => layer.Detail = Int(args, layer.Detail));
    private void ChangeRenderSoftness(ChangeEventArgs args) => WithRender(layer => layer.Softness = Number(args, layer.Softness));
    private void ChangeRenderContrast(ChangeEventArgs args) => WithRender(layer => layer.RenderContrast = Number(args, layer.RenderContrast));
    private void ChangeRenderStripeWidth(ChangeEventArgs args) => WithRender(layer => layer.StripeWidthPx = Number(args, layer.StripeWidthPx));
    private void ChangeRenderAngle(ChangeEventArgs args) => WithRenderLive("render-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));
    private void RandomizeRender() => WithRender(layer => layer.Seed = Random.Shared.Next(1, int.MaxValue));

    private void ChangeBrightness(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-brightness", layer => layer.Brightness = Number(args, layer.Brightness));
    private void ChangeContrast(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-contrast", layer => layer.Contrast = Number(args, layer.Contrast));
    private void ChangeSaturation(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-saturation", layer => layer.Saturation = Number(args, layer.Saturation));
    private void ChangeHue(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-hue", layer => layer.HueRotation = Number(args, layer.HueRotation));
    private void ChangeBlur(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-blur", layer => layer.Blur = Number(args, layer.Blur));
    private void ChangeGrayscale(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-grayscale", layer => layer.Grayscale = Number(args, layer.Grayscale));
    private void ChangeSepia(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-sepia", layer => layer.Sepia = Number(args, layer.Sepia));
    private void ChangeInvert(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-invert", layer => layer.Invert = Number(args, layer.Invert));

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

    private void WithRaster(Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    private void WithRasterLive(string key, Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    private void WithText(Action<TextPictureLayer> update)
    {
        if (State.SelectedLayer is TextPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    private void WithShape(Action<ShapePictureLayer> update)
    {
        if (State.SelectedLayer is ShapePictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    private void WithFill(Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    private void WithFillLive(string key, Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    private void WithRender(Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    private void WithRenderLive(string key, Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }


    private static bool IsSupportedImageDataUrl(string value) =>
        value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && value.Contains(",", StringComparison.Ordinal);

    private static PictureImageSize FitRasterCanvasSize(int width, int height)
    {
        if (width <= 0 || height <= 0) return new PictureImageSize { Width = 1200, Height = 800 };
        var scale = Math.Min(1d, 8192d / Math.Max(width, height));
        return new PictureImageSize
        {
            Width = Math.Clamp((int)Math.Round(width * scale), 16, 8192),
            Height = Math.Clamp((int)Math.Round(height * scale), 16, 8192)
        };
    }

    private static string LayerIcon(PictureLayer layer) => layer.Kind switch
    {
        PictureLayerKind.Raster => "▧",
        PictureLayerKind.Text => "T",
        PictureLayerKind.Shape => "◇",
        PictureLayerKind.Fill => "◩",
        PictureLayerKind.Render => "☁",
        PictureLayerKind.Paint => "✎",
        _ => "•"
    };

    private static string LayerDescription(PictureLayer layer) => layer switch
    {
        RasterPictureLayer raster => raster.FitMode.ToString(),
        TextPictureLayer text => Truncate(text.Text, 28),
        ShapePictureLayer shape => shape.Shape.ToString(),
        FillPictureLayer fill => fill.FillKind.ToString(),
        RenderPictureLayer render => render.RenderKind.ToString(),
        PaintPictureLayer paint => $"{paint.Strokes.Count} stroke{(paint.Strokes.Count == 1 ? string.Empty : "s")}",
        _ => layer.Kind.ToString()
    };

    private static string Truncate(string value, int length) => string.IsNullOrWhiteSpace(value)
        ? "Empty"
        : value.Length <= length ? value : value[..length] + "…";

    private static string Text(ChangeEventArgs args) => Convert.ToString(args.Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    private static bool Bool(ChangeEventArgs args) => args.Value is bool value && value;
    private static double Number(ChangeEventArgs args, double fallback) => double.TryParse(Text(args), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    private static int Int(ChangeEventArgs args, int fallback) => int.TryParse(Text(args), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    private static string Inv(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string SafeColor(string value) => value.StartsWith('#') && value.Length is 4 or 7 ? value : "#000000";

    public void Dispose() => State.Changed -= StateChanged;

    public async ValueTask DisposeAsync()
    {
        State.Changed -= StateChanged;
        _self?.Dispose();
        if (_module is not null) await _module.DisposeAsync();
    }

    private sealed class PictureImageSize
    {
        public PictureImageSize() { }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
