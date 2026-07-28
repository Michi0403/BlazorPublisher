Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Component diagnostics validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'component-diagnostics-baseline.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail 'The component diagnostics baseline is missing.' }
$utf8 = New-Object System.Text.UTF8Encoding($false)
$manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
$appRoot = Join-Path $root ([string]$manifest.sourceRoot).Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
$imports = [System.IO.File]::ReadAllText((Join-Path $appRoot 'Components\_Imports.razor'), $utf8)
if ($imports -notmatch '@inject\s+ILoggerFactory\s+OperationalLoggerFactory' -or $imports -notmatch '@inject\s+IUserNotificationService\s+OperationalNotifications') {
    Fail 'Global component logger/notifier availability was removed from Components/_Imports.razor.'
}
$baseline = @{}
foreach ($property in $manifest.files.PSObject.Properties) { $baseline[[string]$property.Name] = $property.Value }
$failures = New-Object 'System.Collections.Generic.List[string]'
$files = Get-ChildItem -LiteralPath (Join-Path $appRoot 'Components') -Recurse -File | Where-Object { $_.Extension -eq '.cs' -or $_.Extension -eq '.razor' }
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    $relative = $file.FullName.Substring($appRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $catchCount = [regex]::Matches($text, '\bcatch\b').Count
    $logCount = [regex]::Matches($text, '\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(').Count
    $notificationCount = [regex]::Matches($text, '\b(?:Notifications|OperationalNotifications)\.(?:Success|Info|Warning|Error)\s*\(').Count
    if (-not $baseline.ContainsKey($relative)) {
        if (($text -match '@code\s*\{' -or $text -match '\bclass\s+[A-Za-z_]') -and ($catchCount -lt 1 -or $logCount -lt 1 -or $notificationCount -lt 1)) {
            $failures.Add("New operational component '$relative' must include a catch/log/notification boundary. Dispose-only methods remain exempt.")
        }
        continue
    }
    $expected = $baseline[$relative]
    if ($catchCount -lt [int]$expected.minCatchBlocks) { $failures.Add("$relative catch blocks decreased from $($expected.minCatchBlocks) to $catchCount.") }
    if ($logCount -lt [int]$expected.minLogCalls) { $failures.Add("$relative log calls decreased from $($expected.minLogCalls) to $logCount.") }
    if ($notificationCount -lt [int]$expected.minNotificationCalls) { $failures.Add("$relative notification calls decreased from $($expected.minNotificationCalls) to $notificationCount.") }
}
if ($failures.Count -gt 0) {
    Write-Host 'Component diagnostics validation failed:' -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host "  - $failure" -ForegroundColor Red }
    throw "Component diagnostics validation failed with $($failures.Count) problem(s)."
}
Write-Host "Component diagnostics validation passed for $($files.Count) PublisherStudio component source files." -ForegroundColor Green
