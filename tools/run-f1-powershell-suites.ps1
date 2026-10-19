# The F1 PowerShell suites (tests/f1_*.ps1) on both runtimes, from the ring (tools/run-tests.sh, Windows only).
# Run under pwsh. Windows PowerShell 5.1 is started from here, never from the ring's x64 bash: on an ARM64 host the 5.1
# image is ARM64X and takes its parent's architecture, so a bash-started 5.1 is an x64 process that reports an x64 OS
# (register entry EMULATED-PROCESS-REPORTS-AN-EMULATED-OS). Each runtime's process architecture is measured and must be
# the host's native one, read from CIM (the one source an emulated process does not repaint); otherwise F1-PS-EMULATED
# fails the ring before any suite runs. Prints one line per suite and runtime with milliseconds and the exit code, and a
# total. Exit 0 green, 1 a suite failed or a runtime is emulated.
param([string]$Scratch = '')
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
if (-not $Scratch) { $Scratch = Join-Path $root '.tmp-tests' }
New-Item -ItemType Directory -Path $Scratch -Force | Out-Null
$clock = [Diagnostics.Stopwatch]::StartNew()
# A Windows PowerShell child inherits this pwsh's PSModulePath (the Core module directories first) and then cannot resolve
# its own cmdlets (Get-FileHash absent; measured 2026-10-19, register entry PWSH-CHILD-INHERITS-CORE-MODULE-PATH). Every
# child gets the Windows PowerShell default; a pwsh child prepends its own directories to it at startup.
$documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
$env:PSModulePath = (Join-Path $documents 'WindowsPowerShell\Modules') + ';' + (Join-Path $env:ProgramFiles 'WindowsPowerShell\Modules') + ';' +
    (Join-Path $env:SystemRoot 'system32\WindowsPowerShell\v1.0\Modules')

function Invoke-Runtime([string]$Exe, [string[]]$Arguments, [string]$Label) {
    $out = Join-Path $Scratch "$Label.stdout.txt"; $err = Join-Path $Scratch "$Label.stderr.txt"
    $process = Start-Process -FilePath $Exe -ArgumentList (@('-NoProfile', '-NonInteractive') + $Arguments) -PassThru -NoNewWindow `
        -RedirectStandardOutput $out -RedirectStandardError $err
    $null = $process.Handle   # POWERSHELL-REDIRECTED-EXITCODE-HANDLE-A: acquire before the wait
    $process.WaitForExit()
    return @{ exit = $process.ExitCode; out = $out; err = $err }
}

# The native architecture per CIM, and the name RuntimeInformation gives it (the two names the packet's table maps).
$cim = [string](Get-CimInstance Win32_OperatingSystem).OSArchitecture
$native = switch -Regex ($cim) { '^ARM 64' { 'Arm64' } '^64-bit$' { 'X64' } default { '' } }
if (-not $native) { Write-Output "F1-PS-EMULATED unmapped CIM OS architecture '$cim'"; exit 1 }
$runtimes = @(
    @{ name = 'pwsh'; exe = (Get-Process -Id $PID).Path },
    @{ name = 'powershell'; exe = 'powershell.exe' }
)
$failed = 0
$probeScript = Join-Path $Scratch 'f1-ps-runtime-probe.ps1'
[IO.File]::WriteAllText($probeScript, "'process=' + [string][Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture`n" +
    "'get-filehash=' + (`$null -ne (Get-Command Get-FileHash -ErrorAction SilentlyContinue)).ToString()`n")
foreach ($runtime in $runtimes) {
    $probe = Invoke-Runtime $runtime.exe @('-File', $probeScript) "f1-ps-$($runtime.name)-probe"
    $values = @{ process = 'not recorded'; 'get-filehash' = 'not recorded' }
    if ($probe.exit -eq 0) {
        foreach ($line in [IO.File]::ReadAllLines($probe.out)) { $key, $value = $line.Split('=', 2); $values[$key] = $value }
    }
    Write-Output "F1-PS runtime=$($runtime.name) process=$($values.process) native=$native cim='$cim' get-filehash=$($values['get-filehash'])"
    if ($values.process -cne $native) {
        Write-Output "F1-PS-EMULATED $($runtime.name) runs as $($values.process) on a $native host: start it from a native parent (this runner starts 5.1 from pwsh)"
        $failed = 1
    }
    if ($values['get-filehash'] -cne 'True') {
        Write-Output "F1-PS-MODULES $($runtime.name) cannot resolve Get-FileHash: its module path is not its own"
        $failed = 1
    }
}
if ($failed) { exit 1 }
$suites = @(Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter 'f1_*.ps1' -File | Sort-Object Name)
if ($suites.Count -eq 0) { Write-Output 'F1-PS no suites found'; exit 1 }
$count = 0
foreach ($runtime in $runtimes) {
    foreach ($suite in $suites) {
        $label = "f1-ps-$($runtime.name)-$($suite.BaseName)"
        $started = $clock.Elapsed.TotalMilliseconds
        $result = Invoke-Runtime $runtime.exe @('-File', $suite.FullName) $label
        $ms = [int]($clock.Elapsed.TotalMilliseconds - $started)
        $count++
        Write-Output "== f1 $($runtime.name) $($suite.Name) $ms ms exit $($result.exit)"
        if ($result.exit -ne 0) {
            $failed = 1
            Get-Content -LiteralPath $result.out -Tail 5 | ForEach-Object { Write-Output "   | $_" }
            Get-Content -LiteralPath $result.err -Tail 10 | ForEach-Object { Write-Output "   ! $_" }
        }
    }
}
$total = [int]$clock.Elapsed.TotalMilliseconds
[IO.File]::WriteAllText((Join-Path $Scratch 'f1-powershell.ms'), "$total`n")
Write-Output "F1-PS total $count runs ($($suites.Count) suites x $($runtimes.Count) runtimes) $total ms"
if ($failed) { Write-Output 'FAILED: F1 PowerShell suites'; exit 1 }
Write-Output 'F1-PS all suites passed on both runtimes'
exit 0
