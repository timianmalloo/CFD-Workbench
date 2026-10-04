---
id: design-m12c-section-editor
title: "Design: M1.2c — section editor mode, per-surface section points, the Points pane and the Precision workspace"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — M1.2c (operator 2026-10-03, "Start M1.2c next")
tags: [desktop, core, cad, section, profile, section-editor, point-types, anchor, b6, certificate, points-pane, precision, messages, m1.2c, dr-11]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: implements }
  - { to: architecture-application, rel: refines }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: note-m12c-rulings, rel: depends-on }
  - { to: design-section-editor, rel: refines }
  - { to: design-m12b-points, rel: depends-on }
  - { to: design-m12b2-3d-elevations, rel: depends-on }
  - { to: design-app-shell, rel: relates-to }
  - { to: property-grid-rulings, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: mockup-m12c-section-editor, rel: relates-to }
  - { to: review-ui-status-bar, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Detailed design of slice M1.2c. A station's section becomes editable as a CAD mode: Edit section (from Properties,
  the Plan, the Side view or the Browser) replaces the views with a 2D editor of the profile record, and every move,
  type change and construction is a step of one section draft (ADR-0007) that Finish commits as one undo step. Section
  points get Anchor/Control types per surface (DR-11), which needs the deferred B6 certificate restarted as a spike
  first; a paired fallback is pre-designed and tested. The slice also adds the Points pane and the Precision preset,
  resolves the planned Messages pane against DR-STATUS-1, and records the operator's rulings of 2026-10-03 (OD-1 A, OD-2 A, OD-3 B, OD-4 a). Gate: four lenses,
  one repair cycle.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# Design: M1.2c — section editor mode, per-surface section points, the Points pane and the Precision workspace

- **Status:** In review; repair cycle 1 of 2 applied (gate record at the end). **Rulings of 2026-10-03 applied**
  ([`docs/notes/m12c-rulings.md`](../notes/m12c-rulings.md)):
  - OD-1 A, OD-2 A, OD-3 B, OD-4 a;
  - ADR-0007 Amendment 1 accepted;
  - section drawing uses the fast display path (ADR-0010 Amendment 1; §3.7; track DSP).
- **Spec / architecture:**
  - [spec rev 1.6](../specs/cfd-workbench-v1.md): CAD-10, CAD-11, CAD-15, CAD-17, CAD-20, A4.9, A4.15, UI-25, UI-37,
    UI-43, UX-30, UX-31, B1;
  - [FoilDSL](../specs/foildsl.md): §4, §5 items 3, 5, 6 and 10, §6;
  - [ADR-0005](../adr/0005-point-types-in-the-b-spline-record.md);
  - [ADR-0007](../adr/0007-edit-transactions-section-draft-and-gesture-commit.md), with a proposed amendment from this
    design (§3.3).
- **Delivery phase:** M1.2c. It follows M1.2b2 (joined at `4b9bc35`) and precedes M1.2d (catalog Replace, My
  sections) and M1.2e (floats, saved layouts, Review; app-shell track D4, which waits on this slice's section draft).
- **Mockup:** [`docs/mockups/m12c-section-editor.html`](../mockups/m12c-section-editor.html). The operator sees it
  before any build.

## 0. What the operator will see

The operator, after looking at the native M1.2b2 build on 2026-10-03: *"I should be able to edit the sections."* Today
the sections show in the Plan, 3D and Side views, but nothing edits them. The M1.1 "Section" document tab still draws
a canvas. None of its buttons has a handler, and nothing applies the canvas's `VertexMoved` or `VertexSelected` (§1).
**M1.2c is the slice where a section becomes a CAD surface.**

### 0.1 The demo at the end of M1.2c (packaged `.app`, macOS, the Example foil)

1. **Select a station** in any view. Click the Root chip in the Plan, click the root section in the Side view, or
   pick it in 3D or the Browser. Where Side sections overlap, a click picks the outline nearest the pointer, and a
   second click at the same spot picks the next one (§11.3). Properties shows the Station group. Its last row is the
   link **Edit section…**.

2. **Enter the editor.** Choose Edit section…, press Return with the station selected, or double-click the section in
   the Side view. The views give way to the section editor. The **mode bar** at the top of the model area reads:
   "Editing Root section · Shared with Tip · Make unique to Root · Section ▾ · Curvature · Thickness ×2 · Cancel ·
   **Finish section**". Curvature is on and Thickness ×2 is off by default.

   The canvas shows section-a at 1:1 in its own chord coordinates (0–100 % chord):
   - two surfaces, each with its dashed control polygon and the M1.2b glyphs;
   - the nose as a diamond shared by both surfaces, and a diamond at each trailing-edge point;
   - the comb on both surfaces, auto-scaled, with the nose teeth clipped and marked;
   - a probe that follows the pointer.

   Focus is on the first point (CAD-20). The **station strip** under the canvas shows each station as a thumbnail with
   its name, its distance from the root, its chord and its t/c.

3. **Select upper point 4** (x 35.00 %). Properties shows "Upper surface · point 4 of 8": **Type: Control point**,
   **x 35.00 % c**, **y 6.50 % c**. The Section group shows:
   - **Own t/c 11.26 % at 33.13 % c**;
   - **t/c at Root 12.00 %** and **t/c at Tip 12.00 %**, followed by "From the Thickness curve. At each station this
     shape is scaled to that t/c.";
   - the LE radius of each surface and the TE gap and wedge, each as **own** and **at <station>** (what Rule A builds
     there): section-a's LE radius is 1.04 % c own and about 1.18 % c at Root, because Root scales it to 12.00 % t/c.

4. **Type → Anchor point.** Only the upper surface changes: it goes from 8 to 13 points, the curve now passes through
   (35.00 %, 6.50 %), and the point has two handles. The lower surface is unchanged (DR-11).

   The strip says: "Upper point 4 is now an anchor (now point 7 of 13). Upper surface 8 → 13 points; lower unchanged.
   Largest change 0.88 % chord. Curvature now breaks at 35.00 % chord." The comb shows that break.

   **Tangent → Horizontal** levels the handles, so the crest is flat at 35 % in the section's own coordinates. Each of
   these changes is one step of the draft.

5. **Drag** lower point 6 up, past the upper surface.
   - A red dashed marker shows where the surfaces cross.
   - **Finish section** stays visible but is off, with the reason "⚠ Upper and lower surfaces cross. Move the point
     back to finish."
   - The status strip shows the same warning with a **Show** button that frames the crossing.

   Press ⌘Z: the step is undone, and the strip says "Surfaces no longer cross. Finish is available." (COPY-123/124.)

6. **Nudge and type.** ↑ moves 0.1 % chord, ⌘↑ moves 0.01 %, and ⇧↑ moves 1 %. Holding a key down is one step.
   Return goes to the point's x value in Properties (a handle's Angle). Typing `36` there moves the point to 36.00 %;
   typing `43.2 mm` is converted at Root's 120.00 mm chord and echoed in % in the strip. ] and [ walk the points.
   ⌫ deletes the selected point. Double-click on a surface inserts a point there.

7. **Finish section.** One undo step is added, and the views come back with the Root station selected. The 3D view,
   the Side view and the Wing block's Max t/c show the new section. ⌘Z restores the old one exactly. **Cancel** instead
   leaves the source, the assignments and the undo depth as they were at entry.

8. **Make unique to Root**, from the scope chip or Section ▾. Root gets its own copy, `section-a-i1`. The chip then
   reads "Only Root uses this section", and Tip keeps section-a.

9. **Section ▾** holds:
   - Insert point…
   - Insert anchor (keep shape) — Boehm only, with no shift
   - Delete point
   - Smooth… (Fair or Rebuild)
   - Import .dat…
   - Make unique to <station>
   - Station t/c ▸ From the Thickness curve / From this section

   Each command reports in the strip with its measured change.

10. **Precision (⌘2)** shows the **Points pane** (§13, OD-3). In the section editor it lists the control net of each
    surface (x, y, Type, Kind) with typed edits, and selection agrees both ways. **Planform (⌘1)** hides it. A user
    who had already saved a Precision layout keeps the Points pane where they put it (layouts are honoured as saved).

11. **Save, close, reopen.** The section, its point types and its tangent kinds come back as saved. The Foil source tab
    shows `foildsl "4.1"`, per-surface knot vectors, and a `tangents` row on the upper surface.

### 0.2 What the operator will NOT see in M1.2c, and which slice brings it

| Not in M1.2c | Brought by |
|---|---|
| Replace from catalog, Save to My sections (Section ▾ leaves them out, not disabled) | **M1.2d** |
| Floats, Maximize pane, saved per-workspace layouts, focus-safe floats, the close-with-draft rows F11 S1/S10 | **M1.2e** (app-shell D4) |
| A list of past edit reports | **None planned** (§13 OD-2). ⌘Z is the history; the strip shows the last report |
| Dragging points on the Side view's placed section | **Not planned** (§13 OD-1 C): the placed section is not the record (§3.6) |
| A read-only table of offsets (upper, lower, t and camber at standard stations); a neighbour-station ghost; a reference-section overlay | No slice yet (OI-12C-3) |
| A curvature-continuous (G2) tangent kind | No slice yet (OI-12C-4) |
| Comb scale and density controls (beyond the section auto-scale) | No slice yet (M1.2b OI-5) |
| Insert / Delete / Fair on rails and channels | No slice yet (M1.2b OI-2) |
| Moving several section points at once | No slice yet (OI-3) |
| Windows | Deferred (architecture §8) |

## 1. Grounding — what this design must satisfy

**Traversal:** `spec-cfd-workbench-v1` (CAD-15, CAD-20, A4.15, UI-37, UI-43, UX-31, B1) → `adr-0005-point-types`
(DR-11) → `adr-0007-edit-transactions` → `design-section-editor` (§2, §3, §9 B6) → `design-m12b-points` (§0.2,
§3.5–3.8, §11.2) → `design-m12b2-3d-elevations` (§3.6, §11.1 Side) → `property-grid-rulings` (DR-CELL-1..5, DR-DEN-1/3,
DR-STATUS-1..4, D-4, DR-NAV-1, DR-VIEW-1..8, MC-2/3/6) → `rulings` (53 DR-11, 58 Q-1) → the code at `4b9bc35`.

| Source | Load-bearing statement |
|---|---|
| CAD-20 | Edit section (the Properties button, or double-click or Return on the station) opens the section editor **in the model area**, with focus on its first point. The toolbar reads "Editing <station> section" with Section ▾, Curvature, Thickness ×2, Cancel and Finish section. Finish adds exactly one undo step. A crossing disables Finish (COPY-123); clearing it announces COPY-124. Cancel restores the entry state and leaves the undo depth unchanged. Escape with edits discards nothing and moves focus to Cancel. Undo at the entry depth does nothing. Switching station with edits is refused. Save asks to Finish or Cancel first |
| CAD-15, A4.15 | A section point is an Anchor (on the curve, with handles and a tangent kind: Smooth · Symmetric · Corner · Horizontal · Vertical · Fixed angle) or a Control point (off the curve). The nose and the two trailing-edge points are named points with a fixed type. **The nose has the LE radius as a readout; the trailing-edge points have the TE gap and wedge angle.** A type change is one undoable step |
| A4.9 | The Tracing probe follows the pointer |
| ADR-0005 | A point's type is **derived** from the knot vector (an interior knot of multiplicity exactly p) and never stored. Tangent kinds are FoilDSL 4.1 `tangents` rows outside geometry identity. Every type change is measured and reported. **DR-11 was ruled "Independent section point types. This requires the B6 restart in the M1.2c wave"** (Ruling 53) |
| ADR-0007 | One section draft per editor visit: an ordered list of source-patch steps. Inner Undo/Redo never pass the entry. Cancel discards. Finish applies **one** accepted row. Recovery resumes the latest bytes. "design-slice names" the receipt fields and owns the step shape and the mode state machine |
| FoilDSL §5.5, §6 | Upper and lower are compared at equal chord x, **each on its own knot vector and inverse abscissa**, so per-surface bases are already legal in the language. Rule A: C = (u + l)/2, T = (u − l)/max(u − l), and the placed point is q = (x, C ± t(η)·T/2) |
| FoilDSL §5.6 | In a foil, the Thickness channel sets the effective t/c. A profile's own thickness is normalized away unless **Use source thickness** retargets the channel |
| DR-STATUS-1, D-4 | Reports go to the status strip (one slot, no history) plus a warning toast. **No scrollable message list sits in, or docks to, the bottom bar.** Whether M1.2c keeps an edit-report history is decided here |
| DR-CELL-1..5, DR-DEN-1/3 | Structure-B property sheet. Type is 11 px, nothing smaller. Targets are 24 px |
| DR-NAV-1 | Tab leaves a canvas for the selected point's first Properties value. Shift+Tab returns. ] and [ move between points |
| DR-VIEW-1, -7 | Views are framed (1 px) with a 4 px gutter. Each view has a top-left label plate |
| Ruling 58 Q-1 | Control points stay off the curve with no handles. Only anchors carry handles |
| MC-3, MC-6, DR-UID-1 | Precision follows the quantity. Echoes go to the strip. Typed values show 0.01 of their unit |
| UX-31, B1 | Precision adds Points (and Messages) in the bottom panel. Points is "a grid of every point with typed edits and two-way selection". Messages is "anything that stops Finish or Save, each with Show" |

**Code as built (opened at `4b9bc35`, not recalled):**

- **The Section tab is a dead surface.** `SectionEditorView.axaml`:1-78 has Insert CV, Delete CV, Fair, Rebuild,
  Import .dat, Preview, Apply, Cancel draft, an X/Y pair and a vertex list. Its code-behind only sets the canvas
  profile. No handler in `src/` subscribes to these controls or to `SectionCanvas.VertexMoved` / `VertexSelected`;
  `tools/check-event-subscribers.py`:26-28 allow-lists those events "until M1.2c". The controller still has the M1.1
  single-vertex API (`WorkbenchController.cs`:1381-1449). **Verified.**
- The section body is a Dock document tab, "Section" (`Shell/ShellLayout.cs`:73-79, `ShellHost.cs`:141-148).
  **Verified.**
- **No section point can be an anchor.** `FoilSource.IsAnchor` returns false unless the degree is 3
  (`FoilSource.cs`:260-267), and `BindTangents` refuses a row on a non-anchor (`:1272`). A 4.1 probe fixture with a
  multiplicity-5 upper knot and a `horizontal` row returns `DSL-LOCK` "A tangent row names an interior anchor".
  **Verified** by running the CLI. `Geometry.CheckTangentRows` would check profile rows with the rail rule
  (`Geometry.cs`:407-462); that path is unreachable today.
- **The certifier requires a shared basis.** `Assess` refuses sides that differ in degree, knots or CV abscissae
  (`Geometry.cs`:315-317, `:342`) and adjacent distinct profiles without a shared abscissa (`:365`). Separation and
  the thickness maximum are proved on the shared-parameter difference polynomial (`:343-352`). The blend maximum is
  proved per query on scaled sums of those differences (`EncloseMaximumT0`, `:209-218`).
