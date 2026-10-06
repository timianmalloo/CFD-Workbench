---
id: proof-a3a-pack
title: A3a Proof Pack (wing VLM + strip, inviscid): tracks, tests, residuals and the operator demo
type: proof-pack
status: active
owner: "@trk-aux"
phase: implementation
tags: [a3a, aux, proof, analysis, demo]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: review-a3a-native, rel: relates-to }
  - { to: proof-a3a-lay-pack, rel: relates-to }
review-by: 2026-11-05
summary: >-
  What slice A3a built, the tests that prove it (per-track named totals, receipts, mutants), the two persona verdicts,
  the residuals, and the script for the operator's morning demo of the packaged macOS app.
---

# A3a Proof Pack

Gate (design section 16): ring-0 rows of sections 13.2, 13.3 and 13.5 marked A3a, plus the operator demo.
Head under proof: branch `feature/a3a-aux`, from main `75ebb7c6` plus the AUX commit.

## 1. What A3a is

Toggle CAD | Analysis, conditions band (Custom operating point), Evaluate, VLM + strip lift / induced drag / loads with the
method envelope, layers on Plan, 3D, Side and Front, Properties wing and strip results, the Loads and Provenance tabs, the
Historical state, and `-2` run persistence. Profile drag, section Cp and cavitation, Find alpha and Compare are later slices
and read **Unavailable** with a reason.

## 2. Tracks, as merged

| Track | What it delivered | Receipt |
|---|---|---|
| PRE | contracts, Analysis assembly skeleton, ADR 0011, harness | `docs/proof/a3a-pre/red-first.md` |
| RNG (B1, B2, B4) | ms ring clocks, check-test-costs C-2..C-6, Analysis harness in two parts | merges `f63f97f2`, `ef9c3f6e`, `90947022` |
| COR | `Placement.Sections` (one placement rule) | `a3a-cor/red-first.md` |
| STO | run storage, JCS form, run key, content hash, `-2` round trip | `a3a-sto/red-first.md` |
| VLM | lattice, solve, forces, Trefftz, fixtures F-1..F-16 | `a3a-vlm/`, `a3a-vlm3/` |
| STP | strip coupling, local-chord Re (BC-4), water table | `a3a-stp/`, `a3a-water-table/` |
| SVC | service, freshness, CLI | `a3a-svc/`, `a3a-svc2/` |
| PRJ | projection, labels, copy (COPY-206..240) | `a3a-prj2/proof-pack.md` (no `a3a-prj` red-first file, see residual R3) |
| TGL | toggle, conditions band, Evaluate/Cancel, status item, inert edit refusal | `a3a-tgl/red-first.md` |
| CTX | projection feed (stations, verdicts, root t/c), layer visibility | `a3a-ctx/red-first.md` |
| PNA | Layers pane, bottom panel with chart twin, Properties analysis groups | `a3a-pna/red-first.md` |
| HIST | selected-run feed from its own revision, previous-Completed after Failed, `LayersChanged` | `a3a-hist/red-first.md` |
| LAY | Plan, 3D, Side/Front layers from the selected run's feed | `a3a-lay/proof-pack.md` |
| LIM | planform limits felt during the gesture (Ruling 96) | merge `e556f66f` |
| AUX | this review, polish, captures, the pack | `docs/reviews/a3a-native.md` |

Rulings that govern the build: 82 (Area 3 copy, COPY-206..240), 92 (tip strip "Not judged"; retires COPY-218/219), 93-95
(tip chord minimum, spec 1.7.4), 94 and 96 (tip and root limits, COPY-241..249), 87 and 99 (ring budget and load gates).

## 3. Test evidence

One full `tools/run-tests.sh` at the AUX tree: **exit 0, "all test harnesses passed", wall 50 s of a 60 s budget**.
PASS counts: Core 234 + 233 + 233, Desktop 710 (its `--analysis` part 9.0 s), Analysis 67 + 89, Cli 6. Test costs: 0 failures,
0 COST-MISS.

Named tests per track (design section 18.8, `tools/check-named-tests.py`; defined, and PASS in the ring-0 logs):

| Track | Defined | PASS in ring 0 | Note |
|---|---|---|---|
| PRE | 1 | 1 | |
| COR | 2 | 2 | |
| STO | 14 | 12 | `Retention_PruneThenUndo_TombstoneReadsPruned`, `Store_HundredThousandStrips_RefusedDocSize` run in the readiness ring |
| VLM | 23 | 16 | F-2, F-5 and five `Readiness_*` run in the readiness ring |
| STP | 13 | 13 | |
| SVC | 27 | 26 | `Freshness_ProfileEdit_Historical` runs in the readiness ring |
| PRJ | 41 | 41 | |
| TGL | 12 | 11 | `Toggle_LayersFirstFrame_P95WithinPreviewBudget` runs in the readiness ring |
| LAY | 1 | 1 | |
| PNA | 5 | 5 | |
| **Total** | **139** | **128 in ring 0 + 11 in readiness = 139** | CTX, HIST, LIM, AUX own no ledger names |

All 139 names exist in `tests/` (test architect, grep). The 11 readiness-ring names each printed `PASS` in the readiness run (`tools/run-readiness.py`, GREEN, 147 s of a 240 s budget): checked by grep of its log.

