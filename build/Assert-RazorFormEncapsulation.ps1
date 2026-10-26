# Mandatory maintained subform ownership contract. No implementation writes or baselines.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$contracts = @(
    @{ Path='src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor'; Fragment='<OrganicCouncilRequestForm Request="@_councilRequest"' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor'; Fragment='<DxFormLayout CssClass="razor-component-layout-owner OrganicCouncilRequestForm"' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor'; Fragment='<EditForm Model="@Draft" OnValidSubmit="SubmitAsync" Context="councilEditContext"' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor'; Fragment='<DataAnnotationsValidator />' },
    @{ Path='src/PublisherStudio.Web/BusinessObjects/OrganicCouncilRequestFormModel.cs'; Fragment='OrganicCouncilRequestFormModel : IValidatableObject' },
    @{ Path='src/PublisherStudio.Web/Controllers/OrganicPluginController.cs'; Fragment='ValidationProblem(ModelState)' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor'; Fragment='SubmitFormOnClick="true"' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor.cs'; Fragment='Connection.SendCouncilRequestAsync(Request)' },
    @{ Path='src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor.cs'; Fragment='Notifications.Error(' },
    @{ Path='src/PublisherStudio.Web/Controllers/OrganicPluginController.cs'; Fragment='[FromBody] OrganicCouncilPromptRequest request' }
)
foreach ($contract in $contracts) {
    $path = Join-Path $RepositoryRoot ([string]$contract.Path)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FORMCONTRACT0001: Missing required component/contract $path" }
    $source = [IO.File]::ReadAllText($path)
    if (-not $source.Contains([string]$contract.Fragment)) {
        throw "FORMCONTRACT0002: $path no longer contains its required component/validation/service contract: $($contract.Fragment)"
    }
}
Write-Host "FORMCONTRACT: PublisherStudio reusable Razor subforms, existing service/HTTP routes and validation boundaries verified."
