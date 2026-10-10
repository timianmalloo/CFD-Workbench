param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ObserverSession
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$samplesPath = Join-Path (Split-Path -Parent $proofRoot) 'win-l3-observer\samples.jsonl'
$eventsPath = Join-Path $proofRoot 'guard-events.jsonl'
$statePath = Join-Path $proofRoot 'observer-live.state.json'
$stopRequest = Join-Path $proofRoot 'observer-stop.request'
$capturePath = Join-Path $proofRoot 'capture.json'
$observerOut = Join-Path $proofRoot 'observer.stdout.txt'
$observerErr = Join-Path $proofRoot 'observer.stderr.txt'
$probeOut = Join-Path $proofRoot 'b1.stdout.txt'
$probeErr = Join-Path $proofRoot 'b1.stderr.txt'
$probePidPath = Join-Path $proofRoot 'b1.pid'
$probeAttemptPath = Join-Path $proofRoot 'b1.attempt'
$probeCancelPath = Join-Path $proofRoot 'probe-cancel.request'
$checkOut = Join-Path $proofRoot 'check-owned.stdout.txt'
$checkErr = Join-Path $proofRoot 'check-owned.stderr.txt'
$stopOut = Join-Path $proofRoot 'stop-owned.stdout.txt'
$stopErr = Join-Path $proofRoot 'stop-owned.stderr.txt'
$distro = 'cfdw-openfoam2512'
$linuxRoot = '/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1'
$observerScript = "$linuxRoot/observe-live.sh"
$probeScript = "$linuxRoot/inspect-b1.sh"
$stopScript = "$linuxRoot/stop-owned.sh"
$prelaunchBudgetMilliseconds = 750000
$workloadBudgetMilliseconds = 600000
$terminationReserveMilliseconds = 10000
$observerCount = 3

Remove-Item -LiteralPath @(
    $eventsPath, $statePath, $stopRequest, $capturePath, $observerOut, $observerErr,
    $probeOut, $probeErr, $probePidPath, $probeAttemptPath, $probeCancelPath,
    $checkOut, $checkErr, $stopOut, $stopErr
) -Force -ErrorAction SilentlyContinue

$scriptNames = @(
    'bind-prefix.py', 'capture.ps1', 'observe-live.sh', 'observer_guard.py',
    'inspect-b1.sh', 'resolve_packages.py', 'stop-owned.sh', 'wait-observer.ps1'
)
$scriptHashesBeforeExecution = [ordered]@{}
foreach ($name in $scriptNames) {
    $scriptHashesBeforeExecution[$name] = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $proofRoot $name)).Hash.ToLowerInvariant()
}

