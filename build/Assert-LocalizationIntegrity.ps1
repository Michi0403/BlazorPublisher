Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw "Localization integrity validation failed: $Message" }

# Keep this script ASCII-only. Windows PowerShell 5.1 reads UTF-8 scripts without a
# BOM using the active ANSI code page; non-ASCII source literals would be corrupted.
$script:StrictUtf8 = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList @($false, $true)

function Read-StrictUtf8([string]$Path) {
    try { return [System.IO.File]::ReadAllText($Path, $script:StrictUtf8) }
    catch { Fail "Catalog is not valid UTF-8: $Path. $($_.Exception.Message)" }
}

function Expand-SpaceMarkers([string]$Template) {
    $spaceMarker = [string][char]0x2420
    return $Template.Replace('<SP>', $spaceMarker)
}

function Assert-NoMojibake([string]$Raw, [string]$Path) {
    foreach ($codePoint in @(0xFFFD, 0x00C2, 0x00C3)) {
        if ($Raw.IndexOf([char]$codePoint) -ge 0) {
            Fail "Catalog contains a replacement or mojibake marker (U+$('{0:X4}' -f $codePoint)): $Path"
        }
    }
}

function Read-Catalog([string]$Path) {
    $raw = Read-StrictUtf8 $Path
    Assert-NoMojibake $raw $Path
    $seen = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in [regex]::Matches($raw, '(?m)^\s*"((?:\\.|[^"\\])*)"\s*:')) {
        try { $decoded = ConvertFrom-Json -InputObject ('"' + $match.Groups[1].Value + '"') }
        catch { Fail "Catalog key JSON could not be decoded in $Path. $($_.Exception.Message)" }
        $key = [string]$decoded
        if ($seen.ContainsKey($key)) { Fail "Catalog contains case-insensitive duplicate keys '$($seen[$key])' and '$key': $Path" }
        $seen[$key] = $key
    }
    try { $catalog = ConvertFrom-Json -InputObject $raw }
    catch { Fail "A catalog is not valid JSON. $($_.Exception.Message)" }
    return $catalog
}

$root = Split-Path -Parent $PSScriptRoot
$localization = Join-Path $root 'src\PublisherStudio.Web\Localization'
$englishPath = Join-Path $localization 'en-US.json'
$germanPath = Join-Path $localization 'de-DE.json'
if (-not (Test-Path -LiteralPath $englishPath -PathType Leaf)) { Fail "Missing $englishPath" }
if (-not (Test-Path -LiteralPath $germanPath -PathType Leaf)) { Fail "Missing $germanPath" }
$english = Read-Catalog $englishPath
$german = Read-Catalog $germanPath
$englishKeys = @($english.PSObject.Properties.Name | Sort-Object)
$germanKeys = @($german.PSObject.Properties.Name | Sort-Object)
if ($englishKeys.Count -lt 2800) { Fail "English catalog coverage unexpectedly dropped to $($englishKeys.Count) entries." }
if (($englishKeys -join "`n") -cne ($germanKeys -join "`n")) { Fail 'English and German catalog keys differ.' }
$requiredTemplates = @(
    'Text.Panel<SP>/<SP>Div<SP>Studio',
    'Text.Save<SP>panel',
    'Text.Arrange<SP>mode',
    'Text.Export<SP>JSON<SP>Canvas',
    'Text.Panel<SP>changes<SP>applied<SP>to<SP>the<SP>Mainframe<SP>and<SP>export<SP>model.'
)
foreach ($template in $requiredTemplates) {
    $key = Expand-SpaceMarkers $template
    $property = $german.PSObject.Properties[$key]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) { Fail "Required German UI string is missing: $key" }
}
Write-Host "Localization integrity validation passed for $($englishKeys.Count) PublisherStudio UI strings."
