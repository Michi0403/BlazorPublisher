Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$issues = [Collections.Generic.List[string]]::new()
$sourceRoot = Join-Path $root 'src/PublisherStudio.Web'
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' }) {
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($file.Name -match 'PanelStudio|PanelView|Editor' -and $text -match 'async\s+void\s+') { $issues.Add("$($file.FullName): unsafe async void in Panel Studio state path.") }
}
& (Join-Path $PSScriptRoot 'Assert-PanelStudioInteractionLifecycle.ps1')
& (Join-Path $PSScriptRoot 'Assert-PanelStudioAuthoringGeometry.ps1')
if ($issues.Count) { $issues | ForEach-Object { Write-Error $_ }; throw 'Panel Studio persistence contract failed.' }
Write-Host 'PublisherStudio Panel Studio persistence, interaction lifecycle, and geometry were validated in PowerShell.'
