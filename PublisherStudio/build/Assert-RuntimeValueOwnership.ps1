Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Runtime-value ownership validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$sourceRoot = Join-Path $root 'src\PublisherStudio.Web'
$baselinePath = Join-Path $PSScriptRoot 'runtime-value-ownership-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'The removal-only runtime-value baseline is missing.' }
$known = @{}; foreach ($item in ([IO.File]::ReadAllText($baselinePath) | ConvertFrom-Json)) { $known[[string]$item] = $true }
$failures = [Collections.Generic.List[string]]::new()
$declarationPattern = '(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:static|readonly|const|sealed|new|partial)\s+)*(?:Regex|TimeSpan|string|int|long|double|decimal|bool|char|Guid|Uri|FrozenSet<[^>]+>|IReadOnly(?:List|Dictionary|Set)<[^>]+>)\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|=>)\s*[^\r\n]+'
$generatedPattern = '(?m)^\s*\[GeneratedRegex\([^\r\n]+'
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' -and $_.FullName -notmatch '[\\/]Services[\\/]Configuration[\\/]' }) {
    $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\','/')
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($regex in @($declarationPattern, $generatedPattern)) {
        foreach ($match in [regex]::Matches($text, $regex)) {
            $declaration = ([regex]::Replace($match.Value, '\s+', ' ')).Trim()
            $id = "${relative}|${declaration}"
            if (-not $known.ContainsKey($id)) { $failures.Add("New runtime value outside a data boundary: $id") }
        }
    }
}
$panelPath = Join-Path $sourceRoot 'Services\PanelStudioTextService.cs'
$panel = [IO.File]::ReadAllText($panelPath)
foreach ($forbidden in @('private readonly Regex', 'new Regex(', 'RegexOptions.', 'TimeSpan.FromSeconds(2)', '@"cancel(?:led|ed)?', '@"<br\s*/?>"', '@"[^a-z0-9._-]+"')) {
    if ($panel.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { $failures.Add("PanelStudioTextService reintroduced service-owned runtime pattern data: $forbidden") }
}
foreach ($required in @('IPanelStudioTextPatternDataService patterns', '_patterns.ShutdownPattern', '_patterns.HtmlBreakPattern', '_patterns.HtmlTagPattern', '_patterns.UnsafeFileNamePattern')) {
    if ($panel.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("PanelStudioTextService lost object-store ownership token: $required") }
}
$dataPath = Join-Path $sourceRoot 'Services\Configuration\PanelStudioTextPatternDataService.cs'
$data = [IO.File]::ReadAllText($dataPath)
foreach ($required in @('ReadStore(seedPath)', 'File.Exists(overridePath)', 'JsonSerializer.Deserialize<PatternStoreDocument>', 'TimeSpan.FromMilliseconds(definition.TimeoutMilliseconds)')) {
    if ($data.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("Panel text pattern data service lost serializable-object-store token: $required") }
}
$storePath = Join-Path $sourceRoot 'Configuration\panel-text-patterns.json'
if (-not (Test-Path -LiteralPath $storePath -PathType Leaf)) { $failures.Add('Panel text pattern seed object store is missing.') }
else {
    $store = Get-Content -Raw -LiteralPath $storePath | ConvertFrom-Json
    foreach ($name in @('ShutdownPattern','HtmlBreakPattern','HtmlTagPattern','UnsafeFileNamePattern')) {
        if (-not $store.patterns.PSObject.Properties[$name]) { $failures.Add("Panel text pattern object store is missing $name.") }
    }
}
$project = [IO.File]::ReadAllText((Join-Path $sourceRoot 'PublisherStudio.Web.csproj'))
if ($project.IndexOf('Configuration\panel-text-patterns.json', [StringComparison]::Ordinal) -lt 0) { $failures.Add('Panel text pattern object store must be copied to build and publish output.') }
$registrations = [IO.File]::ReadAllText((Join-Path $sourceRoot 'PublisherStudioServiceCollectionExtensions.cs'))
foreach ($required in @('AddSingleton<IPanelStudioTextPatternDataService, PanelStudioTextPatternDataService>', 'AddSingleton<PanelStudioTextService, PanelStudioTextService>')) {
    if ($registrations.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { $failures.Add("PublisherStudio DI lost runtime-value data-service registration: $required") }
}
if ($failures.Count -gt 0) { $failures | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Runtime-value ownership passed. Panel text values are object-store-backed and the removal-only magic-value baseline did not grow.'
