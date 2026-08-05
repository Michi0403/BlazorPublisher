param(
    [string]$DocumentationRoot = "",
    [string]$OutputArchive = "",
    [string]$BranchPagesRoot = ""
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

if ([string]::IsNullOrWhiteSpace($BranchPagesRoot)) {
    $BranchPagesRoot = Join-Path $repositoryRoot "docs"
}
$BranchPagesRoot = [IO.Path]::GetFullPath($BranchPagesRoot)

$validator = Join-Path $repositoryRoot ".github\scripts\prepare-pages-artifact.py"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Repository-root GitHub Pages validator was not found: $validator"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($null -eq $python) { throw "Python was not found. Python 3 is required to validate the GitHub Pages snapshot." }

$operationId = [Guid]::NewGuid().ToString("N")
$temporaryPreparedRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-Prepared-" + $operationId)
$temporaryVerificationRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-Verify-" + $operationId)
$temporaryArchive = "$OutputArchive.$operationId.tmp"
$branchStage = Join-Path $repositoryRoot (".publisherstudio-pages-stage-" + $operationId)
$archiveBackup = "$OutputArchive.$operationId.backup"
$branchBackup = Join-Path $repositoryRoot (".publisherstudio-pages-backup-" + $operationId)
$archiveInstalled = $false
$branchInstalled = $false

try {
    # Prepare a repaired, validated copy first. The source tree is never packaged directly.
    & $python.Source $validator --source $DocumentationRoot --output $temporaryPreparedRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Generated PublisherStudio documentation did not pass the GitHub Pages validator for version $expectedVersion."
    }

    # Runtime deployment metadata contains machine-local paths and is generated again by Actions.
    Remove-Item -LiteralPath (Join-Path $temporaryPreparedRoot "github-pages-deployment.json") -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path -LiteralPath (Join-Path $temporaryPreparedRoot ".nojekyll") -PathType Leaf)) {
        [IO.File]::WriteAllText((Join-Path $temporaryPreparedRoot ".nojekyll"), [string]::Empty, [Text.UTF8Encoding]::new($false))
    }

    $archiveDirectory = Split-Path -Parent $OutputArchive
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null

    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $sourceRoot = $temporaryPreparedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Prepared PublisherStudio documentation directory is empty: $sourceRoot" }

    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
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

    & $python.Source $validator --archive $temporaryArchive --output $temporaryVerificationRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) {
        throw "The new PublisherStudio GitHub Pages snapshot did not pass final validation for version $expectedVersion."
    }

    # Keep a branch-publishing mirror as a no-Jekyll fallback. This makes the repository
    # work even when GitHub Pages is still configured for /docs instead of GitHub Actions.
    Remove-Item -LiteralPath $branchStage -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $branchStage -Force | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $temporaryPreparedRoot -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $branchStage -Recurse -Force
    }
    if (-not (Test-Path -LiteralPath (Join-Path $branchStage "index.html") -PathType Leaf)) {
        throw "Prepared branch Pages mirror does not contain index.html."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $branchStage ".nojekyll") -PathType Leaf)) {
        throw "Prepared branch Pages mirror does not contain .nojekyll."
    }

    # Replace archive and /docs mirror as one rollback-capable publication transaction.
    Remove-Item -LiteralPath $archiveBackup -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $branchBackup -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $OutputArchive -PathType Leaf) {
        [IO.File]::Move($OutputArchive, $archiveBackup)
    }
    if (Test-Path -LiteralPath $BranchPagesRoot -PathType Container) {
        [IO.Directory]::Move($BranchPagesRoot, $branchBackup)
    }

    try {
        [IO.File]::Move($temporaryArchive, $OutputArchive)
        $archiveInstalled = $true
        [IO.Directory]::Move($branchStage, $BranchPagesRoot)
        $branchInstalled = $true
    }
    catch {
        if ($branchInstalled -and (Test-Path -LiteralPath $BranchPagesRoot -PathType Container)) {
            Remove-Item -LiteralPath $BranchPagesRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $branchBackup -PathType Container) {
            [IO.Directory]::Move($branchBackup, $BranchPagesRoot)
        }
        if ($archiveInstalled -and (Test-Path -LiteralPath $OutputArchive -PathType Leaf)) {
            Remove-Item -LiteralPath $OutputArchive -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $archiveBackup -PathType Leaf) {
            [IO.File]::Move($archiveBackup, $OutputArchive)
        }
        throw
    }

    Remove-Item -LiteralPath $archiveBackup -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $branchBackup -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Updated PublisherStudio $expectedVersion GitHub Pages snapshot and repository /docs mirror." -ForegroundColor Green
    Write-Host "Snapshot: $OutputArchive" -ForegroundColor Green
    Write-Host "Branch mirror: $BranchPagesRoot" -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $temporaryPreparedRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryVerificationRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $branchStage -Recurse -Force -ErrorAction SilentlyContinue
    if (-not $archiveInstalled -and (Test-Path -LiteralPath $archiveBackup -PathType Leaf) -and -not (Test-Path -LiteralPath $OutputArchive -PathType Leaf)) {
        [IO.File]::Move($archiveBackup, $OutputArchive)
    }
    if (-not $branchInstalled -and (Test-Path -LiteralPath $branchBackup -PathType Container) -and -not (Test-Path -LiteralPath $BranchPagesRoot -PathType Container)) {
        [IO.Directory]::Move($branchBackup, $BranchPagesRoot)
    }
}
