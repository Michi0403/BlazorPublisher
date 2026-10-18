[CmdletBinding()]
param(
    [string]$RepositoryRoot = '',
    [ValidateSet('publisherstudio')][string]$Product = 'publisherstudio'
)
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

$ErrorActionPreference = 'Stop'
$webRoot = Join-Path $RepositoryRoot 'src/PublisherStudio.Web'
$components = Join-Path $webRoot 'Components'
$cssPath = Join-Path $webRoot 'wwwroot/css/site.css'
$issues = [Collections.Generic.List[string]]::new()
if (-not (Test-Path -LiteralPath $components -PathType Container)) { throw "Missing Razor component root: $components" }

# Keep the 4.1.9 semantic policy identities, including RAZORUI0015 structural renderer
# ownership, rather than reducing the old 15-rule contract to a tag-count scan.
# Policy: maintenance-only DevExpress wrapper shells remain box-neutral.
# Policy: generic StackLayout wrappers and unexplained Grid-to-Stack nesting are rejected.
$COMPONENT_BOUNDARY_CLASS = 'razor-component-boundary'
$LAYOUT_COMPATIBILITY_CSS_MARKER = 'DEVEXPRESS_RAZOR_LAYOUT_COMPATIBILITY'
$POPUP_VIEWPORT_CSS_MARKER = 'DEVEXPRESS_POPUP_VIEWPORT_CONTRACT'
$STRUCTURAL_RENDERER_MARKER_RE = '(?is)@\*\s*razor-structural-renderer\s*\|\s*reason:\s*(?<reason>.*?)\s*\*@'
$PRIMARY_LAYOUT_MARKER_RE = '(?is)@\*\s*razor-primary-layout:\s*(?<layout>Dx(?:GridLayout|Carousel|Drawer|FormLayout|Splitter|StackLayout|Tabs))\s*\|\s*reason:\s*(?<reason>.*?)\s*\*@'
$STACK_CHILD_INTENT_RE = '(?is)@\*\s*razor-layout-intent:\s*stack-child\s*\|\s*reason:\s*(?<reason>.*?)\s*\*@'
$layoutNames = @('DxGridLayout','DxCarousel','DxDrawer','DxFormLayout','DxSplitter','DxStackLayout','DxTabs')
$layoutRegex = '(?i)^Dx(?:GridLayout|Carousel|Drawer|FormLayout|Splitter|StackLayout|Tabs)$'
$markupTagPattern = '(?i)<\s*(?!/|!|\?)(?<tag>[A-Za-z][\w.:]*)\b'

function Add-Finding([string]$File, [string]$Code, [string]$Message) { $issues.Add("${File}: $Code $Message") }
function Test-RequiredMarkers([string]$File, [string]$Text, [string[]]$Markers, [string]$Rule) {
    foreach ($marker in $Markers) {
        if ($Text.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) { Add-Finding $File $Rule "missing maintained stylesheet selector: $marker" }
    }
}
$appShell = Join-Path $components 'App.razor'
if (-not (Test-Path -LiteralPath $appShell -PathType Leaf) -or [IO.File]::ReadAllText($appShell).IndexOf('css/site.css',[StringComparison]::Ordinal) -lt 0) {
    Add-Finding 'Components/App.razor' 'RAZORUI0012' 'Site layout stylesheet must be loaded by App.razor.'
}
if (-not (Test-Path -LiteralPath $cssPath -PathType Leaf)) {
    Add-Finding 'wwwroot/css/site.css' 'RAZORUI0012' 'DevExpress layout and popup stylesheet is missing.'
} else {
    $css = [IO.File]::ReadAllText($cssPath)
    Test-RequiredMarkers 'wwwroot/css/site.css' $css @(
        $LAYOUT_COMPATIBILITY_CSS_MARKER,'.razor-component-boundary','.razor-component-layout-owner',
        '.razor-section-layout-owner','.dxbl-row','dxbl-form-layout-item','.dxbl-fl-ctrl',
        'display: contents !important'
    ) 'RAZORUI0012'
    Test-RequiredMarkers 'wwwroot/css/site.css' $css @(
        $POPUP_VIEWPORT_CSS_MARKER,'dxbl-popup-root > dxbl-popup-cell > dxbl-modal',
        'dxbl-modal-dialog.dxbl-popup','.dxbl-modal-content','.dxbl-modal-body',
        'max-width: calc(100vw - 1rem) !important','max-height: calc(100dvh - 1rem) !important',
        'overflow: auto !important','overscroll-behavior: contain','.page-effect-studio'
    ) 'RAZORUI0013'
}

