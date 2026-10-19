# Pure host control functions. The offline SelfTest.ps1 exercises failure and deadline paths.
function ConvertTo-EvidenceUtc([object]$Timestamp) {
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
    return $sample
}
function Get-GuardAgeSeconds([object]$Timestamp, [DateTimeOffset]$Now) {
    return ($Now.ToUniversalTime() - (ConvertTo-EvidenceUtc $Timestamp)).TotalSeconds
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
function Get-DerivedTargetPath([string]$RepoRoot) {
    # Ruling 237: the target root derives from the repository location (<repository parent>\CFD-Workbench-wsl\<target>),
    # never a constant and never a parameter; the contract records the path it was prepared for and Assert-TargetPath
    # checks equality. contract.py derived_target_path is the same rule; tests/f1_derived_paths.ps1 proves them equal.
    if ($RepoRoot -notmatch '^[A-Za-z]:\\' -or $RepoRoot -match '\s') { throw "repository root must be a no-space absolute drive path: $RepoRoot" }
    $root = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\')
    $parent = [IO.Path]::GetDirectoryName($root)   # string-only: Join-Path and Split-Path validate the drive, a fixture drive may not exist
    if ([string]::IsNullOrEmpty($parent)) { throw "repository root has no parent: $root" }
    return [IO.Path]::Combine($parent, 'CFD-Workbench-wsl', 'cfdw-petsc-cuda132-f1')
}

function Get-ContractHostFloors([object]$ContractData) {
    # Ruling 238 (3): the free-space floor and reserve are contract fields from the host's worksheet, never constants.
    foreach ($name in @('host_start_floor_bytes', 'host_reserve_bytes')) {
        if ($null -eq $ContractData.psobject.Properties[$name]) { throw "contract has no $name" }
        $value = $ContractData.$name
        if (($value -isnot [int] -and $value -isnot [long]) -or $value -le 0) { throw "contract $name must be a positive integer" }
    }
    $floor = [int64]$ContractData.host_start_floor_bytes
    $reserve = [int64]$ContractData.host_reserve_bytes
    if ($floor -le $reserve) { throw 'contract host_start_floor_bytes must exceed host_reserve_bytes' }
    return @($floor, $reserve)
}

function Get-InvocationRunRoot([object]$ContractData, [string]$RepoRoot, [string]$RunId, [string]$AgentSession) {
    if ($null -eq $ContractData.psobject.Properties['attempt']) { return (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1') }
    if ($ContractData.attempt -is [int] -or $ContractData.attempt -is [long]) {
        if ($ContractData.attempt -eq 2) {
            if (($ContractData.leader_epoch -isnot [int] -and $ContractData.leader_epoch -isnot [long]) -or
                $ContractData.leader_epoch -le 0 -or $ContractData.leader -cne 'mac' -or
                $ContractData.execution_branch -cne 'win/gpu-f1-attempt2' -or
                $ContractData.session -cne 'pc-f1-attempt2' -or $AgentSession -cne $ContractData.session -or
                $ContractData.run_id -cne $RunId -or $RunId -cnotmatch '^f1-attempt2-[0-9]{8}$' -or
                $ContractData.run_root -cne 'docs/proof/win-gpu-f1-preflight/attempt-2') { throw 'attempt-2 binding differs' }
            return (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\attempt-2')
        }
    }
    if ($ContractData.attempt -ne 1 -or $ContractData.leader_epoch -ne 8 -or
        $ContractData.execution_branch -cne 'win/gpu-f1-r225-attempt1' -or
        $ContractData.session -cne 'pc-f1-r225-attempt1' -or $AgentSession -cne $ContractData.session -or
        $ContractData.run_id -cne $RunId -or $RunId -cnotmatch '^r225-attempt1-epoch8-[0-9]{8}$' -or
        $ContractData.run_root -cne 'docs/proof/win-gpu-f1-preflight/attempt-1') {
        throw 'fresh attempt run/session/root binding differs'
    }
    return (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\attempt-1')
}

function Get-BoundTargetKey([object[]]$Registration, [string]$TargetPath, [string]$ExpectedKey = '', [int]$AttemptNumber = 1) {
    if ($Registration.Count -ne 1) { throw 'exactly one target registration required' }
    $row = $Registration[0]
    $key = [string]$row.PSChildName
    if ($row.DistributionName -cne 'cfdw-petsc-cuda132-f1' -or
        $key -notmatch '^\{[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\}$' -or $row.Version -ne 2 -or
        ([string]$row.BasePath).Replace('\\?\', '').TrimEnd('\') -ine $TargetPath -or
        $key -ieq '{b7566a82-9cee-47ce-a246-f83632353cea}' -or
        $key -ieq '{e958c4d5-ae78-4b50-a634-7102f334d3aa}' -or
        ($AttemptNumber -eq 2 -and $key -ieq '{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}') -or
        ($ExpectedKey -and $key -ine $ExpectedKey)) { throw 'created target GUID/path binding changed' }
    return $key
}

function Assert-FreshRegistrations([object[]]$Rows, [string[]]$Names, [bool]$PathPresent, [int]$AttemptNumber = 1, [string]$TargetPath = '') {
    if ([string]::IsNullOrEmpty($TargetPath)) { throw 'target path required for the registration check' }
    if ($PathPresent -or $Names -ccontains 'cfdw-petsc-cuda132-f1') { throw 'fresh target name/path absence unproved' }
    $protected = @()
    $expectedPath = [Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512')
    foreach ($row in $Rows) {
        if ($row.DistributionName -ceq 'cfdw-petsc-cuda132-f1' -or
            $row.PSChildName -ieq '{e958c4d5-ae78-4b50-a634-7102f334d3aa}' -or
            ($AttemptNumber -eq 2 -and $row.PSChildName -ieq '{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}') -or
            ([string]$row.BasePath).Replace('\\?\', '').TrimEnd('\') -ieq $TargetPath.TrimEnd('\')) {
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

function Assert-AttemptLeader([object]$Sample, [long]$Epoch) {
    $ttl=$Sample.expires_in
    if ($Sample.leader -cne 'mac' -or $Sample.state -cne 'live' -or
        ($Sample.epoch -isnot [int] -and $Sample.epoch -isnot [long]) -or $Sample.epoch -ne $Epoch -or
        ($ttl -isnot [double] -and $ttl -isnot [int] -and $ttl -isnot [long] -and $ttl -isnot [decimal]) -or
        [double]::IsNaN([double]$ttl) -or [double]::IsInfinity([double]$ttl) -or $ttl -lt 200 -or
        $Sample.oid -cnotmatch '^[a-f0-9]{40}$') { throw 'F1-LEADER-REFUSED: exact live epoch and finite TTL >=200 required' }
}

function Get-ProcessCreationUtc([object]$Value) {
    if ($Value -is [DateTimeOffset]) { return $Value.ToUniversalTime().ToString('o') }
    if ($Value -is [DateTime] -and $Value.Kind -ne [DateTimeKind]::Unspecified) {
        return ([DateTimeOffset]$Value).ToUniversalTime().ToString('o')
    }
    throw 'F1-PROCESS-IDENTITY-REFUSED: creation time is not an offset-bearing timestamp'
}

function Assert-AttemptExclusivity([object[]]$Processes, [object[]]$Owned, [string]$ProtectedKey) {
    $parent=@($Processes | Where-Object {$_.ProcessId -eq 19296})
    $child=@($Processes | Where-Object {$_.ProcessId -eq 3524})
    if ($ProtectedKey -cne '{b7566a82-9cee-47ce-a246-f83632353cea}' -or $parent.Count -ne 1 -or $child.Count -ne 1 -or
        $parent[0].ParentProcessId -ne 15528 -or $child[0].ParentProcessId -ne 19296 -or $parent[0].Name -cne 'wsl.exe' -or $child[0].Name -cne 'wsl.exe' -or
        $parent[0].CommandLine -cne '"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity' -or
        $child[0].CommandLine -cne $parent[0].CommandLine -or
        (Get-ProcessCreationUtc $child[0].CreationDate) -cne '2026-10-08T17:02:23.2168250+00:00' -or
        (Get-ProcessCreationUtc $parent[0].CreationDate) -cne '2026-10-08T17:02:23.1776050+00:00') {
        throw 'F1-EXCLUSIVITY-REFUSED: exact protected keepalive identity unavailable'
    }
    foreach ($row in $Processes) {
        if ($row.ProcessId -in @(19296,3524)) { continue }
        # Host services/VM infrastructure are not user launchers. Include shells whose command invokes WSL.
        $actor=$row.Name -imatch '^(wsl|bash|ubuntu(?:[0-9.]*)|debian|kali)\.exe$' -or
            ([string]$row.CommandLine -imatch '(?:^|[\\\s"''])wsl(?:\.exe)?(?:[\s"'']|$)')
        if (-not $actor) { continue }
        $bound=@($Owned | Where-Object {$_.pid -eq $row.ProcessId})
        if ($bound.Count -ne 1 -or $bound[0].command -cne $row.CommandLine -or
            $bound[0].created_utc -cne (Get-ProcessCreationUtc $row.CreationDate)) {
            throw 'F1-EXCLUSIVITY-REFUSED: additional or changed WSL actor'
        }
    }
}
