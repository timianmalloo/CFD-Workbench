# Pure host control functions. The offline SelfTest.ps1 exercises failure and deadline paths.
function Get-GuardAgeSeconds([object]$Timestamp, [DateTimeOffset]$Now) {
    if ($Timestamp -is [DateTimeOffset]) {
        $sample = ([DateTimeOffset]$Timestamp).ToUniversalTime()
    } elseif ($Timestamp -is [DateTime]) {
        $date = [DateTime]$Timestamp
        if ($date.Kind -eq [DateTimeKind]::Unspecified) { throw 'guard timestamp lost its UTC offset' }
        $sample = ([DateTimeOffset]$date).ToUniversalTime()
    } elseif ($Timestamp -is [string]) {
        if ($Timestamp -notmatch '(?:Z|[+-][0-9]{2}:[0-9]{2})$') { throw 'guard timestamp lacks offset' }
        $sample = [DateTimeOffset]::Parse($Timestamp, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    } else { throw 'guard timestamp has unsupported type' }
    return ($Now.ToUniversalTime() - $sample).TotalSeconds
}

function Get-ChildWaitSeconds([double]$Elapsed, [double]$StageStart, [double]$StageCap,
    [ValidateSet('work','checkpoint','cleanup')][string]$Phase) {
    $phaseEnd = switch ($Phase) { 'work' { 2700.0 } 'checkpoint' { 3000.0 } 'cleanup' { 3600.0 } }
    $end = [Math]::Min([Math]::Min($phaseEnd, 3600.0), ($StageStart + $StageCap))
    return [Math]::Max(0.0, ($end - $Elapsed))
}

function Update-TargetSettle([string]$Observed, [string]$Previous, [double]$Elapsed,
    [double]$StableSince) {
    if ($Observed -cne $Previous) { $StableSince = $Elapsed }
    return [pscustomobject]@{ key=$Observed; stable_since=$StableSince;
        stable=($Elapsed - $StableSince -ge 5.0) }
}

function Invoke-Finalization(
    [Alias('StopOwned')][scriptblock]$StopOwnedAction,
    [Alias('Checkpoint')][scriptblock]$CheckpointAction,
    [Alias('Cleanup')][scriptblock]$CleanupAction,
    [Alias('Snapshot')][scriptblock]$SnapshotAction,
    [Alias('StopGuard')][scriptblock]$StopGuardAction,
    [Alias('BindGuard')][scriptblock]$BindGuardAction
) {
    $errors = [Collections.Generic.List[string]]::new()
    $ownedStopped = $false
    $checkpointProved = $false
    $cleanupProved = $false
    $snapshotProved = $false
    $guardStopped = $false
    $guardBound = $false
    try {
        try { & $StopOwnedAction; $ownedStopped = $true } catch { $errors.Add("owned-stop: $($_.Exception.Message)") }
        try { & $CheckpointAction; $checkpointProved = $true } catch { $errors.Add("checkpoint: $($_.Exception.Message)") }
        if ($ownedStopped -and $checkpointProved) {
            try { & $CleanupAction; $cleanupProved = $true } catch { $errors.Add("cleanup: $($_.Exception.Message)") }
        } elseif (-not $checkpointProved) {
            $errors.Add('MAC-DECISION-REQUIRED: checkpoint unproved; archive deletion/unregister withheld')
        } else {
            $errors.Add('MAC-DECISION-REQUIRED: owned process not proved stopped; archive deletion/unregister withheld')
        }
        try { & $SnapshotAction; $snapshotProved = $true } catch { $errors.Add("snapshot: $($_.Exception.Message)") }
        try { & $StopGuardAction; $guardStopped = $true } catch { $errors.Add("guard-stop: $($_.Exception.Message)") }
        try { & $BindGuardAction; $guardBound = $true } catch { $errors.Add("guard-bind: $($_.Exception.Message)") }
    } finally {
        # A cleanup callback may have launched new work after the first stop.
        # This is the last callback: no workload launch follows the fresh reap.
        $ownedStopped = $false
        try { & $StopOwnedAction; $ownedStopped = $true }
        catch { $errors.Add("final-owned-stop: $($_.Exception.Message)") }
    }
    return [pscustomobject]@{ errors=@($errors.ToArray()); owned_stopped=$ownedStopped
        checkpoint_proved=$checkpointProved; cleanup_proved=$cleanupProved
        snapshot_proved=$snapshotProved; guard_stopped=$guardStopped; guard_bound=$guardBound }
}
