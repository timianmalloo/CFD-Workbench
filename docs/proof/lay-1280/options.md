---
id: proof-lay-1280-options
title: Four views at 1280x800 in Analysis - layout options with measurements (round-oct06, Ruling 101 3d)
type: proof-pack
status: active
owner: "@trk-lay"
phase: design
tags: [layout, analysis, four-views, 1280x800, ruling-101]
links:
  - { to: proof-a3a-pack, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Today Analysis falls back to One view at 1280x800 because the four views are 503 x 239.5, half a pixel under the
  320 x 240 floor. Width is not the shortage, height is. Four measured options, what each costs, and a recommendation
  for the operator to pick before step b builds it.
---

# Four views at 1280x800 in Analysis: layout options

Ruling 101: Analysis keeps four views at 1280x800. The 320 x 240 floor (`ModelArea.MinimumFourViewSize`) stays.
All numbers are observed: a real `ShellHost` and `WorkbenchController` in an Avalonia window, state "Wing result Current"
(example foil, alpha 2, Spanwise loading tab), sizes read from control `Bounds`, window rendered with `RenderTargetBitmap`
(method of `docs/reviews/a3a-native.md` section 1). Window sizes are client sizes as the harness reports them. Option
changes are harness-side overrides (sizes set on the live controls); no source file was changed.

## 1. Today, measured

| Region | 1500x870 | 1280x800 |
|---|---|---|
| Window client | 1500 x 860 (harness reported 860, once 861) | 1280 x 800 |
| Dock host (tab strip 46 + model area + dock) | 1500 x 646 | 1280 x 586 |
| Left dock (Properties; Browser and Layers are tabs, 0x0 while not active) | 258 x 590 | 258 x 530 |
| Model area (nav tabs + conditions band + views) | 1234 x 599 | 1014 x 539 |
| Conditions band | 1234 x 41 | 1014 x **52** |
| View arrangement grid | 1234 x 558 | 1014 x 487 |
| Bottom panel (fixed `Height = 190` in `AnalysisPanel.axaml.cs:23`) | 1500 x 190 | 1280 x 190 |
| Status strip | 1500 x 24 | 1280 x 24 |
| Each of four views (grid minus 4 px gutter, halved, minus 2 px frame) | 613 x 275 : OK | **503 x 239.5 : short by 0.5 px** -> One view |

The Right dock (Points) is not in the layout in Analysis. Evidence: [today at 1500](today-1500x870.png),
[today at 1280](today-1280x800.png) (shows the fallback: Plan alone).

Three findings:

1. **Width is not the constraint.** Each view is 503 wide against 320. Height is: 239.5 against 240. Narrower docks
   (Option A below) add width nobody needs; they only help by a side effect.
2. **The 1280 conditions band is 11 px taller than at 1500** (52 vs 41). At 1280 the derived values no longer fit, so the
   band shows a `More ▾` button (43 px tall) and the three text boxes stretch to its height (43 instead of 32). At 1500 the
   `More` button is hidden. Fixing that button recovers 11 px with no user-visible loss.
3. **Height budget at 1280x800:** 46 dock tab strip + 52 band + 487 views + 190 bottom panel + 24 status + about 1 border = 800.
   Four views need a grid of 488 px, so today needs a client height of **801 px**. 1 px.

Minimum client height for four views (formula: 611 + panel height, minus 11 when the band is one row; the 761 and
1280x800 points are measured, the others follow the formula):

| Variant | Panel | Band | Min client height | Measured edge |
|---|---|---|---|---|
| Today | 190 | 52 | 801 | 800 short (seen) |
| D | 190 | 41 | 790 | |
| C | 150 | 52 | 761 | 760 short, 761 four views (seen) |
| C + D | 150 | 41 | 750 | |

This matters because a real 1280x800 display loses height to the menu bar and title bar (macOS) or the title bar and task
bar (Windows). If "1280x800" means the screen, not the window, the usable client is near 730-750 and only C + D (750) or
a smaller panel gets there. Flagged for the operator (section 5).

## 2. Options

Each capture is Analysis, "Wing result Current", Spanwise loading, at 1280x800. Gutter 4 px and frame 2 px as in
`ModelArea.EffectiveLayout`.

### A. Narrower left dock below a width (default 260 -> 160 px)

| | |
|---|---|
| Override | Left dock 160 px (`ShellHost.DefaultLeftPaneWidth` at small widths) |
| Measured | Left dock 158 x 530; band 1114 x 41; grid 1114 x 498; each view **553 x 245**: OK (floor 320 x 240) |
| Capture | [A at 1280](optA-narrow-docks-1280x800.png), [A at 1500](optA-narrow-docks-1500x870.png) |
| What shrinks | Properties from 258 to 158 px: value columns clip ("Wing result" rows already clip at 258) |
| Fragility | Passes only through the band: 200 px (view 239.5), 180 px (239.5) fail; 170 px (245) and 160 px pass. The dock must be 170 px or less, and passing is the band losing its `More` button, not the extra width |
| Trade-off | Properties becomes close to unreadable to buy 5 px of margin |
| Build touches | `Shell/ShellHost.cs` (`DefaultLeftPaneWidth`, `ApplyDefaultLeftPaneWidth`), `Shell/WorkspacePresets.cs` / `LayoutCodec` chrome sizes |

### B. Left dock collapses to a rail (icon strip) below a width, expands on click

| | |
|---|---|
| Override | Left dock 48 px. The harness shows the real pane squeezed to 46 px, not a drawn rail: the rail itself is a build task |
| Measured | Model area 1226 x 539; band 1226 x 41; grid 1226 x 498; each view **609 x 245**: OK |
| Capture | [B at 1280](optB-rail-1280x800.png), [B at 1500](optB-rail-1500x870.png) |
| What collapses | Properties, Browser, Layers into a 48 px strip; the result values in Properties need a click or a flyout |
| Trade-off | Properties holds the Wing result numbers; Analysis is where they are read. Largest build (new rail control, expand/collapse, focus and keyboard rules, persistence) |
| Build touches | `Shell/ShellLayout.cs`, `Shell/ShellHost.cs`, `Shell/WorkspacePresets.cs`, `ModelArea.axaml(.cs)` for the plan, new rail control |

It also passes only through the band (the wider band drops `More`); the views gain width (609) that is not needed.

### C. Bottom panel shorter at small heights (190 -> 150 px)

| | |
|---|---|
| Override | `AnalysisPanel.Height = 150` |
| Measured | Panel 1280 x 150; Properties 258 x 570; grid 1014 x 527; each view **503 x 259.5**: OK. Edge: 761 px client four views, 760 short |
| Capture | [C at 1280](optC-short-panel-1280x800.png), [C at 1500](optC-short-panel-1500x870.png) |
| What shrinks | The spanwise chart loses 40 px of height (the 0.16 tick drops from the axis in the capture); the Section, Loads and Provenance tabs scroll sooner (they already sit in a `ScrollViewer`) |
| Trade-off | Less chart height. No pane hidden, no click |
| Build touches | `Analysis/AnalysisPanel.axaml.cs` (the `Height` rule, or a height from `ShellHost` row), `Shell/ShellHost.cs` (only if the rule lives in the row) |

Rule: `Height = client height < T ? 150 : 190`. At 1500x870 (client 860) the rule must not fire, so T is at most 860;
1280x800 (client 800) must fire. The build picks T in between (for example 820) and the operator confirms it.

### D. Fix the conditions band `More` button height (no layout trade)

| | |
|---|---|
| Override | `MoreButton.Height = 32` (the other controls' height) |
| Measured | Band 1014 x 41; grid 1014 x 498; each view **503 x 245**: OK. Margin 5 px |
| Capture | [D at 1280](optD-band-button-1280x800.png), [D at 1500](optD-band-button-1500x870.png) |
| What shrinks | Nothing the user sees. The band is as tall as at 1500. In the capture the label is clipped at a fixed 32: a build sets `MinHeight` and padding, not a fixed `Height` |
| Trade-off | Only 5 px of margin, and 790 px minimum client height |
| Build touches | `Analysis/ConditionsBand.axaml` (outside the expected files; named here) |

### C + D (recommended)

| | |
|---|---|
| Override | Panel 150 and `MoreButton.Height = 32` |
| Measured | Grid 1014 x 538; each view **503 x 265**: OK, 25 px above the floor. Minimum client height 750 |
| Capture | [C+D at 1280](optCD-both-1280x800.png), [C+D at 1500](optCD-both-1500x870.png) |
| What shrinks | Bottom panel by 40 px; nothing else |
| Build touches | `Analysis/ConditionsBand.axaml`, `Analysis/AnalysisPanel.axaml.cs`, `Shell/ShellHost.cs` (if the row owns the height) |

## 3. At 1500x870 nothing changes

Each option is a rule that does not fire at 1500x870 (the band has no `More` button, the panel threshold is not
reached, the dock stays 260). The option captures at 1500 are therefore the non-firing state: the harness measures
613 x 275.5 per view for all five, same as today. The PNGs are not byte-identical to [today at 1500](today-1500x870.png):
the harness window client was 860 px tall in one run and 861 in the others (a 1 px harness difference, not a layout
difference). The 1500 captures prove the harness path; they do not prove the build's rule. Step b needs a test that
asserts the 1500x870 geometry unchanged.

Captures: [A 1500](optA-narrow-docks-1500x870.png), [B 1500](optB-rail-1500x870.png),
[C 1500](optC-short-panel-1500x870.png), [D 1500](optD-band-button-1500x870.png),
[C+D 1500](optCD-both-1500x870.png).

## 4. Summary

| Option | Each view at 1280x800 | vs 320 x 240 | Min client height | User loses | Build size |
|---|---|---|---|---|---|
| Today | 503 x 239.5 (One view) | short 0.5 px | 801 | four views | |
| A dock 160 | 553 x 245 | OK, +5 | 790 | Properties readability | small |
| B rail | 609 x 245 | OK, +5 | 790 | Properties needs a click | large |
| C panel 150 | 503 x 259.5 | OK, +19 | 761 | 40 px of chart | small |
| D band button | 503 x 245 | OK, +5 | 790 | nothing | tiny |
| C + D | 503 x 265 | OK, +25 | 750 | 40 px of chart | small |

## 5. Recommendation

**C + D.** The floor fails by half a pixel because of an 11 px band bug and a 190 px panel. D removes the bug with no user
cost. C gives back 40 px of chart height for a stable 25 px margin and a 750 px minimum client height. A and B spend
the Properties pane to buy width that is not short, and A passes only by the band side effect.

Questions for the operator:

1. Does "1280x800" mean the window client or the screen? On a 1280x800 screen the client is near 730-750, which only
   C + D (750) approaches; a screen reading needs the panel at about 120 px or collapsed by default.
2. Is a 150 px bottom panel acceptable at small heights (chart 40 px shorter), and where is the threshold?
3. Not in scope here, seen in the capture: at about 265 px the Side and Front views' twist/thickness strips crowd
   their drawings. Track CPY owns cut-off cells.
