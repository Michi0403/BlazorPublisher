param(
    [ValidateSet("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
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

function Get-ProjectVersion {
    param([Parameter(Mandatory)][string]$ProjectPath)
    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $version = @($project.Project.PropertyGroup.Version | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    if ($version.Count -ne 1) { throw "Project version is missing or ambiguous: $ProjectPath" }
    return [string]$version[0]
}

function Write-ReleaseManifest {
    param(
        [Parameter(Mandatory)][string]$OutputRoot,
        [Parameter(Mandatory)][string]$PayloadKind,
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string]$Version
    )
    $files = @(
        Get-ChildItem -LiteralPath $OutputRoot -File -Recurse |
            Where-Object { -not [string]::Equals($_.Name, 'publisherstudio-release.json', [StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object FullName |
            ForEach-Object {
                $relativePath = $_.FullName.Substring($OutputRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
                [ordered]@{
                    Path = $relativePath
                    Length = [long]$_.Length
                    Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
                }
            }
    )
    if ($files.Count -eq 0) { throw "Cannot create an empty PublisherStudio $PayloadKind release manifest." }
    if (-not ($files.Path -contains $Executable)) { throw "PublisherStudio $PayloadKind manifest does not contain executable $Executable." }

    $manifest = [ordered]@{
        SchemaVersion = 2
        Product = 'PublisherStudio'
        Version = $Version
        RuntimeIdentifier = $Runtime
        PayloadKind = $PayloadKind
        Executable = $Executable
        WireProtocolVersion = $WireProtocolVersion
        CreatedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        Files = $files
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputRoot 'publisherstudio-release.json') -Encoding UTF8
}

function Write-BootstrapRepairManifest {
    param(
        [Parameter(Mandatory)][string]$SetupRoot,
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string[]]$Files
    )
    $catalogue = @(
        $Files | Sort-Object -Unique | ForEach-Object {
            $path = Join-Path $SetupRoot $_
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Launcher repair file is unavailable: $path" }
            [ordered]@{
                Path = $_.Replace('\', '/')
                Length = [long](Get-Item -LiteralPath $path).Length
                Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            }
        }
    )
    $manifest = [ordered]@{
        SchemaVersion = 2
        Product = 'PublisherStudio'
        Version = $Version
        RuntimeIdentifier = $Runtime
        PayloadKind = 'SetupRepair'
        Executable = $Executable
        WireProtocolVersion = $WireProtocolVersion
        CreatedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        Files = $catalogue
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $SetupRoot 'publisherstudio-bootstrap-repair.json') -Encoding UTF8
}

function New-ReleaseArchive {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$ArchivePath,
        [Parameter(Mandatory)][string]$RootFolderName,
        [string]$LastFile = "",
        [string]$PreludeRoot = "",
        [string]$PreludeFolderName = "",
        [string[]]$PreludeFiles = @()
    )

    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $ArchivePath -Force -ErrorAction SilentlyContinue
    $files = @(Get-ChildItem -LiteralPath $SourceRoot -File -Recurse | Sort-Object FullName)
    if (-not [string]::IsNullOrWhiteSpace($LastFile)) {
        $lastPath = Join-Path $SourceRoot $LastFile
        $files = @($files | Where-Object { -not [string]::Equals($_.FullName, $lastPath, [StringComparison]::OrdinalIgnoreCase) }) + @($files | Where-Object { [string]::Equals($_.FullName, $lastPath, [StringComparison]::OrdinalIgnoreCase) })
    }

    $stream = [IO.File]::Open($ArchivePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            if (-not [string]::IsNullOrWhiteSpace($PreludeRoot) -and $PreludeFiles.Count -gt 0) {
                if ([string]::IsNullOrWhiteSpace($PreludeFolderName)) { throw 'PreludeFolderName is required when PreludeFiles are supplied.' }
                foreach ($relative in $PreludeFiles) {
                    $preludePath = Join-Path $PreludeRoot $relative
                    if (-not (Test-Path -LiteralPath $preludePath -PathType Leaf)) { throw "Release archive prelude file is unavailable: $preludePath" }
                    $entryName = "$PreludeFolderName/$($relative.Replace('\', '/'))"
                    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $preludePath, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
                }
            }
            foreach ($file in $files) {
                $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
                $entryName = "$RootFolderName/$relative"
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Assert-ReleaseArchiveLayout {
    param(
        [Parameter(Mandatory)][string]$ArchivePath,
        [Parameter(Mandatory)][string]$RootFolderName,
        [Parameter(Mandatory)][string]$Executable,
        [string]$AllowedAdditionalRoot = ""
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/').TrimStart('/') })
        $expectedExecutable = "$RootFolderName/$Executable"
        $expectedManifest = "$RootFolderName/publisherstudio-release.json"
        if (-not ($names -contains $expectedExecutable)) { throw "Release archive does not preserve the legacy-compatible $expectedExecutable layout: $ArchivePath" }
        if (-not ($names -contains $expectedManifest)) { throw "Release archive is missing ${expectedManifest}: $ArchivePath" }
        if (-not [string]::IsNullOrWhiteSpace($AllowedAdditionalRoot)) {
            $repairExecutable = "$AllowedAdditionalRoot/PublisherStudio.Setup.repair.exe"
            $repairManifest = "$AllowedAdditionalRoot/publisherstudio-bootstrap-repair.json"
            if (-not ($names -contains $repairExecutable) -or -not ($names -contains $repairManifest)) { throw "Release archive is missing the existing-launcher repair prelude: $ArchivePath" }
            if ([Array]::IndexOf($names, $repairExecutable) -gt [Array]::IndexOf($names, $expectedExecutable)) { throw "Launcher repair executable must be archived before the application executable: $ArchivePath" }
        }
        foreach ($name in $names) {
            if ([string]::IsNullOrWhiteSpace($name)) { continue }
            $inPrimaryRoot = $name.StartsWith("$RootFolderName/", [StringComparison]::OrdinalIgnoreCase)
            $inAdditionalRoot = -not [string]::IsNullOrWhiteSpace($AllowedAdditionalRoot) -and $name.StartsWith("$AllowedAdditionalRoot/", [StringComparison]::OrdinalIgnoreCase)
            if (-not $inPrimaryRoot -and -not $inAdditionalRoot) { throw "Release archive entry '$name' escapes expected wrappers '$RootFolderName' and '$AllowedAdditionalRoot'." }
            if ($name.StartsWith('/') -or $name.Split('/') -contains '..') { throw "Unsafe archive path '$name' in $ArchivePath" }
        }
    }
    finally { $archive.Dispose() }
}

$appVersion = Get-ProjectVersion -ProjectPath $webProject
$setupVersion = Get-ProjectVersion -ProjectPath $setupProject
if ($appVersion -ne $setupVersion) { throw "PublisherStudio application version $appVersion does not match setup version $setupVersion." }

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
    "win-x64"     { @{ Asset = "winx64";     SetupAsset = "setupwinx64";     AppFolder = "winx64"; Profile = "winx64";     SetupFolder = "setupwinx64" } }
    "win-x86"     { @{ Asset = "winx86";     SetupAsset = "setupwinx86";     AppFolder = "winx86"; Profile = "winx86";     SetupFolder = "setupwinx86" } }
    "win-arm64"   { @{ Asset = "winarm64";   SetupAsset = "setupwinarm64";   AppFolder = "winarm64"; Profile = "winarm64";   SetupFolder = "setupwinarm64" } }
    "linux-x64"   { @{ Asset = "linx64";     SetupAsset = "setuplinx64";     AppFolder = "linx64"; Profile = "linx64";     SetupFolder = "setuplinx64" } }
    "linux-arm64" { @{ Asset = "linarm64";   SetupAsset = "setuplinarm64";   AppFolder = "linarm64"; Profile = "linarm64";   SetupFolder = "setuplinarm64" } }
    "osx-x64"     { @{ Asset = "macosx64";   SetupAsset = "setupmacosx64";   AppFolder = "macosx64"; Profile = "macosx64";   SetupFolder = "setupmacosx64" } }
    "osx-arm64"   { @{ Asset = "macosarm64"; SetupAsset = "setupmacosarm64"; AppFolder = "macosarm64"; Profile = "macosarm64"; SetupFolder = "setupmacosarm64" } }
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
    "-p:PublishProfile=$($profile.Profile)",
    "--no-restore",
    "-maxcpucount:1"
) + $wireProperties) -FailureMessage "BlazorPublisher application publish failed."
Assert-PublishedConfigurationFiles -SourceRoot $webDirectory -PublishRoot $appFolder

Write-Host "Restoring BlazorPublisher setup for $Runtime..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("restore", $setupProject, "-r", $Runtime, "--disable-parallel") -FailureMessage "BlazorPublisher setup restore failed."

Write-Host "Publishing BlazorPublisher setup for $Runtime..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @(
    "publish", $setupProject,
    "-c", $Configuration,
    "-p:PublishProfile=$($profile.Profile)",
    "--no-restore",
    "-maxcpucount:1",
    "-p:DebugType=None",
    "-p:DebugSymbols=false"
) -FailureMessage "BlazorPublisher setup publish failed."

$appExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Web.exe" } else { "PublisherStudio.Web" }
$setupExecutable = if ($Runtime.StartsWith("win-")) { "PublisherStudio.Setup.exe" } else { "PublisherStudio.Setup" }
if (-not (Test-Path (Join-Path $appFolder $appExecutable))) { throw "Published application executable not found: $(Join-Path $appFolder $appExecutable)" }
if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) { throw "Published setup executable not found: $(Join-Path $setupFolder $setupExecutable)" }

# Existing installed launchers can be running from setupwin*/PublisherStudio.Setup.exe while
# they extract a new release. Windows will not overwrite that active executable. The repair
# copy is catalogued and extracted before the locked executable; updated launchers promote it
# atomically on their next invocation.
$setupRepairExecutable = ""
if ($Runtime.StartsWith("win-")) {
    $setupRepairExecutable = "PublisherStudio.Setup.repair.exe"
    Copy-Item -LiteralPath (Join-Path $setupFolder $setupExecutable) -Destination (Join-Path $setupFolder $setupRepairExecutable) -Force
}

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

$bootstrapRepairFiles = @()
if ($Runtime.StartsWith("win-")) {
    $bootstrapRepairFiles = @($requiredSetupFiles + $setupRepairExecutable | Sort-Object -Unique)
    Write-BootstrapRepairManifest -SetupRoot $setupFolder -Executable $setupRepairExecutable -Version $setupVersion -Files $bootstrapRepairFiles
    $bootstrapRepairFiles += 'publisherstudio-bootstrap-repair.json'
}

Write-ReleaseManifest -OutputRoot $appFolder -PayloadKind 'Application' -Executable $appExecutable -Version $appVersion
Write-ReleaseManifest -OutputRoot $setupFolder -PayloadKind 'Setup' -Executable $setupExecutable -Version $setupVersion

if ($Runtime.StartsWith("win-")) {
    New-ReleaseArchive -SourceRoot $appFolder -ArchivePath $appZip -RootFolderName $profile.AppFolder -LastFile $appExecutable -PreludeRoot $setupFolder -PreludeFolderName $profile.SetupFolder -PreludeFiles $bootstrapRepairFiles
    Assert-ReleaseArchiveLayout -ArchivePath $appZip -RootFolderName $profile.AppFolder -Executable $appExecutable -AllowedAdditionalRoot $profile.SetupFolder
}
else {
    New-ReleaseArchive -SourceRoot $appFolder -ArchivePath $appZip -RootFolderName $profile.AppFolder
    Assert-ReleaseArchiveLayout -ArchivePath $appZip -RootFolderName $profile.AppFolder -Executable $appExecutable
}
New-ReleaseArchive -SourceRoot $setupFolder -ArchivePath $setupZip -RootFolderName $profile.SetupFolder -LastFile $setupExecutable
Assert-ReleaseArchiveLayout -ArchivePath $setupZip -RootFolderName $profile.SetupFolder -Executable $setupExecutable
Write-Host "Release assets:" -ForegroundColor Green
Write-Host "  $appZip"
Write-Host "  $setupZip"
Write-Host "  $(Join-Path $artifacts $wireProtocolPackageName)"
