---
id: design-m12b-points
title: "Design: M1.2b — CAD point editing on the Plan view (rail points, point types, gestures, typed chords)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — M1.2b (spec 1.6, architecture §10.6)
tags: [desktop, core, cad, plan-view, point-types, anchor, control-point, tangent, gesture, undo, wing-estimates, driving-dimension, foildsl-4.1, dr-2, dr-6, dr-9, dr-10, m1.2b]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: architecture-application, rel: refines }
  - { to: adr-0001-master-curve-degree, rel: depends-on }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0006-driving-dimensions, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: design-app-shell, rel: depends-on }
  - { to: spec-foildsl, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: mockup-workbench-v10, rel: relates-to }
  - { to: review-ui-workbench-v10, rel: relates-to }
  - { to: coordination-app-shell-build, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-29
summary: >-
  Detailed design of slice M1.2b: a real Plan view (top-down, both rails as curves, stations, every rail point as a
  typed glyph, a Tracing probe and a curvature comb) on which a point or handle is selected, dragged, nudged at
  0.01/0.1/1 mm or typed, and committed as one undo step at the end of the gesture while the Wing estimates follow the
  drag. Properties sets Anchor/Control type and Smooth/Symmetric/Corner tangents (FoilDSL 4.1); typed Root and Tip
  chord refit both rails under the ruled quarter-chord hold and root-flat blend with both numbers reported. Amends
  ADR-0001 to 6-16 channel vertices under 4.1.
review-suggested:
  - { by: design-m12c-section-editor, on: 2026-10-03, reason: "M1.2c resolves §0.2's 'Messages pane' row (OD-2: no history list; DR-STATUS-1) and scopes Precision to a preset without memory (D4 owns workspace memory)." }
  - { by: property-grid-rulings, on: 2026-10-02, reason: "DR-STATUS-1: reports render in a 24 px status strip at the bottom of the shell plus a transient warning toast; no scrollable message list sits in or docks to the bottom bar (V3 rejected). The M1.2c Messages pane (bottom panel, history of edit reports, role log) must not be a docked scrolling pane in the bottom bar; where history goes is open (docs/reviews/ui-status-bar.md D-4)." }
  - { by: adr-0001-master-curve-degree, on: 2026-09-30, reason: "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)." }
  - { by: design-language, on: 2026-09-30, reason: "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1." }
  - { by: design-m12b2-3d-elevations, on: 2026-09-30, reason: "M1.2b2 design asks seams SR-1..SR-5 before B0/U1b dispatch: rename AftMeters/AftOnly to Ordinate/ValueOnly across PointView, GestureFrame, PlanSample, CombTooth, HandleTarget and UpdateGesture; CurvePointLayer extraction from PlanCanvas; one binary64 channel inversion; curve guard from one table; optional unit-free row rule for rails (F-14). Each has a fallback owned by M1.2b2." }
  - { by: mockup-property-grid, on: 2026-10-01, reason: "F-1 property grid: Properties becomes identity + collapsible groups + label | value | unit rows; Tangent is a labelled group shown on handles too (F-4); one identity per selection (O-6); units and UI-40 precision everywhere (O-4); estimates Unavailable with a reason instead of ≈ — (COPY-155); COPY-149..157 proposed. Review §11.4 Properties rows and the precision conflict DR-UID-1." }
  - { by: property-grid-rulings, on: 2026-10-01, reason: "Operator rulings DR-UID-1 and MC-6 need spec-owner amendments: precision follows the quantity (UI-40 angle text: placed/typed 0.01°, derived 0.1°; placed t/c 0.01 %; station chord at root/tip 0.01 mm; m12b §11.4 "Lengths display at 0.01 mm" covers typed dimensions only; status "MAC 101.3 mm"); a point's spanwise coordinate is "From root" with η (hover/peer names, probe, CAD-15/UI-37); A4.8 expressions are set once; COPY-149..167 proposed." }
---

# Design: M1.2b — CAD point editing on the Plan view

- **Status:** In review — gate passed with conditions after 2 of 2 repair cycles (Gate record); every veto cleared by its own lens. Owner acceptance pending.
  DR-12, DR-13 and OI-1 ruled by Ruling 56 (2026-09-30) and applied here.
- **Spec / architecture:** [spec rev 1.6](../specs/cfd-workbench-v1.md) A4.8, A4.9, A4.15, CAD-03/04, CAD-15, CAD-16,
  CAD-17, F11 (P, P1–P5, M, D–D4), UX-23 (1.6 note), B7 navigation · [architecture §10](../architecture/application.md)
  (§10.2, §10.6 row M1.2b, §10.7) · [ADR-0005](../adr/0005-point-types-in-the-b-spline-record.md) ·
  [ADR-0006](../adr/0006-driving-dimensions-and-wing-estimates.md) ·
  [ADR-0007](../adr/0007-edit-transactions-section-draft-and-gesture-commit.md) §3 ·
  [ADR-0001, Amendment 1](../adr/0001-master-curve-degree.md) · [app-shell design](app-shell.md) §3.6–3.8, §6.7 ·
  [FoilDSL 4.0](../specs/foildsl.md) §4–§5, §8 · [Ruling 53](../notes/rulings.md) (DR-2, DR-6, DR-9, DR-10), Rulings 54–55.
- **Delivery phase:** M1.2b of architecture §10.6. Real: everything in this slice. Absent: section editor mode (M1.2c),
  catalog (M1.2d), floats and saved layouts (M1.2e). No new mock seam: the Plan view reads a pure Core projection, so
  Desktop tests drive it from source bytes.
- **Author / date:** `/design-slice` sub-agent (session track-m12b-design), 2026-09-30. Code read at `10f0628` (branch
  head `dc83292` differs only in the audit log).

## 0. What the operator will see

A standing rule: an unstated slice boundary cost an operator review session. M1.2a's "Plan + 3D" tab holds only an
isometric plot of 15 certified sample dots (`ModelArea.axaml`:40-58, `Viewport.cs`:144-154). There was no Plan view and
nothing on it could be picked. **M1.2b is the slice that makes the Plan view a CAD surface.**

### 0.1 The demo at the end of M1.2b (packaged `.app`, macOS)

1. **New foil.** The Planform workspace opens on the **Plan** tab, which fills the model area: the near-elliptic
   planform from the top, both halves, the leading-edge and trailing-edge rails as curves over a light fill, dashed
   station chord lines with name chips, a centre line, a scale bar. On the starboard half each rail shows its ten points:
   a diamond at the root end and at the tip end, a filled circle for each control point, and a dashed control polygon.
   Properties is on the left and ends with the complete Wing block. (The M1.2a isometric sample plot moves to its own
   "3D samples" tab beside "Section sample"; it is no longer beside the Plan.)
2. **Hover** over the planform: the **Tracing probe** in the corner of the Plan reads "η 0.412 · span 206.00 mm · 41.2 %
   half-span · LE 4.71 mm · TE 118.52 mm · chord 113.81 mm". Hover a trailing-edge point: a ring and a tooltip, "Trailing
   edge, point 5 of 10, control point, span 180.00 mm, aft 118.52 mm".
3. **Click** it: the glyph fills; Properties shows "Trailing edge · point 5 of 10", **Type: Control point**, **Span** and
   **Aft** in millimetres.
4. **Drag** it aft. The trailing-edge curve, the fill and the mirrored half follow the pointer, and the probe shows the
   live Δ ("Δ aft +2.14 mm"). **MAC, Area and Aspect ratio in the Wing block change during the drag** (the block header
   reads "≈ preview"). Hold Shift to lock the drag to the aft axis. Release: one undo step; the status line reads "Moved
   trailing edge point 5 by 2.14 mm. MAC 101.30 mm." ⌘Z returns the rail exactly.
5. **Nudge:** ↓ moves 0.1 mm, ⌘↓ 0.01 mm, ⇧↓ 1 mm. Holding a key is one undo step when it is released.
6. Drag a trailing-edge point **across the leading edge**: a red dashed marker shows where the edges would cross.
   Release: the point goes back, "That would make the leading and trailing edges cross. The point is back where it
   was." Undo depth is unchanged.
7. **Type → Anchor point.** The rail gains three points (10 → 13); the point is now a square on the curve with two
   handles; tangent **Smooth**. The status line reports the measured change. Drag a handle, or select it and use the
   arrows: the other handle turns to stay in line. Switch to **Symmetric** (equal lengths) or **Corner** (independent;
   the comb shows a break). Each change is one undo step.
8. **Save, close, reopen.** Types and tangent kinds are as saved. The Foil source tab shows `foildsl "4.1"` and a
   `tangents` block.
9. **Wing block → Root chord.** Type `152.09` (×1.2): one undo step; "Root chord 152.09 mm. Fit 9.01 µm (limit 10 µm).
   4.21 mm from a straight taper. Planform moved 6.34 mm so the leading edge stays at the root." Type `190` (×1.5):
   accepted with a **warning**, "Root chord 190.00 mm. Fit 22.53 µm (limit 10 µm) — above the limit. 10.54 mm from a
   straight taper. Planform moved 15.84 mm so the leading edge stays at the root." (§3.9 spike; Ruling 56 DR-12.)
   **Tip chord** works the same way; `15 cm` and `#root_chord * 0.1` are accepted and echoed in mm.
10. **Keyboard only:** Tab enters the Plan (on the selected point, or the first target when none is selected); ] and [
    move to the next and previous point; Tab from a selected point leaves the Plan for its first Properties value (DR-NAV-1);
    arrows nudge; Return jumps to the point's Span field; Escape returns to the point. ⌥+arrows pan, ⌘= and ⌘− zoom, ⌘0
    fits.
11. **Pointer navigation (Workbench preset, spec B7; trackpad per Ruling 56 DR-13):** the wheel zooms about the
    pointer; on the trackpad two-finger scroll pans and pinch zooms; middle-drag or Shift-drag on empty canvas pans;
    right-click (Control-click) on a point opens Make Anchor Point / Make Control Point / Tangent / Fit.
12. **View ▸ Curvature comb** (C with the Plan focused) shows the comb on the selected rail, live during a drag.

### 0.2 What the operator will NOT see yet, and which slice brings it

| Not in M1.2b | Brought by |
|---|---|
| Section editor mode, section point types (independent per surface, B6 restart), Points and Messages panes, Precision workspace | **M1.2c** |
| Replace from catalog, Save to My sections | **M1.2d** |
| Floating panes, Maximize pane, saved layouts, the Review workspace (four views) | **M1.2e** |
| A 3D view beside the Plan (wireframe or shaded, with orbit) — the certified sample plot stays in its own tab | **M1.2b2** (Ruling 56; right after M1.2b, before M1.2c), starting with a design pass for one shared placement rule. A wireframe needs one shared placement rule with the certificate (twist, dihedral, section placement); inventing a display copy of it here would be a second geometry definition |
| Front, Side and Starboard elevations; editing dihedral, twist and thickness channels | **M1.2b2** (Ruling 56); Core commands are curve-named, only the two rails are wired |
| Insert / Delete / Fair / Rebuild on rails; an "insert anchor on the curve without changing its shape" verb | **No slice yet** — OI-2 |
| Moving or typing several points at once (a multi-selection shows shared values and "Mixed", read-only) | **No slice yet** — OI-3 |
| The Rhino navigation preset and a trackpad-mode setting | **No slice yet** — OI-4 (the trackpad gestures themselves are ruled: two-finger scroll pans, pinch zooms — DR-13) |
| Comb scale and density controls, the monotone-piece count, a curvature/radius readout on hover (A4.9) | **No slice yet** — OI-5 |
| A history list of edit reports (the last report shows in the status line and under the edited field) | **M1.2c** (Messages pane) |
| A CLI `dimension` command (CLI `inspect` does show point types and tangent rows) | **No slice yet** — deviation D-3 |
| Windows | Deferred (architecture §8) |

## 1. Grounding — what this design must satisfy

Traversal: `spec-cfd-workbench-v1` → `architecture-application` §10 → `adr-0005`, `adr-0006`, `adr-0007` §3, `adr-0001`
→ `design-app-shell` §3.6–3.8, §6.7, §12.4 → `rulings` 53–55 → `mockup-workbench-v10` (+ `.html`) → `defect-classes`
(M1.2a entries) → `coordination-app-shell-build` (Planned vs actual). The code was opened, not recalled.

| Source | Statement this design must satisfy |
|---|---|
| §10.6 M1.2b | "Select a rail point; Properties shows its derived type; gesture commit; estimates live during drag; Control ⇄ Anchor and tangent kinds on channels; typed Root/Tip chord." Demo: drag a TE point, watch MAC change, release, Undo; make a point an Anchor, save, reopen; type a root chord and read the residual |
| A4.15 | Anchor (on the curve, handles, Smooth · Symmetric · Corner) · Control (off the curve) · named points with constrained types; a type change is one undo step and changes the curve only between the nearest anchors |
| CAD-15 | Control: measured gap > identity tolerance, handles removed, helper text; Anchor passes through within identity tolerance; locality; handle behaviour per kind; named point read-only; Undo exact; save/reopen keeps types |
| CAD-16 | COPY-106/107/118; "15 cm" and `#root_chord * 0.5`; lock refusal; one undo step; no preview; Undo exact; a closing tip is not an input |
| CAD-17 | Wing block rows and order; "changes during the drag, before release"; nothing stored |
| A4.8 | Nudge 0.01 / 0.1 / 1 mm (⌘ fine, plain default, Shift coarse); 0.01 / 0.1 / 1 ° for angles; `value [unit]` and `#name` echoed; "a handle can be clicked to type" |
| A4.9 | A Tracing probe in every curve editor (η, % half-span, chord at the pointer); comb diagnostics |
| B7 | Workbench preset: Shift+LMB or MMB pan · wheel zoom · LMB selects · RMB context menu; arrows nudge the selection |
| ADR-0005 | Type = knot multiplicity (derived); `tangents` rows (4.1), Corner = no row; MakeAnchor by Boehm, anchor and handles move by the same Δ; MakeControl removes the handles; every change measured and reported; ceiling refusal names it |
| ADR-0006 | Chords refit each moved rail on its own knots and abscissae, ordinates only, with hard rows; the rule id and both numbers reported; estimates are `WingEstimates.From` on accepted or draft bytes |
| ADR-0007 §3 | Pointer-down opens the draft; each move advances the generation; release validates and applies when Certified, else cancels and shows the refusal; Escape cancels; nudges commit on key release, focus loss and deactivation; a held-key run is one gesture |
| Ruling 53 | DR-2 quarter-chord held, rigid x-translation at a root edit, said in the status line; DR-6 commit at gesture end; DR-9 root-flat on root-mirror-locked rails, both numbers reported; DR-10 ceiling 16, ADR-0001 amended here |
| Ruling 55 Q1 | The rail CV editor pane is also the UI for a resumed rail recovery draft; M1.2b keeps that path |
| app-shell §12.4 | M1.2b owns edge F11 P2-Yes and the v10 focus checks `focusAfterRootCommitTab`, `focusAfterTypeChange` |

**Code as built (10f0628) that constrains this design:**

- The rail draft is one vertex, ordinate only: `BeginRailEditCore` accepts only `leading`/`trailing`
  (`AuthoringSession.cs`:282-292); `UpdateDraftCore` calls `FoilSource.PatchRail` (`:586-591`), which rewrites one
  ordinate token (`FoilSource.cs`:186-201). No span (abscissa) move exists.
- `ApplyDimension` accepts only `span` (`AuthoringSession.cs`:678) and runs the full certificate under the session lock
  on the caller's thread (`:669-701`); the Desktop calls it on the UI thread (`WorkbenchController.cs`:213-228) and
  duplicates two Core refusal rules there (`:217-219`).
- **Reopen rebuilds the operation memo with a different payload from the one written at commit:** a dimension commit
  memoizes `"dimension:"+Name+":"+Text` (`AuthoringSession.cs`:674) but reopen writes `"apply:"+SessionBinding` for every
  non-open, non-undo cursor (`:802-810`), so a retry with the same operation id after reopen is refused
  `DOC-OPERATION-CONFLICT` instead of returning the prior id (finding F-5, a new defect class, §13).
- The parser reads the version token at `FoilSource.cs`:1044 but **checks it only in `Validate`** (`:1162`), after the
  whole grammar pass (`:130-131`). A file with a block an old build does not know fails as `DSL-SYNTAX`, not as newer.
  Channels are limited to 6–10 points (`:1117`); interior knot multiplicity up to p is already legal (`:1124`), so an
  Anchor is representable in 4.0 and only its tangent row is not. The last station must be at η = 1 (`:1187`).
- Boehm insertion refuses multiplicity ≥ p per insertion (`FoilSource.cs`:640-647); repeated single insertions reach 3.
- `Geometry.Assess` certifies only `root_mirror` locks (`Geometry.cs`:292), checks `P0.y == P1.y` (`:296`), requires
  `LE(0) = 0` (`:298`), and returns Not assessed on a conservative chord hull (`:321`).
- The receipt record serializes with no default-ignore setting (`AuthoringSession.cs`:860); `Intent` carries its own
  `JsonIgnore(WhenWritingDefault)` (`:14-15`). The receipt admits `draftId, generation, rail, vertexId` plus optional
  `intent` (`:909`); `EditReference` already accepts `dimension` with `root-chord`/`tip-chord` (`:941`).
- The authored projection marks both root vertices of a locked rail not editable (`FoilSource.cs`:44-48); the Desktop
  refuses a draft on them (`WorkbenchController.cs`:553-557). Undo/Redo are unavailable while any draft exists (`:124-130`).
- Estimates are computed only for the accepted revision (`WorkbenchController.cs`:195-211). The Wing block shows Span,
  Root chord, Tip chord, Area and Aspect ratio only, at 0.1 mm (`PropertiesView.cs`:86-116): CAD-17's Mean chord, MAC,
  Max t/c rows and "≈" marks are missing (finding F-1; M1.2b completes them because its demo reads MAC).
- Apply requires a 15-point preview sample (`WorkbenchController.cs`:722-725); a gesture must not pay for it.
- `SectionCanvas` (`SectionCanvas.cs`) is the in-repo precedent for a pointer canvas, but its accessibility peers are
  detached TextBlocks with no bounds or focus (`:437-441`), and its Tab handling cycles inside and never lets Tab leave
  (`:344-375`, a WCAG 2.1.2 trap, finding F-2). The Plan view copies neither.
- New foil has **10** points per rail (`FoilSource.cs`:292) — already at the 4.0 ceiling (the measured case for DR-10).
- Validation is single-flight (`DSL-VALIDATION-BUSY`, `AuthoringSession.cs`:604); `ProofBudget` is wall-clock
  (`Geometry.cs`:682).

**SHELLFIX preconditions (parallel track `fix/m12a-shell-visuals`).** Every Desktop track branches from `main` **after**
SHELLFIX joins: (a) viewport re-attach — the Plan canvas lives in a Dock document slot and must survive re-attach; all
view state (camera, comb toggle, selection) lives in the controller, so a re-attached control redraws from state;
(b) tab strips — the "Plan" document tab must render; (c) left pane — Properties must be visible. Core tracks (B0, B1a)
do not wait for SHELLFIX.

## 2. Responsibility

This slice owns: the Plan view (drawing, hit testing, hover, Tracing probe, selection, gestures, keyboard, camera,
comb); the point projection in Core (roles, freedoms, tangent kinds, display samples); the gesture draft (2D move,
handle co-motion, tangent constraints); point-type and tangent-kind commands; typed Root and Tip chord under the ruled
rules; FoilDSL 4.1 (`tangents`, 11–16 channel points) and the early version check; the receipt expansion for the new
edit kinds; the Properties rows for a point or handle and the complete Wing block; Browser rail groups; the retirement of
the rail CV editor pane with its recovery role moved to the Plan.

Not this slice's: section points and the section editor (M1.2c); Messages and Points panes (M1.2c); catalog (M1.2d);
floats and layouts (M1.2e); channel constructions; views other than Plan; certification itself (`Geometry.Assess` gains
only row checks and the range).

## 3. Data model (settled first)

### 3.1 Bounded context and ubiquitous language

Context: **Planform authoring**, inside the Authoring context (architecture §2). Terms used verbatim in code, copy and
tests: **Rail** (the leading-edge or trailing-edge channel curve); **Point** (a control vertex of a rail, by stable id);
**Point type** (Anchor point · Control point — derived); **Named point** (Root end · Tip end); **Handle** (the vertex next
to an Anchor or an end); **Tangent kind** (Smooth · Symmetric · Corner); **Gesture** (one drag, one nudge run or one typed
position); **Nudge run**; **Driving dimension** (Span · Root chord · Tip chord); **Blend rule** (`chord-blend-rootflat/1`,
`chord-blend-linear/1`); **Held line** (quarter chord); **Planform shift**; **Fit residual**; **Deviation from a straight
taper** (the operator's linear rule, in UI copy); **Tracing probe**.

### 3.2 Aggregates and invariants

| Aggregate (root) | The one invariant it protects | Referenced by |
|---|---|---|
| **Foil source revision** (the FoilDSL bytes of an accepted row) | The bytes parse and certify; they are the only geometry authority (ADR-0002) | accepted row id |
| **History** (accepted-row chain and cursor facts, `AuthoringSession`) | Append-only; exactly one row per committed gesture or command; Undo/Redo move a cursor, never rewrite | session |
| **Draft** (the one open draft) | At most one; generation strictly increases; its bytes are the only state; it becomes history only by Apply | draft id |

A **Point** is not an aggregate: its identity is `(curve, vertex id)` inside a revision, and its type, role and freedom
are **derived** from the knot vector and locks on read. A **tangent row** is part of the revision's bytes. **Camera, comb
toggle and selection** are session values owned by the controller, not persisted (Type-1: a recorded decision to discard).

### 3.3 Durable representation, grain, history rule, additivity

| Store / projection | Grain ("one row is exactly one …") | History rule | Measures |
|---|---|---|---|
| Accepted row (native envelope, existing) | one committed user edit — a drag, a nudge run, a typed position, a type change, a tangent change or a typed dimension; identified by accepted id; recorded at commit | Type-2 by construction (append-only) | none stored |
| Source revision (existing) | one complete FoilDSL document per accepted row | Type-2 | none |
| `tangents` row (new, in the source) | one tangent kind of one interior Anchor of one curve | versioned with its revision | — |
| `PlanformView` (projection, new) | one rendering of one source revision or draft generation (basis + generation) | not stored | display samples, non-additive |
| `DimensionOutcome` / `PointOutcome` (returned, new) | one command's result, returned at commit or carried by its refusal | not stored | fit residual, deviation, change: maxima, non-additive |
| `WingEstimates` (existing) | one source revision or draft generation | never stored (CAD-17) | area additive over the two halves; span, chords, MAC, AR non-additive |
| `apply` / `gesture.end` events | one commit / one gesture | ring of 256 (existing) | frames additive; durations and p95 non-additive |

**Derive, don't store.** Point type, role and freedom (knot multiplicity and locks); estimates; the Tracing probe. There
is **no** materialised derivation: reports are returned by the command and shown once; a past row's report is not
re-derived (so no rule table can silently change what a past report meant).

**The blend rule id is stored** (provenance, not a derived value): a chord receipt carries `rule` ∈
{`chord-blend-rootflat/1`, `chord-blend-linear/1`}, because a later build may change the rule table and the history
must still say which rule produced each revision. `BlendRule_LockStateToRuleId_Pinned` pins the table so any change fails
until a new rule id is added (Data & Persistence F3, Geometry F9).

**Writer and compute reader per persisted field:**

| Field | Writer | Compute reader |
|---|---|---|
| `tangents { "<id>" smooth \| symmetric }` | point commands (MakeAnchor writes `smooth`; SetTangent writes or removes) | `Geometry.Assess` (row check), `Planform.View` (kind), chord refit (KKT rows), gesture co-motion |
| header `foildsl "4.1"` | `EnsureHeader41` — the single writer, called by any patch that writes a row or takes a channel above 10 points; never lowers it | parser (range and grammar gate) |
| channel points 11–16 | MakeAnchor (Boehm) | parser, `Assess`, `WingEstimates`, `Planform.View` |
| receipt `curve` (new, optional, `JsonIgnore(WhenWritingNull)`) | point commands only | `EditReference` — accepted only when `rail` is `point-type` or `tangent-kind` |
| receipt `rule` (new, `JsonIgnore(WhenWritingNull)`) | chord commands only | `EditReference` — **required** on `root-chord`/`tip-chord` rows (none exist from before) and refused elsewhere; value in the closed set |
| receipt `rail` = `point-type` · `tangent-kind` (new values) | point commands | `EditReference`; `RecoveryReference` refuses them (a direct command never has a recovery row) |
| receipt `rail` = `leading`/`trailing` for a 2D gesture (existing shape) | gesture commit | `EditReference` (unchanged) |

### 3.4 Invariants enforced and tested

- Append-only: re-applying an operation id with a different payload is refused (`History_ReapplyOperationDifferentPayload_Refused`);
  a refused command leaves source, accepted count and undo depth byte-identical (`ApplyPointCommand_Refused_HistoryUnchanged`,
  `ApplyDimension_Refused_HistoryUnchanged`).
- One memo fingerprint per command, used at commit **and** at reopen replay, built only from persisted values: `rail`,
  `curve`, `vertexId`, `rule`, the parent accepted id and the **resulting source id**. Apply evaluates first (pure, under
  the lock), then compares; so the same operation id with a different Kind, kept handle or typed value produces a
  different source and is refused `DOC-OPERATION-CONFLICT`, in the session and after reopen. Two inputs that give the
  same source ("152.09" and "152.090") are deliberately the same command (`Reopen_RetrySameDimensionOperationId_ReturnsPriorId`,
  `Reopen_RetrySamePointOperationId_ReturnsPriorId`, `ApplyPointCommand_SameOperationDifferentKind_DocOperationConflict`,
  `Reopen_SameOperationDifferentKind_DocOperationConflict`; Patterns F1).
- One row per gesture (`Gesture_DragManyFrames_OneAcceptedRow`); a click adds none (`Gesture_ReleaseWithoutMove_NoAcceptedRow`).
- Parse gates (rows on non-anchors, kinds on rails, ranges per version) are in the §12.4 B0 list; every name above is
  attributed to its track there.

### 3.5 Point roles, freedoms and named points (derived, Core)

With Piegl–Tiller indexing on a rail of n points (degree 3): vertex *i* is an **Anchor** iff one interior knot has
multiplicity exactly 3 at t(i+1) = t(i+2) = t(i+3) (ADR-0005 §2). Anchors lie at indices 3 … n−4, and two anchors are at
least three indices apart (their knot runs are disjoint, parser `:1124`), so each handle belongs to exactly one anchor
or end (Geometry lens, Verified). Plan coordinates: span y = η · half-span, aft x.

| Role | Vertex | Type shown | Freedom | Constraint (copy mapped in Properties from role and locks) |
|---|---|---|---|---|
| Root end | 0 | Anchor · root end | LE: **Fixed** (LE(0) = 0, `Geometry.cs`:298). TE: **Aft only**, coupled to its handle under `root_mirror` | LE: "Fixed: the leading edge starts at the root." TE: "On the centre line; its tangent is square to it (root mirror)." |
| Root handle | 1 | Handle of the root end | under `root_mirror`: **Span only** (its aft equals the root end's); otherwise Free | "Moves along the span only (root mirror)." |
| Control | any other non-anchor, non-handle | Control point | Free | — |
| Anchor | i with a multiplicity-3 knot | Anchor point | Free; its handles move with it by the same Δ | — |
| Anchor handle | i ± 1 of an Anchor | Handle | Free, under its anchor's tangent kind | — |
| Tip handle | n−2 | Handle of the tip end | Free | — |
| Tip end | n−1 | Anchor · tip end | **Aft only** (η = 1) | "At the tip; moves fore and aft only." |

A **multiplicity-2 knot** is legal (parser `:1124`) but never written by the product: its vertices stay Control points,
and the comb shows the C¹ joint with a small tick on the curve (`PointModel_MultiplicityTwoKnot_ControlPointsAndC1Marker`).

Ordering: every move keeps span positions strictly increasing (FoilDSL §5 item 4). The gesture clamps the moving set to
a gap of **min(1 mm, the gap when the gesture began)** of span from the nearest vertex not moving — read once at Begin,
not per frame, so a drag cannot close a tight gap step by step to zero, and an existing tight gap never freezes a handle
(**`simplify:`** fixed 1 mm floor; ceiling: a pair closer than 1 mm can open by drag but cannot close further except by
typing; upgrade trigger: an operator report or a fixture that needs sub-millimetre spacing).

### 3.6 Tangent kinds on rails — affine-invariant kinds only

A rail is a graph x(η) drawn at the document's half-span. A typed **Span** rescales the span axis only (ADR-0006 §2).
Collinearity (Smooth) and the midpoint property (Symmetric = collinear handles of equal length ⇔ the anchor is the
midpoint) are invariant under that map, so a Span commit never breaks a rail row. Horizontal, Vertical and Fixed angle are
not (Vertical is impossible on a graph), so **on a rail only `smooth` and `symmetric` rows are legal**; another kind on a
channel curve is `DSL-LOCK` at parse — matching the spec's rail list (A4.15). `Geometry.Assess` checks in plan
coordinates at the document half-span: Smooth — directions within 0.1° (A4.5); Symmetric — the anchor is the handles'
midpoint within relative 10⁻⁶ of the handle length (ADR-0005 §4). With fixed abscissae both rows are **linear** in the
ordinates, so an ordinate-only chord refit takes them as KKT rows exactly (ADR-0006 §3; Geometry lens, Verified). Co-moved
handle coordinates are written with `ExactDecimal`, never re-rounded, so a Smooth row holds through a later large Span
change (Geometry F5).

### 3.7 Quantized steps, exact positions

The **step** of a gesture is quantized, not the absolute position: the grabbed point's Δ is rounded once to 1 µm of aft
(in the document unit) and η 10⁻⁷ of span, and the whole moving set moves by that same Δ. Positions are written by the
as-built `ExactDecimal` (`FoilSource.cs`:466) of the binary64 result. The opposite handle of a Symmetric anchor is
derived as 2A − H with no second rounding; a Smooth one as a rotation, written exactly. Consequences: nudges are multiples
of their step within 1 ulp on any starting position; Symmetric stays a midpoint after an earlier exact-decimal write
(MakeAnchor, a refit, a SetTangent) — the defect the Geometry lens measured at 500 nm against a 25–39 nm tolerance when
absolute positions were rounded (`UpdatePointGesture_SymmetricAnchorDragAfterRefit_RowHolds`). Readable decimals are not a
goal: sources written by fits already carry full binary64 expansions.

### 3.8 FoilDSL change (expand-only) and the ADR-0001 amendment (DR-10)

- **Expand:** the parser accepts `"4.0"` and `"4.1"`. Under `"4.1"`: the optional `tangents` block (ADR-0005 grammar) and
  **channels of 6–16 points**. Under `"4.0"`: unchanged (6–10, and a `tangents` block is `DSL-SYNTAX`). Sections stay
  6–32. Evaluator `cfdw-cv/2` and canonical format `foildsl-geometry-4.0` are unchanged; `tangents` is stripped from
  canonical input with ids and locks (`foildsl.md` §8), so identities and run keys do not move — including when a header
  is rewritten from 4.0 to 4.1 on unchanged geometry (`Identity_Header41RewriteSameGeometry_DefinitionHashUnchanged`).
- **Forward control (new, B0):** the version is checked right after it is read (`FoilSource.cs`:1044), before any block.
  A future `"4.2"` file with an unknown block is then `DSL-VERSION`, not `DSL-SYNTAX` (`Parse_Foil42UnknownBlock_DslVersion`).
- **What an old build actually says (Data & Persistence F1, Verified in code; recorded deviation D-5):** builds up to
  M1.2a read the whole grammar before the version check, so a bare 4.1 `.foil` **with a row** fails `DSL-SYNTAX` (shown
  as NotRecognised, app-shell §6.3); a 4.1 `.foil` with 11–16 points and no row reaches `DSL-VERSION` (COPY-103 "saved by
  a newer version"); a **project** with a point-type receipt fails first on `DOC-UNSUPPORTED-FIELD` (`:909`,
  UnknownContent, COPY-130). In every case the file is unchanged. ADR-0005's "the as-built parser refuses with
  `DSL-VERSION`" is corrected here and flagged on the ADR (V16).
- **Receipt compatibility (Data & Persistence F2):** `curve` and `rule` are `JsonIgnore(WhenWritingNull)`, so a project
  whose history holds only drags, nudges and Span edits saves with no new key and still opens in an M1.2a build
  (`Save_DragAndSpanOnlyHistory_NoNewReceiptKeys`).
- **Migrate:** none forced. `EnsureHeader41` rewrites the header in the same transaction as the first row or the 11th
  channel point, and never lowers it (Make Control of the last anchor keeps `"4.1"`). A document that never needs 4.1
  stays `"4.0"` byte-for-byte.
- **Contract:** nothing removed. **Rollback:** no downgrade path is built (an "Export as 4.0" is possible only at ≤ 10
  points with no rows). The old-build behaviour above is recorded as a **characterization receipt** by running the
  committed 4.1 fixtures, and a project saved by the new build with only drag, nudge and Span edits, through the `10f0628`
  build before B0 changes the parser (the second proves the no-new-key file really opens there) (`docs/proof/m12b-old-build/`, a B0
  exit item); a post-change red-first run is not possible because no 4.0-only build remains afterwards (Test Architect F5).
- **ADR-0001 Amendment 1** (DR-10, Ruling 53): channels 6–16 under 4.1, default unchanged. Measured basis: an interior
  Anchor adds one to three points (Boehm to multiplicity 3; fewer when u\* lands on an existing knot); New foil ships at 10
  (`FoilSource.cs`:292), so under 6–10 it can hold no interior anchor; 16 holds two on New foil and three on the Example.
- `foildsl.md` §4/§5/§8 and its conformance cases change in the same change as the parser (B0), never before (ADR-0005).

### 3.9 Typed chord mapping under the ruled rules (DR-2, DR-9)

For a root edit to typed chord c_r (f = c_r / c(0)) or a tip edit to c_t (f = c_t / c(1)); the tip is always the last
station at η = 1 (parser `:1187`):

- **Blend s(η):** `chord-blend-rootflat/1` when either rail carries `root_mirror` (a linear s has s′(0) ≠ 0 and would put a
  slope on a locked rail): root s = f + (1 − f)η², tip s = 1 + (f − 1)η². Otherwise `chord-blend-linear/1`, the
  operator's rule: s = f + (1 − f)η or 1 + (f − 1)η. When only one rail is locked the root-flat target still has a small
  root slope on that rail; the `root_mirror` hard row absorbs it and the fit residual reports it
  (`ApplyDimension_OneRailLocked_ResidualReported`; Geometry F7).
- **Held line (DR-2):** LE′ = LE + (1 − s)c/4, TE′ = LE + c/4 + 3s·c/4 (so c′ = s·c and the quarter-chord line is held).
  Both rails are refitted on their own knots and abscissae, ordinates only (`ConstrainedFit.Solve`), with hard rows: typed
  end exact, other end exact, `root_mirror` (P0 = P1), tangent rows (§3.6). A root edit then shifts both rails by
  **δ := the fitted LE P0 bits**, so LE(0) = 0 and P0 = P1 hold bitwise (Geometry F8); the status line says so.
- **Acceptance (DR-12, ruled accept-and-report, Ruling 56):** the fit residual against the blend target, max over both
  rails on the distribution-curve oracle (201 η + every knot image), is compared with the 10 µm model/join tolerance.
  At or below it the commit is plain; **above it the commit is still accepted** and reported as a **warning** with the
  number and the limit. The typed end stays exact in both cases (hard row). Every commit reports **both numbers**: the
  fit residual and the deviation from the straight taper. This is a deliberate deviation from A4.6's "above its
  acceptance disables Apply" for typed chords only (D-6; spec-owner finding F-8).
- **Spike** (`docs/proof/cad-first-spikes/m12b-quarter-chord/`, run 2026-09-30; re-run bit-identical by the Geometry lens):

| Foil | Edit | Fit residual LE · TE | From straight taper | Planform shift | Result (Ruling 56) |
|---|---|---|---|---|---|
| New foil (10 pts) | root ×1.01 · ×1.1 · ×1.2 | 0.15 · 0.45 / 1.50 · 4.51 / 3.00 · 9.01 µm | 0.21 / 2.11 / 4.21 mm | 0.32 / 3.17 / 6.34 mm | accepted |
| New foil | root ×1.5 | 7.51 · 22.53 µm | 10.54 mm | 15.84 mm | accepted, **warning** |
| New foil | tip ×0.8 · ×0.5 | 3.00 · 9.01 / 7.51 · 22.53 µm | 4.21 / 10.54 mm | 0 | accepted / accepted, **warning** |
| Example (7 pts) | root ×1.1 · ×1.2 · ×1.5 | 2.82 · 8.46 / 5.64 · 16.92 / 14.10 · 42.29 µm | 2.24 / 4.49 / 11.21 mm | 3 / 6 / 15 mm | accepted / accepted, **warning** / accepted, **warning** |

  The residual is linear in |f − 1| (TE 45.05 µm per unit of f − 1 on New foil) and the trailing-edge rail carries three
  times the leading-edge share. Without a warning a New foil takes about ±22 % per commit and the Example ±12 %; larger edits are accepted with the warning.
  Refinement does not cure it (`typed-chord-map/results.txt`: 80.7 → 73.4 µm from 7 to 10 points).

## 4. Persistence

No new store. The native envelope (`ProjectStore`) changes only by the receipt's optional `curve` and `rule` and two new
`rail` values (§3.3). `NativeProject.Read` adds `"curve"` and `"rule"` to the optional list (`AuthoringSession.cs`:909);
`EditReference` gains:

```csharp
"point-type" or "tangent-kind" => receipt.Rule is null && receipt.Curve is "leading" or "trailing"
    && EditTarget(child, receipt.Curve, vertexId) && EditTarget(parent, receipt.Curve, vertexId),
"dimension" => receipt.Curve is null && vertexId is "span" or "root-chord" or "tip-chord"
    && (vertexId == "span" ? receipt.Rule is null : receipt.Rule is "chord-blend-rootflat/1" or "chord-blend-linear/1"),
"leading" or "trailing" => receipt.Curve is null && receipt.Rule is null && /* as built */ …,
```

Both parent and child hold a point command's id (MakeAnchor gives the new anchor P's id; MakeControl keeps the anchor's
id and removes its handles). `RecoveryReference` refuses both new rails (`_ => false`, unchanged). The replay memo uses
one `Fingerprint(receipt)` shared with commit (§3.4, finding F-5).

**Recovery (Ruling 55 Q1 kept):** a rail recovery row (`rail` leading/trailing, written by M1.1/M1.2a) resumes onto the
Plan: the point is selected and drawn in draft style, and the alert band reads "A recovered edit to Trailing edge point 4
is open." with **Apply** and **Discard**. Apply validates and applies one row (its receipt has no `curve`); Discard
clears the recovery row. Core fixtures use golden bytes written by M1.2a, not synthesized ones
(`Recovery_M12aGoldenRailDraft_ApplyOneRowNoCurve`, `Recovery_M12aGoldenRailDraft_DiscardClears`).

## 5. Contracts

### 5.1 Exposed — Core (new unless marked)

```csharp
// src/CfdWorkbench.Core/PointModel.cs (new) — Projection (read model), derived on read; no state.
public enum PointRole { RootEnd, RootHandle, Control, AnchorHandle, Anchor, TipHandle, TipEnd }
public enum TangentKind { Corner, Smooth, Symmetric }          // Corner = no row (ADR-0005 §4)
public enum PointFreedom { Fixed, SpanOnly, AftOnly, Free }
public sealed record PointView(string Curve, string Id, int Index, double Eta, double SpanMeters, double AftMeters,
    PointRole Role, string? AnchorId, TangentKind? Kind, PointFreedom Freedom, IReadOnlyList<string> Locks);
public sealed record PlanSample(double SpanMeters, double AftMeters);
public sealed record CombTooth(double SpanMeters, double AftMeters, double NormalSpan, double NormalAft,
    double Curvature, bool BreakBefore);                         // one-sided at multiplicity-3 knots: two teeth per anchor
public sealed record CurveView(string Curve, int Ceiling, IReadOnlyList<double> Knots,
    IReadOnlyList<PointView> Points, IReadOnlyList<PlanSample> Samples);
public sealed record PlanformView(string SourceHash, string Basis, long Generation, string Version,
    double HalfSpanMeters, CurveView Leading, CurveView Trailing, IReadOnlyList<AuthoredAssignment> Stations);
public sealed record ProbeReading(double Eta, double SpanMeters, double LeadingAftMeters, double TrailingAftMeters,
    double ChordMeters);
public static class Planform
{
    // Display path: binary64 SplineBasis (note-20260926-binary64-evaluator); never enters ProofBudget (BUDGET-DISPLAY).
    public static PlanformView View(byte[] source, string basis, long generation);
    public static IReadOnlyList<CombTooth> Comb(CurveView curve);           // evenly spaced by arc length, ≥ 8 per span
    public static ProbeReading Probe(PlanformView view, double eta);          // A4.9 Tracing probe
    public static (double SpanMeters, double AftMeters) HandleTarget(PlanformView view, string curve,
        string handleId, double angleDegrees, double lengthMeters);          // typed angle/length → plan target
}

// src/CfdWorkbench.Core/AuthoringSession.cs (expanded; as-built members stay until U2 contracts them)
public SessionDraft BeginPointGesture(string draftId, string curve, string vertexId);
    // DOC-CLOSED · DSL-DRAFT-OWNED · DSL-DRAFT-REUSED · DSL-TARGET · DSL-LOCK (Fixed) · DSL-NOT-ASSESSED
public GestureFrame UpdatePointGesture(string draftId, long generation, double spanMeters, double aftMeters);
    // target of the grabbed point; Core applies freedom, ordering clamp, co-motion and tangent constraint (§6.3),
    // re-parses and checks bits. DSL-CONFLICT on a stale generation; DSL-PATCH if a resolved set fails to re-parse
    // (backstop only). Never throws on a finite target: an unreachable target is clamped (Clamped = true).
public sealed record GestureFrame(SessionDraft Draft, double SpanMeters, double AftMeters,
    IReadOnlyList<string> MovedIds, bool Clamped);
public PointOutcome ApplyPointCommand(string operationId, PointCommand command);   // command message + handler; memoized
public abstract record PointCommand(string Curve, string VertexId)
{
    public sealed record MakeAnchor(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record MakeControl(string Curve, string VertexId) : PointCommand(Curve, VertexId);
    public sealed record SetTangent(string Curve, string VertexId, TangentKind Kind, string? KeepHandleId)
        : PointCommand(Curve, VertexId);
}
public sealed record PointOutcome(string AcceptedId, double MaxDeviationMeters, int PointsBefore, int PointsAfter);
public DimensionOutcome ApplyChord(string operationId, DimensionCommand command);  // root-chord · tip-chord
public sealed record DimensionOutcome(string AcceptedId, DimensionReport Report);
public sealed record DimensionReport(string Dimension, double TypedMeters, string Rule, double FitResidualMeters,
    double ToleranceMeters, double DeviationFromLinearMeters, double PlanformShiftMeters, bool FitAboveLimit);
public sealed class DimensionRefused(DimensionReport report, string code) : ContractError(code) { public DimensionReport Report { get; } = report; }
// ApplyDimension(operationId, DimensionCommand) — as built, Span only; unchanged. Span keeps the old shape because it is
// exact (no fit, nothing to report) and its callers and tests exist; chords need a report, so they get ApplyChord.
// Internally each command is Evaluate(bytes, command) → (report, patched bytes); Apply = memo check → Evaluate under the
// lock → admission → commit or throw. One pure function per command kind; no public plan query (Simplifier F1/F2).

// src/CfdWorkbench.Core/LengthExpression.cs (new) — A4.8 / CAD-16 entry
public static class LengthExpression
{
    // number [mm|cm|m|in] | #span | #root_chord | #tip_chord, with + − * / and parentheses; ≤ 256 chars, depth ≤ 16.
    // A length (first power) is required; rounded half-even to 1 µm. DSL-INVALID-NUMERIC · DSL-UNIT.
    public static double ParseMeters(string text, IReadOnlyDictionary<string, double> dimensionsMeters);
}
```

Refusal codes (all existing): `DSL-LOCK` (fixed point, named point type, row on a non-anchor, angle kind on a rail) ·
`DSL-CURVE` (ceiling — "Making this an anchor needs 3 more points. This rail has 14 of 16." — or a new handle closer than
the η grid to a neighbour) · `DSL-EDGES-CROSS` ·
`DSL-NOT-ASSESSED` · `DSL-TARGET` (tip closes; unknown dimension) · `DSL-UNIT` · `DSL-INVALID-NUMERIC` · `DSL-DRAFT-OWNED`
· `DSL-CONFLICT` · `DSL-PATCH`. The `SessionEvent` for `apply` gains optional `FitMicrometres`, `DeviationMicrometres`,
`ShiftMicrometres` and `FitAboveLimit` (bool) for chords — how often and by how much typed chords exceed the limit.

### 5.2 Exposed — Desktop

```csharp
// WorkbenchController (expanded)
public PlanformView? Planform { get; }               // accepted, or the draft's generation during a gesture
public GestureState Gesture { get; }                  // Idle · Pressed · Dragging · Nudging · Busy
public bool BeginGesture(PointRef point, GestureInput input);   // Pointer · Keyboard · Typed
public void UpdateGesture(double spanMeters, double aftMeters); // latest-wins, applied once per render frame
public Task<GestureOutcome> EndGestureAsync(GestureEnd reason); // flushes the pending target first (§6.2)
public Task<CommitOutcome> ApplyPointCommandAsync(PointCommand command);   // Busy while it runs
public Task<CommitOutcome> ApplyChordAsync(string dimension, string text); // Busy; Task.Run + stale-drop like PreviewAsync
public PlanCamera PlanCamera { get; set; }
public bool CombVisible { get; set; }
public abstract record GestureOutcome { Committed(string AcceptedId, string Report) · Refused(string Code, string Copy)
    · Cancelled(string Copy) · NoChange }
// PlanCanvas : Control (new, src/CfdWorkbench.Desktop/PlanCanvas.cs) — reads controller state, owns none;
//   PlanPointPeer : AutomationPeer per point and handle (bounding rect, focus, selection, Invoke).
// Properties: PropertiesView.Build gains point and handle rows (§11.4); Wing rows complete (CAD-17).
// CommandTable rows (no default gestures): point.make-anchor, point.make-control, point.tangent-smooth,
//   point.tangent-symmetric, point.tangent-corner (Edit menu); view.comb "Curvature comb" (C, Plan focused only),
//   view.zoom-in ⌘=, view.zoom-out ⌘− (View menu); view.fit ⌘0 exists.
```

### 5.3 Consumed

| Contract | Source | Confidence |
|---|---|---|
| Boehm insertion, one knot per call, multiplicity < p | `FoilSource.cs`:640-668 | Verified (read) |
| `ConstrainedFit.Solve` with equality rows | `ConstrainedFit.cs`; used via `FitOrdinates` (`FoilSource.cs`:356-376) | Verified (read) |
| `SplineBasis` binary64 evaluator | `ConstrainedFit.cs`:576; `WingEstimates.cs`:198-205 | Verified (read); agreement with the Bernstein spans to 10⁻¹² relative is tested (`PlanformView_SplineBasisVsBernstein_Within1e12`) |
| `WingEstimates.From(bytes, basis, generation)`; `WingEstimates.ChordMeters` | `WingEstimates.cs`:26-48 | Verified (read) |
| `Geometry.Assess` and its wall-clock 1 s proof budget | `Geometry.cs`:280-321, :682; `AuthoringSession.cs`:147 | Verified (read) |
| Avalonia 11.3.14 `Control.Render`, pointer capture, `KeyDown/KeyUp`, custom `AutomationPeer` | `SectionCanvas.cs` (same version) | Verified (in-repo) for rendering and input; **Inferred** for a peer with bounds, focus and Invoke on the macOS bridge — `assume:` Avalonia's macOS AX bridge exposes a custom peer's bounding rect, keyboard focus and AXPress; confirm: `PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` plus an AX dump and a VoiceOver trace in U3; if false, U1b stops and reports (no silent downgrade to TextBlock peers) |
| Avalonia `RenderTargetBitmap` of the **whole window** in the console harness on macOS | not used in-repo | **Inferred** — `assume:` it renders the realized window with Skia off-screen; confirm: `PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` first; if false, U1b reads pixels from a window screenshot through the supported native attach, and U1b owns that fallback |
| `TopLevel.RequestAnimationFrame` | Avalonia 11 API | **Inferred** — `assume:` present in 11.3; confirm at U1a compile; fallback `Dispatcher.UIThread.Post(…, DispatcherPriority.Render)` |

## 6. Patterns and structure

### 6.1 Named patterns (checked by the Patterns Expert and the Simplifier; see Gate record)

| Pattern | Where | Why this, not a bespoke shape |
|---|---|---|
| **Command message + handler** with **idempotent receiver** (LOA P8) | `ApplyPointCommand`, `ApplyChord` | The as-built `ApplyDimension` shape; memoized by operation id with one fingerprint shared by commit and reopen replay |
| **Evaluate / Apply** (one pure evaluation per command; Apply = memo → evaluate under the lock → admit → commit or throw) | chords, point commands | The refusal must show numbers; the report travels in the outcome or in `DimensionRefused` — no second route (Simplifier F2) |
| **Projection / read model** | `Planform.View`, `Probe`, `PropertiesView.Build` | Derive, don't store; one definition per quantity |
| **State machine as a transition table** | controller gesture (§6.2) | Replaces boolean flags (`isDragging`); every cell is a transition or an explicit "ignored", and one test row |
| **Observer** (existing) | `Changed` / `SelectionChanged` | Consistent state before notify (app-shell §3.6) |
| **Latest-wins throttle with trailing flush** | `UpdateGesture` / `EndGestureAsync` | Pointer events outpace patch + parse + estimates; only the newest target matters, and the release flushes it so the committed position is where the user let go |
| **Advisory check + authoritative check** | live crossing marker (binary64) vs `Geometry.Assess` at release | The certificate costs up to 1 s; the advisory tier never decides (`PlanCanvas_AdvisoryCrossingClear_CertificateStillDecides`) |
| **Revision cursor over append-only history** (existing; memento-like) | Undo/Redo | Undo is a cursor move, never an inverse command |
| **Parallel Change** (expand–migrate–contract in code) | `UpdatePointGesture` beside `UpdateDraft(si)`; `RailEditorPane` retired | ADR-0007 §2 precedent |

Rejected: a view-model per point (MVVM items) — the canvas draws one projection, as `SectionCanvas` does. A second
evaluator for the plan curves — the display path reuses `SplineBasis`. An undo macro grouping frames — one draft already
yields one row. Public plan queries (`PlanDimension`, `PlanPointCommand`) and `DescribeEdit` — cut at the gate (Simplifier
F1/F2): nothing calls a plan before commit (CAD-16: no preview), and no past report is re-derived. A new code
`DSL-CHANNEL-ORDER` — the re-parse already refuses a broken order (Simplifier F3).

### 6.2 Gesture state machine (controller) — transition table

States: **Idle**, **Pressed** (pointer down on a movable point, < 3 px), **Dragging**, **Nudging** (arrow held or
repeating), **Busy** (a gesture commit or a direct command is validating). "Ignored" means no state change and no draft.

| Event ↓ / State → | Idle | Pressed | Dragging | Nudging | Busy |
|---|---|---|---|---|---|
| Pointer-down on movable point/handle | Begin draft → Pressed | ignored | ignored | end run (commit) → Busy; the press only selects | select only; status "Checking the last change…" |
| Pointer-down on fixed point | select; status names the lock | ignored | ignored | end run → Busy | select only |
| Move < 3 px | — | stay | Update | ignored | ignored |
| Move ≥ 3 px | — | → Dragging | Update (Shift = axis lock) | ignored | ignored |
| Release | — | Cancel draft → Idle (NoChange; selection only) | flush → Busy; at the start point → Cancel, NoChange, no validate | ignored | ignored |
| Arrow key on focused point/handle | Begin + Update → Nudging | ignored | ignored | Update | ignored (status) |
| Arrow KeyUp | — | — | — | flush → Busy | — |
| Escape | clear tooltip, else clear selection (on a handle: focus its point) | Cancel → Idle | Cancel → Idle (Cancelled, "Drag cancelled. The point is back where it was.") | Cancel → Idle | ignored |
| CaptureLost | — | Cancel → Idle | Cancel → Idle (Cancelled, same copy) | — | — |
| FocusLost / Deactivated | — | Cancel → Idle | Cancel → Idle | flush → Busy (commit) | continue |
| Save / Close / Open / New | proceed | Cancel, proceed | flush → Busy, then proceed | flush → Busy, then proceed | wait for Busy to end, then proceed |
| Typed position / handle angle-length (Properties) | Begin + Update + flush → Busy | ignored | ignored | ignored | ignored |
| Direct command (type, tangent, chord) | → Busy | ignored | ignored | ignored | refused (disabled) |
| Busy completes (any result, stale or not) | — | — | — | — | → Idle in `finally`; a stale result is not shown, but the state still exits |

- **Drag threshold** 3 px, so a click never opens a row. **Capture lost** cancels a drag (the hand is still down and the
  user has not finished); a nudge run **commits** on focus loss or deactivation because each step was already a seen,
  deliberate change (ADR-0007 §3). The marine-CAD lens accepted the split (Blender modal rule).
- **Trailing flush:** entering Busy first sends any pending coalesced target through `UpdatePointGesture`, so the
  committed position equals the last pointer target (`Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget`).
- **Busy** covers both gesture commits and direct commands (chords, types, tangents); all run `Validate`/evaluate on
  `Task.Run` with cancellation and a stale-drop, exactly like `PreviewAsync` (`WorkbenchController.cs`:684-720), so the
  UI thread never waits on the session lock. The Desktop does not repeat Core refusal rules (unlike `ApplySpan`
  `:217-219`, recorded as finding F-6).

### 6.3 Motion rules (Core, one authority)

`UpdatePointGesture(target)`: (1) apply the role's freedom (§3.5); (2) compute Δ of the grabbed point and quantize it
(§3.7); (3) move the set by Δ — the point alone, an anchor with both handles, or the TE root end with its handle under
`root_mirror`; (4) for a dragged anchor handle apply the kind to the opposite handle in plan coordinates — Smooth: opposite
= A − u·|opposite − A|, u the unit direction to the dragged handle; Symmetric: opposite = 2A − dragged; Corner: none;
(5) clamp span ordering against the nearest non-moving vertices with the min(1 mm, gap at Begin) rule; if the resolved
opposite handle would break ordering, keep the last valid frame and set `Clamped`; (6) patch every moved vertex
(abscissa and ordinate) with `ExactDecimal` in one byte edit, re-parse, and check the written bits. Nudges, typed
positions and typed handle angle/length (via `Planform.HandleTarget`) use the same call. **The Desktop never computes
geometry**: it sends targets and draws the returned frame. Arrow keys on a handle move it along the screen axes like a
point, with co-motion (B7; marine-CAD F4); the angle ladder (0.01 / 0.1 / 1 °) lives on the typed angle field.

### 6.4 Point commands (Core)

- **MakeAnchor** (Control point P at index i between its neighbouring anchors or ends): find the unique u\* on that
  segment with η(u\*) = P.η (η is monotone on every span); snap to an existing knot within 10⁻¹² relative; insert u\*
  until its multiplicity is 3 (Boehm, exact; one to three insertions); move the interpolated vertex and both handles by
  Δaft = P.aft − C(u\*).aft (ADR-0005 §5; the handles stay collinear so the `smooth` row written is true). The anchor
  takes P's id; new handle ids are minted `cv-<n>` past the highest numeric suffix. Refused `DSL-CURVE` if the result
  exceeds 16 (naming the ceiling and the count) or if a new handle lands closer than the η grid (10⁻⁷) to a neighbour
  (Geometry F3). Calls `EnsureHeader41` and writes the row in the same patch. Returns the measured change.
- **MakeControl** (Anchor A): delete its two handles and remove two copies of its knot (multiplicity 3 → 1), with **no**
  recomputation of other vertices: every other vertex keeps position and id (ADR-0005 §5, the one construction named;
  Geometry F10). Remove its row. Returns the measured change. Not the inverse of MakeAnchor; Undo is.
- **SetTangent(kind, keep)**: rewrite the row (Corner removes it). If the kind is not already satisfied, adjust only
  ordinates first; span positions move only toward the anchor, so ordering is always kept (Geometry F2): **Smooth** — the handle not kept moves its
  aft onto the line through A and the kept handle, at its own η; with no handle selected, both handles move onto the
  bisector line at their own η (marine-CAD F7). **Symmetric** — first Smooth, then shorten the longer handle along its line
  to the shorter handle's Δη (only toward the anchor). The kept handle is the selected one, even when it is the longer:
  then the other handle is lengthened along its line only if that keeps order, else the kept one is shortened and the
  status line says so; the status line always names what moved ("Lined up the handle toward the tip"). Otherwise a source-only revision.
- **Named points and handles:** any type command on a root end, tip end or handle is `DSL-LOCK` (read-only in the UI).

## 7. Error and concurrency model

- **Threads:** the UI thread owns the controller. Per-frame `UpdatePointGesture`, `Planform.View` and
  `WingEstimates.From` run on the UI thread (small sources; measured by `gesture.end`). Every commit and direct command
  validates on `Task.Run` with a cancellation token; a completion whose `stateVersion`, draft id or generation changed is
  not shown, and Busy still exits in `finally` (Patterns F2).
- **Single committer:** Busy refuses a second begin, so `DSL-VALIDATION-BUSY` cannot arise from the UI.
- **Drag during proof:** covered by Busy (§6.2). Budget exhaustion (`GEOMETRY-BUDGET`) → Not assessed → "This change
  couldn't be checked, so it wasn't applied. Nothing changed."; the draft is cancelled.
- **Refusal:** every refusal cancels the draft or never creates a row; source, accepted count and undo depth are
  unchanged; the point is redrawn at its accepted position.
- **Lock:** a Fixed point starts no draft; the pointer shows not-allowed; the lock copy is announced assertively (CAD-04).
  A foil that is not Certified shows points dimmed and read-only with the banner "Points can't be moved because this foil
  couldn't be checked. Nothing changed."
- **Undo/Redo** stay disabled while a draft exists or Busy, and re-enable at Idle.
- **Idempotency:** commands memoize by operation id with one fingerprint for commit and replay; a gesture commit carries a
  fresh operation id.

## 8. Change-surface list (E7)

| Layer | M1.2b change | Track |
|---|---|---|
| Store | FoilDSL `"4.1"`: `tangents`, 6–16 channel points, early version check; receipt `curve`, `rule` (`JsonIgnore(WhenWritingNull)`), `rail` values `point-type`, `tangent-kind`; chord rows now written | B0, B1a, B1b |
| Model (Core) | parser 4.1; `Curve.Tangents`; derived roles and freedoms; ceiling by version; `EnsureHeader41` | B0 |
| Service (Core) | `BeginPointGesture`/`UpdatePointGesture`; `ApplyPointCommand`; `ApplyChord`; memo `Fingerprint`; `Assess` row checks; `LengthExpression` | B0, B1a, B1b |
| Projection / wire | `PlanformView`, `CurveView`, `PointView`, `CombTooth`, `ProbeReading`, `PointOutcome`, `DimensionOutcome`/`Report`; CLI `inspect --json` gains `points` (role, kind, locks) per rail | B0, B1a, B1b |
| Client type (Desktop) | `GestureState`, `GestureOutcome`, `CommitOutcome`, `PlanCamera`; `Selection.Points` reconcile against roles | U1a |
| UI | `PlanCanvas` + point peers; Plan tab; "3D samples" tab; Properties point and handle rows; Wing Root/Tip inputs and full estimates; Browser rail groups; context menu; command rows; alert-band recovery Apply/Discard; status-line reports; tokens | U1b, U2, U3 |
| Compute reader | `Assess` (rows, range), `WingEstimates` (drafts), chord refit (rows as KKT), canonical identity (strips rows) | B0, B1a, B1b |
| Accessibility | peers with bounds, focus, selection, Invoke; live status; handle names | U1b, U3 |
| Telemetry | `gesture.end`; `apply` edit kinds and chord numbers | B1a, B1b, U1a |
| Contract (remove) | `RailEditorPane`; `BeginEdit(rail,id)`, `UpdateDraft(si)` and `PatchRail` once no caller remains, their tests ported | U2 |

## 9. Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed | Detection | Test |
|---|---|---|---|---|---|
| Plan hosted but not realized or not visible (zero bounds, detached, occluded) — the M1.2a operator report | Dock document slot | prevent + detect | state in controller; SHELLFIX precondition; whole-window pixel checks | `shell.pane.render` error | `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints` (U1b), `PlanCanvas_AfterDockReattach_RendersSameScene` (U1b) |
| Plan render throws | custom drawing | mitigate + detect | per-pane failure copy + Try again | `shell.pane.render` error with exception type | `PlanCanvas_RenderThrows_CopyTryAgainAndEvent` (U1b) |
| State says selected but pixels do not show it (UI-RENDERED-STATE) | custom canvas | detect | rendered, whole-window assertions | — | `PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn` (U1b), `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne` (U1b) |
| Visible control does nothing (UI-DEAD-CONTROL) | new rows, menus, context menu | prevent | sweep extended; one effect test per control | — | `UI_DEAD_CONTROL_PointAndWingControlsHaveActions` (U2), `CommandTable_PointRows_ExecuteOrDisabled` (U2) |
| Click adds an undo step | commit at release | prevent | 3 px threshold; NoChange | `gesture.end` no-change | `Controller_MoveTwoPixels_NoDraft` (U1a), `Controller_MoveFourPixels_DraftOpened` (U1a), `Gesture_ReleaseWithoutMove_NoAcceptedRow` (B1b) |
| Committed position ≠ release position | throttle | prevent | trailing flush | — | `Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget` (U1a) |
| Gesture stuck in Busy after a stale completion | async commit | prevent | `finally` exit | — | `Controller_StaleCommitCompletion_ReturnsToIdle` (U1a) |
| Release on crossing edges applies | commit at release | prevent | certificate; refusal | `apply` refused `DSL-EDGES-CROSS` | `Gesture_ReleaseEdgesCross_RefusedGeometryUnchanged` (U1a), `PlanCanvas_ReleaseEdgesCross_PointRenderedAtOriginal` (U1b) |
| Certificate Not assessed at release | strict certificate | mitigate | distinct copy; cancel (deterministic seam: an already-cancelled token) | `apply` refused `DSL-NOT-ASSESSED` | `Gesture_ReleaseNotAssessed_RefusedDistinctCopy` (U1a) |
| Frame pipeline slower than the pointer | per-frame patch + parse + estimates | mitigate + detect | latest-wins; work bounded per frame; p95 measured at readiness | `gesture.end` p95 | `Gesture_ThousandMoves_CoalescedFramesBounded` (U1a) |
| Display sampling spends the proof budget (BUDGET-DISPLAY) | `Planform.View` | prevent | binary64 only, no `ProofBudget` | — | `PlanformView_Sampling_NeverUsesProofBudget` (B0) |
| New gesture or command while Busy | async | prevent | Busy state | status copy | `Gesture_BeginDuringCommit_NoDraftNoBusy` (U1a), `Controller_PointerDownDuringChordCommit_NoDraft` (U1a) |
| Capture lost mid-drag | deactivation | recover | cancel with copy | `gesture.end` capture-lost | `Controller_CaptureLostDuringDrag_Cancelled` (U1a) |
| KeyUp lost | nudge run | recover | commit on FocusLost / Deactivated | `gesture.end` trigger | `Controller_NudgeRunFocusLost_CommitsOneRow` (U1a), `Controller_NudgeRunWindowDeactivated_CommitsOneRow` (U1a) |
| Save/Close during a gesture | shell verbs | prevent | flush and commit first | — | `Controller_SaveDuringDrag_CommitsThenSaves` (U1a) |
| Drag reorders points | 2D moves | prevent | Core clamp; re-parse backstop | frame `Clamped` | `UpdatePointGesture_PastNeighbour_ClampedOrderKept` (B1b), `UpdatePointGesture_GapBelowOneMillimetre_ClampKeepsCurrentGap` (B1b), `UpdatePointGesture_BypassedClamp_DslPatch` (B1b) |
| Smooth/Symmetric broken by a drag or a later write | co-motion, rounding | prevent | quantized Δ, exact positions, 2A − H | `apply` refused `DSL-LOCK` | `UpdatePointGesture_SymmetricAnchorDragAfterRefit_RowHolds` (B1b), `UpdatePointGesture_SmoothHandleDrag_OppositeCollinear` (B1b) |
| SetTangent breaks ordering | handle adjustment | prevent | ordinates only, toward the anchor | — | `SetTangent_SymmetricNextToNeighbour_OrderKept` (B1b) |
| Make Anchor over the ceiling or too close | ceiling 16, grid | prevent | refused naming the reason | `apply` refused `DSL-CURVE` | `MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling` (B1b), `MakeAnchor_HandleGapBelowGrid_Refused` (B1b) |
| Type change leaks beyond neighbouring anchors | constructions | detect | locality measured | report | `MakeAnchor_Locality_OutsideSegmentWithinIdentity` (B1b), `MakeControl_Locality_OutsideSegmentWithinIdentity` (B1b) |
| CAD-15 gap clause infeasible on a collinear point | spec clause | accept | fixture uses a point off the line (ADR-0005 finding) | — | `MakeControl_OffLinePoint_GapAboveIdentity` (B1b) |
| Chord fit above 10 µm | root-flat blend (DR-12 ruled accept-and-report) | detect + report | accepted; warning with the number and the limit; typed end exact | `apply` accepted with `FitAboveLimit`, fit µm | `ApplyDimension_FitJustAboveLimit_AcceptedWithWarning` (B1a), `ApplyDimension_FitJustBelowLimit_Accepted` (B1a) |
| Quarter-chord root edit leaves LE(0) ≠ 0 | DR-2 | prevent | δ from fitted P0 bits | — | `ApplyDimension_RootChordShift_P0EqualsP1BitsZero` (B1a) |
| Chord refit breaks a tangent row | KKT rows | prevent | rows as hard rows | — | `ChordRefit_SmoothAndSymmetricRows_HeldExactly` (B1b) |
| Tip chord typed on a closing tip | CAD-16 | prevent | not an input; Core backstop | — | `Properties_TipCloses_TipChordIsText` (U2), `ApplyDimension_TipChordClosingTip_DslTarget` (B1a) |
| Hostile expression | `LengthExpression` | prevent | bounds; closed names | `DSL-INVALID-NUMERIC` | `LengthExpression_RandomText_NeverThrowsUnexpected` (B1a) |
| Retry after reopen refused (F-5) | memo payload | prevent | one fingerprint | — | `Reopen_RetrySameDimensionOperationId_ReturnsPriorId` (B1a) |
| New receipt kind not reopenable (EDIT-KIND-REOPEN) | receipt expansion | prevent | `EditReference` learns kinds in the same change | — | `Reopen_PointTypeAndTangentRows_UndoRedoRoundTrip` (B1b), `Reopen_ChordRows_UndoRedoRoundTrip` (B1a), `Reopen_TwoDGestureRow_UndoRedoRoundTrip` (B1b) |
| Silent one-way migration of drag-only histories | receipt serialization | prevent | `WhenWritingNull` | — | `Save_DragAndSpanOnlyHistory_NoNewReceiptKeys` (B1b) |
| Forged `curve`/`rule` on a receipt | user-writable file | prevent | closed per-rail sets | `DOC-REFERENCE` | `Reopen_CurveOnGestureReceipt_DocReference` (B1b), `Reopen_ForgedRuleValue_DocReference` (B1a) |
| Old build opens a 4.1 or new-receipt project | one-way expand | accept (stated, D-5) | refuses; file unchanged; codes in §3.8; forward check for later versions | characterization receipt | `Parse_Foil42UnknownBlock_DslVersion` (B0) |
| Rail recovery draft has no UI once the pane is removed | Ruling 55 Q1 | prevent | Plan + alert band | — | `Recovery_RailDraftResumed_PlanShowsDraftApplyCommits` (U2) |
| 16-point rails with three anchors exceed the proof budget | DR-10 | detect | deterministic work bound in the fast ring; wall time at readiness | `apply` duration | `Assess_SixteenPointThreeAnchors_WorkCountBounded` (B0) |
| Focus lands on a point hidden by zoom/pan | canvas focus | prevent | pan into view (2.4.11) | — | `PlanCanvas_FocusOffscreenPoint_PansIntoView` (U1b) |
| Tab trapped in the canvas (F-2 shape) | keyboard model | prevent | Tab always leaves the Plan (to Properties from a selected point; to the next stop otherwise) | — | `Plan_TabFromSelectedPoint_GoesToPropertiesFirstValue`, `PlanCanvas_TabFromNoSelection_EntersFirstTarget_AndLeaves` (DR-NAV-1) |
| Plan starved at the minimum window (NAV-STAR-COLLAPSE) | layout | prevent | Plan `*`, min 320 × 240 at 1024 × 700 | — | `ModelArea_MinimumWindow_PlanAtLeast320x240` (U1b) |
| Hover probe polluted by the OS pointer (GUI-AMBIENT-INPUT) | hover | prevent | park the pointer off-window first | — | `PlanCanvas_HoverProbe_ParksPointerFirst` (U1b) |
| Single-key C fires inside a text field | comb key | prevent | scoped to the focused canvas (2.1.4) | — | `PlanCanvas_CKeyInTipChordField_CombNotToggled` (U1b) |
| Estimates on a crossing draft show nonsense | live estimates | mitigate | non-finite → "—" with the reason | `estimates.compute` not-converged | `WingBlock_CrossingDraft_ShowsDashAndReason` (U2) |

## 10. Telemetry (normal path, no flag)

Per the Observability Standard and architecture §10.5 (apply keeps its event; a new event only for non-apply work). No
names, paths, source text, point ids or positions in any event.

| Event | Emitter | Fields | Answers |
|---|---|---|---|
| `apply` (existing `SessionEvent`) | Core | `edit_kind` ∈ gesture · point-type · tangent-kind · dimension; outcome; code; duration; generation; for chords `FitMicrometres`, `DeviationMicrometres`, `ShiftMicrometres` | how long commits take; which kinds fail; how often typed chords exceed the fit limit and by how much; how often the ceiling bites (`DSL-CURVE`) |
| `gesture.end` (new `ShellEvent` name; the record gains optional `Frames`, `UpdateP95Ms`, `EstimatesP95Ms`, `RenderP95Ms`, `CommitMs`, `EditKind`, and reuses `ClampedCount`, `Trigger`, `Code`) | controller | outcome committed · refused · cancelled · no-change; trigger release · key-up · focus-lost · deactivated · save · escape · capture-lost | is the drag smooth; how often releases are refused and why |
| `estimates.compute` (existing) | Core | basis `preview` during drags | estimate cost per frame |
| `shell.pane.render` (existing) | Plan render failure | outcome error; exception type | a Plan that fails to draw |

Every event carries the trace id and operation id. Tests (attributed in §12.4): `GestureEnd_Committed_EmitsFramesAndP95`,
`ApplyChord_FitAboveLimit_ApplyEventCarriesFitAndWarning`, `Telemetry_PointEdits_NoIdsOrPositions`.

## 11. UI and interaction design

**Medium:** native desktop, macOS (Apple HIG pointer and keyboard; Avalonia 11.3.14); Windows deferred. **Archetype:**
`ParametricWorkbench` (DESIGN.md) — Input PrecisionPointer + KeyboardFirst; Feedback Optimistic + Confirmed (the drag is
optimistic, the release is confirmed by the certificate); Transition HardCut. **Technical UI (TQ):** every quantity has
its unit at 0.01 mm; estimates carry "≈"; a refusal shows the number that caused it; the comb uses one hue.

### 11.1 Plan view layout (key screen)

Focal point: the planform. The M1.2a "Plan + 3D" tab becomes **Plan** (the Plan canvas fills it, min 320 × 240); the
certified isometric sample plot moves unchanged to a **"3D samples"** document tab beside "Section sample". Plan draws,
back to front: viewport fill; grid (`viewport-grid`); centre line (`viewport-mute`, 1 px); planform fill (`foil` at 25 %);
station chord lines (`station`, 1 px, dash 3/3) with chips below the TE; both rails as 2 px `foil` curves over both halves;
the crossing marker when the advisory check fails (`danger-viewport`, 4 px, dash 6/3, "Edges would cross" chip); the comb
when on (`warning-viewport` teeth 0.9 px, envelope 1.2 px, one-sided at anchors, × break marks at corners in
`viewport-ink`, live during a drag); the dashed control polygon (`viewport-mute`, 1 px, dash 3/3); points on the starboard
half only; the handles of the one selected anchor or end; hover ring; focus ring. Uniform scale; span runs left → right,
aft runs down (v10 orientation). Bottom-left: scale bar and "Plan · top". Top-right: the **Tracing probe** (`viewport-ink`
on `viewport-soft`), showing the pointer's reading, or the focused point's when the keyboard is used, and Δ span / Δ aft
during a gesture.

### 11.2 Glyphs, sizes and contrast (measured on the tokens; background is graphite `viewport` #17272c in every theme)

| Element | Shape (CSS px) | Tokens | Contrast on `viewport` |
|---|---|---|---|
| Anchor point | 12 px square, 1.5 px stroke, hollow | stroke `foil` | 8.18:1 |
| Control point | 11 px filled circle | fill `foil` | 8.18:1 |
| Root end / Tip end | 14 px diamond, hollow | stroke `foil` | 8.18:1 |
| Handle | 9 px hollow circle + 1 px line to its point | `focus-ring-viewport` | 9.35:1 |
| Selected anchor / end / handle | the same glyph **filled** `station` (interior viewport → station: 9.35:1) | `station` | 9.35:1 |
| Selected control point | becomes a **hollow** 11 px circle with a 2 px `station` stroke and a 4 px `station` centre dot (interior changes from `foil` to `viewport`: 8.18:1) | `station` | 9.35:1 |
| Hover | ring r 10, 1.5 px | `viewport-mute` | 8.60:1 |
| Keyboard focus | ring r 13, 3 px, 1 px `viewport` gap inside | `focus-ring-viewport` | 9.35:1 |
| Locked or coupled | dashed ring r 11, 1.5 px, dash 2/2, on both coupled points | **`warning-viewport` #efc576 (new token)** | 9.48:1 (`warning` #895900 measures 2.56:1 on graphite and fails 1.4.11) |
| Crossing marker | 4 px dashed stroke | `danger-viewport` | 8.78:1 |
| Hit target | circle r 14 (28 px); nearest centre wins; the selected point wins ties | — | ≥ 24 × 24 (2.5.8) |

Selection is carried by **fill and shape**, never colour alone (`station` vs `foil` is only 1.14:1; 1.4.1). High-contrast
theme on the same graphite viewport: strokes `contrast-ink` #ffffff (15.41:1), selection fill and focus ring
`contrast-primary` #ffee58 (12.93:1), lock ring dashed `contrast-primary`, crossing marker dashed `contrast-primary` 4 px.
The viewport stays graphite in the light theme (DESIGN.md; v10's light canvas is not adopted, D-4).

### 11.3 Interaction and keyboard map (every pointer verb has a keyboard path)

| Verb | Pointer | Keyboard |
|---|---|---|
| Select a point | click | ] / [ (next / previous point; focus selects while at most one point is selected); Browser rail row + Return |
| Add to / remove from selection | Shift+click (< 3 px) / ⌘+click | Space selects the focused point only; Shift+Space toggles it; with several selected, ] / [ move focus without changing the selection |
| Clear selection | click empty canvas | Escape (first dismisses an open tooltip) |
| Move a point | drag; Shift-drag from a point locks to the span or aft axis | ←→ span, ↑↓ aft (screen up = forward): ⌘ 0.01 · plain 0.1 · Shift 1 mm; or type Span/Aft (expressions accepted, echoed in mm) |
| Select a handle | click it | ] from its anchor (handles follow their anchor) |
| Move a handle | drag (co-motion by kind) | arrows as for a point (co-motion by kind); typed angle (from the span axis, positive aft, 0.01 °) and length (mm) in Properties |
| Back from handle to point | click the point | Escape |
| Type a precise value | double-click a point → its Span field; double-click a handle → its angle field | Return on a point → Span field; Return on a handle → angle field; Escape in the field → back to the canvas target |
| Cancel a drag or nudge run | Escape while dragging | Escape before releasing the arrow |
| Point type / tangent | context menu (right-click, Control-click; spec B7 per-OS table) | Properties Type and Tangent controls; Edit menu; command palette |
| Zoom | wheel, pinch; about the pointer | ⌘= / ⌘− about the focused point or the centre |
| Pan | two-finger trackpad scroll (DR-13, Ruling 56); middle-drag; Shift-drag on empty canvas | ⌥+arrows (10 % of the view) |
| Fit | View ▸ Fit | ⌘0 |
| Comb | View ▸ Curvature comb | C with the Plan focused (scoped, 2.1.4) |
| Select station | click chip | ] / [ to chip; Browser row |
| Tracing probe | hover | follows the focused point |

Target order inside the Plan: LE points root → tip, TE points root → tip, then station chips; the selected anchor's handles
follow it. **DR-NAV-1:** ] and [ (`Key.OemCloseBrackets` / `Key.OemOpenBrackets`, the same keys on macOS and Windows layouts)
move to the next and previous target and stop at the ends. Tab does not walk targets: with a point selected, Tab leaves the
Plan and lands on that point's first Properties value (Type), and Shift+Tab from that value returns to the Plan on the
selected point; Tab into the Plan focuses the selected point, or the first target when none is selected. With nothing
selected, Tab from the Plan moves to the next stop in the window. Escape, Return, arrows and ⌥+arrows are unchanged. F6
cycles regions. No keyboard trap (2.1.2): Tab always leaves. Dragging has
single-pointer and keyboard alternatives (2.5.7). A focused point is panned into view (2.4.11), where "in view" excludes
the probe, the scale bar and the station chips as obscuring rectangles.

### 11.4 Properties, Wing block and copy

Point selected — heading "Trailing edge · point 5 of 10" (named: "Trailing edge · root end"; handle: "Trailing edge ·
point 5 · handle toward the tip").

| Group | Row | Control | States |
|---|---|---|---|
| Position | Type | select: Anchor point / Control point; named points and handles read-only text with the constraint (§3.5) | disabled with reason when the foil is not certified |
| Position | Span · Aft | length inputs (mm, 0.01; expressions echoed) | read-only per freedom, with the reason |
| Tangent (Anchor) | Kind | segmented Smooth · Symmetric · Corner, each with its helper | — |
| Handles (Anchor or end) | To root · To tip | angle (° from the span axis, positive aft, 0.01) and length (mm, 0.01) | Symmetric shows one length ("both handles"); a root-mirror handle shows length only ("Square to the centre line") |
| Helper | — | Control: "A control point pulls the curve toward it. The curve does not pass through it." Anchor: "An anchor point is on the curve. Its handles set the curve's direction on each side." | — |

Several points — heading "3 points"; shared values, "Mixed" where they differ, read-only with "Select one point to change
it." (OI-3). **Wing block (CAD-17, completed):** Span, Root chord, Tip chord (inputs, mm), then ≈ Mean chord, ≈ MAC,
≈ Max t/c, ≈ Aspect ratio, ≈ Area, and a "How these are measured" disclosure with the A4.15 table. During a gesture the
estimates read the draft and the block header shows "≈ preview". Lengths display at 0.01 mm. Browser gains **Leading edge**
and **Trailing edge** groups listing the rail points (the 2.5.8 equivalent path for closely spaced points and the list a
screen reader can scan).

**Load-bearing copy (new rows for the spec owner, F-4; existing rows quoted):**

| Situation | String |
|---|---|
| Hover / point name | "Trailing edge, point 5 of 10, control point, span 180.00 mm, aft 118.52 mm" |
| Handle name | "Trailing edge, point 5, handle toward the tip, angle 12.40°, length 18.20 mm" |
| Gesture refused, edges cross | "That would make the leading and trailing edges cross. The point is back where it was." |
| Gesture refused, not assessed | "This change couldn't be checked, so it wasn't applied. Nothing changed." |
| Drag cancelled (Escape or capture lost) | "Drag cancelled. The point is back where it was." |
| Locked point | "The leading edge root is fixed at the root. It can't be moved." / "The root end and its handle move together (root mirror)." |
| Not certified | "Points can't be moved because this foil couldn't be checked. Nothing changed." |
| Ceiling | "Making this an anchor needs 3 more points. This rail has 14 of 16." |
| Committed move | "Moved trailing edge point 5 by 2.14 mm. MAC 101.30 mm." |
| Type change | "Trailing edge point 5 is now an anchor point. The curve passes through it. Largest change 0.84 mm." |
| Tangent change | "Tangent is now symmetric. Lined up the handle toward the tip." |
| Chord accepted | "Root chord 152.09 mm. Fit 9.01 µm (limit 10 µm). 4.21 mm from a straight taper. Planform moved 6.34 mm so the leading edge stays at the root." |
| Chord accepted above the fit limit (warning; new COPY row proposal, F-4) | "Root chord 190.00 mm. Fit 22.53 µm (limit 10 µm) — above the limit. 10.54 mm from a straight taper. Planform moved 15.84 mm so the leading edge stays at the root." Shown under the field and in the status line with the chrome `warning` style and a warning icon (not colour alone); announced politely like any commit report |
| Chord refused (existing) | COPY-106, COPY-107, COPY-118 as written; COPY-108 "Tip closes — edit the tip station" |
| Busy | "Checking the last change…" |
| Recovered draft | "A recovered edit to Trailing edge point 4 is open." Apply · Discard |
| Plan render failure | "Plan view couldn't be drawn. Your foil hasn't changed." Try again |

### 11.5 Component states

| Component | default | hover / focus | active | disabled | loading | empty | error | success | overflow |
|---|---|---|---|---|---|---|---|---|---|
| Plan view | planform, points, stations, probe | hover ring, tooltip, probe; focus ring | drag: curve follows, Δ in probe, crossing marker when advisory fails | dimmed points, not-certified banner | model area "Opening…" (app-shell) | no foil → Start card | render failure copy + Try again | status-line report | zoom: focus pans into view; colliding chips → alternate chips hidden, still in Tab order and in Browser |
| Point glyph | by role | ring | filled + handles | dim, no hover | — | — | refusal redraws it | — | overlapping points: the selected one paints on top |
| Properties point rows | values | field ring | editing | read-only with reason | — | "Select a point or a station to see its properties here." | field error + unchanged value | echo | long names wrap |
| Wing Root/Tip | value | ring | editing | read-only in section editor ("Set these in the workspace.") | "≈ preview" during a gesture | "—" when not finite | COPY rows / fit refusal | report under the field | numbers never truncate |
| Context menu | items | row highlight | — | item disabled with reason | — | — | — | — | — |

**Motion:** none; state changes are instant (HardCut). Nothing to reduce under reduced motion.

### 11.6 Accessibility (WCAG 2.2 AA) and performance

- **Semantics:** the Plan is a `Group` named "Plan view. ] to a point; arrows move it 0.1 mm, with Command 0.01, with
  Shift 1. Return types a value. Escape clears." Each point and handle has a **custom peer** (`PlanPointPeer`) with its
  bounding rectangle, keyboard focus, a focus-changed event, the selection state and **Invoke** (runs the Return action),
  control type **Button**, name as in §11.4 starting with the stable identity and refreshed on commit; "locked, root
  mirror" is in the name if the macOS bridge drops `ItemStatus`. **Deviation D-1 (accepted by the UX & Accessibility
  lens with conditions):** CAD-04 and the DESIGN.md v5 row ask for `role=slider`; a 2D point has no one-axis slider
  equivalent, and VoiceOver's increment would move the wrong axis. The conditions are the peer features above, an AX dump
  in U3, and a spec-owner amendment of CAD-04's slider clause (F-7). The Tracing probe, the Δ readout and the scale bar are
  **not** live regions (announcing every frame would break the v10 no-per-nudge rule); their values are reachable on demand
  through the focused peer's name and Properties.
- **Announcements:** the status line is a polite live region carrying the commit report; values are not announced per
  nudge (v10 rule); refusals and the lock copy on a nudge are assertive.
- **Contrast:** §11.2; every glyph ≥ 3:1 (8.18–9.48:1 standard, 12.93–15.41:1 high contrast).
- **Targets:** 28 px hit circles; where two points are closer than 28 px the 2.5.8 **equivalent** is the Browser rail row.
- **Performance budget:** drag frame (update + estimates + render) target 16 ms, ceiling 100 ms p95 (CAD-03); commit p95
  ≤ 250 ms on New foil, hard ceiling the 1 s proof budget; Plan render ≤ 8 ms at 1440 × 900. All **Inferred** until
  `gesture.end` measures them; wall-clock checks run at readiness (§12.3), not in the fast ring.

### 11.7 Measured against v10

| v10 element (`workbench-v10.html`) | M1.2b | Note |
|---|---|---|
| Anchor square, control filled circle, end diamond, handle circle + line (`:672-677`) | same shapes | 12/11/14/9 px |
| Selected = filled accent; focus ring 2.5 px; hover ring (`:52`, `:150-152`) | same; focus 3 px; selected control point hollow + dot | DESIGN.md ≥ 3 px; 1.4.11 |
| Handles only for a single selected anchor (`:675`) | same, plus ends | ends have handles in the record |
| Shift-click extends (`:674`) | same, plus ⌘-click, Space/Shift+Space | UX F2 |
| Nudge 0.01 / 0.1 / 1 mm (`:683`) | same | A4.8 |
| Handle ←→ rotates 1° / 5° (`:685`) | arrows move handles on screen axes; typed angle at 0.01 ° | B7 arrows nudge; A4.8 angle ladder on the field |
| Escape on handle → point; on point → clear (`:681-682`) | same, tooltip first | 1.4.13 |
| Drag commits via an undo snapshot (`:370`, `:687`) | one accepted row at release, certificate-gated | ADR-0007 |
| Control polygon dashed (`:664`); comb and corner × (`:651-657`) | same, comb one-sided at anchors | — |
| Station chips select; Enter edits section (`:666-668`) | select only | Edit section is M1.2c |
| Points `role=button` with values in the label (`:672`) | custom peers with bounds, focus, Invoke (D-1) | UX F1 |
| 3D view beside the Plan | not in M1.2b (OI-1) | the sample plot is a separate tab |
| Light-theme light canvas | graphite kept | D-4 |
| Segment degree rises with control points (`:288-290`) | not adopted | ADR-0005 |

### 11.8 Design language

`DESIGN.md` gains the `warning-viewport` token, a **Point (v10, M1.2b)** row that supersedes the v5 "Control vertex" row
on the Plan view (the v5 row is marked superseded; its `role=slider` and `{colors.warning}` ring are corrected there), and
the measured `danger-viewport` figure (8.78:1, previously stated 7.4:1). `design-lint.py` is clean.

## 12. Test plan

### 12.1 Triggered directives

| Trigger | Where | Directive and how it is met |
|---|---|---|
| — | all | **D0**: `Method_State_Outcome`, AAA, no sleeps (bounded dispatcher pumps), no wall clock in the fast ring, no culture; fixtures from bytes |
| T1 | roles and freedoms, motion rules, commands, chord mapping, `PropertiesView`, gesture table | **D1** with exact values (spike numbers as named constants citing their source); **boundary fixtures for every guard** — fit at 9.9 and 10.1 µm (from the measured 45.05 µm per unit of f − 1), MakeAnchor at 13 → 16 accepted and 14 → 17 refused, drag at 2 px and 4 px — so the mutants "limit 20 µm", "ceiling 15" and "threshold 0" each turn a named test red; Stryker not wired → mutation confidence **Inferred**, hand-mutant runs recorded in the proof pack |
| T2 | FoilDSL 4.1 parser, `LengthExpression`, motion rules | **D2** with a fixed-seed generator: 4.1 parse ∘ print round trip; `LengthExpression` never throws other than its two codes on random text ≤ 512 chars; random drag targets keep strict ordering, Smooth collinearity and Symmetric midpoint; failing seeds become named regressions |
| T3 | `PointModel.cs` in Core; `PlanCanvas` in Desktop | **D3**: `Architecture_DockConfinedToShell` stays green (no Avalonia in Core) |
| T4 | save/reopen through `ProjectStore` | **D4**: real temp files, every new receipt kind and 4.1 sources |
| T7 | native envelope receipt; 4.1 source; CLI `inspect --json` | **D6**: synthetic golden fixtures (labelled synthetic) plus golden M1.2a recovery bytes; characterization receipt of the old build (§3.8); forged values refused |
| T8 | none new | D7 not triggered (no new substitute; the screenshot fallback is a real window) |

No AI triggers.

### 12.2 Harness change (Coordinator, before dispatch)

`tools/check-named-tests.py` gains `--design <path>` (default `docs/design/app-shell.md`), so this design's §9 and §12.4
are the single authority for M1.2b names; its `--self-test` gains a planted unattributed name in a second design. The
Coordinator lands it before dispatch and gates it on `--self-test` exit 0, with the planted-name case observed red before the flag and green after, and
the extraction run on this design before any track is dispatched (a Python self-test prints no PASS line into
`.tmp-tests`, so it is not a named test; Test Architect F7, Simplifier F9). A Desktop suite `--plan-canvas` joins the
spawned children and prints `PASS <name>`. Names in §9 and §12.4 are written `` `Name` (TRACK) ``; the extraction was run
on this document at the gate (Gate record).

### 12.3 Tiers and the M1.2a lessons

1. **Core** (`CfdWorkbench.Core.Tests`, `CfdWorkbench.Cli.Tests`) — B0, B1a, B1b.
2. **Controller** (`--controller-shell`) — U1a: the §6.2 table driven with synthetic input, no window; one row per cell.
3. **Rendered** (`--plan-canvas`, class **UI-RENDERED-STATE**) — U1b: render the **whole realized window**, find each
   point's position through `TranslatePoint` from the canvas to the window (not the canvas's own mapping), and read
   pixels there: glyph interior, ring radius, handle ends, the rail polyline against the draft's samples, and the
   orientation. Walk the realized tree (the Plan tab hosts a `PlanCanvas` with `IsEffectivelyVisible`, bounds ≥ 320 × 240,
   not covered by another visual at the sampled points). State-only assertions do not count as proof of a visible
   behaviour.
4. **Action** (class **UI-DEAD-CONTROL**) — U2: every new visible enabled control has an action and an effect test.
5. **Native** (packaged `.app`, class **CO-UI-READY**) — rows N-B1 … N-B12 (the §0.1 steps) in `docs/reviews/m12b-native.md`.
   **Before any operator session every row carries an agent attach receipt** accepted by `tools/check-review-attach.mjs`
   (step 1 with a screenshot of the Plan showing both rails and points). If the attach is blocked (review F-ATTACH), U3
   stops and reports; "operator-run, not done" is not an exit. A VoiceOver trace of ] and [ across points and an AX dump of
   the point peers are part of N-B10.
6. **Token control (CD8)** — `ui-craft-gate.py` reads web source, not AXAML; the rung-2 control is `xaml-token-lint`
   (fast ring) plus `PlanCanvas_Brushes_AllFromThemeResources`.
7. **Readiness ring** (not in the fast ring; TEST-RING) — wall-clock checks, each written by the track that owns the
   code and excluded from `run-tests.sh`, and gathered into the readiness receipt of `tools/run-readiness.py` by U3:
   `Readiness_SixteenPointThreeAnchors_AssessUnderProofBudget` (written by B0; macOS; Windows at its qualification),
   `Readiness_NewFoilDrag_FrameP95Under100Ms` and `Readiness_NewFoilCommit_P95Under250Ms` (U1a),
   `Readiness_PlanRender_Under8Ms` (U1b). The fast ring asserts deterministic work counts instead (Test Architect F3).
8. **Budget** — `tools/run-tests.sh` stays under its 60 s budget (TEST-COST). Estimated addition ≈ 6–10 s (Inferred:
   ~150 Core cases at the current ~0.1 s mean, one extra spawned window suite); a track that goes over reports the
   measured seconds, and the Coordinator moves the slowest rendered cases to readiness rather than raising the budget.
9. **Delegation hygiene** — foreground only; a track is done only with its Return section and the claimed commit on the
   branch (HARNESS-SILENT-EXIT); two repair cycles, then stop (COORD-SPIRAL).

### 12.4 Named tests (the ledger; the checker reads this section and §9)

**B0 — Core grammar, model, patches, CLI.**
`Parse_Foil41WithTangents_RowsParsed` (B0) · `Parse_TangentsUnder40_DslSyntax` (B0) · `Parse_TangentRowOnControlPoint_DslLock` (B0) ·
`Parse_AngleKindOnChannel_DslLock` (B0) · `Parse_ElevenChannelPointsUnder40_DslCurve` (B0) · `Parse_SixteenChannelPointsUnder41_Parsed` (B0) ·
`Parse_SeventeenChannelPointsUnder41_DslCurve` (B0) · `Parse_Foil42UnknownBlock_DslVersion` (B0) · `Parse_Foil41RoundTrip_RandomRowsStable` (B0) ·
`Identity_TangentsRows_DefinitionHashUnchanged` (B0) · `Identity_Header41RewriteSameGeometry_DefinitionHashUnchanged` (B0) ·
`Assess_SmoothRowSatisfied_Certified` (B0) · `Assess_SmoothRowOffByHalfDegree_Invalid` (B0) · `Assess_SymmetricRowNotMidpoint_Invalid` (B0) ·
`Assess_SixteenPointThreeAnchors_WorkCountBounded` (B0) · `PointModel_DefaultExample_RolesEndsHandlesControls` (B0) ·
`PointModel_AnchorAtMultiplicityThree_HandlesAssigned` (B0) · `PointModel_RootMirror_TeRootAftOnlyLeRootFixed` (B0) ·
`PointModel_MultiplicityTwoKnot_ControlPointsAndC1Marker` (B0) · `PlanformView_Sampling_NeverUsesProofBudget` (B0) ·
`PlanformView_SplineBasisVsBernstein_Within1e12` (B0) · `Planform_Probe_ChordAndRailsAtEta` (B0) · `Planform_HandleTarget_AngleFromSpanAxis` (B0) ·
`Comb_Anchor_TwoOneSidedTeeth` (B0) · `Comb_CornerAnchor_BreakReported` (B0) · `Comb_SixteenPointRail_FairnessFixture` (B0) ·
`EnsureHeader41_FirstRow_HeaderRewritten` (B0) · `EnsureHeader41_Already41_Unchanged` (B0) · `Cli_InspectJson_PointsRolesAndKinds` (B0).

**B1a — Core chords, expressions, memo fingerprint.**
`ApplyChord_NewFoilRootX12_AcceptedBothNumbers` (B1a) · `ApplyDimension_FitJustBelowLimit_Accepted` (B1a) · `ApplyDimension_FitJustAboveLimit_AcceptedWithWarning` (B1a) ·
`ApplyDimension_NewFoilRootX15_AcceptedWithFitWarning` (B1a) · `ApplyDimension_RootChordShift_P0EqualsP1BitsZero` (B1a) ·
`ApplyDimension_TipChord_NoShiftRootChordUnchanged` (B1a) · `ApplyDimension_LocksOff_LinearRuleExact` (B1a) · `ApplyDimension_OneRailLocked_ResidualReported` (B1a) ·
`ApplyDimension_Cad17Taper_ChordRuleWithinTolerance` (B1a) · `ApplyDimension_Refused_HistoryUnchanged` (B1a) ·
`ApplyDimension_TipChordClosingTip_DslTarget` (B1a) · `ApplyDimension_EdgesWouldCross_DslEdgesCross` (B1a) · `ChordRefit_SyntheticLinearRows_HeldExactly` (B1a) ·
`ApplyDimension_Receipt_CarriesRuleId` (B1a) · `BlendRule_LockStateToRuleId_Pinned` (B1a) · `ApplyChord_FitAboveLimit_ApplyEventCarriesFitAndWarning` (B1a) ·
`Reopen_ChordRows_UndoRedoRoundTrip` (B1a) · `Reopen_ForgedRuleValue_DocReference` (B1a) · `Reopen_ChordRowWithoutRule_DocReference` (B1a) · `Reopen_RetrySameDimensionOperationId_ReturnsPriorId` (B1a) ·
`LengthExpression_UnitsAndReferences_ResolvedToMetres` (B1a) · `LengthExpression_NonLength_DslUnit` (B1a) ·
`LengthExpression_RandomText_NeverThrowsUnexpected` (B1a) · `LengthExpression_SubMicrometreInput_RoundedToMicrometre` (B1a).

**B1b — Core gestures, point commands, receipts, recovery.**
`BeginPointGesture_FixedPoint_DslLock` (B1b) · `UpdatePointGesture_ControlPoint_QuantizedDeltaExactPosition` (B1b) ·
`UpdatePointGesture_Anchor_HandlesMoveBySameDelta` (B1b) · `UpdatePointGesture_SmoothHandleDrag_OppositeCollinear` (B1b) ·
`UpdatePointGesture_SymmetricHandleDrag_Midpoint` (B1b) · `UpdatePointGesture_SymmetricAnchorDragAfterRefit_RowHolds` (B1b) ·
`UpdatePointGesture_TeRootEnd_HandleFollowsRootMirror` (B1b) · `UpdatePointGesture_PastNeighbour_ClampedOrderKept` (B1b) ·
`UpdatePointGesture_GapBelowOneMillimetre_ClampKeepsCurrentGap` (B1b) · `UpdatePointGesture_RandomTargets_OrderAndRowsHold` (B1b) ·
`UpdatePointGesture_StaleGeneration_DslConflict` (B1b) · `UpdatePointGesture_BypassedClamp_DslPatch` (B1b) ·
`Gesture_DragManyFrames_OneAcceptedRow` (B1b) · `Gesture_ReleaseWithoutMove_NoAcceptedRow` (B1b) ·
`MakeAnchor_ControlPoint_PassesThroughWithinIdentity` (B1b) · `MakeAnchor_Locality_OutsideSegmentWithinIdentity` (B1b) ·
`MakeAnchor_NewFoil_TenToThirteenHeader41SmoothRow` (B1b) · `MakeAnchor_ThirteenPoints_SixteenAccepted` (B1b) ·
`MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling` (B1b) · `MakeAnchor_HandleGapBelowGrid_Refused` (B1b) · `MakeAnchor_RootEnd_DslLock` (B1b) ·
`MakeControl_OffLinePoint_GapAboveIdentity` (B1b) · `MakeControl_Locality_OutsideSegmentWithinIdentity` (B1b) · `MakeControl_Undo_RestoresExactly` (B1b) ·
`MakeControl_LastRowRemoved_HeaderStays41` (B1b) · `SetTangent_HandleSelected_SelectionKept` (B1b) · `SetTangent_SmoothNoHandleSelected_BothOnBisector` (B1b) ·
`SetTangent_SymmetricNextToNeighbour_OrderKept` (B1b) · `SetTangent_Corner_RowRemoved` (B1b) · `ChordRefit_SmoothAndSymmetricRows_HeldExactly` (B1b) ·
`ApplyPointCommand_Refused_HistoryUnchanged` (B1b) · `ApplyPointCommand_SameOperationTwice_OneRow` (B1b) · `ApplyPointCommand_SameOperationDifferentKind_DocOperationConflict` (B1b) ·
`Reopen_SameOperationDifferentKind_DocOperationConflict` (B1b) ·
`History_ReapplyOperationDifferentPayload_Refused` (B1b) · `Reopen_RetrySamePointOperationId_ReturnsPriorId` (B1b) ·
`Reopen_PointTypeAndTangentRows_UndoRedoRoundTrip` (B1b) · `Reopen_TwoDGestureRow_UndoRedoRoundTrip` (B1b) ·
`Reopen_CurveOnGestureReceipt_DocReference` (B1b) · `Save_DragAndSpanOnlyHistory_NoNewReceiptKeys` (B1b) ·
`Recovery_PointTypeRail_RefusedByRecoveryReference` (B1b) · `Recovery_M12aGoldenRailDraft_ApplyOneRowNoCurve` (B1b) ·
`Recovery_M12aGoldenRailDraft_DiscardClears` (B1b).

**U1a — controller.**
`GestureStateTable_EveryCell_TransitionOrIgnored` (U1a) · `Controller_MoveTwoPixels_NoDraft` (U1a) · `Controller_MoveFourPixels_DraftOpened` (U1a) ·
`Controller_DragPoint_OneUndoStepUndoExact` (U1a) · `Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget` (U1a) ·
`Controller_NudgeLadder_CommandPlainShift` (U1a) · `Controller_NudgeRunHeld_OneRowOnKeyUp` (U1a) · `Controller_NudgeRunFocusLost_CommitsOneRow` (U1a) ·
`Controller_NudgeRunWindowDeactivated_CommitsOneRow` (U1a) · `Controller_CaptureLostDuringDrag_Cancelled` (U1a) ·
`Controller_EscapeDuringDrag_NoRowGeometryBack` (U1a) · `Controller_SaveDuringDrag_CommitsThenSaves` (U1a) ·
`Controller_StaleCommitCompletion_ReturnsToIdle` (U1a) · `Controller_PointerDownDuringChordCommit_NoDraft` (U1a) ·
`Gesture_ReleaseEdgesCross_RefusedGeometryUnchanged` (U1a) · `Gesture_ReleaseNotAssessed_RefusedDistinctCopy` (U1a) ·
`Gesture_BeginDuringCommit_NoDraftNoBusy` (U1a) · `Gesture_ThousandMoves_CoalescedFramesBounded` (U1a) ·
`Controller_DuringDrag_EstimatesFromDraftGeneration` (U1a) · `Controller_UndoDisabledDuringGesture_EnabledAfter` (U1a) ·
`Controller_Reconcile_MakeControlDropsHandleSelection` (U1a) · `GestureEnd_Committed_EmitsFramesAndP95` (U1a) ·
`Telemetry_PointEdits_NoIdsOrPositions` (U1a).

**U1b — Plan canvas (rendered).**
`PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` (U1b) · `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints` (U1b) ·
`PlanCanvas_Orientation_SpanRightAftDown` (U1b) · `PlanCanvas_ExampleOpen_RendersBothRailsAndEveryPoint` (U1b) ·
`PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn` (U1b) · `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne` (U1b) ·
`PlanCanvas_FocusRing_RenderedPixelsAtLeastThreeToOne` (U1b) · `PlanCanvas_HighContrast_RenderedRingContrastPrimary` (U1b) ·
`PlanCanvas_DragFrame_RenderedCurveThroughDraftSample` (U1b) · `PlanCanvas_ReleaseEdgesCross_PointRenderedAtOriginal` (U1b) ·
`PlanCanvas_AdvisoryCrossingClear_CertificateStillDecides` (U1b) · `PlanCanvas_AfterDockReattach_RendersSameScene` (U1b) ·
`PlanCanvas_RenderThrows_CopyTryAgainAndEvent` (U1b) · `PlanCanvas_HitTest_NearestWithin14Px` (U1b) ·
`PlanCanvas_HoverPoint_TooltipCopyAndRing` (U1b) · `PlanCanvas_HoverRail_TracingProbeReadout` (U1b) · `PlanCanvas_DragDeltaReadout_Live` (U1b) ·
`PlanCanvas_ShiftDragFromPoint_OrthoLocked` (U1b) · `PlanCanvas_ShiftClickAndCommandClick_ExtendAndToggle` (U1b) ·
`PlanCanvas_SpaceAndShiftSpace_SelectAndToggle` (U1b) · `PlanCanvas_BracketWithMultiSelection_KeepsSelection` (U1b) ·
`PlanCanvas_Escape_DismissTooltipThenClearSelection` (U1b) · `PlanCanvas_EscapeOnHandle_FocusBackToPoint` (U1b) ·
`PlanCanvas_ArrowOnHandle_MovesHandleWithCoMotion` (U1b) · `PlanCanvas_ClickStationChip_SelectsStation` (U1b) · `PlanCanvas_DoubleClickPoint_RaisesTypeValueRequest` (U1b) ·
`PlanCanvas_CKeyInTipChordField_CombNotToggled` (U1b) · `PlanCanvas_CombToggle_RenderedTeethOnSelectedRail` (U1b) ·
`PlanCanvas_LockedNudge_AssertiveLockCopy` (U1b) · `PlanCanvas_NotCertifiedFoil_PointsDimmedBannerNoDraft` (U1b) ·
`PlanCanvas_CollidingChips_AlternateHiddenStillInBrowser` (U1b) · `PlanCanvas_TargetOrder_LeadingThenTrailingThenChips` (U1b) ·
`Plan_BracketKeys_MoveBetweenPoints_InTargetOrder` · `Plan_TabFromSelectedPoint_GoesToPropertiesFirstValue` · `Properties_ShiftTabFromFirstValue_ReturnsToSelectedPoint` · `PlanCanvas_TabFromNoSelection_EntersFirstTarget_AndLeaves` (DR-NAV-1) · `PlanCanvas_FocusOffscreenPoint_PansIntoView` (U1b) · `PlanCanvas_FocusUnderProbeOrChip_PansIntoView` (U1b) · `PlanCanvas_ProbeAndDelta_NotLiveRegions` (U1b) · `PlanCanvas_HoverProbe_ParksPointerFirst` (U1b) ·
`PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` (U1b) · `PlanCanvas_AutomationPeers_HandleNamesCarryAngleAndLength` (U1b) ·
`PlanCanvas_Brushes_AllFromThemeResources` (U1b) · `PlanCanvas_ZoomPanFit_KeyboardAndPointerSameCamera` (U1b) ·
`ModelArea_MinimumWindow_PlanAtLeast320x240` (U1b) · `ModelArea_SamplesTab_IsometricMovedUnchanged` (retired with the 3D samples tab in M1.2b2 PRE; superseded by `ModelArea_SectionSampleTab_PlotDrawnAtMinimumWidth`) (U1b).

**U2 — Properties, Wing, Browser, menus, retirement.**
`Properties_ControlPoint_TypeSpanAftRows` (U2) · `Properties_NamedPoint_TypeReadOnlyWithConstraint` (U2) ·
`Properties_TypeToAnchor_OneUndoStepCurvePassesThrough` (U2) · `Properties_TangentSymmetric_OneUndoStep` (U2) ·
`Properties_TypedSpanAftExpression_CommitsAsOneGestureEchoed` (U2) · `Properties_HandleAngleLength_TypedCommitsOneStep` (U2) ·
`Properties_MultiplePoints_MixedReadOnly` (U2) · `Properties_TipCloses_TipChordIsText` (U2) · `WingBlock_AllCad17Rows_InOrderWithApprox` (U2) ·
`WingBlock_DuringDrag_RenderedMacTextChangesBeforeRelease` (U2) · `WingBlock_CrossingDraft_ShowsDashAndReason` (U2) ·
`WingRootChord_FitAboveLimit_WarningShownAndCommitted` (U2) · `WingTipChord_CentimetresAndReference_EchoedMm` (U2) ·
`Focus_RootChordCommitTab_NextField` (U2) · `Focus_TypeChange_StaysOnTypeControl` (U2) · `Focus_ReturnOnPoint_SpanFieldEscapeBack` (U2) · `Focus_TypeValueRequest_SpanFieldFocused` (U2) ·
`StatusLine_CommitReport_PoliteLiveRegion` (U2) · `Copy_M12bOutcomes_ExactStrings` (U2) ·
`Browser_RailGroups_SelectPointOnCanvas` (U2) · `ContextMenu_MakeAnchor_SameEffectAsProperties` (U2) ·
`CommandTable_PointRows_ExecuteOrDisabled` (U2) · `UI_DEAD_CONTROL_PointAndWingControlsHaveActions` (U2) ·
`Recovery_RailDraftResumed_PlanShowsDraftApplyCommits` (U2) · `RailEditorPane_Removed_NoReferencesRemain` (U2).

**U3 — UX review and polish (Claude).** No new test names. U3 runs the rendered, action and contrast rows, the v10
comparison (§11.7), the VoiceOver and AX-dump pass, and the native attach receipts (§12.3 tier 5). It sends findings back
to U1b or U2, or fixes the tokens and styles it owns.

## 13. Decision requests, findings and open items

| ID | Question / finding | Default designed to | If overturned |
|---|---|---|---|
| **DR-12 — ruled (Ruling 56)** | Under DR-9's root-flat blend, is a fit residual above 10 µm refused or reported and accepted? | **Accept and report** as a warning with the number and the limit; the typed end stays exact | — |
| **DR-13 — ruled (Ruling 56)** | On the Mac trackpad, does two-finger scroll zoom or pan? | **Pan** (as Fusion 360); pinch zooms; the wheel zooms about the pointer | — |
| F-1 | Wing block missing Mean chord, MAC, Max t/c and "≈" (CAD-17) at `10f0628` | fixed in U2 | — |
| F-2 | `SectionCanvas` Tab never leaves the control (2.1.2) and its peers have no bounds or focus | M1.2c's design replaces the section canvas | — |
| F-3 (spec owner) | CAD-16's "the held line (DR-2 default: the leading-edge rail's record) is unchanged" is stale after Ruling 53; read "the quarter-chord line is unchanged up to the reported rigid shift" | — | — |
| F-4 (spec owner) | New COPY rows in §11.4 need numbers | — | — |
| **F-5 (as-built defect, new class REPLAY-MEMO)** | Reopen rebuilds the operation memo with a different payload from commit (`AuthoringSession.cs`:674 vs :802-810), so a retried Span commit after reopen is refused | fixed in B1a with one fingerprint; register entry in `defect-classes.md` | — |
| F-6 | `ApplySpan` repeats Core refusal rules in the Desktop (`WorkbenchController.cs`:217-219) | U1a moves Span to the same async path and removes the copies | — |
| F-8 (spec owner) | A4.6 says a path above its acceptance disables Apply; Ruling 56 accepts typed chords above 10 µm with a warning — A4.6 needs a typed-chord exception | — | — |
| F-7 (spec owner) | CAD-04's `role=slider` clause cannot hold for a 2D point (D-1) | amend CAD-04 | — |
| OI-1 — placed (Ruling 56) | A 3D view beside the Plan; Front/Side/Starboard elevations; dihedral/twist/thickness editing | new slice **M1.2b2**, right after M1.2b and before M1.2c | its design pass starts with one placement rule shared with the certificate (a display copy would be a second geometry definition) |
| OI-2 | Insert / Delete / Fair / Rebuild on rails; shape-preserving "insert anchor on the curve" | not in M1.2 | a later slice; ADR-0005 row rules already stated |
| OI-3 | Group move and typed value for several points (F11 edge M) | Mixed read-only | a multi-vertex draft; spec-owner note that edge M's typed clause is unbuilt |
| OI-4 | Rhino navigation preset, trackpad mode setting | Workbench preset only | a preference and a mapping table |
| OI-5 | Comb scale/density controls, monotone-piece count, curvature readout (A4.9) | not in M1.2b | a later slice |

