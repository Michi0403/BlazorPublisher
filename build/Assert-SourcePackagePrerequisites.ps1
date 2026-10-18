param(
    [string]$RepositoryRoot = '',
    [switch]$SkipNodeRuntime
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}


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
    "build/Assert-MaintenanceArchitecture.ps1",
    "build/Ensure-WireProtocolPackage.ps1",
    "build/Ensure-ReleasePackagingPackage.ps1",
    "build/NativeReleasePackaging.ps1",
    "build/Initialize-MacReleaseTrust.ps1",
    "build/Assert-PagesArtifact.ps1"
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
& (Join-Path $PSScriptRoot 'Assert-MaintenanceArchitecture.ps1') -RepositoryRoot $RepositoryRoot
if ($SkipNodeRuntime) {
    Write-Host 'PublisherStudio Node.js preflight skipped because this delegated release child reuses parent-prepared browser assets and documentation.' -ForegroundColor DarkCyan
}
else {
    . (Join-Path $PSScriptRoot 'NodeRuntime.Common.ps1')
    $nodeCacheRoot = Get-PublisherStudioDocumentationToolCacheRoot -FallbackRoot (Join-Path $RepositoryRoot ([IO.Path]::Combine('artifacts', '.documentation-tools')))
    $nodeRuntime = Resolve-PublisherStudioNodeRuntime `
        -CacheRoot $nodeCacheRoot `
        -Version '22.23.2' `
        -MinimumMajor 20 `
        -MaximumPreferredMajor 22 `
        -AllowProvisioning `
        -PreferCompatibleLts
    if ($null -eq $nodeRuntime) {
        throw 'PublisherStudio Node.js preflight could not resolve Node.js 20+.'
    }
    Write-Host "PublisherStudio Node.js preflight: using $($nodeRuntime.Version) from '$($nodeRuntime.Path)'." -ForegroundColor Green
}
