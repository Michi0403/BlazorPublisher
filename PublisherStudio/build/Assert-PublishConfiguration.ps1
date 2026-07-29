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

function Assert-Profile([string]$RelativePath, [string]$Runtime, [string]$Folder, [string]$Platform) {
    $properties = Read-ProfileProperties $RelativePath
    $output = "..\..\artifacts\release\$Folder\"
    foreach ($requirement in @(
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = 'false' },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'PublishReadyToRun'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'PublishProtocol'; Value = 'FileSystem' },
        @{ Name = 'Platform'; Value = $Platform },
        @{ Name = 'TargetFramework'; Value = 'net10.0' },
        @{ Name = 'PublishUrl'; Value = $output },
        @{ Name = 'PublishDir'; Value = $output }
    )) {
        Assert-Property $properties $requirement.Name $requirement.Value $RelativePath
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
    Assert-Profile "src\PublisherStudio.Web\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.App 'AnyCPU'
    Assert-Profile "src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Setup 'Any CPU'
}

# Long-name Linux aliases remain supported for existing developer workflows.
Assert-Profile 'src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\linuxx64.pubxml' 'linux-x64' 'setuplinx64' 'Any CPU'
Assert-Profile 'src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\linuxarm64.pubxml' 'linux-arm64' 'setuplinarm64' 'Any CPU'

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
foreach ($project in @($webProject, $setupProject)) {
    foreach ($marker in @('<PublishSingleFile>false</PublishSingleFile>', '<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>')) {
        if (-not $project.Contains($marker)) { Fail "A publish project is missing $marker." }
    }
}
foreach ($profile in $profiles) {
    foreach ($fragment in @(
        '"' + $profile.Runtime + '"',
        'AppFolder = "' + $profile.App + '"',
        'SetupFolder = "' + $profile.Setup + '"',
        'SetupAsset = "' + $profile.Setup + '"'
    )) {
        if (-not $release.Contains($fragment)) { Fail "Build-Release.ps1 is missing synchronized mapping: $fragment" }
    }
    if (-not $allRuntimes.Contains('"' + $profile.Runtime + '"')) { Fail "Build-AllRuntimes.ps1 is missing runtime $($profile.Runtime)." }
}
if (-not $webProject.Contains('win-x64;win-x86;win-arm64;linux-x64;linux-arm64;osx-x64;osx-arm64')) {
    Fail 'PublisherStudio.Web.csproj must expose the same seven runtime identifiers as both publish lanes.'
}
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true') {
    Fail 'The scripted release lane must remain multi-file and self-contained like the developer profiles.'
}

Write-Host 'Publish configuration validation passed for 7 application profiles, 9 installer/developer profiles and the synchronized scripted release lane.'
