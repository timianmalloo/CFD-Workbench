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
  within a run or two when two runners overlap. Root cause, verified necessary and sufficient: SkiaSharp 2.88.9's array
  helper SKCanvas.DrawVertices creates a native vertices object that nothing keeps reachable during the draw, so a
  garbage collection mid-draw frees the vertex data; the product's one call site is MeshFrame.Draw. On the raster path
  that is an immediate access violation, on the GPU path a use-after-free the segment heap's timer reports later as heap
  corruption. The fix (CR2) holds the vertices object through the draw at that one site, proven by a GC-pressure probe
  and the paired loop on both rendering paths.
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
| H1 the native rendering stack (Skia Ganesh, ANGLE, the NVIDIA driver) overruns a heap buffer under concurrency | a timing-dependent overrun in vertex staging | **Refuted as the cause**: the GPU path only changes the signature of H2's fault (a later use-after-free instead of an immediate access violation); the software path crashes too under the same load, which the first software loop hid (section 7) |
| H2 our own native call path: a managed wrapper's native object freed during a long native call | the SkiaSharp array helper `DrawVertices` creates an `SKVertices` that nothing keeps reachable once its handle is read; a garbage collection during the draw runs its finalizer and frees the vertex data the draw is still reading | **Verified necessary and sufficient** (section 6) |
| H3 the harness parent closes something twice | a handle closed twice | **Refuted**: the crash is in the child, every time |
| H4 a non-finite or astronomically large projected coordinate reaches Skia | a transient camera or zero size under load | **Refuted**: a Skia spike drew huge, infinite and NaN coordinates 200 times each without fault |
| H5 the render thread draws into a window the UI thread is closing | a canvas or surface freed under the draw | **Refuted as stated**: with the vertices call skipped, every stroke draw into the same canvases after the same window closes ran clean for eight paired runs; the timing coincidence with window close is GC pressure at window teardown, not the canvas |

## 6. Root cause (Verified necessary and sufficient)

