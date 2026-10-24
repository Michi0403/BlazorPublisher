[CmdletBinding()]
param([string]$RepositoryRoot = '')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}
$componentsRoot = Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Components'
$editorPath = Join-Path $componentsRoot 'Pages/Editor.razor'
if (-not (Test-Path -LiteralPath $editorPath -PathType Leaf)) {
    throw "PublisherStudio editor source is missing: $editorPath"
}

# Resolve authored application components from source, not a hardcoded list of
# editor children. A successful C# compilation does not necessarily catch an
# unknown Razor component parameter until Blazor renders the affected view.
$components = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
$razorSources = @(Get-ChildItem -LiteralPath $componentsRoot -Recurse -File -Filter '*.razor')
foreach ($source in $razorSources) {
    $name = [IO.Path]::GetFileNameWithoutExtension($source.Name)
    if ($components.ContainsKey($name)) {
        throw "Duplicate PublisherStudio Razor component name '$name' cannot be resolved safely."
    }
    $components.Add($name, $source.FullName)
}

$errors = [Collections.Generic.List[string]]::new()
$inspectedBindings = 0
$inspectedInvocations = 0
$editorInvocations = 0
foreach ($source in $razorSources) {
    $markup = [IO.File]::ReadAllText($source.FullName)
    $seenNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($invocation in [regex]::Matches($markup, '<(?<name>[A-Z][A-Za-z0-9_]*)\b')) {
        $name = $invocation.Groups['name'].Value
        if (-not $seenNames.Add($name) -or -not $components.ContainsKey($name) -or $name -eq [IO.Path]::GetFileNameWithoutExtension($source.Name)) {
            continue
        }
        # Restrict to self-closing component tags; do not mistake children of a
        # container component for its attributes. Quoted '>' is valid in a value.
        $tagPattern = '<' + [regex]::Escape($name) + '\b(?<attrs>(?:"[^"]*"|''[^'']*''|[^>"''])*?)\s*/>'
        $tags = [regex]::Matches($markup, $tagPattern, [Text.RegularExpressions.RegexOptions]::Singleline)
        if ($tags.Count -eq 0) {
            continue
        }
        $componentPath = $components[$name]
        $componentDirectory = [IO.Path]::GetDirectoryName($componentPath)
        $declarations = [IO.File]::ReadAllText($componentPath)
        # Split Razor partials such as MediaStudio.StateAndParameters.razor.cs
        # are equally authoritative for [Parameter] declarations.
        $partials = @(Get-ChildItem -LiteralPath $componentDirectory -File -Filter "$name*.razor.cs" | Where-Object {
            $_.Name -eq "$name.razor.cs" -or $_.Name.StartsWith("$name.", [StringComparison]::Ordinal)
        })
        foreach ($partial in $partials) {
            $declarations += "`n" + [IO.File]::ReadAllText($partial.FullName)
        }
        $parameters = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($parameter in [regex]::Matches($declarations, '(?s)\[\s*Parameter(?:\s*,[^\]]*)?\]\s+public\s+[^\r\n{;=]+?\s+(?<name>[A-Za-z_]\w*)\s*\{')) {
            [void]$parameters.Add($parameter.Groups['name'].Value)
        }
        foreach ($tag in $tags) {
            $inspectedInvocations++
            if ($source.FullName -eq $editorPath) { $editorInvocations++ }
            $attrs = $tag.Groups['attrs'].Value
            foreach ($attribute in [regex]::Matches($attrs, '(?<![@\w-])(?<name>[A-Za-z_]\w*)\s*=\s*(?:"[^"]*"|''[^'']*'')')) {
                $key = $attribute.Groups['name'].Value
                $inspectedBindings++
                if (-not $parameters.Contains($key)) {
                    $lineNumber = 1 + ([regex]::Matches($markup.Substring(0, $tag.Index), "`n")).Count
                    $relativePath = $source.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
                    $errors.Add("${relativePath}:$lineNumber passes unknown '$key' to '$name'. Declare a real [Parameter] on the component or remove the invalid binding.")
                }
            }
        }
    }
}
if ($editorInvocations -lt 15 -or $inspectedInvocations -lt 70 -or $inspectedBindings -lt 100) {
    $errors.Add("Component contract coverage is unexpectedly low: editor=$editorInvocations; invocations=$inspectedInvocations; bindings=$inspectedBindings.")
}
if ($errors.Count -gt 0) {
    throw "PublisherStudio Razor component parameter contract validation failed:`n - $($errors -join "`n - ")"
}
Write-Host "PublisherStudio component parameter contracts passed: $inspectedInvocations component invocation(s), $inspectedBindings binding(s) across $($razorSources.Count) Razor source(s), $editorInvocations editor invocation(s)." -ForegroundColor Green
