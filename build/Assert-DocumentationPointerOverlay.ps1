$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }

$root = Split-Path -Parent $PSScriptRoot
$jsPath = Join-Path $root 'docs/templates/publisherstudio/public/main.js'
$cssPath = Join-Path $root 'docs/templates/publisherstudio/public/main.css'

if (-not (Test-Path -LiteralPath $jsPath) -or -not (Test-Path -LiteralPath $cssPath)) {
    Fail 'PublisherStudio documentation pointer-overlay sources are missing.'
}

$js = Get-Content -LiteralPath $jsPath -Raw
$css = Get-Content -LiteralPath $cssPath -Raw

$requiredJs = @(
    'function ensureKawaiiPointerOverlay()',
    'overlay.className = "publisherstudio-pointer-overlay";',
    'overlay.appendChild(paw);',
    'overlay.appendChild(trail);',
    'overlay.appendChild(sparkle);',
    'overlay.appendChild(scratch);',
    'overlay.appendChild(pop);'
)
foreach ($marker in $requiredJs) {
    if (-not $js.Contains($marker)) { Fail "PublisherStudio documentation pointer-overlay marker is missing: $marker" }
}

$forbiddenJs = @(
    'document.body.appendChild(paw);',
    'document.body.appendChild(trail);',
    'document.body.appendChild(sparkle);',
    'document.body.appendChild(scratch);',
    'document.body.appendChild(pop);'
)
foreach ($marker in $forbiddenJs) {
    if ($js.Contains($marker)) { Fail "PublisherStudio transient pointer decoration escaped the viewport overlay: $marker" }
}

$requiredCss = @(
    '.publisherstudio-pointer-overlay {',
    'contain: strict;',
    'overflow: clip;',
    'position: fixed !important;',
    'body > :not(.publisherstudio-kawaii-sky):not(.publisherstudio-pointer-overlay) { position: relative; z-index: 2; }'
)
foreach ($marker in $requiredCss) {
    if (-not $css.Contains($marker)) { Fail "PublisherStudio documentation pointer-overlay CSS contract is missing: $marker" }
}

Write-Output 'Documentation pointer-overlay validation passed: transient paw/cursor effects are viewport-contained and excluded from document-flow stacking.'
