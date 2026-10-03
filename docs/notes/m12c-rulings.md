---
id: note-m12c-rulings
title: M1.2c — operator rulings on the section editor decisions, the ADR-0007 amendment and the section display path
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [m12c, section-editor, rulings, messages, points-pane, display, evaluator]
links:
  - {to: design-m12c-section-editor, rel: refines}
  - {to: mockup-m12c-section-editor, rel: refines}
  - {to: adr-0007-edit-transactions, rel: refines}
  - {to: adr-0010-one-placement-rule, rel: refines}
  - {to: property-grid-rulings, rel: relates-to}
  - {to: rulings, rel: relates-to}
review-by: 2027-04-01
summary: >-
  Operator and Owner rulings of 2026-10-03 on the M1.2c design, made after seeing the mockup. The editor opens in the
  model area. There is no Messages pane: blockers show where they block, plus Show in the status strip. The Points
  pane goes in the right side bar. A no-go spike ships paired point types. The ADR-0007 amendment (no step count in the
  receipt) is accepted. Section drawing uses the one binary64 display profile evaluator, bound to the certificate within
  1e-9 chord (ADR-0010 Amendment 1).
review-suggested: []
---

# M1.2c rulings (2026-10-03)

The operator reviewed `docs/mockups/m12c-section-editor.html` (commit `4c65770`) and ruled on the four decisions it
showed. The Owner accepted the ADR-0007 amendment. A geometry review of the proof-budget work added a ruling on how
sections are drawn. The rulings register `docs/notes/rulings.md` is written only by `coord decide`, so the Coordinator
records the numbered Ruling there. This note is the design-side record, and it links to the design and the mockup.

| ID | Ruling | Consequence in the design |
|---|---|---|
| **OD-1** | **A** — the editor opens **in the model area**, in place of the views (CAD-20). Side selects the station; Return, double-click or Edit section… enters | §11.1. B (editor in the Side slot) and C (drag on the placed section) are not built and stay in the mockup as the record |
| **OD-2** | **A** — **no Messages pane.** Blockers show where they block (the Finish reason, the canvas marker), plus one **Show** button in the status strip. ⌘Z is the history | `"messages"` leaves the registered panes. The spec's B1/UX-31 Messages wording and app-shell §11's `role=log` obligation are retired (deviation D-3, flagged for the spec owner). Answers the D-4 open item of DR-STATUS-1 |
| **OD-3** | **B** — the **Points pane goes in the right side bar** (Precision, ⌘2) | Its home region moves from the bottom panel to the right side bar (`LayoutCodec.Homes`, read by `WorkspacePresets`). A, C and D are not built (deviation D-2, flagged) |
| **OD-4** | **a** — if the GSPK spike is a no-go (or GCRT reaches its cap), **ship with paired point types** and bring DR-11 back with the numbers | The SPTF list is the fallback. The decision of which list SPT writes is taken at the GSPK verdict |
| **ADR-0007 Amendment 1** | **Accepted (Owner).** The section receipt carries no step count | §3.3. The amendment is recorded in the ADR |
| **Section display path** (geometry review of the proof-budget work) | **Section drawing uses the fast display path.** `AuthoringSession.Sample` / `ProfileView` move to the one binary64 display profile evaluator: `Placement.cs`'s `SplineBasis` jet and abscissa inversion (`OrdinateAt` / `Prepare`), promoted to a shared internal next to `ChannelEvaluator`, with no fourth copy. It is bound to the certificate by test, within 10⁻⁹ chord of `Bernstein.EncloseAt`, on fixtures that include a rebuilt profile with non-dyadic knots, a C⁰ knot and the LE vertical tangent. Samples are cosine-spaced at the nose (replacing uniform x/100). Captions say "display". The certificate still decides validity | ADR-0010 Amendment 1. New track **DSP** in the design (§3.7, §14) |

DR-VIEW-9, -10 and -11 (Home beside the cube, the One-view plate picker, and its "↩ Back" item) are recorded in
`docs/notes/property-grid-rulings.md` on the integration branch `feature/ui-cad-direction`. They do not change the
section editor: the mode hides the views, and the navbar keeps only Fit and Fit Selection.
