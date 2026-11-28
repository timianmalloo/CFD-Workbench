---
id: design-rounded-wing-tips
title: "Design: Rounded & Radiused Wing Tips (FoilDSL 4.2, Geometry Proof, Export B-Rep & Area 2 CAD UI)"
type: design
status: proposed
owner: "@timianmalloo"
phase: "design — Wave 13A (build basis for FoilDSL 4.2 core, export, analysis, and Area 2 CAD UI)"
tags: [foildsl, geometry, wingtip, planform, curvature, radius, export, step, stl, 3dm, cad, design]
links:
  - { to: spec-rounded-wing-tips, rel: implements }
  - { to: spec-foildsl, rel: refines }
  - { to: design-language, rel: depends-on }
  - { to: design-m12b-points, rel: depends-on }
  - { to: design-planform-point-verbs, rel: depends-on }
  - { to: design-application-contracts, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: threat-model, rel: relates-to }
  - { to: privacy-review, rel: relates-to }
review-by: "2027-02-27"
summary: >-
  Build-ready /design-slice for Rounded & Radiused Wing Tips (spec-rounded-wing-tips). Freezes the
  expand-only FoilDSL 4.2 parser/serializer/canonical-identity contract, exact spanwise-local rational
  Bernstein positive-chord certification (Bernstein.CertifyPositiveChord, verified 4 splits / 0.0042%
  ProofBudget on 25 deg swept vertical-tangent tips), exact N>=4 tip radius math & ChannelEdits/PointModel/
  ChordDimension editing contracts, analytic O(1) OverallSpanMeters & s^2 terminal MAC quadrature, nested
  sin(pi*k/(2*(M-1))) tip-clustered sampling, chordal-v CadExportExtractor skinning (2.8 um error vs 302 um
  under v=eta), exact watertight STL/3MF (zero T-junctions) + STEP AP203 (verified port/starboard closed-TE
  and open-TE B-Rep orientation) + Rhino 3DM tip-cap contracts, Structure B PropertiesPane Tip group +
  PlanCanvas vertical handle (COPY-935..942), telemetry, STRIDE/LINDDUN, and red-first test matrix.
---

# Design: Rounded & Radiused Wing Tips (`FoilDSL 4.2`)

**Status:** Proposed build blueprint for Wave 13A (`docs/specs/rounded-wing-tips.md`). Zero new NuGet packages; zero new project references; 100% pure managed IEEE-754 math (`Ruling 156`, zero `Math.Pow`).

---

## 0. Evidence Ledger & Executed Spikes (Stage 3 Grounding)

Every code seam and mathematical algorithm below was **Verified** directly against `src/` and Python execution spikes on 2026-11-27:

| Fact / Spike | Verified Observation | Source / Proof | Confidence |
|---|---|---|---|
| **Spanwise-local positive-chord BFS spike** | `Bernstein.CertifyPositiveChord` (BFS over overlapping `η`-spans splitting wider `η`-piece) certifies `25°` swept vertical-tangent tips (`c_tip = 5 mm` and `25 mm`, `R_le = 25 mm`, `R_te = 10 mm`, matched & mismatched knots) in **4–8 splits** (`42,396–58,402` bit-work = **0.0042–0.0058%** of `DefaultWorkLimit = 1e9`, max `328` bits); unswept fast path takes **0 splits**; interior crossings terminate `Invalid` in `3–63` splits (`0.001–0.052%` budget). | Python faithful `Rational` spike against `Geometry.cs:371-375, 834-949, 1048-1079` | **Verified by execution** |
| **Chordal `v`-parameterization skinning spike** | On §A3 `v2` (`N=5`, `R_le=25 mm`), `CadExportExtractor.BuildSkin` with `v = η` (`:637`) yields `302 µm` error at `N=41` (fails `10 µm` floor 30×); switching to **chordal `v`** yields **`2.82 µm` at `N=41`** and `0.046 µm` at `N=81`. | Python skinning spike against `CadExportExtractor.cs:598-724` | **Verified by execution** |
| **General-`N` exact tip radius formula** | `R_tip = (3/2)·(Δu₂/Δu₁)·(x_{N-1} − x_{N-2})² / ((b/2)·(1 − η_{N-3}))` with `Δu₁ = 1 − u_{N-1}`, `Δu₂ = 1 − u_{N-2}` verified to `< 1e-6` against Cox–de Boor derivatives across `N = 4, 5, 7` (incl. triple interior knot). | Python Cox–de Boor spike against `PlanformCurvature.cs:206, 267-275` | **Verified by execution** |
| **Hidden `Tip == "open"`, `"4.1"`, `1e-7`, & UI gates** | Verified exact seams: `AuthoringSession.cs:940` (`HoldAtChordLimit`: `Tip: "open"`), `AuthoringSession.cs:1745` (`MakeAnchor`: `1e-7`), `WorkbenchController.cs:3321-3329` (`EdgeHullCrossing`: global rail hull), `PropertiesView.cs:974-1013, 1077-1091` (`h:angle` editability on `ValueOnly`), `PropertiesPane.axaml.cs:823` (`UseLink` allowlist), `SurfaceRenderer.cs:267, 337` (`IsTenth` wireframe ribs), `ExportSession.cs:265-355` (export summary rows), `WorkbenchController.cs:383-435` (`DeriveFeed` omits `Source` in `ProjectionContext`). | Direct inspection of `src/CfdWorkbench.{Core,Analysis,Desktop}/` | **Verified** |
| **`StepVerifier.Verify` B-Rep rules** | Enforces `oeIds.Length >= 2`, exact `endA == startB` chaining, `V - E + F == 2` and every edge `(+1, -1)` for `ClosedShell`, `boundaryEdgeCount > 0` for `Untrimmed`, `<= 1e-6 mm` vertex-to-pole match, zero dangling refs; allows collapsed surface pole rows without degenerate `EDGE_CURVE`s. | `src/CfdWorkbench.Core/StepVerifier.cs:45-333` | **Verified** |

---

## 1. Data Model First (DM1–DM16) & E7 Change-Surface List

### 1.1 Conceptual Model, Aggregates, Grain & History Rules

**Bounded context:** `ShapeAuthoring` (zero new aggregates; extends the existing `AuthoredDocument` / `SourceRevision` / `SurfaceRevision` aggregates of `spec-foildsl` §2 and ADR-0002).

