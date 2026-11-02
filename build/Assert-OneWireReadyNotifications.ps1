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
$organicPage = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor') -Raw
$editorText = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Services/PublicationEditorTextService.cs') -Raw
if (-not $organicPage.Contains('CheckedChanged="@((bool enabled) => SaveAutoConnectPreferenceAsync(enabled))"')) {
    throw 'ONWIRE-READY005: OrganicPlugins.razor must bind the DxCheckBox CheckedChanged event to an explicitly typed bool callback (previously CS1503/CS1662).'
}
if (-not $organicPage.Contains('EditorText.ParseLocalGptApplicationOrigins(_manualLocalGptUrls)') -or
    -not $organicPage.Contains('EditorText.FormatLocalGptConnectionErrors(errors)') -or
    $organicPage.Contains('_manualLocalGptUrls.Split(') -or
    $organicPage.Contains('string.Join(Environment.NewLine, errors)') -or
    -not $editorText.Contains('string[] ParseLocalGptApplicationOrigins(') -or
    -not $editorText.Contains('string FormatLocalGptConnectionErrors(')) {
    throw 'ONWIRE-READY006: OrganicPlugins manual URL parsing/error formatting must remain in the injected text service.'
}
if ($link -notmatch '(?s)ConnectByWebUrlAsync\([^{]+\{\s*try\s*\{' -or
    $link -notmatch '(?s)ConnectByWebUrlAsync\([\s\S]*?catch\s*\(Exception exception\)') {
    throw 'ONWIRE-READY007: ConnectByWebUrlAsync requires method-local try/catch with diagnostics and unchanged error propagation.'
}
Write-Host 'ONWIRE-READY: accepted capabilities, accurate user notifications, and fail-closed decryption retained; manual LocalGPT connection callback/service contracts verified.'
