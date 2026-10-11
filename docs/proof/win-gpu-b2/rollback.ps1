param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$rollbackRoot = Join-Path $proofRoot 'rollback'
$targetDistro = 'cfdw-cuda132-b2'
$protectedDistro = 'cfdw-openfoam2512'
$expectedInstallLocation = 'C:\Projects\CFD-Workbench-wsl\cfdw-cuda132-b2'
$expectedSnapshotSha256 = '42e1d0bed05aa071a70fba8445474a85fa43a1746a394adc4e46a433a090aab9'
$linuxRoot = '/mnt/c' + $proofRoot.Substring(2).Replace('\', '/')

function Write-Json([string]$Path, [object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText($Path, ($json -replace "`r`n", "`n") + "`n", [Text.UTF8Encoding]::new($false))
}

function Invoke-External([string]$File, [string[]]$Arguments) {
    $raw = @(& $File @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $lines = @()
    foreach ($item in $raw) { $lines += ([string]$item -replace "`0", '').TrimEnd() }
    return [pscustomobject]@{ exit_code = $exitCode; lines = $lines }
}

function Get-DistroNames {
    $result = Invoke-External 'wsl.exe' @('--list', '--quiet')
    if ($result.exit_code -ne 0) { throw "WSL list failed with exit $($result.exit_code)" }
    return @($result.lines | Where-Object { $_ })
}

function Get-TargetRegistration {
    $registrations = @()
    $root = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss'
    foreach ($key in Get-ChildItem -LiteralPath $root) {
        $entry = Get-ItemProperty -LiteralPath $key.PSPath
        if ([string]$entry.DistributionName -ceq $targetDistro) { $registrations += $entry }
    }
    if ($registrations.Count -ne 1) { throw "expected one exact target registration, found $($registrations.Count)" }
    return $registrations[0]
}

function Assert-ExactDistroSet([string[]]$Names, [bool]$TargetExpected) {
    $targetCount = @($Names | Where-Object { $_ -ceq $targetDistro }).Count
    $protectedCount = @($Names | Where-Object { $_ -ceq $protectedDistro }).Count
    $expectedTargetCount = if ($TargetExpected) { 1 } else { 0 }
    if ($targetCount -ne $expectedTargetCount -or $protectedCount -ne 1) {
        throw "unexpected distro identities: target=$targetCount protected=$protectedCount"
    }
}

function Capture-ProtectedSnapshot([string]$Output) {
    $script = Join-Path $proofRoot 'completed_guard.py'
    $linuxScript = '/mnt/c' + $script.Substring(2).Replace('\', '/')
    $linuxOutput = '/mnt/c' + $Output.Substring(2).Replace('\', '/')
    $result = Invoke-External 'wsl.exe' @(
        '--distribution', $protectedDistro, '--user', 'root', '--exec',
        '/usr/bin/python3', $linuxScript, 'snapshot', '--output', $linuxOutput, '--include-log-sha256'
    )
    if ($result.exit_code -ne 0) { throw "protected snapshot failed with exit $($result.exit_code)" }
    $sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $Output).Hash.ToLowerInvariant()
    if ($sha -cne $expectedSnapshotSha256) { throw "protected snapshot changed: $sha" }
    return $sha
}

New-Item -ItemType Directory -Path $rollbackRoot -Force | Out-Null

$beforeNames = Get-DistroNames
Assert-ExactDistroSet $beforeNames $true

$registration = Get-TargetRegistration
$actualInstallLocation = [IO.Path]::GetFullPath([string]$registration.BasePath)
if (-not $actualInstallLocation.Equals($expectedInstallLocation, [StringComparison]::OrdinalIgnoreCase)) {
    throw "target install location mismatch: $actualInstallLocation"
}

$marker = Invoke-External 'wsl.exe' @('--distribution', $targetDistro, '--user', 'root', '--exec', '/bin/cat', '/etc/cfdw-b2-disposable')
if ($marker.exit_code -ne 0) { throw "target marker read failed with exit $($marker.exit_code)" }
foreach ($required in @('distro=cfdw-cuda132-b2', 'os_id=ubuntu', 'version_id=24.04')) {
    if ($marker.lines -cnotcontains $required) { throw "target marker lacks $required" }
}

$osRelease = Invoke-External 'wsl.exe' @('--distribution', $targetDistro, '--user', 'root', '--exec', '/bin/cat', '/etc/os-release')
if ($osRelease.exit_code -ne 0) { throw "target OS read failed with exit $($osRelease.exit_code)" }
if ($osRelease.lines -cnotcontains 'ID=ubuntu' -or $osRelease.lines -cnotcontains 'VERSION_ID="24.04"') {
    throw 'target is not Ubuntu 24.04'
}

$residual = Invoke-External 'wsl.exe' @(
    '--distribution', $targetDistro, '--user', 'root', '--exec',
    '/bin/bash', "$linuxRoot/residual-owned.sh", '--any-attempt'
)
if ($residual.exit_code -ne 0 -or @($residual.lines | Where-Object { $_ -like 'residual_process=absent*' }).Count -ne 1) {
    throw "owned B2 process remains or inspection failed: exit=$($residual.exit_code)"
}

$snapshotBefore = Join-Path $rollbackRoot 'l3-before-unregister.json'
$snapshotBeforeSha256 = Capture-ProtectedSnapshot $snapshotBefore

$precheck = [ordered]@{
    schema = 'cfdw-win-gpu-b2-rollback-precheck/1'
    checked_utc = (Get-Date).ToUniversalTime().ToString('o')
    target_distro = $targetDistro
    protected_distro = $protectedDistro
    distro_names = $beforeNames
    marker = $marker.lines
    os_release_id = 'ubuntu'
    os_release_version_id = '24.04'
    install_location = $actualInstallLocation
    residual = $residual.lines
    protected_snapshot_sha256 = $snapshotBeforeSha256
    passed = $true
}
Write-Json (Join-Path $rollbackRoot 'precheck.json') $precheck

$unregister = Invoke-External 'wsl.exe' @('--unregister', $targetDistro)
$unregisterRecord = [ordered]@{
    schema = 'cfdw-win-gpu-b2-unregister/1'
    executed_utc = (Get-Date).ToUniversalTime().ToString('o')
    argv = @('wsl.exe', '--unregister', $targetDistro)
    exit_code = $unregister.exit_code
    output = $unregister.lines
}
Write-Json (Join-Path $rollbackRoot 'unregister.json') $unregisterRecord
if ($unregister.exit_code -ne 0) { throw "target unregister failed with exit $($unregister.exit_code)" }

$afterNames = Get-DistroNames
Assert-ExactDistroSet $afterNames $false
$remainingTargetRegistrations = @()
foreach ($key in Get-ChildItem -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss') {
    $entry = Get-ItemProperty -LiteralPath $key.PSPath
    if ([string]$entry.DistributionName -ceq $targetDistro) { $remainingTargetRegistrations += $entry }
}
if ($remainingTargetRegistrations.Count -ne 0) { throw 'target registration remains after unregister' }
if (Test-Path -LiteralPath $expectedInstallLocation) { throw 'target install location remains after unregister' }

$snapshotAfter = Join-Path $rollbackRoot 'l3-after-unregister.json'
$snapshotAfterSha256 = Capture-ProtectedSnapshot $snapshotAfter

$result = [ordered]@{
    schema = 'cfdw-win-gpu-b2-rollback/1'
    completed_utc = (Get-Date).ToUniversalTime().ToString('o')
    target_distro = $targetDistro
    protected_distro = $protectedDistro
    target_absent = $true
    target_registration_absent = $true
    target_install_location_absent = $true
    protected_present = $true
    distro_names = $afterNames
    protected_snapshot_before_sha256 = $snapshotBeforeSha256
    protected_snapshot_after_sha256 = $snapshotAfterSha256
    unregister_exit_code = $unregister.exit_code
    status = 'complete'
}
Write-Json (Join-Path $rollbackRoot 'rollback.json') $result
Write-Output 'B2-ROLLBACK-PASS target_absent=true protected_present=true'
