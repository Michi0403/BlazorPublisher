$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')
$scriptPath = Join-Path $PSScriptRoot 'audit_panelstudio_persistence.py'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Panel Studio persistence audit is missing: $scriptPath" }
$result = Invoke-PublisherStudioPythonScript -ScriptPath $scriptPath
$result.Output | ForEach-Object { Write-Host $_ }
if ($result.ExitCode -ne 0) { throw "Panel Studio persistence source audit failed with exit code $($result.ExitCode)." }
