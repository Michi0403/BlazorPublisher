# Fail the PublisherStudio build if 1-Wire connection notifications claim success before accepted capabilities.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$editor = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Components/Pages/Editor.razor') -Raw
$link = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs') -Raw
if ($editor -notmatch 'connection.IsConnected && connection.IsLinked && connection.RemoteCapabilities.Count > 0') {
    throw 'ONWIRE-READY001: PublisherStudio may report AI link success only after an accepted capability response.'
}
if ($editor -notmatch 'Notifications.Error\(connection.LastError' -or $editor -notmatch '!connection.IsConnected') {
    throw 'ONWIRE-READY002: runtime 1-Wire errors must reach the shared notification service.'
}
if ($editor -match 'Notifications.Success\("LocalGPT link restored\."') {
    throw 'ONWIRE-READY003: transport establishment alone must not produce a restored-link success toast.'
}
if ($link -notmatch 'await security.UnprotectIncomingAsync\(envelope, cancellationToken\)') {
    throw 'ONWIRE-READY004: cryptographic receive verification must remain fail-closed.'
}
Write-Host 'ONWIRE-READY: accepted capabilities, accurate user notifications, and fail-closed decryption retained.'
