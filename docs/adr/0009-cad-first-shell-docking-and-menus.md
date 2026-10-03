---
id: adr-0009-cad-first-shell
title: "ADR-0009: the CAD-first shell uses Dock for Avalonia 11.3.12.1 with OS-window floats, Avalonia NativeMenu, one command table and our own layout file"
type: adr
status: proposed
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [desktop, avalonia, docking, dock, nativemenu, layout, preferences, adr, ux-31, ux-32]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-application-stack, rel: depends-on }
  - { to: review-ui-workbench-v9, rel: relates-to }
  - { to: review-ui-workbench-v10, rel: relates-to }
  - { to: mockup-workbench-v10, rel: relates-to }
  - { to: architecture-application, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Adopts Dock for Avalonia 11.3.12.1 (MIT; the last release for Avalonia 11) for tabbed, dockable and floating panes;
  a spike observed a floated pane as its own NSWindow. Menus use Avalonia NativeMenu, exported to the macOS menu bar in
  the spike. One command table feeds menus, toolbar, palette and shortcuts. Layout is saved in our own versioned file,
  not Dock's serializer (its System.Text.Json path failed and its Newtonsoft JSON stores CLR type names). Maximize,
  monitor clamping and focus-safe floats are ours to build. Windows, mixed-DPI and screen-reader spikes are scheduled.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# ADR-0009: CAD-first shell — docking, menus, commands and layout

- **Status:** Proposed (architect council 2026-09-26)
- **Date:** 2026-09-26
- **Deciders:** the operator (UX-31/32, float option (a)), Native Desktop lens, Data & Persistence (layout file),
  Security (dependency)
- **Context:** spec 1.6 B1 CAD area IA, B7 CAD-first window, F12, UX-30–UX-32, CAD-21; v9 review §5 item 2 (build
  conditions: OS-window floats, NativeMenu, per-workspace layout, clamping, Snap Layouts); ADR-0003 (Avalonia 11.x,
  pinned, "do not float packages", `0003-application-stack.md`:35, `:69`).

## Context

The as-built window is a fixed three-column grid (`src/CfdWorkbench.Desktop/MainWindow.axaml`:21) with a Navigator, a
`TabControl` of document tabs (`MainWindow.axaml`:63) and buttons in a title strip (`:10-14`). There is no docking, no
`NativeMenu`, no command table and no preference store (no `NativeMenu` or `SpecialFolder` use under `src/`). Spec 1.6
needs panes that dock left/right/bottom, tab, float as OS windows on another monitor, maximize and close from a tab
menu; three workspaces that remember their layouts across restarts with floats clamped to connected monitors; platform
menus and shortcuts; and a float that never hides a focused model-area control (option (a)).

## Decision

1. **Docking: Dock for Avalonia 11.3.12.1**, packages `Dock.Avalonia`, `Dock.Model.Mvvm`, `Dock.Avalonia.Themes.Fluent`,
   pinned exactly (ADR-0003). No Dock serializer package is referenced. Floats use the native host (`HostWindow`, an
   `Avalonia.Controls.Window`) enabled by registering our own `HostWindowLocator` (or `DockControl.InitializeFactory`);
   the managed in-window mode is not used. **Floats are owned by the main window** (Dock's owner policy for floating
   windows, `UseOwnerForFloatingWindows` / AlwaysOwned — read in Dock's XML docs, not yet run), so they stay in front of
   it, follow it into a full-screen Space and leave with it; S7 proves this. Each float's title is its pane's name. The
   model area is the one document; Properties, Browser, Points and Messages are tools with stable ids. Dock types stay
   inside the Desktop shell namespace; nothing else references them.
   - *Pinning (ADR-0003 "do not float packages"):* Dock brings about ten transitive packages (among them
     CommunityToolkit.Mvvm 8.4.0, enterprise council, from the nuspec). The Desktop project restores with a committed
     `packages.lock.json` in locked mode, so transitive versions are pinned too.
2. **Menus: per-window Avalonia `NativeMenu`, generated from the one command table.** The Application-level `NativeMenu`
   fills only the macOS app-name menu (Avalonia source: a key window with no window menu shows only the app menu), so it
   holds **About** only — Avalonia itself supplies Services, Hide, Hide Others, Show All and Quit there. Every window —
   the main window at startup and each float in `IFactory.OnWindowOpened` — gets its **own** `NativeMenu`, generated from
   the same command table, so the menu stays whole (File, Edit, Window present) when a float has focus; `NativeMenuBar`
   renders the same menu in-window on Windows (**Not assessed** until S1). No second menu definition exists. The command
   table places the Window menu (Minimize ⌘M, Zoom, Bring All to Front, the pane list); M1.2a proves it from the packaged
   `.app`, not from `dotnet run` (the spike ran the dll, `run-gui.sh`). **Deviation recorded at design-slice**
   (`docs/design/app-shell.md` §1): the carrier is per-window menus rather than one Application menu holding File/Edit/
   Window; the original intent — the menu stays whole when a float has focus, no second menu definition — is kept.
3. **One command table** (Command pattern): id, title, group, platform gesture, enabled predicate, execute. It feeds
   `NativeMenu`, the toolbar, the command palette and key bindings — installed on the main window **and every float
   window** — so a verb reachable in one place is reachable in all (CAD-21). The platform modifier comes from
   `TopLevel.PlatformSettings.HotkeyConfiguration.CommandModifiers` (observed `Meta` on macOS). Shortcuts never act
   while a text field has focus (B7); because macOS menu key equivalents fire before the focused control, the Edit
   verbs (Undo, Redo, Cut, Copy, Paste, Select All) go to a focused text field first and reach the document only when
   no text field has focus. A test row covers ⌘Z in a numeric field. The table and the selection model are recorded in
   the decision note `note-20260926-command-table-selection`.
4. **One selection state** owned by the controller (none · foil · station · point(s)); every view and the Properties
   pane observe it; Properties is a projection of Core read models (point view with derived type and tangent kind,
   station view, foil view, `WingEstimates`) and holds no model state (UX-30 "selection agrees across every view").
   **F6's region ring is owned by the shell** and includes float windows, activating the float when its region is next
   (today F6 cycles inside one window, `MainWindow.axaml.cs:339-360`).
5. **Layout persistence: our own file**, installation-scoped (`<per-user application data>/CFD-Workbench/layout/layout.json`
   — its own subdirectory, so it has its own store claim, design-slice §3.4/§4.4), versioned, one record per workspace: for each pane id its dock (left · right · bottom · float · closed), tab group,
   order and size; for floats the position and size in the platform's window units (the units `Window.Position`,
   `IDockWindow` X/Y and `Screen.WorkingArea` share — the spike shows Dock's X/Y equal `Window.Position`,
   `gui-native.txt`:5,7) plus a screen key (display name, bounds, scaling) used only as a hint. Load rebuilds the Dock
   model through the factory from pane ids. An unknown pane id drops that pane only; an unknown version or a parse error
   falls back to the workspace preset; each fallback is said once in Messages. A build never overwrites a layout file
   written by a newer version (it keeps the layout for the session only). Where publication is not proved for the
   platform (`DOC-UNSUPPORTED-PERSISTENCE`, ADR-0004), the layout is session-only and says so. It is Type-1 replaceable
   preference state, never document data (A3.1).
6. **Ours to build (the spike found no Dock support):** pane **Maximize** (restore on Escape); **clamping** floats into
   a connected screen's `WorkingArea` at launch and on `Screens.Changed`, in the same units as the saved position; and
   **focus-safe floats** (option (a)): when a model-area control takes focus, compare its `PointToScreen` rectangle with
   each float's frame (`FloatFrame.From`, built from `FrameSize`) in screen units — `FrameSize` is DIP, so **one**
   conversion by the float's own `DesktopScaling` is unavoidable and lives in one pure function (deviation recorded at
   design-slice, `docs/design/app-shell.md` §6.5) — and, if covered, move the float to the nearest model-area corner
   that clears it and announce it, else dock it back where it came from and announce it.
