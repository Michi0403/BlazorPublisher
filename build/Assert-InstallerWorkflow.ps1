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
    'TryStartDetachedSetup(args)',
    '_ = Process.Start(startInfo)'
)) {
    if (-not $program.Contains($required)) { Fail "Program.cs is missing the LocalGPT-aligned deployment contract: $required" }
}
foreach ($forbidden in @(
    'Path.Combine(localAppData, "Programs", "PublisherStudio")',
    'PublisherStudioInstallLayout',
    'PublisherStudioDeploymentService',
    'PublisherStudioReleaseManifest',
    'preservation-first',
    'Falling back to first matching setup mode'
)) {
    if ($program.Contains($forbidden)) { Fail "Program.cs still contains the superseded installer contract: $forbidden" }
}

foreach ($required in @(
    'https://github.com/{repo}/releases/latest/download/{Uri.EscapeDataString(expectedAssetName)}',
    'Direct latest-release download for {AssetName} failed. Falling back to the GitHub release API.',
    'https://api.github.com/repos/{repo}/releases/latest',
    'GetExpectedReleaseAssetName(runtimeIdentifier, setupAsset)',
    'string.Equals(name, expectedAssetName, StringComparison.OrdinalIgnoreCase)',
    'Refusing to guess or deploy another runtime'
)) {
    if (-not $program.Contains($required)) { Fail "Program.cs is missing exact latest-release asset selection: $required" }
}

$runtimeAssets = @(
    @{ Runtime = 'win-x64'; Folder = 'winx64' },
    @{ Runtime = 'win-x86'; Folder = 'winx86' },
    @{ Runtime = 'win-arm64'; Folder = 'winarm64' },
    @{ Runtime = 'linux-x64'; Folder = 'linuxx64' },
    @{ Runtime = 'linux-arm64'; Folder = 'linuxarm64' },
    @{ Runtime = 'osx-x64'; Folder = 'macosx64' },
    @{ Runtime = 'osx-arm64'; Folder = 'macosarm64' }
)
foreach ($runtimeAsset in $runtimeAssets) {
    $mapping = '"' + $runtimeAsset.Runtime + '" => "' + $runtimeAsset.Folder + '"'
    if (-not $program.Contains($mapping)) { Fail "Program.cs is missing exact release asset mapping: $mapping" }
}

$installStart = $program.IndexOf('private static async Task InstallPublisherStudioAsync', [StringComparison]::Ordinal)
$uninstallStart = $program.IndexOf('private static void UninstallPublisherStudioWindows', [StringComparison]::Ordinal)
if ($installStart -lt 0 -or $uninstallStart -le $installStart) { Fail 'InstallPublisherStudioAsync could not be isolated for preservation validation.' }
$installText = $program.Substring($installStart, $uninstallStart - $installStart)
$deleteCalls = [Regex]::Matches($installText, 'DeleteIfExists\(targetPath, logger\)').Count
if ($deleteCalls -ne 1) { Fail "Normal installation contains $deleteCalls product-root delete calls; exactly one force-delete-gated call is required." }
if (-not [Regex]::IsMatch($installText, 'if\s*\(options\.ForceDelete\)\s*DeleteIfExists\(targetPath, logger\)', [Text.RegularExpressions.RegexOptions]::CultureInvariant)) {
    Fail 'The product-root delete call is not gated directly by options.ForceDelete.'
}
if ($installText.Contains('Directory.Delete(')) { Fail 'Normal install/update may not recursively delete the PublisherStudio product root.' }

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
    'function New-PublisherStudioReleaseArchive',
    'New-PublisherStudioReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip',
    'New-PublisherStudioReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip',
    'Assert-ReleaseArchiveLayout -ArchivePath $appZip',
    'Assert-ReleaseArchiveLayout -ArchivePath $setupZip'
)) {
    if (-not $release.Contains($required)) { Fail "Build-Release.ps1 is missing the verified archive operation: $required" }
}
if ($release.Contains('Compress-Archive')) {
    Fail 'Build-Release.ps1 may not use Compress-Archive for release payloads.'
}
foreach ($forbidden in @('Write-ReleaseManifest', 'Write-BootstrapRepairManifest', 'PublisherStudio.Setup.repair.exe', 'publisherstudio-bootstrap-repair.json')) {
    if ($release.Contains($forbidden)) { Fail "Build-Release.ps1 still enforces the superseded repair-manifest flow: $forbidden" }
}

Write-Host 'PublisherStudio installer workflow validation passed. Double-click installs or updates under LOCALAPPDATA\PublisherStudio, creates the mandatory shortcuts, and starts the application.'
