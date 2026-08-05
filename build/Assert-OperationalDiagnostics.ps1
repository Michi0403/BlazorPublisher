[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$root = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).ProviderPath
$failures = New-Object 'System.Collections.Generic.List[string]'
function Require-Source([string]$relative, [string[]]$patterns, [string]$purpose) {
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path)) { $failures.Add("Required source is missing: $relative"); return }
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $patterns) { if ($text -notmatch $pattern) { $failures.Add("$purpose is missing in '$relative' (pattern: $pattern).") } }
}
Require-Source 'src\PublisherStudio.Web\Components\_Imports.razor' @(
    '@inject\s+ILoggerFactory\s+OperationalLoggerFactory',
    '@inject\s+IUserNotificationService\s+OperationalNotifications'
) 'Global component logger/notifier availability'
Require-Source 'src\PublisherStudio.Web\Components\Shared\OperationalErrorBoundary.cs' @(
    'ILogger<OperationalErrorBoundary>',
    'IUserNotificationService',
    'OnErrorAsync',
    'LogError',
    'Notifications\.Error'
) 'Global component exception diagnostics'
Require-Source 'src\PublisherStudio.Web\Components\Layout\MainLayout.razor' @(
    '<UserNotificationHost\s*/>',
    '<OperationalErrorBoundary(?:\s|>)'
) 'Circuit-scoped notification and error boundary'
Require-Source 'src\PublisherStudio.Web\Components\Editor\PageSurface.razor' @(
    'ILogger<PageSurface>',
    'IUserNotificationService',
    'ReportCanvasInteractionError',
    'data-selection-visual-frame'
) 'Canvas interaction diagnostics'
Require-Source 'src\PublisherStudio.Web\Program.cs' @(
    'ControllerRequestLoggingFilter',
    'Filters\.AddService<ControllerRequestLoggingFilter>'
) 'Controller request diagnostics'
# Dispose methods are exempt: shutdown/disconnect is an expected lifecycle transition.
if ($failures.Count -gt 0) {
    Write-Host 'Operational diagnostics validation failed:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    throw "Operational diagnostics validation failed with $($failures.Count) problem(s)."
}
Write-Host 'Operational diagnostics validation passed for all components through global imports/boundaries and for controller/canvas entry points.' -ForegroundColor Green
