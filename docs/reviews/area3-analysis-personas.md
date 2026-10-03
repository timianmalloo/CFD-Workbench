---
id: review-area3-analysis-personas
title: Area 3 analysis design — five lenses in Adversary Mode, and the folds
type: doc
status: in-review
owner: "@timianmalloo"
phase: design-slice
tags: [review, area-3, analysis, personas, gate, vlm, data-model, test-plan]
links:
  - {to: design-area3-analysis, rel: documents}
  - {to: mockup-area3-analysis, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2027-04-03
summary: >-
  Gate record for design-area3-analysis revision 1. Hydrodynamicist and Test Architect blocked (VLM result without its
  method envelope; story clauses without tests); CFD verification, computational geometry and data persistence approved
  with changes. Every finding is folded into revision 2 or carried as a DR-ANA item; the mockup's UX and accessibility
  review is recorded at the end. Revision 3 (repair cycle 2 of 2) folds the two lenses' rev 2 re-review (B-H1, M-H1,
  B-T1, M-T1…M-T3 and minors), mapped finding by finding with file:line.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# Area 3 analysis design — the gate

Revision 1 of `docs/design/area3-analysis.md` went to five lenses at once (2026-10-03), each in Adversary Mode, each
reading the design, the spec and the code independently. Revision 2 folds every finding. Clearance of a veto belongs to
the lens at its next review, not to the author.

| Lens | Verdict on rev 1 | Veto | Main findings | Fold in rev 2 |
|---|---|---|---|---|
| Hydrofoil hydrodynamicist | **BLOCK** | **fires** — a VLM number shown as Current without its method envelope | α_i taken from the total control-point velocity (would cancel α_geo); cd needs both Ncrit and a named lookup variable; tip strips below the Re envelope; reconciliation compared 0 with 0 and mixed axes; F-13 would fail a correct inviscid build; friction "bound" pointed the wrong way; cavitation screen unspecified; station result needs its own label; "deep water" label vs depth unset; F-2/F-8 expectations | §5.4 envelope column + per-strip verdict + FM-21 + new copy; §5.2 α_i from trailing wake; cd at both Ncrit at α_eff with consistency check and Re-envelope rule; wind-axis, half-span reconciliation called bookkeeping; F-13 split A3a/A3c; §5.1 wording; §5.1 screen conditions; "Strip of wing run (α_eff)"; DR-ANA-13; F-2/F-8 revised; DR-ANA-11 label |
| CFD & numerical verification | APPROVE WITH CHANGES | no (fires if "Verified implementation" renders before ring 0 is green on both OSes) | independent numpy re-run: F-2 fails a correct lattice (CL 0.4197, e 1.016 at 32 × 6; converges from above at p ≈ 1); F-1 used Helmbold on a rectangle; α_i definition; F-10 axes; F-6 order unstated; singularity cutoff and κ missing; run key missed evaluator and wake settings; no swept fixture (Bertin–Smith reproduced at 3.4440/rad); golden-master tolerance too loose; SPIKE-ANA-1 mixed two tests | DR-ANA-12 (spec fixture correction); F-1 Richardson 0.5 %; F-15 α_i; F-6 p = 1, three lattices, ring 0; cutoff and κ in settings/diagnostics; evaluator + placement-rule + wake in the key; output-hash guard; F-16 Bertin–Smith; F-17 own golden master; F-14 matched lattice 0.5 %; SPIKE-ANA-1 split; §12.5 label condition |
| Computational geometry | APPROVE WITH CHANGES | no | placement must be computed in Core (`Place` is internal); thickness defined wrongly (must come from Section/Blend, not Components); zero-chord tip exclusion wrong for cosine spacing; camber slope must be analytic; manifest lacks evaluator id and rule version; G-1 is "may read" | §4 `Sections` returns `PlacedCamber`, `Camber`, `Thickness`, `CamberSlope` from Core; bitwise test vs `Surface`; FM-3 rewritten (diagonal normals, 10 µm rule); manifest `evaluator`, `placementRule`; G-1 wording |
| Data & persistence | APPROVE WITH CHANGES | no (conditional on 1, 2, 5) | `.bak` and atomic replace are new code without tests; pruning could delete a run Undo would revive; Failed rows unbounded; Trefftz scalars stored twice; idempotency not enforced at the store; tamper mechanism undefined; no forbidden-update test; format string hard-coded; sizes unmeasured | §3.6 `.bak` sequence + test; DR-ANA-4 reachability rule, tombstones, latest Failed per key; Trefftz derived (§3.5); store invariants under the lock; per-run content hash, key recomputed; forbidden-update tests; format from one function + `WhenWritingNull`; strip cap 2,048, size test |
| Test architect | **BLOCK** | **fires** — in-scope story clauses without a traced test | ANA-08/10/16/20/21/22/23, ANA-11 stiffness readout and §5.4 labels untraced; invariance rows pass a constant stub; F-10 tautological; F-13 unfailable in A3a; F-2 reference not written; F-14 provenance; ring claims were prose and `run-tests.sh` has no throttle; no pinned key vector; timing-race tests; tamper test too narrow; new strings not in copy-as-content | §13.5 story → test → slice matrix (ANA-08, ANA-16 marked out of Area 3 with reason); mutant column in §13.2; F-10 relabelled bookkeeping with a mutant; F-13 split; F-2 reference recorded before build; F-14 provenance; §13.4 enforcement (`jobs`, `named`, readiness, per-harness seconds) and G-13; pinned key vector and output hash; barrier seam; tamper test with a forged key; every §12.6 row in copy-as-content |

