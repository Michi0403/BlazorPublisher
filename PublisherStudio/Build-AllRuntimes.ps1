param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$RefreshWireProtocolPackage,
    [switch]$UseBundledWireProtocolPackage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseScript = Join-Path $root "Build-Release.ps1"

$arguments = @{
    Runtime = "all"
    Configuration = $Configuration
    WireProtocolVersion = $WireProtocolVersion
    WireProtocolPackageUrl = $WireProtocolPackageUrl
    LocalGptRepository = $LocalGptRepository
}
if ($RefreshWireProtocolPackage) { $arguments.RefreshWireProtocolPackage = $true }
if ($UseBundledWireProtocolPackage) { $arguments.UseBundledWireProtocolPackage = $true }

Write-Host "Starting ordered PublisherStudio release build for all maintained runtimes..." -ForegroundColor Cyan
& $releaseScript @arguments
