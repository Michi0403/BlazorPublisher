param(
    [ValidateSet("all", "all-rids", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [string]$LocalGptRepository = "",
    [string]$ReleasePackagingVersion = "1.0.2",
    [string]$ReleasePackagingPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$RefreshWireProtocolPackage,
    [switch]$RefreshReleasePackagingPackage,
    [switch]$UseContainerPackaging,
    [switch]$ProvisionNativePackagingTools,
    [switch]$RequireOptionalNativePackages,
    [switch]$AllowUnsignedMacPackages,
    [string]$DocumentationCacheRoot = "",
    [switch]$DisableDocumentationToolProvisioning,
    [string]$ReleaseOutputRoot = "",
    [switch]$ForceRebuildArtifacts,
    [ValidateSet("Auto", "Off", "Require")]
    [string]$WslLinux = "Auto",
    [string]$WslDistribution = "",
    [ValidateSet("IfStarted", "Always", "Never")]
    [string]$WslShutdown = "IfStarted",
    [switch]$ProvisionWslBuildTools,
    [switch]$KeepWslBuildTree,
    [switch]$WslChildBuild,
    [switch]$SkipReleaseBundle,
    [string]$PreparedDocumentationRoot = "",
    [switch]$UsePreparedClientAssets
)

$ErrorActionPreference = "Stop"
# File-provider progress is deliberately suppressed. Recursive Remove-Item progress races with
# DocFX/Spectre rendering and can report impossible file/byte totals in the shared terminal.
$ProgressPreference = "SilentlyContinue"
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
if (-not [string]::IsNullOrWhiteSpace($DocumentationCacheRoot)) {
    $env:FUTURE2_DOCUMENTATION_CACHE_ROOT = [IO.Path]::GetFullPath($DocumentationCacheRoot)
}
$nodeRuntimeCommonScript = Join-Path $root 'build/NodeRuntime.Common.ps1'
if (-not (Test-Path -LiteralPath $nodeRuntimeCommonScript -PathType Leaf)) { throw "Documentation runtime helper is missing: $nodeRuntimeCommonScript" }
. $nodeRuntimeCommonScript
$wslCommonScript = Join-Path $root 'build/WslRelease.Common.ps1'
if (-not (Test-Path -LiteralPath $wslCommonScript -PathType Leaf)) { throw "WSL release helper is missing: $wslCommonScript" }
. $wslCommonScript
& (Join-Path $root 'build/Assert-PowerShellCompatibility.ps1')
& (Join-Path $root ([IO.Path]::Combine('build', 'Assert-SourcePackagePrerequisites.ps1'))) -RepositoryRoot $root -SkipNodeRuntime:($WslChildBuild -and $UsePreparedClientAssets -and -not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot))
& (Join-Path $root ([IO.Path]::Combine('build', 'Assert-CrossPlatformBoundaries.ps1'))) -RepositoryRoot $root
Write-Host "Refreshing reviewed PublisherStudio frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
& (Join-Path $root 'build/Assert-InteractiveServerRenderModes.ps1')
& (Join-Path $root 'build/Assert-PanelStudioAuthoringGeometry.ps1')
& (Join-Path $root 'build/Assert-PanelStudioInteractionLifecycle.ps1')
& (Join-Path $root 'build/Assert-PanelStudioPersistence.ps1')
& (Join-Path $root 'build/Assert-XmlDocumentationCoverage.ps1')
function Clear-RepositoryReleaseBuildState {
    param([switch]$BestEffort)
    $directories = @(
        Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @("bin", "obj") } |
            Sort-Object FullName -Descending
    )
    foreach ($directory in $directories) {
        if (-not (Test-Path -LiteralPath $directory.FullName)) { continue }
        if ($BestEffort) { Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction SilentlyContinue }
        else { Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction Stop }
    }
    return $directories.Count
}

Write-Host "Clearing repository-local bin/obj build state for the authoritative release build..." -ForegroundColor Cyan
$clearedBuildStateCount = Clear-RepositoryReleaseBuildState
Write-Host "Cleared $clearedBuildStateCount repository-local bin/obj director$(if ($clearedBuildStateCount -eq 1) { 'y' } else { 'ies' }). Durable documentation caches outside bin/obj were preserved." -ForegroundColor DarkCyan
$configuredReleaseOutputRoot = if (-not [string]::IsNullOrWhiteSpace($ReleaseOutputRoot)) { $ReleaseOutputRoot } else { [string]$env:FUTURE2_RELEASE_OUTPUT_ROOT }
$artifacts = if (-not [string]::IsNullOrWhiteSpace($configuredReleaseOutputRoot)) { [IO.Path]::GetFullPath($configuredReleaseOutputRoot) } else { Join-Path $root "artifacts/release" }
$packageDirectory = Join-Path $root "packages"
$webProject = Join-Path $root "src/PublisherStudio.Web/PublisherStudio.Web.csproj"
$webDirectory = Split-Path -Parent $webProject
$setupProject = Join-Path $root "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj"
$documentationScript = Join-Path $root "build/Build-Documentation.ps1"
$pagesSnapshotScript = Join-Path $root "build/Update-GitHubPagesSnapshot.ps1"
$pagesSnapshotArchive = Join-Path $root ".github/pages/publisherstudio-kawaii-docs.zip"
$wireProtocolPackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wireProtocolPackage = Join-Path $packageDirectory $wireProtocolPackageName
$documentationToolCacheBase = Get-PublisherStudioDocumentationToolCacheRoot -FallbackRoot (Join-Path $root 'artifacts/.documentation-tools')
$documentationCacheRoot = Join-Path $documentationToolCacheBase 'release-payload/PublisherStudio' 
$documentationPrepared = $false
$releaseZipPaths = New-Object 'System.Collections.Generic.List[string]'
$releasePackagingPackageName = "LocalGPT.ReleasePackaging.$ReleasePackagingVersion.nupkg"
$releasePackagingPackage = Join-Path $packageDirectory $releasePackagingPackageName
$releasePackagingTool = $null
$nativeReleasePackagingScript = Join-Path $root 'build/NativeReleasePackaging.ps1'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$FailureMessage
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Get-ReleaseHostFamily {
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) { return 'Windows' }
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)) { return 'Linux' }
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)) { return 'macOS' }
    throw 'Unsupported release host. PublisherStudio release builds support Windows, Linux, and macOS.'
}

