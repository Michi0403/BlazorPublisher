[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$findings = [System.Collections.Generic.List[string]]::new()

function Add-Finding([string]$Message) { $findings.Add($Message) }
function Read-OptionalText([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Finding "Missing file: $RelativePath"
        return ''
    }
    return [IO.File]::ReadAllText($path)
}

$webProject = Read-OptionalText 'src\PublisherStudio.Web\PublisherStudio.Web.csproj'
$globalUsing = Read-OptionalText 'src\PublisherStudio.Web\GlobalUsings.OneWire.cs'
$interfaces = Read-OptionalText 'src\PublisherStudio.Web\Services\OrganicPlugins\IOrganicPluginServices.cs'
$connection = Read-OptionalText 'src\PublisherStudio.Web\Services\OrganicPlugins\LocalGptConnectionService.cs'
$state = Read-OptionalText 'src\PublisherStudio.Web\Services\OrganicPlugins\OrganicPluginStateServices.cs'
$discovery = Read-OptionalText 'src\PublisherStudio.Web\HostedServices\OrganicPlugins\LocalGptDiscoveryHostedService.cs'
$applicationHost = Read-OptionalText 'src\PublisherStudio.Web\Services\ApplicationHostServices.cs'
$systemVariableStore = Read-OptionalText 'src\PublisherStudio.Web\Services\Configuration\SystemVariableStoreService.cs'
$settingsPath = Join-Path $root 'src\PublisherStudio.Web\appsettings.json'

if ($webProject -and $webProject -notmatch 'PackageReference Include="LocalGPT\.WireProtocolVersion"') { Add-Finding 'PublisherStudio no longer consumes the authoritative protocol package.' }
if ($webProject -match 'ProjectReference[^\r\n]*LocalGPT\.WireProtocolVersion') { Add-Finding 'PublisherStudio contains a protocol source-project reference instead of the package.' }
if (Test-Path -LiteralPath (Join-Path $root 'src\LocalGPT.WireProtocolVersion')) { Add-Finding 'PublisherStudio contains a duplicate protocol source project.' }
if ($globalUsing -and $globalUsing -notmatch 'global using LocalGPT\.WireProtocol;') { Add-Finding 'The application-wide protocol namespace import is missing.' }
if ($interfaces -and $interfaces -notmatch 'IOrganicReplayGuard') { Add-Finding 'The replay-guard contract is missing.' }
if ($state -and $state -notmatch 'class OrganicReplayGuard') { Add-Finding 'The replay-guard implementation is missing.' }
if ($connection -and $connection -notmatch 'IOrganicConnectionRuntimeState') { Add-Finding 'Connection transport locality is no longer owned by the runtime-state service.' }
if ($connection -and $connection -notmatch 'SourcePeerId does not match the peer identity owned by this connection') { Add-Finding 'The TCP connection no longer pins SourcePeerId to its discovered peer.' }
if ($discovery -and $discovery -notmatch 'automaticallyAttemptedPeers\.Remove') { Add-Finding 'Failed automatic connections will not become retryable.' }
if ($applicationHost -and $applicationHost -notmatch 'systemVariables\.DefaultPort') { Add-Finding 'PublisherStudio port resolution no longer uses systemVariables.DefaultPort.' }
if ($systemVariableStore -and $systemVariableStore -notmatch 'Application\.DefaultPort') { Add-Finding 'SystemVariableStoreService.cs no longer owns Application.DefaultPort.' }
if ($connection -and ($connection -notmatch 'SynchronizeLocalCapabilityDirectoryAsync' -or $connection -notmatch 'capabilities\.Changed \+= SignalCapabilitySynchronization')) { Add-Finding 'PublisherStudio no longer performs event-driven post-link capability synchronization.' }
if ($connection -and $connection -notmatch 'OrganicWireMessageType\.CapabilityRequest') { Add-Finding 'PublisherStudio no longer answers linked-peer capability refresh requests.' }
if ($connection -and $connection -notmatch 'OrganicWireMessageType\.CapabilityResponse') { Add-Finding 'PublisherStudio no longer broadcasts refreshed capability directories.' }
if ($connection -and $connection -notmatch 'OrganicWireMessageType\.Pong') { Add-Finding 'PublisherStudio no longer completes lightweight Ping/Pong transport-test waiters.' }
$organicPage = Read-OptionalText 'src\PublisherStudio.Web\Components\Pages\OrganicPlugins.razor'
if ($organicPage -and $organicPage -notmatch 'OrganicWireMessageType\.Ping') { Add-Finding 'The PublisherStudio round-trip test no longer uses a lightweight 1-Wire Ping.' }
if ($organicPage -and $organicPage -notmatch 'OrganicWireMessageType\.Pong') { Add-Finding 'The PublisherStudio round-trip test no longer verifies the expected Pong response.' }

if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
    Add-Finding 'Missing PublisherStudio appsettings.json.'
}
else {
    try { $settings = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json }
    catch { Add-Finding "appsettings.json is invalid JSON: $($_.Exception.Message)"; $settings = $null }
    if ($settings) {
        if ([int]$settings.PublisherStudio.Port -ne 58071) { Add-Finding 'PublisherStudio default port is not 58071.' }
        if ([int]$settings.OrganicPlugins.ServicePort -ne 51140 -or [int]$settings.OrganicPlugins.DiscoveryPort -ne 51141) { Add-Finding 'Organic 1-Wire ports no longer match LocalGPT.' }
    }
}

if ($findings.Count -eq 0) {
    Write-Host 'PublisherStudio 1-Wire static audit completed with no findings.' -ForegroundColor Green
}
else {
    foreach ($finding in $findings) { Write-Warning $finding }
    Write-Host "PublisherStudio 1-Wire static audit completed with $($findings.Count) finding(s). This audit reports only and does not block the build." -ForegroundColor Yellow
}
