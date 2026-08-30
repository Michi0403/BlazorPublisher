[CmdletBinding()]
param(
    [string]$Version = "1.0.1",
    [ValidateSet("Release", "Debug")][string]$Configuration = "Release",
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
        [System.Collections.Generic.List[string]]$Candidates,
        [System.Collections.Generic.List[string]]$Repositories
    )
    if ([string]::IsNullOrWhiteSpace($Repository)) { return }
    $cleanRepository = $Repository.Trim().Trim('"')
    if ([string]::IsNullOrWhiteSpace($cleanRepository)) { return }
    try { $cleanRepository = [System.IO.Path]::GetFullPath($cleanRepository) } catch { return }
    if ($null -ne $Repositories) { $Repositories.Add($cleanRepository) }
    $Candidates.Add((Join-Path $cleanRepository ([IO.Path]::Combine('artifacts', 'release', $packageName))))
    $Candidates.Add((Join-Path $cleanRepository ([IO.Path]::Combine('artifacts', 'release', 'packaging', $packageName))))
    $Candidates.Add((Join-Path $cleanRepository (Join-Path 'packages' $packageName)))
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

    if (-not $ForceDownload -and (Test-ReleasePackagingPackage $packagePath)) {
        Write-Host "Using cached authoritative LocalGPT release-packaging package: $packagePath" -ForegroundColor DarkGreen
    }
    else {
        Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue
        $candidates = [System.Collections.Generic.List[string]]::new()
        $repositories = [System.Collections.Generic.List[string]]::new()
        if (-not $ForceDownload) {
            Add-RepositoryCandidates -Repository $LocalGptRepository -Candidates $candidates -Repositories $repositories
            Add-RepositoryCandidates -Repository $env:LOCALGPT_REPOSITORY -Candidates $candidates -Repositories $repositories

            $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
            if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
                $candidates.Add((Join-Path $localApplicationData ([IO.Path]::Combine('LocalGPT', 'NuGet', $packageName))))
                Add-RepositoryCandidates -Repository (Join-Path $localApplicationData ([IO.Path]::Combine('LocalGPT', 'src'))) -Candidates $candidates -Repositories $repositories
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

            if (-not (Test-ReleasePackagingPackage $packagePath)) {
                foreach ($repository in $repositories | Select-Object -Unique) {
                    $publisherScript = Join-Path $repository ([IO.Path]::Combine('build', 'Publish-ReleasePackagingPackage.ps1'))
                    if (-not (Test-Path -LiteralPath $publisherScript -PathType Leaf)) { continue }
                    Write-Host "Preparing LocalGPT.ReleasePackaging $Version from LocalGPT source at $repository..." -ForegroundColor Cyan
                    try {
                        $packageOutput = @(& $publisherScript -Configuration $Configuration -Version $Version)
                        if ($packageOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$packageOutput[0])) {
                            throw "LocalGPT package publisher returned $($packageOutput.Count) pipeline value(s); expected one package path."
                        }
                        $builtPackage = [string]$packageOutput[0]
                        if (-not (Test-ReleasePackagingPackage $builtPackage)) {
                            throw "LocalGPT package publisher did not produce a valid $packageName package: $builtPackage"
                        }
                        $temporaryCopy = "$packagePath.copying"
                        Remove-Item -LiteralPath $temporaryCopy -Force -ErrorAction SilentlyContinue
                        Copy-Item -LiteralPath $builtPackage -Destination $temporaryCopy -Force
                        Move-Item -LiteralPath $temporaryCopy -Destination $packagePath -Force

                        if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
                            $sharedPackageDirectory = Join-Path $localApplicationData ([IO.Path]::Combine('LocalGPT', 'NuGet'))
                            New-Item -ItemType Directory -Path $sharedPackageDirectory -Force | Out-Null
                            Copy-Item -LiteralPath $packagePath -Destination (Join-Path $sharedPackageDirectory $packageName) -Force
                        }
                        Write-Host "Prepared authoritative LocalGPT release-packaging package from local LocalGPT source." -ForegroundColor Green
                        break
                    }
                    catch {
                        Write-Warning "Local LocalGPT release-packaging preparation failed at $repository`: $($_.Exception.Message)"
                    }
                }
            }
        }

        if (-not (Test-ReleasePackagingPackage $packagePath)) {
            if ([string]::IsNullOrWhiteSpace($PackageUrl)) {
                $modeHint = if ($ForceDownload) { 'The refresh/download switch was used, but no -PackageUrl was supplied.' } else { 'No network download was attempted because no -PackageUrl was supplied.' }
                throw @"
The authoritative LocalGPT release-packaging package could not be prepared locally.
Expected package: $packageName

Searched the PublisherStudio package cache, LOCALGPT_REPOSITORY / -LocalGptRepository,
the shared LocalGPT NuGet cache, and the standard installed LocalGPT source location.
$modeHint

Build LocalGPT once, point LOCALGPT_REPOSITORY at a LocalGPT checkout, pass
-LocalGptRepository, or explicitly pass -ReleasePackagingPackageUrl when an online
fallback is desired.
"@
            }

            Write-Host "Downloading authoritative LocalGPT release-packaging package $Version from the explicitly configured URL..." -ForegroundColor Cyan
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
The explicitly requested LocalGPT release-packaging download failed.
Expected package: $packageName
Package URL: $PackageUrl
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

# Install only from the prepared local package using an isolated NuGet configuration.
# This intentionally avoids --add-source, which conflicts with package-source mapping.
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
