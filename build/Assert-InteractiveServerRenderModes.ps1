Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "InteractiveServer render-mode validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $root 'src\PublisherStudio.Web'
$expected = [ordered]@{
    # InteractiveServer enables prerendering by default. Keep the same page contract used by LocalGPT:
    # route pages own the InteractiveServer boundary; their child/editor components inherit that circuit.
    'Components/Pages/Editor.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Help.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Localization.razor' = '@rendermode InteractiveServer'
    'Components/Pages/OrganicPlugins.razor' = '@rendermode InteractiveServer'
    # This tiny diagnostics island intentionally does not prerender because its only purpose is browser attachment.
    'Components/Layout/JavaScriptDiagnosticsBridge.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
}
$utf8 = New-Object System.Text.UTF8Encoding($false)

foreach ($entry in $expected.GetEnumerator()) {
    $relative = [string]$entry.Key
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required interactive component is missing: $relative" }
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    $first = @($text -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })[0].Trim()
    if ($first -cne [string]$entry.Value) {
        Fail "Render mode changed in $relative. Expected first directive '$($entry.Value)' but found '$first'."
    }
}

# Exactly the reviewed page/island set may create render boundaries. Nested editor components must inherit the
# parent page's InteractiveServer circuit instead of creating competing circuits or changing prerender semantics.
$allRazor = @(Get-ChildItem -LiteralPath (Join-Path $appRoot 'Components') -Recurse -File -Filter '*.razor')
foreach ($file in $allRazor) {
    $relative = $file.FullName.Substring($appRoot.Length + 1).Replace('\','/')
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    if ($text.IndexOf('@rendermode', [StringComparison]::Ordinal) -ge 0 -and -not $expected.Contains($relative)) {
        Fail "$relative must inherit its owning page/island InteractiveServer circuit; unexpected nested @rendermode is forbidden."
    }
}

# Every routed application page except the static fatal-error fallback must be InteractiveServer with default
# prerendering enabled. This catches new pages before they silently become static SSR.
$pagesRoot = Join-Path $appRoot 'Components\Pages'
foreach ($file in @(Get-ChildItem -LiteralPath $pagesRoot -Recurse -File -Filter '*.razor')) {
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    if ($text.IndexOf('@page ', [StringComparison]::Ordinal) -lt 0) { continue }
    $relative = $file.FullName.Substring($appRoot.Length + 1).Replace('\','/')
    if ($relative -eq 'Components/Pages/Error.razor') {
        if ($text.IndexOf('@rendermode', [StringComparison]::Ordinal) -ge 0) { Fail 'Error.razor is the static fatal-error fallback and must not open a circuit.' }
        continue
    }
    if (-not $expected.Contains($relative) -or $expected[$relative] -cne '@rendermode InteractiveServer') {
        Fail "$relative is a routed application page but is not registered as a prerendered InteractiveServer page."
    }
}

$importsPath = Join-Path $appRoot 'Components\_Imports.razor'
$appPath = Join-Path $appRoot 'Components\App.razor'
$programPath = Join-Path $appRoot 'Program.cs'
foreach ($path in @($importsPath, $appPath, $programPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required render architecture file is missing: $path" }
}
$imports = [System.IO.File]::ReadAllText($importsPath, $utf8)
$app = [System.IO.File]::ReadAllText($appPath, $utf8)
$program = [System.IO.File]::ReadAllText($programPath, $utf8)
if (-not $imports.Contains('@using static Microsoft.AspNetCore.Components.Web.RenderMode')) { Fail 'Components/_Imports.razor no longer imports RenderMode.' }
if (-not $program.Contains('AddInteractiveServerComponents()')) { Fail 'Program.cs no longer registers interactive server components.' }
if (-not $program.Contains('AddInteractiveServerRenderMode()')) { Fail 'Program.cs no longer maps InteractiveServer.' }
if ($app -match '<Routes\s+@rendermode' -or $app -match '<HeadOutlet\s+@rendermode') { Fail 'App.razor must not replace reviewed page/island render modes with a single root boundary.' }
if (-not $app.Contains('<Routes />')) { Fail 'App.razor no longer renders Routes through the reviewed page-level render-mode contract.' }

# The Editor is prerendered. A prerender-only instance is disposed before browser attachment, so browser cleanup
# must be gated on successful OnAfterRenderAsync attachment instead of probing IJSRuntime during Dispose.
$editor = [System.IO.File]::ReadAllText((Join-Path $appRoot 'Components\Pages\Editor.razor'), $utf8)
foreach ($token in @(
    'private bool _interactiveAttached;',
    '_interactiveAttached = true;',
    'if (_interactiveAttached)',
    'Editor disposal skipped browser hotkey cleanup because this component instance never attached to an interactive browser circuit'
)) {
    if (-not $editor.Contains($token)) { Fail "Editor.razor is missing the prerender/browser-attachment safety contract: $token" }
}

Write-Host "InteractiveServer render-mode validation passed for 4 prerendered PublisherStudio pages, 1 browser-only diagnostics island, and inherited child-component circuits." -ForegroundColor Green
