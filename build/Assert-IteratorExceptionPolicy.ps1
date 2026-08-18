Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Iterator exception policy validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$script = Join-Path $PSScriptRoot 'audit_iterator_exception_policy.py'
if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { Fail "Strict iterator audit is missing: $script" }
$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) { & $python.Source $script --root $root; if ($LASTEXITCODE -ne 0) { Fail "Python iterator audit exited with code $LASTEXITCODE." }; exit 0 }
$launcher = Get-Command py -ErrorAction SilentlyContinue
if ($launcher) { & $launcher.Source -3 $script --root $root; if ($LASTEXITCODE -ne 0) { Fail "Python iterator audit exited with code $LASTEXITCODE." }; exit 0 }
Fail 'Python 3 is required for the zero-exemption iterator audit; no baseline fallback is permitted.'
