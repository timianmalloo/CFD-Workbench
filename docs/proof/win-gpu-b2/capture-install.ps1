param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ObserverSession
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$parentProof = Split-Path -Parent $proofRoot
$linuxRoot = '/mnt/c' + $proofRoot.Substring(2).Replace('\', '/')
$samplesPath = Join-Path $parentProof 'win-l3-observer\samples.jsonl'
$distro = 'cfdw-cuda132-b2'
$protectedDistro = 'cfdw-openfoam2512'
$observerCount = 3
$prelaunchBudgetMilliseconds = 750000
$workloadBudgetMilliseconds = 600000
$terminationReserveMilliseconds = 10000

$eventsPath = Join-Path $proofRoot 'guard-events.jsonl'
$statePath = Join-Path $proofRoot 'observer-live.state.json'
$stopRequest = Join-Path $proofRoot 'observer-stop.request'
$capturePath = Join-Path $proofRoot 'capture.json'
$observerOut = Join-Path $proofRoot 'observer.stdout.txt'
$observerErr = Join-Path $proofRoot 'observer.stderr.txt'
$workloadOut = Join-Path $proofRoot 'b2.stdout.txt'
$workloadErr = Join-Path $proofRoot 'b2.stderr.txt'
$workloadPidPath = Join-Path $proofRoot 'b2.pid'
$workloadAttemptPath = Join-Path $proofRoot 'b2.attempt'
$workloadCancelPath = Join-Path $proofRoot 'workload-cancel.request'
$checkOut = Join-Path $proofRoot 'check-owned.stdout.txt'
$checkErr = Join-Path $proofRoot 'check-owned.stderr.txt'
$stopOut = Join-Path $proofRoot 'stop-owned.stdout.txt'
$stopErr = Join-Path $proofRoot 'stop-owned.stderr.txt'
$residualOut = Join-Path $proofRoot 'residual-owned.stdout.txt'
$residualErr = Join-Path $proofRoot 'residual-owned.stderr.txt'
$snapshotPath = Join-Path $proofRoot 'samples-prefix.snapshot.jsonl'
$bindingPath = Join-Path $proofRoot 'prefix-binding.json'
$bindOut = Join-Path $proofRoot 'bind-prefix.stdout.txt'
$bindErr = Join-Path $proofRoot 'bind-prefix.stderr.txt'

$observerScript = "$linuxRoot/observe-live.sh"
$workloadScript = "$linuxRoot/install-and-test.sh"
$stopScript = "$linuxRoot/stop-owned.sh"
$residualScript = "$linuxRoot/residual-owned.sh"

Remove-Item -LiteralPath @(
    $eventsPath, $statePath, $stopRequest, $capturePath, $observerOut, $observerErr,
    $workloadOut, $workloadErr, $workloadPidPath, $workloadAttemptPath, $workloadCancelPath,
    $checkOut, $checkErr, $stopOut, $stopErr, $residualOut, $residualErr,
    $snapshotPath, $bindingPath, $bindOut, $bindErr
) -Force -ErrorAction SilentlyContinue

function Convert-WslName([string]$Value) {
    return ($Value -replace "`0", '').Trim()
}

function Read-JsonFile([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Convert-ToUtcInstant($Value) {
    if ($Value -is [DateTime]) { return [DateTimeOffset]::new($Value.ToUniversalTime()) }
    if ($Value -is [DateTimeOffset]) { return $Value.ToUniversalTime() }
    return [DateTimeOffset]::Parse(
        [string]$Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal
    ).ToUniversalTime()
}

function Read-CompleteGuardEvents {
    if (-not (Test-Path -LiteralPath $eventsPath)) { return @() }
    $raw = [IO.File]::ReadAllText($eventsPath)
    $lastLf = $raw.LastIndexOf("`n", [StringComparison]::Ordinal)
    if ($lastLf -lt 0) { return @() }
    $parsed = @()
    foreach ($line in $raw.Substring(0, $lastLf).Split("`n")) {
        $candidate = $line.TrimEnd("`r")
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { $parsed += $candidate | ConvertFrom-Json }
    }
    return @($parsed)
}

function Get-SessionEvents {
    return @(Read-CompleteGuardEvents | Where-Object { $_.session_id -eq $ObserverSession })
}

function Invoke-Control(
    [string[]]$Arguments,
    [string]$OutputPath,
    [string]$ErrorPath,
    [int]$TimeoutMilliseconds
) {
    $control = Start-Process -FilePath 'wsl.exe' -ArgumentList $Arguments `
        -RedirectStandardOutput $OutputPath -RedirectStandardError $ErrorPath -PassThru -WindowStyle Hidden
    if (-not $control.WaitForExit($TimeoutMilliseconds)) {
        Stop-Process -Id $control.Id -Force -ErrorAction SilentlyContinue
        return -1
    }
    return $control.ExitCode
}

$scriptNames = @(
    'accepted-inputs.sha256', 'bind-prefix.py', 'capture-install.ps1', 'install-and-test.sh',
    'observe-live.sh', 'observer_guard.py', 'residual-owned.sh', 'saxpy.cu',
    'stop-owned.sh', 'verify-install-state.py'
)
$scriptHashesBeforeExecution = [ordered]@{}
foreach ($name in $scriptNames) {
    $scriptHashesBeforeExecution[$name] = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $proofRoot $name)).Hash.ToLowerInvariant()
}

$initialRows = @(Get-Content -LiteralPath $samplesPath).Count
$startRow = $initialRows + 1
$startedUtc = [DateTimeOffset]::UtcNow.ToString('o')
$prelaunchClock = [Diagnostics.Stopwatch]::StartNew()
$workloadClock = $null
$observer = $null
$workload = $null
$baselineEvent = $null
$finalEvent = $null
$wrapperFailure = $null
$startupAcknowledged = $false
$ownershipCheckExit = $null
$terminationExit = $null
$prelaunchResidualExit = $null
$postResidualExit = $null
$observerWaitMilliseconds = 0

try {
    $names = @(& wsl.exe --list --quiet) | ForEach-Object { Convert-WslName ([string]$_) } | Where-Object { $_ }
    if ($names -notcontains $distro -or $names -notcontains $protectedDistro) {
        throw 'target or protected WSL distro identity is unavailable'
    }
    & git merge-base --is-ancestor 5d884ecc HEAD
    if ($LASTEXITCODE -ne 0) { throw 'accepted phase-B commit is not an ancestor of HEAD' }

    $observer = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $protectedDistro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $observerScript, '--count', "$observerCount",
        '--start-row', "$startRow", '--session-id', $ObserverSession
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
    if ($stateAtLaunch.state -ne 'running' -or $stateAtLaunch.session_id -ne $ObserverSession -or
        [int]$stateAtLaunch.start_row -ne $startRow) {
        throw 'observer state does not match the live B2 session'
    }
    $sampleAge = [DateTimeOffset]::UtcNow - (Convert-ToUtcInstant $stateAtLaunch.last_sample_utc)
    if ($sampleAge.TotalSeconds -lt -120 -or $sampleAge.TotalSeconds -gt 720) {
        throw "fresh baseline sample age is invalid: $($sampleAge.TotalSeconds) seconds"
    }

    $prelaunchClock.Stop()
    $workloadClock = [Diagnostics.Stopwatch]::StartNew()
    $prelaunchResidualExit = Invoke-Control @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $residualScript, '--any-attempt'
    ) $residualOut $residualErr 5000
    if ($prelaunchResidualExit -ne 0) { throw "prelaunch residual check failed with exit $prelaunchResidualExit" }

    $workloadArguments = @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $workloadScript, '--attempt-id', $ObserverSession
    )
    $workload = Start-Process -FilePath 'wsl.exe' -ArgumentList $workloadArguments `
        -RedirectStandardOutput $workloadOut -RedirectStandardError $workloadErr -PassThru -WindowStyle Hidden

    $startupClock = [Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-Path -LiteralPath $workloadPidPath) -or -not (Test-Path -LiteralPath $workloadAttemptPath)) {
        if ($workload.HasExited) { throw "workload exited before ownership acknowledgement, exit $($workload.ExitCode)" }
        if ($observer.HasExited) { throw "observer exited during workload startup, exit $($observer.ExitCode)" }
        if ($startupClock.ElapsedMilliseconds -ge 10000) { throw 'workload ownership acknowledgement exceeded 10 seconds' }
        Start-Sleep -Milliseconds 100
    }
    $ownershipCheckExit = Invoke-Control @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $stopScript, '--attempt-id', $ObserverSession, '--launch-method', 'foreground', '--check'
    ) $checkOut $checkErr 5000
    if ($ownershipCheckExit -ne 0) { throw "ownership acknowledgement failed with exit $ownershipCheckExit" }
    $startupAcknowledged = $true

    while (-not $workload.HasExited) {
        $sessionEvents = @(Get-SessionEvents)
        $stopEvent = $sessionEvents | Where-Object { $_.verdict -eq 'stop' } | Select-Object -First 1
        $stateNow = Read-JsonFile $statePath
        $stopReason = $null
        if ($null -ne $stopEvent) { $stopReason = "guard stop: $($stopEvent.reason)" }
        elseif ($observer.HasExited) { $stopReason = "observer exited while workload was active: $($observer.ExitCode)" }
        elseif ($stateNow.state -ne 'running' -or $stateNow.session_id -ne $ObserverSession -or
            [int]$stateNow.start_row -ne $startRow) { $stopReason = 'observer state stopped or changed identity' }
        else {
            $currentAge = [DateTimeOffset]::UtcNow - (Convert-ToUtcInstant $stateNow.last_sample_utc)
            if ($currentAge.TotalSeconds -lt -120 -or $currentAge.TotalSeconds -gt 720) {
                $stopReason = "observer sample age is invalid: $($currentAge.TotalSeconds) seconds"
            }
        }
        if ($null -eq $stopReason -and
            $workloadClock.ElapsedMilliseconds -ge ($workloadBudgetMilliseconds - $terminationReserveMilliseconds)) {
            $stopReason = 'workload reached its termination reserve'
        }
        if ($null -ne $stopReason) {
            [IO.File]::WriteAllText($workloadCancelPath, $ObserverSession + "`n", [Text.UTF8Encoding]::new($false))
            $terminationExit = Invoke-Control @(
                '--distribution', $distro, '--user', 'root', '--exec',
                '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
                '/bin/bash', $stopScript, '--attempt-id', $ObserverSession, '--launch-method', 'foreground'
            ) $stopOut $stopErr 10000
            throw "$stopReason; owned stop exit $terminationExit"
        }
        Start-Sleep -Milliseconds 200
    }
    $workloadClock.Stop()
    if ($workload.ExitCode -ne 0) { throw "B2 workload failed with exit $($workload.ExitCode)" }

    $postResidualExit = Invoke-Control @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $residualScript, '--any-attempt'
    ) $residualOut $residualErr 5000
    if ($postResidualExit -ne 0) { throw "post-workload residual check failed with exit $postResidualExit" }

    $observerWaitClock = [Diagnostics.Stopwatch]::StartNew()
    while (-not $observer.HasExited) {
        if ($observerWaitClock.ElapsedMilliseconds -ge 750000) { throw 'post-workload observer exceeded 750 seconds' }
        Start-Sleep -Milliseconds 500
    }
    $observerWaitClock.Stop()
    $observerWaitMilliseconds = [int][Math]::Round($observerWaitClock.Elapsed.TotalMilliseconds)
    $finalState = Read-JsonFile $statePath
    if ($observer.ExitCode -ne 0 -or $finalState.state -ne 'stopped' -or
        [int]$finalState.completed_samples -ne $observerCount) {
        throw "observer completion failed with process exit $($observer.ExitCode)"
    }
    $expectedFinalRow = $startRow + $observerCount - 1
    $finalEvent = Get-SessionEvents | Where-Object { [int]$_.observed_row -eq $expectedFinalRow } | Select-Object -Last 1
    if ($null -eq $finalEvent -or $finalEvent.verdict -ne 'continue') {
        throw 'final live guard event is absent or did not continue'
    }

    $bind = Start-Process -FilePath 'py' -ArgumentList @(
        '-3', (Join-Path $proofRoot 'bind-prefix.py'), '--samples', $samplesPath,
        '--rows', "$expectedFinalRow", '--snapshot', $snapshotPath, '--binding', $bindingPath
    ) -RedirectStandardOutput $bindOut -RedirectStandardError $bindErr -PassThru -Wait -WindowStyle Hidden
    if ($bind.ExitCode -ne 0) { throw "observer prefix binding failed with exit $($bind.ExitCode)" }
}
catch {
    $wrapperFailure = $_.Exception.Message
    if ($null -ne $workload -and -not $workload.HasExited -and $null -eq $terminationExit) {
        try {
            [IO.File]::WriteAllText($workloadCancelPath, $ObserverSession + "`n", [Text.UTF8Encoding]::new($false))
            $terminationExit = Invoke-Control @(
                '--distribution', $distro, '--user', 'root', '--exec',
                '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
                '/bin/bash', $residualScript, '--attempt-id', $ObserverSession, '--stop'
            ) $stopOut $stopErr 10000
        } catch { $wrapperFailure += "; cleanup failed: $($_.Exception.Message)" }
    }
    if ($null -ne $observer -and -not $observer.HasExited) {
        New-Item -ItemType File -Path $stopRequest -Force | Out-Null
    }
}
finally {
    if ($prelaunchClock.IsRunning) { $prelaunchClock.Stop() }
    if ($null -ne $workloadClock -and $workloadClock.IsRunning) { $workloadClock.Stop() }
    $endedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $capture = [ordered]@{
        schema = 'cfdw-gpu-b2-capture/1'
        authority = 'Ruling 200; Astra phase-B ACCEPT at 5d884ecc'
        observer_session = $ObserverSession
        target_distro = $distro
        protected_distro = $protectedDistro
        started_utc = $startedUtc
        ended_utc = $endedUtc
        prelaunch_elapsed_ms = [int][Math]::Round($prelaunchClock.Elapsed.TotalMilliseconds)
        workload_elapsed_ms = if ($null -ne $workloadClock) { [int][Math]::Round($workloadClock.Elapsed.TotalMilliseconds) } else { 'Not started' }
        workload_budget_ms = $workloadBudgetMilliseconds
        wrapper_error = if ($null -ne $wrapperFailure) { $wrapperFailure } else { 'Not applicable' }
        script_sha256_before_execution = $scriptHashesBeforeExecution
        observer = [ordered]@{
            initial_rows = $initialRows
            start_row = $startRow
            requested_samples = $observerCount
            windows_pid = if ($null -ne $observer) { $observer.Id } else { 'Not started' }
            process_exit_code = if ($null -ne $observer -and $observer.HasExited) { $observer.ExitCode } else { 'Not recorded' }
            baseline_event = if ($null -ne $baselineEvent) { $baselineEvent } else { 'Not recorded' }
            final_event = if ($null -ne $finalEvent) { $finalEvent } else { 'Not recorded' }
            wait_after_workload_ms = $observerWaitMilliseconds
            final_state = if (Test-Path -LiteralPath $statePath) { Read-JsonFile $statePath } else { 'Not recorded' }
            prefix_binding = if (Test-Path -LiteralPath $bindingPath) { Read-JsonFile $bindingPath } else { 'Not recorded' }
        }
        workload = [ordered]@{
            argv = @('wsl.exe', '--distribution', $distro, '--user', 'root', '--exec', '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10', '/bin/bash', $workloadScript, '--attempt-id', $ObserverSession)
            windows_pid = if ($null -ne $workload) { $workload.Id } else { 'Not started' }
            process_exit_code = if ($null -ne $workload -and $workload.HasExited) { $workload.ExitCode } else { 'Not recorded' }
            startup_acknowledged = $startupAcknowledged
            ownership_check_exit_code = if ($null -ne $ownershipCheckExit) { $ownershipCheckExit } else { 'Not reached' }
            termination_exit_code = if ($null -ne $terminationExit) { $terminationExit } else { 'Not needed' }
            prelaunch_residual_exit_code = if ($null -ne $prelaunchResidualExit) { $prelaunchResidualExit } else { 'Not reached' }
            post_residual_exit_code = if ($null -ne $postResidualExit) { $postResidualExit } else { 'Not reached' }
        }
        saxpy = if (Test-Path -LiteralPath (Join-Path $proofRoot 'saxpy-result.json')) {
            Read-JsonFile (Join-Path $proofRoot 'saxpy-result.json')
        } else { 'Not recorded' }
    }
    $json = ($capture | ConvertTo-Json -Depth 15) -replace "`r`n", "`n"
    [IO.File]::WriteAllText($capturePath, $json + "`n", [Text.UTF8Encoding]::new($false))
}

if ($null -ne $wrapperFailure) { exit 5 }
exit 0
