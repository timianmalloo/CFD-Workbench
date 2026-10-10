$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$cacheRoot = Join-Path $proofRoot '.source-cache'
New-Item -ItemType Directory -Path $cacheRoot -Force | Out-Null
$records = [System.Collections.Generic.List[object]]::new()

function Invoke-Captured {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $stdoutPath = Join-Path $proofRoot "$Name.stdout.txt"
    $stderrPath = Join-Path $proofRoot "$Name.stderr.txt"
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    $startedUtc = [DateTimeOffset]::UtcNow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    $completed = $process.WaitForExit($TimeoutSeconds * 1000)
    if (-not $completed) {
        $process.Kill($true)
        $process.WaitForExit()
    } else {
        $process.WaitForExit()
    }
    $process.Refresh()
    $numericExit = if ($completed -and $process.HasExited) { [int]$process.ExitCode } else { $null }
    $stopwatch.Stop()
    $records.Add([ordered]@{
        name = $Name
        file = $FilePath
        argv = $ArgumentList
        cwd = (Get-Location).Path
        started_utc = $startedUtc.ToString('o')
        ended_utc = [DateTimeOffset]::UtcNow.ToString('o')
        elapsed_ms = $stopwatch.ElapsedMilliseconds
        timeout_seconds = $TimeoutSeconds
        timed_out = -not $completed
        exit_code = $numericExit
        stdout = [IO.Path]::GetFileName($stdoutPath)
        stderr = [IO.Path]::GetFileName($stderrPath)
        capture_encoding = 'Start-Process redirected text; exact child byte encoding not independently established'
    })
    if (-not $completed -or $numericExit -ne 0) {
        throw "$Name failed or timed out; exit=$numericExit timeout=$(-not $completed)"
    }
}

$tagApi = 'https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tags/OpenFOAM-v2512'
Invoke-Captured -Name 'source-tag' -FilePath 'curl.exe' -ArgumentList @(
    '--fail', '--location', '--silent', '--show-error', $tagApi
) -TimeoutSeconds 60

$sourceUrl = 'https://dl.openfoam.com/source/v2512/OpenFOAM-v2512.tgz'
$archivePath = Join-Path $cacheRoot 'OpenFOAM-v2512.tgz'
$headersPath = Join-Path $proofRoot 'source-archive.headers.txt'
Remove-Item -LiteralPath $archivePath, $headersPath -Force -ErrorAction SilentlyContinue
Invoke-Captured -Name 'source-download' -FilePath 'curl.exe' -ArgumentList @(
    '--fail', '--location', '--silent', '--show-error', '--dump-header', $headersPath,
    '--output', $archivePath, $sourceUrl
) -TimeoutSeconds 300

Invoke-Captured -Name 'source-build-info' -FilePath 'tar.exe' -ArgumentList @(
    '-xOf', $archivePath, 'OpenFOAM-v2512/META-INFO/build-info'
) -TimeoutSeconds 30

$source = [ordered]@{
    schema = 'cfdw-source-capture/1'
    authority = 'Ruling 197 G1'
    source_url = $sourceUrl
    tag_api = $tagApi
    expected_tag = 'OpenFOAM-v2512'
    expected_commit = '87ed40d256d22ea38fcc648dfc82a22162427b18'
    archive_bytes = (Get-Item -LiteralPath $archivePath).Length
    archive_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash.ToLowerInvariant()
    archive_retention = 'untracked .source-cache file retained locally through owner review'
    captured_utc = [DateTimeOffset]::UtcNow.ToString('o')
}
$source | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $proofRoot 'source.json') -Encoding utf8

$wslScript = '/mnt/c/Projects/CFD-Workbench-win-gpu-g1-r197/docs/proof/win-gpu-g1/inspect-installed.sh'
Invoke-Captured -Name 'installed-linkage' -FilePath 'wsl.exe' -ArgumentList @(
    '--distribution', 'cfdw-openfoam2512', '--user', 'root', '--exec',
    '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10', '/bin/bash', $wslScript
) -TimeoutSeconds 60

$capture = [ordered]@{
    schema = 'cfdw-read-only-capture/1'
    authority = 'Ruling 197 G1'
    scope = 'Official v2512 source identity plus installed-library linkage; no build, package install, solver run or L3 access'
    wrapper = 'capture.ps1'
    wrapper_sha256_before_execution = (Get-FileHash -Algorithm SHA256 -LiteralPath $MyInvocation.MyCommand.Path).Hash.ToLowerInvariant()
    records = $records
}
$capture | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $proofRoot 'capture.json') -Encoding utf8
