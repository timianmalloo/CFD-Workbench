---
id: proof-win-setup
title: "W-0 Windows setup host survey"
type: proof-pack
status: in-review
owner: "@timianmalloo"
tags: [windows, w-0, host-survey]
links:
  - { to: coordination-pc-kickoff, rel: relates-to }
review-by: 2026-11-06
summary: >-
  W-0 host survey for the Windows PC. Rows are filled only from command output
  observed in this session. Disposition is Verified, Failed, or Not recorded.
---

# W-0 Windows setup receipt

Observed on 2026-10-07 after the evidence worker resumed following a server restart.
Worktree: `C:\Projects\CFD-Workbench-win-setup`; branch: `win/setup`.
Surveyed base HEAD: `d0fc7fde4a71afe0cb049f93330ca4b6977a5c83`.
The original brief named `7102e90fd04d70ea818acd551ad295e0b62bf93c`;
the coordinator fast-forwarded this branch to the surveyed HEAD before resumption.

Goal: complete the W-0 host survey and record tool/coordination readiness.
Done when every required row has an observed disposition and the owned receipt is committed.
Application builds, tests, Linux installation and W-1 onward remain outside this evidence task.
All Verified rows below come from command output read during this resumed run.

## Disposition

| Item | Disposition | Observed result |
|---|---|---|
| Windows edition/build | Verified | `Win32_OperatingSystem`: Microsoft Windows 11 Pro, version 10.0.26300, build 26300. Registry UBR: 9457; display version 26H2. `wsl --version` also reports 10.0.26300.9457. Registry ProductName still says Windows 10 Pro; the OS caption and `systeminfo` say Windows 11 Pro. |
| Architecture | Verified | `systeminfo`: x64-based PC; OSArchitecture: 64-bit. |
| CPU model and cores | Verified | 12th Gen Intel(R) Core(TM) i9-12900H; 14 physical cores; 20 logical processors. |
| RAM | Verified | `Win32_ComputerSystem.TotalPhysicalMemory`: 34,009,374,720 bytes. OS TotalVisibleMemorySize: 33,212,280 KiB; FreePhysicalMemory: 17,461,752 KiB at the initial sample. |
| Fixed-volume disk space | Verified | `Win32_LogicalDisk` DriveType=3 returned only C:. Size: 2,046,020,284,416 bytes; free: 1,858,804,023,296 bytes. Repo and user-local SDK are on C:. |
| Hypervisor | Verified | `Win32_ComputerSystem.HypervisorPresent=true`; `Get-ComputerInfo.HyperVisorPresent=true`. `systeminfo`: a hypervisor has been detected; virtualization-based security Running. |
| Firmware virtualization flag | Not recorded | First query: `Win32_Processor.VirtualizationFirmwareEnabled=false`, SLAT=false, VMMonitorModeExtensions=false. One follow-up: `Get-ComputerInfo.HyperVRequirementVirtualizationFirmwareEnabled=null`; `systeminfo` says Hyper-V requirement features are not displayed because a hypervisor is detected. No independent firmware setting was obtained; do not treat these conflicting/masked fields as a verified firmware-off state. |
| Reboot-pending indicators | Verified | CBS RebootPending=false; WindowsUpdate RebootRequired=false; PendingFileRenameOperations property absent; Microsoft Updates key absent (no UpdateExeVolatile value); configured and active computer names equal. These queried indicators show no pending reboot; they do not prove every installer-specific reboot state. Last boot: 2026-10-06 06:29:11.5 -07:00. |
| WSL package/runtime | Verified | Appx package MicrosoftCorporationII.WindowsSubsystemForLinux 2.7.14.0, Status=0; `wsl --version`: 2.7.14.0, kernel 6.18.33.2-2, WSLg 1.0.73.2, MSRDC 1.2.7214. |
| WSL default version | Verified | `wsl --status`: Default Version 2; also warns WSL1 is unsupported until its optional Windows component is enabled. Exit 0. |
| WSL distributions | Verified | `wsl --list --verbose`: Windows Subsystem for Linux has no installed distributions. Exit -1. Repeated once with the same result. No distro was installed by this worker. |
| WSL registration repair history | Not recorded | Coordinator reports it repaired WSL registration to package 2.7.14.0 before this resumed run. Current registration/version is independently Verified above; the repair action itself was not rerun or directly observed by this worker. |
| GPU/VRAM/driver | Verified | `nvidia-smi`: NVIDIA GeForce RTX 3080 Ti Laptop GPU, 16384 MiB, driver 596.47. CIM Windows driver version: 32.0.15.9647. Also present: Intel(R) Iris(R) Xe Graphics, driver 32.0.101.7085. |
| Git | Verified | git version 2.54.0.windows.1. `core.autocrlf=false`, source C:/Users/malla/.gitconfig. |
| Python | Verified | `py -3 --version`: Python 3.13.14. |
| .NET SDK | Verified | Exact user-local executable and refreshed plain `dotnet --version` both return 10.0.203. `global.json`: version 10.0.203, rollForward disable. |
| Persisted .NET environment | Verified | User DOTNET_ROOT=C:\Users\malla\.dotnet; persisted User PATH begins C:\Users\malla\.dotnet. Inherited process initially resolved C:\Program Files\dotnet\dotnet.exe; after refresh it resolves C:\Users\malla\.dotnet\dotnet.exe. |
| GitHub CLI/auth | Verified | gh 2.94.0 (2026-06-10); github.com account timianmalloo, keyring, active=true, Git protocol https; scopes gist/read:org/repo/workflow. No token value is recorded. |
| Coordination install state | Verified | Local merge.coord-regen.driver and merge.coord-register.driver registered to Python313 and the primary checkout's coord-core.py. `.gitattributes` contains 9 coordination patterns. No install was needed or rerun during resumption. |
| Coordination doctor | Verified | Registry ok: 9 patterns. coord-regen and coord-register declared, registered and effective. No leader designated; heartbeat and requests not recorded; no live lease overlap. Harness capability section explicitly describes historical spikes, not measurements here. |
| Repository state | Verified | Branch win/setup; HEAD d0fc7fde4a71afe0cb049f93330ca4b6977a5c83. Initial status: only untracked docs/proof/win-setup/. Origin fetch/push: https://github.com/timianmalloo/CFD-Workbench.git. |
| Documentation check | Failed | `py -3 tools/check-docs.py` exits 1 on the initial run and its one repeat after completing this receipt. Graph validation reports 1 defect: `file not in index: proof-win-setup`; 0 problems, 0 orphans; 131 review-suggested warnings. This task excludes docs-index regeneration. |

