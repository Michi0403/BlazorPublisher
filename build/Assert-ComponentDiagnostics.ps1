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
function Test-DocumentationOnlyRazorPartial([System.IO.FileInfo]$File, [string]$Text) {
    if (-not $File.Name.EndsWith('.razor.cs', [System.StringComparison]::OrdinalIgnoreCase)) { return $false }
    $razorPath = $File.FullName.Substring(0, $File.FullName.Length - 3)
    if (-not (Test-Path -LiteralPath $razorPath -PathType Leaf)) { return $false }

    # XML-documentation companion files intentionally contain only an empty partial
    # declaration so DocFX/compiler XML docs can describe the generated Razor class.
    # They are not an operational component boundary. Any field, property, method,
    # constructor, base type, attribute, or other executable/member content makes this
    # strict whole-file pattern fail and the file is audited normally below.
    $documentationOnlyPattern = '(?s)^\s*namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;\s*(?:(?:\s*///[^\r\n]*(?:\r?\n|$))+\s*)public\s+partial\s+class\s+[A-Za-z_][A-Za-z0-9_]*\s*\{\s*\}\s*$'
    return [regex]::IsMatch($Text, $documentationOnlyPattern)
}
$documentationOnlyPartials = 0
$files = Get-ChildItem -LiteralPath (Join-Path $appRoot 'Components') -Recurse -File | Where-Object { $_.Extension -eq '.cs' -or $_.Extension -eq '.razor' }
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    $relative = $file.FullName.Substring($appRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $catchCount = [regex]::Matches($text, '\bcatch\b').Count
    $logCount = [regex]::Matches($text, '\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(').Count
    $notificationCount = [regex]::Matches($text, '\b(?:Notifications|OperationalNotifications)\.(?:Success|Info|Warning|Error)\s*\(').Count
    if (-not $baseline.ContainsKey($relative)) {
        if (Test-DocumentationOnlyRazorPartial $file $text) {
            $documentationOnlyPartials++
            continue
        }
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
Write-Host "Component diagnostics validation passed for $($files.Count) PublisherStudio component source files; $documentationOnlyPartials XML-documentation-only Razor partial shell(s) were classified as non-operational." -ForegroundColor Green
