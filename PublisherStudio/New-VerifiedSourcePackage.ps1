param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts/source/PublisherStudio-v2.1.1-source.zip')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path -LiteralPath $PSScriptRoot).Path

$output = [IO.Path]::GetFullPath($OutputPath)
$directory = Split-Path -Parent $output
New-Item -ItemType Directory -Path $directory -Force | Out-Null
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
$stage = Join-Path ([IO.Path]::GetTempPath()) ("publisherstudio-source-" + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stage 'PublisherStudio'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
try {
    Get-ChildItem -LiteralPath $root -Recurse -File -Force |
        Where-Object {
            $relative = $_.FullName.Substring($root.Length).TrimStart([char[]]"\/").Replace('\', '/')
            $fileName = [IO.Path]::GetFileName($relative)
            $excludedDirectory = $relative -match '(^|/)(\.git|\.vs|\.cr|node_modules|artifacts|bin|obj|AppPackages|BundleArtifacts)(/|$)' -or $relative -match '^docs/(_site|input|api|_tools|_print-book)(/|$)'
            $excludedGeneratedVendor = $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/(devextreme-dist|devexpress-aspnetcore-spreadsheet|jquery)(/|$)' -or $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license(?:\.generated)?\.js$' -or $relative -match '^src/PublisherStudio\.Web/wwwroot/vendor/devextreme-license\.(?:meta\.json|version)$'
            $excludedFile = $fileName -match '\.(?:user|suo|db|pfx|snk|licx|download)$' -or $fileName -in @('.DS_Store', 'Thumbs.db')
            -not $excludedDirectory -and -not $excludedGeneratedVendor -and -not $excludedFile
        } |
        ForEach-Object {
            $relative = $_.FullName.Substring($root.Length).TrimStart([char[]]"\/")
            $destination = Join-Path $packageRoot $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $output, [IO.Compression.CompressionLevel]::Optimal, $false)
    $archive = [IO.Compression.ZipFile]::OpenRead($output)
    try {
        if ($archive.Entries.Count -eq 0) { throw 'PublisherStudio source archive is empty.' }
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\', '/').TrimStart('/')
            if (-not $name.StartsWith('PublisherStudio/', [StringComparison]::Ordinal)) { throw "Source archive entry '$name' is outside the PublisherStudio repository root." }
            if ($name.Split('/') -contains '..') { throw "Unsafe source archive entry '$name'." }
            if ($name -match '(^|/)(\.vs|\.cr|node_modules|artifacts|bin|obj)(/|$)' -or $name -match '\.(?:user|suo|db|pfx|snk|licx|download)$') { throw "Excluded source-package artifact leaked into the archive: $name" }
        }
    }
    finally { $archive.Dispose() }
}
finally {
    Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
}

$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$output.sha256" -Value "$hash  $([IO.Path]::GetFileName($output))" -Encoding ascii
Write-Host "PublisherStudio source package created: $output"
