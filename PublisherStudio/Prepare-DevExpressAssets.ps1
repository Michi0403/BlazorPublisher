param(
    [switch]$SkipPackageRestore
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$webDirectory = Join-Path $root "src\PublisherStudio.Web"
$prepareModule = Join-Path $webDirectory "tools\prepare-devexpress-assets.mjs"
$packageJsonPath = Join-Path $webDirectory "package.json"
$vendorDirectory = Join-Path $webDirectory "wwwroot\vendor"
$runtimeLicensePath = Join-Path $vendorDirectory "devextreme-license.js"
$runtimeLicenseTempPath = Join-Path $vendorDirectory "devextreme-license.generated.js"

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
    Remove-Item $runtimeLicenseTempPath -Force -ErrorAction SilentlyContinue

    Write-Host "Generating the public DevExtreme runtime license..." -ForegroundColor Cyan
    Write-Host "The private DevExpress license remains on this build machine and is never copied into the application." -ForegroundColor DarkGray
    & $npx --package "devextreme@$devExtremeVersion" --yes devextreme-license `
        --non-modular `
        --out $runtimeLicenseTempPath `
        --force `
        --no-gitignore
    if ($LASTEXITCODE -ne 0) {
        Remove-Item $runtimeLicenseTempPath -Force -ErrorAction SilentlyContinue
        throw @"
DevExtreme runtime-license generation failed with exit code $LASTEXITCODE.

Register a valid DevExpress license on this developer/build machine or provide it through the DevExpress_License environment variable, then run Prepare-DevExpressAssets.cmd again. Do not place the private DevExpress license directly in PublisherStudio or in an exported HTML file.

An existing generated runtime key was left untouched.
"@
    }

    if (-not (Test-Path $runtimeLicenseTempPath)) {
        throw "The DevExtreme license generator completed without creating '$runtimeLicenseTempPath'."
    }
    $generatedLicenseSource = Get-Content $runtimeLicenseTempPath -Raw
    if ($generatedLicenseSource -notmatch 'DevExpress\s*\.\s*config\s*\(' -or
        $generatedLicenseSource -notmatch 'licenseKey\s*:') {
        Remove-Item $runtimeLicenseTempPath -Force -ErrorAction SilentlyContinue
        throw "The DevExtreme license generator produced an invalid non-modular runtime file."
    }
    Move-Item $runtimeLicenseTempPath $runtimeLicensePath -Force

    Write-Host "Copying DevExpress browser packages into wwwroot/vendor..." -ForegroundColor Cyan
    & $node $prepareModule
    if ($LASTEXITCODE -ne 0) {
        throw "DevExpress client-asset preparation failed with exit code $LASTEXITCODE."
    }
}
finally {
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
    "wwwroot\vendor\devextreme-license.version"
)
$missingAssets = @($requiredAssets | Where-Object { -not (Test-Path (Join-Path $webDirectory $_)) })
if ($missingAssets.Count -gt 0) {
    throw "DevExpress client assets are incomplete. Missing: $($missingAssets -join ', ')"
}

$metadataPath = Join-Path $vendorDirectory "devextreme-license.meta.json"
$metadata = Get-Content $metadataPath -Raw | ConvertFrom-Json
$versionMarkerPath = Join-Path $vendorDirectory "devextreme-license.version"
$versionMarker = (Get-Content $versionMarkerPath -Raw).Trim()
if ($metadata.devExtremeVersion -ne $devExtremeVersion -or $versionMarker -ne $devExtremeVersion) {
    throw "The generated runtime-license metadata does not match DevExtreme $devExtremeVersion."
}

Write-Host "DevExpress client assets and the public runtime license are ready." -ForegroundColor Green
Write-Host "End-user installations remain self-contained and do not require Node.js, npm, npx, or a private DevExpress license." -ForegroundColor Green
