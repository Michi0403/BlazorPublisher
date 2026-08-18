param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $RepositoryRoot 'src\PublisherStudio.Web'
$servicesRoot = Join-Path $sourceRoot 'Services'
$errors = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $servicesRoot -PathType Container)) {
    throw "Service architecture validation failed: Services directory not found: $servicesRoot"
}

$serviceFiles = Get-ChildItem -LiteralPath $servicesRoot -Recurse -File -Filter '*.cs'
foreach ($file in $serviceFiles) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $text = Get-Content -LiteralPath $file.FullName -Raw

    $staticClasses = [regex]::Matches($text, '(?m)^\s*(?:public|internal|private|protected)?\s*static\s+class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($match in $staticClasses) {
        $name = $match.Groups['name'].Value
        $isApprovedHelper = ($relative.IndexOf('/Services/Helpers/', [StringComparison]::Ordinal) -ge 0) -and $name.EndsWith('Helper', [StringComparison]::Ordinal)
        if (-not $isApprovedHelper) {
            $errors.Add("Static runtime class '$name' is not an approved pure helper: $relative")
        }
    }

    if ($text -match '(?m)^\s*(?:public|internal)\s+static\s+class\s+[A-Za-z_][A-Za-z0-9_]*(Service|Client|Registry|Runner)\b') {
        $errors.Add("Runtime services/clients/registries/runners must be DI instances, not static classes: $relative")
    }

    if ($text -match '(?m)^\s*(?:public|internal|private|protected)?\s*static\s+(?:readonly\s+)?(?:HashSet|Dictionary|List|ConcurrentDictionary|ConcurrentQueue|ConcurrentBag|ObservableCollection)<[^>]+>\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|\{)') {
        $errors.Add("Mutable collection state must not be stored in static service fields; use an immutable/frozen catalog or a DI-owned instance: $relative")
    }
}

$maintainedFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    $_.Extension -in @('.cs', '.razor') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}
foreach ($file in $maintainedFiles) {
    $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@('\', '/')).Replace([char]'\', [char]'/')
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text -match '(?m)^\s*_\s*=(?!>)\s*(?!await\b)[^;\r\n]*(?:Async\s*\(|InvokeAsync\s*\(|Task\.Run\s*\()') {
        $errors.Add("Discarded asynchronous work is forbidden; await it or use ISupervisedTaskRunner: $relative")
    }

    if ($text -match '(?m)^\s*new\s+SupervisedTaskRunner\s*\(') {
        $errors.Add("SupervisedTaskRunner must be resolved through DI, not manually constructed: $relative")
    }
}


$registrationPath = Join-Path $sourceRoot 'PublisherStudioServiceCollectionExtensions.cs'
$registrationText = Get-Content -LiteralPath $registrationPath -Raw
if ($registrationText.IndexOf('AddSingleton<ISupervisedTaskRunner, SupervisedTaskRunner>(services);', [StringComparison]::Ordinal) -lt 0) {
    $errors.Add('PublisherStudio must retain singleton DI registration for ISupervisedTaskRunner -> SupervisedTaskRunner.')
}

$methodGuard = Join-Path $PSScriptRoot 'Assert-MethodDiagnostics.ps1'
$methodGuardText = Get-Content -LiteralPath $methodGuard -Raw
foreach ($token in @('audit_service_resilience.py', '--product publisherstudio')) {
    if ($methodGuardText.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
        $errors.Add("Broad service-method try/catch enforcement must remain wired through Assert-MethodDiagnostics.ps1: missing '$token'.")
    }
}

$serviceAudit = Join-Path $PSScriptRoot 'audit_service_resilience.py'
if (-not (Test-Path -LiteralPath $serviceAudit -PathType Leaf)) {
    $errors.Add('Broad service resilience audit is missing.')
}

if ($errors.Count -gt 0) {
    $errors | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
    throw "Service architecture validation failed with $($errors.Count) problem(s)."
}

Write-Host 'Service architecture validation passed: DI/static-state rules, ISupervisedTaskRunner ownership, and zero-exemption service resilience enforcement remain intact.' -ForegroundColor Green
