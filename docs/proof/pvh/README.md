---
id: proof-pvh-crash-loop
title: PVH crash loop - the properties-view part 2/2 native crash did not reproduce, and a native exit is now named as one
type: proof-pack
status: draft
owner: "@trk-d1"
tags: [pvh, desktop-tests, native-crash, harness-crash-as-verdict]
links:
  - { to: proof-pvh-red-first, rel: relates-to }
review-by: 2026-11-21
summary: >-
  Not reproduced in 6 runs of the Desktop harness alone and 6 runs of the full suite (0 crashes in 12); cause not found. The harness now prints a native child exit as a native crash with its NTSTATUS hex or signal, pinned by a check seen red then green.
---
# PVH crash loop (track D1, 2026-10-22)

Branch `d1-desktop-diagnostics`, base `176c9540`. Machine: Windows ARM64 laptop, real display, 200 percent. Earlier work:
`docs/proof/pvh/red-first.md` (the hang track: 0 hangs in 104 Mac runs).

## The evidence being chased (2026-10-22 join, Verified from its saved log)

- The child `--properties-view --part=2/2` exited **-1073740940 = 0xC0000374, STATUS_HEAP_CORRUPTION** after 56 s, having printed no checks.
- The PARTITION check then failed. Totals: 778 PASS, 2 FAIL, 23 SKIP.
- Run standalone, the same child passed 3 of 3 (34 PASS each).

This is a native crash under concurrency, not a hang. A standalone loop of the one part would not see it, so two loops were run.

## Commands

    E=tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.exe
    $E --harness                      # loop A: the whole Desktop harness (all children in parallel), stdout and stderr to separate files
    bash tools/run-tests.sh           # loop B: four test projects at once; Desktop.log copied after each run

## Loop A: Desktop harness alone (6 runs)

| run | harness exit | `SUITE --properties-view --part=2/2` | crashed child | exit hex | stderr tail |
|---|---|---|---|---|---|
| 1 | 0 | exit 0 | none | - | (none) |
| 2 | 0 | exit 0 | none | - | (none) |
| 3 | 0 | exit 0 | none | - | (none) |
| 4 | 0 | exit 0 | none | - | (none) |
| 5 | 0 | exit 0 | none | - | (none) |
| 6 | 0 | exit 0 | none | - | (none) |

Crash rate: **0 crashes / 6 runs**. No `SUITE ... exit <n>` line with n other than 0 in any run.

## Loop B: full suite (6 runs)

Runs 1 to 5 were on the unchanged formatter, run 6 on the final commit.

| run | verdict | seconds | non-zero child exits | crashed child | exit hex |
|---|---|---|---|---|---|
| 1 | TESTS-GREEN | 94 | none | none | - |
| 2 | TESTS-GREEN | 86 | none | none | - |
| 3 | TESTS-GREEN | 85 | none | none | - |
| 4 | TESTS-GREEN | 86 | none | none | - |
| 5 | TESTS-GREEN | 86 | none | none | - |
| 6 | TESTS-GREEN | 92 | none | none | - |

Crash rate: **0 crashes / 6 runs**. Run 6 project lines: Core 762 passed, 26 skipped; Analysis 260 passed; Desktop 782 passed,
23 skipped; Cli 19 passed, 2 skipped; 0 failed in each.

## Result

**Not reproduced in 6 runs of loop A and 6 runs of loop B.** 0 / 12 crashes. The Windows Application event log was not read
because no native crash fired, so there is no faulting module to record. **Cause not found.** Twelve clean runs do not prove the
crash is gone: the evidence is one crash in one join, and the rate is unknown. Nothing in this track points at our own unsafe
or native code, so no fix to the crash was attempted (an Avalonia, Skia or driver fault would be a finding, not a fix).

## The control: a native exit is named as one

Before, the harness printed `FAIL --properties-view --part=2/2 exited -1073740940`, which reads like a check verdict. Now
`DesktopChecks.ExitLine` prints, for a negative exit on Windows (an NTSTATUS, 0xC0000000 and up) or 129 to 255 on POSIX (128 plus
the signal): `FAIL <mode> exited -1073740940 (0xC0000374): native crash, not a check verdict`. Exits 124 (timeout) and 125 (drain)
keep their meaning. Note -1073740940 sits above -1073741000, so a "below -1073741000" threshold would have missed it.

The pure check `ExitLine_NativeTermination_NamedNotAVerdict` feeds -1073740940 and -1073741819 (Windows), 134 (POSIX), 124 and 1.

Red on the old formatter (Verified):

    FAIL ExitLine_NativeTermination_NamedNotAVerdict Exception: -1073740940/win: got 'FAIL m exited -1073740940' | -1073741819/win: got 'FAIL m exited -1073741819' | 134/posix: got 'FAIL m exited 134'

Green on the new:

    PASS ExitLine_NativeTermination_NamedNotAVerdict

