# Read-only receipt for the Ruling 237 re-scope plan: what this host reports about its architecture, CPU and GPU under
# the PowerShell runtime that runs this script, what WSL registers, what the stock distro reports from inside, and where
# Git for Windows keeps gpgv. Writes one JSON file. Never records the machine name or a user path (the profile prefix is
# replaced by %USERPROFILE%). Run it once under each runtime: pwsh and Windows PowerShell 5.1.
param([Parameter(Mandatory = $true)][string]$Output, [string]$ToolsCheck = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:WSL_UTF8 = '1'
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ToolsCheck) { $ToolsCheck = Join-Path $ScriptRoot 'wsl_tools_check.sh' }

function Redact([string]$Value) {
    if ([string]::IsNullOrEmpty($Value)) { return $Value }
    return $Value.Replace($env:USERPROFILE, '%USERPROFILE%')
}
function Convert-ToWslPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    return '/mnt/' + $full.Substring(0, 1).ToLowerInvariant() + $full.Substring(2).Replace('\', '/')
}

$runtime = [ordered]@{
    ps_version = $PSVersionTable.PSVersion.ToString()
    ps_edition = $PSVersionTable.PSEdition
    clr = [Environment]::Version.ToString()
    process_architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
}
$windows = [ordered]@{
    processor_architecture_env = $env:PROCESSOR_ARCHITECTURE
    processor_architew6432_env = $env:PROCESSOR_ARCHITEW6432
    os_architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    os_architecture_cim = (Get-CimInstance Win32_OperatingSystem).OSArchitecture
    os_version = [Environment]::OSVersion.Version.ToString()
    cpu = @(Get-CimInstance Win32_Processor | ForEach-Object { $_.Name })
    gpu = @(Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name })
}
$registrations = @(Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object {
    $row = Get-ItemProperty $_.PSPath
    [ordered]@{ key = $_.PSChildName; name = $row.DistributionName; version = $row.Version; base_path = (Redact ([string]$row.BasePath)) }
})
$wslList = @(& wsl.exe --list --verbose 2>&1 | ForEach-Object { ([string]$_ -replace "`0", '').TrimEnd() } | Where-Object { $_ })
$inside = @(& wsl.exe -d Ubuntu-24.04 -u root -- sh (Convert-ToWslPath $ToolsCheck) 2>&1 | ForEach-Object { ([string]$_ -replace "`0", '').TrimEnd() } | Where-Object { $_ })
$insideMap = [ordered]@{}
foreach ($line in $inside) { $k, $v = $line.Split('=', 2); $insideMap[$k] = $v }

$execPath = (& git.exe --exec-path).Trim()
$gitRoot = $execPath
$gpgv = $null
for ($i = 0; $i -lt 5 -and $null -eq $gpgv; $i++) {
    $gitRoot = Split-Path -Parent $gitRoot
    $candidate = Join-Path $gitRoot 'usr\bin\gpgv.exe'
    if (Test-Path -LiteralPath $candidate) { $gpgv = $candidate }
}
$git = [ordered]@{
    exec_path = $execPath
    gpgv_path = $gpgv
    gpgv_version = $(if ($gpgv) { (& $gpgv --version | Select-Object -First 1) } else { 'absent' })
    walk_up_levels_from_exec_path = $(if ($gpgv) { $i } else { -1 })
}
$repoDrive = (Split-Path -Qualifier $ScriptRoot).TrimEnd(':')
$receipt = [ordered]@{
    schema = 'cfdw-arm64-spike-host-identity/1'
    recorded_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    runtime = $runtime
    windows = $windows
    wsl_registrations = $registrations
    wsl_list_verbose = $wslList
    stock_distro_inside = $insideMap
    git = $git
    repo_drive_free_bytes = [ordered]@{ drive = $repoDrive; free = [int64](Get-PSDrive -Name $repoDrive).Free }
}
$json = $receipt | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText($Output, ($json -replace "`r`n", "`n") + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "HOST-IDENTITY-WRITTEN $Output runtime=$($runtime.ps_version) os=$($windows.os_architecture) process=$($runtime.process_architecture)"
