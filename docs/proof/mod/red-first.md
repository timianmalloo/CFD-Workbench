---
id: proof-mod-red-first
title: "MOD red-first receipt"
type: proof-pack
status: active
owner: "@trk-mod"
phase: implementation
tags: [mod, row-modal-a, command-table]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  The sweep, the red run of the modal-flag guard, the green run, and the scratch unflag of section.export-dat.
---

# MOD: one modal flag for command rows (ROW-MODAL-A)

## Sweep (item 1): tests that run commands from the table

| Test | Executes rows? | Now |
|---|---|---|
| `WindowsShellTests` `WindowsShell_EveryTableGesture_FiresItsCommandOnce` | yes, presses every gesture | filters by `row.Modal`; the `ShowExportDialog` hook is gone |
| `PointsPaneTests.SectionCommands_EveryRow_RunsOrNamesReason` (readiness) | yes, `RunCommand` on every shell row | filters by `row.Modal`; the hard-coded list is gone (Finish and Cancel stay apart: they end the mode) |
| `ControllerSectionTests.Commands_ModalFlag_MatchesRowsThatShowADialog` | yes, every row, with recording hooks | new guard |
| `ControllerSectionTests.Commands_SectionMenu_ReplaceSaveImportRowsRun`, `ExportTests`, `UnitsSwitchTests`, `PropertiesCellsTests`, `ControllerViewTests`, `ShellModelTests` | named ids only | unchanged |
| `ShellWindowTests`, `ShellModelTests`, `AnalysisPanelTests`, `AnalysisToggleTests`, `PlanCombTests` | read rows (titles, gestures, menus) | unchanged |

`file.open`, `file.save` and `file.save-as` open pickers from `MainWindow.OnAction`, not from `ShellHost.RunCommand`; the
probes do not run them as commands, so they are not flagged Modal.

## Red (guard added, flag defaulting false, no row flagged)

`docs/proof/mod/run-red.txt`:
`FAIL Commands_ModalFlag_MatchesRowsThatShowADialog ... file.export opens the export dialog but is not flagged Modal; section.replace-catalog ...; section.save-mine ...; section.import-dat ...; section.export-dat ...`

## Green (five rows flagged)

`docs/proof/mod/run-green.txt`: `PASS Commands_ModalFlag_MatchesRowsThatShowADialog` (3.1 s).

## Scratch mutation: unflag `section.export-dat` only (the DAT defect), then restore

`docs/proof/mod/run-unflag-export-dat.txt`:
`FAIL ... section.export-dat opens the export dialog but is not flagged Modal`. Restored; the flag count in CommandTable.cs is 5.
