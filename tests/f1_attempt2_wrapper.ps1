# Offline-fast actual wrapper functions with all live boundaries replaced by sentinels.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$packet=Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$errors=$null;$tokens=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'wrapper syntax'}
foreach($name in @('Capture-AttemptLeader','Assert-FinalAbsence','Invoke-Cleanup','Stop-OwnedOrContain','Invoke-WslLinux')) {
    $node=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
    if($null -eq $node){throw "missing production function: $name"}; Invoke-Expression $node.Extent.Text
}
$attemptNumber=2;$freshAttempt=$true;$CreationArmed=$true;$CheckpointSha='a'*40
$Target='cfdw-petsc-cuda132-f1';$Protected='cfdw-openfoam2512';$TargetPath='C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$script:CreatedTargetKey='{11111111-2222-3333-4444-555555555555}'
$script:launches=0;$script:samples=@();$script:written=@{}
$RunId='f1-attempt2-20261013';$Live='C:\Projects\fixture';$RepoRoot=$Live;$PacketRoot=$packet
$contractData=[pscustomobject]@{leader_epoch=19}
$script:leaderPhase=''
function Invoke-Child {
    param($File,$Arguments,$Label)
    if($File -ne 'py'){ $script:launches++;throw 'WSL-SENTINEL' }
    $at=[Array]::IndexOf($Arguments,'--phase');$script:leaderPhase=$Arguments[$at+1]
    $script:samples+= $script:leaderPhase
    return 'fake-sample-path'
}
function Read-LiveText {return (@{leader='mac';state='live';epoch=19;expires_in=200;oid=('a'*40);phase=$script:leaderPhase;run_id=$RunId;event_id=$script:leaderPhase;utc='2026-10-13T00:00:00Z'}|ConvertTo-Json)}
function Write-Json {param($Path,$Value) $script:written[$Path]=$Value}
function Assert-AttemptIdentity {param($Force,$Phase) if($Phase -cnotin @('activation','immediately-pre-live')){throw 'keepalive phase not captured'}}
Capture-AttemptLeader 'activation'; Capture-AttemptLeader 'immediately-pre-live'
if(($script:samples -join ',') -ne 'activation,immediately-pre-live' -or $script:written.Count -ne 2){throw 'two distinct samples not captured'}
$protectedRow=[pscustomobject]@{DistributionName=$Protected;PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}';Version=2;BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512');PSPath='protected'} # machine-path-ok: fixture only.
function Get-DistroNames {return @($Protected)}
function Get-ChildItem {return $script:rows}
function Get-ItemProperty {param($LiteralPath) return @($script:rows|Where-Object{$_.PSPath -eq $LiteralPath})}
function Test-Path {return $false}
function Get-TargetRegistration {return @($script:target)}
function Wait-TargetSettled {return [pscustomobject]@{registration_count=1;listed=$true}}
function Stop-Owned {throw 'must reject retired identity before owned-stop'}
function Invoke-Stage {$script:launches++;throw 'stage-sentinel'}
function Convert-LinuxPath {return '/mnt/c/fixture'}
function Assert-TargetPath {return $TargetPath}
foreach($key in @('{e958c4d5-ae78-4b50-a634-7102f334d3aa}','{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}')) {
    $script:target=[pscustomobject]@{DistributionName=$Target;PSChildName=$key;Version=2;BasePath=$TargetPath;PSPath='target'}
    $script:CreatedTargetKey=$key
    foreach($call in @({Invoke-Cleanup},{Stop-OwnedOrContain},{Invoke-WslLinux $Target '0-1' 'stage' 'owner' @('noop') 10})) {
        $failed=$false;try{&$call}catch{$failed=$true};if(-not $failed -or $script:launches){throw 'retired identity reached lifecycle boundary'}
    }
    $alias=$script:target.psobject.Copy();$alias.DistributionName='renamed';$alias.BasePath='C:\Projects\moved'
    $script:rows=@($protectedRow,$alias)
    $failed=$false;try{Assert-FinalAbsence}catch{$failed=$true};if(-not $failed){throw 'retired GUID alias accepted as absent'}
}
# Execute actual child-launch composition, replacing only system boundaries.
$node=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Invoke-Child'},$true)
Invoke-Expression $node.Extent.Text
$Clock=[Diagnostics.Stopwatch]::StartNew();$script:LifecyclePhase='work';$script:FirstWslStarted=$false
$script:order=[Collections.Generic.List[string]]::new();$script:blockRuntime=$false;$script:blockSample=$false
function Assert-Deadline {}
function Assert-AttemptRuntime {$script:order.Add('runtime');if($script:blockRuntime){throw 'runtime-refusal'}}
function Capture-AttemptLeader {param($Phase) $script:order.Add($Phase);if($script:blockSample){throw 'sample-refusal'}}
function Register-AttemptLauncher {param($Process) $script:order.Add('registered')}
function Start-Process {
    param($FilePath,$ArgumentList,$RedirectStandardOutput,$RedirectStandardError,[switch]$PassThru,$WindowStyle)
    $script:order.Add('start')
    $p=[pscustomobject]@{Handle=1;HasExited=$true;ExitCode=0}
    $p|Add-Member ScriptMethod WaitForExit {param($ms) return $true};return $p
}
$null=Invoke-Child 'wsl.exe' @('--list') 'fixture' 10 $false $false
if(($script:order -join ',') -ne 'runtime,immediately-pre-live,start,registered'){throw 'pre-first-WSL admission order wrong'}
$script:order.Clear();$null=Invoke-Child 'wsl.exe' @('--list') 'fixture' 10 $false $false
if(($script:order -join ',') -ne 'runtime,start,registered'){throw 'sample reused or unnecessary duplicate first sample'}
foreach($mode in @('runtime','sample')) {
    $script:order.Clear();$script:FirstWslStarted=$false
    $script:blockRuntime=$mode -eq 'runtime';$script:blockSample=$mode -eq 'sample'
    $refused=$false;try{$null=Invoke-Child 'wsl.exe' @('--list') 'fixture' 10 $false $false}catch{$refused=$true}
    if(-not $refused -or $script:order.Contains('start')){throw 'invalid live admission crossed launch boundary'}
}
Write-Output 'attempt2 wrapper PASS distinct samples; retired lifecycle/absence refusal; real pre-first-WSL ordering; live_system_calls=0'
