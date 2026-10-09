---
id: mockup-export
title: Export dialog - section .dat, wing STL, 3MF, and the hard states
type: design
status: proposed
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, export, dat, stl, 3mf, te-floor, fidelity, operator-show, trk-exd]
links:
  - {to: design-export, rel: documents}
  - {to: design-language, rel: depends-on}
  - {to: rulings, rel: implements}
  - {to: mockup-w2-save-picker, rel: refines}
review-by: 2026-12-31
summary: >-
  Stop-and-show page for the first Export slice (Ruling 193). One modal dialog with a format list, per-format options, a
  computed "What will be written" block with the fidelity lines, the trailing-edge floor finding, the fixed safety string
  and the hard states: draft open, geometry check not passed, Analysis mode, large mesh, preparing, writing, write failure,
  closure failure, cancelled, no foil open. Numbers are the probe receipts in docs/proof/exd/ for three sample foils.
  Proposed for the operator; nothing is built until the operator approves it.
review-suggested: []
---

# Export - the mockup

Open [`export.html`](export.html) over `file://`. The header switches the state (1 to 10), its detail, the sample foil, the
theme and reduced motion. Captures (light, plus two dark) are in [`export/`](export/). The design is
[`docs/design/export.md`](../design/export.md); every proposed string is in its section 8 and in the page's JSON block.

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
| 2 Section .dat | Ready, draft open, Analysis mode | Options, computed summary, real first rows of the file (probed section only), TE finding |
| 3 Wing STL | Ready, preparing, large mesh, draft open, Analysis mode | Scope, tolerance picks the mesh, size, fidelity; large-mesh band; skeleton while preparing |
| 4 Wing 3MF | Ready | Same options, unit attribute row |
| 5 Geometry not accepted | Check has not passed | Export disabled with the reason and the way out |
| 6 Save panel | STL, .dat, 3MF | The suggested name carries the unit (STL `-mm`) |
| 7 Writing | Large mesh | Indeterminate bar, Cancel |
| 8 Exported | STL, .dat, 3MF | Status-strip sentence with counts, unit and measured gap; Show in Finder |
| 9 Write failed | Disk full, no permission, folder gone, mesh did not close | In-dialog error, safe earlier file, next steps |
| 10 Cancelled | Closed the save panel | One strip sentence, nothing written |

Sample foils: **A** Example foil with an open trailing edge, 0.26 mm (below the 0.30 mm floor); **B** the same foil, 0.36 mm
(meets it, so the finding disappears); **C** the Untitled NACA 0012 wing, closed trailing edge (0.00 mm, below the floor),
whose Fine tolerance reaches 513,596 triangles and so shows the large-mesh state with real numbers.

## Where the numbers come from

The page's `FOILS` table is the probe output in `docs/proof/exd/` (`probe-open.txt`, `probe-open14.txt`,
`probe-default-wing.txt`, `probe2-*.txt`): per mesh rung the triangle count and the largest measured gap, per station the
chord, trailing-edge gap and the .dat deviation. Revision r12 is illustrative. The summary, the file name, the size
(84 + 50 x triangles bytes), the rung a tolerance picks and the finding are **computed from that table and the options at
render time**, not typed. The file lines shown are real rows for one probed section; other combinations show the summary
without file lines rather than invented ones. The half-wing size was not probed and the page says so.

## Keyboard

Focus lands on the default button; Escape closes (Cancel); Enter activates the default button but not inside a field or
the station list; Tab and Shift+Tab stay inside the dialog; the format list is one radiogroup (Tab enters it once, arrows
move the choice). A browser run over all 27 state captures found no console error, no NaN or placeholder text, and no text
under 12 px. The only targets under 24 px are the 16 px radio inputs, whose wrapping label is at least 28 px high.

## Craft gate

`python3 docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/export.html --markdown` returned **no findings** after one
repair (an undocumented scrim colour became the viewport token; the window frame and status strip gained inset). Result in
`docs/proof/exd/craft-gate.txt`. A clean run is a floor, never a verdict.

## Not shown

The marine-CAD UX and manufacturing adversary passes, the Windows save dialog (same flow, its own native frame), the
EX31 "tolerance not reached" band (no sample wing reaches it), and the half-wing numbers.
