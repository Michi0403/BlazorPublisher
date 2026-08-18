param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Component safety validation failed: $Message" }

$componentRoot = Join-Path $RepositoryRoot 'src\PublisherStudio.Web\Components'
$importsPath = Join-Path $componentRoot '_Imports.razor'
$mainLayoutPath = Join-Path $componentRoot 'Layout\MainLayout.razor'
$boundaryPath = Join-Path $componentRoot 'Shared\OperationalErrorBoundary.cs'
$notificationHostPath = Join-Path $componentRoot 'Shared\UserNotificationHost.razor'

foreach ($requiredPath in @($importsPath, $mainLayoutPath, $boundaryPath, $notificationHostPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { Fail "Required component-safety file is missing: $requiredPath" }
}

$imports = Get-Content -LiteralPath $importsPath -Raw
foreach ($token in @(
    '@inject ILoggerFactory OperationalLoggerFactory',
    '@inject IUserNotificationService OperationalNotifications')) {
    if ($imports.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "Global component safety import was removed: $token" }
}

$mainLayout = Get-Content -LiteralPath $mainLayoutPath -Raw
foreach ($token in @('<UserNotificationHost />', '<OperationalErrorBoundary')) {
    if ($mainLayout.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "MainLayout must retain component safety boundary token: $token" }
}

$boundary = Get-Content -LiteralPath $boundaryPath -Raw
foreach ($token in @(
    'protected override Task OnErrorAsync(Exception exception)',
    'try',
    'catch (Exception boundaryException)',
    'Logger.LogCritical',
    'Notifications.Error(')) {
    if ($boundary.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "OperationalErrorBoundary must retain '$token'." }
}

$audit = Join-Path $PSScriptRoot 'audit_component_resilience.py'
if (-not (Test-Path -LiteralPath $audit -PathType Leaf)) { Fail 'The method-granular Razor component resilience audit is missing.' }

$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source $audit --root $RepositoryRoot
    if ($LASTEXITCODE -ne 0) { Fail 'Razor component method resilience audit failed.' }
}
else {
    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        & $launcher.Source -3 $audit --root $RepositoryRoot
        if ($LASTEXITCODE -ne 0) { Fail 'Razor component method resilience audit failed.' }
    }
    else {
        Fail 'Python is required for the method-granular Razor component resilience audit; the build must not silently weaken this safety policy.'
    }
}

Write-Host 'Component safety validation passed: global logging/notification boundary, operational error boundary, and method-granular Razor resilience policy are intact.' -ForegroundColor Green
