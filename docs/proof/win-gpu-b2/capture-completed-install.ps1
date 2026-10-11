param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ObserverSession,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^Ruling [0-9]+$')]
    [string]$CompletedGuardRuling
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$parentProof = Split-Path -Parent $proofRoot
$repoRoot = (Resolve-Path (Join-Path $proofRoot '..\..\..')).Path
$rulingsPath = Join-Path $repoRoot 'docs\notes\rulings.md'
$linuxRoot = '/mnt/c' + $proofRoot.Substring(2).Replace('\', '/')
$distro = 'cfdw-cuda132-b2'
$protectedDistro = 'cfdw-openfoam2512'
$observerMaximumSamples = 120
$prelaunchBudgetMilliseconds = 60000
$workloadBudgetMilliseconds = 600000
$terminationReserveMilliseconds = 10000

$eventsPath = Join-Path $proofRoot 'completed-guard-events.jsonl'
$statePath = Join-Path $proofRoot 'completed-guard.state.json'
$stopRequest = Join-Path $proofRoot 'completed-guard-stop.request'
$terminalBeforePath = Join-Path $proofRoot 'l3-terminal-before.json'
$terminalAfterPath = Join-Path $proofRoot 'l3-terminal-after.json'
$guardSnapshotPath = Join-Path $proofRoot 'completed-guard.snapshot.jsonl'
$guardBindingPath = Join-Path $proofRoot 'completed-guard-binding.json'
$terminalOut = Join-Path $proofRoot 'terminal-snapshot.stdout.txt'
$terminalErr = Join-Path $proofRoot 'terminal-snapshot.stderr.txt'
$bindOut = Join-Path $proofRoot 'completed-bind.stdout.txt'
$bindErr = Join-Path $proofRoot 'completed-bind.stderr.txt'
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
$completedGuardScript = "$linuxRoot/completed_guard.py"
$workloadScript = "$linuxRoot/install-and-test.sh"
$stopScript = "$linuxRoot/stop-owned.sh"
$residualScript = "$linuxRoot/residual-owned.sh"

Remove-Item -LiteralPath @(
    $eventsPath, $statePath, $stopRequest, $capturePath, $observerOut, $observerErr,
    $workloadOut, $workloadErr, $workloadPidPath, $workloadAttemptPath, $workloadCancelPath,
    $checkOut, $checkErr, $stopOut, $stopErr, $residualOut, $residualErr,
    $terminalBeforePath, $terminalAfterPath, $guardSnapshotPath, $guardBindingPath,
    $terminalOut, $terminalErr, $bindOut, $bindErr
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
    $remainingBeforeLaunch = if ($null -eq $workloadClock) {
        $TimeoutMilliseconds
    } else {
        $workloadBudgetMilliseconds - [int][Math]::Ceiling($workloadClock.Elapsed.TotalMilliseconds)
    }
    if ($remainingBeforeLaunch -le 0) { return -2 }
    $control = Start-Process -FilePath 'wsl.exe' -ArgumentList $Arguments `
        -RedirectStandardOutput $OutputPath -RedirectStandardError $ErrorPath -PassThru -WindowStyle Hidden
    $remainingAfterLaunch = if ($null -eq $workloadClock) {
        $TimeoutMilliseconds
    } else {
        $workloadBudgetMilliseconds - [int][Math]::Ceiling($workloadClock.Elapsed.TotalMilliseconds)
    }
    if ($remainingAfterLaunch -le 0) {
        Stop-Process -Id $control.Id -Force -ErrorAction SilentlyContinue
        return -2
    }
    $waitBudget = [Math]::Min($TimeoutMilliseconds, $remainingAfterLaunch)
    if (-not $control.WaitForExit($waitBudget)) {
        Stop-Process -Id $control.Id -Force -ErrorAction SilentlyContinue
        return -1
    }
    return $control.ExitCode
}

$scriptNames = @(
    'accepted-inputs.sha256', 'capture-completed-install.ps1', 'completed_guard.py',
    'install-and-test.sh', 'residual-owned.sh', 'saxpy.cu',
    'stop-owned.sh', 'verify-install-state.py'
)
$scriptHashesBeforeExecution = [ordered]@{}
foreach ($name in $scriptNames) {
    $scriptHashesBeforeExecution[$name] = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $proofRoot $name)).Hash.ToLowerInvariant()
}

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
$cleanupResidualExit = $null
$prelaunchResidualExit = $null
$postResidualExit = $null
$observerWaitMilliseconds = 0
$workloadLaunchAttempted = $false
$terminalBeforeSha256 = $null
$terminalAfterSha256 = $null
$terminalSnapshotExit = $null
$workloadProcessElapsedMilliseconds = $null
$observerTerminationConfirmed = $false

function Invoke-GuardBinding {
    if (-not (Test-Path -LiteralPath $eventsPath)) { return }
    $bind = Start-Process -FilePath 'py' -ArgumentList @(
        '-3', (Join-Path $proofRoot 'completed_guard.py'), 'bind',
        '--events', $eventsPath, '--snapshot', $guardSnapshotPath,
        '--binding', $guardBindingPath, '--session-id', $ObserverSession
    ) -RedirectStandardOutput $bindOut -RedirectStandardError $bindErr -PassThru -Wait -WindowStyle Hidden
    if ($bind.ExitCode -ne 0) { throw "completed guard binding failed with exit $($bind.ExitCode)" }
}

try {
    $rulingsText = [IO.File]::ReadAllText($rulingsPath)
    $rulingHeading = "### $CompletedGuardRuling "
    $rulingStart = $rulingsText.IndexOf($rulingHeading, [StringComparison]::Ordinal)
    if ($rulingStart -lt 0) { throw "$CompletedGuardRuling is absent from docs/notes/rulings.md" }
    $nextRuling = $rulingsText.IndexOf("`n### Ruling ", $rulingStart + $rulingHeading.Length, [StringComparison]::Ordinal)
    $rulingSection = if ($nextRuling -lt 0) {
        $rulingsText.Substring($rulingStart)
    } else {
        $rulingsText.Substring($rulingStart, $nextRuling - $rulingStart)
    }
    if ($rulingSection.IndexOf('B2-COMPLETED-GUARD-GO', [StringComparison]::Ordinal) -lt 0) {
        throw "$CompletedGuardRuling does not contain B2-COMPLETED-GUARD-GO"
    }
    $names = @(& wsl.exe --list --quiet) | ForEach-Object { Convert-WslName ([string]$_) } | Where-Object { $_ }
    if ($names -notcontains $distro -or $names -notcontains $protectedDistro) {
        throw 'target or protected WSL distro identity is unavailable'
    }
    & git merge-base --is-ancestor 5d884ecc HEAD
    if ($LASTEXITCODE -ne 0) { throw 'accepted phase-B commit is not an ancestor of HEAD' }

    $terminalSnapshotExit = Invoke-Control @(
        '--distribution', $protectedDistro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/python3', $completedGuardScript, 'snapshot',
        '--output', "$linuxRoot/l3-terminal-before.json", '--include-log-sha256'
    ) $terminalOut $terminalErr 60000
    if ($terminalSnapshotExit -ne 0 -or -not (Test-Path -LiteralPath $terminalBeforePath)) {
        throw "terminal pre-snapshot failed with exit $terminalSnapshotExit"
    }
    $terminalBeforeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $terminalBeforePath).Hash.ToLowerInvariant()

    $observer = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
        '--distribution', $protectedDistro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/python3', $completedGuardScript, 'watch',
        '--expected', "$linuxRoot/l3-terminal-before.json",
        '--expected-sha256', $terminalBeforeSha256,
        '--events', "$linuxRoot/completed-guard-events.jsonl",
        '--state', "$linuxRoot/completed-guard.state.json",
        '--stop-request', "$linuxRoot/completed-guard-stop.request",
        '--session-id', $ObserverSession, '--interval-seconds', '5',
        '--max-samples', "$observerMaximumSamples"
    ) -RedirectStandardOutput $observerOut -RedirectStandardError $observerErr -PassThru -WindowStyle Hidden

    while ($null -eq $baselineEvent) {
        if ($observer.HasExited) { throw "completed guard exited before baseline, exit $($observer.ExitCode)" }
        $sessionEvents = @(Get-SessionEvents)
        $stopEvent = $sessionEvents | Where-Object { $_.verdict -eq 'stop' } | Select-Object -First 1
        if ($null -ne $stopEvent) { throw "completed guard stopped during baseline: $($stopEvent.reason)" }
        $baselineEvent = $sessionEvents | Where-Object { $_.sample_number -eq 1 -and $_.verdict -eq 'continue' } | Select-Object -Last 1
        if ($prelaunchClock.ElapsedMilliseconds -ge $prelaunchBudgetMilliseconds) {
            throw 'completed guard baseline exceeded its 60-second host deadline'
        }
        if ($null -eq $baselineEvent) { Start-Sleep -Milliseconds 200 }
    }

    $stateAtLaunch = Read-JsonFile $statePath
    if ($stateAtLaunch.state -ne 'running' -or $stateAtLaunch.session_id -ne $ObserverSession -or
        $stateAtLaunch.expected_sha256 -ne $terminalBeforeSha256) {
        throw 'completed guard state does not match the B2 session'
    }
    $sampleAge = [DateTimeOffset]::UtcNow - (Convert-ToUtcInstant $stateAtLaunch.last_sample_utc)
    if ($sampleAge.TotalSeconds -lt -5 -or $sampleAge.TotalSeconds -gt 30) {
        throw "completed guard sample age is invalid: $($sampleAge.TotalSeconds) seconds"
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
    $workloadLaunchAttempted = $true
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
        elseif ($observer.HasExited) { $stopReason = "completed guard exited while workload was active: $($observer.ExitCode)" }
        elseif ($stateNow.state -ne 'running' -or $stateNow.session_id -ne $ObserverSession -or
            $stateNow.expected_sha256 -ne $terminalBeforeSha256) { $stopReason = 'completed guard stopped or changed identity' }
        else {
            $currentAge = [DateTimeOffset]::UtcNow - (Convert-ToUtcInstant $stateNow.last_sample_utc)
            if ($currentAge.TotalSeconds -lt -5 -or $currentAge.TotalSeconds -gt 30) {
                $stopReason = "completed guard sample age is invalid: $($currentAge.TotalSeconds) seconds"
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
    $workloadProcessElapsedMilliseconds = [int][Math]::Round($workloadClock.Elapsed.TotalMilliseconds)
    if ($workload.ExitCode -ne 0) { throw "B2 workload failed with exit $($workload.ExitCode)" }

    $postResidualExit = Invoke-Control @(
        '--distribution', $distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
        '/bin/bash', $residualScript, '--any-attempt'
    ) $residualOut $residualErr 5000
    if ($postResidualExit -ne 0) { throw "post-workload residual check failed with exit $postResidualExit" }

    $terminalSnapshotExit = Invoke-Control @(
        '--distribution', $protectedDistro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/python3', $completedGuardScript, 'snapshot',
        '--output', "$linuxRoot/l3-terminal-after.json", '--include-log-sha256'
    ) $terminalOut $terminalErr 60000
    if ($terminalSnapshotExit -ne 0 -or -not (Test-Path -LiteralPath $terminalAfterPath)) {
        throw "terminal post-snapshot failed with exit $terminalSnapshotExit"
    }
    $terminalAfterSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $terminalAfterPath).Hash.ToLowerInvariant()
    if ($terminalAfterSha256 -ne $terminalBeforeSha256) {
        throw 'protected L3 terminal snapshot changed across the B2 workload'
    }

    New-Item -ItemType File -Path $stopRequest -Force | Out-Null
    $observerWaitClock = [Diagnostics.Stopwatch]::StartNew()
    while (-not $observer.HasExited) {
        $remainingWorkloadMilliseconds = $workloadBudgetMilliseconds - [int][Math]::Ceiling($workloadClock.Elapsed.TotalMilliseconds)
        if ($remainingWorkloadMilliseconds -le 0 -or $observerWaitClock.ElapsedMilliseconds -ge 15000) {
            throw 'completed guard did not stop within the shared workload deadline'
        }
        Start-Sleep -Milliseconds 200
    }
    $observerWaitClock.Stop()
    $observerWaitMilliseconds = [int][Math]::Round($observerWaitClock.Elapsed.TotalMilliseconds)
    $observerTerminationConfirmed = $true
    $finalState = Read-JsonFile $statePath
    if ($observer.ExitCode -ne 0 -or $finalState.state -ne 'stopped' -or
        $finalState.session_id -ne $ObserverSession -or $finalState.expected_sha256 -ne $terminalBeforeSha256) {
        throw "completed guard completion failed with process exit $($observer.ExitCode)"
    }
    $finalEvent = Get-SessionEvents | Select-Object -Last 1
    Invoke-GuardBinding
    if ($null -eq $finalEvent -or $finalEvent.verdict -ne 'continue') {
        throw 'final completed guard event is absent or did not continue'
    }
    $workloadClock.Stop()
}
catch {
    $wrapperFailure = $_.Exception.Message
    if ($workloadLaunchAttempted) {
        try {
            [IO.File]::WriteAllText($workloadCancelPath, $ObserverSession + "`n", [Text.UTF8Encoding]::new($false))
            $cleanupResidualExit = Invoke-Control @(
                '--distribution', $distro, '--user', 'root', '--exec',
                '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
                '/bin/bash', $residualScript, '--attempt-id', $ObserverSession, '--stop'
            ) $stopOut $stopErr 10000
            if ($cleanupResidualExit -ne 0) {
                $wrapperFailure += "; cmdline cleanup was not verified, exit $cleanupResidualExit"
            }
        } catch { $wrapperFailure += "; cleanup failed: $($_.Exception.Message)" }
    }
    if (Test-Path -LiteralPath $terminalBeforePath) {
        try {
            if (-not (Test-Path -LiteralPath $terminalAfterPath)) {
                $terminalSnapshotExit = Invoke-Control @(
                    '--distribution', $protectedDistro, '--user', 'root', '--exec',
                    '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
                    '/usr/bin/python3', $completedGuardScript, 'snapshot',
                    '--output', "$linuxRoot/l3-terminal-after.json", '--include-log-sha256'
                ) $terminalOut $terminalErr 60000
                if ($terminalSnapshotExit -ne 0) { throw "terminal failure post-snapshot failed with exit $terminalSnapshotExit" }
            }
            $terminalAfterSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $terminalAfterPath).Hash.ToLowerInvariant()
            if ($terminalAfterSha256 -ne $terminalBeforeSha256) {
                $wrapperFailure += '; protected L3 terminal snapshot changed across the failed B2 workload'
            }
        } catch { $wrapperFailure += "; terminal failure post-check failed: $($_.Exception.Message)" }
    }
    if ($null -ne $observer -and -not $observer.HasExited) {
        New-Item -ItemType File -Path $stopRequest -Force | Out-Null
        $failedObserverWait = [Diagnostics.Stopwatch]::StartNew()
        while (-not $observer.HasExited -and $failedObserverWait.ElapsedMilliseconds -lt 15000 -and
            ($null -eq $workloadClock -or $workloadClock.ElapsedMilliseconds -lt $workloadBudgetMilliseconds)) {
            Start-Sleep -Milliseconds 200
        }
        $failedObserverWait.Stop()
        $observerWaitMilliseconds = [int][Math]::Round($failedObserverWait.Elapsed.TotalMilliseconds)
        if (-not $observer.HasExited) {
            $wrapperFailure += '; failed-run completed guard termination is unconfirmed at the shared deadline'
        } else {
            $observerTerminationConfirmed = $true
        }
    } elseif ($null -ne $observer -and $observer.HasExited) {
        $observerTerminationConfirmed = $true
    }
    if (Test-Path -LiteralPath $eventsPath) {
        try {
            Invoke-GuardBinding
        } catch { $wrapperFailure += "; failed guard binding failed: $($_.Exception.Message)" }
    }
}
finally {
    if ($prelaunchClock.IsRunning) { $prelaunchClock.Stop() }
    if ($null -ne $workloadClock -and $workloadClock.IsRunning) { $workloadClock.Stop() }
    $endedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $capture = [ordered]@{
        schema = 'cfdw-gpu-b2-capture/2'
        authority = "Ruling 200; $CompletedGuardRuling; Astra phase-B ACCEPT at 5d884ecc"
        observer_session = $ObserverSession
        target_distro = $distro
        protected_distro = $protectedDistro
        started_utc = $startedUtc
        ended_utc = $endedUtc
        prelaunch_elapsed_ms = [int][Math]::Round($prelaunchClock.Elapsed.TotalMilliseconds)
        workload_elapsed_ms = if ($null -ne $workloadProcessElapsedMilliseconds) { $workloadProcessElapsedMilliseconds } else { 'Not completed' }
        workload_and_cleanup_elapsed_ms = if ($null -ne $workloadClock) { [int][Math]::Round($workloadClock.Elapsed.TotalMilliseconds) } else { 'Not started' }
        workload_budget_ms = $workloadBudgetMilliseconds
        wrapper_error = if ($null -ne $wrapperFailure) { $wrapperFailure } else { 'Not applicable' }
        script_sha256_before_execution = $scriptHashesBeforeExecution
        completed_guard = [ordered]@{
            mode = 'immutable-completed-l3'
            maximum_samples = $observerMaximumSamples
            interval_seconds = 5
            terminal_before_sha256 = if ($null -ne $terminalBeforeSha256) { $terminalBeforeSha256 } else { 'Not recorded' }
            terminal_after_sha256 = if ($null -ne $terminalAfterSha256) { $terminalAfterSha256 } else { 'Not recorded' }
            windows_pid = if ($null -ne $observer) { $observer.Id } else { 'Not started' }
            process_exit_code = if ($null -ne $observer -and $observer.HasExited) { $observer.ExitCode } else { 'Not recorded' }
            termination_confirmed = $observerTerminationConfirmed
            baseline_event = if ($null -ne $baselineEvent) { $baselineEvent } else { 'Not recorded' }
            final_event = if ($null -ne $finalEvent) { $finalEvent } else { 'Not recorded' }
            wait_after_workload_ms = $observerWaitMilliseconds
            final_state = if (Test-Path -LiteralPath $statePath) { Read-JsonFile $statePath } else { 'Not recorded' }
            event_binding = if (Test-Path -LiteralPath $guardBindingPath) { Read-JsonFile $guardBindingPath } else { 'Not recorded' }
        }
        workload = [ordered]@{
            argv = @('wsl.exe', '--distribution', $distro, '--user', 'root', '--exec', '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10', '/bin/bash', $workloadScript, '--attempt-id', $ObserverSession)
            windows_pid = if ($null -ne $workload) { $workload.Id } else { 'Not started' }
            process_exit_code = if ($null -ne $workload -and $workload.HasExited) { $workload.ExitCode } else { 'Not recorded' }
            startup_acknowledged = $startupAcknowledged
            ownership_check_exit_code = if ($null -ne $ownershipCheckExit) { $ownershipCheckExit } else { 'Not reached' }
            termination_exit_code = if ($null -ne $terminationExit) { $terminationExit } else { 'Not needed' }
            cleanup_residual_exit_code = if ($null -ne $cleanupResidualExit) { $cleanupResidualExit } else { 'Not needed' }
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
