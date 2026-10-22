---
id: proof-desktop-native-crash
title: "Proof: the Desktop harness native crash (0xC0000374) captured and read"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: investigate
tags: [desktop-tests, native-crash, heap-corruption, windows, arm64, proof]
links:
  - { to: investigation-desktop-native-crash, rel: documents }
  - { to: proof-pvh-crash-loop, rel: relates-to }
review-by: "2026-11-22"
summary: >-
  The loop scripts and the stdlib dump readers behind the investigation of the intermittent 0xC0000374 crash: how three
  dumps were captured under load with the ProcDump wrapper, how each was read without a debugger, and the seven
  software-rendered paired runs that never crashed. The dumps themselves (0.9 to 1.3 GB each) stay out of git.
---

# Proof: the Desktop harness native crash captured and read

Every number here was observed on 2026-10-22 on the Windows ARM64 laptop; the commands are the scripts in this folder.
Paths that depend on the machine come from two environment variables: `CFD_CRASH_SCRATCH` (a scratch folder for logs
and dumps) and `CFD_PROCDUMP` (the ProcDump executable; Sysinternals, v12.02, the ARM64 `procdump64a.exe`). The loop
scripts read the trees to run from `CFD_CRASH_TREE_A` and `CFD_CRASH_TREE_B`.

## What was run

