param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$SessionId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$linuxRoot = '/mnt/c' + $proofRoot.Substring(2).Replace('\', '/')
$distro = 'cfdw-cuda132-b2'
$installLocation = 'C:\Projects\CFD-Workbench-wsl\cfdw-cuda132-b2'

& (Join-Path $proofRoot 'download-sources.ps1')
$process = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
    '--distribution', $distro, '--user', 'root', '--exec',
    '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
    '/bin/bash', "$linuxRoot/download-lock.sh"
) -RedirectStandardOutput (Join-Path $proofRoot 'download-lock.stdout.txt') `
    -RedirectStandardError (Join-Path $proofRoot 'download-lock.stderr.txt') -PassThru -Wait -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw "package lock/download failed with exit $($process.ExitCode)" }

$capacity = Get-Content -LiteralPath (Join-Path $proofRoot 'capacity-linux.json') -Raw | ConvertFrom-Json
$drive = Get-PSDrive -Name 'C'
$vhd = Get-Item -LiteralPath (Join-Path $installLocation 'ext4.vhdx')
$hostCapacity = [ordered]@{
    schema = 'cfdw-cuda-host-capacity/1'
    session_id = $SessionId
    install_location = $installLocation
    vhd_bytes = $vhd.Length
    windows_c_used_bytes = $drive.Used
    windows_c_free_bytes = $drive.Free
    required_free_bytes = $capacity.required_free_bytes
    windows_gate_passed = $drive.Free -ge $capacity.required_free_bytes
}
if (-not $hostCapacity.windows_gate_passed) { throw 'Windows backing volume capacity is insufficient' }
$json = $hostCapacity | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText(
    (Join-Path $proofRoot 'capacity-host.json'),
    ($json -replace "`r`n", "`n") + "`n",
    [Text.UTF8Encoding]::new($false)
)
