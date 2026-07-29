param(
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.0",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$RefreshWireProtocolPackage,
    [switch]$UseBundledWireProtocolPackage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseScript = Join-Path $root "Build-Release.ps1"
$packageDirectory = Join-Path $root "packages"
$runtimes = @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
if (-not $UseBundledWireProtocolPackage) {
    $ensureArguments = @{
        Version = $WireProtocolVersion
        PackageDirectory = $packageDirectory
        PackageUrl = $WireProtocolPackageUrl
        LocalGptRepository = $LocalGptRepository
    }
    if ($RefreshWireProtocolPackage) { $ensureArguments.ForceDownload = $true }
    & (Join-Path $root "build\Ensure-WireProtocolPackage.ps1") @ensureArguments | Out-Null
}

foreach ($runtime in $runtimes) {
    Write-Host "Starting ordered PublisherStudio release build for $runtime..." -ForegroundColor Cyan
    & $releaseScript -Runtime $runtime -Configuration $Configuration -WireProtocolVersion $WireProtocolVersion -UseBundledWireProtocolPackage
    if ($LASTEXITCODE -ne 0) { throw "BlazorPublisher release build failed for $runtime." }
}