| Concept | Aggregate / Kind | Grain | Additivity | History & Persistence Rule | Writer → Compute Reader |
|---|---|---|---|---|---|
| **Vertical-Tangent Tip Rail** | Value inside `AuthoredDocument` (`FoilDSL 4.2` source bytes) | One `leading` or `trailing` curve per `SourceRevision` | Non-additive | **Type-2 immutable** per `SourceRevision` (append-only `Sources[]` + `Accepted[]` in `.cfdw.json`). Stored purely as standard B-spline `cv { degree 3 knots […] points […] ids […] tangents { "<tip-id>" vertical } }`. Zero shadow `tip_radius` field exists. | Writer: `ChannelEdits` → `AuthoringSession`. Readers: `FoilSource.Parse`, `Geometry.Assess`, `Planform`, `Placement`, `WingEstimates`, `CadExportExtractor`. |
| **Planform Tip Radius (`R_le`, `R_te`)** | **Derived** measure (DM7 — never stored) | One derived length (`m`) per rail at `t = 1` per `SourceRevision` | Non-additive | Pure deterministic derivation from `(knots, points, halfSpan)` via `Planform.TipRadiusMeters`. Zero persisted state, zero drift. | Writer: none (derived). Readers: `PropertiesView`, `PlanCanvas`, tests. |
| **Tip Closure (`TipMode` & `TipCapRatio`)** | Value object on `Definition` (`FoilDSL 4.2`) | One `tip` clause (`open`, `point`, or `rounded <r>`) per `SourceRevision` | Non-additive | **Type-2 immutable** per `SourceRevision`. Canonical semantic JSON (`foildsl-geometry-4.0` discriminator preserved; existing `open`/`point` byte-identical): `"tip": "open"`, `"tip": "point"`, or `"tip": {"kind": "rounded", "ratio": r}` (`r` omitted in source materializes `"ratio": 1.0`, so `tip rounded` and `tip rounded 1.0` share identical `SurfaceHash`; `FoilSource.Print` writes explicit `tip rounded <r>`). | Writer: `AuthoringSession.SetTipCap` → `FoilSource`. Readers: `AuthoredProjection`, `Geometry.Assess`, `Placement`, `WingEstimates`, `StlExport`, `ThreeMfExport`, `CadExportExtractor`, `StepAp203Export`, `Rhino3dmExport`, `AnalysisProjection`, `ExportSession`. |
| **Tip Cap Fairing Surface** | **Derived** 3D mesh/NURBS (DM7 — never stored) | One analytical semi-elliptical fairing `P(x, θ)` (+ optional planar TE-end face for `closure open`) per starboard/port tip | Non-additive (`OverallSpanMeters` semi-additive across disjoint halves only) | Recomputed deterministically on demand by `TipCapMath`; never stored as a second CAD AST. | Writer: none (derived). Readers: `WingEstimates`, `SurfaceRenderer`, `StlExport`, `ThreeMfExport`, `CadExportExtractor`, `StepAp203Export`, `Rhino3dmExport`. |
| **`EditReceipt` (`tip-radius`, `tip-tangent`, `tip-cap`)** | Append-only fact in `AcceptedRow` | One receipt per committed tip edit transaction | Count additive | Append-only in `Envelope.Accepted[]`; validated on reopen by closed `NativeProject.EditReference` switch arms (`AuthoringSession.cs:3447-3481`). | Writer: `AuthoringSession`. Readers: `NativeProject.Check`, Undo/Redo history. |

**Migration rule (DM16 — Expand-Only):**
- Zero `.cfdw.json` schema breaking changes (`Envelope` keeps `"cfdw-project-1"`/`"cfdw-project-2"`).
- Existing `foildsl "4.0"` and `"4.1"` files open with **byte-identical** `Definition`, `SourceHash`, and `SurfaceHash` (zero backfill, zero silent upgrade).
- Upgrading `"4.0"`/`"4.1"` → `"4.2"` happens **only** inside an explicit user edit transaction that introduces a `"4.2"` feature; 1-step Undo restores the exact original `"4.0"`/`"4.1"` bytes.

### 1.2 E7 End-to-End Change-Surface List

| Layer | Exact Files & Symbols Touched |
|---|---|
| **1. Store / DSL & Identity** | `Core/Contracts.cs` (`AuthoredProjection`: add `string Tip`, `double? TipCapRatio`; `CommitOutcome`: add `double? LimitMeters`); `Core/FoilSource.cs` (`Definition.TipCapRatio`, `"4.2"` superset checks `:213, 1406, 1465, 1487, 1538, 1555-1576, 1613, 1656, 1665`, `Authored()` populate `Tip`/`TipCapRatio`, `EnsureHeader41` non-downgrading, `EnsureHeader42`, `Print` `:269`, `PatchTipCap`); `Core/AuthoringSession.cs` (`EditReference` `:3447-3481` new `"tip-radius"`, `"tip-tangent"`, `"tip-cap"` arms; `HoldAtChordLimit` `:940`; `UpdateGestureCore` `:822-923` & `ApplyGroupValue` `:1282-1298` `TipHandle` riding + drag clamp; `MakeAnchor` `:1745`; `PrintCurve` `:1686` → `EnsureHeader42` when `"4.2"` feature present; `PreviewRebuild` `:433-465`). |
| **2. Domain / Geometry Proof** | `Core/Geometry.cs` (`AssessWithin` `:337` admit `"rounded"`, `:371-375` → `Bernstein.CertifyPositiveChord`; `CheckTangentRows` `:455` admit terminal `"vertical"`; `GeometryCertificate.ProofScope` `:36`); `Core/ChordDimension.cs` (`:22` admit `"rounded"`, `:44-56` ride `TipHandle` on `tip-chord` edits, `:72` reason mapping); `Core/ChannelEdits.cs` (`SetTipVertical`, `SetTipRadius`, `MaxFeasibleTipRadiusMeters`, `Add` `:38-40`, `Remove` `:96-97`, `PinEndsAndRows`/`PinHandle` `:274-287`, `Strict` `:473-478`, `Rebuild` `:128-162`); `Core/PointModel.cs` (`:35, 93-94, 138-144, 209`); `Core/PlanformCurvature.cs` (`TipRadiusMeters`); `Core/WingEstimates.cs` (`OverallSpanMeters` via analytic `TipCapMath.MaxCapProtrusionMeters`, `s²` terminal MAC substitution `:89-119`); `Core/TipCapMath.cs` *(new pure zero-I/O file)*. |
| **3. Placement & Export** | `Core/Placement.cs` (`MeshEtas` `:816-827` nested `double.SinPi`, `SurfaceView` `TipMode`/`TipCapRatio` + `MaximumY`/`MinimumY` including cap protrusion); `Core/StlExport.cs` (`Close` rounded cap + convex `V(U(1))` open-TE fan + float32 zero-thickness ring collapse + sag-controlled `N_θ`); `Core/ThreeMfExport.cs` (pass `toleranceMm` to `StlExport.Close`); `Core/CadExportExtractor.cs` (`BuildInitialEtas` `double.SinPi`, chordal `vParams`, `StationVParams`, local chordal `vMidChord` in refinement `:643-662`, `v0` chordal lookup `:724`, `ExtractMasterCurvesMm` `:803-807`, `StarboardTipCap`, `StarboardTipCapTeEnd`, `HalfSizeYMm` `:695`); `Core/StepAp203Export.cs` (exact port/starboard closed-TE & open-TE rounded tip cap B-Rep); `Core/Rhino3dmExport.cs` (`Tip Cap` + open-TE `NurbsSurface`s). |
| **4. Analysis & Export Projection** | `Analysis/Labels.cs` (`RoundedTipCapNotice` `COPY-941`); `Analysis/AnalysisProjection.cs` (`ProjectionContext.RoundedTipCap`, `"Labels"` row after `:339`); `Analysis/Export/ExportSession.cs` & `ExportCopy.cs` (tip-closure summary row with `OverallSpanMeters` for STL/3MF/STEP/3DM). |
| **5. Desktop Controller & UI** | `Desktop/SurfaceRenderer.cs` (`Topology.Build` rounded tip cap + open-TE fan; `:267, 337` wireframe rib selection fallback when `IsTenth` finds `< 5` interior ribs on `SinPi` grid); `Desktop/WorkbenchController.cs` (`EdgeHullCrossing` `:3321-3329` local crossing check, `RunFeed.RoundedTipCap` & `DeriveFeed` `:383-435`, `ApplyTipTangentAsync`, `ApplyTipRadiusAsync`, `ApplyTipCapAsync`); `Desktop/PropertiesView.cs` (`Tip` group + `OverallSpanMeters` + `COPY-935..942`; lock `h:angle` as `AngleFact` when `TipEnd.Kind == Vertical` at `:974-1013, 1077-1091`); `Desktop/Panes/PropertiesPane.axaml.cs` (keyed `EnumField` dictionary pool, `:823` `UseLink` allowlist for `tip:le-radius`/`tip:te-radius`, `TipCopy` where `TipCopy.AnalysisCapNotModelled => Labels.RoundedTipCapNotice`); `Desktop/PlanCanvas.cs` (vertical guide tick along `y = half_span`, live radius tracing); `Desktop/ExportDialog.axaml(.cs)` (binds `ExportSession` tip-closure row). |
| **6. Docs Sync** | `docs/specs/foildsl.md` (mandatory per `check-foildsl-spec-sync.py` in the same Wave 13A commit that modifies `FoilSource.cs`). |

