$ErrorActionPreference = 'Stop'
$freshAttempt = $false # Legacy cleanup fixture; fresh-attempt controls have their own sentinel suite.
$attemptNumber = 1 # Explicit legacy context for AST-loaded production cleanup.
$script:CreatedTargetKey = ''
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
# Ruling 229: exercise the actual live-evidence reader without wrapper top-level actions.
$openLiveReaderAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Open-LiveEvidenceStream'
}, $true)
Require ($null -ne $openLiveReaderAst) 'Ruling 229 live evidence FileStream opener missing'
$openText = $openLiveReaderAst.Extent.Text
Require ($openText -match 'FileStream' -and $openText -match 'FileShare.*ReadWrite' -and
    $openText -match 'FileShare.*Delete') 'Ruling 229 reader must share ReadWrite and Delete'
$readLiveTextAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-LiveText'
}, $true)
$readStateAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-GuardState'
}, $true)
$assertGuardAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Assert-Guard'
}, $true)
Require ($null -ne $readLiveTextAst -and $null -ne $readStateAst -and $null -ne $assertGuardAst) 'Ruling 229 live reader functions missing'
Require ($readStateAst.Extent.Text -match 'Read-LiveText' -and $assertGuardAst.Extent.Text -match 'Read-LiveText') 'guard state and event reads must use the shared FileStream reader'
Invoke-Expression $openLiveReaderAst.Extent.Text
Invoke-Expression $readLiveTextAst.Extent.Text
Invoke-Expression $readStateAst.Extent.Text
Invoke-Expression $assertGuardAst.Extent.Text
$readerFixture = Join-Path ([IO.Path]::GetTempPath()) ('f1-reader-sharing-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $readerFixture
try {
    $Live = $readerFixture
    $script:BeforeSha = 'reader-fixture-sha'
    $RunId = 'reader-fixture'
    $Guard = [pscustomobject]@{ HasExited=$false }
    $statePath = Join-Path $Live 'completed-guard.state.json'
    $eventsPath = Join-Path $Live 'completed-guard-events.jsonl'
    $stateJson = [ordered]@{ state='running'; session_id=$RunId; expected_sha256=$script:BeforeSha
        last_sample_utc=[DateTimeOffset]::UtcNow.ToString('o'); completed_samples=7 } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText($statePath, $stateJson, [Text.UTF8Encoding]::new($true))
    $eventJson = [ordered]@{ verdict='continue'; session_id=$RunId; expected_sha256=$script:BeforeSha } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText($eventsPath, ($eventJson + "`r`n" + '{"partial":true}'), [Text.UTF8Encoding]::new($false))
    Require ((Read-GuardState).session_id -ceq $RunId) 'Ruling 229 BOM state read changed'
    Require ((Assert-Guard) -eq 7) 'Ruling 229 complete event selection changed'
    $reader = Open-LiveEvidenceStream -Path $eventsPath
    try {
        $writer = [IO.FileStream]::new($eventsPath, [IO.FileMode]::Open, [IO.FileAccess]::Write,
            ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        try {
            $writer.Seek(0, [IO.SeekOrigin]::End) | Out-Null
            $tail = [Text.Encoding]::UTF8.GetBytes('{"concurrent":true}')
            $writer.Write($tail, 0, $tail.Length)
            $writer.Flush()
        } finally { $writer.Dispose() }
        [IO.File]::Delete($eventsPath)
        Require (-not (Test-Path -LiteralPath $eventsPath)) 'Ruling 229 reader blocked concurrent delete'
    } finally { $reader.Dispose() }
} finally { [IO.Directory]::Delete($readerFixture, $true) }
foreach ($file in @('Control.ps1', 'SelfTest.ps1')) {
    $null = [Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $PSScriptRoot $file), [ref]$tokens, [ref]$parseErrors)
    Require ($parseErrors.Count -eq 0) "$file parse failed"
}
# Live checkpoint failure: exercise the actual Save-Checkpoint body through the
# finalizer's dynamic scope. Offline fast ring; child/Git/writers are inert mocks.
$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
$RunRoot = Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1'
$expectedCheckpointPath = Join-Path $RunRoot 'checkpoint'
$checkpointPathAssignment = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -cin @('$Checkpoint', '$CheckpointPath')
}, $true)
Require ($null -ne $checkpointPathAssignment) 'checkpoint path assignment missing'
Invoke-Expression $checkpointPathAssignment.Extent.Text
Require ((Get-Variable -Name $checkpointPathAssignment.Left.VariablePath.UserPath).Value -ceq $expectedCheckpointPath) 'wrapper checkpoint path is not canonical'
$Live = Join-Path $RunRoot 'live'
$PacketRoot = $PSScriptRoot
$Clock = [Diagnostics.Stopwatch]::StartNew()
$RunId = 'offline-checkpoint-fixture'
$LastStage = 'simulate'
$InitiatingError = 'planted simulation failure'
$checkpointState = @{ freeze=$null; commit=$null; binding=$null }
$shaFixture = [IO.Path]::GetTempFileName()
[IO.File]::WriteAllText($shaFixture, ('a' * 40))
function Write-Json {}
function Write-Lf {}
function Invoke-Child([string]$Executable, [object[]]$Arguments) {
    if ($Executable -eq 'py') {
        $frozenIndex = [Array]::IndexOf($Arguments, '--frozen')
        $directory = $Arguments[$frozenIndex + 1]
        Require ($directory -is [string] -and $directory -ceq $expectedCheckpointPath) 'checkpoint freeze/bind received a shadowed path'
        if ($Arguments -contains 'freeze') { $checkpointState.freeze = $directory }
        elseif ($Arguments -contains 'verify') { $checkpointState.binding = $directory }
        else { throw 'unexpected checkpoint Python command' }
    } elseif ($Executable -eq 'git') {
        Require ($Arguments[1] -ceq $RepoRoot) 'checkpoint Git command left the repository'
        switch ($Arguments[2]) {
            'add' {
                Require ($Arguments[5] -ceq 'docs/proof/win-gpu-f1-preflight/run-1/checkpoint') 'checkpoint Git path conversion differs'
                $checkpointState.commit = $Arguments[5]
            }
            'commit' { Require ($null -ne $checkpointState.commit) 'checkpoint commit preceded scoped add' }
            'rev-parse' { return $shaFixture }
            default { throw 'unexpected checkpoint Git command' }
        }
    } else { throw 'unexpected checkpoint child command' }
}
try {
    foreach ($functionName in @('Save-Checkpoint', 'Invoke-GitCommit')) {
        $checkpointAst = $wrapperAst.Find({ param($node)
            $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $functionName
        }, $true)
        Require ($null -ne $checkpointAst) "$functionName body missing"
        Invoke-Expression $checkpointAst.Extent.Text
    }
    $closing = Invoke-Finalization -StopOwned {} -Checkpoint { Save-Checkpoint } `
        -Cleanup {} -Snapshot {} -StopGuard {} -BindGuard {}
    Require $closing.checkpoint_proved ('canonical checkpoint callback failed: ' + ($closing.errors -join '; '))
    Require ($checkpointState.freeze -ceq $expectedCheckpointPath -and $checkpointState.binding -ceq $expectedCheckpointPath) 'canonical checkpoint path did not reach manifest freeze and binding'
    Require ($CheckpointSha -ceq ('a' * 40)) 'checkpoint SHA was not retained'
} finally { Remove-Item -LiteralPath $shaFixture -Force }
$cleanupAst = $wrapperAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-Cleanup'
}, $true)
$cleanupBody = $cleanupAst.Body.GetScriptBlock()
$CreationArmed = $true
$CheckpointSha = 'offline-fixture'
$TargetPath = Get-DerivedTargetPath 'X:\fixture\CFD-Workbench'
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
