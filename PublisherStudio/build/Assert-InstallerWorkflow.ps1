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
foreach ($required in @(
    'Path.Combine(localAppData, "PublisherStudio")',
    'ExtractZipWithFallback(zipPath, targetPath, logger)',
    'ExtractZipWithFallback(setupZipPath, targetPath, logger)',
    'PublisherStudio app and setup/bootstrap files now reside',
    'Running the default install, update, shortcut, and start routine.',
    'TryStartDetachedSetup(args)'
)) {
    if (-not $program.Contains($required)) { Fail "Program.cs is missing the LocalGPT-aligned deployment contract: $required" }
}
foreach ($forbidden in @(
    'Path.Combine(localAppData, "Programs", "PublisherStudio")',
    'PublisherStudioInstallLayout',
    'PublisherStudioDeploymentService',
    'PublisherStudioReleaseManifest',
    'preservation-first'
)) {
    if ($program.Contains($forbidden)) { Fail "Program.cs still contains the superseded installer contract: $forbidden" }
}

$parseStart = $program.IndexOf('public static CliOptions Parse(string[] args)', [StringComparison]::Ordinal)
if ($parseStart -lt 0) { Fail 'CliOptions.Parse was not found.' }
$parseText = $program.Substring($parseStart)
$defaultStart = $parseText.IndexOf('if (argsList.Count == 0)', [StringComparison]::Ordinal)
$loopIndex = $parseText.IndexOf('for (var i = 0; i < argsList.Count; i++)', $defaultStart, [StringComparison]::Ordinal)
if ($defaultStart -lt 0 -or $loopIndex -lt 0) { Fail 'The no-command workflow was not found.' }
$defaultBlock = $parseText.Substring($defaultStart, $loopIndex - $defaultStart)
foreach ($required in @(
    'argsList.Add("--install-publisherstudio")',
    'argsList.Add("--update-publisherstudio")',
    'argsList.Add("--install-ffmpeg")',
    'argsList.Add("--start-publisherstudio")',
    'argsList.Add("--shortcuts")'
)) {
    if (-not $defaultBlock.Contains($required)) { Fail "The default workflow is missing $required." }
}
if ($defaultBlock.Contains('ForceDelete') -or $defaultBlock.Contains('--force-delete')) {
    Fail 'The no-command workflow may not delete the PublisherStudio AppData installation.'
}

$launchers = @('Install.cmd', 'Update.cmd', 'Start.cmd')
foreach ($launcher in $launchers) {
    $projectPath = Join-Path $installerRoot $launcher
    $mirrorPath = Join-Path $launcherMirrorRoot $launcher
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { Fail "Missing installer launcher $launcher." }
    if (-not (Test-Path -LiteralPath $mirrorPath -PathType Leaf)) { Fail "Missing release-launcher mirror $launcher." }
    $projectContent = Get-Content -LiteralPath $projectPath -Raw
    $mirrorContent = Get-Content -LiteralPath $mirrorPath -Raw
    if ($projectContent -ne $mirrorContent) { Fail "$launcher differs between the installer project and installer-launchers mirror." }
    if ($projectContent.Contains('--force-delete')) { Fail "$launcher may not delete the PublisherStudio AppData installation." }
    if (-not $program.Contains('"' + $launcher + '"')) { Fail "Shortcut provisioning is missing $launcher." }
}

$obsoleteLaunchers = @('Default.cmd', 'Start-NoBrowser.cmd', 'Check-FFmpeg.cmd', 'Install-FFmpeg.cmd', 'Uninstall.cmd')
foreach ($launcher in $obsoleteLaunchers) {
    if (Test-Path -LiteralPath (Join-Path $installerRoot $launcher)) { Fail "Obsolete installer launcher still exists: $launcher" }
    if (Test-Path -LiteralPath (Join-Path $launcherMirrorRoot $launcher)) { Fail "Obsolete release-launcher mirror still exists: $launcher" }
}

$launchSettingsPath = Join-Path $installerRoot 'Properties\launchSettings.json'
$launchSettings = Get-Content -LiteralPath $launchSettingsPath -Raw | ConvertFrom-Json
$launchProfileNames = @($launchSettings.profiles.PSObject.Properties | ForEach-Object { $_.Name })
$requiredLaunchProfiles = @('PublisherStudio Install', 'PublisherStudio Update', 'PublisherStudio Start')
foreach ($profileName in $requiredLaunchProfiles) {
    if ($launchProfileNames -notcontains $profileName) { Fail "Visual Studio launch profile is missing: $profileName" }
}
if ($launchProfileNames.Count -ne 3) { Fail 'Visual Studio launch profiles must expose only Install, Update, and Start.' }

$project = Get-Content -LiteralPath (Join-Path $installerRoot 'PublisherStudio.InstallerConsole.csproj') -Raw
foreach ($launcher in $launchers) {
    if (-not $project.Contains('<None Update="' + $launcher + '" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" />')) {
        Fail "The installer project does not deploy $launcher."
    }
}

$release = Get-Content -LiteralPath (Join-Path $root 'Build-Release.ps1') -Raw
foreach ($launcher in $launchers) {
    if (-not $release.Contains('"' + $launcher + '"')) { Fail "Build-Release.ps1 does not validate deployed launcher $launcher." }
}
foreach ($required in @(
    'Compress-Archive -Path $appFolder -DestinationPath $appZip',
    'Compress-Archive -Path $setupFolder -DestinationPath $setupZip'
)) {
    if (-not $release.Contains($required)) { Fail "Build-Release.ps1 is missing the LocalGPT-aligned archive operation: $required" }
}
foreach ($forbidden in @('Write-ReleaseManifest', 'Write-BootstrapRepairManifest', 'PublisherStudio.Setup.repair.exe', 'publisherstudio-bootstrap-repair.json')) {
    if ($release.Contains($forbidden)) { Fail "Build-Release.ps1 still enforces the superseded repair-manifest flow: $forbidden" }
}

Write-Host 'PublisherStudio installer workflow validation passed. Double-click installs or updates under LOCALAPPDATA\PublisherStudio, creates the mandatory shortcuts, and starts the application.'
