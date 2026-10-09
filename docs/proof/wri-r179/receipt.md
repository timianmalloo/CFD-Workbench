---
id: proof-wri-r179
title: "Ruling 179 Windows proof: blocked before product checks, restoration verified"
type: doc
status: blocked
owner: "@win-wri-r179"
tags: [windows, dpi, proof, blocked]
links:
  - { to: review-pr-21, rel: relates-to }
  - { to: proof-wri-probe-mac-join-note-pr21, rel: relates-to }
  - { to: plan-wri-r179, rel: depends-on }
review-by: "2026-11-09"
summary: >-
  Two preparation/runner repair cycles were exhausted before any of the 14 product checks ran.
  Settings was restored to exact 150% (Recommended); a fresh process measured RenderScaling 1.5
  and PrimaryScaling 1.5 with OS Process.ExitCode 0. All checks and item-6 bounds remain unassessed.
---

# Ruling 179 Windows blocked checkpoint

**BLOCKED, Verified.** The authorized two-scale proof was not delivered. The worker stopped at
repair cycle 2/2. No product check was executed, no 200% selection occurred, and no product test
was repaired or rerun. This checkpoint is evidence of the blocker and safe restoration, not
Windows acceptance or readiness admission.

Worktree: `C:/Projects/CFD-Workbench-win-wri-r179`; branch: `win/wri-r179-full-nine`.
Authorized and built head: `13cb9a14c8315c1b01f30e33b672c936b5b36726`.
No product-tested head exists for this attempt. Pinned SDK `10.0.203` was observed using
`%USERPROFILE%/.dotnet/dotnet.exe --version`. The instrumented build exited 0, in 35.6710529 s,
PID 25424, affinity `0x3F`. `source-hashes.json`, `instrumentation.diff.txt`, and `dll-hashes.json`
bind the temporary source and generated DLLs. The source was restored byte-for-byte; the
instrumented DLL remains a local generated artifact and was not committed.

## Preflight and restoration

`settings-preflight.stdout.txt` records the authorized UIA preflight before scale selection:
Display 1 selected True; Multiple displays Group; its `EntityItemButton` Show more settings
button initially Collapsed; expansion; reacquired
`SystemSettings_Display_MainMonitor_CheckBox` with ToggleState On; scale combo
`SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox` scrolled into view and reacquired.
ItemContainer queried exact `150% (Recommended)` and `200%` items and VirtualizedItem.Realize
was called. The selected 150% item's SelectionItem state was True. After reacquisition the
nonselected 200% item's state remained unavailable in the virtualized provider (printed blank);
no claim that its selection state was measured is made. Final SelectionPattern returned exact
`150% (Recommended)`. All observed IDs, supported patterns, and offscreen states are retained.

`settings-restored.stdout.txt` records exact `150% (Recommended)` selected and Display 1
main monitor On. Its fresh Settings child PID 17636 exited 0. The fresh diagnostic PID 20184
ran afterward and exited 0:

```text
R179_SCALE_CONTEXT RenderScaling=1.5 PrimaryScaling=1.5 WorkingArea=0, 0, 2560, 1528 UseLayoutRounding=True
```

It ran from `2026-10-09T04:08:05.7086260Z` to `2026-10-09T04:08:11.0142997Z`,
duration 5.3056737 s, affinity `0x3F`. No AppliedDPI predicate or registry mutation was used.
Settings and the in-process reading meet the restoration condition, Verified.

## Exact requested results

| # | Exact check | 150% | 200% |
|---|---|---|---|
| 1 | KeyBindings_MenuGesture_NotBound | NOT ASSESSED | NOT ASSESSED |
| 2 | ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView | NOT ASSESSED | NOT ASSESSED |
| 3 | ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack | NOT ASSESSED | NOT ASSESSED |
| 4 | Elevation_SideSelectedStation_RenderedFullWeight | NOT ASSESSED | NOT ASSESSED |
| 5 | View3d_SelectedStation_RenderedWidthAndChip | NOT ASSESSED | NOT ASSESSED |
| 6 | PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs | NOT ASSESSED | NOT ASSESSED |
| 7 | PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused | NOT ASSESSED | NOT ASSESSED |
| 8 | PropertiesPane_Density_EveryTargetAtLeast24 | NOT ASSESSED | NOT ASSESSED |
| 9 | PropertiesPane_Density_EveryInputDeclaresMinHeightOf24 | NOT ASSESSED | NOT ASSESSED |
| 10 | ModelArea_Views_SeparatedByGutterAndFramed | NOT ASSESSED | NOT ASSESSED |
| 11 | Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870 | NOT ASSESSED | NOT ASSESSED |
| 12 | View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock | NOT ASSESSED | NOT ASSESSED |
| 13 | Elevation_ChipBorderSampler_HoldsHalfOfASplitLine | NOT ASSESSED | NOT ASSESSED |
| 14 | View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne | NOT ASSESSED | NOT ASSESSED |

