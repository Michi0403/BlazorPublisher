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

$webProjectPath = 'src\PublisherStudio.Web\PublisherStudio.Web.csproj'
$setupProjectPath = 'src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj'
$webProject = Read-Text $webProjectPath
$setupProject = Read-Text $setupProjectPath
$release = Read-Text 'Build-Release.ps1'
$allRuntimes = Read-Text 'Build-AllRuntimes.ps1'

foreach ($project in @(
    @{ Name = $webProjectPath; Text = $webProject },
    @{ Name = $setupProjectPath; Text = $setupProject }
)) {
    if ($project.Text -notmatch '<SelfContained\s+Condition="''\$\(RuntimeIdentifier\)'' != ''''">true</SelfContained>') { Fail "$($project.Name) must default RID-based publishes to self-contained output without changing ordinary RID-less builds." }
    if ($project.Text -notmatch '<PublishSingleFile>false</PublishSingleFile>') { Fail "$($project.Name) must disable single-file publishing." }
    if ($project.Text -notmatch '<PublishTrimmed>false</PublishTrimmed>') { Fail "$($project.Name) must disable trimming." }
    if ($project.Text -notmatch '<PublishReadyToRun>false</PublishReadyToRun>') { Fail "$($project.Name) must disable ReadyToRun publishing." }
}

$configurationMarkers = @(
    '<Content Update="appsettings\*\.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />',
    '<Content Update="Localization\\\*\*\\\*\.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />',
    '<Content Update="Configuration\\\*\*\\\*" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />',
    '<None Update="Configuration\\\*\*\\\*" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />',
    '<PublisherStudioConfigurationFile Include="appsettings\*\.json;Configuration\\\*\*\\\*;Localization\\\*\*\\\*\.json" />',
    'ValidatePublisherConfigurationFilesForPublish'
)
foreach ($marker in $configurationMarkers) {
    if ($webProject -notmatch $marker) { Fail "PublisherStudio.Web.csproj does not preserve the complete reviewed configuration payload marker: $marker" }
}

$expectedWebProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'winx64' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'winarm64' },
    @{ File = 'linx64.pubxml'; Runtime = 'linux-x64'; Folder = 'linx64' },
    @{ File = 'linarm64.pubxml'; Runtime = 'linux-arm64'; Folder = 'linarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; Folder = 'macosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; Folder = 'macosarm64' }
)
$expectedSetupProfiles = @(
    @{ File = 'FolderProfile.pubxml'; Runtime = 'win-x64'; Folder = 'setupwin-x64' },
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'setupwin-x64' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'setupwin-arm64' },
    @{ File = 'linuxx64.pubxml'; Runtime = 'linux-x64'; Folder = 'setuplin-x64' },
    @{ File = 'linuxarm64.pubxml'; Runtime = 'linux-arm64'; Folder = 'setuplin-arm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; Folder = 'setupmacos-x64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; Folder = 'setupmacos-arm64' }
)

$webProfileRoot = Join-Path $root 'src\PublisherStudio.Web\Properties\PublishProfiles'
$setupProfileRoot = Join-Path $root 'src\PublisherStudio.InstallerConsole\Properties\PublishProfiles'
$actualWebProfiles = @(Get-ChildItem -LiteralPath $webProfileRoot -File -Filter '*.pubxml' | Select-Object -ExpandProperty Name | Sort-Object)
$actualSetupProfiles = @(Get-ChildItem -LiteralPath $setupProfileRoot -File -Filter '*.pubxml' | Select-Object -ExpandProperty Name | Sort-Object)
$expectedWebNames = @($expectedWebProfiles | ForEach-Object { $_.File } | Sort-Object)
$expectedSetupNames = @($expectedSetupProfiles | ForEach-Object { $_.File } | Sort-Object)
if (($actualWebProfiles -join '|') -ne ($expectedWebNames -join '|')) { Fail "Unexpected PublisherStudio.Web publish-profile inventory: $($actualWebProfiles -join ', ')" }
if (($actualSetupProfiles -join '|') -ne ($expectedSetupNames -join '|')) { Fail "Unexpected installer publish-profile inventory: $($actualSetupProfiles -join ', ')" }

foreach ($profile in $expectedWebProfiles) {
    $relative = "src\PublisherStudio.Web\Properties\PublishProfiles\$($profile.File)"
    $properties = Read-ProfileProperties $relative
    Assert-Property $properties 'RuntimeIdentifier' $profile.Runtime $relative
    Assert-Property $properties 'SelfContained' 'true' $relative
    Assert-Property $properties 'PublishSingleFile' 'false' $relative
    Assert-Property $properties 'PublishTrimmed' 'false' $relative
    Assert-Property $properties 'PublishReadyToRun' 'false' $relative
    Assert-Property $properties 'DeleteExistingFiles' 'true' $relative
    Assert-Property $properties 'PublishUrl' "..\..\artifacts\release\$($profile.Folder)\" $relative
}

foreach ($profile in $expectedSetupProfiles) {
    $relative = "src\PublisherStudio.InstallerConsole\Properties\PublishProfiles\$($profile.File)"
    $properties = Read-ProfileProperties $relative
    Assert-Property $properties 'RuntimeIdentifier' $profile.Runtime $relative
    Assert-Property $properties 'SelfContained' 'true' $relative
    Assert-Property $properties 'PublishSingleFile' 'false' $relative
    Assert-Property $properties 'PublishTrimmed' 'false' $relative
    Assert-Property $properties 'PublishReadyToRun' 'false' $relative
    Assert-Property $properties 'DeleteExistingFiles' 'true' $relative
    Assert-Property $properties 'PublishDir' "..\..\artifacts\release\$($profile.Folder)\" $relative
}

if ($release -notmatch '\$multiFileSelfContainedProperties\s*=\s*@\(') { Fail 'Build-Release.ps1 must own one shared multi-file self-contained property list.' }
if (([regex]::Matches($release, '\+\s*\$multiFileSelfContainedProperties')).Count -ne 2) { Fail 'Build-Release.ps1 must apply the shared publish properties to both the application and installer.' }
if ($release -match 'PublishSingleFile=true' -or $release -match 'IncludeNativeLibrariesForSelfExtract=true' -or $release -match 'EnableCompressionInSingleFile=true') { Fail 'Build-Release.ps1 still contains a single-file publish switch.' }
if ($release -notmatch 'Assert-PublishedConfigurationFiles\s+-SourceRoot\s+\$webDirectory\s+-PublishRoot\s+\$appFolder') { Fail 'Build-Release.ps1 must validate every published configuration file.' }
if ($release -match 'Join-Path\s+\$artifacts\s+"PublisherStudio\.Setup\.exe"') { Fail 'A multi-file setup may not be exposed as a misleading standalone executable.' }

foreach ($profile in $expectedWebProfiles) {
    $mapping = 'AppFolder = "{0}"' -f $profile.Folder
    if ($release -notmatch [regex]::Escape($mapping)) { Fail "Build-Release.ps1 is missing application folder mapping $($profile.Folder)." }
}
foreach ($profile in @($expectedSetupProfiles | Where-Object { $_.File -ne 'FolderProfile.pubxml' })) {
    $mapping = 'SetupFolder = "{0}"' -f $profile.Folder
    if ($release -notmatch [regex]::Escape($mapping)) { Fail "Build-Release.ps1 is missing installer folder mapping $($profile.Folder)." }
}
foreach ($runtime in @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
    if ($allRuntimes -notmatch [regex]::Escape('"' + $runtime + '"')) { Fail "Build-AllRuntimes.ps1 is missing runtime $runtime." }
}
if ($allRuntimes -notmatch 'Assert-PublishConfiguration\.ps1') { Fail 'Build-AllRuntimes.ps1 must validate publish-profile synchronization before dispatch.' }

Write-Host "Publish configuration validation passed for $($expectedWebProfiles.Count) application and $($expectedSetupProfiles.Count) installer profiles. All publishes are self-contained, multi-file and configuration-complete."
