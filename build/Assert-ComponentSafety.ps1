param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')
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
foreach ($token in @('@inject ILoggerFactory OperationalLoggerFactory','@inject IUserNotificationService OperationalNotifications')) {
    if ($imports.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "Global component safety import was removed: $token" }
}

$razorFiles = @(Get-ChildItem -LiteralPath $componentRoot -Recurse -File -Filter '*.razor' | Where-Object { $_.Name -ne '_Imports.razor' })
foreach ($file in $razorFiles) {
    $componentName = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $expected = "@inject ILogger<$componentName> Logger"
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $matches = [regex]::Matches($text, [regex]::Escape($expected))
    $count = $matches.Count
    if ($count -ne 1) {
        $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace('\','/')
        $lineNumber = 1
        if ($count -gt 0) {
            $lineNumber = 1 + $text.Substring(0, $matches[0].Index).Split([char]10).Count - 1
        }
        Write-Output ("{0}({1},1): error RAZORLOG0001: Every Razor component must own exactly one typed ILogger injection '{2}'; found {3}." -f $relative, $lineNumber, $expected, $count)
        Write-Output "  Architectural choices: restore exactly one typed component logger in the top directive block. Keep component-local diagnostics; do not weaken the rule into a global-only logger or remove the failing operation."
        Fail "Typed component logger ownership failed for $relative."
    }
}

$mainLayout = Get-Content -LiteralPath $mainLayoutPath -Raw
foreach ($token in @('<UserNotificationHost />', '<OperationalErrorBoundary')) {
    if ($mainLayout.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "MainLayout must retain component safety boundary token: $token" }
}
$boundary = Get-Content -LiteralPath $boundaryPath -Raw
foreach ($token in @('protected override Task OnErrorAsync(Exception exception)','try','catch (Exception boundaryException)','Logger.LogCritical','Notifications.Error(')) {
    if ($boundary.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Fail "OperationalErrorBoundary must retain '$token'." }
}

$audit = Join-Path $PSScriptRoot 'audit_component_resilience.py'
if (-not (Test-Path -LiteralPath $audit -PathType Leaf)) { Fail 'The strict method-granular component resilience audit is missing.' }
$result = Invoke-PublisherStudioPythonScript -ScriptPath $audit -Arguments @('--root', $RepositoryRoot)
$result.Output | ForEach-Object { Write-Host ([string]$_) }
if ($result.ExitCode -ne 0) { Fail 'Component method resilience audit failed.' }

$prerenderAudit = Join-Path $PSScriptRoot 'audit_prerender_interop_safety.py'
if (-not (Test-Path -LiteralPath $prerenderAudit -PathType Leaf)) { Fail 'The prerender JavaScript interop safety audit is missing.' }
$result = Invoke-PublisherStudioPythonScript -ScriptPath $prerenderAudit -Arguments @('--root', $RepositoryRoot)
$result.Output | ForEach-Object { Write-Host ([string]$_) }
if ($result.ExitCode -ne 0) { Fail 'Prerender JavaScript interop safety audit failed.' }

Write-Host "Component safety validation passed: $($razorFiles.Count) Razor components own typed loggers; every component method is method-locally guarded; prerender JavaScript interop is attachment-gated; no legacy exemptions are permitted." -ForegroundColor Green
