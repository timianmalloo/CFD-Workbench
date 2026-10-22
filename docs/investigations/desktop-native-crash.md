---
id: investigation-desktop-native-crash
title: "Investigation: the intermittent native crash (0xC0000374) of Desktop harness children under load"
type: investigation
status: proposed
owner: "@timianmalloo"
phase: investigate
tags: [desktop-tests, native-crash, heap-corruption, windows, arm64, harness, rendering]
links:
  - { to: proof-pvh-crash-loop, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: coordination-next-best-six, rel: relates-to }
review-by: "2026-11-22"
summary: >-
  Desktop harness children die with STATUS_HEAP_CORRUPTION about once in eight full runs on the Windows ARM64 laptop and
  within a run or two when two runners overlap. Three captured dumps show a heap buffer overrun found by the segment
  heap's timer, next to a vertex stream, on the GPU rendering path (ANGLE on D3D11 on the NVIDIA driver); seven paired
  software-rendered runs never crashed. The writer inside that path is not yet named. First repair: the test harness
  renders in software; then a rendering-mode switch and page heap name the module.
---

# Investigation: the intermittent native crash of Desktop harness children

Labels: **Verified** = observed in this session with the command, file or dump named; **Inferred** = reasoned from
evidence, not observed. Every file path under `scratchpad/crash/` is in the session scratchpad, not in the tree; the
dumps (0.9 to 1.3 GB each) stay out of git.

## 1. Symptom (Verified)

A child of the Desktop test harness (`CfdWorkbench.Desktop.Tests.exe --harness <mode> --part=k/n`) exits with
`-1073740940` = `0xC0000374` = `STATUS_HEAP_CORRUPTION`. The parent fails the mode line and the PARTITION check, and the
xunit wrapper's fact fails with `exit -1073740940 after N s`. No check verdict fails. Crashes were seen both before any
check printed and after PASS lines, so the fault is not tied to start-up.

## 2. Reproduction and conditions (Verified)

| Condition | Runs | Crashes |
|---|---|---|
| One child alone (three modes, 11 runs in all) | 11 | 0 |
| One full runner alone (`bash tools/run-tests.sh`, loop B) | 8 | 1 (run 8) |
| One full runner alone, every child under ProcDump (loop C) | 12 | 0 |
| Two full runners at once, GPU path, under ProcDump (loop D and the overlapped attempt) | about 7 paired | 3 dumps |
| Two harness parents at once, software rendering, under ProcDump (loop E) | 7 paired | 0 |
| Wave 1 and wave 2 joins and tracks | about 60 full runs | 14 |

The slot count on this laptop is 12 concurrent Desktop children (`ResolveSlots`: ProcessorCount 20 × 5 / 8), and
`tools/run-tests.sh` runs the four test projects at once. The crash rate rises with concurrency; it was never seen with
one child, and it fires in any mode (eight different modes so far).

## 3. Timeline (Verified from the session's logs)

1. 2026-10-22 00:5x: the first join ring of the wave: `--properties-view --part=2/2` exits 0xC0000374. Standalone
   reruns pass.
2. Track D1 loops the harness alone and the full runner (6 + 6): 0 crashes; it adds the `ExitLine` control that names a
   native termination as a crash, never a verdict (lesson HARNESS-CRASH-AS-VERDICT).
3. 02:00 to 09:00: thirteen more crashes across the joins and tracks.
4. 08:xx: the Application event log of the last 24 h holds no Application Error (1000) and no .NET Runtime (1026)
   event for any process (`wevtutil qe Application`, 366 events). WER LocalDumps (HKCU, per exe, full dumps) and the
   runtime's createdump (`DOTNET_DbgEnableMiniDump=1`) were armed; loop B crashed on run 8 and produced no dump from
   either. Crashpad's source (Chromium) records that Windows swallows STATUS_HEAP_CORRUPTION without WerFault unless a
   vectored exception handler forwards it, which explains both empty folders.
5. Track CR1 adds `CFD_TEST_CHILD_WRAPPER` to `SelfLaunch.StartInfo` so each child runs under ProcDump
   (`-ma -e 1 -f C0000374 -x <folder>`), strips the NULs ProcDump's UTF-16 banner leaves on the child's stdout, and
   proves the capture with a forced heap corruption (a 556 MB dump whose managed stack names the probe).