- **Whole-domain admission proofs already consume the maximum's gap.** `PlacementWidth` (`:479-521`) and
  `BlendPlacementWidth` (`:523-573`) prove the placed error ≤ 10⁻⁸ m at admission. Point evaluation already inverts
  each side separately (`Bernstein.EncloseAt`, `:909`). **Verified** by the Computational Geometry lens.
- **The as-built patch and fit paths write one shared basis:**
  - `FoilSource.RewriteCurves` writes one knot array to both sides (~`:731-746`);
  - `InsertProfileKnot` (`:622`) and `DeleteProfileVertex` call `PairAbscissa` (`:917`);
  - `ProfileFair.Rebuild` assumes a shared x(t) (`ConstrainedFit.cs`:176-185), and `ConstrainedFit` has pin rows only
    (`:330-341`);
  - `DatImport.ProfileBlock` writes one basis (`DatImport.cs`:263).

  **Verified** by the lens.
- **Recovery is checked against the base.** `NativeProject.Check` requires a recovery's `Profile` to be the base
  definition's profile at that assignment (`AuthoringSession.cs`:1530-1531), and its rail set has no `"section"`
  (`:1521`, `:1529`). The Desktop's `ResumeRecovery` looks up a rail by name (`WorkbenchController.cs`:1672).
  **Verified** by the Data & Persistence lens.
- **Sections are drawn through the certificate today.** `AuthoringSession.Sample` (`:536-550`, behind `ProfileAt` /
  `ProfileView`) evaluates each of 101 uniform x/100 points through `Bernstein.EncloseAt` at 10⁻⁸. Its own comment
  records about 970 ms on a degree-5, 10-point rebuild, against a 1 s proof budget. `Placement.cs` already has a
  private binary64 profile jet and inversion (`Jet`, `ParameterFor`, `OrdinateAt`, `:403-430`) next to
  `ChannelEvaluator` (`:110`). **Verified.** This is what the display-path ruling folds (§3.7).
- `Selection.Points` already carries `PointRef(Curve, VertexId, Profile?)` (`Selection.cs`:12). `CurvePointLayer`
  draws the M1.2b glyphs, hit-tests at 14 px and maps arrow keys through any axis mapping. The Side view selects a
  station when its section is clicked (`ElevationView.cs`:733-741); it has no double-click or Return verb. **Verified.**
- `WorkspacePresets` registers properties, browser, points and messages, with points and messages in the bottom
  region (`Shell/WorkspacePresets.cs`:7, :43, :62-63). `LayoutCodec.Homes` repeats those homes (`:22-27`). There are no
  Points or Messages panes in `ShellLayoutFactory`. Window ▸ Planform / Precision / Review are `NoOp` rows
  (`Shell/CommandTable.cs`:120-122). **Verified.**
- **Measured:** `geometry.validate` on the Example took 9.35–10.57 ms in the CLI session events (Debug build, this Mac,
  two runs). The session event ring holds 256 events (`AuthoringSession.cs`:96).
- **DR-VIEW-9/10/11** (Home beside the cube; the One-view plate picker; its "↩ Back" item) are recorded in
  `docs/notes/property-grid-rulings.md` on the integration branch `feature/ui-cad-direction`. **Verified** with
  `git grep`. They do not touch the section mode, which hides the views. This design depends on DR-VIEW-1 and -7.

## 2. Responsibility

M1.2c makes one station's section editable as a CAD mode, end to end. That covers:

- **the section draft** (ADR-0007): Core steps, inner Undo, Finish, Cancel, the receipt and recovery;
- **section points**: roles, named points and their readouts, per-surface point types, tangent kinds, the profile row
  rule, and per-surface constructions;
- **the certificate for per-surface bases** (the B6 restart), or the tested paired fallback;
- **the editor surface**: mode bar, canvas, comb, probe and station strip, with entry from Properties, the Plan, the
  Side view and the Browser;
- **the section rows in Properties**;
- **the Points pane and the Precision preset**.

It is **not** responsible for: catalog Replace and My sections (M1.2d); workspace memory, floats and close-with-draft
(M1.2e/D4); analysis; rails and channels (unchanged except for the shared glyph layer).

## 3. Data model (settled first)

### 3.1 Bounded context and ubiquitous language

This is the authoring context; the FoilDSL document is the aggregate (ADR-0002). User-facing words, and the code
names behind them:

| Word (UI) | Meaning | Code |
|---|---|---|
| Section | the profile assigned at a station | `Assignment` → `ProfileDefinition` |
| Surface | the upper or lower side of a section | `SurfaceSide { Upper, Lower }` (new); `profile:<i>:upper` paths |
| Point · Anchor · Control | a control vertex of a surface; its type is derived (ADR-0005) | `PointView`, `PointRole` |
| Control net | the vertices of a surface (not points on the curve) | Points pane heading |
| Nose · Trailing-edge point | named points: vertex 0 of both surfaces (shared, at (0,0)); the last vertex of each surface | `PointRole.Nose`, `PointRole.TrailingEnd` (new) |
| Handle | the neighbour vertex of an anchor or a named point | `AnchorHandle`, `NoseHandle`, `TrailingHandle` (new) |
| Own t/c · t/c at <station> | max(u − l) of the record · the Thickness channel at the station | derived, never stored |
| Section draft · step · Finish · Cancel | ADR-0007 | `SectionDraft`, `SectionStep` (new) |
| Make unique to <station> | copy the profile for one assignment | `SectionScope.Independent` (as built) |
| Per-surface point types | an anchor on one surface does not add points to the other (DR-11) | per-side knot vectors |

"Independent" is already the code name of the scope (Make unique). The point-type property is called **per-surface**
everywhere, so the two never share a word. A `"section"` receipt's `VertexId` holds a **profile name**, the same way
the `"dimension"` rail's `VertexId` holds "span" or "root-chord".

### 3.2 Aggregates and invariants

| Aggregate / entity | Root and identity | The one invariant it protects |
|---|---|---|
| **Foil document** (unchanged) | the accepted source revision chain | the accepted source denotes exactly one certified shape (FoilDSL §2) |
| **Section draft** (new; session memory) | draft id; holds the base accepted id, the assignment, the target profile name, the step list and a cursor | the bytes at the cursor are the base bytes plus steps 1..cursor, and each step parses and passes the structural checks. **Finish happens only when the cursor bytes certify and differ from the base** |
| Profile (entity in the document) | unique name | Sides run LE → TE from (0,0). Each side's abscissae are nondecreasing and its evaluated x strictly increases. **Upper is strictly above lower at equal x on (0,1), each side on its own basis.** Closure holds. Each `tangents` row names an interior anchor of its own side and holds (§3.4) |

Value objects: a surface curve (degree 5, 6–32 vertices, its own knots and ids), a `SectionStep`, a tangent row. The
draft refers to the document only by the base accepted id.

### 3.3 Durable representation, grain, history, receipt, recovery, migration

**Representation (unchanged; ADR-0002, ADR-0005, ADR-0007).** The source bytes are kept in append-only accepted rows.
There is no new store and no new authority. Per-surface knots are legal in the 4.0 grammar. Section `tangents` rows
are legal under 4.1, and the Finish patch that writes the first one also rewrites the header through
`FoilSource.EnsureHeader41`, which never lowers it.

**Grain.** One accepted row is exactly **one Finish of one section draft whose cursor bytes differ from its base
bytes**. That includes a draft resumed from recovery. A Finish whose bytes equal the base adds no row and acts as
Cancel (`SectionDraft_FinishBytesEqualBase_NoRow`).

**Receipt.** The receipt is `EditReceipt(DraftId, Generation, Rail: "section", VertexId: <profile name at Finish>,
Intent)`. `"section"` is the only new value in the closed `rail` set. **There is no step count in the receipt.** The
Data & Persistence ruling on OI-12C-2: it would have no compute reader (DM15), it would be undefined after a resume
from recovery, and the `section.finish` telemetry answers "how many steps per Finish". This design proposes an
**ADR-0007 amendment** striking "the receipt carries the step count", with a dated note in the ADR.

The `EditReference` arm for `"section"`:

- Curve and Rule are null;
- the child has a profile named `VertexId`;
- the parent also has that name, **or** the child assigns a profile of that name that is new relative to the parent
  (Make unique).

Tests: `Receipt_SectionRailWithCurveOrRule_DocReference`, `Receipt_SectionMakeUnique_ChildOnlyName_Accepted` and
`Receipt_ForgedSectionRailUnknownProfile_DocReference`. Finish is idempotent through the generic `apply:` binding,
live and after reopen (`FinishSection_SameOperationIdAfterReopen_Idempotent`).

**History rule.** Profile content is Type-2 by construction: every Finish is a new source revision. A point's type is
derived from the knots. A tangent row is stored editing intent, outside identity (ADR-0005 §4). No attribute is Type-1.

**Derive, don't store.** These are all computed on read:

- own t/c and its x;
- station t/c;
- the LE radius, TE gap and wedge;
- point roles and types.

No measure is stored, so additivity does not apply (stated, not omitted).

**Recovery (expand-only).** A `RecoveryRow` with rail `"section"` has:

- `Profile` and `VertexId` = the **base profile name at the assignment at entry**, which is always present in the base,
  even after a later Make unique step;
- `Assignment` and `Intent` set;
- its scope derived from the bytes.

`"section"` is added to the rail sets at `:1521` and `:1529` and to `RecoveryReference`. The Desktop resumes a
`"section"` recovery by entering section mode (CTL), with cursor 0 on the recovered bytes and an empty inner undo.

**Legacy profile recoveries.** A project saved by an M1.1–M1.2b2 build can hold a recovery row with rail
upper/lower/insert/delete/fair/rebuild and a profile. It resumes **as a section draft**: base = `BaseAcceptedId`, entry
bytes = the recovered bytes, cursor 0. The contract step (§5.1) deletes **writers only**. Every reader of the old
shapes stays, along with its fixtures: `EditReference` :1464-1467, `RecoveryReference` :1481-1483, and the profile
checks at :1521-1536.

**Writer and compute reader per persisted field:**

| Field | Writer | Readers |
|---|---|---|
| source bytes | `AuthoringSession.FinishSection` | `Geometry.Assess`, `Placement`, `Sections.View`, CLI `inspect` |
| `rail "section"` | `FinishSection` | `NativeProject.Check` (`EditReference`); the Undo label after reopen ("Undo edit Root section") |
| recovery `"section"` | `CaptureRecovery` | `ResumeRecovery`, `RecoveryReference` |

**Migration.** None is forced. What a build older than M1.2c does with the new files is **observed, not assumed**: SDR
records a characterization receipt in `docs/proof/m12c-old-build/` by running the `4b9bc35` CLI and app build on
committed fixtures. Each case records the code, the bytes unchanged, and an attempted edit plus Save that is refused.
The expected outcomes come from the Data & Persistence lens's code reading:

| Case | Expected in a `4b9bc35` build |
|---|---|
| (a) bare `.foil`, per-surface bases, no rows | opens as Unsupported geometry ("Independent profile x mappings are not assessed"), read-only |
| (b) bare `.foil` with a profile `tangents` row | `DSL-LOCK` at parse ("A tangent row names an interior anchor"), because the old `IsAnchor` is degree 3 only |
| (c1) project with a `"section"` receipt whose sources carry no profile row | `DOC-REFERENCE` (`EditReference` falls through) |
| (c2) project with a `"section"` receipt whose source carries a profile row | **Observed `DSL-LOCK`** (receipt, SDR 2026-10-03): `Check` parses every retained source (`:1495`) and throws the parse error's own code, not `DOC-SCHEMA` (the design expected `DOC-SCHEMA`) |
| (d) project with a `"section"` recovery; and the same with a row in the history | `DOC-REFERENCE` (`:1521`), observed; with a row: **observed `DSL-LOCK`**, as (c2) |
| (e) project whose per-surface source arrived through an open row, with and without rows | without rows: **observed refused with `DSL-NOT-ASSESSED`** (the read-only path exists only for a bare `.foil`); with rows: **observed `DSL-LOCK`**. Every case: no edit, no Save, file SHA-256 unchanged |

**No FoilDSL version bump.** The grammar has always allowed per-side knots. A bump would mark what a certifier can
prove, not a language change. The accepted consequence, recorded as deviation D-6: an old build shows "a tangent row
names an interior anchor" or "not assessed" instead of "saved by a newer version". It never mutates the file, provided
the receipt confirms that edit and Save are refused.

### 3.4 Section points — roles, readouts, kinds, operations (derived, Core)

`Sections.View(source, assignment, side, basis, generation) → CurveView` gives each surface the same shape the rails
and channels use (`PointView`, with `SpanMeters` = the chord fraction x and `Ordinate` = y). `IsAnchor` is generalized
to degree p: an interior knot of multiplicity **exactly** p.

| Role | Vertex | Type shown | Freedom | Readouts |
|---|---|---|---|---|
| Nose | vertex 0 of both surfaces, (0,0) | Anchor, read-only (COPY-177) | Fixed | **LE radius per surface, own and at each station.** Own: r = 1/κ(0) from the analytic derivatives (section-a: 1.04 % c each side, hand-checked by the CG lens). At a station: from the placed Rule A curve (y² ≈ 2 r x at the vertical nose tangent); for a symmetric section it is r·k² with k = station t/c ÷ own t/c. In the mockup's edited state: 1.04 own → 1.02 at Root (k = 0.990). A note appears when upper and lower differ |
| Nose handle | vertex 1 of each surface (x = 0) | handle | y only, sign kept (upper > 0, lower < 0) | length, % c |
| Trailing-edge point | last vertex of each surface (x = 1) | Anchor, read-only (COPY-178) | y only with `closure open`; Fixed when closed | **TE gap** (% c, and mm at the station) and **wedge angle**, own and at each station (tan of the half-angle and the gap scale by k for a symmetric section): 11.42° own → 11.31° at Root in the mockup's edited state |
| Trailing handle | vertex N−2 | handle | Free; it has a typed **Angle** (the TE angle) | angle, length |
| Anchor | interior; multiplicity-5 knot at it | Anchor | Free (x between its neighbours on its side); an anchor move carries its handles | kind |
| Anchor handle | either side of an anchor | handle | Free; constrained by the kind | angle, length |
| Control | every other interior vertex | Control | Free (x between neighbours) | COPY-117 |

**Tangent kinds on an interior section anchor:** Corner (no row), Smooth, Symmetric, Horizontal, Fixed angle.
`TangentKind` gains `Horizontal`, `Vertical` and `Angle`, and `PointView` gains `AngleDegrees?`. The Kind list on an
interior section anchor shows **Vertical disabled**, with the reason "A vertical tangent inside a surface makes a step.
The nose already has one." (deviation D-5 from A4.15's list; flagged for the spec owner). The parser and certifier
still accept and check `vertical` rows, so a hand-edited file is judged correctly.

Angles are in the section's own chord coordinates. Help text, COPY-183: "Angles are in the section's own chord
coordinates. A flat crest stays flat at a station only if the section is symmetric or uses its own thickness." That
is because Rule A rescales thickness, and the placed slope is ((1+k)u′ + (1−k)l′)/2.

