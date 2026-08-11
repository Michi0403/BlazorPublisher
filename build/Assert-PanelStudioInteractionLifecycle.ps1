Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Panel Studio interaction lifecycle validation failed: $Message" }
function Require([string]$Text, [string]$Pattern, [string]$Message) {
    if (-not [regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) { Fail $Message }
}
function Reject([string]$Text, [string]$Pattern, [string]$Message) {
    if ([regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) { Fail $Message }
}

$root = Split-Path -Parent $PSScriptRoot
$panelPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\PanelStudio.razor'
$interopPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\publisherInterop.js'
if (-not (Test-Path -LiteralPath $panelPath -PathType Leaf)) { Fail 'PanelStudio.razor is missing.' }
if (-not (Test-Path -LiteralPath $interopPath -PathType Leaf)) { Fail 'publisherInterop.js is missing.' }

$panel = [System.IO.File]::ReadAllText($panelPath, [System.Text.Encoding]::UTF8)
$interop = [System.IO.File]::ReadAllText($interopPath, [System.Text.Encoding]::UTF8)

Require $panel 'private readonly string _interactionBindingId\s*=\s*Guid\.NewGuid\(\)\.ToString\("N"\);' 'A stable component-lifetime binding id is required.'
Require $panel 'data-panel-studio-binding-id="@_interactionBindingId"' 'The canvas must expose the stable binding id.'
Require $panel 'var bindingKey\s*=\s*_draft is null \? null : _interactionBindingId;' 'The binding key must not depend on interaction mode, view, preview revision, or authoring dimensions.'
Reject $panel 'var bindingKey[^\r\n]*(?:PanelDesignWidth|PanelDesignHeight|CanvasWidth|CanvasHeight)' 'Authoring layout dimensions must never become part of the browser interaction binding identity.'
Require $panel 'var designSurfaceLayoutKey\s*=\s*_draft is null \? null : \$"\{PanelDesignWidthPx\}:\{PanelDesignHeightPx\}";' 'Authoring layout changes require a separate non-lifecycle layout key.'
Require $panel 'bindPanelStudioDropSurface", _canvasElement, _self, _interactionBindingId\)\.ConfigureAwait\(true\)' 'The stable binding id must be passed to browser interop.'
Require $panel 'refreshPanelStudioDesignSurface", _canvasElement\)\.ConfigureAwait\(true\)' 'Canvas dimension changes must refresh layout without rebinding browser interaction.'
Require $panel 'TokenCancellationRequested:\{exception\.CancellationToken\.IsCancellationRequested\}' 'Cancellation diagnostics must include token state.'
Require $panel 'Panel Studio browser interaction ended normally\. Binding:' 'Expected browser shutdown and cancellation must be logged with binding context.'
Require $panel '_lastInteractionSurfaceNotification' 'Repeated browser interop failures must be notification-deduplicated instead of flooding the user interface.'
Require $panel 'await FlushPanelStudioInteractionsAsync\(\)\.ConfigureAwait\(true\);[\s\S]{0,260}template\.Prototype = Files\.CloneElement\(SelectedElement\);' 'Saving/updating a reusable module must flush queued pointer bounds before cloning it.'
Require $panel 'private async Task Save\(\)[\s\S]{0,260}await FlushPanelStudioInteractionsAsync\(\)\.ConfigureAwait\(true\);' 'Applying a panel must flush queued pointer bounds before cloning the complete graph.'

$modeBlock = [regex]::Match($panel, '(?s)private void EnableInteractionPreview\(\).*?private Task EditSelectedComponent').Value
if ([string]::IsNullOrWhiteSpace($modeBlock)) { Fail 'Panel Studio mode methods could not be inspected.' }
Reject $modeBlock '_dropSurfaceBound\s*=\s*false|_dropSurfaceBindingKey\s*=\s*null' 'Arrange/interact mode changes must not tear down the browser binding.'
$refreshBlock = [regex]::Match($panel, '(?s)private void RefreshPreview\(\).*?private void ChangePanelName').Value
if ([string]::IsNullOrWhiteSpace($refreshBlock)) { Fail 'Panel Studio refresh method could not be inspected.' }
Reject $refreshBlock '_dropSurfaceBound\s*=\s*false|_dropSurfaceBindingKey\s*=\s*null' 'Preview refresh must retain the browser binding.'
Reject $panel 'case\s+"interact"\s*:' 'Browser command dispatch must not switch interaction mode implicitly.'

Require $interop "bindPanelStudioDropSurface\(element, dotNetReference, bindingId = ''\)" 'Browser binding must accept the stable binding id.'
Require $interop 'export function refreshPanelStudioDesignSurface\(element\)' 'Layout refresh must be independent from interaction binding lifecycle.'
Require $interop 'export async function flushPanelStudioInteractions\(element\)' 'Panel Studio must expose a browser-side queue flush before save snapshots.'
Require $interop 'await \(binding\.invokeQueue \|\| Promise\.resolve\(\)\);' 'The queue flush must wait for all previously queued .NET layout commits.'
Require $interop 'flushPanelStudioInteractions\(element\) \{ try \{ return flushPanelStudioInteractions\(element\);' 'The queue flush must be exposed through window.publisherStudio for Blazor JS interop.'
Require $interop 'refreshPanelStudioDesignSurface\(element\) \{ try \{ return refreshPanelStudioDesignSurface\(element\);' 'The layout refresh function must be exposed through window.publisherStudio for Blazor JS interop.'
Require $interop 'existing\.bindingId === normalizedBindingId' 'Repeated renders must reuse the existing binding instead of aborting it.'
Require $interop 'existing\.dotNetReference = dotNetReference \|\| existing\.dotNetReference;' 'An idempotent bind must refresh the .NET reference.'
Require $interop 'operation=\$\{operation\}; binding=\$\{binding\?\.bindingId' 'Browser cancellation diagnostics must identify the operation and binding.'
Require $interop 'ReportPanelInteractionError' 'Browser cancellation details must be reported to the component logger.'
Reject $interop "panelStudioInvoke\(binding, 'interact'\)" 'Gamepad or keyboard interop must not switch the editor into interaction mode.'

Write-Host 'Panel Studio interaction lifecycle validation passed. Binding is stable across renders and mode changes, implicit mode switching is forbidden, and cancellations carry operation context.'
