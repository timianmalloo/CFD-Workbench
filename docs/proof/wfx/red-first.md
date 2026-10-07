---
id: proof-wfx-red-first
title: "WFX red-first receipts (W-1 Windows defects)"
type: proof-pack
status: active
owner: "@trk-wfx"
phase: implementation
tags: [windows, w-1, proof]
links:
  - { to: proof-win-smoke, rel: relates-to }
summary: "Red-first receipts for the four W-1 Windows defect fixes, run on macOS with the non-macOS branch forced."
review-by: "2027-01-07"
---

# WFX red-first receipts

Track `trk-wfx`, branch `fix/windows-w1-defects`, run on macOS (arm64). Each fix has a check that ran red on the old
behaviour and green on the new. For item 1 the old behaviour is the committed code; for items 2-4 the old behaviour is
planted back as a one-line mutant (the real old code is not recoverable without removing the new seams), then restored
with `git checkout`. Every mutant below was observed red; the tree was clean after each (`RESTORED: 0 changed paths`).
Windows itself was not run: the checks force the non-macOS branch through `new MainWindow(null, macOS: false)` and
`NativeMenuBuilder.ParseGesture(gesture, macOS: false)`.

## Item 1 - the python resolver (`tools/py-resolve.sh`)

Red (old code, committed `a5d8f034`, with a fake `python3` first on PATH that prints the Store-alias message and exits 9009):

```
PATH="$S/fake:$PATH" tools/run-tests.sh      ->  exit 49
RING-LOCK waited 0 s
Python was not found; run without arguments to install from the Microsoft Store
```

Exit 49 is 9009 mod 256, the code the PC saw. Green (`tools/py-resolve.sh --self-test`, exit 0):

```
SELFTEST PASS Store-alias python3 is skipped, py -3 chosen
SELFTEST PASS alias py is skipped too, python chosen
SELFTEST PASS working python3 wins first
SELFTEST PASS run-tests.sh and join-ring.sh call no bare python3
SELFTEST 4/4 cases
```

Mutant: `now_ms() { python3 -c ...` put back in `tools/run-tests.sh` ->
`SELFTEST FAIL run-tests.sh and join-ring.sh call no bare python3: expected '0', got '1'` (3/4).

## Items 2-4 - mutants run against `WindowsShellTests` (Desktop `--shell-window` mode)

| Mutant (planted, then restored) | Check that went red | Observed |
| --- | --- | --- |
| A. `ShowInWindow` binds no keys (the W-1 state: gestures only on the menu item) | `WindowsShell_CtrlZ_UndoesExactlyOneStep`, `WindowsShell_EveryTableGesture_FiresItsCommandOnce` | `After one Ctrl+Z: can undo True, can redo False`; `New foil [Ctrl+N]: fired 0, expected 1; ... Save [Ctrl+S]: fired 0` and every other table gesture |
| B. no in-window bar off macOS | `WindowsShell_MenuBar_InWindowOffMac_AbsentOnMac`, `..._CtrlZ_...`, `WindowsShell_MenuReachableFromKeyboard_AltAndF10` | `The Windows shell has no in-window menu bar`; Ctrl+Z not fired; NullReferenceException on the missing bar |
| C. bar and bindings on macOS too | `WindowsShell_MenuBar_InWindowOffMac_AbsentOnMac` | `macOS has an in-window menu bar beside the system menu` |
| D. save refusal not reported to the strip | `WindowsShell_SaveRefused_ShowsMessageInStatusStrip` | strip kept `Accepted η 0.5 slice; ...` (kind info) while the save threw `DOC-TYPE` |
| E. plan point name from the captured snapshot | `PlanCanvas_RetainedPointPeer_NameFollowsTheDrag_AndRaisesNameChanged` | retained peer `aft 109.03 mm`, expected `aft 113.03 mm` |
| F. no name-changed event | same check | `The retained peer raised no name-changed event` |
| G. elevation name from the snapshot | `ElevationView_PointName_ReadsTheLivePointNotTheSnapshot` | `The twist point name did not follow the drag` |
| H. `ParseGesture` ignores the platform flag | `WindowsShell_Gesture_CommandKeyMapsToControlOffMac` | `⌘Z off macOS parsed as Cmd+Z` |

Green on the committed tree (`CFD_TEST_ONLY="WindowsShell,PlanCanvas_Retained,ElevationView_PointName"`): 9 PASS, 0 FAIL.
Each check costs 0.1-2.1 s; the window checks together add about 7 s to the `--shell-window` job.

## What these checks cannot show

- Key presses are delivered by the test the way Avalonia's `KeyboardDevice` does for key bindings (focused element up to
  the window, first matching binding handles). A raw platform key event cannot be injected here (the API is internal).
- The `NativeMenuBar` draws nothing where the platform exports the menu, and a Mac exports it, so the bar has no height
  in these windows. That the bar draws the table, that Alt and F10 open it, and that a gesture does not also fire a
  second time through the bar's own menu items (a double fire) are Windows-only facts: Inferred, and the PC's to check
  (`pc-reverify.md`). The single-fire count in these checks covers the key bindings, not a bar-side hotkey.
- Finding against `docs/reviews/pr-3.md` row c': on the result path the controller already writes
  `DOC-UNSUPPORTED-PERSISTENCE: Save was not acknowledged...` to the strip (verified here with a relative path, which
  `ParentPath.Open` refuses as a Windows drive path would be). Only a save that THROWS (`DOC-TYPE`,
  `DSL-INVALID-NUMERIC`, `DOC-SAVE-PENDING`, an I/O exception) reached stderr alone; that is what item 3 routes.