---

## 2. Delivery Phasing & Solution-Selection Ladder

- **Vertical slice:** Wave 13A (deploys & tests end-to-end independently of Wave 13B AI Notebook).
- **Mock-substitutable seams:** `ICadStepExporter`, pure `TipCapMath` static kernels, `AnalysisProjection.Build(run, context)`.
- **Solution-Selection Ladder (L1):**
  1. *YAGNI:* Zero shadow tip-radius fields; zero new `foildsl-geometry-4.2` discriminator when `"open"`/`"point"` unchanged; zero new NuGet packages; zero new theme brushes.
  2. *Reuse:* Reuse `Bernstein.Split`, `ProofBudget`, `TangentKind.Vertical`, `PointFreedom.ValueOnly`, `StlExport.Close` → `ThreeMfExport`, `CadNurbsMath.SkinSections`, `StepVerifier.Verify`, Structure B `PropertiesPane` `EnumField` + `DR-CELL-3` `UseLink`.
  3. *Bounded `simplify:` markers:*
     - `// simplify: TipCapMath evaluates G0-watertight semi-elliptical cap P(x, theta); ceiling: finite non-zero tip thickness & ratio in [0.25, 2.0]; upgrade trigger: if CNC/CFD qualification requires true G1-tangent-matched variable-cross-section tip fairings across nonzero dihedral/chord slopes.`
     - `// simplify: Bernstein.CertifyPositiveChord BFS cap maxNodes=2048, maxDepth=32; ceiling: realistic <=16-CV rails; upgrade trigger: if valid high-sweep/tight-clearance wings hit NotAssessed below DefaultWorkLimit.`

---

## 3. Exact C# Contracts, Algorithms & Patterns

### 3.1 Named Patterns
- **Canonical-Value-Object Expansion (`FoilDSL 4.2`):** Preserves single B-spline authority and `4.0`/`4.1` `SurfaceHash` byte-for-byte.
- **Exact Interval Branch-and-Bound (`Bernstein.CertifyPositiveChord`):** Exact rational Bernstein hulls over overlapping `η`-spans with zero sampling blind spots.
- **Shared Pure Kernel (`TipCapMath`):** Single deterministic math source shared by `WingEstimates`, `SurfaceRenderer`, `StlExport`, `ThreeMfExport`, `CadExportExtractor`, `StepAp203Export`, and `Rhino3dmExport`.

### 3.2 `FoilDSL 4.2` Validation Split, Canonical Identity & `EditReference` Tuples (`Core/FoilSource.cs`, `Core/AuthoringSession.cs`)
- **Clear validation layer separation (no `PatchGesture` drag crashes):**
  - **`FoilSource.Parse` (lexical / syntactic / structural):**
    - `"4.0"`/`"4.1"` files attempting `tip rounded`, rail `"vertical"` tangent row (explicitly checked BEFORE `:1556` `DSL-LOCK`), or `η_{N-2} == 1` on a rail (explicitly checked BEFORE `:1576` `DSL-CURVE`) → **`DSL-SYNTAX`**.
    - `tip rounded <number>`: bound with scale `0` in `CheckNumericRange()` (`:1665`): `NaN`/overflow → **`DSL-LEX`**; finite value `< 0.25 || > 2.0` → **`DSL-UNIT`**; omitted → `1.0`.
    - Double repeated tip abscissa (`η_{N-3} == 1`) → **`DSL-CURVE`** (`points[i][0] < points[i+1][0]` for all `i <= N - 3`, `points[^2][0] <= points[^1][0] == 1` on `leading`/`trailing` under `"4.2"`).
    - `"<tip-id>" vertical` on any index `!= N - 1` or on non-rail curves → **`DSL-LOCK`**.
  - **`Geometry.CheckTangentRows` (geometric invariants, matching `root_mirror`/`smooth`/`symmetric`):**
    - Admits `index == curve.Points.Length - 1 && row.Kind == "vertical" && curve.Path is "leading" or "trailing"`; checks `curve.Points[^2][0] == 1.0` AND `signedDx >= 1e-6` (`signedDx = path == "leading" ? x_{N-1} - x_{N-2} : x_{N-2} - x_{N-1}`), else throws `ProofRefusal(..., GeometryStatus.Invalid, "DSL-LOCK")`.
- **Canonical semantic JSON (`:1656`) & text print (`:269`):**
  - `tip == "rounded"` → `["tip"] = new Dictionary<string, object?> { ["kind"] = "rounded", ["ratio"] = tipCapRatio }` (`1.0` when omitted → `tip rounded` == `tip rounded 1.0` `SurfaceHash`); `FoilSource.Print` writes explicit `"  tip rounded " + ExactDecimal(tipCapRatio) + "\n"`.
  - `tip is "open" or "point"` → unchanged `["tip"] = tip` (`4.0`/`4.1` byte-identical `SurfaceHash`).
