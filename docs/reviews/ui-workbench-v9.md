---
id: review-ui-workbench-v9
title: UI review — v9 docked panes and Properties pane (elevate)
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, docking, properties, window-management, accessibility, native-desktop]
links:
  - {to: mockup-workbench-v9, rel: documents}
  - {to: review-ui-workbench-v8, rel: refines}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-26
summary: >-
  Elevated v8 with VS Code / Premiere Pro window management and a selection-driven Properties pane, placed per the
  operator in one narrow left panel with optional right and bottom docks. Two repair cycles against accessibility,
  native-desktop and simplifier lenses; one accessibility Major (floats covering focused canvas targets) is open at the cap.
---

# UI review — v9 docked panes

**Mode:** elevate (v8 → v9). **Artifact:** [`docs/mockups/workbench-v9.html`](../mockups/workbench-v9.html).
**Operator (verbatim):** "the properties should be in a 'properties pane' - think about the window management like in … VS Code, Adobe Premier … when I click on a node e.g. an anchor spline node … the properties should be in the properties pane docked in a side panel or floating." Then: "right side and bottom panels should be 'optional' · the left panel should be the default place for properties and not take too much space away from the cad surfaces · by default it would have the side panel then the main views depending on the task."

## 1. Reference bar (established, not recalled)
- **VS Code:** primary and secondary side bars plus a panel; views drag between them; *Move View* is the keyboard route; toggles ⌘B / ⌥⌘B / ⌘J; *Reset View Locations* ([Custom Layout](https://code.visualstudio.com/docs/configure/custom-layout)).
- **Premiere Pro:** workspaces with *Reset to Saved Layout*; panels in tab groups; drop zones highlight while dragging (centre groups as a tab, edges dock); panel menu *Undock*; ⌘-drag floats ([dock, group, undock](https://helpx.adobe.com/premiere/desktop/get-started/tour-the-workspace/dock-group-undock-panels.html)).

## 2. Window-management model
| Element | Behaviour |
|---|---|
| Left side bar (default home) | 260 px (200–420); tabs **Properties** · Browser; 18% of the body width at 1440 |
| Right side bar, bottom panel | Optional, closed by default; an opened empty dock says how to fill it; a dock closes when its last pane leaves |
| Panes | Properties (selection-driven), Browser, Points (grid of every point, typed edits, two-way selection), Messages |
| Moving | drag a tab to a drop zone · ⋯ → Move to / Float / Size / Maximize / Close · floats: Position menu, Alt+arrows, ⤓ or Escape docks to origin |
| Workspaces (Window menu) | **Planform** (Plan + 3D, Properties) · **Precision** (+ Points, Messages) · **Review** (four views, no panes); each remembers its layout; Reset layout restores the preset |
| Platform | macOS: File / Edit / Window in the system menu bar, ⌘ shortcuts · Windows: in-window menus, caption buttons right, Ctrl shortcuts · shortcuts ignored while typing |
| Keyboard | F6 cycles regions; ⌘/Ctrl+B, J, ⌥⌘B/Ctrl+Alt+B; ⌘/Ctrl+Shift+M maximize; splitters: arrows, Home/End |

## 3. Adversarial record
| Lens | Round 1 | Cycle 1 | Cycle 2 | Outcome |
|---|---|---|---|---|
| UX & Accessibility (hard veto) | BLOCK — focus lost on every dock rebuild; maximize not inert; drag-only splitters; table/tab/menu ARIA | BLOCK — floats movable only by drag; float covered the left bar | **Blocker cleared; one Major open**: a float can cover focused canvas targets (2.4.11); the sweep's occlusion check exempted SVG targets | **Open at the repair cap — operator decision** |
| Native desktop (advisory) | pass-with-conditions: real OS-window floats; macOS global menu bar; platform shortcuts; dead-key maximize; per-workspace persistence; Windows caption layout | applied in the mockup where a mockup can show it | — | Conditions carried to the build (§5) |
| Simplifier (soft veto) | BLOCK — delete right dock, workspaces, ⋯, floats, drop zones | Overridden with written rationale: the operator asked for floats and docking, and for optional right/bottom docks. Applied: workspaces became task presets that change the views; title + command rows merged; Tangent folded into Position; Constraints only when locked | — | **Cleared by rationale** |

## 4. Final measurements
| Check | Result |
|---|---|
| Harness combinations (platform × theme × screen × layout, plus 1280/1920 selections) | **92/92**: 0 contrast failures (12 token pairs per theme), 0 HTML or canvas targets under 24×24 CSS px; splitters use the equivalent-control exception (pane menu → Size) |
| Focus after every action | **0 failures** (focused control visible, not inert, topmost at its centre for HTML controls): Browser Enter, Properties commit + Tab, Size, Close, Close last pane, show pane, maximize (five Tabs stay inside), Escape restore, Float, Alt+arrow move, Position, Escape dock, Move right/left, grid select and typed edit, F6, workspace switch, menu Tab-close, section Enter/Escape |
| Defaults | left shown with Properties; right and bottom hidden; Plan + 3D |
| Craft gate (`ui-craft-gate.py`) | no findings — a floor, not a verdict |
| JS errors | 0 |

## 5. Open items
1. **[Major, accessibility — operator decision]** A float over the model can hide canvas targets that take focus (station chips, points). Options: (a) move the float clear when a covered canvas target receives focus — recommended; (b) open floats at Bottom right and rely on 2.4.11's allowance for user-opened, movable content. The sweep's occlusion test must drop its SVG exemption either way.
2. **Build conditions (native desktop):** floats are real OS windows (multi-monitor; Dock for Avalonia with managed windows as opt-in); macOS menus via `NativeMenu`; per-platform shortcuts; per-workspace layout persistence (serialized) with floats clamped to connected monitors; Windows 11 Snap Layouts preserved. **Spike first:** OS-window floats on a mixed-DPI dual-monitor setup on both platforms.
3. **Not yet done:** a VoiceOver / NVDA pass on tabs, the Points grid, separators and the Position items.

## 6. Ranked plan
| Rank | Change | Why |
|---|---|---|
| **1 — highest leverage** | Build the shell with the v9 structure: left Properties panel, optional docks, task workspaces; delete the always-on v7 columns | Every other UI change sits on it; removes most engine vocabulary |
| 2 | Spike OS-window floats + Dock (Avalonia) on mixed-DPI dual monitors, macOS and Windows | Biggest implementation risk named by the native lens |
| 3 | Decide open item 1 and fold it into the build's focus contract | Clears the last accessibility Major |
| 4 | Screen-reader pass on the built shell | No SR evidence yet |
