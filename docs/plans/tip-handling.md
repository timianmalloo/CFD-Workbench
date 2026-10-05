---
id: plan-tip-handling
title: Tip handling — one proposal for the VLM tip strip, the RANS tip mesh and the tip geometry
type: doc
status: in-review
owner: "@cfd-leader-4e90c621"
tags: [plan, tip, vlm, a3a, a3c, spike-03, mesh, geometry, ruling-78, ruling-80]
links:
  - {to: rulings, rel: depends-on}
  - {to: design-area3-analysis, rel: relates-to}
  - {to: adr-0012-openfoam-backend-macos, rel: relates-to}
  - {to: plan-fluids-round3, rel: relates-to}
  - {to: coordination-round-oct05, rel: implements}
review-by: "2026-11-05"
summary: >-
  Track A of round-oct05 (Ruling 80): a 9-agent judge panel (Sonnet grounding, Opus proposals, Fable comparison and
  synthesis, Opus completeness pass) recommends keeping Ruling 78 with a written scope (certified finite-chord tips),
  no tip zone for the envelope verdict, two bounded measurements (a small-tip-chord VLM sweep and a 4-variant tip mesh
  coupon), the open planar end as the v1 tip of record, salvage of the held branch's evidence without the eta* law, and
  16 decision requests for the operator.
---

> Produced by workflow `wf_2c98fb91-cac` on 2026-10-05. Ranking: smallest-correct (winner), physics-first,
> geometry-first. The compare stage first failed during the HOOK-CWD-RELATIVE outage and was re-run from cache.

# Tip handling: one proposal for the VLM tip strip, the RANS tip mesh and the tip geometry

This is the study output for Ruling 80 (DR-F3-8). Ruling 80 names its destination as `docs/plans/tip-handling.md`. That file does not exist yet (`ls`, Verified). Writing it is the coordinator's step, not this read-only session's.

This session was read-only. I read main at 70f7d49 (`git rev-parse`, Verified). The brief named bf07a8f. That commit is an ancestor of main: it is "docs(rulings): Rulings 78-83" (`git merge-base --is-ancestor`, Verified). Since then main has gained the SPIKE-ANA-1 merge (465b452) and Rulings 84-85. So there is no conflict, only a newer base. I read the held branch `fix/a3a-vlm-tip-law` in its own worktree. I edited no file and ran no solver or mesher. The only re-analyses are scratch reads of the held branch's CSVs.

Confidence labels:
- **Verified**: opened or run in this session, cited.
- **Inferred**: derived from Verified facts, not observed.
- **Flagged**: recalled, taken from a snippet, or from a file I did not open.
- **Estimate**: a cost scaled from a measured run.

## 1. Result first

**Keep Ruling 78 as built, and write its scope down.**
- The scope is certified finite-chord tips. Those are the only tips the product can analyse today (Geometry.cs:337-339, :371, Verified).
- The k2/k3 verdict flips that blocked the held branch occur only on the closing elliptic planform. That planform comes from a test or probe harness (probe/Program.cs:57 builds it from `Sqrt`; Verified by the panel). A non-rational B-spline rail cannot represent it (foildsl.md:190, Verified by the panel).
- The lattice itself handles closing tips by design (FM-3, area3-analysis.md:512; `Vlm_ClosingTip_FiniteAndListed`, LatticeFixtureTests.cs:39; Verified). Closing tips are out of reach only because the certifier refuses them. So the scope note is tied to certification, and certification can change.

**Do not build a tip zone for the envelope verdict now.** A tip zone is needed in one place: the A3c polar consistency check. There, its width is the measured strip-theory consistency edge, not a constant.

**A3c is already running.** SPIKE-ANA-1 merged as GO (465b452), and `feature/a3c-polar-source` holds red NeuralFoil fixtures (4840786; Verified). So the A3c tip conditions go to that track now, as a seam request.

**Run two bounded measurements:**
1. A small-tip-chord VLM sweep, under 5 min of compute. It decides whether Ruling 78 ever needs to change.
2. A tip-only mesh coupon: 4 variants, about 1.5 min each. It separates pole degeneracy from the boundary-layer fan.

**Other recommendations:**
- Declare the open planar end at the last authored station the v1 tip of record for every consumer.
- Salvage the held branch's evidence onto a fresh branch and drop the eta* law.
- Close out Ruling 75's tolerance law and Ruling 77(5) formally.
- Send the tip copy (COPY-218..220) back to the operator. All three strings cite a "tip law" that will now never exist.

## 2. The problem and how the three places couple

### 2.1 VLM tip strip (Area 3, A3a)

**How alpha_i is computed.** Production alpha_i is the Trefftz-plane downwash at the strip's own midpoint, over a piecewise-constant Gamma sheet. Then alpha_eff = alpha + twist − alpha_i (Trefftz.cs:5,10-19; VortexLattice.cs:219-225; StripCoupler.cs:39-40). The grounding reader verified these lines; I did not re-open them (Inferred). The design states the same rule: α_i comes from the trailing-wake-only downwash at the bound vortex, and α_eff = α + twist − α_i (area3-analysis.md:339-340, Verified).

**On a closing tip the outermost strips diverge with refinement.**
- Exact elliptic Gamma, whose alpha_i is a known constant 0.955 deg, gives k1 alpha_i of −5.3 / −11.6 / −24 / −49 / −99 deg at n = 16..256 when pushed through the production formula. Source: grounding tz.py (Inferred for me).
- The solved lattice at elliptic alpha 8, n = 64 reads alpha_eff 16.10 / 12.35 / 10.34 / 9.25 deg on k1..k4 (repaired-1.1.0.csv, Verified). So three strips per half sit above the 10 deg envelope at the default lattice, and Ruling 78 hides only k1.
- Across the whole CSV, every k1-k3 row above 10 deg is on the `ell` wing: 19 rows, and no finite-chord wing appears (awk over repaired-1.1.0.csv, Verified).

**Closing tips also change the sweep verdict.** VLM-3 recorded that "the sweep verdict changes on straight-quarter-chord closing tips" (area3-fixture-arithmetic.md:245, Verified). It also recorded an open finding that the midpoint rule is unstable where near-coincident bound segments meet at the closing tip (area3-fixture-arithmetic.md:183, Verified). Both are more closing-tip debt of the same kind.

**On finite-chord tips nothing flips.** Rect alpha 5 k1 alpha_eff reads 3.102 / 3.077 / 3.066 / 3.061 / 3.059 deg at n = 16..256 (CSV, Verified).

**On finite tips, Cl_local at k1 goes to 0 with refinement.** Rect alpha 5 reads 0.077 / 0.040 / 0.021 / 0.010 / 0.005 (CSV, Verified). This is the edge limit Gamma(eta) → 0, not an unloaded section. It is why an independent Cl_local check cannot read a genuine excess Outside at the tip. That is the CFD veto on the held branch.