**The cause.** `MeshFrame.Draw` (`src/CfdWorkbench.Desktop/SurfaceRenderer.cs`, the product's only draw-vertices site)
calls SkiaSharp 2.88.9's array helper `SKCanvas.DrawVertices(SKVertexMode, SKPoint[], SKPoint[], SKColor[], SKPaint)`.
That helper creates an `SKVertices` (a managed wrapper owning a native `SkVertices`), reads its handle for the native
call, and keeps no reference to it, so the wrapper is unreachable while `sk_canvas_draw_vertices` runs. A garbage
collection during the draw runs the wrapper's finalizer, which frees the native vertex data the draw is still reading.
On the raster path the draw faults at once (`0xC0000005` in `sk_canvas_draw_vertices`, the managed stack naming
`MeshFrame.Draw` on Avalonia's render thread); on the GPU path the freed block is reused while Skia's vertex staging
still copies from it, and the segment heap's timer reports the damage later as `0xC0000374`. Load matters because it is
GC pressure (many windows opened and closed, many children) and thread count that make a collection land inside a draw.

**Necessary (Verified).** With the draw-vertices call skipped and everything else unchanged, eight paired
software-rendered runs produced no fatal exit (`runs6-default-novertices`), against two fatal exits on the first pair
with it; every stroke draw into the same canvases kept running.

**Sufficient (Verified, deterministic).** A spike outside the repo (`scratchpad/skia-spike/Program.cs`; raster surface,
3,000 vertices per draw, a thread forcing `GC.Collect` and `GC.WaitForPendingFinalizers` continuously): the array
helper crashed with a native fault inside its first 60 s run, with the stack `sk_canvas_draw_vertices` <-
`SKCanvas.DrawVertices(SKVertexMode, SKPoint[], SKPoint[], SKColor[], SKPaint)`; an explicit
`using var vertices = SKVertices.CreateCopy(...)` drawn with `canvas.DrawVertices(vertices, SKBlendMode.Modulate, paint)`
survived 317 draws under 205,338 collections.

**The evidence that led here**, kept because the dumps are the only record of the GPU-path shape:

## 6a. What the dumps showed before the cause was known (Verified facts, superseded reading)

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
- **The withdrawn reading.** Seven software-rendered paired runs (app-local ANGLE removed, Git's own ANGLE off the PATH,
  verified by the loaded-module lists) showed "no crash" against three GPU-path dumps, and the report at its first
  review took the GPU path as necessary. That reading was an artefact: the wrapper of the time returned 0 for a failing
  child (section 7), so the software path's own fatal exits (access violations, no dump because the ProcDump filter was
  the heap code only) never printed, and the loop's crash count did not look for them. The R1 harness, which reads the
  child's real exit, showed two fatal access violations on its first software-rendered pair and the heap signature on
  its first GPU pair (dump 4, 1,258 MB).
- **What the GPU path adds:** nothing but the signature. With the cause fixed at the call site the path is immaterial;
  `ANGLE_DEFAULT_PLATFORM=warp` does not move Avalonia's ANGLE off the NVIDIA device (Verified), which no longer matters.

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

- **The class:** a managed wrapper's native object handed to a long native call with nothing keeping the wrapper
  reachable, so the garbage collector may finalize it mid-call and free memory the native code is still using.
  Signature: a native fault (access violation, or heap corruption reported later by the heap's own checks) inside a
  library call that was given a temporary created by a convenience overload; it appears only under GC pressure, so
  "only under load, never alone, in any mode". Why it survived: the convenience overload reads as safe, the fault is
  reported by the platform nowhere (no event, no dump), the detector on the GPU path is a background timer that names
  nothing, and every reproduction isolated the suspect, which removed the pressure.
- **The instance:** SkiaSharp 2.88.9's `SKCanvas.DrawVertices(SKVertexMode, SKPoint[], SKPoint[], SKColor[], SKPaint)`
  called from `MeshFrame.Draw`.
- **Siblings swept (CR2 sweeps the code; this is the shape to look for):** any SkiaSharp call site that passes a
  native temporary the caller never holds: array overloads that build an `SKVertices`, text blobs or pictures created
  inline for one draw, a `new SKPath(...)` passed straight into a draw without a `using`. `DrawPoints` and the other
  array overloads that pin the array for the call are safe. The product has one `DrawVertices` site; the sweep's result
  is in the CR2 proof section.
- **The reusable rules:** (1) hold a wrapper through the native call it feeds (`using` plus `GC.KeepAlive` after the
  call) whenever the library's overload would otherwise leave it unreachable; (2) test such a call under a thread that
  forces collections (the GC-pressure probe is the red-first test and stays as a child mode); (3) a native crash under
  load is captured by an out-of-process debugger wrapper on the child and the dump is read for its failure record before
  any hypothesis is believed; (4) a first discriminating experiment removes one call or one path and is verified by what
  was actually loaded and actually exited, never by the switch that was flipped (the first software loop's "no crash"
  came from a wrapper that masked the child's exit).
- **Second class, kept from the first review:** a native fault the platform reports nowhere (no event, no dump, no exit
  reason beyond a status code), detected by a background mechanism at its own cadence. Its control is the capture
  wrapper (R0, on main) and the join rule.

## 10. Phased repair plan

| Phase | Scope (code + tests) | Failure mode it removes | Validation | Status |
|---|---|---|---|---|
| R0 | CR1: the `CFD_TEST_CHILD_WRAPPER` hook, the wrapped child's real exit read from ProcDump's `Process Exit` line, the gated forced-crash probe, probe modes bare, both variables unset by the runner | a crash with no dump; a wrapped run that reads green while its child failed | the hook's pure checks and mutants, one real wrapped forced failure, the proof | **on main at 48ea4297** |
| R1 | The harness renders in software behind `CFD_TEST_RENDERING=gpu`, with a loaded-module check | none, in the end: the software path has the same fault | built on branch `cr-r1`; the module check was red on today's harness and green after; its paired loop crashed on the first software pair and the first GPU pair | **dropped** (the leader's decision: a harness that renders unlike the shipped app diverges from the path the signature appeared on; the branch closes unmerged, its findings stay here) |
| R2 (CR2) | `MeshFrame.Draw` builds the `SKVertices` explicitly and holds it through the draw (`using` plus `GC.KeepAlive`); a GC-pressure child probe mode (`array` arm must crash on today's code, `held` arm exits 0); three mutants; a sweep of `src/` and `tests/` for other unheld native temporaries; the register entries; the proof section | the crash itself, on both rendering paths | the probe observed red then green; the paired loop on both rendering paths with zero fatal exits over at least as many pairs as crashed before (two parents, eight pairs each path); every surface mode green with no pixel check touched | **on main at 28e23de0** (review accepted with two probe conditions, repaired; the probe drives the shipped `MeshFrame.Draw` and treats the array arm as its positive control; sixteen paired runs on the fixed build, eight per rendering path, zero fatal exits and zero dumps) |
| R3 | Product decision: none needed. The fix changes no behaviour a user sees; the shipped app carries the same call and the same fix | the shipped app crashing the same way under GC pressure | the probe and the surface modes above | folded into R2 |
| R4 | The register: `HARNESS-CRASH-AS-VERDICT` gains this instance; `NATIVE-FAULT-REPORTED-NOWHERE` and `WRAPPER-MASKS-CHILD-EXIT` (on main with the first report); the new class for the unheld native temporary lands with CR2; the join rule (a crashed or timed-out child rerun alone, two consecutive crashes or a crash after a check printed stops the join, loops paused during joins) into the testing knowledge doc | the lesson staying a memoir | `check-docs` green; each entry names the control observed failing on the un-fixed code | partly on main; the rest with CR2 |

## 11. Residual risk and what would change the diagnosis

- The sufficiency proof is a spike with a thread forcing collections; the harness probe (R2) must reproduce it in a
  harness child on today's code before the fix is believed in context. A probe that cannot crash today's code would
  send the investigation back to section 6 with the spike as the only evidence.
- The paired loop after the fix is the necessity proof in context. A fatal exit in it, on either path, means a second
  cause exists; the dump readers and the managed stack would say where within minutes.
- CI runners (x64, no NVIDIA driver, lighter load) never showed the crash; after R2 the GC-pressure probe runs there on
  its variable only, so CI proves the fix's presence through the pure checks, not the race.
- The two handled first-chance access violations per child run (each followed by a managed-exception banner) are not
  investigated; they are consistent with managed null dereferences inside tests and did not change under any experiment.

## 12. Gate record

The first Adversary pass on this report was the CR1 review (Test Architect, then SRE) on phase R0, which set three
blocking conditions, all repaired and joined. The report's first version named the GPU path as the cause; that was
withdrawn by the investigator when the R1 harness, reading the child's real exit, showed the software path crashing
too, and the leader seat withdrew the same reading in its notes. Two later hypotheses from the seats were refuted by
experiment before any brief was dispatched on them (H4 by the coordinate spike, H5 by the stroke draws surviving window
close), and the verified cause came from the leader's two-arm GC-pressure spike run by the investigator. On
self-certification: the earlier "debugger perturbation" reading was held by both seats and withdrawn on the base-rate
argument. Sections 6, 9 and 10 in this version are the investigator's; the CR2 review (native desktop, then Test
Architect) is their Adversary pass, and the report's final gate is the paired loop after the fix.

**Stop.** This report ends the investigation at the review gate. The operator's standing instruction ("keep going")
authorises R2 as the repair; no step needs elevation and no Owner ruling is needed, because the fix changes nothing a
user sees.
