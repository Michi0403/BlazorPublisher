param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = Join-Path $root "artifacts\release"

$profile = switch ($Runtime) {
    "win-x64"     { @{ Asset = "winx64";     SetupAsset = "setupwinx64";     AppFolder = "winx64";     SetupFolder = "setupwin-x64" } }
    "win-arm64"   { @{ Asset = "winarm64";   SetupAsset = "setupwinarm64";   AppFolder = "winarm64";   SetupFolder = "setupwin-arm64" } }
    "linux-x64"   { @{ Asset = "linx64";     SetupAsset = "setuplinx64";     AppFolder = "linx64";     SetupFolder = "setuplin-x64" } }
    "linux-arm64" { @{ Asset = "linarm64";   SetupAsset = "setuplinarm64";   AppFolder = "linarm64";   SetupFolder = "setuplin-arm64" } }
    "osx-x64"     { @{ Asset = "macosx64";   SetupAsset = "setupmacosx64";   AppFolder = "macosx64";   SetupFolder = "setupmacos-x64" } }
    "osx-arm64"   { @{ Asset = "macosarm64"; SetupAsset = "setupmacosarm64"; AppFolder = "macosarm64"; SetupFolder = "setupmacos-arm64" } }
    default { throw "Unsupported release runtime: $Runtime" }
}

$appFolder = Join-Path $artifacts $profile.AppFolder
$setupFolder = Join-Path $artifacts $profile.SetupFolder
$appZip = Join-Path $artifacts "$($profile.Asset).zip"
$setupZip = Join-Path $artifacts "$($profile.SetupAsset).zip"

Remove-Item $appFolder,$setupFolder,$appZip,$setupZip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$webProject = Join-Path $root "src\PublisherStudio.Web\PublisherStudio.Web.csproj"
$webDirectory = Split-Path -Parent $webProject
$setupProject = Join-Path $root "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj"

Write-Host "Preparing local Spreadsheet client assets..." -ForegroundColor Cyan
& (Join-Path $root "Prepare-SpreadsheetAssets.ps1")
if ($LASTEXITCODE -ne 0) { throw "Spreadsheet client asset preparation failed." }

$requiredSpreadsheetAssets = @(
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.js",
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.css",
    "wwwroot\vendor\devextreme-dist\js\dx.all.js",
    "wwwroot\vendor\devextreme-dist\css\dx.light.css",
    "wwwroot\vendor\jquery\jquery.min.js"
)
$missingSpreadsheetAssets = @($requiredSpreadsheetAssets | Where-Object { -not (Test-Path (Join-Path $webDirectory $_)) })
if ($missingSpreadsheetAssets.Count -gt 0) {
    throw "Spreadsheet client assets are incomplete. Missing: $($missingSpreadsheetAssets -join ', ')"
}

Write-Host "Publishing BlazorPublisher application for $Runtime..." -ForegroundColor Cyan
dotnet publish $webProject `
    -c $Configuration -f net10.0 -r $Runtime --self-contained true `
    -p:PublishTrimmed=false -p:PublishSingleFile=false `
    -p:DeleteExistingFiles=true -o $appFolder
if ($LASTEXITCODE -ne 0) { throw "BlazorPublisher application publish failed." }

Write-Host "Publishing BlazorPublisher setup for $Runtime..." -ForegroundColor Cyan
dotnet publish $setupProject `
    -c $Configuration -f net10.0 -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -p:DeleteExistingFiles=true -o $setupFolder
if ($LASTEXITCODE -ne 0) { throw "BlazorPublisher setup publish failed." }

$appExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Web.exe" } else { "PublisherStudio.Web" }
$setupExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Setup.exe" } else { "PublisherStudio.Setup" }

if (-not (Test-Path (Join-Path $appFolder $appExecutable))) {
    throw "Published application executable not found: $(Join-Path $appFolder $appExecutable)"
}
if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) {
    throw "Published setup executable not found: $(Join-Path $setupFolder $setupExecutable)"
}

$requiredSetupFiles = @("Install.cmd", "Update.cmd", "Start.cmd", "Uninstall.cmd", "PublisherStudio.ico")
$missingSetupFiles = @($requiredSetupFiles | Where-Object { -not (Test-Path (Join-Path $setupFolder $_)) })
if ($missingSetupFiles.Count -gt 0) {
    throw "Published setup is incomplete. Missing: $($missingSetupFiles -join ', ')"
}

# Compress the directories themselves, not only their contents. Extraction therefore creates
# the same runtime/setup directory layout used by the LocalGPT installer construct.
Compress-Archive -Path $appFolder -DestinationPath $appZip -CompressionLevel Optimal -Force
Compress-Archive -Path $setupFolder -DestinationPath $setupZip -CompressionLevel Optimal -Force

# The generic Windows setup executable remains the initial bootstrap download.
if ($Runtime -eq "win-x64") {
    Copy-Item (Join-Path $setupFolder "PublisherStudio.Setup.exe") (Join-Path $artifacts "PublisherStudio.Setup.exe") -Force
}

Write-Host "Release assets:" -ForegroundColor Green
Write-Host "  $appZip"
Write-Host "  $setupZip"
if ($Runtime -eq "win-x64") {
    Write-Host "  $(Join-Path $artifacts 'PublisherStudio.Setup.exe')"
}