- **Version gates (`4.2 ⊇ 4.1 ⊇ 4.0`):** `EnsureHeader41` is an explicit no-op on `"4.2"`; `EnsureHeader42` upgrades `"4.0"`/`"4.1"` → `"4.2"` in place; `PrintCurve` (`AuthoringSession.cs:1686`) calls `EnsureHeader42` whenever the curve carries a `"vertical"` tip tangent or `η[^2] == 1.0`.
- **Closed `NativeProject.EditReference` receipt tuples (`AuthoringSession.cs:3447-3481`):**
  - `Rail == "tip-cap"`: requires `Curve == null`, `Rule == null`, `VertexId is "open" or "rounded"`.
  - `Rail == "tip-tangent"`: requires `Curve is "leading" or "trailing"`, `Rule is "free" or "vertical"`, `VertexId == child.Curves[Curve].Ids[^1]`.
  - `Rail == "tip-radius"`: requires `Curve is "leading" or "trailing"`, `Rule == null`, `VertexId == child.Curves[Curve].Ids[^1]`.

### 3.3 `Bernstein.CertifyPositiveChord` Algorithm & `ChordDimension.cs:72` Contract (`Core/Geometry.cs`)
- **Signature:** `internal static void CertifyPositiveChord(PolynomialSpan[] leading, PolynomialSpan[] trailing, ProofBudget watch)` (replaces `Geometry.cs:371-375`).
- **Exact algorithm:**
  1. **Endpoint check:** If `trailing[0].Y[0] <= leading[0].Y[0]` or `trailing[^1].Y[^1] <= leading[^1].Y[^1]` → `throw new ProofRefusal("Continuous chord is nonpositive.", GeometryStatus.Invalid)`.
  2. **Unswept fast path (`0` splits):** If `trailing.Min(s => s.Y.Min()) > leading.Max(s => s.Y.Max())` → `return`.
  3. **BFS queue over overlapping `η`-spans:** Enqueue all `(L, T, depth: 0)` where `Max(L.X[0], T.X[0]) < Min(L.X[^1], T.X[^1])`. Maintain `splits = 0`, caps `maxNodes = 2048`, `maxDepth = 32`.
  4. Loop while queue non-empty (`watch.Check()` each iteration):
     - Pop `(L, T, d)`. Compute `lMin, lMax` of `L.Y` and `tMin, tMax` of `T.Y`.
     - If `tMin > lMax` → overlap certified positive (`continue`).
     - **Proven crossing witness (sound by IVT on monotone `η`):** If `tMax <= lMin`, OR (`L.X[0] == T.X[0] && T.Y[0] <= L.Y[0]`), OR (`L.X[^1] == T.X[^1] && T.Y[^1] <= L.Y[^1]`) → `throw new ProofRefusal("Continuous chord is nonpositive.", GeometryStatus.Invalid)`.
     - **Budget/depth exhaustion (NOT a crossing!):** If `splits >= maxNodes || d >= maxDepth` → `throw new ProofRefusal("Positive-chord subdivision budget exceeded.", GeometryStatus.NotAssessed)`.
     - Increment `splits`, split the wider `η`-piece (`L.X[^1] - L.X[0] >= T.X[^1] - T.X[0]` → `Bernstein.Split(L)`, else `Bernstein.Split(T)`), enqueueing only children with `Max(c.X[0], o.X[0]) < Min(c.X[^1], o.X[^1])`.
- **`ChordDimension.cs:72` mapping:** Proven crossings emit `GeometryStatus.Invalid` + `"Continuous chord is nonpositive."` → **`DSL-EDGES-CROSS`**; budget exhaustion emits `GeometryStatus.NotAssessed` + `"Positive-chord subdivision budget exceeded."` → **`DSL-NOT-ASSESSED`** (never misclassifies budget exhaustion as crossing!).

### 3.4 Exact Tip Radius Math, `ChannelEdits`, `ChordDimension`, & Gesture Riding
- **Derived `Planform.TipRadiusMeters(Curve curve, double halfSpanMeters)`:**
  - When `curve.Points[^2][0] == 1.0`: with `n = curve.Points.Length`, `du1 = 1.0 - curve.Knots[n - 1]`, `du2 = 1.0 - curve.Knots[n - 2]`, `dx = curve.Points[n - 1][1] - curve.Points[n - 2][1]`, `dy = halfSpanMeters * (1.0 - curve.Points[n - 3][0])` → `1.5 * (du2 / du1) * (dx * dx) / dy`.
  - Otherwise: `var r = CurvatureAt(curveView, 1.0, CurveSide.Before); return r.Straight ? double.PositiveInfinity : r.RadiusMeters;`
- **`ChannelEdits.SetTipVertical(Curve curve, bool enable, double halfSpanMeters)`:**
  - Enable (`true`): if `curve.Points.Length == 4`, perform exact Boehm knot insertion at `u = 0.70` (`N = 5`) isolating mid-span sweep. Snap `P_{N-2}.eta = 1.0`. Compute `signedDx = isLeading ? (x_{N-1} - x_{N-2}) : (x_{N-2} - x_{N-1})`. Whenever **`signedDx < 1e-4`** (catches zero, near-zero, AND aft-swept TE inverted handles!), initialize `dx = Math.Sqrt((2.0 / 3.0) * (du1 / du2) * rDefault * dy)` (`rDefault = min(0.015, 0.4 * maxFeasible)`) in the valid sign direction (`P_{N-2}.y = isLeading ? x_{N-1} - dx : x_{N-1} + dx`). Attach `"<tip-id>" vertical`.
  - Disable (`false`): remove `"<tip-id>" vertical`; if `P_{N-2}.eta == 1.0`, move `P_{N-2}.eta = 0.5 * (P_{N-3}.eta + 1.0)`.
- **`ChannelEdits.SetTipRadius(Curve curve, double targetRadiusMeters, double halfSpanMeters, Curve otherRail)`:**
  - Solves `dx = Math.Sqrt((2.0 / 3.0) * (du1 / du2) * targetRadiusMeters * dy)` and places `P_{N-2} = (1.0, isLeading ? x_{N-1} - dx : x_{N-1} + dx)`.
  - If `Geometry.Assess` or `TipChord.Admits` refuses, binary-searches `MaxFeasibleTipRadiusMeters` on **`[0.0, targetRadiusMeters]`** (24 iterations), verifies feasibility at the lower end, and truncates down to `0.01 mm` (`Math.Floor(maxMm * 100.0) / 100.0`, matching `AuthoringSession.cs:1301`) returning `CommitOutcome.Refused(code: "DSL-TIP-RADIUS-BOUNDS", limitMeters: maxFeasibleMeters)` so `"Use <max> mm"` (`COPY-938`) is guaranteed admitted!
- **`ChannelEdits.Add` / `Remove` / `PinEndsAndRows` / `PinHandle` / `Rebuild` (exact live-code contracts):**
  - `Add` (`:38-40`) & `Strict` (`:473-478`, `bool allowVerticalTip`): permit `points[^2][0] == points[^1][0] == 1.0` on `leading`/`trailing` while enforcing `>= 1e-7` for all `i < points.Length - 1`.
  - `Remove` (`:96-97`): when `right == nextCount - 2` and `curve.Points[^2][0] == 1.0` (vertical tip), **skip** the Greville overwrite at `:96` and keep `points[right][0] = 1.0`.
  - `PinEndsAndRows` (`:274-275`) & `PinHandle` (`:280-287`): note live call passes `end = count - 2, handle = count - 1`. Detect `curve.Points[end][0] == 1.0` (i.e., `curve.Points[^2][0] == 1.0` on vertical tip) and pin `values[slot]` to preserve the valid vertical handle ordinate/radius (never zeroing slope into a cusp!).
  - `Rebuild` (`:128-162`): if `"vertical"` tip tangent is active (`count >= 4`), capture `rBefore = TipRadiusMeters(...)`, assign interior Greville abscissae for `0..count-3` with `points[count-2][0] = 1.0`, solve `dx` for `rBefore` (note `halfSpan` cancels out between `rBefore` and inverse), and re-attach `"<tip-id>" vertical` (on `count == 4` with `root_mirror`, `P_1.y == P_0.y` leaves `P_2` free on `η = 1` cleanly; refuse cleanly if infeasible).
