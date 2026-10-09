---
id: proof-sdg
title: "SDG scale-diagnostic mode receipt"
type: proof-pack
status: in-review
owner: "@trk-sdg"
phase: implementation
tags: [scale, diagnostic, ruling-184]
links:
  - { to: proof-wri-probe-windows-scale, rel: relates-to }
review-by: 2026-10-28
summary: >-
  The --scale-diagnostic mode prints one SCALE_CONTEXT line, runs no check and exits 0.
  Red first, the Mac run and why the default ring never runs it.
review-suggested: []
---
# SDG receipt: `--scale-diagnostic` (Ruling 184, option A)

## Red first

Check `ScaleDiagnostic_PrintsOneScaleLine_NoCheckLines_ExitsZero` (in `StageTimingTests.cs`). Ring: fast (in-process, pure over a
fake line source). Cost: 0.3 ms measured (`COST` line), under the 5 ms budget.

- Red (before `DesktopChecks.ScaleDiagnostic` existed):
  `dotnet build -c Release tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj` gave
  `StageTimingTests.cs(52,38): error CS0117: 'DesktopChecks' does not contain a definition for 'ScaleDiagnostic'`.
- Green: `CFD_TEST_ONLY=ScaleDiagnostic_ tools/run-suite.sh dotnet <dll>` printed
  `PASS ScaleDiagnostic_PrintsOneScaleLine_NoCheckLines_ExitsZero`. The `FAIL SELECTOR ... matched no check` lines in that run
  come from the eight spawned children, which do not hold the check; the same happens for any parent-only selector
  (`Spawn_WindowMode...` prints it too).

## Kept out of the default ring

The mode is a branch in the `args` chain only. It is in no `Spawn(...)` call, not in `ScaleModes` (which lists the modes Spawn
requires a SCALE_CONTEXT line from), not in `--readiness`, and `tools/run-tests.sh` never passes it. No existing control changed.

## Real run on the Mac

Command (from the worktree root, after `dotnet build -c Release` of the Desktop test project):

    dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --scale-diagnostic

Output:

    NATIVE-STARTUP app-initialize
    NATIVE-STARTUP lifetime=none
    SCALE_CONTEXT mode=scale-diagnostic RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x892 UseLayoutRounding=True

Exit code 0. Wall time 0.39 s. The two `NATIVE-STARTUP` lines come from App initialisation and appear in every window mode; the
mode prints exactly one `SCALE_CONTEXT` line, no `PASS`/`FAIL` line, and runs no check. (Mac values: Retina RenderScaling=2.)
