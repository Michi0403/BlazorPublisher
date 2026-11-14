Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$journal = [IO.File]::ReadAllText((Join-Path $root 'src/PublisherStudio.Web/Services/OrganicPlugins/OrganicWireTrafficJournal.cs'))
$options = [IO.File]::ReadAllText((Join-Path $root 'src/PublisherStudio.Web/BusinessObjects/OrganicWireTrafficOptions.cs'))
foreach ($pattern in @('WriteFile\(entry, traffic\)', 'JsonSerializer\.Serialize\(entry\)', 'File\.AppendAllText', 'File\.Move\(path, path \+ "\.previous", overwrite: true\)', 'fileSync')) {
    if ($journal -notmatch $pattern) { throw "PublisherStudio 1-Wire disk file contract missing: $pattern" }
}
foreach ($pattern in @('FileLoggingEnabled', 'FilePath', 'MaximumFileBytes')) {
    if ($options -notmatch $pattern) { throw "PublisherStudio traffic option missing: $pattern" }
}
$config = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/appsettings.json') -Raw | ConvertFrom-Json
if ($config.OrganicPlugins.Traffic.FileLoggingEnabled -ne $true -or [long]$config.OrganicPlugins.Traffic.MaximumFileBytes -lt 1048576) {
    throw 'PublisherStudio 1-Wire disk journal is not enabled with a bounded rotation policy.'
}
$webProject = [xml](Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/PublisherStudio.Web.csproj') -Raw)
$installerProject = [xml](Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj') -Raw)
$webVersion = [version]($webProject.Project.PropertyGroup.Version | Where-Object { $_ })
$installerVersion = [version]($installerProject.Project.PropertyGroup.Version | Where-Object { $_ })
if ($webVersion -lt [version]'4.7.6' -or $webVersion -ne $installerVersion) {
    throw "PublisherStudio Web ($webVersion) and Installer ($installerVersion) must share a version of 4.7.6 or later."
}
Write-Host 'PublisherStudio dedicated 1-Wire traffic file source contracts passed.' -ForegroundColor Green
