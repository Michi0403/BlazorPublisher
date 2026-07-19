param(
    [switch]$SkipPackageRestore
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$webDirectory = Join-Path $root "src\PublisherStudio.Web"
$prepareModule = Join-Path $webDirectory "tools\prepare-spreadsheet-assets.mjs"

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

$node = Resolve-Executable -CommandNames @("node", "node.exe") -CandidatePaths $nodeCandidates
$npm = Resolve-Executable -CommandNames @("npm", "npm.cmd") -CandidatePaths $npmCandidates

if (-not $node -or -not $npm) {
    throw @"
Node.js with npm was not found.

PublisherStudio needs Node.js only once on the build machine to restore the DevExpress Spreadsheet browser files. The installed application remains fully offline.

Install Node.js 20 LTS or newer, then close and reopen Visual Studio so its PATH is refreshed. Afterwards run:

    Prepare-SpreadsheetAssets.cmd

The script also checks the standard Node.js and NVM for Windows installation folders, so manually editing the project file is not required.
"@
}

$nodeVersionText = (& $node --version).Trim()
if ($LASTEXITCODE -ne 0 -or $nodeVersionText -notmatch '^v(?<major>\d+)') {
    throw "Could not determine the Node.js version from '$node'."
}
if ([int]$Matches.major -lt 20) {
    throw "Node.js 20 or newer is required. Found $nodeVersionText at '$node'."
}

Write-Host "Node.js: $nodeVersionText" -ForegroundColor DarkGray
Write-Host "npm: $npm" -ForegroundColor DarkGray

Push-Location $webDirectory
try {
    if (-not $SkipPackageRestore) {
        Write-Host "Restoring Spreadsheet browser packages..." -ForegroundColor Cyan
        # The Spreadsheet browser package declares the full `devextreme` source package as a peer.
        # PublisherStudio intentionally ships the prebuilt `devextreme-dist` assets instead. Ignoring
        # automatic peer installation prevents npm from pulling unused build-time dependencies
        # (including deprecated lodash.isequal) into this asset-only restore.
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

    Write-Host "Copying Spreadsheet browser packages into wwwroot/vendor..." -ForegroundColor Cyan
    & $node $prepareModule
    if ($LASTEXITCODE -ne 0) {
        throw "Spreadsheet client asset preparation failed with exit code $LASTEXITCODE."
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
    "wwwroot\vendor\jquery\jquery.min.js"
)
$missingAssets = @($requiredAssets | Where-Object { -not (Test-Path (Join-Path $webDirectory $_)) })
if ($missingAssets.Count -gt 0) {
    throw "Spreadsheet client assets are incomplete. Missing: $($missingAssets -join ', ')"
}

Write-Host "Spreadsheet client assets are ready. Visual Studio builds no longer need to execute npm." -ForegroundColor Green
