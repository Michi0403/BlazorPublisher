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

$assetToken = switch ($Runtime) {
    "win-x64"     { "winx64" }
    "win-arm64"   { "winarm64" }
    "linux-x64"   { "linx64" }
    "linux-arm64" { "linarm64" }
    "osx-x64"     { "macosx64" }
    "osx-arm64"   { "macosarm64" }
    default { throw "Unsupported release runtime: $Runtime" }
}

$appZip = Join-Path $artifacts "$assetToken.zip"
$runtimeSetupName = if ($Runtime.StartsWith("win-")) {
    "PublisherStudio.Setup-$assetToken.exe"
} else {
    "PublisherStudio.Setup-$assetToken"
}
$runtimeSetupAsset = Join-Path $artifacts $runtimeSetupName
$genericWindowsSetupAsset = Join-Path $artifacts "PublisherStudio.Setup.exe"

Remove-Item $payload,$setup,$appZip,$runtimeSetupAsset -Recurse -Force -ErrorAction SilentlyContinue
if ($Runtime -eq "win-x64") {
    Remove-Item $genericWindowsSetupAsset -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

function Assert-PublishedPayload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentifier,
        [Parameter(Mandatory = $true)][bool]$IsSelfContained
    )

    $required = @(
        "PublisherStudio.Web.dll",
        "PublisherStudio.Web.deps.json",
        "PublisherStudio.Web.runtimeconfig.json",
        "PublisherStudio.ico"
    )

    if ($IsSelfContained) {
        if ($RuntimeIdentifier.StartsWith("win-")) {
            $required += @("PublisherStudio.Web.exe", "hostfxr.dll", "hostpolicy.dll")
        }
        elseif ($RuntimeIdentifier.StartsWith("linux-")) {
            $required += @("PublisherStudio.Web", "libhostfxr.so", "libhostpolicy.so")
        }
        elseif ($RuntimeIdentifier.StartsWith("osx-")) {
            $required += @("PublisherStudio.Web", "libhostfxr.dylib", "libhostpolicy.dylib")
        }
    }

    $missing = @($required | Where-Object { -not (Test-Path (Join-Path $Path $_)) })
    if ($missing.Count -gt 0) {
        throw "Published payload for $RuntimeIdentifier is incomplete. Missing: $($missing -join ', ')"
    }

    $runtimeConfig = Get-Content (Join-Path $Path "PublisherStudio.Web.runtimeconfig.json") -Raw | ConvertFrom-Json
    $hasFramework = $null -ne $runtimeConfig.runtimeOptions.framework -or $null -ne $runtimeConfig.runtimeOptions.frameworks
    if ($IsSelfContained -and $hasFramework) {
        throw "The $RuntimeIdentifier payload was requested as self-contained, but its runtimeconfig is framework-dependent."
    }
    if (-not $IsSelfContained -and -not $hasFramework) {
        throw "The $RuntimeIdentifier payload was requested as framework-dependent, but its runtimeconfig does not declare a framework."
    }
}

dotnet publish (Join-Path $root "src\PublisherStudio.Web\PublisherStudio.Web.csproj") `
    -c $Configuration -r $Runtime --self-contained $selfContained `
    -p:SelfContained=$selfContained -p:UseAppHost=true -o $payload
if ($LASTEXITCODE -ne 0) { throw "Web publish failed." }

Assert-PublishedPayload -Path $payload -RuntimeIdentifier $Runtime -IsSelfContained (-not $FrameworkDependent)

dotnet publish (Join-Path $root "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj") `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -o $setup
if ($LASTEXITCODE -ne 0) { throw "Installer publish failed." }

Compress-Archive -Path (Join-Path $payload "*") -DestinationPath $appZip -CompressionLevel Optimal

$setupExecutableName = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Setup.exe" } else { "PublisherStudio.Setup" }
$setupExecutable = Join-Path $setup $setupExecutableName
if (-not (Test-Path $setupExecutable)) { throw "Installer executable not found: $setupExecutable" }
Copy-Item $setupExecutable $runtimeSetupAsset -Force

# The current public release layout uses one generic Windows setup filename.
# Keep that exact name for win-x64 while also producing a runtime-specific copy.
if ($Runtime -eq "win-x64") {
    Copy-Item $setupExecutable $genericWindowsSetupAsset -Force
}

Write-Host "Release assets:" -ForegroundColor Green
Write-Host "  $appZip"
Write-Host "  $runtimeSetupAsset"
if ($Runtime -eq "win-x64") {
    Write-Host "  $genericWindowsSetupAsset"
}
Write-Host "Upload the application ZIP and the matching setup executable to the same GitHub release."