7. **Platform scope.** macOS is the M1 target; Windows qualification stays deferred (m1-scope decision). The spikes
   below are scheduled, not run.

## Evidence — spike `docs/proof/cad-first-spikes/dock-avalonia/` (Domain Researcher, 2026-09-26, macOS)

| Claim | Label | Basis |
|---|---|---|
| Dock 11.3.12.1 packages depend on Avalonia ≥ 11.3.12, target net10.0; Dock 12.x needs Avalonia 12 | Verified | nuspec read |
| Licence MIT | Verified | LICENSE.TXT read |
| 11.3.12.1 (2026-04-24) is the last 11.x release; 12.x continues (12.1.0.6, 2026-08-27) | Verified | NuGet/GitHub read |
| Restore and build with Avalonia 11.3.14: exit 0, 0 warnings | Verified | ran |
| A floated pane is its own OS window (`HostWindow` with its own `NSWindow` handle); without a locator or `InitializeFactory` no window opens | Verified | ran, `gui-native.txt` |
| `IDockWindow` exposes X, Y, Width, Height, WindowState; factory has Float/Move/Split/Close/Hide/Restore | Verified | reflection |
| `Dock.Serializer.SystemTextJson` default: "object cycle detected" on our layout | Verified | ran |
| `Dock.Serializer.Newtonsoft` round-trips, but stores CLR type names in `$type` | Verified | ran, `layout-newtonsoft-RootDock.json` |
| No pane-maximize command; no clamping (`HostWindow.SetPosition` casts X/Y to `PixelPoint`) | Inferred absent | source grep |
| Automation peers for dock control, tab strips, tab items, `HostWindow` | Inferred | docs read, not screen-reader tested |
| `NativeMenu` exported to the macOS menu bar; `CommandModifiers = Meta` | Verified | ran |
| `Screens.All`, `Screen.WorkingArea`, `Scaling`, `Screens.Changed` exist | Verified | reflection |
| Retina display reported `Scaling = 1`, bounds 1512 × 982 — **Avalonia hard-codes `Screen.Scaling = 1` on macOS** (bounds in points) | Verified | observed, then confirmed by Avalonia source (`Screens.mm`, Native Desktop lens, design-slice §1) — resolves this row; the float's own `DesktopScaling` is used, never a saved hint |

