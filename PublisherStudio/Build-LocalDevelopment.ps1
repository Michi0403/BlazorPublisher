param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$UseWireProtocolPackage,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "PublisherStudio.sln"
if (-not (Test-Path $solution)) {
    $solution = Get-ChildItem $root -Filter *.sln -File | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $solution) { throw "PublisherStudio solution file was not found." }
$packageDirectory = Join-Path $root "packages"
$useProject = if ($UseWireProtocolPackage) { "false" } else { "true" }

$loggingGuard = Join-Path $root "build\Assert-LoggingIntegrity.ps1"
& $loggingGuard

if ($Clean) {
    Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object FullName -Descending |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

$properties = @(
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:SkipLoggingIntegrityGuard=true"
)
& dotnet restore $solution @properties
if ($LASTEXITCODE -ne 0) { throw "PublisherStudio solution restore failed." }
& dotnet build $solution -c $Configuration --no-restore @properties
if ($LASTEXITCODE -ne 0) { throw "PublisherStudio solution build failed." }
Write-Host "PublisherStudio development build succeeded with UseLocalWireProtocolProject=$useProject." -ForegroundColor Green
