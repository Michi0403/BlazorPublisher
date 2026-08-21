param(
    [switch]$SkipPackageRestore
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$webDirectory = Join-Path $root "src\PublisherStudio.Web"
$prepareModule = Join-Path $webDirectory "tools\prepare-devexpress-assets.mjs"
$licenseResolverModule = Join-Path $webDirectory "tools\resolve-devextreme-package-root.mjs"
$packageJsonPath = Join-Path $webDirectory "package.json"
$vendorDirectory = Join-Path $webDirectory "wwwroot\vendor"
$runtimeLicensePath = Join-Path $vendorDirectory "devextreme-license.js"
$runtimeLicenseMetadataPath = Join-Path $vendorDirectory "devextreme-license.meta.json"
$runtimeLicenseVersionPath = Join-Path $vendorDirectory "devextreme-license.version"
$licenseTempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("PublisherStudio-DevExtreme-" + $PID + "-" + [Guid]::NewGuid().ToString("N"))
$runtimeLicenseGeneratedPath = Join-Path $licenseTempDirectory "devextreme-license.js"
$previousDevExtremeSourceRoot = $env:PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT


function Remove-GeneratedPathWithRetry {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [int]$Attempts = 8
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $lastError = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                try { $_.IsReadOnly = $false } catch { }
            }
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            if (-not (Test-Path -LiteralPath $Path)) {
                return
            }
        }
        catch {
            $lastError = $_.Exception
        }

        Start-Sleep -Milliseconds (150 * $attempt)
    }

    if (Test-Path -LiteralPath $Path) {
        $parent = Split-Path -Parent $Path
        $leaf = Split-Path -Leaf $Path
        $stale = Join-Path $parent ($leaf + ".stale-" + [DateTime]::UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + $PID)
        try {
            Rename-Item -LiteralPath $Path -NewName (Split-Path -Leaf $stale) -Force -ErrorAction Stop
            try {
                Remove-Item -LiteralPath $stale -Recurse -Force -ErrorAction SilentlyContinue
            }
            catch {
                # The renamed directory no longer participates in the live vendor path.
            }
            return
        }
        catch {
            $lastError = $_.Exception
        }
    }

    $message = if ($null -ne $lastError) { $lastError.Message } else { "unknown Windows file-system error" }
    throw "Could not clear generated browser asset path '$Path'. Close PublisherStudio/browser/file-indexer handles that are locking the generated vendor folder, then retry. Last error: $message"
}


function Resolve-Executable {
    param(
        [Parameter(Mandatory = $true)][string[]]$CommandNames,
        [Parameter(Mandatory = $true)][string[]]$CandidatePaths
    )

    foreach ($commandName in $CommandNames) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($command -and $command.Source) {
            return $command.Source
        }
    }

    foreach ($candidate in $CandidatePaths) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    return $null
}

$programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
$nodeCandidates = @(
    $(if ($env:NVM_SYMLINK) { Join-Path $env:NVM_SYMLINK "node.exe" }),
    $(if ($env:ProgramFiles) { Join-Path $env:ProgramFiles "nodejs\node.exe" }),
    $(if ($programFilesX86) { Join-Path $programFilesX86 "nodejs\node.exe" }),
    $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "Programs\nodejs\node.exe" })
) | Where-Object { $_ }

$npmCandidates = @(
    $(if ($env:NVM_SYMLINK) { Join-Path $env:NVM_SYMLINK "npm.cmd" }),
    $(if ($env:ProgramFiles) { Join-Path $env:ProgramFiles "nodejs\npm.cmd" }),
    $(if ($programFilesX86) { Join-Path $programFilesX86 "nodejs\npm.cmd" }),
    $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "Programs\nodejs\npm.cmd" }),
    $(if ($env:APPDATA) { Join-Path $env:APPDATA "npm\npm.cmd" })
) | Where-Object { $_ }

$npxCandidates = @(
    $(if ($env:NVM_SYMLINK) { Join-Path $env:NVM_SYMLINK "npx.cmd" }),
    $(if ($env:ProgramFiles) { Join-Path $env:ProgramFiles "nodejs\npx.cmd" }),
    $(if ($programFilesX86) { Join-Path $programFilesX86 "nodejs\npx.cmd" }),
    $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "Programs\nodejs\npx.cmd" }),
    $(if ($env:APPDATA) { Join-Path $env:APPDATA "npm\npx.cmd" })
) | Where-Object { $_ }

