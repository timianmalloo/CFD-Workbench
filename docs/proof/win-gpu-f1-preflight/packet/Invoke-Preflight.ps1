param(
    [Parameter(Mandatory = $true)][string]$Rootfs,
    [Parameter(Mandatory = $true)][string]$UbuntuSums,
    [Parameter(Mandatory = $true)][string]$UbuntuSignature,
    [Parameter(Mandatory = $true)][string]$OpenFoamKeyring,
    [Parameter(Mandatory = $true)][string]$NvidiaKeyring,
    [Parameter(Mandatory = $true)][string]$UbuntuCloudimageKeyring,
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
$Protected = $null   # Ruling 240 (1): the contract's protected field ('cfdw-openfoam2512' or 'none'), bound after the precheck
$VhdMaximum = [int64]8589934592
$NvidiaFingerprint = 'EB693B3035CD5710E231E123A4B469963BF863CC'
$OpenFoamFingerprint = 'DC93C096174122E256DA24063386DD74948D208F'
$PacketRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $PacketRoot 'Control.ps1')
$RepoRoot = (Resolve-Path (Join-Path $PacketRoot '..\..\..\..')).Path
$TargetPath = Get-DerivedTargetPath $RepoRoot   # Ruling 237: derived from the repository location, never a constant
$HostStartFloor = [int64]0   # Ruling 238 (3): both floors come from the contract (Get-ContractHostFloors) after the precheck
$HostReserve = [int64]0
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
$attemptNumber = 1
$script:FirstWslStarted = $false
$script:Renewal = $null
$script:OwnedLaunchers = @()
$script:RenewalIdentity = $null
$script:LastIdentitySampleSeconds = -100.0
$script:Architecture = ''
$script:GpgvPath = ''

