Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw $Message }

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src\PublisherStudio.Web'
$baselinePath = Join-Path $PSScriptRoot 'system-variable-initialization-baseline.json'
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { Fail 'System-variable initialization baseline is missing.' }
$parsedBaseline = [System.IO.File]::ReadAllText($baselinePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$known = @{}
foreach ($item in $parsedBaseline) { $known[[string]$item] = $true }
$failures = @()
$allowed = @(
    'src/PublisherStudio.Web/Program.cs',
    'src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs',
    'src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs'
)
$constructorPattern = '(?m)^(?<line>[^\r\n]*(?:=\s*new\s+|Add\w*\s*\(\s*new\s+)[A-Za-z_][\w<>,.?\[\]]*\s*\([^\r\n;]*"(?:[^"\\\r\n]|\\.)*"[^\r\n;]*)$'
$directVariablePattern = '(?m)(?:SystemVariables|systemVariables|_systemVariables)\s*\.\s*(?:GetString|GetInt|GetTimeSpan|Set)\s*\(\s*"'
$files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    $_.Extension -in @('.cs', '.razor') -and
    $_.FullName -notmatch '[\/](?:bin|obj|Migrations)[\/]' -and
    $_.Name -notlike '*.Designer.cs'
})
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    if ($allowed -notcontains $relative -and [regex]::IsMatch($text, $directVariablePattern)) {
        $failures += "${relative}|direct-system-variable-name"
    }
    if ($allowed -contains $relative) { continue }
    foreach ($match in [regex]::Matches($text, $constructorPattern)) {
        $line = ([regex]::Replace($match.Groups['line'].Value.Trim(), '\s+', ' ')).Trim()
        if ($line -match '\bnew\s+[A-Za-z_]*Exception\b') { continue }
        $id = "${relative}|${line}"
        if (-not $known.ContainsKey($id)) { $failures += $id }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'System-variable initialization validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "System-variable initialization validation failed with $($failures.Count) new problem(s). Move initialization literals to SystemVariableStoreService or configuration-backed system variables."
}
Write-Host 'System-variable initialization validation passed. PublisherStudio initialization values are centralized in the singleton system-variable store and new literal constructor initialization is forbidden.'