function Get-HostDefaultRuntimes {
    switch (Get-ReleaseHostFamily) {
        'Windows' { return @('win-x64', 'win-x86', 'win-arm64') }
        'Linux'   { return @('linux-x64', 'linux-arm64') }
        'macOS'   { return @('osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64', 'win-x64', 'win-x86', 'win-arm64') }
    }
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
    $profilePath = Join-Path $projectDirectory "Properties/PublishProfiles/$ProfileName.pubxml"
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
        [Parameter(Mandatory)][string]$Version,
        [switch]$RequirePhysicalPdf
    )

    $requiredArtifacts = @(
        (Join-Path $DocumentationRoot "index.html"),
        (Join-Path $DocumentationRoot "api/index.html"),
        (Join-Path $DocumentationRoot "api/toc.html"),
        (Join-Path $DocumentationRoot "public/docfx.min.css"),
        (Join-Path $DocumentationRoot "public/docfx.min.js"),
        (Join-Path $DocumentationRoot "documentation-status.json"),
        (Join-Path $DocumentationRoot "PublisherStudio.Web.xml"),
        (Join-Path $DocumentationRoot "styles/publisherstudio-kawaii.css"),
        (Join-Path $DocumentationRoot "styles/publisherstudio-kawaii.js"),
        (Join-Path $DocumentationRoot "favicon.svg"),
        (Join-Path $DocumentationRoot "favicon.ico"),
        (Join-Path $DocumentationRoot "logo.svg"),
        (Join-Path $DocumentationRoot "PublisherStudio-$Version.pdf")
    )
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Published PublisherStudio documentation is incomplete: $requiredArtifact"
        }
    }

    $status = Get-Content -LiteralPath (Join-Path $DocumentationRoot "documentation-status.json") -Raw | ConvertFrom-Json
    if (-not [string]::Equals([string]$status.version, $Version, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Published PublisherStudio documentation version '$($status.version)' does not match application version '$Version'."
    }
    $versionedPdfs = @(Get-ChildItem -LiteralPath $DocumentationRoot -File -Filter 'PublisherStudio-*.pdf' -ErrorAction SilentlyContinue)
    $versionedPdfNames = @($versionedPdfs | ForEach-Object { $_.Name })
    $versionedPdfDisplay = if ($versionedPdfNames.Count -eq 0) { '<none>' } else { $versionedPdfNames -join ', ' }
    if ($versionedPdfs.Count -ne 1 -or -not [string]::Equals($versionedPdfs[0].Name, "PublisherStudio-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Published PublisherStudio documentation must contain exactly one current embedded PDF (PublisherStudio-$Version.pdf). Found: $versionedPdfDisplay"
    }
    if ([string]$status.documentationMode -ne "docfx") { throw "Published PublisherStudio documentation did not use the DocFX modern site." }
    $browserBackedPdfModes = @("html-browser-print", "html-browser-print-compatibility", "html-browser-chunked")
    if ([string]$status.pdfMode -notin @($browserBackedPdfModes + "docfx-pdf-plugin")) { throw "Published PublisherStudio documentation does not contain the complete HTML-backed documentation PDF." }
    if (-not ([bool]$status.htmlPreflightValidated)) { throw "Published PublisherStudio documentation did not pass the generated HTML accessibility/link preflight before PDF rendering." }
    $acceptedPdfAccessibilityModes = if ([string]$status.pdfMode -eq "html-browser-print" -and [string]$status.pdfCompressionMode -eq "browser-native") {
        @("tagged-pdf-required")
    } elseif ([string]$status.pdfMode -eq "html-browser-print" -and [string]$status.pdfCompressionMode -eq "cached-validated-pdf") {
        # The durable cache preserves the validated PDF/accessibility result but intentionally records cache reuse
        # rather than the original compression mode. A cached browser-native tagged PDF and a cached post-processed
        # HTML-fallback PDF are therefore both valid, while unknown/unavailable accessibility states remain rejected.
        @("tagged-pdf-required", "html-accessibility-fallback")
    } else {
        @("html-accessibility-fallback")
    }
    if ([string]$status.pdfAccessibilityMode -notin $acceptedPdfAccessibilityModes) { throw "Published PublisherStudio documentation has an unexpected PDF accessibility mode '$($status.pdfAccessibilityMode)' for PDF mode '$($status.pdfMode)' and compression mode '$($status.pdfCompressionMode)'." }
    if ([string]$status.pdfMode -in $browserBackedPdfModes -and [int]$status.pdfSourcePageCount -lt 10) { throw "The PublisherStudio documentation PDF did not include the expected HTML page set." }
    if ([string]$status.pdfMode -in $browserBackedPdfModes -and [int]$status.apiHtmlCount -gt 0 -and [int]$status.pdfSourcePageCount -lt [int]$status.apiHtmlCount) { throw "The PublisherStudio documentation PDF omitted generated API pages." }
    if (-not ([bool]$status.completeApiReference)) { throw "Published PublisherStudio documentation is missing the complete XML-generated API reference." }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "Published PublisherStudio documentation contains an incomplete API graph." }
    $physicalApiHtmlCount = @(Get-ChildItem -LiteralPath (Join-Path $DocumentationRoot "api") -Filter "*.html" -File -Recurse -ErrorAction SilentlyContinue).Count
    if ($physicalApiHtmlCount -le 1) { throw "Published PublisherStudio documentation API directory is physically incomplete ($physicalApiHtmlCount HTML file(s))." }
    $apiIndexText = Get-Content -LiteralPath (Join-Path $DocumentationRoot "api/index.html") -Raw
    if ($apiIndexText.IndexOf("PublisherStudio API reference", [StringComparison]::OrdinalIgnoreCase) -lt 0) { throw "Published PublisherStudio api/index.html is not the generated API reference entry point." }
    if ([long]$status.pdfBytes -lt 1048576) { throw "Published PublisherStudio documentation contains an unexpectedly small PDF." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "Published PublisherStudio documentation did not record a real documentation PDF source." }
    if (-not ([bool]$status.pdfAvailable)) { throw "Runtime documentation status must declare pdfAvailable=true because the compressed handbook is embedded." }
    if (-not ([bool]$status.runtimePdfPublished)) { throw "Runtime documentation status must declare runtimePdfPublished=true because the compressed handbook is embedded." }
    if (-not [string]::Equals([string]$status.releasePdfFileName, "PublisherStudio-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) { throw "Runtime documentation did not preserve the embedded PDF identity." }
    if ([long]$status.releasePdfBytes -lt 1048576) { throw "Runtime documentation did not preserve the embedded PDF size metadata." }
    $physicalPdf = Get-Item -LiteralPath (Join-Path $DocumentationRoot "PublisherStudio-$Version.pdf")
    if ([long]$physicalPdf.Length -ne [long]$status.pdfBytes) { throw "Embedded PublisherStudio PDF byte size does not match documentation-status.json." }
    if ($null -ne $status.maximumSanePdfBytes -and [long]$physicalPdf.Length -gt [long]$status.maximumSanePdfBytes) { throw "Embedded PublisherStudio PDF exceeds the configured sane-size ceiling." }

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

    Write-Host "Verified complete PublisherStudio $Version DocFX modern HTML and compressed embedded PDF documentation in $DocumentationRoot" -ForegroundColor Green
}

function Assert-ReleaseArchiveLayout {
    param(
        [Parameter(Mandatory)][string]$ArchivePath,
        [Parameter(Mandatory)][string]$RootFolderName,
        [Parameter(Mandatory)][string]$Executable,
        [string]$Version = "",
        [switch]$RequireDocumentation
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $rawNames = @($archive.Entries | ForEach-Object { $_.FullName })
        $backslashEntries = @($rawNames | Where-Object { $_.Contains('\') })
        if ($backslashEntries.Count -gt 0) {
            throw "Release archive contains Windows-style backslash entry names that flatten on POSIX extractors: $($backslashEntries -join ', ')"
        }
        $names = @($rawNames | ForEach-Object { $_.TrimStart('/') })
        $expectedExecutable = "$RootFolderName/$Executable"
        if (-not ($names -contains $expectedExecutable)) {
            throw "Release archive does not contain ${expectedExecutable}: $ArchivePath"
        }
        if (-not [string]::IsNullOrWhiteSpace($Version)) {
            $versionStampName = "$RootFolderName/RELEASE-VERSION.txt"
            $sourceStampName = "$RootFolderName/SOURCE-SHA256.txt"
            if (-not ($names -contains $versionStampName) -or -not ($names -contains $sourceStampName)) {
                throw "Release archive is missing version/source identity stamps: $ArchivePath"
            }
            $versionStampEntry = $archive.Entries | Where-Object { $_.FullName.TrimStart('/') -ieq $versionStampName } | Select-Object -First 1
            $versionReader = [IO.StreamReader]::new($versionStampEntry.Open())
            try { $stampedVersion = $versionReader.ReadToEnd().Trim() } finally { $versionReader.Dispose() }
            if (-not [string]::Equals($stampedVersion, $Version, [StringComparison]::Ordinal)) {
                throw "Release archive version stamp '$stampedVersion' does not match '$Version': $ArchivePath"
            }
            $sourceStampEntry = $archive.Entries | Where-Object { $_.FullName.TrimStart('/') -ieq $sourceStampName } | Select-Object -First 1
            if ($null -eq $sourceStampEntry -or $sourceStampEntry.Length -lt 64) { throw "Release archive source identity stamp is missing or malformed: $ArchivePath" }
        }
        if ($RequireDocumentation) {
            $requiredDocumentation = @(
                "$RootFolderName/wwwroot/help-docs/index.html",
                "$RootFolderName/wwwroot/help-docs/api/index.html",
                "$RootFolderName/wwwroot/help-docs/documentation-status.json",
                "$RootFolderName/wwwroot/help-docs/public/docfx.min.css",
                "$RootFolderName/wwwroot/help-docs/public/docfx.min.js",
                "$RootFolderName/wwwroot/help-docs/styles/publisherstudio-kawaii.css",
                "$RootFolderName/wwwroot/help-docs/styles/publisherstudio-kawaii.js"
            )
            foreach ($requiredDocumentationEntry in $requiredDocumentation) {
                if (-not ($names -contains $requiredDocumentationEntry)) {
                    throw "Release archive is missing required installed documentation entry ${requiredDocumentationEntry}: $ArchivePath"
                }
                $documentationEntry = $archive.Entries | Where-Object { $_.FullName.TrimStart('/') -ieq $requiredDocumentationEntry } | Select-Object -First 1
                $minimumBytes = if ($requiredDocumentationEntry.EndsWith('documentation-status.json', [StringComparison]::OrdinalIgnoreCase)) { 64L } else { 512L }
                if ($null -eq $documentationEntry -or $documentationEntry.Length -lt $minimumBytes) {
                    throw "Release archive contains empty or truncated installed documentation entry ${requiredDocumentationEntry}: $ArchivePath"
                }
            }
            if ([string]::IsNullOrWhiteSpace($Version)) { throw "Version is required when validating release documentation." }
            $expectedPdf = "$RootFolderName/wwwroot/help-docs/PublisherStudio-$Version.pdf"
            $pdfPrefix = "$RootFolderName/wwwroot/help-docs/PublisherStudio-"
            $versionedPdfs = @($names | Where-Object { $_.StartsWith($pdfPrefix, [StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.pdf', [StringComparison]::OrdinalIgnoreCase) })
            if ($versionedPdfs.Count -ne 1 -or -not [string]::Equals($versionedPdfs[0], $expectedPdf, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Runtime release archive must contain exactly the current compressed embedded PublisherStudio PDF '$expectedPdf'. Found: $($versionedPdfs -join ', ')"
            }
            $pdfEntry = $archive.Entries | Where-Object { $_.FullName.TrimStart('/') -ieq $expectedPdf } | Select-Object -First 1
            if ($null -eq $pdfEntry -or $pdfEntry.Length -lt 1048576L) {
                throw "Runtime release archive contains a missing or unexpectedly small embedded PublisherStudio PDF: $ArchivePath"
            }
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
        [Parameter(Mandatory)][string]$RootFolderName,
        [string[]]$UnixExecutableRelativePaths = @(),
        [switch]$WriteUnixPermissions
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

    $unixExecutableSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($unixExecutablePath in @($UnixExecutableRelativePaths)) {
        if ([string]::IsNullOrWhiteSpace($unixExecutablePath)) { continue }
        $normalizedExecutablePath = $unixExecutablePath.TrimStart([char[]]"\/").Replace('\', '/')
        if ($normalizedExecutablePath.Split('/') -contains '..') { throw "Unsafe Unix executable path: $unixExecutablePath" }
        [void]$unixExecutableSet.Add($normalizedExecutablePath)
    }
    $unixRegularFileAttributes = [int]-2119958528 # 0100644 << 16
    $unixExecutableFileAttributes = [int]-2115174400 # 0100755 << 16

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
                            if ($WriteUnixPermissions) {
                                $entry.ExternalAttributes = if ($unixExecutableSet.Contains($relative)) { $unixExecutableFileAttributes } else { $unixRegularFileAttributes }
                            }
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
            if ($WriteUnixPermissions) {
                foreach ($unixExecutablePath in $unixExecutableSet) {
                    $expectedEntryName = "$RootFolderName/$unixExecutablePath"
                    $executableEntry = $entries | Where-Object { $_.FullName -eq $expectedEntryName } | Select-Object -First 1
                    if ($null -eq $executableEntry) { throw "Unix executable entry is missing from release archive: $expectedEntryName" }
                    $permissionBits = (($executableEntry.ExternalAttributes -shr 16) -band 511)
                    if ($permissionBits -ne 493) { throw "Unix executable entry '$expectedEntryName' does not carry mode 0755 (actual permission bits: $permissionBits)." }
                }
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


function Get-ReleaseSourceFingerprint {
    $rootFull = [IO.Path]::GetFullPath($root).TrimEnd([char[]]@('\', '/'))
    $rootPrefix = $rootFull + [IO.Path]::DirectorySeparatorChar
    $excludedSegments = @('.git', 'artifacts', 'bin', 'obj', '__pycache__', '.pytest_cache', 'packages')
    $fingerprintLines = New-Object 'System.Collections.Generic.List[string]'
    $files = @(Get-ChildItem -LiteralPath $rootFull -File -Recurse -Force -ErrorAction SilentlyContinue | Sort-Object FullName)
    foreach ($file in $files) {
        if (-not $file.FullName.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\', '/')
        $segments = @($relative -split '/')
        if (@($segments | Where-Object { $_ -in $excludedSegments }).Count -gt 0) { continue }
        if ($relative.StartsWith('src/PublisherStudio.Web/wwwroot/help-docs/', [StringComparison]::OrdinalIgnoreCase)) { continue }
        if ($relative.StartsWith('.github/pages/', [StringComparison]::OrdinalIgnoreCase)) { continue }
        if ($file.Extension -in @('.pyc', '.pyo')) { continue }
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        [void]$fingerprintLines.Add("$relative`t$hash")
    }
    if ($fingerprintLines.Count -eq 0) { throw 'Release source fingerprint cannot be computed from an empty source set.' }
    $payload = [Text.Encoding]::UTF8.GetBytes(($fingerprintLines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($payload))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Initialize-ReleaseArtifactSourceIdentity {
    param([Parameter(Mandatory)][string]$Version)
    New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
    $markerPath = Join-Path $artifacts "PublisherStudio-$Version-SOURCE-SHA256.txt"
    $existingFingerprint = if (Test-Path -LiteralPath $markerPath -PathType Leaf) { ([string](Get-Content -LiteralPath $markerPath -Raw -Encoding UTF8)).Trim().ToLowerInvariant() } else { '' }
    $sourceChanged = -not [string]::Equals($existingFingerprint, $script:releaseSourceFingerprint, [StringComparison]::Ordinal)
    if ($ForceRebuildArtifacts -or $sourceChanged) {
        $reason = if ($ForceRebuildArtifacts) { 'forced rebuild' } elseif ([string]::IsNullOrWhiteSpace($existingFingerprint)) { 'missing source fingerprint' } else { 'source fingerprint changed' }
        Write-Host "Clearing same-version PublisherStudio release artifacts for $Version because $reason; stale native payloads must not be reused." -ForegroundColor Yellow
        Remove-Item -LiteralPath (Join-Path $artifacts $Version) -Recurse -Force -ErrorAction SilentlyContinue
        Get-ChildItem -LiteralPath $artifacts -File -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ne [IO.Path]::GetFileName($markerPath) -and ($_.Name -like "*-$Version-*" -or $_.Name -like "*-$Version.*" -or $_.Name -like "PublisherStudio-$Version*") } |
            Remove-Item -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $artifacts 'PublisherStudio.app') -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $artifacts 'staging') -Recurse -Force -ErrorAction SilentlyContinue
    }
    [IO.File]::WriteAllText($markerPath, "$($script:releaseSourceFingerprint)`n", (New-Object Text.UTF8Encoding($false)))
}

$script:releaseSourceFingerprint = Get-ReleaseSourceFingerprint
Initialize-ReleaseArtifactSourceIdentity -Version $appVersion


function Test-VersionDirectoryName {
    param([Parameter(Mandatory)][string]$Name)
    return $Name -match '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$'
}

function Test-ExistingReleaseBundleComplete {
    param([Parameter(Mandatory)][string]$Version)
    $versionDirectory = Join-Path $artifacts $Version
    $checksumPath = Join-Path $versionDirectory 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { return $false }
    $lines = @(Get-Content -LiteralPath $checksumPath -Encoding UTF8 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -eq 0) { return $false }
    $manifestNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in $lines) {
        if ([string]$line -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') { return $false }
        $expectedHash = $Matches[1].ToLowerInvariant()
        $name = $Matches[2].Trim()
        if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOfAny([char[]]"/\\") -ge 0) { return $false }
        $path = Join-Path $versionDirectory $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::Equals($expectedHash, $actualHash, [StringComparison]::Ordinal)) { return $false }
        [void]$manifestNames.Add($name)
    }
    $payloadFiles = @(Get-ChildItem -LiteralPath $versionDirectory -File | Where-Object { $_.Name -ne 'SHA256SUMS.txt' })
    return $manifestNames.Count -eq $payloadFiles.Count
}

function Move-OrReuseReleaseFile {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationDirectory,
        [switch]$Move
    )
    $destination = Join-Path $DestinationDirectory ([IO.Path]::GetFileName($SourcePath))
    $sourceFull = [IO.Path]::GetFullPath($SourcePath)
    $destinationFull = [IO.Path]::GetFullPath($destination)
    if ([string]::Equals($sourceFull, $destinationFull, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not (Test-Path -LiteralPath $destinationFull -PathType Leaf)) { throw "Release file is missing: $destinationFull" }
        return $destinationFull
    }

    if (Test-Path -LiteralPath $destinationFull -PathType Leaf) {
        if (Test-Path -LiteralPath $sourceFull -PathType Leaf) {
            $sourceInfo = Get-Item -LiteralPath $sourceFull
            $destinationInfo = Get-Item -LiteralPath $destinationFull
            if ($sourceInfo.Length -ne $destinationInfo.Length) { throw "Release resume conflict: $sourceFull and $destinationFull have different sizes." }
            $sourceHash = (Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash
            $destinationHash = (Get-FileHash -LiteralPath $destinationFull -Algorithm SHA256).Hash
            if (-not [string]::Equals($sourceHash, $destinationHash, [StringComparison]::OrdinalIgnoreCase)) { throw "Release resume conflict: $sourceFull and $destinationFull contain different bytes." }
            if ($Move) { Remove-Item -LiteralPath $sourceFull -Force }
        }
        return $destinationFull
    }

    if (-not (Test-Path -LiteralPath $sourceFull -PathType Leaf)) { throw "Required release file is missing: $sourceFull" }
    if ($Move) { Move-Item -LiteralPath $sourceFull -Destination $destinationFull }
    else { Copy-Item -LiteralPath $sourceFull -Destination $destinationFull -Force }
    return $destinationFull
}

function Complete-ReleaseBundle {
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string[]]$ReleaseZipPaths,
        [Parameter(Mandatory)][string]$DocumentationPdfPath,
        [Parameter(Mandatory)][string]$WindowsX64SetupExecutablePath,
        [Parameter(Mandatory)][string]$ReadmePath,
        [Parameter(Mandatory)][string]$LicensePath,
        [Parameter(Mandatory)][string]$WireProtocolPackagePath,
        [Parameter(Mandatory)][string]$SetupIconPath,
        [Parameter(Mandatory)][bool]$RequireWindowsX64Setup
    )

    $versionDirectory = Join-Path $artifacts $Version
    if ($ForceRebuildArtifacts -and (Test-Path -LiteralPath $versionDirectory -PathType Container)) {
        Remove-Item -LiteralPath $versionDirectory -Recurse -Force
    }
    if ((Test-ExistingReleaseBundleComplete -Version $Version) -and -not $ForceRebuildArtifacts) {
        Write-Host "Upload-ready release bundle already exists and all SHA-256 entries validate: $versionDirectory" -ForegroundColor Green
        return
    }
    New-Item -ItemType Directory -Path $versionDirectory -Force | Out-Null

    $uniqueZipPaths = @($ReleaseZipPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($uniqueZipPaths.Count -eq 0) { throw "No release artifacts were produced for release $Version." }

    # Move large generated artifacts directly into the final version directory. If a previous run
    # died halfway through this step, byte-identical files already present there are reused instead
    # of copied again. This keeps peak disk usage low and makes final-bundle assembly crash-resumable.
    foreach ($zipPath in $uniqueZipPaths) {
        Move-OrReuseReleaseFile -SourcePath $zipPath -DestinationDirectory $versionDirectory -Move | Out-Null
    }
    Move-OrReuseReleaseFile -SourcePath $DocumentationPdfPath -DestinationDirectory $versionDirectory -Move | Out-Null
    foreach ($supportFile in @($ReadmePath, $LicensePath, $WireProtocolPackagePath, $SetupIconPath)) {
        if (-not (Test-Path -LiteralPath $supportFile -PathType Leaf)) { throw "Required upload-ready release file is missing: $supportFile" }
        Move-OrReuseReleaseFile -SourcePath $supportFile -DestinationDirectory $versionDirectory | Out-Null
    }
    if ($RequireWindowsX64Setup) {
        if (-not (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf)) { throw "Windows x64 setup executable is required for the full release bundle but is missing: $WindowsX64SetupExecutablePath" }
        Move-OrReuseReleaseFile -SourcePath $WindowsX64SetupExecutablePath -DestinationDirectory $versionDirectory | Out-Null
    }
    elseif (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf) {
        Move-OrReuseReleaseFile -SourcePath $WindowsX64SetupExecutablePath -DestinationDirectory $versionDirectory | Out-Null
    }

    $checksumPath = Join-Path $versionDirectory 'SHA256SUMS.txt'
    $checksumLines = foreach ($file in Get-ChildItem -LiteralPath $versionDirectory -File | Sort-Object Name) {
        if ($file.Name -eq 'SHA256SUMS.txt') { continue }
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($file.Name)"
    }
    [IO.File]::WriteAllLines($checksumPath, [string[]]$checksumLines, (New-Object Text.UTF8Encoding($false)))
    if (-not (Test-ExistingReleaseBundleComplete -Version $Version)) { throw "Release bundle checksum verification failed after assembly: $versionDirectory" }
    Write-Host "Upload-ready release bundle: $versionDirectory" -ForegroundColor Green
}

if (-not $ForceRebuildArtifacts -and -not $SkipReleaseBundle -and $Runtime -eq 'all' -and (Test-ExistingReleaseBundleComplete -Version $appVersion)) {
    Write-Host "Release $appVersion is already complete and SHA-256 verified at $(Join-Path $artifacts $appVersion). Nothing will be rebuilt or resubmitted. Use -ForceRebuildArtifacts to intentionally rebuild it." -ForegroundColor Green
    return
}

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
    & (Join-Path $root "build/Ensure-WireProtocolPackage.ps1") @ensureArguments | Out-Null
    if (-not (Test-Path -LiteralPath $wireProtocolPackage -PathType Leaf)) {
        throw "LocalGPT protocol package preparation did not produce $wireProtocolPackage"
    }
}

function Prepare-PublisherStudioClientAssets {
    if ($UsePreparedClientAssets) {
        Write-Host "Reusing parent-prepared PublisherStudio DevExpress browser assets in this delegated Linux release child." -ForegroundColor DarkCyan
    }
    else {
        Write-Host "Preparing local DevExpress client assets and runtime license..." -ForegroundColor Cyan
        & (Join-Path $root "Prepare-DevExpressAssets.ps1")
    }

    $requiredAssets = @(
        "wwwroot/vendor/devexpress-aspnetcore-spreadsheet/dist/dx-aspnetcore-spreadsheet.js",
        "wwwroot/vendor/devexpress-aspnetcore-spreadsheet/dist/dx-aspnetcore-spreadsheet.css",
        "wwwroot/vendor/devextreme-dist/js/dx.all.js",
        "wwwroot/vendor/devextreme-dist/css/dx.light.css",
        "wwwroot/vendor/jquery/jquery.min.js",
        "wwwroot/vendor/devextreme-license.js",
        "wwwroot/vendor/devextreme-license.meta.json",
        "wwwroot/vendor/devextreme-license.version",
        "wwwroot/vendor/devextreme-assets.meta.json"
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
    if (-not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot)) {
        $preparedRoot = [IO.Path]::GetFullPath($PreparedDocumentationRoot)
        if (-not (Test-Path -LiteralPath $preparedRoot -PathType Container)) { throw "Prepared PublisherStudio documentation root is missing: $preparedRoot" }
        Assert-PublisherStudioDocumentationPayload -DocumentationRoot $preparedRoot -Version $appVersion -RequirePhysicalPdf
        Remove-Item -LiteralPath $script:documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $script:documentationCacheRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $preparedRoot '*') -Destination $script:documentationCacheRoot -Recurse -Force
        $script:documentationPrepared = $true
        Write-Host "Reused parent-prepared PublisherStudio documentation for this Linux release child." -ForegroundColor Green
        return
    }
    if (-not (Test-Path -LiteralPath $documentationScript -PathType Leaf)) {
        throw "Documentation build script not found: $documentationScript"
    }

    $neutralOutputRoot = Join-Path $webDirectory "bin/$Configuration/net10.0"
    $documentationAssembly = Join-Path $neutralOutputRoot "PublisherStudio.Web.dll"
    $documentationXml = Join-Path $neutralOutputRoot "PublisherStudio.Web.xml"
    $documentationOutput = Join-Path $neutralOutputRoot "wwwroot/help-docs"
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
        -DocumentationCacheRoot $documentationToolCacheBase `
        -PackagingTool $releasePackagingTool `
        -RequirePdf `
        -DisablePdfToolProvisioning:$DisableDocumentationToolProvisioning

    Assert-PublisherStudioDocumentationPayload -DocumentationRoot $documentationOutput -Version $appVersion -RequirePhysicalPdf
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


function Copy-PublisherStudioRuntimeDocumentation {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$DestinationRoot,
        [Parameter(Mandatory)][string]$Version
    )

    $pdfName = "PublisherStudio-$Version.pdf"
    Remove-Item -LiteralPath $DestinationRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $SourceRoot -Force) {
        Copy-Item -LiteralPath $entry.FullName -Destination (Join-Path $DestinationRoot $entry.Name) -Recurse -Force
    }

    $pdfPath = Join-Path $DestinationRoot $pdfName
    if (-not (Test-Path -LiteralPath $pdfPath -PathType Leaf)) {
        throw "Embedded PublisherStudio documentation PDF is missing after runtime documentation copy: $pdfPath"
    }
    $statusPath = Join-Path $DestinationRoot 'documentation-status.json'
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    $releasePdfBytes = [long](Get-Item -LiteralPath $pdfPath).Length
    $status | Add-Member -NotePropertyName releasePdfFileName -NotePropertyValue $pdfName -Force
    $status | Add-Member -NotePropertyName releasePdfBytes -NotePropertyValue $releasePdfBytes -Force
    $status | Add-Member -NotePropertyName runtimePdfPublished -NotePropertyValue $true -Force
    $status | Add-Member -NotePropertyName pdfAvailable -NotePropertyValue $true -Force
    $status | Add-Member -NotePropertyName pdfBytes -NotePropertyValue $releasePdfBytes -Force
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8
}

function Publish-UnixRuntime {
    param([Parameter(Mandatory)][string]$Rid)
    if ($Rid.StartsWith('win-')) { throw "Publish-UnixRuntime received Windows RID $Rid." }
    if (-not $script:releasePackagingTool) { throw 'Release packaging tool was not prepared.' }
    $wireProperties = Get-WireProperties
    $mode = 'Full'
    $selfContained = 'true'
    $publishFolder = Join-Path $artifacts ("staging/$Rid/$($mode.ToLowerInvariant())")
    if ($Rid.StartsWith('osx-') -and -not $ForceRebuildArtifacts) {
        $existingNativeArtifacts = @(
            & $script:nativeReleasePackagingScript -ProductName 'PublisherStudio' -ExecutableName 'PublisherStudio.Web' -Version $appVersion -Rid $Rid -Mode $mode -PayloadDirectory $publishFolder -OutputDirectory $artifacts -PackagingTool $script:releasePackagingTool -DependencyPolicy PublisherStudio -ProbeExistingArtifactsOnly -MacIconSource (Join-Path $root 'assets/PublisherStudio.png') -DmgBackgroundPath (Join-Path $root 'build/assets/PublisherStudio-dmg-background.png')
        )
        if ($existingNativeArtifacts.Count -eq 3) {
            foreach ($artifact in $existingNativeArtifacts) { $script:releaseZipPaths.Add([string]$artifact) }
            return
        }
    }
        Remove-Item -LiteralPath $publishFolder -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $publishFolder -Force | Out-Null
        Write-Host "Publishing PublisherStudio $Rid $mode application payload (no setup console)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments (@('restore', $webProject, '-r', $Rid, '--disable-parallel', '--force-evaluate') + $wireProperties) -FailureMessage "PublisherStudio application restore failed for $Rid $mode."
        Invoke-DotNet -Arguments (@(
            'publish', $webProject, '-c', $Configuration, '-r', $Rid, '--no-restore', '-o', $publishFolder,
            '--self-contained', $selfContained, '-p:PublishSingleFile=false',
            '-p:BuildPublisherStudioDocumentation=false', '-p:SeedPublisherStudioGitHubPagesSnapshotOnBuild=false',
            '-p:RequirePublisherStudioDocumentationPdf=false', '-maxcpucount:1'
        ) + $wireProperties) -FailureMessage "PublisherStudio application publish failed for $Rid $mode."
        $appExecutable = 'PublisherStudio.Web'
        if (-not (Test-Path -LiteralPath (Join-Path $publishFolder $appExecutable) -PathType Leaf)) { throw "Published PublisherStudio apphost is missing for $Rid $mode." }
        $publishedAssemblyPath = Join-Path $publishFolder 'PublisherStudio.Web.dll'
        if (-not (Test-Path -LiteralPath $publishedAssemblyPath -PathType Leaf)) { throw "Published PublisherStudio managed assembly is missing for $Rid $mode." }
        $publishedAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($publishedAssemblyPath).Version
        $publishedSemanticVersion = "$($publishedAssemblyVersion.Major).$($publishedAssemblyVersion.Minor).$($publishedAssemblyVersion.Build)"
        if (-not [string]::Equals($publishedSemanticVersion, $appVersion, [StringComparison]::Ordinal)) { throw "Published PublisherStudio assembly identity mismatch for $Rid ${mode}: expected $appVersion, found $publishedSemanticVersion. Refusing to package a stale runtime." }
        $utf8NoBom = New-Object Text.UTF8Encoding($false)
        [IO.File]::WriteAllText((Join-Path $publishFolder 'RELEASE-VERSION.txt'), "$appVersion`n", $utf8NoBom)
        [IO.File]::WriteAllText((Join-Path $publishFolder 'SOURCE-SHA256.txt'), "$($script:releaseSourceFingerprint)`n", $utf8NoBom)
        Write-Host "Stamped $Rid PublisherStudio runtime payload with version $appVersion and source fingerprint $($script:releaseSourceFingerprint)." -ForegroundColor DarkCyan
        $publishedDocumentationRoot = Join-Path $publishFolder 'wwwroot/help-docs'
        Copy-PublisherStudioRuntimeDocumentation -SourceRoot $script:documentationCacheRoot -DestinationRoot $publishedDocumentationRoot -Version $appVersion
        Assert-PublishedConfigurationFiles -SourceRoot $webDirectory -PublishRoot $publishFolder
        Assert-PublisherStudioDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion
        $protocolDirectory = Join-Path $publishFolder 'protocol'; New-Item -ItemType Directory -Path $protocolDirectory -Force | Out-Null
        Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $protocolDirectory $wireProtocolPackageName) -Force
        $publisherIcon = Join-Path $root 'assets/PublisherStudio.ico'; if (Test-Path -LiteralPath $publisherIcon -PathType Leaf) { Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $publishFolder 'PublisherStudio.ico') -Force }
        $nativeArtifacts = & $script:nativeReleasePackagingScript -ProductName 'PublisherStudio' -ExecutableName $appExecutable -Version $appVersion -Rid $Rid -Mode $mode -PayloadDirectory $publishFolder -OutputDirectory $artifacts -PackagingTool $script:releasePackagingTool -DependencyPolicy PublisherStudio -UseContainerFallback:$UseContainerPackaging -ProvisionHomebrewTools:$ProvisionNativePackagingTools -RequireOptionalPackages:$RequireOptionalNativePackages -ForceRebuildArtifacts:$ForceRebuildArtifacts -MacIconSource (Join-Path $root 'assets/PublisherStudio.png') -DmgBackgroundPath (Join-Path $root 'build/assets/PublisherStudio-dmg-background.png')
        foreach ($artifact in @($nativeArtifacts)) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $script:releaseZipPaths.Add([string]$artifact) } }
        # Native artifacts are now complete; do not keep another multi-gigabyte documentation-bearing RID tree alive.
        Remove-Item -LiteralPath $publishFolder -Recurse -Force -ErrorAction SilentlyContinue
        $transientMacApp = Join-Path $artifacts 'PublisherStudio.app'
        if ($Rid.StartsWith('osx-')) { Remove-Item -LiteralPath $transientMacApp -Recurse -Force -ErrorAction SilentlyContinue }
        Write-Host "Released transient $Rid $mode staging workspace after native package validation." -ForegroundColor DarkCyan
}

function Publish-Runtime {
    param([Parameter(Mandatory)][string]$Rid)

    if (-not $Rid.StartsWith('win-')) { Publish-UnixRuntime -Rid $Rid; return }

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
    $publishedDocumentationRoot = Join-Path $appFolder "wwwroot/help-docs"
    Copy-PublisherStudioRuntimeDocumentation -SourceRoot $script:documentationCacheRoot -DestinationRoot $publishedDocumentationRoot -Version $appVersion
    Write-Host "Reused the verified HTML documentation and compressed embedded PDF payload for $Rid." -ForegroundColor Cyan

    Assert-PublishedConfigurationFiles -SourceRoot $webDirectory -PublishRoot $appFolder
    Assert-PublisherStudioDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion

    $protocolAppDirectory = Join-Path $appFolder "protocol"
    $protocolSetupDirectory = Join-Path $setupFolder "protocol"
    New-Item -ItemType Directory -Path $protocolAppDirectory, $protocolSetupDirectory -Force | Out-Null
    Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $protocolAppDirectory $wireProtocolPackageName) -Force
    Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $protocolSetupDirectory $wireProtocolPackageName) -Force

    $publisherIcon = Join-Path $root "assets/PublisherStudio.ico"
    if (-not (Test-Path -LiteralPath $publisherIcon -PathType Leaf)) { throw "PublisherStudio release icon is unavailable: $publisherIcon" }
    Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $setupFolder "PublisherStudio.ico") -Force
    Copy-Item -LiteralPath $publisherIcon -Destination (Join-Path $appFolder "PublisherStudio.ico") -Force

    $utf8NoBom = New-Object Text.UTF8Encoding($false)
    foreach ($releasePayloadFolder in @($appFolder, $setupFolder)) {
        [IO.File]::WriteAllText((Join-Path $releasePayloadFolder 'RELEASE-VERSION.txt'), "$appVersion`n", $utf8NoBom)
        [IO.File]::WriteAllText((Join-Path $releasePayloadFolder 'SOURCE-SHA256.txt'), "$($script:releaseSourceFingerprint)`n", $utf8NoBom)
    }
    Write-Host "Stamped Windows PublisherStudio runtime and setup payloads with version $appVersion and source fingerprint $($script:releaseSourceFingerprint)." -ForegroundColor DarkCyan

    $requiredSetupFiles = @("Install.cmd", "Update.cmd", "Start.cmd", "PublisherStudio.ico", "RELEASE-VERSION.txt", "SOURCE-SHA256.txt")
    $missingSetupFiles = @($requiredSetupFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $setupFolder $_) -PathType Leaf) })
    if ($missingSetupFiles.Count -gt 0) { throw "Published setup is incomplete. Missing: $($missingSetupFiles -join ', ')" }

    # Final release-boundary check: no later publish step may reintroduce stale help-docs.
    Assert-PublisherStudioDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion
    New-PublisherStudioReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip -RootFolderName $profile.AppFolder -WriteUnixPermissions:(!$Rid.StartsWith("win-")) -UnixExecutableRelativePaths @($appExecutable)
    New-PublisherStudioReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip -RootFolderName $profile.SetupFolder -WriteUnixPermissions:(!$Rid.StartsWith("win-")) -UnixExecutableRelativePaths @($setupExecutable)
    Assert-ReleaseArchiveLayout -ArchivePath $appZip -RootFolderName $profile.AppFolder -Executable $appExecutable -Version $appVersion -RequireDocumentation
    Assert-ReleaseArchiveLayout -ArchivePath $setupZip -RootFolderName $profile.SetupFolder -Executable $setupExecutable -Version $appVersion
    $script:releaseZipPaths.Add($appZip)
    $script:releaseZipPaths.Add($setupZip)
    Write-Host "Created portable ZIP $appZip" -ForegroundColor Green
    Write-Host "Created portable ZIP $setupZip" -ForegroundColor Green
}

$releaseHost = Get-ReleaseHostFamily
$runtimes = if ($Runtime -eq "all") {
    @(Get-HostDefaultRuntimes)
} elseif ($Runtime -eq "all-rids") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
} else {
    @($Runtime)
}

$wslLinuxRuntimes = @()
$wslResolvedDistribution = ''
$wslWasRunningBeforeProbe = $false
$wslEffectiveShutdown = $WslShutdown
$wslExecutable = $null
if ($releaseHost -eq 'Windows' -and -not $WslChildBuild -and $WslLinux -ne 'Off') {
    $wslCandidates = if ($Runtime -eq 'all') {
        @('linux-x64','linux-arm64')
    } else {
        @($runtimes | Where-Object { $_ -in @('linux-x64','linux-arm64') })
    }

    if ($wslCandidates.Count -gt 0) {
        $wslExecutable = Get-WslReleaseExecutable
        if (-not [string]::IsNullOrWhiteSpace($wslExecutable)) {
            $wslResolvedDistribution = Resolve-WslReleaseDistribution -WslExecutable $wslExecutable -RequestedDistribution $WslDistribution
        }

        if (-not [string]::IsNullOrWhiteSpace($wslResolvedDistribution)) {
            $runningBeforeProbe = @(Get-WslReleaseRunningDistributions -WslExecutable $wslExecutable)
            $wslWasRunningBeforeProbe = @($runningBeforeProbe | Where-Object { [string]::Equals($_, $wslResolvedDistribution, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
            $wslStatus = Get-WslReleaseBuildStatus -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution
            if ((-not $wslStatus.CoreReady) -and $ProvisionWslBuildTools) {
                Write-Host "Provisioning the existing WSL distribution '$wslResolvedDistribution' because -ProvisionWslBuildTools was requested..." -ForegroundColor Cyan
                & (Join-Path $root 'Setup-WslLinuxBuild.ps1') -Distribution $wslResolvedDistribution -Provision -Shutdown Never
                $wslStatus = Get-WslReleaseBuildStatus -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution
            }
            $wslLicenseReady = $wslStatus.CoreReady -and (Test-WslReleaseDevExpressLicenseAvailable -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution -Status $wslStatus)
            if ($wslStatus.CoreReady -and $wslLicenseReady) {
                $wslLinuxRuntimes = @($wslCandidates)
                $runtimes = @($runtimes | Where-Object { $_ -notin $wslLinuxRuntimes })
                if ($WslShutdown -eq 'IfStarted') { $wslEffectiveShutdown = if ($wslWasRunningBeforeProbe) { 'Never' } else { 'Always' } }
                Write-Host "Ready WSL Linux backend '$wslResolvedDistribution' will build: $($wslLinuxRuntimes -join ', ')." -ForegroundColor Cyan
            }
            else {
                $reason = if (-not $wslStatus.CoreReady) { Get-WslReleaseReadinessMessage $wslStatus } else { 'DevExpress build license is not available through the WSL profile or Windows license bridge.' }
                if (-not $wslWasRunningBeforeProbe) { & $wslExecutable --terminate $wslResolvedDistribution 2>$null | Out-Null }
                if ($WslLinux -eq 'Require') { throw "WSL Linux release was required, but '$wslResolvedDistribution' is not ready: $reason" }
                Write-Host "WSL Linux release backend not used: $reason" -ForegroundColor DarkCyan
                if ($Runtime -eq 'all') { Write-Host 'Continuing with the normal Windows release only. Run .\\Setup-WslLinuxBuild.ps1 -Provision to enable automatic Linux packaging.' -ForegroundColor DarkCyan }
                else { Write-Host 'Explicit Linux RIDs remain in the local runtime list, so the existing cross-publish path is preserved.' -ForegroundColor DarkCyan }
            }
        }
        else {
            if ($WslLinux -eq 'Require') { throw 'WSL Linux release was required, but no usable WSL distribution is installed and initialized.' }
            if ($Runtime -eq 'all') { Write-Host 'No ready WSL distribution was found; continuing with the normal Windows release only.' -ForegroundColor DarkCyan }
            else { Write-Host 'No ready WSL distribution was found; explicit Linux RIDs will use the existing Windows cross-publish path.' -ForegroundColor DarkCyan }
        }
    }
}

$displayRuntimes = @($runtimes) + @($wslLinuxRuntimes | ForEach-Object { "$_ (WSL)" })
Write-Host "Release host $releaseHost selected runtime(s): $($displayRuntimes -join ', ')" -ForegroundColor Cyan
if ($Runtime -eq 'all') {
    Write-Host "Runtime 'all' is host-aware. Use -Runtime all-rids only for an explicit cross-host publish attempt." -ForegroundColor DarkCyan
    if ($releaseHost -eq 'Windows' -and $wslLinuxRuntimes.Count -gt 0) {
        Write-Host 'Windows is the release coordinator: Windows packages are native; Linux self-contained Full packages are delegated headlessly to WSL and imported into the same release bundle.' -ForegroundColor DarkCyan
    }
    if ($releaseHost -eq 'macOS') {
        Write-Host "macOS is the full release coordinator: macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup payloads are built in one run." -ForegroundColor DarkCyan
        Write-Host "macOS produces native self-contained Full DMG/PKG/TAR.GZ packages; Linux self-contained Full TAR.GZ/DEB are managed and RPM uses Homebrew rpmbuild when available. AppImage remains a Linux/WSL/container finishing step." -ForegroundColor DarkCyan
    }
}
& (Join-Path $root 'build/Initialize-MacReleaseTrust.ps1') -ProductName 'PublisherStudio' -SelectedRuntimes @($runtimes) -AllowUnsignedMacPackages:$AllowUnsignedMacPackages
$requiresReleasePackaging = @($runtimes | Where-Object { -not $_.StartsWith('win-') }).Count -gt 0

New-Item -ItemType Directory -Path $packageDirectory, $artifacts -Force | Out-Null
Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
Ensure-WireProtocolPackage

if ($wslLinuxRuntimes.Count -gt 0) {
    try {
        $wslPackageArguments = @{
            Version = $ReleasePackagingVersion
            Configuration = $Configuration
            PackageDirectory = $packageDirectory
            PackageUrl = $ReleasePackagingPackageUrl
            LocalGptRepository = $LocalGptRepository
            PackageOnly = $true
        }
        if ($RefreshReleasePackagingPackage) { $wslPackageArguments.ForceDownload = $true }
        $wslPackageOutput = @(& (Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1') @wslPackageArguments)
        if ($wslPackageOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$wslPackageOutput[0])) { throw "WSL release-packaging package preparation returned $($wslPackageOutput.Count) pipeline value(s); expected exactly one package path." }
        $releasePackagingPackage = [string]$wslPackageOutput[0]
        if (-not (Test-Path -LiteralPath $releasePackagingPackage -PathType Leaf)) { throw "WSL release-packaging package is missing: $releasePackagingPackage" }
        Write-Host "Prepared LocalGPT.ReleasePackaging $ReleasePackagingVersion for the WSL Linux child without installing the tool on Windows." -ForegroundColor DarkCyan
    }
    catch {
        if ($WslLinux -eq 'Require') { throw }
        Write-Warning "The ready WSL backend could not be supplied with LocalGPT.ReleasePackaging: $($_.Exception.Message)"
        if ($Runtime -ne 'all') { $runtimes = @($runtimes + $wslLinuxRuntimes | Select-Object -Unique) }
        $wslLinuxRuntimes = @()
        if (-not $wslWasRunningBeforeProbe -and -not [string]::IsNullOrWhiteSpace($wslResolvedDistribution)) { & $wslExecutable --terminate $wslResolvedDistribution 2>$null | Out-Null }
        Write-Host "Continuing without WSL. Explicit Linux requests retain the existing Windows cross-publish path; host-aware 'all' continues with Windows only." -ForegroundColor DarkCyan
    }
}
$requiresReleasePackaging = @($runtimes | Where-Object { -not $_.StartsWith('win-') }).Count -gt 0

# Documentation PDF assembly uses LocalGPT.ReleasePackaging on every host, not only Unix packaging lanes.
$releasePackagingEnsureArguments = @{
    Version = $ReleasePackagingVersion
    Configuration = $Configuration
    PackageDirectory = $packageDirectory
    PackageUrl = $ReleasePackagingPackageUrl
    LocalGptRepository = $LocalGptRepository
}
if ($RefreshReleasePackagingPackage) { $releasePackagingEnsureArguments.ForceDownload = $true }
$releasePackagingToolOutput = @(& (Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1') @releasePackagingEnsureArguments)
if ($releasePackagingToolOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$releasePackagingToolOutput[0])) { throw "Release-packaging tool preparation returned $($releasePackagingToolOutput.Count) pipeline value(s); expected exactly one executable path." }
$releasePackagingTool = [string]$releasePackagingToolOutput[0]
if (-not (Test-Path -LiteralPath $releasePackagingTool -PathType Leaf)) { throw "Prepared release-packaging tool is missing: $releasePackagingTool" }
if (-not (Test-Path -LiteralPath $releasePackagingPackage -PathType Leaf)) { throw "LocalGPT release-packaging package preparation did not produce $releasePackagingPackage" }
Copy-Item -LiteralPath $releasePackagingPackage -Destination (Join-Path $artifacts $releasePackagingPackageName) -Force

Copy-Item -LiteralPath $wireProtocolPackage -Destination (Join-Path $artifacts $wireProtocolPackageName) -Force
Prepare-PublisherStudioClientAssets
Prepare-PublisherStudioDocumentation

try {
    foreach ($rid in $runtimes) { Publish-Runtime -Rid $rid }

    if ($wslLinuxRuntimes.Count -gt 0) {
        $wslArtifacts = @(& (Join-Path $root 'build/Invoke-WslLinuxRelease.ps1') `
            -ProductName PublisherStudio `
            -RepositoryRoot $root `
            -OutputDirectory $artifacts `
            -PreparedDocumentationRoot $documentationCacheRoot `
            -Version $appVersion `
            -Runtimes $wslLinuxRuntimes `
            -Configuration $Configuration `
            -Distribution $wslResolvedDistribution `
            -ReleasePackagingPackagePath $releasePackagingPackage `
            -UseContainerPackaging:$UseContainerPackaging `
            -RequireOptionalNativePackages:$RequireOptionalNativePackages `
            -KeepBuildTree:$KeepWslBuildTree `
            -Shutdown $wslEffectiveShutdown)
        foreach ($artifact in $wslArtifacts) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $script:releaseZipPaths.Add([string]$artifact) } }
    }

    $documentationPdf = Join-Path $documentationCacheRoot "PublisherStudio-$appVersion.pdf"
    $winX64Profile = Resolve-ReleaseProfile -Rid "win-x64"
    $winX64SetupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $winX64Profile.SetupProfile
    $winX64SetupExecutable = Join-Path $winX64SetupFolder "PublisherStudio.Setup.exe"
    $requireWinX64Setup = @($runtimes) -contains "win-x64"
    $licensePath = Join-Path $root "LICENSE.MD"
    if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) { $licensePath = Join-Path $root "LICENSE" }

    if (-not $SkipReleaseBundle) {
        Complete-ReleaseBundle `
            -Version $appVersion `
            -ReleaseZipPaths @($releaseZipPaths) `
            -DocumentationPdfPath $documentationPdf `
            -WindowsX64SetupExecutablePath $winX64SetupExecutable `
            -ReadmePath (Join-Path $root "README.md") `
            -LicensePath $licensePath `
            -WireProtocolPackagePath $wireProtocolPackage `
            -SetupIconPath (Join-Path $root "assets/PublisherStudio.ico") `
            -RequireWindowsX64Setup $requireWinX64Setup
    }
    else {
        Write-Host 'Skipping the upload-ready version bundle because this is a delegated Linux release child.' -ForegroundColor DarkCyan
    }
}
finally {
    Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $artifacts 'staging') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $artifacts 'PublisherStudio.app') -Recurse -Force -ErrorAction SilentlyContinue
    $postReleaseBuildStateCount = Clear-RepositoryReleaseBuildState -BestEffort
    if ($postReleaseBuildStateCount -gt 0) {
        Write-Host "Released $postReleaseBuildStateCount repository-local bin/obj build-state director$(if ($postReleaseBuildStateCount -eq 1) { 'y' } else { 'ies' }) after the release attempt." -ForegroundColor DarkCyan
    }
}

$releaseBundle = if ($SkipReleaseBundle) { $artifacts } else { Join-Path $artifacts $appVersion }
Write-Host "Release output: $releaseBundle" -ForegroundColor Green
Write-Host "Protocol package cache: $(Join-Path $artifacts $wireProtocolPackageName)" -ForegroundColor Green