function Write-Lf([string]$Path, [string]$Value) {
    [IO.File]::WriteAllText($Path, ($Value -replace "`r`n", "`n"), [Text.UTF8Encoding]::new($false))
}
function Write-Json([string]$Path, [object]$Value) {
    Write-Lf $Path (($Value | ConvertTo-Json -Depth 20) + "`n")
}
function Write-HostObservations([string]$Path) {
    # Ruling 238 (1): the raw host identifiers, recorded before the first WSL effect; architecture.py maps them once.
    $gpgv = Get-GpgvObservation   # Ruling 240 (2): the Windows-side verifier's path and version go into the receipt
    $script:GpgvPath = [string]$gpgv.path
    Write-Json $Path ([ordered]@{
        schema='cfdw-f1-host-observations/1'; utc=[DateTimeOffset]::UtcNow.ToString('o')
        powershell_version=$PSVersionTable.PSVersion.ToString(); powershell_edition=[string]$PSVersionTable.PSEdition
        os_architecture=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        process_architecture=[Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        processor_architecture_env=[string]$env:PROCESSOR_ARCHITECTURE
        processor_architew6432_env=[string]$env:PROCESSOR_ARCHITEW6432
        os_architecture_cim=[string](Get-CimInstance Win32_OperatingSystem).OSArchitecture
        os_version=[Environment]::OSVersion.Version.ToString()
        cpu=@(Get-CimInstance Win32_Processor | ForEach-Object { [string]$_.Name })
        gpu=@(Get-CimInstance Win32_VideoController | ForEach-Object { [string]$_.Name })
        gpgv_path=[string]$gpgv.path; gpgv_version=[string]$gpgv.version
    })
}
function Assert-HostIdentity {
    # No target creation without the host record, and none for a contract prepared for another architecture.
    $path = Join-Path $Live 'host-identity.json'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'host identity receipt absent before target creation' }
    $identity = (Read-LiveText $path) | ConvertFrom-Json
    if ([string]$identity.schema -cne 'cfdw-f1-host-identity/1') { throw 'host identity receipt schema mismatch' }
    if (-not [string]::IsNullOrEmpty([string]$identity.refusal)) { throw "host identity refused: $($identity.refusal)" }
    $token = [string]$identity.architecture
    if ([string]::IsNullOrEmpty($token) -or $token -cne [string]$contractData.architecture) {
        throw "contract architecture '$($contractData.architecture)' differs from the detected host '$token'"
    }
    return $token
}
function New-ProtectedRecord {
    # Ruling 240 (1): every receipt says whether a protected distro is declared and whether the completed-L3 guard binds.
    if ($null -eq $Protected) { return [ordered]@{ protected = 'Not recorded'; guard = 'Not recorded' } }
    if ($Protected -ceq 'none') { return [ordered]@{ protected = 'none'; guard = 'not applicable' } }
    return [ordered]@{ protected = $Protected; guard = 'binds' }
}
function Get-GpgvPath([string]$ExecPath) {
    # Ruling 240 (2): walk up from `git --exec-path` to the Git root that carries usr\bin\gpgv.exe; the refusal names
    # every path searched.
    $searched = @()
    $folder = [IO.Path]::GetFullPath($ExecPath)
    while (-not [string]::IsNullOrEmpty($folder)) {
        $candidate = [IO.Path]::Combine($folder, 'usr', 'bin', 'gpgv.exe')
        $searched += $candidate
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
        $folder = [IO.Path]::GetDirectoryName($folder)
    }
    throw "gpgv.exe absent: searched $($searched -join '; ')"
}
function Get-GpgvObservation {
    # Never throws: the host-identity receipt records an absence; Invoke-RootfsSignature then refuses with the path named.
    try {
        $execPath = [string](& git --exec-path 2>$null | Select-Object -First 1)
        if ([string]::IsNullOrEmpty($execPath)) { return [ordered]@{ path = 'absent: git --exec-path gave nothing'; version = 'Not recorded' } }
        $gpgv = Get-GpgvPath $execPath
        $version = [string](& $gpgv --version 2>$null | Select-Object -First 1)
        if ([string]::IsNullOrEmpty($version)) { $version = 'Not recorded' }
        return [ordered]@{ path = $gpgv; version = $version }
    } catch { return [ordered]@{ path = 'absent: ' + $_.Exception.Message; version = 'Not recorded' } }
}
function Convert-MsysPath([string]$Path) {
    # Git's gpgv is an MSYS program: started from a native parent it reads `D:\x` as a URL with scheme `D` ("invalid key
    # resource URL", measured 2026-10-19 on pwsh 7.6.7 with gpgv 2.4.9) and accepts the MSYS form /d/x. Reviewed inputs
    # are absolute drive paths without whitespace, so the conversion is a prefix swap.
    if ($Path -notmatch '^[A-Za-z]:\\' -or $Path -match '\s') { throw "MSYS path requires an absolute drive path without whitespace: $Path" }
    $full = [IO.Path]::GetFullPath($Path)   # the raw input is checked above: GetFullPath would admit a relative path
    return '/' + $full.Substring(0, 1).ToLowerInvariant() + '/' + $full.Substring(3).Replace('\', '/')
}
function Invoke-RootfsSignature([string]$Gpgv, [string]$Keyring, [string]$Signature, [string]$Sums, [string]$Rootfs,
                                [string]$StatusPath, [string]$ContractPath, [string]$ContractSha, [string]$ExpectedSha256) {
    # Ruling 240 (2): the Ubuntu detached signature is verified on the Windows side, before any WSL effect, by the
    # Git-for-Windows gpgv against the reviewed cloud-image keyring; the keyring's identities and the VALIDSIG signer
    # must be in the contract's allowlist, and the signed sums line for the rootfs must carry the contract's pin.
    if (-not (Test-Path -LiteralPath $Gpgv -PathType Leaf)) { throw "gpgv.exe absent: $Gpgv" }
    $gpg = '"' + (Join-Path (Split-Path -Parent $Gpgv) 'gpg.exe') + '"'   # under "Program Files": quoted for the argument line
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'contract.py'), 'keyring', '--contract', $ContractPath,
        '--expected-sha256', $ContractSha, '--keyring', $Keyring, '--kind', 'ubuntu_rootfs', '--gpg', $gpg) 'rootfs-keyring' 60 $false $false
    $status = Invoke-Child $Gpgv @('--status-fd', '1', '--keyring', (Convert-MsysPath $Keyring), $Signature, $Sums) 'rootfs-gpgv' 60 $false $false
    Copy-Item -LiteralPath $status -Destination $StatusPath -Force
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'contract.py'), 'signer', '--contract', $ContractPath,
        '--expected-sha256', $ContractSha, '--status', $StatusPath, '--kind', 'ubuntu_rootfs') 'rootfs-signer' 60 $false $false
    $name = [IO.Path]::GetFileName($Rootfs)
    $lines = @([IO.File]::ReadAllLines($Sums) | Where-Object { $_ -cmatch ('^[0-9a-f]{64} [ *]' + [regex]::Escape($name) + '$') })
    if ($lines.Count -ne 1) { throw "signed SHA256SUMS has $($lines.Count) lines for $name" }
    $signed = $lines[0].Substring(0, 64)
    $measured = (Get-FileHash -Algorithm SHA256 -LiteralPath $Rootfs).Hash.ToLowerInvariant()
    if ($signed -cne $ExpectedSha256 -or $measured -cne $ExpectedSha256) {
        throw "rootfs hash differs: signed $signed, measured $measured, contract $ExpectedSha256"
    }
    Write-Lf (Join-Path $Live 'rootfs-signature.txt') "ROOTFS-SIGNED-PASS sha256=$signed`n"
    return $signed
}
function Convert-LinuxPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if ($full -notmatch '^[A-Za-z]:\\' -or $full -match '\s') {
        throw "path must be a no-space absolute drive path: $full"
    }
    return '/mnt/' + $full.Substring(0, 1).ToLowerInvariant() + '/' + $full.Substring(3).Replace('\', '/')
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
    # The bound path, the path derived from the repository and the path the contract was prepared for must agree.
    $actual = [IO.Path]::GetFullPath($TargetPath).TrimEnd('\')
    $derived = Get-DerivedTargetPath $RepoRoot
    $recorded = ([string]$contractData.target_path).TrimEnd('\')
    if (-not $actual.Equals($derived, [StringComparison]::OrdinalIgnoreCase)) { throw "unsafe target path: $actual (derived $derived)" }
    if (-not $recorded.Equals($derived, [StringComparison]::OrdinalIgnoreCase)) {
        throw "contract target path '$recorded' differs from the derived path $derived"
    }
    return $actual
}
function Get-HostFree {
    return [int64](Get-PSDrive -Name $TargetPath.Substring(0, 1)).Free   # the target's drive, whichever letter
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
function Open-LiveEvidenceStream([string]$Path) {
    return [IO.FileStream]::new($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
}
function Read-LiveText([string]$Path) {
    $stream = Open-LiveEvidenceStream -Path $Path
    try {
        $reader = [IO.StreamReader]::new($stream, [Text.Encoding]::UTF8, $true)
        try { return $reader.ReadToEnd() }
        finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
}
function Capture-AttemptLeader([ValidateSet('activation','immediately-pre-live')][string]$Phase) {
    $stdout = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'leader_supervisor.py'), 'sample',
        '--repo', $RepoRoot, '--epoch', [string]$contractData.leader_epoch, '--run-id', $RunId, '--phase', $Phase) `
        "leader-$Phase" 15 $false $false
    $sample = (Read-LiveText $stdout) | ConvertFrom-Json
    Assert-AttemptLeader $sample $contractData.leader_epoch
    if ($sample.phase -cne $Phase -or $sample.run_id -cne $RunId) { throw 'F1-LEADER-SAMPLE-IDENTITY' }
    Write-Json (Join-Path $Live "leader-$Phase.json") $sample
    Assert-AttemptIdentity $true $Phase
}
function Assert-AttemptIdentity([bool]$Force = $true, [string]$CapturePhase = '') {
    if (-not $Force -and $Clock.Elapsed.TotalSeconds - $script:LastIdentitySampleSeconds -lt 5) { return }
    $key = 'none'   # Ruling 240 (1): with no protected distro declared there is no registration and no keepalive to identify
    if ($Protected -cne 'none') {
        $key = '{b7566a82-9cee-47ce-a246-f83632353cea}'
        $registration = Get-ItemProperty -LiteralPath ("HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss\" + $key)
        if ($registration.DistributionName -cne $Protected -or $registration.PSChildName -cne $key) {
            throw 'F1-PROTECTED-REGISTRATION-CHANGED'
        }
    }
    $processes = @(Get-CimInstance Win32_Process)
    Assert-AttemptExclusivity $processes $script:OwnedLaunchers $key
    $script:LastIdentitySampleSeconds = $Clock.Elapsed.TotalSeconds
    if ($CapturePhase) {
        if ($CapturePhase -cnotin @('activation','immediately-pre-live','finalization')) { throw 'F1-KEEPALIVE-PHASE' }
        $keepalivePids = if ($Protected -cne 'none') { @(19296,3524) } else { @() }
        $public = @($processes | Where-Object {$_.ProcessId -in $keepalivePids} | ForEach-Object {
            [ordered]@{pid=$_.ProcessId;parent_pid=$_.ParentProcessId;name=$_.Name;
                created_utc=(Get-ProcessCreationUtc $_.CreationDate);command=$_.CommandLine}
        })
        Write-Json (Join-Path $Live "keepalive-$CapturePhase.json") ([ordered]@{
            schema='cfdw-f1-keepalive-sample/1';event_id=[Guid]::NewGuid().ToString('N');
            utc=[DateTimeOffset]::UtcNow.ToString('o');run_id=$RunId;phase=$CapturePhase;protected_key=$key;processes=$public})
    }
}
function Assert-AttemptRuntime([bool]$ForceIdentity = $true) {
    if ($null -eq $script:Renewal -or $script:Renewal.HasExited) { throw 'F1-RENEW-SUPERVISOR-NOT-LIVE' }
    $events = Read-LiveText (Join-Path $Live 'leader-renew-events.jsonl')
    $last = $events.LastIndexOf("`n", [StringComparison]::Ordinal)
    if ($last -lt 0) { throw 'F1-RENEW-NO-COMPLETE-EVENT' }
    $event = $events.Substring(0,$last).Split("`n")[-1] | ConvertFrom-Json
    Assert-AttemptLeader $event $contractData.leader_epoch
    $age = Get-GuardAgeSeconds $event.utc ([DateTimeOffset]::UtcNow)
    if ($event.run_id -cne $RunId -or $event.event -cne 'renew' -or $age -lt 0 -or $age -gt 90 -or
        [double]$event.expires_in - $age -lt 200 -or
        $null -eq $script:RenewalIdentity -or $event.instance_id -cne $script:RenewalIdentity.instance_id -or
        $event.pid -ne $script:Renewal.Id) { throw 'F1-RENEW-EVENT-STALE-OR-REPLACED' }
    Assert-AttemptIdentity $ForceIdentity
}
function Register-AttemptLauncher([object]$Process) {
    if ($Process.HasExited) { return }
    $rows = @(Get-CimInstance Win32_Process)
    $parent = @($rows | Where-Object {$_.ProcessId -eq $Process.Id})
    if ($parent.Count -ne 1 -or (Get-ProcessCreationUtc $parent[0].CreationDate) -cne
        (Get-ProcessCreationUtc $Process.StartTime)) { throw 'F1-OWN-LAUNCHER-IDENTITY' }
    foreach ($row in @($parent[0]) + @($rows | Where-Object {$_.ParentProcessId -eq $Process.Id})) {
        if (-not $row.CommandLine) { throw 'F1-OWN-LAUNCHER-COMMAND-MISSING' }
        $script:OwnedLaunchers += [pscustomobject]@{pid=$row.ProcessId;
            created_utc=(Get-ProcessCreationUtc $row.CreationDate); command=$row.CommandLine}
    }
    Write-Json (Join-Path $Live 'owned-windows-launchers.json') $script:OwnedLaunchers
}
function Start-AttemptRenewal {
    foreach ($name in @('leader-renew-stop.request','leader-renew-start.json','leader-renew-events.jsonl',
        'leader-renew-exit.json','leader-renew.stdout.txt','leader-renew.stderr.txt',
        'leader-activation.json','leader-immediately-pre-live.json',
        'keepalive-activation.json','keepalive-immediately-pre-live.json','keepalive-finalization.json')) {
        if (Test-Path -LiteralPath (Join-Path $Live $name)) { throw 'F1-RENEW-PREEXISTING-ARTIFACT' }
    }
    $interpreter = (& py -3 -c 'import sys; print(sys.executable)').Trim()
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $interpreter -PathType Leaf)) { throw 'F1-RENEW-INTERPRETER' }
    $owner = Get-Process -Id $PID
    $args = @((Join-Path $PacketRoot 'leader_supervisor.py'), 'supervise', '--repo', $RepoRoot,
        '--epoch', [string]$contractData.leader_epoch, '--run-id', $RunId, '--contract', $Contract,
        '--authorization', $Authorization, '--exclusive-run-id', $ExclusiveRunId, '--output', $Live,
        '--branch', $executionBranch, '--session', $AgentSession, '--owner-pid', [string]$PID,
        '--owner-created-ticks', [string]$owner.StartTime.ToUniversalTime().ToFileTimeUtc())
    $startedUtc = [DateTimeOffset]::UtcNow
    $script:Renewal = Start-Process -FilePath $interpreter -ArgumentList $args -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $Live 'leader-renew.stdout.txt') `
        -RedirectStandardError (Join-Path $Live 'leader-renew.stderr.txt')
    $null = $script:Renewal.Handle
    $start = $Clock.Elapsed.TotalSeconds
    while ($true) {
        if ($script:Renewal.HasExited -or $Clock.Elapsed.TotalSeconds - $start -ge 45) { throw 'F1-RENEW-START-FAILED' }
        $eventPath = Join-Path $Live 'leader-renew-events.jsonl'
        if (Test-Path -LiteralPath $eventPath) {
            $raw = Read-LiveText $eventPath
            if ($raw.Contains("`n")) {
                $first = $raw.Split("`n")[0] | ConvertFrom-Json
                $marker = (Read-LiveText (Join-Path $Live 'leader-renew-start.json')) | ConvertFrom-Json
                Assert-AttemptLeader $first $contractData.leader_epoch
                if ($marker.schema -cne 'cfdw-f1-renewal-start/1' -or $marker.run_id -cne $RunId -or
                    $marker.epoch -ne $contractData.leader_epoch -or $marker.pid -ne $script:Renewal.Id -or
                    $marker.owner.pid -ne $PID -or $marker.owner.created_ticks -ne $owner.StartTime.ToUniversalTime().ToFileTimeUtc() -or
                    (ConvertTo-EvidenceUtc $marker.utc) -lt $startedUtc -or
                    (ConvertTo-EvidenceUtc $first.utc) -lt (ConvertTo-EvidenceUtc $marker.utc) -or
                    $first.instance_id -cne $marker.instance_id -or $first.pid -ne $marker.pid -or
                    $first.run_id -cne $RunId -or $first.event -cne 'renew') { throw 'F1-RENEW-START-IDENTITY' }
                $script:RenewalIdentity = $marker
                break
            }
        }
        Start-Sleep -Milliseconds 100
    }
}
function Get-AttemptLeaderEvidence {
    $names = @('leader-activation.json','leader-immediately-pre-live.json','leader-renew-start.json',
        'leader-renew-events.jsonl','leader-renew.stderr.txt','leader-renew-exit.json',
        'keepalive-activation.json','keepalive-immediately-pre-live.json','keepalive-finalization.json')
    $files = @()
    foreach ($name in $names) {
        $path = Join-Path $Live $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'F1-LEADER-EVIDENCE-MISSING' }
        $files += [ordered]@{path=$name;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
    $activation = (Read-LiveText (Join-Path $Live $names[0])) | ConvertFrom-Json
    $prelive = (Read-LiveText (Join-Path $Live $names[1])) | ConvertFrom-Json
    $start = (Read-LiveText (Join-Path $Live $names[2])) | ConvertFrom-Json
    $raw = Read-LiveText (Join-Path $Live $names[3])
    if (-not $raw.EndsWith("`n") -or -not $raw.Trim()) { throw 'F1-LEADER-EVIDENCE-INCOMPLETE' }
    $events = @($raw.TrimEnd("`n").Split("`n") | ForEach-Object {$_ | ConvertFrom-Json})
    # Ruling 235 (2): the first renewal event carries expires_in_before, the TTL the supervisor read before its CAS write;
    # it must pass the leader floor, so the activation floor is proved on the record that was renewed, not after it.
    if ($null -eq $events[0].psobject.Properties['expires_in_before']) { throw 'F1-LEADER-EVIDENCE-ACTIVATION-FLOOR-MISSING' }
    $beforeFirstRenewal = $events[0].psobject.Copy()
    $beforeFirstRenewal.expires_in = $events[0].expires_in_before
    Assert-AttemptLeader $beforeFirstRenewal $contractData.leader_epoch
    $exit = (Read-LiveText (Join-Path $Live $names[5])) | ConvertFrom-Json
    if ($null -eq $script:RenewalIdentity -or $start.instance_id -cne $script:RenewalIdentity.instance_id -or
        $start.pid -ne $script:RenewalIdentity.pid -or
        (ConvertTo-EvidenceUtc $start.utc) -ne (ConvertTo-EvidenceUtc $script:RenewalIdentity.utc) -or
        $start.owner.pid -ne $script:RenewalIdentity.owner.pid -or
        $start.owner.created_ticks -ne $script:RenewalIdentity.owner.created_ticks) { throw 'F1-LEADER-EVIDENCE-START-CHANGED' }
    foreach ($sample in @($activation,$prelive) + $events) {
        Assert-AttemptLeader $sample $contractData.leader_epoch
        if ($sample.schema -cne 'cfdw-f1-leader-sample/1' -or $sample.run_id -cne $RunId -or
            -not $sample.event_id -or (ConvertTo-EvidenceUtc $sample.utc) -lt (ConvertTo-EvidenceUtc $start.utc)) {
            throw 'F1-LEADER-EVIDENCE-IDENTITY'
        }
    }
    if ($activation.phase -cne 'activation' -or $prelive.phase -cne 'immediately-pre-live' -or
        $activation.event_id -ceq $prelive.event_id -or
        (ConvertTo-EvidenceUtc $prelive.utc) -lt (ConvertTo-EvidenceUtc $activation.utc) -or
        $start.schema -cne 'cfdw-f1-renewal-start/1' -or $start.run_id -cne $RunId -or
        $start.epoch -ne $contractData.leader_epoch -or -not $start.instance_id -or $start.pid -le 0 -or
        $exit.schema -cne 'cfdw-f1-renewal-exit/1' -or $exit.run_id -cne $RunId -or
        $exit.epoch -ne $contractData.leader_epoch -or $exit.instance_id -cne $start.instance_id -or
        $exit.pid -ne $start.pid -or ($exit.exit_code -isnot [int] -and $exit.exit_code -isnot [long]) -or
        $exit.exit_code -ne 0 -or
        ([IO.FileInfo](Join-Path $Live $names[4])).Length -ne 0) { throw 'F1-LEADER-EVIDENCE-REFUSED' }   # Ruling 235 (1): byte length, never a decoded string (a BOM-only file decoded to '')
    foreach ($event in $events) {
        if ($event.phase -cne 'renewal' -or $event.event -cne 'renew' -or
            $event.instance_id -cne $start.instance_id -or $event.pid -ne $start.pid) { throw 'F1-LEADER-EVIDENCE-RENEWAL' }
    }
    $seenIds = @()
    $previousUtc = ConvertTo-EvidenceUtc $start.utc
    foreach ($phase in @('activation','immediately-pre-live','finalization')) {
        $sample = (Read-LiveText (Join-Path $Live "keepalive-$phase.json")) | ConvertFrom-Json
        $utc = ConvertTo-EvidenceUtc $sample.utc
        if ($sample.schema -cne 'cfdw-f1-keepalive-sample/1' -or $sample.run_id -cne $RunId -or
            $sample.phase -cne $phase -or -not $sample.event_id -or $sample.event_id -cin $seenIds -or
            $utc -lt $previousUtc -or
            @($sample.processes).Count -ne $(if ($sample.protected_key -ceq 'none') { 0 } else { 2 })) { throw 'F1-KEEPALIVE-EVIDENCE-IDENTITY' }
        $rows = @($sample.processes | ForEach-Object {
            if (($_.pid -isnot [int] -and $_.pid -isnot [long]) -or
                ($_.parent_pid -isnot [int] -and $_.parent_pid -isnot [long])) { throw 'F1-KEEPALIVE-EVIDENCE-PID' }
            [pscustomobject]@{ProcessId=$_.pid;ParentProcessId=$_.parent_pid;Name=$_.name;
                CreationDate=(ConvertTo-EvidenceUtc $_.created_utc);CommandLine=$_.command}
        })
        Assert-AttemptExclusivity $rows @() $sample.protected_key
        $seenIds += $sample.event_id
        $previousUtc = $utc
    }
    return [ordered]@{status='verified';files=$files}
}
function Stop-AttemptRenewal {
    if ($null -eq $script:Renewal) { return }
    Write-Lf (Join-Path $Live 'leader-renew-stop.request') ($RunId + "`n")
    if (-not $script:Renewal.WaitForExit(20000)) {
        Stop-Process -Id $script:Renewal.Id -Force -ErrorAction Stop
        $null = $script:Renewal.WaitForExit(5000)
        Write-Json (Join-Path $Live 'leader-renew-exit.json') ([ordered]@{
            run_id=$RunId;epoch=$contractData.leader_epoch;exit_code='unknown';result='supervisor-timeout'})
        throw 'F1-RENEW-STOP-FAILED'
    }
    $exitPath = Join-Path $Live 'leader-renew-exit.json'
    if (-not (Test-Path -LiteralPath $exitPath)) {
        Write-Json $exitPath ([ordered]@{run_id=$RunId;epoch=$contractData.leader_epoch;
            exit_code=$script:Renewal.ExitCode;result='supervisor-exit-without-receipt'})
        throw 'F1-RENEW-EXIT-RECEIPT-MISSING'
    }
    $receipt = (Read-LiveText $exitPath) | ConvertFrom-Json
    if ($script:Renewal.ExitCode -ne 0 -or $receipt.exit_code -ne 0 -or $receipt.run_id -cne $RunId -or
        $receipt.epoch -ne $contractData.leader_epoch) { throw 'F1-RENEW-EXIT-FAILED' }
}
function Read-GuardState {
    $path = Join-Path $Live 'completed-guard.state.json'
    if (-not (Test-Path -LiteralPath $path)) { throw 'completed guard state missing' }
    return (Read-LiveText -Path $path) | ConvertFrom-Json
}
function Assert-AttemptRollback([string[]]$Arguments, [string]$Label) {
    if ($script:LifecyclePhase -cne 'cleanup') { throw 'F1-ROLLBACK-PHASE' }
    foreach ($binding in @(@($Contract,$script:ContractSha),@($Authorization,$script:AuthorizationSha))) {
        if ((Get-FileHash -LiteralPath $binding[0] -Algorithm SHA256).Hash.ToLowerInvariant() -cne $binding[1]) {
            throw 'F1-ROLLBACK-AUTHORITY-CHANGED'
        }
    }
    foreach ($map in @($contractData.packet_sha256,$contractData.external_sha256)) {
        foreach ($item in $map.psobject.Properties) {
            if ((Get-FileHash -LiteralPath (Join-Path $RepoRoot $item.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $item.Value) {
                throw 'F1-ROLLBACK-SOURCE-CHANGED'
            }
        }
    }
    $expected = @()
    if ($Label -ceq 'distro-list') { $expected = @('--list','--quiet') }
    elseif ($Label -cin @('target-containment-terminate','target-terminate','target-unregister')) {
        if (-not $script:CreatedTargetKey -or $null -eq $CheckpointSha) { throw 'MAC-DECISION-REQUIRED: checkpoint/GUID unbound; rollback lifecycle withheld' }
        $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey 2
        $verb = if ($Label -ceq 'target-unregister') {'--unregister'} else {'--terminate'}
        $expected = @($verb,$Target)
    }
    elseif ($Label -cin @('owned-stop','owned-check','guard-owned-stop','guard-owned-check','l3-after')) {
        $guardOwner = $Label.StartsWith('guard-',[StringComparison]::Ordinal)
        $protectedRead = $Label -ceq 'l3-after'
        $distro = if ($guardOwner -or $protectedRead) {$Protected} else {$Target}
        $cpus = if ($guardOwner -or $protectedRead) {'0'} else {'0-1'}
        if ($distro -ceq $Target) {
            if (-not $script:CreatedTargetKey) { throw 'F1-ROLLBACK-GUID-UNBOUND' }
            $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey 2
        }
        $ownerName = if ($protectedRead) {'l3-after.owner.txt'} else {"$Label.entry.txt"}
        $expected = @('--distribution',$distro,'--user','root','--exec','/usr/bin/taskset','--cpu-list',$cpus,
            '/usr/bin/nice','-n','10','/usr/bin/setsid','--wait','/bin/bash',
            (Convert-LinuxPath (Join-Path $PacketRoot 'identity.sh')),
            (Convert-LinuxPath (Join-Path $Live $ownerName)),'--')
        if ($protectedRead) {
            $expected += @('/usr/bin/python3',(Convert-LinuxPath (Join-Path $PacketRoot 'completed_guard.py')),
                'snapshot','--output',(Convert-LinuxPath (Join-Path $Live 'l3-terminal-after.json')),'--include-log-sha256')
        } else {
            $mode = if ($Label.EndsWith('-stop',[StringComparison]::Ordinal)) {'stop'} else {'check'}
            $expected += @('/bin/bash',(Convert-LinuxPath (Join-Path $PacketRoot 'owned-control.sh')),
                '--mode',$mode,'--run-root',(Convert-LinuxPath $Live),$RunId)
            if ($guardOwner) { $expected += @('--kind','guard') }
        }
    } else { throw 'F1-ROLLBACK-ACTION-REFUSED' }
    if ($Arguments.Count -ne $expected.Count) { throw 'F1-ROLLBACK-ARGUMENTS-REFUSED' }
    for ($i=0;$i -lt $expected.Count;$i++) {
        if ($Arguments[$i] -cne $expected[$i]) { throw 'F1-ROLLBACK-ARGUMENTS-REFUSED' }
    }
    Assert-AttemptIdentity $true
}
function Assert-Guard {
    if ($Protected -ceq 'none') { return 'not applicable' }   # Ruling 240 (1): the guard binds only when a protected distro is declared
    if ($null -eq $Guard -or $Guard.HasExited) { throw 'completed guard exited or was not started' }
    $state = Read-GuardState
    if ($state.state -cne 'running' -or $state.session_id -cne $RunId -or
        $state.expected_sha256 -cne $script:BeforeSha) { throw 'completed guard state/identity changed' }
    $age = Get-GuardAgeSeconds $state.last_sample_utc ([DateTimeOffset]::UtcNow)
    if ($age -lt -5 -or $age -gt 12) { throw "completed guard sample missing/stale: $age seconds" }
    $events = Join-Path $Live 'completed-guard-events.jsonl'
    if (-not (Test-Path -LiteralPath $events)) { throw 'completed guard event stream missing' }
    $raw = Read-LiveText -Path $events
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
        if ($attemptNumber -eq 2 -and $File -ieq 'wsl.exe') {
            if ($phase -ceq 'cleanup' -and $Label -cnotlike 'stage-delete-debs*') { Assert-AttemptRollback $Arguments $Label }
            else { Assert-AttemptRuntime }
            if (-not $script:FirstWslStarted) { Capture-AttemptLeader 'immediately-pre-live' }
            $script:FirstWslStarted = $true
        }
        $process = Start-Process -FilePath $File -ArgumentList $Arguments `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden
        $null = $process.Handle
        if ($attemptNumber -eq 2 -and $File -ieq 'wsl.exe') { Register-AttemptLauncher $process }
        while ($true) {
            $remaining = Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $stageStart $CapSeconds $phase
            if ($remaining -le 0) { throw "$Label exceeded min(stage cap, absolute $phase deadline)" }
            if ($process.WaitForExit([int][Math]::Max(1, [Math]::Min(200, $remaining * 1000)))) { break }
            if ($GuardRequired) { $null = Assert-Guard }
            if ($attemptNumber -eq 2 -and $File -ieq 'wsl.exe') {
                if ($phase -ceq 'cleanup' -and $Label -cnotlike 'stage-delete-debs*') { Assert-AttemptIdentity $false }
                else { Assert-AttemptRuntime $false }
            }
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
    if ($attemptNumber -eq 2 -and $Distro -ceq $Target) {
        if (-not $script:CreatedTargetKey) { throw 'F1-TARGET-GUID-UNBOUND' }
        $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey 2
    }
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
        '--b2-licences', (Convert-LinuxPath (Join-Path $RepoRoot 'docs\proof\win-gpu-b2\license-map.json')),
        '--openfoam-key', (Convert-LinuxPath $OpenFoamKeyring),
        '--nvidia-key', (Convert-LinuxPath $NvidiaKeyring),
        '--openfoam-fingerprint', $OpenFoamFingerprint, '--run-id', $RunId,
        '--contract', (Convert-LinuxPath $Contract), '--contract-sha256', $script:ContractSha,
        '--architecture', $script:Architecture)
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
        $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey $attemptNumber
    }
    try { Stop-Owned } catch {
        $original = $_.Exception.Message
        if (@(Get-TargetRegistration).Count -eq 1) {
            try {
                $script:LifecyclePhase = 'cleanup'
                if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey $attemptNumber }
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
    if ($freshAttempt) {
        if (-not $script:CreatedTargetKey) { throw 'MAC-DECISION-REQUIRED: created GUID unbound; final absence unknown' }
        $rows = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath })
        Assert-FreshRegistrations $rows $names (Test-Path -LiteralPath $TargetPath) $attemptNumber $TargetPath $Protected
        if (@($rows | Where-Object { $_.PSChildName -ieq $script:CreatedTargetKey }).Count -ne 0) {
            throw 'created target GUID survives under a name/path alias'
        }
        return
    }
    if (@($names | Where-Object { $_ -ceq $Target }).Count -ne 0 -or
        @(Get-TargetRegistration).Count -ne 0 -or (Test-Path -LiteralPath $TargetPath)) {
        throw 'target distro, registration or path remains after cleanup'
    }
    if ($Protected -cne 'none' -and @($names | Where-Object { $_ -ceq $Protected }).Count -ne 1) {
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
            $null = Get-BoundTargetKey $registration $TargetPath $script:CreatedTargetKey $attemptNumber
        }
        $basePath = [IO.Path]::GetFullPath([string]$registration[0].BasePath).TrimEnd('\')
        if (-not $basePath.Equals((Assert-TargetPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw "target registration path mismatch: $basePath"
        }
        $deleteAdmitted = $true
        if ($attemptNumber -eq 2) {
            try { Assert-AttemptRuntime }
            catch {
                $deleteAdmitted = $false
                Write-Json (Join-Path $Live 'archive-deletion-skipped.json') ([ordered]@{
                    run_id=$RunId;result='skipped-after-admission-loss';reason=$_.Exception.Message})
            }
        }
        try { if ($deleteAdmitted) { Invoke-Stage 'delete-debs' 120 $false $false } }
        finally { Stop-OwnedOrContain }
        if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey $attemptNumber }
        $null = Invoke-Child 'wsl.exe' @('--terminate', $Target) 'target-terminate' 30 $false $false
        if ($freshAttempt) { $null = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath $script:CreatedTargetKey $attemptNumber }
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
foreach ($inputPath in @($Rootfs, $UbuntuSums, $UbuntuSignature, $OpenFoamKeyring, $NvidiaKeyring, $UbuntuCloudimageKeyring, $Contract)) {
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) { throw "missing reviewed input: $inputPath" }
    $null = Convert-LinuxPath $inputPath
}
# The contract is read once: every launcher field, the hash the children check against and verify's --expected-sha256
# come from these bytes (two reads admitted a mixed state; node R, Security & Identity Architect F1).
$contractBytes = [IO.File]::ReadAllBytes($Contract)
$script:ContractSha = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($contractBytes)) -replace '-', '').ToLowerInvariant()
$contractData = ([Text.Encoding]::UTF8.GetString($contractBytes)).TrimStart([char]0xFEFF) | ConvertFrom-Json
$RunRoot = Get-InvocationRunRoot $contractData $RepoRoot $RunId $AgentSession
$Live = Join-Path $RunRoot 'live'
$CheckpointPath = Join-Path $RunRoot 'checkpoint'
$Final = Join-Path $RunRoot 'final'
$freshAttempt = $null -ne $contractData.psobject.Properties['attempt']
if ($freshAttempt) { $attemptNumber = [int]$contractData.attempt }
if (-not $freshAttempt) { throw 'a contract without attempt is retired: attempt 2 is the only live form (Ruling 237 (2); node R)' }
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
$executionBranch = $contractData.execution_branch   # from the single read above
$contractArguments = @('-3', (Join-Path $PacketRoot 'contract.py'), 'verify',
    '--contract', $Contract, '--repo', $RepoRoot, '--rootfs', $Rootfs,
    '--ubuntu-sums', $UbuntuSums, '--ubuntu-signature', $UbuntuSignature,
    '--openfoam-keyring', $OpenFoamKeyring, '--nvidia-keyring', $NvidiaKeyring,
    '--ubuntu-cloudimage-keyring', $UbuntuCloudimageKeyring,
    '--expected-sha256', $script:ContractSha,
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
$HostStartFloor, $HostReserve = Get-ContractHostFloors $contractData
$Protected = [string]$contractData.protected   # Ruling 240 (1)
if ($Protected -cnotin @('cfdw-openfoam2512', 'none')) { throw "unknown protected state: '$Protected'" }
if ($attemptNumber -eq 2) { $script:AuthorizationSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $Authorization).Hash.ToLowerInvariant() }
New-Item -ItemType Directory -Path $Live -Force | Out-Null
Copy-Item -LiteralPath $precheckOut -Destination (Join-Path $Live 'invocation-contract.stdout.txt')
Write-HostObservations (Join-Path $Live 'host-observations.json')
$null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'architecture.py'), 'host-identity',
    '--observations', (Join-Path $Live 'host-observations.json'), '--output', (Join-Path $Live 'host-identity.json'),
    '--expected-architecture', [string]$contractData.architecture) 'host-identity' 60 $false $true
$script:Architecture = Assert-HostIdentity
$linuxGuard = Convert-LinuxPath (Join-Path $PacketRoot 'completed_guard.py')
try {
    if ($attemptNumber -eq 2) {
        Start-AttemptRenewal
        Capture-AttemptLeader 'activation'
    }
    $initialHostFree = Assert-Host $true
    $names = Get-DistroNames
    if ($freshAttempt) {
        $rows = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath })
        Assert-FreshRegistrations $rows $names (Test-Path -LiteralPath $TargetPath) $attemptNumber $TargetPath $Protected
    }
    if (($Protected -cne 'none' -and @($names | Where-Object { $_ -ceq $Protected }).Count -ne 1) -or
        ($Protected -ceq 'none' -and @($names | Where-Object { $_ -ceq 'cfdw-openfoam2512' }).Count -ne 0) -or   # DR-4: a host that carries the protected distro must declare it
        @($names | Where-Object { $_ -ceq $Target }).Count -ne 0 -or
        @(Get-TargetRegistration).Count -ne 0 -or (Test-Path -LiteralPath $TargetPath)) {
        throw 'final precheck found target collision or missing protected distro'
    }
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $Rootfs).Hash.ToLowerInvariant() -cne [string]$contractData.inputs.rootfs.sha256) {
        throw 'reviewed rootfs SHA-256 changed'
    }
    $packetHashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $PacketRoot -File) {
        $packetHashes[$file.Name] = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
    }
    $protectedRecord = New-ProtectedRecord
    Write-Json (Join-Path $Live 'final-precheck.json') ([ordered]@{
        schema='cfdw-f1-r217-final-precheck/1'; utc=[DateTimeOffset]::UtcNow.ToString('o')
        target=$Target; protected=$protectedRecord.protected; guard=$protectedRecord.guard; distro_names=$names; host_free_bytes=$initialHostFree
        host_required_bytes=$HostStartFloor; host_reserve_bytes=$HostReserve; target_path=$TargetPath
        architecture=$script:Architecture
        rootfs_sha256=[string]$contractData.inputs.rootfs.sha256
        invocation_contract_sha256=$script:ContractSha
        accepted_packet_commit=$contractData.accepted_packet_commit
        leader='mac'; leader_epoch=$contractData.leader_epoch; execution_branch=$executionBranch; execution_session=$AgentSession
        b2_licenses_sha256=$contractData.inputs.b2_licenses.sha256
        openfoam_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $OpenFoamKeyring).Hash.ToLowerInvariant()
        nvidia_key_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $NvidiaKeyring).Hash.ToLowerInvariant()
        openfoam_fingerprint=$OpenFoamFingerprint; nvidia_fingerprint=$NvidiaFingerprint
        packet_sha256=$packetHashes; affinity='host lifecycle N/A'
    })
    # Ruling 240 (2): the rootfs signature is verified on the Windows side, before any WSL effect, by the gpgv the
    # host identity recorded, against the reviewed cloud-image keyring input.
    $null = Invoke-RootfsSignature $script:GpgvPath $UbuntuCloudimageKeyring $UbuntuSignature $UbuntuSums $Rootfs `
        (Join-Path $Live 'rootfs-signature.gpgv.txt') $Contract $script:ContractSha ([string]$contractData.inputs.rootfs.sha256)
    if ($Protected -cne 'none') {
        # Ruling 240 (1): the completed-L3 guard binds only when a protected distro is declared.
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
        if ($attemptNumber -eq 2) { Assert-AttemptRuntime }
        $Guard = Start-Process -FilePath 'wsl.exe' -ArgumentList $guardArguments `
            -RedirectStandardOutput (Join-Path $Live 'guard.stdout.txt') `
            -RedirectStandardError (Join-Path $Live 'guard.stderr.txt') -PassThru -WindowStyle Hidden
        $null = $Guard.Handle
        if ($attemptNumber -eq 2) { Register-AttemptLauncher $Guard }
        while (-not (Test-Path -LiteralPath $state)) {
            Assert-Deadline
            if ($Guard.HasExited -or (Get-ChildWaitSeconds $Clock.Elapsed.TotalSeconds $guardStart 20 work) -le 0) {
                throw 'completed guard did not start inside absolute startup deadline'
            }
            Start-Sleep -Milliseconds 200
        }
        $null = Assert-Guard
    }
    $script:CreationArmed = $true  # Before the first target-creation effect.
    if ($freshAttempt) {
        $freshNames = Get-DistroNames
        $freshRows = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath })
        Assert-FreshRegistrations $freshRows $freshNames (Test-Path -LiteralPath $TargetPath) $attemptNumber $TargetPath $Protected
    }
    $null = Assert-HostIdentity
    $null = Invoke-Child 'wsl.exe' @('--install', '--from-file', $Rootfs, '--name', $Target,
        '--location', $TargetPath, '--vhd-size', '8GB', '--no-launch') 'target-create' 300 $true $true
    if (@(Get-TargetRegistration).Count -ne 1) { throw 'target creation did not register exactly once' }
    $script:CreatedTargetKey = Get-BoundTargetKey @(Get-TargetRegistration) $TargetPath '' $attemptNumber
    Write-Json (Join-Path $Live 'created-target-identity.json') ([ordered]@{
        target=$Target; target_path=$TargetPath; target_key=$script:CreatedTargetKey; run_id=$RunId })
    $null = Assert-Host $false $true
    Invoke-Stage 'probe' 60 $true
    $null = Invoke-Child 'py' @('-3', (Join-Path $PacketRoot 'architecture.py'), 'check',
        '--host-identity', (Join-Path $Live 'host-identity.json'),
        '--guest-identity', (Join-Path $Live 'guest-identity.txt')) 'host-guest-architecture' 60 $true $true
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
    if ($attemptNumber -eq 2) {
        try { Assert-AttemptIdentity $true 'finalization' } catch { $CleanupErrors += "final identity: $($_.Exception.Message)" }
        try { Stop-AttemptRenewal } catch { $CleanupErrors += $_.Exception.Message }
        try { $leaderEvidence = Get-AttemptLeaderEvidence }
        catch { $leaderEvidence = 'Not recorded'; $CleanupErrors += "leader evidence: $($_.Exception.Message)" }
    }
    $protectedRecord = New-ProtectedRecord
    $cleanupReceipt = [ordered]@{
        schema='cfdw-f1-r217-cleanup/1'; run_id=$RunId; target=$Target; protected=$protectedRecord.protected; guard=$protectedRecord.guard
        initiating_error=if ($null -eq $InitiatingError) { 'Not recorded' } else { $InitiatingError }
        cleanup_errors=$CleanupErrors; checkpoint_sha=if ($null -eq $CheckpointSha) { 'Not recorded' } else { $CheckpointSha }
        elapsed_seconds=if ($null -eq $Clock) { 'Not recorded' } else { $Clock.Elapsed.TotalSeconds }
        target_absent=if ($CreationArmed -and $script:CleanupProved -and -not $CreationUncertain) { $true }
                      elseif ($CreationArmed) { 'Unknown or blocked' } else { 'Not applicable' }
        creation_uncertain=$CreationUncertain; finalization=$closing
        affinity='Linux CPU0-1 or guard CPU0, nice10; host lifecycle/Git N/A'
    }
    if ($attemptNumber -eq 2) { $cleanupReceipt.leader_evidence = $leaderEvidence }
    Write-Json (Join-Path $Live 'cleanup-receipt.json') $cleanupReceipt
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