The lesson is `HARNESS-CRASH-AS-VERDICT` in `docs/lessons/defect-classes.md`.

## Crash capture (CR1)

Goal: when a Desktop harness child dies with 0xC0000374, a full memory dump of it exists afterwards. Measured before this track:
Windows Error Reporting writes no event and no LocalDumps file for this fault, and the .NET runtime's createdump does not fire,
so the capture is a debugger attached from the child's first instruction: ProcDump 12.02 (`procdump64a.exe`, the ARM64 build) with `-x`.
Get it from Sysinternals (the Sysinternals Suite or the standalone ProcDump download at learn.microsoft.com/sysinternals/downloads/procdump).
It is not committed and is not a repo dependency. The wrapper value is split on spaces, so a wrapper path that contains a space is
not supported: copy ProcDump to a folder whose path has none.

The switch: `CFD_TEST_CHILD_WRAPPER` (unset in CI and in `tools/run-tests.sh`). `SelfLaunch.StartInfo` splits its value on spaces
(quote-free) into a program and leading arguments and starts the child as
`<program> <leading args> <process path> [entry assembly] --harness <mode>`. The wrapper applies to the suite modes only. Every mode
whose name contains `-probe` (`--startup-failure-probe`, `--inflight-probe`, `--drain-probe`, `--drain-probe-hold`) runs bare: those
modes assert their own exit code, process tree or pipe holders, and a wrapper replaces all three (a first wrapped run failed
`Unhandled_Exception_StderrHasNoMarkerPath` and `unhandled child exception exits with a named code` with `exit 0`; the drain readiness
check failed wrapped with `exit 125: (no DRAIN-PROBE line)`). `Drain_OrphanGrandchild_IsNamedAsThePipeHolder` passes with the
variable set (checked with a wrapper name that does not exist, which fails if the wrapper reaches a probe).
`tools/run-tests.sh` unsets both `CFD_TEST_CHILD_WRAPPER` and `CFD_FORCE_HEAP_CORRUPTION` before it builds.