**The genuine-excess falsifier survives Ruling 78.** Rect alpha 18 reads alpha_eff 11.41 / 11.21 / 11.15 / 11.13 / 11.12 deg on k2 at n = 16..256, and k3 is similar (repaired-falsifiers-1.1.0.csv; Verified by the panel; Inferred for me). With k1 indeterminate, k2 and k3 still report Outside.

**The mid-span anchor is sound.** Elliptic AR 8, alpha 5, n = 64 gives alpha_i 0.975759 deg against CL/(pi AR) 0.955301 deg. The gap is 0.020 deg against a 0.05 deg tolerance (repaired-verdict.md:54-57, Verified).

**The eta* law converged to a convention value.** It reads 10.075 deg at n64 and 10.548 deg at n256 at the elliptic tip, against an analytic alpha_eff of about 4.045 deg. Its own Cl_local implies 4.689 / 4.398 deg (repaired-verdict.md:58-61, Verified). The verdict calls U "discretisation of the η* convention, not model uncertainty" (repaired-verdict.md:52, Verified).

**What main shows today.** The outermost strip per half shows the *provisional* state, not *indeterminate*:
- `MarkOutermostProvisional` sets `ProvisionalReason = ANA-TIP-PROVISIONAL` (ProductWingMethod.cs:69, :97; RunRecord.cs:101).
- The projection maps that reason to `Labels.Provisional`, which is COPY-218: "provisional — tip law cannot judge this strip" (AnalysisProjection.cs:154, :169, :187; Labels.cs:18).
- COPY-220, "indeterminate — the tip law cannot judge this strip", is used only for strips that have no verdict (AnalysisProjection.cs:156, :170).
- All of this is Verified.

Ruling 78 says the strip "reads indeterminate (the provisional flag already does this; PRJ reserved the state)" (rulings.md:867, Verified). So the code and the ruling agree on behaviour but use different state names. Both strings also cite a "tip law".

**Stale text on main.**
- area3-analysis.md:322 still says the tip verdict "remains provisional until Ruling 75's η* law is built" (Verified).
- The ProductWingMethod.cs:66 doc comment cites "Ruling 77(5) ... until the η* tolerance law exists" (Verified).
- Ruling 77 condition (5) still reads "the tip strip stays provisional until the eta* law is built", and it "held for the operator ... eta* as a convention (reversible to option b)" (rulings.md:861, Verified).

### 2.2 RANS tip mesh (SPIKE-03, ADR-0012 Proposed)

**Final round-3 repair (R3-M1b, AR 8, Gmsh 4.15.2).** Source: verdict-round3.md:126-134, Verified.
- 246 faces above 70 deg, max 85.24 deg. Split by region: tip pole 176, tip 34, main 34, TE 2.
- 4 faces with weight below 0.05, in the tets 1.7–2.0 mm off the wall near the tip TE and at the tip.
- Location: tip pole faces at x/c 0.9867–0.9968 with median wall distance 66 µm; tip faces at x/c 0.9803–0.9896 with median 106 µm. All sit low in the 1.49 mm stack.

**The TE strip mechanism is real and fixed.** Panel TE faces fell from 616 (M0) and 69,499 (M1a) to 1. The fix sized the strip by the fan ratio √7.0 ≈ 2.64:1 (verdict-round3.md:100-125, Verified). The pre-registered reading ("below 68 TE-flag faces") was not met as written: 73, of which 72 also carry the tip flag. The panel split was computed after the run and is reported beside the pre-registered number (verdict-round3.md:117-122, Verified).

**The tip cap geometry.** The cap is a surface of revolution of the section about the chord line, built in two pi/2 steps (make-wing-gmsh.py:80-81, Verified: `occ.revolve(tc, 0, 0, half, 1, 0, 0, math.pi / 2)` and `occ.revolve(rev[0::4], …)`). Gmsh logs "Skipping boundary layer extrusion of degenerate curve 19/22/29/31" on every run (verdict-round3.md:140-141, Verified).

**The cause is not separated.** Three hypotheses fit:
- H-A: pole degeneracy.
- H-B: anisotropic wall triangles or the fan on the revolved TE region.
- H-C: opposing-layer interference where the stack exceeds the local half-thickness.

The column-spread model predicts failures only aft of x/c ≈ 0.996 and at the top of the stack. The faces reach x/c 0.980 and sit near the wall (verdict-round3.md:135-145, Verified).

**Cost of M1b on this laptop** (verdict-round3.md:126-128, :159-175, Verified):
- Compute 1.5 min (Gmsh 32 s, gmshToFoam 14 s, checkMesh 33 s, postProcess 10 s).
- checkMesh RSS 4.8 GB at 4.52 M cells, about 1.07 kB per cell.
- Wall time in round 3 ran up to 4× compute because of join-lock and load waits from other tracks.

**The previously ruled tip knob order.** The round-3 plan set the tip knob as K2 (a height scale below 1 at the poles) first. K4 (pole-free tip) applies "if R3-M0 puts the bad faces at the tip poles and K2 does not clear them" (fluids-round3.md:121, :123, :293 DR-F3-5, Verified). K2 was never built (verdict-round3.md:114-115, Verified).

**Round-3 steps that did not run.** The mesh NO-GO at AR 8 stopped three steps (verdict-round3.md:17, Verified):
- R3-M2: rebuild at h1 6.0 µm plus a y+ screen.
- R3-M3: AR 5 and AR 12.
- R3-M4: the 3-D y+ gate solve.

### 2.3 Tip geometry (the geometry of record)

**What the record allows.** The DSL accepts `tip point`; a test edits a document to `tip point` (DimensionTests.cs:268, Verified). The record declares `tip: open | point` (foildsl.md:139, :218-220; Verified by the CAGD reader, Flagged for me). The certifier accepts only `Tip == "open"` with no assertions, otherwise it returns Unsupported with "Only an open tip and no assertions are currently certified." (Geometry.cs:337-339, Verified). It also requires rail hulls with strictly positive chord and sets no minimum (Geometry.cs:371, Verified).

**No test pins the tip=point outcome.** No test asserts that `Geometry.Assess` on a `tip point` document returns Unsupported. The only `tip point` test is a chord-dimension refusal (DimensionTests.cs:264-270; git grep, Verified).

**What the spec says.** The tip end is an Anchor that "carries the tip closure", and no closure is defined (cfd-workbench-v1.md:797-799, Verified). The spec does admit closing tips as a design concept: "A closing tip (zero chord, A4.4) is not editable here" (cfd-workbench-v1.md:828, Verified). Tip chord is "the chord at the outermost authored station" (cfd-workbench-v1.md:854, Verified).

**A second geometry authority at the tip.** The RANS probe builds its own NACA 0012 section, extrudes it and revolves the tip (make-wing-gmsh.py:8, :72-81, Verified). That tip adds 7.28 mm to b/2 (+1.5 %) and about 6.0e-4 m² per tip (+1.05 % of half-wing S). It is authorised as an analysis-geometry exception, "rounded tip", under DR-F2-4 (verdict-round3.md:150-153; fluids-round2.md:491; Verified). This is a second geometry authority at the tip (GEO-A) and a silent b/S refit.

