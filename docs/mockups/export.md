---
id: mockup-export
title: Export dialog - section .dat, wing STL, 3MF, and the hard states
type: design
status: proposed
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, export, dat, stl, 3mf, te-floor, fidelity, operator-show, trk-exd, trk-exr]
links:
  - {to: design-export, rel: documents}
  - {to: design-language, rel: depends-on}
  - {to: rulings, rel: implements}
  - {to: mockup-w2-save-picker, rel: refines}
review-by: 2026-12-31
summary: >-
  Stop-and-show page for the first Export slice (Ruling 193), approved visually (Ruling 194) and revised for Ruling 195.
  One modal dialog with a format list, per-format options, a computed "What will be written" block with the fidelity lines
  and an always-on trailing-edge row (least thickness, where, the floor labelled "app default, no source", manufacturing not
  assessed), the below-floor finding band, the fixed safety string and the hard states: draft open, geometry check not
  passed, Analysis mode, large mesh, preparing, writing, write failure, closure failure, cancelled, no foil open. Whole wing
  and starboard half. Numbers are the committed probe receipts in docs/proof/exd/ for three sample foils.
review-suggested: []
---

# Export - the mockup

Open [`export.html`](export.html) over `file://`. The header switches the state (1 to 10), its detail, the sample foil, the
theme and reduced motion. Captures (light, plus two dark) are in [`export/`](export/). The design is
[`docs/design/export.md`](../design/export.md); every proposed string is in its section 14 and in the page's JSON block.

**Revision 2 (trk-exr).** The approved look is unchanged. What changed: a **Trailing edge** row in every summary (Ruling 195);
the floor says "app default, no source"; the band text reads "Trailing edge 0.26 mm, below the floor of 0.30 mm (app default,
no source)"; "deviation" replaces "gap" for mesh and .dat fidelity; the starboard half has real triangle counts, a help line
and a `-half` file name; the numbers come from probe output regenerated from the committed source (seven rungs, four wings).

**Display only.** Nothing under `src/` or `tests/` changed. The dashed frame is a stand-in for the macOS save panel.

## Direction (in words)

An export is a **hand-off**: the file leaves the app and nobody can ask it what it meant. So the dialog shows, before the
write, exactly what the file will be (units, count, size, the largest measured gap, closed or not), repeats the
trailing-edge finding, and states the one limit that matters (no other CAD program has opened it). It never blocks on an
advisory, never claims more than it measured, and puts a failure where the user is still looking, with the earlier file
kept safe. The window already has three homes for a message: the **status strip** (a result), the **alert band** and the
**modal dialog** (a decision). Export is a decision, so it is a modal dialog; its result goes to the strip.

## Archetype Signature

`ParametricWorkbench` shell, unchanged (`DESIGN.md`, ViewportWorkbench). This surface is a **modal flow inside it**:
`ConfiguratorDialog { Type:Configurator; Arch:SpatialBounded; Layout:ModalDialog; Density:Compact; Nav:ListPlusOptions;
Input:KeyboardFirst+PrecisionPointer; Feedback:Confirmed; Motion:None; Persistence:LocalDevice; A11y:WCAG_2.2_AA; }`.
Fit to the task: choosing a format, a few options and one confirm is **serial**, which suits a dialog; the format list is a
single-choice radio group because the formats exclude each other. Spec `:2198` already places Export in a modal dialog.

## Triggered standards

UI-T4 (native client) and UI-T1 (expert, quantitative) fire: units beside every number, measured values labelled as
measured, the unavailable STEP row with its reason. UI-T2 and UI-T3 do not (no imagery, no model). The unconditional floor
applies: complete states, tokens only, keyboard path, WCAG 2.2 AA (priority below function and look, per the operator).

## States

| State | Details | What it shows |
|---|---|---|
| 1 Menus | File, Section, No foil open | Where the new rows sit (outlined); disabled row with its reason |
| 2 Section .dat | Ready, draft open, Analysis mode | Options, computed summary with the Trailing edge row, real first rows of the file (probed section only), TE finding |
| 3 Wing STL | Ready, preparing, large mesh, draft open, Analysis mode | Scope (whole or starboard half), tolerance picks the mesh, size, Trailing edge row, fidelity; large-mesh band; skeleton while preparing |
| 4 Wing 3MF | Ready | Same options, unit attribute row |
| 5 Geometry not accepted | Check has not passed | Export disabled with the reason and the way out |
| 6 Save panel | STL, .dat, 3MF | The suggested name carries the unit (STL `-mm`) |
| 7 Writing | Large mesh | Indeterminate bar, Cancel |
| 8 Exported | STL, .dat, 3MF | Status-strip sentence with counts, unit and measured gap; Show in Finder |
| 9 Write failed | Disk full, no permission, folder gone, mesh did not close | In-dialog error, safe earlier file, next steps |
| 10 Cancelled | Closed the save panel | One strip sentence, nothing written |

Sample foils: **A** Example foil with an open trailing edge, 0.26 mm (below the 0.30 mm floor; least thickness at the tip);
**B** the same foil, 0.36 mm (meets it, so the band disappears and the row still shows "at y = 439 mm"); **C** the Untitled
NACA 0012 wing, closed trailing edge (reads "0.00 mm along the whole span", the jump reads "Show the trailing-edge gap"),
whose Fine tolerance reaches 513,596 triangles and so shows the large-mesh state with real numbers. Its starboard half at
Fine is 257,596 triangles, with no warning.

## Where the numbers come from

The page's `FOILS` table is the probe output in `docs/proof/exd/` (`probe-open.txt`, `probe-open14.txt`,
`probe-default-wing.txt`, `probe2-*.txt`), **regenerated by trk-exr from the committed probe source** (commands in
`probe-commands.txt`): per mesh rung (seven, 11x26 to 321x801) the whole-wing and starboard-half triangle counts and the
largest measured deviation, per wing the least trailing-edge thickness and where, per station the chord, trailing-edge gap and
the .dat deviation. Revision r12 is illustrative. The summary, the file name, the size (84 + 50 x triangles bytes), the rung a
tolerance picks, the trailing-edge row and the finding are **computed from that table and the options at render time**, not
typed. The file lines shown are real rows for one probed section; other combinations show the summary without file lines
rather than invented ones. The mockup shows one trailing-edge value for both "At station" and "Own"; the build computes it
from the written points.

## Keyboard

Focus lands on the default button; Escape closes (Cancel); Enter activates the default button but not inside a field or
the station list; Tab and Shift+Tab stay inside the dialog; the format list is one radiogroup (Tab enters it once, arrows
move the choice). A browser run over 58 renders (29 states in two themes; 31 images saved) found no console error, no NaN or
placeholder text, no text under 12 px, and every dialog inside the window. The only targets under 24 px are the 16 px radio
inputs, whose wrapping label is at least 28 px high. Keyboard probe: initial focus Export, Shift+Tab to Cancel, Tab back, arrow
moves the format, Escape cancels (`docs/proof/exd/capture-run.txt`).

## Craft gate

`python3 docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/export.html --markdown` returned **no findings** on revision
2 (`docs/proof/exd/craft-gate.txt`). A clean run is a floor, never a verdict.

## Not shown

The marine-CAD UX adversary pass, the Windows save dialog (same flow, its own native frame), the EX31 "tolerance not reached"
band (no sample wing reaches it), real file rows for any section but the probed one, and the Own-shape trailing-edge value.
