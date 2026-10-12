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
function Get-InvocationRunRoot([object]$ContractData, [string]$RepoRoot, [string]$RunId, [string]$AgentSession) {
    if ($null -eq $ContractData.psobject.Properties['attempt']) { return (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1') }
    if ($ContractData.attempt -ne 1 -or $ContractData.leader_epoch -ne 7 -or
        $ContractData.execution_branch -cne 'win/gpu-f1-r225-attempt1' -or
        $ContractData.session -cne 'pc-f1-r225-attempt1' -or $AgentSession -cne $ContractData.session -or
        $ContractData.run_id -cne $RunId -or $RunId -cnotmatch '^r225-attempt1-epoch7-[0-9]{8}$' -or
        $ContractData.run_root -cne 'docs/proof/win-gpu-f1-preflight/attempt-1') {
        throw 'fresh attempt run/session/root binding differs'
    }
    return (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\attempt-1')
}

function Get-BoundTargetKey([object[]]$Registration, [string]$TargetPath, [string]$ExpectedKey = '') {
    if ($Registration.Count -ne 1) { throw 'exactly one target registration required' }
    $row = $Registration[0]
    $key = [string]$row.PSChildName
    if ($row.DistributionName -cne 'cfdw-petsc-cuda132-f1' -or
        $key -notmatch '^\{[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\}$' -or $row.Version -ne 2 -or
        ([string]$row.BasePath).Replace('\\?\', '').TrimEnd('\') -ine $TargetPath -or
        $key -ieq '{b7566a82-9cee-47ce-a246-f83632353cea}' -or
        $key -ieq '{e958c4d5-ae78-4b50-a634-7102f334d3aa}' -or
        ($ExpectedKey -and $key -ine $ExpectedKey)) { throw 'created target GUID/path binding changed' }
    return $key
}

function Assert-FreshRegistrations([object[]]$Rows, [string[]]$Names, [bool]$PathPresent) {
    if ($PathPresent -or $Names -ccontains 'cfdw-petsc-cuda132-f1') { throw 'fresh target name/path absence unproved' }
    $protected = @()
    $expectedPath = [Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512')
    foreach ($row in $Rows) {
        if ($row.DistributionName -ceq 'cfdw-petsc-cuda132-f1' -or
            $row.PSChildName -ieq '{e958c4d5-ae78-4b50-a634-7102f334d3aa}' -or
            ([string]$row.BasePath).Replace('\\?\', '').TrimEnd('\') -ieq 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1') {
            throw 'fresh target registration/GUID/path absence unproved'
        }
        if ($row.DistributionName -ceq 'cfdw-openfoam2512') { $protected += $row }
        elseif ($row.PSChildName -ieq '{b7566a82-9cee-47ce-a246-f83632353cea}' -or
            ([string]$row.BasePath).Replace('\\?\', '').TrimEnd('\') -ieq $expectedPath) {
            throw 'non-protected registration aliases protected identity/path'
        }
    }
    if ($protected.Count -ne 1 -or @($Names | Where-Object { $_ -ceq 'cfdw-openfoam2512' }).Count -ne 1 -or
        $protected[0].PSChildName -ine '{b7566a82-9cee-47ce-a246-f83632353cea}' -or $protected[0].Version -ne 2 -or
        ([string]$protected[0].BasePath).Replace('\\?\', '').TrimEnd('\') -ine $expectedPath) {
        throw 'protected GUID/name/path/version differs'
    }
}

