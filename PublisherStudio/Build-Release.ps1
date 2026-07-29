param(
    [ValidateSet("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.0",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$RefreshWireProtocolPackage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = Join-Path $root "artifacts\release"
$packageDirectory = Join-Path $root "packages"
$wireProtocolPackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wireProtocolPackage = Join-Path $packageDirectory $wireProtocolPackageName
$webProject = Join-Path $root "src\PublisherStudio.Web\PublisherStudio.Web.csproj"
$webDirectory = Split-Path -Parent $webProject
$setupProject = Join-Path $root "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj"

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

$multiFileSelfContainedProperties = @(
    "--self-contained", "true",
    "-p:PublishTrimmed=false",
    "-p:PublishSingleFile=false",
    "-p:PublishReadyToRun=false",
    "-p:DeleteExistingFiles=true"
)

function Assert-PublishedConfigurationFiles {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$PublishRoot
    )

    $configurationSources = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    foreach ($file in Get-ChildItem -LiteralPath $SourceRoot -File -Filter 'appsettings*.json') { $configurationSources.Add($file) }
    foreach ($directory in @('Configuration', 'Localization')) {
        $sourceDirectory = Join-Path $SourceRoot $directory
        if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
            throw "Required PublisherStudio configuration directory is unavailable: $sourceDirectory"
        }
        foreach ($file in Get-ChildItem -LiteralPath $sourceDirectory -File -Recurse) { $configurationSources.Add($file) }
    }

    $missing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($source in $configurationSources) {
        $relative = $source.FullName.Substring($SourceRoot.Length).TrimStart([char[]]"\/")
        $published = Join-Path $PublishRoot $relative
        if (-not (Test-Path -LiteralPath $published -PathType Leaf)) { $missing.Add($relative) }
    }
    if ($missing.Count -gt 0) { throw "PublisherStudio publish output is missing configuration files: $($missing -join ', ')" }

    Write-Host "Published configuration validation passed for $($configurationSources.Count) files." -ForegroundColor Green
}
New-Item -ItemType Directory -Path $packageDirectory, $artifacts -Force | Out-Null

if ($UseBundledWireProtocolPackage) {
    if (-not (Test-Path -LiteralPath $wireProtocolPackage -PathType Leaf)) {
        throw "The cached official LocalGPT protocol package is unavailable: $wireProtocolPackage"
    }
}
else {
    $ensureArguments = @{
        Version = $WireProtocolVersion
        PackageDirectory = $packageDirectory
        PackageUrl = $WireProtocolPackageUrl
        LocalGptRepository = $LocalGptRepository
    }
    if ($RefreshWireProtocolPackage) { $ensureArguments.ForceDownload = $true }
    & (Join-Path $root "build\Ensure-WireProtocolPackage.ps1") @ensureArguments | Out-Null
}
if (-not (Test-Path -LiteralPath $wireProtocolPackage -PathType Leaf)) {
    throw "LocalGPT protocol package preparation did not produce $wireProtocolPackage"
}

$profile = switch ($Runtime) {
    "win-x64"     { @{ Asset = "winx64";     SetupAsset = "setupwinx64";     AppFolder = "winx64";     SetupFolder = "setupwinx64" } }
    "win-arm64"   { @{ Asset = "winarm64";   SetupAsset = "setupwinarm64";   AppFolder = "winarm64";   SetupFolder = "setupwinarm64" } }
    "linux-x64"   { @{ Asset = "linx64";     SetupAsset = "setuplinx64";     AppFolder = "linx64";     SetupFolder = "setuplinx64" } }
    "linux-arm64" { @{ Asset = "linarm64";   SetupAsset = "setuplinarm64";   AppFolder = "linarm64";   SetupFolder = "setuplinarm64" } }
    "osx-x64"     { @{ Asset = "macosx64";   SetupAsset = "setupmacosx64";   AppFolder = "macosx64";   SetupFolder = "setupmacosx64" } }
    "osx-arm64"   { @{ Asset = "macosarm64"; SetupAsset = "setupmacosarm64"; AppFolder = "macosarm64"; SetupFolder = "setupmacosarm64" } }
    default { throw "Unsupported release runtime: $Runtime" }
}

$appFolder = Join-Path $artifacts $profile.AppFolder
$setupFolder = Join-Path $artifacts $profile.SetupFolder
$appZip = Join-Path $artifacts "$($profile.Asset).zip"
$setupZip = Join-Path $artifacts "$($profile.SetupAsset).zip"
Remove-Item $appFolder, $setupFolder, $appZip, $setupZip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $appFolder, $setupFolder -Force | Out-Null

Write-Host "Preparing local DevExpress client assets and runtime license..." -ForegroundColor Cyan
& (Join-Path $root "Prepare-DevExpressAssets.ps1")
if ($LASTEXITCODE -ne 0) { throw "DevExpress client-asset preparation failed." }

$requiredSpreadsheetAssets = @(
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.js",
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.css",
    "wwwroot\vendor\devextreme-dist\js\dx.all.js",
    "wwwroot\vendor\devextreme-dist\css\dx.light.css",
    "wwwroot\vendor\jquery\jquery.min.js",
    "wwwroot\vendor\devextreme-license.js",
    "wwwroot\vendor\devextreme-license.meta.json",
    "wwwroot\vendor\devextreme-license.version"
)
$missingSpreadsheetAssets = @($requiredSpreadsheetAssets | Where-Object { -not (Test-Path (Join-Path $webDirectory $_)) })
if ($missingSpreadsheetAssets.Count -gt 0) {
    throw "DevExpress client assets are incomplete. Missing: $($missingSpreadsheetAssets -join ', ')"
}

$wireProperties = @(
    "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:SkipWireProtocolBootstrap=true"
)

Write-Host "Restoring BlazorPublisher application for $Runtime after protocol preparation..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $webProject, "-r", $Runtime, "--disable-parallel") + $wireProperties) -FailureMessage "BlazorPublisher application restore failed."

Write-Host "Publishing BlazorPublisher application for $Runtime..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@(
    "publish", $webProject,
    "-c", $Configuration,
    "-f", "net10.0",
    "-r", $Runtime,
    "--no-restore",
    "-maxcpucount:1",
    "-o", $appFolder
) + $multiFileSelfContainedProperties + $wireProperties) -FailureMessage "BlazorPublisher application publish failed."
Assert-PublishedConfigurationFiles -SourceRoot $webDirectory -PublishRoot $appFolder

