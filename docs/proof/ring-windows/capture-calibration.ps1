param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 3)]
    [int] $Run,
    [switch] $SelfTest
)

$ErrorActionPreference = 'Stop'
$outerCeilingMs = 900000
$captureClock = [System.Diagnostics.Stopwatch]::StartNew()
$started = [DateTimeOffset]::UtcNow

function Get-RemainingMilliseconds([long] $CeilingMs, [long] $ElapsedMs) {
    return [math]::Max(0, $CeilingMs - $ElapsedMs)
}

function Wait-ProcessWithinDeadline($Process, [long] $CeilingMs, [long] $ElapsedMs) {
    $remainingMs = Get-RemainingMilliseconds -CeilingMs $CeilingMs -ElapsedMs $ElapsedMs
    if ($remainingMs -le 0) { return $false }
    return [bool]$Process.WaitForExit([int]$remainingMs)
}

function Stop-ProcessTreeWithinDeadline($Process, [long] $CeilingMs, [long] $ElapsedMs) {
    $Process.Kill($true)
    return Wait-ProcessWithinDeadline -Process $Process -CeilingMs $CeilingMs -ElapsedMs $ElapsedMs
}

function Get-EnvelopeMilliseconds([DateTimeOffset] $Start, [DateTimeOffset] $End) {
    return [math]::Round(($End - $Start).TotalMilliseconds, 3)
}

function Test-EnvelopeWithinCeiling([double] $EnvelopeMs, [long] $CeilingMs) {
    return $EnvelopeMs -le $CeilingMs
}

