---
id: proof-dock-split-s8
title: "S8 spike: can Dock capability overrides block split drops"
type: proof-pack
status: in-review
owner: "@track-s8"
phase: design — S8 before G0 (app-shell build)
tags: [dock, docking, split, capability, candrop, spike, s8]
links:
  - { to: design-app-shell, rel: documents }
review-by: 2026-10-27
summary: >-
  Drives Dock 11.3.12.1 DockManager.IsDockTargetVisible and ValidateDockable with no UI, then executes each drop and
  prints the tree. CanDrop and every capability override or policy block all drops (tab, split and float alike), so they
  cannot block splits alone. The documented AllowedDockOperations mask (Fill|Window) on the dragged pane blocks every
  edge split and keeps tab and float drops. The GUI drag gesture was not driven.
---

# S8 spike: can Dock capability overrides block split drops

**Question.** In Dock for Avalonia 11.3.12.1, can `CanDrop`, `DockCapabilityOverrides` or any documented public
capability block **split** drops (a pane dropped on a group edge, which creates a new split group), while still
allowing tab drops into an existing group and floats? Context: `docs/design/app-shell.md` §3.4, §5.2, failure row
"Unexpected split".

## Verdict

- **split drops blocked by CanDrop/capability overrides: no** — `CanDrop=false`, `DockCapabilityOverrides{CanDrop=false}`,
  `DockCapabilityPolicy{CanDrop=false}` and `RootDockCapabilityPolicy{CanDrop=false}` reject **every** operation on the
  target, including `Fill` (tab) and `Window` (float). They are all-or-nothing per target.
- **split drops blocked by a documented public capability (`IDockableDockingRestrictions.AllowedDockOperations =
  Fill|Window` on the dragged pane): yes, on the DockManager validation path.** Every edge operation
  (`Left/Right/Top/Bottom`) on every target kind returned `IsDockTargetVisible=False`, `ValidateDockable=False`, and the
  executed tree was unchanged. `Fill` still made a tab and `Window` still made a float window.
- **Not assessed:** a real mouse drag in the GUI. The link from the adorner to these calls is the IL scan below, which
  shows that the UI states call the same `IDockManager` methods. It does not show a drop gesture.

## Evidence (verbatim from `probe-output.txt`)

Package: `DOCK_MODEL 11.3.12.1 DOCK_MVVM 11.3.12.1`.
Layout: `Root:Root[Ph:Top[TD:LeftDock[T:A,T:B],|,DD:Docs[D:Doc],|,TD:RightDock[T:C]]]`. The source is tool `C`.
`TD` is a tool group, `DD` a document group, `Ph`/`Pv` a split container, `|` a splitter.

1. Baseline edge drop creates a new split group:
   `CASE set=[baseline] target=tgtDock op=Left | IsDockTargetVisible=True ValidateDockable(execute:false)=True ... execute=True ... TREE Root:Root[Ph:Top[TD[T:C],|,TD:LeftDock[T:A,T:B],|,DD:Docs[D:Doc]]]`
2. `CanDrop=false` also blocks the tab drop:
   `CASE set=[tgtDock.CanDrop=false] target=tgtDock op=Fill | IsDockTargetVisible=True ValidateDockable(execute:false)=False | LastCapabilityEvaluation=Drop:False:Dockable | execute=False`
   The same holds for `DockCapabilityOverrides` (`Drop:False:DockableOverride`), `DockCapabilityPolicy`
   (`Drop:False:DockPolicy`) and `RootDockCapabilityPolicy` (`Drop:False:RootPolicy`), for `Fill` and `Window` alike.
3. Source mask blocks the split and keeps tab and float:
   `CASE set=[src.AllowedDockOperations=Fill|Window] target=tgtDock op=Left | IsDockTargetVisible=False ValidateDockable(execute:false)=False ... execute=False ... TREE Root:Root[Ph:Top[TD:LeftDock[T:A,T:B],|,DD:Docs[D:Doc],|,TD:RightDock[T:C]]]`
   `CASE set=[src.AllowedDockOperations=Fill|Window] target=tgtDock op=Fill | ... execute=True ... TREE Root:Root[Ph:Top[TD:LeftDock[T:A,T:B,T:C],|,DD:Docs[D:Doc]]]`
   `CASE set=[src.AllowedDockOperations=Fill|Window] target=tgtDock op=Window | ... execute=True toolDocks=1 floatWindows=1`
4. The same mask blocks edge drops on the document group, the split container and the root
   (`target=docsDock|topDock|root op=Left..Bottom`: all `IsDockTargetVisible=False ValidateDockable(execute:false)=False`).
5. The target-side mask `AllowedDropOperations=Fill|Window` blocks edges only on the dockable that carries it
   (`set=[tgtDock.AllowedDropOperations=Fill|Window] target=tgtTool op=Left` → `True`). The source-side mask is the one
   that covers every target.
6. UI routing (IL call scan of `Dock.Avalonia` 11.3.12.1):
   `UICALL Dock.Avalonia.Internal.DockControlState.IsDockTargetVisible -> IDockManager.IsDockTargetVisible`,
   `UICALL Dock.Avalonia.Internal.DockManagerState.Execute -> IDockManager.ValidateDockable`,
   `UICALL Dock.Avalonia.Internal.DockControlState.ValidateGlobal -> IDockManager.ValidateDockable`,
   and the same pair in `HostWindowState` and `ManagedHostWindowState`.

## Findings beside the question

- Dropping on a **tool** (not its group) with any edge operation made a tab, not a split
  (`set=[baseline] target=tgtTool op=Left ... TREE ...TD:LeftDock[T:A,T:C,T:B]...`).
- `Window` onto the root dock is rejected even at baseline (`set=[baseline] target=root op=Window ... =False`).
- `Fill` onto the **root** dock at baseline, and under the source mask, adds a new tool group as a direct child of the
  root: `TREE Root:Root[Ph:Top[...],TD[T:C]]`. The mask does not block it because it is a `Fill`. Whether the GUI ever
  offers a root `Fill` target is not assessed.
- `Fill` onto a document group puts the tool among the documents (`DD:Docs[D:Doc,T:C]`).

## Method and reproduction

- `Program.cs` builds a fresh `Dock.Model.Mvvm.Factory` layout per case. It applies one override set and calls
  `new DockManager(new DockService())` → `IsDockTargetVisible(src, target, op)` and
  `ValidateDockable(src, target, DragAction.Move, op, bExecute: false)`. It reads `LastCapabilityEvaluation`. On a
  second fresh layout it calls `ValidateDockable(..., bExecute: true)` and prints the tree and `Root.Windows.Count`.
- Grid: 10 override sets × 5 targets (`tgtDock`, `tgtTool`, `docsDock`, `topDock`, `root`) × 6 operations = 300 `CASE`
  lines.
- The IL scan reads each `Dock.Avalonia` method body for `call`/`callvirt` tokens and resolves them. It is a byte scan,
  so it lists call sites; it does not trace runtime order.
- Run: `./run.sh` (macOS, .NET SDK 10.0.203 pinned by `global.json`). It builds and writes `probe-output.txt`.
- The design and `LayoutDocument` are unchanged here. The Coordinator amends §3.4 after an Owner ruling.
