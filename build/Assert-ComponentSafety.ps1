param([string]$RepositoryRoot = '')

Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

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

# Native method-level resilience: verify that operational component methods retain
# structured error handling and diagnosis. Existing component baseline remains separately enforced.
& (Join-Path $PSScriptRoot 'Assert-ComponentDiagnostics.ps1')
# Blazor component lifecycle methods must use task-returning overrides.
# Do not regress service/UI asynchronous continuation ownership by introducing
# synchronous lifecycle entrypoints. Event handlers are governed separately.
$lifecycleViolations = [Collections.Generic.List[string]]::new()
$lifecycleFiles = @(Get-ChildItem -LiteralPath $componentRoot -Recurse -File |
    Where-Object { $_.Name.EndsWith('.razor', [StringComparison]::OrdinalIgnoreCase) -or
                   $_.Name.EndsWith('.razor.cs', [StringComparison]::OrdinalIgnoreCase) })
# Prevent regression to synchronous recovery disk reads from any Razor UI hook.
foreach ($lifecycleFile in $lifecycleFiles) {
    if ([IO.File]::ReadAllText($lifecycleFile.FullName).Contains('Recovery.TryReadLatest(')) {
        $lifecycleViolations.Add("$($lifecycleFile.FullName): synchronous publication recovery I/O; use awaited TryReadLatestAsync instead.")
    }
}
$lifecyclePattern = '(?m)^\s*protected\s+override\s+void\s+(?:OnInitialized|OnParametersSet|OnAfterRender)\s*\('
foreach ($lifecycleFile in $lifecycleFiles) {
    $lifecycleText = [IO.File]::ReadAllText($lifecycleFile.FullName)
    if ([regex]::IsMatch($lifecycleText, $lifecyclePattern)) {
        $lifecycleViolations.Add("$($lifecycleFile.FullName): synchronous Blazor lifecycle override; use OnInitializedAsync, OnParametersSetAsync, or OnAfterRenderAsync.")
    }
}
if ($lifecycleViolations.Count -gt 0) {
    $lifecycleViolations | ForEach-Object { Write-Error $_ }
    Fail 'Synchronous component lifecycle overrides are forbidden; preserve task-based UI/service continuation ownership.'
}
$interopViolations = [Collections.Generic.List[string]]::new()
# .NET balanced groups constrain inspection to the actual lifecycle method body.
# A fixed-length lookahead was crossing into OnAfterRenderAsync and falsely
# treating legitimate post-render browser calls as prerender initialization.
$initializationMethodPattern = '(?s)\b(?:protected|private|public|internal)\s+(?:(?:override|virtual|sealed|async|new)\s+)*(?:void|Task|ValueTask)\s+OnInitialized(?:Async)?\s*\(\s*\)\s*(?<body>\{(?>[^{}]+|\{(?<depth>)|\}(?<-depth>))*(?(depth)(?!))\})'
foreach ($file in $lifecycleFiles) {
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -notmatch '(?is)@rendermode\s+(?:InteractiveServer|InteractiveAuto)') { continue }
    foreach ($method in [regex]::Matches($text, $initializationMethodPattern)) {
        $body = $method.Groups['body'].Value
        if ($body -match '\b(?:JS|JSRuntime|JsRuntime|_jsRuntime|_js)\s*\.\s*Invoke(?:Void)?Async\s*(?:<[^>]+>)?\s*\(') {
            $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
            $interopViolations.Add("${relative}: JS interop inside $($method.Value.Substring(0,[Math]::Min(90,$method.Value.Length)).Trim()) is forbidden during prerender initialization.")
        }
    }
}
if ($interopViolations.Count) { $interopViolations | ForEach-Object { Write-Error $_ }; Fail 'InteractiveServer prerender interop ownership failed.' }
Write-Host "PowerShell component safety passed: $($razorFiles.Count) typed loggers; the separate diagnostics baseline and prerender boundary are verified."
