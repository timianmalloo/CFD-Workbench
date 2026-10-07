---
id: proof-wfx-pc-reverify
title: "WFX - the Windows walk the PC re-runs"
type: proof-pack
status: active
owner: "@trk-wfx"
phase: implementation
tags: [windows, w-1, proof]
links:
  - { to: proof-win-smoke, rel: relates-to }
summary: "The exact Windows walk the PC re-runs after the W-1 defect fixes, with what each step should now show."
review-by: "2027-01-07"
---

# WFX - what the PC re-runs on Windows

Branch `fix/windows-w1-defects` (after the leader joins it). Same machine and capture method as
`docs/proof/win-smoke/receipt.md`. The Mac proved the code paths by forcing the non-macOS branch
(`docs/proof/wfx/red-first.md`); this walk is the part a Mac cannot see. Record what is seen, including any step that
still fails. Save-to-disk itself is not expected to work: persistence on Windows is W-2.

## 0. Test ring

1. Run `tools/run-tests.sh` in Git Bash. Before: it stopped at `tools/run-tests.sh:49` with exit 49 (`python3` was the
   Store alias). Now: it starts, builds, and reaches the harnesses. Run `tools/py-resolve.sh --self-test`: 4/4 PASS.
   Note which of `python3`, `py -3`, `python` the resolver picked (`bash -c '. tools/py-resolve.sh; py_resolve; echo $PY_RESOLVED'`).

## 1. The W-1 walk (same steps, new expectations)

| Step | Input | Before (W-1) | Should now show |
| --- | --- | --- | --- |
| Launch | start the app | no menu in the window | a menu bar row above the dock: File, Edit, View, Section (the section editor's menu), Window; the status strip still at the bottom; nothing overlaps the title bar |
| Default wing | New foil | opens | unchanged |
| Drag one point | spanwise drag of the leading-edge root handle | length 166.67 to 226.40 mm | unchanged |
| Point name | read the dragged point's UI Automation name (same node as before the drag, then again) | stale: 166.67 mm | the same node now reads 226.40 mm; a name-changed event is raised (Accessibility Insights or an `AutomationPropertyChangedEventHandler`) |
| Undo | Ctrl+Z (`SendKeys '^z'`) | no effect | the drag is undone: length back to 166.67 mm, area back to ~1000 cm2, status reports the undo; pressing Ctrl+Z once undoes one step, not two |
| Redo | Ctrl+Shift+Z | not tried | the drag is redone |
| Edit menu | open Edit with the mouse | no menu | Undo and Redo listed with `Ctrl+Z` / `Ctrl+Shift+Z`; Undo enabled or disabled as the history is |
| Analysis | switch to Analysis, Evaluate | worked | unchanged |
| Save | Ctrl+S | nothing visible | the Save picker opens (`Save native CFD Workbench project`). Choose a `.cfdw.json` path: the strip shows `DOC-UNSUPPORTED-PERSISTENCE: Save was not acknowledged. Resolve the refusal before retry.` (expected; W-2). Choose a path without `.cfdw.json`: the strip shows `DOC-TYPE: Save was not acknowledged...` in the error style |
| Save from the menu | File > Save | not reachable | the same picker and the same strip text as Ctrl+S |

## 2. Menu access from the keyboard (new)

| Step | Input | Should show |
| --- | --- | --- |
| Alt | press and release Alt | the menu bar takes focus; File is highlighted |
| Arrow | Right, Down | moves across the top level, opens a menu, items show their gestures as `Ctrl+...` (never the Mac symbols) |
| Escape | Escape | the menu closes; focus returns to where it was |
| F10 | F10 | the same as Alt |
| Alt+F | Alt then F, if the menu shows access keys | File opens (note if it does not; the table has no access keys defined) |
| Click | click File, then New foil | runs once |

## 3. Each gesture fires once (the double-fire check a Mac cannot make)

For these, press the key once and count what happened. A command that runs twice would show as a doubled effect.

| Key | Expected single effect |
| --- | --- |
| Ctrl+Z | one undo step |
| Ctrl+Shift+Z | one redo step |
| Ctrl+B | the left side bar toggles once (shown, then hidden; not back to the same state) |
| Ctrl+J | the bottom panel toggles once |
| Ctrl+K | the command palette opens once |
| Ctrl+= and Ctrl+- | zoom steps once each |
| Ctrl+1, Ctrl+2, Ctrl+3 | the workspace changes once |
| Ctrl+Shift+A | Analysis toggles once |

Also confirm the Mac symbols appear nowhere in the Windows menu text.

## 4. Not in scope of this walk

Persistence (W-2), packaging, floating windows and their menus, multi-monitor behaviour (ADR-0009 S1 remainder), and
screen-reader reading order of the new bar.

## If a step fails

- Ctrl+Z works but a menu item also fires a second time: the bar's own menu items bind their gesture. Report which keys
  double; the fix is to stop binding on the window for the bar's items (one line in `NativeMenuBuilder.ShowInWindow`).
- The bar is absent or has no height: report `NativeMenu.GetIsNativeMenuExported` for the window (should be false on
  Windows) and a screenshot.
- Alt or F10 do nothing: report whether the bar is focusable by Tab (F6 ring) and whether a click opens a menu.
