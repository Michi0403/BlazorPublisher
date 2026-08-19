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
/// Renders the picture editor Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding PublisherStudio interface.
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
    /// Gets or sets the runtime policy value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime policy value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private IPublisherRuntimePolicyDataService RuntimePolicy { get; set; } = default!;
    /// <summary>
    /// Gets or sets the files value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The files value exposed by <see cref="PictureEditor"/>.</value>
    [Inject] private PublicationFileService Files { get; set; } = default!;
    /// <summary>Gets the localization catalog service used by Picture Studio labels and commands.</summary>
    /// <value>The localization service used to resolve Picture Studio UI text.</value>
    [Inject] private IFileLocalizationService Localization { get; set; } = default!;

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

    /// <summary>Resolves one canonical English Picture Studio label through the active localization catalog.</summary>
    /// <param name="text">Canonical English UI text to resolve.</param>
    /// <returns>The localized UI text, or the supplied English text when no translation exists.</returns>
    private string LT(string text) {
        try
        {
            return Localization.GetText(text);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LT)} failed.");
            throw;
        }
    }

    /// <summary>Gets the system font-family names available to Picture Studio text layers.</summary>
    /// <value>The font-family names exposed by the system font catalog.</value>
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
    /// <summary>Stores whether the current apply operation should preserve the editable Picture Studio source document.</summary>
    private bool _pictureExportPreserveLayers = true;
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
    private string CanvasHint => LT(_drawTool switch
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
    });
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
        try
        {
            State.Changed += StateChanged;
            LocalGptConnection.Changed += LocalGptConnectionChanged;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(OnInitialized)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs LocalGPT connection changed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LocalGptConnectionChanged() {
        try
        {
            TaskRunner.Run(nameof(PictureEditor), nameof(LocalGptConnectionChanged), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LocalGptConnectionChanged)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the parameters set lifecycle or event notification for <see cref="PictureEditor"/>, updating the state required by the surrounding workflow.
    /// </summary>
    protected override void OnParametersSet()
    {
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(OnParametersSet)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the after render async lifecycle or event notification for <see cref="PictureEditor"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (!Visible) return;
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/pictureStudioInterop.js?v=2.6.5").ConfigureAwait(true);
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
                        var natural = await _module.InvokeAsync<PictureImageSize>("getPictureImageSize", InitialRasterDataUrl).ConfigureAwait(true);
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
                    RuntimePolicy.PictureStudio.LayerDropInputId).ConfigureAwait(true);
                _initialized = true;
                _renderRequested = true;
            }
            if (_renderRequested)
            {
                _renderRequested = false;
                await RenderCanvasAsync().ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(OnAfterRenderAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs state changed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void StateChanged()
    {
        try
        {
            _renderRequested = true;
            TaskRunner.Run(nameof(PictureEditor), nameof(StateChanged), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(StateChanged)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render canvas for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RenderCanvasAsync()
    {
        try
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
                }).ConfigureAwait(true);
            }
            catch (JSDisconnectedException)
            {
                // The browser circuit is closing.
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderCanvasAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture layer selected for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    [JSInvokable]
    public void PictureLayerSelected(string? id)
    {
        try
        {
            State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureLayerSelected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reorders one Picture Studio layer after a drag-and-drop operation in the front-to-back layer list.
    /// </summary>
    /// <param name="draggedId">Identifier of the dragged layer.</param>
    /// <param name="targetId">Identifier of the layer under the drop marker.</param>
    /// <param name="placeAfter">Whether the row is placed after the target in the visible front-to-back list.</param>
    /// <returns>A completed task after the document layer order has been updated.</returns>
    [JSInvokable]
    public Task PictureStudioLayerDropped(string draggedId, string targetId, bool placeAfter)
    {
        try
        {
            if (!Guid.TryParse(draggedId, out var dragged) || !Guid.TryParse(targetId, out var target) || dragged == target)
                return Task.CompletedTask;

            var visualOrder = State.Document.Layers.AsEnumerable().Reverse().Select(layer => layer.Id).ToList();
            if (!visualOrder.Remove(dragged)) return Task.CompletedTask;
            var targetVisualIndex = visualOrder.IndexOf(target);
            if (targetVisualIndex < 0) return Task.CompletedTask;
            visualOrder.Insert(Math.Clamp(targetVisualIndex + (placeAfter ? 1 : 0), 0, visualOrder.Count), dragged);
            var finalVisualIndex = visualOrder.IndexOf(dragged);
            var backToFrontIndex = State.Document.Layers.Count - 1 - finalVisualIndex;
            State.MoveLayerToIndex(dragged, backToFrontIndex);
            State.SelectLayer(dragged);
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureStudioLayerDropped)} failed.");
            throw;
        }
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
        try
        {
            if (Guid.TryParse(id, out var parsed))
                State.CommitTransform(parsed, x, y, width, height, rotation);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureTransformCommitted)} failed.");
            throw;
        }
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
        try
        {
            if (!Enum.TryParse<PictureStrokeKind>(tool, true, out var kind) || coordinates.Length < 4) return;
            var points = new List<PicturePoint>(coordinates.Length / 2);
            for (var index = 0; index + 1 < coordinates.Length; index += 2)
                points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
            State.AddStroke(kind, points, color, width, opacity, hardness);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureStrokeCommitted)} failed.");
            throw;
        }
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
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureShapeCommitted)} failed.");
            throw;
        }
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
        try
        {
            if (coordinates.Length < 4) return;
            var points = new List<PicturePoint>(coordinates.Length / 2);
            for (var index = 0; index + 1 < coordinates.Length; index += 2)
                points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
            State.AddPath(points, _drawColor, _drawWidth, closed, smooth);
            SetDrawTool(PictureDrawTool.Select);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PicturePathCommitted)} failed.");
            throw;
        }
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
        try
        {
            if (coordinates.Length < 4) return;
            var points = new List<PicturePoint>(coordinates.Length / 2);
            for (var index = 0; index + 1 < coordinates.Length; index += 2)
                points.Add(new PicturePoint { X = coordinates[index], Y = coordinates[index + 1] });
            State.AddAreaFill(selectionKind, points, primaryColor, secondaryColor, gradient);
            SetDrawTool(PictureDrawTool.Select);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureAreaFillCommitted)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture color picked for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="color">Color value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureColorPicked(string color)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(color)) _drawColor = color;
            _drawTool = PictureDrawTool.Brush;
            _renderRequested = true;
            TaskRunner.Run(nameof(PictureEditor), nameof(PictureColorPicked), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureColorPicked)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture shortcut requested for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="command">Command value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task PictureShortcutRequested(string command)
    {
        try
        {
            switch (command?.Trim().ToLowerInvariant())
            {
                case "undo": State.Undo(); break;
                case "redo": State.Redo(); break;
                case "copy":
                    if (!await CopyAreaSelectionToClipboardAsync().ConfigureAwait(true)) State.CopySelected();
                    break;
                case "paste": State.Paste(); break;
                case "duplicate": State.DuplicateSelected(); break;
                case "delete":
                    if (!await ApplyAreaClipAsync(inverted: true, quietWhenMissing: true).ConfigureAwait(true)) State.DeleteSelected();
                    break;
                case "front": State.BringSelectedToFront(); break;
                case "back": State.SendSelectedToBack(); break;
                case "select": SetDrawTool(PictureDrawTool.Select); break;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureShortcutRequested)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture render failed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureRenderFailed(string message)
    {
        try
        {
            _renderErrorActive = true;
            _error = string.IsNullOrWhiteSpace(message) ? "A picture layer could not be rendered." : message;
            TaskRunner.Run(nameof(PictureEditor), nameof(PictureRenderFailed), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureRenderFailed)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture render recovered for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    [JSInvokable]
    public void PictureRenderRecovered()
    {
        try
        {
            if (!_renderErrorActive) return;
            _renderErrorActive = false;
            _error = null;
            TaskRunner.Run(nameof(PictureEditor), nameof(PictureRenderRecovered), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureRenderRecovered)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs show canvas context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowCanvasContextMenu(MouseEventArgs args)
    {
        try
        {
            if (_module is not null && args.Button == 2)
            {
                var id = await _module.InvokeAsync<string?>("hitTestPictureStudioLayer", RuntimePolicy.PictureStudio.CanvasId, args.ClientX, args.ClientY).ConfigureAwait(true);
                State.SelectLayer(Guid.TryParse(id, out var parsed) ? parsed : null);
            }
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            await _pictureContextMenu.ShowAsync(args).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShowCanvasContextMenu)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs show layer context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowLayerContextMenu(PictureLayer layer, MouseEventArgs args)
    {
        try
        {
            State.SelectLayer(layer.Id);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            await _pictureContextMenu.ShowAsync(args).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShowLayerContextMenu)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs show layer list context menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowLayerListContextMenu(MouseEventArgs args)
    {
        try
        {
            State.SelectLayer(null);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            await _pictureContextMenu.ShowAsync(args).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShowLayerListContextMenu)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs request image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestImage()
    {
        try
        {
            _replaceRasterLayerId = null;
            await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RequestImage)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs request layered import for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestLayeredImport()
    {
        try
        {
            _replaceRasterLayerId = null;
            await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.LayeredInputId).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RequestLayeredImport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs request raster replacement for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestRasterReplacement()
    {
        try
        {
            if (State.SelectedLayer is not RasterPictureLayer { Locked: false } raster) return;
            _replaceRasterLayerId = raster.Id;
            await JS.InvokeVoidAsync("publisherStudio.clickElement", RuntimePolicy.PictureStudio.ImageInputId).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RequestRasterReplacement)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Imports image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportImage(InputFileChangeEventArgs args) {
        try
        {
            return ImportImageCore(args, forceAdd: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportImage)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Imports dropped image for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportDroppedImage(InputFileChangeEventArgs args) {
        try
        {
            return ImportImageCore(args, forceAdd: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportDroppedImage)} failed.");
            throw;
        }
    }

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
            try
            {
                var file = args.File;
                var allowed = new[] { "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml" };
                if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unsupported picture format.");

                var stream = file.OpenReadStream(long.MaxValue);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(true);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer).ConfigureAwait(true);
                var dataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
                var size = _module is null
                    ? new PictureImageSize()
                    : await _module.InvokeAsync<PictureImageSize>("getPictureImageSize", dataUrl).ConfigureAwait(true);
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportImageCore)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Imports layered document for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportLayeredDocument(InputFileChangeEventArgs args) {
        try
        {
            return ImportLayeredDocumentCore(args, append: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportLayeredDocument)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Imports dropped layered document for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ImportDroppedLayeredDocument(InputFileChangeEventArgs args) {
        try
        {
            return ImportLayeredDocumentCore(args, append: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportDroppedLayeredDocument)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Imports layered document core for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the picture editor workflow to provide the corresponding application capability.</param>
    /// <param name="append">Value indicating whether append should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ImportLayeredDocumentCore(InputFileChangeEventArgs args, bool append)
    {
        try
        {
            _error = null;
            _notice = null;
            try
            {
                var file = args.File;
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                PictureImportResult result;
                var stream = file.OpenReadStream(long.MaxValue);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(true);
                if (extension == ".ora")
                {
                    result = await OpenRasterImporter.ImportAsync(stream, file.Name).ConfigureAwait(true);
                }
                else if (extension is ".svg" or ".svgz" || file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
                {
                    if (_module is null) throw new InvalidOperationException("Picture Studio is not ready yet.");
                    string svgText;
                    if (extension == ".svgz" || file.ContentType.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                    {
                        using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                        svgText = await ReadSvgTextAsync(gzip).ConfigureAwait(true);
                    }
                    else
                    {
                        svgText = await ReadSvgTextAsync(stream).ConfigureAwait(true);
                    }
                    var dataUrl = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svgText))}";
                    result = await _module.InvokeAsync<PictureImportResult>("importPictureStudioSvg", dataUrl, file.Name).ConfigureAwait(true);
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ImportLayeredDocumentCore)} failed.");
            throw;
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
        try
        {
            _pendingDropX = x is double px && double.IsFinite(px) ? Math.Clamp(px, 0, State.Document.WidthPx) : null;
            _pendingDropY = y is double py && double.IsFinite(py) ? Math.Clamp(py, 0, State.Document.HeightPx) : null;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureStudioFileDropPositioned)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear pending drop position for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearPendingDropPosition()
    {
        try
        {
            _pendingDropX = null;
            _pendingDropY = null;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ClearPendingDropPosition)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture studio file drop rejected for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void PictureStudioFileDropRejected(string? message)
    {
        try
        {
            ClearPendingDropPosition();
            _error = string.IsNullOrWhiteSpace(message)
                ? "Drop a PNG, JPEG, GIF, WebP, SVG, SVGZ, or OpenRaster picture into Picture Studio."
                : message;
            TaskRunner.Run(nameof(PictureEditor), nameof(PictureStudioFileDropRejected), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureStudioFileDropRejected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds text layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddTextLayer() {
        try
        {
            State.AddText();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddTextLayer)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddRectangle() {
        try
        {
            State.AddShape(PictureShapeKind.Rectangle);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddRectangle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds ellipse for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddEllipse() {
        try
        {
            State.AddShape(PictureShapeKind.Ellipse);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddEllipse)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds arrow shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddArrowShape() {
        try
        {
            State.AddShape(PictureShapeKind.Arrow);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddArrowShape)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds line shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddLineShape() {
        try
        {
            State.AddShape(PictureShapeKind.Line);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddLineShape)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddGradient() {
        try
        {
            State.AddFill(PictureFillKind.LinearGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddGradient)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds solid fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddSolidFill() {
        try
        {
            State.AddFill(PictureFillKind.Solid);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddSolidFill)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds clouds for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddClouds() {
        try
        {
            State.AddRender(PictureRenderKind.Clouds);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddClouds)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddNoise() {
        try
        {
            State.AddRender(PictureRenderKind.Noise);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddNoise)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds stripes for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddStripes() {
        try
        {
            State.AddRender(PictureRenderKind.Stripes);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddStripes)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds vignette for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddVignette() {
        try
        {
            State.AddRender(PictureRenderKind.Vignette);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddVignette)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds bloom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddBloom() {
        try
        {
            State.AddRender(PictureRenderKind.Bloom);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddBloom)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds neon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddNeon() {
        try
        {
            State.AddRender(PictureRenderKind.Neon);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddNeon)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds lens flare for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddLensFlare() {
        try
        {
            State.AddRender(PictureRenderKind.LensFlare);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddLensFlare)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds grain noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddGrainNoise() {
        try
        {
            State.AddRender(PictureRenderKind.GrainNoise);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddGrainNoise)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds motion blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddMotionBlur() {
        try
        {
            State.AddRender(PictureRenderKind.MotionBlur);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddMotionBlur)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds wind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddWind() {
        try
        {
            State.AddRender(PictureRenderKind.Wind);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddWind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds ocean waves for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddOceanWaves() {
        try
        {
            State.AddRender(PictureRenderKind.OceanWaves);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddOceanWaves)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds paint layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddPaintLayer() {
        try
        {
            State.AddPaint();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddPaintLayer)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs move up for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoveUp() {
        try
        {
            State.MoveSelectedLayer(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoveUp)} failed.");
            throw;
        }
    }

    /// <summary>Moves one selected layer one step toward the front of the layer stack.</summary>
    /// <param name="layerId">Identifier of the layer selected from the visible Layers panel row.</param>
    private void MoveLayerForward(Guid layerId)
    {
        try
        {
            State.SelectLayer(layerId);
            State.MoveSelectedLayer(1);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoveLayerForward)} failed.");
            throw;
        }
    }

    /// <summary>Moves one selected layer one step toward the back of the layer stack.</summary>
    /// <param name="layerId">Identifier of the layer selected from the visible Layers panel row.</param>
    private void MoveLayerBackward(Guid layerId)
    {
        try
        {
            State.SelectLayer(layerId);
            State.MoveSelectedLayer(-1);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoveLayerBackward)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs move down for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoveDown() {
        try
        {
            State.MoveSelectedLayer(-1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoveDown)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs zoom100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Zoom100() {
        try
        {
            State.SetZoom(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Zoom100)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs fit canvas for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task FitCanvas()
    {
        try
        {
            if (_module is null) return;
            var zoom = await _module.InvokeAsync<double>("fitPictureStudio", RuntimePolicy.PictureStudio.CanvasHostId, State.Document.WidthPx, State.Document.HeightPx).ConfigureAwait(true);
            State.SetZoom(zoom);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FitCanvas)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs apply for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ApplyLayered() {
        try
        {
            return Apply(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyLayered)} failed.");
            throw;
        }
    }

    /// <summary>Flattens all visible Picture Studio layers into the publication image and discards editable Picture Studio layer ownership.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ApplyMerged() {
        try
        {
            return Apply(false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyMerged)} failed.");
            throw;
        }
    }

    /// <summary>Renders the current picture for the Mainframe and optionally preserves the editable Picture Studio document.</summary>
    /// <param name="preserveLayers">Value indicating whether preserve layers should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task Apply(bool preserveLayers)
    {
        try
        {
            if (_module is null || _self is null || _pictureExportId is not null) return;

            var exportId = Guid.NewGuid().ToString("N");
            var sourceDocument = State.CloneDocument();
            _pictureExportId = exportId;
            _pictureExportSourceDocument = sourceDocument;
            _pictureExportName = State.Document.Name;
            _pictureExportPurpose = "save";
            _pictureExportPreserveLayers = preserveLayers;
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
                    exportId).ConfigureAwait(true);
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Apply)} failed.");
            throw;
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
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(BeginPictureExport)} failed.");
            throw;
        }
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
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AppendPictureExportChunk)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Completes picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task CompletePictureExport(string exportId)
    {
        try
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
            var preserveLayers = _pictureExportPreserveLayers;
            ResetPictureExport();
            if (string.Equals(purpose, "ocr", StringComparison.Ordinal))
            {
                await RequestLocalGptOcrAsync(dataUrl).ConfigureAwait(true);
                return;
            }
            await DisposePictureRuntimeAsync().ConfigureAwait(true);
            await InvokeAsync(() => Saved.InvokeAsync(new PictureEditorResult(dataUrl, preserveLayers ? sourceDocument : null, name, preserveLayers))).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CompletePictureExport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs fail picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <param name="message">Message value supplied to the picture editor operation and used when producing its result.</param>
    [JSInvokable]
    public void FailPictureExport(string exportId, string? message)
    {
        try
        {
            if (!IsCurrentPictureExport(exportId)) return;
            ResetPictureExport();
            _error = string.IsNullOrWhiteSpace(message) ? "The browser could not render the picture." : message;
            TaskRunner.Run(nameof(PictureEditor), nameof(FailPictureExport), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(false));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FailPictureExport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether current picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="exportId">Identifier of the export to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsCurrentPictureExport(string exportId) {
        try
        {
            return _pictureExportId is not null &&
        string.Equals(_pictureExportId, exportId, StringComparison.Ordinal);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(IsCurrentPictureExport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs reset picture export for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ResetPictureExport()
    {
        try
        {
            _pictureExportBuffer = null;
            _pictureExportId = null;
            _pictureExportSourceDocument = null;
            _pictureExportName = null;
            _pictureExportPurpose = "save";
            _pictureExportPreserveLayers = true;
            _pictureExportExpectedChunks = 0;
            _pictureExportNextChunk = 0;
            _pictureExportExpectedLength = 0;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ResetPictureExport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Starts LocalGPT OCR for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartLocalGptOcrAsync()
    {
        try
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
                await _module.InvokeVoidAsync("startPictureStudioDataUrlExport", _pictureExportSourceDocument, "image/jpeg", .9d, _self, exportId).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (IsCurrentPictureExport(exportId)) ResetPictureExport();
                Logger.LogError(ex, "Could not render the Picture Studio canvas for LocalGPT OCR.");
                _error = ex.Message;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(StartLocalGptOcrAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs request LocalGPT OCR for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RequestLocalGptOcrAsync(string dataUrl)
    {
        try
        {
            _ocrBusy = true;
            try
            {
                _ocrStatus = "Waiting for LocalGPT approval and local OCR…";
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
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
                var correlationId = await LocalGptConnection.SendEnvelopeAsync(envelope).ConfigureAwait(true);
                var deadline = DateTimeOffset.UtcNow.AddMinutes(6);
                while (true)
                {
                    var remaining = deadline - DateTimeOffset.UtcNow;
                    if (remaining <= TimeSpan.Zero) throw new TimeoutException("LocalGPT OCR did not finish within six minutes.");
                    var response = await LocalGptConnection.WaitForResultAsync(correlationId, remaining).ConfigureAwait(true);
                    if (response.MessageType == OrganicWireMessageType.ApprovalRequired)
                    {
                        _ocrStatus = "Approve the OCR request in the LocalGPT frontend; PublisherStudio will keep waiting for the same request.";
                        await InvokeAsync(StateHasChanged).ConfigureAwait(true);
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
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RequestLocalGptOcrAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs insert OCR text layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void InsertOcrTextLayer()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_ocrText)) return;
            var layer = State.AddText();
            layer.Name = "OCR text";
            layer.Text = _ocrText;
            _notice = "Recognized text was inserted as an editable Picture Studio text layer.";
            Notifications.Success(_notice, "Picture Studio OCR", nameof(PictureEditor));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(InsertOcrTextLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear OCR text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearOcrText()
    {
        try
        {
            _ocrText = string.Empty;
            _ocrStatus = string.Empty;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ClearOcrText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads wire string for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadWireString(OrganicWireEnvelope envelope, string key)
    {
        try
        {
            if (envelope.Properties is null || !envelope.Properties.TryGetValue(key, out var value)) return string.Empty;
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ReadWireString)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs download png for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadPng() {
        try
        {
            await Download("image/png", "png", 1d).ConfigureAwait(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DownloadPng)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs download jpeg for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadJpeg() {
        try
        {
            await Download("image/jpeg", "jpg", .92d).ConfigureAwait(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DownloadJpeg)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs download SVG for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadSvg()
    {
        try
        {
            if (_module is null) return;
            var fileName = $"{Files.SafeFileName(State.Document.Name)}.svg";
            await _module.InvokeVoidAsync("downloadPictureStudioSvg", State.Document, fileName).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DownloadSvg)} failed.");
            throw;
        }
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
        try
        {
            if (_module is null) return;
            try
            {
                var name = Files.SafeFileName(State.Document.Name) + "." + extension;
                await _module.InvokeVoidAsync("downloadPictureStudio", State.Document, name, mimeType, quality).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Download)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task Cancel()
    {
        try
        {
            await CancelPictureInteractionAsync().ConfigureAwait(true);
            await DisposePictureRuntimeAsync().ConfigureAwait(true);
            await Cancelled.InvokeAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Cancel)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Select);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs brush tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void BrushTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Brush);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(BrushTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs pencil tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PencilTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Pencil);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PencilTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs spray tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SprayTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Spray);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SprayTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toothbrush tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToothbrushTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Toothbrush);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToothbrushTool)} failed.");
            throw;
        }
    }
    /// <summary>Creates an editable node path rendered with the brush stroke engine.</summary>
    private void BrushPathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.BrushPath);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(BrushPathTool)} failed.");
            throw;
        }
    }
    /// <summary>Creates an editable node path rendered with the pencil stroke engine.</summary>
    private void PencilPathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.PencilPath);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PencilPathTool)} failed.");
            throw;
        }
    }
    /// <summary>Creates an editable node path rendered with the spray stroke engine.</summary>
    private void SprayPathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.SprayPath);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SprayPathTool)} failed.");
            throw;
        }
    }
    /// <summary>Creates an editable node path rendered with the toothbrush stroke engine.</summary>
    private void ToothbrushPathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.ToothbrushPath);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToothbrushPathTool)} failed.");
            throw;
        }
    }
    /// <summary>Creates an editable node path rendered with the eraser stroke engine.</summary>
    private void EraserPathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.EraserPath);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EraserPathTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs square tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SquareTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Square);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SquareTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs rectangle tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RectangleTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Rectangle);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RectangleTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs ellipse tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EllipseTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Ellipse);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EllipseTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs arrow tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ArrowTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Arrow);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ArrowTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs line tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LineTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Line);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LineTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs path tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PathTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Path);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PathTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs eraser tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EraserTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Eraser);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EraserTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs eyedropper tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EyedropperTool() {
        try
        {
            SetDrawTool(PictureDrawTool.Eyedropper);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EyedropperTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs rectangle select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RectangleSelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.RectangleSelect);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RectangleSelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs ellipse select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void EllipseSelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.EllipseSelect);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EllipseSelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs free select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FreeSelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.FreeSelect);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FreeSelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs magnetic select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MagneticSelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.MagneticSelect);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MagneticSelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs polygon select tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PolygonSelectTool() {
        try
        {
            SetDrawTool(PictureDrawTool.PolygonSelect);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PolygonSelectTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill solid tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillSolidTool() {
        try
        {
            SetDrawTool(PictureDrawTool.FillSolid);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillSolidTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill gradient tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillGradientTool() {
        try
        {
            SetDrawTool(PictureDrawTool.FillGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillGradientTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs clear area selection for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ClearAreaSelection()
    {
        try
        {
            if (_module is not null) await _module.InvokeVoidAsync("clearPictureStudioAreaSelection", RuntimePolicy.PictureStudio.CanvasId).ConfigureAwait(true);
            _renderRequested = true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ClearAreaSelection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads area selection for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>The picture area selection produced by the operation.</returns>
    private async Task<PictureAreaSelection?> ReadAreaSelectionAsync()
    {
        try
        {
            if (_module is null || State.SelectedLayer is null) return null;
            try
            {
                return await _module.InvokeAsync<PictureAreaSelection?>("getPictureStudioAreaSelection", RuntimePolicy.PictureStudio.CanvasId).ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { return null; }
            catch (TaskCanceledException) { return null; }
            catch (JSException) { return null; }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ReadAreaSelectionAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs selection polygon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="selection">Selection value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<PicturePoint> SelectionPolygon(PictureAreaSelection selection)
    {
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SelectionPolygon)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies area clip for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="inverted">Value indicating whether inverted should apply to this operation.</param>
    /// <param name="quietWhenMissing">Value indicating whether quiet when missing should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> ApplyAreaClipAsync(bool inverted, bool quietWhenMissing = false)
    {
        try
        {
            var selection = await ReadAreaSelectionAsync().ConfigureAwait(true);
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
            await ClearAreaSelection().ConfigureAwait(true);
            SetDrawTool(PictureDrawTool.Select);
            return true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyAreaClipAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs keep selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task KeepSelectedArea() {
        try
        {
            return ApplyAreaClipAsync(inverted: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(KeepSelectedArea)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs cut selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task CutSelectedArea() {
        try
        {
            return ApplyAreaClipAsync(inverted: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CutSelectedArea)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy area selection to clipboard for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> CopyAreaSelectionToClipboardAsync()
    {
        try
        {
            var selection = await ReadAreaSelectionAsync().ConfigureAwait(true);
            var polygon = selection is null ? [] : SelectionPolygon(selection);
            if (polygon.Count < 3 || !State.CopySelectedRegion(polygon)) return false;
            _notice = "Selected picture region copied. Paste inserts it as an independently editable clipped layer.";
            return true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CopyAreaSelectionToClipboardAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy selected area for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CopySelectedArea()
    {
        try
        {
            if (!await CopyAreaSelectionToClipboardAsync().ConfigureAwait(true))
                _notice = "Create an area selection on a layer before copying a region.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CopySelectedArea)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy selected area as layer for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CopySelectedAreaAsLayer()
    {
        try
        {
            if (!await CopyAreaSelectionToClipboardAsync().ConfigureAwait(true))
            {
                _notice = "Create an area selection on a layer before copying a region.";
                return;
            }
            State.Paste();
            await ClearAreaSelection().ConfigureAwait(true);
            SetDrawTool(PictureDrawTool.Select);
            _notice = "The selected region was inserted as a new clipped layer.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CopySelectedAreaAsLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear layer cut for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ClearLayerCut()
    {
        try
        {
            if (State.ClearSelectedClip()) _notice = "The layer cut was cleared.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ClearLayerCut)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs distance for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Distance(PicturePoint first, PicturePoint second)
    {
        try
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            return Math.Sqrt(x * x + y * y);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Distance)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets draw tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    private void SetDrawTool(PictureDrawTool tool)
    {
        try
        {
            TaskRunner.Run(nameof(PictureEditor), nameof(SetDrawTool), async _ => await InvokeAsync(CancelPictureInteractionAsync).ConfigureAwait(false));
            _drawTool = tool;
            _renderRequested = true;
            StateHasChanged();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetDrawTool)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel picture interaction for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CancelPictureInteractionAsync()
    {
        try
        {
            if (_module is null) return;
            try
            {
                await _module.InvokeVoidAsync("cancelPictureStudioInteraction", RuntimePolicy.PictureStudio.CanvasId).ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (JSException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CancelPictureInteractionAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs dispose picture runtime for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DisposePictureRuntimeAsync()
    {
        try
        {
            if (_module is null || !_initialized) return;
            try
            {
                await _module.InvokeVoidAsync("disposePictureStudio", RuntimePolicy.PictureStudio.CanvasId).ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (JSException) { }
            finally
            {
                _initialized = false;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DisposePictureRuntimeAsync)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs tool text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="tool">Tool value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ToolText(PictureDrawTool tool, string text) {
        try
        {
     var localized = LT(text); return _drawTool == tool ? $"✓ {localized}" : localized; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToolText)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Determines whether draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsDrawWidth(double value) {
        try
        {
            return Math.Abs(_drawWidth - value) < .001;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(IsDrawWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DrawWidthText(double value) {
        try
        {
            return IsDrawWidth(value) ? $"✓ {value:0.##} px" : $"{value:0.##} px";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidthText)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width button class for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DrawWidthButtonClass(double value) {
        try
        {
            return IsDrawWidth(value) ? "selected" : string.Empty;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidthButtonClass)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawColor(string value) {
        try
        {
     if (!string.IsNullOrWhiteSpace(value)) _drawColor = value; _renderRequested = true; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawColor)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw secondary color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawSecondaryColor(string value) {
        try
        {
     if (!string.IsNullOrWhiteSpace(value)) _drawSecondaryColor = value; _renderRequested = true; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawSecondaryColor)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetDrawWidth(double value)
    {
        try
        {
            _drawWidth = Math.Clamp(value, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
            _renderRequested = true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetDrawWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs width to slider for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double WidthToSlider(double width)
    {
        try
        {
            var clamped = Math.Clamp(width, RuntimePolicy.PictureStudio.MinimumDrawWidth, RuntimePolicy.PictureStudio.MaximumDrawWidth);
            return Math.Log(clamped / RuntimePolicy.PictureStudio.MinimumDrawWidth) / Math.Log(RuntimePolicy.PictureStudio.MaximumDrawWidth / RuntimePolicy.PictureStudio.MinimumDrawWidth) * 100;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WidthToSlider)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs slider to width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="slider">Slider value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double SliderToWidth(double slider)
    {
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SliderToWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth1() {
        try
        {
            SetDrawWidth(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidth1)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width3 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth3() {
        try
        {
            SetDrawWidth(3);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidth3)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth8() {
        try
        {
            SetDrawWidth(8);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidth8)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width16 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth16() {
        try
        {
            SetDrawWidth(16);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidth16)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs draw width32 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void DrawWidth32() {
        try
        {
            SetDrawWidth(32);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DrawWidth32)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle grid ribbon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleGridRibbon() {
        try
        {
            State.SetGrid(!State.Document.GridVisible);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleGridRibbon)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle snap ribbon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSnapRibbon() {
        try
        {
            State.SetSnap(!State.Document.SnapToGrid);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSnapRibbon)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Gets the grid text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grid text value exposed by <see cref="PictureEditor"/>.</value>
    private string GridText => State.Document.GridVisible ? $"✓ {LT("Grid")}" : LT("Grid");
    /// <summary>
    /// Gets the snap text value that forms part of the picture editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snap text value exposed by <see cref="PictureEditor"/>.</value>
    private string SnapText => State.Document.SnapToGrid ? $"✓ {LT("Snap")}" : LT("Snap");
    /// <summary>
    /// Performs make render clouds for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderClouds() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Clouds);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderClouds)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderNoise() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Noise);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderNoise)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render stripes for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderStripes() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Stripes);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderStripes)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render vignette for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderVignette() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Vignette);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderVignette)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render bloom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderBloom() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Bloom);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderBloom)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render neon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderNeon() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Neon);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderNeon)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render lens flare for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderLensFlare() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.LensFlare);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderLensFlare)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render grain noise for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderGrainNoise() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.GrainNoise);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderGrainNoise)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render motion blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderMotionBlur() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.MotionBlur);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderMotionBlur)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render wind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderWind() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.Wind);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderWind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs make render ocean waves for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MakeRenderOceanWaves() {
        try
        {
            WithRender(layer => layer.RenderKind = PictureRenderKind.OceanWaves);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MakeRenderOceanWaves)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster contain for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterContain() {
        try
        {
            WithRaster(layer => layer.FitMode = PictureRasterFitMode.Contain);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterContain)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster cover for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterCover() {
        try
        {
            WithRaster(layer => layer.FitMode = PictureRasterFitMode.Cover);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterCover)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster stretch for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterStretch() {
        try
        {
            WithRaster(layer => layer.FitMode = PictureRasterFitMode.Stretch);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterStretch)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster flip horizontal for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterFlipHorizontal() {
        try
        {
            WithRaster(layer => layer.FlipHorizontal = !layer.FlipHorizontal);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterFlipHorizontal)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster flip vertical for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterFlipVertical() {
        try
        {
            WithRaster(layer => layer.FlipVertical = !layer.FlipVertical);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterFlipVertical)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster rotate left for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterRotateLeft() {
        try
        {
            WithRaster(layer => layer.Rotation = (layer.Rotation - 90 + 360) % 360);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterRotateLeft)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster rotate right for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterRotateRight() {
        try
        {
            WithRaster(layer => layer.Rotation = (layer.Rotation + 90) % 360);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterRotateRight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster reset rotation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterResetRotation() {
        try
        {
            WithRaster(layer => layer.Rotation = 0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterResetRotation)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster no tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterNoTint() {
        try
        {
            WithRaster(layer => layer.TintOpacity = 0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterNoTint)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster blue tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterBlueTint() {
        try
        {
            WithRaster(layer => { layer.TintColor = "#2563eb"; layer.TintOpacity = .28; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterBlueTint)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster warm tint for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterWarmTint() {
        try
        {
            WithRaster(layer => { layer.TintColor = "#f97316"; layer.TintOpacity = .24; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterWarmTint)} failed.");
            throw;
        }
    }
    /// <summary>Changes the selected raster's non-destructive color replacement mode.</summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterColorizeMode(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureRasterColorizeMode>(args.Value?.ToString(), true, out var mode))
                WithRaster(layer => layer.ColorizeMode = mode);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterColorizeMode)} failed.");
            throw;
        }
    }
    /// <summary>Changes the selected raster source color used by near-color replacement.</summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterColorizeSource(ChangeEventArgs args) {
        try
        {
            WithRaster(layer => layer.ColorizeSourceColor = SafeColor(args.Value?.ToString()));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterColorizeSource)} failed.");
            throw;
        }
    }
    /// <summary>Changes the selected raster target color.</summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterColorizeTarget(ChangeEventArgs args) {
        try
        {
            WithRaster(layer => layer.ColorizeTargetColor = SafeColor(args.Value?.ToString()));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterColorizeTarget)} failed.");
            throw;
        }
    }
    /// <summary>Changes how close a pixel must be to the selected source color before it is recolored.</summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterColorizeTolerance(ChangeEventArgs args)
    {
        try
        {
            if (int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                WithRasterLive("raster-colorize-tolerance", layer => layer.ColorizeTolerance = Math.Clamp(value, 0, 255));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterColorizeTolerance)} failed.");
            throw;
        }
    }
    /// <summary>Changes the strength of non-destructive raster recoloring.</summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterColorizeStrength(ChangeEventArgs args)
    {
        try
        {
            if (double.TryParse(args.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                WithRasterLive("raster-colorize-strength", layer => layer.ColorizeStrength = Math.Clamp(value, 0, 1));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterColorizeStrength)} failed.");
            throw;
        }
    }
    /// <summary>Maps white and near-white pixels to red while preserving source alpha and antialiasing.</summary>
    private void RasterWhiteToRed() {
        try
        {
            WithRaster(layer =>
    {
        layer.ColorizeMode = PictureRasterColorizeMode.ReplaceColor;
        layer.ColorizeSourceColor = "#ffffff";
        layer.ColorizeTargetColor = "#dc2626";
        layer.ColorizeTolerance = 72;
        layer.ColorizeStrength = 1;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterWhiteToRed)} failed.");
            throw;
        }
    }
    /// <summary>Maps white and near-white pixels to the selected tint color.</summary>
    private void RasterWhiteToTint() {
        try
        {
            WithRaster(layer =>
    {
        layer.ColorizeMode = PictureRasterColorizeMode.ReplaceColor;
        layer.ColorizeSourceColor = "#ffffff";
        layer.ColorizeTargetColor = SafeColor(layer.TintColor);
        layer.ColorizeTolerance = 72;
        layer.ColorizeStrength = 1;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterWhiteToTint)} failed.");
            throw;
        }
    }
    /// <summary>Maps source luminance through the selected target hue while retaining transparency.</summary>
    private void RasterLuminosityColorize() {
        try
        {
            WithRaster(layer =>
    {
        layer.ColorizeMode = PictureRasterColorizeMode.Luminosity;
        layer.ColorizeTargetColor = SafeColor(layer.TintColor);
        layer.ColorizeStrength = 1;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterLuminosityColorize)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs raster reset colorize for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RasterResetColorize() {
        try
        {
            WithRaster(layer => layer.ColorizeMode = PictureRasterColorizeMode.None);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RasterResetColorize)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs soften light for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SoftenLight() {
        try
        {
            State.UpdateSelected(layer => layer.Blur = 2);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SoftenLight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs soften medium for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void SoftenMedium() {
        try
        {
            State.UpdateSelected(layer => layer.Blur = 6);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SoftenMedium)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Removes softening for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RemoveSoftening() {
        try
        {
            State.UpdateSelected(layer => layer.Blur = 0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RemoveSoftening)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs brighten for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Brighten() {
        try
        {
            State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness + .1, 0, 3));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Brighten)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs darken for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void Darken() {
        try
        {
            State.UpdateSelected(layer => layer.Brightness = Math.Clamp(layer.Brightness - .1, 0, 3));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Darken)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs more contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoreContrast() {
        try
        {
            State.UpdateSelected(layer => layer.Contrast = Math.Clamp(layer.Contrast + .1, 0, 3));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoreContrast)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs more saturation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void MoreSaturation() {
        try
        {
            State.UpdateSelected(layer => layer.Saturation = Math.Clamp(layer.Saturation + .1, 0, 3));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(MoreSaturation)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle grayscale preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleGrayscalePreset() {
        try
        {
            State.UpdateSelected(layer => layer.Grayscale = layer.Grayscale > .5 ? 0 : 1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleGrayscalePreset)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle sepia preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSepiaPreset() {
        try
        {
            State.UpdateSelected(layer => layer.Sepia = layer.Sepia > .5 ? 0 : 1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSepiaPreset)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle invert preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleInvertPreset() {
        try
        {
            State.UpdateSelected(layer => layer.Invert = layer.Invert > .5 ? 0 : 1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleInvertPreset)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Applies bloom effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ApplyBloomEffect() {
        try
        {
            State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .18, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .06, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .12, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, 4), 0, 50);
        layer.Opacity = Math.Clamp(layer.Opacity, .82, 1);
        layer.BlendMode = PictureBlendMode.Screen;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyBloomEffect)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Applies neon effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ApplyNeonEffect() {
        try
        {
            State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .22, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .25, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .6, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, 1.5), 0, 50);
        layer.BlendMode = PictureBlendMode.Screen;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyNeonEffect)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Applies lens flare effect for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ApplyLensFlareEffect() {
        try
        {
            State.UpdateSelected(layer =>
    {
        layer.Brightness = Math.Clamp(layer.Brightness + .28, 0, 3);
        layer.Contrast = Math.Clamp(layer.Contrast + .12, 0, 3);
        layer.Saturation = Math.Clamp(layer.Saturation + .18, 0, 3);
        layer.Blur = Math.Clamp(Math.Max(layer.Blur, .75), 0, 50);
        layer.Opacity = Math.Clamp(layer.Opacity, .9, 1);
        layer.BlendMode = PictureBlendMode.Screen;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ApplyLensFlareEffect)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs shape rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeRectangle() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.Rectangle);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeRectangle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape rounded rectangle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeRoundedRectangle() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.RoundedRectangle);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeRoundedRectangle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape ellipse for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeEllipse() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.Ellipse);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeEllipse)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape arrow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeArrow() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.Arrow);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeArrow)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape line for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeLine() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.Line);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeLine)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape path for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapePath() {
        try
        {
            WithShape(layer => layer.Shape = PictureShapeKind.Path);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapePath)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill solid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillSolid() {
        try
        {
            WithFill(layer => layer.FillKind = PictureFillKind.Solid);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillSolid)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill linear gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillLinearGradient() {
        try
        {
            WithFill(layer => layer.FillKind = PictureFillKind.LinearGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillLinearGradient)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill radial gradient for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillRadialGradient() {
        try
        {
            WithFill(layer => layer.FillKind = PictureFillKind.RadialGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillRadialGradient)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets picture text font for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="font">Font value supplied to the picture editor operation and used when producing its result.</param>
    private void SetPictureTextFont(string font) {
        try
        {
            WithText(layer => layer.FontFamily = font);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetPictureTextFont)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets picture text size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetPictureTextSize(double value) {
        try
        {
            WithText(layer => layer.FontSizePx = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetPictureTextSize)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text size24 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize24() {
        try
        {
            SetPictureTextSize(24);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextSize24)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text size48 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize48() {
        try
        {
            SetPictureTextSize(48);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextSize48)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text size72 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize72() {
        try
        {
            SetPictureTextSize(72);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextSize72)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text size120 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize120() {
        try
        {
            SetPictureTextSize(120);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextSize120)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text size180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextSize180() {
        try
        {
            SetPictureTextSize(180);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextSize180)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle picture text bold for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextBold() {
        try
        {
            WithText(layer => layer.Bold = !layer.Bold);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TogglePictureTextBold)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle picture text italic for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextItalic() {
        try
        {
            WithText(layer => layer.Italic = !layer.Italic);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TogglePictureTextItalic)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle picture text shadow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TogglePictureTextShadow() {
        try
        {
            WithText(layer => layer.ShadowEnabled = !layer.ShadowEnabled);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TogglePictureTextShadow)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text align left for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignLeft() {
        try
        {
            WithText(layer => layer.Alignment = PictureTextAlignment.Left);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextAlignLeft)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text align center for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignCenter() {
        try
        {
            WithText(layer => layer.Alignment = PictureTextAlignment.Center);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextAlignCenter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text align right for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextAlignRight() {
        try
        {
            WithText(layer => layer.Alignment = PictureTextAlignment.Right);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextAlignRight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text color blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorBlue() {
        try
        {
            WithText(layer => layer.FillColor = "#17365d");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextColorBlue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text color black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorBlack() {
        try
        {
            WithText(layer => layer.FillColor = "#000000");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextColorBlack)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text color white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorWhite() {
        try
        {
            WithText(layer => layer.FillColor = "#ffffff");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextColorWhite)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text color red for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextColorRed() {
        try
        {
            WithText(layer => layer.FillColor = "#dc2626");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextColorRed)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text outline none for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineNone() {
        try
        {
            WithText(layer => { layer.OutlineColor = "transparent"; layer.OutlineWidthPx = 0; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextOutlineNone)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text outline thin for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineThin() {
        try
        {
            WithText(layer => { layer.OutlineColor = "#111827"; layer.OutlineWidthPx = 1; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextOutlineThin)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text outline thick for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void TextOutlineThick() {
        try
        {
            WithText(layer => { layer.OutlineColor = "#ffffff"; layer.OutlineWidthPx = 4; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(TextOutlineThick)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape fill solid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillSolid() {
        try
        {
            WithShape(layer => layer.FillKind = PictureFillKind.Solid);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeFillSolid)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape fill linear for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillLinear() {
        try
        {
            WithShape(layer => layer.FillKind = PictureFillKind.LinearGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeFillLinear)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape fill radial for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeFillRadial() {
        try
        {
            WithShape(layer => layer.FillKind = PictureFillKind.RadialGradient);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeFillRadial)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets shape colors for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="stroke">Stroke value supplied to the picture editor operation and used when producing its result.</param>
    private void SetShapeColors(string first, string second, string stroke) {
        try
        {
            WithShape(layer => { layer.FillColor = first; layer.SecondaryFillColor = second; layer.StrokeColor = stroke; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetShapeColors)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape colors blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsBlue() {
        try
        {
            SetShapeColors("#60a5fa", "#dbeafe", "#1d4ed8");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeColorsBlue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape colors green for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsGreen() {
        try
        {
            SetShapeColors("#4ade80", "#dcfce7", "#15803d");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeColorsGreen)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape colors orange for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsOrange() {
        try
        {
            SetShapeColors("#fb923c", "#ffedd5", "#c2410c");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeColorsOrange)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape colors mono for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeColorsMono() {
        try
        {
            SetShapeColors("#111827", "#ffffff", "#000000");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeColorsMono)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets shape stroke for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    private void SetShapeStroke(double width) {
        try
        {
            WithShape(layer => layer.StrokeWidthPx = width);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetShapeStroke)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape stroke0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke0() {
        try
        {
            SetShapeStroke(0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeStroke0)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape stroke1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke1() {
        try
        {
            SetShapeStroke(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeStroke1)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape stroke3 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke3() {
        try
        {
            SetShapeStroke(3);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeStroke3)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs shape stroke8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ShapeStroke8() {
        try
        {
            SetShapeStroke(8);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ShapeStroke8)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets fill colors for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="first">First value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the picture editor operation and used when producing its result.</param>
    private void SetFillColors(string first, string second) {
        try
        {
            WithFill(layer => { layer.PrimaryColor = first; layer.SecondaryColor = second; });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetFillColors)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill colors blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsBlue() {
        try
        {
            SetFillColors("#dbeafe", "#6366f1");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillColorsBlue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill colors green for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsGreen() {
        try
        {
            SetFillColors("#dcfce7", "#16a34a");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillColorsGreen)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill colors sunset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsSunset() {
        try
        {
            SetFillColors("#fde68a", "#f97316");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillColorsSunset)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill colors mono for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillColorsMono() {
        try
        {
            SetFillColors("#ffffff", "#111827");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillColorsMono)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetFillAngle(double value) {
        try
        {
            WithFill(layer => layer.AngleDegrees = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetFillAngle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill angle0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle0() {
        try
        {
            SetFillAngle(0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillAngle0)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill angle45 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle45() {
        try
        {
            SetFillAngle(45);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillAngle45)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill angle90 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle90() {
        try
        {
            SetFillAngle(90);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillAngle90)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill angle180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle180() {
        try
        {
            SetFillAngle(180);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillAngle180)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs fill angle270 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void FillAngle270() {
        try
        {
            SetFillAngle(270);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FillAngle270)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets layer opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetLayerOpacity(double value) {
        try
        {
            State.UpdateSelected(layer => layer.Opacity = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetLayerOpacity)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs layer opacity100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity100() {
        try
        {
            SetLayerOpacity(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerOpacity100)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs layer opacity75 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity75() {
        try
        {
            SetLayerOpacity(.75);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerOpacity75)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs layer opacity50 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity50() {
        try
        {
            SetLayerOpacity(.5);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerOpacity50)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs layer opacity25 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void LayerOpacity25() {
        try
        {
            SetLayerOpacity(.25);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerOpacity25)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle selected lock menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSelectedLockMenu()
    {
        try
        {
            if (State.SelectedLayer is PictureLayer layer) State.ToggleLock(layer.Id);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSelectedLockMenu)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle selected visibility menu for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ToggleSelectedVisibilityMenu()
    {
        try
        {
            if (State.SelectedLayer is PictureLayer layer) State.ToggleVisibility(layer.Id);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSelectedVisibilityMenu)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs checked text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="selected">Value indicating whether selected should apply to this operation.</param>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CheckedText(bool selected, string text) {
        try
        {
            return selected ? $"✓ {LT(text)}" : LT(text);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(CheckedText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change document name for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDocumentName(ChangeEventArgs args) {
        try
        {
            State.SetDocumentName(Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDocumentName)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change canvas width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasWidth(ChangeEventArgs args) {
        try
        {
            State.SetDocumentSize(Int(args, State.Document.WidthPx), State.Document.HeightPx);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeCanvasWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change canvas height for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasHeight(ChangeEventArgs args) {
        try
        {
            State.SetDocumentSize(State.Document.WidthPx, Int(args, State.Document.HeightPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeCanvasHeight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change background preset for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBackgroundPreset(ChangeEventArgs args) {
        try
        {
            State.SetBackground(Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeBackgroundPreset)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change canvas color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeCanvasColor(ChangeEventArgs args) {
        try
        {
            State.SetBackground(Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeCanvasColor)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change grid spacing for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeGridSpacing(ChangeEventArgs args) {
        try
        {
            State.SetGridSpacing(Int(args, State.Document.GridSpacingPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeGridSpacing)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle grid for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleGrid(ChangeEventArgs args) {
        try
        {
            State.SetGrid(Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleGrid)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle snap for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSnap(ChangeEventArgs args) {
        try
        {
            State.SetSnap(Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSnap)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change zoom for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeZoom(ChangeEventArgs args) {
        try
        {
            State.SetZoom(Number(args, State.Document.Zoom));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeZoom)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw tool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawTool(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureDrawTool>(Text(args), true, out var tool)) SetDrawTool(tool);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawTool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw color input for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawColorInput(ChangeEventArgs args) {
        try
        {
            ChangeDrawColor(Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawColorInput)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw secondary color input for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawSecondaryColorInput(ChangeEventArgs args) {
        try
        {
            ChangeDrawSecondaryColor(Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawSecondaryColorInput)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawWidth(ChangeEventArgs args) {
        try
        {
            SetDrawWidth(Number(args, _drawWidth));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw width slider for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawWidthSlider(ChangeEventArgs args) {
        try
        {
            SetDrawWidth(SliderToWidth(Number(args, BrushWidthSliderValue)));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawWidthSlider)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawOpacity(ChangeEventArgs args) {
        try
        {
     _drawOpacity = Math.Clamp(Number(args, _drawOpacity), 0, 1); _renderRequested = true; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawOpacity)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change draw hardness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeDrawHardness(ChangeEventArgs args) {
        try
        {
     _drawHardness = Math.Clamp(Number(args, _drawHardness), 0, 1); _renderRequested = true; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeDrawHardness)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs preset square for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetSquare() {
        try
        {
            State.SetDocumentSize(1200, 1200);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PresetSquare)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs preset landscape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetLandscape() {
        try
        {
            State.SetDocumentSize(1600, 1000);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PresetLandscape)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs preset full hd for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetFullHd() {
        try
        {
            State.SetDocumentSize(1920, 1080);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PresetFullHd)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs preset a4 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void PresetA4() {
        try
        {
            State.SetDocumentSize(2480, 3508);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PresetA4)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change layer name for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerName(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Name = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerName)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer x for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerX(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.X = Number(args, layer.X));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerX)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer y for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerY(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Y = Number(args, layer.Y));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerY)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerWidth(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Width = Number(args, layer.Width));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer height for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerHeight(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Height = Number(args, layer.Height));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerHeight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer rotation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerRotation(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("layer-rotation", layer => layer.Rotation = Number(args, layer.Rotation));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerRotation)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change layer opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeLayerOpacity(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("layer-opacity", layer => layer.Opacity = Number(args, layer.Opacity));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeLayerOpacity)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change blend mode for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBlendMode(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureBlendMode>(Text(args), true, out var value))
                State.UpdateSelected(layer => layer.BlendMode = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeBlendMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle selected visibility for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSelectedVisibility(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Visible = Bool(args), allowLocked: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSelectedVisibility)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle selected lock for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSelectedLock(ChangeEventArgs args) {
        try
        {
            State.UpdateSelected(layer => layer.Locked = Bool(args), allowLocked: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSelectedLock)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs end live edit for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>The void end live edit change event args state produced by the operation.</returns>
    /// <param name="_">_ value supplied to the picture editor operation and used when producing its result.</param>
    private void EndLiveEdit(ChangeEventArgs _) {
        try
        {
            State.EndLiveEdit();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(EndLiveEdit)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change raster fit for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterFit(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureRasterFitMode>(Text(args), true, out var value))
                WithRaster(layer => layer.FitMode = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterFit)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle raster flip horizontal for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleRasterFlipHorizontal(ChangeEventArgs args) {
        try
        {
            WithRaster(layer => layer.FlipHorizontal = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleRasterFlipHorizontal)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle raster flip vertical for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleRasterFlipVertical(ChangeEventArgs args) {
        try
        {
            WithRaster(layer => layer.FlipVertical = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleRasterFlipVertical)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change raster tint color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterTintColor(ChangeEventArgs args) {
        try
        {
            WithRaster(layer => layer.TintColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterTintColor)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change raster tint opacity for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRasterTintOpacity(ChangeEventArgs args) {
        try
        {
            WithRasterLive("raster-tint", layer => layer.TintOpacity = Number(args, layer.TintOpacity));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRasterTintOpacity)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change text content for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextContent(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.Text = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextContent)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text font for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextFont(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.FontFamily = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextFont)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextSize(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.FontSizePx = Number(args, layer.FontSizePx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextSize)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text alignment for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextAlignment(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureTextAlignment>(Text(args), true, out var value))
                WithText(layer => layer.Alignment = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextAlignment)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle text bold for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextBold(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.Bold = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleTextBold)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle text italic for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextItalic(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.Italic = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleTextItalic)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle text shadow for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleTextShadow(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.ShadowEnabled = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleTextShadow)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextFill(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.FillColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextFill)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text outline for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextOutline(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.OutlineColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextOutline)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text outline width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextOutlineWidth(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.OutlineWidthPx = Number(args, layer.OutlineWidthPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextOutlineWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change text shadow blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeTextShadowBlur(ChangeEventArgs args) {
        try
        {
            WithText(layer => layer.ShadowBlurPx = Number(args, layer.ShadowBlurPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeTextShadowBlur)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change shape kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeKind(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureShapeKind>(Text(args), true, out var value))
                WithShape(layer => layer.Shape = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeKind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape fill kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFillKind(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value)) WithShape(layer => layer.FillKind = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeFillKind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFill(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.FillColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeFill)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape secondary fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeSecondaryFill(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.SecondaryFillColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeSecondaryFill)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeFillAngle(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("shape-fill-angle", layer => { if (layer is ShapePictureLayer shape) shape.FillAngleDegrees = Number(args, shape.FillAngleDegrees); });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeFillAngle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape stroke for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeStroke(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.StrokeColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeStroke)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape stroke width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeStrokeWidth(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.StrokeWidthPx = Number(args, layer.StrokeWidthPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeStrokeWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape radius for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapeRadius(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.CornerRadiusPx = Number(args, layer.CornerRadiusPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapeRadius)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle shape path closed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleShapePathClosed(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.PathClosed = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleShapePathClosed)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle shape path smooth for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleShapePathSmooth(ChangeEventArgs args) {
        try
        {
            WithShape(layer => layer.PathSmooth = Bool(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleShapePathSmooth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void AddShapePathPoint() {
        try
        {
            WithShape(layer =>
    {
        layer.PathPoints ??= [];
        var previous = layer.PathPoints.LastOrDefault();
        layer.PathPoints.Add(new PicturePoint
        {
            X = Math.Clamp((previous?.X ?? layer.Width / 2) + 20, 0, Math.Max(1, layer.Width)),
            Y = Math.Clamp(previous?.Y ?? layer.Height / 2, 0, Math.Max(1, layer.Height))
        });
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(AddShapePathPoint)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Removes shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    private void RemoveShapePathPoint(int index) {
        try
        {
            WithShape(layer =>
    {
        if (layer.PathPoints is { Count: > 2 } && index >= 0 && index < layer.PathPoints.Count)
            layer.PathPoints.RemoveAt(index);
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RemoveShapePathPoint)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs reverse shape path for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ReverseShapePath() {
        try
        {
            WithShape(layer => { layer.PathPoints?.Reverse(); });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ReverseShapePath)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape path point x for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapePathPointX(int index, ChangeEventArgs args) {
        try
        {
            ChangeShapePathPoint(index, args, true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapePathPointX)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape path point y for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeShapePathPointY(int index, ChangeEventArgs args) {
        try
        {
            ChangeShapePathPoint(index, args, false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapePathPointY)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change shape path point for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="horizontal">Value indicating whether horizontal should apply to this operation.</param>
    private void ChangeShapePathPoint(int index, ChangeEventArgs args, bool horizontal) {
        try
        {
            WithShape(layer =>
    {
        if (layer.PathPoints is null || index < 0 || index >= layer.PathPoints.Count) return;
        var point = layer.PathPoints[index];
        if (horizontal) point.X = Math.Clamp(Number(args, point.X), -16384, 32768);
        else point.Y = Math.Clamp(Number(args, point.Y), -16384, 32768);
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeShapePathPoint)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change fill kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillKind(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureFillKind>(Text(args), true, out var value))
                WithFill(layer => layer.FillKind = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeFillKind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change fill primary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillPrimary(ChangeEventArgs args) {
        try
        {
            WithFill(layer => layer.PrimaryColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeFillPrimary)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change fill secondary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillSecondary(ChangeEventArgs args) {
        try
        {
            WithFill(layer => layer.SecondaryColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeFillSecondary)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change fill angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeFillAngle(ChangeEventArgs args) {
        try
        {
            WithFillLive("fill-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeFillAngle)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change render kind for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderKind(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PictureRenderKind>(Text(args), true, out var value))
                WithRender(layer => layer.RenderKind = value);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderKind)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render primary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderPrimary(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.PrimaryColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderPrimary)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render secondary for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSecondary(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.SecondaryColor = Text(args));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderSecondary)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render seed for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSeed(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.Seed = Int(args, layer.Seed));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderSeed)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render scale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderScale(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.Scale = Number(args, layer.Scale));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderScale)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render detail for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderDetail(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.Detail = Int(args, layer.Detail));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderDetail)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render softness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderSoftness(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.Softness = Number(args, layer.Softness));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderSoftness)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderContrast(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.RenderContrast = Number(args, layer.RenderContrast));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderContrast)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render stripe width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderStripeWidth(ChangeEventArgs args) {
        try
        {
            WithRender(layer => layer.StripeWidthPx = Number(args, layer.StripeWidthPx));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderStripeWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change render angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeRenderAngle(ChangeEventArgs args) {
        try
        {
            WithRenderLive("render-angle", layer => layer.AngleDegrees = Number(args, layer.AngleDegrees));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeRenderAngle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs randomize render for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RandomizeRender() {
        try
        {
            WithRender(layer => layer.Seed = Random.Shared.Next(1, int.MaxValue));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RandomizeRender)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs focus render properties for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task FocusRenderProperties() {
        try
        {
            return JS.InvokeVoidAsync("publisherStudio.focusElement", "picture-render-properties").AsTask();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FocusRenderProperties)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render primary white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryWhite() {
        try
        {
            WithRender(layer => layer.PrimaryColor = "#ffffff");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderPrimaryWhite)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render primary black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryBlack() {
        try
        {
            WithRender(layer => layer.PrimaryColor = "#000000");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderPrimaryBlack)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render primary blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderPrimaryBlue() {
        try
        {
            WithRender(layer => layer.PrimaryColor = "#2563eb");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderPrimaryBlue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render secondary white for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryWhite() {
        try
        {
            WithRender(layer => layer.SecondaryColor = "#ffffff");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSecondaryWhite)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render secondary black for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryBlack() {
        try
        {
            WithRender(layer => layer.SecondaryColor = "#000000");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSecondaryBlack)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render secondary blue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSecondaryBlue() {
        try
        {
            WithRender(layer => layer.SecondaryColor = "#60a5fa");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSecondaryBlue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render scale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderScale(double value) {
        try
        {
            WithRender(layer => layer.Scale = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderScale)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render scale24 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale24() {
        try
        {
            SetRenderScale(24);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderScale24)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render scale64 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale64() {
        try
        {
            SetRenderScale(64);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderScale64)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render scale128 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale128() {
        try
        {
            SetRenderScale(128);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderScale128)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render scale256 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderScale256() {
        try
        {
            SetRenderScale(256);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderScale256)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render detail for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderDetail(int value) {
        try
        {
            WithRender(layer => layer.Detail = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderDetail)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render detail1 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail1() {
        try
        {
            SetRenderDetail(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetail1)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render detail2 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail2() {
        try
        {
            SetRenderDetail(2);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetail2)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render detail4 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail4() {
        try
        {
            SetRenderDetail(4);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetail4)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render detail6 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail6() {
        try
        {
            SetRenderDetail(6);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetail6)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render detail8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderDetail8() {
        try
        {
            SetRenderDetail(8);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetail8)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render softness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderSoftness(double value) {
        try
        {
            WithRender(layer => layer.Softness = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderSoftness)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render softness0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness0() {
        try
        {
            SetRenderSoftness(0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftness0)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render softness25 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness25() {
        try
        {
            SetRenderSoftness(.25);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftness25)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render softness50 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness50() {
        try
        {
            SetRenderSoftness(.5);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftness50)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render softness75 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness75() {
        try
        {
            SetRenderSoftness(.75);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftness75)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render softness100 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderSoftness100() {
        try
        {
            SetRenderSoftness(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftness100)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderContrast(double value) {
        try
        {
            WithRender(layer => layer.RenderContrast = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderContrast)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render contrast05 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast05() {
        try
        {
            SetRenderContrast(.5);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrast05)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render contrast10 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast10() {
        try
        {
            SetRenderContrast(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrast10)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render contrast15 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast15() {
        try
        {
            SetRenderContrast(1.5);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrast15)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render contrast20 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast20() {
        try
        {
            SetRenderContrast(2);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrast20)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render contrast30 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderContrast30() {
        try
        {
            SetRenderContrast(3);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrast30)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render angle for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderAngle(double value) {
        try
        {
            WithRender(layer => layer.AngleDegrees = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderAngle)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render angle0 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle0() {
        try
        {
            SetRenderAngle(0);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngle0)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render angle45 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle45() {
        try
        {
            SetRenderAngle(45);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngle45)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render angle90 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle90() {
        try
        {
            SetRenderAngle(90);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngle90)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render angle180 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle180() {
        try
        {
            SetRenderAngle(180);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngle180)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render angle270 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderAngle270() {
        try
        {
            SetRenderAngle(270);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngle270)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets render stripe width for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    private void SetRenderStripeWidth(double value) {
        try
        {
            WithRender(layer => layer.StripeWidthPx = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SetRenderStripeWidth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render stripe8 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe8() {
        try
        {
            SetRenderStripeWidth(8);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripe8)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render stripe16 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe16() {
        try
        {
            SetRenderStripeWidth(16);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripe16)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render stripe32 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe32() {
        try
        {
            SetRenderStripeWidth(32);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripe32)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render stripe64 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe64() {
        try
        {
            SetRenderStripeWidth(64);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripe64)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs render stripe128 for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void RenderStripe128() {
        try
        {
            SetRenderStripeWidth(128);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripe128)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs reset render settings for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ResetRenderSettings() {
        try
        {
            WithRender(layer =>
    {
        layer.Scale = 90;
        layer.Detail = 4;
        layer.Softness = .6;
        layer.RenderContrast = 1;
        layer.AngleDegrees = 45;
        layer.StripeWidthPx = 32;
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ResetRenderSettings)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change brightness for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBrightness(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-brightness", layer => layer.Brightness = Number(args, layer.Brightness));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeBrightness)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change contrast for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeContrast(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-contrast", layer => layer.Contrast = Number(args, layer.Contrast));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeContrast)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change saturation for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeSaturation(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-saturation", layer => layer.Saturation = Number(args, layer.Saturation));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeSaturation)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change hue for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeHue(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-hue", layer => layer.HueRotation = Number(args, layer.HueRotation));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeHue)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change blur for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeBlur(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-blur", layer => layer.Blur = Number(args, layer.Blur));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeBlur)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change grayscale for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeGrayscale(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-grayscale", layer => layer.Grayscale = Number(args, layer.Grayscale));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeGrayscale)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change sepia for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeSepia(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-sepia", layer => layer.Sepia = Number(args, layer.Sepia));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeSepia)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change invert for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ChangeInvert(ChangeEventArgs args) {
        try
        {
            State.UpdateSelectedLive("adjust-invert", layer => layer.Invert = Number(args, layer.Invert));
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ChangeInvert)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs reset adjustments for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    private void ResetAdjustments()
    {
        try
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
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ResetAdjustments)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs with raster for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRaster(Action<RasterPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelected(_ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithRaster)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with raster live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRasterLive(string key, Action<RasterPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is RasterPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithRasterLive)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle SVG preserve aspect ratio for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    private void ToggleSvgPreserveAspectRatio(ChangeEventArgs args)
    {
        try
        {
            if (State.SelectedLayer is SvgPictureLayer svg)
                State.UpdateSelected(_ => svg.PreserveAspectRatio = Bool(args));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ToggleSvgPreserveAspectRatio)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs with text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithText(Action<TextPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is TextPictureLayer layer) State.UpdateSelected(_ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithText)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with shape for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithShape(Action<ShapePictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is ShapePictureLayer layer) State.UpdateSelected(_ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithShape)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with fill for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithFill(Action<FillPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelected(_ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithFill)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with fill live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithFillLive(string key, Action<FillPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is FillPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithFillLive)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with render for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRender(Action<RenderPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelected(_ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithRender)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs with render live for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the picture editor operation and used when producing its result.</param>
    private void WithRenderLive(string key, Action<RenderPictureLayer> update)
    {
        try
        {
            if (State.SelectedLayer is RenderPictureLayer layer) State.UpdateSelectedLive(key, _ => update(layer));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(WithRenderLive)} failed.");
            throw;
        }
    }


    /// <summary>
    /// Reads SVG text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="input">Input value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ReadSvgTextAsync(Stream input)
    {
        try
        {
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer).ConfigureAwait(true);
            return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(ReadSvgTextAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether supported image data URL for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSupportedImageDataUrl(string value) {
        try
        {
            return value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && value.Contains(",", StringComparison.Ordinal);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(IsSupportedImageDataUrl)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs fit raster canvas size for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="width">Width value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The picture image size produced by the operation.</returns>
    private PictureImageSize FitRasterCanvasSize(int width, int height)
    {
        try
        {
            if (width <= 0 || height <= 0) return new PictureImageSize { Width = 1200, Height = 800 };
            var scale = Math.Min(1d, 8192d / Math.Max(width, height));
            return new PictureImageSize
            {
                Width = Math.Clamp((int)Math.Round(width * scale), 16, 8192),
                Height = Math.Clamp((int)Math.Round(height * scale), 16, 8192)
            };
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(FitRasterCanvasSize)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs layer icon for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string LayerIconCss(PictureLayer layer) {
        try
        {
            return layer.Kind switch
    {
        PictureLayerKind.Raster => "pub-icon pub-icon-picture",
        PictureLayerKind.Text => "pub-icon pub-icon-text",
        PictureLayerKind.Shape => "pub-icon pub-icon-shape",
        PictureLayerKind.Fill => "pub-icon pub-icon-gradient",
        PictureLayerKind.Render => "pub-icon pub-icon-effects",
        PictureLayerKind.Paint => "pub-icon pub-icon-edit",
        PictureLayerKind.Vector => "pub-icon pub-icon-vector",
        _ => "pub-icon pub-icon-layers"
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerIconCss)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs picture text font size menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string PictureTextFontSizeMenuText(TextPictureLayer text) {
        try
        {
            return $"{LT("Font size")} · {Math.Round(text.FontSizePx).ToString(CultureInfo.InvariantCulture)} px";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(PictureTextFontSizeMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render scale menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderScaleMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Scale")} · {Math.Round(render.Scale).ToString(CultureInfo.InvariantCulture)} px";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderScaleMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render detail menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderDetailMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Detail")} · {render.Detail.ToString(CultureInfo.InvariantCulture)}";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderDetailMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render softness menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderSoftnessMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Softness")} · {Math.Round(render.Softness * 100).ToString(CultureInfo.InvariantCulture)}%";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderSoftnessMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render contrast menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderContrastMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Contrast")} · {render.RenderContrast.ToString("0.0", CultureInfo.InvariantCulture)}×";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderContrastMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render angle menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderAngleMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Angle")} · {Math.Round(render.AngleDegrees).ToString(CultureInfo.InvariantCulture)}°";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderAngleMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs render stripe width menu text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="render">Render value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderStripeWidthMenuText(RenderPictureLayer render) {
        try
        {
            return $"{LT("Stripe width")} · {Math.Round(render.StripeWidthPx).ToString(CultureInfo.InvariantCulture)} px";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(RenderStripeWidthMenuText)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs layer description for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string LayerDescription(PictureLayer layer) {
        try
        {
            return layer switch
    {
        RasterPictureLayer raster => LT(raster.FitMode.ToString()),
        SvgPictureLayer svg => string.IsNullOrWhiteSpace(svg.GroupPath) ? svg.SourceFormat : $"{svg.SourceFormat} · {svg.GroupPath}",
        TextPictureLayer text => Truncate(text.Text, 28),
        ShapePictureLayer shape => LT(shape.Shape.ToString()),
        FillPictureLayer fill => LT(fill.FillKind.ToString()),
        RenderPictureLayer render => LT(render.RenderKind.ToString()),
        PaintPictureLayer paint => paint.Strokes.Count == 1 ? $"1 {LT("stroke")}" : $"{paint.Strokes.Count} {LT("strokes")}",
        _ => LT(layer.Kind.ToString())
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(LayerDescription)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs truncate for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="length">Length value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Truncate(string value, int length) {
        try
        {
            return string.IsNullOrWhiteSpace(value)
        ? LT("Empty")
        : value.Length <= length ? value : value[..length] + "…";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Truncate)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs text for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Text(ChangeEventArgs args) {
        try
        {
            return Convert.ToString(args.Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Text)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs bool for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool Bool(ChangeEventArgs args) {
        try
        {
            return args.Value is bool value && value;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Bool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs number for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Number(ChangeEventArgs args, double fallback) {
        try
        {
            return double.TryParse(Text(args), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Number)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs int for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the picture editor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int Int(ChangeEventArgs args, int fallback) {
        try
        {
            return int.TryParse(Text(args), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Int)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs inv for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Inv(double value) {
        try
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Inv)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs safe color for <see cref="PictureEditor"/>, keeping the operation consistent with the state and invariants of the surrounding picture editor workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the picture editor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeColor(string? value) {
        try
        {
            return !string.IsNullOrWhiteSpace(value) && value.StartsWith('#') && value.Length is 4 or 7 ? value : "#000000";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(SafeColor)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="PictureEditor"/> and leaves the picture editor workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            State.Changed -= StateChanged;
            LocalGptConnection.Changed -= LocalGptConnectionChanged;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(Dispose)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="PictureEditor"/> and leaves the picture editor workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            State.Changed -= StateChanged;
            LocalGptConnection.Changed -= LocalGptConnectionChanged;
            _self?.Dispose();
            try
            {
                await DisposePictureRuntimeAsync().ConfigureAwait(true);
                if (_module is not null) await _module.DisposeAsync().ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(PictureEditor)}.{nameof(DisposeAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Renders the picture area selection Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding PublisherStudio interface.
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
    /// Renders the picture OCR Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding PublisherStudio interface.
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
    /// Renders the picture image size Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding PublisherStudio interface.
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
