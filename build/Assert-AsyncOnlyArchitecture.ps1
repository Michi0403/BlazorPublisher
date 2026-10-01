Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')

function Fail([string]$Message) { throw "Async-boundary component/service architecture validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src\PublisherStudio.Web'
$pythonScript = Join-Path $PSScriptRoot 'audit_async_only_architecture.py'
if (-not (Test-Path -LiteralPath $pythonScript -PathType Leaf)) { Fail "The async-only architecture audit is missing: $pythonScript" }

function Invoke-PythonAudit {
    $result = Invoke-PublisherStudioPythonScript -ScriptPath $pythonScript -Arguments @('--source-root', $sourceRoot, '--product', 'PublisherStudio') -AllowMissing
    if ($null -eq $result) {
        return $null
    }

    foreach ($line in @($result.Output)) { Write-Host ([string]$line) }
    return [int]$result.ExitCode
}

$pythonExit = Invoke-PythonAudit
if ($null -eq $pythonExit) {
    Fail 'Python 3 is required for the zero-baseline async-boundary component/service architecture audit.'
}
if ($pythonExit -ne 0) {
    Fail "audit exited with code $pythonExit after reporting every detected violation."
}