Write-Host "Restoring BlazorPublisher setup for $Runtime..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("restore", $setupProject, "-r", $Runtime, "--disable-parallel") -FailureMessage "BlazorPublisher setup restore failed."

Write-Host "Publishing BlazorPublisher setup for $Runtime..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@(
    "publish", $setupProject,
    "-c", $Configuration,
    "-f", "net10.0",
    "-r", $Runtime,
    "--no-restore",
    "-maxcpucount:1",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-o", $setupFolder
) + $multiFileSelfContainedProperties) -FailureMessage "BlazorPublisher setup publish failed."

$appExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Web.exe" } else { "PublisherStudio.Web" }
$setupExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Setup.exe" } else { "PublisherStudio.Setup" }
if (-not (Test-Path (Join-Path $appFolder $appExecutable))) { throw "Published application executable not found: $(Join-Path $appFolder $appExecutable)" }
if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) { throw "Published setup executable not found: $(Join-Path $setupFolder $setupExecutable)" }

$protocolAppDirectory = Join-Path $appFolder "protocol"
$protocolSetupDirectory = Join-Path $setupFolder "protocol"
New-Item -ItemType Directory -Path $protocolAppDirectory, $protocolSetupDirectory -Force | Out-Null
Copy-Item $wireProtocolPackage (Join-Path $protocolAppDirectory $wireProtocolPackageName) -Force
Copy-Item $wireProtocolPackage (Join-Path $protocolSetupDirectory $wireProtocolPackageName) -Force
Copy-Item $wireProtocolPackage (Join-Path $artifacts $wireProtocolPackageName) -Force

# Keep the repository-owned icon explicit in both publish outputs so desktop and Start-menu
# shortcuts never depend on incidental SDK Content-item behavior.
$publisherIcon = Join-Path $root "assets\PublisherStudio.ico"
if (-not (Test-Path -LiteralPath $publisherIcon -PathType Leaf)) {
    throw "PublisherStudio release icon is unavailable: $publisherIcon"
}
Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $setupFolder "PublisherStudio.ico") -Force
Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $appFolder "PublisherStudio.ico") -Force

$requiredSetupFiles = @("Default.cmd", "Install.cmd", "Update.cmd", "Start.cmd", "Start-NoBrowser.cmd", "Check-FFmpeg.cmd", "Install-FFmpeg.cmd", "Uninstall.cmd", "PublisherStudio.ico")
$missingSetupFiles = @($requiredSetupFiles | Where-Object { -not (Test-Path (Join-Path $setupFolder $_)) })
if ($missingSetupFiles.Count -gt 0) { throw "Published setup is incomplete. Missing: $($missingSetupFiles -join ', ')" }

Compress-Archive -Path $appFolder -DestinationPath $appZip -CompressionLevel Optimal -Force
Compress-Archive -Path $setupFolder -DestinationPath $setupZip -CompressionLevel Optimal -Force
Write-Host "Release assets:" -ForegroundColor Green
Write-Host "  $appZip"
Write-Host "  $setupZip"
Write-Host "  $(Join-Path $artifacts $wireProtocolPackageName)"
