$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$scriptPath = Join-Path $PSScriptRoot 'audit_panelstudio_persistence.py'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Panel Studio persistence audit is missing: $scriptPath" }
$output = & python $scriptPath 2>&1
$exitCode = $LASTEXITCODE
$output | ForEach-Object { Write-Host $_ }
if ($exitCode -ne 0) { throw "Panel Studio persistence source audit failed with exit code $exitCode." }
