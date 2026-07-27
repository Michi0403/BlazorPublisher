param(
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.0.1",
    [string]$WireProtocolPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseScript = Join-Path $root "Build-Release.ps1"
$runtimes = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")

$first = $true
foreach ($runtime in $runtimes) {
    $arguments = @{
        Runtime = $runtime
        Configuration = $Configuration
        WireProtocolVersion = $WireProtocolVersion
    }
    if ($first -and -not [string]::IsNullOrWhiteSpace($WireProtocolPackageUrl)) {
        $arguments.WireProtocolPackageUrl = $WireProtocolPackageUrl
    } else {
        $arguments.UseBundledWireProtocolPackage = $true
    }
    if ($first -and $UseBundledWireProtocolPackage) { $arguments.UseBundledWireProtocolPackage = $true }

    & $releaseScript @arguments
    if ($LASTEXITCODE -ne 0) { throw "BlazorPublisher release build failed for $runtime." }
    $first = $false
}
