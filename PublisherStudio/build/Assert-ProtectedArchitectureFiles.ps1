[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'protected-architecture-files.sha256'

function Get-NormalizedTextSha256([string]$Path) {
    $text = [IO.File]::ReadAllText($Path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = [Text.UTF8Encoding]::new($false)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha256.ComputeHash($encoding.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha256.Dispose() }
}

$expectedFiles = @(
    '.gitignore',
    'Directory.Build.targets',
    'Build-LocalDevelopment.ps1',
    'Build-Release.ps1',
    'build/Assert-ProtectedArchitectureFiles.ps1',
    'build/Assert-OneWireArchitecture.ps1',
    'build/Assert-SecurityRulePreservation.ps1',
    'build/security-rules-final19.sha256',
    'build/Assert-RuntimeValueOwnership.ps1',
    'build/runtime-value-ownership-baseline.json',
    'build/Assert-GitSourceVisibility.ps1',
    'tests/final20RuntimeValueOwnership.test.mjs',
    'TEST-RESULTS-v2.0.1-final20-runtime-value-ownership.txt',
    'docs/RUNTIME_VALUE_OWNERSHIP.md',
    'CHANGELOG-v2.0.1-final20-runtime-value-ownership.md',
    'src/PublisherStudio.Web/Services/Configuration/IPanelStudioTextPatternDataService.cs',
    'src/PublisherStudio.Web/Services/Configuration/PanelStudioTextPatternDataService.cs',
    'src/PublisherStudio.Web/Services/Configuration/PanelTextPatternStoreOptions.cs',
    'src/PublisherStudio.Web/Configuration/panel-text-patterns.json',
    'src/PublisherStudio.Web/Services/PanelStudioTextService.cs',
    'src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs',
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
    'src/PublisherStudio.Web/appsettings.json',
    'build/Assert-JavaScriptDiagnostics.ps1',
    'build/javascript-diagnostics-files.sha256',
    'build/Assert-PanelStudioInteractionLifecycle.ps1',
    'build/Assert-InteractiveServerRenderModes.ps1',
    'build/async-continuation-baseline.json',
    'docs/JAVASCRIPT_RUNTIME_DIAGNOSTICS.md',
    'CHANGELOG-v2.0.1-final23-browser-runtime-diagnostics.md',
    'tests/final23JavaScriptRuntimeFix.test.mjs',
    'tests/editorUsabilityRepair.test.mjs',
    'tests/final13PanelRuntimePolicy.test.mjs',
    'tests/interfaceWorkflow.test.mjs',
    'tests/mapInteractionModes.test.mjs',
    'TEST-RESULTS-v2.0.1-final23-browser-runtime-diagnostics.txt',
    'src/PublisherStudio.Web/Components/App.razor',
    'src/PublisherStudio.Web/Components/_Imports.razor',
    'src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor',
    'build/Update-ReviewedProtectionManifest.ps1',
    'docs/REVIEWED_MANIFEST_REFRESH.md',
    'tests/final27ReviewedManifestRefresh.test.mjs',
    'Build-AllRuntimes.ps1',
    'build/Assert-PublishConfiguration.ps1',
    'build/Assert-InstallerWorkflow.ps1',
    'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
    'src/PublisherStudio.InstallerConsole/Program.cs',
    'src/PublisherStudio.InstallerConsole/Properties/launchSettings.json',
    'src/PublisherStudio.InstallerConsole/Default.cmd',
    'src/PublisherStudio.InstallerConsole/Install.cmd',
    'src/PublisherStudio.InstallerConsole/Update.cmd',
    'src/PublisherStudio.InstallerConsole/Start.cmd',
    'src/PublisherStudio.InstallerConsole/Start-NoBrowser.cmd',
    'src/PublisherStudio.InstallerConsole/Check-FFmpeg.cmd',
    'src/PublisherStudio.InstallerConsole/Install-FFmpeg.cmd',
    'src/PublisherStudio.InstallerConsole/Uninstall.cmd',
    'installer-launchers/Default.cmd',
    'installer-launchers/Install.cmd',
    'installer-launchers/Update.cmd',
    'installer-launchers/Start.cmd',
    'installer-launchers/Start-NoBrowser.cmd',
    'installer-launchers/Check-FFmpeg.cmd',
    'installer-launchers/Install-FFmpeg.cmd',
    'installer-launchers/Uninstall.cmd',
    'src/PublisherStudio.Web/Properties/PublishProfiles/winx64.pubxml',
    'src/PublisherStudio.Web/Properties/PublishProfiles/winarm64.pubxml',
    'src/PublisherStudio.Web/Properties/PublishProfiles/linx64.pubxml',
    'src/PublisherStudio.Web/Properties/PublishProfiles/linarm64.pubxml',
    'src/PublisherStudio.Web/Properties/PublishProfiles/macosx64.pubxml',
    'src/PublisherStudio.Web/Properties/PublishProfiles/macosarm64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/FolderProfile.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/winx64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/winarm64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/linuxx64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/linuxarm64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/macosx64.pubxml',
    'src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/macosarm64.pubxml'
)

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Protected architecture-file manifest is missing.' }
$manifest = @{}
foreach ($line in Get-Content -LiteralPath $manifestPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') { throw "Invalid protected architecture manifest line: $line" }
    $relative = $Matches[2].Replace('\', '/')
    if ($manifest.ContainsKey($relative)) { throw "Duplicate protected architecture manifest entry: $relative" }
    $manifest[$relative] = $Matches[1].ToLowerInvariant()
}

$errors = [Collections.Generic.List[string]]::new()
foreach ($relative in $expectedFiles) {
    if (-not $manifest.ContainsKey($relative)) { $errors.Add("Protected architecture file is absent from the manifest: $relative"); continue }
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $errors.Add("Protected architecture file is missing: $relative"); continue }
    if ((Get-NormalizedTextSha256 $path) -ne $manifest[$relative]) { $errors.Add("Protected architecture file changed: $relative") }
}
foreach ($relative in $manifest.Keys | Where-Object { $_ -notin $expectedFiles }) { $errors.Add("Unexpected protected architecture manifest entry: $relative") }
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Protected PublisherStudio final20 architecture files match the reviewed SHA-256 manifest.'
