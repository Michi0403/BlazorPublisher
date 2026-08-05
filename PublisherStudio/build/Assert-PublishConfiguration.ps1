[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) { throw "Publish configuration validation failed: $Message" }

function Read-Text([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required file is missing: $RelativePath" }
    return [IO.File]::ReadAllText($path)
}

function Read-ProfileProperties([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Publish profile is missing: $RelativePath" }
    try { [xml]$document = [IO.File]::ReadAllText($path) }
    catch { Fail "Publish profile is not valid XML: $RelativePath. $($_.Exception.Message)" }

    $properties = @{}
    foreach ($group in @($document.Project.PropertyGroup)) {
        foreach ($node in @($group.ChildNodes)) {
            if ($node.NodeType -eq [Xml.XmlNodeType]::Element) { $properties[$node.Name] = [string]$node.InnerText }
        }
    }
    return $properties
}

function Assert-Property([hashtable]$Properties, [string]$Name, [string]$Expected, [string]$RelativePath) {
    if (-not $Properties.ContainsKey($Name)) { Fail "$RelativePath does not define $Name." }
    if (-not [string]::Equals($Properties[$Name], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        Fail "$RelativePath defines $Name='$($Properties[$Name])'; expected '$Expected'."
    }
}

function Assert-Profile([string]$RelativePath, [string]$Runtime, [string]$Folder, [string]$Platform, [bool]$SingleFile) {
    $properties = Read-ProfileProperties $RelativePath
    $output = "..\..\artifacts\release\$Folder\"
    foreach ($requirement in @(
        @{ Name = 'Configuration'; Value = 'Release' },
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = $(if ($SingleFile) { 'true' } else { 'false' }) },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'PublishProtocol'; Value = 'FileSystem' },
        @{ Name = 'Platform'; Value = $Platform },
        @{ Name = 'TargetFramework'; Value = 'net10.0' }
    )) {
        Assert-Property $properties $requirement.Name $requirement.Value $RelativePath
    }
    if ($properties.ContainsKey('PublishReadyToRun')) {
        Assert-Property $properties 'PublishReadyToRun' 'false' $RelativePath
    }
    $declaredOutput = @('PublishDir', 'PublishUrl') | Where-Object { $properties.ContainsKey($_) }
    if ($declaredOutput.Count -eq 0) {
        Fail "$RelativePath must define PublishDir or PublishUrl so release scripts can consume profile-owned output."
    }
    foreach ($outputProperty in $declaredOutput) {
        Assert-Property $properties $outputProperty $output $RelativePath
    }
    if ($SingleFile) {
        Assert-Property $properties 'IncludeNativeLibrariesForSelfExtract' 'true' $RelativePath
        Assert-Property $properties 'EnableCompressionInSingleFile' 'true' $RelativePath
    }
}


$profiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; App = 'winx64'; Setup = 'setupwinx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; App = 'winx86'; Setup = 'setupwinx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; App = 'winarm64'; Setup = 'setupwinarm64' },
    @{ File = 'linx64.pubxml'; Runtime = 'linux-x64'; App = 'linx64'; Setup = 'setuplinx64' },
    @{ File = 'linarm64.pubxml'; Runtime = 'linux-arm64'; App = 'linarm64'; Setup = 'setuplinarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; App = 'macosx64'; Setup = 'setupmacosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; App = 'macosarm64'; Setup = 'setupmacosarm64' }
)

foreach ($profile in $profiles) {
    Assert-Profile "src\PublisherStudio.Web\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.App 'AnyCPU' $false
    Assert-Profile "src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Setup 'Any CPU' $true
}

$profileUserFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)
if ($profileUserFiles.Count -gt 0) { Fail 'Machine-specific .pubxml.user files must not be shipped in the source package.' }

$migration = Read-Text 'build\Migrate-ObsoletePublishConfiguration.ps1'
if ($migration.Contains('Remove-ObsoleteProfileRoot') -or $migration.Contains(".Extension -eq '.pubxml'")) {
    Fail 'The migration script must preserve developer .pubxml profiles.'
}
if (-not $migration.Contains('*.pubxml.user')) { Fail 'The migration script must still clean machine-specific .pubxml.user overlays.' }

