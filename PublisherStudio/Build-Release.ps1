param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = Join-Path $root "artifacts\release"
$payload = Join-Path $artifacts "payload-$Runtime"
$setup = Join-Path $artifacts "setup-$Runtime"
$appZip = Join-Path $artifacts "BlazorPublisher-$Runtime.zip"

Remove-Item $payload,$setup,$appZip -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

dotnet publish (Join-Path $root "src\PublisherStudio.Web\PublisherStudio.Web.csproj") `
    -c $Configuration -r $Runtime --self-contained $selfContained -o $payload
if ($LASTEXITCODE -ne 0) { throw "Web publish failed." }

dotnet publish (Join-Path $root "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj") `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $setup
if ($LASTEXITCODE -ne 0) { throw "Installer publish failed." }

Compress-Archive -Path (Join-Path $payload "*") -DestinationPath $appZip -CompressionLevel Optimal
$setupExe = Join-Path $setup "PublisherStudio.Setup.exe"
if (-not (Test-Path $setupExe)) { throw "Installer executable not found: $setupExe" }
Copy-Item $setupExe (Join-Path $artifacts "PublisherStudio.Setup-$Runtime.exe") -Force

Write-Host "Release assets:" -ForegroundColor Green
Write-Host "  $appZip"
Write-Host "  $(Join-Path $artifacts "PublisherStudio.Setup-$Runtime.exe")"
Write-Host "Upload both files to the same GitHub release."
