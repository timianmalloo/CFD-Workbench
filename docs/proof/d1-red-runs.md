---
id: proof-d1-red-runs
title: D1 shell model red-first runs
type: proof-pack
status: in-review
owner: "@track-d1"
phase: implementation
tags: [app-shell, desktop, shell-model, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: Red-first run for track D1 (Shell model) of the app-shell build — proves that the spawned --shell-model suite fails and turns tools/run-tests.sh red with exit code 1.
---

# D1 Shell Model: red-first run

Track D1 implements the pure model code for the app shell (`docs/design/app-shell.md` §3, §5.1, §6.4–§6.6, §9, §12.4, §14).
This proof pack verifies that the spawned `--shell-model` suite makes `tools/run-tests.sh` exit nonzero when any named check fails.

## 1. Red Run: `tools/run-tests.sh` exits 1

The 12 named checks were committed with stub implementations in `src/CfdWorkbench.Desktop/Shell/` throwing `NotImplementedException("track D1 stub")`.
Seam 1 (`named=" Core Desktop "`) was added to `tools/run-tests.sh`.

```text
$ tools/run-tests.sh
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.36
build 2 s (Release)
== CfdWorkbench.Core.Tests 28 s, 236 PASS
RESULT failures=0
== CfdWorkbench.Cli.Tests 1 s, 0 PASS
CLI Example identity, assessment, bounded projection and extension refusal passed.
== CfdWorkbench.Desktop.Tests 26 s, 1 PASS
FAIL Preset_EveryPaneSubset_ExactlyOnePlacement NotImplementedException: track D1 stub
FAIL ProportionFor_Bounds NotImplementedException: track D1 stub
FAIL FloatFrame_Scaling1_15_2_Exact NotImplementedException: track D1 stub
FAIL FloatRestoreSnapshot_NoDrift NotImplementedException: track D1 stub
FAIL Clamp_ScreenGone_FullyInsidePrimary NotImplementedException: track D1 stub
FAIL Clear_NearestClearCorner_Chosen NotImplementedException: track D1 stub
FAIL Clear_TieBreak_FixedOrder NotImplementedException: track D1 stub
FAIL Clear_TwoFloats_NoOverlap NotImplementedException: track D1 stub
FAIL Clear_NeverOverlapsTarget_Property NotImplementedException: track D1 stub
FAIL Clear_FloatLargerThanModelArea_DocksBack NotImplementedException: track D1 stub
FAIL CommandTable_Parity_EveryRowInMenuPaletteKey NotImplementedException: track D1 stub
FAIL --shell-model exited 1
SectionToolsTests: all 6 scenarios passed.
Section tools tests passed.
FAIL Preset_EveryPaneSubset_ExactlyOnePlacement NotImplementedException: track D1 stub
FAIL ProportionFor_Bounds NotImplementedException: track D1 stub
FAIL FloatFrame_Scaling1_15_2_Exact NotImplementedException: track D1 stub
FAIL FloatRestoreSnapshot_NoDrift NotImplementedException: track D1 stub
FAIL Clamp_ScreenGone_FullyInsidePrimary NotImplementedException: track D1 stub
FAIL Clear_NearestClearCorner_Chosen NotImplementedException: track D1 stub
FAIL Clear_TieBreak_FixedOrder NotImplementedException: track D1 stub
FAIL Clear_TwoFloats_NoOverlap NotImplementedException: track D1 stub
FAIL Clear_NeverOverlapsTarget_Property NotImplementedException: track D1 stub
FAIL Clear_FloatLargerThanModelArea_DocksBack NotImplementedException: track D1 stub
PASS FocusRing_HiddenRegionsAndFloats_Order
FAIL CommandTable_Parity_EveryRowInMenuPaletteKey NotImplementedException: track D1 stub
SUITE --shell-model exit 1
FAIL --shell-model exited 1
SUITE --controller-shell exit 0
NATIVE-STARTUP app-initialize
NATIVE-STARTUP lifetime=none
SUITE --shell-window exit 0
FAILED: CfdWorkbench.Desktop.Tests (exit 1)
wall 30 s (budget 60 s)
```

Exit code: `1`

The spawned `--shell-model` child process exited with code 1 due to 11 failing stub checks, which caused `CfdWorkbench.Desktop.Tests` to exit with code 1, and `tools/run-tests.sh` terminated with exit code 1.
