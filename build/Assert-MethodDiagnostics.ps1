Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-ArchitectureAudit.ps1') -Mode methods
Write-Host 'PublisherStudio native PowerShell service method diagnostics completed.'