$node = Resolve-Executable -CommandNames @("node", "node.exe") -CandidatePaths $nodeCandidates
$npm = Resolve-Executable -CommandNames @("npm", "npm.cmd") -CandidatePaths $npmCandidates
$npx = Resolve-Executable -CommandNames @("npx", "npx.cmd") -CandidatePaths $npxCandidates

if (-not $node -or -not $npm -or -not $npx) {
    throw @"
Node.js with npm and npx was not found.

PublisherStudio uses Node.js only on developer/build machines to restore the local DevExpress browser files and generate the public DevExtreme runtime license used by standalone HTML exports. Installed applications remain fully offline and do not require Node.js.

Install Node.js 20 LTS or newer, then close and reopen Visual Studio so its PATH is refreshed. Afterwards run:

    Prepare-DevExpressAssets.cmd

The script also checks standard Node.js and NVM for Windows installation folders.
"@
}

$nodeVersionText = (& $node --version).Trim()
if ($LASTEXITCODE -ne 0 -or $nodeVersionText -notmatch '^v(?<major>\d+)') {
    throw "Could not determine the Node.js version from '$node'."
}
if ([int]$Matches.major -lt 20) {
    throw "Node.js 20 or newer is required. Found $nodeVersionText at '$node'."
}

if (-not (Test-Path $packageJsonPath)) {
    throw "PublisherStudio package.json was not found: $packageJsonPath"
}
$packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
$devExtremeVersion = $packageJson.dependencies.'devextreme-dist'
if ([string]::IsNullOrWhiteSpace($devExtremeVersion)) {
    throw "package.json does not define dependencies.devextreme-dist."
}

Write-Host "Node.js: $nodeVersionText" -ForegroundColor DarkGray
Write-Host "npm: $npm" -ForegroundColor DarkGray
Write-Host "npx: $npx" -ForegroundColor DarkGray
Write-Host "DevExtreme: $devExtremeVersion" -ForegroundColor DarkGray

Write-Host "Clearing generated DevExpress browser asset folders before preparation..." -ForegroundColor Cyan
$generatedVendorPaths = @(
    (Join-Path $vendorDirectory "devextreme-dist"),
    (Join-Path $vendorDirectory "devexpress-aspnetcore-spreadsheet"),
    (Join-Path $vendorDirectory "jquery"),
    (Join-Path $vendorDirectory "devextreme-assets.meta.json"),
    $runtimeLicenseMetadataPath,
    $runtimeLicenseVersionPath
)
foreach ($generatedPath in $generatedVendorPaths) {
    Remove-GeneratedPathWithRetry -Path $generatedPath
}

if (-not $SkipPackageRestore) {
    # Clear only the generated package folders involved in this preparation.
    # This also avoids npm tar extraction reusing a stale, partially locked package tree.
    $nodeModulesDirectory = Join-Path $webDirectory "node_modules"
    foreach ($packageName in @("devextreme-dist", "devexpress-aspnetcore-spreadsheet", "devextreme")) {
        Remove-GeneratedPathWithRetry -Path (Join-Path $nodeModulesDirectory $packageName)
    }
}

