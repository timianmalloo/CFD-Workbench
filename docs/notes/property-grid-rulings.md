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
  - {to: review-ui-property-grid-density, rel: relates-to}
  - {to: mockup-status-bar, rel: relates-to}
  - {to: note-m12c-rulings, rel: relates-to}
review-by: 2027-03-30
summary: >-
  Operator rulings of 2026-10-01 on the property-grid review. Precision follows the quantity, not the row. The field
  nudge is adopted for point and handle fields only, as the canvas Nudging gesture. Drag-to-scrub is rejected. A
  dedicated track builds the component between the M1.2b fix track and PNL. Both root-chord fields stay editable and
  are labelled. Expressions are set once and say so. A point's spanwise coordinate is "From root", with η beside it.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
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

## Density rulings (2026-10-01, after the native build)

Both lenses cleared the density design with conditions at `4bc94e6`
(`docs/reviews/ui-property-grid-density.md`). The operator ruled:

| ID | Ruling | Consequence |
|---|---|---|
| **DR-DEN-1** target size | **Accept 24 px targets** for the property grid, as the one exception to DESIGN.md §4's 32 px dense-control rule. SC 2.5.8 is met by size | DESIGN.md §4 records the exception |
| **DR-DEN-2** Wing at large text | At 200 % text the Wing stays pinned and scrolls inside itself; **a focused field is always brought into view**, with its message line | Mockup and oracle (DN-6); build test `PropertiesPane_Density_FocusedWingFieldInViewAtLargeText` |
| **DR-DEN-3** type size | **11 px, nothing below 11.** Labels, values, units, messages (errors, warnings, reasons), notes, descriptions, summaries and the crumb are all 11 px. No 10 px anywhere | `typography.prop` and `typography.prop-note` are 11/14. The reviewers recommended 12 px; their reasons are kept as residual risk: Windows' 100 % default UI text is about 12 px, and 11 px is harder to read on non-Retina displays. So the native B-2 capture includes a Windows-class 100 % (non-Retina) display check |
| **DN-5** text resize | **Build an app Text size setting:** View ▸ Text size 100 / 125 / 150 / 200 %, ⌘+ / ⌘− (Ctrl on Windows), persisted per user. One multiplier scales every Prop type and row token; ≥ 150 % switches to stacked rows | Owner: the density build track. In the mockup the setting is the real control in the title bar |
| **DR-DEN-4** shortcut scope (raised by this pass; **ruled**: ⌘= / ⌘− zoom a focused model view, else Text size) | ⌘= / ⌘− already zoom the Plan and 3D views (M1.2b §0.1 step 10; M1.2b2 command table). Recommendation, pending the operator: in a model view they keep zooming the view; with focus anywhere else they step the Text size; the View menu items always work | Brief §8.5; flagged for the operator **Ruled (operator, 2026-10-01): context — with a model view focused ⌘= / ⌘− zoom the view; anywhere else they change text size; the View ▸ Text size menu items always change text size.** |

## Structure ruling (2026-10-01)

| Ruling | Decision |
|---|---|
| **DR-CELL-1** structure | The operator saw three cell layouts side by side (docs/mockups/property-grid-cells.html, bd7dbc5: A Visual Studio Properties window, B Premiere Pro Effect Controls, C VS Code compact table) and ruled, verbatim: "All look good... I like B as is". **B is the property sheet's structure**: twirl groups with indented rows, labels left in muted ink, values right-aligned in the accent colour with a dotted underline marking editable, no input box until the value is clicked, light rules between rows. The approved look is fixed; a reviewer condition that changes it visibly goes back to the operator before any build. Supersedes the row-with-boxes layout of the PGRID build and the density pass's field band (DN-2), where they conflict. |

## Structure B rulings (2026-10-02, after the B reviews)

| ID | Ruling | Consequence |
|---|---|---|
| **DR-CELL-2** B's visible changes (2026-10-02) | **OK to all three:** the Wing header has no ▾; labels wrap at the 200 px dock; Tangent kind is a ▾ dropdown with Type's commit rules (arrows stage, Return or a pick applies, Esc keeps, leaving drops) | **Amends PG-06 / MC-1:** the radio list's "leaving the group commits" rule is superseded by the dropdown's "leaving drops" |
| **DR-CELL-3** focused value in error (CL-1) | A focused value in error draws the 1 px accent focus box with the 1 px danger box just outside it. An unfocused error is the danger box only. The rail, icon and message are unchanged | Mockup, brief §5.2, oracle CL-1 check; native test `PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused` |
| **DR-CELL-4** angle reference (CB-2) | The angle-reference note stays focus-only, as approved | — |
| **DR-CELL-5** links (CL-4) | Links get a **solid** underline; the dotted underline is reserved for editable values | Mockup ("Estimates · definitions"), DESIGN.md |

DR-DEN-4 is ruled (context from the Coordinator): ⌘= / ⌘− zoom a focused model view; anywhere else they step the
Text size.

