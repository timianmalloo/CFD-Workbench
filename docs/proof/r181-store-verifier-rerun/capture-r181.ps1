# Ruling 181 one-run verifier capture. Do not use this script for a second run.
# The verifier's internal 60 s contract is unchanged; this wrapper allows 180 s
# for the verifier child and its shutdown/cleanup lifecycle.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PythonPath)

$ErrorActionPreference = 'Stop'
$ExpectedVerifierSha256 = 'a79ac73c04cc8769d65868cde2f3c69154f27ebfcff40743bd20ca0b668afd52'
$ExpectedRunnerSha256 = 'cc201aca4cd2e9e434bec1b9630487a2184622d2ece08617c373d037d528848d'
$VerifierRelative = 'tools/verify-windows-store.py'
$RunnerRelative = 'tools/windows-runner.ps1'
$VerifierSnapshotRelative = "source-snapshots/$ExpectedVerifierSha256/verify-windows-store.py"
$RunnerSnapshotRelative = "source-snapshots/$ExpectedRunnerSha256/windows-runner.ps1"
$Proof = [IO.Path]::GetFullPath($PSScriptRoot)
$Repo = [IO.DirectoryInfo]::new($Proof).Parent.Parent.Parent.FullName
$VerifierPath = Join-Path $Repo $VerifierRelative
$RunnerPath = Join-Path $Repo $RunnerRelative
$VerifierSnapshotPath = Join-Path $Proof $VerifierSnapshotRelative
$RunnerSnapshotPath = Join-Path $Proof $RunnerSnapshotRelative
$CapturePaths = @(
    (Join-Path $Proof 'verifier.stdout.txt'),
    (Join-Path $Proof 'verifier.stderr.txt'),
    (Join-Path $Proof 'verifier.json'),
    (Join-Path $Proof 'residuals.stdout.txt'),
    (Join-Path $Proof 'residuals.stderr.txt'),
    (Join-Path $Proof 'residuals.json')
)
$LaunchMarkerPath = Join-Path $Proof 'launch.marker.json'

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-FingerprintSha256([string]$Fingerprint) {
    $bytes = [Text.Encoding]::UTF8.GetBytes($Fingerprint)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

function Assert-NoPriorCapture {
    if (Test-Path -LiteralPath $LaunchMarkerPath) { throw 'R181-CAPTURE: launch marker exists; verifier run is consumed' }
    foreach ($path in $CapturePaths) {
        if (Test-Path -LiteralPath $path) { throw 'R181-CAPTURE: append-only capture already exists' }
    }
}

function Assert-Snapshots {
    if ((Get-Sha256 $VerifierPath) -cne $ExpectedVerifierSha256 -or
        (Get-Sha256 $RunnerPath) -cne $ExpectedRunnerSha256 -or
        (Get-Sha256 $VerifierSnapshotPath) -cne $ExpectedVerifierSha256 -or
        (Get-Sha256 $RunnerSnapshotPath) -cne $ExpectedRunnerSha256) {
        throw 'R181-SOURCE: live source or immutable snapshot hash mismatch'
    }
}

function Assert-ShutdownOrder($Events, $Result) {
    $rootExit = "target-root-exit:$($Result.ExitCode)"
    $rootIndex = $Events.IndexOf($rootExit)
    $shutdownStartIndex = $Events.IndexOf('shutdown-start')
    $shutdownExitIndex = $Events.IndexOf('shutdown-exit:0')
    $jobQueryIndex = $Events.IndexOf('target-job-query')
    $jobCloseIndex = $Events.IndexOf('target-job-close')
    if ($rootIndex -lt 0 -or $shutdownStartIndex -le $rootIndex -or
        $shutdownExitIndex -le $shutdownStartIndex -or $jobQueryIndex -le $shutdownExitIndex -or
        $jobCloseIndex -le $jobQueryIndex -or $Result.BuildServerShutdownExit -isnot [int] -or
        $Result.BuildServerShutdownExit -ne 0) {
        throw 'R181-LIFECYCLE: shutdown must exit 0 before target residual query and job close'
    }
}

function Invoke-ResidualSample([string]$SamplerPath, $Toolchain, [Diagnostics.Stopwatch]$Clock) {
    # The sample is a separate guarded child and runs only after Assert-ShutdownOrder.
    $shell = (Get-Process -Id $PID).Path
    return Invoke-WriChild -Repo $Repo -Exe $shell `
        -Arguments @('-NoProfile','-File',$SamplerPath) `
        -Clock $Clock -CeilingMs 180000 -ChildCeilingMs 20000 `
        -Mode Normal -PythonPath $Toolchain.Python
}

function Assert-VerifierOutputContract($Result, [string]$TestedHead) {
    $text = [string]$Result.Stdout
    if ($text -notmatch "(?m)^HEAD=$([regex]::Escape($TestedHead))\s*$" -or
        $text -notmatch "(?m)^SCRIPT_SHA256=$ExpectedVerifierSha256\s*$") {
        throw 'R181-OUTPUT: verifier identity does not match the tested source'
    }
    $requiredPasses = @(
        'WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped',
        'WindowsStore_Unqualified_ProductionAdmissionRemainsClosed',
        'WindowsNative_Environment_RealNtfsX64',
        'WindowsNative_Layouts_SdkX64',
        'WindowsNative_RelativeCreate_PrivateAclBeforeAnyBytes',
        'WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage',
        'WindowsNative_Replace_ShareReadOnlyReaderRefused',
        'WindowsNative_ClaimDispose_HeldObserverNamespaceRemoved',
        'WindowsNative_Rename_FreshWin32VersusNtFixtures',
        'WindowsNative_Rename_AttributionNullRootExAndRootedLegacy',
        'WindowsNative_CreateOnly_CollisionPreservesBothObjects',
        'WindowsNative_RelativeName_TraversalRefusedBeforeCreate'
    )
    $namedPasses = @([regex]::Matches($text, '(?m)^PASS (?!Windows store qualification checks=)(\S+)\s*$') |
        ForEach-Object { $_.Groups[1].Value })
    $failures = [regex]::Matches($text, '(?m)^FAIL\s+')
    if ($namedPasses.Count -ne $requiredPasses.Count -or
        (Compare-Object -ReferenceObject $requiredPasses -DifferenceObject $namedPasses).Count -ne 0 -or
        $failures.Count -ne 1 -or
        $text -notmatch '(?m)^FAIL WindowsNative_Replace_HeldReaderKeepsOldImage\b' -or
        $text -notmatch '(?m)^RESULT failures=1\s*$' -or
        $text -notmatch '(?m)^SUBSET .* ran=13 skipped=\d+\s*$' -or
        $text -notmatch '(?m)^EXPECTED-FAIL 1 \(manifest\)\s*$' -or
        $text -notmatch '(?m)^UNEXPECTED 0\s*$' -or
        $text -notmatch '(?m)^TEST_EXIT=1\s*$' -or
        $text -notmatch '(?m)^CLASSIFIER_EXIT=0\s*$' -or
        $text -notmatch '(?m)^PASS Windows store qualification checks=13 expected_failures=1\s*$') {
        throw 'R181-OUTPUT: qualification output does not match the reviewed 13-check contract'
    }
    $stderrLines = @($Result.Stderr -split '\r?\n' | Where-Object { $_ })
    $expectedStderr = @('TEST_STDERR_BEGIN','TEST_STDERR_END','CLASSIFIER_STDERR_BEGIN','CLASSIFIER_STDERR_END')
    if ($stderrLines.Count -ne $expectedStderr.Count -or
        (Compare-Object -ReferenceObject $expectedStderr -DifferenceObject $stderrLines -SyncWindow 0).Count -ne 0) {
        throw 'R181-OUTPUT: verifier stderr delimiters are incomplete or contain payload'
    }
    $wall = [regex]::Match($text, '(?m)^TOTAL_WALL_SECONDS=(\d+(?:\.\d+)?)\s*$')
    if (-not $wall.Success -or [double]::Parse($wall.Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture) -gt 60) {
        throw 'R181-OUTPUT: verifier internal wall time is absent or exceeds 60 seconds'
    }
    return [double]::Parse($wall.Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
}

function Assert-VerifierResult($Result, $Events, [string]$TestedHead) {
    Assert-WriNumericExit $Result.ExitCode
    if ($Result.ExitCode -ne 0 -or $Result.TimedOut -or -not $Result.RetainedHandle -or
        $Result.Mode -cne 'BuildVerifier' -or $Result.ResidualStatus -cne 'job-active-zero-verified' -or
        $Result.Cleanup -notin @('none-active-verified','terminated-complete') -or
        $Result.PhnStatus -cne 'PASS' -or $null -eq $Result.SubstitutionBinding -or
        $Result.RawStdoutSha256 -notmatch '^[0-9a-f]{64}$' -or
        $Result.RawStderrSha256 -notmatch '^[0-9a-f]{64}$') {
        throw 'R181-RESULT: verifier process, cleanup, or PHN contract failed'
    }
    Assert-ShutdownOrder $Events $Result
    return Assert-VerifierOutputContract $Result $TestedHead
}

function Assert-ResidualResult($Result) {
    Assert-WriNumericExit $Result.ExitCode
    if ($Result.ExitCode -ne 0 -or $Result.TimedOut -or -not $Result.RetainedHandle -or
        $Result.Mode -cne 'Normal' -or $Result.ResidualStatus -cne 'job-active-zero-verified' -or
        $Result.Cleanup -notin @('none-active-verified','terminated-complete') -or
        $Result.PhnStatus -cne 'PASS' -or $null -eq $Result.SubstitutionBinding -or
        $Result.Stderr.Trim()) {
        throw 'R181-RESIDUAL: sampler process, cleanup, or PHN contract failed'
    }
    try { $parsed = $Result.Stdout | ConvertFrom-Json -ErrorAction Stop }
    catch { throw 'R181-RESIDUAL: sampler returned invalid JSON' }
    if ($parsed.completion -cne 'complete' -or [int]$parsed.unreadableCount -ne 0 -or
        [int]$parsed.count -ne 0 -or @($parsed.processes).Count -ne 0) {
        throw 'R181-RESIDUAL: build-server absence was not completely observed'
    }
    return $parsed
}

function Get-FailureCode($Exception) {
    $message = [string]$Exception.Message
    if ($message -match '^((?:R181|WRI)-[A-Z-]+)') { return $Matches[1] }
    return 'R181-UNCLASSIFIED'
}

if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'R181-PLATFORM: PowerShell 7 on Windows required'
}
if ([IntPtr]::Size -ne 8) { throw 'R181-PLATFORM: Windows x64 required' }
Assert-NoPriorCapture
Assert-Snapshots

# Dot-sourcing takes the committed runner's function-library path. Its Ready
# action and Settings preflight are below the dot-source return and do not run.
. $RunnerPath

$toolchainClock = [Diagnostics.Stopwatch]::StartNew()
$toolchain = Assert-WriToolchain -Repo $Repo -PythonPath $PythonPath
$toolchainClock.Stop()
if ($toolchain.Sdk -cne '10.0.203' -or [IO.Path]::GetFullPath($toolchain.Python) -cne [IO.Path]::GetFullPath($PythonPath)) {
    throw 'R181-TOOLCHAIN: pinned toolchain identity mismatch'
}

$beforeFingerprint = Get-WriSourceFingerprint $Repo
Assert-WriSourceClean $Repo
$testedHead = (& git -C $Repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $testedHead -notmatch '^[0-9a-f]{40}$') { throw 'R181-SOURCE: tested HEAD unavailable' }
$clock = [Diagnostics.Stopwatch]::StartNew()
$events = [Collections.Generic.List[string]]::new()
$launchUtc = [DateTime]::UtcNow.ToString('o')
Write-Utf8NoBom $LaunchMarkerPath (([ordered]@{
    ruling=181; launch_count=1; tested_head=$testedHead; verifier_sha256=$ExpectedVerifierSha256;
    launch_authorized_utc=$launchUtc; state='verifier-launch-consumed'
} | ConvertTo-Json) + "`n")

$verifier = $null
$sample = $null
$sampleJson = $null
$verifierWallSeconds = $null
$afterFingerprint = $null
$sourceUnchanged = $false
$failureCode = $null
$result = 'BLOCKED'
$samplerPath = Join-Path $Proof 'sample-build-servers.ps1'
try {
    $verifier = Invoke-WriChild -Repo $Repo -Exe $toolchain.Python `
        -Arguments @($VerifierRelative) -Clock $clock -CeilingMs 180000 -ChildCeilingMs 180000 `
        -Mode BuildVerifier -Toolchain $toolchain -PythonPath $toolchain.Python -Events $events
    Write-Utf8NoBom $CapturePaths[0] $verifier.Stdout
    Write-Utf8NoBom $CapturePaths[1] $verifier.Stderr
    $verifierWallSeconds = Assert-VerifierResult $verifier $events $testedHead

    # This guarded sample shares the verifier's 180 s Stopwatch and follows the
    # observed numeric shutdown exit and target-job query.
    $sample = Invoke-ResidualSample $samplerPath $toolchain $clock
    Write-Utf8NoBom $CapturePaths[3] $sample.Stdout
    Write-Utf8NoBom $CapturePaths[4] $sample.Stderr
    $sampleJson = Assert-ResidualResult $sample
    Write-Utf8NoBom $CapturePaths[5] (($sampleJson | ConvertTo-Json -Depth 6) + "`n")

    $afterFingerprint = Get-WriSourceFingerprint $Repo $clock 180000
    Assert-WriSourceUnchanged $Repo $beforeFingerprint $clock 180000
    $sourceUnchanged = $beforeFingerprint -ceq $afterFingerprint
    if (-not $sourceUnchanged) { throw 'R181-SOURCE: source fingerprint changed' }
    Assert-WriEnvelope $clock 180000
    $result = 'PASS'
} catch {
    $failureCode = Get-FailureCode $_.Exception
    $result = 'BLOCKED'
} finally {
    $clock.Stop()
}

$sourceHashBefore = Get-FingerprintSha256 $beforeFingerprint
$sourceHashAfter = if ($null -ne $afterFingerprint) { Get-FingerprintSha256 $afterFingerprint } else { $null }
$sampledAtUtc = [DateTime]::UtcNow.ToString('o')

$verifierMetadata = [ordered]@{
    ruling = 181
    tested_head = $testedHead
    launch_count = 1
    launch_utc = $launchUtc
    command = @('Python executable selected by Assert-WriToolchain','tools/verify-windows-store.py')
    verifier_sha256 = $ExpectedVerifierSha256
    verifier_snapshot = $VerifierSnapshotRelative.Replace('\','/')
    runner_sha256 = $ExpectedRunnerSha256
    runner_snapshot = $RunnerSnapshotRelative.Replace('\','/')
    sdk_version = $toolchain.Sdk
    python_version = $toolchain.PythonVersion
    toolchain_check_ms = $toolchainClock.ElapsedMilliseconds
    mode = if ($null -ne $verifier) { $verifier.Mode } else { 'not-recorded' }
    observed_process_exit = if ($null -ne $verifier) { $verifier.ExitCode } else { $null }
    timed_out = if ($null -ne $verifier) { [bool]$verifier.TimedOut } else { $null }
    retained_handle = if ($null -ne $verifier) { [bool]$verifier.RetainedHandle } else { $null }
    child_wall_ms = if ($null -ne $verifier) { $verifier.WallMs } else { $null }
    verifier_total_wall_seconds = $verifierWallSeconds
    wrapper_envelope_elapsed_ms = $clock.ElapsedMilliseconds
    child_ceiling_ms = 180000
    shutdown_exit = if ($null -ne $verifier) { $verifier.BuildServerShutdownExit } else { $null }
    lifecycle_events = @($events)
    root_exit_descendants = if ($null -ne $verifier) { $verifier.RootExitDescendants } else { $null }
    cleanup = if ($null -ne $verifier) { $verifier.Cleanup } else { 'not-recorded' }
    job_active_zero = if ($null -ne $verifier) { $verifier.ResidualStatus -ceq 'job-active-zero-verified' } else { $false }
    stdout_raw_sha256 = if ($null -ne $verifier) { $verifier.RawStdoutSha256 } else { $null }
    stderr_raw_sha256 = if ($null -ne $verifier) { $verifier.RawStderrSha256 } else { $null }
    phn_status = if ($null -ne $verifier) { $verifier.PhnStatus } else { 'not-recorded' }
    substitution_binding = if ($null -ne $verifier) { $verifier.SubstitutionBinding } else { $null }
    source_fingerprint_before_sha256 = $sourceHashBefore
    source_fingerprint_after_sha256 = $sourceHashAfter
    source_unchanged = $sourceUnchanged
    residual_sample_after_shutdown_exit = $null -ne $sample -and $verifier.BuildServerShutdownExit -eq 0
    residual_sample_exit = if ($null -ne $sample) { $sample.ExitCode } else { $null }
    residual_sample_ceiling_ms = 20000
    residual_sample_phn_status = if ($null -ne $sample) { $sample.PhnStatus } else { 'not-recorded' }
    residual_sample = $sampleJson
    capture_generated_utc = $sampledAtUtc
    failure_code = $failureCode
    result = $result
}

$jsonOptions = @{Depth=8}
Write-Utf8NoBom $CapturePaths[2] (($verifierMetadata | ConvertTo-Json @jsonOptions) + "`n")
if ($null -eq $sampleJson -and -not (Test-Path -LiteralPath $CapturePaths[5])) {
    Write-Utf8NoBom $CapturePaths[5] (([ordered]@{
        completion='not-recorded'; count=$null; unreadableCount=$null; processes=@();
        reason=if ($failureCode) { $failureCode } else { 'R181-RESIDUAL-NOT-RUN' }
    } | ConvertTo-Json -Depth 4) + "`n")
}

"R181 CAPTURE result=$result failure=$failureCode launch_count=1"
if ($result -ne 'PASS') { exit 1 }
exit 0
