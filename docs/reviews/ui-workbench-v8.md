---
id: review-ui-workbench-v8
title: UI review — CAD-first direction v8 (elevate)
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, cad, direction, accessibility, section-editor, points, handles]
links:
  - {to: mockup-workbench-v8, rel: documents}
  - {to: design-language, rel: depends-on}
  - {to: design-section-editor, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2026-12-26
summary: >-
  Measured the shipped native window (43 controls, 38 labels, three always-on panels, implementation vocabulary),
  set a CAD-first direction at the Fusion 360 / Shape3D bar, and built mockup v8 through two repair cycles against
  accessibility, simplifier and marine-CAD adversaries. Mechanical checks pass; three decisions remain the operator's.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-26, reason: "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims." }
  - { by: design-language, on: 2026-09-30, reason: "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1." }
---

# UI review — CAD-first direction v8

**Mode:** elevate (review the running app, then re-direct). **Artifact:** [`docs/mockups/workbench-v8.html`](../mockups/workbench-v8.html).
**Operator brief (verbatim):** "the experience is a cad experience, the experience is SUPER BUSY with lots of extraneous panels that dont add value to the cad experience … open a file or create a new file and have a CAD experience with the views for editing and then if you choose a section you have the section editor experience … ground yourself in … Autodesk Fusion 360 and Shape3D … thats the bar." Follow-up: "everything 'is a curve or a point' … handles for control point splines … specify a 'point' … the terminal point of the trailing edge … the end point of the le or te."

## 1. Measurements of the current implementation (Stage 3, before diagnosis)

`src/CfdWorkbench.Desktop/MainWindow.axaml` at `405e54b` (counted by script):

| Measure | Value |
|---|---|
| Interactive controls in one window | **43** (20 buttons, 7 text boxes, 4 list boxes, 4 radios, 1 combo, 3 tabs, 2 viewports, 2 section canvases) |
| Text labels / readouts | **38** |
| Always-on columns | 3 (240 px navigator · viewport · 300 px properties) |
| Implementation vocabulary on screen | "certified XYZ point samples", "segment error Not assessed", "Accepted source bytes", "Operation facts", "Accept candidate IDs", "Analysis Unavailable — no method implemented", "normalized span η" |

Diagnosis: the window renders the engine's state model (drafts, recovery, provenance, certification, source bytes) as permanent panels. The model — the thing a designer works on — gets what is left over.

## 2. Direction (Stage 1, in words before pixels)

- **Job:** shape a foil by editing the curves and points that describe it, in the views that show them; drop into one section and refine it.
- **Archetype:** G1 Parametric Modeling Workbench (catalog), with two recorded deviations: no feature-history timeline (Undo is the history); no permanent properties dock — properties appear only for a selection (Fusion command-panel pattern).
- **Adjectives:** quiet (not busy) · direct (not form-driven) · precise (not approximate).
- **Taken from Fusion 360:** model fills the window; one grouped toolbar; collapsible browser; view cube; bottom navigation; commands as dialogs with OK/Cancel; editing a section is a mode ending in a green **Finish** ([Autodesk Help](https://help.autodesk.com/view/fusion360/ENU/?guid=GS-THE-FUSION-INTERFACE), [Autodesk blog](https://www.autodesk.com/products/fusion-360/blog/how-to-navigate-autodesk-fusion-user-interface-ui/)).
- **Taken from Shape3D:** the design lives in its curve views (outline, profile, slices); points are grabbed directly; double-click adds a point; curvature on demand; tangent kinds per point ([shape3d.com](https://www.shape3d.com/), [Shape3d X](https://www.shape3d.com/products/FromV8toVX.aspx)).
- **Triggered standards:** UI-T1 (CAD, expert quantities — TQ rules) and UI-T4 (Avalonia native — the HTML mockup is direction evidence only; native proof is an implementation obligation). UI-T2, UI-T3: not triggered.

## 3. What the mockup does

| Screen | Content |
|---|---|
| Start | New foil · New from example · Open… · Recent. Error (file from a newer version), loading, overflow. |
| Design workspace | Plan / 3D / Side / Front; toolbar: File · Add station · Measure · Curvature · Undo/Redo; collapsed browser; view cube; Fit and one/four views. Station chips open a small card with **Edit section**. LE/TE are point-and-handle curves; root ends lock perpendicular to the centreline; tip ends name the closure. |
| Section editor (mode) | Nose = one point with upper/lower vertical handles and LE radius; upper/lower points with handles; TE upper/lower end points move in y and dimension the TE gap and wedge angle; tangent kinds Smooth / Corner / Horizontal / Vertical / Fixed angle; true-geometry curvature comb (auto-scaled, clipped teeth marked, break marks at corners); "Shared with Root · Make unique to Mid"; t/c readout; locks listed; Smooth… dialog; thickness choice only when Finish changes thickness; station strip. |

Keyboard: arrows 0.1 mm · ⌘/Ctrl 0.01 mm · Shift 1 mm; on a handle ← → turn (1°, Shift 5°, ⌘/Ctrl 0.1°) and ↑ ↓ lengthen; K curvature; Return finishes; Esc steps back (drag → handle → leave the editor); Shift F10 / right-click for the point menu.

## 4. Adversarial record (Stage 4)

| Lens | Round 1 | Round 2 (cycle 1) | Cycle 2 | Outcome |
|---|---|---|---|---|
| UX & Accessibility (hard veto) | BLOCK — focus lost on every mode change; harness states were relabelled defaults; canvas targets unmeasured | **PASS** — focus paths, real states, SVG targets, names, ARIA state, crossing cue all cleared | three inferred focus paths now measured; two untrue-copy Minors fixed | **Cleared by the lens** |
| The Simplifier (soft veto) | BLOCK — delete-list, net −21 controls | applied; two retentions justified below | — | **Cleared by written rationale** |
| Marine / precision CAD (soft veto) | BLOCK — plan not editable, comb not curvature | BLOCK — plan "Fixed angle" was inert; comb illegible | both fixed and measured | **Cleared on its stated predicate by measurement; not re-reviewed by the lens (repair cap reached)** |

Simplifier retentions (written rationale, per its veto rule): **four views by default** — the operator asked for "a CAD experience with the views for editing", Shape3D's model; one toggle gives a single view. **"Thickness ×2" toggle** — drawing a section at exaggerated thickness without saying so misstates a quantity (TQ correctness), so the disclosure stays and doubles as the control.

## 5. Measured results (final)

| Check | Result |
|---|---|
| Harness combinations (each screen's designed states × 3 themes × 1280/1440) | **90/90** pass: 0 contrast failures (13 token pairs per theme) and 0 targets under 24×24 CSS px, canvas points/handles/chips included |
| Visible controls | Start 6 · Workspace 13 · Section 14 · Smooth dialog 15 (implementation: 43) |
| Focus lands where intended | plan point nudge/Esc; Enter on station → first section point; Edit section button → first point; Finish/Cancel/Keep → station chip; Smooth open → Tolerance; Smooth Esc/OK/Cancel → Smooth…; thickness dialog Esc → Finish; menu Esc → opener |
| Real states | a real drag creates a detected crossing; Finish becomes aria-disabled with the reason attached; Cancel restores the entry shape |
| Points and handles | handles appear on the selected point; dragging a handle changes the curve; horizontal lock holds; plan end point edited from the keyboard (136.0 → 137.1 mm); plan handle turns by angle and lengthens |
| Comb | 57 of 102 teeth longer than 10 px on the default section |
| Craft gate (`ui-craft-gate.py`) | 0 Major · 6 Minor (nested floating panels = command-panel pattern; edge-to-edge views; mock title bar) — a floor, not a verdict |
| JS errors | 0 |

## 6. Decisions that remain the operator's

1. **Which curve model did you mean?** "Control point splines" with "handles on the point" describes **fit-point curves with tangent handles** (the curve passes through your points — Shape3D, BoardCAD, Fusion's fit-point spline). A **control-vertex** curve (points off the curve pulling it — Rhino default, today's FoilDSL) has no per-point handles. The mockup shows fit points with handles.
2. **Representation (advice for `/design-slice`, not decided).** The marine-CAD lens recommends **quintic Hermite points** (position, tangent, optional curvature), stored exactly as the existing degree-5 B-spline with triple interior knots: C2 at every point, lossless conversion, no curvature jumps. Plain cubic Bézier handles are only G1 and the comb would show a jump at every point.
3. **Undo contract for the build** (GEO-14): Cancel leaves the undo depth unchanged; Finish adds exactly one undo step. The mockup has no undo stack.

## 7. Ranked plan

| Rank | Change | Why |
|---|---|---|
| **1 — highest leverage** | Rebuild the desktop shell to v8's structure: Start → workspace → section mode; delete the always-on Navigator and Properties columns, source text, recovery/provenance panels | Removes 30 of 43 controls and every piece of engine vocabulary; everything else sits on it |
| 2 | Settle the curve model (decision 1) and run `/design-slice` for the point-and-handle representation (decision 2) | The editor contract, FoilDSL and the geometry certificate depend on it |
| 3 | Plan editing of LE/TE with the same point contract as the section | The operator's "end point of the LE or TE" requirement |
| 4 | Native proof pack for the rebuilt shell (UIA/NSAccessibility tree, keyboard, themes, DPI) | UI-T4: the HTML mockup cannot clear native accessibility |

Residual risk: no screen-reader pass (VoiceOver/NVDA); the live region re-announces on every nudge (fix at implementation with a real trace); trackpad-only and per-OS modifier mappings not exercised.