Red first, then green, then the mutant (the wrapper's arguments after the mode instead of before the program):

    red     System.Exception: SelfLaunchTests failed:
              a child wrapper prefixes its program and arguments: C:/b/CfdWorkbench.Desktop.Tests.exe [--harness, --section-flow] != W [-a, -b, DIR, C:/b/CfdWorkbench.Desktop.Tests.exe, --harness, --section-flow]
    green   SelfLaunchTests: all 12 cases passed.   (11 when this was first run; the 12th pins the probe-mode exemption)
    mutant  a child wrapper prefixes its program and arguments: W [C:/b/CfdWorkbench.Desktop.Tests.exe, --harness, --section-flow, -a, -b, DIR] != W [-a, -b, DIR, C:/b/CfdWorkbench.Desktop.Tests.exe, --harness, --section-flow]

ProcDump's banner is UTF-16 text. On a pipe the parent reads it as lines of characters separated by NUL, and one NUL landed at the
start of a harness line (`\0SCALE_CONTEXT mode=...`): five modes then printed `FAIL SCALE_CONTEXT ... printed no SCALE_CONTEXT line`
and the PARTITION counts were lost. Red first: `Spawn_NulPrefixedChildLines_ParsedAsTheHarnessWroteThem` failed with
`FAIL SCALE_CONTEXT --properties-view --part=1/1 printed no SCALE_CONTEXT line before its first check` and
`FAIL PARTITION --properties-view parts are incomplete`; `DesktopChecks.SpawnWith` now removes NUL characters from a child's lines
before it parses or prints them, only when `CFD_TEST_CHILD_WRAPPER` is set (a bare child's line reaches the parser unchanged:
`Spawn_WrapperSet_NamesItOnce_BareLeavesNulsAlone`), and the check is green.

The child's exit code. ProcDump `-x` exits 0 when the child itself fails, so under the wrapper `SpawnWith` would read a child that
died as `exit 0` (measured in review: a selector failure exits 1 bare, 0 wrapped; an unhandled managed exception 70 bare, 0 wrapped).
ProcDump prints the child's code on its own output as `Process Exit: PID <n>, Exit Code 0x<hex>`. With the wrapper set,
`ChildWrapper.Read` (in `SelfLaunch.cs`) takes the last such line as the child's exit code, NULs ignored, and a missing line fails the
child: `FAIL CHILD-WRAPPER <mode>: no ProcDump "Process Exit" line in its output, so the child's exit code is unknown`. A wrapped
log opens with one `CHILD-WRAPPER active: <wrapper program name>` line, so it cannot pass for a bare verdict run. Pure checks
`ChildWrapper_ReadsTheChildsExitFromProcDumpsLine` and `ChildWrapper_NoExitLine_FailsTheChildByName` pin both.

    red     (parser stubbed to return the wrapper's code)
            FAIL ChildWrapper_ReadsTheChildsExitFromProcDumpsLine Exception: '[09:00:01]Process Exit: PID 4242, Exit Code 0x00000001': exit 0, failure ; expected 1
            FAIL ChildWrapper_NoExitLine_FailsTheChildByName Exception: exit 0, failure (none)
    green   PASS ChildWrapper_ReadsTheChildsExitFromProcDumpsLine / PASS ChildWrapper_NoExitLine_FailsTheChildByName
    mutant  (parsed code multiplied by 0)  FAIL ChildWrapper_ReadsTheChildsExitFromProcDumpsLine ... exit 0, failure ; expected 1
    real    CFD_TEST_ONLY=NoSuchCheck, wrapped, `--harness --theme-evidence`: `FAIL SELECTOR NoSuchCheck matched no check`,
            `[11:56:29]Process Exit: PID 20760, Exit Code 0x00000001`, `SUITE --theme-matrix exit 1`, `FAIL --theme-matrix exited 1`, run exit 1
            (bare, the same selector exits 1; before this reading it exited 0 wrapped)

End-to-end run. Reproduce from the repo root, in bash, after `dotnet build tests/CfdWorkbench.Desktop.Tests -c Release`:

    export CFD_TEST_CHILD_WRAPPER="<procdump64a.exe> -accepteula -ma -e 1 -f C0000374 -x <dump folder>"
    tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.exe --harness

Result of that first wrapped run (before the exit-code reading above): exit 0, no `FAIL` line, no `FAIL SCALE_CONTEXT` or `FAIL PARTITION`
line, no dump written. It did not show 18 green exits: the wrapper reports 0 whatever a child's code, and the exit code was not read
until the `Process Exit` parser. Cost, measured once on `--theme-matrix` (`--harness --theme-evidence`, one sample each):
SUITE-TIME 11.6 s bare against 13.0 s wrapped. A dump costs about 556 MB of disk each (see below); a loop must bound the dump folder.

Forced-crash probe. `ExitLine_ForcedHeapCorruption_Probe` (in `StageTimingTests.Run`) is registered only when
`CFD_FORCE_HEAP_CORRUPTION=1` (an ordinary run prints no line for it); then it allocates 16 bytes with `Marshal.AllocHGlobal`, writes 64 bytes past the block and frees it,
and ntdll raises 0xC0000374. Run one mode under the wrapper:

    CFD_FORCE_HEAP_CORRUPTION=1 CFD_TEST_ONLY=ExitLine_ForcedHeapCorruption_Probe \
      <procdump64a.exe> -accepteula -ma -e 1 -f C0000374 -x <dump folder> <Desktop.Tests.exe> --harness --theme-evidence

- ProcDump log: `Exception: C0000374`, `Dump 1 initiated`, `Dump 1 complete: 556 MB written in 1.9 seconds`, `Dump count reached.`
- ProcDump exit code: 1 (one dump written). ProcDump's status is not the child's code; the child's code is on the `Process Exit` line.
- Dump: `CfdWorkbench.Desktop.Tests.exe_261022_085706.dmp`, 568,914,357 bytes (556 MB), in the dump folder. It was written by an
  earlier build of this branch (the probe sat at line 121 of `StageTimingTests.cs`, before the NUL check was inserted above it).
  At the repair commit the probe's `FreeHGlobal` call is at line 163 of the same file; the stack below is that earlier build's.
- `dotnet-dump analyze <dump> -c "clrstack -all" -c "exit"` (the `--command "..." "exit"` form of the brief is refused by the tool
  version here: `exit` is not an argument; `-c` is repeated). The crashing thread, with the repository path shortened:

        OS Thread Id: ...
        Child SP               IP Call Site
        [InlinedCallFrame] 
        System.Runtime.InteropServices.Marshal.FreeHGlobal(IntPtr)
        CfdWorkbench.Desktop.Tests.StageTimingTests+<>c.<Run>b__0_7() [tests/CfdWorkbench.Desktop.Tests/StageTimingTests.cs @ 121]
        CfdWorkbench.Desktop.Tests.DesktopChecks.Check(System.String, System.Action)
        CfdWorkbench.Desktop.Tests.StageTimingTests.Run() [... StageTimingTests.cs @ 121]
        DesktopHarness+<Run>d__0.MoveNext() [tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs @ ...]
        System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox ... .MoveNext
        System.Threading.ExecutionContext.RunInternal(...)

  The first 40 lines of the same output are the other threads (the main thread waiting on the harness task, three thread-pool
  workers waiting on the completion port, the pool's gate thread); the faulting thread starts at line 47.

Limits. A wrapped run is a diagnostic, not a verdict run, and its log says so (`CHILD-WRAPPER active`). Under the wrapper a child's
exit is read from ProcDump's `Process Exit` line (the real code, 0xC0000374 included); when that line is missing the child fails by
name. The probe modes run bare. One unwrapped full run of this track exited -1073740940 (0xC0000374) in `--analysis`, with no wrapper
active and so no dump.
