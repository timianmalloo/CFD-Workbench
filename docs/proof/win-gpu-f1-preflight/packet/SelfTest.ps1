$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Control.ps1')

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
$now = [DateTimeOffset]::Parse('2026-10-12T01:00:05Z')
$decoded = '{"last_sample_utc":"2026-10-12T01:00:00Z"}' | ConvertFrom-Json
Require ((Get-GuardAgeSeconds $decoded.last_sample_utc $now) -eq 5) 'UTC Z DateTime coercion changed guard age'
Require ((Get-GuardAgeSeconds '2026-10-12T01:00:00Z' $now) -eq 5) 'UTC Z string age changed'
Require ((Get-ChildWaitSeconds 2699 2696 5 work) -eq 1) 'startup was excluded from work cutoff'
Require ((Get-ChildWaitSeconds 2696 2690 5 work) -eq 0) 'delayed startup extended stage cap'
Require ((Get-ChildWaitSeconds 2701 2696 5 work) -eq 0) 'work continued after 45-minute cutoff'
Require ((Get-ChildWaitSeconds 2999.5 2990 60 checkpoint) -eq 0.5) 'checkpoint crossed 50-minute deadline'
Require ((Get-ChildWaitSeconds 3600 3590 30 cleanup) -eq 0) 'cleanup crossed host deadline'
Require ((Get-ChildWaitSeconds 3599 3590 30 cleanup) -eq 1) 'hung list lacks host deadline'
$stable = Update-TargetSettle 'False|0|False' '' 0 0
Require (-not $stable.stable) 'unseen creation was declared settled'
$stable = Update-TargetSettle 'False|0|False' $stable.key 4 $stable.stable_since
Require (-not $stable.stable) 'partial creation settled too soon'
$stable = Update-TargetSettle 'True|1|True' $stable.key 6 $stable.stable_since
Require (-not $stable.stable -and $stable.stable_since -eq 6) 'delayed creation did not reset stability timer'
$stable = Update-TargetSettle 'True|1|True' $stable.key 11 $stable.stable_since
Require $stable.stable 'stable creation did not settle'

$events = [Collections.Generic.List[string]]::new()
$failure = Invoke-Finalization `
    -StopOwned { $events.Add('stop-owned') } `
    -Checkpoint { throw 'checkpoint failure' } `
    -Cleanup { $events.Add('destructive-cleanup') } `
    -Snapshot { $events.Add('snapshot') } `
    -StopGuard { $events.Add('stop-guard') } `
    -BindGuard { $events.Add('bind-guard') }
Require ($events.Contains('stop-owned') -and $events.Contains('stop-guard') -and $events.Contains('bind-guard')) 'checkpoint failure stranded owned process or observer'
Require (-not $events.Contains('destructive-cleanup')) 'destructive cleanup preceded checkpoint'
Require ($failure.errors.Count -gt 0) 'checkpoint failure was lost'
$events.Clear()
$failure = Invoke-Finalization `
    -StopOwned { $events.Add('stop-owned') } `
    -Checkpoint { $events.Add('checkpoint') } `
    -Cleanup { $events.Add('destructive-cleanup') } `
    -Snapshot { throw 'snapshot failure' } `
    -StopGuard { $events.Add('stop-guard') } `
    -BindGuard { $events.Add('bind-guard') }
Require ($events.Contains('stop-guard') -and $events.Contains('bind-guard')) 'snapshot failure stranded observer'
Require ($failure.errors.Count -gt 0) 'snapshot failure was lost'
$events.Clear()
$failure = Invoke-Finalization `
    -StopOwned { $events.Add('stop-owned') } `
    -Checkpoint { $events.Add('checkpoint') } `
    -Cleanup { $events.Add('destructive-cleanup') } `
    -Snapshot { $events.Add('snapshot') } `
    -StopGuard { throw 'guard stop failure' } `
    -BindGuard { $events.Add('bind-guard') }
Require ($events.Contains('bind-guard') -and $failure.errors.Count -gt 0) 'guard stop failure suppressed binding/error'
Write-Output 'PowerShell packet self-test: PASS'
