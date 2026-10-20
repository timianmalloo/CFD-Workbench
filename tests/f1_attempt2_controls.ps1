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
# Ruling 243 (2): attempt 3 under the same scheme, every name derived from the number; a mixed binding refuses.
$data3=$data.psobject.Copy();$data3.attempt=3;$data3.execution_branch='win/gpu-f1-attempt3';$data3.session='pc-f1-attempt3';$data3.run_id='f1-attempt3-20261020';$data3.run_root='docs/proof/win-gpu-f1-preflight/attempt-3'
Require ((Get-InvocationRunRoot $data3 'C:\Projects\fixture' $data3.run_id $data3.session) -like '*attempt-3') 'attempt-3 rejected (Ruling 243 (2))'
foreach ($field in @('execution_branch','session','run_id','run_root')) {
    $bad=$data3.psobject.Copy();$bad.$field=$data.$field
    Refuses {Get-InvocationRunRoot $bad 'C:\Projects\fixture' $bad.run_id $bad.session}
}
$bad=$data3.psobject.Copy();$bad.attempt=7
Refuses {Get-InvocationRunRoot $bad 'C:\Projects\fixture' $bad.run_id $bad.session}
# Ruling 245 (4): the new cap's numbers 4, 5 and 6 are fresh attempts with derived names.
foreach ($n in @(4,5,6)) {
    $dataN=$data.psobject.Copy();$dataN.attempt=$n;$dataN.execution_branch="win/gpu-f1-attempt$n";$dataN.session="pc-f1-attempt$n";$dataN.run_id="f1-attempt$n-20261021";$dataN.run_root="docs/proof/win-gpu-f1-preflight/attempt-$n"
    Require ((Get-InvocationRunRoot $dataN 'C:\Projects\fixture' $dataN.run_id $dataN.session) -like "*attempt-$n") "attempt-$n rejected (Ruling 245 (4))"
}
$path='C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$target=[pscustomobject]@{DistributionName='cfdw-petsc-cuda132-f1';PSChildName='{11111111-2222-3333-4444-555555555555}';Version=2;BasePath=$path}
$protected=[pscustomobject]@{DistributionName='cfdw-openfoam2512';PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}';Version=2;BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512')} # machine-path-ok: fixture only, value not emitted.
foreach ($key in @('{e958c4d5-ae78-4b50-a634-7102f334d3aa}','{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}')) {
    $bad=$target.psobject.Copy(); $bad.PSChildName=$key
    Refuses { Get-BoundTargetKey @($bad) $path '' 2 }
    $bad.DistributionName='renamed';$bad.BasePath='C:\Projects\moved'
    Refuses { Assert-FreshRegistrations @($protected,$bad) @('cfdw-openfoam2512') $false 2 $path }
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
foreach ($field in @('ProcessId','ParentProcessId','CreationDate','CommandLine')) {
    $bad=$launcher.psobject.Copy();$bad.$field=if($field -cin @('ProcessId','ParentProcessId')){123}else{'changed'}
    Refuses {Assert-AttemptExclusivity @($bad,$child) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
}
Refuses {Assert-AttemptExclusivity @($launcher) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$bad=$child.psobject.Copy();$bad.ParentProcessId=55
Refuses {Assert-AttemptExclusivity @($launcher,$bad) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$other=$launcher.psobject.Copy();$other.ProcessId=55
Refuses {Assert-AttemptExclusivity @($launcher,$child,$other) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'}
$message=''; try { Assert-AttemptExclusivity @($launcher,$child,$other) @() '{b7566a82-9cee-47ce-a246-f83632353cea}' } catch { $message=$_.Exception.Message }
Require ($message -ceq 'F1-EXCLUSIVITY-REFUSED: additional or changed WSL actor: wsl.exe pid 55') ('the exclusivity refusal names no actor: ' + $message)
$own=[pscustomobject]@{pid=55;created_utc=$other.CreationDate.ToUniversalTime().ToString('o');command=$other.CommandLine}
Assert-AttemptExclusivity @($launcher,$child,$other) @($own) '{b7566a82-9cee-47ce-a246-f83632353cea}'
$other.CommandLine='wsl.exe -d wrong'
Refuses {Assert-AttemptExclusivity @($launcher,$child,$other) @($own) '{b7566a82-9cee-47ce-a246-f83632353cea}'}
# Ruling 243 (1): bash.exe is WSL's legacy launcher only under System32; Git's MSYS bash is not a WSL actor unless its
# command line invokes wsl; a bash.exe whose path is unknown stays an actor (fail closed).
$gitBash=[pscustomobject]@{ProcessId=77;ParentProcessId=1;Name='bash.exe';ExecutablePath='C:\Program Files\Git\usr\bin\bash.exe';CreationDate=$launcher.CreationDate;CommandLine='"C:\Program Files\Git\usr\bin\bash.exe" --login -i'}
Assert-AttemptExclusivity @($launcher,$child,$gitBash) @() '{b7566a82-9cee-47ce-a246-f83632353cea}'
Assert-AttemptExclusivity @($gitBash) @() 'none'
$wslBash=$gitBash.psobject.Copy();$wslBash.ExecutablePath=(Join-Path $env:SystemRoot 'System32\bash.exe');$wslBash.CommandLine='bash.exe'
Refuses {Assert-AttemptExclusivity @($wslBash) @() 'none'}
$wslBashCased=$wslBash.psobject.Copy();$wslBashCased.ExecutablePath=$wslBash.ExecutablePath.ToUpperInvariant()
Refuses {Assert-AttemptExclusivity @($wslBashCased) @() 'none'}
$dotted=$wslBash.psobject.Copy();$dotted.ExecutablePath=(Join-Path $env:SystemRoot 'System32\..\System32\.\bash.exe')
Refuses {Assert-AttemptExclusivity @($dotted) @() 'none'}
$prefixed=$wslBash.psobject.Copy();$prefixed.ExecutablePath='\\?\' + $wslBash.ExecutablePath
Refuses {Assert-AttemptExclusivity @($prefixed) @() 'none'}
$gitDotted=$gitBash.psobject.Copy();$gitDotted.ExecutablePath='C:\Program Files\Git\bin\..\usr\bin\bash.exe'
Assert-AttemptExclusivity @($gitDotted) @() 'none'
$noPath=[pscustomobject]@{ProcessId=78;ParentProcessId=1;Name='bash.exe';CreationDate=$launcher.CreationDate;CommandLine='bash.exe'}
Refuses {Assert-AttemptExclusivity @($noPath) @() 'none'}
$emptyPath=$gitBash.psobject.Copy();$emptyPath.ExecutablePath=$null
Refuses {Assert-AttemptExclusivity @($emptyPath) @() 'none'}
$invoking=$gitBash.psobject.Copy();$invoking.CommandLine='"C:\Program Files\Git\usr\bin\bash.exe" ./probe.sh wsl -d cfdw-petsc-cuda132-f1'
Refuses {Assert-AttemptExclusivity @($invoking) @() 'none'}
# Ruling 245 (3): the own-child identity across the CIM (microsecond) and .NET (100 ns) timestamps compares at the
# coarser precision; a real microsecond or larger difference still refuses; an offset-less timestamp refuses.
$t=[DateTimeOffset]::Parse('2026-10-20T16:47:29.7226630+00:00')
Require (Test-OwnLauncherCreation $t ($t.AddTicks(8))) 'own-child identity refused an 8-tick difference (TIMESTAMP-IDENTITY-ACROSS-PRECISIONS)'
Require (Test-OwnLauncherCreation $t.LocalDateTime ($t.AddTicks(9)).LocalDateTime) 'own-child identity refused local DateTimes 9 ticks apart'
Require (-not (Test-OwnLauncherCreation $t ($t.AddTicks(10)))) 'own-child identity accepted a one-microsecond difference'
Require (-not (Test-OwnLauncherCreation $t ($t.AddSeconds(1)))) 'own-child identity accepted a one-second difference'
Refuses {Test-OwnLauncherCreation $t ([DateTime]::SpecifyKind($t.DateTime,[DateTimeKind]::Unspecified))}
# Ruling 245 (3): the run root's capture folders keep their bytes, asserted by git check-attr before the first WSL effect.
$repoFixture=Join-Path ([IO.Path]::GetTempPath()) ('cfdw-attr-' + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $repoFixture | Out-Null
try {
    $null=& git -C $repoFixture init -q
    [IO.File]::WriteAllText((Join-Path $repoFixture '.gitattributes'), "* text=auto eol=lf`n")
    Refuses {Assert-CaptureAttributes $repoFixture 'docs/proof/win-gpu-f1-preflight/attempt-9'}
    [IO.File]::AppendAllText((Join-Path $repoFixture '.gitattributes'), "docs/proof/win-gpu-f1-preflight/attempt-9/checkpoint/** -text`ndocs/proof/win-gpu-f1-preflight/attempt-9/final/** -text`n")
    Refuses {Assert-CaptureAttributes $repoFixture 'docs/proof/win-gpu-f1-preflight/attempt-9'}
    [IO.File]::AppendAllText((Join-Path $repoFixture '.gitattributes'), "docs/proof/win-gpu-f1-preflight/attempt-9/live/** -text`n")
    Assert-CaptureAttributes $repoFixture 'docs/proof/win-gpu-f1-preflight/attempt-9'
} finally { Remove-Item -LiteralPath $repoFixture -Recurse -Force -ErrorAction SilentlyContinue }
$errors=$null;$tokens=$null
$launcherAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'),[ref]$tokens,[ref]$errors)
$attrCall=@($launcherAst.FindAll({param($n)$n -is [Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq 'Assert-CaptureAttributes'},$true))
$hostObs=@($launcherAst.FindAll({param($n)$n -is [Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq 'Write-HostObservations'},$true))
Require ($attrCall.Count -eq 1 -and $hostObs.Count -ge 1 -and $attrCall[0].Extent.StartOffset -lt $hostObs[0].Extent.StartOffset) 'the launcher does not assert the capture attributes before its first evidence write'
Write-Output ('attempt2 controls PASS wsl_launches=0 elapsed_seconds={0:F3}' -f $clock.Elapsed.TotalSeconds)
