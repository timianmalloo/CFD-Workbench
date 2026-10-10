param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ObserverSession
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$samplesPath = Join-Path (Split-Path -Parent $proofRoot) 'win-l3-observer\samples.jsonl'
$statePath = Join-Path $proofRoot 'observer-live.state.json'
$eventsPath = Join-Path $proofRoot 'guard-events.jsonl'
$capturePath = Join-Path $proofRoot 'capture.json'
$snapshotPath = Join-Path $proofRoot 'samples-prefix.snapshot.jsonl'
$bindingPath = Join-Path $proofRoot 'prefix-binding.json'
$completionPath = Join-Path $proofRoot 'observer-completion.json'
$bindOut = Join-Path $proofRoot 'bind-prefix.stdout.txt'
$bindErr = Join-Path $proofRoot 'bind-prefix.stderr.txt'
$budgetMilliseconds = 750000
$clock = [Diagnostics.Stopwatch]::StartNew()

function Read-CompleteGuardEvents {
    $raw = [IO.File]::ReadAllText($eventsPath)
    $lastLf = $raw.LastIndexOf("`n", [StringComparison]::Ordinal)
    if ($lastLf -lt 0) { return @() }
    $parsed = @()
    foreach ($line in $raw.Substring(0, $lastLf).Split("`n")) {
        $candidate = $line.TrimEnd("`r")
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        $parsed += $candidate | ConvertFrom-Json
    }
    return @($parsed)
}

$capture = Get-Content -LiteralPath $capturePath -Raw | ConvertFrom-Json
$startRow = [int]$capture.observer.start_row
$expectedFinalRow = $startRow + [int]$capture.observer.requested_samples - 1
$state = $null
while ($true) {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.session_id -ne $ObserverSession -or [int]$state.start_row -ne $startRow) {
        throw 'observer completion state identity changed'
    }
    if ($state.state -eq 'stopped') { break }
    if ($clock.ElapsedMilliseconds -ge $budgetMilliseconds) {
        throw 'post-workload observer exceeded its 750-second host deadline'
    }
    Start-Sleep -Milliseconds 500
}
$clock.Stop()
if ([int]$state.completed_samples -ne [int]$state.requested_samples) {
    throw "observer stopped incompletely: exit=$($state.exit_code), completed=$($state.completed_samples)"
}

$events = @(Read-CompleteGuardEvents | Where-Object { $_.session_id -eq $ObserverSession })
$finalEvent = $events | Where-Object { [int]$_.observed_row -eq $expectedFinalRow } | Select-Object -Last 1
if ($null -eq $finalEvent) { throw "guard event for row $expectedFinalRow is absent" }
if ([int]$state.exit_code -notin @(0, 5)) {
    throw "observer stopped with unexpected exit $($state.exit_code)"
}
if ([int]$state.exit_code -eq 5 -and $finalEvent.verdict -ne 'stop') {
    throw 'observer exit 5 is not backed by a final stop event'
}

$bind = Start-Process -FilePath 'py' -ArgumentList @(
    '-3', (Join-Path $proofRoot 'bind-prefix.py'),
    '--samples', $samplesPath, '--rows', "$expectedFinalRow",
    '--snapshot', $snapshotPath, '--binding', $bindingPath
) -RedirectStandardOutput $bindOut -RedirectStandardError $bindErr -PassThru -WindowStyle Hidden
if (-not $bind.WaitForExit(30000)) {
    Stop-Process -Id $bind.Id -Force -ErrorAction SilentlyContinue
    throw 'prefix binding exceeded 30 seconds'
}
if ($bind.ExitCode -ne 0) { throw "prefix binding failed with exit $($bind.ExitCode)" }

$completion = [ordered]@{
    schema = 'cfdw-live-observer-completion/1'
    observer_session = $ObserverSession
    waited_ms = [int][Math]::Round($clock.Elapsed.TotalMilliseconds)
    state = $state
    expected_final_row = $expectedFinalRow
    final_event = $finalEvent
    prefix_binding = Get-Content -LiteralPath $bindingPath -Raw | ConvertFrom-Json
}
$json = $completion | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText($completionPath, $json + "`n", [Text.UTF8Encoding]::new($false))

if ($finalEvent.verdict -eq 'stop') { exit 5 }
exit 0
