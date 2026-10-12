param(
    [Parameter(Mandatory = $true)][string]$Rootfs,
    [Parameter(Mandatory = $true)][string]$UbuntuSums,
    [Parameter(Mandatory = $true)][string]$UbuntuSignature,
    [Parameter(Mandatory = $true)][string]$OpenFoamKeyring,
    [Parameter(Mandatory = $true)][string]$NvidiaKeyring,
    [Parameter(Mandatory = $true)][string]$Contract,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]+$')][string]$RunId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]+$')][string]$AgentSession,
    [string]$Authorization = '',
    [string]$ExclusiveRunId = ''
)

# Ruling 217: one disposable metadata/download preflight. This file is never run by packet authors.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:AGENT_SESSION = $AgentSession
$Target = 'cfdw-petsc-cuda132-f1'
$Protected = 'cfdw-openfoam2512'
$TargetPath = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$HostStartFloor = [int64]19684091961
$HostReserve = [int64]10737418240
$VhdMaximum = [int64]8589934592
$RootfsSha256 = '8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea'
$NvidiaFingerprint = 'EB693B3035CD5710E231E123A4B469963BF863CC'
$OpenFoamFingerprint = 'DC93C096174122E256DA24063386DD74948D208F'
$PacketRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $PacketRoot 'Control.ps1')
$RepoRoot = (Resolve-Path (Join-Path $PacketRoot '..\..\..\..')).Path
$RunRoot = Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1'
$Live = Join-Path $RunRoot 'live'
$CheckpointPath = Join-Path $RunRoot 'checkpoint'
$Final = Join-Path $RunRoot 'final'
$CreationUncertain = $false
$script:LifecyclePhase = 'work'
$Clock = $null
$Guard = $null
$CreationArmed = $false
$CheckpointSha = $null
$script:CreatedTargetKey = ''
$InitiatingError = $null
$CleanupErrors = @()
$LastStage = 'not-started'
$LastCapacitySampleSeconds = -100.0

