[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-SourceContract {
    param([string]$RelativePath, [string]$Expected)
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Family protocol contract is missing its source: $RelativePath"
    }
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Family protocol compatibility contract failed in $RelativePath : expected $Expected"
    }
}

Assert-SourceContract 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs' 'public void ApplyEnumCompatibility(JsonSerializerOptions options)'
Assert-SourceContract 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs' 'options.PropertyNameCaseInsensitive = true;'
Assert-SourceContract 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs' 'DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull'
Assert-SourceContract 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs' 'new JsonStringEnumConverter()'
Assert-SourceContract 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs' 'NumberHandling = JsonNumberHandling.AllowReadingFromString'
Assert-SourceContract 'src/PublisherStudio.Web/Services/OrganicPlugins/OrganicPluginProtocolCodec.cs' 'new FamilyWireJsonConventions().CreateOneWireOptions()'
Assert-SourceContract 'src/PublisherStudio.Web/Controllers/OrganicWireHttpController.cs' 'return Ok(profile);'
Assert-SourceContract 'src/PublisherStudio.Web/Program.cs' 'familyWireJsonConventions.ApplyEnumCompatibility(options.JsonSerializerOptions)'
Assert-SourceContract 'src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiBridgeService.cs' 'rawCapabilities.GetRawText(), codec.JsonOptions'
Assert-SourceContract 'src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiBridgeService.cs' '}, codec.JsonOptions)'
Assert-SourceContract 'src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs' 'JsonSerializer.Deserialize<LocalGptWebConnectionProfile>(profileBody, codec.JsonOptions)'

# The JSON convention is a pure, stateless configuration value, not an operational Service.
# It must not introduce a static application type or bypass architecture/logging guards.
$familyConventionFile = Join-Path $root 'src/PublisherStudio.Web/BusinessObjects/FamilyWireJsonConventions.cs'
$familyConventionSource = [System.IO.File]::ReadAllText($familyConventionFile)
if ($familyConventionSource -notmatch 'public sealed class FamilyWireJsonConventions' -or
    $familyConventionSource -match '\bstatic\s+class\s+FamilyWireJsonConventions\b' -or
    $familyConventionSource -match '\bpublic\s+static\s+(?:void|JsonSerializerOptions)\b') {
    throw 'Family JSON convention must remain a non-static, stateless shared configuration type.'
}
if (Test-Path -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Services/OrganicPlugins/FamilyWireJsonConventions.cs')) {
    throw 'Family JSON convention was incorrectly reintroduced as an operational Service.'
}

Write-Host 'Family protocol JSON compatibility source contracts passed; platform runtime round trips still require integration tests.'