$webProject = Read-Text 'src\PublisherStudio.Web\PublisherStudio.Web.csproj'
$setupProject = Read-Text 'src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj'
$release = Read-Text 'Build-Release.ps1'
$allRuntimes = Read-Text 'Build-AllRuntimes.ps1'
foreach ($marker in @('<PublishSingleFile>false</PublishSingleFile>', '<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>')) {
    if (-not $webProject.Contains($marker)) { Fail "The PublisherStudio application project is missing $marker." }
}
foreach ($marker in @('<PublishSingleFile Condition="''$(RuntimeIdentifier)'' != ''''">true</PublishSingleFile>', '<IncludeNativeLibrariesForSelfExtract Condition="''$(RuntimeIdentifier)'' != ''''">true</IncludeNativeLibrariesForSelfExtract>', '<EnableCompressionInSingleFile Condition="''$(RuntimeIdentifier)'' != ''''">true</EnableCompressionInSingleFile>', '<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>')) {
    if (-not $setupProject.Contains($marker)) { Fail "The PublisherStudio setup project is missing $marker." }
}
foreach ($profile in $profiles) {
    $runtimeLiteral = [Regex]::Escape('"' + $profile.Runtime + '"')
    $mappingMatch = [Regex]::Match(
        $release,
        "(?s)$runtimeLiteral\s*\{\s*(?:return\s+)?@\{(?<Body>.*?)\}\s*\}",
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $mappingMatch.Success) {
        Fail "Build-Release.ps1 is missing the release mapping block for $($profile.Runtime)."
    }

    $mappingBody = $mappingMatch.Groups['Body'].Value
    foreach ($property in @(
        @{ Name = 'AppAsset'; Value = $profile.App + '.zip' },
        @{ Name = 'SetupAsset'; Value = $profile.Setup + '.zip' },
        @{ Name = 'AppProfile'; Value = [IO.Path]::GetFileNameWithoutExtension($profile.File) },
        @{ Name = 'SetupProfile'; Value = [IO.Path]::GetFileNameWithoutExtension($profile.File) },
        @{ Name = 'AppFolder'; Value = $profile.App },
        @{ Name = 'SetupFolder'; Value = $profile.Setup }
    )) {
        $propertyPattern = '(?m)\b' + [Regex]::Escape($property.Name) + '\s*=\s*"' + [Regex]::Escape($property.Value) + '"'
        if (-not [Regex]::IsMatch($mappingBody, $propertyPattern, [Text.RegularExpressions.RegexOptions]::CultureInvariant)) {
            Fail "Build-Release.ps1 maps $($profile.Runtime) without $($property.Name)='$($property.Value)'."
        }
    }
}
if (-not $allRuntimes.Contains('Runtime = "all"')) {
    Fail 'Build-AllRuntimes.ps1 must delegate to the shared Build-Release.ps1 all-runtime lane.'
}
if (-not $release.Contains('[string]$Runtime = "all"')) {
    Fail 'Build-Release.ps1 must use the same all-runtime entry point as LocalGPT.'
}
if ($release -match '\.Contains\([^\r\n,]+,\s*\[(?:System\.)?StringComparison\]::') {
    Fail 'Build-Release.ps1 must remain compatible with Windows PowerShell 5.1; use String.IndexOf for comparison-aware substring checks.'
}
if (-not $webProject.Contains('win-x64;win-x86;win-arm64;linux-x64;linux-arm64;osx-x64;osx-arm64')) {
    Fail 'PublisherStudio.Web.csproj must expose the same seven runtime identifiers as both publish lanes.'
}
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true') {
    Fail 'Build-Release.ps1 must consume the reviewed profiles instead of overriding their application/setup packaging policies.'
}
foreach ($required in @(
    'function New-PublisherStudioReleaseArchive',
    'New-PublisherStudioReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip',
    'New-PublisherStudioReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip',
    'Assert-ReleaseArchiveLayout -ArchivePath $appZip',
    'Assert-ReleaseArchiveLayout -ArchivePath $setupZip',
    '$entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)'
)) {
    if (-not $release.Contains($required)) { Fail "Build-Release.ps1 is missing the verified archive contract: $required" }
}
if ($release.Contains('Compress-Archive')) {
    Fail 'Build-Release.ps1 may not use Compress-Archive for release payloads; the verified retrying ZIP writer is required.'
}
foreach ($forbidden in @(
    'Write-ReleaseManifest',
    'Write-BootstrapRepairManifest',
    'New-ReleaseArchive',
    'PublisherStudio.Setup.repair.exe',
    'publisherstudio-bootstrap-repair.json'
)) {
    if ($release.Contains($forbidden)) { Fail "Build-Release.ps1 still contains the superseded custom deployment contract: $forbidden" }
}

Write-Host 'Publish configuration validation passed for 7 multi-file application profiles, 7 standalone setup profiles and the LocalGPT-shaped shared release lane.'
