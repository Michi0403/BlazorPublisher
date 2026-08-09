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
$panelViewPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\PanelView.razor'
$dataVisualHostPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\DataVisualClientHost.razor'
$sitePath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\css\site.css'
$interopPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\publisherInterop.js'
$liveDataPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\liveDataInterop.js'
foreach ($path in @($panelStudioPath, $panelViewPath, $dataVisualHostPath, $sitePath, $interopPath, $liveDataPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required source is missing: $path" }
}

$panelStudio = [IO.File]::ReadAllText($panelStudioPath)
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
Reject $panelStudio '</PanelView>[\s\S]{0,500}<div class="panel-studio-hit-layer"' 'A hit layer outside PanelView would reintroduce split coordinate owners.'
Require $site 'panel-layout-responsive:not\(\.panel-force-canvas\)' 'Responsive presentation CSS must not override forced authoring canvas coordinates.'
Reject $site '\.panel-layout-responsive\s+\.publication-panel-element' 'Responsive element rules must explicitly exclude panel-force-canvas authoring mode.'
Require $site 'publication-panel-viewport\[data-panel-authoring-viewport="true"\]>.panel-studio-hit-layer' 'Hitbox positioning must be anchored to the publication-panel viewport.'
Require $interop 'function panelStudioCoordinateSurface\(element\)' 'Browser coordinate conversion must resolve the panel authoring viewport.'
Require $interop 'panelStudioCoordinateSurface\(element\) \|\| element' 'Drop-point conversion must use the authoring viewport when available.'
Require $interop 'syncPanelStudioDesignSurface\(element\)' 'Panel Studio must uniformly fit the 96-DPI design surface into the editor workspace.'
Require $liveData 'new ResizeObserver' 'Data visuals must observe their actual rendered host size.'
Require $liveData 'visualSizeUsable' 'Data visual rendering must reject degenerate one-pixel layout measurements.'
Require $liveData 'resizeObserver\?\.disconnect' 'Data visual resize observers must be disposed with their widgets.'
Require $dataVisualHost '_lastClientConfigJson' 'Blazor re-renders must not recreate unchanged DevExtreme visual instances.'

Write-Host 'Panel Studio authoring geometry validation passed. Live content, hitboxes and drop coordinates share one viewport; responsive authoring is isolated; DataVisuals resize from their actual host.'
