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

function Assert-OptionalProperty([hashtable]$Properties, [string]$Name, [string]$Expected, [string]$RelativePath) {
    if ($Properties.ContainsKey($Name) -and -not [string]::Equals($Properties[$Name], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        Fail "$RelativePath defines $Name='$($Properties[$Name])'; expected '$Expected' when present."
    }
}

function Assert-OnePropertyEquals(
    [hashtable]$Properties,
    [string[]]$Names,
    [string]$Expected,
    [string]$RelativePath,
    [string]$Description
) {
    foreach ($name in $Names) {
        if ($Properties.ContainsKey($name) -and [string]::Equals($Properties[$name], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }
    Fail "$RelativePath does not define a supported $Description value of '$Expected' in any of: $($Names -join ', ')."
}

function Assert-CommonProfile(
    [hashtable]$Properties,
    [string]$RelativePath,
    [string]$Runtime,
    [string]$Folder
) {
    $output = "..\..\artifacts\release\$Folder\"
    foreach ($requirement in @(
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'TargetFramework'; Value = 'net10.0' },
        @{ Name = 'PublishUrl'; Value = $output }
    )) {
        Assert-Property $Properties $requirement.Name $requirement.Value $RelativePath
    }
    Assert-OptionalProperty $Properties 'PublishDir' $output $RelativePath
    Assert-OptionalProperty $Properties 'PublishReadyToRun' 'false' $RelativePath
}

function Assert-WebProfile([string]$RelativePath, [string]$Runtime, [string]$Folder) {
    $properties = Read-ProfileProperties $RelativePath
    Assert-CommonProfile $properties $RelativePath $Runtime $Folder
    Assert-Property $properties 'PublishSingleFile' 'false' $RelativePath
    Assert-OnePropertyEquals $properties @('PublishProtocol', 'WebPublishMethod', 'PublishProvider') 'FileSystem' $RelativePath 'file-system publish method'
    Assert-OnePropertyEquals $properties @('Platform', 'LastUsedPlatform') 'Any CPU' $RelativePath 'platform'
}

function Assert-SetupProfile([string]$RelativePath, [string]$Runtime, [string]$Folder) {
    $properties = Read-ProfileProperties $RelativePath
    Assert-CommonProfile $properties $RelativePath $Runtime $Folder
    Assert-Property $properties 'PublishSingleFile' 'true' $RelativePath
    Assert-Property $properties 'PublishDir' "..\..\artifacts\release\$Folder\" $RelativePath
    Assert-Property $properties 'PublishProtocol' 'FileSystem' $RelativePath
    Assert-Property $properties 'Platform' 'Any CPU' $RelativePath
}

function Assert-ProfileInventory([string]$RelativeDirectory, [string[]]$ExpectedNames) {
    $directory = Join-Path $root $RelativeDirectory
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) { Fail "Publish profile directory is missing: $RelativeDirectory" }
    $actualNames = @(Get-ChildItem -LiteralPath $directory -File -Filter '*.pubxml' | ForEach-Object { $_.Name } | Sort-Object)
    $expectedSorted = @($ExpectedNames | Sort-Object)
    $difference = @(Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actualNames)
    if ($difference.Count -gt 0) {
        Fail "Unexpected publish-profile inventory in $RelativeDirectory. Expected: $($expectedSorted -join ', '); actual: $($actualNames -join ', ')."
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

$expectedProfileNames = @($profiles | ForEach-Object { $_.File })
Assert-ProfileInventory 'src\PublisherStudio.Web\Properties\PublishProfiles' $expectedProfileNames
Assert-ProfileInventory 'src\PublisherStudio.InstallerConsole\Properties\PublishProfiles' $expectedProfileNames

foreach ($profile in $profiles) {
    Assert-WebProfile "src\PublisherStudio.Web\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.App
    Assert-SetupProfile "src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Setup
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
foreach ($project in @($webProject, $setupProject)) {
    foreach ($marker in @('<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>')) {
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
    Fail 'The scripted release lane must remain multi-file and self-contained.'
}

Write-Host 'Publish configuration validation passed for 7 application profiles, 7 installer profiles and the scripted release lane.'