- **Gesture Riding & `ChordDimension` (`AuthoringSession.cs:822-923, 1282-1298`, `ChordDimension.cs:44-56`):**
  - When `TipEnd` (`P_{N-1}`) moves (drag or typed `p:aft` or `ChordDimension` `tip-chord` edit) on a rail with active `"vertical"` tip tangent, `TipHandle` (`P_{N-2}`) translates by the exact same `Δx` (mirroring how `RootHandle` rides `RootEnd` under `root_mirror` at `AuthoringSession.cs:834-835, 1296-1297`), preserving `R_tip` and preventing handle inversion.
  - When dragging `TipHandle` (`P_{N-2}`) along `y = half_span`, `UpdateGestureCore` clamps `signedDx >= 1e-4 m` so live drag frames never cross into a cusp (`DSL-LOCK`).

### 3.5 `TipCapMath`, `WingEstimates`, & Export B-Rep (`Core/TipCapMath.cs`, `StlExport`, `CadExportExtractor`, `StepAp203Export`, `Rhino3dmExport`)
- **Analytic `O(1)` `MaxCapProtrusionMeters` for `WingEstimates.OverallSpanMeters`:**
  - Under Rule A, `max_x(unitThickness(x)) == 1.0` at `η = 1`, so exact protrusion per side is `TipCapMath.MaxCapProtrusionMeters(tipChord, tipThicknessRatio, capRatio) = 0.5 * capRatio * tipChord * tipThicknessRatio` (`O(1)`, zero profile sampling; agrees with discrete `ChordGrid` maxima in `StlExport`/`CadExportExtractor` to `< 1 µm`).
  - `WingEstimates.OverallSpanMeters = definition.Tip == "rounded" ? span + 2.0 * TipCapMath.MaxCapProtrusionMeters(tipChord, tipTc, capRatio) : span`.
- **Analytical tip cap surface (`TipCapMath`):**
  - `M(x) = 0.5*(U(x)+L(x))`, `h(x) = 0.5*(U(x)-L(x))` (`h_y == 0`), `halfThick(x) = Math.Sqrt(h.X*h.X + h.Z*h.Z)`.
  - `P(x, θ) = (M.X + cos(θ)*h.X, M.Y + sin(θ)*r*halfThick(x), M.Z + cos(θ)*h.Z)` via managed `double.SinCosPi(thetaOverPi)` (`Ruling 156`).
  - Angular count `CapThetaSteps(maxHalfThickMm, ratio, toleranceMm)`: smallest even `n >= 12` (cap `64`) with `ratio * maxHalfThickMm * (1 - cos(π / (2*n))) <= toleranceMm`.
- **Watertight STL / 3MF (`StlExport.Close` — ZERO T-junctions & ZERO zero-area triangles):**
  - Collapse any chord ring `j` whose quantized binary32 `V(U_j) == V(L_j)` (or `halfThickMm < 1e-4` rounding to identical `P(x_j, θ)` points) to a single welded vertex `V(U_j)` so closed LE/TE strips never emit collinear zero-area triangles.
  - **Open-TE (`V(U[^1]) != V(L[^1])`) planar closure (NO midpoint `M(1)`!):** Fan directly from `V(U[^1])` over `k = 1..N_θ - 1`: emit `(V(U[^1]), V(P(1, θ_{k+1})), V(P(1, θ_k)))` on starboard (reversed on port). Because the semi-ellipse is strictly convex, every triangle has strictly positive area, every interior spoke pairs `(+1, -1)`, and the final edge `V(U[^1]) → V(L[^1])` mates identically with the span TE strip's `V(L[^1]) → V(U[^1])` (`0` T-junctions, `0` zero-area triangles!).
- **Chordal `v` Skinning & NURBS Tip Cap (`CadExportExtractor.cs`):**
  - `BuildInitialEtas`: when either rail has `Points[^2][0] == 1.0`, seed `33` tip-clustered stations via `double.SinPi(i / 64.0)` (`i = 0..32`).
  - `BuildSkin`: compute cumulative chordal `vParams` across `(LE, mid, TE)` of `upperGrid`/`lowerGrid`; store `StationVParams` on `WingCadSkin`; in the refinement loop (`:643-662`), evaluate `upperSrf`/`lowerSrf` at local chordal `vMidChord = vParams[k] + (vParams[k+1] - vParams[k]) * d(k, mid) / (d(k, mid) + d(mid, k+1))` and always insert `midEtas[k]` into `etas`; pass `skin.StationVParams` to `ExtractMasterCurvesMm` (`:803-807`); in `MeasureDeviationAgainstPlacementSurface` (`:724`), compute initial guess `v0` by piecewise-linear chordal `v(sec.Eta)` lookup in `(StationEtas, StationVParams)`; set `HalfSizeYMm = halfSpanMm + maxCapProtrusionMm`.
  - **Shared-knot NURBS tip cap (`StarboardTipCap`):** Solve the 13-point uniform cubic B-spline collocation system **once** on `v_j = j / 12.0` (`j = 0..12`, `θ_j = j*π/12`) for scalar weights `(alpha[j], beta[j])` matching `(cos(θ_j), sin(θ_j))` (`alpha[0]=1, alpha[12]=-1, beta[0]=beta[12]=0`), then construct `Cap.Poles[i, j] = M_i + alpha[j]*h_i + beta[j]*r*‖h_i‖*(+ŷ)` directly from `(upperTip.Poles[i], lowerTip.Poles[i])` — guaranteeing bit-for-bit `Poles[i,0] == upperTip.Poles[i]` and `Poles[i,12] == lowerTip.Poles[i]` with shared `KnotsU` and `KnotsV`!