6. 09:51: loop D (two full runners at once) produces the first natural dump on its first paired run. 10:19 and 10:20:
   two more dumps from an overlapped attempt whose processes had loaded Git for Windows' own ANGLE build.
7. 10:30 to 10:48: loop E (software rendering, seven paired runs) produces no crash.

## 4. System map (Verified from source and from the dumps' module lists)

- The xunit wrapper (`tests/HarnessTests.cs`) spawns `<assembly> --harness` once per test process. The parent
  (`DesktopHarness.Run`, `WorkbenchTests.cs`) spawns one child per mode and part through `SelfLaunch.StartInfo`, up to
  12 at a time, pumps both pipes, kills a child after 120 s.
- Each child initialises Avalonia 11.3 with `UsePlatformDetect()`. On this machine that is the Win32 platform with
  ANGLE (`av_libGLESv2.dll`, Avalonia.Angle.Windows.Natives 2.1.25547) on Direct3D 11 (`d3d11.dll`) on the NVIDIA ARM64
  user-mode driver (`nvwgf2umax.dll`, `nvldumdax.dll`), with Skia (`libSkiaSharp.DLL`, SkiaSharp 2.88.9) rendering
  through Ganesh; `dcomp.dll` composes. All of these are in every crashed process's module list.
- Heap corruption is per process; what children share is the OS, the driver, the font caches and the CPU budget.

## 5. Hypotheses

