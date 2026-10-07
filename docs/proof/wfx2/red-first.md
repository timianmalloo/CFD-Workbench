---
id: proof-wfx2-red-first
title: "WFX2 red-first receipts"
type: proof-pack
status: active
owner: "@trk-wfx2"
tags: [windows, wfx, red-first]
links:
  - { to: proof-win-smoke-reverify, rel: relates-to }
review-by: "2026-11-07"
summary: "For each WFX2 item: the check that failed on the old code and passed on the new."
---

# WFX2 red-first receipts

Each entry: the check, the observed result before the fix (red), the observed result after (green). All on macOS arm64.

## Item 1 - desktop crash frame

Check: `SelfLaunchTests` case "crash detail names the message and a frame; product shape stays type-only"
(`tests/CfdWorkbench.Desktop.Tests/SelfLaunch.cs`).

- Red: `dotnet build tests/CfdWorkbench.Desktop.Tests -c Release` fails with
  `error CS0117: 'StartupFailure' does not contain a definition for 'Describe'` (old handler printed the type only and had no testable seam).
- Green: Desktop harness (default mode) prints `SelfLaunchTests: all 7 cases passed.` and exits 0.

Finding: the Windows ring's `APP-UNHANDLED APP-CRASH System.Exception` is reproduced on macOS when `TMPDIR` is the symlinked
`/var/folders/...` (the ring uses a non-symlinked `.tmp-tests`). With the frame now printed it reads
`System.Exception: Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` at `WorkbenchTests.cs` main flow
(the `workbench.SaveAsync(conflictPath)` check). On Windows the cause is the store refusing every path
(`ProjectStore.Supported()`): the PC's W-2 B2 (Rulings 136, 137), not fixed here.
The harness now also prints `STAGE <name>` before each main-flow stage, so the last STAGE or SUITE line names the crashing stage.

## Item 2 - JSON line endings

Check: Core `LayoutCodec_IndentedJson_PinsLfNewLine` (`tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs`). `Environment.NewLine` cannot be forced to
`\r\n` on macOS, so the failing part on old code is the structural scan (every `src` file that indents JSON must pin `NewLine = "\n"`).

- Red: `FAIL LayoutCodec_IndentedJson_PinsLfNewLine InvalidOperationException: indented JSON without NewLine = "\n": CfdWorkbench.Persistence/PreferenceStore.cs, .../LayoutCodec.cs, .../RecentList.cs, CfdWorkbench.Core/AuthoringSession.cs, CfdWorkbench.Cli/Program.cs`
- Green (after pinning the five sites and `RunStoreTests.NoRunByteIdentical`): `PASS LayoutCodec_IndentedJson_PinsLfNewLine`.

## Item 3 - symlink privilege in tests

Check: Core `PrefStore_DirectoryLink_WindowsBranchUsesJunction` forces the Windows branch with a fake runner and asserts the command is `cmd.exe /c mklink /J <link> <target>` and that the macOS branch never shells out and creates a real link.

- Red: the helper `TryDirectoryLink` did not exist; the old sites called `Directory.CreateSymbolicLink` directly (Windows ring: `IOException: A required privilege is not held by the client`). The check was written with the helper, so its red is the missing member (compile error), not a run.
- Green: `PASS PrefStore_DirectoryLink_WindowsBranchUsesJunction`; `PASS PrefStore_SymlinkedDirectory_SessionOnly` and `PASS PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites` unchanged on macOS.

## Item 4 - catalog refusal names its check

Check: Core `Catalog_Refusal_NamesItsCheck` plants five mismatches (hash, missing resource, one flipped byte with a matching hash, recorded LE shift, short row) and reads `Data["check"]` / `Data["detail"]` of the CAT-UNAVAILABLE error. The user copy (COPY-135, `Reason`) is unchanged. The Core harness FAIL line now appends `Data` as `[check=...; detail=...]`.

- Red: `FAIL Catalog_Refusal_NamesItsCheck InvalidOperationException: Expected coordinate-hash; actual ` (no Data on the old error).
- Green: `PASS Catalog_Refusal_NamesItsCheck`; the five existing `Catalog_*` checks still PASS (equality policy unchanged).

## Item 5 - F10, Escape focus, undo status text

Checks (Desktop `--shell-window`, `tests/CfdWorkbench.Desktop.Tests/WindowsShellTests.cs`): `WindowsShell_F10_FocusesMenu_EscapeReturnsFocusToOrigin` (a visible `Menu` in a plain window, `MenuBarKeys` installed), `WindowsShell_MenuBarKeys_WiredOffMacOnly`, `WindowsShell_Undo_StripKeepsTheUndoText_AfterSampling` (a MainWindow built with `macOS: false`).

- Red, with `MenuBarKeys` an empty stub and the controller unchanged:
  - `FAIL WindowsShell_F10_FocusesMenu_EscapeReturnsFocusToOrigin InvalidOperationException: F10 handled False; focus Button is not in the menu` (the PC observation: F10 leaves focus on Evaluate)
  - `FAIL WindowsShell_F10_IsWiredOffMacOnly InvalidOperationException: F10 is not handled by the Windows shell` (first form of the wiring check; replaced, see below)
  - `FAIL WindowsShell_Undo_StripKeepsTheUndoText_AfterSampling InvalidOperationException: After Undo and sampling the strip reads 'Accepted η 0.5 slice; 15 measured display points in 153 ms. Segment interpolation error is Not assessed.' (provenance accepted)`
- Premise check (measured, not assumed): the brief says macOS shows an undo text. On this build macOS (and the forced non-macOS branch) shows `Sampling accepted geometry at η 0.5…` immediately after Undo and `Accepted η 0.5 slice; ...` once settled; the undo text is overwritten in the same call by `RefreshAcceptedAsync`. No platform ever showed it. The fix keeps the existing sentence (minus its stale `Sampling…` tail) by refreshing with `preserveStatus: true`; it applies to macOS too. Operator: this is a visible copy change.
- Wiring-check oracle changed: a plain Avalonia window with a Button returns `Handled=True` for F10 (measured), so `Handled` cannot tell Windows from macOS; the check asserts `ShellHost.MenuKeys` is set off macOS and null on macOS.
- Green: all three PASS; the other seven `WindowsShell_*` checks PASS.

## Item 6 - Imperial spanwise table

Checks (Desktop `--analysis`, `AnalysisPanelTests.cs`): `LoadingChart_TableTwin_Imperial_ReadsLbfPerFt` (header `L/span lbf/ft`, 12.5 N/m reads `0.857`, no `N/m` cell), and the wiring assertions added to `Analysis_Units_BandSpeedFollowsResults_ConditionsSummaryAndHistoricalChip` (panel header follows the controller units).

- Red: `LoadingChart.TwinCells` and `Update(points, Units)` did not exist (compile error CS1061 / CS1501); the old header was the literal `"L/span N/m"` and the value unconverted.
- Green: `PASS LoadingChart_TableTwin_Imperial_ReadsLbfPerFt`, `PASS Analysis_Units_BandSpeedFollowsResults_ConditionsSummaryAndHistoricalChip`. The conversion is `Labels.ForcePerSpan` (one definition), applied in the chart; the projection stays in N/m.