**The profile row rule** (Geometry; the section plane is isotropic, so Euclidean distances are meaningful):

| Kind | Holds when |
|---|---|
| smooth | the perpendicular distance of the anchor from its handle line, \|(r − l) × (a − l)\| / \|r − l\|, is ≤ τ_s, **and** the anchor lies strictly between its handles: (a − l)·(r − a) > 0 |
| symmetric | as smooth, and the anchor is the handles' midpoint within τ_s |
| horizontal | both handles' y equal the anchor's y **exactly**, and l.x < a.x < r.x |
| vertical | both handles' x equal the anchor's x **exactly**, and (l.y − a.y)(r.y − a.y) < 0 |
| angle α | the right handle lies within τ_s of the ray from a at α, and the left handle within τ_s of the ray at α + 180° |

τ_s = 10⁻⁹ chord. The Computational Geometry lens confirmed it: the solver residual is about 10⁻¹⁶, a margin of
7 orders, and 10⁻⁹ chord is 2 nm at 2 m. The ordering conditions refuse a cusp. The probe showed that a same-side
"vertical" pair would otherwise certify a spike.

**Operations on one surface** (ADR-0005 §5, applied per surface under DR-11). Every SetType and SetTangent step
**reports its measured change**, `MaxChange` (`SectionEdits_TypeAndTangentSteps_ReportCountsAndMeasuredMaxChange`).
The oracle is named in the report: `FoilSource.MaxOrdinateDeviation`, the maximum |Δy| over 2001 equal-x samples. That
is a sampled lower bound and is less reliable near the nose.

- ***Control → Anchor*** at vertex P.
  - Find u\* with x(u\*) = P.x by bisection on the monotone x; snap to an existing knot within 10⁻¹² relative.
  - Insert u\* up to multiplicity 5 by Boehm (5 − m insertions).
  - Move the interpolated vertex **and both handles** by Δy = P.y − C(u\*).y, and write a `smooth` row.
  - The 32-vertex ceiling refuses with `DSL-CURVE`, naming the ceiling.
  - The curve changes only **between the adjacent multiplicity-5 knots, or the ends**.
  - The other surface is untouched.
  - Measured on section-a upper point 4 (35 %, 6.5 %): 8 → 13 points; Boehm change 4.4 × 10⁻¹⁶ (the lens measured
    5.6 × 10⁻¹⁶); Δy = 0.88 % chord, which is the reported change. The one-sided curvature at the anchor is
    −6.29 / −1.73 per chord, so curvature breaks there (G1 at best) and the comb shows it.
- ***Insert anchor (keep shape)*** (Section ▾; the Rhino InsertKnot idiom): the Boehm step only, with no shift and no
  row. The change is 0 within 10⁻¹², and the result is a Corner anchor whose handles are still in line: the Kind readout
  says "Corner (handles in line)", so Corner is not read as a measured break.
- ***Anchor → Control***: delete the two handles and lower the multiplicity from 5 to 3. Only this surface changes, and
  it is C² at the remaining knot. No other surface is refit under DR-11.
  - Measured on the new anchor: 13 → 11 points, a change of 0.31–0.33 % chord. The lens's probe gave 0.31 %; the
    mockup's 800-sample measure gave 0.33 %.
- ***Tangent kind***: rewrite the row. If the kind is not already satisfied, the solver moves the handle(s) and reports
  the change. Under a vertical row, an x move moves all three vertices together.
- ***x moves*** change one surface only. The M1.1 paired-abscissa rule ends with per-surface bases.
- ***Insert / Delete*** act on one chosen surface. `RewriteCurves` writes per-side knots, and `PairAbscissa` is no
  longer applied.
- ***Fair / Rebuild*** act on each surface on **its own knots**. Anchors are kept as KKT rows (smooth, symmetric,
  horizontal and angle rows are linear in the ordinates with x fixed), and horizontal rows are snapped to exact equality
  after the solve (`SectionEdits_FairWithAnchorRows_AnchorsKeptRowsHold`, `SectionEdits_RebuildPerSurface_NoBasisCollapse`).
- ***Import .dat*** stays shared-basis in M1.2c (`DatImport.ProfileBlock`, as built). It replaces both surfaces, and
  the report says so.

**Pre-agreed fallback (§13 OD-4; track SPTF).** If the certificate fails, every operation above runs **paired**, as
ADR-0005 §6 words it:

- the other surface gets the same knots (exact);
- Anchor → Control refits the other surface on the affected segment only, refuses above 10 µm at the station's local
  chord, and reports the number;
- when a profile is shared by stations with different chords, the **largest** local chord sets the limit (the refit is on the chord-normalised profile, so its deviation in mm is the normalised deviation × chord; corrected 2026-10-03 from "smallest", found by the paired-mockup track);
- x moves stay paired.

### 3.5 The B6 restart — certifying per-surface bases (spike GSPK first)

Per-surface types put the two sides of one profile on different knot vectors and abscissae. Three proofs use the
shared-parameter difference today, and each needs an x-based replacement:

1. **separation** (upper > lower at equal x);
2. **the thickness maximum** max_x(u − l), which scales T;
3. **the Rule A blend maximum** max_x T0, the original B6 deferred after two measured cycles in M1.1.

**The approach to spike: an x-overlay certificate.** The x-range is split at every Bézier piece's end abscissa, then
refined by dyadic de Casteljau cuts where enclosures overlap. Each cell ("atom") encloses each side's parameter range
through its monotone x, its y hull, and the hull of u − l. Unresolved cells are refined best-first. Refusal is only for
the atom budget, the 32,768-bit ceiling or the 1 s budget, giving `GEOMETRY-BUDGET` and **Not assessed**, never
Certified. Two exact rules close the ends, where hulls cannot:

- **Nose:** a separating line through the origin, y = m·x. The upper first-span coefficients satisfy y_j − m·x_j ≥ 0,
  with the nose-handle coefficient strictly > 0, and the lower ≤ 0 with its nose-handle coefficient < 0. This covers
  drooped and high-camber noses where y = 0 fails (`Overlay_DroopedNose_SeparatedByLine`).
- **Trailing edge:** the mirror rule, a line through (1, y_te), y = y_te + m(x − 1). On the last spans, upper
  coefficients satisfy y_j − y_te − m(x_j − 1) ≥ 0, strict at the second-to-last, and lower ≤ 0. Without it, a closed
  TE (u = l = 0 at x = 1) can never be proved and every fixture would exhaust its budget: a false no-go
  (`Overlay_ClosedTrailingEdge_WedgeCertified`).

**The tolerance, settled by the existing proofs, not a model.** `PlacementWidth` and `BlendPlacementWidth` already
prove a whole-domain placed error ≤ 10⁻⁸ m at admission, and both consume the maximum's relative gap δ. They bound
thickness by 1 and use a 1/4 floor on max T0 (which multiplies the gap term by 16).

The lens's first-order reduction of those formulas (Inferred):

| Case | δ needed |
|---|---|
| single profile | δ ≤ 2 × 10⁻⁸ / c: 1.7 × 10⁻⁷ at 0.12 m, 2 × 10⁻⁸ at 1 m, 10⁻⁸ at 2 m |
| blend | δ ≤ 3.8 × 10⁻¹⁰ at 1 m and 1.9 × 10⁻¹⁰ at 2 m |

So M1.1's measured 3.3 × 10⁻¹⁰ (the +0.02 move) is borderline, and 2.8 × 10⁻⁶ (+0.20) fails. This design's earlier
model claimed δ ≈ 10⁻⁷ at 1 m; it was optimistic by about 450× for blends and is withdrawn.

GSPK therefore:

- uses **the admission proofs as its oracle**, with the achieved gaps (not sampled `PointAt`);
- derives δ per document from the certified chord and thickness hulls;
- runs F4 at 2 m;
- makes the three 10⁻¹² uses (`Bernstein.Maximum` :946, `BlendPlacementWidth` :540 and the depth bound :384) **one
  constant** (`Overlay_ToleranceConstant_SingleSourceAcrossProofs`).

Tightening the bounds themselves (the thickness-hull maximum instead of 1, the certified max T0 lower bound instead of
1/4) is a proof change, and it gets its own review if GSPK proposes it.

**The blend weight.** max T0 depends on the blend weight w, and today it is enclosed per query. An atom set built at
admission must bound the max T0 width ≤ δ **for all w ∈ [0, 1]**: T0 is linear in w, so the atoms that are candidates
for the upper envelope are refined. `QueryFeasibility` needs a bit and operation model for the stored atoms, within
32,768 bits and 10⁶ operations. Atoms are derived and recomputed on open, never persisted. **The overlay replaces the
difference path**, so there is one certification path for one quantity.

**GSPK go criteria** (all measured on this Mac, Release; a no-go is recorded with its numbers):

| Fixture | Must certify | Budget |
|---|---|---|
| F1 section-a, upper anchor at 35 % (§3.4), lower unchanged, closed TE | separation + max(u − l) + admission widths ≤ 10⁻⁸ m | ≤ 1 s, ≤ 4,096 atoms, ≤ 32,768-bit rationals |
| F2 NACA 0012 at 12 upper / 10 lower points (built by the probe per side, provenance recorded; no per-side fit exists yet) | same | same |
| F3 thin 6 % section, `closure open`, TE gap 0.2 % | same, incl. the open TE | same |
| F4 section-a ↔ an x-edited unique copy (+0.02 and +0.20 at vertex 3), **at 120 mm and at 2 m** | max T0 uniform in w; `BlendPlacementWidth` ≤ 10⁻⁸ m | same; `QueryFeasibility` within ceiling |
| F5 cambered section with a drooped nose and per-surface bases | separation by the nose line | same |

**Go**: GCRT builds the overlay into `Assess`, and SPT writes the per-surface operations (list SPTG). **No-go** (after
the spike, or at GCRT's two-cycle cap): OD-4, SPT writes the paired operations (list SPTF), and DR-11 goes back to the
operator with the numbers.

### 3.6 Why the placed Side section is not an edit surface (OD-1 C)

The Side view draws the **placed** section q = (x, C ± t(η)·T/2), rotated by twist and scaled by chord (ADR-0010). A
record point does not map to one placed point. Measured on section-a at 120 mm chord and 12 % t/c, and confirmed
independently by the Computational Geometry lens: raising record upper point 4 by 0.6 mm (0.5 % chord) moves the
placed **upper** surface 0.126 mm at 35 % chord and the placed **lower** surface 0.123 mm, upward.

Two effects stack:

- the record surface itself moves only 0.249 mm there, because an off-curve control point always moves the curve less
  than itself;
- the renormalization and the mixing of both surfaces add the rest.

A drag on the placed section would leave the pointer about 5× from the surface and pull the other surface along. So
the editor edits the **record** in chord coordinates, and shows the consequence: the Section group's per-station
t/c rows and the probe's "at Root … mm".

### 3.7 Section display path (ruling 2026-10-03; ADR-0010 Amendment 1; track DSP)

**Section drawing uses the fast display path.** The certificate decides validity; it does not draw.

- **One binary64 display profile evaluator.** `Placement.cs`'s private `Jet`, `ParameterFor` and `OrdinateAt` (the
  `SplineBasis` jet with abscissa inversion) become the shared internal `ProfileEvaluator`, beside
  `ChannelEvaluator`. Three readers use it, and nothing else may: `Placement.Prepare` (bits unchanged; the
  `Placement` golden master pins it), `AuthoringSession.Sample` / `ProfileView` (the strip thumbnails), and every
  M1.2c `Sections` projection (`View`, `Facts`, `Probe`, `Comb`, `DisplayCrossing`). **No fourth copy:** a source scan
  fails the build on another profile inversion in `src/`. The certificate keeps its Bernstein path: two evaluators,
  as note-20260926 and ADR-0010 decision 3 require.
- **Bound by test** to the certificate: every display sample is within **10⁻⁹ chord** of `Bernstein.EncloseAt`. The
  fixtures are section-a, a Rebuild-produced profile with non-dyadic knots, a C⁰ knot (multiplicity 5 on an anchor)
  and the LE vertical tangent (repeated zero abscissa). The display maximum (own t/c) stays inside the certified
  maximum.
- **Cosine spacing at the nose:** x = (1 − cos θ)/2 replaces uniform x/100, so the LE radius is drawn, not chorded.
- **Captions say "display"** on the editor plate and the probe. Finish, crossing refusals and every accepted shape
  follow `Geometry.Assess` only (§7).
- **Cost:** `ProfileAt` stops spending proof budget. The as-built 970 ms rebuild case becomes a binary64 loop:
  `Readiness_ProfileViewRebuilt_Under5Ms` measures it at readiness (Inferred until run).

## 4. Persistence

- **Source bytes, receipt and recovery:** §3.3. The old-build characterization receipt is SDR's.
- **Layout (`cfdw-layout` v1): no schema change.**
  - The Points pane's home moves to the **right side bar** (OD-3 B, ruled).
  - **One source of truth for pane homes:** `WorkspacePresets` reads `LayoutCodec.Homes` instead of repeating it
    (`Presets_DesktopEqualsCodec_EveryWorkspace`).
  - `"messages"` leaves the registered panes (OD-2 A, ruled). A saved layout naming it drops that pane with code
    `LAYOUT-PANE`. The other panes are kept, an emptied group is removed, and the first remaining pane becomes active.
    This is reversible, because `PlaceMissing` re-adds a re-registered pane.

## 5. Contracts

### 5.1 Exposed — Core (new unless marked)