Push-Location $webDirectory
try {
    if (-not $SkipPackageRestore) {
        Write-Host "Restoring local DevExpress browser packages..." -ForegroundColor Cyan
        $lockFile = Join-Path $webDirectory "package-lock.json"
        if (Test-Path $lockFile) {
            & $npm ci --legacy-peer-deps --no-audit --no-fund
            $restoreCommand = "npm ci"
        }
        else {
            & $npm install --legacy-peer-deps --no-audit --no-fund
            $restoreCommand = "npm install"
        }
        if ($LASTEXITCODE -ne 0) {
            throw "$restoreCommand failed with exit code $LASTEXITCODE."
        }
    }

    New-Item -ItemType Directory -Path $vendorDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $licenseTempDirectory -Force | Out-Null

    Write-Host "Resolving the exact DevExtreme package used for browser assets and license generation..." -ForegroundColor Cyan
    $resolverOutput = @(& $npx --package "devextreme@$devExtremeVersion" --yes node $licenseResolverModule $devExtremeVersion)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not resolve the exact devextreme@$devExtremeVersion package used by npx."
    }
    $devExtremeSourceRoot = $resolverOutput | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_) } | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace($devExtremeSourceRoot)) {
        throw "npx did not expose a usable devextreme@$devExtremeVersion package root."
    }

    $resolvedPackageJsonPath = Join-Path $devExtremeSourceRoot "package.json"
    $resolvedPackageJson = Get-Content $resolvedPackageJsonPath -Raw | ConvertFrom-Json
    if ($resolvedPackageJson.version -ne $devExtremeVersion) {
        throw "Resolved DevExtreme package is $($resolvedPackageJson.version), but PublisherStudio requires $devExtremeVersion."
    }
    $licenseGeneratorPath = Join-Path $devExtremeSourceRoot "bin\devextreme-license.js"
    if (-not (Test-Path -LiteralPath $licenseGeneratorPath)) {
        throw "The exact devextreme@$devExtremeVersion package does not contain bin\devextreme-license.js."
    }

    # One exact devextreme package is authoritative for both the generated runtime key
    # and the browser runtime overlay copied by prepare-devexpress-assets.mjs.
    $env:PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT = $devExtremeSourceRoot

    Write-Host "Generating the public DevExtreme runtime license from devextreme@$devExtremeVersion..." -ForegroundColor Cyan
    Write-Host "The private DevExpress license remains on this build machine and is never copied into the application." -ForegroundColor DarkGray
    & $node $licenseGeneratorPath `
        --non-modular `
        --out $runtimeLicenseGeneratedPath `
        --force `
        --no-gitignore
    if ($LASTEXITCODE -ne 0) {
        throw @"
DevExtreme runtime-license generation failed with exit code $LASTEXITCODE.

Register a valid DevExpress license on this developer/build machine or provide it through the DevExpress_License environment variable, then run Prepare-DevExpressAssets.cmd again. Do not place the private DevExpress license directly in PublisherStudio or in an exported HTML file.

An existing generated runtime key was left untouched.
"@
    }

    if (-not (Test-Path -LiteralPath $runtimeLicenseGeneratedPath)) {
        throw "The DevExtreme license generator completed without creating '$runtimeLicenseGeneratedPath'."
    }
    $generatedLicenseSource = Get-Content $runtimeLicenseGeneratedPath -Raw
    if ($generatedLicenseSource -notmatch 'DevExpress\s*\.\s*config\s*\(' -or
        $generatedLicenseSource -notmatch 'licenseKey\s*:' -or
        $generatedLicenseSource -notmatch 'licenseKey\s*:\s*(["''`]).+?\1') {
        throw "The DevExtreme license generator produced an invalid or empty non-modular runtime file."
    }

    Write-Host "Copying DevExpress browser packages into wwwroot/vendor..." -ForegroundColor Cyan
    & $node $prepareModule
    if ($LASTEXITCODE -ne 0) {
        throw "DevExpress client-asset preparation failed with exit code $LASTEXITCODE."
    }

    # Only publish the newly generated key after the browser asset preparation succeeded.
    # The Node asset copier never guesses or owns this path.
    Remove-GeneratedPathWithRetry -Path $runtimeLicensePath
    Copy-Item -LiteralPath $runtimeLicenseGeneratedPath -Destination $runtimeLicensePath -Force

    $licenseHash = (Get-FileHash -LiteralPath $runtimeLicensePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $licenseMetadata = [ordered]@{
        schemaVersion = 2
        generatorPackage = "devextreme"
        generatorPackageVersion = $devExtremeVersion
        generatedAtUtc = [DateTime]::UtcNow.ToString("o")
        sha256 = $licenseHash
    }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText(
        $runtimeLicenseMetadataPath,
        (($licenseMetadata | ConvertTo-Json -Depth 4) + [Environment]::NewLine),
        $utf8NoBom)
    [System.IO.File]::WriteAllText($runtimeLicenseVersionPath, ($devExtremeVersion + [Environment]::NewLine), $utf8NoBom)
}
finally {
    if ($null -eq $previousDevExtremeSourceRoot) {
        Remove-Item Env:PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT -ErrorAction SilentlyContinue
    }
    else {
        $env:PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT = $previousDevExtremeSourceRoot
    }
    Remove-Item -LiteralPath $licenseTempDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Pop-Location
}

$requiredAssets = @(
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.js",
    "wwwroot\vendor\devexpress-aspnetcore-spreadsheet\dist\dx-aspnetcore-spreadsheet.css",
    "wwwroot\vendor\devextreme-dist\js\dx.all.js",
    "wwwroot\vendor\devextreme-dist\css\dx.light.css",
    "wwwroot\vendor\jquery\jquery.min.js",
    "wwwroot\vendor\devextreme-license.js",
    "wwwroot\vendor\devextreme-license.meta.json",
    "wwwroot\vendor\devextreme-license.version",
    "wwwroot\vendor\devextreme-assets.meta.json"
)
$missingAssets = @($requiredAssets | Where-Object { -not (Test-Path (Join-Path $webDirectory $_)) })
if ($missingAssets.Count -gt 0) {
    throw "DevExpress client assets are incomplete. Missing: $($missingAssets -join ', ')"
}