if ($SelfTest) {
    $probe = [pscustomobject]@{ WaitArgument = -1; KillCount = 0; KilledTree = $false }
    $probe | Add-Member -MemberType ScriptMethod -Name WaitForExit -Value {
        param([int] $TimeoutMs)
        $this.WaitArgument = $TimeoutMs
        return $true
    }
    $probe | Add-Member -MemberType ScriptMethod -Name Kill -Value {
        param([bool] $EntireProcessTree)
        $this.KillCount++
        $this.KilledTree = $EntireProcessTree
    }

    $waitResult = Wait-ProcessWithinDeadline -Process $probe -CeilingMs 900000 -ElapsedMs 899750
    if (-not $waitResult -or $probe.WaitArgument -ne 250) { throw 'Deadline wait self-test failed to pass the remaining budget.' }
    $probe.WaitArgument = -1
    if (Wait-ProcessWithinDeadline -Process $probe -CeilingMs 900000 -ElapsedMs 900000) { throw 'Deadline wait self-test accepted an expired deadline.' }
    if ($probe.WaitArgument -ne -1) { throw 'Expired deadline self-test unexpectedly waited.' }
    if (Stop-ProcessTreeWithinDeadline -Process $probe -CeilingMs 900000 -ElapsedMs 900000) { throw 'Post-kill wait self-test accepted an expired deadline.' }
    if ($probe.KillCount -ne 1 -or -not $probe.KilledTree -or $probe.WaitArgument -ne -1) { throw 'Tree termination self-test failed.' }

    $scriptText = Get-Content -LiteralPath $PSCommandPath -Raw
    $waitSites = [regex]::Matches($scriptText, '\.WaitForExit\(')
    if ($waitSites.Count -ne 1 -or $scriptText -notmatch '\$Process\.WaitForExit\(\[int\]\$remainingMs\)') {
        throw 'Wait call site bypassed the remaining-deadline helper.'
    }
    if ($scriptText -notmatch '\$outerEnvelopeExceeded = -not \(Test-EnvelopeWithinCeiling' -or
        $scriptText -notmatch 'if \(\$outerEnvelopeExceeded\) \{ Write-Error') {
        throw 'Total-envelope failure check is missing or bypassed.'
    }

    $origin = [DateTimeOffset]::Parse('2026-10-09T00:00:00Z')
    $inside = Get-EnvelopeMilliseconds -Start $origin -End $origin.AddMilliseconds(900000)
    $outside = Get-EnvelopeMilliseconds -Start $origin -End $origin.AddMilliseconds(905960.528)
    if (-not (Test-EnvelopeWithinCeiling -EnvelopeMs $inside -CeilingMs 900000) -or
        (Test-EnvelopeWithinCeiling -EnvelopeMs $outside -CeilingMs 900000) -or
        [math]::Round(($outside - 900000), 3) -ne 5960.528) { throw 'Total UTC envelope self-test failed.' }
    Write-Output 'capture deadline self-test PASS: actual wait receives remaining time; termination shares the deadline; bypass mutations and a 905960.528 ms envelope fail'
    exit 0
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$seriesDir = Join-Path $PSScriptRoot 'calibration-ruling-170'
$runDir = Join-Path $seriesDir ("run-{0}" -f $Run)
if (Test-Path -LiteralPath $runDir) { throw "Run directory already exists: $runDir" }
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$sdkRoot = Join-Path $env:USERPROFILE '.dotnet'
$dotnet = Join-Path $sdkRoot 'dotnet.exe'
$bash = 'C:\Program Files\Git\bin\bash.exe'
if (-not (Test-Path -LiteralPath (Join-Path $sdkRoot 'sdk\10.0.203'))) { throw "Required SDK 10.0.203 is missing under $sdkRoot" }
if (-not (Test-Path -LiteralPath $bash)) { throw "Git Bash is missing: $bash" }
$env:DOTNET_ROOT = $sdkRoot
$env:PATH = "$sdkRoot;$env:PATH"
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:CFD_RING_HOST = 'pc-win'
$sdkVersion = (& $dotnet --version).Trim()
if ($sdkVersion -ne '10.0.203') { throw "Expected SDK 10.0.203, got $sdkVersion" }

$process = [System.Diagnostics.Process]::GetCurrentProcess()
$process.ProcessorAffinity = [IntPtr]0x3F
$processAffinity = ('0x{0:X}' -f $process.ProcessorAffinity.ToInt64())
function Read-RingLoad {
    $reading = (& $bash -c 'cat /proc/loadavg' 2>$null).Trim()
    if (-not $reading) { return 'not-recorded' }
    return ($reading -split '\s+')[0]
}
function Get-Descendants([int] $ParentId) {
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ParentId" -ErrorAction SilentlyContinue)
    foreach ($childProcess in $children) {
        $childProcess
        Get-Descendants -ParentId ([int]$childProcess.ProcessId)
    }
}

$loadStart = Read-RingLoad
$cpuStart = @(Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 3).CounterSamples | ForEach-Object { [math]::Round($_.CookedValue, 1) }
$stdout = Join-Path $runDir 'console.stdout.txt'
$stderr = Join-Path $runDir 'console.stderr.txt'
$remainingMs = Get-RemainingMilliseconds -CeilingMs $outerCeilingMs -ElapsedMs $captureClock.ElapsedMilliseconds
if ($remainingMs -le 0) { throw 'The absolute 900,000 ms deadline expired before ring launch.' }
$child = Start-Process -FilePath $bash -ArgumentList 'tools/run-tests.sh' -WorkingDirectory $repo `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
$child.ProcessorAffinity = [IntPtr]0x3F
$childAffinity = ('0x{0:X}' -f $child.ProcessorAffinity.ToInt64())
$timeout = $false
$affinityPath = Join-Path $runDir 'process-affinity.txt'
$processRows = @("pid=$($child.Id) name=Git Bash affinity=$childAffinity")
$child.Refresh()
if (-not $child.HasExited) {
    Start-Sleep -Milliseconds 500
    $descendants = @(Get-Descendants -ParentId $child.Id)
    foreach ($descendant in $descendants) {
        try {
            $p = [System.Diagnostics.Process]::GetProcessById([int]$descendant.ProcessId)
            $p.ProcessorAffinity = [IntPtr]0x3F
            $mask = ('0x{0:X}' -f $p.ProcessorAffinity.ToInt64())
            $processRows += "pid=$($p.Id) name=$($p.ProcessName) affinity=$mask"
        } catch { $processRows += "pid=$($descendant.ProcessId) name=$($descendant.Name) affinity=not-readable" }
    }
}
$processRows | Set-Content -LiteralPath $affinityPath -Encoding utf8
$firstOutput = 'not-yet-captured'
for ($i = 0; $i -lt 20 -and -not (Test-Path -LiteralPath $stdout); $i++) { Start-Sleep -Milliseconds 250 }
for ($i = 0; $i -lt 20 -and (Get-Item -LiteralPath $stdout -ErrorAction SilentlyContinue).Length -eq 0; $i++) { Start-Sleep -Milliseconds 250 }
if ((Get-Item -LiteralPath $stdout -ErrorAction SilentlyContinue).Length -gt 0) { $firstOutput = (Get-Content -LiteralPath $stdout -TotalCount 1) }
Write-Output "RUN_ACTIVE pid=$($child.Id) affinity=$childAffinity first_output=$firstOutput"
$child.Refresh()
if (-not $child.HasExited) {
    if (-not (Wait-ProcessWithinDeadline -Process $child -CeilingMs $outerCeilingMs -ElapsedMs $captureClock.ElapsedMilliseconds)) {
        $timeout = $true
        $killUtc = [DateTimeOffset]::UtcNow.ToString('o')
        try {
            [void](Stop-ProcessTreeWithinDeadline -Process $child -CeilingMs $outerCeilingMs -ElapsedMs $captureClock.ElapsedMilliseconds)
            "Process.Kill(entireProcessTree=True) requested at $killUtc for launcher PID $($child.Id)." |
                Set-Content -LiteralPath (Join-Path $runDir 'timeout-tree-kill.txt') -Encoding utf8
        } catch {
            "Process.Kill(entireProcessTree=True) failed at $killUtc for launcher PID $($child.Id): $_" |
                Set-Content -LiteralPath (Join-Path $runDir 'timeout-tree-kill.txt') -Encoding utf8
        }
    }
}
$child.Refresh()
$ended = [DateTimeOffset]::UtcNow
$utcEnvelopeMs = Get-EnvelopeMilliseconds -Start $started -End $ended
$outerEnvelopeExceeded = -not (Test-EnvelopeWithinCeiling -EnvelopeMs $utcEnvelopeMs -CeilingMs $outerCeilingMs)
$ringWallMs = $captureClock.ElapsedMilliseconds
$exitCode = if ($timeout) { 'timeout-900s' } else { $child.ExitCode }
$loadEnd = Read-RingLoad
$cpuEnd = @(Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 3).CounterSamples | ForEach-Object { [math]::Round($_.CookedValue, 1) }

$scratch = Join-Path $repo '.tmp-tests'
$heldReader = @()
$catalogPassCount = 0
if (Test-Path -LiteralPath $scratch) { Copy-Item -LiteralPath $scratch -Destination (Join-Path $runDir 'tmp-tests') -Recurse }
if (Test-Path -LiteralPath (Join-Path $runDir 'tmp-tests')) {
    $desktopLog = Join-Path $runDir 'tmp-tests\Desktop.log'
    $desktopMs = Join-Path $runDir 'tmp-tests\Desktop.ms'
    if (Test-Path -LiteralPath $desktopMs) {
        Copy-Item -LiteralPath $desktopMs -Destination (Join-Path $runDir 'Desktop.ms')
    }
    if (Test-Path -LiteralPath $desktopLog) {
        Select-String -LiteralPath $desktopLog -Pattern '^SUITE-TIME ' | ForEach-Object { $_.Line } |
            Set-Content -LiteralPath (Join-Path $runDir 'suite-time-lines.txt') -Encoding utf8
        Select-String -LiteralPath $desktopLog -Pattern '^PASS .*Catalog' | ForEach-Object { $_.Line } |
            Set-Content -LiteralPath (Join-Path $runDir 'catalog-pass-lines.txt') -Encoding utf8
        $catalogPassCount = @(Select-String -LiteralPath $desktopLog -Pattern '^PASS .*Catalog').Count
    }
    $heldReader = @(Get-ChildItem -LiteralPath (Join-Path $runDir 'tmp-tests') -Filter 'Core.part*.log' -File |
        ForEach-Object { Select-String -LiteralPath $_.FullName -Pattern '^PASS Library_UserFileHeldReader_SurvivesReplaceByRename$' })
    $heldReader | ForEach-Object { '{0}: {1}' -f (Split-Path $_.Path -Leaf), $_.Line } |
        Set-Content -LiteralPath (Join-Path $runDir 'held-reader-pass.txt') -Encoding utf8
    $classifierDir = Join-Path $runDir 'classifications'
    New-Item -ItemType Directory -Path $classifierDir | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $runDir 'tmp-tests') -Filter '*.log' -File | Sort-Object Name | ForEach-Object {
        $classification = & py -3 (Join-Path $repo 'tools\check-expected-failures.py') --log $_.FullName --host windows 2>&1
        $classification | Set-Content -LiteralPath (Join-Path $classifierDir ($_.BaseName + '.txt')) -Encoding utf8
    }
    $drift = @()
    foreach ($file in @($stdout, $stderr) + @(Get-ChildItem -LiteralPath (Join-Path $runDir 'tmp-tests') -Filter '*.log' -File | ForEach-Object { $_.FullName })) {
        $drift += Select-String -LiteralPath $file -Pattern '^DRIFT ' | ForEach-Object { '{0}: {1}' -f (Split-Path $file -Leaf), $_.Line }
    }
    $drift | Set-Content -LiteralPath (Join-Path $runDir 'drift-lines.txt') -Encoding utf8
}
$head = (& git -C $repo rev-parse HEAD).Trim()
@(
    "run=$Run"
    "tested_head=$head"
    "started_utc=$($started.ToString('o'))"
    "ended_utc=$($ended.ToString('o'))"
    "wall_ms=$ringWallMs"
    "utc_envelope_ms=$utcEnvelopeMs"
    "outer_envelope_exceeded=$outerEnvelopeExceeded"
    "exit=$exitCode"
    "timed_out=$timeout"
    "sdk=$sdkVersion"
    'sdk_root=%USERPROFILE%\.dotnet'
    'CFD_RING_HOST=pc-win'
    "process_affinity=$processAffinity"
    "bash_affinity=$childAffinity"
    'affinity_mask=0x3F'
    'L3_ranks=6 (coordinator-reported active workload; continuous residency not independently sampled)'
    "load_start=$loadStart"
    "load_end=$loadEnd"
    "cpu_start_percent=$($cpuStart -join ',')"
    "cpu_end_percent=$($cpuEnd -join ',')"
    "outer_ceiling_ms=$outerCeilingMs"
    "held_reader_pass_count=$($heldReader.Count)"
    "catalog_pass_count=$catalogPassCount"
) | Set-Content -LiteralPath (Join-Path $runDir 'measurement.txt') -Encoding utf8
Write-Output "RUN_COMPLETE run=$Run exit=$exitCode wall_ms=$ringWallMs utc_envelope_ms=$utcEnvelopeMs load=$loadStart->$loadEnd affinity=$childAffinity timeout=$timeout"
if ($timeout) { exit 124 }
if ($outerEnvelopeExceeded) { Write-Error "Outer UTC envelope $utcEnvelopeMs ms exceeds $outerCeilingMs ms."; exit 125 }
exit $child.ExitCode
