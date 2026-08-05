[CmdletBinding()]
param(
    [string]$Version = "2.1.1",
    [string]$PackageDirectory = "",
    [string]$PackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$ForceDownload
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot "packages"
}
$PackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory.Trim().Trim('"'))
$packageName = "LocalGPT.WireProtocolVersion.$Version.nupkg"
$packagePath = Join-Path $PackageDirectory $packageName

function Test-WireProtocolPackage {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
        try {
            $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
            $hasAssembly = $entryNames -contains 'lib/net10.0/LocalGPT.WireProtocolVersion.dll'
            $hasNuspec = @($entryNames | Where-Object { $_ -like '*.nuspec' }).Count -gt 0
            return $hasAssembly -and $hasNuspec
        }
        finally { $archive.Dispose() }
    }
    catch { return $false }
}

function Add-RepositoryCandidates {
    param([string]$Repository, [System.Collections.Generic.List[string]]$Candidates)
    if ([string]::IsNullOrWhiteSpace($Repository)) { return }
    $cleanRepository = $Repository.Trim().Trim('"')
    if ([string]::IsNullOrWhiteSpace($cleanRepository)) { return }
    try { $cleanRepository = [System.IO.Path]::GetFullPath($cleanRepository) } catch { return }
    $Candidates.Add((Join-Path $cleanRepository "artifacts\release\$packageName"))
    $Candidates.Add((Join-Path $cleanRepository "packages\$packageName"))
}

function Get-LockName {
    param([Parameter(Mandatory)][string]$Value)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant())
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($bytes) } finally { $sha.Dispose() }
    return 'PublisherStudio.WireProtocol.' + ([System.BitConverter]::ToString($hash, 0, 12).Replace('-', ''))
}

New-Item -ItemType Directory -Path $PackageDirectory -Force | Out-Null
$mutex = New-Object System.Threading.Mutex($false, (Get-LockName -Value $packagePath))
$hasLock = $false
try {
    try { $hasLock = $mutex.WaitOne([TimeSpan]::FromMinutes(3)) }
    catch [System.Threading.AbandonedMutexException] { $hasLock = $true }
    if (-not $hasLock) { throw "Timed out waiting for another restore to prepare $packageName." }

    # A parallel restore may have completed while this invocation waited. Recheck under the lock.
    if (-not $ForceDownload -and (Test-WireProtocolPackage $packagePath)) {
        Write-Host "Using cached authoritative LocalGPT protocol package: $packagePath" -ForegroundColor DarkGreen
        return $packagePath
    }
    Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue

    $candidates = [System.Collections.Generic.List[string]]::new()
    Add-RepositoryCandidates -Repository $LocalGptRepository -Candidates $candidates
    Add-RepositoryCandidates -Repository $env:LOCALGPT_REPOSITORY -Candidates $candidates

    $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        $candidates.Add((Join-Path $localApplicationData "LocalGPT\NuGet\$packageName"))
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-WireProtocolPackage $candidate) {
            $temporaryCopy = "$packagePath.copying"
            Remove-Item -LiteralPath $temporaryCopy -Force -ErrorAction SilentlyContinue
            Copy-Item -LiteralPath $candidate -Destination $temporaryCopy -Force
            Move-Item -LiteralPath $temporaryCopy -Destination $packagePath -Force
            Write-Host "Copied authoritative LocalGPT protocol package from $candidate" -ForegroundColor Cyan
            return $packagePath
        }
    }

    if ([string]::IsNullOrWhiteSpace($PackageUrl)) {
        $PackageUrl = "https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName"
    }

    Write-Host "Downloading authoritative LocalGPT protocol package $Version..." -ForegroundColor Cyan
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    $temporaryPath = "$packagePath.download"
    Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    try {
        Invoke-WebRequest -Uri $PackageUrl -OutFile $temporaryPath -UseBasicParsing
        if (-not (Test-WireProtocolPackage $temporaryPath)) {
            throw "The downloaded file is not a DLL-backed LocalGPT.WireProtocolVersion $Version NuGet package."
        }
        Move-Item -LiteralPath $temporaryPath -Destination $packagePath -Force
    }
    catch {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        throw @"
The authoritative LocalGPT protocol package could not be prepared.
Expected package: $packageName
Default release URL: $PackageUrl

Build LocalGPT once with Build-Release.cmd, set LOCALGPT_REPOSITORY to that checkout,
pass -LocalGptRepository, or upload the package as an asset of the current LocalGPT GitHub release.
Underlying error: $($_.Exception.Message)
"@
    }

    Write-Host "Prepared authoritative LocalGPT protocol package: $packagePath" -ForegroundColor Green
    return $packagePath
}
finally {
    if ($hasLock) { try { $mutex.ReleaseMutex() } catch { } }
    $mutex.Dispose()
}