| Hypothesis | Mechanism | Status |
|---|---|---|
| H1 the native rendering stack (Skia Ganesh, ANGLE, the NVIDIA driver) overruns a heap buffer under concurrency | a timing-dependent overrun in vertex staging | **Supported (Verified necessary at the path level; the module is open)** |
| H2 our own P/Invoke (WindowsNative, the harness's pipe holders, the placeholder calls) writes past a buffer | a layout or lifetime error under load | **Refuted** for the three dumps: the faulting thread has no managed frames, the corrupted memory is a rendering vertex stream, and the software-rendered loop, which runs every P/Invoke of ours just the same, never crashed |
| H3 the harness parent closes something twice | a handle closed twice | **Refuted**: the crash is in the child, every time |

## 6. Root cause (Verified to the rendering path; the module is Inferred)

Three full dumps, read without a debugger (`minidump_exception.py`, `minidump_native_stack.py` walking the ARM64
frame chain, `symbolize.py` with `ntdll.pdb` and `kernel32.pdb` fetched from the Microsoft symbol server by their RSDS
keys and resolved through the System32 `dbghelp.dll`, `heap_failure.py` reading the exception's
`HEAP_FAILURE_INFORMATION`):

- The exception is raised on a **native thread-pool worker with no managed frames**:
  `RtlpHpGCTimerCallback` → `RtlpEnumProcessHeaps` → `RtlpHpLfhContextCompact` → `RtlpHpLfhOwnerCompact` →
  `RtlpHpLfhSubsegmentDelayFreeListProcess` → `RtlpLogHeapFailure` → `RtlpHpHeapHandleError` →
  `RtlpReportHeapFailure` → `RtlReportCriticalFailure` → `RtlReportFatalFailure` (dumps 1 and 3); dump 2 reaches the
  same detector from `RtlpHpAllocateHeap` under `malloc` on a managed thread. **So the detector is the segment heap's
  own periodic compaction, which runs at its own cadence on a thread-pool thread and walks every heap.** That is why the
  crash is intermittent, strikes in any mode, carries no check verdict, and never names the writer.
- The failure type is `heap_failure_vs_subsegment_corruption` (dumps 1 and 3) and `heap_failure_invalid_segment_heap`
  (dump 2); the corrupted block lies in a variable-size subsegment of a segment heap.
- In dump 1 the memory immediately before the corrupted block header is a stream of 12-byte records, each two 32-bit
  floats in pixel range (30 to 533) followed by an opaque ARGB colour in the pane's dark palette, running over the
  block's tail guard bytes to the next header. **Inferred:** a per-vertex-colour vertex writer wrote one or more
  records past its allocation. The writer's module is not recorded in the dump (the detector runs later, elsewhere).
- **Necessary (Verified by contrast):** with the app-local ANGLE removed and Git for Windows' own `av_libGLESv2.dll`
  kept off the PATH (verified by the children's loaded-module lists: Skia present, neither ANGLE nor the NVIDIA driver
  nor D3D11 loaded), seven paired runs produced no crash; the same paired load on the GPU path produced three dumps in
  about seven paired runs. The sample is small (eight paired runs were planned, seven completed), but the contrast is
  three to zero under the same load.
- **Sufficient (Verified):** two runners at once on the GPU path crashed on the first paired run (loop D) and twice
  more in the overlapped attempt.
- **What would name the module:** page heap with allocation stacks (an HKLM IFEO entry for the harness exe, the one step
  that needs elevation on this laptop), or a harness rendering-mode switch (ANGLE on WARP, Vulkan, software) run under
  the paired loop; `ANGLE_DEFAULT_PLATFORM=warp` does not move Avalonia's ANGLE off the NVIDIA device (Verified).

## 7. Ruled out and corrected

- The event log and WER are empty because the platform swallows this status (Verified by the empty log and the empty
  LocalDumps folder after a crash, consistent with Crashpad's documented reason).
- "The attached debugger hides the fault" (an earlier Inferred reading of loop C's 12 clean runs) is withdrawn. The
  decisive evidence is the three dumps of the real fault captured under the wrapper: the crash happens with ProcDump
  attached. The wrapper does change the child: a process started by a debugger gets the debug heap flags (the dumps show
  the tail guards), so the wrapped child is not identical to the bare one, and 12 clean runs merely fail to reject the
  idea (p about 0.2 at the one-in-eight base rate). Load is the lever.
- Under the wrapper a child's own exit code is lost (ProcDump `-x` returns 0 when the child fails; measured by the CR1
  review: a selector failure 1 bare and 0 wrapped, an unhandled managed exception 70 bare and 0 wrapped), so the wrapped
  loops' "green" verdicts are unverified. Their dump counts stand, because the ProcDump filter writes a dump on the fault
  whatever the harness prints, and every contrast in this report rests on dump counts, not on wrapped verdicts. CR1's
  repair parses ProcDump's `Process Exit` line so a wrapped run reports the child's real code.
- Git for Windows ships `av_libGLESv2.dll` in `clangarm64/bin` and `mingw64/bin`; once the app-local copy is gone, the
  Windows loader finds Git's copy on any Git bash session's PATH (Verified by the module path in the children). Two of
  the three dumps come from processes running Git's ANGLE build, which means the crash is not specific to Avalonia's
  build of ANGLE either.

## 8. The capture control and its evidence (Verified)

CR1 (branch `cr1-crash-capture`, tree `D:/Projects/CFD-Workbench-cr1-crash-capture`, not yet joined): the wrapper hook
with its red-first pure check and a mutant; the NUL-stripping fix with its red-first check; the forced-crash probe
`ExitLine_ForcedHeapCorruption_Probe` (inert unless `CFD_FORCE_HEAP_CORRUPTION=1`). The full suite stays green with the
wrapper unset on this machine (the branch has not been pushed, so CI has not run on it); the wrapped runs showed no
FAIL line and no missing SCALE_CONTEXT or PARTITION line, which, until CR1's repair lands, is all a wrapped run can show
(see section 7). The CR1 review (Test Architect, then SRE; verdict in `cr1-review/verdict.md`) set three blocking
conditions on R0: the runner must unset both variables, the probe must be registered only under its variable, and the
wrapped child's real exit must be read from ProcDump's `Process Exit` line. The loop scripts (`loop3.sh` to `loop5.sh`), the dump readers,
the module probe and the process killer are stdlib Python and bash in `scratchpad/crash/`; they become
`docs/proof/desktop-native-crash/` with the join of the report.

## 9. Generalisation

- **The class:** a native fault that the platform reports nowhere (no event, no dump, no exit reason beyond a status
  code), detected by a background mechanism at its own cadence, so that it shows up as an intermittent harness exit in
  unrelated modes. Signature: a Desktop child exits 0xC0000374 (or any status with no WER event) under load, never
  alone, in no fixed mode. Why it survived: the harness treated the exit as a mode failure (fixed by D1's `ExitLine`),
  WER was trusted to catch it (it cannot), and every reproduction tried the suspect alone (it needs load).
- **Siblings swept:** the only other native-stack consumers in a child are HarfBuzz (text shaping) and DirectWrite
  (both loaded in every crashed process and in the software-rendered runs that did not crash, so they are not
  necessary) and our P/Invoke (refuted above). No other process in the repo renders through Avalonia.
- **The reusable rule:** a native crash under load is captured by an out-of-process debugger wrapper on the child
  (opt-in env hook, kept in the harness), the dump is read for its failure record and faulting module before any
  hypothesis is believed, and the first discriminating experiment removes whole paths (GPU versus software), verified by
  the loaded-module list rather than by the switch that was flipped.

## 10. Phased repair plan

| Phase | Scope (code + tests) | Failure mode it removes | Validation | Depends on |
|---|---|---|---|---|
| R0 | Join CR1: the `CFD_TEST_CHILD_WRAPPER` hook, the NUL strip, the forced-crash probe; `docs/proof/pvh/README.md` gains the capture section | a crash with no dump | the hook's pure check and mutant; 18 modes green with the wrapper on; CI green with it off | none |
| R1 | The Desktop test harness renders in software: one `Win32PlatformOptions { RenderingMode = [Software] }` in the harness app builder (one shared helper for the ten `AppBuilder.Configure` sites), behind an env override `CFD_TEST_RENDERING=gpu` for the GPU runs that stay opt-in; a check that the loaded modules of a harness child exclude ANGLE and the driver by default | the intermittent 0xC0000374 at joins and in CI on this machine class | the module check red first (today's harness loads ANGLE) then green; the paired loop (two harness parents, 8 pairs) green with the default and the crash reproduced with the override; pixel checks that sample colours re-verified in software rendering | R0 |
| R2 | Name the module: a paired loop under each of ANGLE-on-WARP, Vulkan and GPU (the override from R1 extended to those modes), and, if the operator runs the one elevated step, page heap with allocation stacks on the GPU mode; the dump's allocation stack names the writer | not knowing whether the shipped app (which renders on the GPU) carries the same overrun | one dump with the allocation stack, or a clean 16-pair loop on each non-NVIDIA mode | R1 |
| R3 | The product decision from R2: a driver report, a SkiaSharp or ANGLE version change, or a rendering-mode default for the shipped app on ARM64 with the NVIDIA driver; an Owner ruling if the shipped app's rendering changes | the shipped app crashing the same way under load | the paired loop on the shipped rendering mode | R2, Owner ruling |
| R4 | Register the class in `docs/lessons/defect-classes.md` (HARNESS-CRASH-AS-VERDICT gains the instance and the capture control; a new entry for "the platform reports the fault nowhere"); the join rule (a crashed or timed-out child is rerun alone, two consecutive crashes or a crash after a check printed stops the join, loops paused during joins) into the testing knowledge doc | the lesson staying a memoir | `check-docs` green; the register entry names the control that was observed failing on the unfixed harness (loop D's dump) | R0 |

## 11. Residual risk and what would change the diagnosis

- The software-rendered sample is seven paired runs. A crash in a later software-rendered loop would refute "the GPU
  path is necessary" and point at Skia's raster path or at our code; the dump readers would say which within minutes.
- Naming the module is open. If page heap names our own code after all (for instance a Skia callback we supply), R1
  stays a correct mitigation for the harness but R3 becomes a product fix with a layout or lifetime test.
- The three dumps come from one machine, one driver build and one OS build; the CI runners (x64, no NVIDIA driver)
  have never shown the crash, which is consistent with H1 but is not a measurement of the shipped app on other
  hardware.

## 12. Gate record

No Adversary pass had been run on this report at the time of writing; the first one is the CR1 review (Test Architect,
then SRE) on phase R0, and the report joins only after it. The Test Architect's veto on the repair phases is cleared
when R1's module check is observed red on today's harness. On self-certification: the "debugger perturbation" reading
of loop C was held by both the investigator and the leader seat; it was withdrawn by the investigator on the base-rate
argument once the first dump showed the segment heap and its tail guards active in the wrapped process, and the leader
withdrew it in its notes on the same argument. Nothing in sections 6 to 10 has yet been challenged by a reviewer.

**Stop.** This report ends the investigation at the review gate. The operator's standing instruction ("keep going")
authorises R0 and R1 as the first repair phases; R2 needs the operator's one elevated step for page heap, and R3 needs
an Owner ruling.
