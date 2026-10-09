---
id: proof-msp-red-first
type: proof-pack
title: "MSP red-first runs"
status: complete
summary: "Red-first runs for the SCALE_CONTEXT control."
owner: "@trk-msp"
links:
  - { to: proof-msp-receipt, rel: relates-to }
review-by: 2026-12-01
---

# MSP red-first (Ruling 182)

Control: `Spawn_WindowModeWithoutScaleContext_Fails` (`tests/CfdWorkbench.Desktop.Tests/StageTimingTests.cs`), backed by the
check in `DesktopChecks.SpawnWith` (`WorkbenchTests.cs`). Ring: fast (in-process, before the spawn). Cost: 1.1 ms.

## Red A: a mode loses its print (the real failure shape)

Change: deleted the `PrintScaleContext("views")` line from the `--views` mode entry; Release build; default Desktop run
(`tools/run-suite.sh dotnet <Desktop dll>`). Exit 1. Output, `red-views-print-removed.stdout.txt`:

```
FAIL SCALE_CONTEXT --views --part=1/2 printed no SCALE_CONTEXT line before its first check
FAIL SCALE_CONTEXT --views --part=2/2 printed no SCALE_CONTEXT line before its first check
```

With the print restored the same run exits 0 (`desktop-all-modes.stdout.txt`).

## Red B: the control itself is disabled

Change: `if (false && ScaleModes.Contains(...))` in `SpawnWith`; `CFD_TEST_ONLY=Spawn_WindowMode` run. Output,
`red-control-disabled.stdout.txt` (the later `FAIL SELECTOR` and `exited 1` lines are the children refusing the selector, expected):

```
FAIL Spawn_WindowModeWithoutScaleContext_Fails Exception: a window mode with no SCALE_CONTEXT line did not fail: ...
```

Restored: `PASS Spawn_WindowModeWithoutScaleContext_Fails`.
