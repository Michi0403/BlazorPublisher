[CmdletBinding()]
param(
    [string]$Version = "1.0.2",
    [ValidateSet("Release", "Debug")][string]$Configuration = "Release",
    [string]$PackageDirectory = "",
    [string]$PackageUrl = "",
    [string]$LocalGptRepository = "",
    [switch]$ForceDownload,
    [switch]$PackageOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $repositoryRoot "packages"
}
$PackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory.Trim().Trim('"'))
$packageName = "LocalGPT.ReleasePackaging.$Version.nupkg"
$packagePath = Join-Path $PackageDirectory $packageName

function Test-ReleasePackagingPackage {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
        try {
            $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
            $hasNuspec = @($entryNames | Where-Object { $_ -like '*.nuspec' }).Count -gt 0
            $hasToolSettings = @($entryNames | Where-Object { $_ -like 'tools/*/any/DotnetToolSettings.xml' }).Count -gt 0
            $hasToolAssembly = @($entryNames | Where-Object { $_ -like 'tools/*/any/LocalGPT.ReleasePackaging.dll' }).Count -gt 0
            return $hasNuspec -and $hasToolSettings -and $hasToolAssembly
        }
        finally { $archive.Dispose() }
    }
    catch { return $false }
}

function Add-RepositoryCandidates {
    param(
        [string]$Repository,
        [System.Collections.Generic.List[string]]$Candidates
    )
    if ([string]::IsNullOrWhiteSpace($Repository)) { return }
    $cleanRepository = $Repository.Trim().Trim('"')
    if ([string]::IsNullOrWhiteSpace($cleanRepository)) { return }
    try { $cleanRepository = [System.IO.Path]::GetFullPath($cleanRepository) } catch { return }

    $Candidates.Add((Join-Path $cleanRepository ([IO.Path]::Combine('packages', $packageName))))
    $releaseRoot = Join-Path $cleanRepository ([IO.Path]::Combine('artifacts', 'release'))
    $Candidates.Add((Join-Path $releaseRoot $packageName))
    $Candidates.Add((Join-Path $releaseRoot ([IO.Path]::Combine('packaging', $packageName))))

    if (Test-Path -LiteralPath $releaseRoot -PathType Container) {
        foreach ($versionDirectory in @(Get-ChildItem -LiteralPath $releaseRoot -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending)) {
            $Candidates.Add((Join-Path $versionDirectory.FullName $packageName))
        }
    }
}

function Get-LockName {
    param([Parameter(Mandatory)][string]$Value)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant())
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($bytes) } finally { $sha.Dispose() }
    return 'PublisherStudio.ReleasePackaging.' + ([System.BitConverter]::ToString($hash, 0, 12).Replace('-', ''))
}

