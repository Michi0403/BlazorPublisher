param([string]$RepositoryRoot = '')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) }
function Assert-Contains([string]$RelativePath, [string]$Needle) {
    $path = Join-Path $RepositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required AI integration source missing: $RelativePath" }
    $content = [IO.File]::ReadAllText($path)
    if (-not $content.Contains($Needle)) { throw "Expected AI integration contract missing: $RelativePath : $Needle" }
}
Assert-Contains 'src/PublisherStudio.Web/Components/Editor/PageSurface.razor' 'OpenSelectedAiAssist'
Assert-Contains 'src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor' 'OpenAiAssistForSelected'
Assert-Contains 'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor' 'DocumentInsertion.Authorize('
Assert-Contains 'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor' 'DocumentInsertion.TryApply('
Assert-Contains 'src/PublisherStudio.Web/Components/Pages/Editor.razor' 'OrganicResults.Changed += OnAiInsertionResult'
Assert-Contains 'src/PublisherStudio.Web/Components/Pages/Editor.razor' 'AiInsertion.TryApply(proposal)'
Assert-Contains 'src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs' 'editor.UpdateTextDocument('
Assert-Contains 'src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs' 'editor.AddSpreadsheet('
Assert-Contains 'src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs' 'editor.Document.Id != intent.DocumentId'
Assert-Contains 'src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs' 'AddScoped<PublisherAiDocumentInsertionService, PublisherAiDocumentInsertionService>'
$root = Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Localization'
$base = (Get-Content -LiteralPath (Join-Path $root 'en-US.json') -Raw | ConvertFrom-Json)
$keys = @($base.PSObject.Properties.Name | Sort-Object)
foreach ($culture in @('de-DE','es-ES','fr-FR','ja-JP','uk-UA')) {
    $catalog = Get-Content -LiteralPath (Join-Path $root "$culture.json") -Raw | ConvertFrom-Json
    $other = @($catalog.PSObject.Properties.Name | Sort-Object)
    if ((Compare-Object $keys $other).Count -gt 0) { throw "AI context localization catalog mismatch: $culture" }
}
Write-Output 'PublisherStudio AI Assist context, native insertion, and localization source contract passed.'
