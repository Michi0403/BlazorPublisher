Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'async-continuation-baseline.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail 'The async continuation baseline is missing.' }
$utf8 = New-Object System.Text.UTF8Encoding($false)
$manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1) { Fail "Unsupported baseline schema version: $($manifest.schemaVersion)" }
$appRoot = Join-Path $root ([string]$manifest.sourceRoot).Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $appRoot -PathType Container)) { Fail "Source root is missing: $appRoot" }

$baseline = @{}
foreach ($property in $manifest.files.PSObject.Properties) { $baseline[[string]$property.Name] = $property.Value }
$failures = New-Object 'System.Collections.Generic.List[string]'
$checked = 0
$totalAwait = 0
$totalFalse = 0
$totalTrue = 0
$files = Get-ChildItem -LiteralPath $appRoot -Recurse -File | Where-Object {
    ($_.Extension -eq '.cs' -or $_.Extension -eq '.razor') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    $awaitCount = [regex]::Matches($text, '\bawait\b').Count
    if ($awaitCount -eq 0) { continue }
    $checked++
    $falseCount = [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*false\s*\)').Count
    $trueCount = [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*true\s*\)').Count
    $unconfigured = $awaitCount - $falseCount - $trueCount
    $relative = $file.FullName.Substring($appRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $isRendererSource = $relative.StartsWith('Components/', [System.StringComparison]::OrdinalIgnoreCase)
    if ($unconfigured -lt 0) {
        $failures.Add("${relative}: continuation count exceeds await count; inspect strings/comments and update the reviewed baseline.")
        continue
    }
    $allowedUnconfigured = 0
    $allowedTrue = 0
    $minimumFalse = 0
    if ($baseline.ContainsKey($relative)) {
        $allowedUnconfigured = [int]$baseline[$relative].maxUnconfiguredAwaitCount
        $allowedTrue = [int]$baseline[$relative].maxConfigureAwaitTrueCount
        $minimumFalse = [int]$baseline[$relative].minConfigureAwaitFalseCount
    }
    if ($falseCount -lt $minimumFalse) {
        $failures.Add("$relative has only $falseCount ConfigureAwait(false) call(s); reviewed minimum is $minimumFalse.")
    }
    if ($isRendererSource -and $trueCount -gt 0) {
        $allowedRendererMethods = @('OnAfterRenderAsync', 'OnInitializedAsync', 'OnParametersSetAsync', 'SetParametersAsync')
        $methodPattern = '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|async|new)\s+)*(?:[A-Za-z0-9_\.<>,?\[\]]+\s+)+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\('
        foreach ($trueMatch in [regex]::Matches($text, '\.ConfigureAwait\s*\(\s*true\s*\)')) {
            $prefix = $text.Substring(0, $trueMatch.Index)
            $methods = [regex]::Matches($prefix, $methodPattern)
            $methodName = if ($methods.Count -gt 0) { [string]$methods[$methods.Count - 1].Groups['name'].Value } else { '' }
            if ($allowedRendererMethods -notcontains $methodName) {
                $failures.Add("$relative uses ConfigureAwait(true) in '$methodName'. Renderer-affine true continuations are reserved for component lifecycle/initialization methods; use ConfigureAwait(false) elsewhere.")
            }
        }
    }
    if ($unconfigured -gt $allowedUnconfigured) {
        $expected = 'ConfigureAwait(false)'
        $failures.Add("$relative has $unconfigured unconfigured await(s); reviewed maximum is $allowedUnconfigured. Use $expected or review the baseline deliberately.")
    }
    if ($trueCount -gt $allowedTrue) {
        $failures.Add("$relative has $trueCount ConfigureAwait(true) call(s); reviewed maximum is $allowedTrue. Review renderer-affine additions deliberately.")
    }
    $totalAwait += $awaitCount
    $totalFalse += $falseCount
    $totalTrue += $trueCount
}
if ($failures.Count -gt 0) {
    Write-Host 'Async continuation validation failed:' -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host "  - $failure" -ForegroundColor Red }
    throw "Async continuation validation failed with $($failures.Count) problem(s)."
}
Write-Host "Async continuation validation passed for $checked PublisherStudio source files ($totalAwait await tokens, $totalFalse ConfigureAwait(false), $totalTrue ConfigureAwait(true))." -ForegroundColor Green
