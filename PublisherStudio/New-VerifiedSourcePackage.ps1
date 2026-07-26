param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts/source/PublisherStudio-v1.0.89-source.zip')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path -LiteralPath $PSScriptRoot).Path

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) { throw 'Node.js is required for the source-closure tests.' }
Push-Location (Join-Path $root 'src/PublisherStudio.Web')
try {
    & node ../../tests/csharpCompilationSafety.test.mjs
    if ($LASTEXITCODE -ne 0) { throw 'C# source-closure test failed.' }
    & node ../../tests/organicPluginOneWire.test.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Organic WireProtocolVersion test failed.' }
}
finally { Pop-Location }

$output = [IO.Path]::GetFullPath($OutputPath)
$directory = Split-Path -Parent $output
New-Item -ItemType Directory -Path $directory -Force | Out-Null
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
$stage = Join-Path ([IO.Path]::GetTempPath()) ("publisherstudio-source-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    Get-ChildItem -LiteralPath $root -Recurse -File -Force |
        Where-Object {
            $relative = [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
            $relative -notmatch '(^|/)(\.git|\.vs|\.cr|node_modules|artifacts|bin|obj|AppPackages|BundleArtifacts)(/|$)'
        } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
            $destination = Join-Path $stage $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $output, [IO.Compression.CompressionLevel]::Optimal, $false)
}
finally {
    Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
}

$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$output.sha256" -Value "$hash  $([IO.Path]::GetFileName($output))" -Encoding ascii
Write-Host "Verified PublisherStudio source package created: $output"