## Commands and results

Native command exits were recorded directly after execution. PowerShell cmdlet rows
above record returned values or explicit errors rather than assigning a native exit code.

| Command | Exit | Result read |
|---|---:|---|
| `git --version` | 0 | 2.54.0.windows.1 |
| `git config --get core.autocrlf` | 0 | false |
| `py -3 --version` | 0 | Python 3.13.14 |
| `gh --version` | 0 | 2.94.0 |
| `gh auth status` | 0 | Active authenticated account and scopes, as above |
| `dotnet --version` after refresh | 0 | 10.0.203 |
| `wsl --version` | 0 | Runtime 2.7.14.0 and component versions |
| `wsl --status` | 0 | Default Version 2; WSL1 warning |
| `wsl --list --verbose` | -1 | Explicit no-installed-distributions message, including on one repeat |
| `nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader` | 0 | GPU, 16384 MiB, driver 596.47 |
| `py -3 docs/ai-forward-pack/scripts/coord-core.py doctor` | 0 | Registry ok, 9 patterns; both merge drivers effective; historical harness caveats preserved above |
| `py -3 tools/check-docs.py` | 1 | Graph index drift for this new receipt; CalledProcessError identifies docs-graph.py validate exit 1 |

The explicit SDK command was
`& 'C:\Users\malla\.dotnet\dotnet.exe' --version`, which printed 10.0.203.
Before the plain command, this process environment was refreshed from persisted values:

```powershell
$env:DOTNET_ROOT=[Environment]::GetEnvironmentVariable('DOTNET_ROOT','User')
$env:Path=[Environment]::GetEnvironmentVariable('Path','User')+';'+[Environment]::GetEnvironmentVariable('Path','Machine')
(Get-Command dotnet).Source
dotnet --version
```

The host survey used `Get-CimInstance` for Win32_OperatingSystem, Win32_Processor,
Win32_ComputerSystem, Win32_LogicalDisk (DriveType=3), and Win32_VideoController;
`Get-ItemProperty` for CurrentVersion/UBR and the reboot registry indicators;
`Get-AppxPackage '*WindowsSubsystemForLinux*'`; `Get-ComputerInfo` and `systeminfo`.
Initial named-property reads for PendingFileRenameOperations and UpdateExeVolatile
reported property/path absence. One follow-up checked existence explicitly and confirmed
both absences. No repair, install or restart was attempted.

## Remaining gate

The check's `SPIRAL` message occurred inside its self-test output; the same output
continues through `self-test OK`. The observed blocking result is graph index drift.

The coordinator must regenerate the excluded Docs Explorer index before a green docs
gate can be claimed. Direct firmware virtualization state remains Not recorded with
the contradictory query results preserved. These are the residual limits of this receipt;
it does not claim a successful Windows application build, test run or Linux solver route.