- **Exact STEP AP203 B-Rep Topology (`StepAp203Export.cs` — `ClosedShell`; `Untrimmed` keeps tip caps excluded so `boundaryEdgeCount > 0`):**
  - **`closure closed` (`IsOpenTrailingEdge == false`):**
    - Starboard: `f_stbd_tip_cap` on `srf_stbd_tip_cap` (`sameSense = true`), 2-edge loop `[(eStbdTipU, true), (eStbdTipL, false)]`.
    - Port (`Whole`): `f_port_tip_cap` on `srf_stbd_tip_cap.MirrorY()` (`sameSense = false`), 2-edge loop `[(ePortTipL, true), (ePortTipU, false)]`.
    - Topology: `Half: V=4, E=6, F=4 (V-E+F=2)`; `Whole: V=6, E=10, F=6 (V-E+F=2)`.
  - **`closure open` (`IsOpenTrailingEdge == true`):**
    - Starboard: emit `eStbdTipCapTe` (`srf_stbd_tip_cap.IsoU(nu-1)`, `vStbdTipTeU → vStbdTipTeL`); `f_stbd_tip_cap` (`sameSense = true`, 3-edge loop `[(eStbdTipU, true), (eStbdTipCapTe, true), (eStbdTipL, false)]`); `f_stbd_tip_te_end` (`PLANE(M(1), normal = Normalize(Cross(L(1)-U(1), +ŷ)), refDir = +ŷ)`, `sameSense = true`, 2-edge loop `[(eStbdTipTe, true), (eStbdTipCapTe, false)]`).
    - Port (`Whole`): emit `ePortTipCapTe` (`srf_port_tip_cap.IsoU(nu-1)`, `vPortTipTeU → vPortTipTeL`); `f_port_tip_cap` (`sameSense = false`, 3-edge loop `[(ePortTipL, true), (ePortTipCapTe, false), (ePortTipU, false)]`); `f_port_tip_te_end` (`PLANE(M_port(1), normal = Normalize(Cross(U_port(1)-L_port(1), -ŷ)), refDir = -ŷ)`, `sameSense = true`, 2-edge loop `[(ePortTipCapTe, true), (ePortTipTe, false)]`).
    - Topology: `Half: V=6, E=10, F=6 (V-E+F=2)`; `Whole: V=9, E=17, F=10 (V-E+F=2)`. Every edge strictly `(+1, -1)`.
- **Rhino 3DM (`Rhino3dmExport.cs`):** Emit `Tip Cap (Starboard/Port)` `NurbsSurface` from `StarboardTipCap` (`MirrorY()` for port) + for `closure open` degree-`(1×3)` `Tip Cap TE End` (`Poles[0, j] = M(1)`, `Poles[1, j] = capTeArc.Poles[j]`). Mandatory red-first gate in `T-RT-07` verifies `ns.IsValid == true` on `Rhino3dm 8.35.0` (if openNURBS flags collapsed `Poles[0, j] = M(1)`, split `Tip Cap TE End` into a non-singular trimmed/2-span planar `NurbsSurface` before merge).

---

## 4. Failure-Mode Analysis (SRE + Test Architect)

| Category | Failure Mode | Disposition | Control / Telemetry / Test |
|---|---|---|---|
| **Input** | `"4.0"`/`"4.1"` file uses `tip rounded`, `"vertical"` rail tangent, or `η_{N-2} = 1` | **Prevent** | `FoilSource.Parse` rejects with `DSL-SYNTAX` before structural checks; unit test per `"4.0"` & `"4.1"`. |
| **Input** | `tip rounded NaN` / `1e309` / `0.24` / `2.01` | **Prevent** | `CheckNumericRange()` scale `0` → `DSL-LEX` (`NaN`/overflow), `DSL-UNIT` (`<0.25` or `>2.0`). |
| **Input** | Zero-length or inverted vertical handle (`signedDx < 1e-6 m`), e.g. aft-swept TE | **Prevent** | `SetTipVertical` checks `signedDx < 1e-4`; `Geometry.CheckTangentRows` rejects with `DSL-LOCK` (`COPY-939`); `UpdateGestureCore` clamps drag frames. |
| **Input** | Double repeated tip abscissa (`η_{N-3} = η_{N-2} = 1`) | **Prevent** | `FoilSource.ConvertCurveValues` enforces `η_i < η_{i+1}` for `i <= N - 3` (`DSL-CURVE`). |
| **Geometry / Proof** | High sweep (`25°`) + vertical rounded tip where `leading(1) > trailing(0)` falsely rejected | **Prevent** | `Bernstein.CertifyPositiveChord` BFS + `WorkbenchController.EdgeHullCrossing` local check. |
| **Geometry / Proof** | Hostile near-touching 16-CV rails exhaust positive-chord BFS | **Detect + Mitigate** | Hard caps `maxNodes = 2048`, `maxDepth = 32`, `ProofBudget` → `GeometryStatus.NotAssessed` (`"Positive-chord subdivision budget exceeded."` → `ChordDimension` `DSL-NOT-ASSESSED`). |
| **Editing** | `Remove(count-3)` or `PinHandle` or `TipEnd` drag/tip-chord edit destroys `η_{N-2} = 1` or inverts handle | **Prevent** | Skip Greville overwrite on `right == nextCount - 2`; `PinHandle` uses `end = count - 2`; `TipHandle` rides `TipEnd` on drag/`ChordDimension` edits. |
| **Numerics** | MAC quadrature fails to converge due to `(1 − η)^{-1/2}` cusp at `η = 1` | **Prevent** | Exact `η = 1 − (1 − η_last) s²` substitution in `WingEstimates.IntegrateSquare`. |
| **Export** | `CadExportExtractor` skin deviates `> 10 µm` near vertical tip under `v = η` or mismatched `vMid` | **Prevent** | `double.SinPi` initial `etas`, chordal `vParams`, local `vMidChord` refinement, chordal `v0`, `StationVParams` in `ExtractMasterCurvesMm`. |
| **Export** | Open-TE (`closure open`) + `tip rounded` leaves hole, T-junction, or inward port normal | **Prevent** | Convex `V(U[^1])` STL fan + float32 ring collapse; verified port/starboard STEP AP203 2-face loops (`V - E + F == 2`); `StlExport.Check` & `StepVerifier.Verify`. |
| **State / Version** | Calling `EnsureHeader41` on `"4.2"` throws `DSL-VERSION` or downgrades header | **Prevent** | `EnsureHeader41` explicit no-op on `"4.2"`; `PrintCurve` calls `EnsureHeader42` when `"4.2"` features exist. |
| **Display** | 3D Wireframe ribs vanish on `SinPi` stations (`IsTenth` misses interior rows) | **Prevent** | `SurfaceRenderer` falls back to evenly spaced station indices (`k % 4 == 0`) when `IsTenth` matches `< 5` interior rows. |
| **Physics Honesty** | User mistakes 3D rounded tip cap as modelled in Area 3 VLM/strip | **Detect / Disclose** | Mandatory `"Tip cap"` row (`COPY-941`) in `"Labels"` group (both `PropertiesPane` & `AnalysisPanel`) + dual span (`COPY-942`). |
| **Continuity** | G0 seam between sloped skin (`dihedral'/chord' != 0`) and semi-elliptical 3D cap | **Consciously accept** | Disclosed honestly as G0-watertight fairing (`simplify:` marker); residual: reflection crease on high-anhedral tips. |