$files = @(Get-ChildItem -LiteralPath $components -Recurse -File -Filter '*.razor' | Where-Object { $_.Name -ne '_Imports.razor' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
    $source = [IO.File]::ReadAllText($file.FullName)
    # Razor comments, HTML comments, and @code/@functions are not rendered markup.
    $markup = [regex]::Replace($source, '(?s)@\*.*?\*@|<!--.*?-->', ' ')
    # Razor directives can contain generic C# types (ILogger<T>), not HTML tags.
    $markup = [regex]::Replace($markup, '(?m)^[ \t]*@(?:page|using|inject|inherits|implements|namespace|attribute|typeparam|layout|rendermode|preservewhitespace)\b[^\r\n]*', ' ')
    $codeIndex = [regex]::Match($markup, '(?m)^\s*@(?:code|functions)\s*\{')
    if ($codeIndex.Success) { $markup = $markup.Substring(0,$codeIndex.Index) }
    $tags = @([regex]::Matches($markup, $markupTagPattern))
    $tagNames = @($tags | ForEach-Object { $_.Groups['tag'].Value })
    $layouts = @($tags | Where-Object { $_.Groups['tag'].Value -match $layoutRegex })
    $structural = [regex]::Match($source.Substring(0,[Math]::Min(2400,$source.Length)), $STRUCTURAL_RENDERER_MARKER_RE)
    $structuralRenderer = $structural.Success -and $structural.Groups['reason'].Value.Trim().Length -ge 12

    if ($markup -match '(?is)<\s*/?\s*section\b') { Add-Finding $relative 'RAZORUI0001' 'Native section is not an approved DevExpress layout owner.' }
    if ($markup -match '(?is)<\s*/?\s*dialog\b') { Add-Finding $relative 'RAZORUI0002' 'Native dialog is not DxPopup.' }
    if ($markup -match '(?is)<\s*(?:div|main|article|aside|nav|form|fieldset)\b[^>]*(?:modal-backdrop|dialog-backdrop|role\s*=\s*["'']dialog["''])') {
        Add-Finding $relative 'RAZORUI0005' 'Manual modal/backdrop or native dialog role bypasses DxPopup ownership.'
    }
    if ($markup -match '(?is)<\s*BodyContentTemplate\b[^>]*>\s*<\s*DxFormLayout\b[^>]*class\s*=\s*["''][^"'']*razor-section-layout-owner') {
        Add-Finding $relative 'RAZORUI0014' 'DxPopup BodyContentTemplate may not gain an artificial FormLayout owner.'
    }
    if ($structuralRenderer) {
        if ($markup -match 'razor-(?:component|section)-layout-owner') {
            Add-Finding $relative 'RAZORUI0015' 'Geometry-sensitive structural renderer must stay direct-root without a maintenance wrapper.'
        }
    } elseif ($tags.Count -gt 0 -and $file.Name -ne 'App.razor') {
        if ($layouts.Count -eq 0) {
            Add-Finding $relative 'RAZORUI0003' 'Rendered UI lacks an approved DevExpress layout owner.'
        } else {
            $first = $tags[0]
            $firstTag = $first.Groups['tag'].Value
            $openingEnd = $markup.IndexOf('>',$first.Index)
            $opening = if ($openingEnd -ge 0) { $markup.Substring($first.Index,$openingEnd-$first.Index+1) } else { $first.Value }
            $hasBoundary = $firstTag -ieq 'div' -and $opening -match '(?is)class\s*=\s*["''][^"'']*\brazor-component-boundary\b'
            if (-not $hasBoundary) { Add-Finding $relative 'RAZORUI0004' 'Rendered UI must start with its containment-only razor-component-boundary.' }
            $primary = $layouts[0]
            $primaryName = $primary.Groups['tag'].Value
            if ($primaryName -ne 'DxFormLayout') {
                $window = $source.Substring(0,[Math]::Min($source.Length,[Math]::Max($primary.Index+800,1600)))
                $marker = [regex]::Match($window,$PRIMARY_LAYOUT_MARKER_RE)
                if (-not ($marker.Success -and $marker.Groups['layout'].Value -ieq $primaryName -and $marker.Groups['reason'].Value.Trim().Length -ge 12)) {
                    Add-Finding $relative 'RAZORUI0006' "Primary $primaryName needs a nearby documented reason; DxFormLayout is the default."
                }
            }
            if ($markup -match '(?is)<\s*(?:DxTextBox|DxMemo|DxSpinEdit|DxComboBox|DxTagBox|DxCheckBox|DxDateEdit|DxDateRangePicker|DxTimeEdit|DxMaskedInput|DxRadioGroup)\b' -and $tagNames -notcontains 'DxFormLayout') {
                Add-Finding $relative 'RAZORUI0008' 'Editor fields require DxFormLayout ownership.'
            }
        }
    }
    $seenContexts = @{}
    foreach ($template in [regex]::Matches($markup,'(?is)<\s*DxFormLayoutItem\b[^>]*>\s*<\s*Template(?<attrs>[^>]*)>')) {
        $context = [regex]::Match($template.Groups['attrs'].Value,'(?i)\bContext\s*=\s*["''](?<name>[^"'']+)["'']')
        if (-not $context.Success) { Add-Finding $relative 'RAZORUI0010' 'DxFormLayoutItem Template must have an explicit Context.' }
        elseif ($seenContexts.ContainsKey($context.Groups['name'].Value)) { Add-Finding $relative 'RAZORUI0011' 'DxFormLayoutItem Template Context must be unique within the component.' }
        else { $seenContexts[$context.Groups['name'].Value] = $true }
    }
    if ($markup -match '(?is)<\s*DxStackLayout\b[^>]*class\s*=\s*["''][^"'']*razor-(?:component|section)-layout-owner') {
        Add-Finding $relative 'RAZORUI0007' 'Generic layout ownership may not use DxStackLayout.'
    }
    $gridDepth = 0
    foreach ($layout in [regex]::Matches($markup,'(?is)<\s*(?<close>/?)\s*(?<name>DxGridLayout|DxStackLayout)\b[^>]*>')) {
        $name = $layout.Groups['name'].Value
        if ($name -ieq 'DxGridLayout') {
            if ($layout.Groups['close'].Value) { $gridDepth = [Math]::Max(0,$gridDepth-1) } else { $gridDepth++ }
        } elseif (-not $layout.Groups['close'].Value -and $gridDepth -gt 0) {
            $begin = [Math]::Max(0,$layout.Index-900)
            $end = [Math]::Min($source.Length,$layout.Index+300)
            $window = $source.Substring($begin,$end-$begin)
            $intent = [regex]::Match($window,$STACK_CHILD_INTENT_RE)
            if (-not ($intent.Success -and $intent.Groups['reason'].Value.Trim().Length -ge 12)) {
                Add-Finding $relative 'RAZORUI0009' 'Grid-to-Stack nesting needs documented one-dimensional subgroup intent.'
            }
        }
    }
}
if ($issues.Count -gt 0) {
    foreach ($finding in $issues) { [Console]::Error.WriteLine($finding) }
    throw "PublisherStudio PowerShell Razor architecture validation found $($issues.Count) issue(s)."
}
Write-Host "PublisherStudio Razor architecture, DevExpress FormLayout/Popup, and structural-renderer ownership validation passed for $($files.Count) component(s)."