function Write-Lf([string]$Path, [string]$Value) {
    [IO.File]::WriteAllText($Path, ($Value -replace "`r`n", "`n"), [Text.UTF8Encoding]::new($false))
}
function Write-Json([string]$Path, [object]$Value) {
    Write-Lf $Path (($Value | ConvertTo-Json -Depth 20) + "`n")
}
function Convert-LinuxPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith('C:\', [StringComparison]::OrdinalIgnoreCase) -or $full -match '\s') {
        throw "path must be a no-space absolute C: path: $full"
    }
    return '/mnt/c/' + $full.Substring(3).Replace('\', '/')
}
function RemainingSeconds {
    if ($null -eq $Clock) { return 3600.0 }
    return 3600.0 - $Clock.Elapsed.TotalSeconds
}
function Assert-Deadline {
    if ((RemainingSeconds) -le 0) { throw 'R217 host deadline of 3600 seconds expired' }
}
function Get-DistroNames([int]$CapSeconds = 15) {
    $stdout = Invoke-Child 'wsl.exe' @('--list', '--quiet') 'distro-list' $CapSeconds $false $false
    $names = @([IO.File]::ReadAllText($stdout) -split "`n")
    return @($names | ForEach-Object { ([string]$_ -replace "`0", '').Trim() } | Where-Object { $_ })
}
function Get-TargetRegistration {
    $root = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss'
    $matches = @()
    if (Test-Path -LiteralPath $root) {
        foreach ($key in Get-ChildItem -LiteralPath $root) {
            $item = Get-ItemProperty -LiteralPath $key.PSPath
            if ([string]$item.DistributionName -ceq $Target) { $matches += $item }
        }
    }
    if ($matches.Count -gt 1) { throw 'duplicate exact target registrations' }
    return $matches
}
function Assert-TargetPath {
    $actual = [IO.Path]::GetFullPath($TargetPath).TrimEnd('\')
    $expected = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
    if (-not $actual.Equals($expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "unsafe target path: $actual"
    }
    $parent = [IO.Path]::GetFullPath((Split-Path -Parent $actual)).TrimEnd('\')
    if (-not $parent.Equals('C:\Projects\CFD-Workbench-wsl', [StringComparison]::OrdinalIgnoreCase)) {
        throw "unsafe target parent: $parent"
    }
    return $actual
}
function Get-HostFree {
    return [int64](Get-PSDrive -Name C).Free
}
function Assert-Host([bool]$Initial, [bool]$RequireVhd = $false) {
    $free = Get-HostFree
    $floor = if ($Initial) { $HostStartFloor } else { $HostReserve }
    if ($free -lt $floor) { throw "Windows C: free below floor: $free < $floor" }
    $vhdBytes = 'Not recorded'
    if (Test-Path -LiteralPath $TargetPath) {
        $vhds = @(Get-ChildItem -LiteralPath $TargetPath -Filter '*.vhdx' -File -Recurse)
        if ($vhds.Count -gt 1) { throw 'more than one target VHD' }
        if ($RequireVhd -and $vhds.Count -ne 1) { throw 'target physical VHD missing after creation' }
        if ($vhds.Count -eq 1) { $vhdBytes = [int64]$vhds[0].Length }
        if ($vhds.Count -eq 1 -and [int64]$vhds[0].Length -gt $VhdMaximum) {
            throw "target physical VHD exceeds 8 GiB: $($vhds[0].Length)"
        }
    }
    if ($RequireVhd -and $vhdBytes -isnot [int64]) { throw 'target physical VHD was not measured' }
    if ($null -ne $Clock -and $Clock.Elapsed.TotalSeconds - $script:LastCapacitySampleSeconds -ge 5) {
        $script:LastCapacitySampleSeconds = $Clock.Elapsed.TotalSeconds
        $row = [ordered]@{ utc=[DateTimeOffset]::UtcNow.ToString('o'); elapsed_seconds=$Clock.Elapsed.TotalSeconds
            host_free_bytes=$free; host_floor_bytes=$floor; target_vhd_physical_bytes=$vhdBytes }
        [IO.File]::AppendAllText((Join-Path $Live 'host-capacity.jsonl'),
            (($row | ConvertTo-Json -Compress) + "`n"), [Text.UTF8Encoding]::new($false))
    }
    return $free
}
function Read-GuardState {
    $path = Join-Path $Live 'completed-guard.state.json'
    if (-not (Test-Path -LiteralPath $path)) { throw 'completed guard state missing' }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}
function Assert-Guard {
    if ($null -eq $Guard -or $Guard.HasExited) { throw 'completed guard exited or was not started' }
    $state = Read-GuardState
    if ($state.state -cne 'running' -or $state.session_id -cne $RunId -or
        $state.expected_sha256 -cne $script:BeforeSha) { throw 'completed guard state/identity changed' }
    $age = Get-GuardAgeSeconds $state.last_sample_utc ([DateTimeOffset]::UtcNow)
    if ($age -lt -5 -or $age -gt 12) { throw "completed guard sample missing/stale: $age seconds" }
    $events = Join-Path $Live 'completed-guard-events.jsonl'
    if (-not (Test-Path -LiteralPath $events)) { throw 'completed guard event stream missing' }
    $raw = [IO.File]::ReadAllText($events)
    $lastLf = $raw.LastIndexOf("`n", [StringComparison]::Ordinal)
    if ($lastLf -lt 0) { throw 'completed guard has no complete event' }
    $line = $raw.Substring(0, $lastLf).Split("`n")[-1].TrimEnd("`r")
    $event = $line | ConvertFrom-Json
    if ($event.verdict -cne 'continue' -or $event.session_id -cne $RunId -or
        $event.expected_sha256 -cne $script:BeforeSha) { throw 'completed guard stop or event identity changed' }
    return $state.completed_samples
}
function Invoke-Child(
    [string]$File, [string[]]$Arguments, [string]$Label, [int]$CapSeconds,
    [bool]$GuardRequired = $true, [bool]$NewWork = $false, [string]$OwnerFile = ''
) {
    Assert-Deadline
    $phase = if ($NewWork) { 'work' } else { $script:LifecyclePhase }
    $stageStart = $Clock.Elapsed.TotalSeconds
    if ((Get-ChildWaitSeconds $stageStart $stageStart $CapSeconds $phase) -le 0) {
        throw "no $phase deadline remains for $Label"
    }
    $stdout = Join-Path $Live "$Label.stdout.txt"
    $stderr = Join-Path $Live "$Label.stderr.txt"
    $process = $null
    $ack = [string]::IsNullOrEmpty($OwnerFile)
    try {
        $process = Start-Process -FilePath $File -ArgumentList $Arguments `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden
        $null = $process.Handle
        while ($true) {
            $remaining = Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $stageStart $CapSeconds $phase
            if ($remaining -le 0) { throw "$Label exceeded min(stage cap, absolute $phase deadline)" }
            if ($process.WaitForExit([int][Math]::Max(1, [Math]::Min(200, $remaining * 1000)))) { break }
            if ($GuardRequired) { $null = Assert-Guard }
            if ($phase -eq 'work') { $null = Assert-Host $false }
            if (-not $ack -and (Test-Path -LiteralPath $OwnerFile)) { $ack = $true }
        }
        if ((Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $stageStart $CapSeconds $phase) -le 0) {
            throw "$Label completed after absolute $phase deadline"
        }
        if (-not $ack -and (Test-Path -LiteralPath $OwnerFile)) { $ack = $true }
        if (-not $ack) { throw "$Label never acknowledged its Linux owner" }
        if ($process.ExitCode -ne 0) { throw "$Label exited $($process.ExitCode)" }
        if ($GuardRequired) { $null = Assert-Guard }
        return $stdout
    } catch {
        if ($Label -eq 'target-create') { $script:CreationUncertain = $true }
        if ($Label -like 'stage-*') {
            Write-Lf (Join-Path $Live 'cancel.request') ($RunId + "`n")
        }
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        throw
    }
}
function Invoke-WslLinux(
    [string]$Distro, [string]$Cpus, [string]$Label, [string]$OwnerPath,
    [string[]]$Command, [int]$CapSeconds, [bool]$GuardRequired = $true,
    [bool]$NewWork = $false
) {
    $linuxOwner = Convert-LinuxPath $OwnerPath
    $identity = Convert-LinuxPath (Join-Path $PacketRoot 'identity.sh')
    $arguments = @('--distribution', $Distro, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', $Cpus, '/usr/bin/nice', '-n', '10',
        '/usr/bin/setsid', '--wait', '/bin/bash', $identity, $linuxOwner, '--') + $Command
    $stdout = Invoke-Child 'wsl.exe' $arguments $Label $CapSeconds $GuardRequired $NewWork $OwnerPath
    $record = Get-Content -LiteralPath $OwnerPath -Raw
    if ($record -notmatch '(?m)^pid=([0-9]+)$' -or $record -notmatch '(?m)^pgid=([0-9]+)$' -or
        $record -notmatch '(?m)^sid=([0-9]+)$' -or $record -notmatch '(?m)^nice=10$' -or
        $record -notmatch '(?m)^Cpus_allowed_list=(0|0-1|0,1)$') {
        throw "$Label owner record is incomplete"
    }
    return $stdout
}
function Invoke-Stage([string]$Stage, [int]$CapSeconds, [bool]$NewWork, [bool]$GuardRequired = $true) {
    $script:LastStage = $Stage
    if ($Stage -eq 'delete-debs') {
        Remove-Item -LiteralPath (Join-Path $Live 'cancel.request') -Force -ErrorAction SilentlyContinue
    }
    $intent = Join-Path $Live "$Stage.intent.json"
    $intentTemp = "$intent.tmp"
    Write-Json $intentTemp ([ordered]@{ run_id=$RunId; target=$Target; stage=$Stage
        utc=[DateTimeOffset]::UtcNow.ToString('o'); command='preflight.sh';
        ownership='stage owner is atomically published before side effects' })
    Move-Item -LiteralPath $intentTemp -Destination $intent -Force
    $linuxPacket = Convert-LinuxPath $PacketRoot
    $command = @('/bin/bash', "$linuxPacket/preflight.sh", '--stage', $Stage,
        '--run-root', (Convert-LinuxPath $Live), '--packet-root', $linuxPacket,
        '--b2-lock', (Convert-LinuxPath (Join-Path $RepoRoot 'docs\proof\win-gpu-b2\package-lock.json')),
        '--openfoam-key', (Convert-LinuxPath $OpenFoamKeyring),
        '--nvidia-key', (Convert-LinuxPath $NvidiaKeyring),
        '--openfoam-fingerprint', $OpenFoamFingerprint, '--run-id', $RunId,
        '--contract', (Convert-LinuxPath $Contract), '--contract-sha256', $script:ContractSha)
    $owner = Join-Path $Live "$Stage.entry.txt"
    $null = Invoke-WslLinux $Target '0-1' "stage-$Stage" $owner $command $CapSeconds $GuardRequired $NewWork
    if (-not (Test-Path -LiteralPath (Join-Path $Live "$Stage.owner.txt"))) {
        throw "$Stage stage owner metadata missing"
    }
    $checkOwner = Join-Path $Live "$Stage.residual-check.entry.txt"
    $check = @('/bin/bash', "$linuxPacket/owned-control.sh", '--mode', 'check',
        '--run-root', (Convert-LinuxPath $Live), $RunId)
    $null = Invoke-WslLinux $Target '0-1' "stage-$Stage-residual" $checkOwner $check 15 $GuardRequired $NewWork
    $ownerHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $Live "$Stage.owner.txt")).Hash.ToLowerInvariant()
    Write-Lf (Join-Path $Live "$Stage.residual-absent.sha256") ($ownerHash + "`n")
}
function Invoke-GitCommit([string]$Folder, [string]$Message, [string]$Label, [bool]$GuardRequired = $true) {
    $messageFile = Join-Path $Live "$Label.commit-message.txt"
    Write-Lf $messageFile ($Message + "`n")
    $relative = $Folder.Substring($RepoRoot.Length + 1).Replace('\', '/')
    $null = Invoke-Child 'git' @('-C', $RepoRoot, 'add', '-f', '--', $relative) "$Label-add" 60 $GuardRequired $false
    $null = Invoke-Child 'git' @('-C', $RepoRoot, 'commit', '-F', $messageFile) "$Label-commit" 120 $GuardRequired $false
    $shaOut = Invoke-Child 'git' @('-C', $RepoRoot, 'rev-parse', 'HEAD') "$Label-sha" 15 $GuardRequired $false
    $sha = ([IO.File]::ReadAllText($shaOut)).Trim()
    if ($sha -notmatch '^[0-9a-f]{40}$') { throw "$Label did not produce a commit SHA" }
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'manifest.py'), 'verify',
        '--repo', $RepoRoot, '--frozen', $Folder, '--commit', $sha) "$Label-bind" 120 $GuardRequired $false
    return $sha
}
function Save-Checkpoint {
    $script:LifecyclePhase = 'checkpoint'
    if ($Clock.Elapsed.TotalSeconds -ge 3000) { throw '50-minute checkpoint deadline passed' }
    Write-Json (Join-Path $Live 'precleanup-state.json') ([ordered]@{
        schema='cfdw-f1-r217-precleanup/1'; run_id=$RunId; stage=$LastStage
        initiating_error=if ($null -eq $InitiatingError) { 'Not recorded' } else { $InitiatingError }
        elapsed_seconds=$Clock.Elapsed.TotalSeconds; affinity='Linux CPU0-1 nice10; host Git N/A'
    })
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'manifest.py'), 'freeze',
        '--live', $Live, '--frozen', $CheckpointPath) 'checkpoint-freeze' 90 $false $false
    $candidate = Invoke-GitCommit $CheckpointPath 'proof: checkpoint F1 R217 before cleanup' 'checkpoint' $false
    if ($Clock.Elapsed.TotalSeconds -ge 3000) { throw 'checkpoint commit exceeded 50-minute deadline' }
    $script:CheckpointSha = $candidate
    Write-Lf (Join-Path $Live 'checkpoint-sha.txt') ($CheckpointSha + "`n")
}
function Stop-Owned {
    $script:LifecyclePhase = 'cleanup'
    $settled = Wait-TargetSettled 35
    if ($settled.registration_count -eq 0) {
        if ($settled.listed) { throw 'listed target lacks a registered owner' }
        return
    }
    $owner = Join-Path $Live 'owned-stop.entry.txt'
    $packet = Convert-LinuxPath $PacketRoot
    $command = @('/bin/bash', "$packet/owned-control.sh", '--mode', 'stop',
        '--run-root', (Convert-LinuxPath $Live), $RunId)
    $null = Invoke-WslLinux $Target '0-1' 'owned-stop' $owner $command 12 $false $false
    $checkOwner = Join-Path $Live 'owned-check.entry.txt'
    $check = @('/bin/bash', "$packet/owned-control.sh", '--mode', 'check',
        '--run-root', (Convert-LinuxPath $Live), $RunId)
    $null = Invoke-WslLinux $Target '0-1' 'owned-check' $checkOwner $check 10 $false $false
}
function Stop-OwnedOrContain {
    if ($freshAttempt -and @(Get-TargetRegistration).Count -ne 0) {
        if (-not $script:CreatedTargetKey) { throw 'MAC-DECISION-REQUIRED: created target GUID unbound; containment withheld' }
        $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey
    }
    try { Stop-Owned } catch {
        $original = $_.Exception.Message
        if (@(Get-TargetRegistration).Count -eq 1) {
            try {
                $script:LifecyclePhase = 'cleanup'
                if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey }
                $null = Invoke-Child 'wsl.exe' @('--terminate', $Target) 'target-containment-terminate' 30 $false $false
            } catch { throw "owner stop failed: $original; exact-target containment failed: $($_.Exception.Message)" }
        }
        throw "owner stop unproved after exact-target containment: $original"
    }
}
function Wait-TargetSettled([int]$CapSeconds) {
    $started = $Clock.Elapsed.TotalSeconds
    $last = ''
    $stableSince = $started
    while ($true) {
        $remaining = [Math]::Min(($started + $CapSeconds - $Clock.Elapsed.TotalSeconds), (RemainingSeconds))
        if ($remaining -lt 1) { throw 'target creation/absence did not settle inside deadline' }
        $names = Get-DistroNames ([int][Math]::Floor([Math]::Min(10, $remaining)))
        $registrations = @(Get-TargetRegistration)
        $present = @($names | Where-Object { $_ -ceq $Target }).Count -ne 0
        $pathPresent = Test-Path -LiteralPath $TargetPath
        $key = "$present|$($registrations.Count)|$pathPresent"
        $settle = Update-TargetSettle $key $last $Clock.Elapsed.TotalSeconds $stableSince
        $last = $settle.key
        $stableSince = $settle.stable_since
        if ($settle.stable) {
            return [pscustomobject]@{ listed=$present; registration_count=$registrations.Count
                path_exists=$pathPresent; observation=$key }
        }
        $sleepMs = [int][Math]::Min(200, [Math]::Max(1, ($started + $CapSeconds - $Clock.Elapsed.TotalSeconds) * 1000))
        Start-Sleep -Milliseconds $sleepMs
    }
}
function Assert-FinalAbsence {
    $names = Get-DistroNames
    if (@($names | Where-Object { $_ -ceq $Target }).Count -ne 0 -or
        @(Get-TargetRegistration).Count -ne 0 -or (Test-Path -LiteralPath $TargetPath)) {
        throw 'target distro, registration or path remains after cleanup'
    }
    if (@($names | Where-Object { $_ -ceq $Protected }).Count -ne 1) {
        throw 'protected distro missing after cleanup'
    }
}
function Invoke-Cleanup {
    if (-not $CreationArmed) { return }
    if ($null -eq $CheckpointSha) {
        throw 'MAC-DECISION-REQUIRED: checkpoint unproved, archive deletion/unregister withheld'
    }
    $script:LifecyclePhase = 'cleanup'
    $null = Wait-TargetSettled 35
    $registration = @(Get-TargetRegistration)
    if ($registration.Count -eq 1) {
        if ($freshAttempt) {
            if (-not $script:CreatedTargetKey) { throw 'MAC-DECISION-REQUIRED: target GUID unbound; cleanup withheld' }
            $null = Get-BoundTargetKey $registration $TargetPath $script:CreatedTargetKey
        }
        $basePath = [IO.Path]::GetFullPath([string]$registration[0].BasePath).TrimEnd('\')
        if (-not $basePath.Equals((Assert-TargetPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw "target registration path mismatch: $basePath"
        }
        try { Invoke-Stage 'delete-debs' 120 $false $false }
        finally { Stop-OwnedOrContain }
        if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey }
        $null = Invoke-Child 'wsl.exe' @('--terminate', $Target) 'target-terminate' 30 $false $false
        if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey }
        $null = Invoke-Child 'wsl.exe' @('--unregister', $Target) 'target-unregister' 120 $false $false
    }
    if (Test-Path -LiteralPath $TargetPath) {
        if ($freshAttempt) { throw 'MAC-DECISION-REQUIRED: residual target path; no unbound host deletion' }
        $checked = Assert-TargetPath
        $item = Get-Item -LiteralPath $checked -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'target path is a reparse point' }
        Remove-Item -LiteralPath $checked -Recurse -Force
    }
    $null = Wait-TargetSettled 35
    Assert-FinalAbsence
}
function Capture-FinalSnapshot {
    $script:LifecyclePhase = 'cleanup'
    if ($null -eq $Guard -or -not $script:BeforeSha) { return }
    if ($script:CleanupProved) {
        try {
            $beforeCount = (Read-GuardState).completed_samples
            $sampleStart = $Clock.Elapsed.TotalSeconds
            while ((Read-GuardState).completed_samples -le $beforeCount) {
                if ((Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $sampleStart 12 cleanup) -le 0 -or
                    $Guard.HasExited) { throw 'no post-cleanup guard sample' }
                Start-Sleep -Milliseconds 200
            }
        } catch { throw "post-cleanup guard sample failed: $($_.Exception.Message)" }
    }
    $after = Join-Path $Live 'l3-terminal-after.json'
    $null = Invoke-WslLinux $Protected '0' 'l3-after' (Join-Path $Live 'l3-after.owner.txt') `
        @('/usr/bin/python3', (Convert-LinuxPath (Join-Path $PacketRoot 'completed_guard.py')),
          'snapshot', '--output', (Convert-LinuxPath $after), '--include-log-sha256') 60 $false $false
    $afterSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $after).Hash.ToLowerInvariant()
    if ($afterSha -cne $BeforeSha) { throw 'protected terminal snapshot changed' }
    if ($script:CleanupProved -and $CreationArmed) { Assert-FinalAbsence }
}
function Stop-Observer {
    if ($null -eq $Guard) { return }
    $script:LifecyclePhase = 'cleanup'
    $observerErrors = [Collections.Generic.List[string]]::new()
    try { Write-Lf (Join-Path $Live 'completed-guard-stop.request') ($RunId + "`n") }
    catch { $observerErrors.Add("stop request: $($_.Exception.Message)") }
    try {
        $stopStart = $Clock.Elapsed.TotalSeconds
        $wait = Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $stopStart 20 cleanup
        $reaped = $wait -gt 0 -and $Guard.WaitForExit([int]($wait * 1000))
        if (-not $reaped -and -not $Guard.HasExited) {
            Stop-Process -Id $Guard.Id -Force -ErrorAction SilentlyContinue
        }
    } catch { $observerErrors.Add("observer wait: $($_.Exception.Message)") }
    if (-not $Guard.HasExited) {
        try { Stop-Process -Id $Guard.Id -Force -ErrorAction Stop }
        catch { $observerErrors.Add("observer launcher stop: $($_.Exception.Message)") }
    }
    $packet = Convert-LinuxPath $PacketRoot
    $command = @('/bin/bash', "$packet/owned-control.sh", '--mode', 'stop',
        '--run-root', (Convert-LinuxPath $Live), $RunId, '--kind', 'guard')
    try {
        $null = Invoke-WslLinux $Protected '0' 'guard-owned-stop' (Join-Path $Live 'guard-owned-stop.entry.txt') `
            $command 15 $false $false
    } catch { $observerErrors.Add("observer owned stop: $($_.Exception.Message)") }
    $command[3] = 'check'
    try {
        $null = Invoke-WslLinux $Protected '0' 'guard-owned-check' (Join-Path $Live 'guard-owned-check.entry.txt') `
            $command 15 $false $false
    } catch { $observerErrors.Add("observer residual check: $($_.Exception.Message)") }
    if (-not $Guard.HasExited) { $observerErrors.Add('observer launcher was not reaped') }
    elseif ($Guard.ExitCode -ne 0) { $observerErrors.Add("observer exit $($Guard.ExitCode) after bounded stop") }
    if ($observerErrors.Count -gt 0) { throw ($observerErrors -join '; ') }
}
function Bind-Observer {
    $script:LifecyclePhase = 'cleanup'
    if ($null -eq $Guard) { return }
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'completed_guard.py'), 'bind',
        '--events', (Join-Path $Live 'completed-guard-events.jsonl'),
        '--snapshot', (Join-Path $Live 'completed-guard.snapshot.jsonl'),
        '--binding', (Join-Path $Live 'completed-guard-binding.json'), '--session-id', $RunId) `
        'guard-bind' 30 $false $false
}

# Input validation and static self-tests precede the measured final precheck.
foreach ($inputPath in @($Rootfs, $UbuntuSums, $UbuntuSignature, $OpenFoamKeyring, $NvidiaKeyring, $Contract)) {
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) { throw "missing reviewed input: $inputPath" }
    $null = Convert-LinuxPath $inputPath
}
$contractData = [IO.File]::ReadAllText($Contract) | ConvertFrom-Json
$RunRoot = Get-InvocationRunRoot $contractData $RepoRoot $RunId $AgentSession
$Live = Join-Path $RunRoot 'live'
$CheckpointPath = Join-Path $RunRoot 'checkpoint'
$Final = Join-Path $RunRoot 'final'
$freshAttempt = $null -ne $contractData.psobject.Properties['attempt']
if ($freshAttempt -and (-not $Authorization -or -not (Test-Path -LiteralPath $Authorization -PathType Leaf))) {
    throw 'attempt authorization missing before WSL'
}
$null = Assert-TargetPath
if ($RunRoot -match '\s' -or -not [IO.Path]::GetFullPath($Contract).Equals(
        [IO.Path]::GetFullPath((Join-Path $RunRoot 'invocation-contract.json')),
        [StringComparison]::OrdinalIgnoreCase)) { throw 'contract path differs from validated invocation root' }
if ((Test-Path -LiteralPath $Live) -or (Test-Path -LiteralPath $CheckpointPath) -or
    (Test-Path -LiteralPath $Final)) { throw 'execution output already exists; one attempt only' }
$authority = & py -3 (Join-Path $PacketRoot 'authority_scan.py')
if ($LASTEXITCODE -ne 0) { throw 'packet authority scan failed before live work' }
$Clock = [Diagnostics.Stopwatch]::StartNew()
$precheckOut = Join-Path $env:TEMP ("cfdw-r217-contract-" + $RunId + '.stdout.txt')
$precheckErr = Join-Path $env:TEMP ("cfdw-r217-contract-" + $RunId + '.stderr.txt')
$precheckStart = $Clock.Elapsed.TotalSeconds
$contractData = [IO.File]::ReadAllText($Contract) | ConvertFrom-Json
$executionBranch = $contractData.execution_branch
$contractArguments = @('-3', (Join-Path $PacketRoot 'contract.py'), 'verify',
    '--contract', $Contract, '--repo', $RepoRoot, '--rootfs', $Rootfs,
    '--ubuntu-sums', $UbuntuSums, '--ubuntu-signature', $UbuntuSignature,
    '--openfoam-keyring', $OpenFoamKeyring, '--nvidia-keyring', $NvidiaKeyring,
    '--branch', $executionBranch, '--session', $AgentSession, '--run-id', $RunId)
if ($freshAttempt) { $contractArguments += @('--authorization', $Authorization, '--exclusive-run-id', $ExclusiveRunId) }
$precheck = Start-Process -FilePath 'py' -ArgumentList $contractArguments `
    -RedirectStandardOutput $precheckOut -RedirectStandardError $precheckErr -PassThru -WindowStyle Hidden
