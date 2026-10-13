# Offline-fast rollback/admission composition and explicit identity sampling cadence.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$packet=Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$errors=$null;$tokens=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Assert-AttemptRollback','Assert-AttemptIdentity','Invoke-Child','Invoke-Cleanup')) {
 $node=$ast.Find({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
 if($null -eq $node){throw "missing rollback function: $name"};Invoke-Expression $node.Extent.Text
}
function Refuses([scriptblock]$work){$failed=$false;try{&$work|Out-Null}catch{$failed=$true};if(-not $failed){throw 'unsafe rollback admitted'}}
$Clock=[pscustomobject]@{Elapsed=[pscustomobject]@{TotalSeconds=0.0}}
$script:LastIdentitySampleSeconds=-100.0;$script:scans=0;$script:OwnedLaunchers=@()
$Protected='cfdw-openfoam2512';$Target='cfdw-petsc-cuda132-f1';$TargetPath='C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
function Get-ItemProperty {return [pscustomobject]@{DistributionName=$Protected;PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}'}}
function Get-CimInstance {$script:scans++;return @()}
function Assert-AttemptExclusivity {}
for($i=0;$i -lt 50;$i++){$Clock.Elapsed.TotalSeconds=$i/5;Assert-AttemptIdentity $false}
if($script:scans -ne 2){throw 'identity cadence exceeds one enumeration per five seconds'}
Assert-AttemptIdentity $true;if($script:scans -ne 3){throw 'effect did not force fresh identity'}
$script:LifecyclePhase='cleanup';$attemptNumber=2;$script:FirstWslStarted=$true
$script:CreatedTargetKey='{11111111-2222-3333-4444-555555555555}'
$script:ContractSha='a'*64;$script:AuthorizationSha='a'*64
$CheckpointSha='a'*40
$Contract='contract';$Authorization='authorization';$RepoRoot='C:\fixture';$PacketRoot=$packet;$Live='C:\fixture\live';$RunId='run'
$contractData=[pscustomobject]@{packet_sha256=[pscustomobject]@{packet=('a'*64)};external_sha256=[pscustomobject]@{external=('a'*64)}}
function Get-FileHash {return [pscustomobject]@{Hash=('a'*64)}}
function Get-TargetRegistration {return [pscustomobject]@{DistributionName=$Target;PSChildName=$script:CreatedTargetKey;Version=2;BasePath=$TargetPath}}
function Convert-LinuxPath {param($Path) return '/mnt/c/'+$Path.Replace('\','/')}
function Assert-Deadline {}
$script:effects=@();$script:loss='lease';$script:identityLoss=$false
function Assert-AttemptRuntime {throw $script:loss}
function Assert-AttemptIdentity {if($script:identityLoss){throw 'exclusivity lost'}}
function Register-AttemptLauncher {}
function Start-Process {param($FilePath,$ArgumentList,$RedirectStandardOutput,$RedirectStandardError,[switch]$PassThru,$WindowStyle)
 $script:effects+=,$ArgumentList;$p=[pscustomobject]@{Handle=1;HasExited=$true;ExitCode=0};$p|Add-Member ScriptMethod WaitForExit {param($ms)return $true};return $p}
foreach($loss in @('supervisor-exited','lease-expired')) {
 $script:loss=$loss
 foreach($label in @('target-containment-terminate','target-terminate','target-unregister','distro-list')) {
  $argv=if($label -eq 'distro-list'){@('--list','--quiet')}elseif($label -eq 'target-unregister'){@('--unregister',$Target)}else{@('--terminate',$Target)}
  $null=Invoke-Child 'wsl.exe' $argv $label 10 $false $false
 }
 Refuses {Invoke-Child 'wsl.exe' @('--install','wrong') 'target-create' 10 $false $true}
 Refuses {Invoke-Child 'wsl.exe' @('--terminate',$Protected) 'target-terminate' 10 $false $false}
 Refuses {Invoke-Child 'wsl.exe' @('--unregister',$Target,'extra') 'target-unregister' 10 $false $false}
}
$script:identityLoss=$true;$before=$script:effects.Count
Refuses {Invoke-Child 'wsl.exe' @('--terminate',$Target) 'target-terminate' 10 $false $false}
if($script:effects.Count -ne $before){throw 'lost exclusivity crossed effect boundary'}
$script:identityLoss=$false;$CheckpointSha=$null
Refuses {Invoke-Child 'wsl.exe' @('--terminate',$Target) 'target-terminate' 10 $false $false}
Refuses {Invoke-Child 'wsl.exe' @('--unregister',$Target) 'target-unregister' 10 $false $false}
# Run the real finalization sequencer and cleanup with only system boundaries replaced.
$CreationArmed=$true;$freshAttempt=$true
function Wait-TargetSettled {return [pscustomobject]@{registration_count=1;listed=$true}}
function Assert-TargetPath {return $TargetPath}
function Test-Path {return $false}
function Write-Json {param($Path,$Value) $script:skipRecorded=$Value.result -ceq 'skipped-after-admission-loss'}
function Invoke-Stage {throw 'forward/archive stage reached after admission loss'}
function Assert-FinalAbsence {$script:absenceCalled=$true}
function Invoke-OwnedRollback($Label,$Distro,$GuardOwner) {
 $cpus=if($GuardOwner){'0'}else{'0-1'}
 $mode=if($Label.EndsWith('-stop')){'stop'}else{'check'}
 $argv=@('--distribution',$Distro,'--user','root','--exec','/usr/bin/taskset','--cpu-list',$cpus,'/usr/bin/nice','-n','10',
 '/usr/bin/setsid','--wait','/bin/bash',(Convert-LinuxPath (Join-Path $PacketRoot 'identity.sh')),
 (Convert-LinuxPath (Join-Path $Live "$Label.entry.txt")),'--','/bin/bash',(Convert-LinuxPath (Join-Path $PacketRoot 'owned-control.sh')),
 '--mode',$mode,'--run-root',(Convert-LinuxPath $Live),$RunId)
 if($GuardOwner){$argv+=@('--kind','guard')}
 $null=Invoke-Child 'wsl.exe' $argv $Label 10 $false $false
}
function Stop-OwnedOrContain {Invoke-OwnedRollback 'owned-stop' $Target $false;Invoke-OwnedRollback 'owned-check' $Target $false}
foreach($loss in @('supervisor','lease','exclusivity')) {
 foreach($bound in @($true,$false)) {
  $script:identityLoss=$loss -ceq 'exclusivity';$script:effects=@();$script:skipRecorded=$false;$script:absenceCalled=$false
  $CheckpointSha=if($bound){'a'*40}else{$null}
  $closing=Invoke-Finalization -StopOwned {Stop-OwnedOrContain} -Checkpoint {if(-not $bound){throw 'checkpoint unbound'}} `
   -Cleanup {Invoke-Cleanup} -Snapshot {Assert-AttemptIdentity $true} `
   -StopGuard {Invoke-OwnedRollback 'guard-owned-stop' $Protected $true;Invoke-OwnedRollback 'guard-owned-check' $Protected $true} -BindGuard {}
  $destructive=@($script:effects|Where-Object{$_[0] -cin @('--terminate','--unregister')})
  if($bound -and -not $script:identityLoss) {
   if($destructive.Count -ne 2 -or -not $closing.cleanup_proved -or -not $closing.guard_stopped -or -not $script:skipRecorded -or -not $script:absenceCalled){throw 'admission-loss finalization did not complete permitted rollback'}
  } elseif($destructive.Count -ne 0 -or $closing.cleanup_proved){throw 'unbound or identity-lost finalization claimed cleanup'}
  if($script:identityLoss -and $closing.snapshot_proved){throw 'identity loss claimed protected snapshot'}
 }
}
Write-Output 'attempt2 rollback PASS lease/supervisor loss does not block exact rollback; identity loss refuses; cadence 2 scans/10s plus forced launch'
