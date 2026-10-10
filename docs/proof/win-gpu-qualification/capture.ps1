$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$records = [System.Collections.Generic.List[object]]::new()

function Invoke-Captured {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $stdoutPath = Join-Path $proofRoot "$Name.stdout.txt"
    $stderrPath = Join-Path $proofRoot "$Name.stderr.txt"
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    $startedUtc = [DateTimeOffset]::UtcNow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    $completed = $process.WaitForExit($TimeoutSeconds * 1000)
    if (-not $completed) {
        $process.Kill($true)
        $process.WaitForExit()
    } else {
        $process.WaitForExit()
    }
    $process.Refresh()
    $numericExit = if ($completed -and $process.HasExited) { [int]$process.ExitCode } else { $null }
    $stopwatch.Stop()
    $records.Add([ordered]@{
        name = $Name
        file = $FilePath
        argv = $ArgumentList
        cwd = (Get-Location).Path
        started_utc = $startedUtc.ToString('o')
        ended_utc = [DateTimeOffset]::UtcNow.ToString('o')
        elapsed_ms = $stopwatch.ElapsedMilliseconds
        timeout_seconds = $TimeoutSeconds
        timed_out = -not $completed
        exit_code = $numericExit
        stdout = [IO.Path]::GetFileName($stdoutPath)
        stderr = [IO.Path]::GetFileName($stderrPath)
        capture_encoding = 'Start-Process redirected text; exact child byte encoding not independently established'
    })
}

Invoke-Captured -Name 'host-gpu' -FilePath 'nvidia-smi.exe' -ArgumentList @(
    '--query-gpu=name,driver_version,memory.total,compute_cap',
    '--format=csv,noheader'
) -TimeoutSeconds 15

Invoke-Captured -Name 'wsl-version' -FilePath 'wsl.exe' -ArgumentList @('--version') -TimeoutSeconds 15
Invoke-Captured -Name 'wsl-list' -FilePath 'wsl.exe' -ArgumentList @('--list', '--verbose') -TimeoutSeconds 15

$wslScript = '/mnt/c/Projects/CFD-Workbench-win-gpu-qualification-plan/docs/proof/win-gpu-qualification/inspect-wsl.sh'
Invoke-Captured -Name 'wsl-inventory' -FilePath 'wsl.exe' -ArgumentList @(
    '--distribution', 'cfdw-openfoam2512', '--', 'bash', $wslScript
) -TimeoutSeconds 30

$capture = [ordered]@{
    schema = 'cfdw-read-only-capture/1'
    authority = 'Ruling 191'
    scope = 'GPU, WSL, installed compute stack and OpenFOAM metadata only; active L3 paths, units, processes and files excluded'
    wrapper = 'capture.ps1'
    wrapper_sha256_before_execution = (Get-FileHash -Algorithm SHA256 -LiteralPath $MyInvocation.MyCommand.Path).Hash.ToLowerInvariant()
    records = $records
}
$capture | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $proofRoot 'capture.json') -Encoding utf8
