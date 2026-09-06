[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$unsupportedContainsPattern = '\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::'
$unsupportedPathRelativePattern = '\[(?:System\.)?IO\.Path\]::GetRelativePath\s*\('
$unsupportedArgumentListPattern = '\.ArgumentList(?:\.|\s*=)'
$unsupportedKillTreePattern = '\.Kill\(\s*\$true\s*\)'
$readOnlyPlatformVariableAssignmentPattern = '(?i)\$(?:IsWindows|IsLinux|IsMacOS|IsCoreCLR)\s*='
$failures = [System.Collections.Generic.List[string]]::new()

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return $Path.Substring($root.Length).TrimStart([char[]]'\/').Replace('\', '/')
}

function Read-RepositoryScriptText {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    $relative = Get-RepositoryRelativePath -Path $File.FullName
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            return [System.IO.File]::ReadAllText($File.FullName)
        }
        catch [System.IO.FileNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because it disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
        catch [System.IO.DirectoryNotFoundException] {
            if ($attempt -lt 2) {
                Start-Sleep -Milliseconds 50
                continue
            }

            throw "PowerShell compatibility validation could not read source script '$relative' because its directory disappeared during validation. Re-run the build after checking for a concurrent checkout, cleanup, or generator process."
        }
    }
}

# Parse every maintained repository PowerShell script before release preparation gets expensive.
# PublisherStudio still contains reviewed legacy Join-Path calls with backslash child paths that
# are known to run under pwsh on its supported hosts, so this validator intentionally does not
# impose LocalGPT's stricter style-only Join-Path rule. It does enforce parser correctness and
# compatibility hazards that can otherwise fail late in a release.
$scriptFiles = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $isPowerShellScript =
        [string]::Equals($_.Extension, '.ps1', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($_.Extension, '.psm1', [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $isPowerShellScript) {
        return $false
    }

    $relative = Get-RepositoryRelativePath -Path $_.FullName
    return $relative -notmatch '(^|/)(\.git|\.vs|artifacts|bin|obj|packages|node_modules)(/|$)' -and
        $relative -notmatch '^docs/_site(/|$)'
}

foreach ($file in $scriptFiles) {
    $content = Read-RepositoryScriptText -File $file

    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseInput(
        $content,
        [ref]$tokens,
        [ref]$parseErrors)
    foreach ($parseError in @($parseErrors)) {
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $line = $parseError.Extent.StartLineNumber
        $message = $parseError.Message
        $failures.Add("${relative}:$line has a PowerShell parser error: $message")
    }

    foreach ($match in [regex]::Matches($content, $unsupportedContainsPattern)) {
        $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
        $relative = Get-RepositoryRelativePath -Path $file.FullName
        $failures.Add("${relative}:$line uses String.Contains(value, StringComparison), which is unavailable in Windows PowerShell 5.1. Use String.IndexOf(value, comparison) instead.")
    }

    foreach ($compatibilityPattern in @(
        [pscustomobject]@{ Pattern = $unsupportedPathRelativePattern; Message = 'uses Path.GetRelativePath directly, which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable reflection/URI helper instead.' },
        [pscustomobject]@{ Pattern = $unsupportedArgumentListPattern; Message = 'uses ProcessStartInfo.ArgumentList directly, which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable process-argument helper instead.' },
        [pscustomobject]@{ Pattern = $unsupportedKillTreePattern; Message = 'uses Process.Kill(true), which is unavailable on Windows PowerShell 5.1/.NET Framework. Use the portable process-stop helper instead.' }
    )) {
        foreach ($match in [regex]::Matches($content, $compatibilityPattern.Pattern)) {
            $line = [regex]::Matches($content.Substring(0, $match.Index), "`r`n|`r|`n").Count + 1
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$line $($compatibilityPattern.Message)")
        }
    }

    $sourceLines = $content -split "`r`n|`r|`n"
    for ($lineIndex = 0; $lineIndex -lt $sourceLines.Length; $lineIndex++) {
        if ([regex]::IsMatch($sourceLines[$lineIndex], $readOnlyPlatformVariableAssignmentPattern)) {
            $relative = Get-RepositoryRelativePath -Path $file.FullName
            $failures.Add("${relative}:$($lineIndex + 1) assigns to a PowerShell 7 read-only platform automatic variable (IsWindows/IsLinux/IsMacOS/IsCoreCLR). Use a repository-specific variable name instead; PowerShell variable names are case-insensitive.")
        }
    }
}

if ($failures.Count -gt 0) {
    throw "PowerShell compatibility validation failed:`n - $($failures -join "`n - ")"
}

Write-Host 'PowerShell compatibility validation passed for Windows PowerShell 5.1 and modern pwsh parser/runtime API usage, cross-platform path handling, and protected platform automatic-variable assignments.'
