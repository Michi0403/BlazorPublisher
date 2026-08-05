param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts/source/PublisherStudio-v2.1.9-source.zip')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $projectRoot)).Path

$output = [IO.Path]::GetFullPath($OutputPath)
$directory = Split-Path -Parent $output
New-Item -ItemType Directory -Path $directory -Force | Out-Null
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
$stage = Join-Path ([IO.Path]::GetTempPath()) ("publisherstudio-source-" + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stage 'BlazorPublisher'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
try {
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Force |
        Where-Object {
            $relative = $_.FullName.Substring($repositoryRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
            $fileName = [IO.Path]::GetFileName($relative)
            $excludedDirectory = $relative -match '(^|/)(\.git|\.vs|\.cr|__pycache__|node_modules|artifacts|bin|obj|AppPackages|BundleArtifacts|\.publisherstudio-pages-(?:stage|backup)-[^/]+)(/|$)' -or $relative -match '^PublisherStudio/docs/(_site|input|api|\.tools|\.print-book)(/|$)'
            $excludedGeneratedVendor = $relative -match '^PublisherStudio/src/PublisherStudio\.Web/wwwroot/vendor/(devextreme-dist|devexpress-aspnetcore-spreadsheet|jquery)(/|$)' -or $relative -match '^PublisherStudio/src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license(?:\.generated)?\.js$' -or $relative -match '^PublisherStudio/src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license\.(?:meta\.json|version)$' -or $relative -eq 'PublisherStudio/src/PublisherStudio.Web/wwwroot/vendor/devextreme-assets.meta.json'
            $excludedFile = $fileName -match '\.(?:user|suo|db|pfx|snk|licx|download|pyc)$' -or $fileName -in @('.DS_Store', 'Thumbs.db')
            -not $excludedDirectory -and -not $excludedGeneratedVendor -and -not $excludedFile
        } |
        ForEach-Object {
            $relative = $_.FullName.Substring($repositoryRoot.Length).TrimStart([char[]]"\/")
            $destination = Join-Path $packageRoot $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $output, [IO.Compression.CompressionLevel]::Optimal, $false)
    $archive = [IO.Compression.ZipFile]::OpenRead($output)
    try {
        if ($archive.Entries.Count -eq 0) { throw 'PublisherStudio source archive is empty.' }
        $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/').TrimStart('/') })
        foreach ($name in $names) {
            if (-not $name.StartsWith('BlazorPublisher/', [StringComparison]::Ordinal)) { throw "Source archive entry '$name' is outside the BlazorPublisher repository root." }
            if ($name.Split('/') -contains '..') { throw "Unsafe source archive entry '$name'." }
            if ($name -match '(^|/)(\.vs|\.cr|__pycache__|node_modules|artifacts|bin|obj|\.publisherstudio-pages-(?:stage|backup)-[^/]+)(/|$)' -or $name -match '\.(?:user|suo|db|pfx|snk|licx|download|pyc)$') { throw "Excluded source-package artifact leaked into the archive: $name" }
        }
        foreach ($required in @(
            'BlazorPublisher/README.md',
            'BlazorPublisher/RELEASE.md',
            'BlazorPublisher/.github/workflows/publish-shipped-docs.yml',
            'BlazorPublisher/.github/scripts/prepare-pages-artifact.py',
            'BlazorPublisher/.github/pages/publisherstudio-kawaii-docs.zip',
            'BlazorPublisher/docs/.nojekyll',
            'BlazorPublisher/docs/index.html',
            'BlazorPublisher/docs/documentation-status.json',
            'BlazorPublisher/PublisherStudio/build/Repair-DocfxNamespacePages.ps1',
            'BlazorPublisher/PublisherStudio/Build-Release.ps1'
        )) {
            if ($names -notcontains $required) { throw "Required source-package file is missing: $required" }
        }
    }
    finally { $archive.Dispose() }
}
finally {
    Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
}

$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$output.sha256", "$hash  $([IO.Path]::GetFileName($output))`r`n", [Text.Encoding]::ASCII)
Write-Host "PublisherStudio source package created: $output"
