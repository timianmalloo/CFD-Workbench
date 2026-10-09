param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 3)]
    [int] $Run
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$runDir = Join-Path $PSScriptRoot ("run-{0}" -f $Run)
if (Test-Path -LiteralPath $runDir) {
    throw "Run directory already exists: $runDir"
}
New-Item -ItemType Directory -Path $runDir | Out-Null

$sdkRoot = Join-Path $env:USERPROFILE '.dotnet'
$dotnet = Join-Path $sdkRoot 'dotnet.exe'
$bash = 'C:\Program Files\Git\bin\bash.exe'
if (-not (Test-Path -LiteralPath (Join-Path $sdkRoot 'sdk\10.0.203'))) {
    throw "Required SDK 10.0.203 is missing under $sdkRoot"
}
if (-not (Test-Path -LiteralPath $bash)) {
    throw "Git Bash is missing: $bash"
}

$env:DOTNET_ROOT = $sdkRoot
$env:PATH = "$sdkRoot;$env:PATH"
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$sdkVersion = (& $dotnet --version).Trim()
if ($sdkVersion -ne '10.0.203') {
    throw "Expected SDK 10.0.203, got $sdkVersion"
}

$process = [System.Diagnostics.Process]::GetCurrentProcess()
$process.ProcessorAffinity = [IntPtr]0x3F
$affinity = ('0x{0:X}' -f $process.ProcessorAffinity.ToInt64())

function Read-RingLoad {
    $reading = (& $bash -c 'cat /proc/loadavg' 2>$null).Trim()
    if (-not $reading) { return 'not-recorded' }
    return ($reading -split '\s+')[0]
}

function Read-CpuSamples {
    $samples = Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 3
    return @($samples.CounterSamples | ForEach-Object { [math]::Round($_.CookedValue, 1) })
}

$started = [DateTimeOffset]::UtcNow
$loadStart = Read-RingLoad
$cpuStart = Read-CpuSamples
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$stdout = Join-Path $runDir 'console.stdout.txt'
$stderr = Join-Path $runDir 'console.stderr.txt'
$child = Start-Process -FilePath $bash -ArgumentList 'tools/run-tests.sh' -WorkingDirectory $repo `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
$child.ProcessorAffinity = [IntPtr]0x3F
$childAffinity = ('0x{0:X}' -f $child.ProcessorAffinity.ToInt64())
$child.WaitForExit()
$stopwatch.Stop()
$exitCode = $child.ExitCode
$ended = [DateTimeOffset]::UtcNow
$loadEnd = Read-RingLoad
$cpuEnd = Read-CpuSamples

Copy-Item -LiteralPath (Join-Path $repo '.tmp-tests') -Destination (Join-Path $runDir 'tmp-tests') -Recurse
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

@(
    "run=$Run"
    "head=$((& git -C $repo rev-parse HEAD).Trim())"
    "started_utc=$($started.ToString('o'))"
    "ended_utc=$($ended.ToString('o'))"
    "wall_ms=$($stopwatch.ElapsedMilliseconds)"
    "exit=$exitCode"
    "sdk=$sdkVersion"
    "sdk_root=$sdkRoot"
    "process_affinity=$affinity"
    "bash_affinity=$childAffinity"
    "load_start=$loadStart"
    "load_end=$loadEnd"
    "cpu_start_percent=$($cpuStart -join ',')"
    "cpu_end_percent=$($cpuEnd -join ',')"
    'concurrent_work=L3 WSL six-rank workload was active during this calibration series.'
) | Set-Content -LiteralPath (Join-Path $runDir 'measurement.txt') -Encoding utf8

Write-Output "run=$Run exit=$exitCode wall_ms=$($stopwatch.ElapsedMilliseconds) load=$loadStart->$loadEnd affinity=$childAffinity"
exit $exitCode
