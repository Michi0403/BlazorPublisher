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
$storyPath = Join-Path $root 'src\PublisherStudio.Web\Components\Editor\StoryEditor.razor'
$interopPath = Join-Path $root 'src\PublisherStudio.Web\wwwroot\js\publisherInterop.js'
if (-not (Test-Path -LiteralPath $panelPath -PathType Leaf)) { Fail 'PanelStudio.razor is missing.' }
if (-not (Test-Path -LiteralPath $storyPath -PathType Leaf)) { Fail 'StoryEditor.razor is missing.' }
if (-not (Test-Path -LiteralPath $interopPath -PathType Leaf)) { Fail 'publisherInterop.js is missing.' }

$panel = [System.IO.File]::ReadAllText($panelPath, [System.Text.Encoding]::UTF8)
$story = [System.IO.File]::ReadAllText($storyPath, [System.Text.Encoding]::UTF8)
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
Require $panel 'await FlushPanelStudioInteractionsAsync\(\)\.ConfigureAwait\(false\);[\s\S]{0,260}template\.Prototype = Files\.CloneElement\(SelectedElement\);' 'Saving/updating a reusable module must flush queued pointer bounds before cloning it.'
Require $panel 'private async Task Save\(\)[\s\S]{0,260}await FlushPanelStudioInteractionsAsync\(\)\.ConfigureAwait\(false\);' 'Applying a panel must flush queued pointer bounds before cloning the complete graph.'

$modeBlock = [regex]::Match($panel, '(?s)private void EnableInteractionPreview\(\).*?private Task EditSelectedComponent').Value
if ([string]::IsNullOrWhiteSpace($modeBlock)) { Fail 'Panel Studio mode methods could not be inspected.' }
Reject $modeBlock '_dropSurfaceBound\s*=\s*false|_dropSurfaceBindingKey\s*=\s*null' 'Arrange/interact mode changes must not tear down the browser binding.'
$refreshBlock = [regex]::Match($panel, '(?s)private void RefreshPreview\(\).*?private void ChangePanelName').Value
if ([string]::IsNullOrWhiteSpace($refreshBlock)) { Fail 'Panel Studio refresh method could not be inspected.' }
Reject $refreshBlock '_dropSurfaceBound\s*=\s*false|_dropSurfaceBindingKey\s*=\s*null' 'Preview refresh must retain the browser binding.'
Reject $panel 'case\s+"interact"\s*:' 'Browser command dispatch must not switch interaction mode implicitly.'

Require $story 'DocumentContent="@_content"' 'Story RichEdit must receive the loaded document as one-way initialization state; the live browser document must not be server-controlled on every edit.'
Require $story 'SelectionChanged="HandleSelectionChanged"' 'Story RichEdit selection may be observed for editor commands but must not be rebound from server state.'
Require $story '_suppressSelectionRender\s*=\s*true;' 'RichEdit selection notifications must suppress the automatic StoryEditor render they would otherwise schedule.'
Require $story 'protected override bool ShouldRender\(\)[\s\S]{0,420}if \(_suppressSelectionRender\)[\s\S]{0,220}return false;' 'StoryEditor must consume the RichEdit selection-render suppression before allowing normal renders.'
Reject $story '@bind-DocumentContent="_content"' 'Two-way RichEdit document binding replays server state into the live editor and can race caret/format ownership.'
Reject $story '@bind-Selection="_selection"' 'Two-way RichEdit selection binding can replay stale caret state into the live editor.'
Require $story '_mailMergeSettingsContent = null;[\s\S]{0,120}_selection = default!;[\s\S]{0,120}_editorRevision\+\+;' 'Every RichEdit document-generation replacement must discard the previous generation selection before recreating the editor.'
Require $story 'if \(!Visible\)[\s\S]{0,260}_selection = default!;[\s\S]{0,120}_richEdit = null;' 'Closing Story Editor must release both the prior selection and RichEdit instance reference.'