## 14. Build tracks (exclusive file ownership)

Priors (Ruling 54 P1: box = 3 × a measured prior of the same class) from `app-shell-build.md` Planned vs actual: Grok C1
30 min, P1 45 min; Codex D3a 47 min, D3b 43 min. Every brief: foreground only, the Return section required, two repair
cycles, `tools/run-tests.sh` then `tools/check-named-tests.py <track> --design docs/design/m12b-points.md`. Agy is not
used: every track here is either Core work better suited to Codex/Grok or long UI work (two HARNESS-SILENT-EXITs).

| Track | Harness | Owns (exclusive) | Depends on | Box (prior × 3) | Exit |
|---|---|---|---|---|---|
| **B0** Core grammar and model | Grok | `FoilSource.cs`, `Geometry.cs`, `PointModel.cs` (new), `docs/specs/foildsl.md` (§4/§5/§8 and conformance, same change), `src/CfdWorkbench.Cli/Program.cs`, `tests/CfdWorkbench.Cli.Tests/` additions, `FoilSourceTests.cs`, `GeometryTests.cs`, `PointModelTests.cs` (new), `tests/CfdWorkbench.Core.Tests/Fixtures/m12b/`, `docs/proof/m12b-old-build/` (characterization receipt, first step) | — | 135 min (P1 45 × 3) | B0 names PASS; receipt committed before the parser change |
| **B1a** Core chords | Grok | `ChordDimension.cs` (new), `LengthExpression.cs` (new), in `AuthoringSession.cs` only `ApplyDimensionCore`/`ApplyChord` and the memo `Fingerprint`, `DimensionTests.cs`, `LengthExpressionTests.cs` (new) | — (parallel with B0: tangent rows enter the chord fit as generic linear equality rows; B1b wires the parsed rows) | 90 min (C1 30 × 3) | B1a names PASS |
| **B1b** Core gestures and commands | Codex | the rest of `AuthoringSession.cs` (session members, `NativeProject` receipt code, `EditReference`, `RecoveryReference`), `PointGestureTests.cs`, `PointCommandTests.cs`, `ReopenPointEditTests.cs` (new), golden M1.2a recovery fixture | B0 and B1a merged | 140 min (D3a 47 × 3) | B1b names PASS |
| **U1a** controller | Codex | `WorkbenchController.cs`, `Selection.cs`, `Shell/ShellEvents.cs`, `ControllerShellTests.cs` | B1b; SHELLFIX joined | 130 min (D3b 43 × 3) | U1a names PASS |
| **U1b** Plan canvas | Codex | `PlanCanvas.cs` (new, with `PlanPointPeer`), `ModelArea.axaml`(.cs) (Plan and 3D samples tabs), `Styles.axaml` (new tokens), `PlanCanvasTests.cs` (new), the `--plan-canvas` entry point | U1a | 140 min (D3a 47 × 3) | U1b names PASS; three identical PASS sets |
| **U2** panes and menus | Grok | `PropertiesView.cs`, `Panes/PropertiesPane.axaml`(.cs), `Panes/BrowserPane.axaml`(.cs), `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `Shell/ShellHost.cs` (pane registration only), delete `Panes/RailEditorPane.*`, `ShellModelTests.cs`, `ShellWindowTests.cs` additions | U1a (parallel with U1b) | 135 min (P1 45 × 3) | U2 names PASS; UI-DEAD-CONTROL sweep green |
| **U3** UX review and polish | Claude (UI judgement) | `DESIGN.md` (if tokens move), `Styles.axaml` after U1b, `docs/reviews/m12b-native.md` (new), the four readiness checks in `tools/run-readiness.py` | U1b, U2 | 120 min (no same-class prior; Ruling 54: box as written, measured time recorded) | a11y and UX lenses re-review; every native row has an attach receipt |

Order: Coordinator lands the checker flag → {B0 ∥ B1a} → B1b → U1a → {U1b ∥ U2} → U3 → join (width ≤ 2). Critical path
B0 → B1b → U1a → U1b → U3 ≈ 11 h of boxes; measured priors suggest ≈ 4 h.

## 15. Deviations recorded

- **D-1** Points are Buttons with custom peers, not sliders (CAD-04, DESIGN.md v5) — §11.6; accepted by UX & A11y.
- **D-2** Rail tangent kinds are Smooth/Symmetric/Corner only; ADR-0005's grammar allows more on sections — §3.6.
- **D-3** No CLI `dimension` command (architecture §10.2 CLI column); M1.2a did not build one for Span either.
- **D-4** The light theme keeps the graphite viewport (v10 used a light canvas) — DESIGN.md governs.
- **D-6** Typed chords with a fit above 10 µm are accepted with a warning, not refused (A4.6) — Ruling 56 DR-12; §3.9.
- **D-5** Old builds report `DSL-SYNTAX` (bare 4.1 `.foil` with rows) or `DOC-UNSUPPORTED-FIELD` (project), not
  `DSL-VERSION`, for most 4.1 files — §3.8; the forward version check prevents this for future versions.

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| FoilDSL file (user-writable) → parser | T: crafted `tangents` block (unknown kind, row on a missing id, very many rows) | mitigate | closed kind set; id must be an interior Anchor; existing token and count limits (`FoilSource.cs`:942, :954, :1165) | `Parse_TangentRowOnControlPoint_DslLock`, `Parse_Foil41RoundTrip_RandomRowsStable` |
| FoilDSL file → parser | D: 16-point rails make certification slow | mitigate | 1 s proof budget; Not assessed, never a hang | `Assess_SixteenPointThreeAnchors_WorkCountBounded` |
| Native envelope (user-writable) → reopen | T: forged `curve`, `rule` or `rail` value | mitigate | closed per-rail sets in `EditReference`; `DOC-REFERENCE` | `Reopen_CurveOnGestureReceipt_DocReference`, `Reopen_ForgedRuleValue_DocReference` |
| Wing and Properties text fields → expression parser | D/T: huge or recursive expression | mitigate | 256 chars, depth 16, closed names | `LengthExpression_RandomText_NeverThrowsUnexpected` |
| Telemetry ring | I: point positions or ids leak | mitigate | fields limited to counts, durations, codes and µm figures | `Telemetry_PointEdits_NoIdsOrPositions` |
| History | R: an edit without attribution | accept | single local user; receipts name the edit kind, point and rule; the product has no identity | — |
| — | S, E | not applicable | no authentication, privilege levels or network | — |

## Privacy analysis (LINDDUN-lite)

This component touches no personal data (verified: the FoilDSL source, receipts, planform projections and the events
carry geometry, counts, durations, codes and µm figures only; no path, name, account or free text is added to any event —
`Telemetry_PointEdits_NoIdsOrPositions` enforces it for the new fields).

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| gesture and apply events (local ring) | D: disclosure through logs | mitigate | no ids, names or positions | in-memory ring of 256; gone at exit |

## Conformance notes

- ADR-0002: one geometry authority — every edit is a source patch; `PlanformView`, the probe, the comb and estimates are
  derived binary64 display, never stored, and the advisory crossing check never decides.
- ADR-0005: type derived; rows 4.1; ceiling refusal names it (16 per the amendment); MakeControl is the one construction
  ADR-0005 names.
- ADR-0006: chord refit on own basis, ordinates only, hard rows, the rule id and both numbers; `WingEstimates.From`.
- ADR-0007 §3: gesture = draft at pointer-down, applied at release when Certified; Escape cancels; nudge runs commit on
  key release, focus loss and deactivation.
- LOA P8: operation ids on every commit, one fingerprint for commit and replay. No model call; T0 deterministic.

## Flagged risks and residual unknowns

- Under Ruling 56 a typed chord can leave a fit residual above 10 µm (22.53 µm at ×1.5 on New foil); it is always shown
  as a warning, and the revision still certifies (the typed end is exact).
- Per-frame cost, commit latency and 16-point certification time are **Inferred** until readiness measures them.
- `RenderTargetBitmap` of the whole window and the custom peers on the macOS AX bridge are **Inferred** (first U1b tests).
- Native AX attach (CO-UI-READY, review F-ATTACH) still blocks native rows; U3 stops rather than exit on "operator-run".
- Anchors are G1/C¹ joins; fairness depends on the comb and the user (architecture §10.9). The 16-point fairness fixture
  measures, it does not certify.
- The one-rail-locked chord case and a chord refit with anchors are specified but not yet run (Geometry residual).
- SHELLFIX may change the model-area host; U1b adapts to its merged shape.

## Status & next action

| | |
|---|---|
| **Completed** | M1.2b detailed design (this document); ADR-0001 Amendment 1 (DR-10); quarter-chord spike; DESIGN.md token and component row; register entries UI-RENDERED-STATE and REPLAY-MEMO |
| **Remaining** | M1.2c design (section editor, B6 restart), M1.2d design; M1.2b2 design (3D view and elevations, Ruling 56); OI-2–OI-5 placement; spec-owner findings F-3, F-4, F-7, F-8 |
| **Best next action** | The Coordinator lands the checker `--design` flag (named precondition, §12.2) and dispatches B0 ∥ B1a |

## Gate record

`GATE design · 2026-09-30 · Patterns Expert, Simplifier, Test Architect (hard veto), Computational Geometry (hard veto, narrow), UX & Accessibility (hard veto), Marine-CAD UX (soft veto), Data & Persistence (hard veto) · criteria met: data model first, E7, contracts with sources and marked assumptions, named patterns past both Patterns and Simplifier, failure modes with tests, STRIDE-lite, LINDDUN-lite, telemetry, UI vs v10, 187 attributed test names (checker extract: 0 errors) · verdict: PASS WITH CONDITIONS · vetoes → resolution: Test Architect HARD VETO → cleared on re-review; Simplifier SOFT VETO → cleared on re-review; all others pass with conditions · author did not self-clear`

| Lens | Initial verdict | Main findings | Repair cycle 1 | Re-review | Cycle 2 (conditions from re-review, applied by the author, not re-reviewed) |
|---|---|---|---|---|---|
| Patterns Expert (advisory) | Pass with conditions (4 Major) | memo key differs between commit and reopen (as-built, F-5); incomplete state machine; no trailing flush; two commit idioms; wrong labels | fingerprint; transition table; flush; one Busy state; Evaluate/Apply; labels | Conditions open (1 Minor): fingerprint fields must be persisted | fingerprint from persisted values incl. the resulting source id; two conflict tests |
| Simplifier | **Soft veto** | plan queries, DescribeEdit, new error code, T0 track, extra events, Desktop computing handle polar | all cut or moved to Core; context menu and Browser groups defended | **Cleared** | span-vs-chord entry note |
| Test Architect | **Hard veto** (2 Blockers) | ledger unreadable by the checker (0 names); promises without tests; wall-clock tests in the fast ring; surviving mutants; red-first impossible; tautological pixel oracle; T0 unrunnable; weak demo gate | ledger re-formatted (extract: 181 → 187, 0 errors); ~30 tests added; readiness ring; boundary fixtures; characterization receipt; whole-window oracle; attach receipts | **Cleared with conditions** | two names renamed; checker flag red→green before dispatch; readiness owners and receipt; double-click test split U1b/U2 |
| Computational Geometry | Pass with conditions; ADR-0001 accept with conditions | grid rounding breaks Symmetric (measured 500 nm vs 25–39 nm); SetTangent breaks order; clamp below existing gaps; mult-2 knots; Smooth vs Span; η_t; δ bits; rule id; comb sides; MakeControl ambiguity | quantized Δ with exact positions; ordinate-first SetTangent; min(1 mm, gap); stored rule; one-sided comb; one MakeControl construction; four amendment conditions | **Pass**; ADR-0001 **accept** | gap read at Begin; wording "toward the anchor"; "within 1 ulp" |
| UX & Accessibility | Pass with conditions; D-1 accepted with conditions | peers without bounds/focus; focus-selects breaks multi-select; handle names; selected-vs-unselected contrast; live regions; HC figures; 2.5.8 equivalent; tooltip Escape; DESIGN.md rows | custom peers; Space/Shift+Space; handle names; hollow selected control; live-region tests; HC on graphite; Browser rows; lock ring r 11; DESIGN.md fixed | **Pass, veto cleared** (re-arms at U3 if peers or live regions fail) | overlays count as obscuring for 2.4.11; probe and Δ are not live regions |
| Marine-CAD UX | Pass with conditions (5 Major) | rejected isometric view beside the Plan; no Tracing probe; comb short of A4.9; arrows turned handles; Shift-drag undefined; trackpad; SetTangent handle; handle typing; cancel copy | sample plot to its own tab, OI-1; probe; OI-5; arrows move handles; Shift axis lock; DR-13; keep selected handle; angle axis and field; copy | **Pass** | status line names which handle shortened |
| Data & Persistence | Pass with conditions; ADR-0001 accept with conditions | old builds say DSL-SYNTAX / DOC-UNSUPPORTED-FIELD, not DSL-VERSION; null `curve` would migrate every save one way; rule provenance; stray fields; pinned codes; header writer; golden recovery bytes | D-5 and forward version check; WhenWritingNull; stored `rule`; closed per-rail sets; EnsureHeader41; golden fixtures | **Pass**; ADR-0001 **accept** | `rule` required on chord rows; ADR-0005 V16 flag; drag-only file in the characterization run |

Two repair cycles of two were used. Not convened, with reason: Security & Identity (no identity, secrets, PII or network; file-input threats are in STRIDE-lite above), Distributed Systems (no messaging; in-process async covered by the Patterns review). Residual: every screen-reader and rendering claim stays **Inferred** until U1b's first tests and U3's native pass.

---
**Handoff:** → `/implement` (tracks §14).
