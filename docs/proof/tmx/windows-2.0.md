---
id: proof-tmx-windows-2-0
title: TMX Ruling 221 at this laptop's 200 percent - BringIntoView falls short, the keyboard path does not
type: proof-pack
status: draft
owner: "@trk-d1"
tags: [tmx, desktop-tests, scroll, ruling-221, windows]
links:
  - { to: proof-pvh-red-first, rel: relates-to }
review-by: 2026-11-21
summary: >-
  At RenderScaling 2 (native), and at 1.5 and 1.0 (forced), BringIntoView on the second Browser row stops one row (40 px) short, so the fixture's own correction stays; the keyboard path (Tab into the list, then Down) lands the row fully in view at all three scales, so there is no product fix.
---
# Ruling 221 at 200 percent (2026-10-22)

Track D1 of `next-best-six`, branch `d1-desktop-diagnostics`, base `176c9540`. Machine: Windows ARM64 laptop, real display, native
RenderScaling 2. Earlier work: `docs/proof/tmx/diagnosis.md` (the Mac-side diagnosis).

Ruling 238 makes this laptop's 200 percent the Windows evidence for Ruling 221's "150 percent on the PC" clause. Scale 1.5 and
scale 1.0 were **forced** through `CFD_DIAG_FORCE_SCALE` (Avalonia's non-public `TopLevel.HandleScalingChanged`, by reflection);
that emulates a display and is **Inferred**-equivalent to a real 150 percent display, not Verified. Only scale 2 is native.

## Commands

The `DIAG-SCROLL` prints live in the theme matrix, which is the `--theme-matrix` mode, not `--shell-window` (a `--shell-window`
run with `CFD_DIAG_SCROLL=1` prints no `DIAG-SCROLL` line). From the repo root after `dotnet build CFDWorkbench.slnx -c Release`:

    E=tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.exe
    CFD_DIAG_SCROLL=1 $E --harness --theme-matrix                              # scale 2 native, exit 0
    CFD_DIAG_SCROLL=1 CFD_DIAG_FORCE_SCALE=1.5 $E --harness --theme-matrix     # exit 0
    CFD_DIAG_SCROLL=1 CFD_DIAG_FORCE_SCALE=1.0 $E --harness --theme-matrix     # exit 0
    CFD_TEST_ONLY=BrowserRow_BelowFold $E --harness --shell-window             # the new check

`--shell-window --part=1/2` and `--part=2/2` both exit 0 with the new check passing (the check runs in part 1/2; the full run is
in the final `bash tools/run-tests.sh`, TESTS-GREEN).

## The raw numbers (Verified at 2.0; 1.5 and 1.0 forced)

`DIAG-SCROLL-RAW` prints after `BringIntoView` and `Settle`, before the fixture's correction. The first line of each pair is the
list's own ScrollViewer, the second the pane's outer one (the one the overflow is measured in). Theme `default` shown; light,
dark and high-contrast print the same numbers.

    DIAG-SCROLL-RAW theme=default scale=2 offset=0, 120 extent=258, 1696 viewport=258, 584 rowTop=584 rowHeight=40 overflow=40.00
    DIAG-SCALE forced=1.5 RenderScaling=1.5
    DIAG-SCROLL-RAW theme=default scale=1.5 offset=0, 130.67 extent=257.33, 1720.67 viewport=257.33, 583.33 rowTop=583.33 rowHeight=40.67 overflow=40.67
    DIAG-SCALE forced=1 RenderScaling=1
    DIAG-SCROLL-RAW theme=default scale=1 offset=0, 120 extent=258, 1696 viewport=258, 584 rowTop=584 rowHeight=40 overflow=40.00

| scale | how | offset after BringIntoView | row top | viewport | overflow |
|---|---|---|---|---|---|
| 2.0 | native, Verified | 120 | 584 | 584 | 40.00 px |
| 1.5 | forced, Inferred-equivalent | 130.67 | 583.33 | 583.33 | 40.67 px |
| 1.0 | forced, Inferred-equivalent | 120 | 584 | 584 | 40.00 px |

The corrected `DIAG-SCROLL` line then shows offset 160, row top 544, at scales 2 and 1.0: the fixture scrolls the remaining
40 px. The shortfall is one row at every scale, including 1.0, so it is **not scale dependent**.

## Mechanism (Inferred from the DBG reads of this session, not from Avalonia source)

Under the pane's outer ScrollViewer the station list sits below the fold (list top 648 against a 584 viewport). Before the list
moves, the second row is **not realized**: `IsVisible` is false, so `Focus(Tab)` on it is refused and `BringIntoView` on it
scrolls to a stale position, one row short. The first row is realized and takes Tab focus. Pressing Down from it realizes and
focuses the second row, and the list scrolls it fully in.

## Decision

The scroll does fall short for the **explicit `BringIntoView` call** the fixture makes, but not for the **keyboard path** a user
takes. The new check `BrowserRow_BelowFold_FocusBringsIntoView_AtScales` (in `ShellWindowTests.Run`, so the `--shell-window`
mode) opens the Example, shows the Browser pane in a 1024 x 700 window, requires the last row to start below the fold, Tabs into
the list, presses Down, and asserts the focused row lies inside every ScrollViewer ancestor's viewport (top >= -0.01 and bottom
<= viewport + 0.01) at 1.0, 1.5 and 2.0, with no fixture scroll. It passed first time at all three scales, so the product needs
no fix and `BrowserPane.axaml(.cs)` is unchanged. Ruling 221's second branch applies: only the fixture is repaired.

Which of the three fixture repairs held before this track:

| repair | before | now |
|---|---|---|
| an explicit scroll that lands | held (`ScrollRowIntoView` scrolls the remainder) | unchanged |
| a named precondition | partly (a final throw naming scale, top, height, viewport, offset) | unchanged; the new check names its own precondition |
| `Probe` around the bare throws | missing: a throw aborted the whole matrix | added: `browser.scrollIntoView`, `focus.browser.reach` and `focus.browser.selected.reach` are probes, so a failure is a named row failure |

## The check cannot pass vacuously (Verified)

Mutant: the Down key replaced by Left, everything else equal. The check goes red at all three scales, for example:

    FAIL BrowserRow_BelowFold_FocusBringsIntoView_AtScales InvalidOperationException: Focused Browser row not fully in view: Down arrow did not focus the last row at scale 1 | Down arrow to the last row at scale 1: top 584.00, height 40.00, viewport 584.00, offset 120.00 | ... scale 1.5 ... scale 2: top 584.00, height 40.00, viewport 584.00, offset 120.00

Unmutated:

    PASS BrowserRow_BelowFold_FocusBringsIntoView_AtScales

There is no red-on-old-product line because the product was already correct; the red above is on a mutant of the check.