## Status bar ruling (2026-10-02)

The operator asked for reports to leave the Properties sheet: "the status from the change of the point from anchor to
tangent should not be in the property sheet... it should be in a status bar, we should have it render in a status bar
at bottom of the shell". The pick page (`docs/mockups/status-bar.html`, b7c03c5) showed three variants at 1280 × 800.

| ID | Ruling | Consequence |
|---|---|---|
| **DR-STATUS-1** status strip + warning toast | Verbatim: "i prefer V2 ... i really dont want the bottom bar being scrollable (like the message pane in 3) thats awful". **V2 is the design:** a status strip along the bottom of the shell plus a transient toast area above it for warnings. Field-level validation errors stay beside their field. Command reports move **out** of the property sheet into the strip: type change, kind change, the typed-chord fit report, the nudge value, "Selected …", and the pending drop "… unchanged". **No scrollable message list sits in, or docks to, the bottom bar** (V3 rejected) | DESIGN.md §4 *Status strip* and *Toast*; the inventory and build brief in `docs/reviews/ui-status-bar.md`. **Constrains M1.2c:** the planned Messages pane (`docs/design/m12b-points.md` §0.2 "a history list of edit reports … M1.2c (Messages pane)"; `docs/design/app-shell.md` §1 B1 bottom panel *Points, Messages* and §11 live regions) must not be a docked scrolling pane in the bottom bar. Both design docs are flagged review-suggested |

## Status bar decisions (operator, 2026-10-02)

| Ruling | Decision |
|---|---|
| **DR-STATUS-2** (D-1) | While a warning toast shows, the status strip shows the same warning text, so the report survives the toast closing. |
| **DR-STATUS-3** (D-2) | Unit and expression echoes (incl. the set-once echo) go to the status strip, not under the field. |
| **DR-STATUS-4** (D-3) | Drop the idle "No point change."; the strip is empty until something happens. |
| D-4 | Edit-report history for M1.2c: open — decided at M1.2c design; constraint DR-STATUS-1 (no docked scrolling list in the bottom bar) holds. |

## Keyboard navigation ruling (operator, 2026-10-02)

