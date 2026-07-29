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
    if ($project.Text -notmatch '<SelfContained\s+Condition="''\$\(RuntimeIdentifier\)'' != ''''">true</SelfContained>') { Fail "$($project.Name) must default RID-based publishes to self-contained output." }
    foreach ($marker in @('<PublishSingleFile>false</PublishSingleFile>', '<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>')) {
        if (-not $project.Text.Contains($marker)) { Fail "$($project.Name) is missing $marker." }
    }
}

$expectedWebProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'winx64' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'winarm64' },
    @{ File = 'linx64.pubxml'; Runtime = 'linux-x64'; Folder = 'linx64' },
    @{ File = 'linarm64.pubxml'; Runtime = 'linux-arm64'; Folder = 'linarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; Folder = 'macosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; Folder = 'macosarm64' }
)

$webProfileRoot = Join-Path $root 'src\PublisherStudio.Web\Properties\PublishProfiles'
$actualWebProfiles = @(Get-ChildItem -LiteralPath $webProfileRoot -File -Filter '*.pubxml' | Select-Object -ExpandProperty Name | Sort-Object)
$expectedWebNames = @($expectedWebProfiles | ForEach-Object { $_.File } | Sort-Object)
if (($actualWebProfiles -join '|') -ne ($expectedWebNames -join '|')) { Fail "Unexpected PublisherStudio.Web publish-profile inventory: $($actualWebProfiles -join ', ')" }

$setupProfileRoot = Join-Path $root 'src\PublisherStudio.InstallerConsole\Properties\PublishProfiles'
if (Test-Path -LiteralPath $setupProfileRoot) {
    $obsoleteProfiles = @(Get-ChildItem -LiteralPath $setupProfileRoot -File -ErrorAction SilentlyContinue)
    if ($obsoleteProfiles.Count -gt 0) { Fail 'Installer publish profiles are obsolete; Build-Release.ps1 is the single installer publish path.' }
}

$profileUserFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)
if ($profileUserFiles.Count -gt 0) { Fail 'User-specific .pubxml.user files must not be shipped in the source package.' }

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

$releaseMappings = @(
    @{ Runtime = 'win-x64'; App = 'winx64'; Setup = 'setupwinx64' },
    @{ Runtime = 'win-arm64'; App = 'winarm64'; Setup = 'setupwinarm64' },
    @{ Runtime = 'linux-x64'; App = 'linx64'; Setup = 'setuplinx64' },
    @{ Runtime = 'linux-arm64'; App = 'linarm64'; Setup = 'setuplinarm64' },
    @{ Runtime = 'osx-x64'; App = 'macosx64'; Setup = 'setupmacosx64' },
    @{ Runtime = 'osx-arm64'; App = 'macosarm64'; Setup = 'setupmacosarm64' }
)
foreach ($mapping in $releaseMappings) {
    foreach ($fragment in @(
        '"' + $mapping.Runtime + '"',
        'AppFolder = "' + $mapping.App + '"',
        'SetupFolder = "' + $mapping.Setup + '"',
        'SetupAsset = "' + $mapping.Setup + '"'
    )) {
        if (-not $release.Contains($fragment)) { Fail "Build-Release.ps1 is missing synchronized mapping: $fragment" }
    }
    if (-not $allRuntimes.Contains('"' + $mapping.Runtime + '"')) { Fail "Build-AllRuntimes.ps1 is missing runtime $($mapping.Runtime)." }
}

if ($release -match 'SetupFolder\s*=\s*"[^"]*-' ) { Fail 'Installer output folders must use the same canonical token as their ZIP asset names.' }
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true') { Fail 'Single-file publishing is not part of the reviewed deployment path.' }

Write-Host "Publish configuration validation passed for $($expectedWebProfiles.Count) web-host profiles and the single scripted installer publish path."