## What the gate changed that the author had wrong

- The author's run-tests claim ("≤ min(CPU/2, 4) harnesses at once") came from an audit summary about a different
  spawn; `run-tests.sh`:36–54 launches every harness at once. Corrected in §13.4 (G-13).
- The default lattice (32 × 6) was a guess; two independent re-runs showed e > 1 there. DR-ANA-7 now recommends 64 × 4
  with that evidence, confirmed by F-6 before A3a ships.
- The spec's own ANA-04 oracles fail a correct lattice — surfaced as drift (G-10, DR-ANA-12), not silently re-toleranced.

## Mockup review (UX & Accessibility)

**Verdict on mockup v1: PASS WITH CONDITIONS; the accessibility veto does not fire.** Findings and repair cycle 1 (of 2):

| # | Severity | Finding | Fold |
|---|---|---|---|
| 1 | Major | Running screen showed Evaluate, not Cancel (`sc.running` never set) | fixed: keyed on `state === 'running'` |
| 2 | Major | "Show table" had no handler; the chart had no text alternative | fixed: the twin toggles, focus kept (measured: 17 rows, focus on the button) |
| 3 | Major | navbar covered the Plan legend and the 3D caption (colour alone carried Γ) | fixed: legend and caption 48 px up; 3D labels on plates |
| 4 | Major | Outside-envelope, tampered and running-with-prior states missing | fixed: screens 7 (α 12°, 38 strips outlined dashed with a count) and 8; screen 2 keeps the α 2° run under its Historical banner |
| 5 | Major | station η 0.662 in Properties vs 0.667 on Plan and strip | fixed: one computed value everywhere |
| 6 | Minor | depth unset hid h/c and Fr_h | fixed: "h/c · Fr_h · σ Unavailable — depth not set" |
| 7 | Minor | visible "(placeholder key)" | fixed: removed; the page header and source note say keys are illustrative |
| 8 | Minor | 3D labels over arrows; clipped free-surface label; chart label collision at 1024 | fixed: label plates; label moved inside; elliptic label moved to the top-left |
| 9 | Minor | error card gave no next step | fixed: "Change the panel count or the tip, then Evaluate." (new copy, design §12.3) |
| 10 | Minor | dock and bottom tabs are spans without tab roles | **kept** — static, as in m12b2-views.html; the build uses the shell's real tab controls (app-shell). Residual for the native proof |
| 11 | Nit | status vocabulary; Re unit cells | fixed: "Analysis: no result / … / Unavailable" (design §12.2); Re rows show "–" |

After the repair the in-artifact audit is clean in light and dark at 1280 × 800 and 1024 × 700 (text contrast ≥ 4.5:1,
targets ≥ 24 px, no NaN or placeholder, text ≥ 11 px, no row overflow, no page errors). Not re-reviewed by the lens
after the repair; residual per the lens: overlay contrast over drawings, live-region announcement, keyboard on the
static tabs.

## Rev 3 repairs (repair cycle 2 of 2, 2026-10-03)

The hydrofoil hydrodynamicist and the test architect re-reviewed revision 2 and both still blocked, narrowly. Revision 3
folds every finding below; nothing was re-opened beyond them. Line numbers are revision 3's. Design =
`docs/design/area3-analysis.md`, mockup = `docs/mockups/area3-analysis.html`, note =
`docs/notes/area3-fixture-arithmetic.md`. Clearance stays with the two lenses.

### Hydrofoil hydrodynamicist

