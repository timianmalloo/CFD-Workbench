---
id: kb-hw-open-questions
title: "Open questions and domain failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, generated, open-questions]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
review-by: 2026-12-19
summary: >-
  What the research could not settle, the cheapest next probe for each, the silent and expensive failure modes of the domain, and the disconfirming views that were sought, compiled from the area files.
---

# Open questions and domain failure modes

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

## CAD programs and their UI/UX paradigms for a precision parametric foil editor

*Source: [01-cad-programs-and-ux.md](01-cad-programs-and-ux.md) (`kb-hw-cad-programs-and-ux`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe in parentheses).**
1. Does the chosen degree-5 evaluator admit real-time direct manipulation with derivative constraints at anchors (Fowler-Bartels)? (Spike: implement the constrained least-squares move for one curve in C#, measure solve time per drag frame.)
2. Does Rhino's Fair contract transfer to degree 5, or does automatic fairing need a different formulation (energy minimisation)? (Spike: run `Curve.Fair` via rhino3dm on a degree-5 chord curve and record the achieved tolerance; rhino3dm is MIT and already in the permissive path.)
3. Which UI framework Plasticity uses, and whether any indie NURBS tool documents a colour-vision-deficiency mode. (One vendor email or one release-notes read; low value.)
4. Does Alias 2026 support macOS at all? (Open the Autodesk requirements page from a browser; fetch returned 403.)
5. Current Autodesk Fusion VPAT edition and whether the 2018 exceptions still stand. (Open the Section 508 index [S22] and download the newest Fusion ACR.)
6. Avalonia's accessibility tree on macOS and Windows for a custom-drawn viewport control: can a station be exposed as a focusable, named element? (Spike: one AutomationPeer on a custom control, read with VoiceOver and Narrator.)
7. Blender's numeric input grammar as a reference implementation for our expression parser: which operators and unit tokens exactly. (Open the manual page [S31] and copy the token table.)
8. Users' interpretation of "influence weight" remains unobserved (grounding register); no comparable exposes it as a first-class control. (Five-user formative test on the mockup, as the spec's A7 already requires.)

**Domain failure modes.**
- *Silent unfairness*: a curvature reversal invisible in shaded view and only visible on the comb; mitigated by the persistent comb and the monotone-piece counter (section 6).
- *Mode confusion*: editing an influence control believing it is an on-curve anchor; the novice study measures this class of error as the second most frequent difficulty; mitigated by distinct glyphs, labels and the status strip.
- *Modifier collision across OS*: Alt+arrow (Rhino nudge) versus Option-key dead keys on macOS; Ctrl-click meaning right-click on a one-button Mac; mitigated by the per-OS table and by never assigning a meaning to Ctrl-click on macOS.
- *Bitmap-stretched viewport on DPI change*: wrong pick radius and blurred combs; mitigated by PMv2.
- *Half-applied preview*: a failed Apply that leaves some controls changed; mitigated by Blender's rollback-before-report rule.
- *Preset drift*: users who chose a navigation preset lose it on reinstall or on the second machine; mitigated by storing presets in the project-independent user profile and exporting them with settings.
- *Public-by-default data*: Onshape's free tier makes documents public; a local-first tool must never default any share or telemetry to public.

### Disconfirming views sought

1. *"Few master controls" is too restrictive for expert users.* Counter-evidence sought in Alias and Rhino guidance; both point the other way (minimum CVs, single span, Rebuild to fewer points). The surviving form of the objection is that local insertion must be cheap and local (Forsey-Bartels), which A4 already provides. Objection reduced to a requirement, not overturned. ([S13][S14][S57])
2. *Keyboard-first is unnecessary because CAD is a mouse domain.* Onshape documents keyboard-only orbit, zoom and pan, and its forum shows demand ("Handling without a mouse"); Fusion's own VPAT lists keyboard gaps as exceptions to be fixed, not as design intent. The objection fails; keyboard-first is a documented direction of the market. ([S21][S26][S60])
3. *A feature tree would give users a familiar model.* The 2025 novice study locates the most frequent beginner difficulties in exactly the tree's mode and tab structure (Sketch vs Solid, in/out of sketch). For a five-curve domain the tree adds the measured confusion without adding capability. Objection fails for v1; a domain history (C1) retains the benefit (re-editable parameters). ([S50])
4. *Rhino is non-history, so "Rhino-like" implies no dependency tracking.* Rhino's History command contradicts this reading; the loft-updates-from-curves example is our model. See "Contradicts existing repo knowledge" below. ([S5])
5. *Accessibility can be inherited from the UI toolkit.* Blender shows the opposite: a custom-drawn toolkit has no accessibility tree at all, and Fusion (Qt) still lists exceptions. The objection fails; the proof must be product-level. ([S21][S33])
6. *Comparables already show a good empty state; copy it.* No comparable page opened this session documents one; the market commentary is about install time. Objection unsupported; GAP-16 remains open. (Flagged)

**Contradicts existing repo knowledge.** `docs/knowledge/sources/bench-cad-ux.md.txt` §1 states Rhino "is non-history and cannot construct from a feature tree". The second half is true; the first is incomplete: Rhino's `History` command records a parent-child connection so that "when the input geometry changes, the result updates accordingly", and the documented example is a loft updating when its input curves are edited; it is opt-in per command or "Always Record History" *(Verified, [S5])*. The correction matters because the repo's chosen model (curves and stations as parents, loft as child) is precisely Rhino's History semantics, and the existing text implies the opposite. No other contradiction found; the grounding register's corrections (degree-5 does not guarantee G4; catalog-to-CST is a fit with residual) are consistent with the sources read here.

## Parametric curves, lofts, splines and surfaces for foil profiles and wings

*Source: [02-parametric-curves-lofts-and-surfaces.md](02-parametric-curves-lofts-and-surfaces.md) (`kb-hw-parametric-curves-lofts-and-surfaces`).*

### Open questions and domain failure modes

