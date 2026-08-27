Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')
function Fail([string]$Message) { throw "Iterator exception policy validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$script = Join-Path $PSScriptRoot 'audit_iterator_exception_policy.py'
if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { Fail "Strict iterator audit is missing: $script" }
$result = Invoke-PublisherStudioPythonScript -ScriptPath $script -Arguments @('--root', $root)
$result.Output | ForEach-Object { Write-Host ([string]$_) }
if ($result.ExitCode -ne 0) { Fail "Python iterator audit exited with code $($result.ExitCode)." }
