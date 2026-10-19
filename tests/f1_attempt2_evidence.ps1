# Offline-fast actual receipt evidence validation. Files only; no runtime operations.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$packet=Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$errors=$null;$tokens=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Open-LiveEvidenceStream','Read-LiveText','Get-AttemptLeaderEvidence','Assert-AttemptIdentity')) {
    $node=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
    if($null -eq $node){throw "missing evidence validator: $name"};Invoke-Expression $node.Extent.Text
}
function WriteFixture($name,$value) {[IO.File]::WriteAllText((Join-Path $Live $name),(($value|ConvertTo-Json -Depth 10 -Compress)+"`n"),[Text.UTF8Encoding]::new($false))}
function Refuses([scriptblock]$work) {$failed=$false;try{&$work|Out-Null}catch{$failed=$true};if(-not $failed){throw 'missing/invalid evidence accepted'}}
$Live=Join-Path ([IO.Path]::GetTempPath()) ('f1-a2-evidence-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Live|Out-Null
$RunId='f1-attempt2-20261013';$contractData=[pscustomobject]@{leader_epoch=19}
try {
    $base=@{schema='cfdw-f1-leader-sample/1';leader='mac';state='live';epoch=19;expires_in=300;oid=('a'*40);run_id=$RunId;utc='2026-10-13T01:00:01Z';event_id='activation'}
    $activation=$base.Clone();$activation.phase='activation'
    $pre=$base.Clone();$pre.phase='immediately-pre-live';$pre.event_id='pre'
    $renew=$base.Clone();$renew.phase='renewal';$renew.event='renew';$renew.instance_id='instance';$renew.pid=42;$renew.expires_in_before=200.0
    $start=@{schema='cfdw-f1-renewal-start/1';instance_id='instance';pid=42;run_id=$RunId;epoch=19;utc='2026-10-13T01:00:00Z';owner=@{pid=41;created_ticks=123}}
    $script:RenewalIdentity=($start|ConvertTo-Json -Depth 5)|ConvertFrom-Json
    $exit=@{schema='cfdw-f1-renewal-exit/1';run_id=$RunId;epoch=19;exit_code=0;instance_id='instance';pid=42}
    $fixtures=@{'leader-activation.json'=$activation;'leader-immediately-pre-live.json'=$pre;'leader-renew-start.json'=$start;'leader-renew-events.jsonl'=$renew;'leader-renew-exit.json'=$exit}
    $keepalive=@{schema='cfdw-f1-keepalive-sample/1';run_id=$RunId;protected_key='{b7566a82-9cee-47ce-a246-f83632353cea}';utc='2026-10-13T01:00:02Z';processes=@(
        @{pid=19296;parent_pid=15528;name='wsl.exe';created_utc='2026-10-08T17:02:23.1776050Z';command='"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity'},
        @{pid=3524;parent_pid=19296;name='wsl.exe';created_utc='2026-10-08T17:02:23.2168250Z';command='"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity'})}
    foreach($phase in @('activation','immediately-pre-live','finalization')) {
        $row=$keepalive.Clone();$row.phase=$phase;$row.event_id=$phase
        $fixtures["keepalive-$phase.json"]=$row
    }
    foreach($name in $fixtures.Keys){WriteFixture $name $fixtures[$name]}
    $stderrPath=Join-Path $Live 'leader-renew.stderr.txt'
    [IO.File]::WriteAllBytes($stderrPath,[byte[]]@())   # zero bytes, never WriteAllText(''): a default encoding may emit a preamble on 5.1
    $result=Get-AttemptLeaderEvidence
    if($result.status -cne 'verified' -or $result.files.Count -ne 9){throw 'actual evidence not bound'}
    # JSON runtimes may retain an offset-bearing string or materialize DateTime.
    $script:RenewalIdentity=$start.Clone();$null=Get-AttemptLeaderEvidence
    # Ruling 235 (1): the renewal stderr is byte-empty or the evidence refuses; a decoded length accepted a BOM-only file.
    [IO.File]::WriteAllBytes($stderrPath,[byte[]]@(0xEF,0xBB,0xBF));Refuses {Get-AttemptLeaderEvidence}   # RenewStderr_BomOnly_Refused
    [IO.File]::WriteAllText($stderrPath," `n");Refuses {Get-AttemptLeaderEvidence}                         # RenewStderr_WhitespaceOnly_Refused
    [IO.File]::WriteAllBytes($stderrPath,[byte[]]@());$null=Get-AttemptLeaderEvidence                      # RenewStderr_Empty_Accepted
    # Ruling 235 (2): the first renewal event carries expires_in_before, the TTL read before the CAS write, and it must
    # pass the leader floor (FirstRenewEvent_ExpiresInBefore199_Refused: null, 199.999, a string, a boolean, NaN,
    # infinity and a missing field all refuse).
    foreach($ttl in @($null,199.999,'300',$true,[double]::NaN,[double]::PositiveInfinity)) {
        $bad=$renew.Clone();$bad.expires_in_before=$ttl
        WriteFixture 'leader-renew-events.jsonl' $bad;Refuses {Get-AttemptLeaderEvidence}
    }
    $bad=$renew.Clone();$bad.Remove('expires_in_before');WriteFixture 'leader-renew-events.jsonl' $bad;Refuses {Get-AttemptLeaderEvidence}
    WriteFixture 'leader-renew-events.jsonl' $renew
    $null=Get-AttemptLeaderEvidence
    foreach($name in @($fixtures.Keys)+@('leader-renew.stderr.txt')) {
        $path=Join-Path $Live $name;$raw=[IO.File]::ReadAllBytes($path)
        Remove-Item -LiteralPath $path
        Refuses {Get-AttemptLeaderEvidence}
        [IO.File]::WriteAllBytes($path,$raw)
    }
    foreach($field in @('schema','run_id','phase','epoch','expires_in','event_id','utc')) {
        $bad=$pre.Clone();$bad[$field]=if($field -eq 'epoch'){8}elseif($field -eq 'expires_in'){199}elseif($field -eq 'event_id'){'activation'}elseif($field -eq 'utc'){'2026-10-13T01:00:01'}else{'changed'}
        WriteFixture 'leader-immediately-pre-live.json' $bad
        Refuses {Get-AttemptLeaderEvidence}
    }
    WriteFixture 'leader-immediately-pre-live.json' $pre
    foreach($field in @('phase','event_id','run_id','protected_key')) {
        $bad=$fixtures['keepalive-finalization.json'].Clone();$bad[$field]=if($field -eq 'event_id'){'activation'}else{'changed'}
        WriteFixture 'keepalive-finalization.json' $bad;Refuses {Get-AttemptLeaderEvidence}
    }
    WriteFixture 'keepalive-finalization.json' $fixtures['keepalive-finalization.json']
    foreach($field in @('pid','parent_pid','name','created_utc','command')) {
        $bad=($fixtures['keepalive-finalization.json']|ConvertTo-Json -Depth 10)|ConvertFrom-Json
        $bad.processes[1].$field=if($field -cin @('pid','parent_pid')){999}elseif($field -eq 'created_utc'){'2026-10-08T17:02:24.2168250Z'}else{'changed'}
        WriteFixture 'keepalive-finalization.json' $bad;Refuses {Get-AttemptLeaderEvidence}
    }
    WriteFixture 'keepalive-finalization.json' $fixtures['keepalive-finalization.json']
    $exit.exit_code=5;WriteFixture 'leader-renew-exit.json' $exit;Refuses {Get-AttemptLeaderEvidence}
    # Execute the actual public capture with private unrelated process fields present.
    $Protected='cfdw-openfoam2512';$Clock=[Diagnostics.Stopwatch]::StartNew();$script:LastIdentitySampleSeconds=-100.0;$script:OwnedLaunchers=@()
    function Get-ItemProperty {return [pscustomobject]@{DistributionName=$Protected;PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}'}}
    $script:fixtureProcesses=@($keepalive.processes|ForEach-Object{[pscustomobject]@{ProcessId=$_.pid;ParentProcessId=$_.parent_pid;Name=$_.name;CreationDate=(ConvertTo-EvidenceUtc $_.created_utc);CommandLine=$_.command;PrivateField='private-host-path'}})
    function Get-CimInstance {return $script:fixtureProcesses}
    function Write-Json {param($Path,$Value)WriteFixture ([IO.Path]::GetFileName($Path)) $Value}
    $ids=@()
    foreach($phase in @('activation','immediately-pre-live','finalization')) {
        Assert-AttemptIdentity $true $phase
        $raw=Read-LiveText (Join-Path $Live "keepalive-$phase.json");$record=$raw|ConvertFrom-Json
        if($raw.Contains('private-host-path') -or $record.phase -cne $phase -or $record.processes.Count -ne 2 -or $record.event_id -cin $ids){throw 'actual keepalive capture lost phase/identity/redaction'}
        $ids+=$record.event_id
    }
} finally {
    $resolved=[IO.Path]::GetFullPath($Live)
    if(-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath())) -or -not ([IO.Path]::GetFileName($resolved)).StartsWith('f1-a2-evidence-')){throw 'unsafe fixture cleanup'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Output 'attempt2 actual receipt evidence PASS missing/invalid samples refuse; hashes bound'
