param(
    [ValidateSet("all", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
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
Write-Host "Clearing repository-local bin/obj build state for the authoritative release build..." -ForegroundColor Cyan
Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
    Where-Object { $_.Name -in @("bin", "obj") } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
$artifacts = Join-Path $root "artifacts\release"
$packageDirectory = Join-Path $root "packages"
$webProject = Join-Path $root "src\PublisherStudio.Web\PublisherStudio.Web.csproj"
$webDirectory = Split-Path -Parent $webProject
$setupProject = Join-Path $root "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj"
$documentationScript = Join-Path $root "build\Build-Documentation.ps1"
$pagesSnapshotScript = Join-Path $root "build\Update-GitHubPagesSnapshot.ps1"
$pagesSnapshotArchive = Join-Path $root ".github\pages\publisherstudio-kawaii-docs.zip"
$wireProtocolPackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wireProtocolPackage = Join-Path $packageDirectory $wireProtocolPackageName
$documentationCacheRoot = Join-Path $artifacts ".documentation-cache"
$documentationPrepared = $false

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$FailureMessage
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Resolve-ProjectVersion {
    param([Parameter(Mandatory)][string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $versions = @(
        $project.Project.PropertyGroup |
            ForEach-Object { [string]$_.Version } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    if ($versions.Count -eq 0) { throw "Project version was not found in $ProjectPath" }
    return $versions[0]
}

function Resolve-PublishProfilePath {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $projectDirectory = Split-Path -Parent $ProjectPath
    $profilePath = Join-Path $projectDirectory "Properties\PublishProfiles\$ProfileName.pubxml"
    if (-not (Test-Path -LiteralPath $profilePath -PathType Leaf)) {
        throw "Publish profile not found: $profilePath"
    }
    return $profilePath
}

function Resolve-ProfilePublishFolder {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $profilePath = Resolve-PublishProfilePath -ProjectPath $ProjectPath -ProfileName $ProfileName
    [xml]$profile = Get-Content -LiteralPath $profilePath -Raw
    $propertyGroups = @($profile.Project.PropertyGroup)
    $publishDirectory = @(
        $propertyGroups |
            ForEach-Object { $_.PublishDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    ) | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        $publishDirectory = @(
            $propertyGroups |
                ForEach-Object { $_.PublishUrl } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
        ) | Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        throw "Publish profile does not define PublishDir or PublishUrl: $profilePath"
    }

    $projectDirectory = Split-Path -Parent $ProjectPath
    $resolved = if ([IO.Path]::IsPathRooted([string]$publishDirectory)) {
        [string]$publishDirectory
    }
    else {
        Join-Path $projectDirectory ([string]$publishDirectory)
    }
    return [IO.Path]::GetFullPath($resolved)
}

function Resolve-ReleaseProfile {
    param([Parameter(Mandatory)][string]$Rid)

    switch ($Rid) {
        "win-x64"     { return @{ AppAsset = "winx64.zip";     SetupAsset = "setupwinx64.zip";     AppProfile = "winx64";     SetupProfile = "winx64";     AppFolder = "winx64";     SetupFolder = "setupwinx64" } }
        "win-x86"     { return @{ AppAsset = "winx86.zip";     SetupAsset = "setupwinx86.zip";     AppProfile = "winx86";     SetupProfile = "winx86";     AppFolder = "winx86";     SetupFolder = "setupwinx86" } }
        "win-arm64"   { return @{ AppAsset = "winarm64.zip";   SetupAsset = "setupwinarm64.zip";   AppProfile = "winarm64";   SetupProfile = "winarm64";   AppFolder = "winarm64";   SetupFolder = "setupwinarm64" } }
        "linux-x64"   { return @{ AppAsset = "linx64.zip";     SetupAsset = "setuplinx64.zip";     AppProfile = "linx64";     SetupProfile = "linx64";     AppFolder = "linx64";     SetupFolder = "setuplinx64" } }
        "linux-arm64" { return @{ AppAsset = "linarm64.zip";   SetupAsset = "setuplinarm64.zip";   AppProfile = "linarm64";   SetupProfile = "linarm64";   AppFolder = "linarm64";   SetupFolder = "setuplinarm64" } }
        "osx-x64"     { return @{ AppAsset = "macosx64.zip";   SetupAsset = "setupmacosx64.zip";   AppProfile = "macosx64";   SetupProfile = "macosx64";   AppFolder = "macosx64";   SetupFolder = "setupmacosx64" } }
        "osx-arm64"   { return @{ AppAsset = "macosarm64.zip"; SetupAsset = "setupmacosarm64.zip"; AppProfile = "macosarm64"; SetupProfile = "macosarm64"; AppFolder = "macosarm64"; SetupFolder = "setupmacosarm64" } }
        default { throw "Unsupported release runtime: $Rid" }
    }
}

function Assert-PublishedConfigurationFiles {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$PublishRoot
    )

    $configurationSources = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $SourceRoot -File -Filter "appsettings*.json") { $configurationSources.Add($file) }
    foreach ($directory in @("Configuration", "Localization")) {
        $sourceDirectory = Join-Path $SourceRoot $directory
        if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
            throw "Required PublisherStudio configuration directory is unavailable: $sourceDirectory"
        }
        foreach ($file in Get-ChildItem -LiteralPath $sourceDirectory -File -Recurse) { $configurationSources.Add($file) }
    }

    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($source in $configurationSources) {
        $relative = $source.FullName.Substring($SourceRoot.Length).TrimStart([char[]]"\/")
        $published = Join-Path $PublishRoot $relative
        if (-not (Test-Path -LiteralPath $published -PathType Leaf)) { $missing.Add($relative) }
    }
    if ($missing.Count -gt 0) {
        throw "PublisherStudio publish output is missing configuration files: $($missing -join ', ')"
    }
    Write-Host "Published configuration validation passed for $($configurationSources.Count) files." -ForegroundColor Green
}

function Assert-PublisherStudioDocumentationPayload {
    param(
        [Parameter(Mandatory)][string]$DocumentationRoot,
        [Parameter(Mandatory)][string]$Version
    )

    $requiredArtifacts = @(
        (Join-Path $DocumentationRoot "index.html"),
        (Join-Path $DocumentationRoot "documentation-status.json"),
        (Join-Path $DocumentationRoot "PublisherStudio.Web.xml"),
        (Join-Path $DocumentationRoot "PublisherStudio-$Version.pdf"),
        (Join-Path $DocumentationRoot "styles\publisherstudio-kawaii.css"),
        (Join-Path $DocumentationRoot "styles\publisherstudio-kawaii.js"),
        (Join-Path $DocumentationRoot "favicon.svg"),
        (Join-Path $DocumentationRoot "favicon.ico"),
        (Join-Path $DocumentationRoot "logo.svg")
    )
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Published PublisherStudio documentation is incomplete: $requiredArtifact"
        }
    }

    $status = Get-Content -LiteralPath (Join-Path $DocumentationRoot "documentation-status.json") -Raw | ConvertFrom-Json
    if ([string]$status.documentationMode -ne "docfx") { throw "Published PublisherStudio documentation did not use the DocFX modern site." }
    if ([string]$status.pdfMode -notin @("html-browser-print", "docfx-pdf-plugin")) { throw "Published PublisherStudio documentation does not contain the complete HTML-backed documentation PDF." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.pdfSourcePageCount -lt 10) { throw "The PublisherStudio documentation PDF did not include the expected HTML page set." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.apiHtmlCount -gt 0 -and [int]$status.pdfSourcePageCount -lt [int]$status.apiHtmlCount) { throw "The PublisherStudio documentation PDF omitted generated API pages." }
    if (-not ([bool]$status.completeApiReference)) { throw "Published PublisherStudio documentation is missing the complete XML-generated API reference." }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "Published PublisherStudio documentation contains an incomplete API graph." }
    if ([long]$status.pdfBytes -lt 1048576) { throw "Published PublisherStudio documentation contains an unexpectedly small PDF." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "Published PublisherStudio documentation did not record a real documentation PDF source." }

    $index = Get-Content -LiteralPath (Join-Path $DocumentationRoot "index.html") -Raw
    foreach ($marker in @(
        "publisherstudio-kawaii-docs",
        "data-publisherstudio-theme-bootstrap",
        "data-publisherstudio-favicon",
        "data-publisherstudio-kawaii-style",
        "data-publisherstudio-kawaii-script"
    )) {
        if ($index.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
            throw "Published PublisherStudio documentation is missing Kawaii marker: $marker"
        }
    }

    Write-Host "Verified complete PublisherStudio $Version DocFX modern HTML and HTML-backed PDF documentation in $DocumentationRoot" -ForegroundColor Green
}

