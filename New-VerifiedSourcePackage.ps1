param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
& (Join-Path $repositoryRoot 'build\Assert-PowerShellCompatibility.ps1')

[xml]$project = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src\PublisherStudio.Web\PublisherStudio.Web.csproj') -Raw
$version = @(
    $project.Project.PropertyGroup |
        ForEach-Object { [string]$_.Version } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
) | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace([string]$version)) { throw 'PublisherStudio version could not be resolved.' }
$version = ([string]$version).Trim()

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot "artifacts\source\BlazorPublisher-v$version-source.zip"
}
$output = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $output
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue

$stage = Join-Path ([IO.Path]::GetTempPath()) ("publisherstudio-source-" + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stage 'BlazorPublisher'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
try {
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Force |
        Where-Object {
            $relative = $_.FullName.Substring($repositoryRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
            $fileName = [IO.Path]::GetFileName($relative)
            $excludedDirectory = $relative -match '(^|/)(\.git|\.vs|\.cr|__pycache__|node_modules|artifacts|bin|obj|AppPackages|BundleArtifacts)(/|$)' -or
                $relative -match '^docs/(input|api|_site|\.tools|\.print-book)(/|$)' -or
                $relative -match '^src/PublisherStudio\.Web/wwwroot/help-docs(/|$)'
            $excludedGeneratedVendor = $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/(devextreme-dist|devexpress-aspnetcore-spreadsheet|jquery)(/|$)' -or
                $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license(?:\.generated)?\.js$' -or
                $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license\.(?:meta\.json|version)$' -or
                $relative -eq 'src/PublisherStudio.Web/wwwroot/vendor/devextreme-assets.meta.json'
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
            if ($name -match '(^|/)(\.vs|\.cr|__pycache__|node_modules|artifacts|bin|obj)(/|$)' -or $name -match '\.(?:user|suo|db|pfx|snk|licx|download|pyc)$') {
                throw "Excluded source-package artifact leaked into the archive: $name"
            }
        }
        foreach ($required in @(
            'BlazorPublisher/AGENTS.md',
            'BlazorPublisher/Build-Release.ps1',
            'BlazorPublisher/Directory.Build.targets',
            'BlazorPublisher/.config/dotnet-tools.json',
            'BlazorPublisher/.github/workflows/publish-shipped-docs.yml',
            'BlazorPublisher/.github/scripts/prepare-pages-artifact.py',
            'BlazorPublisher/build/Build-Documentation.ps1',
            'BlazorPublisher/build/Update-GitHubPagesSnapshot.ps1',
            'BlazorPublisher/src/PublisherStudio.sln'
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
