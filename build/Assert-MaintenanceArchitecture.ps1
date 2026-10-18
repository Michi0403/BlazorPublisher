param([string]$RepositoryRoot = '')
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

$ErrorActionPreference = 'Stop'
$issues = [Collections.Generic.List[string]]::new()
$source = [IO.Path]::GetFullPath($RepositoryRoot)
# Overlaying a new source ZIP does not delete retired 4.1.9 files. Preserve them
# under repository-local artifacts before checking the absolute no-Python rule.
& (Join-Path $PSScriptRoot 'Migrate-ObsoletePythonMaintenance.ps1') -RepositoryRoot $source
$buildFiles = @(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {
    $_.FullName -notmatch '[\\/](?:bin|obj|artifacts|node_modules|\.git)[\\/]' -and $_.Extension -in @('.ps1','.psm1','.py')
})
foreach ($file in $buildFiles) {
    $relative = $file.FullName.Substring($source.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
    if ($file.Extension -eq '.py') { $issues.Add("Python maintenance dependency must not be shipped: $relative"); continue }
    $tokens = $null; $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$parseErrors)
    foreach ($errorItem in @($parseErrors)) { $issues.Add("${relative}:$($errorItem.Extent.StartLineNumber): $($errorItem.Message)") }
}
foreach ($entry in @('Build-Release.ps1','Build-LocalDevelopment.ps1','build/Assert-PagesArtifact.ps1','build/Assert-CrossPlatformBoundaries.ps1','build/Assert-XmlDocumentationCoverage.ps1','build/Assert-RazorMaintenanceArchitecture.ps1','build/Assert-TransientUiStateOwnership.ps1','build/Assert-ServiceMethodResilience.ps1','build/Migrate-ObsoletePythonMaintenance.ps1','build/obsolete-python-maintenance.sha256')) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $entry) -PathType Leaf)) { $issues.Add("Missing required PowerShell build contract: $entry") }
}
foreach ($file in $buildFiles | Where-Object { $_.Extension -in @('.ps1','.psm1') -and $_.FullName -match '[\\/](?:build|\.github)[\\/]' }) {
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($file.Name -eq 'Assert-MaintenanceArchitecture.ps1') { continue }
    if ($text -match '(?i)Invoke-PublisherStudioPythonScript|Resolve-PublisherStudioPythonRuntime|Get-Command\s+(?:python|python3)|audit_[\w_]+\.py|prepare-pages-artifact\.py') {
        $issues.Add("Active maintenance PowerShell script still references Python: $($file.FullName)")
    }
}
if ($issues.Count) { $issues | ForEach-Object { Write-Error $_ }; throw "PublisherStudio PowerShell-only maintenance inventory found $($issues.Count) issue(s)." }
Write-Host "PublisherStudio PowerShell-only maintenance validated $($buildFiles.Count) script(s), parser errors=0, Python files=0."
