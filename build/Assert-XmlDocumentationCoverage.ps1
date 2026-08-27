$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PythonRuntime.Common.ps1')

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$scriptPath = Join-Path $PSScriptRoot "Assert-XmlDocumentationCoverage.py"
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
    throw "XML documentation coverage audit is missing: $scriptPath"
}

$result = Invoke-PublisherStudioPythonScript -ScriptPath $scriptPath -Arguments @((Join-Path $repositoryRoot "src"))
$result.Output | ForEach-Object { Write-Host $_ }
if ($result.ExitCode -ne 0) {
    throw "XML documentation coverage validation failed with exit code $($result.ExitCode)."
}
