param([string]$RepositoryRoot = '')
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

$ErrorActionPreference = 'Stop'
$app = Join-Path $RepositoryRoot 'src/PublisherStudio.Web'
$issues = [Collections.Generic.List[string]]::new()
$checks = 0
function Require-Token([string]$Relative,[string]$Token) {
    $script:checks++
    $path = Join-Path $RepositoryRoot $Relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $script:issues.Add("Missing platform contract file: $Relative"); return }
    if (-not [IO.File]::ReadAllText($path).Contains($Token)) { $script:issues.Add("$Relative lacks platform contract '$Token'") }
}
$project = [IO.File]::ReadAllText((Join-Path $app 'PublisherStudio.Web.csproj'))
foreach ($package in @('System.Drawing.Common','System.Security.Cryptography.ProtectedData')) { $checks++; if ($project.Contains($package)) { $issues.Add("Web project must not reference $package") } }
$approvedOsBranches = @('Program.cs','PublisherStudioServiceCollectionExtensions.cs','Services/PublisherPlatformRuntimeServices.cs','Services/Streaming/Capture/WindowsProcessLoopbackCapture.cs','Services/Streaming/Capture/WindowsProcessLoopbackNativeService.cs','Services/Streaming/Hotkeys/WindowsHotkeyNativeService.cs')
$files = @(Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:obj|bin)[\\/]' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($app.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -match '\busing\s+System\.Drawing\s*;|\bSystem\.Drawing\.') { $issues.Add("System.Drawing leak: $relative") }
    if ($relative -notin $approvedOsBranches -and $text -match 'OperatingSystem\.Is(?:Windows|Linux|MacOS|FreeBSD)|RuntimeInformation\.IsOSPlatform') { $issues.Add("OS branch escaped platform owner: $relative") }
}
foreach ($marker in @('IPublisherPlatformRuntimeService, WindowsPublisherPlatformRuntimeService','IPublisherPlatformRuntimeService, UnixPublisherPlatformRuntimeService','IGlobalHotkeyNativeService, WindowsHotkeyNativeService','IGlobalHotkeyNativeService, UnixGlobalHotkeyNativeService','IProcessLoopbackNativeService, WindowsProcessLoopbackNativeService','IProcessLoopbackNativeService, UnixProcessLoopbackNativeService','IProcessLoopbackCaptureFactory, WindowsProcessLoopbackCaptureFactory','IProcessLoopbackCaptureFactory, UnixProcessLoopbackCaptureFactory','INativeDeviceDiscoveryPlatformService, WindowsNativeDeviceDiscoveryPlatformService','INativeDeviceDiscoveryPlatformService, UnixNativeDeviceDiscoveryPlatformService')) { Require-Token 'src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs' $marker }
foreach ($marker in @('IsSameOrDescendantPath','File.SetUnixFileMode','FfmpegUnixInstallPaths')) { Require-Token 'src/PublisherStudio.Web/Services/PublisherPlatformRuntimeServices.cs' $marker }
foreach ($relative in @('Services/Documentation/PublisherDocumentationCatalogService.cs','Services/Streaming/Lan/LanStreamingServer.cs','Services/Streaming/UseCases/Lan/StreamingLanUseCases.cs')) { Require-Token ('src/PublisherStudio.Web/' + $relative) 'IsSameOrDescendantPath' }
foreach ($relative in @('Build-Release.ps1','Build-LocalDevelopment.ps1')) { Require-Token $relative 'Assert-CrossPlatformBoundaries.ps1'; Require-Token $relative 'Assert-SourcePackagePrerequisites.ps1' }
Require-Token 'src/PublisherStudio.Web/PublisherStudio.Web.csproj' 'PublisherStudioPowerShellHost'
Require-Token 'build/Build-Documentation.ps1' 'html-accessibility-fallback'
Require-Token 'build/Assert-PagesArtifact.ps1' 'htmlPreflightValidated'
if ($issues.Count) { $issues | ForEach-Object { Write-Error $_ }; throw "PublisherStudio cross-platform boundary audit found $($issues.Count) issues." }
Write-Host "PublisherStudio PowerShell cross-platform audit passed ($($files.Count) sources, $checks contracts)."
