[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$installerRoot = Join-Path $root 'src\PublisherStudio.InstallerConsole'
$launcherMirrorRoot = Join-Path $root 'installer-launchers'

function Fail([string]$Message) {
    throw "PublisherStudio installer workflow validation failed: $Message"
}

$program = Get-Content -LiteralPath (Join-Path $installerRoot 'Program.cs') -Raw
if ($program.Contains('The setup help will be shown.')) {
    Fail 'The installer startup message still claims the removed help-only no-command behavior.'
}
if (-not $program.Contains('Running the default preservation-first install and update routine.')) {
    Fail 'The installer startup message must announce the active no-command install/update routine.'
}
$parseStart = $program.IndexOf('public static CliOptions Parse(string[] args)', [StringComparison]::Ordinal)
if ($parseStart -lt 0) { Fail 'CliOptions.Parse was not found.' }
$parseText = $program.Substring($parseStart)
$defaultStart = $parseText.IndexOf('if (argsList.Count == 0)', [StringComparison]::Ordinal)
$returnIndex = $parseText.IndexOf('return options;', $defaultStart, [StringComparison]::Ordinal)
if ($defaultStart -lt 0 -or $returnIndex -lt 0) { Fail 'The no-command workflow was not found.' }
$defaultBlock = $parseText.Substring($defaultStart, ($returnIndex - $defaultStart) + 'return options;'.Length)
foreach ($required in @(
    'UpdateBlazorPublisher = true',
    'StartBlazorPublisher = true',
    'InstallFfmpeg = true',
    'DesktopShortcuts = true',
    'StartMenuShortcuts = true'
)) {
    if (-not $defaultBlock.Contains($required)) { Fail "The default workflow is missing $required." }
}
if ($defaultBlock.Contains('ForceDelete') -or $defaultBlock.Contains('argsList.Add("--force-delete")')) {
    Fail 'The no-command workflow may not delete the LocalAppData installation.'
}

$launchers = @(
    'Default.cmd',
    'Install.cmd',
    'Update.cmd',
    'Start.cmd',
    'Start-NoBrowser.cmd',
    'Check-FFmpeg.cmd',
    'Install-FFmpeg.cmd',
    'Uninstall.cmd'
)
foreach ($launcher in $launchers) {
    $projectPath = Join-Path $installerRoot $launcher
    $mirrorPath = Join-Path $launcherMirrorRoot $launcher
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { Fail "Missing installer launcher $launcher." }
    if (-not (Test-Path -LiteralPath $mirrorPath -PathType Leaf)) { Fail "Missing release-launcher mirror $launcher." }

    $projectContent = Get-Content -LiteralPath $projectPath -Raw
    $mirrorContent = Get-Content -LiteralPath $mirrorPath -Raw
    if ($projectContent -ne $mirrorContent) { Fail "$launcher differs between the installer project and installer-launchers mirror." }
    if ($launcher -ne 'Uninstall.cmd' -and $projectContent.Contains('--force-delete')) {
        Fail "$launcher may not delete the LocalAppData installation."
    }
    if (-not $program.Contains('"' + $launcher + '"')) {
        Fail "Shortcut provisioning is missing $launcher."
    }
}

$defaultLauncher = Get-Content -LiteralPath (Join-Path $installerRoot 'Default.cmd') -Raw
foreach ($required in @(
    'set "SETUP_EXE=%~dp0PublisherStudio.Setup.exe"',
    'set "SETUP_REPAIR=%~dp0PublisherStudio.Setup.repair.exe"',
    'copy /b /y "%SETUP_REPAIR%" "%SETUP_EXE%.incoming"',
    'move /y "%SETUP_EXE%.incoming" "%SETUP_EXE%"',
    'call "%SETUP_EXE%"',
    ':setup_repair_failed'
)) {
    if (-not $defaultLauncher.Contains($required)) {
        Fail "Default.cmd is missing the launcher-repair contract: $required"
    }
}
if ($defaultLauncher.Contains('call "%SETUP_EXE%" --')) {
    Fail 'Default.cmd must invoke the setup executable without command-line arguments after promoting any staged repair.'
}

$launchSettingsPath = Join-Path $installerRoot 'Properties\launchSettings.json'
$launchSettings = Get-Content -LiteralPath $launchSettingsPath -Raw | ConvertFrom-Json
$launchProfileNames = @($launchSettings.profiles.PSObject.Properties | ForEach-Object { $_.Name })
$requiredLaunchProfiles = @(
    'BlazorPublisher Default Install and Update',
    'BlazorPublisher Install Preserving Data',
    'BlazorPublisher Update Preserving Data',
    'BlazorPublisher Start',
    'BlazorPublisher Start without Browser',
    'BlazorPublisher Check FFmpeg',
    'BlazorPublisher Install FFmpeg',
    'BlazorPublisher Uninstall Preview',
    'BlazorPublisher Uninstall Explicit Delete'
)
foreach ($profileName in $requiredLaunchProfiles) {
    if ($launchProfileNames -notcontains $profileName) {
        Fail "Visual Studio launch profile is missing: $profileName"
    }
}

$project = Get-Content -LiteralPath (Join-Path $installerRoot 'PublisherStudio.InstallerConsole.csproj') -Raw
if (-not $project.Contains('<None Update="*.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" />')) {
    Fail 'The installer project must deploy every maintained command launcher.'
}

$release = Get-Content -LiteralPath (Join-Path $root 'Build-Release.ps1') -Raw
foreach ($launcher in $launchers) {
    if (-not $release.Contains('"' + $launcher + '"')) {
        Fail "Build-Release.ps1 does not validate deployed launcher $launcher."
    }
}

Write-Host 'PublisherStudio installer workflow validation passed. Double-click runs the preservation-first default update/install routine, FFmpeg is checked, and all launchers are synchronized.'
