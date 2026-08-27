Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')

function Fail([string]$Message) { throw "Async continuation validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src\PublisherStudio.Web'
$pythonScript = Join-Path $PSScriptRoot 'audit_async_continuations.py'
if (-not (Test-Path -LiteralPath $pythonScript -PathType Leaf)) { Fail "The strict async-continuation audit is missing: $pythonScript" }

function Invoke-PythonAudit {
    $result = Invoke-PublisherStudioPythonScript -ScriptPath $pythonScript -Arguments @('--source-root', $sourceRoot) -AllowMissing
    if ($null -eq $result) {
        return $null
    }

    foreach ($line in $result.Output) { Write-Host ([string]$line) }
    return [int]$result.ExitCode
}

$pythonExit = Invoke-PythonAudit
if ($null -ne $pythonExit) {
    if ($pythonExit -ne 0) { Fail "Python async-continuation audit exited with code $pythonExit." }
    exit 0
}

# The repository already uses Python-backed architecture audits. If Python is absent, do not silently
# downgrade a zero-tolerance continuation invariant to the historical count/baseline heuristic.
Fail 'Python 3 is required for the syntax-aware zero-tolerance async-continuation audit. No raw-await baseline fallback is permitted.'