**How the VLM is reached.** Only the CLI constructs `AnalysisService` (git grep, Verified). The CLI admits a document only after `Geometry.Assess` returns Certified (src/CfdWorkbench.Cli/Program.cs:96-102, Verified). `AnalysisService.EvaluateAsync` checks tier, scope and operating point, but not certification (AnalysisService.cs:71-93, Verified). Nothing under src/CfdWorkbench.Desktop references `AnalysisService` or `EvaluateAsync` (git grep, no match, Verified).

### 2.4 Coupling

1. **Geometry → VLM.** The lattice ends at the last authored station. Certification excludes closing tips, so the VLM's closing-tip divergence lives on a planform the product does not certify today. The lattice still supports closing tips (FM-3). The elliptic fixtures F-2 and COPY-217's "Verified fixture family: rectangular and elliptic planforms" (Labels.cs:17, Verified) are lattice fixtures, not documents. If `tip point` is ever certified, the divergence returns at once (Inferred).
2. **Geometry → mesh.** The mesh tip is not the record's tip. Whatever tip the mesh uses sets b, S, the tip vortex and the planform label. The pole-making revolve is the probe's own choice, not a property of the record.
3. **VLM ↔ mesh.** A mesh that fails the DR-F3-1 A gate cannot settle the VLM tip-upwash question, because the failing faces are exactly where a RANS tip stall would read. Nothing in the VLM or polar tracks waits on the mesh.
4. **Polar (A3c) → VLM tip.** The polar changes the per-strip envelope: "each strip's Cl bound becomes its own polar's converged bracket at its Re_local" (DR-ANA-14, area3-analysis.md:827, Verified). It also adds a per-strip consistency check between polar Cl(α_eff) and lattice Cl_local (area3-analysis.md:341-342, Verified). That check fails by construction near the tip. The KB says so: "near tips the 2D assumption breaks (the tip vortex is not a section phenomenon)" (KB 07:94, Verified; the KB itself labels this Inferred).
5. **Measured polar bracket.** SPIKE-ANA-1's GO covers NACA 0012 at Re 2e5 and 1e6, Ncrit 2/4/9, α −6°…+6°. It "does not establish a wider attached-flow envelope". Accuracy on other foils, other Re and stalled flow is unmeasured (spike-ana-1/verdict.md:28, :84, :135, Verified). Ruling 85 adds conditions on shipping the weights (rulings.md:907-909, Verified).

## 3. The operator's physics question: is the tip upwash real, or a Trefftz-midpoint artefact?

**Closing (zero-chord) tips: artefact.**
- The divergence reproduces from exact elliptic Gamma, with no solver and no flow physics (grounding tz.py; Inferred for me).
- The exponent probe shows the same strip-midpoint evaluator diverges linearly for Gamma ~ (1−eta)^0.5, at about +6 deg per doubling (log rate) for exponent 1, and converges for 1.5 (tipexp.py, re-run by the panel: Verified by them, Inferred for me).
- A piecewise-linear Gamma sheet does not cure it (tz2.py, per grounding; Inferred).

So for any closing tip, the outermost-strip upwash is a property of the evaluator, not of the flow. The eta* law converged to a convention, not to physics (repaired-verdict.md:52-61, Verified). Lifting-surface truth at a closing tip remains unmeasured.

**Finite-chord tips: a converged flat-wake model value, not a 2D section.**
- alpha_i at k1 converges: rect alpha 5 alpha_eff 3.102 → 3.059 deg (Verified), which means alpha_i 1.898 → 1.941 deg (Inferred arithmetic).
- That is larger than mid-span (0.955 deg). So alpha_eff at the tip is lower than inboard, and the envelope read is conservative within the model (Inferred).
- But Cl_local → 0 there (Verified). On k1-k4 at n = 64 the ratio Cl_local / (2π alpha_eff) is 0.06-0.25 (smallest-correct consist2.py; Inferred for me).
- That disagreement is a model-validity limit, not a bug. Strip theory near a side edge is not a 2D section (KB 07:94, Verified).
- Tip-vortex roll-up and side-edge separation cannot be represented in a flat rigid wake. They stay labelled "not modelled" (Labels.cs:25-26, COPY-225/226, Verified).

**The check that would settle how far inboard the lattice is a lifting-surface result:**
- Measure alpha_i(eta) and Cl_local at fixed eta in {0.9, 0.95, 0.98, 0.99}.
- Use three lattices: cosine n = 64/128/256; uniform span spacing; and a chord-adapted tip with nc 8 or 16.
- Add a numerical lifting line and AeroSandbox VLM as code-to-code arms.

Agreement inboard, with divergence only within about one tip chord, means the tip zone is a model-validity limit. "About one chord" is Flagged recall. This is the optional second arm of step S2, not a gate for Ruling 78. Only a gate-passing RANS tip can test it against flow, and only on one case, on a single mesh, labelled "not quantified".

## 4. Recommended strategy

### 4.1 VLM tip (A3a)

1. **Keep `MarkOutermostProvisional` unchanged** (ProductWingMethod.cs:69-80, Verified). Amend its doc comment (:66, Verified) to cite Ruling 78 and the scope note. Amend area3-analysis.md:322 the same way (Verified stale). Both are doc-only changes inside S1.
2. **Add one `simplify:` marker** at `MarkOutermostProvisional`.
   - Ceiling: certified finite-chord tips (Geometry.cs:337, :371).
   - Upgrade trigger (a): `tip point` or any closing tip becomes certified.
   - Upgrade trigger (b): the S2 sweep shows a judged-strip flip at a reachable tip-chord ratio. The certifier sets no minimum tip chord (Geometry.cs:371), so any positive ratio is reachable.
3. **Add two controls, red-first, in the fast ring:**
   - (a) A test that `Geometry.Assess` on a `tip point` document returns `Unsupported`. Today that is true through the generic message (Geometry.cs:337-339) and is not pinned. The test pins the status, not the message. Changing the message to name the VLM tip debt is copy, so the operator decides it.
   - (b) An `AnalysisService` guard or test: evaluating an uncertified document is refused. Today only the CLI reaches the service (Verified), and the service does not re-check (AnalysisService.cs:71-93, Verified).
4. **Do not build** any of these:
   - the eta* law;
   - a distance-based tip zone for the envelope;
   - a 3D Cl_max knock-down (no sourced coefficient);
   - an external lifting-surface reference as a gate.
5. **Fallback if S2 finds flips below some tip-chord ratio r.** Use the measured strip-theory consistency edge.
   - Index: C = Cl_local / (a0 (alpha_eff − alpha_L0)), normalised at the root.
   - The tip band is every strip outboard of the first strip where C/C_root < 0.9.
   - Measured edges at n = 64/128/256: elliptic 0.937/0.926/0.923; rect 0.796/0.785/0.779; taper 0.5 0.888/0.885/0.883 (band3.py, re-run by the panel: Verified by them, Inferred for me).
   - Zero judged-strip flips over 20 calibration cases plus both falsifiers at n = 16..256 (same source).
   - The width is never a constant taken from recall (3 chords; Flagged) or from one n = 64 read.
