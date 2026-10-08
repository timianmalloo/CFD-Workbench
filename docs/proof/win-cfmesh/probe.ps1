$ErrorActionPreference = 'Stop'
$outDir = $PSScriptRoot
$started = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$hostInfo = @(
  'Host OS: Windows 11 Pro, build 26300.9457 (prior verified W-3 survey)'
  "Host architecture: $env:PROCESSOR_ARCHITECTURE"
  "Logical processors: $env:NUMBER_OF_PROCESSORS"
  'Computer name: <redacted-host>'
  'User: <redacted-user>'
  'WSL distribution: cfdw-openfoam2512 (WSL2; prior verified W-3 route)'
) -join "`n"
$metaOut = & wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc 'printf "resolved_binary="; command -v cartesianMesh || true; printf "WM_PROJECT_VERSION="; printf "%s\n" "$WM_PROJECT_VERSION"; dpkg-query -W -f="${Version}\n" openfoam2512' 2>&1
$metaExit = $LASTEXITCODE
$stdoutFile = Join-Path $outDir 'cartesianMesh-help.stdout.txt'
$stderrFile = Join-Path $outDir 'cartesianMesh-help.stderr.txt'
& wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc 'cartesianMesh -help' 1> $stdoutFile 2> $stderrFile
$probeExit = $LASTEXITCODE
$ended = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$hostInfo | Set-Content -Encoding utf8 (Join-Path $outDir 'host-runtime.txt')
$metaOut | Set-Content -Encoding utf8 (Join-Path $outDir 'resolved-tool.txt')
@(
  '# W-5 cartesianMesh availability probe'
  ''
  'Redaction: user and machine identifiers are omitted; no solver or mesh generator was run.'
  'A prior capture script failed in PowerShell argument quoting before reaching the binary; that attempt is excluded from the measured probe.'
  "Started UTC: $started"
  "Ended UTC: $ended"
  'Metadata argv: wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc [command -v cartesianMesh; print WM_PROJECT_VERSION; dpkg-query openfoam2512]'
  "Metadata exit: $metaExit"
  'Metadata output: resolved-tool.txt'
  "Probe argv: wsl.exe --distribution cfdw-openfoam2512 --exec /bin/bash -lc [cartesianMesh -help]"
  "Probe exit: $probeExit"
  'Probe stdout: cartesianMesh-help.stdout.txt'
  'Probe stderr: cartesianMesh-help.stderr.txt'
  'Host/runtime: host-runtime.txt'
) | Set-Content -Encoding utf8 (Join-Path $outDir 'probe.md')
Write-Output "Probe exit=$probeExit"
Get-Content (Join-Path $outDir 'resolved-tool.txt')
Get-Content $stderrFile
