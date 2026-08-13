Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Panel Studio authoring geometry validation failed: $Message" }
function Require([string]$Text, [string]$Pattern, [string]$Message) {
    if (-not [regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) { Fail $Message }
}
function Reject([string]$Text, [string]$Pattern, [string]$Message) {
    if ([regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) { Fail $Message }
}

$root = Split-Path -Parent $PSScriptRoot
$panelStudioPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\PanelStudio.razor'
$editorPath = Join-Path $root 'src\PublisherStudio.Web\Components\Pages\Editor.razor'
$panelViewPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\PanelView.razor'
$dataVisualHostPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\DataVisualClientHost.razor'
$sitePath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\css\site.css'
$interopPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\publisherInterop.js'
$liveDataPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\liveDataInterop.js'
foreach ($path in @($panelStudioPath, $editorPath, $panelViewPath, $dataVisualHostPath, $sitePath, $interopPath, $liveDataPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required source is missing: $path" }
}

$panelStudio = [IO.File]::ReadAllText($panelStudioPath)
$editor = [IO.File]::ReadAllText($editorPath)
$panelView = [IO.File]::ReadAllText($panelViewPath)
$dataVisualHost = [IO.File]::ReadAllText($dataVisualHostPath)
$site = [IO.File]::ReadAllText($sitePath)
$interop = [IO.File]::ReadAllText($interopPath)
$liveData = [IO.File]::ReadAllText($liveDataPath)

Require $panelView 'panel-force-canvas' 'ForceCanvasLayout must create an explicit CSS escape hatch from responsive layout rules.'
Require $panelView 'data-panel-authoring-viewport=' 'PanelView must identify the viewport that owns authoring coordinates.'
Require $panelView '@AuthoringOverlay' 'The authoring overlay must render inside PanelView rather than beside it.'
Require $panelStudio '<AuthoringOverlay>[\s\S]*panel-studio-hit-layer' 'Selection hitboxes must be supplied through the PanelView authoring overlay.'
Require $panelStudio 'data-panel-studio-design-width="@PanelDesignWidthPx"' 'Panel Studio must expose the edited panel width in 96-DPI design pixels.'
Require $panelStudio 'data-panel-studio-design-height="@PanelDesignHeightPx"' 'Panel Studio must expose the edited panel height in 96-DPI design pixels.'
Require $panelStudio 'class="panel-studio-design-frame" data-panel-studio-design-frame' 'Panel Studio must isolate the selected panel inside its own authoring design frame.'
Require $panelStudio 'PreviewViewportPx[\s\S]{0,900}_draft\?\.CanvasWidth' 'Panel Studio design-size mode must come from the panel-local CanvasWidth, while named preview sizes may simulate alternate viewports.'
Require $panelStudio 'PreviewViewportPx[\s\S]{0,900}_draft\?\.CanvasHeight' 'Panel Studio design-size mode must come from the panel-local CanvasHeight, while named preview sizes may simulate alternate viewports.'
Reject $panelStudio 'PanelDesignWidthPxValue[^\r\n]*_draft\.Width' 'Panel Studio must not size the authoring surface from the Mainframe Width.'
Reject $panelStudio 'PanelDesignHeightPxValue[^\r\n]*_draft\.Height' 'Panel Studio must not size the authoring surface from the Mainframe Height.'
Reject $panelStudio '</PanelView>[\s\S]{0,500}<div class="panel-studio-hit-layer"' 'A hit layer outside PanelView would reintroduce split coordinate owners.'
Require $site 'panel-layout-responsive:not\(\.panel-force-canvas\)' 'Responsive presentation CSS must not override forced authoring canvas coordinates.'
Reject $site '\.panel-layout-responsive\s+\.publication-panel-element' 'Responsive element rules must explicitly exclude panel-force-canvas authoring mode.'
Require $site 'publication-panel-viewport\[data-panel-authoring-viewport="true"\]>\[data-panel-canvas-region\]>.panel-studio-hit-layer' 'Hitbox positioning must be anchored to the authored canvas region inside the publication-panel viewport.'
Require $site 'panel-studio-design-frame>.publication-panel' 'The authoring frame must own the selected panel size independently from generic Mainframe panel sizing.'
Require $interop 'function panelStudioCoordinateSurface\(element\)' 'Browser coordinate conversion must resolve the panel authoring canvas region.'
Require $interop 'panelStudioCoordinateSurface\(element\) \|\| element' 'Drop-point conversion must use the authoring viewport when available.'
Require $interop 'syncPanelStudioDesignSurface\(element\)' 'Panel Studio must uniformly fit the 96-DPI design surface into the editor workspace.'
Require $interop 'Math\.min\(8, availableWidth / width, availableHeight / height\)' 'Panel Studio must uniformly zoom the selected panel to a human-usable fit while preserving its own aspect ratio and coordinates.'
Require $interop "querySelector\('\[data-panel-studio-design-frame\]'\)" 'Panel Studio layout sync must track the dedicated design frame.'
Require $liveData 'new ResizeObserver' 'Data visuals must observe their actual rendered host size.'
Require $liveData 'visualSizeUsable' 'Data visual rendering must reject degenerate one-pixel layout measurements.'
Require $liveData 'resizeObserver\?\.disconnect' 'Data visual resize observers must be disposed with their widgets.'
Require $dataVisualHost '_lastClientConfigJson' 'Blazor re-renders must not recreate unchanged DevExtreme visual instances.'
Require $editor 'var fillsLocalCanvas = html is not null' 'Standalone HTML must explicitly test whether Panel Studio local geometry is still canvas-equivalent.'
Require $editor 'Math\.Abs\(html\.Width - draft\.CanvasWidth\)' 'Standalone HTML width must be compared with the panel-local canvas before lightweight apply.'
Require $editor 'Math\.Abs\(html\.Height - draft\.CanvasHeight\)' 'Standalone HTML height must be compared with the panel-local canvas before lightweight apply.'
Require $editor 'State\.PromoteSelectedHtmlEmbedToPanel\(draft\)' 'Authored local HTML geometry must promote to a panel instead of being discarded.'

Write-Host 'Panel Studio authoring geometry validation passed. The selected panel owns a centered fitted design frame; live content, hitboxes and drop coordinates share one aspect-preserving canvas region; preview viewport simulation is isolated; responsive authoring is explicit; DataVisuals resize from their actual host.'
