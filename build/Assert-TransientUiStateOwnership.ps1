param(
    [string]$RepositoryRoot = '',
    [ValidateSet('publisherstudio')][string]$Product = 'publisherstudio'
)
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

$ErrorActionPreference = 'Stop'
$root = Join-Path $RepositoryRoot 'src/PublisherStudio.Web'
$components = Join-Path $root 'Components'
$issues = [Collections.Generic.List[string]]::new()
$files = @(Get-ChildItem -LiteralPath $components -Recurse -File -Filter '*.razor')
$liveRanges = 0
foreach ($file in $files) {
    $text = [IO.File]::ReadAllText($file.FullName)
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
    foreach ($match in [regex]::Matches($text, '(?is)<\s*Dx(?:RichEdit|HtmlEditor|[A-Za-z0-9_]+)\b[^>]*>')) {
        $tag = $match.Value
        $tagName = ([regex]::Match($tag,'(?i)^<\s*(Dx\w+)')).Groups[1].Value
        $binding = if ($tagName -eq 'DxRichEdit') {
            '@bind-(?:DocumentContent|Selection)\s*='
        } elseif ($tagName -eq 'DxHtmlEditor') {
            '@bind-(?:Markup|Selection)\s*='
        } else {
            '@bind-(?:SelectedDataItems|FocusedRowIndex|FocusedColumnIndex|Selection|CaretPosition|CursorPosition|ScrollPosition|PlaybackPosition|CurrentPageIndex)\s*='
        }
        if ($tag -match $binding) { $issues.Add("${relative}: transient DevExpress state is two-way bound through InteractiveServer in $tagName.") }
        if ($tagName -eq 'DxRangeSelector' -and $tag -match 'ValueChangeMode\s*=\s*"RangeSelectorValueChangeMode\.OnHandleMove"') {
            $issues.Add("${relative}: DxRangeSelector must commit on handle release, not on every movement.")
        }
        # Live document callbacks require the paired component-local ShouldRender gate.
        if ($tagName -in @('DxRichEdit','DxHtmlEditor')) {
            $attr = if ($tagName -eq 'DxRichEdit') { 'SelectionChanged' } else { 'MarkupChanged' }
            $callback = [regex]::Match($tag, [regex]::Escape($attr) + '\s*=\s*"(?<handler>[A-Za-z_]\w*)"')
            $readOnly = $tagName -eq 'DxHtmlEditor' -and $tag -match '\bReadOnly\s*=\s*"true"'
            if ($callback.Success -and -not $readOnly) {
                $handler = $callback.Groups['handler'].Value
                $related = @($text)
                foreach ($companion in Get-ChildItem -LiteralPath $file.DirectoryName -File -Filter "$($file.BaseName)*.cs") {
                    $related += [IO.File]::ReadAllText($companion.FullName)
                }
                $combined = $related -join "`n"
                $method = [regex]::Match($combined,"(?s)\b(?:private|public|protected|internal)\s+(?:async\s+)?(?:void|Task|ValueTask)\s+$([regex]::Escape($handler))\s*\([^)]*\)\s*\{(?<body>.{0,2200}?)\n\s*\}")
                $gate = [regex]::Match($combined,'(?s)protected\s+override\s+bool\s+ShouldRender\s*\(\s*\)\s*\{(?<body>.{0,2200}?)\n\s*\}')
                $assignment = if ($method.Success) { [regex]::Match($method.Groups['body'].Value,'(?i)(?<flag>_[A-Za-z0-9_]*suppress[A-Za-z0-9_]*render[A-Za-z0-9_]*)\s*=\s*true\s*;') } else { $null }
                $suppressed = $null -ne $assignment -and $assignment.Success -and $gate.Success -and $gate.Groups['body'].Value.Contains($assignment.Groups['flag'].Value) -and $gate.Groups['body'].Value -match 'return\s+false\s*;'
                if (-not $suppressed) { $issues.Add("${relative}: ${attr} callback '$handler' must suppress automatic rerender of the live editor.") }
            }
        }
    }
    foreach ($match in [regex]::Matches($text, '(?is)<input\b[^>]*>')) {
        if ($match.Value -match '(?i)\btype\s*=\s*"range"' -and $match.Value.Contains('@oninput')) { $liveRanges++ }
    }
}
if ($liveRanges -gt 0) {
    $interopPath = Join-Path $root 'wwwroot/js/publisherInterop.js'
    if (-not (Test-Path -LiteralPath $interopPath)) { $issues.Add('The browser-owned native range coalescer is missing.') }
    else {
        $interop = [IO.File]::ReadAllText($interopPath)
        foreach ($token in @('function installStableNativeRangeLifecycle()', 'target.closest(' , 'event.stopPropagation();', 'requestAnimationFrame', "document.addEventListener('change'", "window.addEventListener('pointerup'")) {
            if (-not $interop.Contains($token)) { $issues.Add("Native range browser coalescer lost '$token'.") }
        }
    }
}
if ($issues.Count) { $issues | ForEach-Object { Write-Error $_ }; throw 'PublisherStudio transient UI-state boundary failed.' }
Write-Host "PublisherStudio native PowerShell transient-state audit passed for $($files.Count) components; $liveRanges native range controls retain their reviewed browser-owned lifecycle."