```csharp
// Contracts.cs (PRE lands these before any track starts)
public enum SurfaceSide { Upper, Lower }
public abstract record SectionStep
{
    public sealed record Move(SurfaceSide Side, string VertexId, double X, double Y) : SectionStep;        // chord fractions
    public sealed record SetType(SurfaceSide Side, string VertexId, bool Anchor) : SectionStep;
    public sealed record InsertAnchor(SurfaceSide Side, double X) : SectionStep;                           // keep shape
    public sealed record SetTangent(SurfaceSide Side, string VertexId, TangentKind Kind, double? AngleDegrees,
        string? KeepHandleId) : SectionStep;
    public sealed record Insert(SurfaceSide Side, double X) : SectionStep;
    public sealed record Delete(SurfaceSide Side, string VertexId) : SectionStep;
    public sealed record Fair(SurfaceSide? Side, double Tolerance, PreserveEnds Ends) : SectionStep;       // null = each surface on its own knots
    public sealed record Rebuild(SurfaceSide? Side, int VertexCount, double Tolerance, PreserveEnds Ends) : SectionStep;
    public sealed record Import(byte[] Dat) : SectionStep;                                                  // both surfaces, shared basis
    public sealed record MakeUnique : SectionStep;
    public sealed record Thickness(ThicknessIntent Intent) : SectionStep;
}
public sealed record SectionStepReport(string Kind, double MaxChange, string MaxChangeOracle, int UpperPoints, int LowerPoints,
    ImportReport? Import, ThicknessProposal? Thickness, IReadOnlyList<string> RowsRemoved);
public sealed record SectionDraftView(string DraftId, string BaseAcceptedId, int Assignment, string Profile,
    SectionScope Scope, ThicknessIntent Intent, long Generation, int Cursor, int StepCount, byte[] Bytes, SectionStepReport? Last);

// AuthoringSession.cs (SDR) — same guards, memoization and one-draft rule as the as-built members
SectionDraftView BeginSectionDraft(string draftId, int assignmentIndex);
SectionDraftView ApplySectionStep(string draftId, long generation, SectionStep step);   // refuses a step that does not parse or pass structure
SectionDraftView UndoSectionStep(string draftId);    // no-op at Cursor 0
SectionDraftView RedoSectionStep(string draftId);    // no-op at the end
SessionAssessment AssessSection(string draftId, long generation, CancellationToken cancellation);   // full certificate
string? FinishSection(string operationId, SessionAssessment assessment);   // one accepted row; null when bytes equal base (acts as Cancel)
// Cancel, Snapshot, CaptureRecovery, ResumeRecovery: as built, now accepting the section draft.

// Placement.cs (DSP) — the one binary64 display profile evaluator (ADR-0010 Amendment 1); certificate unaffected
internal static class ProfileEvaluator
{
    internal static ProfileJet Jet(Curve curve, double t);              // x, y and their first and second t-derivatives
    internal static double ParameterFor(Curve curve, double x);         // abscissa inversion
    internal static double OrdinateAt(Curve curve, double x);
    internal static ProfilePoint[] Samples(Curve curve, int count);     // cosine-spaced at the nose
}

// SectionModel.cs (SPT) — derived views, binary64, display only, all through ProfileEvaluator
public static class Sections
{
    public static CurveView View(byte[] source, int assignment, SurfaceSide side, string basis, long generation);
    public static SectionFacts Facts(byte[] source, int assignment);   // own t/c and x; LE radius, TE gap and wedge — own AND per station (placed Rule A); t/c per station
    public static SectionProbe Probe(byte[] source, int assignment, double x);   // upper y, lower y, local t, camber, placed mm at the station
    public static (double X0, double X1)? DisplayCrossing(byte[] source, int assignment);
    public static IReadOnlyList<CombTooth> Comb(CurveView surface);    // analytic derivatives; one-sided teeth either side of each anchor
}
// SectionEdits.cs (SPT) — pure byte patches, one per step kind, each over the previous step's bytes
internal static class SectionEdits { internal static (byte[] Bytes, SectionStepReport Report) Apply(byte[] bytes, int assignment, SectionStep step); }
// PointModel.cs: PointRole += Nose, NoseHandle, TrailingEnd, TrailingHandle; TangentKind += Horizontal, Vertical, Angle;
//                PointView += double? AngleDegrees (non-positional, default null — seam S-6)
```

**Contract step (ADR-0007 §2, expand → migrate → contract).** The M1.1 single-vertex **writers** are deleted in one
change with their tests once the Desktop no longer calls them (CTL, seam S-3): `BeginProfileEdit`,
`UpdateProfileDraft` and `BeginProfile{Insert,Delete,Fair,Rebuild,Import}`. Every **reader** of their receipts and
recoveries stays (§3.3). `ProfileAt`, `DescribeScope` and `ProfileView` also stay, for the strip thumbnails and the
scope chip.

### 5.2 Exposed — Desktop

- **`WorkbenchController` (CTL).**
  - `SectionMode? Section` holds the draft view, Finish availability and its reason, and the last step report.
  - Mode: `EnterSectionAsync(int assignment, EntryOrigin origin)`, `FinishSectionAsync()`, `CancelSection()`.
  - Steps: `ApplySectionStepAsync(SectionStep)`, `UndoSectionStep()`, `RedoSectionStep()`.
  - `SectionCurve(SurfaceSide)` and `SectionChanged`.
  - `ResumeRecovery` branches on rail `"section"` and on the legacy profile rails, entering section mode (§3.3).
  - A gesture on a section point (`PointRef("upper"|"lower", id, profile)`) ends as **a step**, reusing the gesture
    machinery. The step ends on release, on KeyUp, or on focus loss or deactivation (ADR-0007 §3).
  - **No substitute assessor** (D7 is not triggered). The tests gate the real `AssessSection` with a
    `TaskCompletionSource`, and pass NotAssessed or disagreement as `SessionAssessment` values.
- **`ModelArea.Mode { Views, Section }` (EDT).**
  - In `Section` mode the views' frames hide, and so do the navbar's Views ▾ and Display ▾; Fit and Fit Selection
    stay.
  - The section editor body fills the model area.
  - `ModelArea` never references Dock (D3: `Architecture_DockConfinedToShell` stays green).
- **`SectionCanvas` (EDT)**, rewritten on `CurvePointLayer` with the x/c, y/c ↔ screen mapping. It has no events of
  its own and calls the controller like `PlanCanvas` does.
- **Commands (PNL)**, one `CommandTable` row each, in the menus and the palette:

  | Command | Shortcut | Notes |
  |---|---|---|
  | `section.edit` | Return on a selected station | "Edit section…" |
  | `section.finish` | ⌘↩ | |
  | `section.cancel` | — | |
  | `section.insert-point` | double-click on a surface | Section ▾ |
  | `section.insert-anchor` | — | "Insert anchor (keep shape)", Section ▾ |
  | `section.delete-point` | ⌫ | refused on named points with the reason |
  | `section.smooth`, `section.import-dat`, `section.make-unique` | — | Section ▾ |
  | `section.thickness-channel`, `section.thickness-source` | — | Section ▾ |
  | `view.thickness-x2` | — | |
  | `window.points` | — | |
  | `window.workspace-planform`, `-precision`, `-review` | ⌘1, ⌘2, ⌘3 | apply that workspace's preset (§11.8) |

  Every row either runs or names why it cannot.
- **`StatusStrip.Show(report, action?)` (PNL).** The one action slot (today it holds Try again) also carries
  **Show**. Show frames and selects what a blocker names. A blocker with no location says so: "This can't be shown on
  the section."
- **`PointsPane` and `PointsView.Build(controller) → PointsModel` (PNL)**, on the structure-B row kinds.

### 5.3 Consumed

| Contract | Source | Confidence |
|---|---|---|
| Boehm insertion and knot removal | `FoilSource.cs`:751 `InsertKnot`, :783 `RemoveKnot` | Verified (read) |
| `ConstrainedFit` (KKT; pin rows today) | `ConstrainedFit.cs`:330-341 | Verified (lens) |
| Per-side inverse-abscissa enclosure | `Geometry.cs`:909 `Bernstein.EncloseAt` | Verified (read) |
| Whole-domain admission proofs | `Geometry.cs`:479-573 | Verified (lens) |
| `EnsureHeader41`, `MakeIndependent`, `DescribeScope`, thickness proposal, DAT import | `FoilSource.cs`:166, :364; `AuthoringSession.cs`:180-186; `ThicknessFit.cs`; `DatImport.cs` | Verified (read) |
| `CurvePointLayer` mapping, glyphs and peers | `CurvePointLayer.cs` | Verified (read) |
| `Planform.Comb` (the anchor break teeth) | `PointModel.cs`:41-74 | Verified (read) |
| The x-overlay certificate | none yet | **Spike GSPK** before GCRT |

## 6. Patterns and structure

### 6.1 Named patterns

- **Command + Memento-by-bytes with a cursor** (ADR-0007). A step is a command whose memento is the full source bytes
  after it, and inner Undo moves the cursor. `simplify:` full byte copies; the ceiling is a draft measured over 64 MB,
  which is also the upgrade trigger.
- **State machine (controller):**

  | From | Event | To | Effect | Test |
  |---|---|---|---|---|
  | Views | Edit section | Editing | begin the draft; the mode replaces the views; focus on the first point | `SectionEditor_EnterFromSideDoubleClick_ModeShown` |
  | Editing | step | Editing | append; cursor++; latest-wins assessment | `SectionMode_GestureEnd_AppendsStepNotAcceptedRow` |
  | Editing | ⌘Z / ⇧⌘Z | Editing | cursor −1 / +1; at the ends a no-op that says so; **never document undo** | `SectionMode_UndoAtEntry_DocumentUndoDepthUnchanged` |
  | Editing | Finish (enabled) | Views | `FinishSection`: one row, the station selected; bytes equal to the base act as Cancel | `SectionMode_Finish_OneUndoStepStationSelected` |
  | Editing | Finish (disabled) | Editing | focus the reason | `SectionEditor_FinishWhileBlocked_FocusesReason` |
  | Editing | Cancel | Views | discard; undo depth unchanged | `SectionMode_Cancel_ViewsBackStationSelected` |
  | Editing | Escape | Editing / Views | drag → handle → point → selection; with edits, focus Cancel + COPY-119; with no edits and nothing selected, leave | `SectionEditor_EscapeCascade_DragPointSelectionThenLeave` |
  | Editing (edits) | strip thumbnail | Editing | refused: "Finish or cancel <station> before editing <other>." | `SectionEditor_StripSwitchWithEdits_RefusedByClick` |
  | Editing (no edits) | strip thumbnail | Editing | rebind the draft to the new station | `SectionMode_StripSwitchNoEdits_DraftRebindsToNewStation` |
  | Editing | Save | Editing | asks Finish or Cancel first; file bytes unchanged until then (SRC-07) | `SectionMode_SaveWhileDrafting_AsksFinishOrCancelFirst` |

- **Latest-wins async assessment**, using the monotonic ticket of M1.2b2's surface compute. It also respects the
  **status slot**: an assessment result never replaces a newer strip report (STATUS-CLOBBER).
- **Extract and reuse:** `CurvePointLayer` serves sections. There is one glyph path for rails, channels and sections,
  and `Planform.Comb`'s break-teeth pattern is reused for sections.