function Read-JsonFile([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Read-CompleteGuardEvents {
    if (-not (Test-Path -LiteralPath $eventsPath)) { return @() }
    $raw = [IO.File]::ReadAllText($eventsPath)
    $lastLf = $raw.LastIndexOf("`n", [StringComparison]::Ordinal)
    if ($lastLf -lt 0) { return @() }
    $complete = $raw.Substring(0, $lastLf)
    $parsed = @()
    foreach ($line in $complete.Split("`n")) {
        $candidate = $line.TrimEnd("`r")
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        $parsed += $candidate | ConvertFrom-Json
    }
    return @($parsed)
}

function Get-SessionEvents {
    return @(Read-CompleteGuardEvents | Where-Object { $_.session_id -eq $ObserverSession })
}

$initialRows = @(Get-Content -LiteralPath $samplesPath).Count
$startRow = $initialRows + 1
$startedUtc = [DateTimeOffset]::UtcNow.ToString('o')
$prelaunchClock = [Diagnostics.Stopwatch]::StartNew()
$workloadClock = $null
$observer = $null
$probe = $null
$baselineEvent = $null
$wrapperError = $null
$probeTimedOut = $false
$guardStoppedProbe = $false
$guardStopReason = $null
$terminationExit = $null
$startupAcknowledged = $false
$startupCheckExit = $null

function Remaining-WorkloadMilliseconds {
    if ($null -eq $workloadClock) { return $workloadBudgetMilliseconds }
    $remaining = $workloadBudgetMilliseconds - [int][Math]::Ceiling($workloadClock.Elapsed.TotalMilliseconds)
    if ($remaining -lt 0) { return 0 }
    return $remaining
}

function Request-ProbeCancel {
    [IO.File]::WriteAllText($probeCancelPath, $ObserverSession + "`n", [Text.UTF8Encoding]::new($false))
}

function Invoke-OwnedControl([string]$Mode) {
    $remaining = Remaining-WorkloadMilliseconds
    if ($remaining -le 0) { return $null }
    $waitBudget = $remaining
    if ($Mode -eq 'check') {
        $waitBudget = [Math]::Min(5000, [Math]::Max(1, $remaining - $terminationReserveMilliseconds))
    }
    $arguments = @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $stopScript, '--attempt-id', $ObserverSession
    )
    $outputPath = $stopOut
    $errorPath = $stopErr
    if ($Mode -eq 'check') {
        $arguments += '--check'
        $outputPath = $checkOut
        $errorPath = $checkErr
    }
    $control = Start-Process -FilePath 'wsl.exe' -ArgumentList $arguments -RedirectStandardOutput $outputPath -RedirectStandardError $errorPath -PassThru -WindowStyle Hidden
    if (-not $control.WaitForExit($waitBudget)) {
        Stop-Process -Id $control.Id -Force -ErrorAction SilentlyContinue
        return $null
    }
    return $control.ExitCode
}

function Invoke-OwnedStop {
    Request-ProbeCancel
    while ((-not (Test-Path -LiteralPath $probePidPath) -or -not (Test-Path -LiteralPath $probeAttemptPath)) -and
        $null -ne $probe -and -not $probe.HasExited -and (Remaining-WorkloadMilliseconds) -gt 0) {
        Start-Sleep -Milliseconds 100
    }
    if ((Test-Path -LiteralPath $probePidPath) -and (Test-Path -LiteralPath $probeAttemptPath)) {
        $stopResult = Invoke-OwnedControl 'stop'
        return $stopResult
    }
    if ($null -ne $probe -and $probe.HasExited) {
        return 0
    }
    if ($null -ne $probe) { Stop-Process -Id $probe.Id -Force -ErrorAction SilentlyContinue }
    return 75
}

try {
    $observer = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10', '/bin/bash', $observerScript,
        '--count', "$observerCount", '--start-row', "$startRow", '--session-id', $ObserverSession
    ) -RedirectStandardOutput $observerOut -RedirectStandardError $observerErr -PassThru -WindowStyle Hidden

    while ($null -eq $baselineEvent) {
        if ($observer.HasExited) { throw "observer exited before fresh baseline, exit $($observer.ExitCode)" }
        $sessionEvents = @(Get-SessionEvents)
        $stopEvent = $sessionEvents | Where-Object { $_.verdict -eq 'stop' } | Select-Object -First 1
        if ($null -ne $stopEvent) { throw "guard stopped during fresh baseline: $($stopEvent.reason)" }
        $baselineEvent = $sessionEvents | Where-Object { $_.verdict -eq 'baseline-ready' } | Select-Object -Last 1
        if ($prelaunchClock.ElapsedMilliseconds -ge $prelaunchBudgetMilliseconds) {
            throw 'fresh baseline exceeded its 750-second host deadline'
        }
        if ($null -eq $baselineEvent) { Start-Sleep -Milliseconds 200 }
    }

    $stateAtLaunch = Read-JsonFile $statePath
    if ($stateAtLaunch.state -ne 'running' -or
        $stateAtLaunch.session_id -ne $ObserverSession -or
        [int]$stateAtLaunch.start_row -ne $startRow) {
        throw 'observer state does not match the live B1 session'
    }
    $sampleAge = [DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse([string]$stateAtLaunch.last_sample_utc)
    if ($sampleAge.TotalSeconds -lt -120 -or $sampleAge.TotalSeconds -gt 720) {
        throw "fresh baseline sample age is invalid: $($sampleAge.TotalSeconds) seconds"
    }

    $prelaunchClock.Stop()
    $workloadClock = [Diagnostics.Stopwatch]::StartNew()
    $probe = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/setsid', '/bin/bash', $probeScript, '--attempt-id', $ObserverSession
    ) -RedirectStandardOutput $probeOut -RedirectStandardError $probeErr -PassThru -WindowStyle Hidden

    $startupClock = [Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-Path -LiteralPath $probePidPath) -or -not (Test-Path -LiteralPath $probeAttemptPath)) {
        if ($probe.HasExited) { throw "probe exited before ownership acknowledgement, exit $($probe.ExitCode)" }
        if ($observer.HasExited) {
            Request-ProbeCancel
            $terminationExit = Invoke-OwnedStop
            throw "observer exited during probe startup, exit $($observer.ExitCode)"
        }
        if ($startupClock.ElapsedMilliseconds -ge 10000) {
            Request-ProbeCancel
            $terminationExit = Invoke-OwnedStop
            throw 'probe ownership acknowledgement exceeded 10 seconds'
        }
        Start-Sleep -Milliseconds 100
    }
    $startupCheckExit = Invoke-OwnedControl 'check'
    if ($startupCheckExit -ne 0) {
        $terminationExit = Invoke-OwnedStop
        throw "probe ownership acknowledgement failed with exit $startupCheckExit"
    }
    $startupAcknowledged = $true

    while (-not $probe.HasExited) {
        $sessionEvents = @(Get-SessionEvents)
        $stopEvent = $sessionEvents | Where-Object { $_.verdict -eq 'stop' } | Select-Object -First 1
        if ($null -ne $stopEvent) {
            $guardStoppedProbe = $true
            $guardStopReason = [string]$stopEvent.reason
            $terminationExit = Invoke-OwnedStop
            break
        }
        if ($observer.HasExited) {
            $guardStoppedProbe = $true
            $guardStopReason = "observer exited while B1 workload was active: $($observer.ExitCode)"
            $terminationExit = Invoke-OwnedStop
            break
        }
        $observerState = Read-JsonFile $statePath
        if ($observerState.state -ne 'running' -or
            $observerState.session_id -ne $ObserverSession -or
            [int]$observerState.start_row -ne $startRow) {
            $guardStoppedProbe = $true
            $guardStopReason = 'observer state became stopped or changed identity'
            $terminationExit = Invoke-OwnedStop
            break
        }
        $sampleAge = [DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse([string]$observerState.last_sample_utc)
        if ($sampleAge.TotalSeconds -lt -120 -or $sampleAge.TotalSeconds -gt 720) {
            $guardStoppedProbe = $true
            $guardStopReason = "observer sample age became invalid: $($sampleAge.TotalSeconds) seconds"
            $terminationExit = Invoke-OwnedStop
            break
        }
        if ((Remaining-WorkloadMilliseconds) -le $terminationReserveMilliseconds) {
            $probeTimedOut = $true
            $terminationExit = Invoke-OwnedStop
            break
        }
        Start-Sleep -Milliseconds 200
    }
}
catch {
    $wrapperError = $_.Exception.Message
    if ($null -ne $probe -and -not $probe.HasExited -and $null -eq $terminationExit) {
        $terminationExit = Invoke-OwnedStop
    }
    if ($null -eq $probe -and $null -ne $observer -and -not $observer.HasExited) {
        New-Item -ItemType File -Path $stopRequest -Force | Out-Null
    }
}
finally {
    if ($prelaunchClock.IsRunning) { $prelaunchClock.Stop() }
    if ($null -ne $workloadClock -and $workloadClock.IsRunning) { $workloadClock.Stop() }
    $endedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $drive = Get-PSDrive -Name C
    $capture = [ordered]@{
        schema = 'cfdw-gpu-b1-capture/2'
        authority = 'Rulings 199-200 B1'
        observer_session = $ObserverSession
        started_utc = $startedUtc
        ended_utc = $endedUtc
        prelaunch_elapsed_ms = [int][Math]::Round($prelaunchClock.Elapsed.TotalMilliseconds)
        prelaunch_budget_ms = $prelaunchBudgetMilliseconds
        workload_elapsed_ms = if ($null -ne $workloadClock) { [int][Math]::Round($workloadClock.Elapsed.TotalMilliseconds) } else { 'Not started' }
        workload_budget_ms = $workloadBudgetMilliseconds
        termination_reserve_ms = $terminationReserveMilliseconds
        wrapper_error = if ($null -ne $wrapperError) { $wrapperError } else { 'Not applicable' }
        script_sha256_before_execution = $scriptHashesBeforeExecution
        observer = [ordered]@{
            argv = @('wsl.exe', '--distribution', $distro, '--user', 'root', '--exec', '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10', '/bin/bash', $observerScript, '--count', "$observerCount", '--start-row', "$startRow", '--session-id', $ObserverSession)
            windows_pid = if ($null -ne $observer) { $observer.Id } else { 'Not started' }
            initial_rows = $initialRows
            start_row = $startRow
            requested_samples = $observerCount
            baseline_event = if ($null -ne $baselineEvent) { $baselineEvent } else { 'Not recorded' }
            state_at_capture_end = if (Test-Path -LiteralPath $statePath) { Read-JsonFile $statePath } else { 'Not recorded' }
            continuation = 'bounded separate process continues after B1 for post-workload evidence'
            stdout = 'observer.stdout.txt'
            stderr = 'observer.stderr.txt'
        }
        probe = [ordered]@{
            argv = @('wsl.exe', '--distribution', $distro, '--user', 'root', '--exec', '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10', '/usr/bin/setsid', '/bin/bash', $probeScript, '--attempt-id', $ObserverSession)
            startup_acknowledged = $startupAcknowledged
            startup_check_exit_code = if ($null -ne $startupCheckExit) { $startupCheckExit } else { 'Not reached' }
            exit_code = if ($null -ne $probe -and $probe.HasExited) { $probe.ExitCode } else { 'Not recorded' }
            timed_out = $probeTimedOut
            guard_stopped_probe = $guardStoppedProbe
            guard_stop_reason = if ($null -ne $guardStopReason) { $guardStopReason } else { 'Not applicable' }
            termination_exit_code = if ($null -ne $terminationExit) { $terminationExit } else { 'Not needed' }
            stdout = 'b1.stdout.txt'
            stderr = 'b1.stderr.txt'
        }
        windows_target_volume = [ordered]@{
            name = $drive.Name
            used_bytes = $drive.Used
            free_bytes = $drive.Free
        }
    }
    $json = $capture | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($capturePath, $json + "`n", [Text.UTF8Encoding]::new($false))
}

if ($null -ne $wrapperError -or $probeTimedOut -or $guardStoppedProbe) { exit 5 }
if ($null -eq $probe -or -not $probe.HasExited) { exit 1 }
exit $probe.ExitCode
