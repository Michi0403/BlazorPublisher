param(
    [string]$DocumentationRoot = "",
    [string]$OutputArchive = "",
    [switch]$AllowMissingPdf
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repositoryRoot 'src/PublisherStudio.Web/PublisherStudio.Web.csproj'
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "PublisherStudio project file was not found: $projectFile"
}
$projectText = [IO.File]::ReadAllText($projectFile)
$versionMatch = [regex]::Match($projectText, '<Version>\s*(?<Version>[^<]+?)\s*</Version>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
if (-not $versionMatch.Success) { throw "PublisherStudio source version could not be resolved from $projectFile" }
$expectedVersion = $versionMatch.Groups['Version'].Value.Trim()

function Get-PublisherStudioDocumentationVersion {
    param([Parameter(Mandatory = $true)][string]$Root)

    $statusPath = Join-Path $Root 'documentation-status.json'
    if (-not (Test-Path -LiteralPath $statusPath -PathType Leaf)) { return [string]::Empty }
    try {
        $statusText = [IO.File]::ReadAllText($statusPath)
        $statusVersionMatch = [regex]::Match(
            $statusText,
            '"(?:version|Version)"\s*:\s*"(?<Version>[^"]+)"',
            [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($statusVersionMatch.Success) { return $statusVersionMatch.Groups['Version'].Value.Trim() }
    }
    catch {
        Write-Warning "Could not inspect documentation status at '$statusPath': $($_.Exception.Message)"
    }
    return [string]::Empty
}

if ([string]::IsNullOrWhiteSpace($DocumentationRoot)) {
    $candidateRoots = @(
        (Join-Path $repositoryRoot 'src/PublisherStudio.Web/bin/Release/net10.0/wwwroot/help-docs'),
        (Join-Path $repositoryRoot 'src/PublisherStudio.Web/bin/Debug/net10.0/wwwroot/help-docs'),
        (Join-Path $repositoryRoot 'src/PublisherStudio.Web/wwwroot/help-docs')
    )
    $matchingRoots = @()
    $detectedRoots = [System.Collections.Generic.List[string]]::new()
    foreach ($candidateRoot in $candidateRoots) {
        if (-not (Test-Path -LiteralPath $candidateRoot -PathType Container)) { continue }
        $candidateVersion = Get-PublisherStudioDocumentationVersion -Root $candidateRoot
        $displayVersion = if ([string]::IsNullOrWhiteSpace($candidateVersion)) { '<unknown>' } else { $candidateVersion }
        $detectedRoots.Add("$candidateRoot => $displayVersion")
        if ([string]::Equals($candidateVersion, $expectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
            $statusPath = Join-Path $candidateRoot 'documentation-status.json'
            $matchingRoots += [pscustomobject]@{
                Root = $candidateRoot
                LastWriteTimeUtc = (Get-Item -LiteralPath $statusPath).LastWriteTimeUtc
            }
        }
    }

    if ($matchingRoots.Count -eq 0) {
        $detected = if ($detectedRoots.Count -eq 0) { 'no generated Debug, Release, or source-web documentation output exists' } else { $detectedRoots -join '; ' }
        throw "No generated PublisherStudio documentation matching source version $expectedVersion was found. Build PublisherStudio first. Detected: $detected"
    }

    $DocumentationRoot = ($matchingRoots | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).Root
    Write-Host "Selected version-matched PublisherStudio documentation output: $DocumentationRoot" -ForegroundColor Cyan
}
$DocumentationRoot = [IO.Path]::GetFullPath($DocumentationRoot)
if (-not (Test-Path -LiteralPath $DocumentationRoot -PathType Container)) {
    throw "Generated PublisherStudio documentation was not found: $DocumentationRoot"
}
$versionedDocumentationPdfs = @(Get-ChildItem -LiteralPath $DocumentationRoot -File -Filter 'PublisherStudio-*.pdf' -ErrorAction SilentlyContinue)
$expectedDocumentationPdf = 'PublisherStudio-' + $expectedVersion + '.pdf'
$foundPdfNames = @($versionedDocumentationPdfs | ForEach-Object { $_.Name })

if ($versionedDocumentationPdfs.Count -eq 0 -and $AllowMissingPdf) {
    $statusPath = Join-Path $DocumentationRoot 'documentation-status.json'
    if (-not (Test-Path -LiteralPath $statusPath -PathType Leaf)) {
        throw "Generated PublisherStudio documentation is missing documentation-status.json: $DocumentationRoot"
    }
    $status = [IO.File]::ReadAllText($statusPath) | ConvertFrom-Json
    $pdfAvailableProperty = $status.PSObject.Properties['pdfAvailable']
    if ($null -eq $pdfAvailableProperty -or [bool]$pdfAvailableProperty.Value) {
        throw "PublisherStudio documentation omitted '$expectedDocumentationPdf' without declaring pdfAvailable=false."
    }

    $validator = Join-Path $repositoryRoot ".github/scripts/prepare-pages-artifact.py"
    if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
        throw "Repository-root GitHub Pages validator was not found: $validator"
    }
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
    if ($null -eq $python) { throw "Python 3 is required to validate the GitHub Pages documentation." }
    & $python.Source $validator --source $DocumentationRoot --html-only
    if ($LASTEXITCODE -ne 0) {
        throw "Generated PublisherStudio HTML documentation failed validation for version $expectedVersion."
    }
    Write-Host "Validated PublisherStudio $expectedVersion HTML documentation. A PDF was not required for this build, so the tracked Pages release snapshot was left unchanged." -ForegroundColor Green
    return
}

if ($versionedDocumentationPdfs.Count -ne 1 -or -not [string]::Equals($versionedDocumentationPdfs[0].Name, $expectedDocumentationPdf, [StringComparison]::OrdinalIgnoreCase)) {
    $found = if ($foundPdfNames.Count -eq 0) { '<none>' } else { $foundPdfNames -join ', ' }
    throw "PublisherStudio Pages source must contain exactly one current versioned PDF '$expectedDocumentationPdf'. Found: $found"
}

if ([string]::IsNullOrWhiteSpace($OutputArchive)) {
    $OutputArchive = Join-Path $repositoryRoot '.github/pages/publisherstudio-kawaii-docs.zip'
}
$OutputArchive = [IO.Path]::GetFullPath($OutputArchive)
$validator = Join-Path $repositoryRoot ".github/scripts/prepare-pages-artifact.py"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Repository-root GitHub Pages validator was not found: $validator"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
if ($null -eq $python) { throw "Python 3 is required to validate the GitHub Pages snapshot." }

$operationId = [Guid]::NewGuid().ToString("N")
$preparedRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-Prepared-" + $operationId)
$verificationRoot = Join-Path ([IO.Path]::GetTempPath()) ("PublisherStudio-Pages-Verify-" + $operationId)
$temporaryArchive = "$OutputArchive.$operationId.tmp"
$backupArchive = "$OutputArchive.$operationId.backup"
$installed = $false

try {
    & $python.Source $validator --source $DocumentationRoot --output $preparedRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Generated PublisherStudio documentation did not pass the GitHub Pages validator for version $expectedVersion."
    }
    Remove-Item -LiteralPath (Join-Path $preparedRoot "github-pages-deployment.json") -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path -LiteralPath (Join-Path $preparedRoot ".nojekyll") -PathType Leaf)) {
        [IO.File]::WriteAllText((Join-Path $preparedRoot ".nojekyll"), [string]::Empty, [Text.UTF8Encoding]::new($false))
    }

    $archiveDirectory = Split-Path -Parent $OutputArchive
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $sourceRoot = $preparedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Prepared documentation directory is empty: $sourceRoot" }

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
                        try { $input.CopyTo($output) } finally { $output.Dispose() }
                    } finally { $input.Dispose() }
                    $written = $true
                } catch {
                    $lastReadError = $_.Exception
                    if ($null -ne $entry) { try { $entry.Delete() } catch { } }
                    if ($attempt -lt 4) { Start-Sleep -Milliseconds (150 * $attempt) }
                }
            }
            if (-not $written) { throw "Could not add '$($file.FullName)' after 4 attempts: $($lastReadError.Message)" }
        }
    } finally { $archive.Dispose() }

    & $python.Source $validator --archive $temporaryArchive --output $verificationRoot --expected-version $expectedVersion
    if ($LASTEXITCODE -ne 0) { throw "The new PublisherStudio Pages snapshot failed final validation for version $expectedVersion." }

    $sourceApiIndex = Join-Path $DocumentationRoot 'api/index.html'
    $verifiedApiIndex = Join-Path $verificationRoot 'api/index.html'
    if (-not (Test-Path -LiteralPath $sourceApiIndex -PathType Leaf) -or -not (Test-Path -LiteralPath $verifiedApiIndex -PathType Leaf)) {
        throw "PublisherStudio Pages snapshot API entry point disappeared during snapshot preparation."
    }
    $sourceApiHash = (Get-FileHash -LiteralPath $sourceApiIndex -Algorithm SHA256).Hash
    $verifiedApiHash = (Get-FileHash -LiteralPath $verifiedApiIndex -Algorithm SHA256).Hash
    if (-not [string]::Equals($sourceApiHash, $verifiedApiHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "PublisherStudio Pages snapshot api/index.html differs from the generated DocFX source."
    }
    $sourceApiCount = @(Get-ChildItem -LiteralPath (Join-Path $DocumentationRoot 'api') -Filter '*.html' -File -Recurse).Count
    $verifiedApiCount = @(Get-ChildItem -LiteralPath (Join-Path $verificationRoot 'api') -Filter '*.html' -File -Recurse).Count
    if ($sourceApiCount -ne $verifiedApiCount -or $verifiedApiCount -le 1) {
        throw "PublisherStudio Pages snapshot API page count changed during preparation: source=$sourceApiCount verified=$verifiedApiCount"
    }
    Write-Host "Verified PublisherStudio Pages API tree byte-for-byte at entry point and page-count parity ($verifiedApiCount HTML pages)." -ForegroundColor Green

    Remove-Item -LiteralPath $backupArchive -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $OutputArchive -PathType Leaf) { [IO.File]::Move($OutputArchive, $backupArchive) }
    try {
        [IO.File]::Move($temporaryArchive, $OutputArchive)
        $installed = $true
    } catch {
        if (Test-Path -LiteralPath $backupArchive -PathType Leaf) { [IO.File]::Move($backupArchive, $OutputArchive) }
        throw
    }
    Remove-Item -LiteralPath $backupArchive -Force -ErrorAction SilentlyContinue
    Write-Host "Updated the single PublisherStudio $expectedVersion GitHub Pages snapshot: $OutputArchive" -ForegroundColor Green
} finally {
    Remove-Item -LiteralPath $preparedRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $verificationRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    if (-not $installed -and (Test-Path -LiteralPath $backupArchive -PathType Leaf) -and -not (Test-Path -LiteralPath $OutputArchive -PathType Leaf)) {
        [IO.File]::Move($backupArchive, $OutputArchive)
    }
}
