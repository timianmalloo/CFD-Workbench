---
id: property-grid-rulings
title: Property grid — operator rulings on precision, field nudge, scrubbing, build order and labels
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [ui, properties, property-grid, precision, keyboard, rulings]
links:
  - {to: review-ui-property-grid, rel: refines}
  - {to: mockup-property-grid, rel: refines}
  - {to: design-language, rel: refines}
  - {to: design-m12b-points, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2027-03-30
summary: >-
  Operator rulings of 2026-10-01 on the property-grid review. Precision follows the quantity, not the row. The field
  nudge is adopted for point and handle fields only, as the canvas Nudging gesture. Drag-to-scrub is rejected. A
  dedicated track builds the component between the M1.2b fix track and PNL. Both root-chord fields stay editable and
  are labelled. Expressions are set once and say so. A point's spanwise coordinate is "From root", with η beside it.
review-suggested: []
---

# Property grid — operator rulings (2026-10-01)

These rulings close the decision requests in `docs/reviews/ui-property-grid.md` §14. They come after the UX &
Accessibility review (veto held, PG-01 to PG-18) and the Marine-CAD review (pass with conditions, MC-1 to MC-18) of
commit `2c014a6`. Repair cycle 1 applies them to the mockup, the oracle, DESIGN.md §12.0f and the brief.

| ID | Ruling | Consequence |
|---|---|---|
| **DR-UID-1** precision | Follow UI-40 with the Marine-CAD amendments. Precision follows the **quantity**, not the row. Typed or placed dimensions show 0.01 mm; a station chord at the root or tip is the typed dimension. Derived estimates show 0.1 mm with "≈" (status: "MAC 101.3 mm"). Placed or typed angles show 0.01°, derived angles 0.1°. Placed t/c shows 0.01 %, Max t/c 0.1 %. Δ and "largest change" show 0.01 mm | Two texts need a spec-owner amendment, not an edit here: the `m12b-points.md` §11.4 sentence "Lengths display at 0.01 mm" (typed dimensions only) and the angle clause of spec UI-40 (0.1° becomes "derived 0.1°, placed 0.01°"). Both are flagged review-suggested |
| **DR-UID-2** field nudge | **Adopted** for point and handle fields only, never the Wing driving dimensions. Conditions: **MC-10** — the field run IS the canvas Nudging gesture: Δ on the model value from the per-curve ladder, begin on keydown, one undo row on KeyUp, Escape cancels with no row, the "≈ preview" chip during the run, arrows ignored while the field text is dirty. **MC-18** — both OS columns: ⌘ on macOS, Ctrl on Windows; the caret conflicts go to the Native Desktop lens. **PG-08** — HelpText names the steps, the new value is announced in the polite status, and an AT pass is required before close | Mockup and oracle implement it; brief §10.4 and the build acceptance list carry the conditions |
| **DR-UID-3** scrub-to-change | **Rejected.** Both reviewers concur | — |
| **DR-UID-4** build order | A **dedicated track** builds the component after the M1.2b fix track joins and before M1.2b2 PNL | Brief §10.8 |
| **MC-2** root chord authority | Both fields stay editable and are labelled. Under the TE root point's Aft row: "This is the root chord. Typing here moves only this point; Root chord under Wing rescales the planform." | COPY-159 |
| **MC-3** expressions | Set once, and say so: "#root_chord × 0.1 = 15.21 mm (set once; doesn't follow Root chord)". The `#` sigil stays | COPY-157 / COPY-161; spec A4.8 should state one-shot semantics (spec owner) |
| **DR-UID-5** fit (ruled after cycle 1) | **"Wing always visible."** UI-36 is amended: the Wing block is pinned and always fully visible; the selection section may scroll; groups stay collapsible and remember state | The spec is flagged for its owner, not edited. Control: the oracle gates "Wing block fully visible" at the 260 and 300 px docks. At 200 px the Wing stays pinned but is not fully visible in four recorded states (chord warning, unavailable, MAC unavailable, section editor) |
| **MC-6** Span label | A point's spanwise coordinate is labelled **"From root"**, with η as a read-only fact. "Span" means only the wing span b | Applied in the grid. The rename must also reach the Tracing probe, the point tooltips and peer names, the Points grid, the Browser rail rows and the copy rows. Brief §10.9 lists them for the build track |

**Recorded APG deviation (PG-06 = MC-1).** In the Tangent Kind radio group, arrow keys move the check only. Return
or Space, or leaving the group, commits. That is one undo row per intent, and the list does not wrap. WAI-ARIA APG
radio groups select on arrow. We depart from it because each kind change rewrites geometry (Symmetric equalises the
handles), so selection-follows-focus would commit geometry the user only passed through.