function Assert-ReleaseArchiveLayout {
    param(
        [Parameter(Mandatory)][string]$ArchivePath,
        [Parameter(Mandatory)][string]$RootFolderName,
        [Parameter(Mandatory)][string]$Executable
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/').TrimStart('/') })
        $expectedExecutable = "$RootFolderName/$Executable"
        if (-not ($names -contains $expectedExecutable)) {
            throw "Release archive does not contain ${expectedExecutable}: $ArchivePath"
        }
        foreach ($name in $names) {
            if ([string]::IsNullOrWhiteSpace($name)) { continue }
            if (-not $name.StartsWith("$RootFolderName/", [StringComparison]::OrdinalIgnoreCase)) {
                throw "Release archive entry '$name' escapes expected wrapper '$RootFolderName'."
            }
            if ($name.StartsWith('/', [StringComparison]::Ordinal) -or $name.Split('/') -contains '..') {
                throw "Unsafe archive path '$name' in $ArchivePath"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}


function New-PublisherStudioReleaseArchive {
    param(
        [Parameter(Mandatory)][string]$SourceDirectory,
        [Parameter(Mandatory)][string]$DestinationPath,
        [Parameter(Mandatory)][string]$RootFolderName
    )

    $sourceRoot = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        throw "Release archive source directory does not exist: $sourceRoot"
    }
    if ([string]::IsNullOrWhiteSpace($RootFolderName) -or $RootFolderName.IndexOfAny([char[]]"/\\") -ge 0) {
        throw "Release archive wrapper must be one directory name: $RootFolderName"
    }

    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Release archive source is empty: $sourceRoot" }

    $destination = [IO.Path]::GetFullPath($DestinationPath)
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    $temporaryArchive = "$destination.$([Guid]::NewGuid().ToString('N')).tmp"

    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    try {
        $archive = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($file in $files) {
                $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
                if ([string]::IsNullOrWhiteSpace($relative) -or $relative.Split('/') -contains '..') {
                    throw "Unsafe release archive source path: $($file.FullName)"
                }
                $entryName = "$RootFolderName/$relative"
                $written = $false
                $lastReadError = $null
                for ($attempt = 1; $attempt -le 4 -and -not $written; $attempt++) {
                    $entry = $null
                    try {
                        $input = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                        try {
                            $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
                            $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                            $output = $entry.Open()
                            try { $input.CopyTo($output) }
                            finally { $output.Dispose() }
                        }
                        finally { $input.Dispose() }
                        $written = $true
                    }
                    catch {
                        $lastReadError = $_.Exception
                        if ($null -ne $entry) {
                            try { $entry.Delete() } catch { }
                        }
                        if ($attempt -lt 4) { Start-Sleep -Milliseconds (150 * $attempt) }
                    }
                }
                if (-not $written) {
                    throw "Could not add release file '$($file.FullName)' after 4 attempts: $($lastReadError.Message)"
                }
            }
        }
        finally { $archive.Dispose() }

        $verification = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
        try {
            $entries = @($verification.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) })
            if ($entries.Count -ne $files.Count) {
                throw "Release archive entry count $($entries.Count) does not match source file count $($files.Count): $temporaryArchive"
            }
        }
        finally { $verification.Dispose() }

        Remove-Item -LiteralPath $destination -Force -ErrorAction SilentlyContinue
        [IO.File]::Move($temporaryArchive, $destination)
    }
    finally {
        Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    }
}

