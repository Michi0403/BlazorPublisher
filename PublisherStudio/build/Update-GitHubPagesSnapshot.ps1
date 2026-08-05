param(
    [string]$DocumentationRoot = "",
    [string]$OutputArchive = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $projectRoot
$projectFile = Join-Path $projectRoot "src\PublisherStudio.Web\PublisherStudio.Web.csproj"
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "PublisherStudio project file was not found: $projectFile"
}
$projectText = [IO.File]::ReadAllText($projectFile)
$versionMatch = [regex]::Match($projectText, '<Version>\s*(?<Version>[^<]+?)\s*</Version>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
if (-not $versionMatch.Success) { throw "PublisherStudio source version could not be resolved from $projectFile" }
$expectedVersion = $versionMatch.Groups['Version'].Value.Trim()

if ([string]::IsNullOrWhiteSpace($DocumentationRoot)) {
    $releaseRoot = Join-Path $projectRoot "src\PublisherStudio.Web\bin\Release\net10.0\wwwroot\help-docs"
    $debugRoot = Join-Path $projectRoot "src\PublisherStudio.Web\bin\Debug\net10.0\wwwroot\help-docs"
    $DocumentationRoot = if (Test-Path -LiteralPath $releaseRoot -PathType Container) { $releaseRoot } else { $debugRoot }
}
$DocumentationRoot = [IO.Path]::GetFullPath($DocumentationRoot)
if (-not (Test-Path -LiteralPath $DocumentationRoot -PathType Container)) {
    throw "Generated PublisherStudio documentation was not found: $DocumentationRoot"
}

if ([string]::IsNullOrWhiteSpace($OutputArchive)) {
    $OutputArchive = Join-Path $repositoryRoot ".github\pages\publisherstudio-kawaii-docs.zip"
}
$OutputArchive = [IO.Path]::GetFullPath($OutputArchive)
$validator = Join-Path $repositoryRoot ".github\scripts\prepare-pages-artifact.py"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Repository-root GitHub Pages validator was not found: $validator"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($null -eq $python) { throw "Python was not found. Python 3 is required to validate the GitHub Pages snapshot." }

$temporaryValidationRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-" + [Guid]::NewGuid().ToString("N"))
$temporaryArchive = "$OutputArchive.$([Guid]::NewGuid().ToString('N')).tmp"
try {
    & $python.Source $validator --source $DocumentationRoot --output $temporaryValidationRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) { throw "Generated PublisherStudio documentation did not pass the GitHub Pages validator for version $expectedVersion." }

    $archiveDirectory = Split-Path -Parent $OutputArchive
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null

    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $sourceRoot = $DocumentationRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Generated PublisherStudio documentation directory is empty: $sourceRoot" }

    $archive = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($relative) -or $relative.Split('/') -contains '..') {
                throw "Unsafe GitHub Pages snapshot source path: $($file.FullName)"
            }

            $written = $false
            $lastReadError = $null
            for ($attempt = 1; $attempt -le 4 -and -not $written; $attempt++) {
                $entry = $null
                try {
                    $input = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                    try {
                        $entry = $archive.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
                        $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                        $output = $entry.Open()
                        try { $input.CopyTo($output) }
                        finally { $output.Dispose() }
                    }
                    finally { $input.Dispose() }
                    $written = $true
                }
                catch {
                    $lastReadError = $_.Exception
                    if ($null -ne $entry) { try { $entry.Delete() } catch { } }
                    if ($attempt -lt 4) { Start-Sleep -Milliseconds (150 * $attempt) }
                }
            }
            if (-not $written) {
                throw "Could not add GitHub Pages file '$($file.FullName)' after 4 attempts: $($lastReadError.Message)"
            }
        }
    }
    finally { $archive.Dispose() }

    $verification = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
    try {
        $entries = @($verification.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) })
        if ($entries.Count -ne $files.Count) {
            throw "GitHub Pages archive entry count $($entries.Count) does not match source file count $($files.Count)."
        }
    }
    finally { $verification.Dispose() }

    Remove-Item -LiteralPath $temporaryValidationRoot -Recurse -Force -ErrorAction SilentlyContinue
    & $python.Source $validator --archive $temporaryArchive --output $temporaryValidationRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) { throw "The new PublisherStudio GitHub Pages snapshot did not pass final validation for version $expectedVersion." }

    Remove-Item -LiteralPath $OutputArchive -Force -ErrorAction SilentlyContinue
    [IO.File]::Move($temporaryArchive, $OutputArchive)
    Write-Host "Updated repository-root PublisherStudio $expectedVersion GitHub Pages snapshot: $OutputArchive" -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $temporaryValidationRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
}