| Loop | Script | Condition | Runs | Dumps |
|---|---|---|---|---|
| C | `loop3.sh` | one full runner, every Desktop child under ProcDump | 12 | 0 |
| D | `loop4.sh` | two full runners at once, GPU rendering, under ProcDump | 1 paired | 1 (first run) |
| D' | `loop4.sh` (an overlapped attempt whose children loaded Git for Windows' own ANGLE) | about 5 paired | 2 |
| E | `loop5.sh` | two harness parents at once, software rendering (ANGLE removed and Git's compiler bins off PATH), under ProcDump | 7 paired | 0 |

A wrapped loop's PASS and FAIL lines are not verdicts: ProcDump `-x` returned 0 for a failing child until CR1's repair
(main at 48ea4297) read the child's exit from ProcDump's `Process Exit` line. The evidence here is the dump count, which
the ProcDump filter writes on the fault whatever the harness prints.

## How a dump was read (no debugger installed)

1. `minidump_exception.py <dmp>`: the exception stream (thread, code, address, module+offset) and the module list.
2. `minidump_native_stack.py <dmp>`: the faulting thread's frames by walking the ARM64 frame chain (x29) through the
   dump's memory ranges, plus every thread's current module.
3. `pe_pdb_id.py C:\Windows\System32\ntdll.dll` gives the RSDS key; the matching `ntdll.pdb` and `kernel32.pdb` come
   from `https://msdl.microsoft.com/download/symbols/<pdb>/<key>/<pdb>`; `symbolize.py <dmp> <frames.txt> <pdb-folder>`
   resolves the frames through the System32 `dbghelp.dll`.
4. `heap_failure.py <dmp>`: the `HEAP_FAILURE_INFORMATION` record the exception points at (failure type, heap, block)
   and the memory around the block with each pointer-sized word mapped to a module.
5. `list_modules.py <dmp>` lists every module; `modules_of_process.py <exe name> [substrings]` reads the loaded modules
   of running children (used to prove that the software-rendered children had neither ANGLE nor the NVIDIA driver loaded,
   and to catch that Git for Windows' `clangarm64\bin\av_libGLESv2.dll` was being loaded from the PATH).
6. `kill_tree_processes.py <folder>...` ends every process whose image lives under the loop trees and nothing else.

## What the three dumps say

- Exception 0xC0000374 on a native thread-pool worker with no managed frames; stack `RtlpHpGCTimerCallback` ->
  `RtlpEnumProcessHeaps` -> `RtlpHpLfhContextCompact` -> `RtlpHpLfhOwnerCompact` ->
  `RtlpHpLfhSubsegmentDelayFreeListProcess` -> `RtlpLogHeapFailure` -> `RtlpHpHeapHandleError` ->
  `RtlpReportHeapFailure` -> `RtlReportCriticalFailure` -> `RtlReportFatalFailure` (dumps 1 and 3; dump 2 reaches the
  same detector from `RtlpHpAllocateHeap` under `malloc` on a managed thread).
- Failure types `heap_failure_vs_subsegment_corruption` (dumps 1 and 3) and `heap_failure_invalid_segment_heap` (dump 2).
- In dump 1 the memory before the corrupted block header is a stream of 12-byte (x, y, ARGB) vertex records in pixel
  range, running over the block's tail guard: a per-vertex-colour vertex writer overran its allocation.
- Every crashed process had `av_libGLESv2.dll`, `d3d11.dll` and the NVIDIA ARM64 user-mode driver loaded; the seven
  software-rendered pairs (Skia only, verified by the loaded-module list) never crashed.

## Residuals

The writer's module is not named by the dumps (the detector runs later, elsewhere). Page heap with allocation stacks
(an elevated IFEO entry) or a harness rendering-mode switch under the paired loop would name it; both are phases of the
investigation's repair plan.

## CR2: the cause and the fix

Observed 2026-10-22, SkiaSharp 2.88.9, Windows ARM64 laptop, Release build, nothing else of ours running.

**Cause.** `SKCanvas.DrawVertices(mode, points, texs, colors, paint)` is `var vert = SKVertices.CreateCopy(...)` followed by
`DrawVertices(vert, SKBlendMode.Modulate, paint)` (SkiaSharp v2.88.9 `binding/Binding/SKCanvas.cs`, lines 975 to 979). The inner
call reads `vertices.Handle` and calls `sk_canvas_draw_vertices` (line 998). After the handle is read nothing keeps `vert`
reachable, so a collection during the native draw runs the finalizer and frees the vertex data the draw is still using. The
product's only call site was `MeshFrame.Draw` in `src/CfdWorkbench.Desktop/SurfaceRenderer.cs`.

**Fix.** `MeshFrame.Draw` builds the vertices itself and holds them: `using var vertices = SKVertices.CreateCopy(...)`,
`canvas.DrawVertices(vertices, SKBlendMode.Modulate, fill)`, `GC.KeepAlive(vertices)`. The blend mode is the helper's own
(Modulate, line 978), so no pixel changes.

**The probe.** `CFD_VERTICES_GC_PROBE=1 CfdWorkbench.Desktop.Tests.exe --harness --vertices-lifetime-probe` spawns two
children one after the other (`=array`, `=held`); each draws 3,000 per-vertex-coloured vertices on a raster surface for 60 s while
a second thread runs `GC.Collect()` plus `GC.WaitForPendingFinalizers()` continuously. Without the variable the parent prints one
SKIP line and no arm runs. `tools/run-tests.sh` unsets the variable. The `held` arm carries a copy of the shipped pattern
(`MeshFrame` is internal to the Desktop assembly), in its own non-inlined method so the JIT tracks the local's lifetime as it does in
`MeshFrame.Draw`; a copy inlined in the probe's loop stays alive in unoptimised code and could not be made red.

| Run | Result |
|---|---|
| `array` (the helper, unchanged by the fix) | `FAIL ... exited -1073741819 (0xC0000005): native crash, not a check verdict` after 8.0 s (two parent runs: 8.1 s and 8.0 s of child time) |
| `held`, before the fix to `MeshFrame` | `PASS VerticesLifetime_held: 551 draws under 544403 collections in 60 s`, exit 0 |
| `array`, after the fix | 0xC0000005 again (the arm is the defect, kept as the probe's red) |
| `held`, after the fix | `PASS VerticesLifetime_held: 521 draws under 615433 collections in 60 s`, exit 0 (also 531 under 555037) |
| Mutant: `held` without `using` and without `GC.KeepAlive` | `Fatal error. 0xC0000005`, child killed natively (bash shows 139) |
| Mutant: `held` with `using`, without `GC.KeepAlive` | survived: 546 draws under 626755 collections. `using` alone holds it (`Dispose` is a use); `GC.KeepAlive` is the explicit statement of the intent |
| Mutant: the helper restored in `MeshFrame.Draw` | the `array` arm is this mutant (same call); no Desktop mode renders enough frames to hit it, which is why the probe, not the modes, is the test |

The surface modes stay green with no pixel check changed, each run alone: `--views` 111 PASS, `--analysis` 64,
`--properties-cells` 26, `--shell-window` 121, `--plan-canvas` 112, all with 0 FAIL, exit 0; `--harness --theme-evidence` exit 0.

**Sweep** (every SkiaSharp use in `src/` and `tests/`; `SurfaceRenderer.cs` is the only source file with `using SkiaSharp`):

| Where | What | Verdict |
|---|---|---|
| `SurfaceRenderer.cs:178` `MeshFrame.Draw`, the `DrawVertices` call | helper made an unheld `SKVertices` | fixed |
| `SurfaceRenderer.cs:180-181` `fill`, `stroke` | `SKPaint` | safe: `using var`, in scope through every draw |
| `SurfaceRenderer.cs:160` `lease` | `ISkiaSharpApiLease` | safe: `using var`, the draw runs inside it |
| `SurfaceRenderer.cs:195` `DrawPoints` | array overload | safe: the binding pins the array for the call |
| `SurfaceRenderer.cs:207-213, 278-279, 322-329` | `SKPoint`, `SKColor` arrays and lists | safe: managed value types |
| `VerticesLifetimeProbe.cs` `array` arm | the helper, on purpose | the defect under test, spawned only under the variable |
| `VerticesLifetimeProbe.cs` `surface`, `fill`, `DrawHeld` | `SKSurface`, `SKPaint`, `SKVertices` | safe: `using var` |
| `src/` outside `SurfaceRenderer.cs`, `tests/` outside the probe | Avalonia drawing (`DrawingContext`, `RenderTargetBitmap`) only | no SkiaSharp native temporary; Avalonia owns those lifetimes |

No fix outside `SurfaceRenderer.cs`, so no seam request. The claim that the defect is gone rests on the paired loop on both
rendering paths, run by the main session after this change.
