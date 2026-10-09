# Ruling 182 reusable Windows runner. Dot-source for guarded child execution.
# Ring: Windows runner preparation/self-test; no product checks. Child ceiling 60 s.
param([Alias('Action')][ValidateSet('Ready')][string]$RunnerAction='Ready', [Alias('PythonPath')][string]$RunnerPythonPath)
$ErrorActionPreference = 'Stop'

if (-not ('WriJob' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
public sealed class WriJob : IDisposable {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct STARTUPINFO {
        public int cb; public string reserved, desktop, title;
        public int x,y,xSize,ySize,xChars,yChars,fill,flags; public short show,reservedSize;
        public IntPtr reservedPtr,input,output,error;
    }
    [StructLayout(LayoutKind.Sequential)] struct PROCESSINFO { public IntPtr process,thread; public int pid,tid; }
    [StructLayout(LayoutKind.Sequential)] struct SECURITY { public int size; public IntPtr descriptor; public int inherit; }
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateJobObject(IntPtr attrs,string name);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(IntPtr job,int kind,IntPtr info,int size);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool QueryInformationJobObject(IntPtr job,int kind,IntPtr info,int size,IntPtr returned);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateJobObject(IntPtr job,uint exit);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateProcess(IntPtr process,uint exit);
    [DllImport("kernel32.dll",SetLastError=true)] static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateFile(string name,uint access,uint share,ref SECURITY attrs,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string app,StringBuilder command,IntPtr processAttrs,IntPtr threadAttrs,bool inherit,uint flags,IntPtr env,string cwd,ref STARTUPINFO startup,out PROCESSINFO process);
    IntPtr job, rootNative; public Process Root { get; private set; }
    public bool Assigned { get; private set; }
    static void Require(bool ok,string operation) { if(!ok) throw new Win32Exception(Marshal.GetLastWin32Error(), operation); }
    static string Quote(string value) {
        var result=new StringBuilder("\""); int slashes=0;
        foreach(char c in value) { if(c=='\\') {slashes++;continue;} if(c=='\"') {result.Append('\\',slashes*2+1);result.Append(c);} else {result.Append('\\',slashes);result.Append(c);} slashes=0; }
        result.Append('\\',slashes*2); return result.Append('"').ToString();
    }
    public WriJob() {
        if(IntPtr.Size!=8) throw new PlatformNotSupportedException("WRI requires Windows x64");
        job=CreateJobObject(IntPtr.Zero,null); Require(job!=IntPtr.Zero,"CreateJobObject");
        IntPtr limits=Marshal.AllocHGlobal(144);
        try { for(int i=0;i<144;i++) Marshal.WriteByte(limits,i,0); Marshal.WriteInt32(limits,16,0x2000); Require(SetInformationJobObject(job,9,limits,144),"kill-on-close job limit"); }
        catch { Dispose(); throw; } finally { Marshal.FreeHGlobal(limits); }
    }
    public void Start(string exe,string[] args,string cwd,string stdout,string stderr,bool injectAssignmentFailure) {
        SECURITY security=new SECURITY {size=Marshal.SizeOf<SECURITY>(),inherit=1};
        IntPtr input=CreateFile("NUL",0x80000000,3,ref security,3,0,IntPtr.Zero);
        IntPtr output=CreateFile(stdout,0x40000000,7,ref security,2,0,IntPtr.Zero);
        IntPtr error=CreateFile(stderr,0x40000000,7,ref security,2,0,IntPtr.Zero);
        PROCESSINFO child=new PROCESSINFO();
        try {
            Require(input!=new IntPtr(-1)&&output!=new IntPtr(-1)&&error!=new IntPtr(-1),"capture files");
            STARTUPINFO startup=new STARTUPINFO {cb=Marshal.SizeOf<STARTUPINFO>(),flags=0x100,input=input,output=output,error=error};
            var command=new StringBuilder(Quote(exe)); foreach(string argument in args) command.Append(' ').Append(Quote(argument));
            // CREATE_SUSPENDED | CREATE_NO_WINDOW: the job owns the child before its first instruction.
            Require(CreateProcess(exe,command,IntPtr.Zero,IntPtr.Zero,true,0x08000004,IntPtr.Zero,cwd,ref startup,out child),"CreateProcess suspended");
            rootNative=child.process; child.process=IntPtr.Zero;
            Root=Process.GetProcessById(child.pid); IntPtr retained=Root.Handle;
            if(injectAssignmentFailure) throw new InvalidOperationException("WRI-ASSIGNMENT: injected assignment failure");
            Require(AssignProcessToJobObject(job,rootNative),"AssignProcessToJobObject before resume"); Assigned=true;
            Require(ResumeThread(child.thread)!=0xffffffff,"ResumeThread");
        } finally {
            // Keep rootNative and Root alive on assignment failure. TerminateProcess is asynchronous;
            // the PowerShell cleanup helper requests it and observes Root.WaitForExit with the shared deadline.
            // https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess
            if(child.thread!=IntPtr.Zero) CloseHandle(child.thread);
            if(child.process!=IntPtr.Zero) CloseHandle(child.process);
            if(input!=new IntPtr(-1)) CloseHandle(input); if(output!=new IntPtr(-1)) CloseHandle(output); if(error!=new IntPtr(-1)) CloseHandle(error);
        }
    }
    public void RequestUnassignedTermination() {
        if(Assigned||Root==null) throw new InvalidOperationException("WRI-ASSIGNMENT: no unassigned root");
        Require(TerminateProcess(rootNative,125),"TerminateProcess unassigned root");
    }
    public int ActiveProcesses {
        get { IntPtr accounting=Marshal.AllocHGlobal(48); try { Require(QueryInformationJobObject(job,1,accounting,48,IntPtr.Zero),"job active-process accounting"); return Marshal.ReadInt32(accounting,40); } finally { Marshal.FreeHGlobal(accounting); } }
    }
    public void Terminate() { Require(TerminateJobObject(job,124),"TerminateJobObject"); }
    public void Dispose() { if(job!=IntPtr.Zero) {CloseHandle(job);job=IntPtr.Zero;} if(rootNative!=IntPtr.Zero) {CloseHandle(rootNative);rootNative=IntPtr.Zero;} if(Root!=null) {Root.Dispose();Root=null;} }
}
'@
}

# Adapted from docs/proof/ring-windows/capture-calibration.ps1: remaining Stopwatch budget,
# never a parsed wall-clock deadline. UTC timestamps may describe a run, never govern it.
function Get-WriRemainingMilliseconds([long]$CeilingMs, [long]$ElapsedMs) {
    return [math]::Max(0, $CeilingMs - $ElapsedMs)
}
function Wait-WriDeadline($Process, [long]$CeilingMs, [long]$ElapsedMs) {
    $remainingMs = Get-WriRemainingMilliseconds $CeilingMs $ElapsedMs
    if ($remainingMs -le 0) { return $false }
    return [bool]$Process.WaitForExit([int]$remainingMs)
}
function Assert-WriNumericExit($Value) {
    if ($Value -isnot [int]) { throw 'WRI-EXIT: child exit must be an observed Int32' }
}
function Invoke-WriGit([string]$Repo, [string[]]$Arguments, [Diagnostics.Stopwatch]$Clock, [long]$DeadlineMs) {
    $gitPath = (Get-Command git -CommandType Application).Source
    return Invoke-WriProcess -Repo $Repo -Exe $gitPath -Arguments (@('-C',$Repo) + $Arguments) -Clock $Clock -CeilingMs $DeadlineMs
}
function Get-WriSourceFingerprint([string]$Repo, [Diagnostics.Stopwatch]$Clock=[Diagnostics.Stopwatch]::StartNew(), [long]$DeadlineMs=60000) {
    $listing = Invoke-WriGit $Repo @('ls-files','--','src','tests','tools') $Clock $DeadlineMs
    if ($listing.ExitCode -ne 0) { throw 'WRI-SOURCE: git listing failed' }
    $files = @($listing.Stdout.Trim() -split '\r?\n' | Where-Object { $_ })
    $rows = foreach ($file in $files) {
        Assert-WriEnvelope $Clock $DeadlineMs
        $path = Join-Path $Repo $file
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'WRI-SOURCE: tracked source missing' }
        "$file=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
    }
    return ($rows -join "`n")
}
function Assert-WriSourceClean([string]$Repo, [Diagnostics.Stopwatch]$Clock=[Diagnostics.Stopwatch]::StartNew(), [long]$DeadlineMs=60000) {
    $diff = Invoke-WriGit $Repo @('diff','--quiet','HEAD','--','src','tests','tools') $Clock $DeadlineMs
    if ($diff.ExitCode -ne 0) { throw 'WRI-SOURCE: tracked source differs from HEAD' }
    $untracked = Invoke-WriGit $Repo @('ls-files','--others','--exclude-standard','--','src','tests','tools') $Clock $DeadlineMs
    if ($untracked.ExitCode -ne 0 -or $untracked.Stdout.Trim()) { throw 'WRI-SOURCE: untracked source or unreadable status' }
}
function Assert-WriSourceUnchanged([string]$Repo, [string]$Before, [Diagnostics.Stopwatch]$Clock=[Diagnostics.Stopwatch]::StartNew(), [long]$DeadlineMs=60000) {
    if ((Get-WriSourceFingerprint $Repo $Clock $DeadlineMs) -cne $Before) { throw 'WRI-SOURCE: bytes changed during child execution' }
    Assert-WriSourceClean $Repo $Clock $DeadlineMs
}
function Assert-WriEnvelope([Diagnostics.Stopwatch]$Clock, [long]$DeadlineMs) {
    if ($Clock.ElapsedMilliseconds -ge $DeadlineMs) { throw 'WRI-ENVELOPE: total startup/child/cleanup/source envelope expired' }
}
function Stop-WriJob($Job, [Diagnostics.Stopwatch]$Clock, [long]$DeadlineMs, [switch]$InjectFailure) {
    if ($InjectFailure) { throw 'WRI-CLEANUP: injected termination failure; completion not verified' }
    $Job.Terminate()
    while ($Job.ActiveProcesses -gt 0) {
        Assert-WriEnvelope $Clock $DeadlineMs
        [Threading.Thread]::Sleep(1)
    }
    return 'terminated-complete'
}
function Stop-WriUnassignedProcess($Job, [Diagnostics.Stopwatch]$Clock, [long]$DeadlineMs, [switch]$InjectFailure) {
    if ($InjectFailure) { throw 'WRI-ASSIGNMENT: cleanup=unresolved-request-failed' }
    if (-not $Job.Root.HasExited) { $Job.RequestUnassignedTermination() }
    if (-not (Wait-WriDeadline $Job.Root $DeadlineMs $Clock.ElapsedMilliseconds)) { throw 'WRI-ASSIGNMENT: cleanup=unresolved-termination-timeout' }
    Assert-WriNumericExit $Job.Root.ExitCode
    return 'terminated-complete'
}
function Add-WriEvent($Events, [string]$Event) {
    if ($null -ne $Events) { [void]$Events.Add($Event) }
}
function Invoke-WriLifecycleShutdown([string]$Repo, $Toolchain, [string[]]$SelfTestShutdownArguments,
    [Diagnostics.Stopwatch]$Clock, [long]$DeadlineMs, $Events) {
    if (-not $Toolchain) { throw 'WRI-SHUTDOWN: build/verifier mode requires explicit pinned toolchain' }
    $shutdownArguments = @('build-server','shutdown')
    if ($SelfTestShutdownArguments.Count -gt 0) { $shutdownArguments=$SelfTestShutdownArguments }
    Add-WriEvent $Events 'shutdown-start'
    $shutdown = Invoke-WriProcess -Repo $Repo -Exe $Toolchain.Dotnet -Arguments $shutdownArguments -Clock $Clock -CeilingMs $DeadlineMs
    Assert-WriNumericExit $shutdown.ExitCode
    Add-WriEvent $Events "shutdown-exit:$($shutdown.ExitCode)"
    if ($shutdown.ExitCode -ne 0) { throw 'WRI-SHUTDOWN: numeric shutdown failed; target residual query withheld; job-close cleanup requested, completion unverified' }
    return $shutdown.ExitCode
}
function Read-WriRawCapture([string]$Path) {
    $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $memory=[IO.MemoryStream]::new()
    try { $stream.CopyTo($memory); $bytes=$memory.ToArray() }
    finally { $memory.Dispose(); $stream.Dispose() }
    return [pscustomobject]@{Text=[Text.Encoding]::UTF8.GetString($bytes);Sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()}
}
function Invoke-WriProcess {
    param([string]$Repo, [string]$Exe, [string[]]$Arguments,
          [Diagnostics.Stopwatch]$Clock, [long]$CeilingMs, [int]$ChildCeilingMs=60000,
          [switch]$InjectTerminationFailure, [int]$InjectStartupDelayMs=0,
          [switch]$InjectAssignmentFailure,
          [ValidateSet('Normal','BuildVerifier')][string]$Mode='Normal', $Toolchain,
          [string[]]$SelfTestShutdownArguments=@(), $Events)
    $entryElapsed = $Clock.ElapsedMilliseconds
    $deadline = [math]::Min($CeilingMs, $entryElapsed + $ChildCeilingMs)
    # Reserve half of this envelope for cleanup, PHN, and the final source proof. This is an allowance,
    # not measured capacity. Root wait, descendants, and final checks all consume the same clock.
    $rootDeadline = $entryElapsed + [math]::Floor(($deadline - $entryElapsed) * 0.5)
    Assert-WriEnvelope $Clock $deadline
    $rawDir = Join-Path ([IO.Path]::GetTempPath()) ('cfd-wri-raw-' + [guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($rawDir)
    $job = [WriJob]::new()
    $childClock = [Diagnostics.Stopwatch]::StartNew()
    $cleanup = 'not-recorded'
    $allowResidual = $Mode -eq 'Normal'
    $shutdownExit = $null
    try {
        if ($InjectStartupDelayMs) {
            [Threading.Thread]::Sleep([int][math]::Min($InjectStartupDelayMs,(Get-WriRemainingMilliseconds $rootDeadline $Clock.ElapsedMilliseconds)))
        }
        Assert-WriEnvelope $Clock $rootDeadline
        try {
            $job.Start($Exe,$Arguments,$Repo,(Join-Path $rawDir 'stdout.raw'),(Join-Path $rawDir 'stderr.raw'),[bool]$InjectAssignmentFailure)
        } catch {
            if ($job.Root -and -not $job.Assigned) {
                Add-WriEvent $Events "unassigned-pid:$($job.Root.Id)"
                try { $cleanup = Stop-WriUnassignedProcess $job $Clock $deadline -InjectFailure:$InjectTerminationFailure }
                catch {
                    # The initial cleanup failure stays explicit even when a bounded fallback completes.
                    try { $fallback = Stop-WriUnassignedProcess $job $Clock $deadline }
                    catch { throw "WRI-ASSIGNMENT: cleanup=unresolved-bounded-termination-failed process_id=$($job.Root.Id)" }
                    throw "WRI-ASSIGNMENT: cleanup=unresolved-request-failed fallback=$fallback"
                }
                throw "WRI-ASSIGNMENT: launch not resumed; cleanup=$cleanup"
            }
            throw
        }
        $child = $job.Root
        $handle = $child.Handle
        $completed = Wait-WriDeadline $child $rootDeadline $Clock.ElapsedMilliseconds
        if (-not $completed) {
            if ($Mode -eq 'BuildVerifier') {
                Add-WriEvent $Events 'target-timeout'
                $shutdownExit = Invoke-WriLifecycleShutdown $Repo $Toolchain $SelfTestShutdownArguments $Clock $deadline $Events
                $allowResidual = $true
            }
            $cleanup = Stop-WriJob $job $Clock $deadline -InjectFailure:$InjectTerminationFailure
            throw "WRI-DEADLINE: child timed out; cleanup=$cleanup; no exit fabricated"
        }
        $exitCode = $child.ExitCode
        Assert-WriNumericExit $exitCode
        Add-WriEvent $Events "target-root-exit:$exitCode"
        if ($Mode -eq 'BuildVerifier') {
            $shutdownExit = Invoke-WriLifecycleShutdown $Repo $Toolchain $SelfTestShutdownArguments $Clock $deadline $Events
            $allowResidual = $true
        }
        Add-WriEvent $Events 'target-job-query'
        $rootExitDescendants = $job.ActiveProcesses
        if ($rootExitDescendants -gt 0) {
            Add-WriEvent $Events 'target-job-terminate'
            $cleanup = Stop-WriJob $job $Clock $deadline -InjectFailure:$InjectTerminationFailure
        } else { $cleanup = 'none-active-verified' }
        $stdout = Read-WriRawCapture (Join-Path $rawDir 'stdout.raw')
        $stderr = Read-WriRawCapture (Join-Path $rawDir 'stderr.raw')
        # Internal result only: callers must PHN-check derivatives before returning or publishing text.
        $result = [pscustomobject]@{ExitCode=$exitCode; TimedOut=$false; RetainedHandle=($handle -ne [IntPtr]::Zero);
            WallMs=$childClock.ElapsedMilliseconds; Stdout=$stdout.Text; Stderr=$stderr.Text; RootExitDescendants=$rootExitDescendants;
            Cleanup=$cleanup; ResidualStatus='job-active-zero-verified'; RawStdoutSha256=$stdout.Sha256;
            RawStderrSha256=$stderr.Sha256;BuildServerShutdownExit=$shutdownExit;Mode=$Mode}
        return $result
    } finally {
        try {
            if ($allowResidual -and $job.ActiveProcesses -gt 0) {
                # Fail closed: a cleanup failure is accounted for. Job close still requests kernel cleanup,
                # but only observed active=0 earns completion; close alone is never declared verified.
                $cleanup = Stop-WriJob $job $Clock $deadline -InjectFailure:$InjectTerminationFailure
            }
        } finally {
            Add-WriEvent $Events 'target-job-close'
            $job.Dispose()
            $resolved = [IO.Path]::GetFullPath($rawDir)
            if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)) { throw 'WRI-CAPTURE: raw directory escaped temporary root' }
            Remove-Item -LiteralPath $resolved -Recurse -Force
            Assert-WriEnvelope $Clock $deadline
        }
    }
}
function Invoke-WriChild {
    param([string]$Repo, [string]$Exe, [string[]]$Arguments,
          [Diagnostics.Stopwatch]$Clock, [long]$CeilingMs, [int]$ChildCeilingMs=60000,
          [switch]$InjectTerminationFailure, [int]$InjectStartupDelayMs=0,
          [string]$PythonPath=$env:CFD_WRI_PYTHON,
          [switch]$InjectAssignmentFailure,
          [ValidateSet('Normal','BuildVerifier')][string]$Mode='Normal', $Toolchain,
          [string[]]$SelfTestShutdownArguments=@(), $Events)
    $deadline = [math]::Min($CeilingMs, $Clock.ElapsedMilliseconds + $ChildCeilingMs)
    Assert-WriSourceClean $Repo $Clock $deadline
    $before = Get-WriSourceFingerprint $Repo $Clock $deadline
    try {
        $result = Invoke-WriProcess -Repo $Repo -Exe $Exe -Arguments $Arguments -Clock $Clock -CeilingMs $deadline -ChildCeilingMs $ChildCeilingMs -InjectTerminationFailure:$InjectTerminationFailure -InjectStartupDelayMs $InjectStartupDelayMs -InjectAssignmentFailure:$InjectAssignmentFailure -Mode $Mode -Toolchain $Toolchain -SelfTestShutdownArguments $SelfTestShutdownArguments -Events $Events
        if (-not $PythonPath -or -not [IO.Path]::IsPathFullyQualified($PythonPath)) { throw 'WRI-CAPTURE: absolute Python required for PHN before publication' }
        $capture = Join-Path ([IO.Path]::GetTempPath()) ('cfd-wri-capture-' + [guid]::NewGuid().ToString('N') + '.json')
        try {
            [IO.File]::WriteAllText($capture,(@{stdout=$result.Stdout;stderr=$result.Stderr} | ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
            $checked = Invoke-WriProcess -Repo $Repo -Exe $PythonPath -Arguments @((Join-Path $PSScriptRoot 'check-windows-runner.py'),'--scrub-capture',$capture) -Clock $Clock -CeilingMs $deadline
            if ($checked.ExitCode -ne 0) { throw 'WRI-CAPTURE: PHN rejected derivative; raw text withheld' }
            $derivative = $checked.Stdout | ConvertFrom-Json
            $result.Stdout=$derivative.stdout; $result.Stderr=$derivative.stderr
            $result | Add-Member NoteProperty PhnStatus 'PASS'
            $result | Add-Member NoteProperty SubstitutionBinding $derivative.substitutions
        } finally { Remove-Item -LiteralPath $capture -Force }
        return $result
    } finally {
        Assert-WriSourceUnchanged $Repo $before $Clock $deadline
        Assert-WriEnvelope $Clock $deadline
    }
}
function Assert-WriToolchain {
    param([string]$Repo, [string]$PythonPath, [string]$DotnetPath=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
    # Adapted from docs/proof/ring-windows/capture.ps1:14-29. Never PATH-resolve dotnet.
    $sdkRoot = Join-Path $env:USERPROFILE '.dotnet'
    $expectedDotnet = [IO.Path]::GetFullPath((Join-Path $sdkRoot 'dotnet.exe'))
    if (-not [IO.Path]::IsPathFullyQualified($DotnetPath) -or
        [IO.Path]::GetFullPath($DotnetPath) -ne $expectedDotnet -or
        -not (Test-Path -LiteralPath $expectedDotnet -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $sdkRoot 'sdk/10.0.203') -PathType Container)) {
        throw 'WRI-TOOLCHAIN: expected user-local .dotnet/dotnet.exe and SDK 10.0.203'
    }
    if (-not $PythonPath -or -not [IO.Path]::IsPathFullyQualified($PythonPath) -or
        -not (Test-Path -LiteralPath $PythonPath -PathType Leaf)) { throw 'WRI-TOOLCHAIN: absolute Python executable required' }
    $env:DOTNET_ROOT=$sdkRoot
    $env:PATH="$sdkRoot;$env:PATH"
    $env:DOTNET_MULTILEVEL_LOOKUP='0'
    $identityClock = [Diagnostics.Stopwatch]::StartNew()
    $sdkResult = Invoke-WriProcess -Repo $Repo -Exe $expectedDotnet -Arguments @('--version') -Clock $identityClock -CeilingMs 60000
    $sdkVersion = $sdkResult.Stdout.Trim()
    $sdkExit = $sdkResult.ExitCode
    if ($sdkExit -ne 0 -or $sdkVersion -ne '10.0.203') { throw 'WRI-TOOLCHAIN: SDK version must be exactly 10.0.203' }
    $pythonResult = Invoke-WriProcess -Repo $Repo -Exe $PythonPath -Arguments @('-c','import sys; print(sys.version.split()[0]); print(sys.executable)') -Clock $identityClock -CeilingMs 60000
    $pythonVersion = @($pythonResult.Stdout.Trim() -split '\r?\n')
    $pythonExit = $pythonResult.ExitCode
    if ($pythonExit -ne 0 -or $pythonVersion.Count -ne 2 -or
        [IO.Path]::GetFullPath($pythonVersion[1]) -ne [IO.Path]::GetFullPath($PythonPath)) { throw 'WRI-TOOLCHAIN: Python identity mismatch' }
    $env:CFD_WRI_PYTHON=$PythonPath
    return [pscustomobject]@{Dotnet=$expectedDotnet;Sdk=$sdkVersion;Python=$PythonPath;PythonVersion=$pythonVersion[0];SdkExit=$sdkExit;PythonExit=$pythonExit}
}
function Assert-WriSettingsState($State) {
    if (-not $State.Frame -or -not $State.Selected -or -not $State.Expanded -or
        -not $State.Main -or -not $State.Visible -or $State.Scale -ne '150% (Recommended)') {
        throw 'WRI-PREFLIGHT: Settings must already expose selected Display1, main monitor, and visible 150% scale'
    }
}
function Stop-WriBuildServers([string]$Repo, $Toolchain, [Diagnostics.Stopwatch]$Clock, [long]$CeilingMs) {
    $result = Invoke-WriChild -Repo $Repo -Exe $Toolchain.Dotnet -Arguments @('build-server','shutdown') -Clock $Clock -CeilingMs $CeilingMs
    if ($result.ExitCode -ne 0) { throw 'WRI-SHUTDOWN: build-server shutdown failed; residual sampling forbidden' }
    # Any future residual-process sampler must consume this successful numeric shutdown result first.
    return $result
}
if ($MyInvocation.InvocationName -eq '.') { return }
if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) { throw 'WRI-PLATFORM: PowerShell 7 on Windows required' }
$repo = Split-Path -Parent $PSScriptRoot
Assert-WriSourceClean $repo
$baseline = Get-WriSourceFingerprint $repo
$toolchain = Assert-WriToolchain -Repo $repo -PythonPath $RunnerPythonPath
& $toolchain.Python (Join-Path $PSScriptRoot 'check-windows-runner.py')
if ($LASTEXITCODE -ne 0) { throw 'WRI-POLICY: runner guard failed' }
$clock = [Diagnostics.Stopwatch]::StartNew()
try {
    $preflight = Invoke-WriChild -Repo $repo -Exe (Get-Process -Id $PID).Path -Arguments @('-NoProfile','-File',(Join-Path $PSScriptRoot 'windows-settings-preflight.ps1'),'-Action','Preflight') -Clock $clock -CeilingMs 1200000
    if ($preflight.ExitCode -ne 0) { throw 'WRI-PREFLIGHT: read-only Settings child rejected current state' }
    $preflight.Stdout
    "RUNNER_READY sdk=$($toolchain.Sdk) numeric_preflight_exit=$($preflight.ExitCode) child_ceiling_ms=60000 scale_change=false contract_execution=false"
} finally { Assert-WriSourceUnchanged $repo $baseline }