| Finding | Fix | Where |
|---|---|---|
| **B-H1** station readout Current with no per-strip verdict and no omissions | The strip readout carries its own envelope verdict (each bound with the strip's value), Re_local against the polar's Re range ("Unavailable — no polar method installed" in A3a, never "inside") and the "Not modelled" list; station rows in §12.2/§12.3; three named tests; the mockup's station state renders all three | design :362–375, :547, :562, :762–764; mockup :222–223, :312 |
| M-H1 finding rendered only in Labels | The run verdict sits on the row directly after CL in the Wing result group (removed from Labels) | design :349–352; mockup :318 |
| Minor: bound string dropped α_L0 and the sweep limit | Full bound always written: "(\|α_eff − α_L0\| ≤ 10°, Cl_local ≤ 1.0, quarter-chord sweep ≤ 30°) — n of m strips; exceeded: parts"; the predicate uses α_L0 and sweep | design :350–358, :561, §12.6; mockup :217–221 |
| Minor: no result state with depth unset | Screen 3b: the label reads "… free surface not modelled", h/c, Fr_h, σ, V_crit read COPY-45, no tip-depth or free-surface line; the label is a function of depth, not a constant | design :378–383, :558; mockup :208–209, :373 |
| Minor: DR-ANA-13 needs a spec amendment (spec :962 "never changes") | Recorded as a spec-owner amendment request with proposed 1.7 text; G-11 and §14 point to it | design :125–126, :771–772, :790 |
| Minor: b, moment datum and force axes missing from the Wing result group | Added under the result: "b 900 mm · moment datum: frame origin (root LE) · force axes: body, +x aft, +y starboard, +z up; L and D in wind axes" | design :347, :547; mockup :319 |
| DR-ANA-14 note | "Inferred; tighten per strip when the polar brackets land (low-Re tips stall earlier)" added with the per-strip rule | design :791 |

### Test architect

| Finding | Fix | Where |
|---|---|---|
| **B-T1** clauses traced only to story level | Each clause has its own named test with a failing input, a ring and a cost: ANA-11 missing terms (`Loads_MissingTerm_UnavailableNeverZero`), Not-assessed list (`Loads_StructuralNotAssessed_ListComplete`), attachment moment (`Loads_AttachmentMoment_TransferAboutNamedPoint`, worked (O − P) × F = (0, +10, 0) N·m vs mutant (0, −10, 0)); ANA-15 0–50 °C (`Water_OutsideTable_Unavailable`); ANA-03 CD ≤ 0 and V ≤ 0 (`Projection_CdZeroOrNegative_ClCdUndefined`, `OperatingPoint_SpeedZeroOrNegative_Undefined`); ANA-22 preview budget (`Toggle_NeverEvaluates` ring 0 + `Toggle_LayersFirstFrame_P95WithinPreviewBudget` readiness); ANA-23 arc and sign (`Layers_MomentArc_SenseFollowsSignAboutNamedDatum`, `Layers_Vectors_BodyFrameSignConventionLabelled`). Group names replaced (`Toggle_*`, "and three more", "screen rows", "§13.3 rows"); §13.3 is a per-test table; §13.5 has Ring and Cost columns | design :629–673 (§13.3), :711–764 (§13.5; B-T1 rows :726–727, :739–743, :756, :758–759) |
| **M-T1** F-8 mutant survives (0.8 % inside 1 %) | S_ref pinned to the developed area; dihedral raised to ±20°; expected 0.8938 ± 1 %; the dihedral-ignored mutant gives 1.0000, **11.9 % off** (measured; at ±10° it missed by 2.81 % developed and 1.25 % projected on this wing) | design :616; note :52 |
| **M-T2** F-6 off-by-one mutant is O(h) | Replaced by an O(1) mutant — wake length read per panel, not per wing — measured: CL +5.9/+11.9/+21.3 % at 32/64/128, p(CL) −0.735, p(e) 0.515, both outside 1.0 ± 0.2. Lattices moved to 32/64/128 (p(CL) 1.070, p(e) 1.013; 16/32/64 gave 1.183, at the edge) | design :614; note :51 |
| **M-T3** timing limits were prose; `SECONDS` is whole seconds | Checks C-1…C-6 designed as build-track work: ms clock in `run-tests.sh`, new `tools/check-test-costs.py` run at every join, `COST <name> <ms>` lines from a `Stopwatch` in the harness; each with its failing input (5900 ms passes `SECONDS` but fails C-2; 50400; 43100 naming DR-ANA-10; 512.3 ms per check; a missing `.ms`) | design :674–710 (checks C-1…C-6 :694–710) |
| F-2 band had no numbers | Band **[0.4156, 0.4198]** (0.4177 ± 0.5 %) from an independent JS lattice, CFD lens' 0.4167 inside; lifting line 0.4386, Helmbold 0.4282, Jones 0.427 and an unextrapolated 32-span 0.4206 fail it; the mid-panel mutant gives 0.2391 | design :610; note :50 |
| Minor: F-13 one mutant for two rows | F-13a (ν not read from the water record) and F-13b (polar looked up at the stored fresh-water Re → loads × 1.0269 exactly) | design :621–622 |

### What stays open (said plainly)

- Every cost marked est. is **Inferred**; the C# costs are measured at red-first (C-5 records them). F-1 and F-6 are
  named exemptions from the 0.5 s per-check rule (≤ 1.5 s) because A8.4 keeps them in ring 0; if C# measures them
  above 1.5 s, the harness budget needs a ruling, not a quiet move.
- The F-2 and F-8 expected values come from two independent re-implementations (this note's JS and the CFD lens'
  numpy), not from the product; that the C# lattice lands inside is Inferred until red-first.
- At 1024 × 700 the Properties pane now scrolls on four screens (7–135 px) rather than clipping; at 1280 × 800 only the
  failed screen scrolls (35 px).