Mutants: BC-1 requires one planted mutant per ledger row; the receipts are the `red-first.md` files above. Missing receipts
(test architect finding T4) are the PRJ rows named in `docs/reviews/a3a-native.md` section 6.

## 4. Readiness ring

`python3 tools/run-readiness.py` then `python3 tools/run-readiness.py --check` for the A3a head: GREEN for the AUX commit
(receipt `.tmp-tests/readiness.json`, bound to that commit hash; the hash is in the AUX return).

## 5. Persona verdicts (AUX)

- **Hydrodynamicist:** PASS-WITH-CONDITIONS, no blocker. One Major needs a ruling: "deep water" prints at depth set even when
  h/c is below 5 (`Labels.cs:25`, `AnalysisProjection.cs:32`). Details in `docs/reviews/a3a-native.md` section 5.
- **Test architect (build-time veto):** PASS-WITH-CONDITIONS; veto cleared with conditions. Open: Tampered-run view and Preview
  hidden banner have no test through the UI; the ANA-23 vectors test does not discriminate its mutant; PRJ mutant receipts
  incomplete. Details in section 6 of the review file.

## 6. Residuals

- **R1 (LAY, accepted).** After a geometry edit a selected Historical run keeps its own values, strip spans and local normals,
  but the glyph anchors on the 3D and Plan layers use the current accepted geometry, so an anchor can sit off the earlier
  surface. The Historical banner stays visible. Replaying the exact earlier mesh is a later option (`a3a-lay/proof-pack.md`).
- **R2 (CTX, decided).** Strip verdicts, normals and stations are derived on read from the selected run and its revision
  (design section 3, derive don't store); nothing derived is stored in the run payload. Cost: the feed is recomputed on a
  selection change; benefit: one definition, no stale copies. CTX fixed "every strip reads Not judged".
- **R3.** No PRJ red-first receipt for several rows (test architect T4).
- **R4.** Tampered-run and Preview-hidden screens: no capture, no UI test.
- **R5.** Four views fall back to One view at 1280 x 800 in Analysis (AUX-F1).
- **R6.** Depth label at h/c below 5 (hydrodynamicist H1); copy strings with no approved row (review file section 7).
- **R7.** DR-ANA-14 bounds are Inferred until polar brackets land; Windows parity untested; the packaged app is unsigned and
  framework-dependent (needs the .NET 10 runtime); packaged-app input was not driven in this session (no Accessibility
  permission), so screens are in-process renders.
- Not in A3a: profile drag, section Cp, cavitation screen (A3b/A3c), Find alpha, Compare, goal-state operating points,
  free-surface correction layer, assistant entry.

## 7. Operator demo script (packaged macOS app)

App: `/Users/mallalieut/projects/CFD-Workbench-review-apps/<short-sha>/CFD Workbench.app` (path and sha in the audit entry and
the AUX return). Unsigned: first open with right-click, Open. **Make the window at least 1500 x 870** (zoom or drag), or only
the Plan view draws layers (AUX-F1).

| Step | Click | Expect |
|---|---|---|
| 1 | Start screen, **New from example** | Example foil opens; Properties shows the foil; status strip reads "Analysis: no result" |
| 2 | Model-area navbar, **Analysis** (CAD \| Analysis) | Conditions band appears (Speed 5.14 m/s, Salt, Depth "Not set", alpha 2.00, Evaluate); camera, selection and layout stay; points dim; bottom panel shows "No analysis yet. Set the conditions, then Evaluate." |
| 3 | Click **Evaluate** | Brief "Evaluating — VLM + strip · n panels…", then "Analysis complete — VLM + strip · t s"; Plan gains batlow strips and the loading curve; band shows q, Re_ref, sigma as "Unavailable — depth not set" |
| 4 | Type **0.6** in Depth h_ref, **Evaluate** | q, Re_ref, h/c, Fr_h, sigma show values; 3D shows lift arrows, total L, moment arc, tip depth |
| 5 | Properties, scroll the one column (its bar is visible) | Tier chip "VLM + strip · local calculation", then CL, CDi, e, L, D_i; Total drag Unavailable naming profile, junction, mast, wave, spray; Labels header and first rows in view, Basis, Not modelled and the Depth rows one scroll below |
| 6 | Bottom panel tabs: **Spanwise loading**, **Loads**, **Provenance** | chart with the elliptic reference and **Show table**; Loads with "Structural: Not assessed" and the safety copy; Provenance with the Run manifest |
| 7 | Browser or Plan: select the **tip station** | Properties "Strip of wing run (alpha_eff)"; Envelope row "Not judged — tip strip"; Section tab Unavailable with its reason |
| 8 | Set alpha **12**, Evaluate | "Outside the method envelope"; Plan marks strips with dashed outlines and a count |
| 9 | Layers tab (left bar) | one check per layer; unchecking one removes it from the view |
| 10 | Toggle to **CAD**, change Span in Properties, return to **Analysis** | banner "Historical — geometry changed (r1 → r2)" on the area; layers stay until the next Evaluate |
| 11 | In Analysis, press a point | status reads "Points are edited in CAD. Switch with the CAD \| Analysis toggle." |

Known on screen (do not be surprised): the 3D labels, loading chart axes and Plan legend ramp were fixed by the POL polish
(review file section 4a). Still open: the Envelope and Total drag cells are cut at the Properties cell edge (AUX-F8), and
the chart's x tick row is cut while an Evaluate runs.
