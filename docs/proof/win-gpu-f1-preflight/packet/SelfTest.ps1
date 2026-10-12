$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$testClock = [Diagnostics.Stopwatch]::StartNew()
. (Join-Path $PSScriptRoot 'Control.ps1')

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
# Ruling 220 P1: a post-launch cleanup failure must invalidate the old stop receipt.
# Offline fast ring; no process launch. Cost is measured with the complete suite.
foreach ($failurePoint in @('startup', 'acknowledgement', 'timeout', 'none')) {
    $events = [Collections.Generic.List[string]]::new()
    $state = @{ active=$false; stops=0 }
    $closing = Invoke-Finalization `
        -StopOwned { $events.Add('stop-owned'); $state.active=$false; $state.stops++ } `
        -Checkpoint { $events.Add('checkpoint') } `
        -Cleanup {
            $events.Add('delete-stage-launch'); $state.active=$true
            if ($failurePoint -ne 'none') { throw "delete stage $failurePoint failure" }
        } `
        -Snapshot { $events.Add('snapshot') } `
        -StopGuard { $events.Add('stop-guard') } `
        -BindGuard { $events.Add('bind-guard') }
    Require ($state.stops -eq 2 -and -not $state.active) "P1 $failurePoint cleanup launch escaped final reap"
    Require ($events[$events.Count - 1] -eq 'stop-owned') 'P1 owned work can launch after final stop'
    Require $closing.owned_stopped 'P1 final stop result not recorded'
    Require ($closing.cleanup_proved -eq ($failurePoint -eq 'none')) 'P1 cleanup failure lost'
}
# Exercise the actual cleanup function body, without wrapper top-level execution.
$tokens = $null
$parseErrors = $null
$wrapperAst = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$parseErrors)
Require ($parseErrors.Count -eq 0) 'wrapper parse failed'
foreach ($file in @('Control.ps1', 'SelfTest.ps1')) {
    $null = [Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $PSScriptRoot $file), [ref]$tokens, [ref]$parseErrors)
    Require ($parseErrors.Count -eq 0) "$file parse failed"
}
$cleanupAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-Cleanup'
}, $true)
$cleanupBody = $cleanupAst.Body.GetScriptBlock()
$CreationArmed = $true
$CheckpointSha = 'offline-fixture'
$TargetPath = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$stageEvents = [Collections.Generic.List[string]]::new()
function Wait-TargetSettled { return $null }
function Get-TargetRegistration { return [pscustomobject]@{ BasePath=$TargetPath } }
function Assert-TargetPath { return $TargetPath }
function Invoke-Stage { $stageEvents.Add('delete-stage-launch'); throw 'planted post-launch timeout' }
function Stop-OwnedOrContain { $stageEvents.Add('fresh-stop-reap') }
function Invoke-Child { throw 'unexpected cleanup child launch' }
try { & $cleanupBody; throw 'planted cleanup timeout passed' }
catch { Require ($_.Exception.Message -eq 'planted post-launch timeout') 'cleanup error was replaced' }
Require (($stageEvents -join ',') -eq 'delete-stage-launch,fresh-stop-reap') 'actual cleanup stage bypassed finally reap'
$events = [Collections.Generic.List[string]]::new()
$state = @{ stops=0 }
$closing = Invoke-Finalization `
    -StopOwned { $state.stops++; if ($state.stops -eq 2) { throw 'planted final reap failure' } } `
    -Checkpoint {} -Cleanup {} -Snapshot {} -StopGuard {} -BindGuard {}
Require (-not $closing.owned_stopped) 'stale first-stop result survived a failed final reap'
Require ($closing.errors -contains 'final-owned-stop: planted final reap failure') 'final reap failure was discarded'
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
Write-Output ('PowerShell packet self-test: PASS ring=offline-fast elapsed_seconds={0:F3}' -f $testClock.Elapsed.TotalSeconds)