| ID | Ruling | Consequence |
|---|---|---|
| **DR-NAV-1** Tab leaves the Plan | After clicking a point in the Plan, **Tab leaves the Plan and lands on the selected point's first Properties value** (Type, structure B); **Shift+Tab** from the pane's first value returns to the Plan on the selected point. Movement between Plan points is **] / [** (next / previous in the target order: LE root→tip, TE root→tip, then station chips). With no point selected, Tab into the Plan focuses the first target and ] / [ walk from there. Escape, Return-to-field, arrows-nudge, ⌥-arrows pan and F6 are unchanged. The model area's view labels (VW1; a label opens on Return) stay Tab stops in the model region | **Amends** `docs/design/m12b-points.md` §11.3 (keyboard column and "Tab order inside the Plan") and answers the open decision in `docs/reviews/property-grid-native.md` (NS-1). `PlanCanvas` binds `Key.OemCloseBrackets` / `Key.OemOpenBrackets` (same keys on macOS and Windows layouts; layouts that need AltGr for brackets are not covered). Tests: `Plan_TabFromSelectedPoint_GoesToPropertiesFirstValue`, `Plan_BracketKeys_MoveBetweenPoints_InTargetOrder`, `Properties_ShiftTabFromFirstValue_ReturnsToSelectedPoint` |

## View separation ruling (operator, 2026-10-02)

| ID | Ruling | Consequence |
|---|---|---|
| **DR-VIEW-1** view gutter and frames (NS-4) | Verbatim finding: "there is no visible demarcation between the plan view and the 3d ISO view so it looks like they are part of the same design surface". **A 4 px gutter in the window/canvas background colour between views, and each view gets a 1 px frame in the line colour** (as Fusion/Rhino viewports). Applies to Plan + 3D, Four views and One view (frame only, no gutter); the view label strips stay inside each frame | DESIGN.md `spacing.view-gutter` (4 px); `ModelArea.axaml` frames each slot (`Border.viewFrame`) and `ModelArea.ApplyLayout` sets the grid spacing; the Four-view minimum (each view ≥ 320 × 240) is measured inside the frame and past the gutter (arrangement ≥ 648 × 488). Test: `ModelArea_Views_SeparatedByGutterAndFramed` (pixels, light and dark) |

## M1.2b2 views (operator, 2026-10-02, after docs/mockups/m12b2-views.html)

| Ruling | Decision |
|---|---|
| **DR-VIEW-2** (OI-9) | No perspective/orthographic toggle for now: named axis views orthographic, Iso and orbit perspective. |
| **DR-VIEW-3** (OI-10) | Front and Side each fit themselves, each with its own scale bar. |
| **DR-VIEW-4** | The selected station gets a 3 px station tick at its span in the Front band. |
| **DR-VIEW-5** (V3D deviation 1) | Iso looks **from the front**, as in the mockup: leading edge toward the viewer, the cube shows F, S and T, starboard on the viewer's left. Built: `ViewCamera.Named` Iso az 45°, el 30° (was az 135°, the M1.2a aft quadrant). Tests: `View3d_DefaultIso_FromFrontFillsWidth`, `ViewCamera_Presets_TopFrontSideIsoBottomBackPort`. |
| **DR-VIEW-6** (V3D deviation 2) | The Iso fit **fills the view width with a small margin**, as in the mockup. Built: a perspective `Fit` holds the box's corners on the 24 px fit margin and centres their projection (was a bounding-sphere fit about the box centre). Tests: `View3d_DefaultIso_FromFrontFillsWidth`, `ViewCamera_Fit_BoundsInsideViewportMargin`, `ViewCamera_FitSelection_StationBoundsFill`, `ViewCamera_RandomVerbs_FitRecentres`. |
| **DR-VIEW-7** (V3D deviation 3) | View labels become a small plate top-left, as in the mockup — **not now**: it lands as a separate step after ELV joins (ELV adds the Side and Front slots in `ModelArea`). |
| **DR-VIEW-8** (V3D deviation 4) | Wireframe density **stays as built**: a thin section every 0.1 of η on each half (the mockup drew ten in all). |
| **Build** | The operator approved the views mockup (7a96c05): "Yes, build it" — V3D, ELV, PNL. |
| **DR-VIEW-9** (NS-5, native look 2026-10-03) | Verbatim finding: "in 3D when i select Bottom (B) how do i get back to seeing the other choices?" **An always-visible Home button (⌂, to the DR-VIEW-5 front Iso) beside the view cube**, at least 24 × 24, named and tooltipped "Home (Iso)", a Tab stop after the cube faces; ⌘0 is unchanged (fit). **The cube's four rotate arrows are always shown on a straight axis view** (Top, Bottom, Front, Back, Starboard, Port) and stay hover/focus-only otherwise. Below 240 px the cube hides and Home hides with it; View ▸ Camera remains the path. Built: `View3d` draws Home left of the cube box, centred on the cube and below the 3D title's row (the title's reserve is `View3d.CubeRowReserve`); a click runs the cube's own `ApplyPreset(Iso)` and is announced. Tests: `View3d_AxisView_HomeAndArrowsVisible_HomeReturnsToIso`, `View3d_TabPastCube_LeavesView` (faces → Home → arrows). Capture: `docs/proof/m12b2-cube/bottom-with-home.png`. |
| **DR-VIEW-10** (NS-7, native look 2026-10-03) | Verbatim finding: "in the one view i cant cycle to a different view". **In One view the view's label plate is a ▾ picker**: a click, or Return, Space or ↓ on the focused plate, opens Plan / 3D / Side / Front, and a choice shows that view alone. The plate shows ▾ only in One view; in Plan + 3D and Four views it keeps DR-VIEW-7's behaviour (click sets the target; double-click or Return shows the view alone). Built: `ModelArea` writes the controller's `Layout` and `TargetView` (no second layout implementation), so double-click on the plate still returns to the arrangement chosen before One view. A Four-views fallback (too small for four) is One view on screen, so its plate is a picker too, and a pick changes the shown (target) view while Four views stays chosen. Consequence: in One view, Return now opens the picker. The way back is DR-VIEW-11's Back item. Test: `ModelArea_OneView_PlatePicker_SwitchesView`. Capture: `docs/proof/m12b2-cube/one-view-picker-open.png`. |
| **DR-VIEW-11** (after the native pointer check of DR-VIEW-9/10, 2026-10-03) | The operator confirmed natively, by pointer, that Home, the axis-view arrows and the One-view picker work. Ruling: **the One-view picker's first item is "↩ Back to <previous layout>"** ("↩ Back to Plan + 3D", "↩ Back to Four views"), set apart from Plan / 3D / Side / Front by a separator. Choosing it restores the layout chosen before One view. It is the way back for both pointer and keyboard: Return or Space on the plate opens the picker, ↑/↓ move, and Return picks. A double-click on the plate in One view may keep opening the picker; that is accepted. Built: the item runs the plate double-click's `ToggleOneView` (the existing layout API) and names the layout by its View ▸ Views row title. `WorkbenchController.ArrangementBeforeOne` exposes, read-only, the state the toggle already keeps. A Four-views fallback (no room for four) hides Back, because no One view was chosen. `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` now goes back with Return → Back. Test: `ModelArea_OneView_PickerBack_RestoresPreviousLayout` (from Plan + 3D and from Four views, pointer and keyboard). Capture: `docs/proof/m12b2-cube/one-view-picker-open.png`. The same pass keeps the 3D caption clear of the navbar: it stops short of the navbar, or rises above it when there is too little room beside it (`ModelArea_ThreeDCaption_ClearOfNavbar`). |

