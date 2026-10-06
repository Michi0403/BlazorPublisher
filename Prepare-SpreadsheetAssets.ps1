param(
    [switch]$SkipPackageRestore,
    [switch]$EnsureCurrent,
    [string]$ExpectedVersion
)

# Compatibility alias retained for older build instructions and automation.
$script = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Prepare-DevExpressAssets.ps1"
& $script -SkipPackageRestore:$SkipPackageRestore -EnsureCurrent:$EnsureCurrent -ExpectedVersion $ExpectedVersion
