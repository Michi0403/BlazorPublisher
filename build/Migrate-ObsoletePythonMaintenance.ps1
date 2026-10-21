[CmdletBinding()]
param([string]$RepositoryRoot = '')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$inventoryPath = Join-Path $PSScriptRoot 'obsolete-python-maintenance.sha256'
if (-not (Test-Path -LiteralPath $inventoryPath -PathType Leaf)) { throw "Missing Python migration inventory: $inventoryPath" }
$backupRoot = Join-Path $RepositoryRoot 'artifacts/.build-storage/retired-python-maintenance'
# Exact legacy test names are from the 4.3.4 release; only these overlay remnants are migrated.
# Other Python sources remain forbidden by Assert-MaintenanceArchitecture.ps1.
$movedCount = 0
foreach ($line in [IO.File]::ReadAllLines($inventoryPath)) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -notmatch '^(?<hash>[a-f0-9]{64})  (?<relative>(?:(?:build|\.github/scripts)/[A-Za-z0-9_.\/-]+\.py|tests/static/test_(?:goal_bridge_release|goal_identity_release|organic_capability_release)\.py|build/PythonRuntime\.Common\.ps1))$') {
        throw "Malformed or unsafe obsolete-maintenance inventory entry: $line"
    }
    $relative = $Matches['relative']
    if ($relative -match '(^|/)\.\.?(/|$)') { throw "Unsafe obsolete-maintenance path: $relative" }
    $candidate = Join-Path $RepositoryRoot $relative
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
    $existing = Get-Item -LiteralPath $candidate -Force
    if ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Legacy Python maintenance file is a reparse point; not moving an unsafe link: $relative"
    }
    $backupPath = Join-Path $backupRoot $relative
    $backupDirectory = Split-Path -Parent $backupPath
    if (-not (Test-Path -LiteralPath $backupDirectory -PathType Container)) {
        [void](New-Item -ItemType Directory -Path $backupDirectory -Force)
    }
    # Never discard locally edited files, even if an old source ZIP was overlaid.
    # Disambiguate existing backups by content, not by overwriting the earlier copy.
    if (Test-Path -LiteralPath $backupPath -PathType Leaf) {
        $oldHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
        $newHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
        if ($oldHash -eq $newHash) {
            Remove-Item -LiteralPath $candidate -Force
            $movedCount++
            continue
        }
        $suffix = $newHash.Substring(0,12).ToLowerInvariant()
        $backupPath = "$backupPath.$suffix.bak"
        if (Test-Path -LiteralPath $backupPath -PathType Leaf) {
            if ((Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash -ne $newHash) {
                throw "Conflicting legacy Python backup at $backupPath; no source file was deleted."
            }
            Remove-Item -LiteralPath $candidate -Force
            $movedCount++
            continue
        }
    }
    Move-Item -LiteralPath $candidate -Destination $backupPath -ErrorAction Stop
    $movedCount++
}
if ($movedCount -gt 0) {
    Write-Host "Migrated $movedCount obsolete Python maintenance file(s) to $backupRoot; local modifications were preserved."
}
