---
id: design-m12b2-3d-elevations
title: "Design: M1.2b2 — one placement rule, the 3D view beside the Plan, the Front and Side elevations, and dihedral, twist and thickness editing"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — M1.2b2 (Ruling 56 OI-1; after M1.2b, before M1.2c)
tags: [desktop, core, cad, geometry, placement, certificate, 3d-view, orbit, view-cube, elevations, front, side, body-plan, dihedral, twist, thickness, lanes, m1.2b2, oi-1, dr-13]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-foildsl, rel: implements }
  - { to: architecture-application, rel: refines }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: design-m12b-points, rel: depends-on }
  - { to: note-20260926-binary64-evaluator, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: mockup-workbench-v10, rel: relates-to }
  - { to: coordination-app-shell-build, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-29
summary: >-
  Detailed design of slice M1.2b2. FoilDSL §6 (Rule A and the twist/dihedral placement) is written once in Core and
  instantiated over the certificate's rational intervals (bits unchanged) and over binary64 for every display, bound by
  a measured test (at most 1 nm outside the certified enclosure). On that rule: a shaded or wireframe 3D view beside the
  Plan with orbit, pan, zoom, view cube and named cameras; Front and Side elevations drawing the placed foil with
  dihedral, t/c and twist lanes; and those three channels edited with M1.2b's point, handle and gesture model.
review-suggested:
  - { by: mockup-property-grid, on: 2026-10-01, reason: "PNL should fill the property-grid component (docs/reviews/ui-property-grid.md §10) from PropertiesView.Build by data; channel rows Height (mm), Twist (°), t/c (%) render in the same row kinds; the M1.2b fix track is recommended to build the component first (DR-UID-4)." }
---

# Design: M1.2b2 — one placement rule, the 3D view and the elevations

- **Status:** In review — gate record at the end.
- **Spec / architecture:** [spec rev 1.6](../specs/cfd-workbench-v1.md) CAD-01, CAD-04, CAD-06, CAD-08, GEO-10, A4.4,
  A4.7, A4.8, B7, model-area table (§B1) · [FoilDSL](../specs/foildsl.md) §5 items 2–4, §6 · [architecture §10](../architecture/application.md)
  · [ADR-0010](../adr/0010-one-placement-rule.md) (new) · [ADR-0002](../adr/0002-foildsl-authority.md) ·
  [ADR-0005](../adr/0005-point-types-in-the-b-spline-record.md) · [ADR-0007](../adr/0007-edit-transactions-section-draft-and-gesture-commit.md) §3 ·
  [M1.2b design](m12b-points.md) (the contracts this slice reuses) · [Ruling 56](../notes/rulings.md) (OI-1, DR-13).
- **Delivery phase:** M1.2b2, sequenced by Ruling 56 after M1.2b and before M1.2c. Real: everything in this slice. No
  new mock seam: every view reads a pure Core projection, so Desktop tests drive it from source bytes.
- **Author / date:** `/design-slice` sub-agent (session `track-m12b2-design`), 2026-09-30. Code read at `3a37f5f`.

## 0. What the operator will see

### 0.1 The demo at the end of M1.2b2 (packaged `.app`, macOS)

1. **New foil.** The Planform workspace shows **Plan** (two thirds) and **3D** (one third) side by side, as v10 draws
   it. 3D shows the foil **shaded, outlined, with every authored section drawn**, from the Iso camera (perspective).
   The view title reads "3D · Iso"; a view cube sits in its top-right corner. (The M1.2b "3D samples" tab is retired.)
2. **Orbit and navigate the 3D view.** ⌥-drag orbits (turntable about +z, around the view's target); the title
   changes to "3D · Free · az 212° · el 24°" (az 0° = Front, 90° = Side). On the trackpad, two-finger scroll pans and
   pinch zooms; the mouse wheel zooms about the pointer (DR-13). Middle-drag or Shift-drag on empty space pans. Click
   **FRONT** on the view cube: the camera jumps to Front, orthographic, with no animation. **Home** returns to Iso; **⌘0**
   fits everything; **F** fits the selection.
3. **Keyboard only in 3D:** ⌥← / ⌥→ orbit 15° (⇧⌥ 90°), ⌥↑ / ⌥↓ tilt 15° (⇧⌥ 45°), `[` `]` orbit 5°, ⇧+arrows pan,
   ⌘= / ⌘− or ⇧Z / Z zoom, ⌘0 fit all, F fit selection, Home Iso. Tab reaches the cube's faces and chevrons, then leaves
   the view. View ▸ Pan and View ▸ Camera give the same moves as clickable items.
4. **Views ▾ → Four views:** Plan, 3D, Side, Front (v10's quad). Double-click a view label (or Return on it) → that view
   alone; again → back. **Views ▾ → One view** does the same from the menu.
5. **Display ▾ → Wireframe:** the 3D view draws the outline, the rails, the authored sections (full weight) and ten
   intermediate sections (thin); **Shaded** returns. Each view keeps its own Display setting.
6. **Front · looking aft** (the Front camera: starboard on the viewer's left): the placed foil as a band with its outline;
   on its leading-edge line the **dihedral** points (diamond ends, filled control points, dashed polygon). Below the band,
   the lane "Thickness t/c (%) · tip ← root" on the same span axis, with the **t/c** points.
7. **Drag a dihedral point up.** The band, the 3D view and the Side view follow during the drag; the probe shows
   "Δ height +12.00 mm". Release: one undo step; the status line reads "Moved dihedral point 5 by 12.00 mm. Tip height
   12.40 mm." ⌘Z returns it exactly. The dihedral root is fixed: pressing it says "The dihedral root is at the centre
   line. It can't be moved." (assertively).
8. **Side** (from starboard: nose to the right): every authored section overlaid at its true placement, the selected
   station at full weight — a body plan. Below, the lane "Twist (°) · root → tip" with a zero line and positive up.
   Select the tip end and press ⇧↓: −1.00°; the 3D view shows the tip nose-down. "Moved twist tip end by −1.00°. Tip
   twist −3.00°." Drag far down: the point stops at the checkable limit and the probe says "Twist is limited to
   ±57.30° — larger angles can't be checked yet."
9. **Drag a t/c point** in the Front lane: the Wing block's ≈ Max t/c changes during the drag (≈ preview).
10. **Right-click a twist control point → Make Anchor Point:** handles appear in the lane; Tangent Smooth; Properties
    shows Twist · point 4 as an anchor; the status line reports the largest change. Save, close, reopen: the rows and
    types are as saved.
11. **Hover** the Front band or either lane: the probe reads "η 0.412 · span 185.40 mm · height 0.00 mm · twist −0.84° ·
    t/c 12.00 % · chord 113.81 mm". Hover the Side band: the probe names the section under the pointer ("Tip · η 1.000
    · twist −2.00° · chord 95.00 mm"), or the selected station.
12. **Click the tip section in 3D or in the Side band:** the tip station becomes the selection in Plan (chip), Side
    (full weight), 3D and Properties.
13. **Sign check:** on the Example (tip twist −2°), Side and 3D show the tip section with its trailing edge **higher**
    than its leading edge; a positive twist would take the trailing edge down (FoilDSL §6).

### 0.2 What the operator will NOT see yet, and which slice brings it

| Not in M1.2b2 | Brought by |
|---|---|
| The **Review workspace** (Window ▸ Review ⌘3: Four views with the side bars hidden, remembered per workspace) — Four views itself **is** here, as a model-area layout from Views ▾ | **M1.2e** (workspace switching and saved layouts; the persisted `WorkspaceViews` field already exists, `LayoutDocument.cs`:23) |
| The chosen layout, camera and display mode surviving a relaunch (they are session values here) | **M1.2e** |
| Dragging points in the 3D view (CAD-06 defers it: no drag plane without a gizmo); double-click a 3D edge to open the elevation that edits it | **No slice yet** — OI-6 |
| The display cage ("Box": section control polygons placed in 3D) and "Cage over smooth" (CAD-06) | **No slice yet** — OI-7: a section control vertex has no placement under Rule A's normalized blend, so a cage needs its own definition first |
| The η-plot view (CAD-08); a pointer probe on the 3D surface (ray pick to η) | **No slice yet** — OI-8 |
| Curvature comb on the dihedral, twist and t/c curves, and the curvature κ / radius in the probe (CAD-08) | **No slice yet** — joins M1.2b's OI-5 (comb controls) |
| A perspective/orthographic toggle (named axis cameras are orthographic, Iso and free orbit perspective) | **No slice yet** — OI-9 |
| Front and Side linked in vertical scale and z datum (a lines-plan convention) | **No slice yet** — OI-10; each elevation fits itself |
| Section editor from a station; section points | **M1.2c** |
| Catalog | **M1.2d** |
| Floating panes, Maximize pane | **M1.2e** |
| Rhino preset and a trackpad-mode setting (⌥+two-finger orbit; Fusion's Shift+two-finger) | **No slice yet** — M1.2b OI-4 |
| Analysis layers in 3D | **M2** analysis slices |
| Windows | Deferred (architecture §8) |

## 1. Grounding — what this design must satisfy

Traversal: `rulings` (56) → `design-m12b-points` (§0.2 OI-1, §3.5–3.7, §5, §6.2–6.3, §11, §14) → `spec-foildsl` §5–§6 →
`spec-cfd-workbench-v1` (CAD-01/04/06/08, GEO-10, A4.4/A4.7/A4.8, §B1) → `note-20260926-binary64-evaluator` →
`adr-foildsl-authority`, `adr-0005`, `adr-0007` → `mockup-workbench-v10` → `design-language` → `defect-classes`
(BUDGET-DISPLAY, FRAME-A, UI-RENDERED-STATE, UI-DEAD-CONTROL, EDIT-KIND-REOPEN) → `coordination-app-shell-build`
(Planned vs actual). Code was opened, not recalled.

| Source | Statement this design must satisfy |
|---|---|
| Ruling 56 OI-1 | "3D view beside the Plan plus front/side/starboard elevations, starting with a design pass for one shared placement rule with the certificate" |
| M1.2b §0.2 | "inventing a display copy of it here would be a second geometry definition"; "Core commands are curve-named, only the two rails are wired" |
| FoilDSL §5.2 | frame +x aft, +y starboard, +z up; `leading(0)=dihedral(0)=0`; symmetry `(x,y,z) → (x,−y,z)`; positive twist nose-up about the LE |
| FoilDSL §6 | the placement formula, the pinned constant `0.017453292519943295` rounded once, Rule A blend with `w=(eta-eta_a)/(eta_b-eta_a)`; "A B-spline skin, tessellation … is a derived approximation with measured error; sampling count cannot redefine geometry" |
| CAD-04 | Front carries the Dihedral frame on the centre line and the t/c frame in a captioned lane below the band; Starboard is a body plan with the Twist frame in its own lane; a locked vertex refuses with its lock named |
| CAD-06 | named cameras Top · Front · Starboard · Iso (+ Bottom · Back · Port) over one model; keyboard orbit ⌥←→ 15° (⇧ 90°), ⌥↑↓ 15° (⇧ 45°), `[` `]` 5°, ⇧+arrows pan, Z / ⇧Z, F fit, Home Iso; view cube with faces that have area and four chevrons; "positive twist is nose-up … the sign fixture asserts it"; the Starboard view has the nose to the right; dihedral/twist/t/c polygons "never drawn at invented 3D positions"; direct 3D vertex dragging deferred |
| CAD-08 | a view renders at its own pixel size; a pointer probe writes η, % half-span and the evaluated channels |
| GEO-10 | orbit, pan, zoom, fit-all and fit-selection and named views by pointer, trackpad or keyboard; station selection linked |
| §B1 model area | Views Plan · 3D · Side · Front; layouts Plan + 3D · Four views · One view (Views ▾, or double-click a view label) · Fit; "assume: Side = Starboard (nose to the right), Front = Front" |
| B7 + DR-13 | ⌥+LMB orbit, Shift+LMB or MMB pan, wheel zoom, LMB selects, RMB context menu; trackpad scroll pans, pinch zooms |
| A4.8 | nudge 0.01 / 0.1 / 1 mm and 0.01 / 0.1 / 1 °; `value [unit]` and `#name` echoed |
| §1237 (targets) | orbit p95 frame ≤ 33 ms; edit feedback p95 ≤ 100 ms |

**Code as built (3a37f5f) that constrains this design:**

- Placement exists once, inside the certificate: `Geometry.PointAt` (`Geometry.cs`:85-119) over `RationalInterval`;
  Rule A in `SectionExact`, `SelectBlend`, `SameGeometry`, `ProfileSection`, `ProfileComponents` (`:140-228`). The
  constant appears four times (`:100`, `:413`, `:477`, `:640`). Three models of the same operation tree bound its cost
  or error: `QueryFeasibility.Prove` (`:640-655`), `PlacementWidth` (`:429-450`), `BlendPlacementWidth` (`:491-503`);
  they hard-code the Taylor depth, the 64-bit angle grid and 32! (`:266-276`, `:439`, `:642-649`).
- `RationalInterval` `+ − ×` are exact rational endpoint operations (`:747-753`), so only the expression tree matters for
  its bits, not the evaluation order.
- `Assess` requires `leading` and `dihedral` root ordinates 0 (`:298`), profile z(0) = 0 (`:305`), the thickness hull
  strictly inside (0, 1) (`:322`), and the twist hull inside ±1 rad after the once-rounded product (`:413-416`, `:477-480`).
- Every display today goes through the certificate (`WorkbenchController.cs`:937-947, 15 points;
  `Program.cs`:106-115). `Viewport.cs`:12 states the doctrine "Draws only certified physical point samples".
- The one binary64 channel inversion is private to `WingEstimates` (`WingEstimates.cs`:180-201, 60 bisection steps
  over `SplineBasis`); note-20260926 names `SplineBasis` the one binary64 evaluator. `Curve`, `Definition` and
  `SplineBasis` are `internal`; Core exposes internals only to `CfdWorkbench.Core.Tests` and `CfdWorkbench.Persistence`
  (`ConstrainedFit.cs`:4, `AuthoringSession.cs`:8) — the Desktop cannot evaluate a curve.
- A file with no `locks` block gets `root_mirror` on all five channels (`FoilSource.cs`:1068).
- `LayoutDocument` already persists `WorkspaceViews(ViewArrangement Plan3d|Four|One, SingleView Plan|3d|Side|Front)`
  (`LayoutDocument.cs`:23, :56-69); `WorkspacePresets` gives Review `Four` (`WorkspacePresets.cs`:38-42); the Window ▸
  Review command is a `NoOp` (`CommandTable.cs`:69).
- `MainWindow.ThicknessReadout` refuses to show a non-constant t/c because there is "no public pointwise query"
  (`MainWindow.axaml.cs`:1248-1256). `Placement.Frame` is that query (finding F-12).
- SkiaSharp 2.88.9 is already in the Desktop graph through `Avalonia.Skia` 11.3.14 (`packages.lock.json`:110-121).

**Spike (read + run), `docs/proof/cad-first-spikes/m12b2-placement/`:** the certified `PointAt` costs 13.5–37.9 ms
per point on four fixtures; a binary64 evaluation of §6 lies at most 2.8 × 10⁻¹⁷ m outside the certified enclosure
pointwise, and a separable binary64 grid of 41 × 101 × 2 points costs 13.1–24.0 ms warm and lies at most
1.43 × 10⁻¹⁰ m outside it (ADR-0010 table). The geometry lens probed the spike's grid-and-parabola maximum: relative
error 5.7 × 10⁻⁹ on a smooth thickness, 3–7 × 10⁻⁴ at a C⁰ peak — so the design replaces it (§3.5 step 3).

**M1.2b preconditions.** This design builds on M1.2b as designed (`m12b-points.md`, in review). Every M1.2b2 track
starts from `main` after the M1.2b track it names in §14 has joined. §14.2 lists the seams.

## 2. Responsibility

This slice owns: the one placement rule (ADR-0010) and its display projection (`Placement.Surface`, `Placement.Frame`);
the 3D view (camera, orbit/pan/zoom, named cameras, view cube, shaded and wireframe display, station pick); the Front
and Side elevations (band, lanes, probe); editing the dihedral, twist and thickness channels through M1.2b's point,
handle and gesture contracts; the model-area layouts Plan + 3D, Four views and One view; the Properties, Browser and
menu rows for all of it.

Not this slice's: new point or gesture semantics for the rails (M1.2b owns them; this slice widens the curve set and
adds the channel row rule of §3.6); section points and the section editor (M1.2c); the Review workspace preset and
layout persistence (M1.2e); certification rules other than the channel row checks and the published domain constants.

## 3. Data model (settled first)

### 3.1 Bounded context and ubiquitous language

Context: **Planform authoring**, as M1.2b. New terms, used verbatim in code, copy and tests: **Placement rule** (FoilDSL
§6 as code, ADR-0010); **Channel** (one of the five distribution curves; the UI names them Leading edge, Trailing edge,
**Dihedral** (the `dihedral` channel, values are heights), **Twist**, **Thickness** (t/c)); **Station frame** (the five
channel values and the chord at one η); **Placed section**; **Surface view** (the placed display mesh of one revision or
draft generation); **Camera** (target, azimuth, elevation, distance, projection); **Named camera** (Top · Front · Side ·
Iso · Bottom · Back · Port); **Band** (an elevation's projection of the placed foil); **Lane** (a captioned 2D graph of
one channel against span); **Layout** (Plan + 3D · Four views · One view).

"Side" is the v10 label of the Starboard camera (spec §B1 assumption); the accessible name says "Side view, from
starboard".

### 3.2 Aggregates and invariants

Unchanged from M1.2b §3.2: the **Foil source revision** (the only geometry authority, ADR-0002), the **History** and the
**Draft**. This slice adds **no aggregate and no stored field of its own**:

| Value | Kind | Owner | Invariant |
|---|---|---|---|
| Placement rule | code (one generic function family) | Core | exactly one site for Rule A, station selection and placement; the certificate's enclosure bits are unchanged by it |
| Surface view, station frame, placed section | projection (read model) | Core | derived from one revision or draft generation; never stored; every vertex within 1 nm of the certified enclosure (tested on fixtures, including a 2 m chord and a C⁰ thickness peak) |
| Camera, layout, display mode per view | session value | Desktop controller | not persisted (Type-1 discard, recorded; M1.2e persists layout) |
| Channel point, handle, tangent row | as M1.2b (identity `(curve, vertex id)` inside a revision) | source bytes | as M1.2b; the curve set widens to five channels; channel rows use the §3.6 rule |

### 3.3 Grain, history rule, additivity

| Store / projection | Grain ("one row is exactly one …") | History rule | Measures |
|---|---|---|---|
| Accepted row (existing) | one committed gesture or command, now also on the dihedral, twist or thickness channel | Type-2 (append-only) | none stored |
| `tangents` row (M1.2b, FoilDSL 4.1) | one tangent kind of one interior anchor of one channel — any of the five | versioned with its revision | — |
| `SurfaceView` (projection, new) | one placed display mesh of one revision or draft generation (source hash + basis + generation) | not stored | coordinates non-additive |
| `StationFrame` (projection, new) | the channel values at one η of one revision or generation | not stored | chord = trailing − leading, derived, never a field of its own |
| `view.surface`, `view.navigate.end` events (new) | one computation / one camera gesture | ring of 256 (existing) | durations and p95 non-additive; frames additive |

**Derive, don't store.** The chord in `StationFrame` is a computed property (`Trailing − Leading`), not a constructor
argument, so it cannot disagree with its rails. The port half is the symmetry map applied to the starboard mesh, never a
second mesh. No projection is cached across generations: the controller holds the newest completed `SurfaceView` only.

### 3.4 The placement rule (ADR-0010) — the single authority

```csharp
// src/CfdWorkbench.Core/Placement.cs (new) — FoilDSL §6. The one site of Rule A, station selection, placement and the constant.
internal interface IPlacementScalar<TSelf> : IAdditionOperators<TSelf, TSelf, TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>,
    IMultiplyOperators<TSelf, TSelf, TSelf> where TSelf : struct, IPlacementScalar<TSelf>
{
    static abstract TSelf Half { get; }
    static abstract TSelf Point(double value);                    // exact: a binary64 input as a scalar
    static abstract TSelf BlendWeight(double eta, double left, double right);  // w = (η − ηa)/(ηb − ηa) (§6), in the domain's arithmetic
    static abstract TSelf Radians(TSelf degrees);                 // × RadiansPerDegree, rounded once (§6); never double.DegreesToRadians
    static abstract (TSelf Sin, TSelf Cos) SinCos(TSelf radians);
}
internal static class PlacementRule
{
    internal const double RadiansPerDegree = 0.017453292519943295; // the only occurrence of this literal in src/
    internal const int TaylorTerms = 16, AngleGridBits = 64;      // read by Trigonometry, QueryFeasibility, PlacementWidth, BlendPlacementWidth
    // Station selection (§6, "between adjacent station assignments"): the stations bracketing η, collapsed to one profile
    // when the neighbours are the same geometry. Comparisons are on binary64 station values, exact in both domains.
    internal static (int Left, int Right) Select(ReadOnlySpan<double> stationEtas, ReadOnlySpan<int> profiles, double eta,
        Func<int, int, bool> sameGeometry);                        // Right = −1 → one profile
    // Rule A pieces: C = (u + l)·½; T = (u − l)·maxReciprocal.
    internal static (T Camber, T Unit) Components<T>(T upper, T lower, T maximumReciprocal) where T : struct, IPlacementScalar<T>;
    // One profile: (C ± T·thickness·½), built from Components (the tree of Geometry.cs:181-184).
    internal static (T Upper, T Lower) Section<T>(T upper, T lower, T maximumReciprocal, T thickness) where T : struct, IPlacementScalar<T>;
    // Blended: C = (1−w)·Ca + w·Cb; T0 = (1−w)·Ta + w·Tb; C ± T0·max(T0)⁻¹·thickness·½ (the tree of :147-157).
    internal static (T Upper, T Lower) Blend<T>((T Camber, T Unit) a, (T Camber, T Unit) b, T weight,
        T maximumT0Reciprocal, T thickness) where T : struct, IPlacementScalar<T>;
    // Placement: X = L + c·(x·cos φ + z·sin φ); Z = D + c·(z·cos φ − x·sin φ); c = trailing − leading (the tree of :107-110).
    internal static (T X, T Z) Place<T>(T leading, T trailing, T elevation, (T Sin, T Cos) angle, T x, T z)
        where T : struct, IPlacementScalar<T>;
}
internal readonly record struct Binary64(double Value) : IPlacementScalar<Binary64>;   // IEEE ops, Math.SinCos
// RationalInterval (Geometry.cs) implements IPlacementScalar: Point = the exact rational of the double; BlendWeight = the
// exact rational (η − ηa)/(ηb − ηa) as a point interval (today's :170); Radians = the once-rounded product with nearest
// endpoints and the AngleGridBits dyadic grid (today's :100-105); SinCos = Trigonometry (:259-278); Half = Point(1/2).
```

- **The expression trees are the certificate's trees**, statement for statement: `RationalInterval` arithmetic is exact
  per endpoint (`:747-753`), so the same tree gives the same bits. The complement `1 − w` is computed inside `Blend`
  from `Point(1)` and the weight, as `:147` does today with the exact rational.
- **Golden master first.** At PL0's base commit (M1.2b B0 has changed `Geometry.cs` by then, so not 3a37f5f) the track
  captures, for every fixture, the outward bits of `PointAt` and `SectionAt` on a probe grid, the `Assess` certificate
  fields (placement width, feasibility witness, witnesses) **and the refusal codes** of a refusing set (the 10 nm check,
  budget, cancel, the Taylor domain). It commits them before `Geometry.cs` changes. Fixtures carry non-zero twist,
  non-zero dihedral and a blended station. Red-first is impossible for a pure refactor, so the receipt is a planted
  mutant: reassociating `x·cos φ + z·sin φ` turns each golden test red; reordering the blend sum does not (interval
  addition commutes), and the golden master pins outputs and refusals, not the operation tree, so a structural trace pin
  is required before the first change to §6 (§13 OI-11). The run is recorded in the Proof Pack (as M1.2b §3.8). The two `ChannelEvaluator` fold goldens carry the same receipt
  (one planted evaluator mutant turns each red), and PL0's join checks in `git log` that the golden commit precedes the
  `Geometry.cs` change.
- **The golden master's failure message names the other three models** — `QueryFeasibility.Prove` (`:640-655`),
  `PlacementWidth`, `BlendPlacementWidth` — so a change to the rule's outputs forces their review (a math-preserving tree
  change can stay green; the trace pin of §13 OI-11 covers it). The Taylor depth and
  the angle grid become `PlacementRule` constants read by all of them. **`simplify:`** the three bound models stay
  hand-kept; ceiling: a hand review on each rule change; upgrade trigger: the first real change to §6 (then instantiate
  `QueryFeasibility` over a bit-size domain and version its witness paths).
- `Y = halfSpan·η` (negated for port) stays outside the generic core: it is one product with no channel input.
- **Evaluators stay two** (note-20260926): the certificate feeds the rule with Bernstein enclosures; displays feed it
  with the binary64 channel evaluator `ChannelEvaluator.At(curve, η)` — the private inversion now in `WingEstimates`
  (`:180-201`), made internal and shared. `WingEstimates` and M1.2b's `Planform.View` call it (fold under a golden
  master: estimates and plan samples bit-identical).
- **Domain constants are published by the certificate, not restated.** `Geometry.TwistDomainDegrees` (the largest
  binary64 d whose once-rounded product d × k is ≤ 1, i.e. what `:413-416` admits) and `Geometry.ThicknessDomain`
  (open (0, 1), held on the quantum grid as [10⁻⁷, 1 − 10⁻⁷]) are constants in `Geometry.cs`; `Assess` and the gesture
  clamp (§3.6) read the same constants.

### 3.5 Display projection (Core)

`Placement.Surface(bytes, basis, generation, cancellationToken, stations = 41, chordSamples = 101)`:

1. Stations: 41 uniform η in [0, 1] ∪ every authored station η, sorted, duplicates removed (authored stations are drawn
   exactly where the source puts them; an authored η equal to a uniform η appears once). Chord samples:
   cosine-clustered `x_k = (1 − cos(πk/100))/2`. The wireframe's intermediate sections are mesh rows (η = i/10, a subset
   of i/40), never interpolated.
2. Per station (separable, measured): the five channel values by `ChannelEvaluator.At`; twist → `Binary64.Radians` →
   `SinCos` once; the profile pair and weight by `PlacementRule.Select` and `Binary64.BlendWeight`, with a
   `sameGeometry` predicate that compares the two profiles' curve records (degree, knots, points), as `SameGeometry`
   compares their spans. (A curve-record comparison may blend two copies of one shape that the span comparison
   collapses; the result is the same shape to round-off. `Placement.cs` carries this as a one-line comment.)
3. Per profile, once: upper and lower at every chord sample, and the profile maximum `max_x(u − l)`; per blended
   station the maximum of T0. **Maximum:** bracket on a 401-point grid, then a safeguarded Newton iteration on the
   derivative of (u − l) (or T0) through the `SplineBasis` derivative jet, with every knot image in the bracket taken as
   a candidate (a C⁰ peak sits on a knot) — to binary64 convergence. The grid-and-parabola of the spike is not used
   (its C⁰ error measured 3–7 × 10⁻⁴).
4. Per point: `PlacementRule.Components` → `Section`/`Blend` → `Place`, all over `Binary64`.
5. Output: starboard points only; the Desktop draws port through `Point3.Port()`.

`Placement.Frame(bytes, η)` returns one `StationFrame` (all five channels, chord derived) for the probe and Properties.
Neither function takes a `ProofBudget` (BUDGET-DISPLAY). Both throw `ContractError("DSL-PATCH")` on bytes that do not
parse to a foil, as `WingEstimates.From` does (`:50-56`). `Surface` observes its token between stations.

### 3.6 Channel points — roles, freedoms, units, rows (derived, Core)

M1.2b §3.5 applies to every channel unchanged (roles by knot multiplicity; handles; ordering clamp min(1 mm, gap at
Begin); quantized Δ; exact positions). The per-channel differences are data in one table in `PointModel.cs`:

| Curve | UI name | Ordinate (SI) | Display unit | Quantum of Δ | Nudge ladder (⌘ · plain · ⇧) | Row tolerance τ_c | Root end (with `root_mirror`) | Domain clamp |
|---|---|---|---|---|---|---|---|---|
| `leading` | Leading edge | m | mm | 1 µm | 0.01 · 0.1 · 1 mm | M1.2b rule | Fixed (`:298`) | — |
| `trailing` | Trailing edge | m | mm | 1 µm | 0.01 · 0.1 · 1 mm | M1.2b rule | Value only, coupled | — |
| `dihedral` | Dihedral (value "Height") | m | mm | 1 µm | 0.01 · 0.1 · 1 mm | 1 µm | **Fixed** (`:298`) | — |
| `twist` | Twist | degrees | ° | 10⁻⁵ ° | 0.01 · 0.1 · 1 ° (A4.8) | 10⁻⁶ ° | Value only, coupled | ±`Geometry.TwistDomainDegrees` |
| `thickness` | Thickness (t/c) | chord fraction | % | 10⁻⁷ | 0.01 · 0.1 · 1 % | 10⁻⁸ | Value only, coupled | `Geometry.ThicknessDomain` |

The tip end is Value only (η = 1) on every channel. Without `root_mirror` the root handle is Free and the root end is
Value only (or Fixed where `:298` requires 0). Under `root_mirror` on `dihedral` the root handle has ordinate 0 and moves
along the span only. The t/c ladder is not in A4.8; it is proposed here as the % analogue (spec-owner finding F-10).

**Tangent rows on the three channels — a unit-free rule (Computational Geometry, required).** An angle tolerance in a
plane of metres × degrees has no meaning (0.1° admits a 17 % slope kink on a flat twist curve and changes 57× with the
unit). For `dihedral`, `twist` and `thickness`, with anchor A = (η_A, v_A) and handles H₋ = (η₋, v₋), H₊ = (η₊, v₊):

- **Smooth** ⇔ |v_A − lerp(v₋, v₊, (η_A − η₋)/(η₊ − η₋))| ≤ τ_c (the anchor lies on the handle line, measured along
  the ordinate in the channel's own unit).
- **Symmetric** ⇔ |η_A − (η₋ + η₊)/2| ≤ 10⁻⁹ and |v_A − (v₋ + v₊)/2| ≤ τ_c.
- **Co-motion** (a dragged handle of a Smooth anchor): the opposite handle keeps its η and moves its ordinate onto the
  line through A and the dragged handle — the rule `SetTangent` already uses (M1.2b §6.4). Symmetric: opposite =
  2A − dragged, exact in both coordinates.

These are affine-invariant and unit-consistent; `Assess` checks them with the table's τ_c. The rails keep M1.2b's rule
(both axes are metres there). Whether the rails should adopt the same rule is seam SR-5 (§14.2), not a condition here.

**Domain clamp (sound and not over-eager).** A CV inside the domain keeps the certificate's hull test true (Bernstein
coefficients are convex combinations of CVs; the once-rounded product is monotone). The clamp acts only **in the
direction of growing violation**: a point that starts outside the domain (a certified revision may hold an interior CV
beyond ±D whose Bézier points stay inside) is never snapped at Begin and may move inward freely. The clamp applies to
**every vertex written** by the frame, including a co-moved or derived handle. A clamped frame sets
`GestureFrame.Clamped`.

**Handles typed in Properties:** on Dihedral the handle is typed as **angle** and **length** (mm). The angle is the
local dihedral angle of the tangent line, atan2(Δheight, Δspan) oriented root → tip, for **both** handles, so a Smooth
anchor shows one value on both. On Twist and Thickness an angle in mixed units means nothing, so the handle is typed by
its **Span** and **value**, like a point.

## 4. Persistence

No new store and no new source syntax. What widens:

- **Receipts:** a gesture receipt's `rail` may now be `dihedral`, `twist` or `thickness`; a point-type or tangent-kind
  receipt's `curve` may be any of the five. `EditReference` learns them in the same change (class EDIT-KIND-REOPEN):
  `"leading" or "trailing" or "dihedral" or "twist" or "thickness" => receipt.Curve is null && receipt.Rule is null && …`;
  `"point-type" or "tangent-kind" => receipt.Curve is in the five && …`. `RecoveryReference` refuses the three new rail
  values (a gesture has no recovery row) and is otherwise unchanged; a project holding such a receipt still opens.
- **FoilDSL:** none beyond M1.2b's 4.1 (`tangents` on any channel, 6–16 points). `EnsureHeader41` (M1.2b) is the single
  writer of the header, also for channel rows.
- **Forward compatibility (recorded, D-5b):** an M1.2b build opening a project whose history holds a channel edit
  refuses it with `DOC-REFERENCE` (its closed `rail` set does not know `dihedral`), and the file is unchanged. A `.foil`
  file with a channel row on twist or thickness that an M1.2b build checks with its angle rule may be judged differently;
  M1.2b2 is the first build that writes such rows, so none exist before it.
- **Layout:** Plan + 3D / Four / One, the single-view choice, camera and display mode are session values in the
  controller (Type-1, recorded). `LayoutDocument.WorkspaceViews` is not written by this slice (M1.2e).

## 5. Contracts

### 5.1 Exposed — Core (new unless marked)

```csharp
// src/CfdWorkbench.Core/Placement.cs (new) — §3.4 internals plus this public projection.
public readonly record struct Point3(double X, double Y, double Z)   // body frame, metres (+x aft, +y starboard, +z up)
{ public Point3 Port() => new(X, -Y, Z); }                            // FoilDSL §5.2 symmetry, the only map the Desktop applies
public sealed record StationFrame(double Eta, double SpanMeters, double LeadingMeters, double TrailingMeters,
    double ElevationMeters, double TwistDegrees, double ThicknessRatio)
{ public double ChordMeters => TrailingMeters - LeadingMeters; }
public sealed record PlacedSection(double Eta, int? Assignment, IReadOnlyList<Point3> Upper, IReadOnlyList<Point3> Lower);
public sealed record SurfaceView(string SourceHash, string Basis, long Generation, double HalfSpanMeters,
    IReadOnlyList<PlacedSection> Sections, Point3 Minimum, Point3 Maximum);   // starboard half, root → tip; rows share chord samples
public static class Placement
{
    public static SurfaceView Surface(byte[] source, string basis, long generation, CancellationToken cancellation,
        int stations = 41, int chordSamples = 101);
    public static StationFrame Frame(byte[] source, double eta);          // A4.9 probe, Properties, the station card (F-12)
}
// Internal to Core and its tests: the evaluator-call counter (Placement_Surface_EvaluatorCallsBounded) and the display
// maximum (Placement_DisplayMaximum_WithinCertifiedMaximum).

// src/CfdWorkbench.Core/Geometry.cs (PL0) — published domain constants read by Assess and by the gesture clamp.
public static partial class Geometry { public const double TwistDomainDegrees = /* pinned by test */; public static (double Lower, double Upper) ThicknessDomain { get; } }

// src/CfdWorkbench.Core/PointModel.cs (M1.2b; widened by CH1) — reused, not forked.
// PointView.Ordinate, GestureFrame.Ordinate, the curve samples' Ordinate, HandleTarget's value, PointFreedom.ValueOnly
// replace M1.2b's rail words AftMeters/AftOnly (seam SR-1, the whole set).
public static class Channels
{
    public static CurveView View(byte[] source, string curve, string basis, long generation);   // any of the five
    public static ChannelUnit Unit(string curve);   // SI unit, display unit, quantum, nudge ladder, row tolerance, domain (§3.6 table)
}
// Planform.View composes Channels.View for its two rails (one projection per curve).
```

`AuthoringSession` (M1.2b members, reused unchanged in signature): `BeginPointGesture(draftId, curve, vertexId)`,
`UpdatePointGesture(draftId, generation, spanMeters, ordinate)`, `ApplyPointCommand(operationId, PointCommand)`. CH1
widens the accepted curve set from one table (`PointModel.EditableCurves`) to all five; the motion rules read the
quantum, row rule and domain from `Channels.Unit`. Refusal codes are M1.2b's; new copy only (§11.4). A domain clamp is a
clamp (`GestureFrame.Clamped = true`), never a refusal.

### 5.2 Exposed — Desktop

```csharp
// WorkbenchController (M1.2b; widened by VW1)
public SurfaceView? Surface { get; }                    // newest completed request (accepted, or the draft's during a gesture)
public bool SurfaceUpdating { get; }                    // derived: newest issued ticket ≠ shown ticket (drives "Updating…")
public ViewLayout Layout { get; set; }                  // Plan3d · Four · One(SingleView)
public ViewCamera Camera3d { get; set; }                // one camera for the 3D view
public ViewCamera CameraFor(SingleView elevation);      // Front and Side keep their own pan/zoom; direction fixed
public DisplayMode DisplayFor(SingleView view);         // Shaded (outlined) · Wireframe
// Gesture API unchanged (M1.2b §5.2); a channel point is a PointRef with its curve.

// src/CfdWorkbench.Desktop/ViewCamera.cs (new, VW1) — pure value type (Avalonia's plain Point/Size structs; no controls), testable without a window.
public readonly record struct ViewCamera(Point3 Target, double AzimuthDegrees, double ElevationDegrees, double Distance, Projection Projection)
{
    public static ViewCamera Named(NamedCamera name, Point3 minimum, Point3 maximum); // axis cameras Orthographic, Iso Perspective
    public ViewCamera Orbit(double deltaAzimuth, double deltaElevation);   // turntable about +z around Target; elevation clamped
                                                                           // to ±90° (no flip); becomes Perspective; at the poles the
                                                                           // screen up vector comes from the azimuth, so Top = Plan
    public ViewCamera Pan(double dxPixels, double dyPixels, Size viewport);
    public ViewCamera ZoomAbout(Point pointer, double factor, Size viewport);  // the model point under the pointer stays put;
                                                                           // distance clamped to [0.01, 100] × the fitted distance
    public ViewCamera Fit(Point3 minimum, Point3 maximum, Size viewport);
    public Point Project(Point3 point, Size viewport); public double Depth(Point3 point);
    public string Title { get; }  // "Iso" · "Front · looking aft" · "Free · az 212° · el 24°" (az 0° = Front, 90° = Side)
}
// SurfaceRenderer (new, VW1): draws a SurfaceView through a ViewCamera — Shaded (painter-sorted, back-face-culled
//   triangles on one lit ramp, clamped to foil-shade-lit; the port half with reversed winding) or Wireframe (rails,
//   authored sections, ten intermediate mesh rows); in both, the silhouette (edges between a front- and a back-facing
//   triangle, and boundary edges) in 1.5 px foil. One render path survives VW1 (Skia or its fallback, never both).
// View3d : Control (new, V3D): camera navigation, view cube, station pick, peers.
// ElevationView : Control (new, ELV): Front (band + dihedral frame + t/c lane) and Side (band + twist lane);
//   hosts CurvePointLayer (extracted from M1.2b's PlanCanvas, seam SR-2) — the same glyphs, hit test, peers and keys.
// CommandTable rows (PNL): view.layout-plan3d, view.layout-four, view.layout-one (Views ▾, radio);
//   view.display-shaded, view.display-wireframe (Display ▾, radio, for the target view);
//   view.camera-top · -front · -side · -iso · -bottom · -back · -port (View ▸ Camera; Home = Iso with 3D focused);
//   view.pan-left · -right · -up · -down (View ▸ Pan, showing ⇧-arrow / ⌥-arrow); view.fit-selection (View ▸ Fit Selection, F);
//   view.fit ⌘0 (exists), view.zoom-in ⌘= / ⇧Z, view.zoom-out ⌘− / Z (M1.2b) act on the target view.
```

### 5.3 Consumed

| Contract | Source | Confidence |
|---|---|---|
| FoilDSL §6 placement and Rule A | `docs/specs/foildsl.md` §6 | Verified (read) |
| `Geometry.PointAt`/`SectionAt` enclosures (the golden master's subject) | `Geometry.cs`:85-135 | Verified (read + run in the spike) |
| `SplineBasis.Evaluate` (values and derivative jet) and the `WingEstimates` inversion | `ConstrainedFit.cs`:576; `WingEstimates.cs`:180-201 | Verified (read + run in the spike) |
| M1.2b point/gesture contracts: `PointView`, `CurveView`, `PointRole`, `PointFreedom`, `TangentKind`, `BeginPointGesture`, `UpdatePointGesture`, `ApplyPointCommand`, `GestureFrame`, `GestureOutcome`, the §6.2 transition table, the latest-wins throttle with trailing flush, `PlanCanvas` glyphs and peers | `m12b-points.md` §5.1–5.2, §6.1–6.3, §11.2–11.3 | **Inferred** (designed, not built) — `assume:` M1.2b merges these contracts as designed, plus SR-1…SR-4 or their fallbacks (§14.2); confirm: each M1.2b2 track reads the merged signatures before its first edit; if a signature differs, the track adapts to the merged shape and reports it (it never re-implements the contract) |
| C# static abstract interface members; BCL `IAdditionOperators`/`ISubtractionOperators`/`IMultiplyOperators` over `record struct` operators (net10.0) | language + BCL | Verified (documented since C# 11 / .NET 7; the repo compiles net10.0) |
| Avalonia `ICustomDrawOperation` + `ISkiaSharpApiLeaseFeature` → `SKCanvas.DrawVertices` (painter-sorted triangles drawn in order in one call) | Avalonia 11.3.14 + SkiaSharp 2.88.9 (transitive) | **Inferred** — `assume:` a custom draw operation can lease the Skia canvas in the window and in `RenderTargetBitmap`, and draws the triangle list in order; confirm: `SurfaceRenderer_RenderTargetBitmap_CapturesShadedPixels` and `SurfaceRenderer_ShadedPainterOrder_TopShowsUpperSurface` (VW1) are the first tests; if false, VW1 replaces the Skia path with `DrawingContext` quads at 21 × 41 per surface and half and records it (the fallback replaces, never sits beside) |
| Avalonia pinch (`Gestures.PinchEvent`) and two-finger scroll deltas on the macOS trackpad | Avalonia 11.3.14 | **Inferred** — `assume:` pinch arrives as a pinch gesture and trackpad scroll as `PointerWheelChanged` with fractional deltas distinguishable from a wheel; confirm: M1.2b's Plan uses the same input (DR-13) and V3D's native row N-3D-2; if false, V3D follows whatever M1.2b's Plan does and names the difference |

## 6. Patterns and structure

### 6.1 Named patterns

| Pattern | Where | Why this, not a bespoke shape |
|---|---|---|
| **Single source of truth via generic numeric abstraction** (static abstract interface members over the BCL operator interfaces — the .NET generic-math idiom) | `PlacementRule` over `RationalInterval` and `Binary64` | Rule A, station selection and placement written once; each arithmetic is a type, not a copy of the formula (ADR-0010) |
| **Golden master / characterization test** with a planted mutant as its receipt | the certificate refactor; the `ChannelEvaluator` fold | Proves a pure refactor changed no bits (note-20260926 precedent) |
| **Projection / read model** (existing) | `Placement.Surface`, `Placement.Frame`, `Channels.View` | Derive, don't store |
| **Separate latest-wins channel, single-flight** (M1.2b's latest-wins throttle with trailing flush, keyed by a monotonic ticket) | `SurfaceAsync` in the controller | A 13–24 ms mesh must not block the UI frame; one compute runs and one waits; a result shows only if its ticket is the newest; its own cancellation source and counter, never the commit path's `stateVersion` |
| **Camera as an immutable value** (Value Object) | `ViewCamera` | Each navigation verb is a pure function, so pointer, trackpad, keyboard and menu produce the same camera (one test per verb) |
| **Strategy by data** (a table, not subclasses) | per-channel units, quanta, ladders, row tolerances, domains (§3.6) | Five rows of data, one motion rule |
| **Extract Class** (refactoring) | `CurvePointLayer` from `PlanCanvas` | One point-editing surface for Plan and lanes; no forked glyph or keyboard code |
| **Painter's algorithm with back-face culling** | shaded surface | Depth-sorted, culled triangles drawn back to front; enough for one thin body; no depth buffer or GPU pipeline; the port half's winding reversed because the mirror has determinant −1 |
| **State machine as a transition table** (M1.2b) | gestures on any channel | Unchanged; the curve is data in the `PointRef` |
| **Parallel Change** (expand–migrate–contract) | SR-1 fallback rename | Old names kept as aliases only until CH1's exit; CH1 owns the contract step |

Rejected: a display implementation of §6 with a differential test (two definitions — ADR-0010); a separate recording
scalar pinning the operation trace now (a third implementation; deferred, not cut: the golden master pins outputs, not the
tree, so the trace pin of §13 OI-11 is required before the first change to §6 and before VW1 — Simplifier); a 3D engine or GPU pipeline (OpenGL control, Silk.NET, a scene graph) — one body of
≤ 33k triangles does not need one, and a new dependency fails the ladder's rung 5; a view-model per channel point; a
separate "elevation point" contract (a fork of M1.2b's); a mesh cache keyed by generation; an animated camera
transition (HardCut archetype); an architecture test that the Desktop never calls the channel evaluator (the compiler
already forbids it: `Curve` and `SplineBasis` are internal — Simplifier).

### 6.2 View navigation map (one table; each row is one `ViewCamera` function and one test)

| Verb | 3D view | Front / Side (band and lane) | Function |
|---|---|---|---|
| Orbit | ⌥-drag (mouse or trackpad click-drag); ⌥←→ 15°, ⇧⌥ 90°; ⌥↑↓ 15°, ⇧⌥ 45°; `[` `]` 5°; cube chevrons 90° | — (fixed direction) | `Orbit` |
| Pan | two-finger scroll (DR-13); middle-drag; Shift-drag on empty space; ⇧+arrows (10 % of the view); View ▸ Pan | same pointer verbs; ⌥+arrows (M1.2b's Plan keys); View ▸ Pan | `Pan` |
| Zoom | wheel and pinch about the pointer; ⌘= / ⇧Z in, ⌘− / Z out, about the centre | same | `ZoomAbout` |
| Fit all / fit selection | ⌘0 / F (the selected station, or all when none) | ⌘0 / F (the selected point or station) | `Fit` |
| Named camera | cube face; View ▸ Camera ▸ …; Home = Iso | — | `Named` |

In 3D, plain arrows do nothing (no point to nudge) and ⇧+arrows pan as CAD-06 says. In the elevations the M1.2b Plan
keymap holds (arrows nudge the focused point along **screen** axes, ⌥+arrows pan), so a user's hands do the same thing
in every 2D view; the 3D help text names the one difference ("Shift arrows pan here; in the elevations they move a point
1 mm"). Single-character keys (`[`, `]`, Z, ⇧Z, F) are bound on the view control, not the window: they act only while
that view has focus (2.1.4) and never in a text field, the Browser or another view.

## 7. Error and concurrency model

- **Threads.** The UI thread owns the controller and the cameras. `Placement.Surface` runs off the UI thread through a
  **separate latest-wins channel**: each request takes a ticket from `Interlocked.Increment(ref surfaceTicket)`; at most
  one compute runs and one waits (a newer request replaces the waiting one and cancels the running one through the
  channel's own `CancellationTokenSource`); a completion is shown only if its ticket equals the newest issued ticket. The
  source hash and generation travel with the result for correlation and telemetry only. The channel never touches M1.2b's
  `stateVersion`, `activeSampling` or commit cancellation (`WorkbenchController.cs`:945-950), so a mesh request never
  makes a commit stale. Camera changes redraw the current mesh; they never recompute it. `Placement.Frame` (the probe) is
  small and runs on the UI thread.
- **Undo, Redo, Open** issue a new ticket like any change, so a slow pre-Undo mesh can never replace the post-Undo one
  (tickets are monotonic; hashes are not ordered).
- **During a gesture** the Plan updates synchronously (M1.2b); the 3D view and the other elevation follow as their meshes
  complete, typically one frame behind. While a newer request runs for more than 250 ms, the view title shows
  "Updating…" (TQ reactive recompute).
- **Gestures on channels** use M1.2b's state machine and Busy rule unchanged: one draft at a time on any curve; Undo and
  Redo disabled during a draft or Busy.
- **Refusal at release** (edges cross, Not assessed, row check) behaves as M1.2b §7; the point is redrawn in every view
  at its accepted position.
- **Domain clamp:** a twist or t/c target past the certificate's domain is clamped in Core (`Clamped`), shown in the
  probe with its reason; nothing is refused.
- **A not-certified foil** (opened with a banner, M1.2b §7): the 3D view and the elevations draw it dimmed with the
  title suffix "· not checked"; channel points are read-only, as on the Plan.
- **Mesh failure** (a bug, or a draft that no longer parses): the view keeps its last mesh with the note "Showing the
  last shape that could be drawn."; `view.surface` records `outcome=error` and the code. A render exception shows the
  pane copy "3D view couldn't be drawn. Your foil hasn't changed." with Try again (`shell.pane.render`).

## 8. Change-surface list (E7)

| Layer | M1.2b2 change | Track |
|---|---|---|
| Store | receipts: `rail` ∈ + {dihedral, twist, thickness}; `curve` ∈ five channels (no new key); no new syntax | CH1 |
| Model (Core) | `IPlacementScalar`, `PlacementRule` (Select, Components, Section, Blend, Place, constants), `Binary64`; `RationalInterval` implements the interface; domain constants; channel table (units, rows, clamps) | PL0, CH1 |
| Service (Core) | `Geometry.PointAt`/`SectionAt` call the rule; `Assess` channel row rule; gesture and point commands on five channels; `EditReference` | PL0, CH1 |
| Projection / wire | `Placement.Surface`, `Placement.Frame`, `SurfaceView`, `StationFrame`, `PlacedSection`, `Point3`, `Channels.View`, `ChannelUnit`; `Planform.View` composes `Channels.View`; CLI `inspect --json` points for all channels | PL0, CH1 |
| Compute reader | `WingEstimates` and `Planform.View` read the one `ChannelEvaluator`; station card t/c reads `Placement.Frame` (F-12, PNL if `MainWindow.axaml.cs` is free) | PL0, PNL |
| Client type (Desktop) | `ViewCamera`, `NamedCamera`, `ViewLayout`, `DisplayMode`; controller `Surface`, `SurfaceUpdating`, cameras, layout | VW1 |
| UI | `SurfaceRenderer`, `View3d` (+ cube), `ElevationView` (Front, Side, lanes), `CurvePointLayer`, arrangement host in `ModelArea`, "3D samples" tab retired; Properties channel rows; Browser channel groups; Views ▾, Display ▾, View ▸ Camera, View ▸ Pan, View ▸ Fit Selection; context menu on lane points; copy; tokens | VW1, V3D, ELV, PNL, UXR |
| Accessibility | 3D view group and cube buttons; band groups; lane point peers (reused); announcements | V3D, ELV, UXR |
| Telemetry | `view.surface`, `view.navigate.end`; `gesture.end` and `apply` carry the curve family | VW1, V3D, CH1 |
| Contract (remove) | the "3D samples" document tab and the M1.2a `Viewport` 3D mode; the certified 15-point sampler in the controller (`WorkbenchController.cs`:937-947) once no caller remains; SR-1 aliases at CH1's exit | VW1, CH1 |

## 9. Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed | Detection | Test |
|---|---|---|---|---|---|
| The refactor changes certificate bits or refusals | one generic rule | prevent | golden master of bits and refusal codes captured first; same trees; planted-mutant receipt | test | `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` (PL0), `PlacementRule_CertificateGoldenMaster_AssessWitnessesAndRefusalsUnchanged` (PL0) |
| A display re-derives placement (GEOM-AUTHORITY) | Desktop drawing | prevent | rule, selection and constant internal to Core; Desktop gets `Point3` only and cannot evaluate a curve (internal types) | test | `PlacementRule_RadiansConstant_SingleSiteInSource` (PL0), `PlacementRule_SelectBlend_SameStationsAsCertificate` (PL0) |
| Display drifts from the certificate | binary64 evaluator | detect | 1 nm binding on fixtures incl. a 2 m chord and a C⁰ thickness peak | test | `Placement_DisplayWithinCertifiedEnclosure_Fixtures` (PL0), `Placement_DisplayMaximum_WithinCertifiedMaximum` (PL0), `Placement_FrameLeadingEdge_EqualsCertifiedPointAtXZero` (PL0) |
| The rule changes and the bound models do not | three hand-kept models | detect | the golden master's failure names them; shared constants | test | `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` (PL0), `PlacementRule_TaylorAndGridConstants_SharedByAllModels` (PL0) |
| Twist sign or handedness wrong in a view (FRAME-A) | cameras, rule | prevent | sign fixture in Core and rendered in every view | test | `Placement_SignFixture_PositiveTwistTrailingEdgeDown` (PL0), `ViewCamera_Front_StarboardOnViewerLeft` (VW1), `ViewCamera_Side_NoseRightTrailingEdgeLeft` (VW1), `View3d_SignFixture_ExampleTipTrailingEdgeRenderedHigher` (V3D), `Elevation_SideSignFixture_ExampleTipTrailingEdgeRenderedHigher` (ELV), `Elevation_FrontBand_StarboardPointsOnViewerLeftRendered` (ELV) |
| Display spends the proof budget (BUDGET-DISPLAY) | `Placement.Surface` | prevent | no `ProofBudget` parameter or call | test | `Placement_Surface_NeverUsesProofBudget` (PL0) |
| Mesh too slow for the frame | 41 × 101 mesh | mitigate + detect | off-thread single-flight; deterministic work count in the fast ring, wall time at readiness | `view.surface` duration | `Placement_Surface_EvaluatorCallsBounded` (PL0), `Controller_SurfaceAsync_StaleTicketDropped` (VW1) |
| 3D view lags a drag | async mesh | accept | one frame behind is the design; the Plan stays synchronous; "Updating…" after 250 ms | `gesture.end` p95 with `ThreeDVisible` | `Controller_DuringChannelDrag_SurfaceFromDraftGeneration` (VW1), `Controller_SlowSurface_UpdatingShownAfter250Ms` (VW1) |
| A slow pre-Undo mesh replaces the post-Undo one | async + Undo | prevent | monotonic ticket, not hash | — | `Controller_UndoDuringSurfaceCompute_ShowsUndoneShape` (VW1) |
| A mesh request makes a commit stale | shared cancellation | prevent | separate channel and counter | — | `Controller_SurfaceRequest_NeverStalesCommit` (VW1) |
| Authored station missing, moved or duplicated in the mesh | display stations | prevent | authored η inserted exactly; duplicates removed | test | `Placement_Surface_AuthoredStationsIncludedExactly` (PL0), `Placement_Surface_AuthoredEtaOnUniformGridOnce` (PL0) |
| Twist dragged past the certificate domain (either sign) | channel editing | prevent | clamp at ±`TwistDomainDegrees`, only in the growing direction; reason in the probe | frame `Clamped` | `UpdatePointGesture_TwistBeyondDomain_ClampedAtDomain` (CH1), `UpdatePointGesture_TwistBelowNegativeDomain_ClampedAtNegativeDomain` (CH1), `Elevation_TwistDomainClamp_ProbeShowsReason` (ELV) |
| t/c dragged to 0 or 1 | channel editing | prevent | clamp inside the open interval, both ends | frame `Clamped` | `UpdatePointGesture_ThicknessBelowZero_ClampedInsideOpenInterval` (CH1), `UpdatePointGesture_ThicknessAboveOne_ClampedBelowOne` (CH1) |
| A certified out-of-domain CV snapped at Begin | clamp | prevent | clamp only in the growing direction | — | `UpdatePointGesture_OutOfDomainCvAtBegin_NotSnappedMovesInward` (CH1) |
| A co-moved handle escapes the domain | clamp | prevent | clamp every written vertex | frame `Clamped` | `UpdatePointGesture_CoMovedHandleBeyondDomain_Clamped` (CH1) |
| Dihedral root moved (violates `:298`) | roles | prevent | root end Fixed; root handle span-only at ordinate 0 | lock copy | `PointModel_Dihedral_RootEndFixedTipEndValueOnly` (CH1), `PointModel_DihedralRootMirror_RootHandleZeroSpanOnly` (CH1), `Elevation_LockedDihedralRoot_AssertiveLockCopy` (ELV) |
| A kinked twist or t/c row accepted by a unit-dependent tolerance | row rule | prevent | the unit-free rule of §3.6 | `apply` refused `DSL-LOCK` | `Assess_SmoothRowOnTwistChannel_Certified` (PL0), `Assess_TwistSmoothRowOrdinateOffByTwoTolerances_Invalid` (PL0), `Assess_SymmetricRowOnThicknessNotMidpoint_Invalid` (PL0), `SetTangent_TwistSymmetric_MidpointRowHolds` (CH1) |
| Channel receipt not reopenable (EDIT-KIND-REOPEN) | receipt widening | prevent | `EditReference` learns the values in the same change | test | `Reopen_ChannelGestureRows_UndoRedoRoundTrip` (CH1), `Reopen_ChannelPointTypeAndTangentRows_UndoRedoRoundTrip` (CH1), `Recovery_ChannelGestureRail_RefusedProjectStillOpens` (CH1) |
| Forged `rail`/`curve` value | user-writable project | prevent | closed sets | `DOC-REFERENCE` | `Reopen_UnknownCurveOnReceipt_DocReference` (CH1) |
| M1.2b build opens a project with channel edits | receipt widening | accept (D-5b) | refused `DOC-REFERENCE`, file unchanged | — | — |
| View hosted but not visible, or blank after leaving and returning (UI-RENDERED-STATE) | new canvases | detect | whole-window pixel checks through `TranslatePoint`, including a layout round trip and tab re-entry | `shell.pane.render` | `Workspace_NewFoil_WindowPixelsShow3dSurface` (VW1), `ModelArea_LayoutRoundTripAndTabReentry_AllViewsRenderPixels` (VW1), `Elevation_FrontBand_RenderedFromSurfaceView` (ELV) |
| Band thickness shown only by a low-contrast fill (1.4.11) | shading | prevent | silhouette stroke in `foil` in every band and in 3D | — | `SurfaceRenderer_Silhouette_RenderedFoilStrokeThreeToOne` (VW1) |
| A light overlay drowns on a bright face | lit ramp | prevent | ramp clamped at `foil-shade-lit` | — | `SurfaceRenderer_ShadingRamp_NeverBrighterThanFoilShadeLit` (VW1) |
| Port half lit inside-out | mirror reverses winding | prevent | reversed index order for port; culling | — | `SurfaceRenderer_PortHalf_LitSameAsStarboard` (VW1) |
| Visible control does nothing (UI-DEAD-CONTROL) | menus, cube, lane menus | prevent | one effect test per control; sweep extended | — | `UI_DEAD_CONTROL_ViewAndChannelControlsHaveActions` (PNL), `CommandTable_ViewRows_ExecuteOrDisabled` (PNL) |
| Painter's order wrong (lower surface over upper) | painter's algorithm | detect | rendered check from Top | — | `SurfaceRenderer_ShadedPainterOrder_TopShowsUpperSurface` (VW1) |
| Skia lease unavailable | draw operation | mitigate | fallback replaces the path (§5.3) | test | `SurfaceRenderer_RenderTargetBitmap_CapturesShadedPixels` (VW1) |
| Orbit through the pole flips the view | turntable | prevent | elevation clamped to ±90°; up vector from azimuth at the poles | — | `ViewCamera_OrbitElevation_ClampedAtPolesNoFlip` (VW1) |
| Zoom about the pointer drifts, or zooms to nothing | camera math | prevent | model point under the pointer fixed; distance clamped | — | `ViewCamera_ZoomAboutPointer_PointUnderCursorFixed` (VW1), `ViewCamera_Zoom_ClampedAtLimits` (VW1) |
| Four views starve at the minimum window (NAV-STAR-COLLAPSE) | layout | prevent | each view ≥ 320 × 240, else One view | — | `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` (VW1) |
| Focus lost when a cube face loses area or the cube hides | cube | prevent | focus moves to the current face, or to the view | — | `View3d_FocusedCubeFaceLosesArea_FocusToCurrentFace` (V3D), `View3d_CubeHiddenBelow240_FocusToViewMenuReachesPresets` (V3D) |
| Keyboard trap in 3D or a lane | focus model | prevent | Tab past the last target leaves | — | `View3d_TabPastCube_LeavesView` (V3D), `Elevation_TabPastLastPoint_LeavesView` (ELV) |
| A single-character key fires in a text field (2.1.4) | `[` `]` Z F bound | prevent | bound on the view control | — | `View3d_SingleKeys_InertInTextFieldAndBrowser` (V3D) |
| Hover probe polluted by the OS pointer (GUI-AMBIENT-INPUT) | probe tests | prevent | park the pointer first | — | `Elevation_HoverProbe_AllChannelsAtEta` (ELV) |
| Not-certified foil drawn as if checked | display | mitigate | dimmed, "· not checked", read-only | — | `ModelArea_NotCertifiedFoil_ViewsDimmedWithCaption` (VW1) |
| Mesh fails after a good one | projection error | mitigate + detect | last mesh kept with a note; event | `view.surface` error | `Controller_SurfaceComputeFails_KeepsLastMeshNoteErrorEvent` (VW1) |
| Plan and elevation disagree on a shared point | two views of one channel value | prevent | both read `Channels.View` of the same generation | — | `Elevation_FrontDihedralFrame_PointsOnLeadingEdgeLine` (ELV) |
| Twist clamp copy disagrees with the probe value | rounding | prevent | reason text formatted from the constant by the probe's formatter | — | `Copy_TwistClampReason_SameFormatterAsProbe` (PNL) |

## 10. Telemetry (normal path, no flag)

No names, paths, source text, point ids or positions in any event.

| Event | Emitter | Fields | Answers |
|---|---|---|---|
| `view.surface` (new `ShellEvent` name) | controller | duration (Stopwatch in the controller), stations, chord samples, basis (accepted · preview), outcome ok · error · stale-dropped, code | what a mesh costs; how often requests are dropped or fail |
| `view.navigate.end` (new `ShellEvent` name; optional `Frames`, `RenderP95Ms`) | View3d, ElevationView | per camera gesture | is orbit smooth (the 33 ms target) |
| `gesture.end` (M1.2b) | controller | gains `CurveFamily` rail · dihedral · twist · thickness and `ThreeDVisible` | is a drag still smooth with the 3D view open (the `simplify:` upgrade trigger reads it) |
| `apply` (existing) | Core | `edit_kind` unchanged; gains `curve_family` | which channels are edited; where refusals happen |
| `shell.pane.render` (existing) | View3d, ElevationView | outcome error; exception type | a view that fails to draw |

Every event carries the trace id. Tests: `ViewEvents_SurfaceCompute_EmittedWithDuration` (VW1),
`ViewEvents_SurfaceOutcome_StaleDroppedAndError` (VW1), `Controller_GestureEnd_CurveFamilyAndThreeDVisible` (VW1),
`View3d_NavigateEnd_EmitsFramesAndP95` (V3D), `Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions` (CH1).

## 11. UI and interaction design

**Medium:** native desktop, macOS (Apple HIG; Avalonia 11.3.14); Windows deferred. **Archetype:** `ParametricWorkbench`
(DESIGN.md) — Input PrecisionPointer + SpatialGestures + KeyboardFirst; Depth Diegetic3D; Feedback Optimistic +
Confirmed; Transition HardCut. **Technical UI:** every quantity has its unit (0.01 mm, 0.01°, 0.01 %); the probe
names every channel; the 3D and elevation captions say "display"; the shading ramp is single-hue (never a rainbow).

### 11.1 Layouts and views (key screens)

- **Plan + 3D** (default, v10 `plan3d`): Plan `2*`, 3D `1*`. **Four views:** Plan | 3D over Side | Front (v10's quad;
  CAD-08's "Top · Perspective over Front · Starboard" puts Front bottom-left — deviation D-9). **One view:** the chosen
  view alone. Each view has a label button top-left (v10 `.vlabel`, ≥ 24 × 24): click selects the view as the target of
  Display ▾, zoom and fit commands; double-click or Return → One view and back.
- **Gutter and frames (DR-VIEW-1, NS-4):** a 4 px gutter (`spacing.view-gutter`) in the window background colour separates the views, and each view has a 1 px `line` frame (One view: the frame only, no gutter); Four views keeps each view ≥ 320 × 240 inside its frame, so the arrangement needs 648 × 488.
- **Overlays** (title, caption, triad, probe) sit on a `viewport-soft` plate, so their text never lies on the shaded
  surface; the title and the cube share one row with minimum widths, and the cube hides first when the row is short.
- **3D** — focal point: the foil. Back to front: viewport fill; ground grid at z = min z (`viewport-grid`, 1 px); the
  shaded surface (both halves); the **silhouette** 1.5 px `foil`; LE, TE and tip outline 1.5 px `foil`; authored
  sections 1 px `foil-edge`; the selected station 3 px `station` with a chip naming it; the view cube (DESIGN.md row,
  top-right, 64 px); the axis triad bottom-left (x aft, y starboard, z up, letters); title top-left ("3D · Iso").
  **Wireframe:** no fill; silhouette, rails and authored sections as above; ten intermediate mesh rows 1 px
  `viewport-mute`.
- **Front · looking aft** — band (top 60 %): the Front camera over the surface (orthographic, both halves) with its
  silhouette 1.5 px `foil` (the band's top and bottom outline, as v10 `:693` draws it); the centre line
  (`viewport-mute`); the **dihedral** curve on the LE line 2 px `foil`, its dashed control polygon and points (M1.2b
  glyphs) on the starboard half. Lane (bottom 40 %, caption "Thickness t/c (%) · tip ← root", ticks at round %): the
  t/c curve, its polygon and points, on the band's span axis. Starboard is on the viewer's **left** (Front camera,
  D-2). Arrows and drags move along screen axes (→ goes toward the root here); the probe's Δ span is in model units.
- **Side** — band (top 60 %): the Side camera (from starboard; nose right; v10 draws the nose left — D-10), every
  authored section at its true placement (1 px `foil-edge`), the selected station 3 px `station` with its chip,
  intermediate sections off. Click a section to select its station. Lane (bottom 40 %, caption "Twist (°) · root →
  tip", zero line `viewport-mute`, positive up): the twist curve, polygon and points; root left, tip right.
- **Probe** (every elevation, top-right plate, `viewport-ink`, not a live region; wraps to two lines in a narrow view
  and counts as an obscuring area for 2.4.11): Front band and lanes — "η 0.412 · span 185.40 mm · height 0.00 mm · twist
  −0.84° · t/c 12.00 % · chord 113.81 mm"; Side band — the section under the pointer ("Tip · η 1.000 · twist −2.00° ·
  chord 95.00 mm") or the selected station; during a gesture, the Δ in the edited channel's unit; a clamp reason when
  clamped; the keyboard path is the focused point, which the probe follows. (CAD-08 asks for a strip under the views;
  the overlay follows M1.2b's Plan — deviation D-11.)

### 11.2 Glyphs, tokens and contrast (measured; background `viewport` #17272c)

| Element | Token(s) | Contrast |
|---|---|---|
| Channel points and handles | M1.2b §11.2 rows, unchanged (reused through `CurvePointLayer`) | 8.18–9.48:1 |
| Shaded surface | a lit ramp from `viewport-soft` #243a40 (grazing) to **`foil-shade-lit` #3f6a6c (new token)** (facing the light), clamped there with ambient included; one headlight at the camera | surface vs viewport 1.29–2.56:1 — decorative; the shape is carried by the silhouette and edge strokes |
| Silhouette, LE/TE/tip outline | `foil` 1.5 px | 8.18:1 on viewport; ≥ 3.20:1 on the brightest face |
| Edges on the brightest face | `foil` 3.20:1 · `foil-edge` 4.54:1 · `station` 3.65:1 · `warning-viewport` 3.70:1 · `danger-viewport` 3.43:1 · `viewport-mute` 3.36:1 | all ≥ 3:1 (1.4.11) — the reason the ramp tops out at #3f6a6c |
| Selected station | 3 px `station` + a chip with its name (width and label, not colour alone; `station` vs `foil` is 1.14:1) | 3.65:1 minimum |
| Overlay text (title, caption, triad, probe, lane captions) | `viewport-ink` on a `viewport-soft` plate | 10.73:1 |
| View cube | DESIGN.md "View cube" row: current face filled `station`, its letter in `viewport` (9.35:1; `viewport-ink` on `station` would be 1.48:1); focus ring `focus-ring-viewport` 3 px with a 1 px `viewport` gap (the ring is `station`-coloured, so it needs the gap on a `station` face) | 9.35:1 |
| High contrast | strokes `contrast-ink`, selection and focus `contrast-primary`; the ramp stays (decorative) | 15.41 / 12.93:1 on viewport; 6.02 / 5.05:1 on the brightest face |

Figures computed with the WCAG 2.x formula from the DESIGN.md hex values and re-computed by the UX & Accessibility lens;
UXR re-measures rendered pixels.

### 11.3 Interaction and keyboard map (every pointer verb has a keyboard path)

| Verb | Pointer | Keyboard |
|---|---|---|
| Orbit (3D) | ⌥-drag | ⌥←→ 15° (⇧⌥ 90°), ⌥↑↓ 15° (⇧⌥ 45°), `[` `]` 5°; cube chevrons are buttons |
| Pan | two-finger scroll; middle-drag; Shift-drag on empty space (Shift-drag on a point locks its axis instead); View ▸ Pan items (single pointer, no drag — 2.5.7) | 3D: ⇧+arrows; elevations: ⌥+arrows (10 % of the view) |
| Zoom | wheel, pinch — about the pointer | ⌘= / ⇧Z in, ⌘− / Z out, about the centre (or the focused point) |
| Fit all / selection | View ▸ Fit, View ▸ Fit Selection; cube | ⌘0; F |
| Named camera | cube face click | cube faces in Tab order; View ▸ Camera ▸ …; Home = Iso |
| Select a station | click its section in 3D or in the Side band | Browser row; Plan chip (M1.2b) |
| Select / move a channel point or handle | click; drag (Shift-drag locks an axis) | Tab to it; arrows nudge on the channel's ladder (⌘ · plain · ⇧) along screen axes; Return → its value field |
| Point type / tangent | context menu | Properties; Edit menu; palette (M1.2b rows) |
| Layout | Views ▾; double-click a view label | Views ▾ from the menu bar; Return on a view label |
| Display | Display ▾ | same, from the menu bar |
| Probe | hover | follows the focused point |

Tab order: the view label → (3D) the view itself → visible cube faces → chevrons → leaves; (Front) dihedral points root
→ tip → t/c points root → tip → leaves; (Side) twist points root → tip → leaves. F6 cycles regions. No trap (2.1.2);
a focused point is panned into view with the probe plate and the cube counted as obscuring (2.4.11); dragging has
single-pointer alternatives (menus) and keyboard alternatives (2.5.7).

### 11.4 Properties, Browser and copy

Point selected — heading "Dihedral · point 5 of 7" (ends: "Twist · tip end"; handle: "Thickness · point 3 · handle
toward the tip"). Rows: **Type** (Anchor / Control, M1.2b); **Span** (mm); the value — **Height** (mm) · **Twist** (°) ·
**t/c** (%) — typed with expressions and echoed in the display unit; **Tangent** (anchors); **Handles** — Dihedral: angle
(the local dihedral angle, root → tip, the same on both handles of a Smooth anchor) and length (mm); Twist and
Thickness: Span and value. Browser gains **Dihedral**, **Twist** and **Thickness** groups (the 2.5.8 equivalent path for
close points).

| Situation | String |
|---|---|
| Point name (hover, peer) | "Twist, point 4 of 7, control point, span 180.00 mm, twist −0.84°" |
| Handle name | "Dihedral, point 5, handle toward the tip, angle 3.20°, length 18.20 mm" · "Twist, point 4, handle toward the tip, span 200.00 mm, twist −1.02°" |
| Committed move | "Moved dihedral point 5 by 12.00 mm. Tip height 12.40 mm." · "Moved twist tip end by −1.00°. Tip twist −3.00°." · "Moved thickness point 2 by 0.30 %. Max t/c 12.30 %." |
| Locked dihedral root | "The dihedral root is at the centre line. It can't be moved." |
| Coupled root (twist, thickness) | "The root end and its handle move together (root mirror)." |
| Twist clamp (probe) | "Twist is limited to ±57.30° — larger angles can't be checked yet." (the number formatted from `Geometry.TwistDomainDegrees` by the probe's own formatter, so the text and the clamped reading agree) |
| t/c clamp (probe) | "t/c must stay above 0 % and below 100 %." |
| Not certified (3D, elevations) | title suffix "· not checked"; "Points can't be moved because this foil couldn't be checked. Nothing changed." (M1.2b) |
| Mesh behind | title suffix "· Updating…" |
| Mesh kept after failure | "Showing the last shape that could be drawn." |
| Render failure | "3D view couldn't be drawn. Your foil hasn't changed." · "Front view couldn't be drawn. …" Try again |
| Camera title | "3D · Iso" · "3D · Front" · "3D · Free · az 212° · el 24°" · elevation titles "Front · looking aft", "Side · from starboard" |
| Caption (3D) | "Display · x aft, y starboard, z up · twist about the leading edge" (spec §729 frame statement) |
| 3D help text | "Option arrows orbit. Shift arrows pan here; in the elevations they move a point 1 mm. Command equals zooms. F fits the selection. Home returns to Iso." |

### 11.5 Component states

| Component | default | hover / focus | active | disabled | loading | empty | error | success | overflow |
|---|---|---|---|---|---|---|---|---|---|
| 3D view | shaded Iso, outlined, cube, triad | focus ring on the view; cube face hover | orbit/pan/zoom follow the pointer; title shows az/el | dimmed "· not checked" | "Drawing…" until the first mesh; "· Updating…" when behind | no foil → Start card | failure copy + Try again; last mesh kept with note | — | view under 240 px wide: cube hidden, View ▸ Camera reaches every preset |
| Elevation (Front, Side) | band + lane + points | ring, tooltip, probe | drag: band, lane and 3D follow; Δ in probe | dimmed, read-only points | as 3D | Start card | as 3D | status-line report | close points: Browser group; colliding tick labels thinned; probe wraps |
| View cube | three depth-sorted faces, chevrons (≥ 24 px) | face hover, focus ring with gap | current face filled `station` | — | — | — | — | preset announced | a face is a target only while a 24 px circle fits inside it; otherwise View ▸ Camera is its equivalent |
| View label | name | ring | pressed (target of Display ▾) | — | — | — | — | — | — |

**Motion:** none. Camera presets, layouts and display changes are instant (HardCut); orbit, pan and zoom follow the
input directly. Reduced motion changes nothing because nothing animates (`View3d_PresetChange_NoAnimationFrames`).

### 11.6 Accessibility (WCAG 2.2 AA) and performance

- **Semantics:** the 3D view is a `Group` whose name holds only the camera ("3D view, camera Free, azimuth 212°,
  elevation 24°"), updated once per key step or at the end of a pointer gesture — never per frame; the instructions are
  its help text (§11.4). Cube faces and chevrons are Buttons ("Front view", "Orbit left 90°"). Each band is a `Group`
  named by its content ("Front view of the foil, span 900 mm, tip height 12.4 mm") that contains the lane point peers,
  which reuse M1.2b's `PlanPointPeer` (D-1 applies: Buttons with bounds, focus, selection and Invoke).
- **Announcements:** preset changes and commit reports are polite; the probe and camera titles are not live regions;
  lock and refusal copy is assertive (M1.2b rule).
- **Targets:** 28 px hit circles (M1.2b); cube faces and chevrons ≥ 24 × 24 (§11.5); view labels ≥ 24 × 24.
- **Performance budget (Inferred until measured at readiness):** `Placement.Surface` ≤ 25 ms (measured 13.1–24.0 ms
  warm with the grid maximum, spike; the Newton maximum adds a bounded number of evaluations per profile); orbit frame
  p95 ≤ 33 ms at 1440 × 900 (spec target); a channel drag frame p95 ≤ 100 ms with the 3D view visible (CAD-03).
  **`simplify:`** one mesh density (41 × 101) at rest and in gestures; ceiling: the frame budgets above; upgrade trigger:
  `gesture.end` p95 > 100 ms with `ThreeDVisible` → 21 × 51 during gestures.

### 11.7 Measured against v10

| v10 element (`workbench-v10.html`) | M1.2b2 | Note |
|---|---|---|
| Plan + 3D `2fr 1fr`, quad, single (`:104-106`) | same | Views ▾ radio items as v10 `:846`; quad order kept (D-9) |
| View labels; double-click for one view (`:205-208`) | same, plus Return | keyboard path |
| View cube top-right (`:210`) | same, DESIGN.md row | faces are buttons |
| Fit and Views ▾ in the navbar (`:211`) | same, plus Display ▾ and Fit Selection | wireframe/shaded |
| 3D sections without twist (`:694-697`) | placed by the rule, twist included | v10 is the GEOM-AUTHORITY instance |
| Side: root and tip overlaid, nose left (`:690-691`) | every authored section, true placement, nose right; twist lane | CAD-04 body plan, CAD-06 handedness (D-10) |
| Front with starboard on the right and its outline (`:692-693`) | the Front camera: starboard on the left; silhouette kept | D-2 |
| Dihedral / twist / t/c frames not drawn | frames in band and lanes, M1.2b glyphs | CAD-04 |
| Light-theme light canvas | graphite kept | M1.2b D-4 |

### 11.8 Design language

`DESIGN.md` gains `foil-shade-lit` #3f6a6c and a **Shaded surface** component row (the ramp, the headlight, the
edge-contrast floor of §11.2), an **Elevation lane** row (caption, ticks, zero line, shared span axis), and updates the
View cube row (buttons, chevrons, Home) — **written in this change** (`design-lint.py DESIGN.md`: clean, 0 warnings). UXR
re-measures the rendered pixels and owns any later token move. The ramp is single-hue (TQ colormap rule: never a
rainbow). The CD8 control is M1.2b's: `ui-craft-gate.py` reads web source, not AXAML, so the rung-2 token control is
`xaml-token-lint` in the fast ring plus `SurfaceRenderer_Brushes_AllFromThemeResources` (VW1).

## 12. Test plan

### 12.1 Triggered directives

| Trigger | Where | Directive and how it is met |
|---|---|---|
| — | all | **D0**: `Method_State_Outcome`, AAA, no sleeps (async ordering by an injected `TaskCompletionSource` gate, never timing), no wall clock in the fast ring, no culture; fixtures from bytes |
| T1 | rule, projection, channel table, camera functions, Properties rows | **D1** exact values: golden bits; the 1 nm binding; boundary fixtures for every guard — twist at the domain limit and at `Math.BitIncrement` of it, on both signs; t/c at 10⁻⁷, 0, 1 − 10⁻⁷ and 1; elevation clamp at ±90° and just inside; zoom at both distance limits; the cube at 240 and 239 px; Four views at exactly 320 × 240 and one pixel less — so the mutants "limit 58°", "positive side only", "upper clamp off", "elevation ±91°" each turn a named test red. Per-channel cases are table-driven (one name, rows per channel) |
| T2 | rule, camera | **D2** fixed-seed property tests: random fixtures (twist ±40°, dihedral ±0.2 m, chord 0.05–2 m, two profiles, knot multiplicities up to p) stay inside the certified enclosure; random camera verb sequences keep `Project(Target)` at the view centre after Fit |
| T3 | `Placement.cs` in Core; renderer in Desktop | **D3**: `Architecture_DockConfinedToShell` stays green; the Core internals boundary is the compiler's |
| T4 | save/reopen of channel edits | **D4**: real temp files through `ProjectStore` |
| T7 | receipts; CLI `inspect --json` | **D6**: synthetic golden fixtures, forged values refused |
| T8 | — | D7 not triggered |

No AI triggers.

### 12.2 Harness

The checker `tools/check-named-tests.py --design <path>` is M1.2b's precondition (m12b-points §12.2); every M1.2b2
brief runs `tools/check-named-tests.py <track> --design docs/design/m12b2-3d-elevations.md`. Desktop suites
`--views` (VW1, V3D, ELV) print `PASS <name>` like `--plan-canvas`. The extraction was run on this document at the gate.

### 12.3 Tiers

1. **Core** — PL0, CH1; the golden masters carry the planted-mutant receipt in the Proof Pack. 2. **Controller and
camera** (`--controller-shell`, pure `ViewCamera` tests) — VW1. 3. **Rendered** (class UI-RENDERED-STATE) — VW1, V3D,
ELV: whole realized window, positions through `TranslatePoint`, pixels read at projected points; a "differs" oracle is
not enough (wireframe asserts an interior pixel equals the viewport colour and counts the thin rows); state-only
assertions do not count. 4. **Action** (UI-DEAD-CONTROL) — PNL. 5. **Native** (CO-UI-READY) — rows N-3D-1 … N-3D-13
(the §0.1 steps) in `docs/reviews/m12b2-native.md`, each with an agent attach receipt before any operator session; a
VoiceOver trace of Tab through the cube and a lane, and an AX dump showing the band groups' point children (UXR).
6. **Readiness ring** (TEST-RING): `Readiness_Surface41x101_Under25Ms` (PL0), `Readiness_OrbitFrameP95Under33Ms` (V3D),
`Readiness_ChannelDragWith3dFrameP95Under100Ms` (ELV), gathered by UXR. **Plan render (VW1 decision, 2026-10-02):**
M1.2b's `Readiness_PlanRender_Under8Ms` rendered the whole window once and only printed "met"/"miss"; with the 3D view
beside the Plan it read 24.76 ms and still passed. It now measures what its budget names — the Plan canvas alone, in
One view (the pre-VW1 size, 1174 × 747 at 1440 × 900), median of nine warm frames — and fails above 8 ms (measured
3.55–4.14 ms). The whole window in Plan + 3D with the mesh drawn is a new check, `Readiness_WindowRenderPlan3d_Under33Ms`
(one frame of the §1237 33 ms target; measured 21.21–21.46 ms median; the 3D view alone is 10.1 ms median at 391 × 747,
`Readiness_ThreeDFrame_Measured`). Both were seen red under a planted 1 ms budget. 7. **Budget:** `tools/run-tests.sh` stays under
60 s (TEST-COST); estimated addition 4–7 s (Inferred: ~150 cases); each track reports its measured seconds, and a track
over budget moves its slowest rendered cases to readiness. 8. **Delegation hygiene:** foreground only; Return section
and claimed commit (HARNESS-SILENT-EXIT); two repair cycles, then stop (COORD-SPIRAL).

### 12.4 Named tests (the ledger; the checker reads this section and §9)

**PL0 — the placement rule and the display projection.**
`PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` (PL0) · `PlacementRule_CertificateGoldenMaster_AssessWitnessesAndRefusalsUnchanged` (PL0) ·
`PlacementRule_RadiansConstant_SingleSiteInSource` (PL0) · `PlacementRule_TaylorAndGridConstants_SharedByAllModels` (PL0) ·
`PlacementRule_SelectBlend_SameStationsAsCertificate` (PL0) · `Placement_DisplayWithinCertifiedEnclosure_Fixtures` (PL0) ·
`Placement_RandomFixtures_WithinCertifiedEnclosure` (PL0) · `Placement_DisplayMaximum_WithinCertifiedMaximum` (PL0) ·
`Placement_FrameLeadingEdge_EqualsCertifiedPointAtXZero` (PL0) · `Placement_SignFixture_PositiveTwistTrailingEdgeDown` (PL0) ·
`Placement_SignFixture_PositiveElevationRaisesSection` (PL0) · `Placement_PortHalf_MirrorsYOnly` (PL0) ·
`Placement_Surface_AuthoredStationsIncludedExactly` (PL0) · `Placement_Surface_AuthoredEtaOnUniformGridOnce` (PL0) ·
`Placement_Surface_NeverUsesProofBudget` (PL0) · `Placement_Surface_EvaluatorCallsBounded` (PL0) ·
`Placement_Surface_CancelledBetweenStations` (PL0) · `Placement_UnparsedDraft_DslPatch` (PL0) · `Placement_StationFrame_ChordDerived` (PL0) ·
`ChannelEvaluator_WingEstimatesFold_BitsUnchanged` (PL0) · `ChannelEvaluator_PlanformViewFold_SamplesUnchanged` (PL0) ·
`Geometry_TwistDomain_LargestAssessableDegreesPinned` (PL0) · `Geometry_ThicknessDomain_OpenIntervalOnQuantumGrid` (PL0) ·
`Assess_TwistAtDomainLimit_Certified` (PL0) · `Assess_TwistNextBinary64PastDomain_NotAssessed` (PL0) ·
`Assess_ThicknessAtUpperDomainLimit_Certified` (PL0) · `Assess_SmoothRowOnTwistChannel_Certified` (PL0) ·
`Assess_TwistSmoothRowOrdinateOffByTwoTolerances_Invalid` (PL0) · `Assess_SymmetricRowOnThicknessNotMidpoint_Invalid` (PL0) ·
`Assess_SmoothRowOnDihedral_Certified` (PL0).

**CH1 — channel editing in Core.**
`PointModel_Dihedral_RootEndFixedTipEndValueOnly` (CH1) · `PointModel_DihedralRootMirror_RootHandleZeroSpanOnly` (CH1) ·
`PointModel_TwistAndThicknessRootMirror_RootEndValueOnlyCoupled` (CH1) · `PointModel_NoLocks_RootHandleFree` (CH1) ·
`Channels_UnitTable_QuantumLadderToleranceDomainPerCurve` (CH1) · `Channels_View_AnyChannelSameShapeAsRails` (CH1) ·
`Planform_View_ComposesChannelsView` (CH1) · `UpdatePointGesture_ChannelPoints_QuantizedPerUnitTable` (CH1) ·
`UpdatePointGesture_TwistBeyondDomain_ClampedAtDomain` (CH1) · `UpdatePointGesture_TwistBelowNegativeDomain_ClampedAtNegativeDomain` (CH1) ·
`UpdatePointGesture_ThicknessBelowZero_ClampedInsideOpenInterval` (CH1) · `UpdatePointGesture_ThicknessAboveOne_ClampedBelowOne` (CH1) ·
`UpdatePointGesture_OutOfDomainCvAtBegin_NotSnappedMovesInward` (CH1) · `UpdatePointGesture_CoMovedHandleBeyondDomain_Clamped` (CH1) ·
`UpdatePointGesture_TwistSmoothHandleDrag_OppositeKeepsEtaOnLine` (CH1) · `UpdatePointGesture_TwistRootEnd_HandleFollowsRootMirror` (CH1) ·
`Gesture_DihedralDrag_OneAcceptedRowUndoExact` (CH1) · `Gesture_TwistReleaseAtDomainLimit_CertifiedApplied` (CH1) ·
`MakeAnchor_TwistControlPoint_PassesThroughWithinIdentity` (CH1) · `MakeAnchor_ThicknessAtCeiling_RefusedNamesCeiling` (CH1) ·
`MakeControl_DihedralAnchor_LocalityWithinIdentity` (CH1) · `SetTangent_TwistSymmetric_MidpointRowHolds` (CH1) ·
`ApplyPointCommand_DihedralRootEnd_DslLock` (CH1) · `Reopen_ChannelGestureRows_UndoRedoRoundTrip` (CH1) ·
`Reopen_ChannelPointTypeAndTangentRows_UndoRedoRoundTrip` (CH1) · `Reopen_UnknownCurveOnReceipt_DocReference` (CH1) ·
`Recovery_ChannelGestureRail_RefusedProjectStillOpens` (CH1) · `Cli_InspectJson_ChannelPointsRolesAndKinds` (CH1) ·
`Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions` (CH1) · `PointModel_SrOneAliases_RemovedAtExit` (CH1).

**VW1 — controller, camera, renderer, layouts.**
`ViewCamera_Presets_TopFrontSideIsoBottomBackPort` (VW1) · `ViewCamera_Front_StarboardOnViewerLeft` (VW1) ·
`ViewCamera_Side_NoseRightTrailingEdgeLeft` (VW1) · `ViewCamera_Top_MatchesPlanOrientation` (VW1) ·
`ViewCamera_AxisPresetsOrthographic_OrbitPerspective` (VW1) · `ViewCamera_OrbitElevation_ClampedAtPolesNoFlip` (VW1) ·
`ViewCamera_OrbitPivot_IsTarget` (VW1) · `ViewCamera_ZoomAboutPointer_PointUnderCursorFixed` (VW1) · `ViewCamera_Zoom_ClampedAtLimits` (VW1) ·
`ViewCamera_Fit_BoundsInsideViewportMargin` (VW1) · `ViewCamera_FitSelection_StationBoundsFill` (VW1) · `ViewCamera_RandomVerbs_FitRecentres` (VW1) ·
`ViewCamera_Title_NamedOrAzimuthElevation` (VW1) · `Controller_SurfaceAsync_StaleTicketDropped` (VW1) ·
`Controller_SurfaceRequest_NeverStalesCommit` (VW1) · `Controller_DuringChannelDrag_SurfaceFromDraftGeneration` (VW1) ·
`Controller_UndoDuringSurfaceCompute_ShowsUndoneShape` (VW1) · `Controller_SlowSurface_UpdatingShownAfter250Ms` (VW1) ·
`Controller_SurfaceComputeFails_KeepsLastMeshNoteErrorEvent` (VW1) · `Controller_ChannelGesture_SameTransitionTableAsRails` (VW1) ·
`Controller_GestureEnd_CurveFamilyAndThreeDVisible` (VW1) · `Controller_Layout_SessionValueNotPersisted` (VW1) ·
`SurfaceRenderer_RenderTargetBitmap_CapturesShadedPixels` (VW1) · `SurfaceRenderer_ShadedPainterOrder_TopShowsUpperSurface` (VW1) ·
`SurfaceRenderer_Silhouette_RenderedFoilStrokeThreeToOne` (VW1) · `SurfaceRenderer_ShadingRamp_NeverBrighterThanFoilShadeLit` (VW1) ·
`SurfaceRenderer_PortHalf_LitSameAsStarboard` (VW1) · `SurfaceRenderer_Wireframe_InteriorViewportColourTenThinRows` (VW1) ·
`SurfaceRenderer_Brushes_AllFromThemeResources` (VW1) · `Workspace_NewFoil_WindowPixelsShow3dSurface` (VW1) ·
`ModelArea_Plan3d_PlanTwoThirds3dOneThird` (VW1) · `ModelArea_FourViews_PlanThreeDSideFront` (VW1) ·
`ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` (VW1) · `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` (VW1) ·
`ModelArea_LayoutRoundTripAndTabReentry_AllViewsRenderPixels` (VW1) · `ModelArea_NotCertifiedFoil_ViewsDimmedWithCaption` (VW1) ·
`ModelArea_FirstMesh_DrawingStateThenSurface` (VW1) · `ModelArea_SamplesTabRetired_NoReferencesRemain` (VW1) ·
`ViewEvents_SurfaceCompute_EmittedWithDuration` (VW1) · `ViewEvents_SurfaceOutcome_StaleDroppedAndError` (VW1) ·
`View_RenderThrows_CopyTryAgainAndEvent` (VW1).

**V3D — the 3D view.**
`View3d_DefaultIso_RenderedSurfaceCubeAndTriad` (V3D) · `View3d_AltDrag_OrbitsTitleShowsAzimuthElevation` (V3D) ·
`View3d_PointerBindings_TrackpadPansPinchAndWheelZoomMiddleAndShiftDragPan` (V3D) · `View3d_KeyboardOrbit_Alt15ShiftAlt90Brackets5` (V3D) ·
`View3d_KeyboardTilt_Alt15ShiftAlt45` (V3D) · `View3d_ShiftArrows_Pan` (V3D) · `View3d_PlainArrows_DoNothing` (V3D) ·
`View3d_ZoomKeys_CommandPlusMinusAndZ_AboutCentre` (V3D) · `View3d_HomeIsoCommandZeroFitFFitSelection` (V3D) ·
`View3d_SingleKeys_InertInTextFieldAndBrowser` (V3D) · `View3d_CubeFaceClick_PresetOrthographicAndAnnounced` (V3D) ·
`View3d_CubeChevron_Orbits90` (V3D) · `View3d_CubeFaces_TargetOnlyWhen24PxCircleFits` (V3D) ·
`View3d_FocusedCubeFaceLosesArea_FocusToCurrentFace` (V3D) · `View3d_CubeHiddenBelow240_FocusToViewMenuReachesPresets` (V3D) ·
`View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne` (V3D) · `View3d_ClickStationSection_SelectsStationEverywhere` (V3D) ·
`View3d_SelectedStation_RenderedWidthAndChip` (V3D) · `View3d_DisplayWireframeShaded_RenderedPerMode` (V3D) ·
`View3d_SignFixture_ExampleTipTrailingEdgeRenderedHigher` (V3D) · `View3d_AutomationName_CameraOnlyUpdatedPerStep` (V3D) ·
`View3d_AutomationPeers_CubeButtonsAndHelpText` (V3D) · `View3d_PresetChange_NoAnimationFrames` (V3D) ·
`View3d_TabPastCube_LeavesView` (V3D) · `View3d_NavigateEnd_EmitsFramesAndP95` (V3D).

**ELV — the Front and Side elevations.**
`Elevation_FrontBand_RenderedFromSurfaceView` (ELV) · `Elevation_FrontBand_StarboardPointsOnViewerLeftRendered` (ELV) ·
`Elevation_FrontDihedralFrame_PointsOnLeadingEdgeLine` (ELV) · `Elevation_FrontThicknessLane_CaptionedSharedSpanAxis` (ELV) ·
`Elevation_FrontLane_ArrowsFollowScreenAxes` (ELV) · `Elevation_SideBodyPlan_AuthoredSectionsOverlaid` (ELV) ·
`Elevation_SideSignFixture_ExampleTipTrailingEdgeRenderedHigher` (ELV) · `Elevation_SideSelectedStation_RenderedFullWeight` (ELV) ·
`Elevation_SideClickSection_SelectsStation` (ELV) · `Elevation_SideTwistLane_ZeroLinePositiveUpRootLeft` (ELV) ·
`Elevation_DragDihedralPoint_BandAnd3dFollowBeforeRelease` (ELV) · `Elevation_NudgeLadders_PerChannelTable` (ELV) ·
`Elevation_ShiftDrag_PointLocksAxisEmptySpacePans` (ELV) · `Elevation_TabOrder_DihedralThenThicknessThenLeaves` (ELV) ·
`Elevation_TabPastLastPoint_LeavesView` (ELV) · `Elevation_AutomationPeers_BandGroupChannelNamesCarryValueAndUnit` (ELV) ·
`Elevation_HoverProbe_AllChannelsAtEta` (ELV) · `Elevation_SideBandHover_ProbeNamesStation` (ELV) ·
`Elevation_FocusedPoint_ProbeFollows` (ELV) · `Elevation_Probe_NotLiveRegionWrapsInNarrowView` (ELV) ·
`Elevation_SelectedPoint_RenderedFilledAtLeastThreeToOne` (ELV) · `Elevation_LockedDihedralRoot_AssertiveLockCopy` (ELV) ·
`Elevation_TwistDomainClamp_ProbeShowsReason` (ELV) · `Elevation_FocusOffscreenPoint_PansIntoView` (ELV) ·
`Elevation_ZoomPanFit_KeyboardAndPointerSameCamera` (ELV) · `CurvePointLayer_PlanAndLane_SameGlyphPixels` (ELV).

**PNL — Properties, Browser, menus, copy.**
`Properties_ChannelPoints_SpanAndValueRowsPerUnitTable` (PNL) · `Properties_DihedralHandle_AngleIsLocalDihedralBothHandles` (PNL) ·
`Properties_TwistHandle_SpanAndValueTyped` (PNL) · `Properties_TypedTwistExpression_DegreesEchoed` (PNL) ·
`Properties_TypedThicknessPercent_EchoedPercent` (PNL) · `Browser_ChannelGroups_SelectPointOnElevation` (PNL) ·
`Menu_ViewsLayouts_RadioCheckedMatchesLayout` (PNL) · `Menu_Display_AppliesToTargetView` (PNL) · `Menu_CameraPresets_ApplyTo3dView` (PNL) ·
`Menu_PanItems_PanTargetView` (PNL) · `Menu_FitSelection_FitsSelectedStation` (PNL) ·
`CommandTable_ViewRows_ExecuteOrDisabled` (PNL) · `UI_DEAD_CONTROL_ViewAndChannelControlsHaveActions` (PNL) ·
`ContextMenu_TwistPointMakeAnchor_SameEffectAsProperties` (PNL) · `StatusLine_ChannelCommitReport_PoliteLiveRegion` (PNL) ·
`WingBlock_DuringThicknessDrag_MaxTcChangesBeforeRelease` (PNL) · `Copy_TwistClampReason_SameFormatterAsProbe` (PNL) ·
`Copy_M12b2Outcomes_ExactStrings` (PNL).

**UXR — UX review and polish (Claude).** No new names: rendered and contrast re-measurement, the v10 comparison
(§11.7), VoiceOver and AX dump, native attach receipts, DESIGN.md rows, and the three readiness checks gathered.

## 13. Decisions, findings and open items

| ID | Question / finding | Default designed to | If overturned |
|---|---|---|---|
| **P-1 (ADR-0010)** | How do the certificate and every display share one placement rule? | one generic Core function family (selection, Rule A, placement) over two arithmetics; binding test at 1 nm | — (a veto returns the design) |
| D-2 | v10 draws Front with starboard on the right (a view from behind) | the true Front camera ("Front · looking aft"): starboard on the viewer's left, so the cube, the 3D preset and the elevation agree (FRAME-A); verified by the marine-CAD lens from the view vectors | only the camera direction and label change ("Back · looking forward") |
| F-10 (spec owner) | A4.8 has no t/c nudge ladder | 0.01 / 0.1 / 1 % | a different ladder is one table row |
| F-11 (spec owner) | §B1's assumption "Side = Starboard" is now built; CAD-04's "one row per authored station" is read as one overlaid outline per station | overlaid body plan; CAD-06's "F fit" is read as fit the selection, or everything when nothing is selected (⌘0 always fits everything) | stacked rows would change the Side band only; F as fit-all is one binding |
| F-12 | `MainWindow.ThicknessReadout` refuses a non-constant t/c for want of a pointwise query (`MainWindow.axaml.cs`:1248-1256) | `Placement.Frame` supplies it; PNL switches the card if `MainWindow.axaml.cs` is free, else M1.2c | — |
| F-13 (register) | Defect class **GEOM-AUTHORITY**: a display re-derives placement (v10 `draw3d` drops twist) | register entry with its controls (§9 rows 2–4) | — |
| F-14 (M1.2b) | M1.2b's Smooth co-motion and Symmetric tolerance use a mixed-unit length only on channels whose axes differ; on rails both are metres | channels use §3.6; rails unchanged | seam SR-5 |
| OI-6 | 3D vertex dragging; double-click a 3D edge to open its elevation | not in M1.2b2 | a gizmo design |
| OI-7 | Display cage (Box) | not in M1.2b2 | first define a section CV's placement under Rule A |
| OI-8 | η-plot view; 3D surface probe | not in M1.2b2 | a later slice |
| OI-9 | Perspective / orthographic toggle | axis presets orthographic, Iso and orbit perspective | one `Projection` flag already in `ViewCamera` |
| OI-10 | Front and Side linked in vertical scale and z datum | each elevation fits itself | a shared vertical scale in the controller |
| **OI-11 (PL0 review F2)** | The PL0 golden master pins outputs and refusals, not the operation tree (four math-preserving tree changes stayed green) | **CLOSED (TRACEPIN):** `Trace : IPlacementScalar<Trace>` (`tests/CfdWorkbench.Core.Tests/PlacementTrace.cs`) runs the one `PlacementRule` and `PlacementRule_OperationTree_TraceGolden` pins its operation strings (`Fixtures/m12b2/placement-operation-trace.golden.txt`; golden commit 798c926, test commit follows it). Its failure message names `QueryFeasibility`, `PlacementWidth` and `BlendPlacementWidth`. Four planted tree mutants (distributed chord, lerp-form blend, `unit*(t*½)`, reordered blend sum) each turn it red while the certificate golden stays green | a deliberate §6 change updates the trace golden only after the three bound models are hand-reviewed |

## 14. Build tracks (exclusive file ownership)

Priors (Ruling 54 P1: box = 3 × a measured prior of the same class; `app-shell-build.md` "Planned vs actual"): Grok C1
30 min, P1 45 min; Codex D3a 47 min, D3b 43 min. Harness guidance as M1.2b §14: Core tracks to Grok or Codex, long UI
tracks to Codex, UX judgement to Claude; Agy not used (two HARNESS-SILENT-EXITs). Every brief: foreground only, the
Return section required, two repair cycles, `tools/run-tests.sh` then
`tools/check-named-tests.py <track> --design docs/design/m12b2-3d-elevations.md`; export `AGENT_SESSION` per track.

| Track | Harness | Owns (exclusive) | Depends on (M1.2b joins first) | Box (prior × 3) | Exit |
|---|---|---|---|---|---|
| **PL0** placement rule | Grok | `Geometry.cs`, `Placement.cs` (new), `WingEstimates.cs` and `PointModel.cs` (the evaluator fold only), `PlacementTests.cs` (new), `GeometryTests.cs` additions, `Fixtures/m12b2/` (incl. a 2 m chord and a C⁰ thickness peak), `docs/proof/m12b2-golden/` (the golden master and its planted-mutant receipt, committed first) | M1.2b **B0** | 135 min (P1 45 × 3) | PL0 names PASS; golden committed before `Geometry.cs` changes; mutant receipt recorded |
| **CH1** channel editing | Codex | `PointModel.cs` (channel table, roles, freedoms, row rule, SR-1 aliases and their removal — after PL0), `AuthoringSession.cs` (curve set, receipts, `EditReference`), `src/CfdWorkbench.Cli/Program.cs` (channel points), `ChannelEditTests.cs`, `ReopenChannelEditTests.cs` (new) | PL0; M1.2b **B1a**, **B1b** | 140 min (D3a 47 × 3) | CH1 names PASS; no SR-1 alias left |
| **VW1** controller, camera, renderer, layouts | Codex | `WorkbenchController.cs`, `Selection.cs`, `Shell/ShellEvents.cs`, `ViewCamera.cs` (new), `SurfaceRenderer.cs` (new), `ModelArea.axaml`(.cs), `Viewport.cs` (3D mode removal), `ControllerViewTests.cs`, `ViewCameraTests.cs` (new), the `--views` entry point | CH1; M1.2b **U1a**, **U1b**; SHELLFIX | 130 min (D3b 43 × 3) | VW1 names PASS; one render path |
| **V3D** 3D view | Codex | `View3d.cs` (new, with the cube and peers), `View3dTests.cs` (new) | VW1 | 140 min (D3a 47 × 3) | V3D names PASS; three identical PASS sets |
| **ELV** elevations | Codex | `ElevationView.cs` (new), `CurvePointLayer.cs` (new, extracted), `PlanCanvas.cs` (the extraction only), `ElevationTests.cs` (new) | VW1; M1.2b **U3** (PlanCanvas settled) | 140 min (D3a 47 × 3) | ELV names PASS; M1.2b's U1b names still PASS |
| **PNL** panes and menus | Grok | `PropertiesView.cs`, `Panes/BrowserPane.axaml`(.cs), `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`, `ShellModelTests.cs` and `ShellWindowTests.cs` additions; `MainWindow.axaml.cs` only for F-12 | VW1; M1.2b **U2** | 135 min (P1 45 × 3) | PNL names PASS; UI-DEAD-CONTROL sweep green |
| **UXR** UX review and polish | Claude | `DESIGN.md` (after this design's rows), `Styles.axaml` (tokens), `docs/reviews/m12b2-native.md` (new), the three readiness checks in `tools/run-readiness.py` | V3D, ELV, PNL; M1.2b **U3** | 120 min (no same-class prior; Ruling 54: as written, measured time recorded) | a11y and marine-CAD re-review; every native row has an attach receipt |

Order: {M1.2b B0} → PL0 → {M1.2b B1b} → CH1 → {M1.2b U1a, U1b} → VW1 → {V3D ∥ ELV ∥ PNL} (width ≤ 3; ELV also waits
for M1.2b U3, PNL for U2) → UXR → join. PL0 runs **in parallel with M1.2b's B1b–U3** (it owns only files B0 released);
CH1 runs in parallel with M1.2b's UI tracks. Critical path PL0 → CH1 → VW1 → V3D → UXR = 665 min of boxes (≈ 11 h);
measured priors suggest ≈ 3.7 h.

### 14.2 Seams with M1.2b (named, each with a fallback owned here)

| Seam | Ask of M1.2b (before its track dispatches) | Fallback if not taken |
|---|---|---|
| **SR-1** `PointModel.cs` (B0) and the gesture contracts | name the value `Ordinate` (SI per curve) across the whole set — `PointView.AftMeters`, `GestureFrame.AftMeters`, the curve samples (`PlanSample`), `CombTooth`, `Planform.HandleTarget`, the controller's `UpdateGesture(…, aftMeters)` — and the freedom `ValueOnly` instead of `AftOnly`; derive roles for all five channels (Fixed root where `Geometry.cs`:298 requires 0); `Cli inspect` lists every channel | CH1 renames by Parallel Change; the old names stay as aliases only until CH1's exit, where `PointModel_SrOneAliases_RemovedAtExit` (CH1) requires them gone |
| **SR-2** `PlanCanvas.cs` (U1b) | keep glyph drawing, hit test, peers and the keyboard map in one class parameterized by an axis mapping (span, ordinate) ↔ screen | ELV extracts `CurvePointLayer` after U3, with U1b's rendered tests as the characterization net |
| **SR-3** channel inversion (B0) | `Planform.View` reuses `WingEstimates`' inversion instead of adding its own | PL0 folds both into `ChannelEvaluator` under a golden master |
| **SR-4** curve guard (B1b) | accept the curve set from one table (`PointModel.EditableCurves`) | CH1 replaces the guard |
| **SR-5** rail row rule (B0) — optional | consider §3.6's unit-free rule for rails too (F-14) | rails keep M1.2b's rule; no M1.2b2 dependency |

## 15. Deviations recorded

- **D-2** Front is the true Front camera (starboard on the viewer's left), not v10's view from behind — §13.
- **D-5b** An M1.2b build refuses a project with channel edits (`DOC-REFERENCE`); the file is unchanged — §4.
- **D-8** The M1.2a doctrine "draws only certified physical point samples" (`Viewport.cs`:12) is superseded for the 3D
  view and the elevations by ADR-0010's binding (the display is the rule over binary64, within 1 nm of the certificate).
- **D-9** Four views keep v10's quad order (Side bottom-left, Front bottom-right); CAD-08 and Rhino put Front
  bottom-left. The operator reviewed v10; the order is one grid assignment if overturned.
- **D-10** Side has the nose on the right (CAD-06's Starboard camera); v10 `drawSide` draws it on the left.
- **D-11** The probe is an overlay plate (as M1.2b's Plan), not CAD-08's strip under the views; it wraps and counts as
  an obscuring area.

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| FoilDSL file → parser → display projection | D: a file whose mesh is expensive (16-point channels, many profiles) | mitigate | fixed mesh size (41 × 101 plus ≤ 32 authored stations); off-thread single-flight; cancellable; work count bounded | `Placement_Surface_EvaluatorCallsBounded` |
| FoilDSL file → display | T: a file that certifies nothing but is drawn as if it did | mitigate | not-certified foils are dimmed "· not checked" and read-only | `ModelArea_NotCertifiedFoil_ViewsDimmedWithCaption` |
| Native envelope → reopen | T: forged `rail`/`curve` values | mitigate | closed sets in `EditReference` | `Reopen_UnknownCurveOnReceipt_DocReference` |
| Telemetry ring | I: point ids or positions leak | mitigate | curve family, counts, durations only | `Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions` |
| History | R: an edit without attribution | accept | single local user; receipts name curve and point (M1.2b) | — |
| — | S, E | not applicable | no authentication, privilege levels or network | — |

## Privacy analysis (LINDDUN-lite)

This component touches no personal data (verified: `SurfaceView`, `StationFrame`, receipts and the new events carry
geometry, counts, durations, codes and curve families only; no path, name, account or free text —
`Telemetry_ChannelEdits_CurveFamilyNoIdsOrPositions` enforces it).

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| view and gesture events (local ring) | D: disclosure through logs | mitigate | no ids, names or positions | in-memory ring of 256; gone at exit |

## Conformance notes

- ADR-0002: the source stays the only authority; every view is a projection; the placement rule is one site (ADR-0010).
- ADR-0005, ADR-0007 §3, M1.2b: point types, gestures and commits reused; the curve set widened; channel rows follow
  §3.6's unit-free rule.
- note-20260926: the proof evaluator stays independent; the binary64 evaluator stays one.
- CAD-06 and GEO-10: named cameras, keyboard orbit, Z / ⇧Z / F, fit-selection, cube, sign fixture; cage deferred (OI-7).
- No model call; T0 deterministic.

## Flagged risks and residual unknowns

- The generic refactor must reproduce certificate bits and refusals exactly; if the golden master cannot be met, PL0
  stops and reports (it does not accept "within tolerance").
- The binding is proven on fixtures and a property set, not for every file: a display point outside 1 nm on some
  untested file is possible (the certificate still decides every commit).
- Tessellation (sagitta) deviation between display points is not reported; segment interpolation stays Not assessed,
  as the 3D caption says. Cross-platform `Math.SinCos` agreement is not reviewed (Windows deferred).
- Skia lease, trackpad pinch, the painter's order and the macOS AX tree of the band groups are **Inferred** until VW1's,
  V3D's and ELV's first tests and UXR's AX dump.
- M1.2b contracts are designed, not built; SR-1…SR-4 have fallbacks; a merged signature that differs is adapted to.
- Orbit and drag budgets are Inferred until readiness measures them; the fast-ring addition is Inferred until each
  track reports its measured seconds.

## Status & next action

| | |
|---|---|
| **Completed** | M1.2b2 detailed design (this document); ADR-0010; placement spike; DESIGN.md token and rows; register entry GEOM-AUTHORITY |
| **Remaining** | M1.2c and M1.2d designs; the M1.2e design (Review workspace, layout persistence); OI-6 … OI-10; spec-owner findings F-10, F-11 |
| **Best next action** | The Coordinator relays SR-1…SR-5 to the M1.2b build plan before B0/U1b dispatch, then dispatches PL0 when M1.2b B0 joins |

## Gate record

`GATE design · 2026-09-30 · Patterns Expert, Simplifier, Test Architect (hard veto), Computational Geometry (hard veto, narrow), UX & Accessibility (hard veto), Marine-CAD UX (soft veto) · criteria met: data model first (placement rule as the single authority, ADR-0010), E7, contracts with sources and marked assumptions, named patterns past both Patterns and Simplifier, failure modes with tests, STRIDE-lite, LINDDUN-lite, telemetry, UI vs v10, 170 attributed test names (checker extract: 0 errors) · verdict: PASS WITH CONDITIONS · vetoes → resolution: UX & Accessibility HARD VETO (1.4.11, no outline stroke) → cleared on re-review; Test Architect "not yet" → cleared on re-review; all others pass · author did not self-clear`

| Lens | Initial verdict | Main findings | Repair cycle 1 | Re-review |
|---|---|---|---|---|
| Computational Geometry (hard veto, narrow) | Pass with conditions — no second authority | Rule A only partly in the rule (components, station selection, weight outside); golden base must follow M1.2b B0; grid-and-parabola maximum 3–7 × 10⁻⁴ at a C⁰ peak; mixed-unit tangent tolerance meaningless (requires a unit-free rule now); clamp over-eager; three bound models share hidden constants | `Select`, `Components`, `BlendWeight` in `PlacementRule`; golden at PL0's base; Newton maximum with knot candidates, 2 m and C⁰ fixtures; unit-free channel rows (τ_c); growing-direction clamp on every written vertex; shared constants | **Pass**; nit applied (curve-record vs span comparison comment) |
| Test Architect (hard veto) | Pass with conditions — "not cleared yet" | rendered sign fixtures only in 3D; one-sided clamps; "±57.29°" vs rounded probe; mesh-failure path, stale-drop and `ThreeDVisible` untested; re-entry and "differs" oracle; contrast floor untested; golden masters without a red receipt; ten untested verbs | 33 names added or renamed; both-sign/both-end boundaries; planted-mutant receipts; `TaskCompletionSource` gate; formatter-derived clamp copy | **Cleared** for the design; conditions for /implement: fold goldens get their own mutant receipt (applied in §3.4), red-before-green per name, golden commit before `Geometry.cs` in `git log` |
| UX & Accessibility (hard veto) | **Hard veto** (1 Blocker) | band and grazing 3D outline carried only by a 1.29–2.56:1 fill (1.4.11); `[` `]` scope (2.1.4); cube focus loss, 1:1 ring on the current face, letter contrast; band as image hides its points; per-frame name churn; pan needs a single-pointer path (2.5.7); cube target size; probe overflow; overlay text on the surface; Front lane direction | silhouette stroke in every view; keys bound on the view; cube gap, letter token, focus relocation, 24 px rule; band Groups; camera-only name per step; View ▸ Pan; probe wraps (D-11); overlay plates; lane and title copy | **Veto cleared**; conditions: DESIGN.md rows (applied in this change: cube, lane caption, silhouette) and UXR's rendered/AX/VoiceOver evidence at implementation |
| Marine-CAD UX (soft veto) | Pass with conditions | Side-band probe has no η; GEO-10 fit-selection missing and D-7 dropped CAD-06 keys; Front lane direction; orbit pivot/pole; quad and Side-handedness deviations unrecorded; Front/Side link; perspective vs ortho faces; κ; dihedral handle angle | Side-band probe names the section; F / Fit Selection and Z / ⇧Z added (D-7 removed); screen-axis rule; pivot = target, pole up-vector; D-9, D-10, OI-10; ortho axis presets; κ in §0.2; tangent dihedral angle on both handles | **Pass**; minor applied (F with nothing selected fits all, recorded under F-11) |
| Patterns Expert (advisory) | Pass with conditions (5 Major) | stale-drop keyed by an unordered hash; display channel coupled to commit cancellation; Rule A leak; port winding under a reflection; SR-1 rename set incomplete; camera "no Avalonia" claim | monotonic ticket, separate single-flight channel; `Components`; reversed port winding + culling; SR-1 whole set with CH1 owning removal; BCL operator interfaces | **Pass** |
| Simplifier (soft veto) | Pass with conditions | operation-trace spy (third implementation), public `SurfaceCompute`/`Frames`/`ChordSamples`, a compiler-enforced architecture test, `view.navigate.end` fields, per-channel test sprawl | all cut; table-driven per-channel tests; one render path after VW1 | **Pass**; last cut applied (camera uses Avalonia `Point`/`Size`, no new screen types; `SurfaceUpdating` derived from tickets) |

One repair cycle of two was used. Not convened, with reason: Hydrofoil Hydrodynamicist (no hydrodynamic quantity is computed or
displayed; twist, dihedral and t/c are geometric channels, and no washout/lift vocabulary is used); Security & Identity
(no identity, secrets, PII or network; file-input threats are in STRIDE-lite); Distributed Systems (in-process async only,
covered by the Patterns review); Data & Persistence (no new store or syntax; receipt widening follows M1.2b's accepted
pattern and is tested for reopen). Residual: every rendering, screen-reader and frame-budget claim stays **Inferred** until
the first VW1/V3D/ELV tests, UXR's native pass and the readiness ring.

---
**Handoff:** → `/implement` (tracks §14).