## Spike tasks (scheduled, time-boxed; cannot run in this session)

| # | Spike | Box | Pass / fail |
|---|---|---|---|
| S1 | Same probe on Windows 11 x64 | 2 h | Pass: a float has its own HWND; `CommandModifiers = Control`; `NativeMenuBar` visible and reachable by Alt and F10 with access keys; with a float focused, menu shortcuts still act |
| S2 | Mixed DPI (100 % + 150 %) on macOS and Windows: float, save, restart | 3 h | Pass: the float reopens on the same monitor within ±2 DIP of its saved bounds |
| S3 | Monitor removed between runs | 1 h | Pass: every float lies fully inside a connected `WorkingArea` after launch |
| S4 | Windows 11 Snap Layouts on main window and a `HostWindow` | 1 h | Pass: Win+Z works; the saved bounds equal the snapped bounds |
| S5 | VoiceOver and NVDA on docked tabs, a float and the tab menu | 2 h | Pass: tabs announce name and selected state; the float is reachable; Move/Float/Close work by keyboard |
| S6 | macOS units: Retina display plus a 1× external display | 1 h | Pass: `Window.Position`, `Screen.WorkingArea` and a control's `PointToScreen` agree in one unit on both displays; the clamp keeps a float inside `WorkingArea` |
| S7 | macOS window behaviour of owned floats: full-screen Space, Stage Manager, ⌘`, Window menu, main window moved while its float sits on monitor 2, app deactivation | 2 h | Pass: floats stay in front of the main window, follow it into its Space, cycle with ⌘`, are listed by pane name in the Window menu, and the File/Edit/Window menus stay whole while a float has focus |
| S8 | Dock split-drop capability override: does overriding `CanDrop`/capability overrides block a split drop, this Mac | 1 h | Pass: overriding `CanDrop`/capability blocks the split, so G0 lands the one-group shape (no `groups`, `share`, `origin.group`); Fail: splits stay unblockable and the `groups` shape (§3.4/design-slice) is kept |

S1–S5 and S7's second-monitor row cannot run here (no Windows host, no second monitor, no screen-reader session). S6
and the single-monitor rows of S7 can run on this Mac. Until S1–S5 pass, OI-3 (native floats unspiked on Windows; no
screen-reader trace) stays open. **Run S1 before the layout file format is frozen** (M1.2e); **S8 runs before G0**
(design-slice §14) and decides whether the layout schema keeps `groups`.

## Alternatives considered

- **Build docking ourselves** (Grid, splitters, `Window` for floats). Rejected: drag to drop zones, tab strips and float
  windows are most of Dock's value; the parts Dock lacks (maximize, clamp, focus-safe) are needed either way.
- **Dock 12 with Avalonia 12.** Rejected now: ADR-0003 pins a spiked 11.x API; moving Avalonia is a `/migrate` job.
- **Dock's Newtonsoft serializer for layouts.** Rejected: saved files would name CLR types (`$type`), so a type rename
  breaks every saved layout, and deserializing type names from a user-writable file is an avoidable trust risk.
- **Managed (in-window) floats.** Rejected as the default: UX-32 requires floats that can sit on another monitor.

## Consequences

- **Positive:** the shell gets tabbed, dockable, OS-window floating panes from an MIT library already on the net10.0
  target; menus and shortcuts have one definition.
- **Negative / risks:** Dock's 11.x line is frozen at 11.3.12.1, so fixes arrive only with an Avalonia 12 migration
  (named migration triggers: a blocking Dock defect, an Avalonia 11 security fix we cannot take, or the end of
  Avalonia 11 support). Our own layout file is the exit: it names pane ids, not Dock types. Maximize, clamp and
  focus-safe floats are our code and our tests.
- **Follow-ups:** Security records the new dependency and its transitive set in the licence register and SBOM (A8.5);
  design-slice owns the command table, selection model and layout-file schema (framed in the decision note
  `note-20260926-command-table-selection`). Release Engineer: the packaged `.app` is unsigned today
  (`tools/package-application.py`); signing and notarization stay a release gate, not an M1.2a gate.
