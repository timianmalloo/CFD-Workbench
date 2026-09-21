---
id: decision-loft-rule-a
title: Loft rule A: the record is channel-evaluated; skins are derived
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [geometry, loft, export]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
  - {to: review-spec-v02-critique, rel: relates-to}
review-by: 2027-03-19
summary: The surface of record between stations is the channel-evaluated analytic surface; every B-spline skin (display, STEP, 3DM) is a derived approximation with a measured, reported deviation. Rule B (skin as record) is not offered.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)." }
---

# Loft rule A: the record is channel-evaluated; skins are derived

**Decision.** The Surface revision stores five degree-5 B-spline channels, the authored stations, the blend and
correspondence rules and the evaluator id + version (spec v1 A4.1). The surface *between* stations is defined by
evaluating those channels — the twist rotation makes it non-polynomial in η — so no B-spline skin can be the
record without a refit. Any skin written for display, STEP AP203 or 3DM is a **derived approximation** whose
maximum deviation from the record is measured and reported like any other conversion (A4.4, A4.6, A4.11).

**Why.** Two authorities for one shape is the defect signature the whole geometry contract exists to prevent
(KB-1; DM7 "derive, don't store"). Rule B would make the exported skin the truth and the channels a memoir, so an
edit to a channel and a re-export could disagree by an unreported amount. Under rule A, promotion of an inspection
slice to a station is exact (GEO-06 ≤ 10⁻¹² relative) and the identity oracle (A4.5) has one object to test.

**Rejected.** Rule B (skin as record); a hybrid that stores both and reconciles on load (two authorities plus a
reconciliation rule nobody can test); exposing Loose/Tight/Rebuild/Refit vocabulary (03).

**What it constrains.** The `.cfdw.json` payload (nothing evaluated is persisted as authority); the STEP writer
(gated until open-and-measure in two CAM systems); the A-vs-B fixture in Open decisions, which measures the skin
deviation on a representative wing and is the exit evidence for the loft skin's v-degree and parameterisation.

**Confidence.** Verified for the geometric argument (02 §5, executed probe on knot insertion exactness); Inferred
for the deviation magnitude on production wings until the A-vs-B fixture runs. Owner lens: Computational Geometry.
