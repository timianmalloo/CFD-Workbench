param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$PrecheckId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$resultPath = Join-Path $proofRoot 'launcher-precheck.json'
$distro = 'cfdw-openfoam2512'
$linuxRoot = '/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1'
$script = "$linuxRoot/launcher-precheck.sh"
$common = @(
    '--distribution', $distro, '--user', 'root', '--exec',
    '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10'
)

function Invoke-Pgrep([string]$Marker, [string]$Label) {
    $stdoutPath = Join-Path $proofRoot "launcher-precheck-$Label-pgrep.stdout.txt"
    $stderrPath = Join-Path $proofRoot "launcher-precheck-$Label-pgrep.stderr.txt"
    $process = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/pgrep', '-af', '--', $Marker
    ) -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(5000)) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "pgrep timed out for $Label"
    }
    if ($process.ExitCode -notin @(0, 1)) { throw "pgrep failed for $Label with exit $($process.ExitCode)" }
    return [ordered]@{
        exit_code = $process.ExitCode
        found = $process.ExitCode -eq 0
        stdout = Split-Path -Leaf $stdoutPath
        stderr = Split-Path -Leaf $stderrPath
    }
}

function Stop-TaggedSleep([string]$Marker, [string]$Label) {
    $stdoutPath = Join-Path $proofRoot "launcher-precheck-$Label-cleanup.stdout.txt"
    $stderrPath = Join-Path $proofRoot "launcher-precheck-$Label-cleanup.stderr.txt"
    $process = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/pkill', '-TERM', '-f', '--', $Marker
    ) -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(5000)) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "cleanup timed out for $Label"
    }
    if ($process.ExitCode -notin @(0, 1)) { throw "cleanup failed for $Label with exit $($process.ExitCode)" }
    Start-Sleep -Milliseconds 300
    return [ordered]@{
        exit_code = $process.ExitCode
        stdout = Split-Path -Leaf $stdoutPath
        stderr = Split-Path -Leaf $stderrPath
        after = Invoke-Pgrep $Marker "$Label-after-cleanup"
    }
}

function Invoke-Case([string]$Label, [bool]$UseSetsid) {
    $attemptId = "$PrecheckId-$Label"
    $marker = "cfdw-b1-launcher-precheck-$attemptId"
    $stdoutPath = Join-Path $proofRoot "launcher-precheck-$Label.stdout.txt"
    $stderrPath = Join-Path $proofRoot "launcher-precheck-$Label.stderr.txt"
    $arguments = @($common)
    if ($UseSetsid) { $arguments += @('/usr/bin/setsid', '--wait') }
    $arguments += @('/bin/bash', $script, '--attempt-id', $attemptId)
    $process = Start-Process -FilePath 'wsl.exe' -ArgumentList $arguments `
        -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru -WindowStyle Hidden
    $ready = $false
    $clock = [Diagnostics.Stopwatch]::StartNew()
    while (-not $ready -and $clock.ElapsedMilliseconds -lt 5000) {
        if (Test-Path -LiteralPath $stdoutPath) {
            $ready = (Get-Content -LiteralPath $stdoutPath -Raw) -match [regex]::Escape("precheck_ready=$marker")
        }
        if (-not $ready) { Start-Sleep -Milliseconds 50 }
    }
    $sleepBeforeStop = Invoke-Pgrep $marker "$Label-before-stop"
    if (-not $ready -or -not $sleepBeforeStop.found) {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
        if ($sleepBeforeStop.found) { Stop-TaggedSleep $marker "$Label-failed" | Out-Null }
        throw "precheck did not establish identity and tagged sleep for $Label"
    }
    $launcherExitedBeforeKill = $process.HasExited
    $launcherExitBeforeKill = if ($launcherExitedBeforeKill) { $process.ExitCode } else { 'Still running' }
    if (-not $launcherExitedBeforeKill) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(5000) | Out-Null
    }
    Start-Sleep -Milliseconds 300
    $residual = Invoke-Pgrep $marker $Label
    $cleanup = if ($residual.found) { Stop-TaggedSleep $marker $Label } else { 'Not needed' }
    if ($cleanup -isnot [string] -and $cleanup.after.found) { throw "tagged precheck process survived cleanup for $Label" }
    return [ordered]@{
        argv = @('wsl.exe') + $arguments
        marker = $marker
        identity_observed = $ready
        tagged_sleep_before_windows_launcher_stop = $sleepBeforeStop
        launcher_exited_before_kill = $launcherExitedBeforeKill
        launcher_exit_before_kill = $launcherExitBeforeKill
        residual_after_windows_launcher_stop = $residual
        cleanup = $cleanup
        stdout = Split-Path -Leaf $stdoutPath
        stderr = Split-Path -Leaf $stderrPath
    }
}

$foreground = Invoke-Case 'foreground' $false
$setsidWait = Invoke-Case 'setsid-wait' $true
if (-not $foreground.identity_observed -or -not $setsidWait.identity_observed) {
    throw 'precheck did not capture both process identities'
}
$selectedMethod = if (-not $foreground.residual_after_windows_launcher_stop.found) { 'foreground' } else { 'setsid-wait' }
$document = [ordered]@{
    schema = 'cfdw-b1-launcher-precheck/1'
    authority = 'Ruling 201'
    precheck_id = $PrecheckId
    captured_utc = [DateTimeOffset]::UtcNow.ToString('o')
    selected_method = $selectedMethod
    selection_rule = 'foreground when stopping wsl.exe removes its tagged Linux child; otherwise setsid --wait'
    foreground = $foreground
    setsid_wait = $setsidWait
}
$json = ($document | ConvertTo-Json -Depth 10) -replace "`r`n", "`n"
[IO.File]::WriteAllText(
    $resultPath,
    $json + "`n",
    [Text.UTF8Encoding]::new($false)
)
Write-Output "selected_method=$selectedMethod result=$(Split-Path -Leaf $resultPath)"
