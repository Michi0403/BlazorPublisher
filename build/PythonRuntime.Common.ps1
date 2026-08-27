Set-StrictMode -Version Latest

function Resolve-PublisherStudioPythonRuntime {
    param([switch]$AllowMissing)

    foreach ($commandName in @('python', 'python3')) {
        $command = Get-Command $commandName -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $command) {
            return [pscustomobject]@{
                Executable = [string]$command.Source
                PrefixArguments = @()
                DisplayName = [string]$command.Source
            }
        }
    }

    $launcher = Get-Command py -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $launcher) {
        return [pscustomobject]@{
            Executable = [string]$launcher.Source
            PrefixArguments = @('-3')
            DisplayName = "$($launcher.Source) -3"
        }
    }

    if ($AllowMissing) {
        return $null
    }

    throw 'Python 3 is required. Install/provide python, python3, or the Windows py launcher.'
}

function Invoke-PublisherStudioPythonScript {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [string[]]$Arguments = @(),
        [switch]$AllowMissing
    )

    $runtime = Resolve-PublisherStudioPythonRuntime -AllowMissing:$AllowMissing
    if ($null -eq $runtime) {
        return $null
    }

    $invokeArguments = [System.Collections.Generic.List[string]]::new()
    foreach ($argument in @($runtime.PrefixArguments)) {
        $invokeArguments.Add([string]$argument)
    }
    $invokeArguments.Add($ScriptPath)
    foreach ($argument in @($Arguments)) {
        $invokeArguments.Add([string]$argument)
    }

    $nativeArguments = $invokeArguments.ToArray()
    $output = @(& $runtime.Executable @nativeArguments 2>&1)
    $exitCode = [int]$LASTEXITCODE

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = $output
        Runtime = $runtime
    }
}
