---
id: proof-a3a-prj2-cfd-veto
title: PRJ-2 CFD label veto proof
type: proof-pack
status: active
owner: "@track-prj2"
phase: implementation
tags: [a3a, analysis, prj, cfd, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: design-language, rel: depends-on }
review-by: 2026-11-04
summary: >-
  Red-first and planted-mutant evidence for the PRJ-2 CFD veto. Checks cover the displayed CL/CD claim,
  provisional tip verdicts, Trefftz e, original strip widths, moment-arc sign, and proposed copy scope.
---

# PRJ-2 CFD label veto proof

Session `prj2`, 2026-10-04. Tier T2 because optional `StripLoad` span edges extend the durable run record.
The full review target is the A3a Analysis result projection. DR-VLM-4 remains pending; this track retains
an `Indeterminate` verdict state without assigning it to a stored tip today.

## Red receipt

Each `FAIL` was observed from the Analysis console harness with `CFD_TEST_ONLY`. The width, arc, and copy
mutants were reverted after the red observation. The new tests use ring A in design §18.8; focused runtime
was 2.2–3.3 s including build, with the Example foil case about 0.5–0.6 s and other cases about 0.1–60 ms
including first-use JIT.

| Fix | Check and red line | Oracle / planted fault | Green evidence |
|---|---|---|---|
| 1 · total drag claim | `FAIL Projection_TotalDragMissing_ClCdUnavailable … expected Unavailable — total drag missing; actual 99.863` | A3a lacks total drag even with positive CDi. The product Example foil is also asserted in `TipStrip_ExampleFoil_OutermostProvisional`. | Both checks PASS; CL/CD is Unavailable. |
| 2 · provisional verdict | `FAIL Projection_ProvisionalTip_NotJudgedOutside … expected False; actual True` | A stored `ANA-TIP-PROVISIONAL` strip has an otherwise outside numeric verdict; it must not count outside or draw an outside outline. `Projection_ProvisionalVerdict_EmptyExceededNeverOutside` covers a provisional verdict without the stored flag. | Both PASS; run counts only judged strips. |
| 3 · e and attribution | `FAIL Projection_TrefftzLiftUsedForE … expected 0.497; actual 49.600`; `FAIL Envelope_EOutOfBand_AdvisoryNotBlocking … expected True; actual False`; planted unbounded attribution: `FAIL Labels_EAboveOne_LatticeAttributionOnlyMeasuredBand … expected False; actual True` | e uses Trefftz lift with Trefftz CDi. Low e carries no lattice cause; only 1 < e ≤ 1.02 at the default lattice has a measured-bias note. | All three PASS. |
| 4 · excluded strip width | Planted J-index width: `FAIL Projection_ExcludedClosingTip_UsesKeptStripEdges … expected 0.34142; actual Unavailable` | After a port closing-tip exclusion, contiguous J does not name the original edge. Direct integration of kept `ya/yb` is the oracle for CDi, induced drag, root moment, e, and lift per span. | PASS; `Projection_LegacyStripEdges_OmittedAndHashIntact` also PASS. |
| 5 · BC-1 sign | Planted negative arc x: `FAIL Layers_MomentArc_RightHandPositiveXMatchesRootMoment … expected True; actual False` | Positive starboard lift gives a positive x-axis root bending arc in this body's right-hand frame. | New and existing moment-arc checks PASS. |
| 6 · COPY-217 scope | Planted broad label: `FAIL Labels_VerifiedLattice_NamesFixtureScope … rectangular and elliptic expected True; actual False` | A verified-family label names the tested shapes and angle limits in the same row. | PASS; COPY-217 remains proposed. |

Second-cycle boundary check: `FAIL Projection_OneMissingSpanEdge_WidthUnavailable … expected Unavailable; actual
0.00040`. A strip with only `ya` had silently fallen back to the compacted J. The width reader now treats a
half-populated pair as Unavailable; the malformed, excluded-tip, and legacy-omission checks all PASS.

## Surface and contract evidence

| Surface | Writer → reader | Evidence |
|---|---|---|
| Durable strip span | `ProductWingMethod.Couple` copies `LatticeStrip.YInboard/YOutboard` into optional `StripLoad.Ya/Yb` → `AnalysisProjection.Width` | `TipStrip_ExampleFoil_OutermostProvisional` sees edges on every produced strip; excluded-strip integration check reads them; legacy omission/hash and partial-pair checks cover absent fields. |
| Envelope | `MethodRecord.JudgeStrip` and stored tip reason → projection `VerdictState` → `MethodRecord.JudgeRun`, strip rows and plan layer | Provisional and empty-exceeded checks; no display-text classification. |
| Aerodynamic outputs | Stored Γ, downwash, span edges → Trefftz lift, drag and e; near-field forces → displayed wing CL and lift | Trefftz-lift and excluded-strip checks compare direct arithmetic with the displayed rows. |
| Copy | `Labels` constants → Analysis rows; `DESIGN.md` §7 proposed rows | `Copy_AnalysisStrings_MatchDesignMd`, measured-band and fixture-scope checks. |

## Limits

The h/c = 5 display threshold remains a marked assumption with the experimental source and confirmation path
in design §5.4. The tip-law indeterminate state awaits DR-VLM-4. This repair does not assign a tip law.

## Gate record

`tools/run-tests.sh` ran once: Core parts 334 + 333 PASS (74 s each), Desktop 610 PASS (72 s),
Analysis 108 PASS (6 s), CLI 5 PASS (2 s); all suites reported zero failures. It returned **3** on
`TEST-BUDGET`: wall 80 s versus the 60 s cap, CPU 748 s, machine load 7.59 → 23.17. Six `simpleFoam`
processes were then observed at about 96–98% CPU each, with elapsed time 7 h 56 min. The only Core source
diff in this track is the optional field pair in `RunRecord.cs`; no Core test changed. This evidence supports
external CPU contention as the timing cause, but no uncontended full-ring comparison was made. The full ring
preceded the partial-edge guard; its focused red→green check passed afterward.

The final standalone Analysis harness returned 0 with `RESULT failures=0`, and its refreshed log fed the
named-test checker. `check-named-tests.py PRJ`: 41/41 PASS, 0 failures. `check-docs.py`: exit 0, 0 graph defects, 131 existing
review suggestions. `check-event-subscribers.py`: 27 events, 4 allowed, 0 findings. After the partial-edge
guard, the targeted Analysis check printed `RESULT failures=0` for legacy, malformed and excluded widths.
