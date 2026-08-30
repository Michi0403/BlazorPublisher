param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$RefreshWireProtocolPackage,
    [switch]$UseWireProtocolPackage,
    [switch]$Clean
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
& (Join-Path $root ([IO.Path]::Combine('build', 'Assert-SourcePackagePrerequisites.ps1'))) -RepositoryRoot $root
& (Join-Path $root ([IO.Path]::Combine('build', 'Assert-CrossPlatformBoundaries.ps1'))) -RepositoryRoot $root
Write-Host "Refreshing reviewed PublisherStudio frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
& (Join-Path $root 'build/Assert-InteractiveServerRenderModes.ps1')
& (Join-Path $root 'build/Assert-PanelStudioAuthoringGeometry.ps1')
& (Join-Path $root 'build/Assert-PanelStudioInteractionLifecycle.ps1')
& (Join-Path $root 'build/Assert-PanelStudioPersistence.ps1')
& (Join-Path $root 'build/Assert-XmlDocumentationCoverage.ps1')
Write-Host "Clearing repository-local obj restore state before the ordered CLI build..." -ForegroundColor DarkCyan
Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
    Where-Object { $_.Name -eq "obj" } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
$webProject = Join-Path $root "src/PublisherStudio.Web/PublisherStudio.Web.csproj"
$setupProject = Join-Path $root "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj"
$packageDirectory = Join-Path $root "packages"

# Report narrow protocol/wiring findings without blocking local development.
& (Join-Path $root "build/Assert-OneWireArchitecture.ps1")
& (Join-Path $root "build/Assert-InstallerWorkflow.ps1")

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

if ($Clean) {
    Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object FullName -Descending |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

if ($UseWireProtocolPackage) {
    Write-Host "PublisherStudio is package-only now; -UseWireProtocolPackage is retained as a harmless compatibility switch." -ForegroundColor DarkYellow
}

$ensureArguments = @{
    Version = $WireProtocolVersion
    PackageDirectory = $packageDirectory
    PackageUrl = $WireProtocolPackageUrl
    LocalGptRepository = $LocalGptRepository
}
if ($RefreshWireProtocolPackage) { $ensureArguments.ForceDownload = $true }
& (Join-Path $root "build/Ensure-WireProtocolPackage.ps1") @ensureArguments | Out-Null

$wireProperties = @(
    "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:SkipWireProtocolBootstrap=true",
    "-p:BuildPublisherStudioDocumentation=true",
    "-p:SeedPublisherStudioGitHubPagesSnapshotOnBuild=true"
)

Write-Host "Restoring PublisherStudio.Web after the protocol package is available..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $webProject, "--disable-parallel", "--force-evaluate") + $wireProperties) -FailureMessage "PublisherStudio.Web restore failed."
Write-Host "Restoring PublisherStudio installer..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("restore", $setupProject, "--disable-parallel", "--force-evaluate") -FailureMessage "PublisherStudio installer restore failed."

Write-Host "Building PublisherStudio.Web as a single ordered project..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("build", $webProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") + $wireProperties) -FailureMessage "PublisherStudio.Web build failed."
Write-Host "Building PublisherStudio installer after the web project..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("build", $setupProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") -FailureMessage "PublisherStudio installer build failed."

Write-Host "PublisherStudio development build succeeded in authoritative NuGet package mode." -ForegroundColor Green
