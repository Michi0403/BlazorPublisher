param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$requiredRelativePaths = @(
    "docs/index.md",
    "docs/docfx.json",
    "docs/toc.yml",
    "docs/guide/toc.yml",
    "docs/pdf/toc.yml",
    "docs/pdf-cover.html",
    "docs/templates/publisherstudio/public/main.css",
    "docs/templates/publisherstudio/public/main.js",
    "docs/templates/publisherstudio/public/favicon.svg",
    "docs/templates/publisherstudio/public/logo.svg",
    "build/NodeRuntime.Common.ps1",
    ".github/scripts/prepare-pages-artifact.py"
)

$missing = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $requiredRelativePaths) {
    $nativeRelativePath = $relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    $candidate = Join-Path $RepositoryRoot $nativeRelativePath
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        $missing.Add($relativePath)
    }
}

if ($missing.Count -gt 0) {
    throw "The PublisherStudio source tree is incomplete. Missing required cross-platform documentation/build source file(s): $($missing -join ', ')"
}

Write-Host "PublisherStudio source preflight: $($requiredRelativePaths.Count) required source file(s) are present." -ForegroundColor Green