---

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| `.foil` file / `.cfdw.json` → `FoilSource.Parse` / `Geometry.Assess` | **T:** Crafted `4.2` file with forged `"vertical"` row on interior point, inverted handle, or `tip rounded 1e309` | **mitigate** | Strict `"4.2"` gate (`DSL-SYNTAX`), `index == N - 1`, `signedDx >= 1e-6 m` (`DSL-LOCK`), Ruling 15 `CheckNumericRange()` (`DSL-LEX`/`DSL-UNIT`) | `Parse_Foil42_HostileTipAndTangentInputs_Refused` |
| `.foil` file → `Geometry.Assess` | **D:** Hostile 16-CV near-crossing rails blow up `CertifyPositiveChord` BFS or `Rational` bits | **mitigate** | Hard caps `maxNodes = 2048`, `maxDepth = 32`, `ProofBudget` (`1e9` bit-work) + 32,768-bit `Rational` limit → `NotAssessed` | `Assess_HostileNearTouching16CvRails_BoundedNotAssessed` |
| `.cfdw.json` → `NativeProject.Check` | **T:** Forged `"tip-cap"` / `"tip-radius"` / `"tip-tangent"` `EditReceipt` | **mitigate** | Closed `EditReference` tuples (`"tip-cap"` `Curve == null`; `"tip-tangent"`/`"tip-radius"` `Curve in {"leading","trailing"}`, tip CV ID) → `DOC-REFERENCE` | `Reopen_ForgedTipEditReceipt_DocReference` |
| Export pipeline (`STEP`/`STL`/`3DM`) → Disk | **I:** Tip metadata or foil name leaks user path/PII into STEP header | **mitigate** | `FixedZeroPiiTimestamp`, sanitized `DatImport.Slug`, `StepVerifier` zero-PII header gate unchanged | `StepExport_RoundedTip_ZeroPiiHeaderVerified` |
| Local telemetry ring | **I:** Tip CV coordinates or user foil names leak into telemetry events | **mitigate** | Events emit only `kind`, `rail?`, `outcome`, `code`, `splits`, `durationMs` — zero names, paths, positions, or vertex IDs | `Telemetry_TipEdits_NoNamesPathsOrIds` |
| Authored history | **R:** Single local desktop user without authentication | **accept** | Offline single-user tool; append-only `EditReceipt` records operation ID, kind, and rail; residual: local file editable by same OS user | — |
| Surface | **S, E:** Spoofing / privilege elevation | **not applicable** | Pure local managed geometry & export; no auth, no network, no external process execution | — |

---

## Privacy analysis (LINDDUN-lite)

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| Local tip-edit telemetry events (`tip.edit`, `export.tipcap`) | **D:** Disclosure of foil/user names or paths through logs | **mitigate** | Local 256-event in-memory ring carries only event name, `kind`/`tipKind`, `code`, `splits`, `durationMs` — zero foil names, paths, or vertex IDs | In-memory ring cleared on exit; zero network transmission |
| `.foil` / `.cfdw.json` geometry source | **L / I / U / N:** No new personal data introduced beyond existing local geometry files | **transfer** | Governed identically by `docs/security/privacy-review.md` (local OS-user ACL, user-controlled file deletion, zero egress) | Local file under user control |

---

## 7. Telemetry & Observability (O1–O13)

- **Events emitted to `AuthoringSession` local telemetry ring (zero PII/names/IDs/coordinates):**
  - `tip.edit`: `{ kind: "tangent"|"radius"|"cap", rail?: "leading"|"trailing", outcome: "ok"|"refused", code: string, durationMs: double }`.
  - `geometry.positive_chord`: `{ fastPath: bool, splits: int, maxDepth: int, outcome: "certified"|"invalid"|"not-assessed" }`.
  - `export.tipcap`: `{ format: "stl"|"3mf"|"step"|"3dm", tipKind: "open"|"rounded", openTe: bool, thetaSteps: int, deviationMm: double, durationMs: double }`.
- **Stable error codes:** `DSL-SYNTAX`, `DSL-LEX`, `DSL-UNIT`, `DSL-CURVE`, `DSL-LOCK`, `DSL-GEOMETRY`, `DSL-EDGES-CROSS`, `DSL-NOT-ASSESSED`, `DSL-TIP-CHORD-MIN`, `DSL-TIP-RADIUS-BOUNDS`, `DOC-REFERENCE`, `EXPORT-NOT-CLOSED`, `EXPORT-STEP-INVALID`.

---

## 8. UI Design Summary (U1–U20, TQ1–TQ12, `DESIGN.md` Structure B)

- **Archetype:** Inherited `ParametricWorkbench` (`DESIGN.md:29`, verbatim).
- **Brushes (`Styles.axaml:42-73`, zero new theme brushes):** Chrome uses `PrimaryBrush`, `SurfaceBrush`, `SurfaceSoftBrush`, `InkBrush`, `MutedBrush`, `ControlLineBrush`, `WarningBrush`, `DangerBrush`; graphite `#17272c` viewport uses strictly `ViewportInkBrush`, `PlanMuteBrush`, `StationBrush`, `PlanFocusBrush`, `PlanDangerBrush`.
- **Controls (`PropertiesView.cs` + `PropertiesPane.axaml.cs`):**
  - Collapsible `PropertyGroup("tip", "Tip")` with Structure B `24 px` `PropRowInputHeight` (`DR-DEN-1`), backed by a `Dictionary<string, EnumField>` pool:
    - `tip:le-tangent` / `tip:te-tangent` (`RowKind.Choice`, `Free` / `Vertical (G1)` — `COPY-936`)
    - `tip:le-radius` / `tip:te-radius` (`RowKind.Input`, `mm`, read-only when `Free`, editable + `UseLink` allowlisted at `:823` when `Vertical (G1)` — `COPY-937`, `COPY-938`)
    - `tip:cap` (`RowKind.Choice`, `Flat (open)` / `Rounded` — `COPY-940`)
    - `tip:cap-ratio` (`RowKind.Input`, `0.25–2.00`, visible only when `Rounded` — `COPY-940`)
    - `tip:overall-span` (`RowKind.Fact`, visible only when `Rounded` — `COPY-942`)
  - When `TipEnd.Kind == TangentKind.Vertical`, `PropertiesView.TangentGroup` (`:974-1013`) and `HandleRows` (`:1077-1091`) lock `h:angle` as `AngleFact` (`RowState.Locked`, `±90°`).
