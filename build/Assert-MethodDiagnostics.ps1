Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')
& (Join-Path $PSScriptRoot 'Invoke-ArchitectureAudit.ps1') -Mode methods

$serviceAudit = Join-Path $PSScriptRoot 'audit_service_resilience.py'
$repoRoot = Split-Path -Parent $PSScriptRoot
$result = Invoke-PublisherStudioPythonScript -ScriptPath $serviceAudit -Arguments @('--root', $repoRoot, '--product', 'publisherstudio')
$result.Output | ForEach-Object { Write-Host ([string]$_) }
if ($result.ExitCode -ne 0) { throw 'Service resilience audit failed.' }
