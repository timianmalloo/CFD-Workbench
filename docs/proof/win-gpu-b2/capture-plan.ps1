param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$SessionId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$linuxRoot = '/mnt/c' + $proofRoot.Substring(2).Replace('\', '/')
$cache = Join-Path $proofRoot '.source-cache'
$distro = 'cfdw-cuda132-b2'
$protectedDistro = 'cfdw-openfoam2512'
$installParent = 'C:\Projects\CFD-Workbench-wsl'
$installLocation = Join-Path $installParent $distro
$rootfs = Join-Path $cache 'ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz'

& (Join-Path $proofRoot 'download-sources.ps1')
if ($LASTEXITCODE -notin @(0, $null)) { throw "source download failed with exit $LASTEXITCODE" }

$verifyOut = Join-Path $proofRoot 'source-verify.stdout.txt'
$verifyErr = Join-Path $proofRoot 'source-verify.stderr.txt'
$verify = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
    '--distribution', $protectedDistro, '--user', 'root', '--exec',
    '/usr/bin/taskset', '--cpu-list', '0', '/usr/bin/nice', '-n', '10',
    '/bin/bash', "$linuxRoot/verify-sources.sh"
) -RedirectStandardOutput $verifyOut -RedirectStandardError $verifyErr -PassThru -Wait -WindowStyle Hidden
if ($verify.ExitCode -ne 0) { throw "source verification failed with exit $($verify.ExitCode)" }

$names = @(& wsl.exe --list --quiet) | ForEach-Object { $_.Replace([char]0, '').Trim() } | Where-Object { $_ }
if ($names -contains $distro) { throw "disposable distro $distro already exists" }
if ($names -notcontains $protectedDistro) { throw "protected distro identity is unavailable" }
if (Test-Path -LiteralPath $installLocation) { throw "install location already exists: $installLocation" }
$resolvedParent = [IO.Path]::GetFullPath($installParent)
if ($resolvedParent -ne 'C:\Projects\CFD-Workbench-wsl') { throw 'unexpected WSL install parent' }
New-Item -ItemType Directory -Path $installLocation -Force | Out-Null

$importOut = Join-Path $proofRoot 'wsl-import.stdout.txt'
$importErr = Join-Path $proofRoot 'wsl-import.stderr.txt'
$import = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
    '--import', $distro, $installLocation, $rootfs, '--version', '2'
) -RedirectStandardOutput $importOut -RedirectStandardError $importErr -PassThru -Wait -WindowStyle Hidden
if ($import.ExitCode -ne 0) { throw "WSL import failed with exit $($import.ExitCode)" }

$afterNames = @(& wsl.exe --list --quiet) | ForEach-Object { $_.Replace([char]0, '').Trim() } | Where-Object { $_ }
if ($afterNames -notcontains $distro -or $afterNames -notcontains $protectedDistro) {
    throw 'post-import distro identities are incomplete'
}

$prepareOut = Join-Path $proofRoot 'prepare-lock.stdout.txt'
$prepareErr = Join-Path $proofRoot 'prepare-lock.stderr.txt'
$prepare = Start-Process -FilePath 'wsl.exe' -ArgumentList @(
    '--distribution', $distro, '--user', 'root', '--exec',
    '/usr/bin/taskset', '--cpu-list', '0-1', '/usr/bin/nice', '-n', '10',
    '/bin/bash', "$linuxRoot/prepare-lock.sh"
) -RedirectStandardOutput $prepareOut -RedirectStandardError $prepareErr -PassThru -Wait -WindowStyle Hidden
if ($prepare.ExitCode -ne 0) { throw "lock preparation failed with exit $($prepare.ExitCode)" }

$result = [ordered]@{
    schema = 'cfdw-win-gpu-b2-plan/1'
    session_id = $SessionId
    distro = $distro
    protected_distro = $protectedDistro
    install_location = $installLocation
    image = [ordered]@{
        file = [IO.Path]::GetFileName($rootfs)
        bytes = (Get-Item -LiteralPath $rootfs).Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $rootfs).Hash.ToLowerInvariant()
    }
    source_verified = $true
    cuda_installed = $false
    f1_authorized = $false
}
$json = $result | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText(
    (Join-Path $proofRoot 'plan-capture.json'),
    ($json -replace "`r`n", "`n") + "`n",
    [Text.UTF8Encoding]::new($false)
)