- **2D Planform (`PlanCanvas.cs`):** Vertical guide tick along `y = half_span`; `TipHandle` (`PointFreedom.ValueOnly`) drags/nudges strictly along `x` with live `R = <mm>` in `PlanTracingStrip`.
- **Approved Copy (`DESIGN.md:1189-1196`):** `COPY-935..942` (all operator-approved 2026-11-27, `LAB-01` clean; `TipCopy.AnalysisCapNotModelled => Labels.RoundedTipCapNotice` single source of truth).
- **UI Craft Gate (`CD8`):** `xaml-token-lint.py` + `TokenDisciplineTests` run on every Desktop edit (`0` off-token colors, `0` C# `FontSize` literals, `0` `Color.Parse`).

---

## 9. Testing Strategy Trigger Union & Red-First Test Plan (Test Architect Gate)

**Triggered directives:** **D0** + **D1** + **D2** + **D4** + **D6** + **D7**.

| Test ID | Project | Behavior & Killable Red-First Mutant Verified |
|---|---|---|
| `T-RT-01` | `Core.Tests` | `FoilSource` `"4.2"` parse/print/round-trip, `"4.0"`/`"4.1"` byte-identical `SurfaceHash`, `EnsureHeader41` non-downgrading on `"4.2"`, `tip rounded` == `tip rounded 1.0` hash equality, `AuthoredProjection` exposes `Tip`/`TipCapRatio`. *Mutant:* `EnsureHeader41` throws on `"4.2"` → RED. |
| `T-RT-02` | `Core.Tests` | Hostile parser & geometry bounds: `"4.0"`/`"4.1"` rejects `tip rounded`/`vertical`/`η_{N-2}=1` (`DSL-SYNTAX`), double repeat `η_{N-3}=η_{N-2}=1` (`DSL-CURVE`), cusp/inverted handle (`DSL-LOCK` in `Geometry.CheckTangentRows`), `NaN`/`1e309` (`DSL-LEX`), `0.24`/`2.01` (`DSL-UNIT`). *Mutant:* let `"4.1"` throw `DSL-CURVE` instead of `DSL-SYNTAX` → RED. |
| `T-RT-03` | `Core.Tests` | `Planform.TipRadiusMeters` matches Cox–de Boor derivative jet `< 1e-9` across `N = 4, 5, 7` (incl. triple interior knot); `ChannelEdits.TipAngleDegrees` reads `+90°` LE / `−90°` TE. *Mutant:* swap `du1`/`du2` → RED on `N = 5, 7`. |
| `T-RT-04` | `Core.Tests` | `Bernstein.CertifyPositiveChord`: certifies `25°` swept vertical-tangent wing (`leading(1) > trailing(0)`), rejects interior crossing as `Invalid` (`ChordDimension` → `DSL-EDGES-CROSS`), bounds hostile 16-CV near-touching wing to `NotAssessed` (`ChordDimension` → `DSL-NOT-ASSESSED`). *Mutant:* revert to global rail-hull check → RED on `25°` swept wing. |
| `T-RT-05` | `Core.Tests` | `ChannelEdits` (`SetTipVertical` on `25°` aft-swept TE, `SetTipRadius` + `DSL-TIP-RADIUS-BOUNDS` with truncated `MaxFeasibleMm` guaranteed admitted, `Add`, `Remove(count-3)` preserving `η_{N-2}=1`, `Rebuild(count=4,5,7)`, `PinHandle`, `TipChord.Admits` on legacy `<5 mm` wing), `ChordDimension` `tip-chord` edit riding `TipHandle`, `PointModel` (`TipEnd` `Vertical`, `TipHandle` `ValueOnly`), `AuthoringSession` 1-step Undo back to `"4.1"`, `.cfdw.json` save/reopen & forged receipt refusal (`DOC-REFERENCE`). *Mutant:* omit `Remove` `η_{N-2}=1` guard or accept forged `"tip-cap"` receipt with non-null `Curve` → RED. |
| `T-RT-06` | `Core.Tests` | `WingEstimates` exact `s²` MAC & Area agree with 2048-pt reference `< 1e-6`; analytic `OverallSpanMeters` matches `2*(halfSpan + 0.5*ratio*c_tip*tc_tip)` and agrees with STL/STEP `SizeYMm` within `< 0.01 mm`. *Mutant:* disable `s²` MAC substitution → RED `Converged == false`. |
| `T-RT-07` | `Core.Tests` | Watertight export suite for BOTH `closure closed` AND `closure open` (`0.5 mm` TE gap) × `Whole`/`Half` × `tip rounded 1.0`: `StlExport.Check` (`Closed`, `0` unpaired edges, `0` zero-area tris, deviation `<= 0.02 mm`, cap sag `<= 0.02 mm`), `ThreeMfExport`, `CadExportExtractor` chordal `v` (`<= 0.010 mm`, `HalfSizeYMm` matches `OverallSpanMeters`), `StepAp203Export` + `StepVerifier.Verify` (`V-E+F==2`, outward port/starboard normals, zero-PII), `Rhino3dmExport.Verify` (`IsValid == true` on `Rhino3dm 8.35.0`). *Mutant:* revert `CadExportExtractor` to `v = η` or use midpoint `V(M(1))` fan → RED. |
| `T-RT-08` | `Analysis.Tests` | Area 3 `AnalysisService` succeeds on `tip rounded` (`c_tip >= min`), `AnalysisProjection` emits `COPY-941` (`"Tip cap"`) in `"Labels"`, preserves `"Not judged — tip strip"` (`COPY-220`), `ExportSession` emits tip-closure summary row; `ArchitectureTests` passes. *Mutant:* omit `COPY-941` from `"Labels"` → RED. |
| `T-RT-09` | `Desktop.Tests` | Structure B `PropertiesPane` `Tip` group renders `COPY-935..942` across Light/Dark/HighContrast, `h:angle` locked on `TipHandle`, `DR-CELL-3` `"Use <max> mm"` repairs `DSL-TIP-RADIUS-BOUNDS`, `PlanCanvas` constrains `TipHandle` drag to `x`-axis only, `SurfaceRenderer` renders wireframe ribs on `SinPi` grid, `WorkbenchController.EdgeHullCrossing` gives 0 false alarms on `25°` swept wing, `AnalysisPanel` + `PropertiesPane` both show `COPY-941`, `TokenDisciplineTests` + `xaml-token-lint.py` green, telemetry ring has zero names/IDs/paths/coords. *Mutant:* leave `h:angle` editable on vertical `TipHandle` → RED. |

---

## 10. Confidence Ledger & Residual Risk

| Item | Confidence | Residual Risk & Mitigation |
|---|---|---|
| `Bernstein.CertifyPositiveChord` BFS proof | **Verified by execution** (`4–8` splits / `0.005%` budget) | Extreme adversarial 16-CV rails may hit `maxNodes = 2048` → honest `NotAssessed` (never false `Certified`). |
| Chordal `v` skinning in `CadExportExtractor` | **Verified by execution** (`2.82 µm` at `N=41`) | External CAM open-and-measure (`SPIKE-CAD-02`) remains a manual check on rounded-tip STEP files when CAM apps run. |
| `tip rounded` G0-watertight STL/3MF & STEP AP203 B-Rep topology | **Verified** (Euler & edge-orientation exact) | G0 reflection crease across cap seam when `dihedral'/chord' != 0`; explicitly accepted and disclosed. |
| `Rhino3dm 8.35.0` `NurbsSurface.IsValid` on collapsed-pole `Tip Cap` & `Tip Cap TE End` | **Flagged** (not yet executed against `Rhino3dm 8.35.0`) | Mandatory red-first gate in `T-RT-07`; fallback specified (non-singular 2-span planar NURBS for `Tip Cap TE End`) if openNURBS rejects singular pole row. |
| `check-foildsl-spec-sync.py` gate | **Verified** | Update `docs/specs/foildsl.md` in the same Wave 13A commit that modifies `FoilSource.cs`. |