$appVersion = Resolve-ProjectVersion -ProjectPath $webProject
$setupVersion = Resolve-ProjectVersion -ProjectPath $setupProject
if ($appVersion -ne $setupVersion) { throw "PublisherStudio application version $appVersion does not match setup version $setupVersion." }

function Ensure-WireProtocolPackage {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null

    if ($UseBundledWireProtocolPackage) {
        if (-not (Test-Path -LiteralPath $wireProtocolPackage -PathType Leaf)) {
            throw "The cached official LocalGPT protocol package is unavailable: $wireProtocolPackage"
        }
        return
    }

    $ensureArguments = @{
        Version = $WireProtocolVersion
        PackageDirectory = $packageDirectory
        PackageUrl = $WireProtocolPackageUrl
        LocalGptRepository = $LocalGptRepository
    }
    if ($RefreshWireProtocolPackage) { $ensureArguments.ForceDownload = $true }
    & (Join-Path $root "build\Ensure-WireProtocolPackage.ps1") @ensureArguments | Out-Null
    if (-not (Test-Path -LiteralPath $wireProtocolPackage -PathType Leaf)) {
        throw "LocalGPT protocol package preparation did not produce $wireProtocolPackage"
    }
}

function Prepare-PublisherStudioClientAssets {
    Write-Host "Preparing local DevExpress client assets and runtime license..." -ForegroundColor Cyan
    & (Join-Path $root "Prepare-DevExpressAssets.ps1")

    $requiredAssets = @(
        "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.js",
        "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.css",
        "wwwroot\vendor\devextreme-dist\js\dx.all.js",
        "wwwroot\vendor\devextreme-dist\css\dx.light.css",
        "wwwroot\vendor\jquery\jquery.min.js",
        "wwwroot\vendor\devextreme-license.js",
        "wwwroot\vendor\devextreme-license.meta.json",
        "wwwroot\vendor\devextreme-license.version",
        "wwwroot\vendor\devextreme-assets.meta.json"
    )
    $missingAssets = @($requiredAssets | Where-Object { -not (Test-Path -LiteralPath (Join-Path $webDirectory $_) -PathType Leaf) })
    if ($missingAssets.Count -gt 0) {
        throw "DevExpress client assets are incomplete. Missing: $($missingAssets -join ', ')"
    }
}