6. **Any verdict change bumps the method identity.** If S3 changes which strips are judged, the verdict for the same inputs changes. The method identity must then bump from `cfdw.vlm-strip` 1.1.0 (MethodRecord.cs:19-22, Verified), and prior runs become Historical. The precedent is the 1.0.0 → 1.1.0 bump (area3-fixture-arithmetic.md:241-244, Verified). Keeping Ruling 78 as built needs no bump.

### 4.2 Mesh tip (SPIKE-03)

1. **The next and only mesh step is a tip-only coupon.** Half-span 20-30 mm, same section and same layer recipe, no solver. Variants:
   - **V0**, the current revolve. It must first reproduce the tip-pole and tip face classes.
   - **V1**, the tip of record: a planar flat cut at b/2, keeping the DR-F2-4 TE radius.
   - **V2**, K4: short flats at each pole before the revolve.
   - **V3**, stack height cut to about 0.3-0.6 mm at a fixed first height. This separates H-B/H-C from H-A.
2. **Amend DR-F3-5's precondition.** DR-F3-5 as ruled applies K4 only after K2 fails (fluids-round3.md:293, Verified). The coupon tests K4 (V2) and V1 without K2. That changes a ruled order, so the operator rules it (D5).
   - V1 is not a "rounded tip". It is the record's own tip, so it needs no DR-F2-4 tip exception.
   - The "TE blunted for analysis" part of DR-F2-4 and ADR-0012 D7 stays.
3. **Pre-registered readings**, written in the plan before the first run:
   - V1 passes the tip region of DR-F3-1 A → adopt V1 and retire the DR-F2-4 tip exception.
   - V1 fails at its convex edges and V2 passes → pole degeneracy (H-A) is supported, and K4 goes to the operator under the amended DR-F3-5.
   - Counts fall with H in V3 and do not depend on pole treatment → fan or interference (H-B/H-C).
   - All variants fail regardless of pole and H → stop and report. Inside this step there is no K2, no structured generator and no cfMesh.
4. **Receipts per variant:**
   - mesh-locate-r3.py by region and wall distance;
   - the count of Gmsh "Skipping boundary layer extrusion of degenerate curve" lines (target 0);
   - watertight within the 10 µm join tolerance;
   - measured continuity of the cap-to-skin seam;
   - wall nodes within 10 µm of the reference surface;
   - wall-triangle aspect ratio per region, from the surface mesh;
   - low-weight face count, since the 4 such faces sit at the tip TE (verdict-round3.md:128-129, Verified).