$metadata = Get-Content $runtimeLicenseMetadataPath -Raw | ConvertFrom-Json
$versionMarker = (Get-Content $runtimeLicenseVersionPath -Raw).Trim()
if ($metadata.schemaVersion -ne 2 -or
    $metadata.generatorPackage -ne "devextreme" -or
    $metadata.generatorPackageVersion -ne $devExtremeVersion -or
    $versionMarker -ne $devExtremeVersion) {
    throw "The generated runtime-key metadata does not match the exact devextreme@$devExtremeVersion generator package."
}
$actualLicenseHash = (Get-FileHash -LiteralPath $runtimeLicensePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($metadata.sha256 -ne $actualLicenseHash) {
    throw "The generated DevExtreme runtime-key hash does not match its preparation metadata."
}

$assetMetadataPath = Join-Path $vendorDirectory "devextreme-assets.meta.json"
$assetMetadata = Get-Content $assetMetadataPath -Raw | ConvertFrom-Json
if (($assetMetadata.schemaVersion -lt 1 -or $assetMetadata.schemaVersion -gt 4) -or $assetMetadata.devExtremeVersion -ne $devExtremeVersion) {
    throw "The DevExtreme client-asset metadata does not match DevExtreme $devExtremeVersion."
}

if ($assetMetadata.schemaVersion -ge 2 -and $assetMetadata.restoredPackageVersion -ne $devExtremeVersion) {
    throw "The restored DevExtreme lock version recorded in client-asset metadata does not match DevExtreme $devExtremeVersion."
}

if ($assetMetadata.schemaVersion -ge 3) {
    if ($assetMetadata.lockedPackageVersion -ne $devExtremeVersion) {
        throw "The DevExtreme package-lock version recorded in client-asset metadata does not match DevExtreme $devExtremeVersion."
    }
    if ([string]::IsNullOrWhiteSpace([string]$assetMetadata.lockedPackageIntegrity)) {
        throw "The DevExtreme client-asset metadata is missing the npm lock integrity hash."
    }
}

if ($assetMetadata.schemaVersion -ge 4) {
    if ($assetMetadata.authoritativeRuntimePackage -ne "devextreme" -or
        $assetMetadata.authoritativeRuntimePackageVersion -ne $devExtremeVersion) {
        throw "The prepared browser runtime was not sourced from the exact devextreme@$devExtremeVersion package used by the license generator."
    }
}

$copiedPackageJsonPath = Join-Path $vendorDirectory "devextreme-dist\package.json"
if (-not (Test-Path -LiteralPath $copiedPackageJsonPath)) {
    throw "The copied DevExtreme package metadata is missing: $copiedPackageJsonPath"
}
$copiedPackageJson = Get-Content $copiedPackageJsonPath -Raw | ConvertFrom-Json
if ($copiedPackageJson.version -ne $devExtremeVersion) {
    Write-Warning "The copied devextreme-dist package.json reports $($copiedPackageJson.version), while the npm lock and prepared asset manifest target $devExtremeVersion. The lock integrity and asset SHA-256 values remain authoritative."
}

$expectedAssetEntries = @(
    @{ RelativePath = "devextreme-dist\js\dx.all.js"; ManifestPath = "devextreme-dist/js/dx.all.js" },
    @{ RelativePath = "devextreme-dist\css\dx.light.css"; ManifestPath = "devextreme-dist/css/dx.light.css" }
)
foreach ($expectedAsset in $expectedAssetEntries) {
    $entries = @($assetMetadata.assets | Where-Object { $_.path -eq $expectedAsset.ManifestPath })
    if ($entries.Count -ne 1) {
        throw "The DevExtreme client-asset metadata must contain exactly one entry for $($expectedAsset.ManifestPath)."
    }

    $assetPath = Join-Path $vendorDirectory $expectedAsset.RelativePath
    $assetFile = Get-Item -LiteralPath $assetPath
    if ([long]$entries[0].bytes -ne [long]$assetFile.Length) {
        throw "The DevExtreme client-asset size does not match its metadata: $($expectedAsset.ManifestPath)."
    }

    $actualHash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($entries[0].sha256 -ne $actualHash) {
        throw "The DevExtreme client-asset hash does not match its metadata: $($expectedAsset.ManifestPath)."
    }
}

Write-Host "DevExpress client assets and the public runtime license are ready." -ForegroundColor Green
Write-Host "End-user installations remain self-contained and do not require Node.js, npm, npx, or a private DevExpress license." -ForegroundColor Green
