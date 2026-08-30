param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$RefreshWireProtocolPackage,
    [switch]$UseBundledWireProtocolPackage,
    [switch]$UseContainerPackaging
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Initialize-BuildConsoleEncoding {
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) {
        $utf8 = New-Object Text.UTF8Encoding($false)
        [Console]::InputEncoding = $utf8
        [Console]::OutputEncoding = $utf8
        $global:OutputEncoding = $utf8
    }
}
Initialize-BuildConsoleEncoding
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Refreshing reviewed PublisherStudio frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
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
if ($UseContainerPackaging) { $arguments.UseContainerPackaging = $true }

Write-Host "Starting ordered PublisherStudio release build for all maintained runtimes supported by this host..." -ForegroundColor Cyan
& $releaseScript @arguments
