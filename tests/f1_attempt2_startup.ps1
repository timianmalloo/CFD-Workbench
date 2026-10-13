# Offline-fast actual supervisor startup; all launcher, clock and file boundaries mocked.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$packet=Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$errors=$null;$tokens=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'),[ref]$tokens,[ref]$errors)
$node=$ast.Find({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Start-AttemptRenewal'},$true)
Invoke-Expression $node.Extent.Text
$Live='C:\fixture\live';$RepoRoot='C:\fixture';$PacketRoot=$packet;$Contract='contract';$Authorization='auth'
$RunId='run';$ExclusiveRunId='run';$executionBranch='branch';$AgentSession='session'
$contractData=[pscustomobject]@{leader_epoch=19};$Clock=[pscustomobject]@{Elapsed=[pscustomobject]@{TotalSeconds=0.0}}
$script:files=@{};$script:starts=0;$script:wrongPid=$false;$script:created=[DateTime]::Now
function Test-Path {param($LiteralPath,$PathType) return $LiteralPath -eq 'C:\fixture\python.exe' -or $script:files.ContainsKey([IO.Path]::GetFileName($LiteralPath))}
function py {$global:LASTEXITCODE=0;return 'C:\fixture\python.exe'}
function Get-Process {param($Id)return [pscustomobject]@{StartTime=$script:created}}
function Read-LiveText {param($Path)return $script:files[[IO.Path]::GetFileName($Path)]}
function Start-Sleep {param($Milliseconds)$Clock.Elapsed.TotalSeconds+=$Milliseconds/1000.0}
function Start-Process {param($FilePath,$ArgumentList,$WindowStyle,[switch]$PassThru,$RedirectStandardOutput,$RedirectStandardError)
 $script:starts++
 $marker=@{schema='cfdw-f1-renewal-start/1';run_id=$RunId;epoch=19;pid=42;instance_id='fresh';utc=[DateTimeOffset]::UtcNow.ToString('o');owner=@{pid=$PID;created_ticks=$script:created.ToUniversalTime().ToFileTimeUtc()}}
 $event=@{leader='mac';state='live';epoch=19;expires_in=300;oid=('a'*40);run_id=$RunId;event='renew';instance_id='fresh';pid=42;utc=[DateTimeOffset]::UtcNow.ToString('o')}
 if($script:wrongPid){$event.pid=43}
 $script:files['leader-renew-start.json']=($marker|ConvertTo-Json -Depth 5)
 $script:files['leader-renew-events.jsonl']=($event|ConvertTo-Json -Compress)+"`n"
 return [pscustomobject]@{Id=42;Handle=1;HasExited=$false}
}
foreach($name in @('leader-renew-stop.request','leader-renew-start.json','leader-renew-events.jsonl','leader-renew-exit.json','leader-renew.stdout.txt','leader-renew.stderr.txt')) {
 $script:files=@{};$script:files[$name]='stale';$failed=$false
 try{Start-AttemptRenewal}catch{$failed=$true}
 if(-not $failed -or $script:starts){throw 'preexisting artifact reached launcher'}
}
$script:files=@{};Start-AttemptRenewal
if($script:RenewalIdentity.pid -ne 42 -or $script:starts -ne 1){throw 'fresh startup was not bound'}
$script:files=@{};$script:wrongPid=$true;$failed=$false
try{Start-AttemptRenewal}catch{$failed=$true}
if(-not $failed){throw 'another process event admitted startup'}
Write-Output 'attempt2 startup PASS preexisting control/evidence refused before launch; first event bound to new process/run/epoch/owner'