function Get-WireProperties {
    return @(
        "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
        "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
        "-p:RestoreAdditionalProjectSources=$packageDirectory",
        "-p:SkipWireProtocolBootstrap=true"
    )
}

function Prepare-PublisherStudioDocumentation {
    if ($script:documentationPrepared) { return }
    if (-not (Test-Path -LiteralPath $documentationScript -PathType Leaf)) {
        throw "Documentation build script not found: $documentationScript"
    }

    $neutralOutputRoot = Join-Path $webDirectory "bin\$Configuration\net10.0"
    $documentationAssembly = Join-Path $neutralOutputRoot "PublisherStudio.Web.dll"
    $documentationXml = Join-Path $neutralOutputRoot "PublisherStudio.Web.xml"
    $documentationOutput = Join-Path $neutralOutputRoot "wwwroot\help-docs"
    $wireProperties = Get-WireProperties
    $documentationProperties = @(
        "-p:RuntimeIdentifier=",
        "-p:RuntimeIdentifiers=",
        "-p:BuildPublisherStudioDocumentation=false",
        "-p:SeedPublisherStudioGitHubPagesSnapshotOnBuild=false",
        "-p:RequirePublisherStudioDocumentationPdf=false"
    )

    Write-Host "Building the RID-neutral PublisherStudio assembly once for shared release documentation..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("restore", $webProject, "--disable-parallel", "--force-evaluate") + $wireProperties + $documentationProperties) -FailureMessage "RID-neutral PublisherStudio restore for documentation failed."
    Invoke-DotNet -Arguments (@("build", $webProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") + $wireProperties + $documentationProperties) -FailureMessage "RID-neutral PublisherStudio build for documentation failed."

    if (-not (Test-Path -LiteralPath $documentationAssembly -PathType Leaf)) { throw "Documentation assembly not found: $documentationAssembly" }
    if (-not (Test-Path -LiteralPath $documentationXml -PathType Leaf)) { throw "Documentation XML not found: $documentationXml" }

    Write-Host "Generating the complete PublisherStudio documentation once for all runtime packages..." -ForegroundColor Cyan
    & $documentationScript `
        -RepositoryRoot $root `
        -AssemblyPath $documentationAssembly `
        -XmlDocumentationPath $documentationXml `
        -Version $appVersion `
        -OutputWebRoot $documentationOutput `
        -RequirePdf

    Assert-PublisherStudioDocumentationPayload -DocumentationRoot $documentationOutput -Version $appVersion
    if (-not (Test-Path -LiteralPath $pagesSnapshotScript -PathType Leaf)) { throw "GitHub Pages snapshot script not found: $pagesSnapshotScript" }
    Write-Host "Validating and seeding the PublisherStudio $appVersion GitHub Pages snapshot from the release documentation payload..." -ForegroundColor Cyan
    & $pagesSnapshotScript -DocumentationRoot $documentationOutput -OutputArchive $pagesSnapshotArchive
    if (-not (Test-Path -LiteralPath $pagesSnapshotArchive -PathType Leaf)) { throw "PublisherStudio GitHub Pages snapshot update failed to create $pagesSnapshotArchive." }
    Remove-Item -LiteralPath $script:documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $script:documentationCacheRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $documentationOutput "*") -Destination $script:documentationCacheRoot -Recurse -Force
    $script:documentationPrepared = $true
    Write-Host "Cached one verified documentation payload for all RID publishes." -ForegroundColor Green
}

function Publish-Runtime {
    param([Parameter(Mandatory)][string]$Rid)

    $profile = Resolve-ReleaseProfile -Rid $Rid
    $appFolder = Resolve-ProfilePublishFolder -ProjectPath $webProject -ProfileName $profile.AppProfile
    $setupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $profile.SetupProfile
    $appZip = Join-Path $artifacts $profile.AppAsset
    $setupZip = Join-Path $artifacts $profile.SetupAsset

    Remove-Item $appFolder, $setupFolder, $appZip, $setupZip -Recurse -Force -ErrorAction SilentlyContinue
    $wireProperties = Get-WireProperties

    Write-Host "Restoring PublisherStudio application for $Rid after protocol preparation..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("restore", $webProject, "-r", $Rid, "--disable-parallel") + $wireProperties) -FailureMessage "PublisherStudio application restore failed for $Rid."

    Write-Host "Publishing PublisherStudio application through profile $($profile.AppProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@(
        "publish", $webProject,
        "-c", $Configuration,
        "-p:PublishProfile=$($profile.AppProfile)",
        "-p:BuildPublisherStudioDocumentation=false",
        "-p:SeedPublisherStudioGitHubPagesSnapshotOnBuild=false",
        "-p:RequirePublisherStudioDocumentationPdf=false",
        "--no-restore",
        "-maxcpucount:1"
    ) + $wireProperties) -FailureMessage "PublisherStudio application publish failed for $Rid."

    Write-Host "Restoring PublisherStudio setup for $Rid..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @("restore", $setupProject, "-r", $Rid, "--disable-parallel") -FailureMessage "PublisherStudio setup restore failed for $Rid."

    Write-Host "Publishing PublisherStudio setup through profile $($profile.SetupProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $setupProject,
        "-c", $Configuration,
        "-p:PublishProfile=$($profile.SetupProfile)",
        "--no-restore",
        "-maxcpucount:1",
        "-p:DebugType=None",
        "-p:DebugSymbols=false"
    ) -FailureMessage "PublisherStudio setup publish failed for $Rid."

    $appExecutable = if ($Rid.StartsWith("win-")) { "PublisherStudio.Web.exe" } else { "PublisherStudio.Web" }
    $setupExecutable = if ($Rid.StartsWith("win-")) { "PublisherStudio.Setup.exe" } else { "PublisherStudio.Setup" }
    if (-not (Test-Path -LiteralPath (Join-Path $appFolder $appExecutable) -PathType Leaf)) { throw "Published PublisherStudio executable not found: $(Join-Path $appFolder $appExecutable)" }
    if (-not (Test-Path -LiteralPath (Join-Path $setupFolder $setupExecutable) -PathType Leaf)) { throw "Published PublisherStudio setup executable not found: $(Join-Path $setupFolder $setupExecutable)" }

    if (-not (Test-Path -LiteralPath $script:documentationCacheRoot -PathType Container)) {
        throw "The shared PublisherStudio documentation cache is missing: $script:documentationCacheRoot"
    }
    $publishedDocumentationRoot = Join-Path $appFolder "wwwroot\help-docs"
    Remove-Item -LiteralPath $publishedDocumentationRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $publishedDocumentationRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $script:documentationCacheRoot "*") -Destination $publishedDocumentationRoot -Recurse -Force
    Write-Host "Reused the verified complete documentation payload for $Rid." -ForegroundColor Cyan

    Assert-PublishedConfigurationFiles -SourceRoot $webDirectory -PublishRoot $appFolder
    Assert-PublisherStudioDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion

    $protocolAppDirectory = Join-Path $appFolder "protocol"
    $protocolSetupDirectory = Join-Path $setupFolder "protocol"
    New-Item -ItemType Directory -Path $protocolAppDirectory, $protocolSetupDirectory -Force | Out-Null
    Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $protocolAppDirectory $wireProtocolPackageName) -Force
    Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $protocolSetupDirectory $wireProtocolPackageName) -Force

    $publisherIcon = Join-Path $root "assets\PublisherStudio.ico"
    if (-not (Test-Path -LiteralPath $publisherIcon -PathType Leaf)) { throw "PublisherStudio release icon is unavailable: $publisherIcon" }
    Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $setupFolder "PublisherStudio.ico") -Force
    Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $appFolder "PublisherStudio.ico") -Force

    $requiredSetupFiles = @("Install.cmd", "Update.cmd", "Start.cmd", "PublisherStudio.ico")
    $missingSetupFiles = @($requiredSetupFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $setupFolder $_) -PathType Leaf) })
    if ($missingSetupFiles.Count -gt 0) { throw "Published setup is incomplete. Missing: $($missingSetupFiles -join ', ')" }

    New-PublisherStudioReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip -RootFolderName $profile.AppFolder
    New-PublisherStudioReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip -RootFolderName $profile.SetupFolder
    Assert-ReleaseArchiveLayout -ArchivePath $appZip -RootFolderName $profile.AppFolder -Executable $appExecutable
    Assert-ReleaseArchiveLayout -ArchivePath $setupZip -RootFolderName $profile.SetupFolder -Executable $setupExecutable
    Write-Host "Created $appZip" -ForegroundColor Green
    Write-Host "Created $setupZip" -ForegroundColor Green
}

New-Item -ItemType Directory -Path $packageDirectory, $artifacts -Force | Out-Null
Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
Ensure-WireProtocolPackage
Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $artifacts $wireProtocolPackageName) -Force
Prepare-PublisherStudioClientAssets
Prepare-PublisherStudioDocumentation

$runtimes = if ($Runtime -eq "all") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
}
else {
    @($Runtime)
}

try {
    foreach ($rid in $runtimes) { Publish-Runtime -Rid $rid }
}
finally {
    Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Release output: $artifacts" -ForegroundColor Green
Write-Host "Protocol package: $(Join-Path $artifacts $wireProtocolPackageName)" -ForegroundColor Green