New-Item -ItemType Directory -Path $PackageDirectory -Force | Out-Null
$mutex = New-Object System.Threading.Mutex($false, (Get-LockName -Value $packagePath))
$hasLock = $false
try {
    try { $hasLock = $mutex.WaitOne([TimeSpan]::FromMinutes(3)) }
    catch [System.Threading.AbandonedMutexException] { $hasLock = $true }
    if (-not $hasLock) { throw "Timed out waiting for another restore to prepare $packageName." }

    # Recheck under the lock because another build may have populated the cache while this process waited.
    if (-not $ForceDownload -and (Test-ReleasePackagingPackage $packagePath)) {
        Write-Host "Using cached authoritative LocalGPT release-packaging package: $packagePath" -ForegroundColor DarkGreen
    }
    else {
        Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue
        $candidates = [System.Collections.Generic.List[string]]::new()
        if (-not $ForceDownload) {
            Add-RepositoryCandidates -Repository $LocalGptRepository -Candidates $candidates
            Add-RepositoryCandidates -Repository $env:LOCALGPT_REPOSITORY -Candidates $candidates

            # LocalGPT and BlazorPublisher are commonly checked out as sibling repositories.
            $siblingLocalGpt = Join-Path (Split-Path -Parent $repositoryRoot) 'LocalGPT'
            Add-RepositoryCandidates -Repository $siblingLocalGpt -Candidates $candidates

            $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
            if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
                $candidates.Add((Join-Path $localApplicationData ([IO.Path]::Combine('LocalGPT', 'NuGet', $packageName))))
            }

            foreach ($candidate in $candidates | Select-Object -Unique) {
                if (Test-ReleasePackagingPackage $candidate) {
                    $temporaryCopy = "$packagePath.copying"
                    Remove-Item -LiteralPath $temporaryCopy -Force -ErrorAction SilentlyContinue
                    Copy-Item -LiteralPath $candidate -Destination $temporaryCopy -Force
                    Move-Item -LiteralPath $temporaryCopy -Destination $packagePath -Force
                    Write-Host "Copied authoritative LocalGPT release-packaging package from $candidate" -ForegroundColor Cyan
                    break
                }
            }
        }

        if (-not (Test-ReleasePackagingPackage $packagePath)) {
            if ([string]::IsNullOrWhiteSpace($PackageUrl)) {
                $PackageUrl = "https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName"
            }

            Write-Host "Downloading authoritative LocalGPT release-packaging package $Version..." -ForegroundColor Cyan
            [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
            $temporaryPath = "$packagePath.download"
            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
            try {
                Invoke-WebRequest -Uri $PackageUrl -OutFile $temporaryPath -UseBasicParsing
                if (-not (Test-ReleasePackagingPackage $temporaryPath)) {
                    throw "The downloaded file is not a LocalGPT.ReleasePackaging $Version .NET tool package."
                }
                Move-Item -LiteralPath $temporaryPath -Destination $packagePath -Force
            }
            catch {
                Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
                throw @"
The authoritative LocalGPT release-packaging package could not be prepared.
Expected package: $packageName
Release URL: $PackageUrl

PublisherStudio intentionally does not carry or compile LocalGPT.ReleasePackaging source.
Build LocalGPT once so it places the authoritative package in the shared LocalGPT NuGet cache,
set LOCALGPT_REPOSITORY / pass -LocalGptRepository to a LocalGPT release checkout, or upload
$packageName as an asset of the current LocalGPT release.
Underlying error: $($_.Exception.Message)
"@
            }
        }
        Write-Host "Prepared authoritative LocalGPT release-packaging package: $packagePath" -ForegroundColor Green
    }
}
finally {
    if ($hasLock) { try { $mutex.ReleaseMutex() } catch { } }
    $mutex.Dispose()
}

if ($PackageOnly) {
    Write-Host "Prepared LocalGPT.ReleasePackaging package without installing the .NET tool because -PackageOnly was requested." -ForegroundColor DarkCyan
    Write-Output ([string][IO.Path]::GetFullPath($packagePath))
    return
}

# Install from the authoritative package only. NuGet.org remains enabled solely so the .NET tool
# can resolve its MIT-licensed PDFsharp dependency; PublisherStudio never compiles helper source.
$toolRoot = Join-Path $repositoryRoot ([IO.Path]::Combine('artifacts', 'release-tools'))
Remove-Item -LiteralPath $toolRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
$nugetConfig = Join-Path $toolRoot 'NuGet.ReleasePackaging.config'
$escapedPackages = [Security.SecurityElement]::Escape($PackageDirectory)
$nugetConfigText = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalReleasePackages" value="$escapedPackages" />
    <add key="NuGetOrg" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
[IO.File]::WriteAllText($nugetConfig, $nugetConfigText, (New-Object Text.UTF8Encoding($false)))

& dotnet tool install LocalGPT.ReleasePackaging --tool-path $toolRoot --version $Version --configfile $nugetConfig --ignore-failed-sources | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging tool installation failed." }
$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$commandName = if ($isWindowsHost) { 'localgpt-release-packaging.exe' } else { 'localgpt-release-packaging' }
$command = Join-Path $toolRoot $commandName
if (-not (Test-Path -LiteralPath $command -PathType Leaf)) { throw "Installed release-packaging tool was not found: $command" }
Write-Output ([string]$command)
