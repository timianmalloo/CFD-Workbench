# Ring: Windows runner preparation, no product checks. Ceiling: 60 s.
param([Parameter(Mandatory=$true)][string]$PythonPath)
$ErrorActionPreference = 'Stop'
$env:CFD_WRI_PYTHON=$PythonPath
. (Join-Path $PSScriptRoot 'windows-runner.ps1')
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('cfd-runner-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($scratch)
function Reject([string]$Label, [scriptblock]$Probe, [string]$Message) {
    $caught = $null
    try { $null = & $Probe } catch { $caught = $_.Exception.Message }
    if (-not $caught -or $caught -notlike "*$Message*") { throw "RED NOT OBSERVED $Label" }
    "RED $Label rejected=$Message"
}
try {
    Reject 'd wrong SDK path' { Assert-WriToolchain -Repo $scratch -DotnetPath (Join-Path $scratch 'wrong-dotnet.exe') -PythonPath $PythonPath } 'WRI-TOOLCHAIN'
    Reject 'd wrong Python path' { Assert-WriToolchain -Repo $scratch -PythonPath (Join-Path $scratch 'wrong-python.exe') } 'WRI-TOOLCHAIN'
    Reject 'b Settings closed' { Assert-WriSettingsState ([pscustomobject]@{Frame=$false}) } 'WRI-PREFLIGHT'
    Reject 'b Settings collapsed' { Assert-WriSettingsState ([pscustomobject]@{Frame=$true;Selected=$true;Expanded=$false;Main=$true;Visible=$true;Scale='150% (Recommended)'}) } 'WRI-PREFLIGHT'
    Reject 'e lost exit' { Assert-WriNumericExit $null } 'WRI-EXIT'
    Reject 'e nonnumeric exit' { Assert-WriNumericExit 'not-recorded' } 'WRI-EXIT'
    $probe = [pscustomobject]@{WaitArgument=-1}
    $probe | Add-Member ScriptMethod WaitForExit { param([int]$TimeoutMs); $this.WaitArgument=$TimeoutMs; return $true }
    if (-not (Wait-WriDeadline $probe 900000 899750) -or $probe.WaitArgument -ne 250) { throw 'WRI-DEADLINE remaining budget incorrect' }
    $probe.WaitArgument=-1
    if ((Wait-WriDeadline $probe 900000 900000) -or $probe.WaitArgument -ne -1) { throw 'WRI-DEADLINE expired budget waited' }
    'GREEN c stopwatch boundary=899750:250,900000:expired'
    $repo = Join-Path $scratch 'fixture'
    [void][IO.Directory]::CreateDirectory((Join-Path $repo 'src'))
    $fixture = Join-Path $repo 'src/fixture.txt'
    [IO.File]::WriteAllText($fixture, 'baseline')
    & git -C $repo init -q
    & git -C $repo add -A
    & git -C $repo -c user.name=fixture -c user.email=fixture@example.invalid commit -q -m fixture
    if ($LASTEXITCODE -ne 0) { throw 'fixture git initialization failed' }
    Assert-WriSourceClean $repo
    $before = Get-WriSourceFingerprint $repo
    [IO.File]::AppendAllText($fixture, 'x')
    Reject 'f one-byte tracked edit' { Assert-WriSourceClean $repo } 'WRI-SOURCE'
    Reject 'f after-child mutation' { Assert-WriSourceUnchanged $repo $before } 'WRI-SOURCE'
    [IO.File]::WriteAllText($fixture, 'baseline')
    [IO.File]::WriteAllText((Join-Path $repo 'src/untracked.txt'), 'x')
    Reject 'f untracked source' { Assert-WriSourceClean $repo } 'WRI-SOURCE'
    Remove-Item -LiteralPath (Join-Path $repo 'src/untracked.txt')
    Assert-WriSourceClean $repo
    Assert-WriSourceUnchanged $repo $before
    'GREEN f baseline=clean final=clean same_bytes=true'
    $engine = (Get-Process -Id $PID).Path
    foreach ($expected in @(0,3)) {
        $stub = Join-Path $scratch "exit-$expected.ps1"
        [IO.File]::WriteAllText($stub, "exit $expected")
        $result = Invoke-WriChild -Repo $repo -Exe $engine -Arguments @('-NoProfile','-File',$stub) -Clock ([Diagnostics.Stopwatch]::StartNew()) -CeilingMs 60000
        if ($result.ExitCode -isnot [int] -or $result.ExitCode -ne $expected -or $result.TimedOut) { throw "WRI-EXIT stub $expected failed" }
        "GREEN e stub=$expected numeric_exit=$($result.ExitCode) retained_handle=$($result.RetainedHandle)"
    }
    $absent = Invoke-WriChild -Repo $repo -Exe $engine -Arguments @('-NoProfile','-File',(Join-Path $PSScriptRoot 'windows-settings-preflight.ps1'),'-Action','Preflight','-SelfTestAbsentFrame') -Clock ([Diagnostics.Stopwatch]::StartNew()) -CeilingMs 60000
    if ($absent.ExitCode -ne 1 -or $absent.Stderr -notmatch 'exact Settings frame absent') { throw 'actual UIA preflight did not reject absent-frame provider' }
    'RED b actual preflight absent-frame provider rejected before any scale action'
    foreach ($injectTermination in @($false,$true)) {
        $assignmentEvents=[Collections.Generic.List[string]]::new()
        $assignmentClock=[Diagnostics.Stopwatch]::StartNew()
        $expectedCleanup=if ($injectTermination) { 'cleanup=unresolved-request-failed fallback=terminated-complete' } else { 'cleanup=terminated-complete' }
        Reject "assignment failure termination_injected=$injectTermination" {
            Invoke-WriChild -Repo $repo -Exe $engine -Arguments @('-NoProfile','-File',$stub) -Clock $assignmentClock -CeilingMs 60000 -ChildCeilingMs 5000 -InjectAssignmentFailure -InjectTerminationFailure:$injectTermination -Events $assignmentEvents
        } $expectedCleanup
        if ($assignmentClock.ElapsedMilliseconds -ge 5000 -or $assignmentEvents.Count -lt 1 -or $assignmentEvents[0] -notlike 'unassigned-pid:*') { throw 'assignment cleanup proof incomplete or unbounded' }
        $unassignedPid=[int]($assignmentEvents[0] -split ':')[1]
        $unassigned=Get-Process -Id $unassignedPid -ErrorAction SilentlyContinue
        if ($unassigned -and -not $unassigned.HasExited) { throw 'assignment cleanup returned with unassigned process alive' }
        "GREEN assignment-failure injected_termination=$injectTermination stable_handle_wait=true observed_dead=true total_ms=$($assignmentClock.ElapsedMilliseconds) limit_ms=5000"
    }
    $descendantScript=Join-Path $scratch 'descendant.ps1'
    [IO.File]::WriteAllText($descendantScript, 'Start-Sleep -Seconds 60')
    $rootScript=Join-Path $scratch 'root-with-descendant.ps1'
    [IO.File]::WriteAllText($rootScript, @'
param($Engine,$Child,$PidFile)
$info=[Diagnostics.ProcessStartInfo]::new()
$info.FileName=$Engine; $info.UseShellExecute=$false; $info.CreateNoWindow=$true
$info.ArgumentList.Add('-NoProfile'); $info.ArgumentList.Add('-File'); $info.ArgumentList.Add($Child)
$descendant=[Diagnostics.Process]::Start($info)
[IO.File]::WriteAllText($PidFile,[string]$descendant.Id)
[Console]::WriteLine($env:USERPROFILE)
[Console]::WriteLine([Environment]::MachineName)
[Console]::WriteLine('S-1-5-21-' + '1-2-3-4')
Start-Sleep -Milliseconds 150
exit 0
'@)
    $pidFile=Join-Path $scratch 'descendant.pid'
    $descendantArguments=@('-NoProfile','-File',$rootScript,$engine,$descendantScript,$pidFile)
    $boundedClock=[Diagnostics.Stopwatch]::StartNew()
    $inherited=Invoke-WriChild -Repo $repo -Exe $engine -Arguments $descendantArguments -Clock $boundedClock -CeilingMs 60000 -ChildCeilingMs 5000
    if ($inherited.ExitCode -ne 0 -or $inherited.RootExitDescendants -lt 1 -or $inherited.Cleanup -ne 'terminated-complete' -or $inherited.ResidualStatus -ne 'job-active-zero-verified' -or $boundedClock.ElapsedMilliseconds -ge 5000) { throw 'descendant inherited-handle cleanup was not bounded/complete' }
    if ($inherited.Stdout.Contains($env:USERPROFILE) -or $inherited.Stdout.Contains([Environment]::MachineName) -or $inherited.Stdout.Contains(('S-1-5-21-' + '1-2-3-4')) -or $inherited.PhnStatus -ne 'PASS' -or $inherited.SubstitutionBinding.Count -ne 3) { throw 'PHN derivative protection failed' }
    "GREEN inherited-output root_exit=0 descendants_at_root_exit=$($inherited.RootExitDescendants) cleanup=$($inherited.Cleanup) bounded_ms=$($boundedClock.ElapsedMilliseconds) limit_ms=5000 PHN=PASS substitution_count=3 raw_hash_bound=true"
    $failureClock=[Diagnostics.Stopwatch]::StartNew()
    Reject 'cleanup injected termination failure' { Invoke-WriChild -Repo $repo -Exe $engine -Arguments $descendantArguments -Clock $failureClock -CeilingMs 60000 -ChildCeilingMs 5000 -InjectTerminationFailure } 'WRI-CLEANUP'
    if ($failureClock.ElapsedMilliseconds -ge 5000) { throw 'injected cleanup failure exceeded envelope' }
    $descendantPid=[int][IO.File]::ReadAllText($pidFile)
    $remainingProcess=Get-Process -Id $descendantPid -ErrorAction SilentlyContinue
    if ($remainingProcess -and -not $remainingProcess.WaitForExit(1000)) { throw 'job-close fallback left descendant alive' }
    'GREEN injected_failure completion_claim=withheld fallback_job_close=observed_dead bounded=true'
    $timeoutClock=[Diagnostics.Stopwatch]::StartNew()
    Reject 'timeout cleanup' { Invoke-WriChild -Repo $repo -Exe $engine -Arguments @('-NoProfile','-File',$descendantScript) -Clock $timeoutClock -CeilingMs 60000 -ChildCeilingMs 2000 } 'WRI-DEADLINE'
    if ($timeoutClock.ElapsedMilliseconds -ge 2000) { throw 'timeout cleanup exceeded end-to-end envelope' }
    "GREEN timeout-cleanup total_ms=$($timeoutClock.ElapsedMilliseconds) limit_ms=2000 raw_publication=false"
    $startupClock=[Diagnostics.Stopwatch]::StartNew()
    Reject 'delayed startup' { Invoke-WriChild -Repo $repo -Exe $engine -Arguments @('-NoProfile','-File',$descendantScript) -Clock $startupClock -CeilingMs 60000 -ChildCeilingMs 1000 -InjectStartupDelayMs 2000 } 'WRI-ENVELOPE'
    if ($startupClock.ElapsedMilliseconds -ge 1000) { throw 'startup rejection exceeded end-to-end envelope' }
    "GREEN delayed-startup total_ms=$($startupClock.ElapsedMilliseconds) limit_ms=1000 child_launch=false"
    $shutdownZero=Join-Path $scratch 'shutdown-zero.ps1'
    $shutdownThree=Join-Path $scratch 'shutdown-three.ps1'
    [IO.File]::WriteAllText($shutdownZero,'exit 0')
    [IO.File]::WriteAllText($shutdownThree,'exit 3')
    $shutdownFixture=[pscustomobject]@{Dotnet=$engine}
    $lifecycleEvents=[Collections.Generic.List[string]]::new()
    $lifecycle=Invoke-WriChild -Repo $repo -Exe $engine -Arguments $descendantArguments -Clock ([Diagnostics.Stopwatch]::StartNew()) -CeilingMs 60000 -ChildCeilingMs 10000 -Mode BuildVerifier -Toolchain $shutdownFixture -SelfTestShutdownArguments @('-NoProfile','-File',$shutdownZero) -Events $lifecycleEvents
    $expectedEvents=@('target-root-exit:0','shutdown-start','shutdown-exit:0','target-job-query','target-job-terminate','target-job-close')
    if (($lifecycleEvents -join ',') -cne ($expectedEvents -join ',') -or $lifecycle.BuildServerShutdownExit -isnot [int] -or $lifecycle.BuildServerShutdownExit -ne 0) { throw 'R181 target lifecycle ordering incorrect' }
    "GREEN R181 lifecycle_order=$($lifecycleEvents -join '>') numeric_shutdown=0 residual_evidence=job-accounting-only"
    $failedLifecycleEvents=[Collections.Generic.List[string]]::new()
    Reject 'R181 shutdown failure' {
        Invoke-WriChild -Repo $repo -Exe $engine -Arguments $descendantArguments -Clock ([Diagnostics.Stopwatch]::StartNew()) -CeilingMs 60000 -ChildCeilingMs 10000 -Mode BuildVerifier -Toolchain $shutdownFixture -SelfTestShutdownArguments @('-NoProfile','-File',$shutdownThree) -Events $failedLifecycleEvents
    } 'numeric shutdown failed; target residual query withheld'
    $expectedFailureEvents=@('target-root-exit:0','shutdown-start','shutdown-exit:3','target-job-close')
    if (($failedLifecycleEvents -join ',') -cne ($expectedFailureEvents -join ',')) { throw 'R181 failed shutdown queried/terminated residuals before fail-closed disposition' }
    $failedDescendant=Get-Process -Id ([int][IO.File]::ReadAllText($pidFile)) -ErrorAction SilentlyContinue
    if ($failedDescendant -and -not $failedDescendant.WaitForExit(1000)) { throw 'failed shutdown job-close fallback left fixture descendant alive' }
    'GREEN R181 shutdown_failure=3 query=withheld job_close_fallback=observed_dead completion_claim=withheld'
    Assert-WriSettingsState ([pscustomobject]@{Frame=$true;Selected=$true;Expanded=$true;Main=$true;Visible=$true;Scale='150% (Recommended)'})
    'GREEN b read-only state fixture accepted'
    $toolchain = Assert-WriToolchain -Repo $repo -PythonPath $PythonPath
    "GREEN d sdk=$($toolchain.Sdk) sdk_path=%USERPROFILE%\.dotnet\dotnet.exe python=$($toolchain.PythonVersion)"
    $shutdown = Stop-WriBuildServers -Repo $repo -Toolchain $toolchain -Clock ([Diagnostics.Stopwatch]::StartNew()) -CeilingMs 60000
    "GREEN R181 build_server_shutdown_numeric_exit=$($shutdown.ExitCode) residual_sampling=not-run"
    'SELFTEST PASS no scale change; no product contract execution'
} finally {
    $resolved = [IO.Path]::GetFullPath($scratch)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) { throw 'scratch outside temporary directory' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
