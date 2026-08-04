Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Git source visibility validation failed: $Message" }

$root = Split-Path -Parent $PSScriptRoot
$ignorePath = Join-Path $root '.gitignore'
$protectedPaths = @(
    'Directory.Build.targets',
    'Build-LocalDevelopment.ps1',
    'Build-AllRuntimes.ps1',
    'Build-Release.ps1',
    'New-VerifiedSourcePackage.ps1',
    'build/Assert-GitSourceVisibility.ps1',
    'build/Assert-LoggingIntegrity.ps1',
    'build/Assert-OneWireArchitecture.ps1',
    'build/Assert-RuntimeValueOwnership.ps1',
    'build/Assert-LocalizationIntegrity.ps1',
    'build/Assert-OperationalDiagnostics.ps1',
    'build/Assert-InteractiveServerRenderModes.ps1',
    'build/Assert-AsyncContinuationPolicy.ps1',
    'build/Assert-ComponentDiagnostics.ps1',
    'build/Assert-MethodDiagnostics.ps1',
    'build/Assert-ApplicationStaticPolicy.ps1',
    'build/Assert-TextServiceOwnership.ps1',
    'build/Assert-IteratorExceptionPolicy.ps1',
    'build/Assert-SystemVariableInitialization.ps1',
    'build/Assert-PanelStudioInteractionLifecycle.ps1',
    'build/Assert-JavaScriptDiagnostics.ps1',
    'build/Assert-PublishConfiguration.ps1',
    'build/Assert-InstallerWorkflow.ps1',
    'build/Invoke-ArchitectureAudit.ps1',
    'build/audit_application_architecture.py',
    'build/logging-baseline.json',
    'build/runtime-value-ownership-baseline.json',
    'build/async-continuation-baseline.json',
    'build/component-diagnostics-baseline.json',
    'build/method-diagnostics-baseline.json',
    'build/application-static-baseline.json',
    'build/text-service-ownership-baseline.json',
    'build/iterator-exception-baseline.json',
    'build/system-variable-initialization-baseline.json',
    'build/javascript-diagnostics-files.sha256',
    'CHANGELOG-v2.0.2.md',
    'CHANGELOG-v2.0.3.md',
    'RELEASE.md',
    'docs/RUNTIME_VALUE_OWNERSHIP.md',
    'docs/PUBLISH_CONFIGURATION_POLICY.md',
    'tests/applicationArchitecturePolicy.test.mjs',
    'tests/final14RenderModeGuard.test.mjs',
    'tests/final16BuildGuardRegressions.test.mjs',
    'tests/final18GuardCompatibility.test.mjs',
    'tests/final28InstallerWorkflow.test.mjs',
    'tests/v203BuildPolicyCompatibility.test.mjs',
    'tests/installerResilience.test.mjs',
    'tests/mediaRecordingPreviewResilience.test.mjs',
    'tests/systemVariableInitialization.test.mjs',
    'src/PublisherStudio.Web/Localization/en-US.json',
    'src/PublisherStudio.Web/Localization/de-DE.json'
)
$requiredIgnoreRules = @(
    '!Directory.Build.targets',
    '!Build-LocalDevelopment.ps1',
    '!Build-Release.ps1',
    '!New-VerifiedSourcePackage.ps1',
    '!build/',
    '!build/*.ps1',
    '!build/*.json',
    '!build/*.sha256',
    '!tests/',
    '!tests/*.mjs',
    '!src/PublisherStudio.Web/Localization/',
    '!src/PublisherStudio.Web/Localization/*.json'
)

if (-not (Test-Path -LiteralPath $ignorePath -PathType Leaf)) { Fail "Missing $ignorePath" }
$ignoreLines = @([System.IO.File]::ReadAllLines($ignorePath) | ForEach-Object { $_.Trim() })
foreach ($rule in $requiredIgnoreRules) {
    if ($ignoreLines -cnotcontains $rule) { Fail "Required .gitignore protection rule is missing: $rule" }
}
foreach ($relative in $protectedPaths) {
    $nativeRelative = $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar.ToString())
    if (-not (Test-Path -LiteralPath (Join-Path $root $nativeRelative) -PathType Leaf)) { Fail "Protected source file is missing: $relative" }
}

$gitDirectory = Join-Path $root '.git'
$git = Get-Command git -ErrorAction SilentlyContinue
if ((Test-Path -LiteralPath $gitDirectory) -and $git) {
    foreach ($relative in $protectedPaths) {
        & $git.Source -C $root check-ignore --no-index --quiet -- $relative
        if ($LASTEXITCODE -eq 0) { Fail "Protected source file is ignored by Git: $relative" }
        if ($LASTEXITCODE -ne 1) { Fail "git check-ignore failed for $relative with exit code $LASTEXITCODE" }
    }
}
Write-Host "Git source visibility validation passed for $($protectedPaths.Count) PublisherStudio files."