Item-6 PointAftInput, Value_p_eta, and presenter bounds were instrumented but never measured.
P3 clause prints were instrumented but never executed. Required items 2, 3, 4, and 5 have no
PASS evidence at 150%. Items 10 and 11 are registered in RunReadiness; temporary exact-name
gated routing in the requested `--views`/`--analysis` modes retained the exact CFD_TEST_ONLY
selection. No readiness checks or admission ran.

## Repair and process ledger

The two-cycle cap was exhausted; the 20-minute ceiling was not reached. See `cycle-ledger.txt`
for exact shell commands, boundaries, and observed exits. Child captures use a stable
System.Diagnostics.Process handle, redirected BaseStreams, WaitForExit, and numeric ExitCode.
The shell orchestrator and first Python preparation were launched outside that wrapper; the
first Python exit was not separately recorded. This missing process receipt is Flagged.

| Captured child | PID | Numeric OS Process.ExitCode |
|---|---:|---:|
| settings-preflight | 25344 | 0 |
| build (system SDK, failed) | 11392 | -2147450725 |
| build-pinned | 25424 | 0 |
| settings-150 | 5028 | 0 |
| settings-restored | 17636 | 0 |
| restoration-diagnostic | 20184 | 0 |
| source-restoration (PATH-resolved Python, failed cleanup) | 12476 | 2 |
| source-restoration-exact | 15856 | 0 |
| deadline-observation (read-only, after stop) | 36096 | 0 |

The execution shell returned numeric tool-reported exit 1, not a separately observed
System.Diagnostics.Process.ExitCode. No 150% or 200% check child was started.

## Source restoration and capture fidelity

`source-restoration.json` contains original, instrumented, and restored SHA-256 values for all
three temporary test files. Restored equals original for each. `source-restoration-exact.stdout.txt`
records byte-for-byte PASS and `git diff --exit-code -- src tests tools` exit 0. The final change
is docs only. **RING-SKIPPED** applies to the application ring at a docs-only join.

The capture folder has `* -text` attributes. All stdout/stderr bytes remain untouched except
the failed cleanup stderr: its exact user-home prefix is replaced with `%USERPROFILE%` in the
committed derivative. Command metadata has the same substitution. Raw originals are retained
locally outside the repository, and `command-sanitization.json` / `stderr-sanitization.json`
bind raw and derivative hashes. This is an explicit exception to verbatim raw-byte preservation
required by the repository PII gate; it is not described as raw output. No test output exists.
The capture manifest is generated from staged Git blobs and checked against committed blobs.

## Failure class and next-run requirement

Verified runner defect: `DateTime.Parse("2026-10-09T04:20:00Z")` produces a Local-kind value
on this Windows host (`deadline-observation.stdout.txt`, PID 36096, numeric exit 0).
At 04:12:17Z the direct comparison printed True; UTC-normalized comparison printed False.
Comparing it directly with `DateTime.UtcNow` compares clock fields with
different bases. The execution cutoff fired at 04:08:03Z before any check, then finally restored
150%. The read-only sweep finds exactly one parsed-deadline comparison in `execute.ps1:19`;
Settings and process scripts only record UTC timestamps. This is a runner failure, not a
product failure or actual 20-minute expiration.

**Proposed, not implemented:** before another authorized run, use DateTimeOffset with explicit
AssumeUniversal/AdjustToUniversal, or another verified UTC instant representation. Require a
planted timezone self-test that proves a future UTC deadline remains future under UTC and
America/Los_Angeles offsets and that a past deadline expires. Also require observed newline
handling and explicit executable identity for the pinned SDK/Python. No runner modification or
rerun followed the cap. A fresh execution budget requires leader authorization.

Verified: UIA preflight, pinned SDK/build, numeric captured child exits, 150% restoration,
and byte-for-byte source restoration. Verified: the mixed-Kind source comparison reproduces
the early-cutoff condition in a read-only observation. Inferred: no further product claim.
Flagged: all requested test outcomes, item-6
bounds, P3 clauses, unavailable virtualized 200% item state, and the first Python preparation
exit. The Mac leader retains readiness and acceptance authority.
