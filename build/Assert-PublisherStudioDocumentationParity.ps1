param(
    [string]$RepositoryRoot = ""
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$helpRoot = Join-Path $RepositoryRoot 'src\PublisherStudio.Web\wwwroot\help-docs'
$pagesArchive = Join-Path $RepositoryRoot '.github\pages\publisherstudio-kawaii-docs.zip'
foreach ($required in @(
    (Join-Path $helpRoot 'index.html'),
    (Join-Path $helpRoot 'api\index.html'),
    (Join-Path $helpRoot 'api\toc.html'),
    (Join-Path $helpRoot 'documentation-status.json'),
    $pagesArchive
)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "PublisherStudio documentation parity requirement is missing: $required" }
}
$status = Get-Content -LiteralPath (Join-Path $helpRoot 'documentation-status.json') -Raw | ConvertFrom-Json
$projectText = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src\PublisherStudio.Web\PublisherStudio.Web.csproj'))
$m = [regex]::Match($projectText, '<Version>\s*(?<Version>[^<]+?)\s*</Version>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
if (-not $m.Success) { throw 'PublisherStudio project version could not be read.' }
$version = $m.Groups['Version'].Value.Trim()
if (-not [string]::Equals([string]$status.version, $version, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Tracked in-app documentation version $($status.version) does not match PublisherStudio source version $version."
}
$apiCount = @(Get-ChildItem -LiteralPath (Join-Path $helpRoot 'api') -Filter '*.html' -File -Recurse).Count
if ($apiCount -le 1) { throw "Tracked in-app API reference is incomplete: $apiCount HTML page(s)." }
Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
$zip = [IO.Compression.ZipFile]::OpenRead($pagesArchive)
try {
    $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\\','/') })
    if ($names -notcontains 'api/index.html') { throw 'Tracked Pages snapshot is missing api/index.html.' }
    $pagesApiCount = @($names | Where-Object { $_ -like 'api/*.html' }).Count
    if ($pagesApiCount -ne $apiCount) { throw "Tracked Pages API count $pagesApiCount does not match in-app API count $apiCount." }
} finally { $zip.Dispose() }
Write-Host "PublisherStudio documentation parity passed: version=$version; API HTML=$apiCount; in-app and Pages both contain api/index.html." -ForegroundColor Green
