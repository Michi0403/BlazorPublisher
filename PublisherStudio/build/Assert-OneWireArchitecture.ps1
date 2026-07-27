param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Fail([string]$Message) { throw "1-Wire architecture validation failed: $Message" }
function ReadText([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { Fail "Required file is missing: $RelativePath" }
    return [System.IO.File]::ReadAllText($path)
}

$webProject = ReadText "src\PublisherStudio.Web\PublisherStudio.Web.csproj"
$globalUsing = ReadText "src\PublisherStudio.Web\GlobalUsings.OneWire.cs"
$securityService = ReadText "src\PublisherStudio.Web\Services\OrganicPlugins\OrganicRuntimeSecurityService.cs"
$installer = ReadText "src\PublisherStudio.InstallerConsole\Program.cs"
$portResolver = ReadText "src\PublisherStudio.Web\Services\ApplicationHostServices.cs"
$buildScript = ReadText "Build-LocalDevelopment.ps1"
$installerProject = ReadText "src\PublisherStudio.InstallerConsole\PublisherStudio.InstallerConsole.csproj"
$installLauncher = ReadText "src\PublisherStudio.InstallerConsole\Install.cmd"
$updateLauncher = ReadText "src\PublisherStudio.InstallerConsole\Update.cmd"
$startLauncher = ReadText "src\PublisherStudio.InstallerConsole\Start.cmd"
$uninstallLauncher = ReadText "src\PublisherStudio.InstallerConsole\Uninstall.cmd"
$settings = Get-Content -Raw -LiteralPath (Join-Path $root "src\PublisherStudio.Web\appsettings.json") | ConvertFrom-Json
$launch = Get-Content -Raw -LiteralPath (Join-Path $root "src\PublisherStudio.Web\Properties\launchSettings.json") | ConvertFrom-Json

if ($webProject -notmatch 'PackageReference Include="LocalGPT\.WireProtocolVersion"') { Fail "PublisherStudio must consume the authoritative protocol package." }
if ($webProject -match 'ProjectReference[^\r\n]*LocalGPT\.WireProtocolVersion') { Fail "PublisherStudio must not contain a protocol source-project reference." }
if (Test-Path -LiteralPath (Join-Path $root "src\LocalGPT.WireProtocolVersion")) { Fail "PublisherStudio must not duplicate the LocalGPT protocol source project." }
if ($globalUsing -notmatch 'global using LocalGPT\.WireProtocol;') { Fail "The application-wide protocol namespace import is missing." }
if ($securityService -notmatch 'using LocalGPT\.WireProtocol;') { Fail "OrganicRuntimeSecurityService must explicitly import the protocol namespace in addition to the global safeguard." }
if ($installer -notmatch 'WaitForRuntimeEndpoint' -or $installer -notmatch 'TryGetRunningEndpoint') { Fail "PublisherStudio start must wait for the process-owned runtime URL before opening a browser." }
if ($installer -match 'Thread\.Sleep\(TimeSpan\.FromSeconds\(2\)\)') { Fail "The old guessed two-second browser start returned." }
if ($installer -notmatch 'PublisherStudio startup failed' -or $installer -notmatch 'throw;') { Fail "PublisherStudio desktop startup failures must propagate to the launcher." }
if ($installer -match 'Doomland|Your args to string|args were initially empty') { Fail "Temporary debug-console wording must not ship in PublisherStudio Setup." }
if ($installerProject -notmatch '<None Update="Install\.cmd">' -or $installerProject -notmatch '<None Update="Update\.cmd">' -or $installerProject -notmatch '<None Update="Start\.cmd">' -or $installerProject -notmatch '<None Update="Uninstall\.cmd">') { Fail "The published setup must contain all four reviewed PublisherStudio launchers." }
if ($installLauncher -notmatch '--install-blazorpublisher --force-delete --start-blazorpublisher --port 58071 --shortcuts') { Fail "PublisherStudio fresh install launcher no longer uses the canonical install/start/shortcut path." }
if ($updateLauncher -notmatch '--update-blazorpublisher --start-blazorpublisher --port 58071 --shortcuts' -or $updateLauncher -match '--force-delete') { Fail "PublisherStudio update must preserve local runtime data while restarting on the canonical port." }
if ($startLauncher -notmatch '--start-blazorpublisher --port 58071') { Fail "PublisherStudio Start launcher no longer uses the canonical loopback port." }
if ($uninstallLauncher -notmatch '--uninstall --force-delete') { Fail "PublisherStudio Uninstall launcher no longer performs the reviewed application removal path." }
if ($portResolver -notmatch 'DefaultPort = 58071') { Fail "PublisherStudio debug, installer and desktop start paths must retain port 58071." }
if ($securityService -notmatch 'OneWireProtocol\.') { Fail "Organic runtime security no longer references the authoritative protocol contract." }
if ($buildScript -notmatch 'Ensure-WireProtocolPackage\.ps1') { Fail "The build must bootstrap the authoritative protocol package before restore." }
if ([int]$settings.PublisherStudio.Port -ne 58071) { Fail "PublisherStudio default web port must remain 58071." }
if ([int]$settings.OrganicPlugins.ServicePort -ne 51140 -or [int]$settings.OrganicPlugins.DiscoveryPort -ne 51141) { Fail "Organic 1-Wire defaults must match LocalGPT TCP 51140 / UDP 51141." }
if ([string]$settings.OrganicPlugins.BroadcastAddress -ne '255.255.255.255') { Fail "Organic broadcast address must remain 255.255.255.255." }
if ([string]$launch.profiles.'PublisherStudio.Web'.applicationUrl -ne 'http://127.0.0.1:58071') { Fail "Visual Studio and installer start paths must share the same PublisherStudio loopback URL." }

Write-Host "1-Wire architecture validation passed for PublisherStudio." -ForegroundColor Green
