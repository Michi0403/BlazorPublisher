Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "InteractiveServer render-mode validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $root 'src\PublisherStudio.Web'
$expected = [ordered]@{
    'Components/Layout/JavaScriptDiagnosticsBridge.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'
    'Components/Pages/Editor.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: true))'
    'Components/Pages/Localization.razor' = '@rendermode InteractiveServer'
    'Components/Pages/OrganicPlugins.razor' = '@rendermode InteractiveServer'
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($entry in $expected.GetEnumerator()) {
    $relative = [string]$entry.Key
    $path = Join-Path $appRoot ($relative.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Required interactive page is missing: $relative" }
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    $expectedDirective = [string]$entry.Value
    $escapedDirective = [System.Text.RegularExpressions.Regex]::Escape($expectedDirective)
    if ($text -notmatch "(?m)^\s*$escapedDirective\s*$") {
        $directives = @($text -split '\r?\n' | Where-Object { $_ -match '^\s*@(?:page|rendermode)\b' } | ForEach-Object { $_.Trim() })
        $found = if ($directives.Count -eq 0) { '<no page or render-mode directives>' } else { $directives -join ', ' }
        Fail "Render mode changed in $relative. Expected directive '$expectedDirective'; found $found."
    }
}
$importsPath = Join-Path $appRoot 'Components\_Imports.razor'
$imports = [System.IO.File]::ReadAllText($importsPath, $utf8)
if (-not $imports.Contains('@using static Microsoft.AspNetCore.Components.Web.RenderMode')) {
    Fail 'Components/_Imports.razor no longer imports RenderMode for @rendermode InteractiveServer.'
}
$programPath = Join-Path $appRoot 'Program.cs'
$appPath = Join-Path $appRoot 'Components\App.razor'
$program = [System.IO.File]::ReadAllText($programPath, $utf8)
$app = [System.IO.File]::ReadAllText($appPath, $utf8)
if (-not $program.Contains('AddInteractiveServerComponents()')) { Fail 'Program.cs no longer registers interactive server components.' }
if (-not $program.Contains('AddInteractiveServerRenderMode()')) { Fail 'Program.cs no longer maps InteractiveServer.' }
if ($app -match '<Routes\s+@rendermode' -or $app -match '<HeadOutlet\s+@rendermode') { Fail 'App.razor must not replace the reviewed page render modes with a global render boundary.' }
Write-Host "InteractiveServer render-mode validation passed for $($expected.Count) PublisherStudio pages." -ForegroundColor Green
