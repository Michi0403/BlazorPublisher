# Optional PowerShell-only source regression. No Python runtime or relaxed build gates.
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$web = Join-Path $root 'src/PublisherStudio.Web'
function Require([string]$Source, [string]$Token) {
    if (-not $Source.Contains($Token)) { throw "Organic release contract is missing: $Token" }
}
$bridge = [IO.File]::ReadAllText((Join-Path $web 'Services/OrganicPlugins/PublisherAiBridgeService.cs'))
foreach ($token in @('public List<string> RequiredCapabilities { get; set; } = [];', 'RequestedOrganicCapabilities = request.RequiredCapabilities', 'Distinct(StringComparer.OrdinalIgnoreCase).ToList()', 'UserConfirmedArtifactBuild = false', 'ResultJson = resultJson', 'public string GoalId { get; set; }', 'property.Name.Equals("GoalExecution"', 'detail.Name.Equals("GoalId"', 'GoalId = goalId, ResultJson = resultJson', 'localGpt.SendCouncilRequestAsync(', 'Math.Clamp(request.MaxRounds', 'Math.Clamp(request.MaxParallelModels', 'SaveToMemory = request.SaveToMemory')) { Require $bridge $token }
if ($bridge.Contains('Return only the final human-visible answer')) { throw 'The legacy answer-only Council bridge prompt was restored.' }
$catalog = Get-Content -LiteralPath (Join-Path $web 'Configuration/publisher-dx-functions.json') -Raw | ConvertFrom-Json
$source = [IO.File]::ReadAllText((Join-Path $web 'Services/OrganicPlugins/MediaConversionOrganicHandlers.cs'))
foreach ($key in @('publisher.media.jobs','publisher.media.profiles','publisher.media.cancel')) {
    $capability = @($catalog.functions | Where-Object { $_.key -eq $key })
    if ($capability.Count -ne 1 -or -not $capability[0].isExposedToPeer) { throw "Missing executable catalog entry: $key" }
    Require $source ('"' + $key + '"')
}
$cancel = @($catalog.functions | Where-Object { $_.key -eq 'publisher.media.cancel' })[0]
if (-not $cancel.requiresFrontendUserConfirmation -or $cancel.isReadOnly) { throw 'Organic cancellation must remain approval-gated and state-changing.' }
Require $source 'media.Cancel(jobId)'
Require $source 'logger.LogError(ex, "Organic media cancellation failed.")'
$capabilitySource = [IO.File]::ReadAllText((Join-Path $web 'Services/OrganicPlugins/ObjectStoreOrganicCapabilityCatalog.cs'))
Require $capabilitySource '!executor.SupportsCapability(capability.Key)'
Require $capabilitySource 'capability.IsOnline = false'
Write-Host 'PowerShell organic release bridge, approval and executable catalog contracts passed.'
