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
/// Represents a picture editor application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public partial class PictureEditor
{
    /// <summary>
    /// Stores the internal picture colors state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string[] PictureColors =
    [
        "#000000", "#ffffff", "#ef4444", "#f97316", "#eab308", "#22c55e", "#06b6d4", "#3b82f6", "#8b5cf6", "#ec4899", "#64748b", "#92400e"
    ];

    /// <summary>
    /// Gets or sets the JavaScript value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JavaScript value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private IJSRuntime JS { get; set; } = default!;
    /// <summary>
    /// Gets or sets the system fonts value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The system fonts value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private SystemFontCatalog SystemFonts { get; set; } = default!;
    /// <summary>
    /// Gets or sets the state value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The state value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] public PictureEditorStateService State { get; set; } = default!;
    /// <summary>
    /// Gets or sets the open raster importer value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The open raster importer value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private OpenRasterImportService OpenRasterImporter { get; set; } = default!;
    /// <summary>
    /// Gets or sets the LocalGPT connection value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The LocalGPT connection value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private ILocalGptConnectionService LocalGptConnection { get; set; } = default!;
    /// <summary>
    /// Gets or sets the notifications value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notifications value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private IUserNotificationService Notifications { get; set; } = default!;
    /// <summary>
    /// Gets or sets the logger value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logger value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private ILogger<PictureEditor> Logger { get; set; } = default!;
    /// <summary>
    /// Gets or sets the runtime policy value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime policy value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private IPublisherRuntimePolicyDataService RuntimePolicy { get; set; } = default!;
    /// <summary>
    /// Gets or sets the files value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The files value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private PublicationFileService Files { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the picture editor state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public bool Visible { get; set; }
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this picture editor instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the initial document value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial document value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public PictureDocument? InitialDocument { get; set; }
    /// <summary>
    /// Gets or sets the initial raster data URL that identifies the network or application endpoint associated with this picture editor state.
    /// </summary>
    /// <value>The initial raster data URL value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public string? InitialRasterDataUrl { get; set; }
    /// <summary>
    /// Gets or sets the initial name value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial name value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public string InitialName { get; set; } = "Picture";
    /// <summary>
    /// Gets or sets a value indicating whether editing existing applies to the picture editor state.
    /// </summary>
    /// <value>The editing existing value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public bool EditingExisting { get; set; }
    /// <summary>
    /// Gets or sets the saved value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saved value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public EventCallback<PictureEditorResult> Saved { get; set; }
    /// <summary>
    /// Gets or sets the cancelled value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cancelled value exposed by <see cref="PictureEditor"/>.</value>
    [Parameter] public EventCallback Cancelled { get; set; }

    /// <summary>
    /// Gets the picture fonts collection maintained or exposed by this picture editor instance for downstream processing.
    /// </summary>
    /// <value>The picture fonts value exposed by <see cref="PictureEditor"/>.</value>
    private IReadOnlyList<string> PictureFonts => SystemFonts.FontFamilies;

    /// <summary>
    /// Stores the JavaScript object reference dependency used by <see cref="PictureEditor"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IJSObjectReference? _module;
    /// <summary>
    /// Stores the internal picture context menu state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private DxContextMenu _pictureContextMenu = default!;
    /// <summary>
    /// Stores the internal self state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private DotNetObjectReference<PictureEditor>? _self;
    /// <summary>
    /// Stores the internal loaded session state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _loadedSession;
    /// <summary>
    /// Stores the internal render requested state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private bool _renderRequested;
    /// <summary>
    /// Stores the internal initialized state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private bool _initialized;
    /// <summary>
    /// Stores the internal pending raster initialization state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private bool _pendingRasterInitialization;
    /// <summary>
    /// Stores the internal error state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string? _error;
    /// <summary>
    /// Stores the internal notice state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string? _notice;
    /// <summary>
    /// Stores the internal render error active state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private bool _renderErrorActive;
    /// <summary>
    /// Stores the internal draw tool state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private PictureDrawTool _drawTool = PictureDrawTool.Select;
    /// <summary>
    /// Stores the internal draw color state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string _drawColor = "#111827";
    /// <summary>
    /// Stores the internal draw secondary color state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string _drawSecondaryColor = "#ffffff";
    /// <summary>
    /// Stores the internal draw width state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private double _drawWidth = 12;
    /// <summary>
    /// Stores the internal draw opacity state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private double _drawOpacity = 1;
    /// <summary>
    /// Stores the internal draw hardness state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private double _drawHardness = .8;
    /// <summary>
    /// Stores the internal picture export buffer state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private StringBuilder? _pictureExportBuffer;
    /// <summary>
    /// Stores the internal picture export identifier state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string? _pictureExportId;
    /// <summary>
    /// Stores the internal picture export source document state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private PictureDocument? _pictureExportSourceDocument;
    /// <summary>
    /// Stores the internal picture export name state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string? _pictureExportName;
    /// <summary>
    /// Stores the internal picture export purpose state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string _pictureExportPurpose = "save";
    /// <summary>
    /// Stores the internal OCR text state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string _ocrText = string.Empty;
    /// <summary>
    /// Stores the internal OCR status state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private string _ocrStatus = string.Empty;
    /// <summary>
    /// Stores the internal OCR busy state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private bool _ocrBusy;
    /// <summary>
    /// Stores the internal picture export expected chunks state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private int _pictureExportExpectedChunks;
    /// <summary>
    /// Stores the internal picture export next chunk state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private int _pictureExportNextChunk;
    /// <summary>
    /// Stores the internal picture export expected length state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private int _pictureExportExpectedLength;
    /// <summary>
    /// Stores the internal replace raster layer identifier state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private Guid? _replaceRasterLayerId;
    /// <summary>
    /// Stores the internal pending drop x state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private double? _pendingDropX;
    /// <summary>
    /// Stores the internal pending drop y state used by <see cref="PictureEditor"/> while executing its surrounding workflow.
    /// </summary>
    private double? _pendingDropY;

    /// <summary>
    /// Gets a value indicating whether selection applies to the picture editor state.
    /// </summary>
    /// <value>The has selection value exposed by <see cref="PictureEditor"/>.</value>
    private bool HasSelection => State.SelectedLayer is not null;
    /// <summary>
    /// Gets a value indicating whether delete applies to the picture editor state.
    /// </summary>
    /// <value>The can delete value exposed by <see cref="PictureEditor"/>.</value>
    private bool CanDelete => State.SelectedLayer is { Locked: false };
    /// <summary>
    /// Gets a value indicating whether layer clip applies to the picture editor state.
    /// </summary>
    /// <value>The has layer clip value exposed by <see cref="PictureEditor"/>.</value>
    private bool HasLayerClip => State.SelectedLayer is { ClipPolygon.Count: >= 3 };
    /// <summary>
    /// Gets a value indicating whether render selected applies to the picture editor state.
    /// </summary>
    /// <value>The is render selected value exposed by <see cref="PictureEditor"/>.</value>
    private bool IsRenderSelected => State.SelectedLayer is RenderPictureLayer;
    /// <summary>
    /// Gets a value indicating whether raster selected applies to the picture editor state.
    /// </summary>
    /// <value>The is raster selected value exposed by <see cref="PictureEditor"/>.</value>
    private bool IsRasterSelected => State.SelectedLayer is RasterPictureLayer;
    /// <summary>
    /// Gets a value indicating whether paint selected applies to the picture editor state.
    /// </summary>
    /// <value>The is paint selected value exposed by <see cref="PictureEditor"/>.</value>
    private bool IsPaintSelected => State.SelectedLayer is PaintPictureLayer;
    /// <summary>
    /// Gets a value indicating whether draw applies to the picture editor state.
    /// </summary>
    /// <value>The can draw value exposed by <see cref="PictureEditor"/>.</value>
    private bool CanDraw => _drawTool != PictureDrawTool.Select;
    /// <summary>
    /// Gets a value indicating whether picture exporting applies to the picture editor state.
    /// </summary>
    /// <value>The is picture exporting value exposed by <see cref="PictureEditor"/>.</value>
    private bool IsPictureExporting => _pictureExportId is not null;
    /// <summary>
    /// Gets a value indicating whether use LocalGPT OCR applies to the picture editor state.
    /// </summary>
    /// <value>The can use LocalGPT OCR value exposed by <see cref="PictureEditor"/>.</value>
    private bool CanUseLocalGptOcr => Visible && LocalGptConnection.State.IsLinked && LocalGptConnection.State.HasCapability("localgpt.vision.ocr");
    /// <summary>
    /// Gets a value indicating whether OCR text applies to the picture editor state.
    /// </summary>
    /// <value>The has OCR text value exposed by <see cref="PictureEditor"/>.</value>
    private bool HasOcrText => !string.IsNullOrWhiteSpace(_ocrText);
    /// <summary>
    /// Gets the select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string SelectToolText => ToolText(PictureDrawTool.Select, "Select");
    /// <summary>
    /// Gets the brush tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brush tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string BrushToolText => ToolText(PictureDrawTool.Brush, "Brush");
    /// <summary>
    /// Gets the pencil tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pencil tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string PencilToolText => ToolText(PictureDrawTool.Pencil, "Pencil");
    /// <summary>
    /// Gets the spray tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The spray tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string SprayToolText => ToolText(PictureDrawTool.Spray, "Spray can");
    /// <summary>
    /// Gets the toothbrush tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The toothbrush tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string ToothbrushToolText => ToolText(PictureDrawTool.Toothbrush, "Toothbrush");
    /// <summary>
    /// Gets the square tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The square tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string SquareToolText => ToolText(PictureDrawTool.Square, "Square");
    /// <summary>
    /// Gets the rectangle tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rectangle tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string RectangleToolText => ToolText(PictureDrawTool.Rectangle, "Rectangle");
    /// <summary>
    /// Gets the ellipse tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ellipse tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string EllipseToolText => ToolText(PictureDrawTool.Ellipse, "Ellipse");
    /// <summary>
    /// Gets the arrow tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arrow tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string ArrowToolText => ToolText(PictureDrawTool.Arrow, "Arrow");
    /// <summary>
    /// Gets the line tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The line tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string LineToolText => ToolText(PictureDrawTool.Line, "Line");
    /// <summary>
    /// Gets the path tool text used by this picture editor instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string PathToolText => ToolText(PictureDrawTool.Path, "Path");
    /// <summary>
    /// Gets the eraser tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The eraser tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string EraserToolText => ToolText(PictureDrawTool.Eraser, "Eraser");
    /// <summary>
    /// Gets the eyedropper tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The eyedropper tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string EyedropperToolText => ToolText(PictureDrawTool.Eyedropper, "Eyedropper");
    /// <summary>
    /// Gets the rectangle select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rectangle select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string RectangleSelectToolText => ToolText(PictureDrawTool.RectangleSelect, "Rectangle select");
    /// <summary>
    /// Gets the ellipse select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ellipse select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string EllipseSelectToolText => ToolText(PictureDrawTool.EllipseSelect, "Ellipse select");
    /// <summary>
    /// Gets the free select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The free select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string FreeSelectToolText => ToolText(PictureDrawTool.FreeSelect, "Freehand select");
    /// <summary>
    /// Gets the magnetic select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The magnetic select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string MagneticSelectToolText => ToolText(PictureDrawTool.MagneticSelect, "Magnetic select");
    /// <summary>
    /// Gets the polygon select tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The polygon select tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string PolygonSelectToolText => ToolText(PictureDrawTool.PolygonSelect, "Polygon select");
    /// <summary>
    /// Gets the fill solid tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill solid tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string FillSolidToolText => ToolText(PictureDrawTool.FillSolid, "Solid fill");
    /// <summary>
    /// Gets the fill gradient tool text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fill gradient tool text value exposed by <see cref="PictureEditor"/>.</value>
    private string FillGradientToolText => ToolText(PictureDrawTool.FillGradient, "Gradient fill");
    /// <summary>
    /// Gets the brush width slider value value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brush width slider value value exposed by <see cref="PictureEditor"/>.</value>
    private double BrushWidthSliderValue => WidthToSlider(_drawWidth);
    /// <summary>
    /// Gets the brush width slider style value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The brush width slider style value exposed by <see cref="PictureEditor"/>.</value>
    private string BrushWidthSliderStyle => $"--picture-range-progress: {Inv(BrushWidthSliderValue)}%;";
    /// <summary>
    /// Gets the draw width display value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The draw width display value exposed by <see cref="PictureEditor"/>.</value>
    private string DrawWidthDisplay => $"{_drawWidth:0.##} px";
    /// <summary>
    /// Gets the canvas hint value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas hint value exposed by <see cref="PictureEditor"/>.</value>
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
    /// <summary>
    /// Gets the canvas color value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas color value exposed by <see cref="PictureEditor"/>.</value>
    private string CanvasColor => State.Document.Background.StartsWith('#') && State.Document.Background.Length is 4 or 7
        ? State.Document.Background
        : "#ffffff";
    /// <summary>
    /// Gets the status text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status text value exposed by <see cref="PictureEditor"/>.</value>
    private string StatusText => _error ?? _notice ?? (IsPictureExporting
        ? "Rendering PNG for the publication…"
        : _drawTool != PictureDrawTool.Select
            ? $"{_drawTool} tool · {_drawWidth:0.#} px · {_drawColor}"
            : State.SelectedLayer is null ? "No layer selected" : $"{State.SelectedLayer.Kind}: {State.SelectedLayer.Name}");

    /// <summary>
    /// Handles the initialized lifecycle or event notification for <see cref="PictureEditor"/>, updating the state required by the surrounding workflow.
    /// </summary>
    protected override void OnInitialized()
    {
        State.Changed += StateChanged;
        LocalGptConnection.Changed += LocalGptConnectionChanged;
    }

    /// <summary>
    /// Performs LocalGPT connection changed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LocalGptConnectionChanged() => _ = InvokeAsync(StateHasChanged);

    /// <summary>
    /// Handles the parameters set lifecycle or event notification for <see cref="PictureEditor"/>, updating the state required by the surrounding workflow.
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
    /// Handles the after render async lifecycle or event notification for <see cref="PictureEditor"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs state changed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void StateChanged()
    {
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Performs render canvas for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs picture layer selected for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    [JSInvokable]
    public void PictureLayerSelected(string? id)
    {
        State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
    }

    /// <summary>
    /// Performs picture transform committed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="x">X value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="rotation">Rotation value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureTransformCommitted(string id, double x, double y, double width, double height, double rotation)
    {
        if (Guid.TryParse(id, out var parsed))
            State.CommitTransform(parsed, x, y, width, height, rotation);
    }

    /// <summary>
    /// Performs picture stroke committed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="coordinates">Coordinates value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="color">Color value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="opacity">Opacity value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="hardness">Hardness value supplied to the picture editor operation and used when producing its result.</param>
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
    /// Performs picture shape committed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="rotation">Rotation value supplied to the picture editor operation and used when producing its result.</param>
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
    /// Performs picture path committed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="coordinates">Coordinates value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="closed">Value indicating whether closed should apply to this operation.</param>
    /// <param name="smooth">Value indicating whether smooth should apply to this operation.</param>
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
    /// Performs picture area fill committed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="selectionKind">Selection kind value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="coordinates">Coordinates value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="primaryColor">Primary color value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="secondaryColor">Secondary color value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="gradient">Value indicating whether gradient should apply to this operation.</param>
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
    /// Performs picture color picked for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="color">Color value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureColorPicked(string color)
    {
        if (!string.IsNullOrWhiteSpace(color)) _drawColor = color;
        _drawTool = PictureDrawTool.Brush;
        _renderRequested = true;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Performs picture shortcut requested for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="command">Command value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs picture render failed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureRenderFailed(string message)
    {
        _renderErrorActive = true;
        _error = string.IsNullOrWhiteSpace(message) ? "A picture layer could not be rendered." : message;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Performs picture render recovered for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Performs show canvas context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs show layer context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowLayerContextMenu(PictureLayer layer, MouseEventArgs args)
    {
        State.SelectLayer(layer.Id);
        await InvokeAsync(StateHasChanged);
        await _pictureContextMenu.ShowAsync(args);
    }

    /// <summary>
    /// Performs show layer list context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowLayerListContextMenu(MouseEventArgs args)
    {
        State.SelectLayer(null);
        await InvokeAsync(StateHasChanged);
        await _pictureContextMenu.ShowAsync(args);
    }

    /// <summary>
    /// Performs request image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestImage()
    {
        _replaceRasterLayerId = null;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId);
    }

    /// <summary>
    /// Performs request layered import for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestLayeredImport()
    {
        _replaceRasterLayerId = null;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.LayeredInputId);
    }

    /// <summary>
    /// Performs request raster replacement for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestRasterReplacement()
    {
        if (State.SelectedLayer is not RasterPictureLayer { Locked: false } raster) return;
        _replaceRasterLayerId = raster.Id;
        await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId);
    }

    /// <summary>
    /// Imports image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportImage(InputFileChangeEventArgs args) => ImportImageCore(args, forceAdd: false);

    /// <summary>
    /// Imports dropped image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportDroppedImage(InputFileChangeEventArgs args) => ImportImageCore(args, forceAdd: true);

    /// <summary>
    /// Imports image core for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <param name="forceAdd">Value indicating whether force add should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Imports layered document for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportLayeredDocument(InputFileChangeEventArgs args) => ImportLayeredDocumentCore(args, append: false);

    /// <summary>
    /// Imports dropped layered document for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportDroppedLayeredDocument(InputFileChangeEventArgs args) => ImportLayeredDocumentCore(args, append: true);

    /// <summary>
    /// Imports layered document core for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <param name="append">Value indicating whether append should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs picture studio file drop positioned for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="x">X value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureStudioFileDropPositioned(double? x, double? y)
    {
        _pendingDropX = x is double px && double.IsFinite(px) ? Math.Clamp(px, 0, State.Document.WidthPx) : null;
        _pendingDropY = y is double py && double.IsFinite(py) ? Math.Clamp(py, 0, State.Document.HeightPx) : null;
    }

    /// <summary>
    /// Performs clear pending drop position for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearPendingDropPosition()
    {
        _pendingDropX = null;
        _pendingDropY = null;
    }

    /// <summary>
    /// Performs picture studio file drop rejected for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
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
    /// Adds text layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddTextLayer() => State.AddText();
    /// <summary>
    /// Adds rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddRectangle() => State.AddShape(PictureShapeKind.Rectangle);
    /// <summary>
    /// Adds ellipse for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddEllipse() => State.AddShape(PictureShapeKind.Ellipse);
    /// <summary>
    /// Adds arrow shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddArrowShape() => State.AddShape(PictureShapeKind.Arrow);
    /// <summary>
    /// Adds line shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddLineShape() => State.AddShape(PictureShapeKind.Line);
    /// <summary>
    /// Adds gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddGradient() => State.AddFill(PictureFillKind.LinearGradient);
    /// <summary>
    /// Adds solid fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddSolidFill() => State.AddFill(PictureFillKind.Solid);
    /// <summary>
    /// Adds clouds for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddClouds() => State.AddRender(PictureRenderKind.Clouds);
    /// <summary>
    /// Adds noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddNoise() => State.AddRender(PictureRenderKind.Noise);
    /// <summary>
    /// Adds stripes for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddStripes() => State.AddRender(PictureRenderKind.Stripes);
    /// <summary>
    /// Adds vignette for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddVignette() => State.AddRender(PictureRenderKind.Vignette);
    /// <summary>
    /// Adds bloom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddBloom() => State.AddRender(PictureRenderKind.Bloom);
    /// <summary>
    /// Adds neon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddNeon() => State.AddRender(PictureRenderKind.Neon);
    /// <summary>
    /// Adds lens flare for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddLensFlare() => State.AddRender(PictureRenderKind.LensFlare);
    /// <summary>
    /// Adds grain noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddGrainNoise() => State.AddRender(PictureRenderKind.GrainNoise);
    /// <summary>
    /// Adds motion blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddMotionBlur() => State.AddRender(PictureRenderKind.MotionBlur);
    /// <summary>
    /// Adds wind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddWind() => State.AddRender(PictureRenderKind.Wind);
    /// <summary>
    /// Adds ocean waves for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddOceanWaves() => State.AddRender(PictureRenderKind.OceanWaves);
    /// <summary>
    /// Adds paint layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddPaintLayer() => State.AddPaint();
    /// <summary>
    /// Performs move up for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoveUp() => State.MoveSelectedLayer(1);
    /// <summary>
    /// Performs move down for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoveDown() => State.MoveSelectedLayer(-1);
    /// <summary>
    /// Performs zoom100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Zoom100() => State.SetZoom(1);

    /// <summary>
    /// Performs fit canvas for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task FitCanvas()
    {
        if (_module is null) return;
        var zoom = await _module.InvokeAsync<double>("fitPictureStudio", RuntimePolicy.PictureStudio.CanvasHostId, State.Document.WidthPx, State.Document.HeightPx);
        State.SetZoom(zoom);
    }

    /// <summary>
    /// Performs apply for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs begin picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <param name="totalLength">Total length value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="chunkCount">Chunk count value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs append picture export chunk for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <param name="chunkIndex">Chunk index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="chunk">Chunk value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Completes picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs fail picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void FailPictureExport(string exportId, string? message)
    {
        if (!IsCurrentPictureExport(exportId)) return;
        ResetPictureExport();
        _error = string.IsNullOrWhiteSpace(message) ? "The browser could not render the picture." : message;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Determines whether current picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsCurrentPictureExport(string exportId) =>
        _pictureExportId is not null &&
        string.Equals(_pictureExportId, exportId, StringComparison.Ordinal);

    /// <summary>
    /// Performs reset picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Starts LocalGPT OCR for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs request LocalGPT OCR for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs insert OCR text layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Performs clear OCR text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearOcrText()
    {
        _ocrText = string.Empty;
        _ocrStatus = string.Empty;
    }

    /// <summary>
    /// Reads wire string for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadWireString(OrganicWireEnvelope envelope, string key)
    {
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(key, out var value)) return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
    }

    /// <summary>
    /// Performs download png for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadPng() => await Download("image/png", "png", 1d);
    /// <summary>
    /// Performs download jpeg for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadJpeg() => await Download("image/jpeg", "jpg", .92d);
    /// <summary>
    /// Performs download SVG for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadSvg()
    {
        if (_module is null) return;
        var fileName = $"{Files.SafeFileName(State.Document.Name)}.svg";
        await _module.InvokeVoidAsync("downloadPictureStudioSvg", State.Document, fileName);
    }

    /// <summary>
    /// Performs download for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="mimeType">Mime type value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="extension">Extension value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="quality">Quality value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Determines whether cel for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task Cancel()
    {
        await CancelPictureInteractionAsync();
        await DisposePictureRuntimeAsync();
        await Cancelled.InvokeAsync();
    }

    /// <summary>
    /// Performs select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SelectTool() => SetDrawTool(PictureDrawTool.Select);
    /// <summary>
    /// Performs brush tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void BrushTool() => SetDrawTool(PictureDrawTool.Brush);
    /// <summary>
    /// Performs pencil tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PencilTool() => SetDrawTool(PictureDrawTool.Pencil);
    /// <summary>
    /// Performs spray tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SprayTool() => SetDrawTool(PictureDrawTool.Spray);
    /// <summary>
    /// Performs toothbrush tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToothbrushTool() => SetDrawTool(PictureDrawTool.Toothbrush);
    /// <summary>
    /// Performs square tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SquareTool() => SetDrawTool(PictureDrawTool.Square);
    /// <summary>
    /// Performs rectangle tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RectangleTool() => SetDrawTool(PictureDrawTool.Rectangle);
    /// <summary>
    /// Performs ellipse tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EllipseTool() => SetDrawTool(PictureDrawTool.Ellipse);
    /// <summary>
    /// Performs arrow tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ArrowTool() => SetDrawTool(PictureDrawTool.Arrow);
    /// <summary>
    /// Performs line tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LineTool() => SetDrawTool(PictureDrawTool.Line);
    /// <summary>
    /// Performs path tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PathTool() => SetDrawTool(PictureDrawTool.Path);
    /// <summary>
    /// Performs eraser tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EraserTool() => SetDrawTool(PictureDrawTool.Eraser);
    /// <summary>
    /// Performs eyedropper tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EyedropperTool() => SetDrawTool(PictureDrawTool.Eyedropper);
    /// <summary>
    /// Performs rectangle select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RectangleSelectTool() => SetDrawTool(PictureDrawTool.RectangleSelect);
    /// <summary>
    /// Performs ellipse select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EllipseSelectTool() => SetDrawTool(PictureDrawTool.EllipseSelect);
    /// <summary>
    /// Performs free select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FreeSelectTool() => SetDrawTool(PictureDrawTool.FreeSelect);
    /// <summary>
    /// Performs magnetic select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MagneticSelectTool() => SetDrawTool(PictureDrawTool.MagneticSelect);
    /// <summary>
    /// Performs polygon select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PolygonSelectTool() => SetDrawTool(PictureDrawTool.PolygonSelect);
    /// <summary>
    /// Performs fill solid tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillSolidTool() => SetDrawTool(PictureDrawTool.FillSolid);
    /// <summary>
    /// Performs fill gradient tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillGradientTool() => SetDrawTool(PictureDrawTool.FillGradient);
    /// <summary>
    /// Performs clear area selection for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ClearAreaSelection()
    {
        if (_module is not null) await _module.InvokeVoidAsync("clearPictureStudioAreaSelection", RuntimePolicy.PictureStudio.CanvasId);
        _renderRequested = true;
    }

    /// <summary>
    /// Reads area selection for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>The picture area selection produced by the operation.</returns>
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
    /// Performs selection polygon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="selection">Selection value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Applies area clip for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="inverted">Value indicating whether inverted should apply to this operation.</param>
    /// <param name="quietWhenMissing">Value indicating whether quiet when missing should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs keep selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task KeepSelectedArea() => ApplyAreaClipAsync(inverted: false);
    /// <summary>
    /// Performs cut selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task CutSelectedArea() => ApplyAreaClipAsync(inverted: true);

    /// <summary>
    /// Performs copy area selection to clipboard for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> CopyAreaSelectionToClipboardAsync()
    {
        var selection = await ReadAreaSelectionAsync();
        var polygon = selection is null ? [] : SelectionPolygon(selection);
        if (polygon.Count < 3 || !State.CopySelectedRegion(polygon)) return false;
        _notice = "Selected picture region copied. Paste inserts it as an independently editable clipped layer.";
        return true;
    }

    /// <summary>
    /// Performs copy selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CopySelectedArea()
    {
        if (!await CopyAreaSelectionToClipboardAsync())
            _notice = "Create an area selection on a layer before copying a region.";
    }

    /// <summary>
    /// Performs copy selected area as layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs clear layer cut for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearLayerCut()
    {
        if (State.ClearSelectedClip()) _notice = "The layer cut was cleared.";
    }

    /// <summary>
    /// Performs distance for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Distance(PicturePoint first, PicturePoint second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }
    /// <summary>
    /// Sets draw tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    private void SetDrawTool(PictureDrawTool tool)
    {
        _ = CancelPictureInteractionAsync();
        _drawTool = tool;
        _renderRequested = true;
        StateHasChanged();
    }

    /// <summary>
    /// Determines whether cel picture interaction for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs dispose picture runtime for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs tool text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ToolText(PictureDrawTool tool, string text) => _drawTool == tool ? $"✓ {text}" : text;
    /// <summary>
    /// Determines whether draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsDrawWidth(double value) => Math.Abs(_drawWidth - value) < .001;
    /// <summary>
    /// Performs draw width text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DrawWidthText(double value) => IsDrawWidth(value) ? $"✓ {value:0.##} px" : $"{value:0.##} px";
    /// <summary>
    /// Performs draw width button class for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DrawWidthButtonClass(double value) => IsDrawWidth(value) ? "selected" : string.Empty;
    /// <summary>
    /// Performs change draw color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawColor(string value) { if (!string.IsNullOrWhiteSpace(value)) _drawColor = value; _renderRequested = true; }
    /// <summary>
    /// Performs change draw secondary color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawSecondaryColor(string value) { if (!string.IsNullOrWhiteSpace(value)) _drawSecondaryColor = value; _renderRequested = true; }
    /// <summary>
    /// Sets draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetDrawWidth(double value)
    {
        _drawWidth = Math.Clamp(value, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
        _renderRequested = true;
    }
    /// <summary>
    /// Performs width to slider for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double WidthToSlider(double width)
    {
        var clamped = Math.Clamp(width, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
        return Math.Log(clamped / RuntimePolicy.PictureStudio.MinimumDrawWidth) / Math.Log(RuntimePolicy.PictureStudio.MaximumDrawWidth / RuntimePolicy.PictureStudio.MinimumDrawWidth) * 100;
    }
    /// <summary>
    /// Performs slider to width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="slider">Slider value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
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
    /// Performs draw width1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth1() => SetDrawWidth(1);
    /// <summary>
    /// Performs draw width3 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth3() => SetDrawWidth(3);
    /// <summary>
    /// Performs draw width8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth8() => SetDrawWidth(8);
    /// <summary>
    /// Performs draw width16 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth16() => SetDrawWidth(16);
    /// <summary>
    /// Performs draw width32 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth32() => SetDrawWidth(32);
    /// <summary>
    /// Performs toggle grid ribbon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleGridRibbon() => State.SetGrid(!State.Document.GridVisible);
    /// <summary>
    /// Performs toggle snap ribbon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSnapRibbon() => State.SetSnap(!State.Document.SnapToGrid);
    /// <summary>
    /// Gets the grid text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grid text value exposed by <see cref="PictureEditor"/>.</value>
    private string GridText => State.Document.GridVisible ? "✓ Grid" : "Grid";
    /// <summary>
    /// Gets the snap text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snap text value exposed by <see cref="PictureEditor"/>.</value>
    private string SnapText => State.Document.SnapToGrid ? "✓ Snap" : "Snap";
    /// <summary>
    /// Performs make render clouds for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderClouds() => WithRender(layer => layer.RenderKind = PictureRenderKind.Clouds);
    /// <summary>
    /// Performs make render noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderNoise() => WithRender(layer => layer.RenderKind = PictureRenderKind.Noise);
    /// <summary>
    /// Performs make render stripes for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderStripes() => WithRender(layer => layer.RenderKind = PictureRenderKind.Stripes);
    /// <summary>
    /// Performs make render vignette for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderVignette() => WithRender(layer => layer.RenderKind = PictureRenderKind.Vignette);
    /// <summary>
    /// Performs make render bloom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderBloom() => WithRender(layer => layer.RenderKind = PictureRenderKind.Bloom);
    /// <summary>
    /// Performs make render neon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderNeon() => WithRender(layer => layer.RenderKind = PictureRenderKind.Neon);
    /// <summary>
    /// Performs make render lens flare for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderLensFlare() => WithRender(layer => layer.RenderKind = PictureRenderKind.LensFlare);
    /// <summary>
    /// Performs make render grain noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderGrainNoise() => WithRender(layer => layer.RenderKind = PictureRenderKind.GrainNoise);
    /// <summary>
    /// Performs make render motion blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderMotionBlur() => WithRender(layer => layer.RenderKind = PictureRenderKind.MotionBlur);
    /// <summary>
    /// Performs make render wind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderWind() => WithRender(layer => layer.RenderKind = PictureRenderKind.Wind);
    /// <summary>
    /// Performs make render ocean waves for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderOceanWaves() => WithRender(layer => layer.RenderKind = PictureRenderKind.OceanWaves);
    /// <summary>
    /// Performs raster contain for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterContain() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Contain);
    /// <summary>
    /// Performs raster cover for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterCover() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Cover);
    /// <summary>
    /// Performs raster stretch for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterStretch() => WithRaster(layer => layer.FitMode = PictureRasterFitMode.Stretch);
    /// <summary>
    /// Performs raster flip horizontal for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterFlipHorizontal() => WithRaster(layer => layer.FlipHorizontal = !layer.FlipHorizontal);
    /// <summary>
    /// Performs raster flip vertical for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterFlipVertical() => WithRaster(layer => layer.FlipVertical = !layer.FlipVertical);
    /// <summary>
    /// Performs raster rotate left for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterRotateLeft() => WithRaster(layer => layer.Rotation = (layer.Rotation - 90 + 360) % 360);
    /// <summary>
    /// Performs raster rotate right for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterRotateRight() => WithRaster(layer => layer.Rotation = (layer.Rotation + 90) % 360);
    /// <summary>
    /// Performs raster reset rotation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterResetRotation() => WithRaster(layer => layer.Rotation = 0);
    /// <summary>
    /// Performs raster no tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterNoTint() => WithRaster(layer => layer.TintOpacity = 0);
    /// <summary>
    /// Performs raster blue tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterBlueTint() => WithRaster(layer => { layer.TintColor = "#2563eb"; layer.TintOpacity = .28; });
    /// <summary>
    /// Performs raster warm tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterWarmTint() => WithRaster(layer => { layer.TintColor = "#f97316"; layer.TintOpacity = .24; });
    /// <summary>
    /// Performs soften light for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SoftenLight() => State.UpdateSelected(layer => layer.Blur = 2);
    /// <summary>
    /// Performs soften medium for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SoftenMedium() => State.UpdateSelected(layer => layer.Blur = 6);
    /// <summary>
    /// Removes softening for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RemoveSoftening() => State.UpdateSelected(layer => layer.Blur = 0);
    /// <summary>
    /// Performs brighten for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Brighten() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness + .1, 0, 3));
    /// <summary>
    /// Performs darken for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Darken() => State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness - .1, 0, 3));
    /// <summary>
    /// Performs more contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoreContrast() => State.UpdateSelected(layer => layer.Contrast = Math.Clamp(layer.Contrast + .1, 0, 3));
    /// <summary>
    /// Performs more saturation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoreSaturation() => State.UpdateSelected(layer => layer.Saturation = Math.Clamp(layer.Saturation + .1, 0, 3));
    /// <summary>
    /// Performs toggle grayscale preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleGrayscalePreset() => State.UpdateSelected(layer => layer.Grayscale = layer.Grayscale > .5 ? 0 : 1);
    /// <summary>
    /// Performs toggle sepia preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSepiaPreset() => State.UpdateSelected(layer => layer.Sepia = layer.Sepia > .5 ? 0 : 1);
    /// <summary>
    /// Performs toggle invert preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleInvertPreset() => State.UpdateSelected(layer => layer.Invert = layer.Invert > .5 ? 0 : 1);
    /// <summary>
    /// Applies bloom effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Applies neon effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Applies lens flare effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Performs shape rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeRectangle() => WithShape(layer => layer.Shape = PictureShapeKind.Rectangle);
    /// <summary>
    /// Performs shape rounded rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeRoundedRectangle() => WithShape(layer => layer.Shape = PictureShapeKind.RoundedRectangle);
    /// <summary>
    /// Performs shape ellipse for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeEllipse() => WithShape(layer => layer.Shape = PictureShapeKind.Ellipse);
    /// <summary>
    /// Performs shape arrow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeArrow() => WithShape(layer => layer.Shape = PictureShapeKind.Arrow);
    /// <summary>
    /// Performs shape line for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeLine() => WithShape(layer => layer.Shape = PictureShapeKind.Line);
    /// <summary>
    /// Performs shape path for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapePath() => WithShape(layer => layer.Shape = PictureShapeKind.Path);
    /// <summary>
    /// Performs fill solid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillSolid() => WithFill(layer => layer.FillKind = PictureFillKind.Solid);
    /// <summary>
    /// Performs fill linear gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillLinearGradient() => WithFill(layer => layer.FillKind = PictureFillKind.LinearGradient);
    /// <summary>
    /// Performs fill radial gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillRadialGradient() => WithFill(layer => layer.FillKind = PictureFillKind.RadialGradient);
    /// <summary>
    /// Sets picture text font for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="font">Font value supplied to the picture editor operation and used when producing its result.</param>
    private void SetPictureTextFont(string font) => WithText(layer => layer.FontFamily = font);
    /// <summary>
    /// Sets picture text size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetPictureTextSize(double value) => WithText(layer => layer.FontSizePx = value);
    /// <summary>
    /// Performs text size24 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize24() => SetPictureTextSize(24);
    /// <summary>
    /// Performs text size48 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize48() => SetPictureTextSize(48);
    /// <summary>
    /// Performs text size72 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize72() => SetPictureTextSize(72);
    /// <summary>
    /// Performs text size120 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize120() => SetPictureTextSize(120);
    /// <summary>
    /// Performs text size180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize180() => SetPictureTextSize(180);
    /// <summary>
    /// Performs toggle picture text bold for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextBold() => WithText(layer => layer.Bold = !layer.Bold);
    /// <summary>
    /// Performs toggle picture text italic for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextItalic() => WithText(layer => layer.Italic = !layer.Italic);
    /// <summary>
    /// Performs toggle picture text shadow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextShadow() => WithText(layer => layer.ShadowEnabled = !layer.ShadowEnabled);
    /// <summary>
    /// Performs text align left for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignLeft() => WithText(layer => layer.Alignment = PictureTextAlignment.Left);
    /// <summary>
    /// Performs text align center for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignCenter() => WithText(layer => layer.Alignment = PictureTextAlignment.Center);
    /// <summary>
    /// Performs text align right for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignRight() => WithText(layer => layer.Alignment = PictureTextAlignment.Right);
    /// <summary>
    /// Performs text color blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorBlue() => WithText(layer => layer.FillColor = "#17365d");
    /// <summary>
    /// Performs text color black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorBlack() => WithText(layer => layer.FillColor = "#000000");
    /// <summary>
    /// Performs text color white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorWhite() => WithText(layer => layer.FillColor = "#ffffff");
    /// <summary>
    /// Performs text color red for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorRed() => WithText(layer => layer.FillColor = "#dc2626");
    /// <summary>
    /// Performs text outline none for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineNone() => WithText(layer => { layer.OutlineColor = "transparent"; layer.OutlineWidthPx = 0; });
    /// <summary>
    /// Performs text outline thin for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineThin() => WithText(layer => { layer.OutlineColor = "#111827"; layer.OutlineWidthPx = 1; });
    /// <summary>
    /// Performs text outline thick for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineThick() => WithText(layer => { layer.OutlineColor = "#ffffff"; layer.OutlineWidthPx = 4; });
    /// <summary>
    /// Performs shape fill solid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillSolid() => WithShape(layer => layer.FillKind = PictureFillKind.Solid);
    /// <summary>
    /// Performs shape fill linear for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillLinear() => WithShape(layer => layer.FillKind = PictureFillKind.LinearGradient);
    /// <summary>
    /// Performs shape fill radial for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillRadial() => WithShape(layer => layer.FillKind = PictureFillKind.RadialGradient);
    /// <summary>
    /// Sets shape colors for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="stroke">Stroke value supplied to the picture editor operation and used when producing its result.</param>
    private void SetShapeColors(string first, string second, string stroke) => WithShape(layer => { layer.FillColor = first; layer.SecondaryFillColor = second; layer.StrokeColor = stroke; });
    /// <summary>
    /// Performs shape colors blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsBlue() => SetShapeColors("#60a5fa", "#dbeafe", "#1d4ed8");
    /// <summary>
    /// Performs shape colors green for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsGreen() => SetShapeColors("#4ade80", "#dcfce7", "#15803d");
    /// <summary>
    /// Performs shape colors orange for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsOrange() => SetShapeColors("#fb923c", "#ffedd5", "#c2410c");
    /// <summary>
    /// Performs shape colors mono for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsMono() => SetShapeColors("#111827", "#ffffff", "#000000");
    /// <summary>
    /// Sets shape stroke for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    private void SetShapeStroke(double width) => WithShape(layer => layer.StrokeWidthPx = width);
    /// <summary>
    /// Performs shape stroke0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke0() => SetShapeStroke(0);
    /// <summary>
    /// Performs shape stroke1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke1() => SetShapeStroke(1);
    /// <summary>
    /// Performs shape stroke3 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke3() => SetShapeStroke(3);
    /// <summary>
    /// Performs shape stroke8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke8() => SetShapeStroke(8);
    /// <summary>
    /// Sets fill colors for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    private void SetFillColors(string first, string second) => WithFill(layer => { layer.PrimaryColor = first; layer.SecondaryColor = second; });
    /// <summary>
    /// Performs fill colors blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsBlue() => SetFillColors("#dbeafe", "#6366f1");
    /// <summary>
    /// Performs fill colors green for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsGreen() => SetFillColors("#dcfce7", "#16a34a");
    /// <summary>
    /// Performs fill colors sunset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsSunset() => SetFillColors("#fde68a", "#f97316");
    /// <summary>
    /// Performs fill colors mono for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsMono() => SetFillColors("#ffffff", "#111827");
    /// <summary>
    /// Sets fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetFillAngle(double value) => WithFill(layer => layer.AngleDegrees = value);
    /// <summary>
    /// Performs fill angle0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle0() => SetFillAngle(0);
    /// <summary>
    /// Performs fill angle45 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle45() => SetFillAngle(45);
    /// <summary>
    /// Performs fill angle90 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle90() => SetFillAngle(90);
    /// <summary>
    /// Performs fill angle180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle180() => SetFillAngle(180);
    /// <summary>
    /// Performs fill angle270 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle270() => SetFillAngle(270);
    /// <summary>
    /// Sets layer opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetLayerOpacity(double value) => State.UpdateSelected(layer => layer.Opacity = value);
    /// <summary>
    /// Performs layer opacity100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity100() => SetLayerOpacity(1);
    /// <summary>
    /// Performs layer opacity75 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity75() => SetLayerOpacity(.75);
    /// <summary>
    /// Performs layer opacity50 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity50() => SetLayerOpacity(.5);
    /// <summary>
    /// Performs layer opacity25 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity25() => SetLayerOpacity(.25);
    /// <summary>
    /// Performs toggle selected lock menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSelectedLockMenu()
    {
        if (State.SelectedLayer is PictureLayer layer) State.ToggleLock(layer.Id);
    }
    /// <summary>
    /// Performs toggle selected visibility menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSelectedVisibilityMenu()
    {
        if (State.SelectedLayer is PictureLayer layer) State.ToggleVisibility(layer.Id);
    }
    /// <summary>
    /// Performs checked text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="selected">Value indicating whether selected should apply to this operation.</param>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CheckedText(bool selected, string text) => selected ? $"✓ {text}" : text;

    /// <summary>
    /// Performs change document name for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDocumentName(ChangeEventArgs args) => State.SetDocumentName(Text(args));
    /// <summary>
    /// Performs change canvas width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasWidth(ChangeEventArgs args) => State.SetDocumentSize(Int(args, State.Document.WidthPx), State.Document.HeightPx);
    /// <summary>
    /// Performs change canvas height for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasHeight(ChangeEventArgs args) => State.SetDocumentSize(State.Document.WidthPx, Int(args, State.Document.HeightPx));
    /// <summary>
    /// Performs change background preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBackgroundPreset(ChangeEventArgs args) => State.SetBackground(Text(args));
    /// <summary>
    /// Performs change canvas color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasColor(ChangeEventArgs args) => State.SetBackground(Text(args));
    /// <summary>
    /// Performs change grid spacing for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeGridSpacing(ChangeEventArgs args) => State.SetGridSpacing(Int(args, State.Document.GridSpacingPx));
    /// <summary>
    /// Performs toggle grid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleGrid(ChangeEventArgs args) => State.SetGrid(Bool(args));
    /// <summary>
    /// Performs toggle snap for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSnap(ChangeEventArgs args) => State.SetSnap(Bool(args));
    /// <summary>
    /// Performs change zoom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeZoom(ChangeEventArgs args) => State.SetZoom(Number(args, State.Document.Zoom));
    /// <summary>
    /// Performs change draw tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawTool(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureDrawTool>(Text(args), true, out var tool)) SetDrawTool(tool);
    }
    /// <summary>
    /// Performs change draw color input for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawColorInput(ChangeEventArgs args) => ChangeDrawColor(Text(args));
    /// <summary>
    /// Performs change draw secondary color input for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawSecondaryColorInput(ChangeEventArgs args) => ChangeDrawSecondaryColor(Text(args));
    /// <summary>
    /// Performs change draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawWidth(ChangeEventArgs args) => SetDrawWidth(Number(args, _drawWidth));
    /// <summary>
    /// Performs change draw width slider for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawWidthSlider(ChangeEventArgs args) => SetDrawWidth(SliderToWidth(Number(args, BrushWidthSliderValue)));
    /// <summary>
    /// Performs change draw opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawOpacity(ChangeEventArgs args) { _drawOpacity = Math.Clamp(Number(args, _drawOpacity), 0, 1); _renderRequested = true; }
    /// <summary>
    /// Performs change draw hardness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawHardness(ChangeEventArgs args) { _drawHardness = Math.Clamp(Number(args, _drawHardness), 0, 1); _renderRequested = true; }

    /// <summary>
    /// Performs preset square for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetSquare() => State.SetDocumentSize(1200, 1200);
    /// <summary>
    /// Performs preset landscape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetLandscape() => State.SetDocumentSize(1600, 1000);
    /// <summary>
    /// Performs preset full hd for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetFullHd() => State.SetDocumentSize(1920, 1080);
    /// <summary>
    /// Performs preset a4 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetA4() => State.SetDocumentSize(2480, 3508);

    /// <summary>
    /// Performs change layer name for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerName(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Name = Text(args));
    /// <summary>
    /// Performs change layer x for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerX(ChangeEventArgs args) => State.UpdateSelected(layer => layer.X = Number(args, layer.X));
    /// <summary>
    /// Performs change layer y for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerY(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Y = Number(args, layer.Y));
    /// <summary>
    /// Performs change layer width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerWidth(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Width = Number(args, layer.Width));
    /// <summary>
    /// Performs change layer height for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerHeight(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Height = Number(args, layer.Height));
    /// <summary>
    /// Performs change layer rotation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerRotation(ChangeEventArgs args) => State.UpdateSelectedLive("layer-rotation", layer => layer.Rotation = Number(args, layer.Rotation));
    /// <summary>
    /// Performs change layer opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerOpacity(ChangeEventArgs args) => State.UpdateSelectedLive("layer-opacity", layer => layer.Opacity = Number(args, layer.Opacity));
    /// <summary>
    /// Performs change blend mode for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBlendMode(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureBlendMode>(Text(args), true, out var value))
            State.UpdateSelected(layer => layer.BlendMode = value);
    }
    /// <summary>
    /// Performs toggle selected visibility for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSelectedVisibility(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Visible = Bool(args), allowLocked: true);
    /// <summary>
    /// Performs toggle selected lock for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSelectedLock(ChangeEventArgs args) => State.UpdateSelected(layer => layer.Locked = Bool(args), allowLocked: true);
    /// <summary>
    /// Performs end live edit for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>The void end live edit change event args state produced by the operation.</returns>
    /// <param name="_">_ value supplied to the picture editor operation and used when producing its result.</param>
    private void EndLiveEdit(ChangeEventArgs _) => State.EndLiveEdit();

    /// <summary>
    /// Performs change raster fit for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterFit(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRasterFitMode>(Text(args), true, out var value))
            WithRaster(layer => layer.FitMode = value);
    }
    /// <summary>
    /// Performs toggle raster flip horizontal for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleRasterFlipHorizontal(ChangeEventArgs args) => WithRaster(layer => layer.FlipHorizontal = Bool(args));
    /// <summary>
    /// Performs toggle raster flip vertical for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleRasterFlipVertical(ChangeEventArgs args) => WithRaster(layer => layer.FlipVertical = Bool(args));
    /// <summary>
    /// Performs change raster tint color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterTintColor(ChangeEventArgs args) => WithRaster(layer => layer.TintColor = Text(args));
    /// <summary>
    /// Performs change raster tint opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterTintOpacity(ChangeEventArgs args) => WithRasterLive("raster-tint", layer => layer.TintOpacity = Number(args, layer.TintOpacity));

    /// <summary>
    /// Performs change text content for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextContent(ChangeEventArgs args) => WithText(layer => layer.Text = Text(args));
    /// <summary>
    /// Performs change text font for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextFont(ChangeEventArgs args) => WithText(layer => layer.FontFamily = Text(args));
    /// <summary>
    /// Performs change text size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextSize(ChangeEventArgs args) => WithText(layer => layer.FontSizePx = Number(args, layer.FontSizePx));
    /// <summary>
    /// Performs change text alignment for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextAlignment(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureTextAlignment>(Text(args), true, out var value))
            WithText(layer => layer.Alignment = value);
    }
    /// <summary>
    /// Performs toggle text bold for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextBold(ChangeEventArgs args) => WithText(layer => layer.Bold = Bool(args));
    /// <summary>
    /// Performs toggle text italic for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextItalic(ChangeEventArgs args) => WithText(layer => layer.Italic = Bool(args));
    /// <summary>
    /// Performs toggle text shadow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextShadow(ChangeEventArgs args) => WithText(layer => layer.ShadowEnabled = Bool(args));
    /// <summary>
    /// Performs change text fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextFill(ChangeEventArgs args) => WithText(layer => layer.FillColor = Text(args));
    /// <summary>
    /// Performs change text outline for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextOutline(ChangeEventArgs args) => WithText(layer => layer.OutlineColor = Text(args));
    /// <summary>
    /// Performs change text outline width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextOutlineWidth(ChangeEventArgs args) => WithText(layer => layer.OutlineWidthPx = Number(args, layer.OutlineWidthPx));
    /// <summary>
    /// Performs change text shadow blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextShadowBlur(ChangeEventArgs args) => WithText(layer => layer.ShadowBlurPx = Number(args, layer.ShadowBlurPx));

    /// <summary>
    /// Performs change shape kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureShapeKind>(Text(args), true, out var value))
            WithShape(layer => layer.Shape = value);
    }
    /// <summary>
    /// Performs change shape fill kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFillKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value)) WithShape(layer => layer.FillKind = value);
    }
    /// <summary>
    /// Performs change shape fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFill(ChangeEventArgs args) => WithShape(layer => layer.FillColor = Text(args));
    /// <summary>
    /// Performs change shape secondary fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeSecondaryFill(ChangeEventArgs args) => WithShape(layer => layer.SecondaryFillColor = Text(args));
    /// <summary>
    /// Performs change shape fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFillAngle(ChangeEventArgs args) => State.UpdateSelectedLive("shape-fill-angle", layer => { if (layer is ShapePictureLayer shape) shape.FillAngleDegrees = Number(args, shape.FillAngleDegrees); });
    /// <summary>
    /// Performs change shape stroke for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeStroke(ChangeEventArgs args) => WithShape(layer => layer.StrokeColor = Text(args));
    /// <summary>
    /// Performs change shape stroke width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeStrokeWidth(ChangeEventArgs args) => WithShape(layer => layer.StrokeWidthPx = Number(args, layer.StrokeWidthPx));
    /// <summary>
    /// Performs change shape radius for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeRadius(ChangeEventArgs args) => WithShape(layer => layer.CornerRadiusPx = Number(args, layer.CornerRadiusPx));
    /// <summary>
    /// Performs toggle shape path closed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleShapePathClosed(ChangeEventArgs args) => WithShape(layer => layer.PathClosed = Bool(args));
    /// <summary>
    /// Performs toggle shape path smooth for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleShapePathSmooth(ChangeEventArgs args) => WithShape(layer => layer.PathSmooth = Bool(args));
    /// <summary>
    /// Adds shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Removes shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    private void RemoveShapePathPoint(int index) => WithShape(layer =>
    {
        if (layer.PathPoints is { Count: > 2 } && index >= 0 && index < layer.PathPoints.Count)
            layer.PathPoints.RemoveAt(index);
    });
    /// <summary>
    /// Performs reverse shape path for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ReverseShapePath() => WithShape(layer => { layer.PathPoints?.Reverse(); });
    /// <summary>
    /// Performs change shape path point x for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapePathPointX(int index, ChangeEventArgs args) => ChangeShapePathPoint(index, args, true);
    /// <summary>
    /// Performs change shape path point y for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapePathPointY(int index, ChangeEventArgs args) => ChangeShapePathPoint(index, args, false);
    /// <summary>
    /// Performs change shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="horizontal">Value indicating whether horizontal should apply to this operation.</param>
    private void ChangeShapePathPoint(int index, ChangeEventArgs args, bool horizontal) => WithShape(layer =>
    {
        if (layer.PathPoints is null || index < 0 || index >= layer.PathPoints.Count) return;
        var point = layer.PathPoints[index];
        if (horizontal) point.X = Math.Clamp(Number(args, point.X), -16384, 32768);
        else point.Y = Math.Clamp(Number(args, point.Y), -16384, 32768);
    });

    /// <summary>
    /// Performs change fill kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value))
            WithFill(layer => layer.FillKind = value);
    }
    /// <summary>
    /// Performs change fill primary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillPrimary(ChangeEventArgs args) => WithFill(layer => layer.PrimaryColor = Text(args));
    /// <summary>
    /// Performs change fill secondary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillSecondary(ChangeEventArgs args) => WithFill(layer => layer.SecondaryColor = Text(args));
    /// <summary>
    /// Performs change fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillAngle(ChangeEventArgs args) => WithFillLive("fill-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));

    /// <summary>
    /// Performs change render kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderKind(ChangeEventArgs args)
    {
        if (Enum.TryParse<PictureRenderKind>(Text(args), true, out var value))
            WithRender(layer => layer.RenderKind = value);
    }
    /// <summary>
    /// Performs change render primary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderPrimary(ChangeEventArgs args) => WithRender(layer => layer.PrimaryColor = Text(args));
    /// <summary>
    /// Performs change render secondary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSecondary(ChangeEventArgs args) => WithRender(layer => layer.SecondaryColor = Text(args));
    /// <summary>
    /// Performs change render seed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSeed(ChangeEventArgs args) => WithRender(layer => layer.Seed = Int(args, layer.Seed));
    /// <summary>
    /// Performs change render scale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderScale(ChangeEventArgs args) => WithRender(layer => layer.Scale = Number(args, layer.Scale));
    /// <summary>
    /// Performs change render detail for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderDetail(ChangeEventArgs args) => WithRender(layer => layer.Detail = Int(args, layer.Detail));
    /// <summary>
    /// Performs change render softness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSoftness(ChangeEventArgs args) => WithRender(layer => layer.Softness = Number(args, layer.Softness));
    /// <summary>
    /// Performs change render contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderContrast(ChangeEventArgs args) => WithRender(layer => layer.RenderContrast = Number(args, layer.RenderContrast));
    /// <summary>
    /// Performs change render stripe width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderStripeWidth(ChangeEventArgs args) => WithRender(layer => layer.StripeWidthPx = Number(args, layer.StripeWidthPx));
    /// <summary>
    /// Performs change render angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderAngle(ChangeEventArgs args) => WithRenderLive("render-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));
    /// <summary>
    /// Performs randomize render for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RandomizeRender() => WithRender(layer => layer.Seed = Random.Shared.Next(1, int.MaxValue));
    /// <summary>
    /// Performs focus render properties for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task FocusRenderProperties() => JS.InvokeVoidAsync("publisherStudio.focusElement", "picture-render-properties").AsTask();
    /// <summary>
    /// Performs render primary white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryWhite() => WithRender(layer => layer.PrimaryColor = "#ffffff");
    /// <summary>
    /// Performs render primary black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryBlack() => WithRender(layer => layer.PrimaryColor = "#000000");
    /// <summary>
    /// Performs render primary blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryBlue() => WithRender(layer => layer.PrimaryColor = "#2563eb");
    /// <summary>
    /// Performs render secondary white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryWhite() => WithRender(layer => layer.SecondaryColor = "#ffffff");
    /// <summary>
    /// Performs render secondary black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryBlack() => WithRender(layer => layer.SecondaryColor = "#000000");
    /// <summary>
    /// Performs render secondary blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryBlue() => WithRender(layer => layer.SecondaryColor = "#60a5fa");
    /// <summary>
    /// Sets render scale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderScale(double value) => WithRender(layer => layer.Scale = value);
    /// <summary>
    /// Performs render scale24 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale24() => SetRenderScale(24);
    /// <summary>
    /// Performs render scale64 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale64() => SetRenderScale(64);
    /// <summary>
    /// Performs render scale128 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale128() => SetRenderScale(128);
    /// <summary>
    /// Performs render scale256 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale256() => SetRenderScale(256);
    /// <summary>
    /// Sets render detail for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderDetail(int value) => WithRender(layer => layer.Detail = value);
    /// <summary>
    /// Performs render detail1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail1() => SetRenderDetail(1);
    /// <summary>
    /// Performs render detail2 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail2() => SetRenderDetail(2);
    /// <summary>
    /// Performs render detail4 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail4() => SetRenderDetail(4);
    /// <summary>
    /// Performs render detail6 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail6() => SetRenderDetail(6);
    /// <summary>
    /// Performs render detail8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail8() => SetRenderDetail(8);
    /// <summary>
    /// Sets render softness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderSoftness(double value) => WithRender(layer => layer.Softness = value);
    /// <summary>
    /// Performs render softness0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness0() => SetRenderSoftness(0);
    /// <summary>
    /// Performs render softness25 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness25() => SetRenderSoftness(.25);
    /// <summary>
    /// Performs render softness50 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness50() => SetRenderSoftness(.5);
    /// <summary>
    /// Performs render softness75 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness75() => SetRenderSoftness(.75);
    /// <summary>
    /// Performs render softness100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness100() => SetRenderSoftness(1);
    /// <summary>
    /// Sets render contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderContrast(double value) => WithRender(layer => layer.RenderContrast = value);
    /// <summary>
    /// Performs render contrast05 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast05() => SetRenderContrast(.5);
    /// <summary>
    /// Performs render contrast10 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast10() => SetRenderContrast(1);
    /// <summary>
    /// Performs render contrast15 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast15() => SetRenderContrast(1.5);
    /// <summary>
    /// Performs render contrast20 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast20() => SetRenderContrast(2);
    /// <summary>
    /// Performs render contrast30 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast30() => SetRenderContrast(3);
    /// <summary>
    /// Sets render angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderAngle(double value) => WithRender(layer => layer.AngleDegrees = value);
    /// <summary>
    /// Performs render angle0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle0() => SetRenderAngle(0);
    /// <summary>
    /// Performs render angle45 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle45() => SetRenderAngle(45);
    /// <summary>
    /// Performs render angle90 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle90() => SetRenderAngle(90);
    /// <summary>
    /// Performs render angle180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle180() => SetRenderAngle(180);
    /// <summary>
    /// Performs render angle270 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle270() => SetRenderAngle(270);
    /// <summary>
    /// Sets render stripe width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderStripeWidth(double value) => WithRender(layer => layer.StripeWidthPx = value);
    /// <summary>
    /// Performs render stripe8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe8() => SetRenderStripeWidth(8);
    /// <summary>
    /// Performs render stripe16 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe16() => SetRenderStripeWidth(16);
    /// <summary>
    /// Performs render stripe32 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe32() => SetRenderStripeWidth(32);
    /// <summary>
    /// Performs render stripe64 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe64() => SetRenderStripeWidth(64);
    /// <summary>
    /// Performs render stripe128 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe128() => SetRenderStripeWidth(128);
    /// <summary>
    /// Performs reset render settings for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Performs change brightness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBrightness(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-brightness", layer => layer.Brightness = Number(args, layer.Brightness));
    /// <summary>
    /// Performs change contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeContrast(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-contrast", layer => layer.Contrast = Number(args, layer.Contrast));
    /// <summary>
    /// Performs change saturation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeSaturation(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-saturation", layer => layer.Saturation = Number(args, layer.Saturation));
    /// <summary>
    /// Performs change hue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeHue(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-hue", layer => layer.HueRotation = Number(args, layer.HueRotation));
    /// <summary>
    /// Performs change blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBlur(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-blur", layer => layer.Blur = Number(args, layer.Blur));
    /// <summary>
    /// Performs change grayscale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeGrayscale(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-grayscale", layer => layer.Grayscale = Number(args, layer.Grayscale));
    /// <summary>
    /// Performs change sepia for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeSepia(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-sepia", layer => layer.Sepia = Number(args, layer.Sepia));
    /// <summary>
    /// Performs change invert for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeInvert(ChangeEventArgs args) => State.UpdateSelectedLive("adjust-invert", layer => layer.Invert = Number(args, layer.Invert));

    /// <summary>
    /// Performs reset adjustments for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
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
    /// Performs with raster for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRaster(Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Performs with raster live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRasterLive(string key, Action<RasterPictureLayer> update)
    {
        if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    /// <summary>
    /// Performs toggle SVG preserve aspect ratio for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSvgPreserveAspectRatio(ChangeEventArgs args)
    {
        if (State.SelectedLayer is SvgPictureLayer svg)
            State.UpdateSelected(_ => svg.PreserveAspectRatio = Bool(args));
    }

    /// <summary>
    /// Performs with text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithText(Action<TextPictureLayer> update)
    {
        if (State.SelectedLayer is TextPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Performs with shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithShape(Action<ShapePictureLayer> update)
    {
        if (State.SelectedLayer is ShapePictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Performs with fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithFill(Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Performs with fill live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithFillLive(string key, Action<FillPictureLayer> update)
    {
        if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }
    /// <summary>
    /// Performs with render for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRender(Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelected(_ => update(layer));
    }
    /// <summary>
    /// Performs with render live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRenderLive(string key, Action<RenderPictureLayer> update)
    {
        if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    }


    /// <summary>
    /// Reads SVG text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="input">Input value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ReadSvgTextAsync(Stream input)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    }

    /// <summary>
    /// Determines whether supported image data URL for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSupportedImageDataUrl(string value) =>
        value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && value.Contains(",", StringComparison.Ordinal);

    /// <summary>
    /// Performs fit raster canvas size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The picture image size produced by the operation.</returns>
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
    /// Performs layer icon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs picture text font size menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string PictureTextFontSizeMenuText(TextPictureLayer text) =>
        $"Font size · {Math.Round(text.FontSizePx).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Performs render scale menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderScaleMenuText(RenderPictureLayer render) =>
        $"Scale · {Math.Round(render.Scale).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Performs render detail menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderDetailMenuText(RenderPictureLayer render) =>
        $"Detail · {render.Detail.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Performs render softness menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderSoftnessMenuText(RenderPictureLayer render) =>
        $"Softness · {Math.Round(render.Softness * 100).ToString(CultureInfo.InvariantCulture)}%";

    /// <summary>
    /// Performs render contrast menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderContrastMenuText(RenderPictureLayer render) =>
        $"Contrast · {render.RenderContrast.ToString("0.0", CultureInfo.InvariantCulture)}×";

    /// <summary>
    /// Performs render angle menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderAngleMenuText(RenderPictureLayer render) =>
        $"Angle · {Math.Round(render.AngleDegrees).ToString(CultureInfo.InvariantCulture)}°";

    /// <summary>
    /// Performs render stripe width menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderStripeWidthMenuText(RenderPictureLayer render) =>
        $"Stripe width · {Math.Round(render.StripeWidthPx).ToString(CultureInfo.InvariantCulture)} px";

    /// <summary>
    /// Performs layer description for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs truncate for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="length">Length value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Truncate(string value, int length) => string.IsNullOrWhiteSpace(value)
        ? "Empty"
        : value.Length <= length ? value : value[..length] + "…";

    /// <summary>
    /// Performs text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Text(ChangeEventArgs args) => Convert.ToString(args.Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    /// <summary>
    /// Performs bool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool Bool(ChangeEventArgs args) => args.Value is bool value && value;
    /// <summary>
    /// Performs number for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Number(ChangeEventArgs args, double fallback) => double.TryParse(Text(args), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    /// <summary>
    /// Performs int for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int Int(ChangeEventArgs args, int fallback) => int.TryParse(Text(args), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    /// <summary>
    /// Performs inv for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Inv(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    /// <summary>
    /// Performs safe color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeColor(string value) => value.StartsWith('#') && value.Length is 4 or 7 ? value : "#000000";

    /// <summary>
    /// Releases resources owned by <see cref="PictureEditor"/> and leaves the picture editor workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        State.Changed -= StateChanged;
        LocalGptConnection.Changed -= LocalGptConnectionChanged;
    }

    /// <summary>
    /// Releases resources owned by <see cref="PictureEditor"/> and leaves the picture editor workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Represents a picture area selection helper type nested within <see cref="PictureEditor"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class PictureAreaSelection
    {
        /// <summary>
        /// Gets or sets the kind value that forms part of the picture area selection state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The kind value exposed by <see cref="PictureAreaSelection"/>.</value>
        public string Kind { get; set; } = "polygon";
        /// <summary>
        /// Gets or sets the points collection maintained or exposed by this picture area selection instance for downstream processing.
        /// </summary>
        /// <value>The points value exposed by <see cref="PictureAreaSelection"/>.</value>
        public List<PicturePoint> Points { get; set; } = [];
    }

    /// <summary>
    /// Represents the outcome of picture OCR, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    private sealed class PictureOcrResult
    {
        /// <summary>
        /// Gets or sets the text value that forms part of the picture OCR state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The text value exposed by <see cref="PictureOcrResult"/>.</value>
        public string Text { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the model name value that forms part of the picture OCR state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model name value exposed by <see cref="PictureOcrResult"/>.</value>
        public string ModelName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the provider URI that identifies the network or application endpoint associated with this picture OCR state.
        /// </summary>
        /// <value>The provider URI value exposed by <see cref="PictureOcrResult"/>.</value>
        public string ProviderUri { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the media type value that forms part of the picture OCR state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The media type value exposed by <see cref="PictureOcrResult"/>.</value>
        public string MediaType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether human review applies to the picture OCR state.
        /// </summary>
        /// <value>The needs human review value exposed by <see cref="PictureOcrResult"/>.</value>
        public bool NeedsHumanReview { get; set; } = true;
    }

    /// <summary>
    /// Represents a picture image size helper type nested within <see cref="PictureEditor"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class PictureImageSize
    {
        /// <summary>
        /// Initializes a new <see cref="PictureImageSize"/> instance and captures the dependencies or initial state required by its picture image size workflow.
        /// </summary>
        public PictureImageSize() { }
        /// <summary>
        /// Gets or sets the width value that forms part of the picture image size state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The width value exposed by <see cref="PictureImageSize"/>.</value>
        public int Width { get; set; }
        /// <summary>
        /// Gets or sets the height value that forms part of the picture image size state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The height value exposed by <see cref="PictureImageSize"/>.</value>
        public int Height { get; set; }
    }
}
