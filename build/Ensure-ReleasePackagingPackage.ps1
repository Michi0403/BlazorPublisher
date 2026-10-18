[CmdletBinding()]
param(
    [string]$Version = "1.1.2",
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
            $hasMit = $entryNames -contains 'licenses/PDFsharp-6.2.4-MIT-LICENSE.txt'
            $hasNotices = $entryNames -contains 'licenses/THIRD-PARTY-NOTICES.txt'
            return $hasNuspec -and $hasToolSettings -and $hasToolAssembly -and $hasMit -and $hasNotices
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
            foreach ($siblingName in @('localgpt','LocalGPT','LOCALGPT')) {
                Add-RepositoryCandidates -Repository (Join-Path (Split-Path -Parent $repositoryRoot) $siblingName) -Candidates $candidates
            }

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

        if (-not (Test-ReleasePackagingPackage $packagePath) -and -not $ForceDownload) {
            # When the companion LocalGPT repository is present, the normal PublisherStudio
            # entrypoint builds its own authoritative helper package instead of asking the
            # developer to run a separate LocalGPT command or install a .NET global tool.
            $sourceRepositories = @($LocalGptRepository, $env:LOCALGPT_REPOSITORY)
            foreach ($sibling in @('localgpt','LocalGPT','LOCALGPT')) {
                $sourceRepositories += Join-Path (Split-Path -Parent $repositoryRoot) $sibling
            }
            foreach ($sourceRepository in @($sourceRepositories | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
                $sourceProject = Join-Path $sourceRepository 'src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj'
                if (-not (Test-Path -LiteralPath $sourceProject -PathType Leaf)) { continue }
                [xml]$helperProjectXml = Get-Content -LiteralPath $sourceProject -Raw
                $helperVersion = [string](@($helperProjectXml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)[0])
                if ($helperVersion -ne $Version) { continue }
                Write-Host "Building authoritative LocalGPT.ReleasePackaging $Version from adjacent source..." -ForegroundColor Cyan
                & dotnet pack $sourceProject -c Release -o $PackageDirectory
                if ($LASTEXITCODE -ne 0) { throw "The adjacent LocalGPT $Version packaging source failed to produce its NuGet tool. Fix the build failure rather than using an unverified package." }
                if (-not (Test-ReleasePackagingPackage $packagePath)) { throw 'LocalGPT source pack completed without required license and tool entries.' }
                $sourceGuard = Join-Path $sourceRepository 'build/Assert-ReleasePackagingPackageNotices.ps1'
                if (Test-Path -LiteralPath $sourceGuard -PathType Leaf) {
                    & $sourceGuard -PackagePath $packagePath -RepositoryRoot $sourceRepository
                }
                Write-Host "Built authoritative helper package from local LocalGPT source: $packagePath" -ForegroundColor Green
                break
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

# The distributable .NET tool is a NuGet archive containing the managed app, its
# runtimeconfig/deps.json and dependency assemblies. Extract those verified bytes directly.
# dotnet tool install is not needed and can fail on macOS stage filesystem permissions.
$toolRoot = Join-Path $repositoryRoot 'artifacts/release-tools'
$publishRoot = Join-Path $toolRoot 'published'
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
$stage = Join-Path $toolRoot ([string]('prepare-' + [Guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    $archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $prefix = 'tools/net10.0/any/'
        $foundAssembly = $false
        foreach ($entry in $archive.Entries) {
            $relative = $entry.FullName.Replace('\','/')
            if (-not $relative.StartsWith($prefix, [StringComparison]::Ordinal)) { continue }
            $local = $relative.Substring($prefix.Length)
            if (-not $local -or $local.EndsWith('/')) { continue }
            if ($local.Split('/') -contains '..' -or $local.StartsWith('/') -or $local.Contains(':')) { throw "Unsafe release helper entry: $relative" }
            $dest = [IO.Path]::GetFullPath((Join-Path $stage $local))
            $basePath = [IO.Path]::GetFullPath($stage).TrimEnd([char[]]@([char]'\',[char]'/')) + [IO.Path]::DirectorySeparatorChar
            if (-not $dest.StartsWith($basePath, [StringComparison]::Ordinal)) { throw "Release helper entry escapes staging: $relative" }
            New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
            $input = $entry.Open()
            try {
                $output = [IO.File]::Open($dest,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
                try { $input.CopyTo($output) } finally { $output.Dispose() }
            } finally { $input.Dispose() }
            if ($local -eq 'LocalGPT.ReleasePackaging.dll') { $foundAssembly = $true }
        }
        if (-not $foundAssembly) { throw 'Verified NuGet helper does not contain its executable DLL.' }
    } finally { $archive.Dispose() }
    foreach ($required in @('LocalGPT.ReleasePackaging.dll','LocalGPT.ReleasePackaging.deps.json','LocalGPT.ReleasePackaging.runtimeconfig.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $stage $required) -PathType Leaf)) { throw "The .NET helper is incomplete: $required" }
    }
    if (Test-Path -LiteralPath $publishRoot) { Remove-Item -LiteralPath $publishRoot -Recurse -Force }
    Move-Item -LiteralPath $stage -Destination $publishRoot
} finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
}
$command = Join-Path $publishRoot 'LocalGPT.ReleasePackaging.dll'
# These commands run from the original Build-Release/Build-LocalDevelopment orchestration.
& dotnet $command capabilities | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Repository-cached release helper did not expose the required managed packaging capabilities.' }
Write-Host "Prepared directly executable .NET helper from authoritative package: $command" -ForegroundColor Green
Write-Output ([string]$command)