Require $interop "bindPanelStudioDropSurface\(element, dotNetReference, bindingId = ''\)" 'Browser binding must accept the stable binding id.'
Require $interop 'export function refreshPanelStudioDesignSurface\(element\)' 'Layout refresh must be independent from interaction binding lifecycle.'
Require $interop 'export async function flushPanelStudioInteractions\(element\)' 'Panel Studio must expose a browser-side queue flush before save snapshots.'
Require $interop 'await \(binding\.invokeQueue \|\| Promise\.resolve\(\)\);' 'The queue flush must wait for all previously queued .NET layout commits.'
Require $interop 'flushPanelStudioInteractions\(element\) \{ try \{ return flushPanelStudioInteractions\(element\);' 'The queue flush must be exposed through window.publisherStudio for Blazor JS interop.'
Require $interop 'refreshPanelStudioDesignSurface\(element\) \{ try \{ return refreshPanelStudioDesignSurface\(element\);' 'The layout refresh function must be exposed through window.publisherStudio for Blazor JS interop.'
Require $interop 'function publisherInputOwnerElement\(target\)' 'Controller arbitration must recognize focused or dragged native/DevExpress editor controls.'
Require $interop 'function publisherNativeInputOwnsInteraction\(\)' 'Controller arbitration must expose one shared native-input ownership decision.'
Require $interop 'publisherNativeInputOwnership\.pointerIds' 'Pointer-driven sliders and scroll controls must hold an input-ownership lease for the full drag sequence.'
Require $interop "window\.addEventListener\('blur',[\s\S]{0,180}publisherNativeInputOwnership\.pointerIds\.clear\(\)" 'Native editor pointer ownership must be released if the browser loses focus mid-drag.'
Require $interop "return publisherControllerState\.mode === 'control' && !publisherNativeInputOwnsInteraction\(\);" 'Controller semantic actions must yield while an editor control owns input.'
Require $interop 'return publisherInputOwnerElement\(target\) instanceof Element;' 'Panel Studio keyboard routing must use the same editor ownership boundary as application-level controller routing.'
Require $interop 'function isPublisherEditableTarget\(target\)[\s\S]{0,180}publisherInputOwnerElement\(target\)' 'Canvas keyboard routing must yield to the same shared editor/input ownership boundary.'
Require $interop 'function pointerDown\(state, event\)[\s\S]{0,260}publisherInputOwnerElement\(event\.target\)' 'Canvas pointer routing must not steal focus or pointer ownership from sliders, text editors, or other editor controls.'
Require $interop 'handlers\.windowPointerDown = event =>[\s\S]{0,420}publisherInputOwnerElement\(event\.target\)' 'Canvas gamepad activation must yield when a native or DevExpress editor receives pointer input.'
Require $interop "'dxbl-spinedit'[\s\S]{0,240}'dxbl-combo-box'[\s\S]{0,240}'dxbl-scroll-viewer'" 'Shared input ownership must recognize the actual DevExpress Blazor editor/scroll host tags emitted by the maintained UI.'
Require $interop "'\.dxreRoot'" 'Shared input ownership must treat the complete DevExpress RichEdit surface as vendor-owned input, including document caret and ribbon controls.'
Require $interop 'if \(currentHost\?\.contains\(event\.target\)\) return;' 'Story Editor layout refresh must not be scheduled by clicks inside the RichEdit host.'
Require $interop 'existing\.bindingId === normalizedBindingId' 'Repeated renders must reuse the existing binding instead of aborting it.'
Require $interop 'existing\.dotNetReference = dotNetReference \|\| existing\.dotNetReference;' 'An idempotent bind must refresh the .NET reference.'
Require $interop 'operation=\$\{operation\}; binding=\$\{binding\?\.bindingId' 'Browser cancellation diagnostics must identify the operation and binding.'
Require $interop 'ReportPanelInteractionError' 'Browser cancellation details must be reported to the component logger.'
Reject $interop "panelStudioInvoke\(binding, 'interact'\)" 'Gamepad or keyboard interop must not switch the editor into interaction mode.'

Write-Host 'Panel Studio/shared editor interaction lifecycle validation passed. Panel binding is stable, native/DevExpress editors own input, Story RichEdit live document/caret state is not server-rebound and selection notifications do not rerender it, controller actions yield without stale edges, and cancellations carry operation context.'
