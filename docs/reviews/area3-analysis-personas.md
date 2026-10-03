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
  review is recorded at the end.
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
