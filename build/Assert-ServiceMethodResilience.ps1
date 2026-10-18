param([string]$RepositoryRoot = '')
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $PSCommandPath) '..'))
}

$ErrorActionPreference = 'Stop'
$services = Join-Path $RepositoryRoot 'src/PublisherStudio.Web/Services'
if (-not (Test-Path -LiteralPath $services -PathType Container)) { throw 'PublisherStudio service source directory is missing.' }
$errors = [Collections.Generic.List[string]]::new()
$inspected = 0
$iterators = 0
# Preserve offsets while ignoring C# strings, char literals and comments. This is a
# repository policy heuristic; the actual .NET compiler remains the syntax authority.
function Mask-NonCode([string]$Text) {
    $pattern = '(?s)\$*@?"{3,}.*?"{3,}|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*''|/\*.*?\*/|(?m://[^\r\n]*)'
    return [regex]::Replace($Text,$pattern, { param($match) (' ' * $match.Length) })
}
$methodPattern = '(?ms)^[ \t]*(?:public|private|protected|internal)\s+(?!(?:(?:static|async|virtual|override|sealed|partial|new|unsafe|readonly)\s+)*(?:class|record|struct|interface|enum|delegate)\b)(?:(?:static|async|virtual|override|sealed|partial|new|unsafe)\s+)*(?:[\w\.\?<>,\[\]]+\s+)+(?<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*(?:where[^\{=>\r\n]+)?\s*(?<body>=>|\{)'
$loggingPattern = '\b[A-Za-z_]\w*\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(|\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\('
foreach ($file in Get-ChildItem -LiteralPath $services -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\/](?:bin|obj|Migrations)[\/]' -and $_.Name -notlike '*.Designer.cs' }) {
    $original = [IO.File]::ReadAllText($file.FullName)
    $masked = Mask-NonCode $original
    foreach ($method in [regex]::Matches($masked,$methodPattern)) {
        $start = $method.Groups['body'].Index
        if ($method.Groups['body'].Value -eq '=>') {
            $end = $masked.IndexOf(';',$start)
        }
        else {
            $depth = 0; $end = -1
            for ($i = $start; $i -lt $masked.Length; $i++) {
                if ($masked[$i] -eq '{') { $depth++ }
                elseif ($masked[$i] -eq '}') {
                    $depth--
                    if ($depth -eq 0) { $end = $i; break }
                }
            }
        }
        if ($end -lt $start) { continue }
        $maskedBody = $masked.Substring($start,$end - $start + 1)
        $body = $original.Substring($start,$end - $start + 1)
        $name = $method.Groups['name'].Value
        $relative = $file.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]@([char]'\',[char]'/')).Replace('\','/')
        $line = 1 + [regex]::Matches($original.Substring(0,$method.Index),"`n").Count
        $id = "${relative}:$line $name"
        if ($maskedBody -match '\byield\s+(?:return|break)\b') {
            $iterators++
            if ($maskedBody -notmatch '\btry\b' -or $maskedBody -notmatch '\bfinally\b' -or $maskedBody -match '\bcatch\b') {
                $errors.Add("${id}: iterator requires logged try/finally and cannot contain catch.")
            }
        }
        else {
            $inspected++
            if ($maskedBody -notmatch '\btry\b' -or $maskedBody -notmatch '\bcatch\b') {
                $errors.Add("${id}: service reaction lacks method-local try/catch.")
            }
        }
        if ($body -notmatch $loggingPattern) { $errors.Add("${id}: service reaction lacks ILogger/Trace diagnostics.") }
    }
}
if ($errors.Count) {
    $errors | Select-Object -First 100 | ForEach-Object { Write-Error $_ }
    throw "PublisherStudio service method resilience rejected $($errors.Count) finding(s)."
}
Write-Host "PublisherStudio PowerShell service resilience passed: $inspected methods with try/catch and diagnostics; $iterators logged iterators with try/finally."