5. **O5 stays rejected.** O5 exempts the tip from the gate. A mesh below the floor that feeds a result shown as current is a hard-veto trigger.
6. **Long-term rule, recorded now, built after the coupon names a meshable closure.** The product mesh route reads the record evaluator (a sampled or STEP skin plus the record tip), not its own NACA section.
7. **After an AR 8 pass, round 3 is not finished.** R3-M2 (h1 6.0 µm plus y+ screen), R3-M3 (AR 5 and AR 12) and R3-M4 (the 3-D y+ gate solve) still have to run (verdict-round3.md:17; fluids-round3.md:67, :139; Verified). The spec's SPIKE-03 exit is "AR 5/8/12 wings on both OSes" (cfd-workbench-v1.md:2702-2703, Verified). Rulings 65 and 70 limit fluids work to macOS arm64 (rulings.md:787, :817, Verified headings). Whether the spec's "both OSes" was amended for SPIKE-03 is Flagged. Run/Results acceptance stays gated until the full exit is met (cfd-workbench-v1.md:181, Verified).
8. **ADR-0012.** The O6 split is supported by all three lenses:
   - accept D1-D5 and D7;
   - name the mesh route, the y+ gate **and the GCI clause** Open (Ruling 79 removes GCI until the operator's L3 run);
   - correct the stale `phase: … round 3 planned` header (adr-0012:7, Verified) and D6 "no new floor" (adr-0012:178, Verified; DR-F3-1 A since measured).

   It is the operator's decision. It is acceptable only if no wing RANS result is shown as current.

### 4.3 Tip geometry

1. **Declare the open planar end at the last authored station the v1 tip of record** for every consumer:
   - VLM, as today;
   - RANS, as coupon V1;
   - the STEP "closed shell with tip cap" variant, as the planar end face.

   There is no new record attribute, no identity-hash change and no history rule. The spec sentence at cfd-workbench-v1.md:799 ("carries the tip closure") gets a one-line amendment. The wording is the operator's.
2. **`tip point` stays uncertified for analysis, as tracked debt.** Evidence:
   - The strip-midpoint artefact exists for any closing tip, at log rate for a linear Gamma closure (tipexp.py; the mapping from Gamma exponent to chord exponent is Inferred).
   - The closing-tip sweep verdict change and the midpoint instability (area3-fixture-arithmetic.md:183, :245, Verified).
   - The loft does not converge on a zero-chord tip, 2.5-18 mm (kernel-spike-occt-loft.md per the CAGD reader; Flagged for me).
   - The spec keeps closing tips as a design concept (cfd-workbench-v1.md:828). This is an analysis-certification limit, not a removal from the DSL.
3. **Record now, build later: the rule for any future cap.** If a cap ever enters the record:
   - it is a typed attribute of the tip-end Anchor, default `none`, generated by the evaluator (one authority);
   - it carries an inset-vs-extends-b planform rule;
   - a deviation report (dS, max skin deviation) runs before Apply.

   Trigger: a designer asks for a rounded tip, or V1 fails while a cap is the only meshable tip.

### 4.4 Polar coupling (A3c, already in flight)

Delivery: one seam request to the A3c track owner (`feature/a3c-polar-source`) **before** that track builds the consistency check. Do not wait for A3c to land.

1. **Envelope per judged strip.** It becomes the polar's converged bracket at Re_local (DR-ANA-14, area3-analysis.md:827, Verified). The only measured bracket today is NACA 0012, α ±6°, Re 2e5-1e6 (spike-ana-1/verdict.md:28, Verified). That is a tested bracket, not an attached-flow limit (verdict.md:84, Verified). So "outside the polar bracket" and "outside the attached-flow envelope" are different statements. Which one the strip verdict means is decision D14. k1 stays indeterminate; k2 onward are judged like any other strip.
2. **Consistency-check exemption zone.** Inside the measured consistency edge (C/C_root < 0.9), the per-strip check of polar Cl(alpha_eff) against lattice Cl_local is suppressed, not failed. The edge is re-measured with the polar's linear a0 and alpha_L0, not 2π and the thin-airfoil alpha_L0. Without the exemption, about 30 of 64 half-span strips at n = 64 would flag (ratio 0.06-0.20 on k1-k4, per smallest-correct; Inferred for me).
3. **Stall on judged strips** by the critical-section method (graft):
   - max of Cl_local / Cl_max(Re_local);
   - report the critical station (eta, mm), Re_local and the margin;
   - second check: alpha_eff − alpha_L0 lies inside the polar's converged bracket.

   Source the method before any margin is shown. It is Flagged: recalled, and not in the KB.
4. **Low-Re strips.** A strip with Re_local below the polar envelope reads Unavailable, named, and is never extrapolated. Example: a 40 mm tip at 10 kn, Re 1.7e5 < 2e5 (area3-analysis.md:342-343, Verified). For k1, "tip strip, not judged" takes precedence over "Re outside polar range": one reason, not two.
5. **`analysis_confidence`** is a section validity flag, never an error bar, and never clears a tip strip.
6. **Show the un-judged share.** Whenever any strip is indeterminate or exempt, show the un-judged share of lift and root bending beside the loads. Show the loading trend into the tip as a fact, never as a verdict (graft). Strip-to-total reconciliation (ANA-11, cfd-workbench-v1.md:1194, Verified) is unaffected, because the share is shown, not subtracted (Inferred).
7. **Ruling 85's conditions** (MIT notices, weight hashes, no XFOIL shipped) and SPIKE-ANA-1's CFD-review conditions bind the A3c build (rulings.md:909, Verified). They are not tip items, but S6 lands inside that build.

### 4.5 How Ruling 78 evolves

**Keep it now, and add the written scope.** Draft content, wording the operator's: "Applies to certified finite-chord tips. A closing tip (`tip point`) is not certified for analysis until a tip-zone rule exists. The elliptic fixture planform is a lattice fixture, not a record-representable document."

**Close out the predecessors.** The operator retires, in writing:
- Ruling 75's lattice-scaled tolerance (option c);
- Ruling 77 condition (5) ("provisional until the eta* law is built");
- the "eta* as a convention (reversible to option b)" item that Ruling 77 held for the operator.

**Later, on S2 evidence only,** one of:
- no change;
- judge k1 on finite tips (it converges and is conservative), with a method-identity bump;
- a tip-chord-ratio floor below which the measured consistency band applies, with a method-identity bump.

Never the eta* law. COPY-218..220 wording stays with the operator (Ruling 82, rulings.md:891, Verified). LabelsTests pins those strings (LabelsTests.cs:103-104, Verified), so any copy change is a tracked edit.

## 5. What the designer sees, state by state

All strings below are content, marked **proposed - awaiting operator**. COPY-218..220 belong to the operator.

| State | What is shown (proposed - awaiting operator) |
|---|---|
| Certified finite-chord tip, analysis run | The drawn tip: a flat end at the last station. Reported b and S are the drawn values. The outermost strip per half reads the tip state with its reason. Today that is COPY-218 "provisional" (AnalysisProjection.cs:154, :169, Verified). Ruling 78 calls it indeterminate, and the state name and wording are D13. Every other strip carries an Inside/Outside verdict that does not change with refinement at taper ≥ 0.5 (measured). A real excess near the tip shows Outside on the next strips in. |
| Any strip indeterminate or exempt | The un-judged share of lift and root bending, beside CL, CDi, centre of lift and root bending. The loading trend into the tip, drawn as a fact. |
| `tip point` document | Unsupported for analysis. Today's reason is "Only an open tip and no assertions are currently certified." (Geometry.cs:338, Verified). Whether the reason should name the VLM tip debt is copy (D13). |
| After A3c lands | A per-strip envelope from the real polar at Re_local, with its basis per D14. Near-tip strips with Re below the polar floor read Unavailable with their Re. Strips inside the consistency edge show no consistency warning; the omission is stated, not hidden. Critical station, Re_local and margin are shown only after the method is sourced. |
| Cavitation panel | The sheet screen by −Cp_min at stations, with its fixed string. "Tip-vortex cavitation not screened at any tier" sits next to it, not only in the not-modelled list (graft). |
| Ventilation panel | Static depth margin, Fr_h and the fixed ventilation string; never "ventilation-safe". The "Tip depth" row is h minus the maximum station `ElevationMeters` (AnalysisProjection.cs:92, Verified), so it is a shallowest-station margin. Its scope label is decision D9. |
| RANS results | None shown as current until the mesh route passes DR-F3-1 A and the SPIKE-03 exit (AR 5/8/12) is met. When shown: "tip modified for analysis" with design-vs-analysis b and S if the tip is not the record's, plus "screening comparison; single mesh; not quantified". |

Under the operator's standing rule, any new UI state (band share, trend) is mocked up and visually approved before a build track starts.

## 6. Execution plan

| Step | What | Owner lens | Cost | Exit criterion | Cap |
|---|---|---|---|---|---|
| S0 | Write this proposal to `docs/plans/tip-handling.md` (Ruling 80). Send the decision batch (section 8) to the operator. | Tech Lead drafts; operator rules | About 30 min; no compute | Plan file committed. Rulings recorded in docs/notes/rulings.md, including the Ruling 75 / 77(5) close-out. Answers taken back to the decision requests. | One batch; a second batch on the same items is a defect signal |
| S1 | Salvage the held branch onto a fresh branch from main (section 6.2). Add the `simplify:` marker. Add the two red-first controls (`tip point` Assess → Unsupported; uncertified evaluate refused). Fix the ProductWingMethod.cs:66 doc comment and area3-analysis.md:322. | Analysis developer with Test Architect | 1-1.5 h | `tools/run-tests.sh` green in the 60 s ring. `TipLawGeometry`/`TryTipBasis`/`JudgePoints` absent from main. Proof docs present. Both controls observed red then green. Stale eta* text gone. | 2 repair cycles; stop and report at the cap |
| S2 | Small-tip-chord VLM sweep on the held probe: taper ratios {0.01, 0.02, 0.05, 0.1, 0.25}; alpha {5, 8, 18}; n {32, 64, 128, 256}. Record k1-k3 alpha_eff and verdict, plus the consistency edge C/C_root < 0.9 per case. Readings pre-registered in the run file: no judged-strip flip across n at any ratio → no change; flips below ratio r → fallback band at the measured edge. Optional second arm: fixed-eta comparison against AeroSandbox VLM and a numerical lifting line (section 3). | Hydrodynamicist / CFD lens | Solves 0.05 s at n64 and 2.1 s at n256 (CSV `seconds` column, Verified by the panel); 60 cases, under 5 min compute (Estimate); about 1 h authoring and reading. The second arm adds under 1 h. | Per ratio: flip or no flip, plus the consistency edge, written to docs/proof/vlm-tip-study/ | One run set, no law fitting; a fitted correction is the eta* trap again |
| S3 | Ruling 78 follow-up from S2: no change; judge k1 on finite tips; or a tip-chord-ratio floor with the measured band. | Operator rules; analysis developer implements | 0-1 h | Ruling recorded. Any code change lands red-first with fixtures (rect AR 8 alpha 18: k2/k3 Outside; elliptic excluded) and a method-identity bump. | 2 repair cycles |
| S4 | RANS tip coupon V0-V3, with pre-registered readings and the B-rep receipts (section 4.2). Needs D5: it lifts Ruling 80's mesh hold and amends the DR-F3-5 order. | Fluids/mesh (CFD & Numerical Verification); Hydrodynamicist reviews readings; CAGD adversary on the B-rep predicates | About 1 h authoring; about 1.5 min and under 2 GB per variant (Estimate from measured M1b: 4.52 M cells, 1.07 kB/cell); one session | Each variant's tip-region gate result recorded against its reading; the cause named, or "not separated" with the reason | One session, at most 6 mesh runs, 2 repair cycles. If V0 does not reproduce, stop. |
| S5 | If V1 passes: one full AR 8 DR-F3-1 A run on the tip of record. If V1 fails and V2 passes: the operator rules the amended DR-F3-5, then one full AR 8 run on K4. | CFD lens; Hydrodynamicist for the b/S record | 1.5 min compute, 4.8 GB (measured M1b) | AR 8 passes DR-F3-1 A; design and analysis b and S recorded, or they coincide | One geometry variant plus one repair; a second failure goes to the operator (O3/O4) |
| S5b | Resume round 3 from R3-M2: h1 6.0 µm plus y+ screen, then R3-M3 (AR 5 and 12), then R3-M4 (3-D y+ gate solve). | CFD lens | Per fluids-round3.md: about 90 min compute for the mesh block (Estimate, :67); AR 12 checkMesh about 5 GB (Estimate, :139) | SPIKE-03 exit evidence at AR 5/8/12 on macOS arm64; ADR-0012 mesh-route and y+ rows updated | Round 3's own caps; needs the operator to lift Ruling 80's hold (D15) |
| S6 | A3c conditions, sent now as a seam request to the running A3c track: consistency exemption at the measured edge with the polar slope; tip-strip reason precedes Re-Unavailable; critical-section stall on judged strips (sourced first); `analysis_confidence` never clears a tip strip; low-Re tip fixture (40 mm tip at 10 kn, seawater 15 °C, Re about 1.7e5, reads Unavailable). | A3c owner with Test Architect; Hydrodynamicist adversary | Small inside A3c: one predicate plus 3 fixtures | Seam request accepted or ruled. Red-first fixtures green: rect AR 8 alpha 5 n64, strips beyond the edge pass the check and strips inside are exempt; the low-Re tip reads Unavailable. | 2 repair cycles under the A3c cap |
| S7 | Documentation rulings, no build: tip-of-record amendment at spec :799; ADR-0012 split; inset-vs-extends-b and deviation-report rule for any future cap; mesh-route-reads-the-record rule; KB 13:226 re-read; "Tip depth" scope label; spec SPIKE-04 "with GCI" text (cfd-workbench-v1.md:1330, :2704) aligned to Ruling 79. | Operator; CAGD and CFD lenses draft | About 30 min each | Each item recorded in rulings.md, the ADR, the spec amendment log or lessons | One decision round each |

**Parallelism.** S1, S2 and S4 are independent after S0 and may run in three worktrees (fan-out cap 3, per Ruling 80). S3 waits on S2. S5 waits on S4, and S5b waits on S5. S6 waits only on S0's D8 ruling. S7 items can run alongside S0. Nothing in the VLM or polar tracks waits on the mesh, and nothing in the mesh track waits on the VLM.

### 6.2 What the held branch contributes

`fix/a3a-vlm-tip-law` is at c53bf15, 15 files, +11,635/−21 against main (`git diff --stat main...fix/a3a-vlm-tip-law`, Verified). Do not merge it whole. Cherry-pick onto a fresh branch from main in S1.

| File group | Disposition | Reason |
|---|---|---|
| docs/proof/vlm-tip-study/ repaired-1.1.0.csv, repaired-falsifiers-1.1.0.csv, run-repaired.sh, probe/Program.cs (+4/−… on a probe that already exists) | KEEP | The measured data behind this proposal, and the S2 harness |
| repaired-verdict.md, verdict.md addendum (+4) | KEEP, status "superseded by the tip-handling study" | The record of why eta* was rejected: mid-span anchor, 10.548 deg, Cl_local falsifier (Verified lines 52-61) |
| repaired-law.py | KEEP as a historical script labelled "rejected convention" | Not a method |
| docs/lessons/defect-classes.md (+8) | KEEP after review | Classes VLM-TIP-A..D, per the proposals; not opened by me (Flagged) |
| src/CfdWorkbench.Analysis/MethodRecord.cs (+165) | DROP | The eta* law: `TipLawGeometry`, `Calibrated` (which recognises the elliptic planform by chord matching sqrt(1−eta²) to 1e-6, unreachable for B-spline rails), `TipStation`, `JudgePoints`, `TryTipBasis`, `StripVerdictState`. Main already marks the strip through `StripLoad.Provisional` (Verified). |
| src/CfdWorkbench.Analysis/VortexLattice.cs (+4) | DROP | Only the `TipLawGeometry` wiring |
| tests/.../LatticeFixtureTests.cs (+188) | SPLIT | Keep the analytic mid-span anchor and any assertion that does not reference the law types; drop the law tests. The per-assertion split is Flagged (not read assertion by assertion). |
| docs/design/area3-analysis.md (+12) | REVIEW, keep only the finding | Not opened (Flagged). Drop text that adopts the law. Main's :322 is amended in S1 regardless. |
| docs/audit/*, docs/docs-index.js | REGENERATE, never merge | Derived artifacts |

Deleting the branch and its worktree (`/Users/mallalieut/projects/CFD-Workbench-fix-a3a-vlm-tip-law`) after salvage is destructive. That is the operator's decision.

## 7. Alternatives considered and why rejected

- **Replace Ruling 78 now with a measured strip-theory consistency band (physics-first).** This is the strongest physics argument, and the only rule with a measured no-flip proof: zero judged-strip flips over 22 cases (panel re-run). It is rejected as a build *now*, for four reasons:
  - Its headline flaw (k2/k3 Outside survive Ruling 78 at n64, alpha 8) is on the elliptic closing tip only, which the product cannot certify (Verified).
  - The band is large on low-taper wings: 37/64 judged strips on rect at n64 (panel).
  - Before the polar lands, the edge drifts with alpha on cambered sections (0.853 → 0.812).
  - Its nine-step chain includes councils and a 2.4-7 h solve, which the operator has rejected as ceremony.

  Its index and measurements are grafted as the S2 fallback and as the A3c exemption width.
- **Tip zone by kappa × mean chord, with a boundary verdict at eta_b (geometry-first).** Rejected on a fatal flaw. At eta_b = 0.975, the lattice's alpha_eff and Cl_local disagree by a factor of 2-4 (rect alpha 8: 5.06 deg against Cl_local 0.24; panel re-run of zone.py). So the boundary "verdict" has no 2D-section basis, and the CFD-veto conflict is moved, not resolved. Two more problems:
  - kappa × c_bar scales with the wrong chord. The measured departure tracks the tip chord: edges at 0.78/0.88/0.92 for rect/taper/elliptic.
  - Its typed-closure build (2-4 days) and mesh generator (1 day) would come before the coupon that says what is meshable.

  It also re-purposes `StripVerdictState.AtBound` and `UncertaintyDeg`, which were defined as "discretisation of the η* convention" (repaired-verdict.md:52, Verified). Its B-rep predicates, one-authority argument, exponent probe and inset/extends-b rule are grafted.
- **Keep Ruling 78 silently (no scope note, no measurement).** Rejected. The scope limit (taper ≥ 0.5 measured) would stay unwritten. The closing-tip path through a future desktop caller would stay unguarded. The stale eta* text (area3-analysis.md:322, ProductWingMethod.cs:66) would keep promising a law that will not be built.
- **Judge tip strips by Cl_local alone.** Rejected. Cl_local → 0 at finite tips (Verified), so a genuine excess cannot read Outside.
- **3D Cl_max knock-down on tip strips.** Rejected: the KB has no sourced coefficient.
- **Inboard-extrapolated alpha_eff for display.** Deferred. It is display-only, it adds surface area, and the fixed-eta value is still not a 2D section.
- **Exempt the tip region from the mesh gate (O5).** Rejected. A mesh below the floor that feeds a result shown as current is a veto trigger, and the 4 low-weight faces are real interpolation defects.
- **Build K2 (per-node height scale) or the structured generator (O4) now.** Rejected:
  - K2's behaviour outside the view support is undocumented. Its docstring is at gmsh.py:6214-6227 (fluids-round3.md:121, Verified citation; the behaviour claim is Flagged).
  - O4 is days of work before the cause is named.

  This rejection reorders DR-F3-5's ruled "K2 first" sequence, which is why D5 asks the operator.
- **`tip point` as the analysis default.** Rejected. It is the worst case for the loft (2.5-18 mm), for the VLM (log-divergent evaluator) and for the mesh (pole).

## 8. Decision requests for the operator

| # | Decision | Options | Recommendation |
|---|---|---|---|
| D1 | Ruling 78, and closing out its predecessors | (a) Keep as built, with the written scope "certified finite-chord tips only; closing tips not certified for analysis; elliptic fixture planform not record-representable". Retire Ruling 75's tolerance law, Ruling 77(5) and the held "eta* as a convention" item. (b) Replace now with the consistency band. (c) Replace with a kappa zone. | (a) |
| D2 | S2 VLM sweep | Approve: lifts this study's read-only rule for lattice solves, under 5 min compute. Or defer. | Approve. Include taper 0.01 and 0.02 at alpha 18, with pre-registered readings. |
| D3 | Tip of record for v1 | (a) The open planar end at the last station, for VLM, RANS and STEP; no new attribute. (b) A typed closure attribute now. | (a). The wording of the spec :799 amendment is yours. |
| D4 | `tip point` | Stays uncertified for analysis as tracked debt / certify with a zone rule | Stays uncertified |
| D5 | S4 mesh coupon, and the DR-F3-5 order | Approve V0-V3: lifts Ruling 80's mesh hold for one session, and amends DR-F3-5 so K4 (V2) may be tested without K2 failing first. Or defer. | Approve. Rule K4 under the amended DR-F3-5 only if V1 fails and V2 passes. |
| D6 | ADR-0012 | (a) Split: accept D1-D5 and D7; name the mesh route, the y+ gate and the GCI clause (Ruling 79) Open; correct the header and D6. (b) Keep whole and Proposed until an AR 8 gate pass and the y+ solve. | (a), with no wing RANS result shown as current |
| D7 | Held branch | Approve the keep/drop/split in 6.2; delete the branch and worktree after salvage, yes or no | Approve the split. Deletion is yours. |
| D8 | A3c conditions, sent now to the running A3c track | Approve: consistency exemption at the measured edge; tip-strip reason precedence; critical-section stall (sourced first); `analysis_confidence` never clears a tip strip | Approve, as a seam request before A3c builds the consistency check |
| D9 | Scope label of the "Tip depth" row | Shallowest-station margin (current) / tip surface including thickness and cap | Your call. Today it reads the maximum station `ElevationMeters` (Verified). |
| D10 | Tip-vortex cavitation | Keep "not screened" for v1, placed next to the −Cp_min screen / commission a screen as research | Keep "not screened" for v1 |
| D11 | Future cap rule (record now, build later) | Inset within b/2 with dS reported / extends b, with every b/S/AR readout stating its basis | Inset |
| D12 | KB 13:226, "tip-vortex (lowest σ_i on a finite wing…)" (Verified wording) | Re-read source S10 and correct it if inverted | Re-read (Flagged physics) |
| D13 | Tip copy and state name | COPY-218 ("provisional — tip law cannot judge…") and COPY-220 ("indeterminate — the tip law cannot judge…") both cite a tip law that will not exist. Decide which state the outermost strip carries (code shows provisional; Ruling 78 says indeterminate). Decide whether COPY-219 "at the bound (+-U)", a law-only state with no producer on main (only LoadsViewTests.cs:74 builds it), is retired. Decide whether the `tip point` refusal names the VLM debt. Decide every new string in section 5. | Yours. LabelsTests pins the strings, so a track flips them. |
| D14 | What a strip's "envelope" means after A3c | (a) The polar's tested bracket (today NACA 0012, α ±6°, Re 2e5-1e6) as the bound. (b) Keep 10° / Cl 1.0 as the attached-flow bound, and show "outside the polar's tested bracket" as a separate availability reason. | Raise this with the A3c owner and the Hydrodynamicist. DR-ANA-14 says (a), but SPIKE-ANA-1 says its bracket is not an attached-flow envelope. |
| D15 | Resume round 3 after an AR 8 pass (S5b) | Lift Ruling 80's hold for R3-M2..M4 / hold until the operator reviews S5 | Resume. The SPIKE-03 exit needs AR 5/8/12. |
| D16 | Spec text for SPIKE-04 | Amend "with GCI" (cfd-workbench-v1.md:1330, :2704) to match Ruling 79's code-to-code fixture, with GCI returning after the L3 triplet | Amend. The wording is yours. |

## 9. Residual risks and assumptions

- **[Major] `assume:` small but finite tip chords do not flip k1-k3 at the default n = 64.** This covers taper ratios well below 0.5. The certifier sets no minimum chord, so these tips are reachable. S2 confirms or refutes it. If false, Ruling 78 gains a tip-chord-ratio floor, the measured consistency band applies below it, and the method identity bumps.
- **[Major] `assume:` the VLM stays reachable only through a path gated on Certified.** Verified for the CLI (Program.cs:96-102). Only the CLI constructs `AnalysisService` (git grep). The S1 control turns this into a test. If false, closing tips reach the lattice and the k2/k3 flips return.
- **[Major] `assume:` a planar flat cut (V1) survives Gmsh 3-D boundary-layer extrusion at its 90° convex edges.** Only S4 confirms this. It is Flagged: forum snippets report collapse at sharp convex edges. If false, the analysis tip again differs from the record, and DR-F3-5 and the b/S deviation report return.
- **[Major] The RANS coupon may not separate pole from fan or interference.** Then "not separated" is reported and the structured-route decision goes to the operator. No K2 on speculation.
- **[Major] The polar's measured bracket is narrow:** one foil, α ±6°, Re 2e5-1e6 (Verified). Under DR-ANA-14 as written, most strips on a non-NACA-0012 section, or at α beyond 6°, may read Unavailable or Outside for reasons unrelated to the tip. D14 settles this before S6's fixtures are written.
- **[Minor] `assume:` alpha_eff near a finite tip is conservative within the flat-wake model,** because alpha_i there (rect k1 about 1.94 deg) exceeds mid-span (0.955 deg). Inferred from Verified CSV values. S2's rows confirm it. If false, a false Inside could occur on k2/k3. The falsifier fixture (rect alpha 18 Outside on k2/k3 at every n) is the control.
- **[Minor] `assume:` the consistency edge measured with 2π and the thin-airfoil alpha_L0 is within a few percent of the edge with the real polar slope.** S6 confirms it by re-measuring. If false, the exemption width is wrong by that amount. It is re-measured, never hard-coded.
- **[Minor] `assume:` main's production verdict equals the CSV raw verdict.** The constants are the same: 10° / Cl 1.0 / 30° (MethodRecord.cs:19-22, Verified). No main run was executed. If false, the elliptic flip counts are off, but they concern a planform the product cannot certify.
- **[Minor] Tip-vortex roll-up and side-edge separation are not modelled at any tier.** They could move real tip loading in either direction. The not-modelled label stays (COPY-225/226, Verified). Towing-tank or flight test is the only settlement.
- **[Minor] Some physics rules are Flagged recall and not in the KB:** the critical-section method and the "about one chord" lifting-line validity scale. A margin is shown only after a source is cited. The KB's near-tip strip-theory failure statement is itself labelled Inferred in the KB (07:94).
- **[Minor] Whether the spec's SPIKE-03 "both OSes" exit was amended to macOS arm64 only** (Rulings 65, 70) is Flagged. If not amended, Run/Results acceptance also needs a Windows mesh run.
- **[Nit] Not read line by line (Flagged):** the held-branch test split and the 12-line area3-analysis.md addendum. S1's owner sorts them. Dropping a test that pins a real finding would lose a control.
- **Scope of all VLM numbers:** AR 8, nc 4, cosine spacing, three planforms, flat / 4 % camber / 1° washin. Sweep, dihedral, winglets, near-surface image cases and nc variation stay unmeasured until S2.
- **Scope of all mesh evidence:** macOS arm64, Gmsh 4.15.2, OpenFOAM v2512 as pinned. Windows is untested.

## Completeness review

**What I changed, with evidence opened in this session:**

1. **Main SHA (Flag resolved).** bf07a8f is an ancestor of main: it is the Rulings 78-83 commit. Main is now 70f7d49 and carries the SPIKE-ANA-1 merge and Rulings 84-85.
2. **Destination named.** Ruling 80 sets `docs/plans/tip-handling.md` (rulings.md:879). The file does not exist; writing it is added to S0.
3. **The displayed tip state was wrong in the draft.** Main shows COPY-218 "provisional", not COPY-220 (AnalysisProjection.cs:154, :169, :187). Both strings cite a "tip law" that is being dropped. COPY-219 (AtBound) has no producer on main. D13 is expanded to cover the state name, retiring COPY-219 and the `tip point` refusal wording.
4. **Stale eta* promises listed.** area3-analysis.md:322, ProductWingMethod.cs:66, and Ruling 75 / Ruling 77(5) together with Ruling 77's held "eta* as convention" item. Fixes are added to S1, and a close-out is added to D1.
5. **Closing tips are supported by design, not absent.** FM-3 (area3-analysis.md:512), `Vlm_ClosingTip_FiniteAndListed`, F-2 and COPY-217 all show this. The sweep-verdict change and midpoint instability on closing tips (area3-fixture-arithmetic.md:183, :245) are added. The scope note now says "lattice fixture, not record-representable" rather than "probe-only".
6. **The `tip point` control was reframed.** Assess already returns Unsupported with a generic message (Geometry.cs:337-339). No test pins it. The control pins the status, and the message is the operator's copy.
7. **Certifier minimum chord.** The certifier sets no minimum tip chord (Geometry.cs:371). Taper 0.01 is added to S2.
8. **Method identity bump** is added for any S3 verdict change (MethodRecord.cs:19-22; precedent area3-fixture-arithmetic.md:241-244).
9. **DR-F3-5 ordering.** The ruled order is K4 only after K2 fails (fluids-round3.md:293; verdict-round3.md:114-115). The coupon reorders it, so D5 now asks for that amendment. V1's relation to DR-F2-4 is stated.
10. **Round 3 after AR 8.** R3-M2..M4 and the spec's AR 5/8/12 SPIKE-03 exit were untraced (verdict-round3.md:17; cfd-workbench-v1.md:181, :2702). S5b and D15 are added.
11. **SPIKE-04 / Ruling 79.** The GCI clause is added to ADR-0012's Open list in D6. A spec amendment for "with GCI" (cfd-workbench-v1.md:1330, :2704) is added as D16 and S7.
12. **A3c timing was wrong.** SPIKE-ANA-1 is GO and merged, and the A3c track is running (feature/a3c-polar-source, 4840786). S6 is now a seam request sent now, not "after SPIKE-ANA-1 passes".
13. **The polar's measured bracket** (NACA 0012, α ±6°, Re 2e5-1e6; "not an attached-flow envelope", verdict.md:28, :84, :135) was missing. Its consequence for the per-strip envelope is added as D14 and a Major risk. Ruling 85 conditions are noted.
14. **Labels promoted to Verified:** the mid-span anchor (repaired-verdict.md:54-57), the 10.548 / 4.045 deg numbers (:58-61), the ell-only k1-k3 > 10 deg rows (awk), the make-wing-gmsh.py revolve lines (:80-81), the AnalysisService no-recheck (:71-93), the spec :797-799 and :828 text, KB 07:94 and KB 13:226 wording, and the mesh numbers in verdict-round3.md:100-175.
15. **S0 and S7 exits made concrete.** S5b gained a cap that references round 3's own caps.

**Still unresolved:**
- Held-branch test split and the area3-analysis.md +12 diff (not read line by line).
- Whether the spec's SPIKE-03 "both OSes" was amended (Flagged).
- The K2 view-support behaviour (Flagged).
- The critical-section method source (Flagged).
- KB 13:226 physics (Flagged).
- The physics-first band numbers and the tz/tipexp probes are panel-verified only; I did not re-run them.
- D14 conflicts with DR-ANA-14 as approved in Ruling 63. It needs the A3c owner and the operator.

**On the operator's question ("is it working still? we had that CD issue"):** if the "CD issue" means the hook working-directory problem, the fix is on main:
- ad5769a "merge: hook commands survive a cwd change (HOOK-CWD-RELATIVE)" and d457868 (Verified, `git log`).
- `.claude/settings.json` hook commands fall back to `git rev-parse --show-toplevel` (settings.json:9, Verified).
- This session's shell calls from the repo root ran without hook errors (observed). That is not a full test from a moved cwd.

If "CD" meant something else, such as continuous deployment, I did not check it.