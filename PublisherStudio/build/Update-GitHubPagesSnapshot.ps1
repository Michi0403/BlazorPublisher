param(
    [string]$DocumentationRoot = "",
    [string]$OutputArchive = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DocumentationRoot)) {
    $releaseRoot = Join-Path $root "src\PublisherStudio.Web\bin\Release\net10.0\wwwroot\help-docs"
    $debugRoot = Join-Path $root "src\PublisherStudio.Web\bin\Debug\net10.0\wwwroot\help-docs"
    $DocumentationRoot = if (Test-Path -LiteralPath $releaseRoot -PathType Container) { $releaseRoot } else { $debugRoot }
}
$DocumentationRoot = [IO.Path]::GetFullPath($DocumentationRoot)
if (-not (Test-Path -LiteralPath $DocumentationRoot -PathType Container)) {
    throw "Generated PublisherStudio documentation was not found: $DocumentationRoot"
}

if ([string]::IsNullOrWhiteSpace($OutputArchive)) {
    $OutputArchive = Join-Path $root ".github\pages\publisherstudio-kawaii-docs.zip"
}
$OutputArchive = [IO.Path]::GetFullPath($OutputArchive)
$validator = Join-Path $root ".github\scripts\prepare-pages-artifact.py"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "GitHub Pages validator was not found: $validator"
}

$temporaryValidationRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-" + [Guid]::NewGuid().ToString("N"))
try {
    & python $validator --source $DocumentationRoot --output $temporaryValidationRoot
    if ($LASTEXITCODE -ne 0) { throw "Generated PublisherStudio documentation did not pass the GitHub Pages validator." }

    $archiveDirectory = Split-Path -Parent $OutputArchive
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
    Remove-Item -LiteralPath $OutputArchive -Force -ErrorAction SilentlyContinue

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::Open($OutputArchive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $sourceRoot = $DocumentationRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        foreach ($file in @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName)) {
            $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $file.FullName,
                $relative,
                [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }

    Remove-Item -LiteralPath $temporaryValidationRoot -Recurse -Force -ErrorAction SilentlyContinue
    & python $validator --archive $OutputArchive --output $temporaryValidationRoot
    if ($LASTEXITCODE -ne 0) { throw "The tracked PublisherStudio GitHub Pages snapshot did not pass final validation." }
    Write-Host "Updated tracked PublisherStudio GitHub Pages snapshot: $OutputArchive" -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $temporaryValidationRoot -Recurse -Force -ErrorAction SilentlyContinue
}
