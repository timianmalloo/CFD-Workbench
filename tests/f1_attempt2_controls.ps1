# Offline-fast pure production controls. No process enumeration, WSL or registry access.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$clock=[Diagnostics.Stopwatch]::StartNew()
$packet=Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
function Require($value,$why) { if (-not $value) { throw $why } }
function Refuses([scriptblock]$work) { $failed=$false; try { & $work | Out-Null } catch {$failed=$true}; Require $failed 'invalid attempt-2 boundary passed' }
$instant=[DateTimeOffset]::Parse('2026-10-13T20:12:10.9234567Z')
foreach($stamp in @($instant,$instant.UtcDateTime,$instant.LocalDateTime,'2026-10-13T20:12:10.9234567Z')) {
    Require ((ConvertTo-EvidenceUtc $stamp).Ticks -eq $instant.Ticks) 'timestamp normalization lost subsecond ticks'
}
Refuses {ConvertTo-EvidenceUtc '2026-10-13T20:12:10.9234567'}
Refuses {ConvertTo-EvidenceUtc ([DateTime]::SpecifyKind($instant.DateTime,[DateTimeKind]::Unspecified))}
$data=[pscustomobject]@{attempt=2;leader='mac';leader_epoch=19;execution_branch='win/gpu-f1-attempt2';session='pc-f1-attempt2';run_id='f1-attempt2-20261013';run_root='docs/proof/win-gpu-f1-preflight/attempt-2'}
Require ((Get-InvocationRunRoot $data 'C:\Projects\fixture' $data.run_id $data.session) -like '*attempt-2') 'attempt-2 rejected'
$path='C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$target=[pscustomobject]@{DistributionName='cfdw-petsc-cuda132-f1';PSChildName='{11111111-2222-3333-4444-555555555555}';Version=2;BasePath=$path}
$protected=[pscustomobject]@{DistributionName='cfdw-openfoam2512';PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}';Version=2;BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512')} # machine-path-ok: fixture only, value not emitted.
foreach ($key in @('{e958c4d5-ae78-4b50-a634-7102f334d3aa}','{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}')) {
    $bad=$target.psobject.Copy(); $bad.PSChildName=$key
    Refuses { Get-BoundTargetKey @($bad) $path '' 2 }
    $bad.DistributionName='renamed';$bad.BasePath='C:\Projects\moved'
    Refuses { Assert-FreshRegistrations @($protected,$bad) @('cfdw-openfoam2512') $false 2 }
}
$lease=[pscustomobject]@{leader='mac';state='live';epoch=19;expires_in=200.0;oid=('a'*40)}
Assert-AttemptLeader $lease 19
foreach ($ttl in @(199.999,[double]::NaN,[double]::PositiveInfinity,'300',$true,$null)) {
    $bad=$lease.psobject.Copy();$bad.expires_in=$ttl;Refuses {Assert-AttemptLeader $bad 19}
}
$launcher=[pscustomobject]@{ProcessId=19296;ParentProcessId=15528;Name='wsl.exe';CreationDate=[DateTimeOffset]::Parse('2026-10-08T10:02:23.1776050-07:00');CommandLine='"C:\WINDOWS\System32\wsl.exe" -d cfdw-openfoam2512 -u root -- /usr/bin/sleep infinity'}
$child=[pscustomobject]@{ProcessId=3524;ParentProcessId=19296;Name='wsl.exe';CreationDate=[DateTimeOffset]::Parse('2026-10-08T10:02:23.2168250-07:00');CommandLine=$launcher.CommandLine}
Assert-AttemptExclusivity @($launcher,$child) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'
foreach ($field in @('Name','CreationDate','CommandLine')) {
    $bad=$child.psobject.Copy();$bad.$field=if($field -eq 'CreationDate'){$child.CreationDate.AddSeconds(1)}else{'changed'}
    Refuses {Assert-AttemptExclusivity @($launcher,$bad) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
}
foreach ($field in @('ProcessId','CreationDate','CommandLine')) {
    $bad=$launcher.psobject.Copy();$bad.$field=if($field -eq 'ProcessId'){123}else{'changed'}
    Refuses {Assert-AttemptExclusivity @($bad,$child) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
}
Refuses {Assert-AttemptExclusivity @($launcher) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$bad=$child.psobject.Copy();$bad.ParentProcessId=55
Refuses {Assert-AttemptExclusivity @($launcher,$bad) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$other=$launcher.psobject.Copy();$other.ProcessId=55
Refuses {Assert-AttemptExclusivity @($launcher,$child,$other) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$own=[pscustomobject]@{pid=55;created_utc=$other.CreationDate.ToUniversalTime().ToString('o');command=$other.CommandLine}
Assert-AttemptExclusivity @($launcher,$child,$other) @($own) '{b7566a82-9cee-47ce-a246-f83632353cea}'
$other.CommandLine='wsl.exe -d wrong'
Refuses {Assert-AttemptExclusivity @($launcher,$child,$other) @($own) '{b7566a82-9cee-47ce-a246-f83632353cea}'}
Write-Output ('attempt2 controls PASS wsl_launches=0 elapsed_seconds={0:F3}' -f $clock.Elapsed.TotalSeconds)
