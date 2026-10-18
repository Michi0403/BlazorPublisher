Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) { throw $Message }
function Normalize-Signature([string]$Value) { return ([regex]::Replace($Value, '\s+', ' ')).Trim() }
function Get-MethodRecords([string]$Text) {
    $pattern = '(?ms)^[ \t]*(?<signature>(?:public|private|protected|internal)\s+(?!(?:(?:static|async|virtual|override|sealed|partial|new)\s+)*(?:class|struct|record|interface|enum)\b)(?:(?:static|async|virtual|override|sealed|partial|new)\s+)*(?:[\w\.\?<>,\[\]]+\s+)+(?<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*(?:where[^\{=>\r\n]+)?\s*)(?<body>=>|\{)'
    $records = @()
    foreach ($match in [regex]::Matches($Text, $pattern)) {
        $bodyStart = $match.Groups['body'].Index
        $body = $null
        if ($match.Groups['body'].Value -eq '=>') {
            $end = $Text.IndexOf(';', $bodyStart)
            if ($end -lt 0) { continue }
            $body = $Text.Substring($bodyStart, $end - $bodyStart + 1)
        }
        else {
            $depth = 0
            $end = -1
            for ($index = $bodyStart; $index -lt $Text.Length; $index++) {
                $character = $Text[$index]
                if ($character -eq '{') { $depth++ }
                elseif ($character -eq '}') {
                    $depth--
                    if ($depth -eq 0) { $end = $index; break }
                }
            }
            if ($end -lt 0) { continue }
            $body = $Text.Substring($bodyStart, $end - $bodyStart + 1)
        }
        $records += [pscustomobject]@{
            Signature = Normalize-Signature $match.Groups['signature'].Value
            Name = $match.Groups['name'].Value
            Body = $body
        }
    }
    return $records
}

$root = Split-Path -Parent $PSScriptRoot
function Get-BaselineRelativePath([string]$RelativePath) {
    if ($RelativePath -match '^(?<prefix>.+?)(?:\.[^/]+)\.cs$') {
        $candidate = $Matches['prefix'] + '.cs'
        if (Test-Path -LiteralPath (Join-Path $root $candidate.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)) -PathType Leaf) { return $candidate }
    }
    return $RelativePath
}
$failures = New-Object System.Collections.Generic.List[string]
$iteratorCount = 0
$files = @(Get-ChildItem -LiteralPath (Join-Path $root 'src/PublisherStudio.Web') -Recurse -File | Where-Object { $_.Extension -in @('.cs','.razor') -and $_.FullName -notmatch '[\\/](?:bin|obj|Migrations)[\\/]' -and $_.Name -notlike '*.Designer.cs' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@([char]'\', [char]'/')).Replace([char]'\', [char]'/')
    $baselineRelative = Get-BaselineRelativePath $relative
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($method in Get-MethodRecords $text) {
        $body = [string]$method.Body
        if (-not [regex]::IsMatch($body, '\byield\s+(?:return|break)\b')) { continue }
        $iteratorCount++
        $id = "$relative $($method.Name)"
        if ([regex]::IsMatch($body, '\bcatch\b')) { $failures.Add("${id}: iterator contains catch, which is forbidden around yield.") }
        if (-not [regex]::IsMatch($body, '\btry\b') -or -not [regex]::IsMatch($body, '\bfinally\b')) {
            $failures.Add("${id}: iterator must use try/finally to own its completion/cleanup.")
        }
        $logPattern = '\b(?:Logger|logger|_logger|[A-Za-z_]\w*Logger)\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(|\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\('
        if (-not [regex]::IsMatch($body, $logPattern)) {
            $failures.Add("${id}: iterator requires structured logging or Trace diagnostics.")
        }
    }
}
if ($failures.Count -gt 0) {
    Write-Host 'Iterator exception policy validation failed:'
    foreach ($failure in $failures | Sort-Object -Unique) { Write-Host "  - $failure" }
    Fail "Iterator exception policy validation failed with $($failures.Count) problem(s). Yield methods require logged try/finally and may not contain catch."
}
Write-Host "PowerShell iterator exception policy passed for $iteratorCount iterator method(s): logged try/finally without catch."