- **Presentation adapter** (as built).
- **Rejected:**
  - a Messages log (DR-STATUS-1, OD-2);
  - editing on the placed section (§3.6);
  - a runtime switch between paired and per-surface operations (S-2: one list is written);
  - a UI-side profile model;
  - steps stored as accepted rows;
  - showing anchor handles only on the selected anchor. The Marine-CAD lens suggested it (MC-12C-9) and it is declined:
    handles are vertices of the record, the M1.2b rails draw them always, and hiding them would make them unpickable.
    Fit Selection and ] / [ are the answer to overlap (§11.2).

### 6.2 Solution-Selection Ladder

- The draft is a list of byte arrays (stdlib).
- Patches reuse `FoilSource`, made per side.
- The canvas reuses `CurvePointLayer`, and the Points pane reuses the structure-B rows.
- The presets reuse `WorkspacePresets`, `ShowPane` and `ClosePane`.
- There is no new dependency.
- The one new algorithm is the overlay, and it is spiked first.

## 7. Error and concurrency model

- **One draft** (`DSL-DRAFT-OWNED`). While the mode is open, the views are hidden and the Wing block is read-only text
  (COPY-122). A palette point command aimed at a rail is refused, naming the section draft.
- **Codes** (all as built):
  - `DSL-PROFILE-TARGET`, `DSL-PROFILE-ORDER`, `DSL-PROFILE-CROSS`;
  - `DSL-LOCK`: a named point, a row violation, or Vertical on an interior anchor;
  - `DSL-CURVE`: the 32-vertex ceiling;
  - `DSL-GEOMETRY`: per-surface bases not assessed; and under the fallback, a neighbour's basis or a refit over 10 µm,
    with the number;
  - `GEOMETRY-BUDGET`: the overlay budget.
- **Concurrency.**
  - Steps apply on the UI thread. `assume:` a step's patch takes under 5 ms on the Example. This is confirmed by the
    `READINESS … value_ms=` line for `Readiness_SectionStepApply_Under5Ms` in the readiness receipt (measured, not a
    gate). If it misses, steps move off-thread under the same ticket.
  - The assessment runs on the thread pool and is cancelled by the next step. Display curves, the comb, the probe and
    the crossing marker are binary64. **Finish availability comes only from the certificate.** For the two
    disagreement cases, see `SectionAssess_DisplayCrossingVsCertificate_FinishFollowsCertificate`: display crossing +
    certified → Finish on; no display crossing + `DSL-PROFILE-CROSS` → Finish off.

## 8. Change-surface list (E7)

| Surface | What changes | Track |
|---|---|---|
| Store | FoilDSL bytes (per-surface knots, profile `tangents` rows, header 4.1); receipt `rail "section"`; recovery `"section"`; layout pane homes | SDR, SPT; PNL |
| Model | `IsAnchor` (degree-general); roles, freedoms and readouts; `TangentKind` +3; profile row rule; per-surface operations, `RewriteCurves` per side, `ConstrainedFit` anchor rows; x-overlay certificate | SPT, GCRT |
| Service | `AuthoringSession` section draft, Finish, Cancel, recovery and legacy recovery; deletion of the M1.1 writers | SDR; CTL via S-3 |
| Projection | `ProfileEvaluator` and `ProfileView` samples (DSP); `Sections.View`, `Facts`, `Probe`, `Comb`, `DisplayCrossing`; `SectionDraftView`; `SectionStepReport` | SPT, SDR |
| Client type | `SectionMode`, the `PointRef` reconcile, `ModelArea.Mode` | CTL, EDT |
| UI | mode bar, canvas, comb, probe, strip; Side double-click and Return; Properties rows and the Station link; Points pane; Show; commands and menus; presets | EDT, PNL |
| Compute readers | `Geometry.Assess` (overlay); `Placement` and `WingEstimates` (unchanged readers of the new bytes); CLI `inspect` (section points, types, kinds) | GCRT, SPT |

## 9. Failure-mode analysis

| Mode | Cause | Disposition | Detect | Test |
|---|---|---|---|---|
| Surfaces cross mid-draft | a move, a type change, Fair | **mitigate**: allowed in the draft; Finish off with COPY-123; marker; strip warning + Show | `section.assess` code `DSL-PROFILE-CROSS` | `SectionMode_Crossing_FinishDisabledWithReason` (CTL), `SectionEditor_Crossing_MarkerAndReasonRendered` (EDT) |
| Crossing cleared | Undo or a move | **detect**: COPY-124 rendered and spoken once | — | `StatusStrip_CrossingCleared_Copy124RenderedOnce` (PNL) |
| Certificate not assessed | overlay budget; bit ceiling | **mitigate**: Finish off with COPY-182; never shown as accepted | `section.assess` status, atoms, bits | `SectionAssess_OverlayBudgetExhausted_NotAssessedNeverCertified` (GCRT), `SectionMode_NotAssessed_FinishDisabledWithReason` (CTL, value-typed assessment) |
| Stale assessment | slow proof, newer step | **prevent**: generation ticket; the older result is dropped and emits `superseded` with no duration | `section.assess` outcome | `SectionMode_StaleAssessment_Dropped` (CTL) |
| Assessment overwrites a newer report | async writer to the strip slot (STATUS-CLOBBER) | **prevent**: slot staleness check | — | `SectionMode_AssessCompletion_NewerStripMessageSurvives` (CTL) |
| Display crossing ≠ certificate | binary64 near tangency | **prevent**: Finish follows the certificate only | — | `SectionAssess_DisplayCrossingVsCertificate_FinishFollowsCertificate` (CTL) |
| Display profile drifts from the certificate | a change to the shared evaluator | **detect**: binding test ≤ 10⁻⁹ chord on four fixtures | — | `ProfileEvaluator_BoundToCertificate_Within1e9Chord` (DSP) |
| A fourth binary64 profile evaluator appears | a projection inverts on its own | **prevent**: single-site source scan | — | `ProfileEvaluator_SingleSite_NoOtherProfileInversionInSource` (DSP) |
| Drawing spends proof budget (≈ 970 ms on a rebuild) | the certificate used as a display | **prevent**: the display path only | readiness `READINESS` line | `ProfileView_RebuiltProfile_NeverUsesProofBudget` (DSP) |
| ⌘Z at the entry falls through to document undo | routing | **prevent**: inner undo owns ⌘Z in the mode | — | `SectionMode_UndoAtEntry_DocumentUndoDepthUnchanged` (CTL) |
| Undo or redo past the ends | cursor 0 or end | **prevent**: no-op, "Nothing to undo in this section." | — | `SectionDraft_CursorEnds_UndoRedoNoChange` (SDR) |
| Step refused | order, lock, ceiling | **prevent**: no step; draft unchanged; code in the strip | `section.step` outcome + code | `SectionDraft_RefusedStep_DraftBytesUnchanged` (SDR) |
| Named point edited | drag or type on the nose or a closed TE point | **prevent**: Fixed freedom; read-only Type with its constraint | — | `SectionPoints_NoseAndClosedTrailingEnd_Fixed` (SPT) |
| Vertical chosen on an interior anchor | Kind list | **prevent**: disabled with its reason | — | `SectionPoints_VerticalOnInteriorAnchor_DisabledWithReason` (SPT) |
| 32-vertex ceiling | Control → Anchor or Insert near 32 | **prevent**: `DSL-CURVE`, names the ceiling | `section.step` code | `SectionEdits_AnchorPast32Points_RefusedNamingCeiling` (SPT) |
| Other surface changed by a per-surface step | a shared-basis path left in place (RewriteCurves, PairAbscissa, Rebuild) | **prevent**: per-side knots in every patch | — | `SectionEdits_EveryStepKindOnPerSurfaceProfile_OtherSurfaceBytesIdentical` (SPTG) |
| Unreported change from a type or tangent step (Silent-Refit class) | step without a measurement | **prevent**: `MaxChange` + oracle on every SetType/SetTangent report; the test is policy-free (counts asserted against the bytes, never "lower unchanged") | — | `SectionEdits_TypeAndTangentSteps_ReportCountsAndMeasuredMaxChange` (SPT) |
| Anchor created off its point or non-locally | wrong u\* or Δy | **prevent**: passes through P within 10⁻¹²; unchanged outside the adjacent multiplicity-5 knots | — | `SectionEdits_ControlToAnchorBetweenTwoAnchors_OutsideUnchanged` (SPT) |
| Profile row violated in a hand-edited file | `tangents` row does not hold | **detect**: Assess Invalid `DSL-LOCK` with the row id | open diagnostics | `Assess_ProfileHorizontalRowUnlevel_InvalidDslLock` (SPT), `Assess_ProfileRowsJustOutsideTau_InvalidDslLock` (SPT) |
| Cusp passes a row check | same-side handles | **prevent**: ordering conditions | — | `Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock` (SPT) |
| Closed TE never proved | hulls meet at x = 1 | **prevent**: TE wedge rule | — | `Overlay_ClosedTrailingEdge_WedgeCertified` (GCRT) |
| Tolerance relaxed in one proof only | three 10⁻¹² constants | **prevent**: one constant | — | `Overlay_ToleranceConstant_SingleSourceAcrossProofs` (GCRT) |
| Save while drafting | ⌘S | **prevent**: asks Finish or Cancel; bytes unchanged until then | — | `SectionMode_SaveWhileDrafting_AsksFinishOrCancelFirst` (CTL) |
| Escape with edits | Escape | **prevent**: nothing discarded; focus Cancel + COPY-119 | — | `SectionEditor_EscapeWithEdits_FocusCancelNothingDiscarded` (EDT) |
| Crash mid-draft, incl. after Make unique | process ends | **recover**: resume the latest bytes; the project opens; inner undo empty and the copy says so | recovery events | `ReopenSectionDraft_MakeUniqueThenCrash_OpensAndResumes` (SDR), `Controller_ResumeSectionRecovery_EntersSectionMode` (CTL) |
| Legacy profile recovery after the contract step | project from an older build | **recover**: resumes as a section draft | — | `Reopen_LegacyProfileRecovery_ResumesAsSectionDraft` (SDR), `Reopen_LegacyInsertDeleteFairRebuildReceipts_StillCheck` (SDR) |
| Old build opens new files | downgrade | **accept** (one-way, ADR-0005/0007): refused or read-only, file unchanged | — | proof receipt `docs/proof/m12c-old-build/` (SDR exit) |
| No-op Finish | bytes equal the base | **prevent**: no row (acts as Cancel) | — | `SectionDraft_FinishBytesEqualBase_NoRow` (SDR) |
| Finish adds more than one row, or Cancel adds one | wiring | **prevent** | `section.finish` / `section.cancel` | `SectionDraft_FinishSixSteps_OneAcceptedRow` (SDR), `SectionDraft_Cancel_UndoDepthAndBytesUnchanged` (SDR) |
| Make unique, then Cancel | copy left behind | **prevent**: the copy is a step | — | `SectionDraft_MakeUniqueThenCancel_NoCopyLeft` (SDR) |
| Thickness proposal infeasible | Use source thickness | **mitigate**: Finish off (as built, B3) | `section.assess` | `SectionDraft_UseSourceInfeasible_FinishBlocked` (SDR) |
| Paired fallback refit over 10 µm | OD-4 only | **prevent**: refused with the number | `section.step` code | `SectionEdits_PairedAnchorToControlRefitOverLimit_Refused` (SPTF) |
| Points pane value invalid | typing | **prevent**: field error at the field (UI-39); no step | — | `PointsPane_InvalidValue_FieldErrorNoStep` (PNL) |
| Dead control (UI-DEAD-CONTROL; F-1's class) | a mode-bar button, link, thumbnail, Show or Save-prompt button with no handler | **prevent**: the sweep walks the section mode and the Save prompt | — | `SectionCommands_EveryRow_RunsOrNamesReason` (PNL) |
| Unwired canvas event (TEST-UNWIRED-EVENT) | an event nothing applies | **prevent**: the canvas calls the controller; allow-list entries removed | `check-event-subscribers.py` | `SectionCanvas_DragUpperPoint_StepAppliedInController` (EDT) |

## 10. Telemetry (normal path, no flag)

| Question | Source |
|---|---|
| How long does checking a step take? | `AuthoringSession` event `section.assess`: duration, status, code, atoms, maximum rational bits |
| Steps per Finish; how long a visit lasts | `section.finish`: step count, ms since begin, outcome |
| How often Finish is blocked, and why | `section.assess` code distribution; `section.mode.finish-blocked` |
| Which entry path is used (does OD-1 matter?) | `section.mode.enter`: origin ∈ properties · plan · side · browser · palette |
| What gets discarded | `section.cancel`: steps discarded |
| Does a step hold up the UI? | `section.step`: kind, duration, outcome, code |

- **Fields:** the existing `SessionEvent` (operation, outcome, durationMilliseconds, generation), plus
  `edit_kind = section`, the step kind and `independent`.
- **Never in an event:** profile names, ids or positions (a profile name is free text). Enforced by
  `SectionTelemetry_Events_NoNamesIdsOrPositions` (SDR).
- **Degrade rule:** a cancelled assessment emits `superseded`, never a duration.
- **No HTTP surface.**
- **Tests:** `SectionTelemetry_FinishEvent_CarriesStepsAndDuration` (SDR), `SectionTelemetry_EnterEvent_CarriesOrigin`
  (CTL).

## 11. UI and interaction design

**Medium:** native desktop, macOS first.

**Archetype:** G1 Parametric Modeling Workbench. The section editor is a modal sub-workspace (Fusion "Finish Sketch",
Shape3D section edit).

**Motion:** none.

**Tokens:** no new colour tokens. UXR adds `spacing.mode-bar` (32 px) and `spacing.station-strip` (84 px) to DESIGN.md
before EDT uses them.

**TQ rules:** every number has a unit, precision follows the quantity, and display versus certified is stated (§7).

### 11.1 Key screens (the mockup draws each)

1. **Side → Edit section.** Four views. The Root section is selected in Side (3 px station outline and chip).
   Properties shows the Station group, ending in the **Edit section…** link. The strip reads "Selected Root station.
   Return or double-click the section to edit it."
2. **Section editor** (the model area, top to bottom):
   - **Mode bar** (32 px): "Editing **Root** section", the scope chip, Section ▾, Curvature (pressed by default),
     Thickness ×2 (off by default), a spacer, Cancel, **Finish section**. The focal point is Finish.
   - **Canvas** (1:1), with the plate "Section · Root · 0.00 mm from root · display" (ADR-0010 Amendment 1):
     - the chord axis, a 10 % grid and the chord line;
     - both curves (2 px `foil`);
     - each surface's dashed control polygon, the active one at full opacity and the other at 0.62 (UI-25);
     - the entry shape of both surfaces as a 1 px dashed `viewport-mute` ghost;
     - the comb on both surfaces.
   - **Tracing probe** (top right, A4.9), following the pointer: "Pointer x 42.00 % · upper 6.18 % · lower −5.46 % · t
     here 11.64 % · at Root 13.83 mm (12.00 % t/c)". It shows the focused point when the pointer is outside, and during
     a drag it shows Δx/Δy from the gesture start plus the live own t/c and its x.
   - **Station strip** (84 px): a thumbnail per station with its name, distance from the root, chord and t/c, the
     current one underlined (`aria-current`).
   - Navbar: Fit and Fit Selection.
3. **Blocked Finish.** The crossing marker; Finish off (`aria-disabled`) with its reason tied by `aria-describedby`;
   the strip warning with Show.

### 11.2 Glyphs, comb and spacing

- **Glyphs** reuse M1.2b §11.2 and UI-37 (contrast measured there on `viewport` #17272c):
  - anchor: 12 px hollow square;
  - control point: 11 px filled circle;
  - nose and trailing-edge points: 14 px diamond;
  - handle: 9 px hollow circle on a 1 px line.
  - Selection is shown by fill and shape. Focus ring r 13; hover ring r 10; hit radius 14.
  - Crossing marker: 4 px dashed `danger-viewport`.
- **Comb** (on by default in the mode):
  - teeth point outward, **spaced evenly by arc length** (the M1.2b `Planform.Comb` contract; the mockup samples by
    parameter), with length proportional to κ from the analytic B-spline derivatives (never finite differences);
  - **auto-scaled** so the 90th-percentile tooth is 30 px; teeth over 60 px are clipped and marked (×);
  - the plate states the scale and the clipped count;
  - one-sided teeth on either side of each anchor, plus a dashed station-coloured break mark (the M1.2b
    `Comb_Anchor_TwoOneSidedTeeth` pattern);
  - computed at 1:1, and mapped through ×2 when Thickness ×2 is on (the plate says so).
- **Thickness ×2:** off by default. It scales y in the drawing only and shows the plate "Thickness drawn ×2". When it
  is on, the angle rows say "Angles are not drawn to scale while Thickness ×2 is on." Values are never scaled.
- **Spacing**, measured in the mockup at 1:1: a degree-5 anchor's nearer handle is 1.35 % chord away. At full chord its
  nearest neighbour is **8.7 px** away with the Points pane on the right, or **11.9 px** without it, so the 12 px glyphs
  overlap.
  - **Rule:** Fit Selection (F) on an anchor frames it with both handles ≥ 24 px apart. The mockup measures 24.7 px.
  - Hover and selection paint over a neighbour; the nearest centre wins, and the selected point wins ties; ] and [
    reach every point.
  - Test: `SectionEditor_FitSelectionOnAnchor_HandlesAtLeast24PxApart` (EDT).

### 11.3 Keyboard and pointer map (every pointer verb has a keyboard path)

| Verb | Pointer | Keyboard |
|---|---|---|
| Enter the editor | double-click a section (Side) or a station chip (Plan); the Properties link | Return on a selected station (Plan, Side, Browser); palette "Edit section" |
| Pick among overlapping Side sections | click picks the outline nearest the pointer; a second click at the same spot picks the next | ] / [ in the Side view walk the stations |
| Select a point | click | Tab into the canvas (the selected point, or the nose); ] / [ walk nose → upper LE→TE → lower LE→TE, handles after their anchor |
| Move | drag (Shift locks the axis) | arrows: 0.1 % · ⌘ 0.01 % · ⇧ 1 % chord; a key run is one step (KeyUp, focus loss) |
| Type a value | — | **Return → the point's x (a handle's Angle)**; Tab leaves the canvas to Type (DR-NAV-1); x accepts `mm` (converted at the edited station's chord, echoed in % in the strip); y refuses `mm` with "y is a fraction of the chord. The station's t/c sets the built thickness." |
| Insert / delete a point | double-click on a surface inserts there | ⌫ deletes the selected point (refused on named points with the reason) |
| Point type, kind | right-click (Control-click): Make anchor · Make control · Tangent ▸ | Properties Type ▾ and Kind ▾ (DR-CELL-2 commit rules) |
| Inner undo, redo | — | ⌘Z, ⇧⌘Z (a text field takes them first, as built) |
| Finish · Cancel | buttons | ⌘↩ Finish; Escape steps back (§6.1); Cancel is a button and a Section ▾ row |
| Curvature comb | toggle | C with the canvas focused (2.1.4: the key is scoped to the view) |
| Station strip | click a thumbnail | arrows along the strip (roving tabindex); Return switches |
| Fit · Fit Selection · pan · zoom | navbar, wheel, two-finger pan, pinch | ⌘0 / F; ⌥+arrows; ⌘= / ⌘− (DR-DEN-4) |

### 11.4 Properties (structure B), the Points pane and copy

**A section point** has an identity such as "Upper surface · point 7 of 13". Its groups:

- **Point:** Type ▾ (Anchor point / Control point; COPY-117 under Control); **x** (% c, 0.01, nudge); **y** (% c,
  0.01, nudge).
- **Tangent** (anchors and handles only): Kind ▾ (Vertical disabled on an interior anchor); for each handle, its
  **Angle** (°, 0.01) and **Length** (% c); COPY-183 as help.
- **A named point:** Type read-only with its constraint (COPY-177 or COPY-178), plus its readouts.

**The Section group** (always present in the mode), with the summary "section-a · degree 5":

| Row | Value |
|---|---|
| **Own t/c** | "12.12 % at 34.88 % c" (one precision, 0.01) |
| **t/c at <station>** | one row per station that uses the section, 0.01 % |
| note | "From the Thickness curve. At each station this shape is scaled to that t/c." |
| **Station t/c** ▾ | "From the Thickness curve" / "From this section". The as-built thickness intent. Choosing "From this section" shows CAD-10's channel targets and the affected span **when it is chosen** |
| **LE radius own** · **LE radius at <station>** | upper · lower, % c; a note when they differ |
| **TE gap own · <station>** | % c |
| **TE wedge own · <station>** | ° |
| **Points** | "13 upper · 8 lower" |

**The Wing block** stays last, as read-only text with COPY-122 (CAD-17).

**The Station group** (in the views) ends with the link "Edit section…".

**The Points pane** (PNL) uses the structure-B row look: twirl groups, 24 px rows, accent values with a dotted
underline, tabular figures.

- **In the mode**, it is headed **"Control net · degree 5"**, with groups Upper surface and Lower surface and columns
  Point · Type · x (% c) · y (% c) · Kind.
- **In the views**, its groups are the five curves, with columns Point · Type · From root (mm) · value (the curve's
  unit) · Kind.
- Typed edits follow the Properties commit rules: one step in the mode, one undo step in the views.
- Selection works both ways.
- Empty: "No foil open".

**New copy** (proposed COPY-172 to COPY-184; UXR records them in DESIGN.md §7):

| ID | String | Where |
|---|---|---|
| 172 | "Edit section…" | Station link, palette |
| 173 | "Editing <station> section" | mode bar |
| 174 | "Upper point <n> is now an anchor (now point <m> of <N>). Upper surface <a> → <b> points; lower unchanged. Largest change <d> % chord. Curvature now breaks at <x> % chord." (and the mirror for lower) | strip |
| 175 | "Upper point <n> is now a control point. Upper surface <a> → <b> points. Largest change <d> % chord." | strip |
| 176 | "Moved upper point <n> by <d> % chord. Own t/c <t> %; <station> stays <s> % t/c." | strip |
| 177 | "The nose is always an anchor. It stays at the leading edge with a vertical tangent." | Type, read-only help |
| 178 | "The trailing-edge point is always an anchor. It moves up and down only." / closed: "… It stays on the chord line: the trailing edge is closed." | Type, read-only help |
| 179 | "Finished <station> section: <n> changes in one undo step." | strip |
| 180 | "Cancelled. <station> section is as it was." | strip |
| 181 | "Nothing to undo in this section." | strip |
| 182 | "This section couldn't be checked, so Finish is off. <reason> Undo the last change or try another." | Finish reason |
| 183 | "Angles are in the section's own chord coordinates. A flat crest stays flat at a station only if the section is symmetric or uses its own thickness." | Tangent help |
| 184 | "A vertical tangent inside a surface makes a step. The nose already has one." | disabled Vertical, reason |

Reused: COPY-117, COPY-119, COPY-122, COPY-123, COPY-124, and "Finish or cancel <station> before editing <other>."

### 11.5 Component states

| Component | default | hover / focus | disabled | loading | empty | error | success | overflow |
|---|---|---|---|---|---|---|---|---|
| Mode bar | title, chip, tools, Cancel, Finish | ring on each control | Finish `aria-disabled` + reason | "Checking…" chip while assessing | — | Finish reason (COPY-123 / 182) | COPY-179 in the strip | the chip truncates with a tooltip; buttons never truncate |
| Canvas | curves, polygons, glyphs, ghost, comb, probe | hover ring; focus ring | read-only while Finishing | last drawing kept while assessing | — | crossing marker | — | Fit keeps the whole chord visible |
| Station strip | thumbnails with name, distance, chord, t/c | ring; roving | switch refused with edits (copy) | — | one station still shows | — | — | scrolls horizontally |
| Section group | rows | DR-CELL rules | — | "Checking…" | — | field error at its field | report in the strip | the selection section scrolls; the Wing stays pinned (DR-UID-5) |
| Points pane | groups and rows | DR-CELL rules | the views' rows read-only in the mode | — | "No foil open" | field error | report in the strip | the grid scrolls inside its pane |

### 11.6 Accessibility (cheap floors, per the operator's priority) and performance

- Points are `CurvePointLayer` peers, named by surface, index, type and x/y in % c (UI-37). The test
  `SectionEditor_FocusedPoint_AccessibleNameSurfaceIndexTypeXY` checks this.
- The crossing alert speaks only on a change (UI-43). Thickness ×2 and Curvature are pressed toggles.
- Targets are 24 px; type is 11 px.
- The strip is the one polite region.
- Drag frame time is measured at readiness, not gated (§12.3).

### 11.7 Measured against v10 (operator-approved)

- v10's app-bar toolbar becomes a mode bar in the model area, because today's shell has no app toolbar (ADR-0009).
- The station strip is kept, and gains each station's chord and t/c.
- "Nose plus three points per surface" is mockup scope, not the record (ADR-0007 §4).
- v10's t/c chip becomes the Section group rows and the probe.

### 11.8 Precision workspace

⌘1 / ⌘2 / ⌘3 apply the preset of that workspace: the panes per `WorkspacePresets` (which now reads
`LayoutCodec.Homes`), and the view arrangement (Review = Four views, no panes). Precision = Planform plus the Points
pane in the right side bar (OD-3 B).

`simplify:` there is no per-workspace memory and no persistence of the switch. The ceiling is that a user's changes to
a workspace are not remembered on return. The upgrade trigger is app-shell D4 (M1.2e), which owns memory and saved
layouts.

## 12. Test plan

### 12.1 Triggered directives

| Trigger | Where | Directive |
|---|---|---|
| — | all | **D0** `Method_State_Outcome`, AAA, no sleeps (gates are `TaskCompletionSource`), no wall clock in the fast ring, fixtures from bytes |
| T1 | step patches, roles, readouts, row rule, overlay, Properties and Points rows | **D1** exact values and boundary rows: 31/32/33 points; 8→7 delete accepted and 7 refused; multiplicity 4/5/6; τ_s just inside and just outside per kind; open versus closed TE; cursor 0 and the cursor at the end |
| T2 | per-surface and paired operations, overlay | **D2** fixed-seed property tests: random sections (6–32 points per side, random anchors) keep the other surface byte-identical (SPTG) or the knots equal (SPTF), keep locality, and the overlay encloses binary64 |
| T3 | `ModelArea.Mode` composition | **D3**: `Architecture_DockConfinedToShell` stays green; `ModelArea` does not reference Dock |
| T4 | Finish, save, reopen; recovery; legacy recovery | **D4** real temp files through `ProjectStore` |
| T7 | receipts; CLI `inspect --json` | **D6** synthetic golden fixtures; forged `"section"` receipts are refused |
| T8 | assessment in controller tests | **D7 not triggered**: no substitute; the real `AssessSection` is gated, and values are passed |
| — | rendered surface (UI-RENDERED-STATE) | whole realized window; targets through `TranslatePoint`; pixels read there |

### 12.2 Harness

PRE adds:

- the Core classes and their `Run()` lines;
- a Desktop `--section-editor` suite in the default spawn list;
- a Core `--readiness` entry, never spawned by `run-tests.sh`.

Each track brief runs `tools/run-tests.sh`, then `python3 tools/check-named-tests.py <track> --design
docs/design/m12c-section-editor.md`. **SPT's brief runs SPT plus the list the GSPK verdict picked (SPTG or SPTF).** At
the verdict, the other list's names are cut from §12.4 (HYG-A).

### 12.3 Tiers

1. **Core:** SPT (with SPTG or SPTF), SDR, GCRT.
2. **Controller:** CTL.
3. **Rendered** (UI-RENDERED-STATE): EDT, PNL. A "differs" oracle is not enough, and state-only assertions do not count
   as proof of a visible behaviour. The `…_ModeShown` tests assert the focused element is the first point, the canvas
   has curve pixels at projected positions, and the Plan is not effectively visible.
4. **Action** (UI-DEAD-CONTROL): PNL extends the existing `UI_DEAD_CONTROL_ShellButtonsHaveActions` walk to the
   section mode and the Save prompt. This is a new state on an existing name.
5. **Native:** UXR rows N-12C-1 … N-12C-11 (the §0.1 steps), each with an attach receipt (CO-UI-READY). These rows
   also cover VoiceOver speech and the comb drawing.
6. **Readiness** (measured, not gated per track; gathered by UXR):
   - `Readiness_ProfileViewRebuilt_Under5Ms` (Core `--readiness`);
   - `Readiness_SectionStepApply_Under5Ms` (Core `--readiness`);
   - `Readiness_SectionAssessExample_Under50Ms` (Core `--readiness`);
   - `Readiness_SectionDragFrameP95Under16Ms` (Desktop `--readiness`).

   Each prints a `READINESS … value_ms=` line into the readiness receipt.

**Red first:** the GCRT F-fixture tests are red today, because `Assess` refuses differing bases. The equivalence and
property names (`SectionEdits_RandomSections…`, `Overlay_RandomSections…`, `Parse_ChannelHorizontalRow_StillDslLock`)
go red through a **planted-mutant receipt** in the SPT and GCRT Proof Packs, as M1.2b2's PL0 did.

**The S-3 deletion ledger** is in CTL's proof pack. Every M1.1 behaviour (shared flow, cancel, fixed vertex, draft
status names the station, Insert/Fair/Import/UseSource reports, the canvas's accessible text) points at a §12.4 name
or reads "behaviour retired". Two rows are retired outright: `Shell_F9_SectionSelectedStation_DrawsProfile` and
`_NoStation_ShowsEmptyCopy`, because the Section tab is gone.

The UI craft gate runs on the mockup now, and on the built surface at UXR, with the CD12 floors.

### 12.4 Named tests (the ledger; the checker reads this section and §9)

Each name protects one behaviour. Names that protect nothing are not listed.

**DSP — the display profile evaluator (ADR-0010 Amendment 1).**
`ProfileEvaluator_BoundToCertificate_Within1e9Chord` (DSP) · `ProfileEvaluator_DisplayMaximum_WithinCertifiedMaximum` (DSP) ·
`ProfileEvaluator_SingleSite_NoOtherProfileInversionInSource` (DSP) · `ProfileView_Samples_CosineSpacedAtNose` (DSP) ·
`ProfileView_RebuiltProfile_NeverUsesProofBudget` (DSP) · `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged` (DSP).

**SPT — section points in Core, policy-free.**
`IsAnchor_DegreeFive_OnlyMultiplicityFiveTrue` (SPT) · `Parse_ProfileHorizontalRowOnAnchor_Accepted` (SPT) ·
`Parse_ChannelHorizontalRow_StillDslLock` (SPT) · `SectionPoints_RolesOnExample_NoseHandlesControlsTrailingEnds` (SPT) ·
`SectionPoints_NoseShared_OnePointTwoHandles` (SPT) · `SectionPoints_OpenTrailingEnd_YOnly` (SPT) ·
`SectionPoints_SolverOutput_PassesItsRowCheck` (SPT) · `Sections_Facts_LeRadiusPerSurfaceMatchesOracle` (SPT) ·
`Sections_Facts_TrailingEdgeWedgeAndGap` (SPT) · `Sections_Facts_LeRadiusAndWedgeAtStation_MatchPlacedRuleA` (SPT) · `Sections_Probe_PlacedThicknessAtStation` (SPT) ·
`SectionComb_AfterMakeAnchor_BreakMarkerAtAnchor` (SPT) · `SectionEdits_HorizontalKind_HandlesLevelExactly` (SPT) ·
`SectionEdits_SymmetricKind_EqualHandleLengths` (SPT) · `SectionEdits_AngleKind_HandlesOnRaysWithinTau` (SPT) ·
`SectionEdits_InsertAnchorKeepShape_ShapeExactCornerAnchor` (SPT) · `SectionEdits_ControlToAnchorBetweenTwoAnchors_OutsideUnchanged` (SPT) ·
`SectionEdits_DeleteBelowSevenPoints_Refused` (SPT) · `SectionEdits_FairWithAnchorRows_AnchorsKeptRowsHold` (SPT) ·
`SectionEdits_FirstRow_HeaderBecomes41` (SPT) · `Assess_ProfileVerticalRowOffByOneUlp_Invalid` (SPT) ·
`Assess_ProfileSmoothRowNearVertical_Certified` (SPT) · `Cli_Inspect_ListsSectionPointTypesAndKinds` (SPT).

**SPTG — per-surface operations (written only on a GSPK go). CUT 2026-10-03: GSPK no-go (S-2, OD-4 a); `docs/proof/m12c-certificate-spike/verdict.md`.**
`SectionEdits_UpperMove_LowerBytesIdentical` (SPTG) · `SectionEdits_UpperAnchor_LowerBytesIdentical` (SPTG) ·
`SectionEdits_InsertOnUpper_ShapeExactLowerUnchanged` (SPTG) · `SectionEdits_AnchorToControl_MultiplicityThreeLocalOnly` (SPTG) ·
`SectionEdits_RebuildPerSurface_NoBasisCollapse` (SPTG) · `SectionEdits_RandomSections_OtherSurfaceAndLocalityHold` (SPTG).

**SPTF — paired operations (written only on a no-go, OD-4).**
`SectionEdits_PairedAnchor_BothSurfacesSameKnotsExact` (SPTF) · `SectionEdits_PairedXMove_BothSurfacesSameAbscissa` (SPTF) ·
`SectionEdits_PairedAnchorToControl_RefitLocalWithin10Um` (SPTF) · `SectionEdits_PairedRefitSharedProfile_LargestChordSetsLimit` (SPTF) ·
`SectionEdits_RandomSectionsPaired_KnotsEqualAndLocalityHold` (SPTF) ·
`SectionEdits_PairedSetTangent_BothSurfacesSameKind` (SPTF) · `SectionEdits_PairedToAnchorWithKind_PartnerRowWritten` (SPTF) · `SectionEdits_PairedToControl_PartnerRowRemoved` (SPTF).

**SDR — the section draft in Core** (fixtures are shared-basis until GCRT merges).
`SectionDraft_Begin_CursorZeroBytesEqualBase` (SDR) · `SectionDraft_StepThenUndoThenRedo_BytesRestoredExactly` (SDR) ·
`SectionDraft_StepAfterUndo_DropsRedoTail` (SDR) · `SectionDraft_SecondDraftWhileOpen_DslDraftOwned` (SDR) ·
`SectionDraft_FinishUncertified_Refused` (SDR) · `SectionDraft_FinishThenUndo_SourceAndAssignmentsRestored` (SDR) ·
`SectionDraft_SharedFinish_BothAssignmentsReadNewProfile` (SDR) · `SectionDraft_MakeUnique_RootGetsCopyTipKeepsSource` (SDR) ·
`SectionDraft_UseSourceThickness_ReportsTargetsAndAffectedEta` (SDR) · `SectionDraft_ImportStep_ReportsResidualAndProvenance` (SDR) ·
`SectionDraft_FairStep_ReportsAchievedDeviation` (SDR) · `Receipt_SectionRecovery_RoundTripsExpandOnly` (SDR) ·
`ReopenSectionDraft_FinishedSection_TypesAndKindsAsSaved` (SDR) · `FinishSection_SameOperationIdAfterReopen_Idempotent` (SDR) ·
`SectionTelemetry_FinishEvent_CarriesStepsAndDuration` (SDR) · `SectionTelemetry_Events_NoNamesIdsOrPositions` (SDR) ·
`Receipt_SectionRailWithCurveOrRule_DocReference` (SDR) · `Receipt_SectionMakeUnique_ChildOnlyName_Accepted` (SDR) ·
`Receipt_ForgedSectionRailUnknownProfile_DocReference` (SDR).

**GCRT — the per-surface certificate (only on a GSPK go; depends on SPT and SDR).**
`Overlay_F1UpperAnchor_SeparationAndThicknessCertified` (GCRT) · `Overlay_F2Naca0012TwelveTen_Certified` (GCRT) ·
`Overlay_F3OpenThinTrailingEdge_Certified` (GCRT) · `Overlay_F4BlendXEditedCopy_MaxT0Certified` (GCRT) ·
`Overlay_BlendAtTwoMetreChord_AdmissionWidthWithin10Nm` (GCRT) · `Overlay_MaxT0Enclosure_UniformInWeight` (GCRT) ·
`Overlay_QueryFeasibility_AtomBitsWithinCeiling` (GCRT) · `Overlay_DroopedNose_SeparatedByLine` (GCRT) ·
`Overlay_SharedBasisInput_SameStatusMutuallyEnclosingMaxima` (GCRT) · `Overlay_CrossingSides_InvalidDslProfileCross` (GCRT) ·
`Overlay_RandomSections_EnclosureContainsBinary64` (GCRT) · `SectionDraft_UpperAnchorFinish_CertifiedReopensPerSurfaceKnots` (GCRT).

**CTL — the controller and the mode.**
`SectionMode_Enter_DraftBoundFirstPointSelected` (CTL) · `SectionMode_GestureEnd_AppendsStepNotAcceptedRow` (CTL) ·
`SectionMode_UndoRoutedToInnerStep_DocumentUndoUntouched` (CTL) · `SectionMode_Finish_OneUndoStepStationSelected` (CTL) ·
`SectionMode_Cancel_ViewsBackStationSelected` (CTL) · `SectionMode_WingFieldsReadOnly_Copy122` (CTL) ·
`Selection_SectionPointReconcile_DroppedAfterLeavingMode` (CTL) · `SectionTelemetry_EnterEvent_CarriesOrigin` (CTL) ·
`SectionMode_StripSwitchNoEdits_DraftRebindsToNewStation` (CTL).

**EDT — the editor surface.**
`SectionEditor_EnterFromSideDoubleClick_ModeShown` (EDT) · `SectionEditor_ReturnOnSelectedStation_ModeShown` (EDT) ·
`SectionEditor_SideOverlap_ClickCyclesSections` (EDT) · `SectionEditor_StripSwitchWithEdits_RefusedByClick` (EDT) ·
`SectionEditor_FinishWhileBlocked_FocusesReason` (EDT) · `SectionEditor_EscapeCascade_DragPointSelectionThenLeave` (EDT) ·
`SectionEditor_Rendered_AnchorSquareControlCircleNamedDiamond` (EDT) · `SectionEditor_SelectedVsUnselected_ShapeNotColourOnly` (EDT) ·
`SectionEditor_DragUnderPointer_WithinTwoPixels` (EDT) · `SectionEditor_ArrowRun_OneStepOnKeyUp` (EDT) ·
`SectionEditor_BracketKeys_WalkInOrder` (EDT) · `SectionEditor_ReturnToX_TabToType` (EDT) ·
`SectionEditor_DoubleClickInserts_BackspaceDeletesNotNamed` (EDT) · `SectionEditor_ThicknessX2_DrawingOnlyValuesUnchanged` (EDT) ·
`SectionEditor_CombAutoScale_ClippedTeethMarked` (EDT) · `SectionEditor_ProbeFollowsPointer_PlacedMmAtStation` (EDT) ·
`SectionEditor_StripThumbnails_OnePerStationCurrentMarked` (EDT) · `SectionEditor_ModeBar_NamesStationAndScopeChip` (EDT) ·
`SectionEditor_FinishThenReenter_ViewsAndCanvasRedrawn` (EDT) · `SectionEditor_FitSelectionOnAnchor_HandlesAtLeast24PxApart` (EDT) ·
`SectionEditor_FocusedPoint_AccessibleNameSurfaceIndexTypeXY` (EDT) · `SectionEditor_PlateAndProbe_SayDisplay` (EDT).

**PNL — panes, menus, workspaces.**
`Properties_SectionPoint_TypeXYRowsInPercentChord` (PNL) · `Properties_SectionPointTypedX_OneStepExact` (PNL) ·
`Properties_SectionPointTypedMmX_ConvertedAtStationChord` (PNL) · `Properties_StationGroup_EndsWithEditSectionLink` (PNL) ·
`Properties_SectionGroup_OwnTcAndPerStationTcConsequence` (PNL) · `PointsPane_SectionMode_ControlNetGroupsWithKind` (PNL) ·
`PointsPane_RowClick_SelectsPointInCanvas` (PNL) · `PointsPane_CanvasSelection_RowSelected` (PNL) · `PointsPane_TypedX_OneStep` (PNL) ·
`StatusStrip_ShowAction_FramesBlockingPoint` (PNL) · `Workspace_Precision_ShowsPointsInHomeRegion` (PNL) ·
`Workspace_Planform_HidesPoints` (PNL) · `Presets_DesktopEqualsCodec_EveryWorkspace` (PNL) · `Layout_SavedMessagesPane_DroppedWithCode` (PNL) ·
`StatusStrip_SectionInsertReport_CountAndDeviation` (PNL) (CTL retirement ruling 1, 2026-10-03: replaces the M1.1 Insert report's exact oracle; the strip reports the inserted point and the measured deviation, 0 for an exact insert).

OD-2 A and OD-3 B were ruled on 2026-10-03, so the Points-pane and Layout names are final.

## 13. Decisions, findings and open items

**Operator decisions** (the mockup shows each as side-by-side variants):

| ID | Question | Options | Recommendation |
|---|---|---|---|
| **OD-1** | How is a section edited from the Side view? | **A** Side selects the station, and Edit section (Return, double-click or the link) opens the editor **in the model area** in place of the views (CAD-20). **B** The editor opens **in the Side view's slot**; Plan, 3D and Front stay and redraw from the draft. **C** Drag points directly on the Side view's placed section | **Ruled A** (operator, 2026-10-03; the recommendation). It is the spec and gives the most room. B costs a re-placement per step in three views. C is not recommended: the placed section is not the record (§3.6, measured, confirmed by two lenses). Marine-CAD agrees; it suggests B's lines-plan benefit come later as a drawing-only "as placed" overlay (OI-12C-3) |
| **OD-2** | Messages (DR-STATUS-1, D-4): where do blockers and past reports go? | **A** No Messages pane: blockers show where they block (Finish reason, canvas marker) and in the strip with one **Show**; no history (⌘Z is the history). **B** A transient **Issues popover** from a strip item. **C** A Messages pane only in the right side bar or as a float | **Ruled A** (operator, 2026-10-03; the recommendation). It is smallest and keeps DR-STATUS-1's intent; the strip's action slot exists. Marine-CAD: acceptable (the SolveSpace pattern) |
| **OD-3** | Where does the Points pane live? | **A** the bottom panel (spec). **B** the **right side bar**. **C** no Points pane in M1.2c. **D** a Points tab in the **left** side bar beside Properties and Browser | **Ruled B** (operator, 2026-10-03; the recommendation). Measured in the mockup at 1:1: **A** keeps 8.85 px per % chord but shows only 4 of 20 rows, a scrolling grid docked above the bottom bar. **B** shows 20 of 20 rows at 6.44 px per % chord. **C** and **D** keep 8.85; D shows all rows but hides Properties while the tab is up, so the Wing block is not visible, against CAD-17 ("with any selection or none"). Marine-CAD: B acceptable, A not (4 rows cannot compare upper and lower), and handles overlap at full chord in every layout, so Fit Selection does the precision work anyway. B needs a spec amendment of B1/UX-31 (flagged) |
| **OD-4** | If the per-surface certificate cannot be built (GSPK no-go, or GCRT at its cap), what ships? | **a** ship with **paired** section point types (ADR-0005 §6; certifies today; tested as SPTF) and bring DR-11 back with the numbers. **b** hold M1.2c | **Ruled a** (operator, 2026-10-03; the recommendation). Nothing else in M1.2c depends on the certificate |

**The variants that lost have no build path.** OD-1 B/C, OD-2 B/C, OD-3 A/C/D and OD-4 b are not built. They stay in
the mockup as the record of the choice. The record of the rulings is `docs/notes/m12c-rulings.md`, and the numbered
Ruling is the Coordinator's (`coord decide`).

**Findings:**

- **F-1:** the M1.1 Section-tab controls are dead (§1). M1.2c replaces the body, and the S-3 ledger maps the old tests.
- **F-2:** the profile-row check would use the rail rule; SPT fixes it (§3.4).
- **F-3 (resolved):** DR-VIEW-9/10/11 are recorded on `feature/ui-cad-direction` (§1).
- **F-4:** Precision versus D4 owning workspaces is resolved by §11.8's `simplify:`.
- **F-5 (Computational Geometry):** the as-built patch and fit paths write one shared basis (§1). SPT owns making them
  per side.
- **F-6 (Data & Persistence):** a recovery after Make unique would have refused the whole project as specified before
  repair. §3.3 now pins the base name.

**Open items:**

- **OI-12C-3:** a read-only table of offsets (upper, lower, t, camber at NACA stations); a neighbour-station ghost; a
  reference-section overlay (reusing CAD-18's dashed preview); a drawing-only "as placed at <station>" ghost.
- **OI-12C-4:** a curvature-continuous (G2) tangent kind.
- **OI-12C-5:** ThicknessFit (B3) targets on per-surface bases are measured on the paired x today. GSPK records
  whether they need a per-side change.

## 14. Build tracks (exclusive file ownership)

Priors (Ruling 54 P1: box = 3 × a measured prior of the same class): Grok C1 30 min, P1 45 min; Codex D3a 47 min,
D3b 43 min (`m12b2` §14). Harnesses: Core to Grok or Codex, UI to Codex, judgement to Claude. Agy is not used (two
HARNESS-SILENT-EXITs).

Every brief: foreground only; the Return section required; two repair cycles; `tools/run-tests.sh`, then
`tools/check-named-tests.py <track> --design docs/design/m12c-section-editor.md`; `AGENT_SESSION` exported.

| Track | Harness | Owns (exclusive) | Depends on | Box | Exit |
|---|---|---|---|---|---|
| **PRE** contracts and harness | Coordinator inline | `Contracts.cs` (§5.1 records); empty Core classes + `Run()` lines in `IdentityTests.cs`; Core `--readiness` entry; `--section-editor` suite entry + empty `SectionEditorTests.cs`; `SectionEdits.cs` signature stub (`Apply` throws for the type kinds; SPT owns the body, seam S-8); `tools/check-event-subscribers.py` (re-date FocusedTargetChanged to D4) | — | 30 min | build green, empty suites listed |
| **GSPK** certificate spike | Claude (Computational Geometry judgement) | `docs/proof/m12c-certificate-spike/**` only (probe, F1–F5, output, verdict) | PRE | **120 min**, shortened from 180. It has two stages: 60 min for the decisive fixtures (F1 and F4 at 2 m: the per-surface case, and the blend that sank B6; a measured no-go there ends the spike), then 60 min for F2, F3 and F5. There is no same-class measured prior: the audit log holds no duration for the M1.1 B6 cycles or any certificate spike (searched 2026-10-03). The measured time is recorded and becomes the prior | the §3.5 go/no-go table against the admission proofs |
| **DSP** display profile evaluator | Grok | `Placement.cs` (move `Jet`, `ParameterFor` and `OrdinateAt` into the shared internal `ProfileEvaluator` beside `ChannelEvaluator`; `Prepare` calls it); in `AuthoringSession.cs`, **`Sample` and its call in `ProfileAtCore` only** (seam S-7); `DisplayProfileTests.cs` (new); `Fixtures/m12c/display/` (Rebuild-produced non-dyadic knots, C⁰ knot, LE vertical tangent) | PRE | 90 min (C1 30 × 3: one fold plus one sampler and binding tests) | DSP names PASS; the `Placement` golden master still green; planted-mutant receipt (a shifted knot span turns the binding test red) |
| **SPT** section points | Grok | `SectionModel.cs` (new), `SectionEdits.cs` (new), `FoilSource.cs` (`IsAnchor`, per-side `RewriteCurves`/insert/delete, row writer), `ConstrainedFit.cs` (`ProfileFair` per surface, anchor KKT rows), `PointModel.cs` (roles, kinds, `AngleDegrees`), `Geometry.cs` (the profile row branch only; handed to GCRT at merge), `src/CfdWorkbench.Cli/Program.cs`, `SectionPointTests.cs`, `SectionEditsTests.cs`, `Fixtures/m12c/` | PRE; **DSP** (its projections read `ProfileEvaluator`); type operations wait for the GSPK verdict | 135 min (P1 45 × 3) | SPT names + the chosen list PASS; planted-mutant receipt |
| **SPTG** per-surface list | (SPT on a go) | as SPT | GSPK = go | in SPT's box | SPTG names PASS |
| **SPTF** paired list | (SPT on a no-go) | as SPT | GSPK = no-go | in SPT's box | SPTF names PASS |
| **SDR** section draft | Codex | `AuthoringSession.cs`, `SectionDraftTests.cs`, `ReopenSectionDraftTests.cs`, `docs/proof/m12c-old-build/` (the §3.3 characterization receipt, cases a–e, run on `4b9bc35`) | PRE. It dispatches SetType/SetTangent to the PRE stub; their tests are registered when SPT merges (S-8). It rebases on DSP's `Sample` change (S-7) | 140 min (D3a 47 × 3) | SDR names PASS; old-build receipt |
| **GCRT** per-surface certificate (**CUT 2026-10-03**: GSPK no-go, OD-4 a) | Grok | `Geometry.cs` (after SPT merges), `SectionOverlay.cs` (new), `OverlayTests.cs` (new), `BlendTests.cs` additions | GSPK = go; SPT; SDR | 135 min (P1 45 × 3) | GCRT names PASS; planted-mutant receipt |
| **CTL** controller and mode | Codex | `WorkbenchController.cs`, `Selection.cs`, `Shell/EditVerbRouter.cs`, `ControllerSectionTests.cs` (new); seam S-3: deleting the M1.1 writers in `AuthoringSession.cs`, `SectionFlowTests.cs`, `SectionToolsTests.cs` and their `WorkbenchTests.cs` rows | SDR; SPT (the controller reads `Sections.View`) | 140 min (D3a 47 × 3) | CTL names PASS; S-3 ledger |
| **EDT** editor surface | Codex | `SectionCanvas.cs` (rewrite), `SectionEditorView.axaml`(.cs), `ModelArea.axaml`(.cs), `ElevationView.cs` (double-click, Return, overlap cycle), `CurvePointLayer.cs` (section roles), `SectionCanvasTests.cs` (rewrite), `SectionEditorTests.cs`, `tools/check-event-subscribers.py` (after PRE) | CTL | 140 min (D3a 47 × 3) | EDT names PASS; no unwired event |
| **PNL** panes, menus, workspaces | Claude | `PropertiesView.cs`, `Panes/PropertiesPane.axaml.cs`, `Panes/PointsPane.axaml`(.cs) (new), `PointsView.cs` (new), `Shell/ShellLayout.cs`, `Shell/ShellHost.cs`, `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `Shell/WorkspacePresets.cs`, `Shell/StatusStrip.axaml`(.cs), `src/CfdWorkbench.Persistence/LayoutCodec.cs`, `PointsPaneTests.cs` (new), additions to `PropertiesViewTests.cs`, `ShellModelTests.cs`, `StatusStripTests.cs`, `LayoutFileTests.cs`, port of the Section-tab rows in `ShellWindowTests.cs` | CTL | 135 min (P1 45 × 3) | PNL names PASS; the UI-DEAD-CONTROL walk green in the mode |
| **UXR** review and polish | Claude | `DESIGN.md` (tokens, COPY-172..184, component rows), `Styles.axaml`, `docs/reviews/m12c-native.md` (new), the readiness rows in `tools/run-readiness.py` | EDT, PNL | 120 min (as written; measured time recorded) | marine-CAD re-review; native rows with attach receipts |

**Order:** PRE → {GSPK ∥ DSP ∥ SDR} → SPT (after DSP; its type operations after the GSPK verdict) → CTL (after SDR and
SPT) → {EDT ∥ PNL} → UXR → join. On a go, GCRT runs in parallel from the SPT and SDR merges. EDT and PNL consume the
display path through the controller (DSP → SPT → CTL). The width cap is 3 concurrent delegated coding tracks, and the
first wave is exactly 3.

Critical path: PRE → DSP → SPT → CTL → EDT → UXR = 30 + 90 + 135 + 140 + 140 + 120 = **655 min of boxes**. The SDR branch
(PRE → SDR → CTL) is 85 min shorter. Measured priors (C1 30, P1 45, D3a 47) suggest about 3.7 h of real time.

### 14.2 Seams

| Seam | Rule | Fallback |
|---|---|---|
| **S-1** `Geometry.cs` | SPT edits the profile row branch only; GCRT owns the file after SPT merges | GCRT rebases on SPT |
| **S-2** `SectionEdits.cs` type operations | written once, for the policy GSPK chose (SPTG or SPTF); the other list is cut from §12.4 at the verdict | — |
| **S-3** M1.1 writers | CTL deletes them and their two Desktop suites after SDR merges; readers stay; every deleted behaviour points at a §12.4 name or "retired". **Amended 2026-10-03 (Coordinator, CTL stopped on it):** the writers also have callers in seven Core test files — `SectionEditTests.cs`, `ThicknessIntentTests.cs`, `ConstructionTests.cs`, `ReopenConstructionTests.cs`, `FairSessionTests.cs`, `DatImportTests.cs`, `ProofBudgetTests.cs`; CTL owns those call sites too (port each to the section draft or retire it, in the same ledger), and `AuthoringSession.cs` for the S-3 deletions plus the three SDR findings (nullable assessment duration on `SessionEvent`, a draft-view reader after `ResumeRecovery`, the legacy-recovery upgrade) | the Owner rules on a name that cannot be ported |
| **S-4** `ShellHost.cs` | PNL only; EDT reaches the mode through `ModelArea.Mode` and the controller | — |
| **S-5** D4 | M1.2c hands D4 a section draft that answers `IsDirty`, Finish and Cancel for its close and save rows (app-shell F11 S1/S10) | — |
| **S-7** `AuthoringSession.cs` | DSP edits `Sample` and its call in `ProfileAtCore` only; SDR owns the rest of the file and rebases on DSP's merge (DSP is the shorter track) | if SDR merges first, DSP rebases its one method |
| **S-8** `SectionEdits.cs` | PRE lands the signature stub; SPT owns the body after PRE. SDR's SetType/SetTangent tests are registered when SPT merges | — |
| **S-6** `PointView` | `AngleDegrees` is non-positional with a default, so existing positional constructions compile unchanged | — |

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| FoilDSL file → parser → certifier | T: hand-edited profile `tangents` rows that do not hold, incl. a cusp | mitigate | the profile row branch (Euclidean, with ordering conditions) | `Assess_ProfileVerticalRowHandlesSameSide_InvalidDslLock` |
| FoilDSL file → certifier | D: a section crafted to exhaust the per-surface overlay | mitigate | atom budget, 32,768-bit ceiling, 1 s budget → Not assessed, never Certified | `SectionAssess_OverlayBudgetExhausted_NotAssessedNeverCertified` |
| Native envelope → reopen | T: a forged `"section"` receipt or recovery | mitigate | closed `rail` set; `EditReference` / `RecoveryReference` "section" arms | `Receipt_ForgedSectionRailUnknownProfile_DocReference` |
| Telemetry ring | I: profile names, point ids or positions leak | mitigate | step kind, counts, durations, codes only | `SectionTelemetry_Events_NoNamesIdsOrPositions` |
| History | R: an edit without attribution | accept | single local user; the receipt names the profile | — |
| — | S, E | not applicable | no authentication, privilege levels or network | — |

## Privacy analysis (LINDDUN-lite)

This component touches no personal data. Section geometry, step reports, receipts and the new events carry geometry,
counts, durations, codes and step kinds only: no path, account or free text. A profile name can be typed freely, so it
is kept out of telemetry (`SectionTelemetry_Events_NoNamesIdsOrPositions`).

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| section events (local ring) | D: disclosure through logs | mitigate | no profile names, ids or positions | in-memory ring of 256; gone at exit |

## Conformance notes

- **ADR-0002:** one authority, the bytes. The overlay is derived and recomputed, and it replaces the difference path.
- **ADR-0005:** types are derived from knots. Under Ruling 53 DR-11, section types are **per surface**; §6's paired
  default survives only as the OD-4 fallback (flagged review-suggested).
- **ADR-0007:** the step shape and state machine are named here. **Proposed amendment:** no step count in the receipt
  (§3.3), with a dated note in the ADR.
- **ADR-0009:** one command table.
- **Rulings honoured (§11):** DR-STATUS-1, DR-CELL-1..5, DR-NAV-1, DR-VIEW-1/-7, Ruling 58 Q-1, MC-3/6, DR-UID-1/5.
- **Deviations:**

  | ID | Deviation | Why |
  |---|---|---|
  | D-1 | a mode bar replaces v10's app-bar toolbar | §11.7 |
  | D-2 | the Points pane's home is the right side bar (OD-3 B, ruled) | spec B1/UX-31 flagged |
  | D-3 | no Messages pane (OD-2 A, ruled) | spec B1/UX-31 flagged; app-shell §11's `role=log` obligation retired |
  | D-4 | Precision has no memory | §11.8 |
  | D-5 | Vertical is disabled on interior section anchors | A4.15 lists it; flagged for the spec owner |
  | D-6 | old builds show "tangent row names an interior anchor" or "not assessed" instead of "saved by a newer version" | §3.3 |
  | D-7 | handle length is in % c on sections | UI-37 says mm; flagged |

## Flagged risks and residual unknowns

- **R-1 (high):** the per-surface certificate failed twice in M1.1, and the corrected tolerance analysis (§3.5) shows
  blends need about 2–4 × 10⁻¹⁰ at 1–2 m. Mitigations: GSPK's oracle is the admission proofs, F4 runs at 2 m, there is
  a TE wedge and nose line, and the tested OD-4 fallback (SPTF). Residual: if GSPK says go and GCRT then caps out, SPT
  switches lists (one track's rework).
- **R-2:** the binary64 display and the certificate may disagree near tangency (§7). Finish follows the certificate.
- **R-3:** the placed result can surprise. Under "From the Thickness curve", a thicker section does not thicken the
  foil. Mitigations: the per-station t/c rows with their consequence line, the probe's placed mm, and COPY-176's "stays
  <s> % t/c".
- **R-4:** a degree-5 anchor is G1 at best, so curvature breaks (−6.29 / −1.73 per chord at the mockup's anchor). It is
  shown by the comb and reported in COPY-174. A G2 kind is OI-12C-4.
- **Inferred until built:** every rendered, frame-time and screen-reader claim; the §3.5 reduction numbers until GSPK
  runs. The old-build cases are observed (`docs/proof/m12c-old-build/receipt.jsonl`).

## Status & next action

| | |
|---|---|
| **Completed** | M1.2c detailed design (data model, contracts, failure modes, telemetry, UI, ledger, tracks); the mockup; the gate (four lenses, repair cycle 1) |
| **Remaining** | the Coordinator's numbered Ruling for the 2026-10-03 rulings; the spec owner's B1/UX-31 and A4.15 amendments (D-2, D-3, D-5); the lenses not convened (Patterns Expert, Simplifier, UX & Accessibility, Native Desktop); then the coordination plan; then the M1.2d and M1.2e designs |
| **Best next action** | the coordination plan for §14, then PRE inline, then GSPK, DSP and SDR in parallel |

## Gate record

`GATE design · 2026-10-03 · Computational Geometry (hard veto, narrow), Test Architect (hard veto), Data & Persistence (hard veto), Marine-CAD UX (soft veto) · each run as a separate Adversary-Mode agent · criteria met: data model first (no new authority; receipt and recovery expand-only; grain, writer and reader stated), E7, contracts with sources and marked assumptions, failure modes with tests, STRIDE-lite, LINDDUN-lite, telemetry, UI with states and copy, 145 attributed test names (checker extract: 0 errors) · verdict: PASS WITH CONDITIONS · vetoes → resolution: Test Architect HARD VETO (the OD-4 fallback had no tests; the SPT ledger could not go green under either verdict) → cleared on re-review; all others pass on re-review · author did not self-clear · repair cycles: 1 of 2`

| Lens | Initial verdict | Main findings | Repair cycle 1 | Re-review |
|---|---|---|---|---|
| Computational Geometry (hard veto, narrow) | Pass with conditions; would become Silent-Refit vetoes if built as written | As-built patch and fit paths write one shared basis. A closed TE cannot be proved by hulls (a false no-go). The tolerance model contradicted the existing admission proofs (≈ 450× optimistic for blends). Anchor → Control residual not reported. The row metric admits cusps. max T0 is not uniform in w. The nose rule fails on drooped noses. Locality has no teeth on section-a | SPT owns the per-side `RewriteCurves`, `ConstrainedFit` anchor rows and per-surface Fair/Rebuild; Import stays shared. TE wedge and nose lines. GSPK's oracle = `PlacementWidth`/`BlendPlacementWidth`, F4 at 2 m, one tolerance constant, max T0 uniform in w, a QueryFeasibility atom model, F5. `MaxChange` + oracle on every type or tangent step. Euclidean rule with ordering conditions. Two-anchor locality fixture. The overlay replaces the difference path | **Pass**, no conditions. LE radius (1.04 % c) and TE wedge (11.42°) hand-checked. The optional nit "Corner (handles in line)" was applied |
| Test Architect (hard veto) | **BLOCK** (1 Blocker, 12 Major) | Fallback claims untested; SPT unreachable under either verdict. Readiness names in the fast-ring checker. Old-build test unobservable in the new suite. No test for undo at entry, STATUS-CLOBBER, rendered announce, re-entry redraw, shared Finish or Make unique. S-3 ledger unenforced. Untested state-machine rows. Dead-control sweep missed the mode bar. D7 unstated | SPT / SPTG / SPTF split with §14 rows. Readiness moved to tier 6 with a Core `--readiness`. Old-build receipt moved to SDR (cases a–e). About 30 names added, renamed or cut. Every §6.1 row names a test. D3 and D7 lines. Planted-mutant receipts | **Veto cleared.** Two Minors (32-point ceiling retagged SPT; the report test made policy-free) applied after re-review |
| Data & Persistence (hard veto) | Pass with conditions (4 Major) | A recovery after Make unique would refuse the whole project. The contract step orphaned legacy profile recoveries. `steps` had no compute reader (DM15). Old-build claims partly wrong. "section" checker arm unspecified. Pane homes defined in two places | Recovery pins the base profile name. Readers kept; legacy rows resume as a section draft. `steps` dropped, with ADR-0007 Amendment 1 proposed. Grain includes the no-op Finish. Old-build characterization receipt. "section" arm and idempotency tests. Presets read `LayoutCodec.Homes` | **Pass.** Minor (case c split into c1 `DOC-REFERENCE` / c2 `DOC-SCHEMA`, amendment text) and Nit (stale ADR frontmatter) applied after re-review |
| Marine-CAD UX (soft veto) | Pass with conditions (6 Major) | Thickness wording a trap. LE radius and TE readouts deferred against A4.15. No curvature-break surfacing. No comb scale. No pointer probe. Vertical on interior anchors is a false model. Thickness ×2 default distorted measurements | Own t/c + per-station t/c with a consequence line. LE radius, TE gap and wedge in scope. Comb on, with break teeth and auto-scale. `Sections.Probe` at the pointer. Vertical disabled (D-5). 1:1 default and re-measured. Return → x. Kind column / control net. mm in x. ⌫ and double-click. Insert anchor (keep shape). Strip chord and t/c. OD-3 D variant. Finding 9 (handles only on selection) **declined with rationale** (§6.1) | **Pass with conditions → applied after re-review, not re-reviewed:** readouts as own **and** at each station (computed from placed Rule A; the mockup agrees with the lens's k² / k scaling); comb spaced by arc length; % units; OD-3 D breaks CAD-17 |

**Post-gate rulings (2026-10-03):** OD-1 A, OD-2 A, OD-3 B, OD-4 a; ADR-0007 Amendment 1 accepted; the section
display path (ADR-0010 Amendment 1), added as track DSP. These came after the gate and were not re-reviewed by the
lenses. The display path takes the certificate out of drawing and adds a binding test, which is the Computational
Geometry lens's "one authority, bound by test" shape.

**Not convened, with reason (pending for the Coordinator):**

- **Patterns Expert and Simplifier.** The brief scoped this gate to the four lenses above. The patterns are named in §6.1 and the Solution-Selection Ladder is in §6.2, but neither has been attacked by those two lenses. *Pending.*
- **UX & Accessibility.** The operator ranks a11y below other concerns; cheap floors only, per §11.6. *Pending*, at the latest at UXR.
- **Native Desktop.** The per-OS keys ⌫, ⌘↩ and ⇧/⌘ arrows, and Control-click. *Pending at UXR.*
- **Hydrofoil Hydrodynamicist.** Not convened: the LE radius and TE readouts are geometric. No hydrodynamic quantity or cavitation claim is computed or labelled.
- **Security.** Not convened: no new trust boundary; the file inputs are in STRIDE-lite.

**Residual:**

- The conditions applied after re-review were not re-reviewed (Test Architect 2 Minor, Data & Persistence 1 Minor and 1 Nit, Marine-CAD 1 Major and 2 Nits, Computational Geometry 1 Nit).
- Every rendered, frame-time and VoiceOver claim stays **Inferred** until EDT, PNL and UXR run.
- The §3.5 tolerance figures stay **Inferred** until GSPK runs.

---
**Handoff:** → operator review of the mockup → `/implement` (tracks §14).
