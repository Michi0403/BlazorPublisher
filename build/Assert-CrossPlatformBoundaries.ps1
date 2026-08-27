param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($null -eq $python) { throw "Python 3 is required for the PublisherStudio cross-platform boundary audit." }

$audit = Join-Path $RepositoryRoot ([IO.Path]::Combine("build", "audit_cross_platform_boundaries.py"))
& $python.Source $audit
if ($LASTEXITCODE -ne 0) { throw "PublisherStudio cross-platform boundary audit failed." }
