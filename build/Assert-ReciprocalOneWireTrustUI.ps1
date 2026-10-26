# Fails release/build when trust diagnostics are hidden or bypassed. Windows PowerShell 5.1 compatible.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$security = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Services/OrganicPlugins/OrganicRuntimeSecurityService.cs'))
$panel = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Components/OrganicPlugins/OrganicSecurityPanel.razor'))
$connection = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor'))
if (-not $security.Contains('IsCurrentTrust(candidate, envelope.SourcePeerId)') -or -not $security.Contains('The secured 1-Wire message came from an untrusted or expired peer.')) {
    throw 'WIRETRUST0001: Fail-closed incoming peer verification was removed or weakened.'
}
if (-not $panel.Contains('No trusted LocalGPT 1-Wire peer is registered in PublisherStudio') -or -not $panel.Contains('Never bypass signature verification')) {
    throw 'WIRETRUST0002: Reciprocal MFA pairing instructions disappeared.'
}
if (-not $connection.Contains('role="alert"') -or -not $connection.Contains('If the last error reports an untrusted or expired peer')) {
    throw 'WIRETRUST0003: Connection failure or pairing recovery is not visible.'
}
Write-Host 'WIRETRUST: fail-closed 1-Wire verification and visible reciprocal-trust diagnostics preserved.'