**Open (with cheapest next probe):**
1. Kulfan's A_0=√(2R_LE/c) and A_n=tanβ+ζ_TE identities — *probe:* fetch the 2008 J. Aircraft paper via the AIAA DOI or NTRS mirror; or verify numerically by fitting a NACA 0012 in AeroSandbox and comparing √(2·0.0110·…) to A_0 (10 lines of Python).
2. Actual residuals of degree-5 B-spline LSQ and of CST (n=8,12) on the nine catalog profiles — *probe:* a fixture script with geomdl/scipy over the catalog DAT files; record max normalized deviation per method and order.
3. Deviation between loft option A (channel-evaluated) and option B (skinned nets) for a representative wing (washout 2°, taper 0.35, 4 stations) — *probe:* build both in Python, sample 50×200 points, report max distance; if < identity tolerance for typical wings, option B can be the exporter's default without a warning.
4. Whether a fairness term ∫(C''')² (linear) is an adequate proxy for MVC's ∫(κ_s)² on foil sections — *probe:* compare comb plots on a smoothed E817 upper surface; if visibly worse, budget a nonlinear MVC pass (Ceres BSD-3 or hand-written Gauss–Newton).
5. Cost and conditioning of the KKT solve at 60 controls × 20 constraints in .NET (Math.NET) — *probe:* `dotnet run app.cs` micro-benchmark with condition-number readout; expected sub-millisecond (Flagged until measured).
6. STEP `B_SPLINE_SURFACE_WITH_KNOTS` acceptance by Fusion/Mastercam for a degenerate-tip surface — *probe:* write one file with the own writer and open it (decision-0001's residual risk).
7. `SharpSpline` — does it exist under a permissive licence? *probe:* NuGet search; otherwise drop the name.
8. .NET double round-trip formatting guarantee for JSON persistence — *probe:* a 100-line fixture that writes/reads 10⁶ random doubles and asserts bit-equality.

**Failure modes in this area (silent, expensive or irreversible):**
- *Silent:* a refit (knot removal, degree reduction, mode switch, exporter skin) that changes shape below visual threshold but above hydrodynamic relevance — always compute and display the deviation. Curvature reversals between sparse stations hidden by shading. NURBS weights ≠ 1 accidentally introduced (e.g. from an imported 3DM) that break polynomial export. A parameterization mismatch (chord vs centripetal) between save and load that re-derives different knots from the same anchors.
- *Expensive:* rebuilding compatible sections by knot merging each edit (control-point explosion [S33]); demanding 1 µm convergence in Newton projections; nonlinear fairing in the interactive loop.
- *Irreversible:* saving evaluated coordinates as the record (the controls, weights and constraints are lost); regenerating a 6-series catalog section from a formula (there is none [S28]); collapsing symmetry by editing one half without a stored mirror relation.

### Disconfirming views sought

1. *"NURBS rational weights are the natural 'influence weight'; the spec should just use them."* — Fared: the executed probe confirms they attract monotonically [S29], but they alter parameter speed, make derivatives rational, provide no constraint mechanism, and forbid polynomial STEP entities; the LSQ weight gives the same user-facing behaviour without those costs. **Rejected for shape; kept as a documented alternative.**
2. *"CST should remain the internal representation, as the bench decided."* — Fared: strong for optimization [S9][S10]; weak for editing (global support, no local constraints, no insertion). **Partially upheld: CST as optimizer coordinates, not as the editor's record.**
3. *"Degree 3 is enough; degree 5 is over-engineering."* — Fared: cubic gives C² (G2) which meets the spec's floor; quintic gives continuous curvature *rate*, which the pressure distribution feels, at the cost of stiffer local control and six-control minimums; the spec already commits to degree 5 [S30]. **Upheld as the default with the caveat that "G4" is a property of interior simple knots only.**
4. *"A B-spline skin through station nets is the record; channels are just a UI."* — Fared: it yields exact STEP but silently re-defines twist between stations; the spec's A4 wording makes channels authoritative. **Rejected as the record; accepted as the export path with a reported deviation.**
5. *"1 µm persistence tolerance is unrealistic."* — Fared: it is Rhino's tightest template (0.001 mm) and ~10¹⁰ ULPs at 1 m; it is realistic as an identity tolerance and unrealistic as an operational tolerance [S15][S16]. **Upheld with the three-tolerance split.**
6. *"Weighted least squares cannot express 'weights attract but a station value is exact'."* — Fared: constraint rows in a KKT system do exactly that; the LSQ with w→∞ converges to the same limit [S29]. **Rejected.**
7. *"rhino3dm can do the loft when it gets hard."* — Fared: its API has no loft/interpolation/projection [S17][S18]. **Rejected; curvo or own code.**

## Marine and board CAD tooling: stations, master curves and loft UX

*Source: [03-marine-and-board-cad-tooling.md](03-marine-and-board-cad-tooling.md) (`kb-hw-marine-and-board-cad-tooling`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe in brackets):**
- Is Shape3d's Mac port Wine-based, and does it support Apple Silicon natively? [Install Shape3d Lite on a Mac; inspect the bundle for `wine`/`wineskin` binaries.]
- Does Shape3d V9 still offer "display thickness between any two curves" as the repo claims? Not found in the V9 manual extract. [Search the V9 manual page for "between two curves"; else the claim is Flagged and should be removed from `bench-cad-ux`.]
- What is the exact spanwise interpolation Shape3d uses between slices (spline order, parameterization)? The manual says only that corresponding points are joined by curves. [Export a two-slice board as IGES and inspect the surface degree/knots.]
- BoardCAD's licence (GPL-2.0? GPL-3.0?) and Flow5's licence terms. [Open the repositories' LICENSE files; open flow5.tech licence page.]
- OpenVSP's actual spanwise interpolation of section shape between XSecs (linear in normalized coordinates? by parameter?), which the enums do not reveal. [Read `WingGeom.cpp` / `XSecSurf` in the OpenVSP source; or export a two-section wing as STEP and check surface degree in v.]
- WingHopper's distribution UI (sliders vs curves) and its licence/price. [Open the app; read its terms page.]
- Whether MultiSurf's "Foil-lofted" surface uses a NACA 4-digit family only. [AeroHydro entity reference for "Foil-lofted Surface".]
- Farin & Sapidis 1989 exact wording. [Library access to IEEE CG&A 9(2) or the ResearchGate copy.]

**Domain failure modes:**
- **Silent kink at the symmetry plane.** A non-zero spanwise slope at y = 0 in any channel produces a G0-only root; smooth shading hides it and the estimator does not care, but VLM panels and STEP surfaces show it. Prevent with the root-tangent invariant (implication 4).
- **Twist of the loft from bad correspondence.** Mismatched section parameterization (LE not aligned, opposite orientation) twists a ruled loft; Onshape's forum is full of this. Prevent by normalizing every section (LE at u=0.5 or an explicit LE index) before lofting and fixturing it.
- **Curvature reversal between sparse stations.** Interpolating splines through three stations can overshoot (already flagged in `bench-parametric-geometry`); Shape3d's "fewer slices is smoother" is the practitioner's mitigation, the comb and the monotone-piece count are the instrument.
- **Approximate export mistaken for exact.** The MultiSurf→NURBS lesson: an IGES/STEP written by fitting looks identical and drifts; report deviation and degree on every export.
- **Zoom-scaled nudge without a visible step** yields un-auditable edits (an edit's magnitude depends on the window size). Keep the step visible and fixed by default.
- **Proprietary-format lock-in.** Reading `.s3dx` without a schema invites silent misreads; interop through open text formats instead.

### Disconfirming views sought

- **"Shape3d is the right UX model to copy" (findings 1–4).** Counter: AkuShaper's simpler three-control-point curves and OpenShaper's minimal UI show shapers accept far less machinery. Outcome: partially fared — Shape3d's precision gestures and Tracing are worth copying, but the spec should not copy its equal-control-count slice rule or its option ladder; keep the gesture set, drop the structural constraint (implication 8).
- **"MultiSurf is not NURBS and cannot round-trip" (finding 8).** Counter: AeroHydro says it is *"NOT limited to NURBS"* and exports IGES. Outcome: the repo's claim is overstated but its conclusion holds — NURBS is one of its surface types, but the relational model exports by approximate one-way conversion [S8][S10].
- **"A foil editor needs almost no loft options" (finding 12).** Counter: Fusion/Onshape users rely on rails and vertex matching to control twist. Outcome: held — those controls fix problems created by unowned inputs; our distributions *are* the rails and our normalized sections *are* the matched vertices. Residual risk: if v2 admits arbitrary imported sections, the vocabulary returns.
- **"Purpose-built hydrofoil CAD is thin" (finding 14).** Counter: KaroroCAD is used to launch commercial wings. Outcome: not refuted, not confirmed — no manual or feature list was found; Flagged.
- **"Small station counts are best" (finding 1).** Counter: XFLR5/AVL users add many sections for panel control. Outcome: those sections are analysis sampling (cosine spacing), not authored geometry; the spec's separation of authored stations from inspection slices resolves it.
- **"The Mac port is a wrapper" (finding 6).** Counter: it could be a recompiled Qt build. Outcome: unresolved; Shape3d's own page credits a third-party porting service, which is the wrapper pattern, but nothing opened states Wine. Flagged.

## Hydrofoil disciplines and design data for water-sports foils

*Source: [04-hydrofoil-disciplines-and-design-data.md](04-hydrofoil-disciplines-and-design-data.md) (`kb-hw-hydrofoil-disciplines-and-design-data`).*

### Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Race wingfoil board speeds at 10 and 20 kn wind (upwind/downwind VMG) — no dataset opened | fetch IWSA/GWA event GPS analyses or a GPS Formula export; or one instrumented session (GPS + wind) |
| iQFOiL 900 front-wing span, section t/c, tail span | fetch the 2025 iQFOiL class rules PDF (equipment appendix) |
| Formula Kite dimensional maxima (span, total length, mast) | fetch the current IKA Formula Kite class rules PDF from kiteclasses.org |
| Pump cadence and heave amplitude, measured | one IMU session (phone accelerometer at 100 Hz) on a dock start; compute f and A |
| Speer H105 design Re/CL, cavitation comparison, coordinate rights | retry tspeer.com with certificate override or the IHS mirror; record rights before vendoring |
| Hough & Moran / Wadlin & Christopher formulae | fetch the NASA/NACA TRs from NTRS and transcribe the lift-slope correction |
| Whether Moth "overall beam" includes foils | read rule 6.2 definitions in the Dec 2024 edition |
| E-foil wing geometry and motor thrust | fetch Fliteboard/Waydoo wing spec pages (areas/spans) |
| Stabilizer spans/AR/anhedral per brand | fetch three stabilizer spec pages (Axis, Armstrong, Sabfoil) |
| Mast EI / stiffness numbers | none published; would need a bench deflection test |

Failure modes in this domain: **silent** — using deep-water polars at h/c < 4 (up to ≈17% lift error at competition Fr [S16]); applying a density multiplier for salt water without re-evaluating Re; treating a maker's AR as b²/S. **Expensive** — sizing a race wing at CL_cruise without checking take-off CL_max at Re 4×10⁵, producing a foil that never lifts in 10 kn. **Irreversible** — declaring a design "cavitation-free" or "ventilation-safe" from a steady estimator; a foil failing or ventilating at 30+ kn injures the rider (GAP-14 safety framing stands).

### Disconfirming views sought

- **"AR conventions are consistent; the differences are rounding."** Fared badly: Armstrong MA rows match b²/S to two decimals across six sizes while Axis ART Pro rows deviate by +1.6% to +2.0% in the same direction across nine sizes — systematic, not rounding. The Leviathan 1060 −3% is a single retailer row and may be a copy error (kept Flagged).
- **"The 35–45 kn ceiling is folklore; modern thin sections go faster."** Partly holds: the arithmetic shows the ceiling is set by −Cp_min at the operating CL and depth, not by a fixed speed; a −Cp_min ≈ 0.3 section at 1 m depth reaches ≈50 kn. But the observed records (37.9 kn wing, ≈38 kn Formula Kite, 39.1 kn kite 1 NM) sit inside the band, so the band stands as the *practical* limit at consumer depths and loadings.
- **"Free-surface effects are negligible for foils 0.8 m deep."** Holds only for h/c ≥ 5–10; at c̄ = 0.09 m and h = 0.5 m, h/c = 5.6 — marginal; the CEHINAV data [S16] put the affected region at h/c < 4–5 with Fr_h ≈ 5, which is exactly the racing regime, so depth must remain an input.
- **"Pumping is a propulsive flapping-foil problem; use St ≈ 0.3 design rules."** Fared badly on the Inferred St ≈ 0.05–0.15 of human cadence — pump-foil design is glide-limited, not thrust-limited; the flapping literature informs the unsteady tier, not the preset.
- **"The GWA box rule makes race-foil design moot."** Fared badly: the rule fixes minima only; area, AR, span, section and configuration remain free above 700 cm², and the production-run requirement (≥50) favours tools that produce manufacturable, documented geometry.
- **"A race board in 10 kn of wind does not reach 10 kn boat speed; take-off is pump-assisted and the whole speed axis of the goal state is wrong."** Sought because every board speed in the goal-state table is Flagged (no GPS dataset opened). Fared: partly holds — the iQFOiL package is designed for 5–35 kn wind [S26] and light-wind wingfoil racing is pump-assisted (practitioner reports, tertiary), so the 10 kn-wind take-off row is the least trustworthy row and pumping is not modelled by any steady tier in this base; the 20 kn-wind rows are bounded above by the 37.9 kn record [S20] and below by the manufacturer envelopes [S9][S10], which is the only corroboration available. Disposition: the speed axis stays **Flagged** and every bound derived from it (take-off CL 0.6–0.8, cruise CL 0.1–0.45, wing loading 8–20 kPa) is Inferred-on-Flagged until an instrumented session or a race GPS export replaces it; the index tags them so.
- **"The 17 % free-surface loss is a laminar-bubble / Reynolds artefact of a 10⁵-Re model with rear wing and mast present, not Froude physics."** Sought because the test Re (0.7–2.9×10⁵) is an order below full scale and the model carried a stabilizer and strut [S16]. Fared: not refutable from this base — the paper reports the loss as a Froude effect at fixed h/c and the deep-water asymptote at h/c > 5 is Re-independent in its own data, but the Re confound is real for a 63-210 section near its laminar-bubble regime. Disposition: the 17 % is carried as measured at model scale with the confound named; a full-scale confirmation is the cheapest next probe.
- **"Seawater just adds 2.7% lift."** Fared badly: Re drops 4.3% at the same speed and chord (15 °C); the effect on CL/CD through laminar-bubble behaviour at Re 4–8×10⁵ can exceed the density gain in either direction — must go through the polar.

**Contradicts existing repo knowledge.** Nothing in the repo was overturned. Two re-centrings are recorded: (1) the proposal's preset bands (`proposal-sequence.md` §3.1a: surf AR 4–6, freeride AR 6–8) sit below the 2025–2026 market, where the lowest production AR opened is 7.1 (Armstrong MA) and freeride lines run 8–9.5 (Code S, Unifoil Progression, F-One SK8) — the bands are not wrong, they are dated, and the presets should be re-centred on the product table above; (2) the Bench claim "seawater yields about 2.7% more force at identical geometry/speed/angle" is confirmed numerically (ρ ratio 1.0269 at 15 °C) *and* the reconciliation register's objection is strengthened: ν_sea/ν_fresh = 1.044 at 15 °C, so Re is 4.3% lower, which at Re 4–8×10⁵ can move CL/CD through the polar by more than the density gain. The Speer H105 rights gate in the spec remains open: tspeer.com was unreachable this session (TLS), so no licence statement was read.

## File formats and grammars for curves, surfaces, meshes and CFD data

*Source: [05-file-formats-and-grammars.md](05-file-formats-and-grammars.md) (`kb-hw-file-formats-and-grammars`).*

### Open questions and domain failure modes

**Open (cheapest next probe in parentheses):**
- Lednicer lower-block layout and LE duplication across the UIUC archive (download `coord/*.dat`, classify by count-line presence, assert the invariants in §Data — one script, one afternoon).
- UIUC redistribution terms (email m-selig@illinois.edu; meanwhile bundle only computable/independently published sections).
- Whether Fusion/Mastercam/PowerMill/Vectric import a bare `B_SPLINE_SURFACE_WITH_KNOTS` as a usable surface body and whether they need `ADVANCED_FACE`/shell (SPIKE-02: write one lofted surface three ways — bare, face, closed shell — and open each in Fusion; measure chord at three stations).
- STEP degree handling: do the target CAM systems keep degree-5 surfaces or degree-reduce (same spike, add a quintic sample).
- XFLR5 XML exact schema and twist axis (export one plane from XFLR5 and open the file).
- Shape3d `.s3d` binary vs text and `.s3dx` schema (obtain a sample file; ask Shape3d).
- ESI-OpenFOAM `force.dat`/`moment.dat` columns (open `postProcessing/` of one v2406 run).
- 3MF spec licence and lib3mf C# binding maturity (read the spec repo LICENSE; build lib3mf on macOS arm64).
- CGNS licence text (open `CGNS/CGNS` `license.txt`).
- OpenVSP `.vsp3` XML structure and licence text (open one `.vsp3` and the OpenVSP LICENSE).
- A pure-.NET JCS implementation to adopt vs own (search NuGet; if none, own ~150 lines with a fixture set from the RFC's test vectors).

**Failure modes:**
- *Silent:* Lednicer read as Selig (plausible wrong shape); STL/OBJ in the wrong unit (1000× scale, "opens fine"); `Sref` in an `.avl` disagreeing with geometry; `G17` noise producing meaningless Git diffs; a hash computed over pretty-printed bytes that changes when indentation changes; NaN written as `null` without a status field, later read as "zero".
- *Expensive:* a STEP that opens as a bag of surfaces the CAM operator must sew (hours per part); a 4-hour CFD run whose fields were embedded in a project file that then cannot be opened; a schema major bump without a migration and a `.bak`.
- *Irreversible:* overwriting the original `.dat` bytes with the normalised copy (provenance lost); rewriting `recipe` after a manual edit (one-way burst violated); dropping unknown keys from a newer-minor file on save (EXP-01 forbids).

### Disconfirming views sought

1. *"Selig vs Lednicer detection is trivial — just check whether the first numeric line's values exceed 1."* Held mostly, but the observed Lednicer file starts its upper block at `0.00001 −0.00005` and Selig files may start at `1.0000000 0.0000000`; a Selig file with a slightly-over-1 TE (`1.00003`) would defeat a naive `>1` test, and a Lednicer count line like `2. 2.` would pass a `>1.5` test but fail the minimum-count rule. The two-block structural test is required, not optional. Finding 1 stands, strengthened.
2. *"OpenVSP proves single-entity STEP export is enough."* Partly overturned: OpenVSP's own notes distinguish untrimmed surfaces from trimmed watertight BREPs [S34]; the precedent proves surface export, not CAM-ready solids. Finding 6 revised accordingly (see Contradiction below).
3. *"Use `JsonSerializer` defaults; .NET already writes round-trippable doubles, so the bytes can be hashed."* Round-trip held (bit-exact in the probe); hashing the bytes fell: .NET's exponent/negative-zero forms differ from JCS, pretty-printing changes bytes, and reflection serialisation is off by default in .NET 10 file-based apps. Finding 12/13 stand.
4. *"KiCad-style fixed decimals are better than shortest round-trip for Git."* Real trade-off: fixed decimals give stable column widths and no exponents but quantise (KiCad accepts a 1 nm floor). Shortest round-trip is exact and still readable for values in [1e-4, 1e15) (probe: `0.0001` prints without exponent; `2.5E-05` does). Recommendation: shortest round-trip **plus** a writer rule that geometry values are stored in metres with magnitudes that avoid exponents in practice; revisit if diffs prove noisy.
5. *"meshio makes mesh I/O a solved problem."* It does for Python, but it is a Python dependency last pushed 2024-07-23 [S31] and the product must not embed CPython; the text grammars we need (`.su2`, `polyMesh`, `.vtu`, STL, 3MF) are small enough to own. Finding 9 stands.
6. *"HDF5 is overkill; keep everything in JSON."* JSON cannot carry a 50-million-cell field or a NaN; PureHDF is MIT and pure managed [S19], so the cost is low. Stands, with Parquet for tabular sweeps.

**Contradicts existing repo knowledge.** `bench-geometry-kernel-decision` and `bench-cad-ux` state that "OpenVSP uses STEPcode to write AP203 files that currently contain only `B_SPLINE_SURFACE_WITH_KNOTS` entities" and treat this as the decisive precedent for CNC-ready export. OpenVSP's 3.21.0/3.21.2 release notes [S34] show that since 2020 OpenVSP exports **trimmed** STEP/IGES that "can form a watertight BREP solid" as well as untrimmed surfaces, and warns that untrimmed files avoid "slivers, gaps and holes" while the shell form is "not a single watertight BREP". Both claims can be true (the surface entity is still the only *geometry* entity; trimmed export adds topology entities), but the repo's inference "one entity type is enough for CAM" is **not established**: EXP-02's open-and-measure gate must test a face/shell form, not only bare surfaces. Confidence: Verified for the OpenVSP behaviour (release notes), Inferred for the CAM requirement.

## Foil section catalog for hydrofoil wings, stabilizers, struts and fins

*Source: [06-foil-section-catalog.md](06-foil-section-catalog.md) (`kb-hw-foil-section-catalog`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe first):**
- *UIUC coordinate terms.* One e-mail to the UIUC Applied Aerodynamics Group webmaster asking for a written statement on redistribution of `coord/*.dat` in a commercial desktop product; until then class (c).
- *H105 coordinates and terms.* One e-mail to Tom Speer; the boatdesign thread and the H105 page show no coordinates. Also ask for design Re/Cl/t/c.
- *Eppler design Cl/Re per section.* Obtain JSR Part 2 (1981) and Part 3 (1985) or the 1990 book plates; one library request.
- *Water Ncrit.* No measured N-factor in water was found. Cheapest probe: run the catalog at Ncrit 2/3/4 against Day et al. 2019 lift–drag data and report which reproduces it best — a bounded spike, but only a one-foil calibration.
- *E817 SAFL inception σ values.* The ASME abstracts were HTTP 403; retrieve the two JFE papers for σ_i vs α to seed a section-level experimental fixture.
- *Göttingen 796/797 "hydrofoil" provenance.* Not confirmed; drop the label or find the citation.
- *Fin section templates.* No primary source found; survey Futures/FCS technical pages or drop fins from v1 text.
- *Mast thickness by brand.* Armstrong page gives chord and "12 mm" but summaries elsewhere say 13.8–15.8 mm; read a datasheet before quoting.
- *Airfoil Tools polar settings* (Ncrit 5/9 recall) and *XFOIL NACA-command scope* — one visit each to confirm; both are Flagged now.

**Domain failure modes:**
- *Silent format misread* (Lednicer as Selig) yields a plausible thin section — Phase 0 already hit this; every Eppler hydrofoil file is Lednicer, so a default-Selig parser fails on exactly the flagship set.
- *Coarse coordinate files* (16-series 33 points, Go 797 29 points) give under-resolved suction peaks → Cp_min under-read → cavitation margin over-stated (unsafe direction).
- *Air Ncrit (9)* applied to water shows a laminar bucket that does not exist at sea; *Ncrit 1* removes buckets that may exist — both are confident wrong numbers.
- *Design Cl guessed from the shape* (Cl at α = 0) presented as "design Cl" — misleads stabilizer sizing.
- *Licence drift*: bundling UIUC files or LSAT data because "everyone does" (AeroSandbox bundles the database) is not a right; a takedown after release is expensive.
- *Rooftop sections at low Re*: E817-class sections separate at the recovery at Re ≲ 5 × 10⁵ and moderate α — a take-off/pumping regime — yet rank well at high-speed Cp_min; single-point ranking hides this.

### Disconfirming views sought

- **"UIUC coordinates are free to use" (counter to finding 1).** The counter-argument is common practice: AeroSandbox, Airfoil Tools and countless repos redistribute the files. Sought and fared: practice is not a licence; the database page and FAQ contain no terms (both opened). The finding stands; the fix is a written statement or download-at-use.
- **"GPL on data is unenforceable / irrelevant" (finding 2).** Counter: copyright in measured data is thin in the US. Fared: the Manifesto's conditions are what the authors ask for regardless of enforceability; COMMIT-02 is a policy, not a legal analysis. Stands, as a policy ruling to make.
- **"Ncrit for water is 1–3, settled" (finding 7).** Counter: Speer is the most experienced practitioner and Feifel's Boeing experience backs him. Fared: Speer himself disclaims test data; the only primary tank comparison used 4. The finding (a band, not a constant) stands.
- **"H105 is the right default for wingfoil" (finding 6).** Counter: Moth use (Axiom) and Speer's rationale fit the 20–30 kn regime exactly. Fared: the rationale is Verified but no coordinates, terms or numbers are; the recommendation cannot enter the catalog on prose. Stands as pending.
- **"6-series beats 16-series" (finding 10).** Counter: 16-series has the deeper bucket at high speed and is the propeller standard. Fared: ADA032272 measures higher L/D for 64A309 in a flapped foil-plus-strut at PCH-like conditions; the 16-series advantage is at higher σ-limited speeds than water sports reach. Stands as a ranking under those conditions only.
- **"NeuralFoil is accurate to 0.4 %" (finding 8).** Counter: the paper's numbers are precise. Fared: they are relative to XFOIL, and XFOIL vs water experiment is itself only loosely tested (Day 2019). Stands with the two-link caveat.
- **"Göttingen 796/797 are classic hydrofoil sections" (task premise).** Sought a source; none found. The premise is now Flagged, not repeated.

## Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity

*Source: [07-low-order-hydrodynamics.md](07-low-order-hydrodynamics.md) (`kb-hw-low-order-hydrodynamics`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe in parentheses).**
1. Ncrit for open salt water — no primary source; the ANA-15 preset 2.0 is a practitioner assumption. (Probe: XFOIL sweep on E817 at Re 5×10⁵ and 10⁶ with Ncrit 1, 2, 4, 9 — report ΔC_d at C_l 0.2 and 0.5; a spike, ≤ 1 h.)
2. Cp_min under-read at 64 stations vs a 200-panel XFOIL Cp on a thin race section near design C_l. (Probe: same spike; compare NeuralFoil Cp_min to XFOIL `CPWR` output at α = 0, 2, 4°.)
3. Warren-12 and Bertin–Smith reference numbers — [S15] could not be rendered (server refused the connection 2026-09-20) and the Bertin & Smith text was not opened. (Probe: re-fetch the Mason PDF or open the textbook; 2 tool calls.)
4. The Faltinsen infinite-Fr closed-form lift ratio and the Hoerner form-factor coefficients are recalled, not read. (Probe: open [S20] ch. 6 and [S26] ch. 6; library access.)
5. Whether the JMSA drag factor (n = 0.40, drag *falls* near the surface) is a physical effect or an artefact of their drag reference; and whether the correction transfers from Re 10⁵ to 10⁶. (Probe: one RANS-VOF run at h/c 2 and 5, Re 10⁶ — a local-CFD task once that tier exists.)
6. The [S16] Thiart citation and several secondary sources ([S27][S28][S29][S30][S31][S32][S36][S37][S38][S42][S43][S44][S46]) could not be re-established in the completion pass because the search budget was exhausted; their rows below are Flagged. (Probe: one search each, ≈ 15 calls.)
7. Junction drag magnitude for wing–fuselage and mast–fuselage joints (gap register GAP-10 stands). (Probe: Hoerner ch. 8 tables; or a local-CFD junction case.)
8. Free-surface correction for the *stabilizer*, which runs shallower than the front wing on a wingfoil and inside the front wing's wave. (No source; probe: JMSA authors' rear-wing data if released.)

**Domain failure modes.**
- *Silent:* applying a deep-water polar at h/c < 3 (up to 5–10 % lift error at goal-state Fr_h [S6]); reading Cp_min from a coarse distribution and declaring a cavitation margin that a resolved peak would erase; using the far-field downwash formula at a 0.7 m tail arm; reporting near-field VLM induced drag without the Trefftz cross-check; entering Oswald e by hand; converting N/m as if it were N (ANA-18).
- *Expensive:* trusting XFOIL/NeuralFoil C_l,max at Re 4×10⁵ to size a take-off wing — the tool over-predicts attached lift near stall (direction Flagged), so the foil never lifts in 10 kn; sizing span from an (L/D) that ignored junction and mast drag.
- *Irreversible:* any UI string saying "cavitation-free", "ventilation-safe" or "trim" from a steady low-order tier; a rider at 30+ kn on a foil whose σ margin was computed at the wrong depth or with fresh-water p_v is a safety outcome (GAP-14).
- *Numerical:* a root-finder that extrapolates outside the polar's converged bracket (ANA-05/12 forbid it); a nonlinear lifting line that converges to an unphysical branch near stall without reporting the residual; an unsymmetric lattice giving a phantom side force.

### Disconfirming views sought

1. *"NeuralFoil is accurate enough to replace XFOIL outright."* Counter: its error is measured against XFOIL, not experiment, and its Cp is 64-node; on thin race sections near design C_l the peak is what cavitation screening needs. Fared: the counter stands — NeuralFoil is the interactive tier, XFOIL the gold master, both labelled "XFOIL-class". *(Verified basis [S2][S3].)*
2. *"−Cp_min is a prediction of inception."* Counter: measured σ_i deviates with Re, nuclei and water quality [S28]; tip-vortex cavitation is invisible to a 2D Cp. Fared: strongest; the screen is a necessary condition only and the UI text says so (ANA-10). *(Inferred.)*
3. *"Free-surface effects are negligible at wingfoil depths, so skip the correction."* Counter: at h/c 3–5 the JMSA fit gives 2–5 % lift loss and a stated Fr_h dependence of ≈ 17 % between Fr_h 2 and 5 at h/c 4 [S6]. Fared: the "negligible" view fails against a 1 % load tolerance; the correction must be offered, labelled, and switchable.
4. *"Lifting line is obsolete; use VLM everywhere."* Counter: Phillips–Snyder lifting line with polar coupling gives viscous strip results a linear VLM cannot, at lower cost [S9]; VLM is better for sweep, dihedral and multi-surface interaction. Fared: both survive as tiers; the spec's "VLM + strip theory" is the union.
5. *"Helmbold vs Prandtl is academic at AR 8–13."* Counter: 1–2.4 % at those AR, which is the band inside which a VLM fixture is judged; the difference matters for tolerances, not for design. Fared: stands as stated (headline 10).
6. *"Water needs a special Ncrit."* Counter-view: XFOIL's own author frames Ncrit by facility turbulence, and no water measurement was found; the preset is convention. Fared: the disconfirming view wins on evidence — hence the ANA-15 label "source-based assumption" and the spike in open question 1.
7. *"A permissive VLM already exists, so do not write one."* Counter: AeroSandbox/MachUpX/OpenAeroStruct are Python and framed for aircraft (no depth, no water snapshot, no σ); the workbench is C#/.NET with a provenance-keyed data model. Fared: borrow as oracles and reference implementations; write the lattice (it is a few hundred lines) and admit it by fixtures.

## Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

*Source: [08-simulation-openfoam-su2-interop.md](08-simulation-openfoam-su2-interop.md) (`kb-hw-simulation-openfoam-su2-interop`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe first).**
1. Does Gmsh produce an unattended boundary-layer mesh on the AR 5/8/12 wings that passes ITTC's y+ and layer floor and imports to SU2? — Probe: SPIKE-03b, one script, three wings, report achieved y+ and layer coverage at the TE. (Decides finding 15.)
2. Does killing `docker run`/`wsl.exe` on macOS/Windows leave the solver running? — Probe: start `sleep`-wrapped `simpleFoam` under each substrate, cancel via CliWrap/`Process.Kill(true)`, `pgrep` inside the substrate.
3. Are the SU2 macOS release binaries signed/notarized, and do they run on Apple silicon natively or under Rosetta? — Probe: download v8.5.0 asset, `codesign -dv`, `file`, run the TMR flat plate.
4. Exact ESI v2606 keyword names for `yPlus`, `wallShearStress`, `sample` and `startFrom latestTime`; snappy default quality thresholds — Probe: fetch the corresponding headers from gitlab.com/openfoam/core/openfoam (the develop.openfoam.com host now redirects there).
5. When is `opencfd/openfoam-default:2606` published and multi-arch? — Probe: Docker Hub tags API (S26 URL).
6. Duncan (1983) exact DOI and digitised data availability; Delft twist-11 geometry/data access terms; NACA 66(mod) source. — Probe: Crossref by title, TU Delft repository search.
7. Does SU2 v8.5.0 support overset or 6-DoF for pumping? — Probe: grep `config_template.cfg` for `OVERSET`/`GRID_MOVEMENT`.
8. What is the conda-forge licence metadata for `su2` actually saying, and does SU2's `LICENSE.md` still read LGPL-2.1? — Probe: fetch both files.
9. Measured cost classes on the target laptops. — Probe: instrumented runs, not estimates.

**Domain failure modes.**
- *Silent wrong answer:* layers missing at the TE (Cd wrong, mesh "passes"); wall functions used with y+ ≈ 5 (buffer layer); transition model with SU2's 5 % TI default; `REF_AREA= 0` auto-computed from a marker; forces in non-dimensional mode misread as newtons; `latest` image drifting under a saved run.
- *Expensive:* wall-resolved 3D transition runs on a laptop; free-surface domain too short (wave reflection, oscillating lift never converging); grid study never performed and then required for a release claim.
- *Irreversible / product-level:* linking any GPL component (licence contamination); redistributing solver binaries without licence review; killing the host client and leaving a container writing into the case directory the user then edits.

### Disconfirming views sought

- **"SU2 first" (proposal §6.2).** Strongest case: single `.cfg`, CSV history, prebuilt binaries on all three OSes (Verified), LGPL. How it fared: it holds for the 2D slice and for install ergonomics; it fails for 3D unless an external mesher is added, because SU2 has no mesher and no free-surface/cavitation path (Verified absence). Result: split recommendation (finding 15), flip condition stated.
- **"OpenFOAM is too hard to install to be v1."** Counter-evidence: the official multi-arch image and OpenFOAM.app make the mac path one Homebrew command or one image pull (Verified); the residual costs are Docker Desktop terms and the unsigned-app dialog, both disclosable. Held partially: Windows still needs WSL2 or Docker Desktop with admin rights.
- **"Fully turbulent SST is enough at Re ≈ 1e6."** Counter: TMR shows ≈ 10 % drag difference tripped vs untripped at Re 3–6e6 (Verified) and the polar tier already models transition via Ncrit; ignoring it makes tiers disagree by construction. Held: transition is required for reconciliation, but it is a second step behind a working fully turbulent pipeline.
- **"A converged residual means a converged result."** Counter: ITTC separates iterative convergence from grid uncertainty (Verified). Held fully; the three-part label follows.
- **"Native bindings would be faster/better."** Counter: OpenFOAM GPL + ABI churn; SU2 wrapper is Python (Verified). Held fully.
- **"The Foundation line is interchangeable with ESI."** Counter: modular solvers since v11, `simpleFoam` a script, different property files (Verified). Held; v1 pins ESI.

## Optimization strategies for 2D foil sections and 3D hydrofoil wings

*Source: [09-optimization-strategies.md](09-optimization-strategies.md) (`kb-hw-optimization-strategies`).*

### Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Jones 1950 optimum (15 %/15 %) vs the spike's family (+25–32 % span) — which admissible family and constraint did Jones use? | Download NTRS 19760012005 PDF, transcribe the load expression, add it to the spike as a fixture |
| Tier correlation estimator ↔ NeuralFoil ↔ VLM ↔ RANS on the catalog sections (GAP-06) | One 3-tier sweep on E817 at the seven goal-state points; compute the AR1 ρ per quantity |
| Exploitation threshold (surrogate gain that survives XFOIL/VLM) | Run Tier 1 on NACA 0012 → E817-like target; compare NeuralFoil vs XFOIL gains at Ncrit 2/4 |
| KS ρ and A_cav k in a 2D screening setting | Reproduce A_cav with a Cp distribution from XFOIL; test k = 10 sensitivity |
| Forrester/Sóbester/Keane (2008), NSGA-III (Deb & Jain 2014), Hansen tutorial contents, PROFOIL | Open the book chapters/papers; these are cited by DOI/title only |
| A permissive, maintained constrained-NLP library for .NET | Survey NuGet (e.g. Math.NET Numerics optimization namespace scope); else own SLSQP with SciPy as fixture oracle |
| Race wingfoil speed/time-on-task PDF for weights | GPS track of one race day with wind log |
| CVaR in aerodynamic OUU — precedent | Search Li/Padula-type robust airfoil papers for tail-risk objectives |
| Anhedral/dihedral near a free surface | NTRS: Wadlin & Christopher 1958 |

Failure modes: **silent** — an optimum on a parameterization bound or with confidence pinned at its floor (exploitation); a single-Ncrit polar giving a laminar bucket that does not exist in water; weights with no basis becoming "the objective"; a multistart that never ran. **Expensive** — CFD re-verification of a candidate that a 1 s XFOIL check would have rejected; optimizing thickness with no structural model. **Irreversible** — labelling a candidate "certified"/"cavitation-free" from surrogate evidence; a rider at 30+ kn on a foil optimized at one point.

### Disconfirming views sought

- **"Multipoint is enough; the exploitation problem is a 1998 artefact."** Counter-evidence: Drela's 6-point case still scalloped between points; NeuralFoil's freedom from the pathology is attributed by Drela to the *smaller design space*, not to multipoint alone [S1][S7]; Garg 2017 (RANS, 2017) still saw off-design degradation from single-point [S2]. Fared: the finding stands; multipoint plus a bounded design space plus regularity are jointly needed.
- **"Adjoint/CFD optimization is the state of the art; the workbench should go straight there."** Counter: Garg's tool chain is LGPL/proprietary and hours per evaluation; Ng/Yildirim needed an XFOIL cross-check; the book's tree puts expensive/noisy problems on surrogates [S2][S4][S6]. Fared: CFD is a re-verification tier in v1, an optimization tier later.
- **"Gradient-free is fine because NeuralFoil is fast."** Counter: 32,000 vs 206 calls at 30 variables [S6]; but at ms per call 10⁵ calls is minutes, so gradient-free *is* acceptable at Tier 1–2 for ≤ ~20 variables. Fared: partially — tier-dependent, stated as such.
- **"Bell loading should be the default planform objective."** Counter: which moment is fixed changes the optimum (22 %/11 % vs 15 %/15 %), and the spike showed the root-BM version is unbounded without a load-sign constraint [S14][S15]; the structural proxy for a wingfoil is unmeasured (GAP-02). Fared: bell loading is a *reference*, not a default.
- **"Multi-fidelity will save the CFD budget."** Counter: MFK needs nested, correlated data [S18]; no tier correlation exists for this domain (GAP-06). Fared: deferred until measured.
- **"AeroSandbox's AD makes Python the obvious optimizer host."** Counter: CasADi LGPL-3.0 and IPOPT EPL-2.0 are transitive; permissible only as a process sidecar [S9]–[S13]. Fared: sidecar, optional, licence-reviewed.

## Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

*Source: [10-integration-and-ai-workflows.md](10-integration-and-ai-workflows.md) (`kb-hw-integration-and-ai-workflows`).*

### Open questions and domain failure modes

**Open questions (cheapest next probe in parentheses).**
1. Does the official `Anthropic` NuGet build and run under net10.0 with structured outputs and strict tools? (Spike:
   `dotnet run` a 20-line probe under `spikes/`, record SDK version.)
2. Is `Blake3` managed throughput adequate for hashing a 21-station/201-slice document at edit rate, or is SHA-256
   simpler? (Micro-benchmark on the A7 fixture.)
3. What `analysis_confidence` threshold corresponds to XFoil non-convergence in the water Re/Ncrit band? (Sweep the
   catalog sections over Re 10⁵–3×10⁶, Ncrit water policy; compare against XFoil convergence.)
4. Do ISolverBackend capability flags need a formal schema shared with the CLI? (Draft in `/define-architecture`;
   verify with SU2 and OpenFOAM smoke tests.)
5. The Peherstorfer taxonomy and the Martins & Lambe architecture classification were confirmed by metadata only.
   (Open the SIAM Review PDF via a library or the authors' site and the AIAA J. paper; 2 fetches.)
6. XFLR5's project-file provenance behaviour and Fusion's manufacturing checks were not opened. (One fetch each.)
7. Is there a published failure-mode taxonomy for OpenFOAM agents (hallucinated keys, cross-file inconsistency, wrong
   solver)? (Read the Foam-Agent and ChatCFD full texts; extract their error categories.)
8. Nextflow/Prefect licences and Grasshopper's dataflow model are recall-only. (Fetch repositories if the roll-up
   needs them.)

**Domain failure modes.**
- *Silent staleness*: a result stays labelled Current after an input changed because the dirty flag lived outside the
  graph — prevented by key equality (Implication 2).
- *Averaged truth*: a UI shows one L/D that is a blend of tiers — forbidden by ANA-06; the discrepancy record makes
  disagreement visible.
- *Runs-but-wrong*: an agent-produced or auto-repaired case converges to a physically meaningless field (ChatCFD's
  14-point gap); prevented by keeping the human on the Run action and by physical-fidelity evals.
- *Valid-but-wrong geometry*: a generative or LLM proposal yields a schema-valid, executable, wrong shape (text-to-CAD
  finding); prevented by the draft → validate → preview → accept path.
- *Off-distribution confidence*: a surrogate returns a plausible number where it has no data — NeuralFoil's confidence
  exists; other surrogates often have none; do not adopt a surrogate without a confidence output or an ensemble.
- *Path-bound provenance*: results referencing files by absolute path (ParaView pattern) break on move/share;
  reference by hash.
- *Version-blind cache*: a cache keyed on inputs only serves results from an older solver build; include method
  version and build digest.
- *Licence contamination*: linking NOSA/LGPL code or training on NC data; COMMIT-02 review at dependency admission.

### Disconfirming views sought

- **"A reactive/incremental graph is over-engineering for one wing; recompute everything on every edit."** Fared
  partly: the estimator and DRC can indeed be recomputed wholesale within 100 ms, but polars via a sidecar and VLM
  cannot, and CFD must never auto-run; the graph is what makes tier policy and staleness explicit. Kept, with the
  scope reduced to a small custom graph (Implication 1).
- **"Multi-fidelity fusion (co-Kriging) is mature; ship a corrected estimator now."** Fared badly for v1: fusion needs
  several high-fidelity points per design and yields a variance the UI must display; with one CFD run per design the
  correction is a scalar with a declared scope. The canon supports recording discrepancies now and fusing later.
  [S15][S17]
- **"LLM agents can now run CFD end to end — let the assistant set up and run cases."** Fared badly: 62.5 % OOD and 68
  % physical fidelity on tutorial-class tasks, plus the authors' own call for human oversight; the human-run rule
  (AI-05) stands. The literature does support feeding the model the error log plus retrieved tutorials for diagnosis.
  [S27][S28][S29][S30]
- **"Structured outputs make deterministic validation redundant."** Fared badly: the supported schema subset cannot
  express numeric bounds or string lengths, so range/domain checks stay in code. [S7]
- **"Text-to-CAD works; let the model emit CST coefficients."** Fared partly: benchmarks report high executability and
  IoU on canonical shapes, but intent errors degrade results below the unconditioned baseline while remaining
  executable; a CST proposal is only acceptable as a Geometry edit draft with fit residual and deterministic
  evaluation, which is what AI-06 already defers. [S32]
- **"NeuralFoil generalizes unusually well, so its confidence output is unnecessary."** Fared partly: the README
  claims strong generalization, but the paper defines confidence precisely because XFoil non-convergence and distance
  from training data are real; storing it costs nothing. [S1][S2]
- **"Use a workflow engine (Snakemake/Dagster/Prefect) instead of writing a graph."** Fared badly: they are
  batch/cloud process orchestrators in Python; the pattern (rerun triggers, asset lineage) transfers, the engines do
  not. [S48][S49]
- **"OpenVSP/VSPAERO is free — embed it as the VLM."** Fared badly: NOSA-1.3 is OSI-approved but GPL-incompatible and
  not in the permitted set; process-invoke only, and the product already commits to its own VLM. [S23][S24]

## Visualization for hydrofoil design and optimization

*Source: [11-visualization.md](11-visualization.md) (`kb-hw-visualization`).*

### Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Avalonia `OpenGlControlBase` on macOS (Metal/ANGLE path), picking latency, and Silk.NET interop | Spike: render 5 × 10⁵ triangles + 2 000 polylines with id-buffer picking in an Avalonia window on macOS and Windows; measure frame time |
| Own `.vtp/.vtu` reader effort (appended raw + base64, zlib) | Spike: read a `foamToVTK`/SU2 `surface_flow.vtu` and a pvbatch `.vtp`; compare arrays with PyVista (MIT) in a test |
| pvbatch availability and version pinning on user machines; state-file (`.pvsm`) compatibility | Spike: detect ParaView, run a reduction script headless on both OSes; open a generated state file in two ParaView versions |
| Actual memory/time of reduction for a 5 M-cell OpenFOAM case | Instrumented spike on a real wing case; record peak RSS and wall time per artifact |
| ScottPlot idioms: inverted axis, gaps, direct labels, table twin, export | Spike: the Cp chart and the cavitation bucket with a failed sample; screenshot review in the harness |
| Crameri LUT ingestion (which of the 100+ maps; `.txt/.csv` formats) and Moreland cool–warm licence text | Read the ZIP's licence and formats; fetch Moreland's LUT licence statement |
| ParaView glTF export (for a possible web/shareable scene) | Open the ParaView exporters list in the installed version |
| Borland & Taylor 2007 full text; Hunt–Wray–Moin 1988; Speer H105 page | Fetch via IEEE/NTRS/site with a working route; content is Flagged until read |
| OpenFOAM kinematic pressure convention and `wallShearStress`/`yPlus` function-object outputs (for the τ_w path) | Open the function-object sources in OpenFOAM-dev (`functionObjects/field/wallShearStress`) |
| SU2 surface output fields (does `SURFACE_PARAVIEW` carry skin friction, y+?) | Run SU2 on a small case with `VOLUME_OUTPUT= COORDINATES,SOLUTION,PRIMITIVE` and inspect `surface_flow.vtu` arrays |
| Practitioner expectations (CD counts axis, Cp colour direction) among water-sports designers | Observed sessions; until then defaults follow XFOIL/XFLR5 idioms |

**Domain failure modes (silent, expensive, irreversible).** A separation claim from a vortex criterion reaching a design decision; a colour range reset between replay frames hiding a real Cp_min change; a point-interpolated Cp_min feeding the cavitation screen (optimistic V_crit); a chart overlaying two sections at different Re without saying so; a Cp chart with the axis un-named and read upside-down; a reduced/decimated surface probed for values; a "surface streamline" from near-wall velocity presented as an oil-flow picture; a rainbow default reintroduced by a library's own default LUT (ScottPlot/ParaView defaults must be overridden explicitly and tested by the craft gate); a state file or cached picture from a superseded revision displayed under the new label.

### Disconfirming views sought

1. *"Engineers expect rainbow/turbo; a foil designer will read jet fine."* Turbo's own authors concede lightness ambiguity and grayscale/achromatopsia failure [S9]; Crameri quantifies > 7 % visual error and CVD exclusion [S5]; Kovesi shows flat spots hiding 10 % of a range [S7]. The counter-view fails for a default; a user-selectable turbo with a warning label is a possible later concession, not a v1 feature.
2. *"Use a sequential map for Cp too; diverging maps confuse."* Crameri's rule allows diverging maps about a physical centre [S5], and Cp = 0 is such a centre; Moreland's smooth-middle requirement addresses the confusion [S6]. The counter-view survives only as an option: sequential Cp with isolines is acceptable when the range is one-signed (a fully suction-side range), and the policy should switch automatically and say so.
3. *"Centre the Cp map at stagnation (Cp = 1)."* Cp = 1 is the physical maximum, so a map centred there would put the whole field on one side; the centre must be Cp = 0 [S2][S5]. Fails.
4. *"Just embed VTK — ActiViz is cheap."* ActiViz is commercial and version-locked [S28]; COMMIT-02 requires permissive OSS; VTK's own list of bindings has no other .NET path [S16]. Fails for v1; the process-plus-files path is BSD-3 end to end.
5. *"Q or λ2 is what everyone uses to show separation."* Jeong & Hussain define a vortex core [S36]; separation is a wall-topology property (Tobak & Peake [S40], Surana et al. [S41]); the two coincide only in special cases. Fails; the grounding register's rule stands and is strengthened.
6. *"Web tech (three.js/Plotly) is the fastest path to good charts and 3D."* Licences are permissive [S24], but a WebView imports a browser runtime, moves accessibility proof into the web layer, and complicates the offline/no-network commitment; the native charting options are active and MIT [S24][S27]. Survives as a *later* option for shareable reports/exports; not for the v1 viewport.
7. *"Precomputing reduced artifacts per sample is premature; stream from the volume."* The size arithmetic (≈ 1.4 GB per 10 M-cell sample) versus a 16 GB laptop makes in-process volumes infeasible; the proposal already says "harvest slices, don't live-tail the 43 M-cell volume" [S53]. The counter-view fails; the only open point is the exact budget, which must be measured.
8. *"Veldrid is fine — it still builds."* README and release history show no public updates since 2023-02 [S25]; a renderer dependency with no maintainer is a licence-clean but supply-chain-dead choice. Fails.
9. *"Uncertainty bands make charts look more scientific; add them from a fixed ±5 %."* A band without a defined interval and source is a manufactured number; HOPs/bands need real samples [S44][S45]; the spec forbids manufactured ranges. Fails.
10. *"XFLR5 conventions are the standard; mirror its UI."* Conventions yes (they are XFOIL's [S1]); UI no — GPLv3 and "Abandoned" [S50] — and its rainbow-ish 3D Cp colouring (recall, Flagged) is exactly what the colour policy rejects.

## Structures, materials, manufacturing and safety for water-sports hydrofoils

*Source: [12-structures-materials-and-manufacturing.md](12-structures-materials-and-manufacturing.md) (`kb-hw-structures-materials-and-manufacturing`).*

### Open questions and domain failure modes

| Open question | Why it matters | Cheapest next probe |
|---|---|---|
| The "within 20 %" predicted-vs-measured bending-stiffness figure (gap register) — exact source and context | It is the only stiffness-accuracy number the repo carries | Open Maung et al. 2021 (DOI 10.1016/j.jcomc.2021.100218, open access) from a browser; extract the table |
| Measured pumping load factors on a board foil (n, frequency, cycle count) | Fatigue case and the dynamic load case are unsourced | A rider-borne IMU + strain-gauged mast for one session; or the Southampton/Delft/AMC groups' towing data |
| Foil-specific TE and LE floors per route | The GEO-12 floor is a practitioner value | Ask two builders (one molded carbon, one SLS) for their as-built minimum TE; measure five production wings with a feeler gauge |
| Draft and undercut practice for two-part foil moulds | Demoldability check needs a number | One mould shop's spec sheet; or run the n·p test on a production-like loft with 1°, 2°, 3° |
| Roughness → transition on a hydrofoil at Re 5×10⁵–1×10⁶ | Polar band width for printed/dinged foils | Run XFOIL/NeuralFoil-class polar clean vs forced trip at x/c = 0.1 for E817 at Re 7×10⁵; compare with the ITTC roughness allowance once the ITTC procedure is opened |
| Full-scale deflection/twist datasets usable as fixtures (Southampton, Delft, Michigan, AMC, ENSTA) | Beam validation | Obtain Faye et al. 2025 (JST, OA) tables; write to the authors for the four-foil load–deflection data |
| DNV-ST-C501 / ISO 12215-9 safety-factor values for FRP appendages | Reserve-factor presets | Purchase ISO 12215-9:2026 (259 USD) or read DNV-ST-C501 (free PDF) — one afternoon |
| Public failure reports for masts/wings/fuselages | Safety copy credibility | Query EU Safety Gate from a browser; scan two brand forums for "mast snapped" threads and classify by joint |
| Water-saturated properties of printed CF-PA and PA12 over weeks | Printed-foil route viability | 4-week immersion of printed coupons + 3-point bend before/after (ASTM D790) |
| Cavitation erosion thresholds for gel-coated vs bare carbon | Race foils at 35–40 kn | Literature: Yamatogi et al. 2009 (ICCM-17) — not opened |

**How things go wrong in this area (silent, expensive or irreversible):**
- **Silent:** a hydrodynamically optimised wing at 7–8 % t/c that passes every hydrodynamic gate and has 0.4–0.5× the baseline stiffness; a layup with an unmirrored off-axis ply that nose-up couples; a clean polar shown for a printed foil; a TE that is resin only. None of these produce an error anywhere in the current tiers.
- **Expensive:** a mould cut before the parting line and draft were checked; a STEP that opens but carries slivers at the tip cap; a shrinkage allowance applied twice (mould scaling plus print-software scaling).
- **Irreversible:** a rider injured by a wing the tool implied was "optimal"; the only defence is the copy in implication 8 and the "Not assessed" readouts.

### Disconfirming views sought

1. *"A beam is too crude; only 3D FE is credible for a laminated foil."* Faye et al. built four foils in one mould and showed the 1D beam matches shell/solid FE and experiment when the coupling is modelled precisely [S6][S7]; Ng et al.'s element was validated against an ABAQUS plate [S1]. **Fared badly** for sizing and coupling; **holds** for local modes (buckling, delamination, joints), which the tool must list as not assessed.
2. *"Bend-twist coupling is a research curiosity; production foils are quasi-isotropic."* No brand publishes a layup, so this cannot be disconfirmed from product data [S25]; but the Moth programmes [S5], the UNSW AFP programme [S27][S28] and the 2026 impact study [S26] show it is engineered on purpose in the boat-foil world. **Undecided for board foils** — the tool must support both (K = 0 is a valid layup).
3. *"Water flutter can be screened with the air formulas plus added mass."* Akcabay & Young show a new low-frequency mode and single-mode flutter in water [S3]. **Fared badly** — the screen may be shown, labelled as a screen.
4. *"There must be a standard for foil-board strength."* Four texts opened: RCD excludes hydrofoils and surfboards [S10]; ISO 25649-1 excludes rigid surf-sport devices [S11]; ISO 12215-9 is keel/centreboard [S12]; ISO 21853 is kite release [S13]. **Held** for the texts opened; a CEN/ISO work item could exist that this session's tooling could not find (search quota exhausted) — Flagged.
5. *"Thickness is set by structure, so a hydro tool has nothing to say about it."* The (t/c)³ / (t/c)² scaling is computable from geometry alone and is already informative without a layup (spike). **Fared badly** — the relative-stiffness readout belongs in v1.
6. *"Roughness does not matter at hydrofoil Reynolds numbers."* Braslow's Re_k ≈ 600 is reached by a 0.3 mm defect at sport speeds; XFOIL's own guide names bypass transition as outside e^N [S16][S17]. **Fared badly**; the magnitude of the drag penalty remains unquantified here (Flagged).
7. *"Printed foils are a toy."* Nothing reachable this session established their fatigue or wet creep either way; what is established is 0.76 % water uptake and 3–3.5 % shrink for SLS PA12 [S20][S21]. **Undecided** — route stays in the policy enum with Flagged floors.

## Validation data, special hydrofoil physics and testing numerical design software

*Source: [13-validation-special-physics-and-numerical-testing.md](13-validation-special-physics-and-numerical-testing.md) (`kb-hw-validation-special-physics-and-numerical-testing`).*

### Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Exact contents and strut/pod configuration of DTIC ADA032272 | download the 107-page PDF from archive.org and read the model description and Table 1 |
| Shen & Dimotakis NACA 66(MOD) Re, σ and load values | open JFE 111(3) 1989 (DOI 10.1115/1.3243645) |
| ASME V&V 20-2009 (and 2016 supplement) exact metric definitions | obtain the standard; until then cite ITTC/Coleman–Stern |
| Whether .NET 10 RyuJIT ever contracts a*b+c into FMA on ARM64/x64 | 20-line `dotnet run app.cs` spike computing (1+ε)(1−ε)−1 on both platforms |
| Theodorsen C(k) and Garrick thrust formulas transcribed and checked | transcribe from TR-496/TR-567 and test against NASA TP-2015-218765 tables |
| Classical Strouhal efficiency band (0.25–0.35 vs 0.4) | open Triantafyllou et al. 1993 (DOI 10.1006/jfls.1993.1012) |
| Brennen's inception/nuclei sentences | fetch the Caltech chapter HTML or PDF (chapters 1 and 6) |
| CEHINAV data access and licence | e-mail the authors; the article states no data licence (area 04) |
| Rider pumping kinematics/power | instrumented session (GPS + IMU + strain-gauged mast) — no literature exists |
| The "AMC T-foil" dataset named in the gap register | ask the author of the register for the citation; nothing at AMC was located |

**Failure modes.** *Silent:* labelling a K-factor-corrected lift Verified when the only tank data disagree by
≈4×; comparing wing-only coefficients with foil+strut lift areas; a golden master that passes on macOS and
fails on Windows by 1 ULP being "fixed" by loosening tolerance globally; σ computed without the depth term; a
parser returning plausible wrong geometry. *Expensive:* running a CFD validation without a grid study (no
U_SN, so no validation statement is possible); building an unsteady estimator before the C(k) test exists.
*Irreversible:* shipping "cavitation-free"/"ventilation-safe" wording; a foil designed to a Cp_min screen
without margin failing at 30 kn.

### Disconfirming views sought

- **"Day 2019 is too slow (≤ 4.5 m/s) and too old a foil to matter."** True that Re ≤ 3.6 × 10⁵ is the low end
  of wingfoil racing and the foil is a 2006 Bladerider; but it is the only open dataset with a flap sweep,
  repeats and a depth pair on a T-foil of the right size, and low Re is exactly where the catalog's Eppler
  ranking flips (area 04). Fared: stands as rank 1 with the Re caveat displayed.
- **"The image-vortex correction is Verified theory (Wadlin) — ship it."** TR-1232 verified it against 1950s
  AR 10/4 data at Fr_h up to the critical speed with satisfactory engineering agreement; the CEHINAV 2026 wing
  shows a much larger Froude-dependent loss at h/c = 4. Two verified sources, different regimes; the
  correction is Verified *for TR-1232's configuration* and unverified for a race wing. Fared: keep as Computed
  estimate.
- **"σ_i = −Cp_min is good enough for a screen."** Amromin's work and the ITTC procedure show inception
  depends on nuclei, Re, Weber number and even material; but as a *conservative bound in nuclei-rich seawater*
  the equality is the right screening variable if a margin and the words "screening estimate" accompany it.
  Fared: kept, with margin and label.
- **"Pumping is governed by St ≈ 0.3–0.4, so an efficiency metric is straightforward."** Floryan et al. show
  dependence on both St and k and on heave–pitch phase; the order-of-magnitude estimate places pump-foiling
  below the classical band; and no rider data exist. Fared: the gap register's "regardless of body geometry"
  claim is weakened (see contradictions).
- **"Bitwise cross-platform determinism is achievable in .NET/Rust."** Verified only for IEEE-exact
  operations; both runtimes document platform-dependent transcendental results. Fared: tolerance-based golden
  masters are mandatory.
- **"Tank data need no U_D if the paper gives none."** ITTC 7.5-02-02-02 shows what a proper budget looks
  like; when absent, the UI must say "U_D not stated", and validation can only be claimed at the level of U_SN
  alone with that caveat. Fared: stands.

**Contradicts existing repo knowledge.** (1) The gap register attributes T-foil towing-tank data to the
"Australian Maritime College"; the only T-foil tank dataset actually cited in the repo (area 06 S12) is Day,
Cocard & Troll at the **Kelvin Hydrodynamics Laboratory, University of Strathclyde, Glasgow** [S2]; no AMC
dataset was located — the AMC attribution is Flagged until a citation appears. (2) The gap register states
"peak propulsive efficiency clusters near St ≈ 0.4 regardless of body geometry or speed (Verified)"; Floryan
et al. 2017 [S17] establish that performance depends on both St and reduced frequency and Triantafyllou et al.
1993 [S18] are usually quoted at 0.25–0.35 — the "regardless" clause is not supported by the sources opened
here. (3) The gap register describes ADA032272 as measuring "NACA 16-309 and NACA 64A309 hydrofoils"; the
archive record says the models are "geometrically similar to the forward foil system of the PCH hydrofoil
craft" [S1], i.e. a foil-system model — consistent with spec A6's foil-plus-strut caveat, sharper than the
register's wording. (4) The repo grounding cites ITTC 7.5-03-01-01 nowhere, but area references assume the
2021 revision family; the current document is Revision 05 (2024) [S8].

