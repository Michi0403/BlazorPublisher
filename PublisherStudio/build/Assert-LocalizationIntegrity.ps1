Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Localization integrity validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'src\PublisherStudio.Web\Localization'
$englishPath = Join-Path $localization 'en-US.json'
$germanPath = Join-Path $localization 'de-DE.json'
function Assert-NoCaseInsensitiveDuplicateKeys([string]$Path) {
    $content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $seen = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in [regex]::Matches($content, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:')) {
        $key = $match.Groups[1].Value
        if ($seen.ContainsKey($key)) {
            Fail "Catalog '$Path' contains case-insensitive duplicate keys '$($seen[$key])' and '$key'."
        }
        $seen[$key] = $key
    }
}
if (-not (Test-Path -LiteralPath $englishPath -PathType Leaf)) { Fail "Missing $englishPath" }
if (-not (Test-Path -LiteralPath $germanPath -PathType Leaf)) { Fail "Missing $germanPath" }
Assert-NoCaseInsensitiveDuplicateKeys $englishPath
Assert-NoCaseInsensitiveDuplicateKeys $germanPath
try {
    $english = Get-Content -LiteralPath $englishPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $german = Get-Content -LiteralPath $germanPath -Raw -Encoding UTF8 | ConvertFrom-Json
} catch { Fail "A catalog is not valid JSON. $($_.Exception.Message)" }
$englishKeys = @($english.PSObject.Properties.Name | Sort-Object)
$germanKeys = @($german.PSObject.Properties.Name | Sort-Object)
if ($englishKeys.Count -lt 2800) { Fail "English catalog coverage unexpectedly dropped to $($englishKeys.Count) entries." }
if (($englishKeys -join "`n") -ne ($germanKeys -join "`n")) { Fail 'English and German catalog keys differ.' }
$required = @(
    'Text.Panel␠/␠Div␠Studio',
    'Text.Save␠panel',
    'Text.Arrange␠mode',
    'Text.Export␠JSON␠Canvas',
    'Text.Panel␠changes␠applied␠to␠the␠Mainframe␠and␠export␠model.'
)
foreach ($key in $required) {
    $property = $german.PSObject.Properties[$key]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { Fail "Required German UI string is missing: $key" }
}
Write-Host "Localization integrity validation passed for $($englishKeys.Count) PublisherStudio UI strings."
