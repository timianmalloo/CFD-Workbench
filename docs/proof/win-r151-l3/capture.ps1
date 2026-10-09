# One read-only WSL invocation; output is allowlisted by readback.sh.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = '/mnt/c/Projects/CFD-Workbench-win-r151-l3/docs/proof/win-r151-l3/readback.sh'
$start = (Get-Date).ToUniversalTime()
$hash = (Get-FileHash -LiteralPath (Join-Path $here 'readback.sh') -Algorithm SHA256).Hash.ToLowerInvariant()
$keepalive = $null
try {
    $p = Get-CimInstance Win32_Process -Filter 'ProcessId = 19296'
    if ($null -ne $p) {
        $identity = Split-Path -Leaf $p.ExecutablePath
        $keepalive = @{ pid = 19296; creation_time_utc = $p.CreationDate.ToUniversalTime().ToString('o'); executable = $identity; current_liveness = 'yes'; historical_distro_relationship = 'Not recorded' }
    } else { $keepalive = @{ pid = 19296; current_liveness = 'no'; creation_time_utc = 'Not recorded'; executable = 'Not recorded'; historical_distro_relationship = 'Not recorded' } }
} catch { $keepalive = @{ pid = 19296; current_liveness = 'Not recorded'; creation_time_utc = 'Not recorded'; executable = 'Not recorded'; historical_distro_relationship = 'Not recorded' } }
$psi = [System.Diagnostics.ProcessStartInfo]::new('wsl.exe')
$psi.ArgumentList.Add('-d'); $psi.ArgumentList.Add('cfdw-openfoam2512')
$psi.ArgumentList.Add('-u'); $psi.ArgumentList.Add('root')
$psi.ArgumentList.Add('--exec'); $psi.ArgumentList.Add('/bin/bash'); $psi.ArgumentList.Add($script)
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$proc = [System.Diagnostics.Process]::Start($psi)
$outTask = $proc.StandardOutput.ReadToEndAsync()
$errTask = $proc.StandardError.ReadToEndAsync()
$proc.WaitForExit()
$outTask.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $here 'capture.stdout.txt') -NoNewline -Encoding utf8
$errTask.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $here 'capture.stderr.txt') -NoNewline -Encoding utf8
$end = (Get-Date).ToUniversalTime()
$afterKeepalive = 'Not recorded'
try {
    $after = Get-CimInstance Win32_Process -Filter 'ProcessId = 19296'
    if ($null -eq $after) { $afterKeepalive = 'no' }
    elseif ($keepalive.current_liveness -eq 'yes' -and $after.CreationDate.ToUniversalTime().ToString('o') -eq $keepalive.creation_time_utc -and (Split-Path -Leaf $after.ExecutablePath) -eq $keepalive.executable) { $afterKeepalive = 'yes; same creation time and executable' }
    else { $afterKeepalive = 'identity changed or initial identity unavailable' }
} catch { $afterKeepalive = 'Not recorded' }
$keepalive.current_liveness_at_capture_end = $afterKeepalive
$record = [ordered]@{ start_utc = $start.ToString('o'); end_utc = $end.ToString('o'); command = 'wsl.exe -d cfdw-openfoam2512 -u root --exec /bin/bash <script>'; script_sha256 = $hash; exit_code = $proc.ExitCode; keepalive = $keepalive; capture_state = $(if ($proc.ExitCode -eq 0) { 'complete readback; mutable sources non-atomic' } else { 'partial/error' }) }
$record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $here 'capture.json') -Encoding utf8
