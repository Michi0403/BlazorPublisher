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
    'Update-GitHubPagesSnapshot.cmd',
    'build/Assert-GitSourceVisibility.ps1',
    'build/Build-Documentation.ps1',
    'build/Update-GitHubPagesSnapshot.ps1',
    'build/Add-XmlDocumentation.py',
    'build/Assert-XmlDocumentationCoverage.py',
    'build/Assert-XmlDocumentationCoverage.ps1',
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
    'CHANGELOG-v2.0.4.md',
    'CHANGELOG-v2.0.5.md',
    'CHANGELOG-v2.1.0.md',
    'CHANGELOG-v2.1.1.md',
    'CHANGELOG-v2.1.2.md',
    'CHANGELOG-v2.1.7.md',
    'DOCUMENTATION-PUBLISHING-R4.md',
    'AGENTS.md',
    'src/PublisherStudio.InstallerConsole/README.md',
    'RELEASE.md',
    '.config/dotnet-tools.json',
    '.github/workflows/publish-shipped-docs.yml',
    '.github/scripts/prepare-pages-artifact.py',
    '.github/pages/publisherstudio-kawaii-docs.zip',
    'docs/docfx.json',
    'docs/toc.yml',
    'docs/guide/toc.yml',
    'docs/pdf/toc.yml',
    'docs/pdf-cover.html',
    'docs/templates/publisherstudio/public/main.css',
    'docs/templates/publisherstudio/public/main.js',
    'docs/templates/publisherstudio/public/favicon.svg',
    'docs/templates/publisherstudio/public/favicon.ico',
    'docs/templates/publisherstudio/public/logo.svg',
    'docs/index.md',
    'docs/articles/getting-started.md',
    'docs/articles/editor-workspace.md',
    'docs/articles/stories-and-spreadsheets.md',
    'docs/articles/pictures-and-media.md',
    'docs/articles/animation-and-interaction.md',
    'docs/articles/streaming-and-recording.md',
    'docs/articles/publishing-and-export.md',
    'docs/articles/installer-and-updates.md',
    'docs/articles/localgpt-and-onewire.md',
    'docs/articles/privacy-and-security.md',
    'docs/articles/architecture.md',
    'docs/articles/developer-build.md',
    'docs/articles/troubleshooting.md',
    'docs/articles/documentation-system.md',
    'src/PublisherStudio.Web/BusinessObjects/DocumentationModels.cs',
    'src/PublisherStudio.Web/Services/Documentation/IPublisherDocumentationCatalogService.cs',
    'src/PublisherStudio.Web/Services/Documentation/PublisherDocumentationCatalogService.cs',
    'src/PublisherStudio.Web/Controllers/DocumentationController.cs',
    'src/PublisherStudio.Web/Components/Pages/Help.razor',
    'src/PublisherStudio.Web/Components/Pages/Help.razor.css',
    'tests/v210KawaiiDocumentation.test.mjs',
    'tests/v211LocalGptInstallerHealing.test.mjs',
    'tests/v213LocalGptParityRepair.test.mjs',
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
    '!Update-GitHubPagesSnapshot.cmd',
    '!build/',
    '!build/*.ps1',
    '!build/*.json',
    '!build/*.sha256',
    '!tests/',
    '!tests/*.mjs',
    '!src/PublisherStudio.Web/Localization/',
    '!src/PublisherStudio.Web/Localization/*.json',
    '!.github/pages/',
    '!.github/pages/publisherstudio-kawaii-docs.zip'
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