$null = $precheck.Handle
$precheckWait = Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $precheckStart 90 work
if ($precheckWait -le 0 -or -not $precheck.WaitForExit([int]($precheckWait * 1000))) {
    if (-not $precheck.HasExited) { Stop-Process -Id $precheck.Id -Force -ErrorAction SilentlyContinue }
    throw 'invocation contract precheck exceeded absolute deadline'
}
if ($precheck.ExitCode -ne 0) { throw "invocation contract rejected: $([IO.File]::ReadAllText($precheckErr))" }
$script:ContractSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $Contract).Hash.ToLowerInvariant()
New-Item -ItemType Directory -Path $Live -Force | Out-Null
Copy-Item -LiteralPath $precheckOut -Destination (Join-Path $Live 'invocation-contract.stdout.txt')
$linuxGuard = Convert-LinuxPath (Join-Path $PacketRoot 'completed_guard.py')
$linuxRoot = Convert-LinuxPath $Rootfs
$linuxSums = Convert-LinuxPath $UbuntuSums
$linuxSignature = Convert-LinuxPath $UbuntuSignature
try {
    $initialHostFree = Assert-Host $true
    $names = Get-DistroNames
    if ($freshAttempt) {
        $rows = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath })
        Assert-FreshRegistrations $rows $names (Test-Path -LiteralPath $TargetPath)
    }
    if (@($names | Where-Object { $_ -ceq $Protected }).Count -ne 1 -or
        @($names | Where-Object { $_ -ceq $Target }).Count -ne 0 -or
        @(Get-TargetRegistration).Count -ne 0 -or (Test-Path -LiteralPath $TargetPath)) {
        throw 'final precheck found target collision or missing protected distro'
    }
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $Rootfs).Hash.ToLowerInvariant() -cne $RootfsSha256) {
        throw 'existing B2 rootfs SHA-256 changed'
    }
    $packetHashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $PacketRoot -File) {
        $packetHashes[$file.Name] = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
    }
    Write-Json (Join-Path $Live 'final-precheck.json') ([ordered]@{
        schema='cfdw-f1-r217-final-precheck/1'; utc=[DateTimeOffset]::UtcNow.ToString('o')
        target=$Target; protected=$Protected; distro_names=$names; host_free_bytes=$initialHostFree
        host_required_bytes=$HostStartFloor; rootfs_sha256=$RootfsSha256
        invocation_contract_sha256=$script:ContractSha
        accepted_packet_commit=$contractData.accepted_packet_commit
        leader='mac'; leader_epoch=$contractData.leader_epoch; execution_branch=$executionBranch; execution_session=$AgentSession
        b2_lock_sha256=$contractData.inputs.b2_lock.sha256
        b2_licenses_sha256=$contractData.inputs.b2_licenses.sha256
        openfoam_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $OpenFoamKeyring).Hash.ToLowerInvariant()
        nvidia_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $NvidiaKeyring).Hash.ToLowerInvariant()
        openfoam_fingerprint=$OpenFoamFingerprint; nvidia_fingerprint=$NvidiaFingerprint
        packet_sha256=$packetHashes; affinity='host lifecycle N/A'
    })
    $verifyRootfs = Convert-LinuxPath (Join-Path $PacketRoot 'verify-rootfs.sh')
    $null = Invoke-WslLinux $Protected '0' 'rootfs-signature' (Join-Path $Live 'rootfs-signature.owner.txt') `
        @('/bin/bash', $verifyRootfs, $linuxSums, $linuxSignature, $linuxRoot,
          (Convert-LinuxPath $Contract), (Convert-LinuxPath $PacketRoot),
          (Convert-LinuxPath (Join-Path $Live 'rootfs-signature.gpgv.txt')),
          $script:ContractSha) 90 $false $false
    $before = Join-Path $Live 'l3-terminal-before.json'
    $null = Invoke-WslLinux $Protected '0' 'l3-before' (Join-Path $Live 'l3-before.owner.txt') `
        @('/usr/bin/python3', $linuxGuard, 'snapshot', '--output', (Convert-LinuxPath $before), '--include-log-sha256') 60 $false $false
    $script:BeforeSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $before).Hash.ToLowerInvariant()
    $events = Join-Path $Live 'completed-guard-events.jsonl'
    $state = Join-Path $Live 'completed-guard.state.json'
    $stopRequest = Join-Path $Live 'completed-guard-stop.request'
    $guardArguments = @('--distribution', $Protected, '--user', 'root', '--exec',
        '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
        '/usr/bin/setsid', '--wait', '/bin/bash', (Convert-LinuxPath (Join-Path $PacketRoot 'identity.sh')),
        (Convert-LinuxPath (Join-Path $Live 'guard.owner.txt')), '--',
        '/usr/bin/python3', $linuxGuard, 'watch', '--expected', (Convert-LinuxPath $before),
        '--expected-sha256', $BeforeSha, '--events', (Convert-LinuxPath $events),
        '--state', (Convert-LinuxPath $state), '--stop-request', (Convert-LinuxPath $stopRequest),
        '--session-id', $RunId, '--interval-seconds', '5', '--max-samples', '732')
    $guardStart = $Clock.Elapsed.TotalSeconds
    $Guard = Start-Process -FilePath 'wsl.exe' -ArgumentList $guardArguments `
        -RedirectStandardOutput (Join-Path $Live 'guard.stdout.txt') `
        -RedirectStandardError (Join-Path $Live 'guard.stderr.txt') -PassThru -WindowStyle Hidden
    $null = $Guard.Handle
    while (-not (Test-Path -LiteralPath $state)) {
        Assert-Deadline
        if ($Guard.HasExited -or (Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $guardStart 20 work) -le 0) {
            throw 'completed guard did not start inside absolute startup deadline'
        }
        Start-Sleep -Milliseconds 200
    }
    $null = Assert-Guard
    $script:CreationArmed = $true  # Before the first target-creation effect.
    if ($freshAttempt) {
        $freshNames = Get-DistroNames
        $freshRows = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath })
        Assert-FreshRegistrations $freshRows $freshNames (Test-Path -LiteralPath $TargetPath)
    }
    $null = Invoke-Child 'wsl.exe' @('--install', '--from-file', $Rootfs, '--name', $Target,
        '--location', $TargetPath, '--vhd-size', '8GB', '--no-launch') 'target-create' 300 $true $true
    if (@(Get-TargetRegistration).Count -ne 1) { throw 'target creation did not register exactly once' }
    $script:CreatedTargetKey = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath
    Write-Json (Join-Path $Live 'created-target-identity.json') ([ordered]@{
        target=$Target; target_path=$TargetPath; target_key=$script:CreatedTargetKey; run_id=$RunId })
    $null = Assert-Host $false $true
    Invoke-Stage 'probe' 60 $true
    # No network stage is entered until both guest and host VHD checks have passed.
    Invoke-Stage 'repositories' 300 $true
    Invoke-Stage 'simulate' 300 $true
    Invoke-Stage 'download' 1200 $true
    Invoke-Stage 'inspect' 600 $true
} catch {
    $InitiatingError = $_.Exception.Message
} finally {
    $script:CleanupProved = $false
    $script:LifecyclePhase = 'cleanup'
    $closing = Invoke-Finalization `
        -StopOwned { if ($CreationArmed) { Stop-OwnedOrContain } } `
        -Checkpoint { Save-Checkpoint } `
        -Cleanup { if ($CreationArmed) { Invoke-Cleanup }; $script:CleanupProved = $true } `
        -Snapshot { Capture-FinalSnapshot } `
        -StopGuard { Stop-Observer } `
        -BindGuard { Bind-Observer }
    $CleanupErrors += $closing.errors
    Write-Json (Join-Path $Live 'cleanup-receipt.json') ([ordered]@{
        schema='cfdw-f1-r217-cleanup/1'; run_id=$RunId; target=$Target; protected=$Protected
        initiating_error=if ($null -eq $InitiatingError) { 'Not recorded' } else { $InitiatingError }
        cleanup_errors=$CleanupErrors; checkpoint_sha=if ($null -eq $CheckpointSha) { 'Not recorded' } else { $CheckpointSha }
        elapsed_seconds=if ($null -eq $Clock) { 'Not recorded' } else { $Clock.Elapsed.TotalSeconds }
        target_absent=if ($CreationArmed -and $script:CleanupProved -and -not $CreationUncertain) { $true }
                      elseif ($CreationArmed) { 'Unknown or blocked' } else { 'Not applicable' }
        creation_uncertain=$CreationUncertain; finalization=$closing
        affinity='Linux CPU0-1 or guard CPU0, nice10; host lifecycle/Git N/A'
    })
    if ($null -ne $CheckpointSha) {
        try {
            $script:LifecyclePhase = 'cleanup'
            $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'manifest.py'), 'freeze',
                '--live', $Live, '--frozen', $Final) 'final-freeze' 90 $false $false
            $null = Invoke-GitCommit $Final 'proof: bind F1 R217 cleanup and target absence' 'final' $false
        } catch { $CleanupErrors += "final commit: $($_.Exception.Message)" }
    }
}
if ($null -ne $InitiatingError -or $CleanupErrors.Count -gt 0 -or $CreationUncertain) {
    Write-Error "R217 preflight blocked; initial=$InitiatingError; cleanup=$($CleanupErrors -join '; ')"
    exit 5
}
Write-Output "R217-PREFLIGHT-PASS checkpoint=$CheckpointSha elapsed_seconds=$($Clock.Elapsed.TotalSeconds)"
exit 0
