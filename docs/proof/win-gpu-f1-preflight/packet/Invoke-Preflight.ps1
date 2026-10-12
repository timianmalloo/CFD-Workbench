param(
    [Parameter(Mandatory = $true)][string]$Rootfs,
    [Parameter(Mandatory = $true)][string]$UbuntuSums,
    [Parameter(Mandatory = $true)][string]$UbuntuSignature,
    [Parameter(Mandatory = $true)][string]$OpenFoamKeyring,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-F0-9]{40}$')][string]$OpenFoamFingerprint,
    [Parameter(Mandatory = $true)][string]$NvidiaKeyring,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]+$')][string]$RunId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]+$')][string]$AgentSession
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
$PacketRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $PacketRoot '..\..\..\..')).Path
$RunRoot = Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1'
$Live = Join-Path $RunRoot 'live'
$Checkpoint = Join-Path $RunRoot 'checkpoint'
$Final = Join-Path $RunRoot 'final'
$Clock = $null
$Guard = $null
$CreationArmed = $false
$CheckpointSha = $null
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
function Get-DistroNames {
    $names = @(& wsl.exe --list --quiet)
    if ($LASTEXITCODE -ne 0) { throw "WSL list failed: $LASTEXITCODE" }
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
function Assert-Host([bool]$Initial) {
    $free = Get-HostFree
    $floor = if ($Initial) { $HostStartFloor } else { $HostReserve }
    if ($free -lt $floor) { throw "Windows C: free below floor: $free < $floor" }
    $vhdBytes = 'Not recorded'
    if (Test-Path -LiteralPath $TargetPath) {
        $vhds = @(Get-ChildItem -LiteralPath $TargetPath -Filter '*.vhdx' -File -Recurse)
        if ($vhds.Count -gt 1) { throw 'more than one target VHD' }
        if ($vhds.Count -eq 1) { $vhdBytes = [int64]$vhds[0].Length }
        if ($vhds.Count -eq 1 -and [int64]$vhds[0].Length -gt $VhdMaximum) {
            throw "target physical VHD exceeds 8 GiB: $($vhds[0].Length)"
        }
    }
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
    $age = ([DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse([string]$state.last_sample_utc)).TotalSeconds
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
    if ($NewWork -and $Clock.Elapsed.TotalSeconds -ge 2700) { throw '45-minute new-work cutoff reached' }
    $stageRemaining = if ($NewWork) { 2700.0 - $Clock.Elapsed.TotalSeconds } else { RemainingSeconds }
    $waitSeconds = [Math]::Min([double]$CapSeconds, [Math]::Min((RemainingSeconds), $stageRemaining))
    if ($waitSeconds -le 0) { throw "no deadline remains for $Label" }
    $stdout = Join-Path $Live "$Label.stdout.txt"
    $stderr = Join-Path $Live "$Label.stderr.txt"
    $process = Start-Process -FilePath $File -ArgumentList $Arguments `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden
    $null = $process.Handle
    $stageClock = [Diagnostics.Stopwatch]::StartNew()
    $ack = [string]::IsNullOrEmpty($OwnerFile)
    try {
        while (-not $process.WaitForExit(200)) {
            if ($GuardRequired) { $null = Assert-Guard }
            $null = Assert-Host $false
            Assert-Deadline
            if ($stageClock.Elapsed.TotalSeconds -ge $waitSeconds) {
                throw "$Label exceeded min(stage cap, remaining host deadline)"
            }
            if (-not $ack -and (Test-Path -LiteralPath $OwnerFile)) { $ack = $true }
        }
        if (-not $ack -and (Test-Path -LiteralPath $OwnerFile)) { $ack = $true }
        if (-not $ack) { throw "$Label never acknowledged its Linux owner" }
        if ($process.ExitCode -ne 0) { throw "$Label exited $($process.ExitCode)" }
        if ($GuardRequired) { $null = Assert-Guard }
        return $stdout
    } catch {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
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
    $linuxPacket = Convert-LinuxPath $PacketRoot
    $command = @('/bin/bash', "$linuxPacket/preflight.sh", '--stage', $Stage,
        '--run-root', (Convert-LinuxPath $Live), '--packet-root', $linuxPacket,
        '--b2-lock', (Convert-LinuxPath (Join-Path $RepoRoot 'docs\proof\win-gpu-b2\package-lock.json')),
        '--openfoam-key', (Convert-LinuxPath $OpenFoamKeyring),
        '--nvidia-key', (Convert-LinuxPath $NvidiaKeyring),
        '--openfoam-fingerprint', $OpenFoamFingerprint, '--run-id', $RunId)
    $owner = Join-Path $Live "$Stage.entry.txt"
    $null = Invoke-WslLinux $Target '0-1' "stage-$Stage" $owner $command $CapSeconds $GuardRequired $NewWork
    if (-not (Test-Path -LiteralPath (Join-Path $Live "$Stage.owner.txt"))) {
        throw "$Stage stage owner metadata missing"
    }
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
    if ($Clock.Elapsed.TotalSeconds -ge 3000) { throw '50-minute checkpoint deadline passed' }
    Write-Json (Join-Path $Live 'precleanup-state.json') ([ordered]@{
        schema='cfdw-f1-r217-precleanup/1'; run_id=$RunId; stage=$LastStage
        initiating_error=if ($null -eq $InitiatingError) { 'Not recorded' } else { $InitiatingError }
        elapsed_seconds=$Clock.Elapsed.TotalSeconds; affinity='Linux CPU0-1 nice10; host Git N/A'
    })
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'manifest.py'), 'freeze',
        '--live', $Live, '--frozen', $Checkpoint) 'checkpoint-freeze' 90 $true $false
    $script:CheckpointSha = Invoke-GitCommit $Checkpoint 'proof: checkpoint F1 R217 before cleanup' 'checkpoint'
    if ($Clock.Elapsed.TotalSeconds -ge 3000) { throw 'checkpoint commit exceeded 50-minute deadline' }
    Write-Lf (Join-Path $Live 'checkpoint-sha.txt') ($CheckpointSha + "`n")
}
function Stop-Owned {
    if (@(Get-TargetRegistration).Count -ne 1) { return }
    $owner = Join-Path $Live 'owned-stop.entry.txt'
    $packet = Convert-LinuxPath $PacketRoot
    $command = @('/bin/bash', "$packet/owned-control.sh", '--mode', 'stop', (Convert-LinuxPath $Live))
    $null = Invoke-WslLinux $Target '0-1' 'owned-stop' $owner $command 12 $false $false
    $checkOwner = Join-Path $Live 'owned-check.entry.txt'
    $check = @('/bin/bash', "$packet/owned-control.sh", '--mode', 'check', (Convert-LinuxPath $Live))
    $null = Invoke-WslLinux $Target '0-1' 'owned-check' $checkOwner $check 10 $false $false
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
        $script:CleanupErrors += 'checkpoint was not proved: no archive deletion or unregister permitted'
        return
    }
    $registration = @(Get-TargetRegistration)
    if ($registration.Count -eq 1) {
        $basePath = [IO.Path]::GetFullPath([string]$registration[0].BasePath).TrimEnd('\')
        if (-not $basePath.Equals((Assert-TargetPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw "target registration path mismatch: $basePath"
        }
        Stop-Owned
        Invoke-Stage 'delete-debs' 120 $false $false
        Stop-Owned
        $null = Invoke-Child 'wsl.exe' @('--terminate', $Target) 'target-terminate' 30 $false $false
        $null = Invoke-Child 'wsl.exe' @('--unregister', $Target) 'target-unregister' 120 $false $false
    }
    if (Test-Path -LiteralPath $TargetPath) {
        $checked = Assert-TargetPath
        $item = Get-Item -LiteralPath $checked -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'target path is a reparse point' }
        Remove-Item -LiteralPath $checked -Recurse -Force
    }
    Assert-FinalAbsence
}

# Input validation and static self-tests precede the measured final precheck.
foreach ($inputPath in @($Rootfs, $UbuntuSums, $UbuntuSignature, $OpenFoamKeyring, $NvidiaKeyring)) {
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) { throw "missing reviewed input: $inputPath" }
    $null = Convert-LinuxPath $inputPath
}
$null = Assert-TargetPath
if ((Test-Path -LiteralPath $RunRoot) -or $RunRoot -match '\s') { throw 'run-1 exists or has whitespace' }
$authority = & py -3 (Join-Path $PacketRoot 'authority_scan.py')
if ($LASTEXITCODE -ne 0) { throw 'packet authority scan failed before live work' }
New-Item -ItemType Directory -Path $Live -Force | Out-Null
$linuxGuard = Convert-LinuxPath (Join-Path $PacketRoot 'completed_guard.py')
$linuxRoot = Convert-LinuxPath $Rootfs
$linuxSums = Convert-LinuxPath $UbuntuSums
$linuxSignature = Convert-LinuxPath $UbuntuSignature
$Clock = [Diagnostics.Stopwatch]::StartNew()
try {
    $initialHostFree = Assert-Host $true
    $names = Get-DistroNames
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
        openfoam_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $OpenFoamKeyring).Hash.ToLowerInvariant()
        nvidia_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $NvidiaKeyring).Hash.ToLowerInvariant()
        openfoam_fingerprint=$OpenFoamFingerprint; nvidia_fingerprint=$NvidiaFingerprint
        packet_sha256=$packetHashes; affinity='host lifecycle N/A'
    })
    $verifyRootfs = Convert-LinuxPath (Join-Path $PacketRoot 'verify-rootfs.sh')
    $null = Invoke-WslLinux $Protected '0' 'rootfs-signature' (Join-Path $Live 'rootfs-signature.owner.txt') `
        @('/bin/bash', $verifyRootfs, $linuxSums, $linuxSignature, $linuxRoot) 90 $false $false
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
    $Guard = Start-Process -FilePath 'wsl.exe' -ArgumentList $guardArguments `
        -RedirectStandardOutput (Join-Path $Live 'guard.stdout.txt') `
        -RedirectStandardError (Join-Path $Live 'guard.stderr.txt') -PassThru -WindowStyle Hidden
    $null = $Guard.Handle
    $startup = [Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-Path -LiteralPath $state)) {
        Assert-Deadline
        if ($Guard.HasExited -or $startup.Elapsed.TotalSeconds -gt 20) { throw 'completed guard did not start' }
        Start-Sleep -Milliseconds 200
    }
    $null = Assert-Guard
    $script:CreationArmed = $true  # Before the first target-creation effect.
    $null = Invoke-Child 'wsl.exe' @('--install', '--from-file', $Rootfs, '--name', $Target,
        '--location', $TargetPath, '--vhd-size', '8GB', '--no-launch') 'target-create' 300 $true $true
    if (@(Get-TargetRegistration).Count -ne 1) { throw 'target creation did not register exactly once' }
    $null = Assert-Host $false
    Invoke-Stage 'probe' 60 $true
    # No network stage is entered until both guest and host VHD checks have passed.
    Invoke-Stage 'repositories' 300 $true
    Invoke-Stage 'simulate' 300 $true
    Invoke-Stage 'download' 1200 $true
    Invoke-Stage 'inspect' 600 $true
} catch {
    $InitiatingError = $_.Exception.Message
} finally {
    if ($CreationArmed) {
        try { Save-Checkpoint } catch { $CleanupErrors += "checkpoint: $($_.Exception.Message)" }
        try { Invoke-Cleanup } catch { $CleanupErrors += "cleanup: $($_.Exception.Message)" }
    }
    if ($null -ne $Guard) {
        try {
            if ($CreationArmed -and $null -ne $CheckpointSha) {
                $preAfterCount = (Read-GuardState).completed_samples
                $sampleWait = [Diagnostics.Stopwatch]::StartNew()
                while ((Read-GuardState).completed_samples -le $preAfterCount) {
                    Assert-Deadline
                    if ($sampleWait.Elapsed.TotalSeconds -gt 12 -or $Guard.HasExited) { throw 'no post-cleanup guard sample' }
                    Start-Sleep -Milliseconds 200
                }
            }
            $after = Join-Path $Live 'l3-terminal-after.json'
            $null = Invoke-WslLinux $Protected '0' 'l3-after' (Join-Path $Live 'l3-after.owner.txt') `
                @('/usr/bin/python3', $linuxGuard, 'snapshot', '--output', (Convert-LinuxPath $after), '--include-log-sha256') 60 $false $false
            $afterSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $after).Hash.ToLowerInvariant()
            if ($afterSha -cne $BeforeSha) { throw 'protected terminal snapshot changed' }
            if ($CreationArmed -and $null -ne $CheckpointSha) { Assert-FinalAbsence }
            Write-Lf $stopRequest ($RunId + "`n")
            $wait = [Math]::Min(20.0, (RemainingSeconds))
            if ($wait -le 0 -or -not $Guard.WaitForExit([int]($wait * 1000)) -or $Guard.ExitCode -ne 0) {
                throw 'completed guard did not stop inside host deadline'
            }
            $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'completed_guard.py'), 'bind',
                '--events', $events, '--snapshot', (Join-Path $Live 'completed-guard.snapshot.jsonl'),
                '--binding', (Join-Path $Live 'completed-guard-binding.json'), '--session-id', $RunId) `
                'guard-bind' 30 $false $false
        } catch { $CleanupErrors += "guard/final snapshot: $($_.Exception.Message)" }
    }
    Write-Json (Join-Path $Live 'cleanup-receipt.json') ([ordered]@{
        schema='cfdw-f1-r217-cleanup/1'; run_id=$RunId; target=$Target; protected=$Protected
        initiating_error=if ($null -eq $InitiatingError) { 'Not recorded' } else { $InitiatingError }
        cleanup_errors=$CleanupErrors; checkpoint_sha=if ($null -eq $CheckpointSha) { 'Not recorded' } else { $CheckpointSha }
        elapsed_seconds=if ($null -eq $Clock) { 'Not recorded' } else { $Clock.Elapsed.TotalSeconds }
        target_absent=if ($CreationArmed -and $null -ne $CheckpointSha) {
            (@(Get-TargetRegistration).Count -eq 0 -and -not (Test-Path -LiteralPath $TargetPath))
        } else { 'Not recorded' }
        affinity='Linux CPU0-1 or guard CPU0, nice10; host lifecycle/Git N/A'
    })
    if ($null -ne $CheckpointSha) {
        try {
            $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'manifest.py'), 'freeze',
                '--live', $Live, '--frozen', $Final) 'final-freeze' 90 $false $false
            $null = Invoke-GitCommit $Final 'proof: bind F1 R217 cleanup and target absence' 'final' $false
        } catch { $CleanupErrors += "final commit: $($_.Exception.Message)" }
    }
}
if ($null -ne $InitiatingError -or $CleanupErrors.Count -gt 0) {
    Write-Error "R217 preflight blocked; initial=$InitiatingError; cleanup=$($CleanupErrors -join '; ')"
    exit 5
}
Write-Output "R217-PREFLIGHT-PASS checkpoint=$CheckpointSha elapsed_seconds=$($Clock.Elapsed.TotalSeconds)"
exit 0
