Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "InteractiveServer render-mode validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $root 'src\PublisherStudio.Web'
$expected = [ordered]@{
    'Components/Pages/Editor.razor' = '@rendermode InteractiveServer'
    'Components/Pages/Localization.razor' = '@rendermode InteractiveServer'
    'Components/Pages/OrganicPlugins.razor' = '@rendermode InteractiveServer'
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($entry in $expected.GetEnumerator()) {
    $relative = [string]$entry.Key
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required interactive page is missing: $relative" }
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    $first = @($text -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })[0].Trim()
    if ($first -cne [string]$entry.Value) { Fail "Render mode changed in $relative. Expected '$($entry.Value)' but found '$first'." }
}
$programPath = Join-Path $appRoot 'Program.cs'
$appPath = Join-Path $appRoot 'Components\App.razor'
$program = [System.IO.File]::ReadAllText($programPath, $utf8)
$app = [System.IO.File]::ReadAllText($appPath, $utf8)
if (-not $program.Contains('AddInteractiveServerComponents()')) { Fail 'Program.cs no longer registers interactive server components.' }
if (-not $program.Contains('AddInteractiveServerRenderMode()')) { Fail 'Program.cs no longer maps InteractiveServer.' }
if ($app -match '<Routes\s+@rendermode' -or $app -match '<HeadOutlet\s+@rendermode') { Fail 'App.razor must not replace the reviewed page render modes with a global render boundary.' }
Write-Host "InteractiveServer render-mode validation passed for $($expected.Count) PublisherStudio pages." -ForegroundColor Green
